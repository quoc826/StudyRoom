using UnityEngine;
using UnityEngine.UI;

public class StudyRoomFurnitureEditUI : MonoBehaviour
{
    private StudyRoomDecorations decorations;
    private GameObject shortcut, toolbar;
    private Text title, hint;
    private Button move, remove, done, rotate, confirm, cancel;
    private Font font;
    private Sprite rounded;
    private readonly Color ink = new Color32(79, 57, 98, 255);
    private readonly Color pale = new Color32(232, 220, 248, 255);
    private readonly Color pink = new Color32(245, 173, 198, 255);

    private void Start()
    {
        decorations = GetComponent<StudyRoomDecorations>();
        if (!decorations) decorations = gameObject.AddComponent<StudyRoomDecorations>();
        BuildInterface();
    }

    public void Open()
    {
        var computer = GetComponent<StudyRoomComputerUI>(); if (computer && computer.IsOpen) computer.Close();
        var rewards = GetComponent<StudyRoomRewardsUI>(); if (rewards && rewards.IsOpen) rewards.Close();
        var shop = GetComponent<StudyRoomShopUI>(); if (shop && shop.IsOpen) shop.Close();
        var menu = GetComponent<StudyRoomMenu>(); if (menu) menu.CloseMenu();
        decorations.BeginEditing();
        Refresh();
    }

    private void Update() => Refresh();

    private void Refresh()
    {
        if (!decorations || !toolbar) return;
        bool editing = decorations.IsEditing;
        toolbar.SetActive(editing);
        var computer = GetComponent<StudyRoomComputerUI>();
        var shop = GetComponent<StudyRoomShopUI>();
        var rewards = GetComponent<StudyRoomRewardsUI>();
        var menu = transform.Find("Canvas/Overlay");
        shortcut.SetActive(!editing && !decorations.IsPlacing && !(computer && computer.IsOpen) &&
            !(shop && shop.IsOpen) && !(rewards && rewards.IsOpen) && !(menu && menu.gameObject.activeSelf));
        if (!editing) return;
        bool placing = decorations.IsPlacing;
        move.gameObject.SetActive(!placing); remove.gameObject.SetActive(!placing); done.gameObject.SetActive(!placing);
        rotate.gameObject.SetActive(placing); confirm.gameObject.SetActive(placing); cancel.gameObject.SetActive(placing);
        bool selected = decorations.SelectedFurniture;
        move.interactable = remove.interactable = selected;
        confirm.interactable = decorations.ValidPlacement;
        title.text = decorations.SelectedFurnitureName;
        hint.text = placing ? (decorations.ValidPlacement ? "Vị trí hợp lệ · Bấm Xác nhận để lưu" : "Chọn vị trí trống bên trong phòng")
            : selected ? "Di chuyển hoặc xóa khỏi phòng · Đồ đã mua được trả về kho" : "Chạm vào đồ vật để chọn · Sàn, tường và cửa được giữ cố định";
    }

    public void BuildInterface()
    {
        if (toolbar) return;
        var canvas = transform.Find("Canvas"); if (!canvas) return;
        var reference = canvas.GetComponentInChildren<Text>(true);
        font = reference && reference.font ? reference.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var style = canvas.Find("MenuButton");
        if (style && style.GetComponent<Image>()) rounded = style.GetComponent<Image>().sprite;
        var root = Rect(canvas, "FurnitureEditUI"); Stretch(root);
        var layer = root.gameObject.AddComponent<Canvas>(); layer.overrideSorting = true; layer.sortingOrder = 260;
        root.gameObject.AddComponent<GraphicRaycaster>();
        var open = Button(root, "EditButton", "Thay đổi\nvị trí", Open, pale);
        shortcut = open.gameObject;
        var openRect = (RectTransform)open.transform; openRect.anchorMin = openRect.anchorMax = openRect.pivot = Vector2.one;
        openRect.sizeDelta = new Vector2(132, 88); openRect.anchoredPosition = new Vector2(-16, -244);
        var bar = Rect(root, "Toolbar");
        bar.anchorMin = Vector2.zero; bar.anchorMax = new Vector2(1, 0); bar.pivot = new Vector2(.5f, 0);
        bar.sizeDelta = new Vector2(-24, 200); bar.anchoredPosition = new Vector2(0, 16);
        var background = bar.gameObject.AddComponent<Image>(); background.color = new Color32(255, 247, 252, 255);
        background.sprite = rounded; background.type = Image.Type.Sliced;
        toolbar = bar.gameObject;
        title = Label(bar, "Title", 24); title.rectTransform.anchorMin = new Vector2(0, 1); title.rectTransform.anchorMax = Vector2.one;
        title.rectTransform.offsetMin = new Vector2(16, -52); title.rectTransform.offsetMax = new Vector2(-16, -8);
        hint = Label(bar, "Hint", 18); hint.rectTransform.anchorMin = new Vector2(0, 1); hint.rectTransform.anchorMax = Vector2.one;
        hint.rectTransform.offsetMin = new Vector2(16, -112); hint.rectTransform.offsetMax = new Vector2(-16, -54);
        move = Action(bar, "Move", "Di chuyển", 0, () => decorations.MoveSelectedFurniture(), pink);
        remove = Action(bar, "Delete", "Xóa khỏi phòng", 1, () => decorations.DeleteSelectedFurniture(), pale);
        done = Action(bar, "Done", "Xong", 2, () => decorations.EndEditing(), pale);
        rotate = Action(bar, "Rotate", "Xoay 90°", 0, () => decorations.RotatePreview(), pale);
        confirm = Action(bar, "Confirm", "Xác nhận", 1, () => decorations.ConfirmPlacement(), pink);
        cancel = Action(bar, "Cancel", "Hủy", 2, () => decorations.CancelPlacement(), pale);
        toolbar.SetActive(false);
    }

    private Button Action(Transform parent, string name, string caption, int column, UnityEngine.Events.UnityAction action, Color color)
    {
        var button = Button(parent, name, caption, action, color);
        var rect = (RectTransform)button.transform;
        rect.anchorMin = new Vector2(column / 3f, 0); rect.anchorMax = new Vector2((column + 1) / 3f, 0);
        rect.offsetMin = new Vector2(8, 12); rect.offsetMax = new Vector2(-8, 80);
        return button;
    }

    private Button Button(Transform parent, string name, string caption, UnityEngine.Events.UnityAction action, Color color)
    {
        var rect = Rect(parent, name); var image = rect.gameObject.AddComponent<Image>(); image.color = color;
        image.sprite = rounded; image.type = Image.Type.Sliced;
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(action);
        rect.gameObject.AddComponent<StudyRoomButtonSound>();
        var text = Label(rect, "Label", 20); text.text = caption; text.alignment = TextAnchor.MiddleCenter;
        Stretch(text.rectTransform); text.rectTransform.offsetMin = new Vector2(6, 4); text.rectTransform.offsetMax = new Vector2(-6, -4);
        return button;
    }

    private Text Label(Transform parent, string name, int size)
    {
        var rect = Rect(parent, name); var text = rect.gameObject.AddComponent<Text>();
        text.font = font; text.fontSize = size; text.color = ink; text.raycastTarget = false;
        text.resizeTextForBestFit = true; text.resizeTextMinSize = 16; text.resizeTextMaxSize = size;
        text.verticalOverflow = VerticalWrapMode.Truncate; text.alignment = TextAnchor.MiddleLeft;
        return text;
    }

    private static RectTransform Rect(Transform parent, string name)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); return rect;
    }

    private static void Stretch(RectTransform rect)
    { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
}
