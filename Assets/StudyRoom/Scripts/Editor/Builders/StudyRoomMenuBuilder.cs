using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class StudyRoomMenuBuilder
{
    private static readonly Color Ink = new Color32(79, 57, 98, 255);
    private static readonly Color Cream = new Color32(255, 247, 252, 255);
    private static readonly Color Accent = new Color32(245, 173, 198, 255);
    private static readonly Color Pale = new Color32(232, 220, 248, 255);
    private static Font font;
    private static Sprite rounded;

    [MenuItem("StudyRoom/Create Menu UI")]
    public static void Build()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        if (GameObject.Find("StudyRoomUI")) throw new InvalidOperationException("StudyRoomUI already exists.");
        const string folder = StudyRoomEditorPaths.Sprites;
        StudyRoomEditorPaths.EnsureFolder(folder);
        StudyRoomEditorPaths.EnsureFolder(StudyRoomEditorPaths.Quality);
        font = AssetDatabase.LoadAssetAtPath<Font>(StudyRoomEditorPaths.Fonts + "/CuteRounded.ttf");
        if (!font) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        rounded = RoundedSprite(folder);
        var root = new GameObject("StudyRoomUI");
        Undo.RegisterCreatedObjectUndo(root, "Create StudyRoom menu");
        var behaviour = root.AddComponent<StudyRoomMenu>();
        var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(root.transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = 1f;
        canvas.sortingOrder = 100;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        var menuButton = Button(canvasObject.transform, "MenuButton", "☰  Menu", new Vector2(144, 52), Vector2.zero, Cream);
        var menuRect = menuButton.GetComponent<RectTransform>();
        menuRect.anchorMin = menuRect.anchorMax = menuRect.pivot = new Vector2(1, 1);
        menuRect.anchoredPosition = new Vector2(-28, -28);

        var overlay = Rect(canvasObject.transform, "Overlay", Vector2.zero, Vector2.zero);
        Stretch(overlay);
        var overlayCanvas = overlay.gameObject.AddComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 101;
        overlay.gameObject.AddComponent<GraphicRaycaster>();
        var shade = overlay.gameObject.AddComponent<Image>();
        shade.color = new Color(0.10f, 0.16f, 0.20f, 0.70f);
        var menu = Panel(overlay, "MenuPanel", new Vector2(360, 386));
        Text(menu, "Eyebrow", "STUDY ROOM", 13, new Vector2(290, 24), new Vector2(0, 144), Accent);
        Text(menu, "Title", "Một chút nghỉ ngơi", 26, new Vector2(310, 46), new Vector2(0, 108), Ink);
        Button(menu, "Close", "Tiếp tục", new Vector2(292, 56), new Vector2(0, 33), Accent);
        Button(menu, "Settings", "Settings", new Vector2(292, 56), new Vector2(0, -37), Pale);
        Button(menu, "Home", "Home", new Vector2(292, 48), new Vector2(0, -113), Pale);

        var settings = Panel(overlay, "SettingsPanel", new Vector2(500, 510));
        Text(settings, "Title", "Settings", 32, new Vector2(420, 44), new Vector2(0, 170), Ink);
        Text(settings, "Subtitle", "Điều chỉnh cho buổi học thật thoải mái", 16, new Vector2(440, 28), new Vector2(0, 130), Ink);
        Volume(settings, "Sfx", "SFX · Hiệu ứng", 77);
        Volume(settings, "Music", "Background Music · Nhạc nền", -9);
        Text(settings, "GraphicsTitle", "CHẤT LƯỢNG ĐỒ HỌA", 13, new Vector2(420, 28), new Vector2(0, -112), Ink);
        for (int i = 0; i < 3; i++) Button(settings, "Quality" + i, new[] { "Thấp", "Vừa", "Cao" }[i], new Vector2(130, 48), new Vector2(-144 + i * 144, -159), i == 2 ? Accent : Pale);
        Button(settings, "Back", "Lưu & quay lại", new Vector2(420, 46), new Vector2(0, -220), Pale);

        StudyRoomMenu.EnsureVoiceControls(settings);
        var home = Panel(overlay, "HomePanel", new Vector2(440, 370));
        Text(home, "Eyebrow", "CHÀO MỪNG VỀ NHÀ", 13, new Vector2(370, 24), new Vector2(0, 135), Accent);
        Text(home, "Title", "StudyRoom", 40, new Vector2(370, 58), new Vector2(0, 81), Ink);
        Text(home, "Subtitle", "Một góc nhỏ để tập trung và thư giãn", 17, new Vector2(390, 32), new Vector2(0, 26), Ink);
        Button(home, "EnterRoom", "Vào phòng", new Vector2(356, 56), new Vector2(0, -51), Accent);
        Button(home, "Settings", "Settings", new Vector2(356, 48), new Vector2(0, -117), Pale);
        settings.gameObject.SetActive(false);
        home.gameObject.SetActive(false);
        overlay.gameObject.SetActive(false);

        Audio(root.transform, "SFX", RoomAudioChannel.Channel.Sfx, false);
        Audio(root.transform, "BackgroundMusic", RoomAudioChannel.Channel.BackgroundMusic, true);
        if (!UnityEngine.Object.FindAnyObjectByType<EventSystem>())
        {
            var events = new GameObject("EventSystem", typeof(EventSystem));
            Undo.RegisterCreatedObjectUndo(events, "Create UI input");
#if ENABLE_INPUT_SYSTEM
            var input = events.AddComponent<InputSystemUIInputModule>();
            input.AssignDefaultActions();
#else
            events.AddComponent<StandaloneInputModule>();
#endif
        }
        var profiles = QualityProfiles(StudyRoomEditorPaths.Quality);
        var serialized = new SerializedObject(behaviour);
        var property = serialized.FindProperty("qualityProfiles");
        property.arraySize = profiles.Length;
        for (int i = 0; i < profiles.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = profiles[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
        ApplyCuteStyle(root);
        root.AddComponent<StudyRoomRewardsUI>().BuildInterface();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(root.scene);
        EditorSceneManager.SaveScene(root.scene);
        Selection.activeGameObject = root;
    }

    public static void ApplyCuteStyle(GameObject root)
    {
        var regular = AssetDatabase.LoadAssetAtPath<Font>(StudyRoomEditorPaths.Fonts + "/CuteRounded.ttf");
        var bold = AssetDatabase.LoadAssetAtPath<Font>(StudyRoomEditorPaths.Fonts + "/CuteRoundedBold.ttf");
        if (!regular || !bold) throw new InvalidOperationException("Cute fonts missing.");
        foreach (var text in root.GetComponentsInChildren<Text>(true))
        {
            Undo.RecordObject(text, "Style cute text");
            bool prominent = text.name == "Title" || text.name == "Label";
            text.font = prominent ? bold : regular;
            text.fontStyle = FontStyle.Normal;
            if (text.name == "Title") text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = text.name == "Eyebrow" ? new Color32(169, 89, 128, 255) : Ink;
            if (text.name == "Label") text.fontSize = 18;
            if (text.name == "MusicLabel") text.fontSize = 15;
            if (text.name == "Subtitle") text.fontSize = 15;
            if (text.name == "Eyebrow") text.fontSize = 12;
        }
        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            Undo.RecordObject(image, "Style pastel UI");
            string name = image.name;
            if (name.EndsWith("Panel")) image.color = Cream;
            else if (name == "Overlay") image.color = new Color(0.16f, 0.12f, 0.24f, 0.60f);
            else if (name == "Track") image.color = Pale;
            else if (name == "Fill") image.color = Accent;
            else if (name == "Handle")
            {
                image.color = new Color32(169, 114, 190, 255);
                image.type = UnityEngine.UI.Image.Type.Simple;
                image.rectTransform.sizeDelta = new Vector2(24, -4);
            }
        }
        foreach (var button in root.GetComponentsInChildren<Button>(true))
        {
            Undo.RecordObject(button, "Style cute button");
            var image = button.GetComponent<Image>();
            bool primary = (button.name == "Close" && button.transform.parent.name == "MenuPanel") || button.name == "EnterRoom" || button.name == "MenuButton" || button.name == "Quality2";
            image.color = primary ? Accent : Pale;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.94f, 0.98f);
            colors.pressedColor = new Color(0.88f, 0.80f, 0.93f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = 0.12f;
            button.colors = colors;
            var shadow = button.GetComponent<Shadow>();
            if (!shadow) shadow = button.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.45f, 0.28f, 0.53f, 0.16f);
            shadow.effectDistance = new Vector2(0, -3);
        }
        var menuButton = root.transform.Find("Canvas/MenuButton");
        menuButton.Find("Label").GetComponent<Text>().text = "Menu";
        menuButton.Find("Label").GetComponent<RectTransform>().offsetMin = new Vector2(26, 0);
        for (int i = 0; i < 3; i++)
        {
            var existing = menuButton.Find("MenuLine" + i);
            var line = existing ? (RectTransform)existing : Rect(menuButton, "MenuLine" + i, new Vector2(18, 3), new Vector2(-43, 6 - i * 6));
            var graphic = line.GetComponent<Image>();
            if (!graphic) graphic = line.gameObject.AddComponent<Image>();
            graphic.color = Ink; graphic.raycastTarget = false;
        }
        foreach (var shadow in root.GetComponentsInChildren<Shadow>(true))
            if (shadow.name.EndsWith("Panel")) shadow.effectColor = new Color(0.35f, 0.21f, 0.45f, 0.18f);
        EditorSceneManager.MarkSceneDirty(root.scene);
    }

    private static RectTransform Rect(Transform parent, string name, Vector2 size, Vector2 position)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    private static RectTransform Panel(Transform parent, string name, Vector2 size)
    {
        var rect = Rect(parent, name, size, Vector2.zero);
        Image(rect, Cream);
        var shadow = rect.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.07f, 0.12f, 0.16f, 0.2f);
        shadow.effectDistance = new Vector2(0, -8);
        return rect;
    }
    private static Image Image(RectTransform rect, Color color)
    {
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = rounded; image.type = UnityEngine.UI.Image.Type.Sliced; image.color = color;
        return image;
    }
    private static Text Text(Transform parent, string name, string value, int size, Vector2 dimensions, Vector2 position, Color color)
    {
        var rect = Rect(parent, name, dimensions, position);
        var text = rect.gameObject.AddComponent<Text>();
        text.font = font; text.fontSize = size; text.text = value; text.color = color;
        text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
        return text;
    }
    private static Button Button(Transform parent, string name, string label, Vector2 size, Vector2 position, Color color)
    {
        var rect = Rect(parent, name, size, position);
        Image(rect, color);
        var button = rect.gameObject.AddComponent<Button>();
        rect.gameObject.AddComponent<StudyRoomButtonSound>();
        var colors = button.colors;
        colors.highlightedColor = new Color(0.94f, 0.97f, 1f);
        colors.pressedColor = new Color(0.82f, 0.86f, 0.90f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.12f;
        button.colors = colors;
        var text = Text(rect, "Label", label, 18, size, Vector2.zero, Ink);
        Stretch(text.rectTransform);
        return button;
    }
    private static void Volume(Transform parent, string prefix, string title, float y)
    {
        var label = Text(parent, prefix + "Label", title, 17, new Vector2(340, 28), new Vector2(-40, y), Ink);
        label.alignment = TextAnchor.MiddleLeft;
        Text(parent, prefix + "Value", "70%", 16, new Vector2(64, 28), new Vector2(178, y), Ink);
        var rect = Rect(parent, prefix + "Slider", new Vector2(420, 28), new Vector2(0, y - 34));
        var hit = rect.gameObject.AddComponent<Image>(); hit.color = Color.clear;
        var track = Rect(rect, "Track", new Vector2(420, 8), Vector2.zero); Image(track, Pale).raycastTarget = false;
        var fillArea = Rect(rect, "FillArea", new Vector2(400, 8), Vector2.zero);
        var fill = Rect(fillArea, "Fill", Vector2.zero, Vector2.zero); Stretch(fill); Image(fill, Accent).raycastTarget = false;
        var handleArea = Rect(rect, "HandleArea", new Vector2(400, 28), Vector2.zero);
        var handle = Rect(handleArea, "Handle", new Vector2(22, 22), Vector2.zero); var image = Image(handle, Ink);
        var slider = rect.gameObject.AddComponent<Slider>();
        slider.fillRect = fill; slider.handleRect = handle; slider.targetGraphic = image;
        slider.minValue = 0; slider.maxValue = 1; slider.value = 0.7f;
    }
    private static void Audio(Transform parent, string name, RoomAudioChannel.Channel channel, bool loop)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        var source = go.AddComponent<AudioSource>(); source.playOnAwake = loop; source.loop = loop; source.spatialBlend = 0;
        go.AddComponent<RoomAudioChannel>().channel = channel;
    }
    private static UniversalRenderPipelineAsset[] QualityProfiles(string folder)
    {
        var original = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(StudyRoomEditorPaths.Rendering + "/PC_RPAsset.asset");
        if (!original) throw new InvalidOperationException("PC render pipeline missing.");
        var result = new UniversalRenderPipelineAsset[3];
        for (int i = 0; i < 3; i++)
        {
            string path = folder + "/Graphics_" + new[] { "Low", "Medium", "High" }[i] + ".asset";
            result[i] = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (!result[i]) { result[i] = UnityEngine.Object.Instantiate(original); AssetDatabase.CreateAsset(result[i], path); }
            result[i].renderScale = new[] { 0.75f, 0.9f, 1f }[i];
            result[i].msaaSampleCount = new[] { 1, 2, 4 }[i];
            result[i].shadowDistance = new[] { 15f, 25f, 40f }[i];
            result[i].mainLightShadowmapResolution = new[] { 512, 1024, 2048 }[i];
            var serialized = new SerializedObject(result[i]);
            var softShadows = serialized.FindProperty("m_SoftShadowsSupported");
            if (softShadows != null) { softShadows.boolValue = i > 0; serialized.ApplyModifiedPropertiesWithoutUndo(); }
            EditorUtility.SetDirty(result[i]);
        }
        return result;
    }
    private static Sprite RoundedSprite(string folder)
    {
        string path = folder + "/RoundedPanel.asset";
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path)) if (asset is Sprite sprite) return sprite;
        var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        texture.name = "RoundedPanel"; texture.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
        {
            Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
            Vector2 center = new Vector2(Mathf.Clamp(p.x, 14, 50), Mathf.Clamp(p.y, 14, 50));
            float alpha = Mathf.Clamp01(14.5f - Vector2.Distance(p, center));
            texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
        }
        texture.Apply(); AssetDatabase.CreateAsset(texture, path);
        var roundedSprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), Vector2.one * 0.5f, 100, 0, SpriteMeshType.FullRect, new Vector4(16, 16, 16, 16));
        roundedSprite.name = "RoundedPanelSprite"; AssetDatabase.AddObjectToAsset(roundedSprite, texture);
        return roundedSprite;
    }
}
