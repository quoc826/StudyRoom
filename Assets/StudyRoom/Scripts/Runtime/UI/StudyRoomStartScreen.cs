using System;
using UnityEngine;
using UnityEngine.UI;

public class StudyRoomStartScreen : MonoBehaviour
{
    public StudyRoomCatalog catalog;
    public Font regularFont;
    public Font titleFont;
    public Sprite roundedSprite;
    [SerializeField] private GameObject authoredPreview;

    public StudyRoomProgress Progress { get; private set; }
    public bool IsShowing => screen && screen.activeSelf;
    public string SelectedRoomId { get; private set; }
    public string SelectedCharacterId { get; private set; }
    private GameObject screen, start, picker, popup, confirmation;
    private GameObject roomNaming;
    private InputField roomNameInput;
    private RectTransform listContent;
    private ScrollRect scroll;
    private Text heading, helper, wallet, message;
    private Button next;
    private RawImage characterImage;
    private GameObject previewRig;
    private RenderTexture previewTexture;
    private Camera previewCamera;
    private bool choosingCharacters;
    private bool loadingRoom;
    private StudyRoomEconomy economy;
    private readonly Color ink = new Color32(79, 57, 98, 255);
    private readonly Color pink = new Color32(245, 173, 198, 255);
    private readonly Color lavender = new Color32(232, 220, 248, 255);
    private readonly Color cream = new Color32(255, 247, 252, 255);

    private void Start()
    {
        if (!catalog || catalog.characters == null || catalog.characters.Length == 0)
        {
            Debug.LogError("StudyRoom needs a catalog with at least one character.", this);
            return;
        }
        economy = StudyRoomEconomy.Ensure(catalog);
        Progress = economy.Progress;
        economy.StateChanged += RefreshWallet;
        var authoredScreen = transform.Find("Canvas/ScreenStart");
        if (authoredScreen) { authoredScreen.gameObject.SetActive(false); Destroy(authoredScreen.gameObject); }
        if (authoredPreview) { authoredPreview.SetActive(false); Destroy(authoredPreview); }
        BuildInterface();
        BuildCharacterPreview(catalog.characters[0].prefab);
        ShowStartScreen();
    }

    public void ShowStartScreen()
    {
        if (!screen) return;
        screen.SetActive(true);
        start.SetActive(true);
        picker.SetActive(false);
        popup.SetActive(false);
        confirmation.SetActive(false);
        roomNaming.SetActive(false);
        previewRig.SetActive(true);
        RefreshWallet();
    }

    public void ShowRoomPopup()
    {
        popup.SetActive(true);
        message.text = "";
    }

    public void ShowSavedRooms()
    {
        choosingCharacters = false;
        BeginPicker("Phòng đã tạo", "Chọn một góc nhỏ để tiếp tục buổi học");
        next.gameObject.SetActive(false);
        foreach (var save in Progress.SavedRooms)
        {
            var room = catalog.FindRoom(save.roomId);
            var character = catalog.FindCharacter(save.characterId);
            if (room == null || character == null) continue;
            var selected = save;
            string description = save.id == "initial-room"
                ? "Phòng khởi đầu đủ nội thất · Học để kiếm coin mua phòng mới"
                : room.displayName + " · " + character.displayName;
            Card(save.name, description,
                "Vào phòng", room.thumbnail, false, () => EnterRoom(selected));
        }
    }

    public void BeginCreateRoom()
    {
        SelectedRoomId = null;
        SelectedCharacterId = null;
        roomNameInput.text = "";
        ShowRoomChoices();
    }

    private void ShowRoomChoices()
    {
        choosingCharacters = false;
        BeginPicker("Chọn phòng", "BƯỚC 1 / 2 · Lướt xuống để khám phá phòng rỗng");
        next.gameObject.SetActive(true);
        next.transform.Find("Label").GetComponent<Text>().text = "Tiếp theo · Chọn nhân vật";
        next.interactable = !string.IsNullOrEmpty(SelectedRoomId);
        foreach (var room in catalog.rooms)
        {
            if (!room.canCreate) continue;
            var choice = room;
            bool unlocked = Progress.RoomUnlocked(room.id);
            Card(room.displayName, unlocked ? "Phòng rỗng · Sẵn sàng cho ý tưởng của bạn" : "Chưa mở khóa · " + room.price + " coin",
                room.id == SelectedRoomId ? "Đã chọn" : unlocked ? "Chọn phòng" : "Mở khóa · " + room.price + " coin",
                room.thumbnail, room.id == SelectedRoomId, () => SelectRoom(choice));
        }
    }

    public void SelectRoom(StudyRoomCatalog.Room room)
    {
        if (!Progress.RoomUnlocked(room.id)) { AskUnlock(room.id, room.displayName, room.price, false); return; }
        SelectedRoomId = room.id;
        ShowRoomChoices();
    }

    public void ShowCharacterChoices()
    {
        if (string.IsNullOrEmpty(SelectedRoomId) || !Progress.RoomUnlocked(SelectedRoomId)) return;
        choosingCharacters = true;
        BeginPicker("Chọn nhân vật", catalog.characters.Length == 1
            ? "BƯỚC 2 / 2 · CuteGirl là người bạn đầu tiên của bạn"
            : "BƯỚC 2 / 2 · Chọn người bạn cùng học");
        next.gameObject.SetActive(true);
        next.transform.Find("Label").GetComponent<Text>().text = "Tiếp theo · Đặt tên phòng";
        next.interactable = !string.IsNullOrEmpty(SelectedCharacterId);
        foreach (var character in catalog.characters)
        {
            var choice = character;
            bool unlocked = Progress.CharacterUnlocked(character.id);
            Card(character.displayName, unlocked ? "Đã mở khóa · Animation idle & walk" : "Chưa mở khóa · " + character.price + " coin",
                character.id == SelectedCharacterId ? "Đã chọn" : unlocked ? "Chọn nhân vật" : "Mở khóa · " + character.price + " coin",
                character.thumbnail, character.id == SelectedCharacterId, () => SelectCharacter(choice));
        }
    }

    public void SelectCharacter(StudyRoomCatalog.Character character)
    {
        if (!Progress.CharacterUnlocked(character.id)) { AskUnlock(character.id, character.displayName, character.price, true); return; }
        SelectedCharacterId = character.id;
        ShowCharacterChoices();
    }

    public void CreateSelectedRoom()
    {
        if (loadingRoom) return;
        var room = catalog.FindRoom(SelectedRoomId);
        if (room == null || !room.canCreate || !Progress.RoomUnlocked(SelectedRoomId) ||
            !Progress.CharacterUnlocked(SelectedCharacterId))
        {
            message.text = "Hãy chọn phòng và nhân vật đã mở khóa.";
            return;
        }
        roomNaming.SetActive(true);
        roomNameInput.Select();
        roomNameInput.ActivateInputField();
    }

    private void ConfirmCreateRoom()
    {
        if (loadingRoom) return;
        var saved = Progress.CreateRoom(SelectedRoomId, SelectedCharacterId, roomNameInput.text);
        if (saved == null) { message.text = "Hãy chọn phòng và nhân vật đã mở khóa."; return; }
        roomNaming.SetActive(false);
        EnterRoom(saved);
    }

    public void AddCoins(int amount)
    {
        Progress.AddCoins(amount);
        RefreshWallet();
    }

    private void AskUnlock(string id, string name, int price, bool character)
    {
        confirmation.SetActive(true);
        confirmation.transform.Find("Card/Title").GetComponent<Text>().text = "Mở khóa " + name + "?";
        bool enough = Progress.Coins >= price;
        confirmation.transform.Find("Card/Description").GetComponent<Text>().text = enough
            ? price + " coin · Mở một lần, dùng mãi về sau"
            : "Cần " + price + " coin · Bạn đang có " + Progress.Coins + " coin";
        var buy = confirmation.transform.Find("Card/Buy").GetComponent<Button>();
        buy.interactable = enough;
        buy.onClick.RemoveAllListeners();
        buy.GetComponent<StudyRoomButtonSound>().Bind();
        buy.onClick.AddListener(() =>
        {
            bool purchased = character ? Progress.TryUnlockCharacter(id) : Progress.TryUnlockRoom(id);
            if (!purchased) return;
            confirmation.SetActive(false);
            if (character) { SelectedCharacterId = id; ShowCharacterChoices(); }
            else { SelectedRoomId = id; ShowRoomChoices(); }
            RefreshWallet();
        });
    }

    private void EnterRoom(StudyRoomProgress.SavedRoom saved)
    {
        var room = catalog.FindRoom(saved.roomId);
        var character = catalog.FindCharacter(saved.characterId);
        if (room == null || character == null || !room.prefab || !character.prefab) return;
        if (loadingRoom) return;
        loadingRoom = true;
        message.text = "Đang mở phòng…";
        if (!StudyRoomSession.OpenRoom(saved))
        {
            loadingRoom = false;
            message.text = "Không mở được scene phòng.";
        }
    }

    private void BeginPicker(string title, string description)
    {
        roomNaming.SetActive(false);
        start.SetActive(false); popup.SetActive(false); picker.SetActive(true);
        previewRig.SetActive(false);
        heading.text = title; helper.text = description; message.text = "";
        foreach (Transform child in listContent) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        RefreshWallet();
        scroll.verticalNormalizedPosition = 1;
    }

    private void RefreshWallet() { if (wallet && Progress != null) wallet.text = Progress.Coins + " coin"; }

    private void Back()
    {
        if (roomNaming.activeSelf) { roomNaming.SetActive(false); return; }
        if (confirmation.activeSelf) { confirmation.SetActive(false); return; }
        if (picker.activeSelf && choosingCharacters) { ShowRoomChoices(); return; }
        ShowStartScreen(); ShowRoomPopup();
    }

    private void BuildInterface()
    {
        var root = Rect(transform.Find("Canvas"), "ScreenStart", Vector2.zero, Vector2.zero);
        Stretch(root);
        screen = root.gameObject;
        var canvas = screen.AddComponent<Canvas>(); canvas.overrideSorting = true; canvas.sortingOrder = 200;
        screen.AddComponent<GraphicRaycaster>();
        Image(root, new Color32(213, 204, 231, 255)).sprite = null;
        start = Rect(root, "Start", Vector2.zero, Vector2.zero).gameObject; Stretch((RectTransform)start.transform);
        var startRect = (RectTransform)start.transform;
        var press = start.AddComponent<Button>();
        start.AddComponent<StudyRoomButtonSound>();
        var hit = start.AddComponent<Image>(); hit.color = Color.clear; press.targetGraphic = hit;
        press.onClick.AddListener(ShowRoomPopup);
        var titleBar = Rect(startRect, "TitleBar", new Vector2(480, 90), Vector2.zero);
        titleBar.anchorMin = titleBar.anchorMax = new Vector2(0.5f, 0.88f);
        Image(titleBar, cream).raycastTarget = false;
        Label(titleBar, "Title", "StudyRoom", 46, new Vector2(440, 80), Vector2.zero, true);
        var subtitle = Label(startRect, "Subtitle", "Một góc nhỏ, một ngày thật dịu", 19, new Vector2(540, 44), Vector2.zero);
        subtitle.rectTransform.anchorMin = subtitle.rectTransform.anchorMax = new Vector2(0.5f, 0.81f);
        var imageArea = Rect(startRect, "CharacterArea", Vector2.zero, Vector2.zero);
        imageArea.anchorMin = new Vector2(0.2f, 0.24f); imageArea.anchorMax = new Vector2(0.8f, 0.76f);
        imageArea.offsetMin = imageArea.offsetMax = Vector2.zero;
        var imageRect = Rect(imageArea, "IdleCharacter", Vector2.zero, Vector2.zero);
        Stretch(imageRect);
        characterImage = imageRect.gameObject.AddComponent<RawImage>(); characterImage.raycastTarget = false;
        var ratio = imageRect.gameObject.AddComponent<AspectRatioFitter>(); ratio.aspectRatio = 0.75f; ratio.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        var prompt = Label(startRect, "StartPrompt", "Ấn để bắt đầu", 25, new Vector2(440, 58), Vector2.zero, true);
        prompt.rectTransform.anchorMin = prompt.rectTransform.anchorMax = new Vector2(0.5f, 0.27f);
        var hint = Label(startRect, "StartHint", "Góc học của bạn đang chờ", 16, new Vector2(440, 40), Vector2.zero);
        hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = new Vector2(0.5f, 0.225f);

        popup = Modal(root, "RoomPopup", new Vector2(530, 390));
        var popupCard = popup.transform.Find("Card");
        Label(popupCard, "Title", "Bắt đầu buổi học", 28, new Vector2(490, 64), new Vector2(0, 126), true);
        Label(popupCard, "Subtitle", "Trở về phòng cũ hoặc tạo một góc mới", 16, new Vector2(490, 44), new Vector2(0, 78));
        Button(popupCard, "SavedRooms", "Chọn phòng đã tạo", new Vector2(440, 60), new Vector2(0, 6), pink, ShowSavedRooms);
        Button(popupCard, "NewRoom", "Tạo phòng mới", new Vector2(440, 60), new Vector2(0, -69), lavender, BeginCreateRoom);
        Button(popupCard, "Close", "Quay lại", new Vector2(440, 42), new Vector2(0, -141), cream, () => popup.SetActive(false));

        var pickerRect = Rect(root, "Picker", Vector2.zero, Vector2.zero); Stretch(pickerRect); picker = pickerRect.gameObject;
        Image(pickerRect, new Color32(241, 232, 245, 255)).sprite = null;
        heading = Label(pickerRect, "Heading", "Chọn phòng", 32, new Vector2(520, 70), Vector2.zero, true);
        heading.rectTransform.anchorMin = heading.rectTransform.anchorMax = new Vector2(0.5f, 0.90f);
        helper = Label(pickerRect, "Step", "", 16, new Vector2(600, 50), Vector2.zero);
        helper.rectTransform.anchorMin = helper.rectTransform.anchorMax = new Vector2(0.5f, 0.84f);
        wallet = Label(pickerRect, "Coins", "0 coin", 18, new Vector2(160, 42), Vector2.zero, true);
        wallet.rectTransform.anchorMin = wallet.rectTransform.anchorMax = new Vector2(1, 1);
        wallet.rectTransform.pivot = new Vector2(1, 1); wallet.rectTransform.anchoredPosition = new Vector2(-26, -18);
        var back = Button(pickerRect, "Back", "‹ Quay lại", new Vector2(146, 44), Vector2.zero, lavender, Back);
        var backRect = back.GetComponent<RectTransform>(); backRect.anchorMin = backRect.anchorMax = backRect.pivot = new Vector2(0, 1); backRect.anchoredPosition = new Vector2(26, -18);
        var viewport = Rect(pickerRect, "RoomList", Vector2.zero, Vector2.zero);
        viewport.anchorMin = new Vector2(0.08f, 0.21f); viewport.anchorMax = new Vector2(0.92f, 0.79f); viewport.offsetMin = viewport.offsetMax = Vector2.zero;
        Image(viewport, Color.clear); viewport.gameObject.AddComponent<RectMask2D>();
        scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.viewport = viewport;
        listContent = Rect(viewport, "Content", Vector2.zero, Vector2.zero);
        listContent.anchorMin = new Vector2(0, 1); listContent.anchorMax = Vector2.one; listContent.pivot = new Vector2(0.5f, 1);
        listContent.sizeDelta = Vector2.zero;
        var layout = listContent.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(4, 4, 6, 6); layout.spacing = 18; layout.childControlWidth = true; layout.childControlHeight = true; layout.childForceExpandHeight = false;
        var fitter = listContent.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = listContent;
        next = Button(pickerRect, "Next", "Tiếp theo · Chọn nhân vật", new Vector2(530, 60), Vector2.zero, pink, () => { if (choosingCharacters) CreateSelectedRoom(); else ShowCharacterChoices(); });
        next.GetComponent<RectTransform>().anchorMin = next.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.13f);
        message = Label(pickerRect, "Message", "", 15, new Vector2(590, 44), Vector2.zero);
        message.rectTransform.anchorMin = message.rectTransform.anchorMax = new Vector2(0.5f, 0.065f);
        confirmation = Modal(root, "UnlockConfirmation", new Vector2(530, 300));
        var confirmCard = confirmation.transform.Find("Card");
        Label(confirmCard, "Title", "Mở khóa phòng?", 25, new Vector2(490, 82), new Vector2(0, 86), true);
        Label(confirmCard, "Description", "", 17, new Vector2(460, 60), new Vector2(0, 16));
        Button(confirmCard, "Buy", "Mở khóa", new Vector2(214, 54), new Vector2(116, -84), pink, () => { });
        Button(confirmCard, "Cancel", "Để sau", new Vector2(214, 54), new Vector2(-116, -84), lavender, () => confirmation.SetActive(false));
        confirmation.SetActive(false);

        roomNaming = Modal(root, "RoomNaming", new Vector2(530, 360));
        var namingCard = roomNaming.transform.Find("Card");
        Label(namingCard, "Title", "Đặt tên phòng học", 27, new Vector2(480, 60), new Vector2(0, 126), true);
        Label(namingCard, "Hint", "Để trống để dùng tên mặc định", 17, new Vector2(460, 44), new Vector2(0, 74));
        var inputRect = Rect(namingCard, "RoomName", new Vector2(440, 56), new Vector2(0, 8));
        var inputBackground = Image(inputRect, Color.white);
        roomNameInput = inputRect.gameObject.AddComponent<InputField>();
        roomNameInput.targetGraphic = inputBackground;
        roomNameInput.lineType = InputField.LineType.SingleLine;
        roomNameInput.characterLimit = 40;
        var inputText = Label(inputRect, "Text", "", 20, Vector2.zero, Vector2.zero);
        Stretch(inputText.rectTransform);
        inputText.rectTransform.offsetMin = new Vector2(14, 5);
        inputText.rectTransform.offsetMax = new Vector2(-14, -5);
        inputText.alignment = TextAnchor.MiddleLeft;
        inputText.supportRichText = false;
        var placeholder = Label(inputRect, "Placeholder", "Tên phòng của bạn…", 18, Vector2.zero, Vector2.zero);
        Stretch(placeholder.rectTransform);
        placeholder.rectTransform.offsetMin = new Vector2(14, 5);
        placeholder.rectTransform.offsetMax = new Vector2(-14, -5);
        placeholder.alignment = TextAnchor.MiddleLeft;
        placeholder.color = new Color32(155, 139, 164, 255);
        roomNameInput.textComponent = inputText;
        roomNameInput.placeholder = placeholder;
        Button(namingCard, "Create", "Tạo phòng & bắt đầu", new Vector2(250, 54), new Vector2(100, -112), pink, ConfirmCreateRoom);
        Button(namingCard, "Cancel", "Quay lại", new Vector2(166, 54), new Vector2(-124, -112), lavender, () => roomNaming.SetActive(false));
        roomNaming.SetActive(false);
    }

    private void Card(string title, string description, string action, Texture2D thumbnail, bool selected, UnityEngine.Events.UnityAction click)
    {
        var rect = Rect(listContent, "ChoiceCard", new Vector2(560, 190), Vector2.zero);
        Image(rect, selected ? new Color32(251, 221, 233, 255) : cream);
        var layout = rect.gameObject.AddComponent<LayoutElement>(); layout.preferredHeight = 190;
        var button = rect.gameObject.AddComponent<Button>(); button.onClick.AddListener(click);
        rect.gameObject.AddComponent<StudyRoomButtonSound>();
        var picture = Rect(rect, "Thumbnail", new Vector2(165, 165), new Vector2(20, 0));
        picture.anchorMin = picture.anchorMax = new Vector2(0, 0.5f); picture.pivot = new Vector2(0, 0.5f);
        var raw = picture.gameObject.AddComponent<RawImage>(); raw.texture = thumbnail; raw.raycastTarget = false;
        var titleLabel = Label(rect, "Name", title, 23, Vector2.zero, Vector2.zero, true);
        titleLabel.supportRichText = false;
        PlaceCardText(titleLabel.rectTransform, -18, -75); titleLabel.alignment = TextAnchor.MiddleLeft;
        var detail = Label(rect, "Detail", description, 15, Vector2.zero, Vector2.zero);
        PlaceCardText(detail.rectTransform, -77, -130); detail.alignment = TextAnchor.MiddleLeft;
        var actionLabel = Label(rect, "Action", action, 17, Vector2.zero, Vector2.zero, true);
        PlaceCardText(actionLabel.rectTransform, -134, -178); actionLabel.alignment = TextAnchor.MiddleLeft;
        actionLabel.color = new Color32(155, 75, 122, 255);
    }

    private static void PlaceCardText(RectTransform rect, float top, float bottom)
    {
        rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(204, bottom); rect.offsetMax = new Vector2(-18, top);
    }

    private GameObject Modal(Transform parent, string name, Vector2 size)
    {
        var root = Rect(parent, name, Vector2.zero, Vector2.zero); Stretch(root);
        Image(root, new Color(0.20f, 0.12f, 0.28f, 0.5f));
        var card = Rect(root, "Card", size, Vector2.zero); Image(card, cream);
        var shadow = card.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0.3f, 0.18f, 0.4f, 0.2f); shadow.effectDistance = new Vector2(0, -8);
        return root.gameObject;
    }

    private RectTransform Rect(Transform parent, string name, Vector2 size, Vector2 position)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f; rect.sizeDelta = size; rect.anchoredPosition = position;
        return rect;
    }
    private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    private Image Image(RectTransform rect, Color color)
    {
        var image = rect.gameObject.AddComponent<Image>(); image.sprite = roundedSprite; image.type = UnityEngine.UI.Image.Type.Sliced; image.color = color; return image;
    }
    private Text Label(Transform parent, string name, string value, int size, Vector2 dimensions, Vector2 position, bool bold = false)
    {
        var text = Rect(parent, name, dimensions, position).gameObject.AddComponent<Text>(); text.font = bold ? titleFont : regularFont;
        text.text = value; text.fontSize = size; text.color = ink; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false; text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }
    private Button Button(Transform parent, string name, string label, Vector2 size, Vector2 position, Color color, UnityEngine.Events.UnityAction action)
    {
        var rect = Rect(parent, name, size, position); var image = Image(rect, color); var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        rect.gameObject.AddComponent<StudyRoomButtonSound>();
        var colors = button.colors; colors.highlightedColor = new Color(1, 0.94f, 0.98f); colors.pressedColor = new Color(0.88f, 0.8f, 0.93f); button.colors = colors;
        button.onClick.AddListener(action);
        var text = Label(rect, "Label", label, 18, size, Vector2.zero, true); Stretch(text.rectTransform);
        return button;
    }

    private void BuildCharacterPreview(GameObject prefab)
    {
        previewRig = new GameObject("StartScreenCharacterPreview"); previewRig.SetActive(false);
        previewRig.transform.SetParent(transform, false);
        previewRig.transform.position = new Vector3(10000, 10000, 10000);
        var character = Instantiate(prefab, previewRig.transform);
        character.transform.localPosition = Vector3.zero; character.transform.localRotation = Quaternion.identity;
        foreach (var behaviour in character.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
        foreach (var collider in character.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (var t in character.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
        var cameraObject = new GameObject("PreviewCamera"); cameraObject.transform.SetParent(previewRig.transform, false);
        previewCamera = cameraObject.AddComponent<Camera>(); previewCamera.orthographic = true; previewCamera.orthographicSize = 1.05f;
        previewCamera.clearFlags = CameraClearFlags.SolidColor; previewCamera.backgroundColor = Color.clear; previewCamera.cullingMask = 1 << 31;
        previewCamera.nearClipPlane = 0.1f; previewCamera.farClipPlane = 10;
        previewCamera.transform.localPosition = new Vector3(0, 0.94f, -4); previewCamera.transform.localRotation = Quaternion.identity;
        previewTexture = new RenderTexture(768, 1024, 24, RenderTextureFormat.ARGB32); previewTexture.Create();
        previewCamera.targetTexture = previewTexture; characterImage.texture = previewTexture;
        var lightObject = new GameObject("PreviewLight"); lightObject.transform.SetParent(previewRig.transform, false);
        var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.5f; light.cullingMask = 1 << 31; light.shadows = LightShadows.None;
        light.transform.localRotation = Quaternion.Euler(30, -25, 0);
        previewRig.SetActive(true);
    }

    private void OnDestroy()
    {
        if (economy) economy.StateChanged -= RefreshWallet;
        if (!Application.isPlaying) return;
        if (previewRig) Destroy(previewRig);
        if (previewTexture) { previewTexture.Release(); Destroy(previewTexture); }
    }
}
