using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(
    fileName = "MagnetCollectFeelProfile",
    menuName = "Marble Sort/Magnet Collect Feel Profile")]
public class MagnetCollectSettings : ScriptableObject
{
    [Header("00 - Sequence Start")]
    [Tooltip("Magnet collect baslamadan once genel bekleme.")]
    [Min(0f)] public float startDelay = 0f;
    [Tooltip("Ayni gruptaki toplar arasindaki kucuk gecikme. 0 = hepsi ayni anda.")]
    [Min(0f)] public float stagger = 0.06f;
    [Tooltip("Ekranda yukari gibi gorunen dunya ekseni. Bu sahnede Z- yukari hissi veriyor.")]
    public Vector3 visualLiftAxis = Vector3.back;

    [Header("01 - Target Pulse")]
    [Tooltip("Toplar firlamadan once hazirlik hareketini acar/kapatir.")]
    public bool useCasualAnticipation = true;
    [Tooltip("Target slotlar firlamadan once kisa pulse atsin.")]
    public bool pulseTargetBeforeLaunch = true;
    [Tooltip("Target slot pulse buyume miktari. 0.16 = %16 buyume.")]
    [Min(0f)] public float targetPulseScale = 0.25f;
    [Tooltip("Target slot pulse suresi.")]
    [Min(0f)] public float targetPulseDuration = 0.3f;

    [Header("02 - Lift And Reach")]
    [Tooltip("Toplar cekilmeden once yukari ne kadar kalkar.")]
    [Min(0f)] public float preLaunchLiftHeight = 0.45f;
    [Tooltip("Toplar cekilmeden once target'a dogru ne kadar uzanir.")]
    [Min(0f)] public float preLaunchReachDistance = 0.14f;
    [Tooltip("Ilk kalkistan itibaren topun cekiliyormus gibi 2D stretch scale'i. X sikisir, Y uzar, Z 1 kalir.")]
    public Vector3 preLaunchStretchScale = new Vector3(0.78f, 1.35f, 1f);
    [Tooltip("Ilk yukari kalkma ve target'a dogru sunme suresi.")]
    [Min(0f)] public float preLaunchDuration = 0.28f;

    [Header("03 - Air Float")]
    [Tooltip("Yukari kalktiktan sonra havada suzulme/askida kalma suresi.")]
    [Min(0f)] public float airFloatDuration = 0f;
    [Tooltip("Suzulme sirasinda ekstra yukari kayma.")]
    [Min(0f)] public float airFloatLiftHeight = 0f;
    [Tooltip("Suzulme sirasinda target'a dogru ekstra uzanma.")]
    [Min(0f)] public float airFloatReachDistance = 0f;
    [Tooltip("Suzulme sonunda target'a dogru ekstra cekilme mesafesi.")]
    [Min(0f)] public float airFloatPullDistance = 0f;
    [Tooltip("Suzulme boyunca kuvvet birikiyormus gibi 2D genisleme scale'i. Z 1 kalmali.")]
    public Vector3 airFloatChargeScale = new Vector3(0.65f, 1.50f, 1f);

    [Header("04 - Launch Tension")]
    [Tooltip("Pull baslamadan once scale ile gerilme/cekilme suresi.")]
    [Min(0f)] public float launchTensionDuration = 0f;
    [Tooltip("Pull baslamadan onceki 2D gerilme scale'i. X sikisir, Y uzar, Z 1 kalir.")]
    public Vector3 launchTensionScale = new Vector3(0.78f, 1.35f, 1f);
    [Tooltip("Sunmeden sonra firlamadan onceki minicik bekleme.")]
    [Min(0f)] public float launchHoldDuration = 0.02f;

    [Header("05 - Snap Pull")]
    [Tooltip("Sadece toplarin target'a gidis hizi (dunya birimi/saniye). Dusurmek sadece Snap Pull hareketini yavaslatir.")]
    [Min(0.01f)] public float ballTravelSpeed = 8f;
    [Tooltip("Ball Travel Speed'in belirledigi sure icinde movement hissini sekillendirir. Ana hiz ayari degildir.")]
    public AnimationCurve ballTravelProgressCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [HideInInspector] public Ease pullEase = Ease.Linear;
    [Tooltip("Snap baslarken topun once ekstra yukari tirmanma miktari.")]
    [Min(0f)] public float snapInitialRiseHeight = 0f;
    [Tooltip("Snap baslarken topun target yonune ilk suruklenme mesafesi.")]
    [Min(0f)] public float snapInitialReachDistance = 0.15f;
    [Tooltip("Toplarin hedefe giderken yaptigi arc yuksekligi.")]
    [Min(0f)] public float arcHeight = 0f;
    [Tooltip("Pull hedefi slotun ne kadar ustunde bitsin.")]
    [Min(0f)] public float pullEndHeight = 0f;
    [Tooltip("Pull sirasinda top yonde hizlaniyormus gibi scale stretch yapsin.")]
    public bool useTravelStretch = true;
    [Tooltip("Pull sirasindaki hareket stretch scale'i. Casual okuma icin X dar, Y uzun.")]
    public Vector3 travelStretchScale = new Vector3(0.65f, 1.50f, 1f);
    [Tooltip("Pull baslayinca stretch formuna gecis suresi. Top hizindan bagimsizdir.")]
    [FormerlySerializedAs("travelStretchDurationRatio")]
    [Min(0.01f)] public float travelStretchDuration = 0.25f;

    [Header("06 - Impact Punch")]
    [Tooltip("Varis aninda squash > overshoot > settle yap.")]
    public bool useImpactPunch = true;
    [Tooltip("Vurus aninda dikey ezilme (Y), yatay yayilma (X).")]
    public Vector3 squashScale = new Vector3(1.35f, 0.65f, 1f);
    [Tooltip("Squash'tan sonra ziplama/overshoot scale'i.")]
    public Vector3 overshootScale = new Vector3(0.85f, 1.30f, 1f);
    [Tooltip("Impact punch'in normale donme suresi.")]
    [Min(0f)] public float impactSettleDuration = 0.18f;

    [Header("06.5 - Impact Screen Shake")]
    [Tooltip("Top target'a vurdugu anda kameraya ufak bir ekran shake'i verir.")]
    public bool useImpactScreenShake = true;
    [Tooltip("Shake suresi.")]
    [Min(0f)] public float screenShakeDuration = 0.11f;
    [Tooltip("Kameranin lokal X/Y shake gucu. Z 0 kalmali.")]
    public Vector3 screenShakeStrength = new Vector3(0.45f, 0.32f, 0f);
    [Min(1)] public int screenShakeVibrato = 14;
    [Range(0f, 180f)] public float screenShakeRandomness = 65f;

    [Header("07 - Landing Settle")]
    [Tooltip("Pull bittikten sonra topun son pozisyona oturma suresi.")]
    [Min(0f)] public float settleDuration = 0f;
    [Tooltip("Landing sirasinda topun tekrar yukari cikacagi ekstra yukseklik. 0 = yukari rebound yok.")]
    [Min(0f)] public float settleBounceHeight = 0f;

    [Header("08 - Group Complete")]
    public bool playSlotPop = true;
    [Tooltip("Tum toplar target'a vardiktan sonra vanish animasyonundan once beklenecek sure.")]
    [Min(0f)] public float groupCompleteDelay = 0f;
    [Tooltip("Slot ve toplar kuculmeden once ne kadar buyusun.")]
    [Min(0f)] public float slotPopScale = 0.18f;
    [Tooltip("Slot ve toplar scale up suresi.")]
    [Min(0f)] public float slotPopDuration = 0.18f;
    [Tooltip("Slot/top buyurken kullanilan curve. 0->1 arasi okunur.")]
    public AnimationCurve slotPopCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Slot ve toplar scale down suresi.")]
    [Min(0f)] public float slotShrinkDuration = 0.28f;
    [Tooltip("Slot/top kuculurken kullanilan curve. 0->1 arasi okunur.")]
    public AnimationCurve slotShrinkCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public void ApplyTo(MagnetCollector collector)
    {
        if (collector == null) return;

        collector.startDelay = startDelay;
        collector.stagger = stagger;
        collector.visualLiftAxis = visualLiftAxis;

        collector.useCasualAnticipation = useCasualAnticipation;
        collector.pulseTargetBeforeLaunch = pulseTargetBeforeLaunch;
        collector.targetPulseScale = targetPulseScale;
        collector.targetPulseDuration = targetPulseDuration;
        collector.preLaunchMoveEase = Ease.OutSine;
        collector.preLaunchScaleEase = Ease.OutSine;
        collector.preLaunchLiftHeight = preLaunchLiftHeight;
        collector.preLaunchReachDistance = preLaunchReachDistance;
        collector.preLaunchStretchScale = preLaunchStretchScale;
        collector.preLaunchDuration = preLaunchDuration;
        collector.airFloatDuration = airFloatDuration;
        collector.airFloatLiftHeight = airFloatLiftHeight;
        collector.airFloatReachDistance = airFloatReachDistance;
        collector.airFloatPullDistance = airFloatPullDistance;
        collector.airFloatChargeScale = airFloatChargeScale;
        collector.launchTensionDuration = launchTensionDuration;
        collector.launchTensionScale = launchTensionScale;
        collector.launchHoldDuration = launchHoldDuration;

        collector.ballTravelSpeed = ballTravelSpeed;
        collector.ballTravelProgressCurve = ballTravelProgressCurve;
        collector.pullEase = Ease.Linear;
        collector.arcDirection = visualLiftAxis;
        collector.snapInitialRiseHeight = snapInitialRiseHeight;
        collector.snapInitialReachDistance = snapInitialReachDistance;
        collector.arcHeight = arcHeight;
        collector.pullEndHeight = pullEndHeight;
        collector.useTravelStretch = useTravelStretch;
        collector.travelStretchScale = travelStretchScale;
        collector.travelStretchDuration = travelStretchDuration;

        collector.useImpactPunch = useImpactPunch;
        collector.squashScale = squashScale;
        collector.overshootScale = overshootScale;
        collector.impactSettleDuration = impactSettleDuration;
        collector.useImpactScreenShake = useImpactScreenShake;
        collector.screenShakeDuration = screenShakeDuration;
        collector.screenShakeStrength = screenShakeStrength;
        collector.screenShakeVibrato = screenShakeVibrato;
        collector.screenShakeRandomness = screenShakeRandomness;
        collector.screenShakeEase = Ease.OutQuad;

        collector.settleDuration = settleDuration;
        collector.settleBounceHeight = settleBounceHeight;
        collector.settleAxis = visualLiftAxis;

        collector.playSlotPop = playSlotPop;
        collector.groupCompleteDelay = groupCompleteDelay;
        collector.slotPopScale = slotPopScale;
        collector.slotPopDuration = slotPopDuration;
        collector.slotPopCurve = slotPopCurve;
        collector.slotShrinkDuration = slotShrinkDuration;
        collector.slotShrinkCurve = slotShrinkCurve;
        collector.slotPopEase = Ease.OutQuad;
        collector.slotShrinkEase = Ease.InQuad;
    }
}
