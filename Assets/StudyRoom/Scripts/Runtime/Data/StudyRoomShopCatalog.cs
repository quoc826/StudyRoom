using System;
using UnityEngine;

[CreateAssetMenu(menuName = "StudyRoom/Decoration shop catalog")]
public class StudyRoomShopCatalog : ScriptableObject
{
    public enum Category
    {
        Decorations, Lighting, Beds, Tables, Seating, Storage, Plants, Textiles,
        WallDecor, Electronics, Books, Stationery, Kitchen, Bathroom, Laundry, Toys, Animals, Rooms
    }

    public static string CategoryName(Category category)
    {
        switch (category)
        {
            case Category.Lighting: return "Đèn & ánh sáng";
            case Category.Beds: return "Giường";
            case Category.Tables: return "Bàn";
            case Category.Seating: return "Ghế & sofa";
            case Category.Storage: return "Tủ, kệ & hộp";
            case Category.Plants: return "Cây & hoa";
            case Category.Textiles: return "Thảm, gối & rèm";
            case Category.WallDecor: return "Tranh, gương & đồng hồ";
            case Category.Electronics: return "Điện tử & âm nhạc";
            case Category.Books: return "Sách";
            case Category.Stationery: return "Đồ học tập";
            case Category.Kitchen: return "Đồ bếp & thức ăn";
            case Category.Bathroom: return "Phòng tắm";
            case Category.Laundry: return "Giặt giũ & cá nhân";
            case Category.Toys: return "Đồ chơi";
            case Category.Animals: return "Thú cưng";
            case Category.Rooms: return "Phòng mới";
            default: return "Đồ trang trí";
        }
    }
    [Serializable]
    public class Item
    {
        public string id;
        public string displayName;
        public string description;
        [Min(1)] public int price = 10;
        public GameObject prefab;
        public Texture2D thumbnail;
        public Category category;
    }

    public Item[] items = Array.Empty<Item>();
    public Item Find(string id) => Array.Find(items, item => item != null && item.id == id);
}
