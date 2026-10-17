using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

/// <summary>Connects the project's laptop artwork to the room's computer interface.</summary>
[InitializeOnLoad]
public static class StudyRoomComputerBuilder
{
    static StudyRoomComputerBuilder()
    {
        EditorApplication.delayCall += ConfigureOpenedScenes;
        EditorSceneManager.sceneOpened += (scene, mode) => Configure(scene);
    }

    private static void ConfigureOpenedScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++) Configure(SceneManager.GetSceneAt(i));
    }

    private static void Configure(Scene scene)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !scene.isLoaded || string.IsNullOrEmpty(scene.path)) return;
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/StudyRoom/UI/Sprites/laptop.png");
        if (!texture) return;
        var tracks = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/StudyRoom/Sound/MusicList" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(path => path, System.StringComparer.OrdinalIgnoreCase)
            .Select(path => AssetDatabase.LoadAssetAtPath<AudioClip>(path))
            .Where(clip => clip).ToArray();
        bool wasDirty = scene.isDirty;
        bool changed = false;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var loader in root.GetComponentsInChildren<StudyRoomRoomLoader>(true))
            {
                var computer = loader.GetComponent<StudyRoomComputerUI>();
                if (!computer) computer = loader.gameObject.AddComponent<StudyRoomComputerUI>();
                var serialized = new SerializedObject(computer);
                var artwork = serialized.FindProperty("laptopTexture");
                if (!artwork.objectReferenceValue) artwork.objectReferenceValue = texture;
                var playlist = serialized.FindProperty("tracks");
                playlist.arraySize = tracks.Length;
                for (int i = 0; i < tracks.Length; i++)
                    playlist.GetArrayElementAtIndex(i).objectReferenceValue = tracks[i];
                if (serialized.ApplyModifiedPropertiesWithoutUndo()) changed = true;
            }
        if (!changed) return;
        EditorSceneManager.MarkSceneDirty(scene);
        if (!wasDirty) EditorSceneManager.SaveScene(scene);
    }
}
