using Seb.Helpers.InputHandling;
using Seb.Types;
using UnityEngine;

namespace Seb.Helpers
{
	public enum MouseButton
	{
		Left = 0,
		Right = 1,
		Middle = 2
	}

	public static class InputHelper
	{
		public static IInputSource InputSource = new UnityInputSource();

		static Camera _worldCam;
		static Vector2 prevWorldMousePos;
		static int prevWorldMouseFrame = -1;
		static int leftMouseDownConsumeFrame = -1;
		static int rightMouseDownConsumeFrame = -1;
		static int middleMouseDownConsumeFrame = -1;

		public static Vector2 MousePos => InputSource.MousePosition; // Screen-space mouse position
		public static string InputStringThisFrame => InputSource.InputString;
		public static bool AnyKeyOrMouseDownThisFrame => InputSource.AnyKeyOrMouseDownThisFrame;
		public static bool AnyKeyOrMouseHeldThisFrame => InputSource.AnyKeyOrMouseHeldThisFrame;
		public static Vector2 MouseScrollDelta => InputSource.MouseScrollDelta;

		public static Camera WorldCam
		{
			get
			{
				if (_worldCam == null)
				{
					_worldCam = Camera.main;
					if (_worldCam == null)
					{
						// Fallback to any enabled camera to avoid null deref on startup scenes
						if (Camera.allCamerasCount > 0)
						{
							_worldCam = Camera.allCameras[0];
						}
					}
				}
				return _worldCam;
			}
		}

		public static Vector2 MousePosWorld
		{
			get
			{
				if (Time.frameCount != prevWorldMouseFrame)
				{
					prevWorldMousePos = SafeScreenToWorldPoint(MousePos, prevWorldMousePos);
					prevWorldMouseFrame = Time.frameCount;
				}
				return prevWorldMousePos;
			}
		}

		public static bool ShiftIsHeld => IsKeyHeld(KeyCode.LeftShift) || IsKeyHeld(KeyCode.RightShift);
		public static bool CtrlIsHeld => IsKeyHeld(KeyCode.LeftControl) || IsKeyHeld(KeyCode.RightControl);
		public static bool AltIsHeld => IsKeyHeld(KeyCode.LeftAlt) || IsKeyHeld(KeyCode.RightAlt);

		public static bool IsKeyDownThisFrame(KeyCode key) => InputSource.IsKeyDownThisFrame(key);
		public static bool IsKeyUpThisFrame(KeyCode key) => InputSource.IsKeyUpThisFrame(key);
		public static bool IsKeyHeld(KeyCode key) => InputSource.IsKeyHeld(key);

		public static bool IsMouseInGameWindow()
		{
			var cam = WorldCam;
			Vector2 mp = MousePos;

			// If camera exists, prefer its pixelRect (multi-display / letterboxed setups)
			if (cam != null)
			{
				var r = cam.pixelRect;
				return IsFinite(mp.x) && IsFinite(mp.y) && r.Contains(mp);
			}

			// Fallback to Screen bounds
			return IsFinite(mp.x) && IsFinite(mp.y) && mp.x >= 0 && mp.y >= 0 && mp.x < Screen.width && mp.y < Screen.height;
		}

		public static bool MouseInBounds_ScreenSpace(Vector2 centre, Vector2 size)
		{
			if (!Application.isPlaying) return false;
			Vector2 offset = MousePos - centre;
			return Mathf.Abs(offset.x) < size.x / 2 && Mathf.Abs(offset.y) < size.y / 2;
		}

		public static bool MouseInBounds_ScreenSpace(Bounds2D bounds)
		{
			if (!Application.isPlaying) return false;
			return bounds.PointInBounds(MousePos);
		}

		public static bool MouseInPoint_ScreenSpace(Vector2 centre, float radius)
		{
			if (!Application.isPlaying) return false;
			Vector2 offset = MousePos - centre;
			return offset.sqrMagnitude < radius * radius;
		}

		public static bool MouseInsidePoint_World(Vector2 centre, float radius)
		{
			if (!Application.isPlaying) return false;
			Vector2 offset = MousePosWorld - centre;
			return offset.sqrMagnitude < radius * radius;
		}

		public static bool MouseInsideBounds_World(Vector2 centre, Vector2 size)
		{
			if (!Application.isPlaying) return false;
			Vector2 offset = MousePosWorld - centre;
			return Mathf.Abs(offset.x) < size.x / 2 && Mathf.Abs(offset.y) < size.y / 2;
		}

		public static bool MouseInsideBounds_World(Bounds2D bounds)
		{
			if (!Application.isPlaying) return false;
			return bounds.PointInBounds(MousePosWorld);
		}

		public static bool IsMouseHeld(MouseButton button)
		{
			if (!Application.isPlaying) return false;
			return InputSource.IsMouseHeld(button);
		}

		// Check if mouse button was pressed this frame. Optionally consume the event, so it will return false for other callers this frame.
		public static bool IsMouseDownThisFrame(MouseButton button, bool consumeEvent = false)
		{
			if (!Application.isPlaying) return false;
			if (MouseDownEventIsConsumed(button)) return false;

			if (consumeEvent)
			{
				ConsumeMouseButtonDownEvent(button);
			}

			return InputSource.IsMouseDownThisFrame(button);
		}

		// Check if any mouse button was pressed this frame, even if the event was consumed.
		public static bool IsAnyMouseButtonDownThisFrame_IgnoreConsumed()
		{
			if (!Application.isPlaying) return false;
			return InputSource.IsMouseDownThisFrame(MouseButton.Left) || InputSource.IsMouseDownThisFrame(MouseButton.Right) || InputSource.IsMouseDownThisFrame(MouseButton.Middle);
		}

		// Consume mouse down event (the mouse event will report false on all subsequent calls this frame)
		public static void ConsumeMouseButtonDownEvent(MouseButton button)
		{
			if (button == MouseButton.Left)
			{
				leftMouseDownConsumeFrame = Time.frameCount;
			}
			else if (button == MouseButton.Right)
			{
				rightMouseDownConsumeFrame = Time.frameCount;
			}
			else if (button == MouseButton.Middle)
			{
				middleMouseDownConsumeFrame = Time.frameCount;
			}
		}

		static bool MouseDownEventIsConsumed(MouseButton button)
		{
			int lastConsumedFrame = button switch
			{
				MouseButton.Left => leftMouseDownConsumeFrame,
				MouseButton.Right => rightMouseDownConsumeFrame,
				MouseButton.Middle => middleMouseDownConsumeFrame,
				_ => -1
			};
			return Time.frameCount == lastConsumedFrame;
		}

		public static bool IsMouseUpThisFrame(MouseButton button)
		{
			if (!Application.isPlaying) return false;
			return InputSource.IsMouseUpThisFrame(button);
		}

		public static void CopyToClipboard(string s) => GUIUtility.systemCopyBuffer = s;
		public static string GetClipboardContents() => GUIUtility.systemCopyBuffer;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		static void Reset()
		{
			_worldCam = null;
			prevWorldMouseFrame = -1;
			leftMouseDownConsumeFrame = -1;
			rightMouseDownConsumeFrame = -1;
			middleMouseDownConsumeFrame = -1;
			InputSource = new UnityInputSource();
		}

		// ---------- Helpers ----------

		static bool IsFinite(float v) => !(float.IsNaN(v) || float.IsInfinity(v));

		/// <summary>
		/// Converts a screen position to world while guarding against NaN/Inf and out-of-frustum values.
		/// If input is invalid or camera missing, returns the provided fallback (usually previous cached value).
		/// </summary>
		static Vector2 SafeScreenToWorldPoint(Vector2 screenPos, Vector2 fallback)
		{
			var cam = WorldCam;
			if (cam == null) return fallback;

			// Reject invalid input early
			if (!IsFinite(screenPos.x) || !IsFinite(screenPos.y))
				return fallback;

			// Optionally skip when mouse is outside the camera rect (prevents inf/inf during resizes)
			var rect = cam.pixelRect;
			if (!rect.Contains(screenPos))
			{
				// Either clamp, or return fallback. Clamping avoids stalling when slightly OOB.
				screenPos.x = Mathf.Clamp(screenPos.x, rect.xMin, rect.xMax);
				screenPos.y = Mathf.Clamp(screenPos.y, rect.yMin, rect.yMax);
			}

			// Build a Vector3 with a sane Z for ScreenToWorldPoint
			Vector3 sp = new Vector3(screenPos.x, screenPos.y, 0f);

			if (cam.orthographic)
			{
				// For orthographic cameras, z component adds to camera.z; we only need x/y for a Vector2 result.
				// Keep z = 0 to avoid precision issues.
				sp.z = 0f;
			}
			else
			{
				// For perspective: distance from camera to the 2D gameplay plane (usually z = 0).
				float distToPlane = Mathf.Abs(cam.transform.position.z);
				// Ensure it's in front of the near clip
				sp.z = Mathf.Max(cam.nearClipPlane, distToPlane);
			}

			Vector3 world = cam.ScreenToWorldPoint(sp);
			// Only x/y are used by callers
			return new Vector2(world.x, world.y);
		}
	}
}
