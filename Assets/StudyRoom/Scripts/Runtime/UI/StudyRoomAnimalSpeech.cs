using UnityEngine;
using UnityEngine.UI;

/// <summary>A screen-space speech bubble that follows an animal while its voice plays.</summary>
public sealed class StudyRoomAnimalSpeech : MonoBehaviour
{
    private RectTransform bubble;
    private Canvas canvas;
    private CanvasGroup group;
    private Text label;
    private AudioSource voice;
    private Renderer[] animalRenderers;
    private float visibility;

    public void Show(string animal, AudioSource source)
    {
        string message;
        switch (animal)
        {
            case "Sparrow": message = "chíp chíp"; break;
            case "Taipan": message = "sì sì"; break;
            case "Colobus": message = "hummm"; break;
            case "Squid": message = "ọc ọc ọc"; break;
            case "Pudu": message = "beeee"; break;
            case "Muskrat": message = "chét chét"; break;
            case "Herring": message = "pop po pop"; break;
            case "Gecko": message = "greeeeee"; break;
            default: return;
        }
        if (!bubble) CreateBubble();
        voice = source;
        label.text = message;
        visibility = 0f;
        group.alpha = 0f;
        bubble.localScale = Vector3.one * 0.65f;
        canvas.gameObject.SetActive(true);
        FollowAnimal();
    }

    private void CreateBubble()
    {
        var root = new GameObject("Animal speech", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        var panel = new GameObject("Kawaii bubble", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        bubble = panel.GetComponent<RectTransform>();
        bubble.SetParent(root.transform, false);
        bubble.pivot = new Vector2(0.5f, 0f);
        bubble.sizeDelta = new Vector2(230f, 90f);
        group = panel.GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        var image = panel.GetComponent<Image>();
        image.sprite = Resources.Load<Sprite>("AnimalSpeechBubble");
        image.raycastTarget = false;
        var textObject = new GameObject("Voice text", typeof(RectTransform), typeof(Text));
        var rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(bubble, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(16f, 18f);
        rect.offsetMax = new Vector2(-16f, -8f);
        label = textObject.GetComponent<Text>();
        label.font = Resources.Load<Font>("AnimalSpeechFont");
        label.fontSize = 28;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(0.22f, 0.20f, 0.38f);
        label.raycastTarget = false;
        animalRenderers = GetComponentsInChildren<Renderer>();
    }

    private void LateUpdate()
    {
        if (!bubble || !canvas.gameObject.activeSelf) return;
        bool speaking = voice && voice.isPlaying;
        visibility = Mathf.MoveTowards(visibility, speaking ? 1f : 0f,
            Time.unscaledDeltaTime / (speaking ? 0.2f : 0.16f));
        group.alpha = Mathf.SmoothStep(0f, 1f, visibility);
        float scale = speaking
            ? 1f + 2.70158f * Mathf.Pow(visibility - 1f, 3f) + 1.70158f * Mathf.Pow(visibility - 1f, 2f)
            : Mathf.Lerp(0.7f, 1f, visibility);
        bubble.localScale = Vector3.one * scale;
        FollowAnimal();
        if (!speaking && visibility <= 0f) HideImmediately();
    }

    private void FollowAnimal()
    {
        var camera = Camera.main;
        if (!camera) { group.alpha = 0f; return; }
        Bounds bounds = new Bounds(transform.position, Vector3.zero);
        foreach (var renderer in animalRenderers)
            if (renderer && renderer.enabled) bounds.Encapsulate(renderer.bounds);
        Vector3 screen = camera.WorldToScreenPoint(new Vector3(bounds.center.x, bounds.max.y + 0.08f, bounds.center.z));
        if (screen.z <= 0f) { group.alpha = 0f; return; }
        var parent = (RectTransform)canvas.transform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, null, out var local);
        local.x = Mathf.Clamp(local.x, parent.rect.xMin + 125f, parent.rect.xMax - 125f);
        local.y = Mathf.Clamp(local.y, parent.rect.yMin + 10f, parent.rect.yMax - 100f);
        bubble.anchoredPosition = local;
    }

    public void HideImmediately()
    {
        voice = null;
        visibility = 0f;
        if (canvas) canvas.gameObject.SetActive(false);
    }

    private void OnDisable() => HideImmediately();
    private void OnDestroy()
    {
        if (canvas) Destroy(canvas.gameObject);
    }
}
