using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StudyRoomShopUI : MonoBehaviour
{
    public StudyRoomShopCatalog catalog;
    private StudyRoomEconomy economy;
    private StudyRoomDecorations decorations;
    private GameObject modal, placementBar;
    private RectTransform content;
    private ScrollRect scroll;
    private Text wallet, message, placementHint, title;
    private Button open, inventoryOpen, close, shopTab, inventoryTab, roomTab, confirm, rotate, cancel;
    private PlayerController player;
    private bool restorePlayer, showingRoom, showingInventory;
    private string placedSignature, inventorySignature;
    private Font font;
    private Sprite rounded;
    private readonly Dictionary<string, ItemView> views = new Dictionary<string, ItemView>();
    private static readonly Color Ink = new Color32(79, 57, 98, 255);
    private static readonly Color Cream = new Color32(255, 247, 252, 255);
    private static readonly Color Pink = new Color32(245, 173, 198, 255);
    private static readonly Color Pale = new Color32(232, 220, 248, 255);
    public bool IsOpen => modal && modal.activeSelf;
    public bool IsPlacing => decorations && decorations.IsPlacing;

    private class ItemView
    {
        public Text stock;
        public Button buy, place;
    }

    private void Start()
    {
        var loader = GetComponent<StudyRoomRoomLoader>();
        if (!catalog && loader) catalog = loader.shopCatalog;
        if (!catalog || !loader) { Debug.LogError("Shop needs its decoration and room catalogs.", this); return; }
        economy = StudyRoomEconomy.Ensure(loader.catalog);
        decorations = GetComponent<StudyRoomDecorations>();
        if (!decorations) decorations = gameObject.AddComponent<StudyRoomDecorations>();
        BuildInterface();
        economy.StateChanged += Refresh;
        decorations.PlacementFinished += PlacementFinished;
        open.onClick.AddListener(Open);
        inventoryOpen.onClick.AddListener(OpenInventory);
        close.onClick.AddListener(Close);
        shopTab.onClick.AddListener(ShowShop);
        inventoryTab.onClick.AddListener(ShowInventory);
        roomTab.onClick.AddListener(ShowRoom);
        confirm.onClick.AddListener(ConfirmPlacement);
        rotate.onClick.AddListener(RotatePlacement);
        cancel.onClick.AddListener(CancelPlacement);
        modal.SetActive(false); placementBar.SetActive(false);
        BindItemButtons();
        Refresh();
    }

    private void Update()
    {
        var rewards = GetComponent<StudyRoomRewardsUI>();
        SetShortcutsVisible(!(rewards && rewards.IsOpen));
        if (!IsPlacing || !placementBar) return;
        confirm.interactable = decorations.ValidPlacement;
        placementHint.text = decorations.ValidPlacement ? "Vị trí hợp lệ · Bấm Đặt để lưu vào phòng" : "Chọn chỗ trống trên sàn, bên trong phòng";
    }

    public void SetShortcutsVisible(bool visible)
    {
        var root = transform.Find("Canvas/ShopUI");
        if (!root) return;
        var shopButton = root.Find("ShopButton");
        var inventoryButton = root.Find("InventoryButton");
        if (shopButton && shopButton.gameObject.activeSelf != visible) shopButton.gameObject.SetActive(visible);
        if (inventoryButton && inventoryButton.gameObject.activeSelf != visible) inventoryButton.gameObject.SetActive(visible);
    }

    public void Open()
    {
        if (!modal || IsOpen) return;
        if (IsPlacing) CancelPlacement();
        var rewards = GetComponent<StudyRoomRewardsUI>();
        if (rewards && rewards.IsOpen) rewards.Close();
        player = FindAnyObjectByType<PlayerController>();
        restorePlayer = player && player.enabled;
        if (player) player.enabled = false;
        modal.SetActive(true);
        message.text = "Mua bằng coin · Đồ đã mua có thể đặt vào phòng";
        ShowShop();
    }

    public void Close()
    {
        if (modal) modal.SetActive(false);
        if (player && restorePlayer) player.enabled = true;
        restorePlayer = false;
    }

    public void OpenInventory()
    {
        if (!IsOpen) Open();
        if (IsOpen) ShowInventory();
    }

    private void ShowShop()
    {
        showingRoom = showingInventory = false;
        title.text = "Shop · Trang trí phòng";
        BuildItemRows(); BindItemButtons(); Refresh();
        scroll.verticalNormalizedPosition = 1;
    }

    private void ShowRoom()
    {
        showingInventory = false;
        showingRoom = true; placedSignature = null;
        title.text = "Đồ trong phòng";
        Refresh(); scroll.verticalNormalizedPosition = 1;
    }

    private void ShowInventory()
    {
        showingRoom = false; showingInventory = true; inventorySignature = null;
        title.text = "Đồ của bạn";
        message.text = "Vật phẩm đã mua · Xem số lượng trong kho và chọn Đặt";
        Refresh(); scroll.verticalNormalizedPosition = 1;
    }

    private void Buy(string id)
    {
        var item = catalog.Find(id);
        if (economy.Progress.TryBuyDecoration(catalog, id)) message.text = "Đã mua " + item.displayName + " · Chọn Đặt để trang trí";
        else message.text = "Chưa đủ coin để mua vật phẩm này.";
        Refresh();
    }

    private void Place(string id)
    {
        Close();
        if (!decorations.BeginPlacement(id)) { Open(); message.text = "Chưa thể đặt vật phẩm này trong phòng."; return; }
        placementBar.SetActive(true);
    }

    private void ConfirmPlacement() { if (decorations.ConfirmPlacement()) Refresh(); }
    private void RotatePlacement() => decorations.RotatePreview();
    public void CancelPlacement() { if (decorations) decorations.CancelPlacement(); if (placementBar) placementBar.SetActive(false); }
    private void PlacementFinished(bool placed) { placementBar.SetActive(false); Refresh(); }

    private void ReturnItem(string instanceId)
    {
        if (!decorations.ReturnToInventory(instanceId)) return;
        message.text = "Đã thu vật phẩm về kho. Bạn có thể đặt lại ở vị trí khác.";
        placedSignature = null;
        Refresh();
    }

    private void Refresh()
    {
        if (!economy || !wallet) return;
        wallet.text = economy.Progress.Coins + " coin";
        shopTab.GetComponent<Image>().color = showingRoom || showingInventory ? Pale : Pink;
        inventoryTab.GetComponent<Image>().color = showingInventory ? Pink : Pale;
        roomTab.GetComponent<Image>().color = showingRoom ? Pink : Pale;
        if (showingRoom)
        {
            string signature = "";
            foreach (var room in economy.Progress.SavedRooms)
                if (room.id == decorations.RoomId)
                    foreach (var placed in room.decorations) signature += placed.instanceId + ";";
            if (placedSignature != signature) { placedSignature = signature; BuildRoomRows(); }
            return;
        }
        if (showingInventory)
        {
            string signature = "";
            foreach (var item in catalog.items)
                signature += item.id + ":" + economy.Progress.DecorationCount(item.id) + ":" + PlacedCount(item.id) + ";";
            if (inventorySignature != signature)
            {
                float position = scroll.verticalNormalizedPosition;
                inventorySignature = signature;
                BuildInventoryRows(); BindItemButtons();
                scroll.verticalNormalizedPosition = position;
            }
        }
        foreach (var entry in views)
        {
            var item = catalog.Find(entry.Key);
            int stock = economy.Progress.DecorationCount(entry.Key);
            entry.Value.stock.text = "Trong kho: " + stock + (showingInventory ? " · Đã đặt: " + PlacedCount(entry.Key) : "");
            if (entry.Value.buy) entry.Value.buy.interactable = economy.Progress.Coins >= item.price;
            entry.Value.place.interactable = stock > 0;
        }
    }

    public void BuildInterface()
    {
        var canvas = transform.Find("Canvas");
        if (!canvas) return;
        if (!catalog) { var loader = GetComponent<StudyRoomRoomLoader>(); if (loader) catalog = loader.shopCatalog; }
        var reference = canvas.GetComponentInChildren<Text>(true);
        font = reference && reference.font ? reference.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var menu = canvas.Find("MenuButton");
        if (menu && menu.GetComponent<Image>()) rounded = menu.GetComponent<Image>().sprite;
        var root = canvas.Find("ShopUI");
        if (!root)
        {
            var rect = Rect(canvas, "ShopUI", Vector2.zero); Stretch(rect); root = rect;
            var layer = rect.gameObject.AddComponent<Canvas>(); layer.overrideSorting = true; layer.sortingOrder = 220;
            rect.gameObject.AddComponent<GraphicRaycaster>();
            var button = Button(root, "ShopButton", "Shop", new Vector2(144, 48), Pink);
            var buttonRect = button.GetComponent<RectTransform>();
            buttonRect.anchorMin = buttonRect.anchorMax = buttonRect.pivot = Vector2.one;
            buttonRect.anchoredPosition = new Vector2(-28, -96);
            var overlay = Rect(root, "ShopPanel", Vector2.zero); Stretch(overlay);
            Panel(overlay, new Color(0.15f, 0.10f, 0.22f, 0.64f)).sprite = null;
            var card = Rect(overlay, "Card", Vector2.zero);
            card.anchorMin = new Vector2(0.08f, 0.06f); card.anchorMax = new Vector2(0.92f, 0.94f);
            card.offsetMin = card.offsetMax = Vector2.zero; Panel(card, Cream);
            Label(card, "Title", "Shop · Trang trí phòng", 28, new Vector2(470, 46), new Vector2(24, -18));
            Label(card, "Wallet", "0 coin", 19, new Vector2(220, 32), new Vector2(24, -65));
            var shut = Button(card, "Close", "Đóng", new Vector2(76, 40), Pale);
            var shutRect = shut.GetComponent<RectTransform>(); shutRect.anchorMin = shutRect.anchorMax = shutRect.pivot = Vector2.one;
            shutRect.anchoredPosition = new Vector2(-18, -18);
            var hint = Label(card, "Message", "Mua bằng coin · Đồ đã mua có thể đặt vào phòng", 14, Vector2.zero, Vector2.zero);
            hint.rectTransform.anchorMin = new Vector2(0, 1); hint.rectTransform.anchorMax = Vector2.one;
            hint.rectTransform.offsetMin = new Vector2(24, -131); hint.rectTransform.offsetMax = new Vector2(-24, -97);
            var shop = Button(card, "ShopTab", "Cửa hàng", new Vector2(156, 40), Pink); TopLeft(shop.GetComponent<RectTransform>(), new Vector2(24, -138));
            var room = Button(card, "RoomTab", "Đồ trong phòng", new Vector2(172, 40), Pale); TopLeft(room.GetComponent<RectTransform>(), new Vector2(194, -138));
            var viewport = Rect(card, "ItemList", Vector2.zero); Stretch(viewport);
            viewport.offsetMin = new Vector2(20, 52); viewport.offsetMax = new Vector2(-20, -192);
            Panel(viewport, Color.clear).sprite = null; viewport.gameObject.AddComponent<RectMask2D>();
            var scroller = viewport.gameObject.AddComponent<ScrollRect>(); scroller.horizontal = false;
            scroller.movementType = ScrollRect.MovementType.Clamped; scroller.viewport = viewport;
            var list = Rect(viewport, "Content", Vector2.zero);
            list.anchorMin = new Vector2(0, 1); list.anchorMax = Vector2.one; list.pivot = new Vector2(0.5f, 1);
            var layout = list.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = 12; layout.padding = new RectOffset(4, 4, 4, 4);
            layout.childControlWidth = layout.childControlHeight = true; layout.childForceExpandHeight = false;
            list.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroller.content = list;
            var footer = Label(card, "Footer", "Lăn chuột / vuốt để xem thêm · Thu về kho để đổi chỗ trang trí", 13, Vector2.zero, Vector2.zero);
            footer.rectTransform.anchorMin = Vector2.zero; footer.rectTransform.anchorMax = new Vector2(1, 0);
            footer.rectTransform.offsetMin = new Vector2(24, 12); footer.rectTransform.offsetMax = new Vector2(-24, 42);
            var placement = Rect(root, "PlacementBar", new Vector2(560, 96));
            placement.anchorMin = placement.anchorMax = placement.pivot = new Vector2(0.5f, 0);
            placement.anchoredPosition = new Vector2(0, 164); Panel(placement, Cream);
            Label(placement, "Hint", "Chọn vị trí trên sàn rồi bấm Đặt", 15, new Vector2(520, 32), new Vector2(18, -4));
            var rotation = Button(placement, "Rotate", "Xoay 90°", new Vector2(150, 38), Pale); TopLeft(rotation.GetComponent<RectTransform>(), new Vector2(18, -49));
            var accept = Button(placement, "Confirm", "Đặt", new Vector2(150, 38), Pink); TopLeft(accept.GetComponent<RectTransform>(), new Vector2(205, -49));
            var abort = Button(placement, "Cancel", "Hủy", new Vector2(150, 38), Pale); TopLeft(abort.GetComponent<RectTransform>(), new Vector2(392, -49));
            overlay.gameObject.SetActive(false); placement.gameObject.SetActive(false);
        }
        if (!root.Find("InventoryButton"))
        {
            var button = Button(root, "InventoryButton", "Đồ của bạn", new Vector2(144, 48), Pale);
            var rect = button.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-28, -156);
            button.transform.SetAsFirstSibling();
        }
        var cardPanel = root.Find("ShopPanel/Card");
        if (!cardPanel.Find("InventoryTab"))
        {
            var tab = Button(cardPanel, "InventoryTab", "Đồ của bạn", new Vector2(156, 40), Pale);
            TopLeft(tab.GetComponent<RectTransform>(), new Vector2(194, -138));
        }
        TopLeft(cardPanel.Find("RoomTab").GetComponent<RectTransform>(), new Vector2(364, -138));
        open = root.Find("ShopButton").GetComponent<Button>();
        inventoryOpen = root.Find("InventoryButton").GetComponent<Button>();
        modal = root.Find("ShopPanel").gameObject; placementBar = root.Find("PlacementBar").gameObject;
        var panel = modal.transform.Find("Card");
        close = panel.Find("Close").GetComponent<Button>(); shopTab = panel.Find("ShopTab").GetComponent<Button>(); roomTab = panel.Find("RoomTab").GetComponent<Button>();
        inventoryTab = panel.Find("InventoryTab").GetComponent<Button>();
        title = panel.Find("Title").GetComponent<Text>();
        wallet = panel.Find("Wallet").GetComponent<Text>(); message = panel.Find("Message").GetComponent<Text>();
        content = panel.Find("ItemList/Content").GetComponent<RectTransform>(); scroll = panel.Find("ItemList").GetComponent<ScrollRect>();
        confirm = placementBar.transform.Find("Confirm").GetComponent<Button>(); rotate = placementBar.transform.Find("Rotate").GetComponent<Button>(); cancel = placementBar.transform.Find("Cancel").GetComponent<Button>();
        placementHint = placementBar.transform.Find("Hint").GetComponent<Text>();
        BuildItemRows();
    }

    private void ClearRows()
    {
        views.Clear();
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            var child = content.GetChild(i).gameObject; child.SetActive(false);
            if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
        }
    }

    private void BuildItemRows()
    {
        ClearRows();
        if (!catalog) return;
        foreach (var item in catalog.items)
        {
            var row = ItemCard(item.id, item.displayName, item.description, item.thumbnail);
            var stock = Label(row, "Stock", "Trong kho: 0", 13, new Vector2(250, 24), new Vector2(122, -91));
            var buy = Button(row, "Buy", "Mua · " + item.price, new Vector2(142, 40), Pink);
            Right(buy.GetComponent<RectTransform>(), -12, -14);
            var place = Button(row, "Place", "Đặt vào phòng", new Vector2(142, 40), Pale);
            Right(place.GetComponent<RectTransform>(), -12, -65);
            buy.interactable = place.interactable = false;
            views[item.id] = new ItemView { stock = stock, buy = buy, place = place };
        }
    }

    private void BindItemButtons()
    {
        foreach (var entry in views)
        {
            string id = entry.Key;
            if (entry.Value.buy) entry.Value.buy.onClick.AddListener(() => Buy(id));
            entry.Value.place.onClick.AddListener(() => Place(id));
        }
    }

    private int PlacedCount(string itemId)
    {
        int count = 0;
        foreach (var room in economy.Progress.SavedRooms)
            foreach (var item in room.decorations) if (item.itemId == itemId) count++;
        return count;
    }

    private void BuildInventoryRows()
    {
        ClearRows();
        foreach (var item in catalog.items)
        {
            int stock = economy.Progress.DecorationCount(item.id);
            if (stock <= 0 && PlacedCount(item.id) <= 0) continue;
            var row = ItemCard(item.id, item.displayName, item.description, item.thumbnail);
            var label = Label(row, "Stock", "", 13, new Vector2(270, 24), new Vector2(122, -91));
            var place = Button(row, "Place", "Đặt vào phòng", new Vector2(142, 40), Pink);
            Right(place.GetComponent<RectTransform>(), -12, -38);
            views[item.id] = new ItemView { stock = label, place = place };
        }
        if (views.Count == 0)
        {
            var empty = Rect(content, "Empty", new Vector2(500, 110)); empty.gameObject.AddComponent<LayoutElement>().preferredHeight = 110;
            var label = Label(empty, "Hint", "Kho của bạn đang trống. Ghé Shop để mua đồ trang trí nhé!", 17, Vector2.zero, Vector2.zero);
            Stretch(label.rectTransform); label.alignment = TextAnchor.MiddleCenter;
        }
    }

    private void BuildRoomRows()
    {
        ClearRows();
        bool any = false;
        foreach (var room in economy.Progress.SavedRooms)
            if (room.id == decorations.RoomId)
                foreach (var placed in room.decorations)
                {
                    var item = catalog.Find(placed.itemId); if (item == null) continue;
                    any = true;
                    var row = ItemCard(placed.instanceId, item.displayName, "Đang trang trí phòng · Thu về kho để đặt lại", item.thumbnail);
                    string instanceId = placed.instanceId;
                    var button = Button(row, "Return", "Thu về kho", new Vector2(142, 40), Pink);
                    Right(button.GetComponent<RectTransform>(), -12, -38);
                    button.onClick.AddListener(() => ReturnItem(instanceId));
                }
        if (!any)
        {
            var empty = Rect(content, "Empty", new Vector2(500, 100)); empty.gameObject.AddComponent<LayoutElement>().preferredHeight = 100;
            var text = Label(empty, "Hint", "Chưa có đồ từ shop trong phòng. Mua và chọn Đặt để bắt đầu!", 17, Vector2.zero, Vector2.zero);
            Stretch(text.rectTransform); text.alignment = TextAnchor.MiddleCenter;
        }
    }

    private RectTransform ItemCard(string id, string title, string description, Texture2D thumbnail)
    {
        var row = Rect(content, id, new Vector2(540, 124)); Panel(row, Pale);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 124;
        var photo = Rect(row, "Thumbnail", new Vector2(96, 96)); TopLeft(photo, new Vector2(12, -14));
        var image = photo.gameObject.AddComponent<RawImage>(); image.texture = thumbnail; image.raycastTarget = false;
        var name = Label(row, "Title", title, 21, Vector2.zero, Vector2.zero);
        name.rectTransform.anchorMin = new Vector2(0, 1); name.rectTransform.anchorMax = Vector2.one;
        name.rectTransform.offsetMin = new Vector2(122, -44); name.rectTransform.offsetMax = new Vector2(-170, -8);
        var detail = Label(row, "Description", description, 13, Vector2.zero, Vector2.zero);
        detail.rectTransform.anchorMin = new Vector2(0, 1); detail.rectTransform.anchorMax = Vector2.one;
        detail.rectTransform.offsetMin = new Vector2(122, -84); detail.rectTransform.offsetMax = new Vector2(-170, -44);
        return row;
    }

    private static RectTransform Rect(Transform parent, string name, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f); rect.sizeDelta = size; return rect;
    }
    private Image Panel(RectTransform rect, Color color) { var image = rect.gameObject.AddComponent<Image>(); image.sprite = rounded; image.type = Image.Type.Sliced; image.color = color; return image; }
    private Text Label(Transform parent, string name, string text, int size, Vector2 dimensions, Vector2 position)
    {
        var label = Rect(parent, name, dimensions).gameObject.AddComponent<Text>();
        TopLeft(label.rectTransform, position); label.font = font; label.fontSize = size; label.text = text; label.color = Ink;
        label.alignment = TextAnchor.MiddleLeft; label.verticalOverflow = VerticalWrapMode.Overflow; label.raycastTarget = false; return label;
    }
    private Button Button(Transform parent, string name, string text, Vector2 size, Color color)
    {
        var rect = Rect(parent, name, size); var image = Panel(rect, color); var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        rect.gameObject.AddComponent<StudyRoomButtonSound>();
        var label = Label(rect, "Label", text, 16, size, Vector2.zero); Stretch(label.rectTransform); label.alignment = TextAnchor.MiddleCenter; return button;
    }
    private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    private static void TopLeft(RectTransform rect, Vector2 position) { rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = position; }
    private static void Right(RectTransform rect, float x, float y) { rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one; rect.anchoredPosition = new Vector2(x, y); }

    private void OnDestroy()
    {
        if (economy) economy.StateChanged -= Refresh;
        if (decorations) decorations.PlacementFinished -= PlacementFinished;
    }
}
