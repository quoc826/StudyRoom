using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Places inventory items on the room floor and restores each room's saved layout.</summary>
public class StudyRoomDecorations : MonoBehaviour
{
    private StudyRoomRoomLoader loader;
    private StudyRoomEconomy economy;
    private Transform room, decorationRoot;
    private MeshFilter floor;
    private float floorY;
    private Vector3[] floorVertices;
    private int[] floorTriangles;
    private Renderer[] environment;
    private readonly Dictionary<string, GameObject> instances = new Dictionary<string, GameObject>();
    private GameObject preview;
    private StudyRoomShopCatalog.Item pending;
    private PlayerController player;
    private bool restorePlayer;
    public bool IsPlacing => preview;
    public bool ValidPlacement { get; private set; }
    public string RoomId => loader ? loader.ActiveSavedRoomId : null;
    public event Action<bool> PlacementFinished;

    private IEnumerator Start()
    {
        yield return null; // The room loader must finish choosing and spawning its room.
        InitializeRoom();
    }

    public bool InitializeRoom()
    {
        if (room && decorationRoot) return true;
        loader = GetComponent<StudyRoomRoomLoader>();
        if (!loader || !loader.ActiveRoom || !loader.shopCatalog) return false;
        economy = StudyRoomEconomy.Ensure(loader.catalog);
        room = loader.ActiveRoom.transform;
        float largest = 0;
        foreach (var mesh in room.GetComponentsInChildren<MeshFilter>())
        {
            var renderer = mesh.GetComponent<Renderer>();
            if (!renderer || !mesh.sharedMesh || !mesh.name.Contains("Floor")) continue;
            float area = renderer.bounds.size.x * renderer.bounds.size.z;
            if (area > largest) { largest = area; floor = mesh; }
        }
        if (!floor) { room = null; return false; }
        floorY = floor.GetComponent<Renderer>().bounds.max.y;
        floorVertices = floor.sharedMesh.vertices;
        for (int i = 0; i < floorVertices.Length; i++) floorVertices[i] = floor.transform.TransformPoint(floorVertices[i]);
        floorTriangles = floor.sharedMesh.triangles;
        environment = room.GetComponentsInChildren<Renderer>();
        decorationRoot = new GameObject("PlayerDecorations").transform;
        decorationRoot.SetParent(room, false);
        var saved = FindRoom();
        if (saved != null)
            foreach (var placement in saved.decorations)
            {
                var item = loader.shopCatalog.Find(placement.itemId);
                if (item == null || !item.prefab) continue;
                var instance = Instantiate(item.prefab, decorationRoot);
                instance.name = item.displayName;
                instance.transform.localPosition = placement.localPosition;
                instance.transform.localRotation = Quaternion.Euler(0, placement.rotation, 0);
                AddCollider(instance);
                instances[placement.instanceId] = instance;
            }
        return true;
    }

    private StudyRoomProgress.SavedRoom FindRoom()
    {
        if (!economy) return null;
        foreach (var saved in economy.Progress.SavedRooms) if (saved.id == RoomId) return saved;
        return null;
    }

    public bool BeginPlacement(string itemId)
    {
        if (!InitializeRoom() || FindRoom() == null || economy.Progress.DecorationCount(itemId) <= 0) return false;
        var item = loader.shopCatalog.Find(itemId);
        if (item == null || !item.prefab) return false;
        if (IsPlacing) CancelPlacement();
        pending = item;
        preview = Instantiate(item.prefab, decorationRoot);
        preview.name = "Decoration Preview";
        foreach (var collider in preview.GetComponentsInChildren<Collider>()) collider.enabled = false;
        foreach (var behaviour in preview.GetComponentsInChildren<MonoBehaviour>()) behaviour.enabled = false;
        player = FindAnyObjectByType<PlayerController>();
        restorePlayer = player && player.enabled;
        if (player) player.enabled = false;
        SetWorldPosition(floor.GetComponent<Renderer>().bounds.center);
        return true;
    }

    private void Update()
    {
        if (!IsPlacing || !Camera.main) return;
        var touches = StudyRoomTouchInput.Read();
        if (touches.Count >= 2) return;
        Vector2 position = default;
        bool hasPointer = false;
        if (touches.Count == 1) { position = touches.FirstPosition; hasPointer = true; }
#if ENABLE_INPUT_SYSTEM
        else if (Mouse.current != null) { position = Mouse.current.position.ReadValue(); hasPointer = true; }
#elif ENABLE_LEGACY_INPUT_MANAGER
        else { position = Input.mousePosition; hasPointer = true; }
#endif
        if (!hasPointer || StudyRoomTouchInput.IsOverUI(position)) return;
        var plane = new Plane(Vector3.up, new Vector3(0, floorY, 0));
        var ray = Camera.main.ScreenPointToRay(position);
        if (plane.Raycast(ray, out float distance)) SetWorldPosition(ray.GetPoint(distance));
    }

    public void SetWorldPosition(Vector3 position)
    {
        if (!preview) return;
        preview.transform.position = new Vector3(position.x, floorY + 0.01f, position.z);
        var bounds = BoundsOf(preview);
        preview.transform.position += Vector3.up * (floorY + 0.01f - bounds.min.y);
        ValidatePreview();
    }

    public void RotatePreview()
    {
        if (!preview) return;
        preview.transform.Rotate(0, 90, 0, Space.Self);
        ValidatePreview();
    }

    private void ValidatePreview()
    {
        var bounds = BoundsOf(preview);
        ValidPlacement = ContainsFloorPoint(bounds.center) &&
            ContainsFloorPoint(new Vector3(bounds.min.x, floorY, bounds.min.z)) &&
            ContainsFloorPoint(new Vector3(bounds.min.x, floorY, bounds.max.z)) &&
            ContainsFloorPoint(new Vector3(bounds.max.x, floorY, bounds.min.z)) &&
            ContainsFloorPoint(new Vector3(bounds.max.x, floorY, bounds.max.z));
        // Thin carpets can sit under furniture; solid furniture must not overlap.
        if (ValidPlacement && bounds.size.y > 0.1f)
        {
            foreach (var renderer in environment)
                if (renderer && renderer.gameObject != floor.gameObject && renderer.bounds.size.y > 0.1f && bounds.Intersects(renderer.bounds))
                { ValidPlacement = false; break; }
            foreach (var instance in instances.Values)
                if (instance && BoundsOf(instance).size.y > 0.1f && bounds.Intersects(BoundsOf(instance)))
                { ValidPlacement = false; break; }
            if (player && player.GetComponent<CharacterController>().bounds.Intersects(bounds)) ValidPlacement = false;
        }
        foreach (var renderer in preview.GetComponentsInChildren<Renderer>())
        {
            var block = new MaterialPropertyBlock();
            var color = ValidPlacement ? new Color(0.65f, 1f, 0.7f) : new Color(1f, 0.4f, 0.4f);
            block.SetColor("_BaseColor", color); block.SetColor("_Color", color);
            renderer.SetPropertyBlock(block);
        }
    }

    private bool ContainsFloorPoint(Vector3 point)
    {
        var p = new Vector2(point.x, point.z);
        for (int i = 0; i < floorTriangles.Length; i += 3)
        {
            var va = floorVertices[floorTriangles[i]];
            var vb = floorVertices[floorTriangles[i + 1]];
            var vc = floorVertices[floorTriangles[i + 2]];
            var a = new Vector2(va.x, va.z); var b = new Vector2(vb.x, vb.z); var c = new Vector2(vc.x, vc.z);
            float area = Cross(b - a, c - a);
            if (Mathf.Abs(area) < 0.0001f) continue;
            float u = Cross(b - p, c - p) / area;
            float v = Cross(c - p, a - p) / area;
            if (u >= -0.001f && v >= -0.001f && u + v <= 1.001f) return true;
        }
        return false;
    }

    private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

    public bool ConfirmPlacement()
    {
        if (!IsPlacing) return false;
        ValidatePreview();
        if (!ValidPlacement) return false;
        var placed = economy.Progress.TryPlaceDecoration(loader.shopCatalog, RoomId, pending.id,
            preview.transform.localPosition, preview.transform.localEulerAngles.y);
        if (placed == null) return false;
        preview.name = pending.displayName;
        foreach (var renderer in preview.GetComponentsInChildren<Renderer>()) renderer.SetPropertyBlock(null);
        foreach (var collider in preview.GetComponentsInChildren<Collider>()) collider.enabled = true;
        AddCollider(preview);
        instances[placed.instanceId] = preview;
        preview = null; pending = null;
        Finish(true);
        return true;
    }

    public bool ReturnToInventory(string instanceId)
    {
        if (!InitializeRoom() || !economy.Progress.ReturnDecoration(RoomId, instanceId)) return false;
        if (instances.TryGetValue(instanceId, out var instance))
        { RemoveObject(instance); instances.Remove(instanceId); }
        return true;
    }

    public void CancelPlacement()
    {
        if (!IsPlacing) return;
        RemoveObject(preview); preview = null; pending = null;
        Finish(false);
    }

    private void Finish(bool placed)
    {
        ValidPlacement = false;
        if (player && restorePlayer) player.enabled = true;
        restorePlayer = false;
        PlacementFinished?.Invoke(placed);
    }

    private static void RemoveObject(GameObject target)
    {
        if (Application.isPlaying) Destroy(target); else DestroyImmediate(target);
    }

    private static Bounds BoundsOf(GameObject target)
    {
        Bounds bounds = new Bounds(target.transform.position, Vector3.zero);
        bool first = true;
        foreach (var renderer in target.GetComponentsInChildren<Renderer>())
        { if (first) { bounds = renderer.bounds; first = false; } else bounds.Encapsulate(renderer.bounds); }
        return bounds;
    }

    private static void AddCollider(GameObject target)
    {
        var bounds = BoundsOf(target);
        if (bounds.size.y <= 0.1f || target.GetComponentsInChildren<Collider>().Length > 0) return;
        var collider = target.AddComponent<BoxCollider>();
        collider.center = target.transform.InverseTransformPoint(bounds.center);
        var size = target.transform.InverseTransformVector(bounds.size);
        collider.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
    }
}
