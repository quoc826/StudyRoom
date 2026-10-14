using System;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class RoomAudioChannel : MonoBehaviour
{
    public enum Channel { Sfx, BackgroundMusic }
    public Channel channel;
    [Range(0f, 1f)] public float baseVolume = 1f;
    public static event Action VolumesChanged;
    private AudioSource source;

    public static float GetVolume(Channel channel) =>
        Mathf.Clamp01(PlayerPrefs.GetFloat(channel == Channel.Sfx ? "StudyRoom.Sfx" : "StudyRoom.Music", 0.7f));

    public static void SetVolume(Channel channel, float volume)
    {
        PlayerPrefs.SetFloat(channel == Channel.Sfx ? "StudyRoom.Sfx" : "StudyRoom.Music", Mathf.Clamp01(volume));
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
