using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Stores wallet diamonds in scenes so they are visible before Play Mode.</summary>
[InitializeOnLoad]
public static class StudyRoomCurrencyIconBuilder
{
    static StudyRoomCurrencyIconBuilder()
    {
        EditorApplication.delayCall += BuildOpenedScenes;
        EditorSceneManager.sceneOpened += (scene, mode) => Build(scene);
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode) BuildOpenedScenes();
        };
    }

    private static void BuildOpenedScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++) Build(SceneManager.GetSceneAt(i));
    }

    private static void Build(Scene scene)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !scene.isLoaded || string.IsNullOrEmpty(scene.path)) return;
        bool wasDirty = scene.isDirty;
        bool changed = false;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                if (button.name != "Claim" || button.transform.Find("Label/RewardDiamond")) continue;
                var parent = button.transform.parent;
                if (!parent || !parent.name.StartsWith("Quest") || !int.TryParse(parent.name.Substring(5), out int index) ||
                    index < 0 || index >= StudyRoomDailyQuests.All.Count) continue;
                StudyRoomCoinIcon.SetRewardLabel(button, StudyRoomDailyQuests.All[index].Coins);
                changed = true;
            }
            foreach (var text in root.GetComponentsInChildren<Text>(true))
            {
                var parent = text.transform.parent;
                if (!parent) continue;
                bool wallet = text.name == "Coins" && (parent.name == "WalletHUD" || parent.name == "Picker");
                wallet |= text.name == "Wallet" && parent.name == "Card" && parent.parent && parent.parent.name == "ShopPanel";
                if (!wallet || text.transform.Find("CoinIcon")) continue;
                StudyRoomCoinIcon.Attach(text);
                changed |= text.transform.Find("CoinIcon") != null;
            }
            foreach (var shop in root.GetComponentsInChildren<StudyRoomShopUI>(true))
            {
                var ui = shop.transform.Find("Canvas/ShopUI");
                if (!ui) continue;
                foreach (var text in ui.GetComponentsInChildren<Text>(true))
                    if (text.text.Contains("coin"))
                    {
                        text.text = text.name == "Wallet" ? text.text.Replace(" coin", "")
                            : text.text.Replace("Mua bằng coin · ", "").Replace("coin", "kim cương");
                        changed = true;
                    }
                foreach (var button in ui.GetComponentsInChildren<Button>(true))
                {
                    if (button.name != "Buy" && button.name != "BuyRoom") continue;
                    var purchase = button.transform.Find("PurchaseLabel");
                    if (purchase)
                    {
                        var priceText = purchase.Find("Price").GetComponent<Text>();
                        if (priceText.alignment != TextAnchor.MiddleLeft && int.TryParse(priceText.text, out int existingPrice))
                        {
                            StudyRoomCoinIcon.SetPurchaseLabel(button, existingPrice, !purchase.gameObject.activeSelf);
                            changed = true;
                        }
                        continue;
                    }
                    var label = button.transform.Find("Label");
                    if (!label) continue;
                    var match = System.Text.RegularExpressions.Regex.Match(label.GetComponent<Text>().text, @"\d+$");
                    if (!int.TryParse(match.Value, out int price)) continue;
                    StudyRoomCoinIcon.SetPurchaseLabel(button, price);
                    changed = true;
                }
            }
        }
        if (!changed) return;
        EditorSceneManager.MarkSceneDirty(scene);
        if (!wasDirty) EditorSceneManager.SaveScene(scene);
    }
}
