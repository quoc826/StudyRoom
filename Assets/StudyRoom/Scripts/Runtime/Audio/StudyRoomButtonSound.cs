using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class StudyRoomButtonSound : MonoBehaviour
{
    private Button button;
    private void Awake() => Bind();

    public void Bind()
    {
        button = GetComponent<Button>();
        button.onClick.RemoveListener(Play);
        button.onClick.AddListener(Play);
    }

    private void Play() => StudyRoomUIAudio.PlayClick();

    private void OnDestroy() { if (button) button.onClick.RemoveListener(Play); }
}
