#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class AutoLoadDemoScene
{
    private const string TargetScenePath = "Assets/ScifiOfficeLite/Scene/Demo.unity";

    static AutoLoadDemoScene()
    {
        EditorApplication.delayCall += CheckAndOpenDemoScene;
    }

    private static void CheckAndOpenDemoScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        // If current scene is empty, untitled, or not set
        if (string.IsNullOrEmpty(activeScene.path) || activeScene.name == "Untitled")
        {
            if (System.IO.File.Exists(TargetScenePath))
            {
                EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
                Debug.Log("[Smart-Evac] Automatically loaded Demo.unity scene.");
            }
        }
    }
}
#endif
