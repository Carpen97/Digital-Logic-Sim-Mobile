using UnityEngine;

public static class TouchInputHelper {
    
    public static bool TouchTapDown() {
        if (Input.touchCount > 0) {
            return Input.GetTouch(0).phase == TouchPhase.Began;
        }
        return false;
    }

    public static bool TouchTapHeld() {
        if (Input.touchCount > 0) {
            TouchPhase phase = Input.GetTouch(0).phase;
            return phase == TouchPhase.Stationary || phase == TouchPhase.Moved;
        }
        return false;
    }

    public static bool TouchTapUp() {
        if (Input.touchCount > 0) {
            return Input.GetTouch(0).phase == TouchPhase.Ended;
        }
        return false;
    }

    public static Vector2 TouchPositionWorld() {
        if (Input.touchCount > 0) {
            Vector2 screenPos = Input.GetTouch(0).position;
            return Camera.main.ScreenToWorldPoint(screenPos);
        }
        return Vector2.zero;
    }
}
