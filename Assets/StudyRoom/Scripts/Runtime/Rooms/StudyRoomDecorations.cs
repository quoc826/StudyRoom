using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Places inventory items on floors or tables and restores each room's saved layout.</summary>
public class StudyRoomDecorations : MonoBehaviour
{
    private StudyRoomRoomLoader loader;
    private StudyRoomEconomy economy;
    private Transform room, decorationRoot;
    private MeshFilter floor;
    private float floorY;
    private Vector3[] floorVertices;
    private int[] floorTriangles;
    private Collider floorCollider;
    private Renderer[] environment;
    private readonly Dictionary<string, GameObject> instances = new Dictionary<string, GameObject>();
    private GameObject preview;
    private StudyRoomShopCatalog.Item pending;
    private PlayerController player;
    private bool restorePlayer;
    private readonly Dictionary<GameObject, string> builtInFurniture = new Dictionary<GameObject, string>();
    private readonly Dictionary<Renderer, MaterialPropertyBlock> selectionBlocks = new Dictionary<Renderer, MaterialPropertyBlock>();
    private GameObject selected, movingOriginal;
    private int editFinger = -1;
    private Vector2 editPress;
    private bool editGestureBlocked;
    private float movingFloorOffset;
    private bool placementUsesTouch;
    private GameObject placementTable;
    private float placementSurfaceY;
    public bool IsEditing { get; private set; }
    public GameObject SelectedFurniture => selected;
    public string SelectedFurnitureName => selected
        ? System.Text.RegularExpressions.Regex.Replace(selected.name, @"^Room_\d+_", "").Replace('_', ' ')
        : "Chạm vào đồ vật muốn thay đổi";
    public bool IsPlacing => preview;
    public bool ValidPlacement { get; private set; }
    public string RoomId => loader ? loader.ActiveSavedRoomId : null;
    public event Action<bool> PlacementFinished;
    public event Action EditStateChanged;

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
        floorCollider = floor.GetComponent<Collider>();
        if (!floorCollider) floorCollider = floor.gameObject.AddComponent<MeshCollider>();
        if (floorCollider is MeshCollider meshCollider && !meshCollider.sharedMesh) meshCollider.sharedMesh = floor.sharedMesh;
        floorVertices = Array.Empty<Vector3>();
        floorTriangles = Array.Empty<int>();
        if (floor.sharedMesh.isReadable)
        {
            floorVertices = floor.sharedMesh.vertices;
            for (int i = 0; i < floorVertices.Length; i++) floorVertices[i] = floor.transform.TransformPoint(floorVertices[i]);
            floorTriangles = floor.sharedMesh.triangles;
        }
        environment = room.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in environment)
        {
            if (!IsMovableFurniture(renderer.transform)) continue;
            var target = renderer.gameObject;
            builtInFurniture[target] = FurnitureKey(target.transform);
            AddCollider(target);
        }
        var saved = FindRoom();
        if (saved != null && saved.starterLayoutPending)
        {
            // Keep the desk seat and computer peripherals, alongside the room shell.
            var starterFurniture = new HashSet<string>(StringComparer.Ordinal)
            {
                "Room_1_Table_1", "Room_1_Chair_1", "Room_1_PC_Monitor_1",
                "Room_1_Keyboard_1", "Room_1_PC_mouse_1"
            };
            foreach (var entry in builtInFurniture)
            {
                if (starterFurniture.Contains(entry.Key.name)) continue;
                if (saved.furnitureChanges.Exists(change => change.hierarchyKey == entry.Value)) continue;
                saved.furnitureChanges.Add(new StudyRoomProgress.FurnitureChange
                {
                    hierarchyKey = entry.Value,
                    localPosition = entry.Key.transform.localPosition,
                    localRotation = entry.Key.transform.localRotation,
                    removed = true
                });
            }
            saved.starterLayoutPending = false;
            economy.Progress.Flush();
        }
        if (saved != null)
            foreach (var entry in builtInFurniture)
            {
                var change = saved.furnitureChanges.Find(item => item.hierarchyKey == entry.Value);
                if (change == null) continue;
                entry.Key.transform.localPosition = change.localPosition;
                entry.Key.transform.localRotation = change.localRotation;
                entry.Key.SetActive(!change.removed);
            }
        player = FindAnyObjectByType<PlayerController>();
        decorationRoot = new GameObject("PlayerDecorations").transform;
        decorationRoot.SetParent(room, false);
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
                var animal = instance.GetComponent<StudyRoomAnimal>();
                if (animal) animal.Initialize(this);
            }
        Physics.SyncTransforms();
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
        if (IsEditing) EndEditing();
        if (!InitializeRoom() || FindRoom() == null || economy.Progress.DecorationCount(itemId) <= 0) return false;
        var item = loader.shopCatalog.Find(itemId);
        if (item == null || !item.prefab) return false;
        if (IsPlacing) CancelPlacement();
        pending = item;
        preview = Instantiate(item.prefab, decorationRoot);
        placementUsesTouch = false;
        preview.name = "Decoration Preview";
        foreach (var collider in preview.GetComponentsInChildren<Collider>()) collider.enabled = false;
        foreach (var behaviour in preview.GetComponentsInChildren<MonoBehaviour>()) behaviour.enabled = false;
        var animal = preview.GetComponent<StudyRoomAnimal>();
        if (animal) animal.Initialize(this, false);
        player = FindAnyObjectByType<PlayerController>();
        restorePlayer = player && player.enabled;
        if (player) player.enabled = false;
        SetWorldPosition(floor.GetComponent<Renderer>().bounds.center);
        return true;
    }

    private void Update()
    {
        if (IsEditing && !IsPlacing) { UpdateEditSelection(); return; }
        if (!IsPlacing || !Camera.main) return;
        var touches = StudyRoomTouchInput.Read();
        if (touches.Count >= 2) return;
        Vector2 position = default;
        bool hasPointer = false;
        bool confirmWithMouse = false;
        if (touches.Count == 1)
        {
            placementUsesTouch = true;
            position = touches.FirstPosition; hasPointer = true;
        }
#if ENABLE_INPUT_SYSTEM
        else if (!placementUsesTouch && Mouse.current != null)
        {
            position = Mouse.current.position.ReadValue();
            hasPointer = !movingOriginal || Mouse.current.leftButton.isPressed;
            confirmWithMouse = Mouse.current.rightButton.wasPressedThisFrame;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        else if (!placementUsesTouch)
        {
            position = Input.mousePosition;
            hasPointer = !movingOriginal || Input.GetMouseButton(0);
            confirmWithMouse = Input.GetMouseButtonDown(1);
        }
#endif
        ApplyPlacementPointer(position, hasPointer, confirmWithMouse);
    }

    private void ApplyPlacementPointer(Vector2 position, bool moving, bool confirm)
    {
        if (!preview || !Camera.main || (!moving && !confirm) ||
            !Camera.main.pixelRect.Contains(position) || StudyRoomTouchInput.IsOverUI(position)) return;
        // Right-click fixes the last preview position; it must not reposition it first.
        if (confirm) { ConfirmPlacement(); return; }
        var plane = new Plane(Vector3.up, new Vector3(0, floorY, 0));
        var ray = Camera.main.ScreenPointToRay(position);
        Physics.SyncTransforms();
        var hits = Physics.RaycastAll(ray, Mathf.Infinity, ~0, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            var table = TableFor(hit.transform);
            if (!table || hit.normal.y < .9f) continue;
            PositionOnSurface(hit.point, hit.point.y, table);
            return;
        }
        if (plane.Raycast(ray, out float distance))
        {
            SetWorldPosition(ray.GetPoint(distance));
        }
    }

    public void SetWorldPosition(Vector3 position)
    {
        PositionOnSurface(position, floorY, null);
    }

    private void PositionOnSurface(Vector3 position, float surfaceY, GameObject table)
    {
        if (!preview) return;
        placementTable = table;
        placementSurfaceY = surfaceY;
        preview.transform.position = new Vector3(position.x, surfaceY + 0.01f, position.z);
        var bounds = BoundsOf(preview);
        if (movingOriginal)
            preview.transform.position += new Vector3(position.x - bounds.center.x, 0, position.z - bounds.center.z);
        preview.transform.position += Vector3.up * (surfaceY + 0.01f + (movingOriginal && !table ? movingFloorOffset : 0) - bounds.min.y);
        ValidatePreview();
    }

    private GameObject TableFor(Transform target)
    {
        for (var current = target; current && current != room; current = current.parent)
        {
            if (current.gameObject == movingOriginal || current.IsChildOf(preview.transform)) return null;
            if (!builtInFurniture.ContainsKey(current.gameObject) && InstanceId(current.gameObject) == null) continue;
            if (System.Text.RegularExpressions.Regex.IsMatch(current.name.ToLowerInvariant(),
                @"(^|[_\s\d])(table|desk)s?([_\s\d]|$)")) return current.gameObject;
            string id = InstanceId(current.gameObject);
            var saved = FindRoom();
            var placement = saved?.decorations.Find(entry => entry.instanceId == id);
            var item = placement == null ? null : loader.shopCatalog.Find(placement.itemId);
            if (item != null && item.category == StudyRoomShopCatalog.Category.Tables) return current.gameObject;
        }
        return null;
    }

    private bool FitsTable(Bounds bounds)
    {
        // Sample the whole footprint so an overhanging object cannot be confirmed.
        for (int x = -1; x <= 1; x++)
            for (int z = -1; z <= 1; z++)
            {
                var origin = new Vector3(bounds.center.x + bounds.extents.x * x,
                    placementSurfaceY + .05f, bounds.center.z + bounds.extents.z * z);
                bool supported = false;
                foreach (var collider in placementTable.GetComponentsInChildren<Collider>())
                    if (collider.enabled && !collider.isTrigger && collider.gameObject.activeInHierarchy &&
                        collider.Raycast(new Ray(origin, Vector3.down), out var hit, .1f) && hit.normal.y >= .9f &&
                        Mathf.Abs(hit.point.y - placementSurfaceY) <= .02f)
                    { supported = true; break; }
                if (!supported) return false;
            }
        return true;
    }

    public void RotatePreview()
    {
        if (!preview) return;
        // Imported room meshes can have their pivot far away from the visible furniture.
        preview.transform.RotateAround(BoundsOf(preview).center, Vector3.up, 90f);
        ValidatePreview();
    }

    private void ValidatePreview()
    {
        var animal = preview.GetComponent<StudyRoomAnimal>();
        var bounds = animal ? animal.BoundsAt(preview.transform.position) : BoundsOf(preview);
        ValidPlacement = ContainsFloorPoint(bounds.center) &&
            ContainsFloorPoint(new Vector3(bounds.min.x, floorY, bounds.min.z)) &&
            ContainsFloorPoint(new Vector3(bounds.min.x, floorY, bounds.max.z)) &&
            ContainsFloorPoint(new Vector3(bounds.max.x, floorY, bounds.min.z)) &&
            ContainsFloorPoint(new Vector3(bounds.max.x, floorY, bounds.max.z));
        if (placementTable) ValidPlacement &= !animal && FitsTable(bounds);
        // Thin carpets can sit under furniture; solid furniture must not overlap.
        if (ValidPlacement && bounds.size.y > 0.1f)
        {
            // Touching a supporting tabletop or an adjacent object is allowed.
            Bounds collisionBounds = bounds;
            collisionBounds.Expand(new Vector3(-Mathf.Min(.02f, bounds.size.x * .1f),
                -Mathf.Min(.02f, bounds.size.y * .1f), -Mathf.Min(.02f, bounds.size.z * .1f)));
            Physics.SyncTransforms();
            var overlaps = new HashSet<Collider>(Physics.OverlapBox(collisionBounds.center, collisionBounds.extents,
                Quaternion.identity, ~0, QueryTriggerInteraction.Ignore));
            foreach (var renderer in environment)
                if (renderer && renderer.enabled && renderer.gameObject.activeInHierarchy &&
                    renderer.gameObject != floor.gameObject && renderer.bounds.size.y > 0.1f &&
                    collisionBounds.Intersects(renderer.bounds) && IntersectsPlacementGeometry(renderer.gameObject, collisionBounds, overlaps))
                { ValidPlacement = false; break; }
            foreach (var instance in instances.Values)
                if (instance && instance.activeInHierarchy && instance != movingOriginal)
                {
                    var otherAnimal = instance.GetComponent<StudyRoomAnimal>();
                    Bounds other = otherAnimal ? otherAnimal.BoundsAt(instance.transform.position) : BoundsOf(instance);
                    if (other.size.y > 0.1f && collisionBounds.Intersects(other) &&
                        (otherAnimal || IntersectsPlacementGeometry(instance, collisionBounds, overlaps)))
                    { ValidPlacement = false; break; }
                }
            var controller = player ? player.GetComponent<CharacterController>() : null;
            if (controller && controller.enabled && controller.bounds.Intersects(collisionBounds)) ValidPlacement = false;
        }
        foreach (var renderer in preview.GetComponentsInChildren<Renderer>())
        {
            var block = new MaterialPropertyBlock();
            var color = ValidPlacement ? new Color(0.65f, 1f, 0.7f) : new Color(1f, 0.4f, 0.4f);
            block.SetColor("_BaseColor", color); block.SetColor("_Color", color);
            renderer.SetPropertyBlock(block);
        }
    }

    private static bool IntersectsPlacementGeometry(GameObject target, Bounds bounds, HashSet<Collider> overlaps)
    {
        bool hasCollider = false;
        foreach (var collider in target.GetComponentsInChildren<Collider>())
        {
            if (!collider.enabled || collider.isTrigger || !collider.gameObject.activeInHierarchy) continue;
            hasCollider = true;
            if (overlaps.Contains(collider)) return true;
        }
        // A joined wall mesh can surround a large empty area. Its renderer bounds
        // are only a broad-phase filter; the collider decides whether it blocks placement.
        return !hasCollider && bounds.Intersects(BoundsOf(target));
    }

    private bool ContainsFloorPoint(Vector3 point)
    {
        if (floorTriangles.Length == 0)
            return floorCollider && floorCollider.Raycast(
                new Ray(new Vector3(point.x, floorY + 1f, point.z), Vector3.down), out _, 2f);
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

    public bool CanAnimalOccupy(StudyRoomAnimal animal, Vector3 position)
        => CanAnimalMove(animal, position, position);

    public bool CanAnimalMove(StudyRoomAnimal animal, Vector3 from, Vector3 to)
    {
        if (!room || !animal) return false;
        Bounds bounds = animal.BoundsAt(to);
        Bounds previous = animal.BoundsAt(from);
        if (!ContainsFloorPoint(bounds.center)) return false;
        for (int x = -1; x <= 1; x++)
            for (int z = -1; z <= 1; z++)
                if (!ContainsFloorPoint(bounds.center + new Vector3(bounds.extents.x * x, 0f, bounds.extents.z * z)))
                    return false;
        foreach (var renderer in environment)
            if (renderer && renderer.enabled && renderer.gameObject.activeInHierarchy &&
                renderer.gameObject != floor.gameObject && renderer.bounds.size.y > 0.1f &&
                BlocksAnimalMove(previous, bounds, AnimalObstacle(renderer.bounds, StudyRoomSeating.IsSeat(renderer.transform))))
                return false;
        foreach (var instance in instances.Values)
            if (instance && instance != animal.gameObject)
            {
                var otherAnimal = instance.GetComponent<StudyRoomAnimal>();
                Bounds other = otherAnimal ? otherAnimal.BoundsAt(instance.transform.position) : BoundsOf(instance);
                if (other.size.y > 0.1f && BlocksAnimalMove(previous, bounds,
                    AnimalObstacle(other, !otherAnimal && StudyRoomSeating.IsSeat(instance.transform)))) return false;
            }
        if (player && player.GetComponent<CharacterController>().enabled &&
            BlocksAnimalMove(previous, bounds, player.GetComponent<CharacterController>().bounds)) return false;
        return true;
    }

    private static bool BlocksAnimalMove(Bounds from, Bounds to, Bounds obstacle)
    {
        if (!to.Intersects(obstacle)) return false;
        if (!from.Intersects(obstacle)) return true;
        // Existing saves can contain overlapping pets. Permit only steps that reduce the overlap.
        float before = OverlapArea(from, obstacle);
        float after = OverlapArea(to, obstacle);
        if (after < before - 0.000001f) return false;
        // A pet fully beneath a wide seat must be able to head towards its edge.
        if (Mathf.Abs(after - before) <= 0.000001f)
        {
            Vector3 oldOffset = Vector3.ProjectOnPlane(from.center - obstacle.center, Vector3.up);
            Vector3 newOffset = Vector3.ProjectOnPlane(to.center - obstacle.center, Vector3.up);
            if (newOffset.sqrMagnitude > oldOffset.sqrMagnitude + 0.000001f) return false;
        }
        return true;
    }

    private Bounds AnimalObstacle(Bounds bounds, bool seat)
    {
        if (!seat) return bounds;
        // Reserve the entire footprint, including the empty space below the cushion.
        bounds.SetMinMax(new Vector3(bounds.min.x - 0.03f, floorY - 0.1f, bounds.min.z - 0.03f),
            new Vector3(bounds.max.x + 0.03f, bounds.max.y, bounds.max.z + 0.03f));
        return bounds;
    }

    private static float OverlapArea(Bounds a, Bounds b)
    {
        float x = Mathf.Max(0f, Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x));
        float z = Mathf.Max(0f, Mathf.Min(a.max.z, b.max.z) - Mathf.Max(a.min.z, b.min.z));
        return x * z;
    }

    private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

    public bool ConfirmPlacement()
    {
        if (!IsPlacing) return false;
        ValidatePreview();
        if (!ValidPlacement) return false;
        if (movingOriginal)
        {
            var target = movingOriginal.transform;
            Vector3 localPosition = target.parent.InverseTransformPoint(preview.transform.position);
            Quaternion localRotation = Quaternion.Inverse(target.parent.rotation) * preview.transform.rotation;
            string id = InstanceId(movingOriginal);
            bool saved = id != null
                ? economy.Progress.MoveDecoration(RoomId, id, localPosition, localRotation.eulerAngles.y)
                : economy.Progress.ChangeBuiltInFurniture(RoomId, builtInFurniture[movingOriginal], localPosition, localRotation, false);
            if (!saved) return false;
            target.SetPositionAndRotation(preview.transform.position, preview.transform.rotation);
            movingOriginal.SetActive(true);
            RemoveObject(preview); preview = null; movingOriginal = null;
            HighlightSelection();
            Physics.SyncTransforms();
            Finish(true);
            return true;
        }
        var placed = economy.Progress.TryPlaceDecoration(loader.shopCatalog, RoomId, pending.id,
            preview.transform.localPosition, preview.transform.localEulerAngles.y);
        if (placed == null) return false;
        preview.name = pending.displayName;
        foreach (var renderer in preview.GetComponentsInChildren<Renderer>()) renderer.SetPropertyBlock(null);
        foreach (var collider in preview.GetComponentsInChildren<Collider>()) collider.enabled = true;
        AddCollider(preview);
        instances[placed.instanceId] = preview;
        var animal = preview.GetComponent<StudyRoomAnimal>();
        if (animal) animal.Initialize(this);
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
        if (movingOriginal) { movingOriginal.SetActive(true); movingOriginal = null; HighlightSelection(); }
        Finish(false);
    }

    private void Finish(bool placed)
    {
        ValidPlacement = false;
        if (!IsEditing)
        {
            if (player && restorePlayer) player.enabled = true;
            restorePlayer = false;
        }
        PlacementFinished?.Invoke(placed);
        EditStateChanged?.Invoke();
    }

    private static void RemoveObject(GameObject target)
    {
        target.SetActive(false);
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

    public bool BeginEditing()
    {
        if (IsEditing) return true;
        if (!InitializeRoom() || FindRoom() == null) return false;
        if (IsPlacing) CancelPlacement();
        player = FindAnyObjectByType<PlayerController>();
        restorePlayer = player && player.enabled;
        if (player) player.enabled = false;
        IsEditing = true;
        editFinger = -1;
        EditStateChanged?.Invoke();
        return true;
    }

    public void EndEditing()
    {
        if (!IsEditing) return;
        if (IsPlacing) CancelPlacement();
        ClearSelectionHighlight(); selected = null;
        IsEditing = false; editFinger = -1;
        if (player && restorePlayer) player.enabled = true;
        restorePlayer = false;
        EditStateChanged?.Invoke();
    }

    public bool SelectFurniture(Transform target)
    {
        if (!IsEditing || IsPlacing) return false;
        GameObject found = null;
        for (var current = target; current && current != room; current = current.parent)
            if (InstanceId(current.gameObject) != null || builtInFurniture.ContainsKey(current.gameObject))
            { found = current.gameObject; break; }
        ClearSelectionHighlight(); selected = found;
        HighlightSelection();
        EditStateChanged?.Invoke();
        return selected;
    }

    public bool SelectFurnitureFromRay(Ray ray)
    {
        var hits = Physics.RaycastAll(ray, Mathf.Infinity, ~0, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            if (SelectFurniture(hit.transform)) return true;
            var renderer = hit.transform.GetComponent<Renderer>();
            if (renderer && renderer.enabled && renderer.gameObject.activeInHierarchy) break;
        }
        return false;
    }

    public bool MoveSelectedFurniture()
    {
        if (!IsEditing || IsPlacing || !selected) return false;
        if (player) player.ReleaseFurniture(selected.transform);
        ClearSelectionHighlight();
        movingOriginal = selected;
        placementTable = null;
        placementUsesTouch = false;
        movingFloorOffset = InstanceId(selected) == null
            ? Mathf.Max(0, BoundsOf(selected).min.y - floorY - .01f) : 0;
        preview = Instantiate(selected, selected.transform.parent);
        preview.name = "Furniture Move Preview";
        foreach (var collider in preview.GetComponentsInChildren<Collider>()) collider.enabled = false;
        foreach (var behaviour in preview.GetComponentsInChildren<MonoBehaviour>()) behaviour.enabled = false;
        var animal = preview.GetComponent<StudyRoomAnimal>(); if (animal) animal.Initialize(this, false);
        selected.SetActive(false);
        ValidatePreview();
        EditStateChanged?.Invoke();
        return true;
    }

    public bool DeleteSelectedFurniture()
    {
        if (!IsEditing || IsPlacing || !selected) return false;
        string id = InstanceId(selected);
        bool saved = id != null ? economy.Progress.ReturnDecoration(RoomId, id)
            : economy.Progress.ChangeBuiltInFurniture(RoomId, builtInFurniture[selected], selected.transform.localPosition, selected.transform.localRotation, true);
        if (!saved) return false;
        if (player) player.ReleaseFurniture(selected.transform);
        ClearSelectionHighlight();
        if (id != null) { instances.Remove(id); RemoveObject(selected); }
        else selected.SetActive(false);
        selected = null;
        Physics.SyncTransforms();
        EditStateChanged?.Invoke();
        return true;
    }

    private string InstanceId(GameObject target)
    {
        foreach (var entry in instances) if (entry.Value == target) return entry.Key;
        return null;
    }

    private string FurnitureKey(Transform target)
    {
        string key = "";
        for (var current = target; current && current != room; current = current.parent)
            key = current.GetSiblingIndex() + "/" + key;
        return key;
    }

    private bool IsMovableFurniture(Transform target)
    {
        if (target == room || target == floor.transform) return false;
        string name = target.name.ToLowerInvariant();
        foreach (string part in new[] { "floor", "wall", "ceiling", "window", "door", "stair", "fence", "boundary", "roof", "baseboard" })
            if (System.Text.RegularExpressions.Regex.IsMatch(name, @"(^|[_\s\d])" + part + @"s?([_\s\d]|$)")) return false;
        return true;
    }

    private void ClearSelectionHighlight()
    {
        foreach (var entry in selectionBlocks) if (entry.Key) entry.Key.SetPropertyBlock(entry.Value);
        selectionBlocks.Clear();
    }

    private void HighlightSelection()
    {
        if (!selected) return;
        foreach (var renderer in selected.GetComponentsInChildren<Renderer>())
        {
            var original = new MaterialPropertyBlock(); renderer.GetPropertyBlock(original);
            selectionBlocks[renderer] = original;
            var tint = new MaterialPropertyBlock(); renderer.GetPropertyBlock(tint);
            tint.SetColor("_BaseColor", new Color(1, .84f, .48f)); tint.SetColor("_Color", new Color(1, .84f, .48f));
            renderer.SetPropertyBlock(tint);
        }
    }

    private void UpdateEditSelection()
    {
        if (!Camera.main) return;
        var touches = StudyRoomTouchInput.Read();
        if (touches.Count >= 2) { editFinger = -1; return; }
        if (touches.Count == 1)
        {
            if (editFinger < 0)
            {
                editFinger = touches.FirstId; editPress = touches.FirstPosition;
                editGestureBlocked = StudyRoomTouchInput.IsOverUI(editPress);
            }
            if (editFinger != touches.FirstId || Vector2.Distance(editPress, touches.FirstPosition) > 20) editGestureBlocked = true;
            return;
        }
        if (editFinger >= 0)
        {
            editFinger = -1;
            if (!editGestureBlocked && !StudyRoomTouchInput.IsOverUI(editPress))
                SelectFurnitureFromRay(Camera.main.ScreenPointToRay(editPress));
            return;
        }
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            var point = Mouse.current.position.ReadValue();
            if (!StudyRoomTouchInput.IsOverUI(point)) SelectFurnitureFromRay(Camera.main.ScreenPointToRay(point));
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButtonDown(0) && !StudyRoomTouchInput.IsOverUI(Input.mousePosition))
            SelectFurnitureFromRay(Camera.main.ScreenPointToRay(Input.mousePosition));
#endif
    }

    private void OnDisable() => EndEditing();
}
