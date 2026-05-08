using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ProjectStartupSceneLoader
{
    const string StartupScenePath = "Assets/_Project/Scenes/MarbleSort.unity";
    const string SessionKey = "MarbleSort.StartupSceneLoader.HasChecked";
    const string IosBuildTargetWarning =
        "iOS build target could not be selected. Make sure the Unity iOS Build Support module is installed.";

    static ProjectStartupSceneLoader()
    {
        EditorApplication.delayCall += ConfigureProjectOnce;
    }

    static void ConfigureProjectOnce()
    {
        if (SessionState.GetBool(SessionKey, false))
            return;

        SessionState.SetBool(SessionKey, true);

        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        EnsureIosBuildTarget();
        ProjectGameViewConfigurator.UseIphone11ProPortrait();
        OpenStartupScene();
    }

    static void EnsureIosBuildTarget()
    {
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
            return;

        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
        {
            UnityEngine.Debug.LogWarning(IosBuildTargetWarning);
            return;
        }

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
    }

    static void OpenStartupScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path == StartupScenePath)
            return;

        if (HasDirtyOpenScene())
            return;

        EditorSceneManager.OpenScene(StartupScenePath, OpenSceneMode.Single);
    }

    static bool HasDirtyOpenScene()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).isDirty)
                return true;
        }

        return false;
    }
}
