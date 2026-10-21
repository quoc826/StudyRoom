using UnityEngine;
using UnityEngine.SceneManagement;

public static class StudyRoomSession
{
    public const string StartScene = "ScreenStart";
    public const string RoomScene = "SampleScene";
    private static StudyRoomProgress.SavedRoom pendingRoom;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() => pendingRoom = null;

    public static bool OpenRoom(StudyRoomProgress.SavedRoom saved)
    {
        if (saved == null || StudyRoomLoadingScreen.IsLoading || !Application.CanStreamedLevelBeLoaded(RoomScene)) return false;
        pendingRoom = saved;
        if (StudyRoomLoadingScreen.Load(RoomScene)) return true;
        pendingRoom = null;
        return false;
    }

    public static StudyRoomProgress.SavedRoom TakeSelection()
    {
        var selected = pendingRoom;
        pendingRoom = null;
        return selected;
    }

    public static void ReturnHome()
    {
        if (StudyRoomLoadingScreen.IsLoading) return;
        if (Application.CanStreamedLevelBeLoaded(StartScene))
            StudyRoomLoadingScreen.Load(StartScene, true);
        else Debug.LogError("ScreenStart is missing from the build scene list.");
    }
}
