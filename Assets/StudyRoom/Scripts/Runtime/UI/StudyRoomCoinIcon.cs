using UnityEngine;
using UnityEngine.UI;

/// <summary>Uses the pink diamond from NEW Button Slider beside wallet amounts.</summary>
public static class StudyRoomCoinIcon
{
    private static Sprite sprite;

    public static void SetRewardLabel(Button button, int amount, bool claimed = false)
    {
        var label = button.transform.Find("Label").GetComponent<Text>();
        label.text = claimed ? "Đã nhận" : "Nhận +" + amount;
        label.rectTransform.offsetMax = new Vector2(claimed ? 0 : -26, 0);
        var icon = label.transform.Find("RewardDiamond") as RectTransform;
        if (!icon)
        {
            icon = new GameObject("RewardDiamond", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            icon.SetParent(label.transform, false);
            icon.anchorMin = icon.anchorMax = new Vector2(.5f, .5f);
            icon.pivot = new Vector2(0, .5f);
            icon.sizeDelta = new Vector2(21, 24);
            if (!sprite) sprite = Resources.Load<Sprite>("KawaiiCoin");
            var image = icon.GetComponent<Image>();
            image.sprite = sprite; image.raycastTarget = false;
        }
        icon.anchoredPosition = new Vector2(label.preferredWidth * .5f + 4, 0);
        icon.gameObject.SetActive(!claimed);
    }

    public static void SetPurchaseLabel(Button button, int price, bool owned = false)
    {
        var original = button.transform.Find("Label").GetComponent<Text>();
        original.gameObject.SetActive(owned);
        original.text = "Đã sở hữu";
        var group = button.transform.Find("PurchaseLabel");
        if (!group)
        {
            var rect = new GameObject("PurchaseLabel", typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(button.transform, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            group = rect;
            PurchaseText(group, "Caption", "Mua", original, .05f, .34f);
            PurchaseText(group, "Price", price.ToString(), original, .57f, .96f);
            var icon = new GameObject("Diamond", typeof(RectTransform), typeof(Image));
            var iconRect = icon.GetComponent<RectTransform>();
            iconRect.SetParent(group, false);
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(.445f, .5f);
            iconRect.sizeDelta = new Vector2(22, 25);
            if (!sprite) sprite = Resources.Load<Sprite>("KawaiiCoin");
            var image = icon.GetComponent<Image>();
            image.sprite = sprite; image.raycastTarget = false;
        }
        group.gameObject.SetActive(!owned);
        var priceText = group.Find("Price").GetComponent<Text>();
        priceText.text = price.ToString();
        priceText.alignment = TextAnchor.MiddleLeft;
        priceText.rectTransform.anchorMin = new Vector2(.445f, .08f);
        // The diamond extends 11 units from its center; leave just 4 units before the price.
        priceText.rectTransform.offsetMin = new Vector2(15, 0);
    }

    private static void PurchaseText(Transform parent, string name, string value, Text style, float min, float max)
    {
        var text = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        text.transform.SetParent(parent, false);
        text.rectTransform.anchorMin = new Vector2(min, .08f);
        text.rectTransform.anchorMax = new Vector2(max, .92f);
        text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
        text.font = style.font; text.fontSize = 20; text.color = style.color;
        text.resizeTextForBestFit = true; text.resizeTextMinSize = 10; text.resizeTextMaxSize = 20;
        text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
        text.text = value;
    }

    public static void Attach(Text amount)
    {
        if (!amount || amount.transform.Find("CoinIcon")) return;
        if (!sprite) sprite = Resources.Load<Sprite>("KawaiiCoin");
        if (!sprite) return;
        var icon = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
        var rect = icon.GetComponent<RectTransform>();
        rect.SetParent(amount.transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0, .5f);
        rect.pivot = new Vector2(1, .5f);
        rect.anchoredPosition = new Vector2(-8, 0);
        rect.sizeDelta = new Vector2(29, 32);
        var image = icon.GetComponent<Image>();
        image.sprite = sprite;
        // Preserve the source shape even when the atlas importer rescales its dimensions.
        image.preserveAspect = false;
        image.raycastTarget = false;
    }
}
