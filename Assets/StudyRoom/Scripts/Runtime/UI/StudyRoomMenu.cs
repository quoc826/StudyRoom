using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class StudyRoomMenu : MonoBehaviour
{
    [SerializeField] private UniversalRenderPipelineAsset[] qualityProfiles;
    private Transform canvas;
    private GameObject overlay, menu, settings, home;
    private PlayerController player;
    private bool restorePlayer;
    private Slider sfx, music;
    private Text sfxValue, musicValue;
    private Button[] qualityButtons;
    private readonly Color accent = new Color32(245, 173, 198, 255);
    private readonly Color pale = new Color32(232, 220, 248, 255);

    private void Awake()
    {
        if (!GetComponent<StudyRoomDayNight>()) gameObject.AddComponent<StudyRoomDayNight>();
        if (!GetComponent<StudyRoomZoom>()) gameObject.AddComponent<StudyRoomZoom>();
        if (!GetComponent<StudyRoomRewardsUI>()) gameObject.AddComponent<StudyRoomRewardsUI>();
        if (!GetComponent<StudyRoomShopUI>()) gameObject.AddComponent<StudyRoomShopUI>();
        canvas = transform.Find("Canvas");
        overlay = canvas.Find("Overlay").gameObject;
        menu = overlay.transform.Find("MenuPanel").gameObject;
        settings = overlay.transform.Find("SettingsPanel").gameObject;
        home = overlay.transform.Find("HomePanel").gameObject;
        player = FindAnyObjectByType<PlayerController>();
        Bind(canvas.Find("MenuButton"), OpenMenu);
        Bind(menu.transform.Find("Settings"), () => ShowPanel(settings));
        Bind(menu.transform.Find("Home"), () =>
        {
            CloseMenu();
            StudyRoomSession.ReturnHome();
        });
        Bind(menu.transform.Find("Close"), CloseMenu);
        Bind(settings.transform.Find("Back"), () => { PlayerPrefs.Save(); ShowPanel(menu); });
        Bind(home.transform.Find("EnterRoom"), CloseMenu);
        Bind(home.transform.Find("Settings"), () => ShowPanel(settings));
        sfx = settings.transform.Find("SfxSlider").GetComponent<Slider>();
        music = settings.transform.Find("MusicSlider").GetComponent<Slider>();
        sfxValue = settings.transform.Find("SfxValue").GetComponent<Text>();
        musicValue = settings.transform.Find("MusicValue").GetComponent<Text>();
        sfx.SetValueWithoutNotify(RoomAudioChannel.GetVolume(RoomAudioChannel.Channel.Sfx));
        music.SetValueWithoutNotify(RoomAudioChannel.GetVolume(RoomAudioChannel.Channel.BackgroundMusic));
        RefreshVolumeLabels();
        sfx.onValueChanged.AddListener(value => { RoomAudioChannel.SetVolume(RoomAudioChannel.Channel.Sfx, value); RefreshVolumeLabels(); });
        music.onValueChanged.AddListener(value => { RoomAudioChannel.SetVolume(RoomAudioChannel.Channel.BackgroundMusic, value); RefreshVolumeLabels(); });
        qualityButtons = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            qualityButtons[i] = settings.transform.Find("Quality" + i).GetComponent<Button>();
            qualityButtons[i].onClick.AddListener(() => ApplyQuality(index));
        }
        ApplyQuality(Mathf.Clamp(PlayerPrefs.GetInt("StudyRoom.Quality", 2), 0, 2));
        overlay.SetActive(false);
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        bool escape = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        bool escape = Input.GetKeyDown(KeyCode.Escape);
#else
        bool escape = false;
#endif
        if (escape)
        {
            var shop = GetComponent<StudyRoomShopUI>();
            if (shop && shop.IsPlacing) { shop.CancelPlacement(); return; }
            if (shop && shop.IsOpen) { shop.Close(); return; }
            var rewards = GetComponent<StudyRoomRewardsUI>();
            if (rewards && rewards.IsOpen) { rewards.Close(); return; }
            if (overlay.activeSelf) CloseMenu(); else OpenMenu();
        }
    }

    private void Bind(Transform target, UnityEngine.Events.UnityAction action) =>
        target.GetComponent<Button>().onClick.AddListener(action);

    public void OpenMenu()
    {
        var shop = GetComponent<StudyRoomShopUI>();
        if (shop && shop.IsPlacing) shop.CancelPlacement();
        if (shop && shop.IsOpen) shop.Close();
        var rewards = GetComponent<StudyRoomRewardsUI>();
        if (rewards && rewards.IsOpen) rewards.Close();
        if (!overlay.activeSelf && player) { restorePlayer = player.enabled; player.enabled = false; }
        overlay.SetActive(true);
        ShowPanel(menu);
    }

    public void SetPlayer(PlayerController controller)
    {
        player = controller;
        restorePlayer = false;
    }

    private void ShowPanel(GameObject panel)
    {
        menu.SetActive(panel == menu);
        settings.SetActive(panel == settings);
        home.SetActive(panel == home);
    }

    public void CloseMenu()
    {
        PlayerPrefs.Save();
        overlay.SetActive(false);
        if (player && restorePlayer) player.enabled = true;
        restorePlayer = false;
    }

    private void RefreshVolumeLabels()
    {
        sfxValue.text = Mathf.RoundToInt(sfx.value * 100f) + "%";
        musicValue.text = Mathf.RoundToInt(music.value * 100f) + "%";
    }

    private void ApplyQuality(int index)
    {
        if (qualityProfiles != null && index < qualityProfiles.Length && qualityProfiles[index])
            QualitySettings.renderPipeline = qualityProfiles[index];
        QualitySettings.globalTextureMipmapLimit = index == 0 ? 1 : 0;
        QualitySettings.anisotropicFiltering = index == 2 ? AnisotropicFiltering.ForceEnable : AnisotropicFiltering.Enable;
        for (int i = 0; i < qualityButtons.Length; i++)
        {
            qualityButtons[i].GetComponent<Image>().color = i == index ? accent : pale;
            qualityButtons[i].transform.Find("Label").GetComponent<Text>().text =
                (i == index ? "•  " : "") + new[] { "Thấp", "Vừa", "Cao" }[i];
        }
        PlayerPrefs.SetInt("StudyRoom.Quality", index);
    }

    private void OnApplicationPause(bool paused) { if (paused) PlayerPrefs.Save(); }
    private void OnDestroy() { PlayerPrefs.Save(); }
}
