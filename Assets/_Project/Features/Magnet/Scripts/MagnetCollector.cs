using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using DG.Tweening;

/// Coklu hedefli "magnet" toplama efekti.
/// Her grup (Row 1, Row 2, Row 3) kendi target SLOT'una sahiptir.
/// target'in cocuklari (TargetBall_1, _2, _3...) otomatik olarak landing pozisyonu sayilir;
/// ball[i] -> target.GetChild(i) eslesmesi yapilir.
/// Akis: target pulse -> lift/reach/stretch -> release -> upward snap pull (slot'un BIRAZ USTUNDE biter) ->
///       IMPACT PUNCH (squash > overshoot > settle) -> hit/trail/destroy.
/// v2: Impact moment polish - casual game juiciness
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

    [Header("00 - Runtime Tuning")]
    [Tooltip("Play Mode'da deneme yapmak icin ayarlari buradan okuyabilir. Project > Create > Marble Sort > Magnet Collect Feel Profile ile olustur.")]
    public MagnetCollectSettings settingsProfile;
    [Tooltip("Collect baslamadan hemen once settingsProfile degerlerini bu componente uygular.")]
    public bool useSettingsProfile = true;

    [Header("01 - Ball Groups")]
    [Tooltip("Tum gruplar AYNI anda baslar. Grup icindeki ball[i] target.GetChild(i)'ye gider.")]
    public List<CollectGroup> groups = new List<CollectGroup>();

    [Header("02 - Timing")]
    [Tooltip("Sadece toplarin target'a gidis hizi (dunya birimi/saniye). Dusurmek sadece Snap Pull hareketini yavaslatir.")]
    [HideInInspector] [Min(0.01f)] public float ballTravelSpeed = 8f;
    [Tooltip("Ayni gruptaki toplar arasindaki kucuk gecikme. 0 = hepsi ayni anda.")]
    [HideInInspector] [Min(0f)] public float stagger = 0.06f;
    [Tooltip("Magnet collect baslamadan once genel bekleme.")]
    [HideInInspector] [Min(0f)] public float startDelay = 0f;
    [Tooltip("Ekranda yukari gibi gorunen dunya ekseni. Bu sahnede Z- yukari hissi veriyor.")]
    [HideInInspector] public Vector3 visualLiftAxis = Vector3.back;

    [HideInInspector]
    public bool autoResolveMissingTargets = true;
    [HideInInspector]
    public string targetSlotBaseName = "TargetSlot";

    [Header("03 - Anticipation")]
    [Tooltip("Toplar firlamadan once target slotlarin pulse atmasini saglar.")]
    [HideInInspector] public bool useCasualAnticipation = true;
    [Tooltip("Target slotlar firlamadan once kisa pulse atsin.")]
    [HideInInspector] public bool pulseTargetBeforeLaunch = true;
    [Tooltip("Target slot pulse buyume miktari. 0.16 = %16 buyume.")]
    [HideInInspector] [Min(0f)] public float targetPulseScale = 0.25f;
    [Tooltip("Target slot pulse suresi.")]
    [HideInInspector] [Min(0f)] public float targetPulseDuration = 0.3f;
    [Tooltip("Toplar cekilmeden once yukari ne kadar kalkar.")]
    [HideInInspector] [Min(0f)] public float preLaunchLiftHeight = 0.45f;
    [Tooltip("Toplar cekilmeden once target'a dogru ne kadar uzanir.")]
    [HideInInspector] [Min(0f)] public float preLaunchReachDistance = 0.14f;
    [Tooltip("Ilk kalkistan itibaren topun cekiliyormus gibi 2D stretch scale'i. X sikisir, Y uzar, Z 1 kalir.")]
    [HideInInspector] public Vector3 preLaunchStretchScale = new Vector3(0.78f, 1.35f, 1f);
    [Tooltip("Yukari kalkma ve target'a dogru sunme suresi.")]
    [HideInInspector] [Min(0f)] public float preLaunchDuration = 0.28f;
    [HideInInspector] [Min(0f)] public float airFloatDuration = 0f;
    [HideInInspector] [Min(0f)] public float airFloatLiftHeight = 0f;
    [HideInInspector] [Min(0f)] public float airFloatReachDistance = 0f;
    [HideInInspector] [Min(0f)] public float airFloatPullDistance = 0f;
    [HideInInspector] public Vector3 airFloatChargeScale = new Vector3(0.65f, 1.50f, 1f);
    [HideInInspector] [Min(0f)] public float launchTensionDuration = 0f;
    [HideInInspector] public Vector3 launchTensionScale = new Vector3(0.78f, 1.35f, 1f);
    [Tooltip("Sunmeden sonra firlamadan onceki minicik bekleme.")]
    [HideInInspector] [Min(0f)] public float launchHoldDuration = 0.02f;

    [HideInInspector] public Ease targetPulseEase = Ease.OutBack;
    [HideInInspector] public bool stretchBallOnPreLaunch = true;
    [HideInInspector] public Ease preLaunchMoveEase = Ease.OutSine;
    [HideInInspector] public Ease preLaunchScaleEase = Ease.OutSine;
    [HideInInspector]
    public bool startTrailOnLaunch = true;

    [Header("05 - Snap Pull")]
    [Tooltip("Ball Travel Speed'in belirledigi sure icinde movement hissini sekillendirir. Ana hiz ayari degildir.")]
    [HideInInspector] public AnimationCurve ballTravelProgressCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [HideInInspector] public Ease pullEase = Ease.Linear;
    [Tooltip("Snap baslarken topun once ekstra yukari tirmanma miktari.")]
    [HideInInspector] [Min(0f)] public float snapInitialRiseHeight = 0f;
    [Tooltip("Snap baslarken topun target yonune ilk suruklenme mesafesi.")]
    [HideInInspector] [Min(0f)] public float snapInitialReachDistance = 0.15f;
    [Tooltip("Toplarin hedefe giderken yaptigi arc yuksekligi.")]
    [HideInInspector] [Min(0f)] public float arcHeight = 0f;
    [Tooltip("Pull hedefi slotun ne kadar ustunde bitsin.")]
    [HideInInspector] [Min(0f)] public float pullEndHeight = 0f;
    [Tooltip("Pull sirasinda top yonde hizlaniyormus gibi scale stretch yapsin.")]
    [HideInInspector] public bool useTravelStretch = true;
    [Tooltip("Pull sirasindaki hareket stretch scale'i. Casual okuma icin X dar, Y uzun.")]
    [HideInInspector] public Vector3 travelStretchScale = new Vector3(0.65f, 1.50f, 1f);
    [Tooltip("Pull baslayinca stretch formuna gecis suresi. Top hizindan bagimsizdir.")]
    [FormerlySerializedAs("travelStretchDurationRatio")]
    [HideInInspector] [Min(0.01f)] public float travelStretchDuration = 0.25f;

    [HideInInspector] public Vector3 arcDirection = Vector3.back;
    [HideInInspector] [Min(0f)] public float arcHeightDistanceFactor = 0.0f;

    [Header("07 - Landing Settle")]
    [Tooltip("Pull bittikten sonra topun son pozisyona oturma suresi. 0 olursa bu ekstra pozisyon bounce'u kapali olur.")]
    [HideInInspector] [Min(0f)] public float settleDuration = 0f;
    [Tooltip("Landing sirasinda topun tekrar yukari cikacagi ekstra yukseklik. 0 = yukari rebound yok.")]
    [HideInInspector] [Min(0f)] public float settleBounceHeight = 0f;
    [HideInInspector] [Range(0.1f, 0.9f)] public float settleLiftRatio = 0.45f;
    [HideInInspector] public Ease settleLiftEase = Ease.OutSine;
    [HideInInspector] public Ease settleDropEase = Ease.OutQuad;
    [HideInInspector] public Vector3 settleAxis = Vector3.back;

    [Header("06 - Impact Punch")]
    [Tooltip("Pull sirasinda smooth scale yerine, varis aninda squash > overshoot > settle yap.")]
    [HideInInspector] public bool useImpactPunch = true;
    [Tooltip("Vurus aninda dikey ezilme (Y), yatay yayilma (X).")]
    [HideInInspector] public Vector3 squashScale = new Vector3(1.35f, 0.65f, 1f);
    [Tooltip("Squash'tan sonra ziplama/overshoot scale'i.")]
    [HideInInspector] public Vector3 overshootScale = new Vector3(0.85f, 1.30f, 1f);
    [Tooltip("Impact punch'in normale donme suresi.")]
    [HideInInspector] [Min(0f)] public float impactSettleDuration = 0.18f;

    [HideInInspector] public Vector3 travelScale = new Vector3(1.05f, 1.05f, 1.05f);
    [HideInInspector] [Min(0f)] public float squashDuration = 0.06f;
    [HideInInspector] public Ease squashEase = Ease.OutQuad;
    [HideInInspector] [Min(0f)] public float overshootDuration = 0.10f;
    [HideInInspector] public Ease overshootEase = Ease.OutBack;
    [HideInInspector] public Vector3 finalScale = new Vector3(1f, 1f, 1f);
    [HideInInspector] public Ease impactSettleEase = Ease.OutElastic;

    [Header("08 - Group Complete")]
    [Tooltip("Tum toplar target'a vardiktan sonra vanish animasyonundan once beklenecek sure.")]
    [HideInInspector] public bool  playSlotPop      = true;
    [HideInInspector] [Min(0f)] public float groupCompleteDelay = 0f;
    [Tooltip("Slot ve toplar kuculmeden once ne kadar buyusun.")]
    [HideInInspector] [Min(0f)] public float slotPopScale = 0.18f;
    [Tooltip("Slot ve toplar scale up suresi.")]
    [HideInInspector] [Min(0f)] public float slotPopDuration = 0.18f;
    [HideInInspector] public AnimationCurve slotPopCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Slot ve toplar scale down suresi.")]
    [HideInInspector] [Min(0f)] public float slotShrinkDuration = 0.28f;
    [HideInInspector] public AnimationCurve slotShrinkCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [HideInInspector] public Ease slotPopEase = Ease.OutQuad;
    [HideInInspector] public Ease slotShrinkEase = Ease.InQuad;
    [HideInInspector] public bool liftBallsBeforeVanish = false;
    [HideInInspector] [Min(0f)] public float ballLiftBeforeVanishHeight = 0.35f;
    [HideInInspector] [Min(0f)] public float ballLiftBeforeVanishDuration = 0.14f;
    [HideInInspector] public Ease ballLiftBeforeVanishEase = Ease.OutQuad;
    [HideInInspector] public Vector3 ballLiftBeforeVanishAxis = Vector3.back;
    [HideInInspector] public bool parentToSlotOnArrive = true;
    [HideInInspector] public bool destroyOnArrive = false;
    [HideInInspector]
    public bool destroySlotOnComplete = true;

    [Header("08 - VFX")]
    public GameObject trailPrefab;
    [Min(0f)] public float trailFadeOut = 0.5f;
    public GameObject hitEffectPrefab;
    [Min(0f)] public float hitEffectLifetime = 2f;
    public GameObject slotVanishEffectPrefab;
    [Min(0f)] public float slotVanishEffectLifetime = 2f;

    [HideInInspector] public bool keepTrailBehindBall = true;
    [HideInInspector] [Min(0f)] public float trailDepthBehindBall = 0.05f;
    [HideInInspector] public bool matchTrailSortingToBall = false;
    [HideInInspector] public int trailSortingOrderOffset = -1;
    [HideInInspector] public bool hitEffectParentToTarget = false;
    [HideInInspector] public bool playHitEffectBeforeSettle = true;

    [Header("09 - Impact Screen Shake")]
    [Tooltip("Top target'a vurdugu anda kameraya ufak bir ekran shake'i verir.")]
    [HideInInspector] public bool useImpactScreenShake = true;
    [Tooltip("Bos birakilirsa Main Camera kullanilir. Bu sahnede Main Camera _Scene altinda oldugu icin gameplay root'u bozulmadan tum ekran sarsilir.")]
    [HideInInspector] public Transform screenShakeTarget;
    [Tooltip("Shake suresi.")]
    [HideInInspector] [Min(0f)] public float screenShakeDuration = 0.11f;
    [Tooltip("Kameranin lokal X/Y shake gucu. Z 0 kalmali.")]
    [HideInInspector] public Vector3 screenShakeStrength = new Vector3(0.45f, 0.32f, 0f);
    [HideInInspector] [Min(1)] public int screenShakeVibrato = 14;
    [HideInInspector] [Range(0f, 180f)] public float screenShakeRandomness = 65f;
    [HideInInspector] public Ease screenShakeEase = Ease.OutQuad;

    [HideInInspector]
    public UnityEvent<Transform> onBallArrived;
    [HideInInspector]
    public UnityEvent            onAllArrived;

    Tween screenShakeTween;
    Transform activeScreenShakeTarget;
    Vector3 screenShakeBaseLocalPosition;
    int lastScreenShakeFrame = -1;

    public void Collect()
    {
        ApplySettingsProfile();

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
                bool isFirstInGroup = (i == 0);

                AnimateBall(ball, slotTransform, delay, groupTarget, isFirstInGroup, () =>
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

        if (destroySlotOnComplete)
            seq.AppendCallback(() => SpawnVanishEffect(slotParent));

        if (liftBallsBeforeVanish && arrivedBalls != null && ballLiftBeforeVanishHeight > 0f && ballLiftBeforeVanishDuration > 0f)
        {
            Vector3 liftAxis = ballLiftBeforeVanishAxis.sqrMagnitude > 1e-6f
                ? ballLiftBeforeVanishAxis.normalized
                : (visualLiftAxis.sqrMagnitude > 1e-6f ? visualLiftAxis.normalized : Vector3.back);

            bool appended = false;
            foreach (Transform ball in arrivedBalls)
            {
                if (ball == null) continue;

                Tween tween = ball.DOMove(ball.position + liftAxis * ballLiftBeforeVanishHeight, ballLiftBeforeVanishDuration)
                    .SetEase(ballLiftBeforeVanishEase);

                if (!appended) { seq.Append(tween); appended = true; }
                else seq.Join(tween);
            }
        }

        if (playSlotPop && slotPopDuration > 0f)
        {
            bool appended = false;
            for (int i = 0; i < scaleTargets.Count; i++)
            {
                Transform target = scaleTargets[i];
                Vector3 popScale = originalScales[i] * (1f + slotPopScale);
                Tween tween = ApplyEase(target.DOScale(popScale, slotPopDuration), slotPopCurve, slotPopEase);

                if (!appended) { seq.Append(tween); appended = true; }
                else seq.Join(tween);
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
                    Tween tween = ApplyEase(target.DOScale(Vector3.zero, slotShrinkDuration), slotShrinkCurve, slotShrinkEase);

                    if (!appended) { seq.Append(tween); appended = true; }
                    else seq.Join(tween);
                }
            }
            else
            {
                seq.AppendCallback(() =>
                {
                    foreach (Transform target in scaleTargets)
                        if (target != null) target.localScale = Vector3.zero;
                });
            }

            seq.OnComplete(() =>
            {
                if (arrivedBalls != null)
                {
                    foreach (Transform ball in arrivedBalls)
                        if (ball != null) Destroy(ball.gameObject);
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

                    if (!appended) { seq.Append(tween); appended = true; }
                    else seq.Join(tween);
                }
            }
        }
    }

    void SpawnVanishEffect(Transform slotParent)
    {
        if (slotVanishEffectPrefab != null && slotParent != null)
            SpawnEffect(slotVanishEffectPrefab, slotParent.position, Quaternion.identity, slotVanishEffectLifetime);
    }

    void SpawnEffect(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime)
    {
        GameObject effect = Instantiate(prefab, position, rotation);
        effect.SetActive(true);

        ParticleSystem[] particles = effect.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem particle in particles)
            particle.Play(true);

        if (lifetime > 0f) Destroy(effect, lifetime);
    }

    void AddScaleTarget(Transform target, List<Transform> scaleTargets, List<Vector3> originalScales)
    {
        if (target == null) return;
        target.DOKill(false);
        scaleTargets.Add(target);
        originalScales.Add(target.localScale);
    }

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

    void AnimateBall(Transform ball, Transform slot, float delay, Transform groupKey, bool isFirstInGroup, System.Action onDone)
    {
        if (ball == null || slot == null)
        {
            onDone?.Invoke();
            return;
        }

        Vector3 liftAxis = visualLiftAxis.sqrMagnitude > 1e-6f ? visualLiftAxis.normalized : Vector3.back;
        Vector3 startPos = ball.position;
        Vector3 targetDir = slot.position - startPos;
        if (targetDir.sqrMagnitude < 1e-6f) targetDir = liftAxis;
        targetDir.Normalize();

        Vector3 originalScale = ball.localScale;

        ball.DOKill(false);

        GameObject trailInstance = null;
        bool hitEffectPlayed = false;

        Vector3 fromPos = startPos;
        Vector3 toPos   = slot.position;
        Vector3 snapControlA = fromPos;
        Vector3 snapControlB = toPos;
        Vector3 axis    = liftAxis;
        Vector3 sAxis   = liftAxis;

        Sequence seq = DOTween.Sequence().SetTarget(ball).SetDelay(delay);

        if (useCasualAnticipation && pulseTargetBeforeLaunch && isFirstInGroup)
            PlayTargetPulse(groupKey, delay);

        void SpawnTrail()
        {
            if (trailInstance != null) return;
            if (trailPrefab != null && ball != null)
            {
                Renderer ballRenderer = ball.GetComponentInChildren<Renderer>();
                trailInstance = Instantiate(trailPrefab, ball.position, Quaternion.identity, ball);
                trailInstance.transform.localPosition = Vector3.zero;
                trailInstance.transform.localRotation = Quaternion.identity;
                ConfigureTrailBehindBall(trailInstance, ball, ballRenderer);
            }
        }

        if (!useCasualAnticipation || !startTrailOnLaunch)
            seq.OnStart(SpawnTrail);

        Vector3 pullStartPos = startPos;

        // 1) Lift + reach + stretch anticipation.
        if (useCasualAnticipation && preLaunchDuration > 0f)
        {
            Vector3 preLaunchPos = startPos
                + liftAxis * preLaunchLiftHeight
                + targetDir * preLaunchReachDistance;
            pullStartPos = preLaunchPos;

            seq.Append(ball.DOMove(preLaunchPos, preLaunchDuration).SetEase(preLaunchMoveEase));

            if (stretchBallOnPreLaunch)
                seq.Join(ball.DOScale(ScaleBy(originalScale, preLaunchStretchScale), preLaunchDuration).SetEase(preLaunchScaleEase));
        }

        // 1.5) Optional extra float. Default kapali; ana cekilme hissi ilk kalkista baslar.
        if (useCasualAnticipation && airFloatDuration > 0f)
        {
            Vector3 floatStart = startPos
                + liftAxis * preLaunchLiftHeight
                + targetDir * preLaunchReachDistance;
            Vector3 floatPos = floatStart
                + liftAxis * airFloatLiftHeight
                + targetDir * (airFloatReachDistance + airFloatPullDistance);
            pullStartPos = floatPos;

            seq.Append(ball.DOMove(floatPos, airFloatDuration).SetEase(Ease.OutSine));
            seq.Join(ball.DOScale(ScaleBy(originalScale, airFloatChargeScale), airFloatDuration).SetEase(Ease.OutSine));
        }

        // 1.75) Scale tension: readable "getting pulled" moment before the snap.
        if (useCasualAnticipation && launchTensionDuration > 0f)
            seq.Append(ball.DOScale(ScaleBy(originalScale, launchTensionScale), launchTensionDuration).SetEase(Ease.InOutSine));

        if (useCasualAnticipation && launchHoldDuration > 0f)
            seq.AppendInterval(launchHoldDuration);

        if (useCasualAnticipation && startTrailOnLaunch)
            seq.AppendCallback(SpawnTrail);

        // 2) Snap pull: once yukari tirmanir, sonra hedefe dogru asagi iner.
        Vector3 estimatedPullTarget = slot.position + sAxis * pullEndHeight;
        float pullDistance = Vector3.Distance(pullStartPos, estimatedPullTarget);
        float pullDuration = Mathf.Max(0.05f, pullDistance / Mathf.Max(0.01f, ballTravelSpeed));

        Tween pullTween = DOVirtual.Float(0f, 1f, pullDuration, t =>
        {
            if (ball == null || slot == null) return;
            float progress = EvaluateProgress(ballTravelProgressCurve, t);
            ball.position = CubicBezier(fromPos, snapControlA, snapControlB, toPos, progress);
        })
        .SetEase(Ease.Linear)
        .SetTarget(ball)
        .OnStart(() =>
        {
            fromPos = ball.position;
            toPos   = slot != null ? slot.position + sAxis * pullEndHeight : ball.position;

            Vector3 travel = toPos - fromPos;
            float dist = travel.magnitude;
            Vector3 travelDir = dist > 1e-6f ? travel / dist : targetDir;
            float curveHeight = arcHeight + arcHeightDistanceFactor * dist;

            snapControlA = fromPos
                + axis * snapInitialRiseHeight
                + travelDir * snapInitialReachDistance;
            snapControlB = Vector3.Lerp(fromPos, toPos, 0.62f)
                + axis * Mathf.Max(curveHeight, snapInitialRiseHeight)
                + travelDir * (snapInitialReachDistance * 0.25f);
        });
        seq.Append(pullTween);

        // 2.5) Pull boyunca hedefe firlama hissi veren stretch.
        if (useTravelStretch)
        {
            float stretchDuration = Mathf.Max(0.01f, travelStretchDuration);
            seq.Join(ball.DOScale(ScaleBy(originalScale, travelStretchScale), stretchDuration)
                .SetEase(Ease.OutQuad));
        }

        // 3) IMPACT MOMENT - bu satir varisin oldugu yer
        if (playHitEffectBeforeSettle)
        {
            seq.AppendCallback(() =>
            {
                PlayImpactScreenShake();
                hitEffectPlayed = PlayHitEffect(slot, hitEffectPlayed);
            });
        }

        // 4) IMPACT PUNCH SEQUENCE - squash > overshoot > settle
        if (useImpactPunch)
        {
            // Squash (yumru)
            Vector3 squash = new Vector3(
                originalScale.x * squashScale.x,
                originalScale.y * squashScale.y,
                originalScale.z * squashScale.z);
            seq.Append(ball.DOScale(squash, squashDuration).SetEase(squashEase));

            // Overshoot (zipla)
            Vector3 overshoot = new Vector3(
                originalScale.x * overshootScale.x,
                originalScale.y * overshootScale.y,
                originalScale.z * overshootScale.z);
            seq.Append(ball.DOScale(overshoot, overshootDuration).SetEase(overshootEase));

            // Settle (jelly)
            Vector3 final = new Vector3(
                originalScale.x * finalScale.x,
                originalScale.y * finalScale.y,
                originalScale.z * finalScale.z);
            seq.Append(ball.DOScale(final, impactSettleDuration).SetEase(impactSettleEase));
        }

        // 5) Optional: slot'a oturma. settleBounceHeight 0 ise yukari rebound yapmaz.
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
                dropDur = settleDuration;
            }

            seq.Append(ball.DOMove(finalSlotPos, dropDur).SetEase(settleDropEase));
        }

        seq.OnComplete(() =>
        {
            if (!hitEffectPlayed)
                PlayImpactScreenShake();

            hitEffectPlayed = PlayHitEffect(slot, hitEffectPlayed);

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

    void PlayImpactScreenShake()
    {
        if (!useImpactScreenShake || screenShakeDuration <= 0f || screenShakeStrength.sqrMagnitude <= 1e-6f)
            return;

        // Three groups can land on the same frame; one camera hit is enough for that instant.
        if (lastScreenShakeFrame == Time.frameCount)
            return;

        Transform target = ResolveScreenShakeTarget();
        if (target == null) return;

        lastScreenShakeFrame = Time.frameCount;

        bool shakeIsActive = screenShakeTween != null && screenShakeTween.IsActive();
        if (activeScreenShakeTarget != target)
        {
            if (shakeIsActive)
                screenShakeTween.Kill(false);

            activeScreenShakeTarget = target;
            screenShakeBaseLocalPosition = target.localPosition;
        }
        else if (shakeIsActive)
        {
            screenShakeTween.Kill(false);
            target.localPosition = screenShakeBaseLocalPosition;
        }
        else
        {
            screenShakeBaseLocalPosition = target.localPosition;
        }

        screenShakeTween = target
            .DOShakePosition(
                screenShakeDuration,
                screenShakeStrength,
                screenShakeVibrato,
                screenShakeRandomness,
                false,
                true)
            .SetEase(screenShakeEase)
            .SetTarget(this)
            .OnKill(() =>
            {
                if (target != null && activeScreenShakeTarget == target)
                    target.localPosition = screenShakeBaseLocalPosition;
            });
    }

    Transform ResolveScreenShakeTarget()
    {
        if (screenShakeTarget != null)
            return screenShakeTarget;

        Camera mainCamera = Camera.main;
        return mainCamera != null ? mainCamera.transform : null;
    }

    bool PlayHitEffect(Transform slot, bool hitEffectPlayed)
    {
        if (hitEffectPlayed || hitEffectPrefab == null || slot == null)
            return hitEffectPlayed;

        Transform parent = hitEffectParentToTarget ? slot : null;
        GameObject hit = Instantiate(hitEffectPrefab, slot.position, Quaternion.identity, parent);
        if (hitEffectLifetime > 0f) Destroy(hit, hitEffectLifetime);

        return true;
    }

    void PlayTargetPulse(Transform target, float delay)
    {
        if (target == null || targetPulseScale <= 0f || targetPulseDuration <= 0f) return;

        DOVirtual.DelayedCall(delay, () =>
        {
            if (target == null) return;

            Vector3 originalScale = target.localScale;
            Vector3 pulseScale = originalScale * (1f + targetPulseScale);
            float upDuration = targetPulseDuration * 0.55f;
            float downDuration = targetPulseDuration * 0.45f;

            target.DOKill(false);
            Sequence pulse = DOTween.Sequence().SetTarget(target);
            pulse.Append(target.DOScale(pulseScale, upDuration).SetEase(targetPulseEase));
            pulse.Append(target.DOScale(originalScale, downDuration).SetEase(Ease.OutQuad));
        }).SetTarget(target);
    }

    void ConfigureTrailBehindBall(GameObject trailInstance, Transform ball, Renderer ballRenderer)
    {
        if (!keepTrailBehindBall || trailInstance == null || ball == null) return;

        if (trailDepthBehindBall > 0f)
        {
            Camera cam = Camera.main;
            Vector3 behindDirection = cam != null ? cam.transform.forward : Vector3.forward;
            trailInstance.transform.localPosition = ball.InverseTransformVector(behindDirection * trailDepthBehindBall);
        }

        if (!matchTrailSortingToBall || ballRenderer == null) return;

        Renderer[] trailRenderers = trailInstance.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer trailRenderer in trailRenderers)
        {
            if (trailRenderer == null) continue;
            trailRenderer.sortingLayerID = ballRenderer.sortingLayerID;
            trailRenderer.sortingOrder = ballRenderer.sortingOrder + trailSortingOrderOffset;
        }
    }

    static Vector3 ScaleBy(Vector3 baseScale, Vector3 multiplier)
    {
        return new Vector3(
            baseScale.x * multiplier.x,
            baseScale.y * multiplier.y,
            baseScale.z * multiplier.z);
    }

    static Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        return (u * u * u * p0)
            + (3f * u * u * t * p1)
            + (3f * u * t * t * p2)
            + (t * t * t * p3);
    }

    static float EvaluateProgress(AnimationCurve curve, float t)
    {
        t = Mathf.Clamp01(t);
        if (curve == null || curve.length == 0)
            return t;

        return Mathf.Clamp01(curve.Evaluate(t));
    }

    static Tween ApplyEase(Tween tween, AnimationCurve curve, Ease fallbackEase)
    {
        if (curve != null && curve.length > 0)
            return tween.SetEase(curve);

        return tween.SetEase(fallbackEase);
    }

    [ContextMenu("Test Collect")]
    void TestCollect() => Collect();

    [ContextMenu("Apply Settings Profile Now")]
    void ApplySettingsProfile()
    {
        if (!useSettingsProfile || settingsProfile == null) return;
        settingsProfile.ApplyTo(this);
    }

    void OnDisable()
    {
        if (screenShakeTween != null && screenShakeTween.IsActive())
            screenShakeTween.Kill(false);

        DOTween.Kill(this);
    }

    [ContextMenu("Apply Recommended Impact Tuning v2")]
    void ApplyRecommendedImpactTuning()
    {
        // === Critical fixes (mevcut Inspector ayarlarini override eder) ===
        ballTravelSpeed      = 8f;
        stagger              = 0.06f;
        visualLiftAxis       = Vector3.back;
        arcDirection         = visualLiftAxis;
        settleAxis           = visualLiftAxis;
        ballLiftBeforeVanishAxis = visualLiftAxis;
        ballTravelProgressCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        pullEase             = Ease.Linear;
        snapInitialRiseHeight = 0f;
        snapInitialReachDistance = 0.15f;
        arcHeight            = 0f;
        pullEndHeight        = 0f;
        useTravelStretch     = true;
        travelStretchScale   = new Vector3(0.65f, 1.50f, 1f);
        travelStretchDuration = 0.25f;
        settleDuration       = 0f;
        settleBounceHeight   = 0f;
        slotPopScale         = 0.18f;
        slotPopDuration      = 0.18f;
        slotPopCurve         = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        slotShrinkDuration   = 0.28f;
        slotShrinkCurve      = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        slotPopEase          = Ease.OutQuad;
        slotShrinkEase       = Ease.InQuad;

        // === Casual Anticipation baseline ===
        useCasualAnticipation    = true;
        pulseTargetBeforeLaunch  = true;
        targetPulseScale         = 0.25f;
        targetPulseDuration      = 0.30f;
        targetPulseEase          = Ease.OutBack;
        preLaunchLiftHeight      = 0.45f;
        preLaunchReachDistance   = 0.14f;
        preLaunchStretchScale    = new Vector3(0.78f, 1.35f, 1f);
        preLaunchDuration        = 0.28f;
        airFloatDuration         = 0f;
        airFloatLiftHeight       = 0f;
        airFloatReachDistance    = 0f;
        airFloatPullDistance     = 0f;
        airFloatChargeScale      = new Vector3(0.65f, 1.50f, 1f);
        launchTensionDuration    = 0f;
        launchTensionScale       = new Vector3(0.78f, 1.35f, 1f);
        stretchBallOnPreLaunch   = true;
        preLaunchMoveEase        = Ease.OutSine;
        preLaunchScaleEase       = Ease.OutSine;
        launchHoldDuration       = 0.02f;
        startTrailOnLaunch       = true;

        // === Impact Punch baseline ===
        useImpactPunch         = true;
        travelScale            = new Vector3(1.05f, 1.05f, 1.05f);
        squashScale            = new Vector3(1.35f, 0.65f, 1f);
        squashDuration         = 0.06f;
        squashEase             = Ease.OutQuad;
        overshootScale         = new Vector3(0.85f, 1.30f, 1f);
        overshootDuration      = 0.10f;
        overshootEase          = Ease.OutBack;
        finalScale             = new Vector3(1f, 1f, 1f);
        impactSettleDuration   = 0.18f;
        impactSettleEase       = Ease.OutElastic;
        useImpactScreenShake    = true;
        screenShakeDuration     = 0.11f;
        screenShakeStrength     = new Vector3(0.45f, 0.32f, 0f);
        screenShakeVibrato      = 14;
        screenShakeRandomness   = 65f;
        screenShakeEase         = Ease.OutQuad;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        if (gameObject.scene.IsValid())
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif

        Debug.Log("[MagnetCollector] Recommended Impact Tuning v2 applied. Save the scene to persist.");
    }
}
