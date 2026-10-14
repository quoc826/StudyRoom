using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private Camera inputCamera;
    [SerializeField] private Animator animator;
    [SerializeField, Min(0f)] private float moveSpeed = 2.5f;
    [SerializeField, Min(0.01f)] private float rotationSmoothTime = 0.12f;
    [SerializeField, Min(0.001f)] private float stoppingDistance = 0.02f;
    [Tooltip("Use this if the model's forward direction is not local +Z.")]
    [SerializeField] private float facingOffset;

    private static readonly int IsWalking = Animator.StringToHash("IsWalking");
    private Vector3 destination;
    private float turnVelocity;
    private bool hasDestination;
    private bool walking;
    private CharacterController characterController;
    private float verticalSpeed;
    private int tapFinger = -1;
    private Vector2 tapStart;
    private bool touchGestureConsumed;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (!inputCamera) inputCamera = Camera.main;
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (animator) animator.applyRootMotion = false;
        destination = transform.position;
    }

    private void Update()
    {
        if (StudyRoomTouchInput.Read().Count >= 2) hasDestination = false;
        if (TryGetPress(out Vector2 screenPosition, out int pointerId)
            && !(EventSystem.current && EventSystem.current.IsPointerOverGameObject(pointerId)))
        {
            if (!inputCamera) inputCamera = Camera.main;
            if (inputCamera)
            {
                // Project onto the character's horizontal floor, including floors without colliders.
                Ray ray = inputCamera.ScreenPointToRay(screenPosition);
                Plane floor = new Plane(Vector3.up, transform.position);
                if (floor.Raycast(ray, out float distance))
                    MoveTo(ray.GetPoint(distance));
            }
        }

        bool moving = false;
        if (hasDestination)
        {
            Vector3 direction = destination - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= stoppingDistance * stoppingDistance)
            {
                hasDestination = false;
            }
            else if (moveSpeed > 0f && Time.deltaTime > 0f)
            {
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + facingOffset;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle,
                    ref turnVelocity, rotationSmoothTime, Mathf.Infinity, Time.deltaTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
                Vector3 before = transform.position;
                Vector3 step = direction.normalized * Mathf.Min(direction.magnitude, moveSpeed * Time.deltaTime);
                characterController.Move(step);
                Vector3 actualStep = transform.position - before;
                actualStep.y = 0f;
                Vector3 remaining = destination - transform.position;
                remaining.y = 0f;
                hasDestination = remaining.sqrMagnitude > stoppingDistance * stoppingDistance;
                moving = actualStep.sqrMagnitude > 0.000001f && hasDestination;
                if (actualStep.sqrMagnitude < step.sqrMagnitude * 0.01f)
                    hasDestination = false;
            }
        }
        if (characterController.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
        verticalSpeed += Physics.gravity.y * Time.deltaTime;
        characterController.Move(Vector3.up * verticalSpeed * Time.deltaTime);
        SetWalking(moving);
    }

    public void MoveTo(Vector3 worldPosition)
    {
        worldPosition.y = transform.position.y;
        destination = worldPosition;
        hasDestination = (destination - transform.position).sqrMagnitude > stoppingDistance * stoppingDistance;
    }

    private void SetWalking(bool value)
    {
        if (walking == value) return;
        walking = value;
        if (animator) animator.SetBool(IsWalking, walking);
    }

    private void OnDisable()
    {
        hasDestination = false;
        turnVelocity = 0f;
        walking = false;
        tapFinger = -1;
        touchGestureConsumed = false;
        if (animator) animator.SetBool(IsWalking, false);
    }

    private bool TryGetPress(out Vector2 position, out int pointerId)
    {
        position = default;
        pointerId = -1;
        var touches = StudyRoomTouchInput.Read();
        if (touches.Count > 0 || tapFinger >= 0 || touchGestureConsumed)
        {
            if (touches.Count >= 2)
            {
                touchGestureConsumed = true;
                tapFinger = -1;
                return false;
            }
            if (touches.Count == 1)
            {
                if (!touchGestureConsumed && tapFinger < 0)
                {
                    tapFinger = touches.FirstId;
                    tapStart = touches.FirstPosition;
                    touchGestureConsumed = StudyRoomTouchInput.IsOverUI(tapStart);
                }
                else if (tapFinger != touches.FirstId || Vector2.Distance(tapStart, touches.FirstPosition) > 20f)
                    touchGestureConsumed = true;
                return false;
            }
            bool tapped = tapFinger >= 0 && !touchGestureConsumed && !touches.Canceled;
            position = tapStart;
            pointerId = tapFinger;
            tapFinger = -1;
            touchGestureConsumed = false;
            return tapped;
        }
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            position = Mouse.current.position.ReadValue();
            return true;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButtonDown(0))
        {
            position = Input.mousePosition;
            return true;
        }
#endif
        return false;
    }
}
