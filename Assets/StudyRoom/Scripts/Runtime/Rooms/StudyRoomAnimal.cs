using UnityEngine;

[DisallowMultipleComponent]
public class StudyRoomAnimal : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float moveSpeed = 0.25f;
    [SerializeField] private string movementState = "Walk";
    private StudyRoomDecorations room;
    private Animator animator;
    private Vector3 destination;
    private float waitTime;
    private float radius, height, groundOffset;
    private bool moving;

    public void SetMovementAnimation(string state) => movementState = state;

    public void Initialize(StudyRoomDecorations owner, bool activate = true)
    {
        room = owner;
        animator = GetComponentInChildren<Animator>();
        if (animator)
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Play("Idle_A", 0, 0f);
            animator.Update(0f);
        }
        var renderers = GetComponentsInChildren<Renderer>();
        Bounds bounds = new Bounds(transform.position, Vector3.one * 0.2f);
        if (renderers.Length > 0)
        {
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        }
        radius = Mathf.Max(0.06f, Mathf.Max(bounds.extents.x, bounds.extents.z));
        height = Mathf.Max(0.12f, bounds.size.y);
        groundOffset = bounds.min.y - transform.position.y;
        moving = false;
        waitTime = Random.Range(0.5f, 2f);
        enabled = activate;
    }

    public Bounds BoundsAt(Vector3 position) => new Bounds(
        position + Vector3.up * (groundOffset + height * 0.5f),
        new Vector3(radius * 2f, height, radius * 2f));

    private void Update() => Tick(Time.deltaTime);

    private void Tick(float elapsed)
    {
        if (!room || elapsed <= 0f) return;
        if (room.IsPlacing || room.IsEditing)
        {
            SetMoving(false);
            waitTime = 0.5f;
            return;
        }
        if (!moving)
        {
            waitTime -= elapsed;
            if (waitTime <= 0f) ChooseDestination();
            return;
        }
        Vector3 direction = destination - transform.position;
        direction.y = 0f;
        float distance = direction.magnitude;
        if (distance < 0.03f) { Rest(); return; }
        Vector3 next = transform.position + direction.normalized * Mathf.Min(distance, moveSpeed * Mathf.Min(elapsed, 0.1f));
        if (!room.CanAnimalMove(this, transform.position, next)) { Rest(); return; }
        transform.rotation = Quaternion.RotateTowards(transform.rotation,
            Quaternion.LookRotation(direction), 150f * elapsed);
        transform.position = next;
    }

    private void ChooseDestination()
    {
        for (int attempt = 0; attempt < 48; attempt++)
        {
            Vector2 offset = Random.insideUnitCircle * (attempt < 24 ? 1.2f : 4f);
            if (offset.sqrMagnitude < 0.16f) continue;
            Vector3 candidate = transform.position + new Vector3(offset.x, 0f, offset.y);
            if (!room.CanAnimalOccupy(this, candidate)) continue;
            int steps = Mathf.CeilToInt(offset.magnitude / 0.08f);
            bool clear = true;
            for (int i = 1; i <= steps; i++)
                if (!room.CanAnimalMove(this,
                    Vector3.Lerp(transform.position, candidate, (float)(i - 1) / steps),
                    Vector3.Lerp(transform.position, candidate, (float)i / steps)))
                { clear = false; break; }
            if (!clear) continue;
            destination = candidate;
            SetMoving(true);
            return;
        }
        Rest();
    }

    private void Rest()
    {
        SetMoving(false);
        waitTime = Random.Range(1.5f, 4f);
    }

    private void SetMoving(bool value)
    {
        if (moving == value) return;
        moving = value;
        if (!animator) return;
        animator.speed = value ? 0.65f : 0.8f;
        animator.CrossFadeInFixedTime(value ? movementState : "Idle_A", 0.15f, 0, 0f);
    }
}
