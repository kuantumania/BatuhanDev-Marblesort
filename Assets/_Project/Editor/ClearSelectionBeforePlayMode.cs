using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ClearSelectionBeforePlayMode
{
    static ClearSelectionBeforePlayMode()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        AssemblyReloadEvents.beforeAssemblyReload += ClearSelection;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
            ClearSelection();
    }

    [MenuItem("Tools/Marble Sort/Clear Inspector Selection")]
    static void ClearSelection()
    {
        Selection.objects = new Object[0];
        ActiveEditorTracker.sharedTracker.ForceRebuild();
    }
}
