using UnityEngine;

/// <summary>Shares the project's existing soft menu click across scenes and buttons.</summary>
[RequireComponent(typeof(AudioSource), typeof(RoomAudioChannel))]
public class StudyRoomUIAudio : MonoBehaviour
{
    private static StudyRoomUIAudio instance;
    private AudioSource source;
    private AudioClip clip;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() => instance = null;

    private void Awake()
    {
        if (instance && instance != this) { Destroy(gameObject); return; }
        instance = this;
        Initialize();
        if (Application.isPlaying) DontDestroyOnLoad(gameObject);
    }

    private void Initialize()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0;
        GetComponent<RoomAudioChannel>().channel = RoomAudioChannel.Channel.Sfx;
        source.volume = RoomAudioChannel.GetVolume(RoomAudioChannel.Channel.Sfx);
        if (!clip) clip = CreateClickSound();
        source.clip = clip;
    }

    public static void PlayClick()
    {
        if (!Application.isPlaying) return;
        if (!instance) new GameObject("UI Button Audio", typeof(StudyRoomUIAudio));
        if (instance && instance.source && instance.clip) instance.source.PlayOneShot(instance.clip);
    }

    // This is the same waveform previously used by StudyRoomMenu.
    public static AudioClip CreateClickSound()
    {
        const int rate = 22050;
        float[] samples = new float[1323];
        for (int i = 0; i < samples.Length; i++)
        {
            float t = (float)i / rate;
            samples[i] = Mathf.Sin(2f * Mathf.PI * (740f - 240f * t) * t) * Mathf.Exp(-t * 65f) * 0.16f;
        }
        var clip = AudioClip.Create("Soft UI Click", samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        if (!clip) return;
        if (Application.isPlaying) Destroy(clip); else DestroyImmediate(clip);
    }
}
