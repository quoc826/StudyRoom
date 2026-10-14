using System;
using UnityEngine;

[CreateAssetMenu(menuName = "StudyRoom/Room and character catalog")]
public class StudyRoomCatalog : ScriptableObject
{
    [Serializable]
    public class Room
    {
        public string id;
        public string displayName;
        public GameObject prefab;
        public Texture2D thumbnail;
        [Min(0)] public int price;
        public bool canCreate = true;
        public Vector3 spawnPosition;
    }

    [Serializable]
    public class Character
    {
        public string id;
        public string displayName;
        public GameObject prefab;
        public Texture2D thumbnail;
        [Min(0)] public int price;
    }

    [Min(0)] public int startingCoins;
    public Room[] rooms;
    public Character[] characters;
    public string initialRoomId = "office-furnished";
    public string initialCharacterId = "cute-girl";
    public Room FindRoom(string id) => Array.Find(rooms, room => room.id == id);
    public Character FindCharacter(string id) => Array.Find(characters, character => character.id == id);
}
