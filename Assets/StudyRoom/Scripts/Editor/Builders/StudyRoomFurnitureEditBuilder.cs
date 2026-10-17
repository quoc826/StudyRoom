using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Stores the furniture controls in the scene so they are visible in Edit Mode.</summary>
[InitializeOnLoad]
public static class StudyRoomFurnitureEditBuilder
{
    static StudyRoomFurnitureEditBuilder()
    {
        EditorApplication.delayCall += BuildOpenedScenes;
        EditorSceneManager.sceneOpened += (scene, mode) => Build(scene);
    }

    private static void BuildOpenedScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++) Build(SceneManager.GetSceneAt(i));
    }

    private static void Build(Scene scene)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !scene.isLoaded || string.IsNullOrEmpty(scene.path)) return;
        bool wasDirty = scene.isDirty;
        bool changed = false;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var loader in root.GetComponentsInChildren<StudyRoomRoomLoader>(true))
            {
                var canvas = loader.transform.Find("Canvas");
                if (!canvas) continue;
                var responsive = canvas.GetComponent<StudyRoomResponsiveUI>();
                if (!responsive)
                {
                    responsive = canvas.gameObject.AddComponent<StudyRoomResponsiveUI>();
                    changed = true;
                }
                responsive.ApplyViewport(Screen.width, Screen.height, Screen.safeArea);
                if (canvas.Find("FurnitureEditUI")) continue;
                var ui = loader.GetComponent<StudyRoomFurnitureEditUI>();
                if (!ui) ui = loader.gameObject.AddComponent<StudyRoomFurnitureEditUI>();
                ui.BuildInterface();
                responsive.ApplyViewport(Screen.width, Screen.height, Screen.safeArea);
                EditorUtility.SetDirty(ui);
                changed = true;
            }
        if (!changed) return;
        EditorSceneManager.MarkSceneDirty(scene);
        if (!wasDirty) EditorSceneManager.SaveScene(scene);
    }
}
