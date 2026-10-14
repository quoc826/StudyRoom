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
        if (saved == null || !Application.CanStreamedLevelBeLoaded(RoomScene)) return false;
        pendingRoom = saved;
        SceneManager.LoadSceneAsync(RoomScene, LoadSceneMode.Single);
        return true;
    }

    public static StudyRoomProgress.SavedRoom TakeSelection()
    {
        var selected = pendingRoom;
        pendingRoom = null;
        return selected;
    }

    public static void ReturnHome()
    {
        if (Application.CanStreamedLevelBeLoaded(StartScene))
            SceneManager.LoadSceneAsync(StartScene, LoadSceneMode.Single);
        else Debug.LogError("ScreenStart is missing from the build scene list.");
    }
}
