using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>A shared desktop and music player for room monitors and laptops.</summary>
public class StudyRoomComputerUI : MonoBehaviour
{
    public AudioClip[] tracks;
    [SerializeField] private Texture2D laptopTexture;
    [SerializeField, Min(.05f)] private float appearanceDuration = .35f;
    private RectTransform laptopFrame;
    private CanvasGroup desktopFade;
    private Coroutine appearance;
    private GameObject desktop, musicWindow, audioObject;
    private AudioSource source;
    private PlayerController player;
    private bool restorePlayer, paused;
    private int trackIndex;
    private Font font;
    private Text trackTitle, clock;
    private GameObject playIcon, pauseIcon;
    private Slider progress, volume;
    private AudioClip[] generatedTracks;
    private Sprite rounded;
    private RectTransform playlistContent;
    private readonly List<Image> playlistRows = new List<Image>();
    private readonly List<int> playlistIndices = new List<int>();
    private readonly Color ink = new Color32(79, 57, 98, 255);
    private readonly Color panel = new Color32(255, 247, 252, 255);
    private readonly Color mint = new Color32(245, 173, 198, 255);
    private readonly Color lavender = new Color32(232, 220, 248, 255);
    public bool IsOpen => desktop && desktop.activeSelf;
    public bool IsPlaying => source && source.isPlaying;

    private void Start()
    {
        EnsureAudio();
        var available = new List<int>();
        for (int i = 0; i < tracks.Length; i++)
            if (tracks[i]) available.Add(i);
        if (available.Count > 0)
            SelectTrack(available[UnityEngine.Random.Range(0, available.Count)], true);
    }

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
        return name.Contains("monitor") || name.Contains("laptop") || name.Contains("latop") || name.Contains("computer_screen");
    }

    public bool TryOpen(Transform target)
    {
        // Imported laptops can have separate child meshes for their screen and keyboard.
        while (target && !IsComputer(target)) target = target.parent;
        if (!target) return false;
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
        if (appearance != null) StopCoroutine(appearance);
        appearance = StartCoroutine(AnimateAppearance());
    }

    private IEnumerator AnimateAppearance()
    {
        desktopFade.alpha = 0f;
        desktopFade.interactable = false;
        float elapsed = 0f;
        while (elapsed < appearanceDuration)
        {
            float t = Mathf.Clamp01(elapsed / appearanceDuration);
            desktopFade.alpha = Mathf.SmoothStep(0f, 1f, t);
            if (laptopFrame)
            {
                float back = t - 1f;
                float eased = 1f + 2.70158f * back * back * back + 1.70158f * back * back;
                laptopFrame.localScale = Vector3.one * Mathf.LerpUnclamped(.86f, 1f, eased);
                laptopFrame.anchoredPosition = Vector2.up * Mathf.Lerp(-36f, 0f, 1f - Mathf.Pow(1f - t, 3f));
            }
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        desktopFade.alpha = 1f;
        desktopFade.interactable = true;
        if (laptopFrame) { laptopFrame.localScale = Vector3.one; laptopFrame.anchoredPosition = Vector2.zero; }
        appearance = null;
    }

    public void Close()
    {
        if (appearance != null) { StopCoroutine(appearance); appearance = null; }
        if (desktop) desktop.SetActive(false);
        if (player && restorePlayer) player.enabled = true;
        restorePlayer = false;
    }

    public void OpenMusic()
    {
        EnsureAudio();
        BuildPlaylist();
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
        if (tracks == null || Array.FindIndex(tracks, clip => clip) < 0)
        {
            generatedTracks = new[] { CreateStudyLoop("Góc học yên tĩnh", 0), CreateStudyLoop("Chiều thư giãn", 5) };
            tracks = generatedTracks;
        }
        SelectTrack(Array.FindIndex(tracks, clip => clip), false);
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
        for (int step = 1; step <= tracks.Length; step++)
        {
            int index = ((trackIndex + direction * step) % tracks.Length + tracks.Length) % tracks.Length;
            if (!tracks[index]) continue;
            SelectTrack(index, true);
            return;
        }
    }

    private void SelectTrack(int index, bool play)
    {
        if (tracks == null || index < 0 || index >= tracks.Length || !tracks[index]) return;
        trackIndex = index;
        source.Stop();
        source.clip = tracks[index];
        paused = false;
        if (play && source.clip) source.Play();
        RefreshPlaylistSelection();
        Refresh();
    }

    private void BuildPlaylist()
    {
        if (!playlistContent) return;
        foreach (Transform child in playlistContent)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
        playlistRows.Clear();
        playlistIndices.Clear();
        for (int i = 0; i < tracks.Length; i++)
        {
            if (!tracks[i]) continue;
            int index = i;
            var row = Button(playlistContent, "Track" + i, "", Vector2.zero, Vector2.one,
                () => SelectTrack(index, true), lavender);
            var rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0, 1); rowRect.anchorMax = Vector2.one;
            rowRect.pivot = new Vector2(.5f, 1);
            rowRect.offsetMin = new Vector2(0, -playlistRows.Count * 52 - 46);
            rowRect.offsetMax = new Vector2(0, -playlistRows.Count * 52);
            Artwork(row.transform, "Note", "Music", new Vector2(.04f, .23f), new Vector2(.13f, .77f));
            var title = Label(row.transform, "Title", tracks[i].name, new Vector2(.18f, .15f), new Vector2(.96f, .85f), 16, ink);
            title.horizontalOverflow = HorizontalWrapMode.Wrap;
            playlistRows.Add(row.GetComponent<Image>());
            playlistIndices.Add(i);
        }
        playlistContent.sizeDelta = new Vector2(0, Mathf.Max(0, playlistRows.Count * 52 - 6));
        RefreshPlaylistSelection();
    }

    private void RefreshPlaylistSelection()
    {
        for (int i = 0; i < playlistRows.Count; i++)
            playlistRows[i].color = source && source.clip == tracks[playlistIndices[i]] ? mint : lavender;
    }

    private void SyncVolume()
    {
        float value = RoomAudioChannel.GetVolume(RoomAudioChannel.Channel.BackgroundMusic);
        if (source) source.volume = value;
        if (volume) volume.SetValueWithoutNotify(value);
    }

    private void Update()
    {
        if (IsOpen) Refresh();
    }

    private void Refresh()
    {
        if (clock) clock.text = DateTime.Now.ToString("HH:mm");
        if (!trackTitle) return;
        trackTitle.text = source && source.clip ? source.clip.name : "Nghe nhạc";
        if (playIcon) playIcon.SetActive(!IsPlaying);
        if (pauseIcon) pauseIcon.SetActive(IsPlaying);
        float length = source && source.clip ? source.clip.length : 0f;
        float time = source && source.clip ? source.time : 0f;
        progress.SetValueWithoutNotify(length > 0 ? time / length : 0f);
    }


    public void BuildInterface()
    {
        if (desktop) return;
        var canvas = transform.Find("Canvas");
        if (!canvas) return;
        var reference = canvas.GetComponentInChildren<Text>(true);
        font = reference && reference.font ? reference.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var menu = canvas.Find("MenuButton");
        if (menu && menu.GetComponent<Image>()) rounded = menu.GetComponent<Image>().sprite;
        var root = Box(canvas, "ComputerDesktop", Vector2.zero, Vector2.one, new Color32(12, 21, 37, 185));
        desktop = root.gameObject;
        desktopFade = root.gameObject.AddComponent<CanvasGroup>();
        var layer = root.gameObject.AddComponent<Canvas>();
        layer.overrideSorting = true; layer.sortingOrder = 240;
        root.gameObject.AddComponent<GraphicRaycaster>();
        var backdrop = Box(root, "ClickOutsideToClose", Vector2.zero, Vector2.one, Color.clear);
        var dismiss = backdrop.gameObject.AddComponent<UnityEngine.UI.Button>();
        dismiss.targetGraphic = backdrop.GetComponent<Image>();
        dismiss.transition = Selectable.Transition.None;
        dismiss.onClick.AddListener(Close);
        Transform screenParent = root;
        if (laptopTexture)
        {
            var slot = Box(root, "LaptopSlot", new Vector2(.01f, .01f), new Vector2(.99f, .99f), Color.clear);
            slot.GetComponent<Image>().raycastTarget = false;
            laptopFrame = new GameObject("LaptopFrame", typeof(RectTransform)).GetComponent<RectTransform>();
            laptopFrame.SetParent(slot, false);
            var fit = laptopFrame.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = 716f / 448f;
            var artwork = laptopFrame.gameObject.AddComponent<RawImage>();
            artwork.texture = laptopTexture;
            artwork.uvRect = new Rect(14f / 742f, 470f / 1344f, 716f / 742f, 448f / 1344f);
            artwork.raycastTarget = true;
            screenParent = Box(laptopFrame, "Screen", new Vector2(.099f, .132f), new Vector2(.901f, .931f), Color.clear);
        }
        var desktopCard = Box(screenParent, "DesktopCard", laptopTexture ? Vector2.zero : new Vector2(.02f, .025f),
            laptopTexture ? Vector2.one : new Vector2(.98f, .975f), panel);
        desktopCard.gameObject.AddComponent<RectMask2D>();
        Artwork(desktopCard, "Wallpaper", "Wallpaper", Vector2.zero, Vector2.one, false);
        var topbar = Box(desktopCard, "Topbar", new Vector2(0, .89f), Vector2.one, panel);
        Artwork(topbar, "HomeStar", "Star", new Vector2(.025f, .19f), new Vector2(.085f, .81f));
        Label(topbar, "Brand", "GÓC NHỎ CỦA BẠN", new Vector2(.10f, .15f), new Vector2(.69f, .85f), 20, ink);
        Label(topbar, "Status", "Một ngày thật dịu dàng", new Vector2(.70f, .15f), new Vector2(.97f, .85f), 14, ink).alignment = TextAnchor.MiddleRight;
        var app = Button(desktopCard, "MusicApp", "", new Vector2(.035f, .55f), new Vector2(.195f, .82f), OpenMusic, panel);
        Artwork(app.transform, "MusicIcon", "Music", new Vector2(.24f, .37f), new Vector2(.76f, .88f));
        Label(app.transform, "Caption", "Nghe nhạc", new Vector2(.06f, .07f), new Vector2(.94f, .32f), 18, ink).alignment = TextAnchor.MiddleCenter;
        var welcome = Box(desktopCard, "WelcomeNote", new Vector2(.32f, .40f), new Vector2(.89f, .75f), new Color32(255, 247, 252, 225));
        Label(welcome, "Eyebrow", "CHÀO BẠN,", new Vector2(.08f, .72f), new Vector2(.80f, .90f), 14, ink);
        Label(welcome, "Welcome", "Hôm nay, mình\nhọc một chút nhé!", new Vector2(.08f, .25f), new Vector2(.79f, .72f), 29, ink);
        Label(welcome, "Hint", "Bật nhạc lên và tận hưởng góc nhỏ của bạn.", new Vector2(.08f, .07f), new Vector2(.92f, .24f), 13, ink);
        Artwork(welcome, "Butterfly", "Butterfly", new Vector2(.79f, .62f), new Vector2(.96f, .94f));
        Artwork(desktopCard, "Flowers", "Flowers", new Vector2(.77f, .115f), new Vector2(.92f, .35f));
        var window = Box(desktopCard, "MusicWindow", new Vector2(.02f, .115f), new Vector2(.98f, .88f), panel);
        musicWindow = window.gameObject;
        Box(window, "Titlebar", new Vector2(0, .83f), Vector2.one, lavender);
        Artwork(window, "AppIcon", "Music", new Vector2(.05f, .87f), new Vector2(.11f, .96f));
        IconButton(window, "Minimize", "Minimize", new Vector2(.86f, .855f), new Vector2(.96f, .975f), () => musicWindow.SetActive(false), lavender);
        var album = Box(window, "Album", new Vector2(.06f, .47f), new Vector2(.27f, .79f), lavender);
        Artwork(album, "MusicArtwork", "Music", new Vector2(.18f, .16f), new Vector2(.82f, .84f));
        trackTitle = Label(window, "TrackTitle", "Nghe nhạc", new Vector2(.32f, .49f), new Vector2(.94f, .77f), 24, ink);
        progress = MakeSlider(window, "Progress", new Vector2(.07f, .38f), new Vector2(.93f, .43f));
        progress.onValueChanged.AddListener(value => { if (source && source.clip) source.time = Mathf.Min(value * source.clip.length, Mathf.Max(0f, source.clip.length - .01f)); });
        IconButton(window, "Previous", "Previous", new Vector2(.27f, .145f), new Vector2(.39f, .285f), () => NextTrack(-1), lavender);
        var playback = IconButton(window, "PlayPause", "Play", new Vector2(.43f, .125f), new Vector2(.57f, .305f), TogglePlayback, mint);
        playIcon = playback.transform.Find("Icon").gameObject;
        Artwork(playback.transform, "PauseIcon", "Pause", new Vector2(.27f, .24f), new Vector2(.73f, .76f));
        pauseIcon = playback.transform.Find("PauseIcon").gameObject;
        pauseIcon.SetActive(false);
        IconButton(window, "Next", "Play", new Vector2(.61f, .145f), new Vector2(.73f, .285f), () => NextTrack(1), lavender);
        Artwork(window, "VolumeIcon", "Sound", new Vector2(.25f, .025f), new Vector2(.31f, .095f));
        volume = MakeSlider(window, "Volume", new Vector2(.34f, .045f), new Vector2(.75f, .085f));
        volume.onValueChanged.AddListener(value => RoomAudioChannel.SetVolume(RoomAudioChannel.Channel.BackgroundMusic, value));
        // Keep the compact player on the left and reserve a separate scroll area for the library.
        var playerPanel = Box(window, "Player", new Vector2(.015f, .025f), new Vector2(.53f, .98f), Color.clear);
        string[] playerControls = { "Album", "TrackTitle", "Progress", "Previous", "PlayPause", "Next", "VolumeIcon", "Volume" };
        foreach (var control in playerControls) window.Find(control).SetParent(playerPanel, false);
        Label(window, "PlaylistTitle", "Bài hát", new Vector2(.57f, .84f), new Vector2(.83f, .97f), 17, ink);
        var viewport = Box(window, "Playlist", new Vector2(.56f, .06f), new Vector2(.97f, .79f), Color.clear);
        viewport.gameObject.AddComponent<RectMask2D>();
        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 26;
        scroll.viewport = viewport;
        playlistContent = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
        playlistContent.SetParent(viewport, false);
        playlistContent.anchorMin = new Vector2(0, 1); playlistContent.anchorMax = Vector2.one;
        playlistContent.pivot = new Vector2(.5f, 1);
        playlistContent.sizeDelta = Vector2.zero;
        playlistContent.anchoredPosition = Vector2.zero;
        scroll.content = playlistContent;
        var dock = Box(desktopCard, "Taskbar", Vector2.zero, new Vector2(1f, .10f), panel);
        var shortcut = Button(dock, "MusicShortcut", "Nghe nhạc", new Vector2(.025f, .15f), new Vector2(.25f, .85f), OpenMusic, mint);
        shortcut.GetComponentInChildren<Text>().rectTransform.anchorMin = new Vector2(.28f, .04f);
        Artwork(shortcut.transform, "SoundIcon", "Sound", new Vector2(.05f, .18f), new Vector2(.27f, .82f));
        Label(dock, "PlaybackHint", "Nhạc vẫn ngân nga khi bạn rời máy tính", new Vector2(.29f, .1f), new Vector2(.82f, .9f), 14, ink);
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
        var image = go.GetComponent<Image>();
        image.color = color;
        if (rounded && color.a > 0 && name != "ComputerDesktop")
        {
            image.sprite = rounded;
            image.type = Image.Type.Sliced;
        }
        return rect;
    }

    private void Artwork(Transform parent, string name, string resource, Vector2 min, Vector2 max, bool preserveAspect = true)
    {
        var texture = Resources.Load<Texture2D>("Computer/" + resource);
        if (!texture) return;
        var slot = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        slot.SetParent(parent, false);
        slot.anchorMin = min; slot.anchorMax = max;
        slot.offsetMin = slot.offsetMax = Vector2.zero;
        var rect = new GameObject("Image", typeof(RectTransform), typeof(RawImage)).GetComponent<RectTransform>();
        rect.SetParent(slot, false);
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var image = rect.GetComponent<RawImage>();
        image.texture = texture; image.raycastTarget = false;
        if (preserveAspect)
        {
            var fit = rect.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = (float)texture.width / texture.height;
        }
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
        var text = Label(rect, "Text", title, new Vector2(.06f, .04f), new Vector2(.94f, .96f), 17, ink);
        text.alignment = TextAnchor.MiddleCenter;
        return button;
    }

    private Button IconButton(Transform parent, string name, string icon, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action, Color color)
    {
        var rect = Box(parent, name, min, max, color);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        button.onClick.AddListener(action);
        Artwork(rect, "Icon", icon, new Vector2(.27f, .24f), new Vector2(.73f, .76f));
        return button;
    }

    private Slider MakeSlider(Transform parent, string name, Vector2 min, Vector2 max)
    {
        var rect = Box(parent, name, min, max, Color.clear);
        var track = Box(rect, "Track", new Vector2(0, .5f), new Vector2(1, .5f), lavender);
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
