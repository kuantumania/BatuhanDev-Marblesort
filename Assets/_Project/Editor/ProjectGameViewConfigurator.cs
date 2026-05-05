using System;
using System.Reflection;
using UnityEditor;

public static class ProjectGameViewConfigurator
{
    const string GameViewSizeLabel = "iPhone 11 Pro Portrait";
    const int GameViewWidth = 1125;
    const int GameViewHeight = 2436;

    public static void UseIphone11ProPortrait()
    {
        try
        {
            int sizeIndex = EnsureGameViewSize();
            SelectGameViewSize(sizeIndex);
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogWarning($"Could not configure Game view size: {exception.Message}");
        }
    }

    static int EnsureGameViewSize()
    {
        Assembly editorAssembly = typeof(EditorWindow).Assembly;
        Type gameViewSizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
        Type gameViewSizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
        Type gameViewSizeTypeEnum = editorAssembly.GetType("UnityEditor.GameViewSizeType");
        Type gameViewSizeGroupType = editorAssembly.GetType("UnityEditor.GameViewSizeGroupType");

        object gameViewSizes = GetScriptableSingletonInstance(gameViewSizesType);
        object iOSGroupType = Enum.Parse(gameViewSizeGroupType, "iOS");
        object sizeGroup = gameViewSizesType.GetMethod("GetGroup").Invoke(gameViewSizes, new[] { iOSGroupType });

        int existingIndex = FindSizeIndex(sizeGroup);
        if (existingIndex >= 0)
            return existingIndex;

        object fixedResolution = Enum.Parse(gameViewSizeTypeEnum, "FixedResolution");
        ConstructorInfo constructor = gameViewSizeType.GetConstructor(new[]
        {
            gameViewSizeTypeEnum,
            typeof(int),
            typeof(int),
            typeof(string)
        });

        object customSize = constructor.Invoke(new[]
        {
            fixedResolution,
            GameViewWidth,
            GameViewHeight,
            GameViewSizeLabel
        });

        sizeGroup.GetType().GetMethod("AddCustomSize").Invoke(sizeGroup, new[] { customSize });

        return FindSizeIndex(sizeGroup);
    }

    static object GetScriptableSingletonInstance(Type singletonType)
    {
        Type scriptableSingletonType = typeof(ScriptableSingleton<>).MakeGenericType(singletonType);
        PropertyInfo instanceProperty = scriptableSingletonType.GetProperty(
            "instance",
            BindingFlags.Public | BindingFlags.Static);

        return instanceProperty.GetValue(null);
    }

    static int FindSizeIndex(object sizeGroup)
    {
        string[] displayTexts = (string[])sizeGroup.GetType()
            .GetMethod("GetDisplayTexts")
            .Invoke(sizeGroup, null);

        for (int i = 0; i < displayTexts.Length; i++)
        {
            if (displayTexts[i].Contains(GameViewSizeLabel))
                return i;
        }

        return -1;
    }

    static void SelectGameViewSize(int sizeIndex)
    {
        if (sizeIndex < 0)
            return;

        Type gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
        PropertyInfo selectedSizeIndexProperty = gameViewType.GetProperty(
            "selectedSizeIndex",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        selectedSizeIndexProperty.SetValue(gameView, sizeIndex);
        gameView.Repaint();
    }
}
