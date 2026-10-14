using System;
using UnityEditor;

/// <summary>Shared asset locations for the StudyRoom authoring tools.</summary>
public static class StudyRoomEditorPaths
{
    public const string Root = "Assets/StudyRoom";
    public const string Scenes = Root + "/Scenes";
    public const string Prefabs = Root + "/Prefabs";
    public const string Catalogs = Root + "/Data/Catalogs";
    public const string ShopCatalog = Catalogs + "/DecorationShopCatalog.asset";
    public const string Fonts = Root + "/UI/Fonts";
    public const string Sprites = Root + "/UI/Sprites";
    public const string Thumbnails = Root + "/UI/Textures/Thumbnails";
    public const string RenderTextures = Root + "/UI/RenderTextures";
    public const string Rendering = Root + "/Settings/Rendering";
    public const string Quality = Rendering + "/Quality";
    public const string RoomModels = "Assets/ThirdParty/Mnostva_Art/Prefabs/Rooms";

    public static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int separator = path.LastIndexOf('/');
        if (separator < 0 || !path.StartsWith("Assets/", StringComparison.Ordinal))
            throw new ArgumentException("Expected an Assets-rooted folder.", nameof(path));
        string parent = path.Substring(0, separator);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(separator + 1));
    }
}
