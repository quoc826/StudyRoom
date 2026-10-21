using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class StudyRoomSceneBuilder
{
    private const string StartPath = StudyRoomEditorPaths.Scenes + "/ScreenStart.unity";

    public static void CreateStartScene(StudyRoomCatalog catalog, GameObject room, GameObject character, GameObject roomUI)
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var roomScene = roomUI.scene;
        string roomPath = roomScene.path;
        var oldStart = roomUI.GetComponent<StudyRoomStartScreen>();
        if (oldStart) Undo.DestroyObjectImmediate(oldStart);
        var loader = roomUI.GetComponent<StudyRoomRoomLoader>();
        if (!loader) loader = Undo.AddComponent<StudyRoomRoomLoader>(roomUI);
        loader.catalog = catalog; loader.initialRoom = room; loader.initialCharacter = character;
        loader.shopCatalog = AssetDatabase.LoadAssetAtPath<StudyRoomShopCatalog>(StudyRoomEditorPaths.ShopCatalog);
        var shop = roomUI.GetComponent<StudyRoomShopUI>();
        if (!shop) shop = Undo.AddComponent<StudyRoomShopUI>(roomUI);
        shop.catalog = loader.shopCatalog;
        shop.BuildInterface();
        var furnitureEdit = roomUI.GetComponent<StudyRoomFurnitureEditUI>();
        if (!furnitureEdit) furnitureEdit = Undo.AddComponent<StudyRoomFurnitureEditUI>(roomUI);
        furnitureEdit.BuildInterface();
        EditorUtility.SetDirty(loader);
        EditorSceneManager.MarkSceneDirty(roomScene);
        if (!EditorSceneManager.SaveScene(roomScene)) throw new InvalidOperationException("Room scene save failed.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(StartPath))
        {
            EditorSceneManager.OpenScene(StartPath);
            RegisterBuildScenes(roomPath);
            return;
        }
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(scene);
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0, 0, -10);
        var camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true; camera.orthographicSize = 5;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color32(213, 204, 231, 255);
        var root = new GameObject("ScreenStartUI");
        var start = root.AddComponent<StudyRoomStartScreen>();
        start.catalog = catalog;
        start.homeBackgroundFx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/StudyRoom/UI/Cute Background FX/Prefabs/URP/Diagonal_lines_pink(URP).prefab");
        start.regularFont = AssetDatabase.LoadAssetAtPath<Font>(StudyRoomEditorPaths.Fonts + "/CuteRounded.ttf");
        start.titleFont = AssetDatabase.LoadAssetAtPath<Font>(StudyRoomEditorPaths.Fonts + "/CuteRoundedBold.ttf");
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(StudyRoomEditorPaths.Sprites + "/RoundedPanel.asset"))
            if (asset is Sprite sprite) start.roundedSprite = sprite;
        var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(root.transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1; canvas.sortingOrder = 100;
        var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
        var events = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#else
        events.AddComponent<StandaloneInputModule>();
#endif
        // Author the hierarchy into the scene so its UI and idle character are visible before Play.
        typeof(StudyRoomStartScreen).GetMethod("BuildInterface", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(start, null);
        typeof(StudyRoomStartScreen).GetMethod("BuildCharacterPreview", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(start, new object[] { catalog.characters[0].prefab });
        var screen = root.transform.Find("Canvas/ScreenStart");
        screen.Find("RoomPopup").gameObject.SetActive(false);
        screen.Find("Picker").gameObject.SetActive(false);
        screen.Find("UnlockConfirmation").gameObject.SetActive(false);
        var preview = root.transform.Find("StartScreenCharacterPreview");
        var previewCamera = preview.GetComponentInChildren<Camera>();
        StudyRoomEditorPaths.EnsureFolder(StudyRoomEditorPaths.RenderTextures);
        const string renderPath = StudyRoomEditorPaths.RenderTextures + "/StartCharacterPreview.renderTexture";
        var target = AssetDatabase.LoadAssetAtPath<RenderTexture>(renderPath);
        if (!target)
        {
            target = new RenderTexture(768, 1024, 24, RenderTextureFormat.ARGB32);
            target.name = "StartCharacterPreview";
            AssetDatabase.CreateAsset(target, renderPath);
        }
        var temporary = previewCamera.targetTexture;
        previewCamera.targetTexture = target;
        screen.Find("Start/CharacterArea/IdleCharacter").GetComponent<RawImage>().texture = target;
        // The authoring render texture is an asset; runtime creates and owns a separate texture.
        typeof(StudyRoomStartScreen).GetField("previewTexture", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(start, null);
        if (temporary) { temporary.Release(); UnityEngine.Object.DestroyImmediate(temporary); }
        var serialized = new SerializedObject(start);
        serialized.FindProperty("authoredPreview").objectReferenceValue = preview.gameObject;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        var animator = preview.GetComponentInChildren<Animator>();
        if (animator)
        {
            animator.Rebind();
            int homeIdle = Animator.StringToHash("Base Layer.HomeIdle");
            if (animator.HasState(0, homeIdle)) animator.Play(homeIdle, 0, 0f);
            animator.Update(0.2f);
        }
        target.Create(); previewCamera.Render(); Canvas.ForceUpdateCanvases();
        EditorUtility.SetDirty(start); AssetDatabase.SaveAssets();
        if (!EditorSceneManager.SaveScene(scene, StartPath)) throw new InvalidOperationException("Start scene save failed.");
        RegisterBuildScenes(roomPath);
        EditorSceneManager.CloseScene(roomScene, true);
        Selection.activeGameObject = root;
    }

    private static void RegisterBuildScenes(string roomPath)
    {
        var scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(StartPath, true),
            new EditorBuildSettingsScene(roomPath, true)
        };
        foreach (var scene in EditorBuildSettings.scenes)
            if (scene.path != StartPath && scene.path != roomPath) scenes.Add(scene);
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
