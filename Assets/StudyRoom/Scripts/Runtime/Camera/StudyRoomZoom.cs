using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class StudyRoomZoom : MonoBehaviour
{
    private Camera roomCamera;
    private float defaultSize;
    private float zoom = 1f;
    private Transform overlay;
    private StudyRoomRewardsUI rewards;
    private StudyRoomShopUI shop;
    private float previousPinchDistance;
    private int firstFinger, secondFinger;
    private bool pinchActive, pinchBlocked;
    private const float MinimumZoom = 0.65f;
    private const float MaximumZoom = 2.5f;

    private void Start()
    {
        overlay = transform.Find("Canvas/Overlay");
        rewards = GetComponent<StudyRoomRewardsUI>();
        shop = GetComponent<StudyRoomShopUI>();
    }

    public void CaptureDefaultView()
    {
        roomCamera = Camera.main;
        if (!roomCamera || !roomCamera.orthographic) return;
        defaultSize = roomCamera.orthographicSize;
        zoom = 1f;
        pinchActive = false;
    }

    private void LateUpdate()
    {
        if (!roomCamera || defaultSize <= 0f) CaptureDefaultView();
        if (!roomCamera || !roomCamera.orthographic || defaultSize <= 0f) return;
        bool modalOpen = (overlay && overlay.gameObject.activeInHierarchy) || (rewards && rewards.IsOpen) || (shop && shop.IsOpen);
        var touches = StudyRoomTouchInput.Read();
        if (touches.Count >= 2)
            UpdatePinch(touches, modalOpen);
        else
        {
            pinchActive = false;
            if (touches.Count == 0 && !modalOpen &&
                !(EventSystem.current && EventSystem.current.IsPointerOverGameObject()))
            {
                float scroll = 0f;
#if ENABLE_INPUT_SYSTEM
                if (Mouse.current != null) scroll = Mouse.current.scroll.ReadValue().y / 120f;
#elif ENABLE_LEGACY_INPUT_MANAGER
                scroll = Input.mouseScrollDelta.y;
#endif
                if (Mathf.Abs(scroll) > 0.001f) ChangeZoom(scroll);
            }
        }
        float target = defaultSize / zoom;
        roomCamera.orthographicSize = Mathf.Lerp(roomCamera.orthographicSize, target,
            1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
        if (Mathf.Abs(roomCamera.orthographicSize - target) < 0.001f)
            roomCamera.orthographicSize = target;
    }

    private void UpdatePinch(StudyRoomTouchInput.Snapshot touches, bool modalOpen)
    {
        float distance = Vector2.Distance(touches.FirstPosition, touches.SecondPosition);
        if (!pinchActive || firstFinger != touches.FirstId || secondFinger != touches.SecondId)
        {
            pinchActive = true;
            firstFinger = touches.FirstId;
            secondFinger = touches.SecondId;
            pinchBlocked = modalOpen || StudyRoomTouchInput.IsOverUI(touches.FirstPosition) ||
                StudyRoomTouchInput.IsOverUI(touches.SecondPosition);
            previousPinchDistance = distance;
            return;
        }
        if (modalOpen) pinchBlocked = true;
        if (!pinchBlocked && previousPinchDistance > 1f && distance > 1f)
            zoom = Mathf.Clamp(zoom * distance / previousPinchDistance, MinimumZoom, MaximumZoom);
        previousPinchDistance = distance;
    }

    public void ChangeZoom(float steps)
    {
        if (!roomCamera || defaultSize <= 0f) CaptureDefaultView();
        zoom = Mathf.Clamp(zoom * Mathf.Pow(1.15f, steps), MinimumZoom, MaximumZoom);
    }

    private void OnDisable() => pinchActive = false;
}
