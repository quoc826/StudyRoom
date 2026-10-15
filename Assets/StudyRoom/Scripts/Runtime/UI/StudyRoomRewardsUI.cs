using System;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Wallet HUD, study timer and the five daily claimable quests.</summary>
public class StudyRoomRewardsUI : MonoBehaviour
{
    private StudyRoomEconomy economy;
    private GameObject modal;
    private Text wallet, countdown, studyStatus, notice, summary, date;
    private Button openButton, closeButton, studyButton;
    private Text studyLabel;
    private readonly Text[] progressLabels = new Text[5];
    private readonly Text[] claimLabels = new Text[5];
    private readonly Image[] fills = new Image[5];
    private readonly Button[] claims = new Button[5];
    private readonly UnityEngine.Events.UnityAction[] claimActions = new UnityEngine.Events.UnityAction[5];
    private PlayerController player;
    private bool restorePlayer;
    private float noticeUntil;
    private Font font;
    private Sprite rounded;
    private static readonly Color Ink = new Color32(79, 57, 98, 255);
    private static readonly Color Cream = new Color32(255, 247, 252, 255);
    private static readonly Color Pink = new Color32(245, 173, 198, 255);
    private static readonly Color Pale = new Color32(232, 220, 248, 255);
    public bool IsOpen => modal && modal.activeSelf;

    private void Start()
    {
        if (GetComponent<StudyRoomStartScreen>()) { BuildInterface(); return; }
        BuildInterface();
        var start = GetComponent<StudyRoomStartScreen>();
        var loader = GetComponent<StudyRoomRoomLoader>();
        var catalog = start ? start.catalog : loader ? loader.catalog : null;
        if (!catalog) { Debug.LogError("Rewards UI needs a room catalog.", this); return; }
        economy = StudyRoomEconomy.Ensure(catalog);
        economy.StateChanged += Refresh;
        economy.CoinsEarned += ShowEarned;
        openButton.onClick.AddListener(Open);
        closeButton.onClick.AddListener(Close);
        studyButton.onClick.AddListener(ToggleStudy);
        for (int i = 0; i < claims.Length; i++)
        {
            int index = i;
            claimActions[i] = () => economy.ClaimQuest(index);
            claims[i].onClick.AddListener(claimActions[i]);
        }
        modal.SetActive(false);
        Refresh();
    }

    private void Update()
    {
        if (notice && Time.unscaledTime > noticeUntil) notice.text = "";
        // Room Escape is routed by StudyRoomMenu to avoid opening both overlays.
        if (!IsOpen || GetComponent<StudyRoomMenu>()) return;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Escape)) Close();
#endif
    }

    public void Open()
    {
        if (!modal || IsOpen || GetComponent<StudyRoomStartScreen>()) return;
        var furniture = GetComponent<StudyRoomDecorations>(); if (furniture && furniture.IsEditing) furniture.EndEditing();
        var computer = GetComponent<StudyRoomComputerUI>();
        if (computer && computer.IsOpen) computer.Close();
        var shop = GetComponent<StudyRoomShopUI>();
        if (shop && shop.IsPlacing) shop.CancelPlacement();
        if (shop && shop.IsOpen) shop.Close();
        player = FindAnyObjectByType<PlayerController>();
        restorePlayer = player && player.enabled;
        if (player) player.enabled = false;
        modal.SetActive(true);
        if (shop) shop.SetShortcutsVisible(false);
        Refresh();
    }

    public void Close()
    {
        if (modal) modal.SetActive(false);
        var shop = GetComponent<StudyRoomShopUI>();
        if (shop) shop.SetShortcutsVisible(true);
        if (player && restorePlayer) player.enabled = true;
        restorePlayer = false;
    }

    private void ToggleStudy() { economy.ToggleStudy(); Refresh(); }

    private void ShowEarned(int amount)
    {
        notice.text = "+" + amount + " coin đã vào ví!";
        noticeUntil = Time.unscaledTime + 5;
        Refresh();
    }

    private void Refresh()
    {
        if (!economy || economy.Progress == null || !wallet) return;
        var progress = economy.Progress;
        wallet.text = progress.Coins + " coin";
        countdown.text = "+5 coin sau " + Duration(progress.NextCoinSeconds);
        studyStatus.text = economy.IsStudying ? "Đang học · " + Duration(progress.DailyStudySeconds) : "15 phút trong game = +5 coin";
        date.text = "NGÀY " + progress.QuestDate + " · LÀM MỚI LÚC 00:00";
        summary.text = "Trong game: " + Duration(progress.DailyOnlineSeconds) + "    •    Đã học: " + Duration(progress.DailyStudySeconds);
        studyButton.interactable = economy.CanStudy;
        studyLabel.text = !economy.CanStudy ? "Vào phòng để bắt đầu học" : economy.IsStudying ? "Tạm dừng học" : "Bắt đầu học";
        for (int i = 0; i < StudyRoomDailyQuests.All.Count; i++)
        {
            var quest = StudyRoomDailyQuests.All[i];
            double amount = progress.QuestProgress(i);
            bool complete = amount >= quest.TargetSeconds;
            bool claimed = progress.QuestClaimed(i);
            progressLabels[i].text = quest.Type == StudyRoomDailyQuests.Goal.Visit ? "Đã tham gia hôm nay" :
                Duration(amount) + " / " + Duration(quest.TargetSeconds);
            fills[i].fillAmount = (float)(amount / quest.TargetSeconds);
            claims[i].interactable = complete && !claimed;
            claimLabels[i].text = claimed ? "Đã nhận" : complete ? "Nhận +" + quest.Coins : "+" + quest.Coins + " coin";
        }
    }

    private static string Duration(double seconds)
    {
        int total = (int)Math.Min(int.MaxValue, Math.Ceiling(Math.Max(0, seconds)));
        return total >= 3600 ? (total / 3600).ToString("00") + ":" + ((total / 60) % 60).ToString("00") + ":" + (total % 60).ToString("00") :
            (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
    }

    public void BuildInterface()
    {
        var canvas = transform.Find("Canvas");
        if (!canvas) return;
        if (GetComponent<StudyRoomStartScreen>())
        {
            var roomHud = canvas.Find("RewardsUI");
            if (roomHud) roomHud.gameObject.SetActive(false);
            return;
        }
        var reference = canvas.GetComponentInChildren<Text>(true);
        font = reference && reference.font ? reference.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var start = GetComponent<StudyRoomStartScreen>();
        if (start && start.regularFont) font = start.regularFont;
        if (start) rounded = start.roundedSprite;
        var menu = canvas.Find("MenuButton");
        if (menu && menu.GetComponent<Image>()) rounded = menu.GetComponent<Image>().sprite;
        var existing = canvas.Find("RewardsUI");
        if (!existing)
        {
            var root = Rect(canvas, "RewardsUI", Vector2.zero, Vector2.zero);
            Stretch(root);
            var hudCanvas = root.gameObject.AddComponent<Canvas>();
            hudCanvas.overrideSorting = true; hudCanvas.sortingOrder = 210;
            root.gameObject.AddComponent<GraphicRaycaster>();
            var hud = Rect(root, "WalletHUD", new Vector2(410, 100), new Vector2(28, 28));
            hud.anchorMin = hud.anchorMax = hud.pivot = Vector2.zero;
            Panel(hud, Cream);
            Label(hud, "Coins", "0 coin", 25, new Vector2(176, 38), new Vector2(18, 28));
            Label(hud, "Countdown", "+5 coin sau 15:00", 15, new Vector2(245, 27), new Vector2(18, 1));
            Label(hud, "StudyStatus", "15 phút trong game = +5 coin", 13, new Vector2(375, 24), new Vector2(18, -26));
            MakeButton(hud, "Quests", "Nhiệm vụ", new Vector2(140, 42), new Vector2(254, 28), Pink);
            var noticeLabel = Label(root, "RewardNotice", "", 19, new Vector2(410, 42), Vector2.zero);
            noticeLabel.rectTransform.anchorMin = noticeLabel.rectTransform.anchorMax = noticeLabel.rectTransform.pivot = Vector2.zero;
            noticeLabel.rectTransform.anchoredPosition = new Vector2(28, 134);
            noticeLabel.alignment = TextAnchor.MiddleCenter;

            var overlay = Rect(root, "DailyQuests", Vector2.zero, Vector2.zero); Stretch(overlay);
            Panel(overlay, new Color(0.15f, 0.10f, 0.22f, 0.64f)).sprite = null;
            // Stretch with margins so the list remains usable in portrait and small windows.
            var card = Rect(overlay, "Card", Vector2.zero, Vector2.zero);
            card.anchorMin = new Vector2(0.08f, 0.06f); card.anchorMax = new Vector2(0.92f, 0.94f);
            card.offsetMin = card.offsetMax = Vector2.zero;
            Panel(card, Cream);
            var title = Label(card, "Title", "Nhiệm vụ hằng ngày", 28, new Vector2(480, 45), new Vector2(24, -18));
            TopLeft(title.rectTransform, new Vector2(24, -18));
            var day = Label(card, "Date", "5 NHIỆM VỤ MỖI NGÀY", 12, new Vector2(480, 26), new Vector2(24, -62));
            TopLeft(day.rectTransform, new Vector2(24, -62));
            var close = MakeButton(card, "Close", "Đóng", new Vector2(76, 40), Vector2.zero, Pale);
            var closeRect = close.GetComponent<RectTransform>(); closeRect.anchorMin = closeRect.anchorMax = closeRect.pivot = Vector2.one;
            closeRect.anchoredPosition = new Vector2(-18, -18);
            var totals = Label(card, "Summary", "Trong game: 00:00    •    Đã học: 00:00", 16, new Vector2(650, 30), Vector2.zero);
            TopLeft(totals.rectTransform, new Vector2(24, -94));
            var study = MakeButton(card, "Study", "Bắt đầu học", new Vector2(246, 42), Vector2.zero, Pink);
            TopLeft(study.GetComponent<RectTransform>(), new Vector2(24, -130));
            var hint = Label(card, "Hint", "Bấm bắt đầu học để tính thời gian. Có thể tạm dừng và tiếp tục.", 13, new Vector2(740, 32), Vector2.zero);
            TopLeft(hint.rectTransform, new Vector2(24, -178));
            hint.rectTransform.anchorMax = Vector2.one;
            hint.rectTransform.offsetMin = new Vector2(24, -213);
            hint.rectTransform.offsetMax = new Vector2(-24, -178);

            var viewport = Rect(card, "QuestList", Vector2.zero, Vector2.zero);
            Stretch(viewport); viewport.offsetMin = new Vector2(20, 52); viewport.offsetMax = new Vector2(-20, -219);
            Panel(viewport, Color.clear).sprite = null; viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.viewport = viewport;
            var content = Rect(viewport, "Content", Vector2.zero, Vector2.zero);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(0.5f, 1);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8; layout.padding = new RectOffset(4, 4, 4, 4);
            layout.childControlHeight = layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            for (int i = 0; i < StudyRoomDailyQuests.All.Count; i++) CreateQuestRow(content, i);
            var footer = Label(card, "Footer", "Tích lũy trong ngày · Mỗi nhiệm vụ nhận thưởng 1 lần · Coin tự lưu", 13, new Vector2(790, 32), Vector2.zero);
            footer.rectTransform.anchorMin = footer.rectTransform.anchorMax = footer.rectTransform.pivot = new Vector2(0, 0);
            footer.rectTransform.anchoredPosition = new Vector2(24, 12);
            footer.rectTransform.anchorMax = new Vector2(1, 0);
            footer.rectTransform.offsetMin = new Vector2(24, 10);
            footer.rectTransform.offsetMax = new Vector2(-24, 46);
            overlay.gameObject.SetActive(false);
        }
        Cache(canvas.Find("RewardsUI"));
    }

    private void CreateQuestRow(Transform parent, int index)
    {
        var quest = StudyRoomDailyQuests.All[index];
        var row = Rect(parent, "Quest" + index, new Vector2(540, 64), Vector2.zero);
        Panel(row, Pale);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 64;
        var title = Label(row, "Title", quest.Title, 19, new Vector2(350, 34), Vector2.zero);
        title.rectTransform.anchorMin = new Vector2(0, 1); title.rectTransform.anchorMax = Vector2.one;
        title.rectTransform.pivot = new Vector2(0, 1); title.rectTransform.offsetMin = new Vector2(16, -30); title.rectTransform.offsetMax = new Vector2(-164, -2);
        var amount = Label(row, "Progress", index == 0 ? "Đã tham gia hôm nay" : "00:00 / " + Duration(quest.TargetSeconds), 13, new Vector2(320, 22), Vector2.zero);
        TopLeft(amount.rectTransform, new Vector2(16, -29));
        var track = Rect(row, "Track", Vector2.zero, Vector2.zero);
        track.anchorMin = new Vector2(0, 0); track.anchorMax = new Vector2(1, 0);
        track.offsetMin = new Vector2(16, 7); track.offsetMax = new Vector2(-168, 13);
        Panel(track, new Color32(210, 191, 225, 255));
        var fill = Rect(track, "Fill", Vector2.zero, Vector2.zero); Stretch(fill);
        var image = Panel(fill, new Color32(176, 135, 189, 255));
        image.type = Image.Type.Filled; image.fillMethod = Image.FillMethod.Horizontal; image.fillAmount = 0;
        var claim = MakeButton(row, "Claim", "+" + quest.Coins + " coin", new Vector2(130, 44), Vector2.zero, Pink);
        claim.interactable = false;
        var rect = claim.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 0.5f);
        rect.anchoredPosition = new Vector2(-16, 0);
    }

    private void Cache(Transform root)
    {
        var hudRect = root.Find("WalletHUD").GetComponent<RectTransform>();
        hudRect.anchorMin = hudRect.anchorMax = hudRect.pivot = new Vector2(0, 1);
        hudRect.anchoredPosition = new Vector2(28, -28);
        var noticeRect = root.Find("RewardNotice").GetComponent<RectTransform>();
        noticeRect.anchorMin = noticeRect.anchorMax = noticeRect.pivot = new Vector2(0, 1);
        noticeRect.anchoredPosition = new Vector2(28, -268);
        wallet = root.Find("WalletHUD/Coins").GetComponent<Text>();
        countdown = root.Find("WalletHUD/Countdown").GetComponent<Text>();
        studyStatus = root.Find("WalletHUD/StudyStatus").GetComponent<Text>();
        notice = root.Find("RewardNotice").GetComponent<Text>();
        openButton = root.Find("WalletHUD/Quests").GetComponent<Button>();
        modal = root.Find("DailyQuests").gameObject;
        // Keep the full-screen dialog above the room HUD and shop shortcuts.
        var modalCanvas = modal.GetComponent<Canvas>();
        if (!modalCanvas) modalCanvas = modal.AddComponent<Canvas>();
        modalCanvas.overrideSorting = true;
        modalCanvas.sortingOrder = 300;
        if (!modal.GetComponent<GraphicRaycaster>()) modal.AddComponent<GraphicRaycaster>();
        var card = modal.transform.Find("Card");
        closeButton = card.Find("Close").GetComponent<Button>();
        studyButton = card.Find("Study").GetComponent<Button>();
        studyLabel = studyButton.transform.Find("Label").GetComponent<Text>();
        summary = card.Find("Summary").GetComponent<Text>();
        date = card.Find("Date").GetComponent<Text>();
        for (int i = 0; i < claims.Length; i++)
        {
            var row = card.Find("QuestList/Content/Quest" + i);
            progressLabels[i] = row.Find("Progress").GetComponent<Text>();
            fills[i] = row.Find("Track/Fill").GetComponent<Image>();
            claims[i] = row.Find("Claim").GetComponent<Button>();
            claimLabels[i] = row.Find("Claim/Label").GetComponent<Text>();
        }
    }

    private static RectTransform Rect(Transform parent, string name, Vector2 size, Vector2 position)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size; rect.anchoredPosition = position;
        return rect;
    }

    private Image Panel(RectTransform rect, Color color)
    {
        var image = rect.gameObject.AddComponent<Image>(); image.sprite = rounded;
        image.type = Image.Type.Sliced; image.color = color; return image;
    }

    private Text Label(Transform parent, string name, string text, int size, Vector2 dimensions, Vector2 position)
    {
        var label = Rect(parent, name, dimensions, position).gameObject.AddComponent<Text>();
        label.font = font; label.fontSize = size; label.text = text; label.color = Ink;
        label.alignment = TextAnchor.MiddleLeft; label.raycastTarget = false;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.rectTransform.anchorMin = label.rectTransform.anchorMax = label.rectTransform.pivot = new Vector2(0, 0.5f);
        return label;
    }

    private Button MakeButton(Transform parent, string name, string caption, Vector2 size, Vector2 position, Color color)
    {
        var rect = Rect(parent, name, size, position);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 0.5f);
        var image = Panel(rect, color); var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        rect.gameObject.AddComponent<StudyRoomButtonSound>();
        var label = Label(rect, "Label", caption, 17, size, Vector2.zero);
        label.alignment = TextAnchor.MiddleCenter; Stretch(label.rectTransform);
        return button;
    }

    private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    private static void TopLeft(RectTransform rect, Vector2 position) { rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = position; }

    private void OnDestroy()
    {
        if (economy) { economy.StateChanged -= Refresh; economy.CoinsEarned -= ShowEarned; }
        if (openButton) openButton.onClick.RemoveListener(Open);
        if (closeButton) closeButton.onClick.RemoveListener(Close);
        if (studyButton) studyButton.onClick.RemoveListener(ToggleStudy);
        for (int i = 0; i < claims.Length; i++) if (claims[i] && claimActions[i] != null) claims[i].onClick.RemoveListener(claimActions[i]);
    }
}
