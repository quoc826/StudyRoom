using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Category = StudyRoomShopCatalog.Category;

/// <summary>Shared, repeatable prices for authored catalogs and catalog rebuilds.</summary>
public static class StudyRoomShopPricing
{
    public static int Price(StudyRoomShopCatalog.Item item)
    {
        string id = item.id ?? "";
        if (item.category == Category.Animals)
        {
            switch (id)
            {
                case "animal-sparrow": return 240;
                case "animal-herring": return 270;
                case "animal-gecko": return 300;
                case "animal-muskrat": return 330;
                case "animal-squid": return 360;
                case "animal-taipan": return 390;
                case "animal-pudu": return 420;
                case "animal-colobus": return 450;
                default: return 300;
            }
        }
        string name = (item.prefab ? item.prefab.name : id).ToLowerInvariant().Replace('_', '-');
        int price;
        switch (item.category)
        {
            case Category.Books: price = 10; break;
            case Category.Stationery: price = 12; break;
            case Category.Kitchen: price = 15; break;
            case Category.Decorations: price = 20; break;
            case Category.Toys: price = 25; break;
            case Category.Plants: price = 30; break;
            case Category.Textiles: price = 35; break;
            case Category.WallDecor: price = 40; break;
            case Category.Lighting: price = 50; break;
            case Category.Bathroom: price = 40; break;
            case Category.Laundry: price = 40; break;
            case Category.Seating: price = 65; break;
            case Category.Tables: price = 80; break;
            case Category.Storage: price = 85; break;
            case Category.Electronics: price = 100; break;
            case Category.Beds: price = 140; break;
            case Category.Rooms: return 500;
            default: price = 20; break;
        }
        // Functional subtypes cost more than small accessories in the same category.
        if (Regex.IsMatch(name, "sofa|wardrobe|computer(?!-mat)|television|washing-machine|drying-machine|fridge|refrigerator|bath-[0-9]|shower")) price += 55;
        else if (Regex.IsMatch(name, "armchair|bookcase|bookshelf|cabinet|desk|guitar|piano|bonsai|floor-lamp|lamp-floor")) price += 25;
        else if (Regex.IsMatch(name, "candle|pillow|cushion|basket|box|bottle|cup|plate|pen-|pencil")) price = Mathf.Max(8, price - 15);
        if (item.category == Category.Stationery && name.Contains("board")) price += 25;
        // Stable per-item variation; prices never change with catalog order or reloads.
        uint hash = 2166136261;
        foreach (char character in id) hash = unchecked((hash ^ character) * 16777619);
        return price + (int)(hash % 5) * 3;
    }

    public static void Apply(StudyRoomShopCatalog catalog)
    {
        foreach (var item in catalog.items)
            if (item != null) item.price = Price(item);
        EditorUtility.SetDirty(catalog);
    }

    [MenuItem("StudyRoom/Balance Shop Prices")]
    public static void Balance()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<StudyRoomShopCatalog>(StudyRoomEditorPaths.ShopCatalog);
        if (!catalog) throw new InvalidOperationException("Decoration shop catalog is missing.");
        Undo.RecordObject(catalog, "Balance shop prices");
        Apply(catalog);
        AssetDatabase.SaveAssets();
    }
}
