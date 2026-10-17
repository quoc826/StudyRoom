using System;
using System.Collections.Generic;
using UnityEngine;

public class StudyRoomProgress
{
    [Serializable]
    public class PlacedDecoration
    {
        public string instanceId;
        public string itemId;
        public Vector3 localPosition;
        public float rotation;
    }

    [Serializable]
    private class DecorationStock
    {
        public string itemId;
        public int count;
    }

    [Serializable]
    public class FurnitureChange
    {
        public string hierarchyKey;
        public Vector3 localPosition;
        public Quaternion localRotation = Quaternion.identity;
        public bool removed;
    }

    [Serializable]
    public class SavedRoom
    {
        public string id;
        public string name;
        public string roomId;
        public string characterId;
        public bool starterLayoutPending;
        public List<PlacedDecoration> decorations = new List<PlacedDecoration>();
        public List<FurnitureChange> furnitureChanges = new List<FurnitureChange>();
    }

    [Serializable]
    private class DailyRewards
    {
        public string date;
        public bool visited;
        public double onlineSeconds;
        public double studySeconds;
        public bool[] claimed = new bool[5];
    }

    [Serializable]
    private class Data
    {
        public int version = 3;
        public int coins;
        public List<string> unlockedRooms = new List<string>();
        public List<string> unlockedCharacters = new List<string>();
        public List<SavedRoom> savedRooms = new List<SavedRoom>();
        public double rewardSeconds;
        public DailyRewards daily = new DailyRewards();
        public List<DecorationStock> decorationStock = new List<DecorationStock>();
    }

    private readonly StudyRoomCatalog catalog;
    private readonly string storageKey;
    private Data data;
    public int Coins => data.coins;
    public IReadOnlyList<SavedRoom> SavedRooms => data.savedRooms;
    public event Action Changed;
    public event Action<int> QuestCompleted;
    public const double CoinIntervalSeconds = 15 * 60;
    public double NextCoinSeconds => CoinIntervalSeconds - data.rewardSeconds;
    public double DailyOnlineSeconds => data.daily.onlineSeconds;
    public double DailyStudySeconds => data.daily.studySeconds;
    public string QuestDate => data.daily.date;

    public StudyRoomProgress(StudyRoomCatalog catalog, string storageKey = "StudyRoom.Progress.v1")
    {
        this.catalog = catalog;
        this.storageKey = storageKey;
        try { data = JsonUtility.FromJson<Data>(PlayerPrefs.GetString(storageKey, "")); }
        catch (ArgumentException) { data = null; }
        if (data == null) data = new Data { coins = catalog.startingCoins };
        data.coins = Mathf.Max(0, data.coins);
        if (data.unlockedRooms == null) data.unlockedRooms = new List<string>();
        if (data.unlockedCharacters == null) data.unlockedCharacters = new List<string>();
        if (data.savedRooms == null) data.savedRooms = new List<SavedRoom>();
        data.version = 3;
        if (data.decorationStock == null) data.decorationStock = new List<DecorationStock>();
        foreach (var room in data.savedRooms)
        {
            if (room.decorations == null) room.decorations = new List<PlacedDecoration>();
            if (room.furnitureChanges == null) room.furnitureChanges = new List<FurnitureChange>();
            // Older saves consumed stock on placement; placed items still grant ownership.
            foreach (var placed in room.decorations)
                Stock(placed.itemId).count = 1;
        }
        foreach (var stock in data.decorationStock)
            stock.count = stock.count > 0 ? 1 : 0;
        if (data.daily == null) data.daily = new DailyRewards();
        if (data.daily.claimed == null || data.daily.claimed.Length != StudyRoomDailyQuests.All.Count)
            Array.Resize(ref data.daily.claimed, StudyRoomDailyQuests.All.Count);
        data.rewardSeconds = SafeSeconds(data.rewardSeconds) % CoinIntervalSeconds;
        data.daily.onlineSeconds = SafeSeconds(data.daily.onlineSeconds);
        data.daily.studySeconds = SafeSeconds(data.daily.studySeconds);
        // Only newly granted starter rooms receive the minimal furniture layout.
        if (data.savedRooms.Count == 0 && catalog.FindRoom(catalog.initialRoomId) != null && catalog.FindCharacter(catalog.initialCharacterId) != null)
            data.savedRooms.Add(new SavedRoom { id = "initial-room", name = "Góc học của tôi", roomId = catalog.initialRoomId, characterId = catalog.initialCharacterId, starterLayoutPending = true });
        Save();
    }

    public bool RoomUnlocked(string id)
    {
        var room = catalog.FindRoom(id);
        return room != null && (room.price <= 0 || data.unlockedRooms.Contains(id));
    }

    public bool CharacterUnlocked(string id)
    {
        var character = catalog.FindCharacter(id);
        return character != null && (character.price <= 0 || data.unlockedCharacters.Contains(id));
    }

    public bool TryUnlockRoom(string id) => Unlock(id, false);
    public bool TryUnlockCharacter(string id) => Unlock(id, true);

    private bool Unlock(string id, bool character)
    {
        var room = character ? null : catalog.FindRoom(id);
        var actor = character ? catalog.FindCharacter(id) : null;
        if (room == null && actor == null) return false;
        if (character ? CharacterUnlocked(id) : RoomUnlocked(id)) return true;
        int price = Mathf.Max(0, character ? actor.price : room.price);
        if (data.coins < price) return false;
        data.coins -= price;
        (character ? data.unlockedCharacters : data.unlockedRooms).Add(id);
        Save();
        return true;
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        data.coins = (int)Math.Min(int.MaxValue, (long)data.coins + amount);
        Save();
    }

    public void VisitDay(DateTime now)
    {
        string date = now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        // Returning the device clock to an earlier date cannot reopen claimed quests.
        if (string.IsNullOrEmpty(data.daily.date) || string.CompareOrdinal(date, data.daily.date) > 0)
            data.daily = new DailyRewards { date = date };
        else if (data.daily.visited) return;
        data.daily.visited = true;
        Save();
        QuestCompleted?.Invoke(0);
    }

    /// <summary>Accepts elapsed running time, never offline wall-clock time.</summary>
    public int AdvanceRewards(double seconds, DateTime now, bool studying)
    {
        VisitDay(now);
        seconds = SafeSeconds(seconds);
        if (seconds <= 0) return 0;
        double previousOnline = data.daily.onlineSeconds;
        double previousStudy = data.daily.studySeconds;
        data.daily.onlineSeconds += seconds;
        if (studying) data.daily.studySeconds += seconds;
        double total = data.rewardSeconds + seconds;
        double intervals = Math.Floor(total / CoinIntervalSeconds);
        data.rewardSeconds = total % CoinIntervalSeconds;
        int earned = (int)Math.Min(int.MaxValue, intervals * 5);
        if (earned > 0)
        {
            data.coins = (int)Math.Min(int.MaxValue, (long)data.coins + earned);
            // Commit the timer and coins together so reloading cannot repeat a payout.
            Save();
        }
        for (int i = 0; i < StudyRoomDailyQuests.All.Count; i++)
        {
            var quest = StudyRoomDailyQuests.All[i];
            double before = quest.Type == StudyRoomDailyQuests.Goal.StudyTime ? previousStudy : previousOnline;
            if (quest.Type != StudyRoomDailyQuests.Goal.Visit && before < quest.TargetSeconds &&
                QuestProgress(i) >= quest.TargetSeconds) QuestCompleted?.Invoke(i);
        }
        return earned;
    }

    public double QuestProgress(int index)
    {
        if (index < 0 || index >= StudyRoomDailyQuests.All.Count) return 0;
        var quest = StudyRoomDailyQuests.All[index];
        double value = quest.Type == StudyRoomDailyQuests.Goal.Visit ? (data.daily.visited ? 1 : 0) :
            quest.Type == StudyRoomDailyQuests.Goal.StudyTime ? data.daily.studySeconds : data.daily.onlineSeconds;
        return Math.Min(quest.TargetSeconds, value);
    }

    public bool QuestClaimed(int index) => index >= 0 && index < data.daily.claimed.Length && data.daily.claimed[index];

    public bool TryClaimQuest(int index, DateTime now)
    {
        VisitDay(now);
        if (index < 0 || index >= StudyRoomDailyQuests.All.Count || QuestClaimed(index) ||
            QuestProgress(index) < StudyRoomDailyQuests.All[index].TargetSeconds) return false;
        data.daily.claimed[index] = true;
        data.coins = (int)Math.Min(int.MaxValue, (long)data.coins + StudyRoomDailyQuests.All[index].Coins);
        Save();
        return true;
    }

    public void Flush() => Save();

    public int DecorationCount(string id) => data.decorationStock.Find(item => item.itemId == id)?.count ?? 0;

    public int OwnedDecorationCount(string id) => DecorationCount(id);

    public bool TryBuyDecoration(StudyRoomShopCatalog shop, string id)
    {
        var item = shop ? shop.Find(id) : null;
        if (item == null || !item.prefab || item.price <= 0 || data.coins < item.price || OwnedDecorationCount(id) > 0) return false;
        data.coins -= item.price;
        Stock(id).count = 1;
        Save();
        return true;
    }

    public PlacedDecoration TryPlaceDecoration(StudyRoomShopCatalog shop, string roomId, string itemId, Vector3 position, float rotation)
    {
        var item = shop ? shop.Find(itemId) : null;
        var room = data.savedRooms.Find(saved => saved.id == roomId);
        if (room == null || item == null || !item.prefab || DecorationCount(itemId) <= 0 ||
            !Finite(position.x) || !Finite(position.y) || !Finite(position.z) || !Finite(rotation)) return null;
        var placed = new PlacedDecoration { instanceId = Guid.NewGuid().ToString("N"), itemId = itemId, localPosition = position, rotation = rotation };
        room.decorations.Add(placed);
        Save();
        return placed;
    }

    public bool ReturnDecoration(string roomId, string instanceId)
    {
        var room = data.savedRooms.Find(saved => saved.id == roomId);
        var placed = room?.decorations.Find(item => item.instanceId == instanceId);
        if (placed == null) return false;
        room.decorations.Remove(placed);
        Stock(placed.itemId).count = 1;
        Save();
        return true;
    }

    public bool MoveDecoration(string roomId, string instanceId, Vector3 position, float rotation)
    {
        var placed = data.savedRooms.Find(room => room.id == roomId)?.decorations.Find(item => item.instanceId == instanceId);
        if (placed == null || !Finite(position.x) || !Finite(position.y) || !Finite(position.z) || !Finite(rotation)) return false;
        placed.localPosition = position;
        placed.rotation = rotation;
        Save();
        return true;
    }

    public bool ChangeBuiltInFurniture(string roomId, string key, Vector3 position, Quaternion rotation, bool removed)
    {
        var room = data.savedRooms.Find(saved => saved.id == roomId);
        if (room == null || string.IsNullOrEmpty(key) || !Finite(position.x) || !Finite(position.y) || !Finite(position.z) ||
            !Finite(rotation.x) || !Finite(rotation.y) || !Finite(rotation.z) || !Finite(rotation.w)) return false;
        var change = room.furnitureChanges.Find(item => item.hierarchyKey == key);
        if (change == null) { change = new FurnitureChange { hierarchyKey = key }; room.furnitureChanges.Add(change); }
        change.localPosition = position;
        change.localRotation = rotation;
        change.removed = removed;
        Save();
        return true;
    }

    private DecorationStock Stock(string id)
    {
        var stock = data.decorationStock.Find(item => item.itemId == id);
        if (stock != null) return stock;
        stock = new DecorationStock { itemId = id };
        data.decorationStock.Add(stock);
        return stock;
    }

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    private static double SafeSeconds(double value) => double.IsNaN(value) || double.IsInfinity(value) ? 0 : Math.Max(0, value);

    public SavedRoom CreateRoom(string roomId, string characterId, string roomName = null)
    {
        var room = catalog.FindRoom(roomId);
        if (room == null || !room.canCreate || !RoomUnlocked(roomId) || !CharacterUnlocked(characterId)) return null;
        var saved = new SavedRoom
        {
            id = Guid.NewGuid().ToString("N"),
            name = string.IsNullOrWhiteSpace(roomName) ? "Phòng của tôi " + data.savedRooms.Count : roomName.Trim(),
            roomId = roomId,
            characterId = characterId
        };
        data.savedRooms.Add(saved);
        Save();
        return saved;
    }

    private void Save()
    {
        PlayerPrefs.SetString(storageKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
