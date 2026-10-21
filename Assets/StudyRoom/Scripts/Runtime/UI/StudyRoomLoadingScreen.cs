using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Owns the transition across scene destruction and blocks repeated navigation.</summary>
public sealed class StudyRoomLoadingScreen : MonoBehaviour
{
    private static StudyRoomLoadingScreen instance;
    private CanvasGroup group;
    private RectTransform layout, marker, fill;
    private Text progressLabel, status;
    private RawImage mimiImage;
    private float mimiStartedAt;
    private const float MimiFrameDuration = .14f;
    private StudyRoomLoadingStyle style;
    private float displayedProgress;
    private int screenWidth, screenHeight;
    private Rect safeArea;
    private bool returningHome;
    private static readonly Color Ink = new Color32(91, 69, 103, 255);
    private static readonly Color Pink = new Color32(239, 153, 185, 255);

    public static bool IsLoading => instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState() => instance = null;

    public static bool Load(string sceneName, bool home = false)
    {
        if (instance || !Application.CanStreamedLevelBeLoaded(sceneName)) return false;
        var loadingStyle = Resources.Load<StudyRoomLoadingStyle>("StudyRoomLoadingStyle");
        if (!loadingStyle || !loadingStyle.furnishedRoom || !loadingStyle.mimi || !loadingStyle.font)
        {
            Debug.LogError("Loading screen assets are missing from StudyRoomLoadingStyle.");
            return false;
        }
        var root = new GameObject("StudyRoom Loading");
        DontDestroyOnLoad(root);
        instance = root.AddComponent<StudyRoomLoadingScreen>();
        instance.style = loadingStyle;
        instance.returningHome = home;
        instance.Build();
        instance.StartCoroutine(instance.Transition(sceneName));
        return true;
    }

    private void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960, 720);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        gameObject.AddComponent<GraphicRaycaster>();
        group = gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = true;
        var background = Box(transform, "Backdrop", Vector2.zero, Vector2.zero,
            new Color32(255, 246, 249, 255));
        background.rectTransform.anchorMin = Vector2.zero;
        background.rectTransform.anchorMax = Vector2.one;
        background.rectTransform.offsetMin = background.rectTransform.offsetMax = Vector2.zero;
        background.raycastTarget = true;
        if (style.backgroundFx)
        {
            var fxRect = Rect(transform, "Cute Background", Vector2.zero, Vector2.zero);
            fxRect.anchorMin = Vector2.zero;
            fxRect.anchorMax = Vector2.one;
            fxRect.offsetMin = fxRect.offsetMax = Vector2.zero;
            fxRect.gameObject.AddComponent<RawImage>().raycastTarget = false;
            var fx = fxRect.gameObject.AddComponent<StudyRoomHomeBackgroundFX>();
            fx.prefab = style.backgroundFx;
            // Home and loading can coexist during the transition; keep their cameras apart.
            fx.renderPosition = new Vector3(24000, 24000, 24000);
            var veil = Box(transform, "Soft Pink Veil", Vector2.zero, Vector2.zero,
                new Color(1f, .97f, .985f, .55f)).rectTransform;
            veil.anchorMin = Vector2.zero;
            veil.anchorMax = Vector2.one;
            veil.offsetMin = veil.offsetMax = Vector2.zero;
        }

        layout = Rect(transform, "Loading Card", new Vector2(650, 740), Vector2.zero);
        Label(layout, "Eyebrow", "S T U D Y R O O M", 17, new Vector2(540, 30), new Vector2(0, 330), Pink);
        Label(layout, "Heading", returningHome ? "Về nhà thôi" : "Góc nhỏ đang chờ bạn", 32,
            new Vector2(620, 52), new Vector2(0, 290), Ink, true);
        var room = Rect(layout, "Furnished Room", new Vector2(450, 450), new Vector2(0, 30));
        var roomImage = room.gameObject.AddComponent<RawImage>();
        roomImage.texture = style.furnishedRoom;
        roomImage.raycastTarget = false;
        var roomAspect = (float)style.furnishedRoom.width / style.furnishedRoom.height;
        room.sizeDelta = roomAspect >= 1f ? new Vector2(450, 450 / roomAspect) : new Vector2(450 * roomAspect, 450);

        status = Label(layout, "Status", returningHome ? "Đang trở về Home…" : "Đang chuẩn bị phòng học…", 19,
            new Vector2(620, 34), new Vector2(0, -345), Ink);
        var track = Box(layout, "Progress Track", new Vector2(540, 18), new Vector2(0, -272),
            new Color32(235, 221, 239, 255)).rectTransform;
        track.GetComponent<Image>().sprite = style.rounded;
        track.GetComponent<Image>().type = Image.Type.Sliced;
        fill = Box(track, "Progress Fill", Vector2.zero, Vector2.zero, Pink).rectTransform;
        fill.GetComponent<Image>().sprite = style.rounded;
        fill.GetComponent<Image>().type = Image.Type.Sliced;
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = new Vector2(0, 1);
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        marker = Rect(track, "Mimi", new Vector2(88, 88), Vector2.zero);
        marker.anchorMin = marker.anchorMax = new Vector2(0, .5f);
        marker.anchoredPosition = new Vector2(0, 31);
        mimiImage = marker.gameObject.AddComponent<RawImage>();
        mimiImage.texture = style.mimi;
        mimiImage.raycastTarget = false;
        mimiStartedAt = Time.unscaledTime;
        SetMimiFrame(0);
        progressLabel = Label(layout, "Progress", "0%", 18, new Vector2(100, 30), new Vector2(0, -306), Ink);
        Resize();
        SetProgress(0);
    }

    private IEnumerator Transition(string sceneName)
    {
        // Let the loading canvas render before scene deserialization starts.
        yield return null;
        yield return new WaitForEndOfFrame();
        AsyncOperation operation;
        try { operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single); }
        catch (System.Exception error)
        {
            Debug.LogException(error);
            Destroy(gameObject);
            yield break;
        }
        if (operation == null) { Destroy(gameObject); yield break; }
        operation.allowSceneActivation = false;
        float started = Time.realtimeSinceStartup;
        while (operation.progress < .9f || displayedProgress < .9f || Time.realtimeSinceStartup - started < 1.25f)
        {
            float target = Mathf.Min(.9f, operation.progress);
            SetProgress(Mathf.MoveTowards(displayedProgress, target, Time.unscaledDeltaTime * .85f));
            yield return null;
        }
        status.text = returningHome ? "Đang mở Home…" : "Đang sắp xếp những thứ cuối cùng…";
        operation.allowSceneActivation = true;
        while (!operation.isDone) yield return null;
        // Start methods in the destination instantiate the selected room and restore furniture.
        yield return null;
        yield return new WaitForEndOfFrame();
        while (displayedProgress < 1f)
        {
            SetProgress(Mathf.MoveTowards(displayedProgress, 1f, Time.unscaledDeltaTime * .5f));
            yield return null;
        }
        status.text = returningHome ? "Chào mừng về nhà" : "Phòng học đã sẵn sàng";
        yield return new WaitForSecondsRealtime(.15f);
        float elapsed = 0f;
        while (elapsed < .65f)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = 1f - Mathf.SmoothStep(0, 1, elapsed / .65f);
            yield return null;
        }
        Destroy(gameObject);
    }

    private void SetProgress(float value)
    {
        displayedProgress = Mathf.Clamp01(value);
        fill.anchorMax = new Vector2(displayedProgress, 1);
        progressLabel.text = Mathf.RoundToInt(displayedProgress * 100) + "%";
        marker.anchorMin = marker.anchorMax = new Vector2(displayedProgress, .5f);
    }

    private void Update()
    {
        if (screenWidth != Screen.width || screenHeight != Screen.height || safeArea != Screen.safeArea) Resize();
        SetMimiFrame(Mathf.FloorToInt((Time.unscaledTime - mimiStartedAt) / MimiFrameDuration) % 12);
        // A gentle bob keeps Mimi alive even when loading progress pauses.
        marker.anchoredPosition = new Vector2(0, 31 + Mathf.Sin(Time.unscaledTime * 5f) * 3f);
    }

    private void SetMimiFrame(int frame)
    {
        // Read all 12 expressions left to right, from the top row to the bottom row.
        int column = frame % 4;
        int row = frame / 4;
        mimiImage.uvRect = new Rect(column / 4f, (2 - row) / 3f, 1f / 4f, 1f / 3f);
    }

    private void Resize()
    {
        screenWidth = Screen.width;
        screenHeight = Screen.height;
        safeArea = Screen.safeArea;
        var canvas = GetComponent<Canvas>();
        Canvas.ForceUpdateCanvases();
        float scale = Mathf.Max(.001f, canvas.scaleFactor);
        float width = safeArea.width / scale, height = safeArea.height / scale;
        layout.localScale = Vector3.one * Mathf.Max(.01f, Mathf.Min((width - 40) / 650f, (height - 32) / 740f));
        layout.anchoredPosition = (safeArea.center - new Vector2(screenWidth, screenHeight) * .5f) / scale;
    }

    private Text Label(Transform parent, string name, string text, int size, Vector2 dimensions,
        Vector2 position, Color color, bool title = false)
    {
        var label = Rect(parent, name, dimensions, position).gameObject.AddComponent<Text>();
        label.font = title && style.titleFont ? style.titleFont : style.font;
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        return label;
    }

    private static Image Box(Transform parent, string name, Vector2 size, Vector2 position, Color color)
    {
        var image = Rect(parent, name, size, position).gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static RectTransform Rect(Transform parent, string name, Vector2 size, Vector2 position)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private void OnDestroy() { if (instance == this) instance = null; }
}
