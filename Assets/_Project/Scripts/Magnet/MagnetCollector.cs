using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using DG.Tweening;

/// Coklu hedefli "magnet" toplama efekti.
/// Her grup (Row 1, Row 2, Row 3) kendi target SLOT'una sahiptir.
/// target'in cocuklari (TargetBall_1, _2, _3...) otomatik olarak landing pozisyonu sayilir;
/// ball[i] -> target.GetChild(i) eslesmesi yapilir.
/// Akis: kickback -> sin parabolic pull (slot'un BIRAZ USTUNDE biter) ->
///       lift (yumusak yukari) -> drop (yerleşme) -> hit/trail/destroy.
[DisallowMultipleComponent]
public class MagnetCollector : MonoBehaviour
{
    [System.Serializable]
    public class CollectGroup
    {
        [Tooltip("Inspector'da ayirt etmek icin etiket (orn 'Row 1').")]
        public string name = "Row";

        [Tooltip("Bu grubun hedef slot'u. Bu transform'un cocuklari otomatik landing pozisyonu olarak kullanilir.")]
        public Transform target;

        [Tooltip("Doluysa bu transform'un cocuklari toplanir, balls listesi yok sayilir.")]
        public Transform source;

        [Tooltip("Spesifik ballar. Doluysa source yok sayilir.")]
        public List<Transform> balls = new List<Transform>();
    }

    [Header("Groups (each row -> own target)")]
    [Tooltip("Tum gruplar AYNI anda baslar. Grup icindeki ball[i] target.GetChild(i)'ye gider.")]
    public List<CollectGroup> groups = new List<CollectGroup>();

    [Header("Auto Repair")]
    [Tooltip("Target referanslari kaybolursa Collect baslamadan TargetSlot, TargetSlot (1), TargetSlot (2) isimlerinden otomatik bulur.")]
    public bool autoResolveMissingTargets = true;
    public string targetSlotBaseName = "TargetSlot";

    [Header("Timing")]
    [Min(0.05f)] public float pullDuration = 0.55f;
    [Tooltip("Grup icinde balllar arasi kucuk gecikme. 0 = hepsi ayni anda.")]
    [Min(0f)] public float stagger    = 0.0f;
    [Min(0f)] public float startDelay = 0f;

    [Header("Magnet Anticipation (kickback)")]
    [Min(0f)] public float kickbackDistance = 0.4f;
    [Min(0f)] public float kickbackDuration = 0.12f;
    public Ease            kickbackEase     = Ease.OutQuad;

    [Header("Pull (Parabolic Arc)")]
    [Tooltip("Pull boyunca uygulanacak ease. Sona dogru yavaslamasi icin OutQuad/OutCubic onerilir.")]
    public Ease  pullEase  = Ease.OutQuad;
    [Tooltip("Apex (yayin tepe noktasi) yuksekligi. 0 = duz cizgi.")]
    [Min(0f)] public float arcHeight = 2f;
    [Tooltip("Yay yonu (normalize edilir). Default Vector3.up.")]
    public Vector3 arcDirection = Vector3.up;
    [Tooltip("Mesafe ile orantili ek apex. 0 = sadece arcHeight.")]
    [Min(0f)] public float arcHeightDistanceFactor = 0.0f;

    [Header("Settle (Soft Landing)")]
    [Tooltip("Pull, slot'un BU KADAR USTUNDE biter (settle baslama yuksekligi).")]
    [Min(0f)] public float pullEndHeight = 0.3f;
    [Tooltip("Settle sirasinda slot'un USTUNE bu kadar daha yukari cikar (lift bounce).")]
    [Min(0f)] public float settleBounceHeight = 0.25f;
    [Min(0f)] public float settleDuration = 0f;
    [Tooltip("Lift fazinin toplam settle icindeki orani (drop = 1 - lift).")]
    [Range(0.1f, 0.9f)] public float settleLiftRatio = 0.45f;
    public Ease settleLiftEase = Ease.OutSine;
    public Ease settleDropEase = Ease.OutQuad;
    [Tooltip("Settle ekseni (yukari cikis yonu). Default Vector3.up.")]
    public Vector3 settleAxis = Vector3.up;

    [Header("Arrival Scale")]
    [FormerlySerializedAs("scaleDownOnArrive")]
    public bool    animateScale = true;
    [Tooltip("Pull sonu olcek (kullaniciya yakin oldugu icin 1.2 onerilir).")]
    [FormerlySerializedAs("arriveScale")]
    public Vector3 arriveScale  = new Vector3(1.2f, 1.2f, 1.2f);
    public Ease    scaleEase    = Ease.OutBack;

    [Header("Arrival Hierarchy")]
    [Tooltip("Slot yok edilmeyecekse top yerine yerleşince landing slot'una parentlanir.")]
    public bool parentToSlotOnArrive = true;
    [Tooltip("Top yok edilsin mi? Slot ile beraber animate edilecekse FALSE birak.")]
    public bool destroyOnArrive = false;

    [Header("Group Complete Vanish")]
    [Tooltip("Grubun tum ballari yerine yerleşince TargetSlot ve ballar kendi local scale'lerinde beraber scale up/down yapar.")]
    public bool  playSlotPop      = true;
    [Tooltip("Tum ballar target'a yerlestikten sonra scale up/down animasyonuna baslamadan once beklenecek sure.")]
    [Min(0f)] public float groupCompleteDelay = 0f;
    [Tooltip("Opsiyonel: Toplar target'a oturduktan sonra scale down oncesi tekrar yukari ciksin. Landing flare icin kapali birak.")]
    public bool liftBallsBeforeVanish = false;
    [Min(0f)] public float ballLiftBeforeVanishHeight = 0.35f;
    [Min(0f)] public float ballLiftBeforeVanishDuration = 0.14f;
    public Ease ballLiftBeforeVanishEase = Ease.OutQuad;
    [Tooltip("Scale down oncesi yukari cikis ekseni. Default Vector3.up.")]
    public Vector3 ballLiftBeforeVanishAxis = Vector3.up;
    [Tooltip("Scale up miktari. 0.2 = once %120'ye cikar.")]
    [Min(0f)]    public float slotPopScale       = 0.2f;
    [Min(0f)] public float slotPopDuration    = 0.08f;
    public Ease slotPopEase = Ease.OutBack;
    [Tooltip("TargetSlot ve icindeki ballar grup tamamlaninca kuculup yok edilsin.")]
    public bool destroySlotOnComplete = true;
    [Min(0f)] public float slotShrinkDuration = 0.18f;
    public Ease slotShrinkEase = Ease.InBack;

    [Header("VFX - Trail")]
    public GameObject trailPrefab;
    [Min(0f)] public float trailFadeOut = 0.5f;

    [Header("VFX - On Hit")]
    public GameObject hitEffectPrefab;
    [Min(0f)] public float hitEffectLifetime = 2f;
    public bool hitEffectParentToTarget = false;
    [Tooltip("True ise hit FX pull biter bitmez, settle/hava hareketinden once oynar.")]
    public bool playHitEffectBeforeSettle = true;

    [Header("VFX - On Vanish")]
    [Tooltip("Toplar scale down olduktan hemen sonra her top pozisyonunda spawn edilir.")]
    public GameObject ballVanishEffectPrefab;
    [Min(0f)] public float ballVanishEffectLifetime = 2f;
    [Tooltip("TargetSlot scale down olduktan hemen sonra slot pozisyonunda spawn edilir.")]
    public GameObject slotVanishEffectPrefab;
    [Min(0f)] public float slotVanishEffectLifetime = 2f;

    [Header("Events")]
    public UnityEvent<Transform> onBallArrived;
    public UnityEvent            onAllArrived;

    public void Collect()
    {
        if (autoResolveMissingTargets)
            ResolveMissingTargets();

        int totalRemaining = 0;
        foreach (var g in groups)
        {
            if (g == null || g.target == null) continue;
            totalRemaining += ResolveGroupBalls(g).Count;
        }
        if (totalRemaining == 0) return;

        foreach (var g in groups)
        {
            if (g == null || g.target == null) continue;
            var list           = ResolveGroupBalls(g);
            var groupTarget    = g.target;
            var arrivedBalls   = new List<Transform>(list.Count);
            int groupRemaining = list.Count;

            for (int i = 0; i < list.Count; i++)
            {
                var ball = list[i];
                if (ball == null)
                {
                    groupRemaining--;
                    totalRemaining--;
                    if (groupRemaining == 0) PlayGroupComplete(groupTarget, arrivedBalls);
                    continue;
                }

                Transform slotTransform = ResolveSlot(groupTarget, i);
                if (slotTransform == null)
                {
                    groupRemaining--;
                    totalRemaining--;
                    if (groupRemaining == 0) PlayGroupComplete(groupTarget, arrivedBalls);
                    continue;
                }

                float delay = startDelay + i * stagger;

                AnimateBall(ball, slotTransform, delay, () =>
                {
                    onBallArrived?.Invoke(ball);
                    if (ball != null)
                        arrivedBalls.Add(ball);

                    if (--groupRemaining == 0) PlayGroupComplete(groupTarget, arrivedBalls);
                    if (--totalRemaining == 0) onAllArrived?.Invoke();
                });
            }
        }
    }

    [ContextMenu("Resolve Missing Targets")]
    void ResolveMissingTargets()
    {
        if (groups == null || groups.Count == 0) return;

        for (int i = 0; i < groups.Count; i++)
        {
            var group = groups[i];
            if (group == null || group.target != null) continue;

            Transform target = FindTargetSlot(i, group.name);
            if (target != null)
                group.target = target;
        }
    }

    Transform FindTargetSlot(int groupIndex, string groupName)
    {
        int slotIndex = ResolveSlotIndex(groupIndex, groupName);
        string slotName = slotIndex == 0 ? targetSlotBaseName : $"{targetSlotBaseName} ({slotIndex})";
        GameObject slotObject = GameObject.Find(slotName);
        return slotObject != null ? slotObject.transform : null;
    }

    int ResolveSlotIndex(int fallbackIndex, string groupName)
    {
        if (!string.IsNullOrEmpty(groupName))
        {
            int end = groupName.Length - 1;
            while (end >= 0 && char.IsWhiteSpace(groupName[end])) end--;

            int start = end;
            while (start >= 0 && char.IsDigit(groupName[start])) start--;

            if (start < end && int.TryParse(groupName.Substring(start + 1, end - start), out int parsedIndex))
                return Mathf.Max(0, parsedIndex - 1);
        }

        return Mathf.Max(0, fallbackIndex);
    }

    void PlayGroupComplete(Transform slotParent, List<Transform> arrivedBalls)
    {
        if (slotParent == null || (!playSlotPop && !destroySlotOnComplete)) return;

        List<Transform> scaleTargets = new List<Transform>();
        List<Vector3> originalScales = new List<Vector3>();
        AddScaleTarget(slotParent, scaleTargets, originalScales);

        if (arrivedBalls != null)
        {
            foreach (Transform ball in arrivedBalls)
                AddScaleTarget(ball, scaleTargets, originalScales);
        }

        Sequence seq = DOTween.Sequence().SetTarget(slotParent).SetDelay(groupCompleteDelay);

        if (liftBallsBeforeVanish && arrivedBalls != null && ballLiftBeforeVanishHeight > 0f && ballLiftBeforeVanishDuration > 0f)
        {
            Vector3 liftAxis = ballLiftBeforeVanishAxis.sqrMagnitude > 1e-6f
                ? ballLiftBeforeVanishAxis.normalized
                : Vector3.up;

            bool appended = false;
            foreach (Transform ball in arrivedBalls)
            {
                if (ball == null)
                    continue;

                Tween tween = ball.DOMove(ball.position + liftAxis * ballLiftBeforeVanishHeight, ballLiftBeforeVanishDuration)
                    .SetEase(ballLiftBeforeVanishEase);

                if (!appended)
                {
                    seq.Append(tween);
                    appended = true;
                }
                else
                {
                    seq.Join(tween);
                }
            }
        }

        if (playSlotPop && slotPopDuration > 0f)
        {
            bool appended = false;
            for (int i = 0; i < scaleTargets.Count; i++)
            {
                Transform target = scaleTargets[i];
                Vector3 popScale = originalScales[i] * (1f + slotPopScale);
                Tween tween = target.DOScale(popScale, slotPopDuration).SetEase(slotPopEase);

                if (!appended)
                {
                    seq.Append(tween);
                    appended = true;
                }
                else
                {
                    seq.Join(tween);
                }
            }
        }

        if (destroySlotOnComplete)
        {
            if (slotShrinkDuration > 0f)
            {
                bool appended = false;
                for (int i = 0; i < scaleTargets.Count; i++)
                {
                    Transform target = scaleTargets[i];
                    Tween tween = target.DOScale(Vector3.zero, slotShrinkDuration).SetEase(slotShrinkEase);

                    if (!appended)
                    {
                        seq.Append(tween);
                        appended = true;
                    }
                    else
                    {
                        seq.Join(tween);
                    }
                }
            }
            else
            {
                seq.AppendCallback(() =>
                {
                    foreach (Transform target in scaleTargets)
                    {
                        if (target != null)
                            target.localScale = Vector3.zero;
                    }
                });
            }

            seq.OnComplete(() =>
            {
                SpawnVanishEffects(slotParent, arrivedBalls);

                if (arrivedBalls != null)
                {
                    foreach (Transform ball in arrivedBalls)
                    {
                        if (ball != null)
                            Destroy(ball.gameObject);
                    }
                }

                if (slotParent != null)
                    Destroy(slotParent.gameObject);
            });
        }
        else
        {
            if (slotPopDuration > 0f)
            {
                bool appended = false;
                for (int i = 0; i < scaleTargets.Count; i++)
                {
                    Transform target = scaleTargets[i];
                    Tween tween = target.DOScale(originalScales[i], slotPopDuration).SetEase(Ease.OutQuad);

                    if (!appended)
                    {
                        seq.Append(tween);
                        appended = true;
                    }
                    else
                    {
                        seq.Join(tween);
                    }
                }
            }
        }
    }

    void SpawnVanishEffects(Transform slotParent, List<Transform> arrivedBalls)
    {
        if (slotVanishEffectPrefab != null && slotParent != null)
            SpawnEffect(slotVanishEffectPrefab, slotParent.position, slotParent.rotation, slotVanishEffectLifetime);

        if (ballVanishEffectPrefab == null || arrivedBalls == null) return;

        foreach (Transform ball in arrivedBalls)
        {
            if (ball != null)
                SpawnEffect(ballVanishEffectPrefab, ball.position, ball.rotation, ballVanishEffectLifetime);
        }
    }

    void SpawnEffect(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime)
    {
        GameObject effect = Instantiate(prefab, position, rotation);
        if (lifetime > 0f)
            Destroy(effect, lifetime);
    }

    void AddScaleTarget(Transform target, List<Transform> scaleTargets, List<Vector3> originalScales)
    {
        if (target == null) return;

        target.DOKill(false);
        scaleTargets.Add(target);
        originalScales.Add(target.localScale);
    }

    /// target'in i'nci child'ini landing pozisyonu olarak dondurur.
    /// Cocuk yoksa target'in kendisini kullanir.
    Transform ResolveSlot(Transform target, int ballIndex)
    {
        if (target.childCount == 0) return target;
        int idx = ballIndex % target.childCount;
        return target.GetChild(idx);
    }

    List<Transform> ResolveGroupBalls(CollectGroup g)
    {
        if (g.balls != null && g.balls.Count > 0)
        {
            var copy = new List<Transform>(g.balls.Count);
            foreach (var t in g.balls) if (t != null) copy.Add(t);
            return copy;
        }
        if (g.source != null)
        {
            var result = new List<Transform>(g.source.childCount);
            for (int i = 0; i < g.source.childCount; i++)
                result.Add(g.source.GetChild(i));
            return result;
        }
        return new List<Transform>();
    }

    void AnimateBall(Transform ball, Transform slot, float delay, System.Action onDone)
    {
        if (ball == null || slot == null)
        {
            onDone?.Invoke();
            return;
        }

        Vector3 startPos = ball.position;
        Vector3 awayDir  = startPos - slot.position;
        if (awayDir.sqrMagnitude < 1e-6f) awayDir = Vector3.up;
        awayDir.Normalize();

        Vector3 kickPos = startPos + awayDir * kickbackDistance;

        ball.DOKill(false);

        GameObject trailInstance = null;
        bool hitEffectPlayed = false;

        // Pull dinamik degerleri (OnStart'ta yakalanir)
        Vector3 fromPos = startPos;
        Vector3 toPos   = slot.position;
        Vector3 axis    = arcDirection.sqrMagnitude > 1e-6f ? arcDirection.normalized : Vector3.up;
        Vector3 sAxis   = settleAxis.sqrMagnitude > 1e-6f ? settleAxis.normalized : Vector3.up;
        float   apexHeight = 0f;

        Sequence seq = DOTween.Sequence().SetTarget(ball).SetDelay(delay);

        // Trail spawn
        seq.OnStart(() =>
        {
            if (trailPrefab != null && ball != null)
            {
                trailInstance = Instantiate(trailPrefab, ball.position, Quaternion.identity, ball);
                trailInstance.transform.localPosition = Vector3.zero;
                trailInstance.transform.localRotation = Quaternion.identity;
            }
        });

        // 1) Kickback
        if (kickbackDistance > 0f && kickbackDuration > 0f)
            seq.Append(ball.DOMove(kickPos, kickbackDuration).SetEase(kickbackEase));

        // 2) Pull: sin parabolik. END = slot + sAxis * pullEndHeight (slot ustunde)
        Tween pullTween = DOVirtual.Float(0f, 1f, pullDuration, t =>
        {
            if (ball == null || slot == null) return;
            Vector3 linear = Vector3.Lerp(fromPos, toPos, t);
            ball.position = linear + axis * (Mathf.Sin(t * Mathf.PI) * apexHeight);
        })
        .SetEase(pullEase)
        .SetTarget(ball)
        .OnStart(() =>
        {
            fromPos    = ball.position;
            toPos      = slot != null ? slot.position + sAxis * pullEndHeight : ball.position;
            float dist = Vector3.Distance(fromPos, toPos);
            apexHeight = arcHeight + arcHeightDistanceFactor * dist;
        });
        seq.Append(pullTween);

        if (playHitEffectBeforeSettle)
            seq.AppendCallback(() => hitEffectPlayed = PlayHitEffect(slot, hitEffectPlayed));

        // 2.5) Pull ile paralel scale
        if (animateScale)
            seq.Join(ball.DOScale(arriveScale, pullDuration).SetEase(scaleEase));

        // 3) Settle: lift (yukari kalk) -> drop (slot'a otur)
        // Pozisyonlar sequence build time'da yakalanir (slot statik kabul ediliyor).
        if (settleDuration > 0f)
        {
            Vector3 finalSlotPos = slot.position;
            float   liftDur      = settleDuration * settleLiftRatio;
            float   dropDur      = settleDuration * (1f - settleLiftRatio);

            if (settleBounceHeight > 0f && liftDur > 0.01f)
            {
                Vector3 peakPos = finalSlotPos + sAxis * (pullEndHeight + settleBounceHeight);
                seq.Append(ball.DOMove(peakPos, liftDur).SetEase(settleLiftEase));
            }
            else
            {
                dropDur = settleDuration; // bounce yok ise tum sure drop'a verilir
            }

            seq.Append(ball.DOMove(finalSlotPos, dropDur).SetEase(settleDropEase));
        }

        seq.OnComplete(() =>
        {
            hitEffectPlayed = PlayHitEffect(slot, hitEffectPlayed);

            // Trail unparent + delayed destroy
            if (trailInstance != null)
            {
                trailInstance.transform.SetParent(null, true);
                if (trailFadeOut > 0f) Destroy(trailInstance, trailFadeOut);
            }

            if (parentToSlotOnArrive && !destroySlotOnComplete && ball != null && slot != null)
            {
                ball.SetParent(slot, true);
                ball.position = slot.position;
            }

            onDone?.Invoke();

            if (destroyOnArrive && !destroySlotOnComplete && ball != null)
                Destroy(ball.gameObject);
        });
    }

    bool PlayHitEffect(Transform slot, bool hitEffectPlayed)
    {
        if (hitEffectPlayed || hitEffectPrefab == null || slot == null)
            return hitEffectPlayed;

        Transform parent = hitEffectParentToTarget ? slot : null;
        GameObject hit = Instantiate(hitEffectPrefab, slot.position, Quaternion.identity, parent);
        if (hitEffectLifetime > 0f)
            Destroy(hit, hitEffectLifetime);

        return true;
    }

    [ContextMenu("Test Collect")]
    void TestCollect() => Collect();

    void OnDisable()
    {
        DOTween.Kill(this);
    }
}
