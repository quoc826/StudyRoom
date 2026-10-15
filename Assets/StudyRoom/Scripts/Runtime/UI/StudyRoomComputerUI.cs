using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>A shared desktop and music player for room monitors and laptops.</summary>
public class StudyRoomComputerUI : MonoBehaviour
{
    public AudioClip[] tracks;
    private GameObject desktop, musicWindow, audioObject;
    private AudioSource source;
    private PlayerController player;
    private bool restorePlayer, paused;
    private int trackIndex;
    private Font font;
    private Text trackTitle, playLabel, clock, timer, volumeLabel;
    private Slider progress, volume;
    private AudioClip[] generatedTracks;
    private readonly Color ink = new Color32(228, 237, 247, 255);
    private readonly Color panel = new Color32(30, 43, 63, 255);
    private readonly Color mint = new Color32(149, 229, 202, 255);
    public bool IsOpen => desktop && desktop.activeSelf;
    public bool IsPlaying => source && source.isPlaying;

    public static bool IsComputer(Transform target)
    {
        if (!target) return false;
        if (ComputerName(target.name)) return true;
        var mesh = target.GetComponent<MeshFilter>();
        return mesh && mesh.sharedMesh && ComputerName(mesh.sharedMesh.name);
    }

    private static bool ComputerName(string name)
    {
        name = name.ToLowerInvariant();
        return name.Contains("monitor") || name.Contains("laptop") || name.Contains("computer_screen");
    }

    public bool TryOpen(Transform target)
    {
        // Inspect only the hit object and its direct mesh, not every monitor in the room.
        if (!IsComputer(target)) return false;
        Open();
        return IsOpen;
    }

    public void Open()
    {
        if (!desktop) BuildInterface();
        if (!desktop || IsOpen) return;
        var furniture = GetComponent<StudyRoomDecorations>(); if (furniture && furniture.IsEditing) furniture.EndEditing();
        var shop = GetComponent<StudyRoomShopUI>();
        if (shop && shop.IsPlacing) shop.CancelPlacement();
        if (shop && shop.IsOpen) shop.Close();
        var rewards = GetComponent<StudyRoomRewardsUI>();
        if (rewards && rewards.IsOpen) rewards.Close();
        player = FindAnyObjectByType<PlayerController>();
        restorePlayer = player && player.enabled;
        if (player) player.enabled = false;
        desktop.SetActive(true);
    }

    public void Close()
    {
        if (desktop) desktop.SetActive(false);
        if (player && restorePlayer) player.enabled = true;
        restorePlayer = false;
    }

    public void OpenMusic()
    {
        EnsureAudio();
        musicWindow.SetActive(true);
        Refresh();
    }

    private void EnsureAudio()
    {
        if (source) return;
        audioObject = new GameObject("Computer Music", typeof(AudioSource));
        audioObject.transform.SetParent(transform, false);
        source = audioObject.GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = RoomAudioChannel.GetVolume(RoomAudioChannel.Channel.BackgroundMusic);
        RoomAudioChannel.VolumesChanged += SyncVolume;
        if (tracks == null || tracks.Length == 0)
        {
            generatedTracks = new[] { CreateStudyLoop("Góc học yên tĩnh", 0), CreateStudyLoop("Chiều thư giãn", 5) };
            tracks = generatedTracks;
        }
        SelectTrack(0, false);
    }

    public void TogglePlayback()
    {
        EnsureAudio();
        if (!source.clip) return;
        if (source.isPlaying) { source.Pause(); paused = true; }
        else if (paused) { source.UnPause(); paused = false; }
        else source.Play();
        Refresh();
    }

    public void NextTrack(int direction)
    {
        EnsureAudio();
        SelectTrack((trackIndex + direction + tracks.Length) % tracks.Length, true);
    }

    private void SelectTrack(int index, bool play)
    {
        trackIndex = index;
        source.Stop();
        source.clip = tracks[index];
        paused = false;
        if (play && source.clip) source.Play();
        Refresh();
    }

    private void SyncVolume()
    {
        float value = RoomAudioChannel.GetVolume(RoomAudioChannel.Channel.BackgroundMusic);
        if (source) source.volume = value;
        if (volume) volume.SetValueWithoutNotify(value);
        if (volumeLabel) volumeLabel.text = "Âm lượng  " + Mathf.RoundToInt(value * 100) + "%";
    }

    private void Update()
    {
        if (IsOpen) Refresh();
    }

    private void Refresh()
    {
        if (clock) clock.text = DateTime.Now.ToString("HH:mm");
        if (!trackTitle) return;
        trackTitle.text = source && source.clip ? source.clip.name : "Chọn một bản nhạc để bắt đầu";
        playLabel.text = IsPlaying ? "Tạm dừng" : "Phát nhạc";
        float length = source && source.clip ? source.clip.length : 0f;
        float time = source && source.clip ? source.time : 0f;
        timer.text = Duration(time) + "  /  " + Duration(length) + "   ·   Lặp lại";
        progress.SetValueWithoutNotify(length > 0 ? time / length : 0f);
    }

    private static string Duration(float seconds) => ((int)seconds / 60).ToString("00") + ":" + ((int)seconds % 60).ToString("00");

    public void BuildInterface()
    {
        if (desktop) return;
        var canvas = transform.Find("Canvas");
        if (!canvas) return;
        var reference = canvas.GetComponentInChildren<Text>(true);
        font = reference && reference.font ? reference.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var root = Box(canvas, "ComputerDesktop", Vector2.zero, Vector2.one, new Color32(12, 21, 37, 255));
        desktop = root.gameObject;
        var layer = root.gameObject.AddComponent<Canvas>();
        layer.overrideSorting = true; layer.sortingOrder = 240;
        root.gameObject.AddComponent<GraphicRaycaster>();
        var desktopCard = Box(root, "DesktopCard", new Vector2(.055f, .07f), new Vector2(.945f, .93f), new Color32(38, 67, 80, 255));
        Label(desktopCard, "Brand", "STUDY ROOM  /  MÁY TÍNH", new Vector2(.04f, .88f), new Vector2(.72f, .96f), 22, ink);
        Button(desktopCard, "CloseComputer", "Đóng máy tính", new Vector2(.76f, .88f), new Vector2(.96f, .96f), Close, panel);
        var app = Button(desktopCard, "MusicApp", "♫\nNghe nhạc", new Vector2(.04f, .58f), new Vector2(.21f, .80f), OpenMusic, mint);
        app.GetComponentInChildren<Text>().fontSize = 23;
        Label(desktopCard, "Welcome", "Không gian của bạn.\nMột chút nhạc cho buổi học.", new Vector2(.27f, .57f), new Vector2(.92f, .78f), 30, ink);
        var window = Box(desktopCard, "MusicWindow", new Vector2(.22f, .14f), new Vector2(.94f, .83f), panel);
        musicWindow = window.gameObject;
        Label(window, "AppTitle", "NGHE NHẠC", new Vector2(.05f, .85f), new Vector2(.72f, .96f), 20, mint);
        Button(window, "Minimize", "Thu nhỏ", new Vector2(.76f, .86f), new Vector2(.95f, .95f), () => musicWindow.SetActive(false), new Color32(54, 69, 91, 255));
        Label(window, "Artwork", "♫", new Vector2(.06f, .47f), new Vector2(.27f, .80f), 72, mint);
        trackTitle = Label(window, "TrackTitle", "Chọn một bản nhạc để bắt đầu", new Vector2(.32f, .59f), new Vector2(.94f, .77f), 24, ink);
        Label(window, "Subtitle", "Nhạc không lời · Tập trung & thư giãn", new Vector2(.32f, .47f), new Vector2(.94f, .59f), 15, ink);
        progress = MakeSlider(window, "Progress", new Vector2(.07f, .38f), new Vector2(.93f, .43f));
        progress.onValueChanged.AddListener(value => { if (source && source.clip) source.time = Mathf.Min(value * source.clip.length, Mathf.Max(0f, source.clip.length - .01f)); });
        timer = Label(window, "Timer", "00:00 / 00:00", new Vector2(.07f, .30f), new Vector2(.93f, .38f), 14, ink);
        Button(window, "Previous", "Trước", new Vector2(.07f, .16f), new Vector2(.28f, .28f), () => NextTrack(-1), new Color32(54, 69, 91, 255));
        playLabel = Button(window, "PlayPause", "Phát nhạc", new Vector2(.32f, .16f), new Vector2(.68f, .28f), TogglePlayback, mint).GetComponentInChildren<Text>();
        Button(window, "Next", "Tiếp", new Vector2(.72f, .16f), new Vector2(.93f, .28f), () => NextTrack(1), new Color32(54, 69, 91, 255));
        volumeLabel = Label(window, "VolumeLabel", "Âm lượng", new Vector2(.07f, .025f), new Vector2(.44f, .12f), 14, ink);
        volume = MakeSlider(window, "Volume", new Vector2(.48f, .05f), new Vector2(.93f, .09f));
        volume.onValueChanged.AddListener(value => RoomAudioChannel.SetVolume(RoomAudioChannel.Channel.BackgroundMusic, value));
        var dock = Box(desktopCard, "Taskbar", Vector2.zero, new Vector2(1f, .10f), panel);
        Button(dock, "MusicShortcut", "♫  Nghe nhạc", new Vector2(.025f, .15f), new Vector2(.25f, .85f), OpenMusic, new Color32(54, 69, 91, 255));
        Label(dock, "PlaybackHint", "Nhạc tiếp tục phát khi đóng máy tính", new Vector2(.29f, .1f), new Vector2(.82f, .9f), 14, ink);
        clock = Label(dock, "Clock", "", new Vector2(.85f, .1f), new Vector2(.98f, .9f), 18, ink);
        SyncVolume();
        musicWindow.SetActive(false);
        desktop.SetActive(false);
    }

    private RectTransform Box(Transform parent, string name, Vector2 min, Vector2 max, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false);
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = color;
        return rect;
    }

    private Text Label(Transform parent, string name, string value, Vector2 min, Vector2 max, int size, Color color)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
        var text = rect.gameObject.AddComponent<Text>(); text.font = font; text.text = value;
        text.fontSize = size; text.color = color; text.alignment = TextAnchor.MiddleLeft;
        text.resizeTextForBestFit = true; text.resizeTextMinSize = 10; text.resizeTextMaxSize = size;
        text.raycastTarget = false;
        return text;
    }

    private Button Button(Transform parent, string name, string title, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action, Color color)
    {
        var rect = Box(parent, name, min, max, color);
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>();
        button.onClick.AddListener(action);
        var text = Label(rect, "Text", title, new Vector2(.06f, .04f), new Vector2(.94f, .96f), 17, color == mint ? panel : ink);
        text.alignment = TextAnchor.MiddleCenter;
        return button;
    }

    private Slider MakeSlider(Transform parent, string name, Vector2 min, Vector2 max)
    {
        var rect = Box(parent, name, min, max, Color.clear);
        var track = Box(rect, "Track", new Vector2(0, .5f), new Vector2(1, .5f), new Color32(68, 86, 107, 255));
        track.sizeDelta = new Vector2(0, 10);
        var fillArea = Box(rect, "FillArea", new Vector2(0, .5f), new Vector2(1, .5f), Color.clear);
        fillArea.sizeDelta = new Vector2(-24, 10);
        var fill = Box(fillArea, "Fill", Vector2.zero, Vector2.one, mint);
        var handleArea = Box(rect, "HandleArea", new Vector2(0, .5f), new Vector2(1, .5f), Color.clear);
        handleArea.sizeDelta = new Vector2(-24, 0);
        var handle = Box(handleArea, "Handle", new Vector2(.5f, .5f), new Vector2(.5f, .5f), Color.white);
        handle.sizeDelta = new Vector2(28, 28);
        var slider = rect.gameObject.AddComponent<Slider>();
        slider.fillRect = fill; slider.handleRect = handle; slider.targetGraphic = handle.GetComponent<Image>();
        slider.minValue = 0; slider.maxValue = 1;
        return slider;
    }

    // Original, quiet synthesized loops provide playable music without downloading external tracks.
    private static AudioClip CreateStudyLoop(string name, int transpose)
    {
        const int rate = 22050, seconds = 32;
        var samples = new float[rate * seconds];
        int[] roots = { 48, 45, 53, 55 };
        int[] notes = { 0, 7, 12, 16, 19, 16, 12, 7 };
        for (int beat = 0; beat < 64; beat++)
        {
            float frequency = 440f * Mathf.Pow(2f, (roots[beat / 16] + transpose + notes[beat % 8] - 69f) / 12f);
            int start = beat * rate / 2;
            for (int i = 0; i < rate * 2 && start + i < samples.Length; i++)
            {
                float t = (float)i / rate;
                float envelope = Mathf.Min(1f, t * 100f) * Mathf.Exp(-t * 2.8f);
                samples[start + i] += (Mathf.Sin(2 * Mathf.PI * frequency * t) + .18f * Mathf.Sin(4 * Mathf.PI * frequency * t)) * envelope * .13f;
            }
        }
        for (int i = 0; i < rate / 20; i++)
        {
            samples[i] *= (float)i / (rate / 20);
            samples[samples.Length - 1 - i] *= (float)i / (rate / 20);
        }
        var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void OnDestroy()
    {
        RoomAudioChannel.VolumesChanged -= SyncVolume;
        Close();
        if (generatedTracks != null)
            foreach (var clip in generatedTracks)
                if (clip) { if (Application.isPlaying) Destroy(clip); else DestroyImmediate(clip); }
    }
}
