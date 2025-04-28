using UnityEngine;

public class TouchInputHelper : MonoBehaviour {
	public static TouchInputHelper Instance { get; private set; }
	private Vector2 touchStartPos;
	private Vector2 touchCurrentPos;
	private float touchStartTime;

	float initialPinchDistance;
	float currentPinchDistance;
	public bool PinchFrameStarted;
	private bool isDragging;
	private bool isPinching;
	private float lastPinchDistance;

	public bool TapDetected { get; private set; }
	public bool LongPressDetected { get; private set; }
	public bool isPressingUI { get; private set; }
	public bool Dragging => isDragging;
	public bool Pinching => isPinching;

	public Vector2 TouchPosition => touchCurrentPos;
	public Vector2 DragDelta => isDragging ? (touchCurrentPos - touchStartPos) : Vector2.zero;
	public float PinchScale { get; private set; }

	private const float longPressThreshold = 0.5f;
	private const float dragThreshold = 10f;

	public Vector2 TouchStartScreenPosition { get; private set; }
    public bool TouchBeganThisFrame { get; internal set; }
    
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
	public Vector2 TouchWorldPosition
	{
		get
		{
			if (Camera.main == null)
				return Vector2.zero;

			return Camera.main.ScreenToWorldPoint(touchCurrentPos);
		}
	}
	public Vector2 TouchStartPosition
	{
		get
		{
			if (Camera.main == null)
				return Vector2.zero;

			return Camera.main.ScreenToWorldPoint(touchStartPos);
		}
	}


    void Awake()
	{
		if (Instance == null) Instance = this;
		else Destroy(gameObject);
	}

	void Update()
	{
		TapDetected = false;
		LongPressDetected = false;
		PinchFrameStarted = false;
		PinchScale = 1f;


		#if UNITY_EDITOR
		// Simulate tap and drag with mouse in editor
		if (Input.GetMouseButtonDown(0))
		{
			touchStartPos = Input.mousePosition;
			touchCurrentPos = Input.mousePosition;
			touchStartTime = Time.time;
			isDragging = false;
		}
		else if (Input.GetMouseButton(0))
		{
			touchCurrentPos = Input.mousePosition;
			if (!isDragging && Vector2.Distance(touchCurrentPos, touchStartPos) > dragThreshold)
				isDragging = true;
			if (!isDragging && Time.time - touchStartTime > longPressThreshold)
				LongPressDetected = true;
		}
		else if (Input.GetMouseButtonUp(0))
		{
			if (!isDragging && Time.time - touchStartTime <= longPressThreshold)
				TapDetected = true;
			isDragging = false;
		}
		#else
		if (Input.touchCount == 1)
		{
			Touch touch = Input.GetTouch(0);
			if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.fingerId))
			{
				isPressingUI = true;
				return;
			}
			isPressingUI = false;
			touchCurrentPos = touch.position;

			if (touch.phase == TouchPhase.Began)
			{
				touchStartPos = touch.position;
				TouchStartScreenPosition = touch.position;
				touchStartTime = Time.time;
				isDragging = false;
				TouchBeganThisFrame = true;	
			}
			else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
			{
				if (!isDragging && Vector2.Distance(touch.position, touchStartPos) > dragThreshold)
					isDragging = true;
				if (!isDragging && Time.time - touchStartTime > longPressThreshold)
					LongPressDetected = true;

				TouchBeganThisFrame = false;	
			}
			else if (touch.phase == TouchPhase.Ended)
			{
				if (!isDragging && Time.time - touchStartTime <= longPressThreshold)
					TapDetected = true;
				isDragging = false;
				TouchBeganThisFrame = false;	
			}

			isPinching = false;
		}
		else if (Input.touchCount == 2)
		{
			Touch touch1 = Input.GetTouch(0);
			Touch touch2 = Input.GetTouch(1);
			Vector2 t2 = touch1.position;
			Vector2 t1 = touch2.position;
			if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch1.fingerId) || 
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch1.fingerId))
			{
				// Touch started on UI → ignore it
				Debug.Log("Touch UI");
				return;
			}
			currentPinchDistance = Vector2.Distance(t1, t2);

			if (!isPinching)
			{
				isPinching = true;
				initialPinchDistance = currentPinchDistance;
				PinchFrameStarted = true;
				PinchScale = 1f;
			}
			else
			{
				PinchScale = currentPinchDistance / initialPinchDistance;
				PinchFrameStarted = false;
			}
		}
		else
		{
			isPinching = false;
		}
	#endif
	}
}
