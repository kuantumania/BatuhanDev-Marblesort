using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class MagnetBoosterController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] PlayableDirector magnetTimeline;
    [SerializeField] MagnetCollector magnetCollect;
    [SerializeField] Collider collectClickCollider;
    [SerializeField] Camera raycastCamera;
    [SerializeField] GameObject magnetVfxRoot;
    [SerializeField] Renderer[] magnetShaderRenderers;
    [SerializeField] ParticleSystem[] collectClickEffects;

    [Header("Flow")]
    [SerializeField] bool collectReadyAtStart;
    [SerializeField] bool waitForTimelineToFinish = true;
    [SerializeField] bool disableCollectColliderUntilReady = true;
    [SerializeField] bool collectOnce = true;
    [SerializeField] bool restartSceneOnButtonAfterCollect = true;
    [SerializeField] bool ignoreUiTouches = true;
    [SerializeField, Min(0f)] float clickMaxDistance = 1000f;

    [Header("Timeline Close")]
    [SerializeField] bool stopTimelineOnCollect = true;
    [SerializeField] bool resetTimelineOnCollect = true;
    [SerializeField] bool pauseTimelineDuringVfxFade = true;

    [Header("Button VFX")]
    [SerializeField] bool stopVfxAtStart = true;
    [SerializeField] bool playVfxOnButton = true;
    [SerializeField] bool stopVfxOnCollect = true;
    [SerializeField, Min(0f)] float vfxFadeOutDuration = 0.6f;
    [SerializeField] bool clearVfxAfterFade = true;
    [SerializeField] bool scaleEffectsDuringFade = true;

    [Header("Button Shader")]
    [SerializeField] bool hideShaderAtStart = true;
    [SerializeField] bool showShaderOnButton = true;
    [SerializeField] bool hideShaderOnCollect = true;

    [Header("Collect Click VFX")]
    [SerializeField] bool stopCollectClickEffectsAtStart = true;
    [SerializeField] bool clearCollectClickEffectsBeforePlay = true;

    bool collectReady;
    bool hasCollected;
    bool magnetSequenceActive;
    bool restartingTimeline;
    ParticleSystem[] magnetParticles;
    readonly List<MaterialFloatState> materialFloatStates = new List<MaterialFloatState>();
    readonly List<MaterialColorState> materialColorStates = new List<MaterialColorState>();
    readonly List<TransformScaleState> transformScaleStates = new List<TransformScaleState>();
    Coroutine stopVfxCoroutine;
    bool materialDefaultsCaptured;
    bool transformDefaultsCaptured;

    static readonly string[] FadeFloatProperties =
    {
        "Alpha",
        "_Alpha",
        "Tex_2_Alpha"
    };

    static readonly string[] FadeColorProperties =
    {
        "Main_Color",
        "_Main_Color",
        "_MainColor",
        "_Color",
        "_TintColor"
    };

    struct MaterialFloatState
    {
        public Material material;
        public string property;
        public float value;
    }

    struct MaterialColorState
    {
        public Material material;
        public string property;
        public Color value;
    }

    struct TransformScaleState
    {
        public Transform transform;
        public Vector3 localScale;
    }

    void Awake()
    {
        if (magnetCollect == null)
            magnetCollect = GetComponent<MagnetCollector>();

        if (raycastCamera == null)
            raycastCamera = Camera.main;

        if (magnetTimeline != null)
            magnetTimeline.stopped += HandleTimelineStopped;

        CacheMagnetParticles();

        collectReady = collectReadyAtStart;
        SetCollectColliderEnabled(collectReady);

        if (stopVfxAtStart)
            StopMagnetVfxImmediate();

        if (hideShaderAtStart)
            SetMagnetShaderVisible(false);

        if (stopCollectClickEffectsAtStart)
            StopCollectClickEffectsImmediate();
    }

    void Start()
    {
        if (stopVfxAtStart && !collectReady)
            StopMagnetVfxImmediate();

        if (hideShaderAtStart && !collectReady)
            SetMagnetShaderVisible(false);

        if (stopCollectClickEffectsAtStart)
            StopCollectClickEffectsImmediate();
    }

    void OnDestroy()
    {
        if (magnetTimeline != null)
            magnetTimeline.stopped -= HandleTimelineStopped;
    }

    void Update()
    {
        if (!collectReady || (collectOnce && hasCollected))
            return;

        if (!TryGetPointerDown(out Vector2 screenPosition, out int pointerId))
            return;

        if (IsPointerOverUi(pointerId))
            return;

        TryCollectFromScreen(screenPosition);
    }

    public void PlayMagnetTimeline()
    {
        if (collectOnce && hasCollected)
        {
            RestartSceneAfterCollect();
            return;
        }

        if (magnetSequenceActive)
            return;

        magnetSequenceActive = true;
        collectReady = !waitForTimelineToFinish;
        SetCollectColliderEnabled(collectReady);
        PlayMagnetVfx();
        ShowMagnetShader();

        if (magnetTimeline == null)
        {
            UnlockCollectClick();
            return;
        }

        restartingTimeline = true;
        magnetTimeline.Stop();
        restartingTimeline = false;
        magnetTimeline.time = 0f;
        magnetTimeline.Evaluate();
        magnetTimeline.Play();

        if (!waitForTimelineToFinish)
            UnlockCollectClick();
    }

    void RestartSceneAfterCollect()
    {
        if (!restartSceneOnButtonAfterCollect)
            return;

        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }

    public void CollectNow()
    {
        if (!collectReady || (collectOnce && hasCollected))
            return;

        hasCollected = true;
        collectReady = false;
        SetCollectColliderEnabled(false);
        PlayCollectClickEffects();
        FadeOutMagnetEffects();

        if (magnetCollect != null)
            magnetCollect.Collect();
    }

    void HandleTimelineStopped(PlayableDirector director)
    {
        if (restartingTimeline)
            return;

        if (director == magnetTimeline)
            UnlockCollectClick();
    }

    void UnlockCollectClick()
    {
        if (collectOnce && hasCollected)
            return;

        collectReady = true;
        SetCollectColliderEnabled(true);
    }

    void CloseMagnetTimeline()
    {
        if (!stopTimelineOnCollect || magnetTimeline == null)
            return;

        restartingTimeline = true;
        magnetTimeline.Stop();

        if (resetTimelineOnCollect)
        {
            magnetTimeline.time = 0f;
            magnetTimeline.Evaluate();
        }

        restartingTimeline = false;
    }

    void CacheMagnetParticles()
    {
        magnetParticles = magnetVfxRoot != null
            ? magnetVfxRoot.GetComponentsInChildren<ParticleSystem>(true)
            : System.Array.Empty<ParticleSystem>();
    }

    void PlayMagnetVfx()
    {
        if (!playVfxOnButton)
            return;

        StopRunningVfxFade();

        if (magnetVfxRoot != null && !magnetVfxRoot.activeSelf)
            magnetVfxRoot.SetActive(true);

        RestoreEffectMaterialDefaults();
        RestoreEffectTransformDefaults();

        if (magnetParticles == null)
            CacheMagnetParticles();

        foreach (ParticleSystem particle in magnetParticles)
        {
            if (particle == null) continue;
            particle.Clear(true);
            particle.Play(true);
        }
    }

    void FadeOutMagnetEffects()
    {
        StopRunningVfxFade();
        StopMagnetTimelineForFade();

        if (vfxFadeOutDuration <= 0f)
        {
            StopMagnetVfxImmediate();
            HideMagnetShader();
            ResetMagnetTimelineToStart();
            magnetSequenceActive = false;
            return;
        }

        FadeOutMagnetVfx();
        stopVfxCoroutine = StartCoroutine(FadeOutEffectRenderers(vfxFadeOutDuration));
    }

    void FadeOutMagnetVfx()
    {
        if (!stopVfxOnCollect)
            return;

        if (magnetParticles == null)
            CacheMagnetParticles();

        foreach (ParticleSystem particle in magnetParticles)
        {
            if (particle == null) continue;
            particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        CaptureEffectMaterialDefaults();
    }

    void StopMagnetVfxImmediate()
    {
        StopRunningVfxFade();
        ClearMagnetParticlesImmediate();
    }

    void ClearMagnetParticlesImmediate()
    {
        if (magnetParticles == null)
            CacheMagnetParticles();

        foreach (ParticleSystem particle in magnetParticles)
        {
            if (particle == null) continue;
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Clear(true);
        }
    }

    void PlayCollectClickEffects()
    {
        if (collectClickEffects == null)
            return;

        foreach (ParticleSystem effect in collectClickEffects)
        {
            if (effect == null)
                continue;

            if (clearCollectClickEffectsBeforePlay)
                effect.Clear(true);

            effect.Play(true);
        }
    }

    void StopCollectClickEffectsImmediate()
    {
        if (collectClickEffects == null)
            return;

        foreach (ParticleSystem effect in collectClickEffects)
        {
            if (effect == null)
                continue;

            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            effect.Clear(true);
        }
    }

    IEnumerator FadeOutEffectRenderers(float duration)
    {
        CaptureEffectMaterialDefaults();
        CaptureEffectTransformDefaults();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float fade = Mathf.Clamp01(1f - elapsed / duration);
            ApplyEffectMaterialFade(fade);
            ApplyEffectTransformFade(fade);
            yield return null;
        }

        ApplyEffectMaterialFade(0f);
        ApplyEffectTransformFade(0f);

        if (clearVfxAfterFade)
            ClearMagnetParticlesImmediate();

        HideMagnetShader();
        ResetMagnetTimelineToStart();
        magnetSequenceActive = false;
        stopVfxCoroutine = null;
    }

    void StopRunningVfxFade()
    {
        if (stopVfxCoroutine == null)
            return;

        StopCoroutine(stopVfxCoroutine);
        stopVfxCoroutine = null;
    }

    void ShowMagnetShader()
    {
        if (showShaderOnButton)
        {
            RestoreEffectMaterialDefaults();
            RestoreEffectTransformDefaults();
            SetMagnetShaderVisible(true);
        }
    }

    void HideMagnetShader()
    {
        if (hideShaderOnCollect)
            SetMagnetShaderVisible(false);
    }

    void SetMagnetShaderVisible(bool visible)
    {
        if (magnetShaderRenderers == null)
            return;

        foreach (Renderer shaderRenderer in magnetShaderRenderers)
        {
            if (shaderRenderer != null)
                shaderRenderer.enabled = visible;
        }
    }

    void StopMagnetTimelineForFade()
    {
        if (!stopTimelineOnCollect || magnetTimeline == null)
            return;

        restartingTimeline = true;

        if (pauseTimelineDuringVfxFade && vfxFadeOutDuration > 0f)
            magnetTimeline.Pause();
        else
            magnetTimeline.Stop();

        restartingTimeline = false;
    }

    void ResetMagnetTimelineToStart()
    {
        if (!resetTimelineOnCollect || magnetTimeline == null)
            return;

        restartingTimeline = true;
        magnetTimeline.time = 0f;
        magnetTimeline.Evaluate();
        restartingTimeline = false;
    }

    void CaptureEffectMaterialDefaults()
    {
        if (materialDefaultsCaptured)
            return;

        materialDefaultsCaptured = true;
        materialFloatStates.Clear();
        materialColorStates.Clear();

        foreach (Renderer effectRenderer in EnumerateEffectRenderers())
        {
            if (effectRenderer == null)
                continue;

            foreach (Material material in effectRenderer.materials)
            {
                if (material == null)
                    continue;

                foreach (string property in FadeFloatProperties)
                {
                    if (material.HasProperty(property))
                    {
                        materialFloatStates.Add(new MaterialFloatState
                        {
                            material = material,
                            property = property,
                            value = material.GetFloat(property)
                        });
                    }
                }

                foreach (string property in FadeColorProperties)
                {
                    if (material.HasProperty(property))
                    {
                        materialColorStates.Add(new MaterialColorState
                        {
                            material = material,
                            property = property,
                            value = material.GetColor(property)
                        });
                    }
                }
            }
        }
    }

    void ApplyEffectMaterialFade(float fade)
    {
        foreach (MaterialFloatState state in materialFloatStates)
        {
            if (state.material != null && state.material.HasProperty(state.property))
                state.material.SetFloat(state.property, state.value * fade);
        }

        foreach (MaterialColorState state in materialColorStates)
        {
            if (state.material == null || !state.material.HasProperty(state.property))
                continue;

            Color color = state.value;
            color.a *= fade;
            state.material.SetColor(state.property, color);
        }
    }

    void CaptureEffectTransformDefaults()
    {
        if (transformDefaultsCaptured || !scaleEffectsDuringFade)
            return;

        transformDefaultsCaptured = true;
        transformScaleStates.Clear();

        if (magnetVfxRoot != null)
            AddEffectTransform(magnetVfxRoot.transform);

        if (magnetShaderRenderers == null)
            return;

        foreach (Renderer shaderRenderer in magnetShaderRenderers)
        {
            if (shaderRenderer != null)
                AddEffectTransform(shaderRenderer.transform);
        }
    }

    void AddEffectTransform(Transform effectTransform)
    {
        if (effectTransform == null || HasEffectTransform(effectTransform))
            return;

        transformScaleStates.Add(new TransformScaleState
        {
            transform = effectTransform,
            localScale = effectTransform.localScale
        });
    }

    bool HasEffectTransform(Transform effectTransform)
    {
        foreach (TransformScaleState state in transformScaleStates)
        {
            if (state.transform == effectTransform)
                return true;
        }

        return false;
    }

    void ApplyEffectTransformFade(float fade)
    {
        if (!scaleEffectsDuringFade)
            return;

        foreach (TransformScaleState state in transformScaleStates)
        {
            if (state.transform != null)
                state.transform.localScale = state.localScale * fade;
        }
    }

    void RestoreEffectMaterialDefaults()
    {
        if (!materialDefaultsCaptured)
            return;

        ApplyEffectMaterialFade(1f);
    }

    void RestoreEffectTransformDefaults()
    {
        if (!transformDefaultsCaptured)
            return;

        ApplyEffectTransformFade(1f);
    }

    IEnumerable<Renderer> EnumerateEffectRenderers()
    {
        if (magnetVfxRoot != null)
        {
            foreach (Renderer effectRenderer in magnetVfxRoot.GetComponentsInChildren<Renderer>(true))
                yield return effectRenderer;
        }

        if (magnetShaderRenderers == null)
            yield break;

        foreach (Renderer shaderRenderer in magnetShaderRenderers)
            yield return shaderRenderer;
    }

    void TryCollectFromScreen(Vector2 screenPosition)
    {
        if (collectClickCollider == null)
            return;

        if (raycastCamera == null)
            raycastCamera = Camera.main;

        if (raycastCamera == null)
            return;

        Ray ray = raycastCamera.ScreenPointToRay(screenPosition);
        if (collectClickCollider.Raycast(ray, out _, clickMaxDistance))
            CollectNow();
    }

    void SetCollectColliderEnabled(bool enabled)
    {
        if (!disableCollectColliderUntilReady || collectClickCollider == null)
            return;

        collectClickCollider.enabled = enabled;
    }

    bool IsPointerOverUi(int pointerId)
    {
        if (!ignoreUiTouches || EventSystem.current == null)
            return false;

#if ENABLE_LEGACY_INPUT_MANAGER
        if (pointerId >= 0)
            return EventSystem.current.IsPointerOverGameObject(pointerId);
#endif

        return EventSystem.current.IsPointerOverGameObject();
    }

    bool TryGetPointerDown(out Vector2 screenPosition, out int pointerId)
    {
        screenPosition = default;
        pointerId = -1;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }

        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            if (touch.press.wasPressedThisFrame)
            {
                screenPosition = touch.position.ReadValue();
                pointerId = touch.touchId.ReadValue();
                return true;
            }
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButtonDown(0))
        {
            screenPosition = Input.mousePosition;
            return true;
        }

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == UnityEngine.TouchPhase.Began)
            {
                screenPosition = touch.position;
                pointerId = touch.fingerId;
                return true;
            }
        }
#endif

        return false;
    }
}
