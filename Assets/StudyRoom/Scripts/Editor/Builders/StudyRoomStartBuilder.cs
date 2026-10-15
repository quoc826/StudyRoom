using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class StudyRoomStartBuilder
{
    private const string Folder = StudyRoomEditorPaths.Prefabs;

    [MenuItem("StudyRoom/Create Start Screen")]
    public static void Build()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var room = GameObject.Find("Office_1");
        var character = GameObject.Find("CuteGirl");
        var ui = GameObject.Find("StudyRoomUI");
        if (!room || !character || !ui) throw new InvalidOperationException("Room, character or existing UI missing.");
        foreach (string folder in new[] { Folder + "/Rooms", Folder + "/Characters", StudyRoomEditorPaths.Catalogs, StudyRoomEditorPaths.Thumbnails })
            StudyRoomEditorPaths.EnsureFolder(folder);

        var catalog = AssetDatabase.LoadAssetAtPath<StudyRoomCatalog>(StudyRoomEditorPaths.Catalogs + "/RoomCatalog.asset");
        bool created = !catalog;
        if (created) { catalog = ScriptableObject.CreateInstance<StudyRoomCatalog>(); AssetDatabase.CreateAsset(catalog, StudyRoomEditorPaths.Catalogs + "/RoomCatalog.asset"); }
        var furnished = SaveScenePrefab(room, Folder + "/Rooms/Office_Furnished.prefab");
        var girl = SaveScenePrefab(character, Folder + "/Characters/CuteGirl.prefab");
        if (created)
        {
            catalog.startingCoins = 0;
            var options = new List<StudyRoomCatalog.Room>();
            options.Add(new StudyRoomCatalog.Room
            {
                id = "office-furnished", displayName = "Góc học của tôi", prefab = furnished,
                price = 0, canCreate = false, spawnPosition = character.transform.position,
                thumbnail = Thumbnail(furnished, StudyRoomEditorPaths.Thumbnails + "/Office_Furnished.png", false)
            });
            string[] models = { "Office_1", "Office_24", "Office_35", "Study_Room_11" };
            string[] titles = { "Phòng nhỏ dịu dàng", "Góc làm việc mới", "Không gian sáng tạo", "Phòng học yên tĩnh" };
            for (int i = 0; i < models.Length; i++)
            {
                var prefab = EmptyRoom(models[i]);
                options.Add(new StudyRoomCatalog.Room
                {
                    id = "empty-" + models[i].ToLowerInvariant(), displayName = titles[i], prefab = prefab,
                    price = 100, canCreate = true, spawnPosition = Vector3.zero,
                    thumbnail = Thumbnail(prefab, StudyRoomEditorPaths.Thumbnails + "/" + models[i] + ".png", false)
                });
            }
            catalog.rooms = options.ToArray();
            catalog.characters = new[] { new StudyRoomCatalog.Character
            {
                id = "cute-girl", displayName = "CuteGirl", prefab = girl, price = 0,
                thumbnail = Thumbnail(girl, StudyRoomEditorPaths.Thumbnails + "/CuteGirl.png", true)
            }};
            EditorUtility.SetDirty(catalog);
        }
        AssetDatabase.SaveAssets();
        StudyRoomSceneBuilder.CreateStartScene(catalog, room, character, ui);
    }

    private static GameObject SaveScenePrefab(GameObject source, string path)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing) return existing;
        var clone = UnityEngine.Object.Instantiate(source);
        try
        {
            clone.name = source.name;
            clone.transform.position = Vector3.zero;
            clone.transform.rotation = Quaternion.identity;
            return PrefabUtility.SaveAsPrefabAsset(clone, path);
        }
        finally { UnityEngine.Object.DestroyImmediate(clone); }
    }

    public static GameObject EmptyRoom(string model)
    {
        string path = Folder + "/Rooms/Empty_" + model + ".prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing) return existing;
        var original = AssetDatabase.LoadAssetAtPath<GameObject>(StudyRoomEditorPaths.RoomModels + "/" + model + ".prefab");
        if (!original) throw new InvalidOperationException("Missing room: " + model);
        foreach (var mesh in original.GetComponentsInChildren<MeshFilter>())
        {
            if (!mesh.name.Contains("_Floor") || !mesh.sharedMesh || mesh.sharedMesh.isReadable) continue;
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(mesh.sharedMesh)) as ModelImporter;
            if (importer && !importer.isReadable) { importer.isReadable = true; importer.SaveAndReimport(); }
        }
        var clone = UnityEngine.Object.Instantiate(original);
        try
        {
            clone.name = "Empty_" + model;
            clone.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            clone.transform.localScale = Vector3.one * 0.8f;
            for (int i = clone.transform.childCount - 1; i >= 0; i--)
            {
                var child = clone.transform.GetChild(i);
                string name = child.name;
                if (!System.Text.RegularExpressions.Regex.IsMatch(name, @"_(Floor|Wall|Window|Blinds|Curtains?)(_[0-9]+)?$"))
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
            var empty = clone.GetComponent<MeshCollider>();
            if (empty && !empty.sharedMesh) UnityEngine.Object.DestroyImmediate(empty);
            foreach (var mf in clone.GetComponentsInChildren<MeshFilter>())
            {
                if (!mf.sharedMesh) continue;
                var collider = mf.GetComponent<MeshCollider>();
                if (!collider) collider = mf.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = mf.sharedMesh; collider.convex = false; collider.isTrigger = false;
            }
            AddBoundary(clone);
            return PrefabUtility.SaveAsPrefabAsset(clone, path);
        }
        finally { UnityEngine.Object.DestroyImmediate(clone); }
    }

    private static void AddBoundary(GameObject room)
    {
        MeshFilter floor = null;
        float largest = 0;
        foreach (var mf in room.GetComponentsInChildren<MeshFilter>())
        {
            if (!mf.name.Contains("_Floor") || !mf.sharedMesh) continue;
            float area = mf.sharedMesh.bounds.size.x * mf.sharedMesh.bounds.size.z;
            if (area > largest) { largest = area; floor = mf; }
        }
        if (!floor) throw new InvalidOperationException("Room has no floor.");
        var points = new List<Vector2>();
        foreach (var vertex in floor.sharedMesh.vertices)
        {
            var point = room.transform.InverseTransformPoint(floor.transform.TransformPoint(vertex));
            var p = new Vector2(point.x, point.z);
            if (!points.Exists(v => (v - p).sqrMagnitude < 0.000001f)) points.Add(p);
        }
        points.Sort((a, b) => a.x == b.x ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
        var hull = new List<Vector2>();
        foreach (var point in points)
        {
            while (hull.Count >= 2 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], point) <= 0.00001f) hull.RemoveAt(hull.Count - 1);
            hull.Add(point);
        }
        int lower = hull.Count;
        for (int i = points.Count - 2; i >= 0; i--)
        {
            while (hull.Count > lower && Cross(hull[hull.Count - 2], hull[hull.Count - 1], points[i]) <= 0.00001f) hull.RemoveAt(hull.Count - 1);
            hull.Add(points[i]);
        }
        hull.RemoveAt(hull.Count - 1);
        var root = new GameObject("InvisibleBoundaries"); root.transform.SetParent(room.transform, false);
        float y = room.transform.InverseTransformPoint(new Vector3(0, floor.GetComponent<Renderer>().bounds.max.y, 0)).y;
        for (int i = 0; i < hull.Count; i++)
        {
            var a = new Vector3(hull[i].x, y, hull[i].y);
            var b = new Vector3(hull[(i + 1) % hull.Count].x, y, hull[(i + 1) % hull.Count].y);
            var edge = b - a;
            var outward = new Vector3(edge.z, 0, -edge.x).normalized;
            var wall = new GameObject("Boundary_" + (i + 1)); wall.transform.SetParent(root.transform, false);
            wall.transform.localPosition = (a + b) * 0.5f + outward * 0.08f + Vector3.up * 3f;
            wall.transform.localRotation = Quaternion.LookRotation(edge.normalized);
            wall.AddComponent<BoxCollider>().size = new Vector3(0.20f, 6, edge.magnitude + 0.25f);
        }
    }
    private static float Cross(Vector2 a, Vector2 b, Vector2 c) => (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);

    private static Texture2D Thumbnail(GameObject prefab, string path, bool character)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing) return existing;
        var preview = new GameObject("ThumbnailPreview"); preview.SetActive(false);
        preview.transform.position = new Vector3(10000, 10000, 10000);
        RenderTexture rt = null; Texture2D image = null;
        var previous = RenderTexture.active;
        try
        {
            var clone = UnityEngine.Object.Instantiate(prefab, preview.transform);
            clone.transform.localPosition = Vector3.zero;
            foreach (var behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
            foreach (var collider in clone.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var t in clone.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
            preview.SetActive(true);
            var animator = clone.GetComponent<Animator>();
            if (animator && animator.runtimeAnimatorController) { animator.Rebind(); animator.Update(0); }
            Bounds bounds = new Bounds(); bool first = true;
            foreach (var renderer in clone.GetComponentsInChildren<Renderer>())
                if (first) { bounds = renderer.bounds; first = false; } else bounds.Encapsulate(renderer.bounds);
            var cameraObject = new GameObject("ThumbnailCamera"); cameraObject.transform.SetParent(preview.transform);
            var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0, 0, 0, 0); camera.cullingMask = 1 << 31;
            var rotation = character ? Quaternion.identity : Quaternion.Euler(35, 180, 0);
            camera.transform.SetPositionAndRotation(bounds.center - rotation * Vector3.forward * 12, rotation);
            float h = 0, w = 0;
            for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2) for (int z = -1; z <= 1; z += 2)
            {
                var p = Quaternion.Inverse(rotation) * Vector3.Scale(bounds.extents, new Vector3(x, y, z));
                h = Mathf.Max(h, Mathf.Abs(p.y)); w = Mathf.Max(w, Mathf.Abs(p.x));
            }
            camera.orthographicSize = Mathf.Max(h, w) * 1.12f;
            camera.nearClipPlane = 0.1f; camera.farClipPlane = 30;
            var lightObject = new GameObject("ThumbnailLight"); lightObject.transform.SetParent(preview.transform);
            var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.5f;
            light.cullingMask = 1 << 31; light.shadows = LightShadows.None; light.transform.rotation = Quaternion.Euler(35, -25, 0);
            rt = new RenderTexture(384, 384, 24, RenderTextureFormat.ARGB32); camera.targetTexture = rt;
            camera.Render(); RenderTexture.active = rt;
            image = new Texture2D(384, 384, TextureFormat.RGBA32, false); image.ReadPixels(new Rect(0, 0, 384, 384), 0, 0); image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(preview);
            if (image) UnityEngine.Object.DestroyImmediate(image);
            if (rt) { rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
        }
        AssetDatabase.ImportAsset(path);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
