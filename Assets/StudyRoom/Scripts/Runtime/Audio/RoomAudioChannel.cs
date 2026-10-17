using System;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class RoomAudioChannel : MonoBehaviour
{
    public enum Channel { Sfx, BackgroundMusic, Voice }
    public Channel channel;
    [Range(0f, 1f)] public float baseVolume = 1f;
    public static event Action VolumesChanged;
    private AudioSource source;

    public static float GetVolume(Channel channel) =>
        Mathf.Clamp01(PlayerPrefs.GetFloat(PreferenceKey(channel), 0.7f));

    private static string PreferenceKey(Channel channel) => channel == Channel.Voice
        ? "StudyRoom.Voice" : channel == Channel.Sfx ? "StudyRoom.Sfx" : "StudyRoom.Music";

    public void SetChannel(Channel value)
    {
        channel = value;
        source = GetComponent<AudioSource>();
        ApplyVolume();
    }

    public static void SetVolume(Channel channel, float volume)
    {
        PlayerPrefs.SetFloat(PreferenceKey(channel), Mathf.Clamp01(volume));
        VolumesChanged?.Invoke();
    }

    private void OnEnable()
    {
        source = GetComponent<AudioSource>();
        VolumesChanged += ApplyVolume;
        ApplyVolume();
    }

    private void OnDisable() => VolumesChanged -= ApplyVolume;
    private void ApplyVolume() { if (source) source.volume = baseVolume * GetVolume(channel); }
}
