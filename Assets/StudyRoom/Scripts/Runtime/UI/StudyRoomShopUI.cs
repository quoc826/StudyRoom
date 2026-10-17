using System.Collections.Generic;
using System.Linq;
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
    private readonly Dictionary<string, Button> roomViews = new Dictionary<string, Button>();
    private Dropdown categoryDropdown;
    private Text pageLabel;
    private Button previousPage, nextPage;
    private int categoryFilter = -1, page, pageCount = 1;
    private const int PageSize = 12;
    private bool ViewingRoomCatalog => !showingRoom && !showingInventory && categoryFilter == (int)StudyRoomShopCatalog.Category.Rooms;
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
        var computer = GetComponent<StudyRoomComputerUI>();
        SetShortcutsVisible(!(rewards && rewards.IsOpen) && !(computer && computer.IsOpen));
        if (!IsPlacing || !placementBar) return;
        confirm.interactable = decorations.ValidPlacement;
        placementHint.text = decorations.ValidPlacement ? "Vị trí hợp lệ · Chuột phải hoặc bấm Đặt để xác nhận" : "Chọn chỗ trống trên sàn, bên trong phòng";
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
        if (decorations && decorations.IsEditing) decorations.EndEditing();
        var computer = GetComponent<StudyRoomComputerUI>();
        if (computer && computer.IsOpen) computer.Close();
        if (IsPlacing) CancelPlacement();
        var rewards = GetComponent<StudyRoomRewardsUI>();
        if (rewards && rewards.IsOpen) rewards.Close();
        player = FindAnyObjectByType<PlayerController>();
        restorePlayer = player && player.enabled;
        if (player) player.enabled = false;
        modal.SetActive(true);
        message.text = "Đồ đã mua có thể đặt vào phòng";
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
        page = 0;
        showingRoom = showingInventory = false;
        title.text = "Shop · Trang trí phòng";
        BuildItemRows(); BindItemButtons(); Refresh();
        scroll.verticalNormalizedPosition = 1;
    }

    private void ShowRoom()
    {
        ResetRoomCategory();
        page = 0;
        showingInventory = false;
        showingRoom = true; placedSignature = null;
        title.text = "Đồ trong phòng";
        Refresh(); scroll.verticalNormalizedPosition = 1;
    }

    private void ShowInventory()
    {
        ResetRoomCategory();
        page = 0;
        showingRoom = false; showingInventory = true; inventorySignature = null;
        title.text = "Đồ của bạn";
        message.text = "Mua một lần · Đặt không giới hạn trong mọi phòng";
        Refresh(); scroll.verticalNormalizedPosition = 1;
    }

    private void Buy(string id)
    {
        var item = catalog.Find(id);
        if (economy.Progress.TryBuyDecoration(catalog, id)) message.text = "Đã mua " + item.displayName + " · Chọn Đặt để trang trí";
        else message.text = "Chưa đủ kim cương để mua vật phẩm này.";
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
        wallet.text = economy.Progress.Coins.ToString();
        shopTab.GetComponent<Image>().color = showingRoom || showingInventory ? Pale : Pink;
        inventoryTab.GetComponent<Image>().color = showingInventory ? Pink : Pale;
        roomTab.GetComponent<Image>().color = showingRoom ? Pink : Pale;
        if (ViewingRoomCatalog)
        {
            foreach (var entry in roomViews)
            {
                var room = GetComponent<StudyRoomRoomLoader>().catalog.FindRoom(entry.Key);
                bool owned = economy.Progress.RoomUnlocked(entry.Key);
                entry.Value.interactable = !owned && economy.Progress.Coins >= room.price;
                StudyRoomCoinIcon.SetPurchaseLabel(entry.Value, room.price, owned);
            }
            return;
        }
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
            entry.Value.stock.text = economy.Progress.OwnedDecorationCount(entry.Key) == 0
                ? "Chưa sở hữu · Mua để sử dụng"
                : "Đã sở hữu · Đặt không giới hạn" + (showingInventory ? " · Đã đặt: " + PlacedCount(entry.Key) : "");
            if (entry.Value.buy) entry.Value.buy.interactable = stock <= 0 && economy.Progress.Coins >= item.price;
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
            Label(card, "Wallet", "0", 19, new Vector2(182, 32), new Vector2(62, -65));
            var shut = Button(card, "Close", "Đóng", new Vector2(76, 40), Pale);
            var shutRect = shut.GetComponent<RectTransform>(); shutRect.anchorMin = shutRect.anchorMax = shutRect.pivot = Vector2.one;
            shutRect.anchoredPosition = new Vector2(-18, -18);
            var hint = Label(card, "Message", "Đồ đã mua có thể đặt vào phòng", 14, Vector2.zero, Vector2.zero);
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
            Label(placement, "Hint", "Chọn vị trí · Chuột phải hoặc bấm Đặt để xác nhận", 15, new Vector2(520, 32), new Vector2(18, -4));
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
        StudyRoomCoinIcon.Attach(wallet);
        content = panel.Find("ItemList/Content").GetComponent<RectTransform>(); scroll = panel.Find("ItemList").GetComponent<ScrollRect>();
        confirm = placementBar.transform.Find("Confirm").GetComponent<Button>(); rotate = placementBar.transform.Find("Rotate").GetComponent<Button>(); cancel = placementBar.transform.Find("Cancel").GetComponent<Button>();
        placementHint = placementBar.transform.Find("Hint").GetComponent<Text>();
        BuildCategoryNavigation(panel);
        BuildItemRows();
    }

    private void ClearRows()
    {
        views.Clear();
        roomViews.Clear();
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
        if (ViewingRoomCatalog) { BuildRoomPurchaseRows(); return; }
        foreach (var item in Paginate(FilteredItems()))
        {
            var row = ItemCard(item.id, item.displayName, item.description, item.thumbnail);
            var stock = Label(row, "Stock", "Chưa sở hữu", 13, new Vector2(250, 24), new Vector2(122, -91));
            var buy = Button(row, "Buy", "Mua · " + item.price, new Vector2(142, 40), Pink);
            StudyRoomCoinIcon.SetPurchaseLabel(buy, item.price);
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
        foreach (var item in Paginate(FilteredItems().Where(i => economy.Progress.OwnedDecorationCount(i.id) > 0)))
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
            var label = Label(empty, "Hint", "Chưa có vật phẩm trong nhóm này. Ghé Shop để mua thêm nhé!", 17, Vector2.zero, Vector2.zero);
            Stretch(label.rectTransform); label.alignment = TextAnchor.MiddleCenter;
        }
    }

    private void BuildRoomRows()
    {
        ClearRows();
        bool any = false;
        if (decorations.RoomId == "initial-room")
        {
            var info = Rect(content, "StarterFurnitureInfo", new Vector2(500, 120));
            info.gameObject.AddComponent<LayoutElement>().preferredHeight = 120;
            var label = Label(info, "Hint",
                "Nội thất có sẵn của phòng khởi đầu chưa thuộc sở hữu của bạn. Bạn có thể dùng tại đây, nhưng cần mua trong Shop để có đồ trong kho và trang trí phòng khác.",
                16, Vector2.zero, Vector2.zero);
            Stretch(label.rectTransform);
            label.alignment = TextAnchor.MiddleCenter;
            any = true;
        }
        var savedRoom = economy.Progress.SavedRooms.FirstOrDefault(room => room.id == decorations.RoomId);
        var placedItems = savedRoom == null ? new List<StudyRoomProgress.PlacedDecoration>() : savedRoom.decorations;
        foreach (var placed in Paginate(placedItems.Where(p => catalog.Find(p.itemId) != null && MatchesCategory(catalog.Find(p.itemId)))))
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

    private bool MatchesCategory(StudyRoomShopCatalog.Item item) => categoryFilter < 0 || (int)item.category == categoryFilter;

    private IEnumerable<StudyRoomShopCatalog.Item> FilteredItems() => catalog.items.Where(i => i != null && MatchesCategory(i))
        .OrderBy(i => i.category).ThenBy(i => i.displayName);

    private IEnumerable<T> Paginate<T>(IEnumerable<T> source)
    {
        var entries = source.ToList();
        pageCount = Mathf.Max(1, Mathf.CeilToInt(entries.Count / (float)PageSize));
        page = Mathf.Clamp(page, 0, pageCount - 1);
        if (pageLabel) pageLabel.text = (page + 1) + " / " + pageCount + " · " + entries.Count + " mục";
        if (previousPage) previousPage.interactable = page > 0;
        if (nextPage) nextPage.interactable = page + 1 < pageCount;
        return entries.Skip(page * PageSize).Take(PageSize);
    }

    private void ResetRoomCategory()
    {
        if (categoryFilter != (int)StudyRoomShopCatalog.Category.Rooms) return;
        categoryFilter = -1;
        if (categoryDropdown) categoryDropdown.SetValueWithoutNotify(0);
    }

    private void ChangePage(int direction)
    {
        page = Mathf.Clamp(page + direction, 0, pageCount - 1);
        RebuildSelectedList();
    }

    private void RebuildSelectedList()
    {
        if (showingRoom) { placedSignature = null; Refresh(); }
        else if (showingInventory) { inventorySignature = null; Refresh(); }
        else { BuildItemRows(); BindItemButtons(); Refresh(); }
        scroll.verticalNormalizedPosition = 1f;
    }

    private void BuildRoomPurchaseRows()
    {
        var loader = GetComponent<StudyRoomRoomLoader>();
        if (!loader || !loader.catalog) { Paginate(new StudyRoomCatalog.Room[0]); return; }
        foreach (var room in Paginate(loader.catalog.rooms.Where(r => r.canCreate)))
        {
            var row = ItemCard(room.id, room.displayName, "Phòng trống · Tự trang trí bằng đồ trong kho", room.thumbnail);
            var buy = Button(row, "BuyRoom", "Mua · " + room.price, new Vector2(142, 40), Pink);
            StudyRoomCoinIcon.SetPurchaseLabel(buy, room.price);
            Right(buy.GetComponent<RectTransform>(), -12, -38);
            string id = room.id;
            buy.onClick.AddListener(() =>
            {
                message.text = economy.Progress.TryUnlockRoom(id)
                    ? "Đã mua phòng · Về Home, chọn Tạo phòng mới để đặt tên và sử dụng"
                    : "Chưa đủ kim cương để mua phòng này.";
                Refresh();
            });
            roomViews[id] = buy;
        }
    }

    private void BuildCategoryNavigation(Transform panel)
    {
        var viewport = panel.Find("ItemList").GetComponent<RectTransform>();
        viewport.offsetMin = new Vector2(20, 76);
        viewport.offsetMax = new Vector2(-20, -244);
        var footer = panel.Find("Footer");
        if (footer) footer.gameObject.SetActive(false);
        var navigation = panel.Find("CategoryNavigation");
        if (!navigation)
        {
            var row = Rect(panel, "CategoryNavigation", new Vector2(440, 38));
            TopLeft(row, new Vector2(24, -190));
            Panel(row, Pale);
            categoryDropdown = row.gameObject.AddComponent<Dropdown>();
            var caption = Label(row, "Caption", "Tất cả", 16, Vector2.zero, Vector2.zero);
            Stretch(caption.rectTransform);
            caption.rectTransform.offsetMin = new Vector2(14, 0);
            caption.rectTransform.offsetMax = new Vector2(-32, 0);
            var arrow = Label(row, "Arrow", "▼", 13, new Vector2(24, 36), Vector2.zero);
            Right(arrow.rectTransform, -8, 0);
            var template = Rect(row, "Template", new Vector2(0, 260));
            template.anchorMin = new Vector2(0, 0); template.anchorMax = new Vector2(1, 0);
            template.pivot = new Vector2(0.5f, 1); template.anchoredPosition = new Vector2(0, -4);
            Panel(template, Cream);
            var clip = Rect(template, "Viewport", Vector2.zero); Stretch(clip);
            clip.gameObject.AddComponent<RectMask2D>();
            var list = Rect(clip, "Content", new Vector2(0, 38));
            list.anchorMin = new Vector2(0, 1); list.anchorMax = Vector2.one; list.pivot = new Vector2(0.5f, 1);
            var option = Rect(list, "Item", new Vector2(0, 38));
            option.anchorMin = new Vector2(0, 1); option.anchorMax = Vector2.one; option.pivot = new Vector2(0.5f, 1);
            var background = Panel(option, Cream);
            var toggle = option.gameObject.AddComponent<Toggle>(); toggle.targetGraphic = background;
            var check = Rect(option, "Checkmark", new Vector2(12, 12));
            check.anchorMin = check.anchorMax = new Vector2(0, 0.5f); check.anchoredPosition = new Vector2(14, 0);
            toggle.graphic = Panel(check, Pink);
            var label = Label(option, "Label", "Nhóm", 16, Vector2.zero, Vector2.zero);
            Stretch(label.rectTransform); label.rectTransform.offsetMin = new Vector2(30, 0);
            var scroller = template.gameObject.AddComponent<ScrollRect>();
            scroller.horizontal = false; scroller.viewport = clip; scroller.content = list;
            scroller.movementType = ScrollRect.MovementType.Clamped;
            categoryDropdown.captionText = caption; categoryDropdown.itemText = label; categoryDropdown.template = template;
            categoryDropdown.targetGraphic = row.GetComponent<Image>();
            template.gameObject.SetActive(false);
            var previous = Button(panel, "PreviousPage", "‹ Trước", new Vector2(116, 38), Pale);
            var previousRect = previous.GetComponent<RectTransform>();
            previousRect.anchorMin = previousRect.anchorMax = previousRect.pivot = new Vector2(0, 0);
            previousRect.anchoredPosition = new Vector2(24, 20);
            var nextButton = Button(panel, "NextPage", "Sau ›", new Vector2(116, 38), Pale);
            var nextRect = nextButton.GetComponent<RectTransform>();
            nextRect.anchorMin = nextRect.anchorMax = nextRect.pivot = new Vector2(1, 0);
            nextRect.anchoredPosition = new Vector2(-24, 20);
            var pageText = Label(panel, "PageLabel", "", 15, new Vector2(240, 38), Vector2.zero);
            pageText.rectTransform.anchorMin = pageText.rectTransform.anchorMax = pageText.rectTransform.pivot = new Vector2(0.5f, 0);
            pageText.rectTransform.anchoredPosition = new Vector2(0, 20);
        }
        categoryDropdown = panel.Find("CategoryNavigation").GetComponent<Dropdown>();
        categoryDropdown.ClearOptions();
        var options = new List<string> { "Tất cả" };
        foreach (StudyRoomShopCatalog.Category category in System.Enum.GetValues(typeof(StudyRoomShopCatalog.Category)))
            options.Add(StudyRoomShopCatalog.CategoryName(category));
        categoryDropdown.AddOptions(options);
        categoryDropdown.SetValueWithoutNotify(categoryFilter + 1);
        categoryDropdown.onValueChanged.RemoveAllListeners();
        categoryDropdown.onValueChanged.AddListener(value =>
        {
            categoryFilter = value - 1;
            if (showingRoom || showingInventory) ResetRoomCategory();
            page = 0;
            RebuildSelectedList();
        });
        previousPage = panel.Find("PreviousPage").GetComponent<Button>();
        nextPage = panel.Find("NextPage").GetComponent<Button>();
        pageLabel = panel.Find("PageLabel").GetComponent<Text>();
        previousPage.onClick.RemoveAllListeners(); previousPage.onClick.AddListener(() => ChangePage(-1));
        nextPage.onClick.RemoveAllListeners(); nextPage.onClick.AddListener(() => ChangePage(1));
    }

    private RectTransform ItemCard(string id, string title, string description, Texture2D thumbnail)
    {
        var row = Rect(content, id, new Vector2(540, 124)); Panel(row, Pale);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 124;
        var photo = Rect(row, "Thumbnail", new Vector2(96, 96)); TopLeft(photo, new Vector2(12, -14));
        var image = photo.gameObject.AddComponent<RawImage>(); image.texture = thumbnail; image.raycastTarget = false;
        var name = Label(row, "Title", title, 21, Vector2.zero, Vector2.zero);
        name.resizeTextForBestFit = true;
        name.resizeTextMinSize = 14;
        name.resizeTextMaxSize = 21;
        name.verticalOverflow = VerticalWrapMode.Truncate;
        name.supportRichText = false;
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
