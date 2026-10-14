using System;
using UnityEngine;

[CreateAssetMenu(menuName = "StudyRoom/Decoration shop catalog")]
public class StudyRoomShopCatalog : ScriptableObject
{
    [Serializable]
    public class Item
    {
        public string id;
        public string displayName;
        public string description;
        [Min(1)] public int price = 10;
        public GameObject prefab;
        public Texture2D thumbnail;
    }

    public Item[] items = Array.Empty<Item>();
    public Item Find(string id) => Array.Find(items, item => item != null && item.id == id);
}
