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
    [SerializeField, Min(0.05f)] private float chairInteractionDistance = 0.3f;
    [SerializeField, Min(1f)] private float afkDelay = 60f;
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
    private Transform currentChair;
    private Transform pendingChair;
    private Vector3 standingPosition;
    private bool seated;
    private static readonly int SitState = Animator.StringToHash("Base Layer.Sit");
    private static readonly int WaveState = Animator.StringToHash("Base Layer.AfkWave");
    private static readonly int LookState = Animator.StringToHash("Base Layer.AfkLook");
    private static readonly int CelebrateState = Animator.StringToHash("Base Layer.Celebrate");
    private static readonly int[] InteractionStates =
    {
        Animator.StringToHash("Base Layer.ClickTriplePose"),
        Animator.StringToHash("Base Layer.ClickAngry"),
        Animator.StringToHash("Base Layer.ClickIdea"),
        Animator.StringToHash("Base Layer.ClickLookAtFeet")
    };
    private int interactionState;
    private StudyRoomEconomy economy;
    private float idleElapsed;
    private bool waving;
    private int lastAfkMinute;
    private bool lookingAround;
    private bool celebrating;
    private bool celebrationRequested;

    public bool IsSeated => seated;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (!inputCamera) inputCamera = Camera.main;
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (animator) animator.applyRootMotion = false;
        destination = transform.position;
        BindEconomy();
    }

    private void BindEconomy()
    {
        if (economy) return;
        economy = FindAnyObjectByType<StudyRoomEconomy>();
        if (economy) economy.QuestCompleted += OnQuestCompleted;
    }

    private void OnQuestCompleted(int index) => celebrationRequested = true;

    private void Update()
    {
        BindEconomy();
        if (StudyRoomTouchInput.Read().Count >= 2)
        {
            hasDestination = false;
            pendingChair = null;
        }
        if (TryGetPress(out Vector2 screenPosition, out int pointerId)
            && !(EventSystem.current && EventSystem.current.IsPointerOverGameObject(pointerId)))
        {
            if (!inputCamera) inputCamera = Camera.main;
            if (inputCamera)
            {
                // Project onto the character's horizontal floor, including floors without colliders.
                Ray ray = inputCamera.ScreenPointToRay(screenPosition);
                if (TryInteractFromRay(ray))
                    return;
                Plane floor = new Plane(Vector3.up, seated ? standingPosition : transform.position);
                if (floor.Raycast(ray, out float distance))
                    MoveTo(ray.GetPoint(distance));
            }
        }

        if (celebrationRequested && animator && animator.HasState(0, CelebrateState))
        {
            celebrationRequested = false;
            StandUp();
            pendingChair = null;
            hasDestination = false;
            StopIdleAnimation();
            SetWalking(false);
            celebrating = true;
            animator.Play(CelebrateState, 0, 0f);
        }

        if (seated)
        {
            if (!currentChair || !currentChair.gameObject.activeInHierarchy) StandUp();
            else return;
        }

        if (TryCompleteChairApproach()) return;

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
        if (TryCompleteChairApproach()) return;
        if (!hasDestination) pendingChair = null;
        UpdateIdleAnimation(Time.deltaTime);
    }

    private void UpdateIdleAnimation(float elapsed)
    {
        if (interactionState != 0)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.fullPathHash == interactionState && state.normalizedTime >= 1f)
                StopIdleAnimation(true);
            return;
        }
        if (celebrating)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.fullPathHash == CelebrateState && state.normalizedTime >= 1f)
                StopIdleAnimation(true);
            return;
        }
        if (hasDestination || walking || pendingChair || seated)
        {
            idleElapsed = 0f;
            lastAfkMinute = 0;
            return;
        }
        idleElapsed += elapsed;
        if ((waving || lookingAround) && animator)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            int activeState = waving ? WaveState : LookState;
            if (state.fullPathHash == activeState && state.normalizedTime >= 1f)
            {
                waving = false;
                lookingAround = false;
                animator.CrossFadeInFixedTime("Base Layer.Idle", 0.15f, 0, 0f);
            }
        }
        int minute = Mathf.FloorToInt(idleElapsed / Mathf.Max(1f, afkDelay));
        if (minute <= lastAfkMinute) return;
        lastAfkMinute = minute;
        int nextState = minute % 2 == 1 ? WaveState : LookState;
        if (animator && animator.HasState(0, nextState))
        {
            waving = nextState == WaveState;
            lookingAround = nextState == LookState;
            animator.Play(nextState, 0, 0f);
        }
    }

    private void StopIdleAnimation(bool blendToIdle = false)
    {
        idleElapsed = 0f;
        if ((waving || lookingAround || celebrating || interactionState != 0) && animator)
        {
            if (blendToIdle) animator.CrossFadeInFixedTime("Base Layer.Idle", 0.15f, 0, 0f);
            else animator.Play("Base Layer.Idle", 0, 0f);
        }
        interactionState = 0;
        waving = false;
        lastAfkMinute = 0;
        lookingAround = false;
        celebrating = false;
    }

    public void MoveTo(Vector3 worldPosition)
    {
        StopIdleAnimation();
        pendingChair = null;
        StandUp();
        worldPosition.y = transform.position.y;
        destination = worldPosition;
        hasDestination = (destination - transform.position).sqrMagnitude > stoppingDistance * stoppingDistance;
    }

    public bool TrySitOn(Transform target)
    {
        Transform chair = target;
        while (chair && chair.name.IndexOf("Chair", System.StringComparison.OrdinalIgnoreCase) < 0)
            chair = chair.parent;
        if (!chair || !animator || !animator.HasState(0, SitState)) return false;
        if (seated && currentChair == chair) return true;

        var renderers = chair.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return false;
        StopIdleAnimation();
        Bounds bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        StandUp();
        if (!IsWithinChairRange(bounds))
        {
            // Walk towards the chair; only snap into the sitting pose once close enough.
            MoveTo(bounds.center);
            pendingChair = chair;
            return true;
        }
        pendingChair = null;
        // Probe the middle of the chair, below the backrest, for the seat surface.
        Vector3 seat = bounds.center;
        seat.y = bounds.min.y + bounds.size.y * 0.55f;
        float highestSurface = bounds.min.y;
        foreach (var collider in chair.GetComponentsInChildren<Collider>())
            if (collider.Raycast(new Ray(seat, Vector3.down), out RaycastHit surface, bounds.size.y))
                highestSurface = Mathf.Max(highestSurface, surface.point.y);
        seat.y = highestSurface > bounds.min.y ? highestSurface : bounds.min.y + bounds.size.y * 0.48f;

        if (!seated) standingPosition = transform.position;
        characterController.enabled = false;
        hasDestination = false;
        verticalSpeed = 0f;
        turnVelocity = 0f;
        SetWalking(false);
        currentChair = chair;
        seated = true;
        transform.SetPositionAndRotation(seat, Quaternion.Euler(0f, chair.eulerAngles.y + facingOffset + 180f, 0f));
        animator.Play(SitState, 0, 0f);
        animator.Update(0f);
        // Align the animated pelvis with the seat rather than assuming a model's root height.
        Transform hips = animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Hips) : null;
        if (!hips)
            foreach (var bone in animator.GetComponentsInChildren<Transform>())
                if (bone.name == "DEF-spine") { hips = bone; break; }
        if (hips) transform.position += seat + Vector3.up * (0.08f * transform.lossyScale.y) - hips.position;
        return true;
    }

    private bool IsWithinChairRange(Bounds bounds)
    {
        Vector3 position = transform.position;
        position.y = bounds.center.y;
        float reach = chairInteractionDistance + characterController.radius * Mathf.Max(
            Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
        return (bounds.ClosestPoint(position) - position).sqrMagnitude <= reach * reach;
    }

    private bool TryCompleteChairApproach()
    {
        if (!pendingChair) return false;
        if (!pendingChair.gameObject.activeInHierarchy)
        {
            pendingChair = null;
            hasDestination = false;
            return false;
        }
        var renderers = pendingChair.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) { pendingChair = null; return false; }
        Bounds bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        if (!IsWithinChairRange(bounds)) return false;
        return TrySitOn(pendingChair);
    }

    private bool TryInteractFromRay(Ray ray)
    {
        // The seated controller is disabled, so also pick the visible character bounds.
        float characterDistance = Mathf.Infinity;
        foreach (var renderer in GetComponentsInChildren<Renderer>())
            if (renderer.enabled && renderer.bounds.IntersectRay(ray, out float distance))
                characterDistance = Mathf.Min(characterDistance, distance);
        var hits = Physics.RaycastAll(ray, Mathf.Infinity, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            // Movement boundaries are invisible and must not block pointer interaction.
            if (hit.transform.IsChildOf(transform) || characterDistance <= hit.distance)
                return PlayRandomInteraction();
            if (TrySitOn(hit.transform)) return true;
            var renderer = hit.collider.GetComponent<Renderer>();
            if (!renderer) renderer = hit.collider.GetComponentInChildren<Renderer>();
            if (renderer && renderer.enabled && renderer.gameObject.activeInHierarchy) return false;
        }
        return characterDistance < Mathf.Infinity && PlayRandomInteraction();
    }

    public bool PlayRandomInteraction()
    {
        if (!animator) return false;
        if (celebrating || celebrationRequested) return true;
        int state = InteractionStates[Random.Range(0, InteractionStates.Length)];
        if (!animator.HasState(0, state)) return false;
        StandUp();
        StopIdleAnimation();
        hasDestination = false;
        pendingChair = null;
        turnVelocity = 0f;
        SetWalking(false);
        interactionState = state;
        animator.Play(state, 0, 0f);
        return true;
    }

    private void StandUp()
    {
        if (!seated) return;
        seated = false;
        currentChair = null;
        transform.position = standingPosition;
        verticalSpeed = 0f;
        characterController.enabled = true;
        if (animator) animator.Play("Base Layer.Idle", 0, 0f);
    }

    private void SetWalking(bool value)
    {
        if (walking == value) return;
        walking = value;
        if (animator) animator.SetBool(IsWalking, walking);
    }

    private void OnDisable()
    {
        StopIdleAnimation();
        pendingChair = null;
        hasDestination = false;
        turnVelocity = 0f;
        walking = false;
        tapFinger = -1;
        touchGestureConsumed = false;
        if (animator) animator.SetBool(IsWalking, false);
    }

    private void OnDestroy()
    {
        if (economy) economy.QuestCompleted -= OnQuestCompleted;
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
