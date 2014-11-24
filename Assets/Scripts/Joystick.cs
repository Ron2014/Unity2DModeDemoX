using UnityEngine;
using System.Collections;

public class Joystick : MonoBehaviour {

	static private Joystick[] joysticks;
	static private bool enumeratedJoysticks = false;
	static private float tapTimeDelta = 0.3f;
	
	public bool touchPad;
	public Rect touchZone;
	public float deadZone = 0f;
	public bool normalize = false;
	public Vector2 position;
	public int tapCount;
	
	private int lastFingerId = -1;								// Finger last used for this joystick
	private float tapTimeWindow;							// How much time there is left for a tap to occur
	private Vector2 fingerDownPos;
	private float fingerDownTime;
	private float firstDeltaTime = 0.5f;

	private Rect defaultRect;
	private GUITexture gui;	
	private Bounds guiBoundary = new Bounds();
	private Vector2 guiTouchOffset;
	private Vector2 guiCenter;

#if !UNITY_IPHONE && !UNITY_ANDROID && !UNITY_WP8 && !UNITY_WP_8_1 && !UNITY_BLACKBERRY && !UNITY_TIZEN
	
	void Awake () {
		gameObject.SetActive (false);
	}
	
#else

	// Use this for initialization
	void Start () {
		// Cache this component at startup instead of looking up every frame
		gui = GetComponent<GUITexture> ();

		// Store the default rect for the gui, so we can snap back to it
		defaultRect = gui.pixelInset;
		
		defaultRect.x += transform.position.x * Screen.width;// + gui.pixelInset.x; // -  Screen.width * 0.5;
		defaultRect.y += transform.position.y * Screen.height;// - Screen.height * 0.5;
		
		Vector3 pos = transform.position;
		pos.x = 0f;
		pos.y = 0f;
		transform.position = pos;
		
		if (touchPad) {
			// If a texture has been assigned, then use the rect ferom the gui as our touchZone
			if (gui.texture)
				touchZone = defaultRect;
		}
		else {
			// This is an offset for touch input to match with the top left
			// corner of the GUI
			guiTouchOffset = new Vector2(defaultRect.width * 0.5f, defaultRect.height * 0.5f);
			
			// Cache the center of the GUI, since it doesn't change
			guiCenter = new Vector2(defaultRect.x + guiTouchOffset.x , defaultRect.y + guiTouchOffset.y);
			
			// Let's build the GUI boundary, so we can clamp joystick movement
			guiBoundary.min = new Vector2(defaultRect.x - guiTouchOffset.x, defaultRect.y - guiTouchOffset.y);
			guiBoundary.max = new Vector2(defaultRect.x + guiTouchOffset.x, defaultRect.y + guiTouchOffset.y);
		}
	}
	
	// Update is called once per frame
	void Update () {
		if (!enumeratedJoysticks) {
			// Collect all joysticks in the game, so we can relay finger latching messages
			joysticks = GameObject.FindObjectsOfType<Joystick>();
			enumeratedJoysticks = true;
		}
		
		var count = Input.touchCount;
		
		// Adjust the tap time window while it still available
		if (tapTimeWindow > 0)
			tapTimeWindow -= Time.deltaTime;
		else
			tapCount = 0;
		
		if (count == 0) {
			ResetJoystick ();
		}
		else {
			for (int i = 0; i < count; i++) {
				Touch touch = Input.GetTouch (i);
				Vector2 guiTouchPos = touch.position - guiTouchOffset;
				
				var shouldLatchFinger = false;
				if (touchPad) {
					if (touchZone.Contains (touch.position))
						shouldLatchFinger = true;
				}
				else if (gui.HitTest (touch.position)) {
					shouldLatchFinger = true;
				}
				
				// Latch the finger if this is a new touch
				if (shouldLatchFinger && (lastFingerId == -1 || lastFingerId != touch.fingerId)) {
					
					if (touchPad) {
						gui.color = new Color(gui.color.r, gui.color.g, gui.color.b, 0.15f);
						
						lastFingerId = touch.fingerId;
						fingerDownPos = touch.position;
						fingerDownTime = Time.time;
					}
					
					lastFingerId = touch.fingerId;
					
					// Accumulate taps if it is within the time window
					if (tapTimeWindow > 0) {
						tapCount++;
					}
					else {
						tapCount = 1;
						tapTimeWindow = tapTimeDelta;
					}
					
					// Tell other joysticks we've latched this finger
					foreach (Joystick j in joysticks) {
						if (j != null && j != this)
							j.LatchedFinger (touch.fingerId);
					}
				}
				
				if (lastFingerId == touch.fingerId) {
					// Override the tap count with what the iPhone SDK reports if it is greater
					// This is a workaround, since the iPhone SDK does not currently track taps
					// for multiple touches
					if (touch.tapCount > tapCount)
						tapCount = touch.tapCount;
					
					if (touchPad) {
						// For a touchpad, let's just set the position directly based on distance from initial touchdown
						position = new Vector2(Mathf.Clamp ((touch.position.x - fingerDownPos.x) / (touchZone.width * 0.5f), -1, 1),
						                       Mathf.Clamp ((touch.position.y - fingerDownPos.y) / (touchZone.height * 0.5f), -1, 1));
					}
					else {
						// Change the location of the joystick graphic to match where the touch is
						position = new Vector2((touch.position.x - guiCenter.x) / guiTouchOffset.x,
						                       (touch.position.y - guiCenter.y) / guiTouchOffset.y);
					}
					
					if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
						ResetJoystick ();
				}
			}
		}
		
		// Calculate the length. This involves a squareroot operation,
		// so it's slightly expensive. We re-use this length for multiple
		// things below to avoid doing the square-root more than one.
		float length = position.magnitude;
		
		
		if (length < deadZone) {
			// If the length of the vector is smaller than the deadZone radius,
			// set the position to the origin.
			position = Vector2.zero;
		}
		else {
			if (length > 1) {
				// Normalize the vector if its length was greater than 1.
				// Use the already calculated length instead of using Normalize().
				position = position / length;
			}
			else if (normalize) {
				// Normalize the vector and multiply it with the length adjusted
				// to compensate for the deadZone radius.
				// This prevents the position from snapping from zero to the deadZone radius.
				position = position / length * Mathf.InverseLerp (length, deadZone, 1);
			}
		}
		
		if (!touchPad) {
			// Change the location of the joystick graphic to match the position
			gui.pixelInset = new Rect( (position.x - 1) * guiTouchOffset.x + guiCenter.x,
			                          (position.y - 1) * guiTouchOffset.y + guiCenter.y,
			                          gui.pixelInset.width, gui.pixelInset.height);

		}
	
	}

	public void OnDisable () {
		gameObject.SetActive (false);
		enumeratedJoysticks = false;
	}

	public void ResetJoystick () {
		// Release the finger control and set the joystick back to the default position
		gui.pixelInset = defaultRect;
		lastFingerId = -1;
		position = Vector2.zero;
		fingerDownPos = Vector2.zero;
		
		if (touchPad) gui.color = new Color(gui.color.r, gui.color.g, gui.color.b, 0.025f);
	}

	public bool IsFingerDown (){
		return (lastFingerId != -1);
	}

	public void LatchedFinger (int fingerId) {
		// If another joystick has latched this finger, then we must release it
		if (lastFingerId == fingerId)
			ResetJoystick ();
	}

#endif
}
