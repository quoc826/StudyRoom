using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>One wallet and running-time tracker across all game scenes.</summary>
public class StudyRoomEconomy : MonoBehaviour
{
    private static StudyRoomEconomy instance;
    public StudyRoomProgress Progress { get; private set; }
    public bool IsStudying { get; private set; }
    public bool CanStudy => SceneManager.GetActiveScene().name == StudyRoomSession.RoomScene;
    public event Action StateChanged;
    public event Action<int> CoinsEarned;
    private double previousTime;
    private double saveTime;
    private double refreshTime;
    private bool paused;
    private bool previousRunInBackground;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() => instance = null;

    public static StudyRoomEconomy Ensure(StudyRoomCatalog catalog)
    {
        if (instance) return instance;
        if (!catalog) throw new ArgumentNullException(nameof(catalog));
        var root = new GameObject("StudyRoomEconomy");
        instance = root.AddComponent<StudyRoomEconomy>();
        instance.Progress = new StudyRoomProgress(catalog);
        instance.Progress.Changed += instance.Notify;
        instance.Progress.VisitDay(DateTime.Now);
        instance.previousTime = Time.realtimeSinceStartupAsDouble;
        instance.previousRunInBackground = Application.runInBackground;
        // Studying alongside another desktop application should keep counting.
        Application.runInBackground = true;
        DontDestroyOnLoad(root);
        SceneManager.activeSceneChanged += instance.SceneChanged;
        return instance;
    }

    private void Update() => Tick();

    private void Tick()
    {
        double runningTime = Time.realtimeSinceStartupAsDouble;
        double elapsed = paused ? 0 : Math.Max(0, Math.Min(5, runningTime - previousTime));
        previousTime = runningTime;
        if (Progress == null) return;
        DateTime now = DateTime.Now;
        int earned = 0;
        // Allocate the few seconds around midnight to the correct day's tasks.
        double afterMidnight = now.TimeOfDay.TotalSeconds;
        if (Progress.QuestDate != now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) && elapsed > afterMidnight)
        {
            earned += Progress.AdvanceRewards(elapsed - afterMidnight, now.Date.AddTicks(-1), IsStudying && CanStudy);
            elapsed = afterMidnight;
        }
        earned += Progress.AdvanceRewards(elapsed, now, IsStudying && CanStudy);
        if (earned > 0) CoinsEarned?.Invoke(earned);
        saveTime += elapsed;
        refreshTime += elapsed;
        if (saveTime >= 30) { saveTime = 0; Progress.Flush(); }
        if (refreshTime >= 1) { refreshTime = 0; Notify(); }
    }

    public void ToggleStudy()
    {
        Tick();
        IsStudying = CanStudy && !IsStudying;
        Progress.Flush();
        Notify();
    }

    public bool ClaimQuest(int index)
    {
        Tick();
        if (!Progress.TryClaimQuest(index, DateTime.Now)) return false;
        CoinsEarned?.Invoke(StudyRoomDailyQuests.All[index].Coins);
        return true;
    }

    private void Notify() => StateChanged?.Invoke();

    private void SceneChanged(Scene previous, Scene next)
    {
        Tick();
        if (next.name != StudyRoomSession.RoomScene) IsStudying = false;
        Progress.Flush();
        Notify();
    }

    private void OnApplicationPause(bool value)
    {
        if (value) Tick();
        paused = value;
        previousTime = Time.realtimeSinceStartupAsDouble;
        if (value) Progress?.Flush();
    }

    private void OnApplicationQuit() { Tick(); Progress?.Flush(); }

    private void OnDestroy()
    {
        if (instance != this) return;
        SceneManager.activeSceneChanged -= SceneChanged;
        if (Progress != null) { Progress.Flush(); Progress.Changed -= Notify; }
        Application.runInBackground = previousRunInBackground;
        instance = null;
    }
}
