using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Phone layouts in logical canvas units, with device safe-area insets.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas), typeof(CanvasScaler))]
public class StudyRoomResponsiveUI : MonoBehaviour
{
    private readonly Dictionary<RectTransform, Vector4> rootAnchors = new Dictionary<RectTransform, Vector4>();
    private Canvas canvas;
    private float nextLayout;
    private int width, height;
    private Rect safeArea;
    private float hudScale = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        InstallInScenes();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => InstallInScenes();

    private static void InstallInScenes()
    {
        foreach (var root in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!root.isRootCanvas || root.renderMode == RenderMode.WorldSpace) continue;
            if (!root.GetComponentInParent<StudyRoomMenu>() && !root.GetComponentInParent<StudyRoomStartScreen>()) continue;
            if (!root.GetComponent<StudyRoomResponsiveUI>()) root.gameObject.AddComponent<StudyRoomResponsiveUI>();
        }
    }

    private void LateUpdate()
    {
        bool changed = width != Screen.width || height != Screen.height || safeArea != Screen.safeArea;
        if (!changed && Time.unscaledTime < nextLayout) return;
        // Also pick up shop rows, dialogs and dropdowns created after the canvas.
        nextLayout = Time.unscaledTime + .15f;
        ApplyViewport(Screen.width, Screen.height, Screen.safeArea);
    }

    public void ApplyViewport(int pixelWidth, int pixelHeight, Rect deviceSafeArea)
    {
        if (pixelWidth <= 0 || pixelHeight <= 0) return;
        width = pixelWidth; height = pixelHeight;
        safeArea = deviceSafeArea;
        if (deviceSafeArea.width <= 0 || deviceSafeArea.height <= 0)
            deviceSafeArea = new Rect(0, 0, width, height);
        bool portrait = deviceSafeArea.height > deviceSafeArea.width;
        canvas = GetComponent<Canvas>();
        var scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = portrait ? new Vector2(540, 960) : new Vector2(960, 540);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = portrait ? 0f : 1f;
        float scale = portrait ? width / 540f : height / 540f;
        canvas.scaleFactor = scale;
        // Preserve the original 1280x720 canvas sizing for the room HUD only.
        hudScale = Mathf.Sqrt((width / 1280f) * (height / 720f)) / scale;
        Vector2 available = deviceSafeArea.size / scale;
        LayoutRoomHUD(portrait);
        LayoutFurnitureEditor(portrait);
        LayoutShop(portrait);
        LayoutRewards(portrait);
        LayoutStart(portrait, available);
        LayoutMenu(available);
        LayoutComputer(portrait, available);
        ApplySafeArea(deviceSafeArea);
    }

    private void ApplySafeArea(Rect area)
    {
        Vector2 min = new Vector2(area.xMin / width, area.yMin / height);
        Vector2 span = new Vector2(area.width / width, area.height / height);
        foreach (Transform child in transform)
        {
            var rect = child as RectTransform;
            if (!rect) continue;
            if (!rootAnchors.TryGetValue(rect, out var anchors))
            {
                anchors = new Vector4(rect.anchorMin.x, rect.anchorMin.y, rect.anchorMax.x, rect.anchorMax.y);
                rootAnchors.Add(rect, anchors);
            }
            Vector2 offsetMin = rect.offsetMin, offsetMax = rect.offsetMax;
            rect.anchorMin = min + Vector2.Scale(new Vector2(anchors.x, anchors.y), span);
            rect.anchorMax = min + Vector2.Scale(new Vector2(anchors.z, anchors.w), span);
            rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
        }
    }

    private void LayoutFurnitureEditor(bool portrait)
    {
        var root = transform.Find("FurnitureEditUI"); if (!root) return;
        Top(root, "EditButton", 28 * hudScale, 216 * hudScale, 144, 64, true);
        R(root, "EditButton").localScale = Vector3.one * hudScale;
        var bar = R(root, "Toolbar"); if (!bar) return;
        bar.anchorMin = Vector2.zero; bar.anchorMax = new Vector2(1, 0); bar.pivot = new Vector2(.5f, 0);
        bar.sizeDelta = new Vector2(-24, portrait ? 200 : 164); bar.anchoredPosition = new Vector2(0, 16);
        Header(bar, "Title", 16, 16, 8, portrait ? 44 : 32, 24);
        Header(bar, "Hint", 16, 16, portrait ? 54 : 42, portrait ? 58 : 38, 18);
    }

    private void LayoutRoomHUD(bool portrait)
    {
        Top(transform, "MenuButton", 28 * hudScale, 28 * hudScale, 144, 52, true);
        var menuButton = R(transform, "MenuButton"); if (menuButton) menuButton.localScale = Vector3.one * hudScale;
        var shop = transform.Find("ShopUI");
        if (shop)
        {
            Top(shop, "ShopButton", 28 * hudScale, 96 * hudScale, 144, 48, true);
            Top(shop, "InventoryButton", 28 * hudScale, 156 * hudScale, 144, 48, true);
            foreach (string name in new[] { "ShopButton", "InventoryButton" })
            { var button = R(shop, name); if (button) button.localScale = Vector3.one * hudScale; }
            var placement = shop.Find("PlacementBar");
            if (placement)
            {
                Stretch(placement, 12, 12, 12, 12, new Vector2(0, 0), new Vector2(1, 0));
                var rect = (RectTransform)placement; rect.sizeDelta = new Vector2(-24, 140);
                rect.pivot = new Vector2(.5f, 0); rect.anchoredPosition = new Vector2(0, 16);
                Fraction(placement, "Hint", .03f, .61f, .97f, .96f);
                Fraction(placement, "Rotate", .025f, .08f, .325f, .54f);
                Fraction(placement, "Confirm", .35f, .08f, .65f, .54f);
                Fraction(placement, "Cancel", .675f, .08f, .975f, .54f);
            }
        }
        var rewards = transform.Find("RewardsUI");
        if (rewards)
        {
            Top(rewards, "WalletHUD", 28 * hudScale, 28 * hudScale, 410, 100);
            var hud = rewards.Find("WalletHUD");
            if (hud) hud.localScale = Vector3.one * hudScale;
            Top(hud, "Coins", 18, 3, 176, 38);
            Top(hud, "Countdown", 18, 36, 245, 27);
            Top(hud, "StudyStatus", 18, 64, 375, 24);
            Top(hud, "Quests", 16, 1, 140, 42, true);
            Top(rewards, "RewardNotice", 28 * hudScale, 140 * hudScale, 410, 42);
            var notice = R(rewards, "RewardNotice"); if (notice) notice.localScale = Vector3.one * hudScale;
        }
        Bottom(transform, "RealTimeBar", 28 * hudScale, 28 * hudScale, 410, 114);
        var time = transform.Find("RealTimeBar");
        if (time) time.localScale = Vector3.one * hudScale;
        Header(time, "Period", 40, 12, 17, 20, 13);
        Header(time, "Clock", 18, 12, 37, 46, 32);
        Header(time, "Date", 20, 12, 81, 26, 15);
    }

    private void LayoutShop(bool portrait)
    {
        var card = transform.Find("ShopUI/ShopPanel/Card");
        if (!card) return;
        Stretch(card, 12, 12, 12, 12);
        Header(card, "Title", 16, 104, 16, 42, 26);
        Top(card, "Close", 16, 12, 88, 64, true);
        Header(card, "Wallet", 16, 112, 60, 30, 22);
        Header(card, "Message", 16, 16, 96, 42, 18);
        if (!portrait)
        {
            Header(card, "Title", 16, 324, 12, 42, 26);
            Top(card, "Wallet", 116, 12, 188, 48, true);
            Header(card, "Message", 16, 116, 64, 36, 18);
        }
        string[] tabs = { "ShopTab", "InventoryTab", "RoomTab" };
        for (int i = 0; i < tabs.Length; i++)
        {
            var rect = R(card, tabs[i]); if (!rect) continue;
            float columns = portrait ? 3f : 4f;
            rect.anchorMin = new Vector2(i / columns, 1); rect.anchorMax = new Vector2((i + 1) / columns, 1);
            rect.pivot = new Vector2(.5f, 1); rect.offsetMin = new Vector2(8, portrait ? -210 : -176); rect.offsetMax = new Vector2(-8, portrait ? -146 : -112);
            FitText(rect, 19);
        }
        Header(card, "CategoryNavigation", 12, 12, 222, 64);
        if (!portrait)
        {
            var category = R(card, "CategoryNavigation");
            category.anchorMin = new Vector2(.75f, 1); category.anchorMax = Vector2.one;
            category.offsetMin = new Vector2(8, -176); category.offsetMax = new Vector2(-8, -112);
        }
        Stretch(card.Find("ItemList"), 8, portrait ? 298 : 188, 8, 88);
        Bottom(card, "PreviousPage", 12, 12, 122, 64);
        Bottom(card, "NextPage", 12, 12, 122, 64, true);
        var label = R(card, "PageLabel");
        if (label)
        {
            label.anchorMin = new Vector2(0, 0); label.anchorMax = new Vector2(1, 0);
            label.offsetMin = new Vector2(142, 12); label.offsetMax = new Vector2(-142, 76); FitText(label, 17);
        }
        var content = card.Find("ItemList/Content");
        if (content) foreach (Transform row in content) LayoutShopRow(row, portrait);
        var option = card.Find("CategoryNavigation/Template/Viewport/Content/Item");
        if (option)
        {
            var rect = (RectTransform)option; rect.sizeDelta = new Vector2(rect.sizeDelta.x, 64);
            var list = (RectTransform)option.parent; list.sizeDelta = new Vector2(list.sizeDelta.x, 64);
        }
    }

    private static void LayoutShopRow(Transform row, bool portrait)
    {
        var element = row.GetComponent<LayoutElement>();
        if (!row.Find("Thumbnail")) { if (element) element.preferredHeight = 160; return; }
        if (element) element.preferredHeight = portrait ? 232 : 176;
        Top(row, "Thumbnail", 12, 16, portrait ? 88 : 112, portrait ? 88 : 112);
        float left = portrait ? 112 : 140;
        Header(row, "Title", left, portrait ? 12 : 180, 12, 58, 23);
        Header(row, "Description", left, portrait ? 12 : 180, 72, 40, 18);
        Header(row, "Stock", left, portrait ? 12 : 180, 114, 30, 18);
        string[] buttons = { "Buy", "Place", "Return", "BuyRoom" };
        foreach (string name in buttons)
        {
            var rect = R(row, name); if (!rect) continue;
            if (portrait)
            {
                bool buy = name == "Buy";
                rect.anchorMin = new Vector2(buy ? 0 : .5f, 0); rect.anchorMax = new Vector2(buy ? .5f : 1, 0);
                rect.offsetMin = new Vector2(12, 12); rect.offsetMax = new Vector2(-12, 76);
            }
            else Top(row, name, 12, name == "Buy" ? 12 : 92, 156, 64, true);
            FitText(rect, 20);
        }
    }

    private void LayoutRewards(bool portrait)
    {
        var card = transform.Find("RewardsUI/DailyQuests/Card"); if (!card) return;
        Stretch(card, 12, 12, 12, 12);
        Header(card, "Title", 16, 112, 16, 52, 25);
        Top(card, "Close", 16, 12, 88, 64, true);
        Header(card, "Date", 16, 16, 78, 26, 18);
        Header(card, "Summary", 16, 16, 112, 48, 19);
        Header(card, "Study", 16, 16, 168, 64);
        var hint = card.Find("Hint"); if (hint) hint.gameObject.SetActive(portrait);
        if (portrait) Header(card, "Hint", 16, 16, 240, 48, 17);
        Stretch(card.Find("QuestList"), 8, portrait ? 298 : 246, 8, 16);
        var footer = card.Find("Footer"); if (footer) footer.gameObject.SetActive(false);
        var list = card.Find("QuestList/Content");
        if (!list) return;
        foreach (Transform row in list)
        {
            var element = row.GetComponent<LayoutElement>(); if (element) element.preferredHeight = 136;
            Header(row, "Title", 12, 156, 10, 55, 21);
            Header(row, "Progress", 12, 156, 66, 38, 18);
            Top(row, "Claim", 12, 24, 132, 72, true);
            var track = R(row, "Track");
            if (track) { track.anchorMin = Vector2.zero; track.anchorMax = new Vector2(1, 0); track.offsetMin = new Vector2(12, 12); track.offsetMax = new Vector2(-156, 20); }
        }
    }

    private void LayoutMenu(Vector2 available)
    {
        var overlay = transform.Find("Overlay"); if (!overlay) return;
        var overlayCanvas = overlay.GetComponent<Canvas>();
        if (overlayCanvas) { overlayCanvas.overrideSorting = true; overlayCanvas.sortingOrder = 400; }
        var menu = R(overlay, "MenuPanel");
        if (menu)
        {
            menu.sizeDelta = new Vector2(Mathf.Min(440, available.x - 32), 430);
            menu.localScale = Vector3.one * Mathf.Min(1, (available.y - 32) / 430f);
            Header(menu, "Eyebrow", 16, 16, 16, 28, 18);
            Header(menu, "Title", 16, 16, 52, 56, 27);
            Header(menu, "Settings", 24, 24, 130, 72);
            Header(menu, "Home", 24, 24, 222, 72);
            Header(menu, "Close", 24, 24, 314, 72);
        }
        var home = R(overlay, "HomePanel");
        if (home)
        {
            home.sizeDelta = new Vector2(Mathf.Min(480, available.x - 32), 430);
            home.localScale = Vector3.one * Mathf.Min(1, (available.y - 32) / 430f);
            Header(home, "Eyebrow", 16, 16, 16, 28, 18);
            Header(home, "Title", 16, 16, 56, 64, 38);
            Header(home, "Subtitle", 16, 16, 132, 52, 19);
            Header(home, "EnterRoom", 24, 24, 210, 72);
            Header(home, "Settings", 24, 24, 306, 72);
        }
        var settings = R(overlay, "SettingsPanel"); if (!settings) return;
        settings.localScale = Vector3.one;
        settings.sizeDelta = new Vector2(Mathf.Min(540, available.x - 24), Mathf.Min(680, available.y - 24));
        var body = MakeScrollableSettings(settings);
        Header(body, "Eyebrow", 20, 20, 16, 28, 18);
        Header(body, "Title", 20, 20, 48, 52, 30);
        Header(body, "Subtitle", 20, 20, 106, 48, 19);
        Header(body, "SfxLabel", 20, 88, 170, 40, 20);
        Top(body, "SfxValue", 20, 170, 64, 40, true);
        Header(body, "SfxSlider", 20, 20, 218, 64);
        Header(body, "MusicLabel", 20, 88, 300, 48, 19);
        Top(body, "MusicValue", 20, 300, 64, 48, true);
        Header(body, "MusicSlider", 20, 20, 356, 64);
        Header(body, "GraphicsTitle", 20, 20, 442, 36, 18);
        for (int i = 0; i < 3; i++)
        {
            var quality = R(body, "Quality" + i); if (!quality) continue;
            quality.anchorMin = new Vector2(i / 3f, 1); quality.anchorMax = new Vector2((i + 1) / 3f, 1);
            quality.offsetMin = new Vector2(12, -558); quality.offsetMax = new Vector2(-12, -486);
        }
        Header(body, "Back", 20, 20, 582, 72);
        foreach (var slider in body.GetComponentsInChildren<Slider>(true)) LayoutSlider(slider);
    }

    private static Transform MakeScrollableSettings(RectTransform settings)
    {
        var existing = settings.Find("SettingsViewport/Content"); if (existing) return existing;
        var children = new List<Transform>(); foreach (Transform child in settings) children.Add(child);
        var viewport = new GameObject("SettingsViewport", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
        viewport.SetParent(settings, false); Stretch(viewport, 4, 4, 4, 4);
        var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(viewport, false); content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
        content.pivot = new Vector2(.5f, 1); content.sizeDelta = new Vector2(0, 680);
        foreach (var child in children) child.SetParent(content, false);
        var scroll = settings.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        return content;
    }

    private static void LayoutSlider(Slider slider)
    {
        var track = R(slider.transform, "Track");
        if (track) { track.anchorMin = new Vector2(0, .5f); track.anchorMax = new Vector2(1, .5f); track.sizeDelta = new Vector2(0, 10); track.anchoredPosition = Vector2.zero; }
        foreach (string name in new[] { "FillArea", "HandleArea" })
        {
            var area = R(slider.transform, name); if (!area) continue;
            area.anchorMin = new Vector2(0, .5f); area.anchorMax = new Vector2(1, .5f);
            area.sizeDelta = new Vector2(-24, name == "FillArea" ? 10 : 0); area.anchoredPosition = Vector2.zero;
        }
        if (slider.handleRect) { slider.handleRect.anchorMin = new Vector2(slider.handleRect.anchorMin.x, .5f); slider.handleRect.anchorMax = new Vector2(slider.handleRect.anchorMax.x, .5f); slider.handleRect.sizeDelta = new Vector2(28, 28); }
    }

    private void LayoutStart(bool portrait, Vector2 available)
    {
        var root = transform.Find("ScreenStart"); if (!root) return;
        var start = root.Find("Start");
        if (start)
        {
            Fraction(start, "TitleBar", .06f, .83f, .94f, .94f);
            Fraction(start, "Subtitle", .04f, .76f, .96f, .82f);
            Fraction(start, "CharacterArea", .12f, .25f, .88f, .74f);
            Fraction(start, "StartPrompt", .06f, .12f, .94f, .22f);
            Fraction(start, "StartHint", .06f, .055f, .94f, .12f);
            var title = start.Find("TitleBar/Title"); if (title) { Stretch(title, 12, 0, 12, 0); FitText((RectTransform)title, 46); }
        }
        foreach (string name in new[] { "RoomPopup", "UnlockConfirmation", "RoomNaming" })
        {
            var card = R(root, name + "/Card"); if (!card) continue;
            float keyboard = name == "RoomNaming" && TouchScreenKeyboard.visible ? TouchScreenKeyboard.area.height / canvas.scaleFactor : 0;
            float visibleHeight = Mathf.Max(180, available.y - keyboard);
            float scale = Mathf.Min(1, (available.x - 32) / card.sizeDelta.x, (visibleHeight - 32) / card.sizeDelta.y);
            card.localScale = Vector3.one * Mathf.Max(.1f, scale);
            card.anchoredPosition = new Vector2(0, keyboard * .5f);
            foreach (var button in card.GetComponentsInChildren<Button>(true))
            {
                var rect = (RectTransform)button.transform;
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, 72);
            }
            var input = R(card, "RoomName"); if (input) input.sizeDelta = new Vector2(input.sizeDelta.x, 72);
        }
        var picker = root.Find("Picker"); if (!picker) return;
        Top(picker, "Back", 12, 12, 152, 64);
        Top(picker, "Coins", 12, 12, 152, 64, true);
        Header(picker, "Heading", 16, 16, 84, 52, 30);
        Header(picker, "Step", 16, 16, 142, 46, 19);
        if (!portrait)
        {
            Header(picker, "Heading", 176, 176, 12, 64, 28);
            Header(picker, "Step", 16, 16, 88, 36, 18);
        }
        Stretch(picker.Find("RoomList"), 12, portrait ? 200 : 136, 12, 148);
        var next = R(picker, "Next");
        if (next) { next.anchorMin = Vector2.zero; next.anchorMax = new Vector2(1, 0); next.offsetMin = new Vector2(16, 64); next.offsetMax = new Vector2(-16, 132); FitText(next, 21); }
        var msg = R(picker, "Message");
        if (msg) { msg.anchorMin = Vector2.zero; msg.anchorMax = new Vector2(1, 0); msg.offsetMin = new Vector2(16, 8); msg.offsetMax = new Vector2(-16, 56); FitText(msg, 18); }
        var list = picker.Find("RoomList/Content");
        if (list) foreach (Transform row in list)
        {
            var thumbnail = row.Find("Thumbnail");
            if (!thumbnail) continue;
            var element = row.GetComponent<LayoutElement>(); if (element) element.preferredHeight = 210;
            Top(row, "Thumbnail", 12, 16, 120, 160);
            Header(row, "Name", 148, 12, 12, 56, 23);
            Header(row, "Detail", 148, 12, 70, 68, 18);
            Header(row, "Action", 148, 12, 144, 52, 20);
        }
    }

    private void LayoutComputer(bool portrait, Vector2 available)
    {
        var card = transform.Find("ComputerDesktop/DesktopCard"); if (!card) return;
        Stretch(card, 12, 12, 12, 12);
        Header(card, "Brand", 16, portrait ? 16 : 216, 12, 44, 22);
        Top(card, "CloseComputer", 16, portrait ? 68 : 12, 184, 64, true);
        Top(card, "MusicApp", 16, portrait ? 148 : 88, portrait ? 144 : 160, portrait ? 112 : 140);
        var welcome = card.Find("Welcome"); if (welcome) welcome.gameObject.SetActive(!portrait);
        if (!portrait) Fraction(card, "Welcome", .25f, .30f, .95f, .72f);
        var window = card.Find("MusicWindow");
        Stretch(window, portrait ? 8 : 192, portrait ? 146 : 76, 8, 80);
        if (window)
        {
            Header(window, "AppTitle", 16, 124, 12, 48, 22);
            Top(window, "Minimize", 12, 12, 108, 64, true);
            bool spacious = portrait && available.y >= 900;
            var artwork = window.Find("Artwork"); if (artwork) artwork.gameObject.SetActive(spacious);
            Header(window, "Artwork", 120, 120, 92, 112, 72);
            Header(window, "TrackTitle", 20, 20, spacious ? 220 : portrait ? 80 : 68, spacious ? 64 : portrait ? 40 : 28, 25);
            var subtitle = window.Find("Subtitle"); if (subtitle) subtitle.gameObject.SetActive(portrait);
            if (portrait) Header(window, "Subtitle", 20, 20, spacious ? 292 : 128, 38, 18);
            var seek = R(window, "Progress");
            if (seek) { seek.anchorMin = Vector2.zero; seek.anchorMax = new Vector2(1, 0); seek.offsetMin = new Vector2(20, portrait ? 220 : 176); seek.offsetMax = new Vector2(-20, portrait ? 284 : 240); }
            var timer = R(window, "Timer");
            if (timer) { timer.anchorMin = Vector2.zero; timer.anchorMax = new Vector2(1, 0); timer.offsetMin = new Vector2(20, portrait ? 176 : 144); timer.offsetMax = new Vector2(-20, portrait ? 216 : 172); FitText(timer, 18); }
            string[] controls = { "Previous", "PlayPause", "Next" };
            for (int i = 0; i < controls.Length; i++)
            {
                var rect = R(window, controls[i]); if (!rect) continue;
                rect.anchorMin = new Vector2(i / 3f, 0); rect.anchorMax = new Vector2((i + 1) / 3f, 0);
                rect.offsetMin = new Vector2(8, portrait ? 88 : 72); rect.offsetMax = new Vector2(-8, portrait ? 160 : 136);
                FitText(rect, 21);
            }
            var volumeLabel = R(window, "VolumeLabel");
            if (volumeLabel) { volumeLabel.anchorMin = Vector2.zero; volumeLabel.anchorMax = new Vector2(.4f, 0); volumeLabel.offsetMin = new Vector2(12, 8); volumeLabel.offsetMax = new Vector2(0, 72); FitText(volumeLabel, 18); }
            var volume = R(window, "Volume");
            if (volume) { volume.anchorMin = new Vector2(.43f, 0); volume.anchorMax = new Vector2(1, 0); volume.offsetMin = new Vector2(0, 8); volume.offsetMax = new Vector2(-12, 72); }
            foreach (var slider in window.GetComponentsInChildren<Slider>(true))
            {
                LayoutSlider(slider);
            }
            FitText(R(window, "TrackTitle"), portrait ? 26 : 24);
        }
        var dock = card.Find("Taskbar");
        if (dock)
        {
            var rect = (RectTransform)dock; rect.anchorMin = Vector2.zero; rect.anchorMax = new Vector2(1, 0);
            rect.offsetMin = Vector2.zero; rect.offsetMax = new Vector2(0, 80);
            Fraction(dock, "MusicShortcut", .02f, .10f, portrait ? .66f : .28f, .90f);
            Fraction(dock, "Clock", .76f, .10f, .97f, .90f);
            var hint = dock.Find("PlaybackHint"); if (hint) hint.gameObject.SetActive(!portrait);
            if (!portrait) Fraction(dock, "PlaybackHint", .31f, .10f, .74f, .90f);
        }
    }

    private static RectTransform R(Transform root, string path) => root ? root.Find(path) as RectTransform : null;

    private static void FitText(RectTransform root, int size)
    {
        if (!root) return;
        foreach (var text in root.GetComponentsInChildren<Text>(true))
        {
            text.resizeTextForBestFit = true; text.resizeTextMinSize = Mathf.Min(16, size); text.resizeTextMaxSize = size;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }
    }

    private static void Top(Transform parent, string name, float inset, float top, float width, float height, bool right = false)
    {
        var rect = R(parent, name); if (!rect) return;
        rect.localScale = Vector3.one;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(right ? 1 : 0, 1);
        rect.sizeDelta = new Vector2(width, height); rect.anchoredPosition = new Vector2(right ? -inset : inset, -top);
    }

    private static void Bottom(Transform parent, string name, float inset, float bottom, float width, float height, bool right = false)
    {
        Top(parent, name, inset, 0, width, height, right);
        var rect = R(parent, name); if (!rect) return;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(right ? 1 : 0, 0);
        rect.anchoredPosition = new Vector2(right ? -inset : inset, bottom);
    }

    private static void Header(Transform parent, string name, float left, float right, float top, float height, int textSize = 0)
    {
        var rect = R(parent, name); if (!rect) return;
        rect.localScale = Vector3.one; rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, -top - height); rect.offsetMax = new Vector2(-right, -top);
        if (textSize > 0) FitText(rect, textSize);
    }

    private static void Stretch(Transform target, float left, float top, float right, float bottom, Vector2? min = null, Vector2? max = null)
    {
        var rect = target as RectTransform; if (!rect) return;
        rect.localScale = Vector3.one; rect.anchorMin = min ?? Vector2.zero; rect.anchorMax = max ?? Vector2.one;
        rect.offsetMin = new Vector2(left, bottom); rect.offsetMax = new Vector2(-right, -top);
    }

    private static void Fraction(Transform parent, string name, float left, float bottom, float right, float top)
    {
        var rect = R(parent, name); if (!rect) return;
        rect.localScale = Vector3.one; rect.anchorMin = new Vector2(left, bottom); rect.anchorMax = new Vector2(right, top);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
