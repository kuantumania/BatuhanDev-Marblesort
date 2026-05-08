using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class MagnetCollectTarget : MonoBehaviour
{
    [SerializeField] MagnetBoosterController magnetController;
    [SerializeField] bool ignoreUiTouches;

    void Reset()
    {
        if (magnetController == null)
            magnetController = FindController();
    }

    void Awake()
    {
        if (magnetController == null)
            magnetController = FindController();
    }

    void OnMouseDown()
    {
        if (ignoreUiTouches && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (magnetController != null)
            magnetController.CollectNow();
    }

    MagnetBoosterController FindController()
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<MagnetBoosterController>();
#else
        return FindObjectOfType<MagnetBoosterController>();
#endif
    }
}
