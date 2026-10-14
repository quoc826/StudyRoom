using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class StudyRoomShopBuilder
{
    [MenuItem("StudyRoom/Create Decoration Shop")]
    public static void Build()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var loader = UnityEngine.Object.FindAnyObjectByType<StudyRoomRoomLoader>();
        if (!loader) throw new InvalidOperationException("Open the room scene first.");
        StudyRoomEditorPaths.EnsureFolder(StudyRoomEditorPaths.Catalogs);
        const string thumbnails = StudyRoomEditorPaths.Root + "/UI/Textures/Shop";
        StudyRoomEditorPaths.EnsureFolder(thumbnails);
        var catalog = AssetDatabase.LoadAssetAtPath<StudyRoomShopCatalog>(StudyRoomEditorPaths.ShopCatalog);
        if (!catalog)
        {
            catalog = ScriptableObject.CreateInstance<StudyRoomShopCatalog>();
            AssetDatabase.CreateAsset(catalog, StudyRoomEditorPaths.ShopCatalog);
            string[] models = { "Plant_on_stand_1", "Lamp_1", "Pouf_1", "Vase_with_Flowers_1", "Table_Star_1", "Toy_Bear_1", "Bookcase_1", "Carpet_1" };
            string[] ids = { "plant-stand", "floor-lamp", "cozy-pouf", "flower-vase", "star-table", "teddy-bear", "bookcase", "soft-carpet" };
            string[] names = { "Cây xanh nhỏ", "Đèn đứng ấm áp", "Ghế đôn êm", "Bình hoa xinh", "Bàn ngôi sao", "Gấu bông", "Tủ sách", "Thảm mềm" };
            string[] descriptions = { "Thêm một chút xanh vào góc học", "Một góc phòng thật ấm cúng", "Chiếc ghế nhỏ cho giờ nghỉ", "Sắc hoa cho không gian dịu dàng", "Bàn nhỏ để trang trí góc phòng", "Người bạn dễ thương khi học bài", "Sắp xếp những cuốn sách yêu thích", "Tạo một góc nghỉ ngơi thật êm" };
            int[] prices = { 10, 20, 15, 10, 25, 5, 35, 20 };
            catalog.items = new StudyRoomShopCatalog.Item[models.Length];
            var thumbnailMethod = typeof(StudyRoomStartBuilder).GetMethod("Thumbnail", BindingFlags.NonPublic | BindingFlags.Static);
            for (int i = 0; i < models.Length; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/Mnostva_Art/Prefabs/Interiors/" + models[i] + ".prefab");
                if (!prefab) throw new InvalidOperationException("Missing decoration: " + models[i]);
                var thumbnail = (Texture2D)thumbnailMethod.Invoke(null, new object[] { prefab, thumbnails + "/" + models[i] + ".png", false });
                catalog.items[i] = new StudyRoomShopCatalog.Item { id = ids[i], displayName = names[i], description = descriptions[i], price = prices[i], prefab = prefab, thumbnail = thumbnail };
            }
            EditorUtility.SetDirty(catalog);
        }
        Undo.RecordObject(loader, "Assign decoration shop");
        loader.shopCatalog = catalog;
        var ui = loader.GetComponent<StudyRoomShopUI>();
        if (!ui) ui = Undo.AddComponent<StudyRoomShopUI>(loader.gameObject);
        ui.catalog = catalog;
        ui.BuildInterface();
        if (!loader.GetComponent<StudyRoomDecorations>()) Undo.AddComponent<StudyRoomDecorations>(loader.gameObject);
        EditorUtility.SetDirty(loader); EditorUtility.SetDirty(ui);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(loader.gameObject.scene);
        if (!EditorSceneManager.SaveScene(loader.gameObject.scene)) throw new InvalidOperationException("Scene save failed.");
    }
}
