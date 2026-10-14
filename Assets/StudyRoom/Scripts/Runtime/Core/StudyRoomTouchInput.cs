using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public static class StudyRoomTouchInput
{
    public struct Snapshot
    {
        public int Count, FirstId, SecondId;
        public Vector2 FirstPosition, SecondPosition;
        public bool Canceled;
    }

    public static Snapshot Read()
    {
        var snapshot = new Snapshot();
#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null)
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled) snapshot.Canceled = true;
                if (!touch.press.isPressed) continue;
                Add(ref snapshot, touch.touchId.ReadValue(), touch.position.ReadValue());
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
        for (int i = 0; i < Input.touchCount; i++)
        {
            var touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Canceled) snapshot.Canceled = true;
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) continue;
            Add(ref snapshot, touch.fingerId, touch.position);
        }
#endif
        return snapshot;
    }

    private static void Add(ref Snapshot snapshot, int id, Vector2 position)
    {
        if (snapshot.Count == 0) { snapshot.FirstId = id; snapshot.FirstPosition = position; }
        else if (snapshot.Count == 1) { snapshot.SecondId = id; snapshot.SecondPosition = position; }
        snapshot.Count++;
    }

    public static bool IsOverUI(Vector2 position)
    {
        if (!EventSystem.current) return false;
        var pointer = new PointerEventData(EventSystem.current) { position = position };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        return hits.Count > 0;
    }
}
