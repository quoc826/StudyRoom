using UnityEngine;

public class StudyRoomRoomLoader : MonoBehaviour
{
    public StudyRoomCatalog catalog;
    public GameObject initialRoom;
    public GameObject initialCharacter;
    public StudyRoomShopCatalog shopCatalog;
    public GameObject ActiveRoom { get; private set; }
    public string ActiveSavedRoomId { get; private set; }

    private void Start()
    {
        var selected = StudyRoomSession.TakeSelection();
        // Opening SampleScene directly in the editor retains its existing room.
        ActiveRoom = initialRoom;
        ActiveSavedRoomId = "initial-room";
        if (selected == null) return;
        var roomOption = catalog.FindRoom(selected.roomId);
        var characterOption = catalog.FindCharacter(selected.characterId);
        if (roomOption == null || characterOption == null || !roomOption.prefab || !characterOption.prefab)
        {
            Debug.LogError("The selected room or character is missing from the catalog.", this);
            return;
        }
        GameObject room, character;
        if (selected.id == "initial-room" && initialRoom && initialCharacter)
        {
            room = initialRoom;
            character = initialCharacter;
        }
        else
        {
            if (initialRoom) { initialRoom.SetActive(false); Destroy(initialRoom); }
            if (initialCharacter) { initialCharacter.SetActive(false); Destroy(initialCharacter); }
            room = Instantiate(roomOption.prefab, Vector3.zero, Quaternion.identity);
            room.name = roomOption.displayName;
            character = Instantiate(characterOption.prefab, roomOption.spawnPosition, Quaternion.identity);
            character.name = characterOption.displayName;
            var cc = character.GetComponent<CharacterController>();
            float floorY = 0;
            foreach (var renderer in room.GetComponentsInChildren<Renderer>())
                if (renderer.name.Contains("Floor_1")) { floorY = renderer.bounds.max.y; break; }
            if (cc)
            {
                cc.enabled = false;
                var position = character.transform.position;
                float feetOffset = character.transform.TransformVector(cc.center).y - cc.height * character.transform.lossyScale.y * 0.5f;
                position.y = floorY - feetOffset + 0.01f;
                character.transform.position = position;
                cc.enabled = true;
            }
        }
        GetComponent<StudyRoomMenu>().SetPlayer(character.GetComponent<PlayerController>());
        ActiveRoom = room;
        ActiveSavedRoomId = selected.id;
        FitCamera(room);
        var zoom = GetComponent<StudyRoomZoom>();
        if (zoom) zoom.CaptureDefaultView();
        Physics.SyncTransforms();
    }

    private static void FitCamera(GameObject room)
    {
        var camera = Camera.main;
        if (!camera) return;
        bool first = true;
        Bounds bounds = new Bounds();
        foreach (var renderer in room.GetComponentsInChildren<Renderer>())
            if (first) { bounds = renderer.bounds; first = false; } else bounds.Encapsulate(renderer.bounds);
        var rotation = Quaternion.Euler(35, 180, 0);
        camera.orthographic = true;
        camera.transform.SetPositionAndRotation(bounds.center - rotation * Vector3.forward * 12f, rotation);
        float height = 0, width = 0;
        for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2) for (int z = -1; z <= 1; z += 2)
        {
            var point = Quaternion.Inverse(rotation) * Vector3.Scale(bounds.extents, new Vector3(x, y, z));
            height = Mathf.Max(height, Mathf.Abs(point.y)); width = Mathf.Max(width, Mathf.Abs(point.x));
        }
        camera.orthographicSize = Mathf.Max(height, width / Mathf.Max(0.1f, camera.aspect)) * 1.12f;
    }
}
