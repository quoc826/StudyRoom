using UnityEngine;

public static class StudyRoomSeating
{
    public static bool IsSeat(Transform target) => Matches(target, false);
    public static bool IsLargeSeat(Transform target) => Matches(target, true);

    private static bool Matches(Transform target, bool largeOnly)
    {
        if (MatchesName(target.name, largeOnly)) return true;
        // Shop instances have localized display names; their mesh names retain the furniture type.
        bool found = false;
        foreach (var mesh in target.GetComponentsInChildren<MeshFilter>())
        {
            if (!mesh.sharedMesh) continue;
            if (!MatchesName(mesh.sharedMesh.name, largeOnly)) return false;
            found = true;
        }
        return found;
    }

    private static bool MatchesName(string name, bool largeOnly)
    {
        name = name.ToLowerInvariant();
        return name.Contains("sofa") || name.Contains("couch") || name.Contains("armchair") ||
            name.Contains("bean_bag") || name.Contains("baen_bag") ||
            (!largeOnly && (name.Contains("chair") || name.Contains("bench") ||
                name.Contains("stool") || name.Contains("pouf") || name.Contains("ottoman")));
    }
}
