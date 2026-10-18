using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Category = StudyRoomShopCatalog.Category;

public static class StudyRoomMnostvaCatalogBuilder
{
    private const string Interiors = "Assets/ThirdParty/Mnostva_Art/Prefabs/Interiors";
    private const string Images = "Assets/StudyRoom/UI/Textures/Shop/Mnostva";
    private static readonly MethodInfo Thumbnail = typeof(StudyRoomStartBuilder).GetMethod("Thumbnail", BindingFlags.NonPublic | BindingFlags.Static);

    public static string[] ItemPaths() => AssetDatabase.FindAssets("t:Prefab", new[] { Interiors })
        .Select(AssetDatabase.GUIDToAssetPath)
        .Where(p => !Regex.IsMatch(System.IO.Path.GetFileNameWithoutExtension(p), @"^(Floor|Wall|Window|Door|Path|Tile|Stairs|Fence|Garden)_\d+$", RegexOptions.IgnoreCase))
        .OrderBy(p => p).ToArray();

    public static string[] RoomPaths() => AssetDatabase.FindAssets("t:Prefab", new[] { StudyRoomEditorPaths.RoomModels })
        .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToArray();

    public static string BuildItems(int offset, int count)
    {
        StudyRoomEditorPaths.EnsureFolder(Images);
        var catalog = AssetDatabase.LoadAssetAtPath<StudyRoomShopCatalog>(StudyRoomEditorPaths.ShopCatalog);
        var entries = catalog.items.ToList();
        foreach (var item in entries) item.category = Classify(item.id.StartsWith("animal-") ? "animal-" : item.prefab.name);
        var paths = ItemPaths();
        foreach (var path in paths.Skip(offset).Take(count))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var item = entries.FirstOrDefault(i => i.prefab == prefab);
            var category = Classify(prefab.name);
            if (item == null)
            {
                item = new StudyRoomShopCatalog.Item
                {
                    id = "mnostva-" + prefab.name.ToLowerInvariant().Replace('_', '-'),
                    displayName = StudyRoomShopCatalog.CategoryName(category) + " · " + prefab.name.Replace('_', ' '),
                    description = "",
                    prefab = prefab,
                    price = Price(category)
                };
                entries.Add(item);
            }
            item.category = category;
            if (!item.thumbnail) item.thumbnail = (Texture2D)Thumbnail.Invoke(null, new object[] { prefab, Images + "/" + prefab.name + ".png", false });
        }
        catalog.items = entries.OrderBy(i => i.category).ThenBy(i => i.displayName).ToArray();
        StudyRoomShopPricing.Apply(catalog);
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        return "Items " + Mathf.Min(offset + count, paths.Length) + "/" + paths.Length + "; shop total=" + catalog.items.Length;
    }

    public static string BuildRooms(int offset, int count)
    {
        StudyRoomEditorPaths.EnsureFolder(StudyRoomEditorPaths.Prefabs + "/Rooms");
        string images = StudyRoomEditorPaths.Thumbnails + "/MnostvaRooms";
        StudyRoomEditorPaths.EnsureFolder(images);
        var catalog = AssetDatabase.LoadAssetAtPath<StudyRoomCatalog>(StudyRoomEditorPaths.Catalogs + "/RoomCatalog.asset");
        var entries = catalog.rooms.ToList();
        var paths = RoomPaths();
        foreach (var path in paths.Skip(offset).Take(count))
        {
            string model = System.IO.Path.GetFileNameWithoutExtension(path);
            string id = "empty-" + model.ToLowerInvariant();
            var prefab = StudyRoomStartBuilder.EmptyRoom(model);
            var room = entries.FirstOrDefault(r => r.id == id);
            if (room == null)
            {
                room = new StudyRoomCatalog.Room { id = id, price = 100, canCreate = true };
                entries.Add(room);
                room.displayName = RoomName(model);
            }
            room.prefab = prefab;
            room.spawnPosition = SpawnPosition(prefab);
            if (!room.thumbnail) room.thumbnail = (Texture2D)Thumbnail.Invoke(null, new object[] { prefab, images + "/" + model + ".png", false });
        }
        catalog.rooms = entries.ToArray();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        return "Rooms " + Mathf.Min(offset + count, paths.Length) + "/" + paths.Length + "; catalog total=" + catalog.rooms.Length;
    }

    private static Vector3 SpawnPosition(GameObject prefab)
    {
        var floor = prefab.GetComponentsInChildren<MeshFilter>().Where(m => m.name.Contains("_Floor") && m.sharedMesh)
            .OrderByDescending(m => m.GetComponent<Renderer>().bounds.size.x * m.GetComponent<Renderer>().bounds.size.z).First();
        var center = floor.GetComponent<Renderer>().bounds.center;
        if (!floor.sharedMesh.isReadable) return new Vector3(center.x, 0, center.z);
        var vertices = floor.sharedMesh.vertices;
        var triangles = floor.sharedMesh.triangles;
        Vector3 best = center;
        float distance = float.PositiveInfinity;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            var a = floor.transform.TransformPoint(vertices[triangles[i]]);
            var b = floor.transform.TransformPoint(vertices[triangles[i + 1]]);
            var c = floor.transform.TransformPoint(vertices[triangles[i + 2]]);
            if (Vector3.Cross(b - a, c - a).normalized.y < 0.5f) continue;
            var candidate = (a + b + c) / 3f;
            float score = (new Vector2(candidate.x - center.x, candidate.z - center.z)).sqrMagnitude;
            if (score >= distance) continue;
            best = candidate; distance = score;
        }
        return new Vector3(best.x, 0f, best.z);
    }

    private static string RoomName(string model)
    {
        string type = Regex.Replace(model, @"_\d+$", "");
        string number = Regex.Match(model, @"\d+$").Value;
        string title;
        switch (type)
        {
            case "Office": title = "Phòng làm việc"; break;
            case "Study_Room": title = "Phòng học"; break;
            case "Reading_Room": title = "Phòng đọc sách"; break;
            case "Bedroom": title = "Phòng ngủ"; break;
            case "Children_Room": case "Childrens_Room": title = "Phòng trẻ em"; break;
            case "Nursery_Room": title = "Phòng em bé"; break;
            case "Living_Room": title = "Phòng khách"; break;
            case "Kitchen": title = "Phòng bếp"; break;
            case "Bathoom": case "Bathroom": title = "Phòng tắm"; break;
            case "Laundry": title = "Phòng giặt"; break;
            case "Greenhouse": title = "Nhà kính"; break;
            case "Frontyard": title = "Sân vườn"; break;
            case "Salon": title = "Phòng thư giãn"; break;
            default: title = type.Replace('_', ' '); break;
        }
        return title + " · " + number;
    }

    public static Category Classify(string name)
    {
        string n = name.ToLowerInvariant();
        if (n.StartsWith("animal-")) return Category.Animals;
        if (Match(n, "lamp|light|lantern|candle|neon")) return Category.Lighting;
        if (Match(n, "bed_[0-9]|baby_bed")) return Category.Beds;
        if (Match(n, "table(_|$)")) return Category.Tables;
        if (Match(n, "chair|armchair|couch|bench|pouf|baen_bag|ottoman|padded_stool")) return Category.Seating;
        if (Match(n, "bath|washbasin|washstand|toilet|holder_with_toilet|faucet|towel|toothbrush|cream|bottle_cream|powder|perfume|stand_toilet|ladder_with_towels")) return Category.Bathroom;
        if (Match(n, "laundry|washing|drying|iron|shoes|slipper|backpack|briefcase|closed_umbrella|heating")) return Category.Laundry;
        if (Match(n, "kitchen|bottle|plate|pot_|pan_|cup|jar_|fork|spoon|cutting|kettle|teapot|coffee|fridge|gas_stove|electronic_stove|frying|bread|avocado|beet|blueberry|cake|carrot|drink|goblet|jug|ketchup|mustard|napkin|pepper|pie_|pumpkin|saucepan|strawberry|tray|turnip|wine_")) return Category.Kitchen;
        if (Match(n, "cabinet|bedside_cabinet|bookcase|shelf|dresser|wardrobe|chest|storage_box|box|basket|stand_|hanger|clothes_hanger|floor_hanger|cupboard")) return Category.Storage;
        if (Match(n, "plant|wall_plant|cactus|flower|bonsai|tree|vase_with_flower|vase_with_plant")) return Category.Plants;
        if (Match(n, "carpet|pillow|curtain|blinds|linen")) return Category.Textiles;
        if (Match(n, "painting|picture|photo|clock|old_clock|mirror|wall_decoration")) return Category.WallDecor;
        if (Match(n, "pc_|laptop|tablet|mobile_|keyboard|headphones|speaker|music_|old_radio|vinyl|tv_|camera|ventilator|guitar|computer_mat")) return Category.Electronics;
        if (Match(n, "book_|books_|openbook|diary|notebook")) return Category.Books;
        if (Match(n, "note_|notice|office_|pen|pencil|ruler|paints|brush_for_drawing|sketchbook|todo|board|moodboard|statistics")) return Category.Stationery;
        if (Match(n, "toy|ball|rubiks|bird")) return Category.Toys;
        return Category.Decorations;
    }

    private static bool Match(string value, string prefixes) => Regex.IsMatch(value, "^(" + prefixes + ")", RegexOptions.CultureInvariant);

    private static int Price(Category category)
    {
        switch (category)
        {
            case Category.Beds: return 60;
            case Category.Tables: case Category.Seating: case Category.Storage: return 30;
            case Category.Lighting: case Category.Electronics: return 20;
            case Category.Bathroom: case Category.Laundry: return 20;
            case Category.Plants: case Category.Textiles: case Category.WallDecor: return 15;
            case Category.Books: case Category.Stationery: case Category.Kitchen: return 5;
            default: return 10;
        }
    }
}
