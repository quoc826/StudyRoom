using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class StudyRoomAnimalsBuilder
{
    private const string SourceFolder = "Assets/ThirdParty/Animals/Quirky Series Ultimate/FREE/Prefabs";
    private const string OutputFolder = "Assets/StudyRoom/Prefabs/Animals";
    private const string MaterialFolder = "Assets/StudyRoom/Materials/Animals";
    private const string Thumbnails = "Assets/StudyRoom/UI/Textures/Shop/Animals";

    [MenuItem("StudyRoom/Add Imported Animals To Shop")]
    public static void Build()
    {
        StudyRoomEditorPaths.EnsureFolder(OutputFolder);
        StudyRoomEditorPaths.EnsureFolder(MaterialFolder);
        StudyRoomEditorPaths.EnsureFolder(Thumbnails);
        var shop = AssetDatabase.LoadAssetAtPath<StudyRoomShopCatalog>(StudyRoomEditorPaths.ShopCatalog);
        if (!shop) throw new InvalidOperationException("Decoration shop catalog is missing.");
        var items = new List<StudyRoomShopCatalog.Item>(shop.items);
        var thumbnailMethod = typeof(StudyRoomStartBuilder).GetMethod("Thumbnail", BindingFlags.NonPublic | BindingFlags.Static);
        var sourcePaths = AssetDatabase.FindAssets("t:Prefab", new[] { SourceFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.Substring(0, path.LastIndexOf('/')) == SourceFolder).OrderBy(path => path);
        foreach (var sourcePath in sourcePaths)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            string id = "animal-" + source.name.ToLowerInvariant();
            var root = new GameObject(source.name);
            try
            {
                var model = UnityEngine.Object.Instantiate(source, root.transform);
                model.name = "Model";
                model.transform.localPosition = Vector3.zero;
                foreach (var behaviour in model.GetComponentsInChildren<MonoBehaviour>(true))
                    UnityEngine.Object.DestroyImmediate(behaviour);
                foreach (var collider in model.GetComponentsInChildren<Collider>(true))
                    UnityEngine.Object.DestroyImmediate(collider);
                var animator = model.GetComponentInChildren<Animator>();
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind();
                animator.Play("Idle_A", 0, 0f);
                animator.Update(0f);
                var renderers = model.GetComponentsInChildren<Renderer>(true);
                Bounds bounds = GetBounds(renderers);
                float longest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
                model.transform.localScale *= 0.55f / Mathf.Max(0.01f, longest);
                bounds = GetBounds(renderers);
                model.transform.localPosition -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                foreach (var renderer in renderers)
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(ConvertMaterial).ToArray();
                foreach (var transform in root.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer = 0;
                bounds = GetBounds(renderers);
                var box = root.AddComponent<BoxCollider>();
                box.center = bounds.center;
                box.size = new Vector3(Mathf.Max(0.12f, bounds.size.x), Mathf.Max(0.12f, bounds.size.y), Mathf.Max(0.12f, bounds.size.z));
                var animal = root.AddComponent<StudyRoomAnimal>();
                animal.SetMovementAnimation(source.name == "Herring" || source.name == "Squid" ? "Swim" : "Walk");
                string prefabPath = OutputFolder + "/" + source.name + ".prefab";
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                var thumbnail = (Texture2D)thumbnailMethod.Invoke(null, new object[] { prefab, Thumbnails + "/" + source.name + ".png", false });
                var item = items.FirstOrDefault(entry => entry.id == id);
                if (item == null) { item = new StudyRoomShopCatalog.Item { id = id, price = 50 }; items.Add(item); }
                item.displayName = "Thú cưng · " + source.name;
                item.category = StudyRoomShopCatalog.Category.Animals;
                item.description = "Người bạn nhỏ đi chậm quanh phòng và dừng nghỉ. Mua rồi chọn Đặt vào phòng.";
                item.prefab = prefab;
                item.thumbnail = thumbnail;
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        shop.items = items.ToArray();
        EditorUtility.SetDirty(shop);
        AssetDatabase.SaveAssets();
    }

    private static Bounds GetBounds(Renderer[] renderers)
    {
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    private static Material ConvertMaterial(Material source)
    {
        if (!source) return null;
        string path = MaterialFolder + "/" + source.name + ".mat";
        var result = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (result) return result;
        result = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = source.name };
        if (source.HasProperty("_MainTex")) result.SetTexture("_BaseMap", source.GetTexture("_MainTex"));
        result.SetColor("_BaseColor", Color.white);
        result.SetFloat("_Smoothness", 0.15f);
        AssetDatabase.CreateAsset(result, path);
        return result;
    }
}
