using UnityEngine;

/// <summary>Shared music source with a fade when starting Home or a room.</summary>
public class StudyRoomBackgroundMusic : MonoBehaviour
{
    private static StudyRoomBackgroundMusic instance;
    private AudioSource source;
    private float elapsed;
    private const float FadeDuration = 5f;

    public static AudioSource Ensure(AudioClip defaultClip)
    {
        if (instance) return instance.source;
        var host = new GameObject("Background Music");
        instance = host.AddComponent<StudyRoomBackgroundMusic>();
        DontDestroyOnLoad(host);
        instance.source = host.AddComponent<AudioSource>();
        instance.source.playOnAwake = false;
        instance.source.loop = true;
        instance.source.spatialBlend = 0f;
        instance.source.volume = 0f;
        instance.source.clip = defaultClip;
        if (defaultClip) instance.source.Play();
        return instance.source;
    }

    public static void RestartDefault(AudioClip defaultClip)
    {
        bool alreadyExists = instance;
        var audio = Ensure(defaultClip);
        if (!alreadyExists || !defaultClip) return;
        audio.Stop();
        audio.clip = defaultClip;
        audio.loop = true;
        // Explicit scene entry restarts with a fade; automatic loops do not.
        instance.elapsed = 0f;
        audio.volume = 0f;
        audio.Play();
    }

    private void Update()
    {
        // Fade after an explicit start; AudioSource handles loops without resetting it.
        elapsed = Mathf.Min(FadeDuration, elapsed + Time.unscaledDeltaTime);
        if (source)
            source.volume = Mathf.SmoothStep(0f, 1f, elapsed / FadeDuration)
                * RoomAudioChannel.GetVolume(RoomAudioChannel.Channel.BackgroundMusic);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
