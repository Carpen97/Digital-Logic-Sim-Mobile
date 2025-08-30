using System;
using System.Collections.Generic;
using DLS.Graphics;
using Seb.Helpers;
using Seb.Types;
using Seb.Vis.UI;
using UnityEngine;
using ContextMenu = DLS.Graphics.ContextMenu;
using Object = UnityEngine.Object;

namespace DLS.Game
{
	public static class CameraController
	{
		public const float StartupOrthoSize = 1;

		static readonly float zoomSpeed = 1f;
		static readonly bool zoomToMouse = true;
		public static readonly Vector2 zoomRange = new(0.2f, 64);
		static Camera camera;
		static Transform camT;
		static bool isMovingCamera;
		static bool isDragZoomingCamera;
		static Vector2 dragZoomMousePrev;

		static Vector2 mouseDragScreenPosOld;

		static ViewState customizeView = new();
		static ViewState mainMenuView = new();
		public static ViewState activeView;
		static Dictionary<string, ViewState> chipViewStateLookup = new();
        private static float currentPinchDistance;
        private static bool isPinching;
        private static float initialPinchDistance;
        private static Vector2 initialPinchMidPoint;

        public static bool PinchFrameStarted { get; private set; }

        static bool CanMove => UIDrawer.ActiveMenu is UIDrawer.MenuType.None or UIDrawer.MenuType.BottomBarMenuPopup or UIDrawer.MenuType.ChipCustomization;
		static bool CanZoom => UIDrawer.ActiveMenu is UIDrawer.MenuType.None or UIDrawer.MenuType.BottomBarMenuPopup or UIDrawer.MenuType.ChipCustomization;
		static bool CanStartNewInput => !InteractionState.MouseIsOverUI;

		public static bool InChipView => UIDrawer.ActiveMenu != UIDrawer.MenuType.ChipCustomization;

        public static float PinchScale { get; private set; }

        public static void Reset()
		{
			chipViewStateLookup = new Dictionary<string, ViewState>();
			customizeView = new ViewState();
			mainMenuView = new ViewState();
			activeView = new ViewState();

			camera = Object.FindAnyObjectByType<Camera>();
			camT = camera.transform;

			AdjustCameraToScreen();
			UpdateCameraState();
			camera.backgroundColor = DrawSettings.ActiveTheme.BackgroundCol;
		}

		public static void Update()
		{
			ViewState newActiveViewState = GetActiveViewState();
			if (activeView != newActiveViewState)
			{
				activeView = newActiveViewState;
			}
			else
			{
				if (KeyboardShortcuts.ResetCameraShortcutTriggered)
				{
					chipViewStateLookup.Remove(Project.ActiveProject.ViewedChip.ChipName);
				}

				#if UNITY_ANDROID || UNITY_IOS
					if(!DLS.Graphics.CustomizationSceneDrawer.IsResizingChip &&
   					!DLS.Graphics.CustomizationSceneDrawer.IsPlacingDisplay &&
   					DLS.Graphics.CustomizationSceneDrawer.SelectedDisplay == null && 
					!UI.IsInteractingWithColorPicker &&
					!UI.IsScrolling &&
					!Project.ActiveProject.controller.isMovingWireEditPoint){


					//if(!DLS.Graphics.CustomizationSceneDrawer.IsResizingChip){
						Vector2 touchScreenPos = TouchInputHelper.Instance.TouchPosition;
						Vector2 touchWorldPos = camera.ScreenToWorldPoint(touchScreenPos);
						HandlePanInput(touchScreenPos, touchWorldPos);
						HandleZoomInputMobile();
					}
				#else
					// Editor, including when simulating Android
					Vector2 mouseScreenPos = InputHelper.MousePos;
					Vector2 mouseWorldPos = camera.ScreenToWorldPoint(mouseScreenPos);
					HandlePanInput(mouseScreenPos, mouseWorldPos);
					HandleZoomInput(mouseScreenPos);
				#endif

			}

			UpdateCameraState();
		}

		#if UNITY_ANDROID || UNITY_IOS
		static bool isTouchPanning;
		static Vector2 panTouchStartScreen;
		static Vector2 panTouchStartWorld;
        private static float zoomPrev =1f;

		static Vector2 t1StartPos;
		static Vector2 t2StartPos;
		static Vector2 startWorldMidPoint;
		#endif

		static void HandleZoomInputMobile()
		{
			if (Input.touchCount != 2) return;

			Touch t1 = Input.GetTouch(0);
			Touch t2 = Input.GetTouch(1);

			Vector2 touch1ScreenPos = t1.position;
			Vector2 touch2ScreenPos = t2.position;

			// When starting pinch
			if (t1.phase == TouchPhase.Began || t2.phase == TouchPhase.Began)
			{
				t1StartPos = touch1ScreenPos;
				t2StartPos = touch2ScreenPos;
				zoomPrev = activeView.OrthoSize;
		
				// Save the midpoint in world coordinates
				startWorldMidPoint = (camera.ScreenToWorldPoint(t1StartPos) + camera.ScreenToWorldPoint(t2StartPos)) * 0.5f;
			}
			else
			{
				float oldDist = Vector2.Distance(t1StartPos, t2StartPos);
				float newDist = Vector2.Distance(touch1ScreenPos, touch2ScreenPos);

				if (Mathf.Abs(oldDist) < 0.01f) return; // Avoid divide-by-zero

				float zoomFactor = oldDist / newDist;
				float newZoom = zoomPrev * zoomFactor;
				SetZoom(newZoom);

				// After zooming, keep the world midpoint under fingers
				Vector2 currentWorldMidPoint = (camera.ScreenToWorldPoint(touch1ScreenPos) + camera.ScreenToWorldPoint(touch2ScreenPos)) * 0.5f;
				Vector2 delta = startWorldMidPoint - currentWorldMidPoint;
				MovePosition(delta);
			}
		}


        static void HandleTouchInput(){

			if (Input.touchCount == 1){

				Touch touch = Input.GetTouch(0);
				Vector2 t = touch.position;
				if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.fingerId))
				{
					// Touch started on UI → ignore it
					return;
				}

				// If no seleceted components
				if (Project.ActiveProject.controller.SelectedElements.Count == 0)
				{

					//if (touch.phase == TouchPhase.Began)
					if (!isTouchPanning)
					{
						// Start of drag
						isTouchPanning = true;
						panTouchStartWorld = camera.ScreenToWorldPoint(t);
					}
					else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
					{
						// During drag
						Vector2 currentWorldPos = camera.ScreenToWorldPoint(t);
						Vector2 delta = panTouchStartWorld - currentWorldPos;
						MovePosition(delta);
					}
					else if (touch.phase == TouchPhase.Ended)
					{

					}
				}

			}
		}

		static void HandlePanInput(Vector2 mouseScreenPos, Vector2 mouseWorldPos)
		{
			if (CanMove)
			{
				#if UNITY_ANDROID || UNITY_IOS
				if (TouchInputHelper.Instance != null &&
		    		TouchInputHelper.Instance.Dragging &&
		    		Project.ActiveProject.controller.SelectedElements.Count == 0 &&
					!MobileUIController.Instance.isBoxSelectToolActive
				)
				{
					if (!isTouchPanning)
					{
						// Start of drag
						isTouchPanning = true;
						panTouchStartScreen = TouchInputHelper.Instance.TouchPosition;
						panTouchStartWorld = camera.ScreenToWorldPoint(panTouchStartScreen);
					}
					else
					{
						// During drag
						Vector2 currentScreen = TouchInputHelper.Instance.TouchPosition;
						Vector2 currentWorld = camera.ScreenToWorldPoint(currentScreen);
						Vector2 delta = panTouchStartWorld - currentWorld;
						MovePosition(delta);
					}
				}
				else
				{
					isTouchPanning = false;
				}
				#endif

				bool altLeftMouseDown = KeyboardShortcuts.CameraActionKeyHeld && InputHelper.IsMouseDownThisFrame(MouseButton.Left);
				bool middleMouseDown = InputHelper.IsMouseDownThisFrame(MouseButton.Middle);

				if ((altLeftMouseDown || middleMouseDown) && !isDragZoomingCamera)
				{
					mouseDragScreenPosOld = mouseScreenPos;
					isMovingCamera = CanStartNewInput;
					ContextMenu.CloseContextMenu();
				}

				if ((InputHelper.IsMouseHeld(MouseButton.Middle) || InputHelper.IsMouseHeld(MouseButton.Left)) && isMovingCamera)
				{
					Vector2 mouseWorldPosOld = camera.ScreenToWorldPoint(mouseDragScreenPosOld);
					MovePosition(mouseWorldPosOld - mouseWorldPos);
					mouseDragScreenPosOld = mouseScreenPos;
				}
			}

			// Release
			if (InputHelper.IsMouseUpThisFrame(MouseButton.Middle) || InputHelper.IsMouseUpThisFrame(MouseButton.Left))
			{
				isMovingCamera = false;
				#if UNITY_ANDROID || UNITY_IOS
				isTouchPanning = false;
				#endif
			}
		}

				// Zoom with middle mouse scroll, or alt+right-mouse drag
		static void HandleZoomInput(Vector2 mouseScreenPos)
		{
			if (CanStartNewInput && CanZoom)
			{
				Vector2 mouseWorldPosAfterPanning = camera.ScreenToWorldPoint(mouseScreenPos);
				float zoomPrev = activeView.OrthoSize;
				float targetZoom = zoomPrev;

				if (isDragZoomingCamera)
				{
					Vector2 delta = mouseScreenPos - dragZoomMousePrev;
					dragZoomMousePrev = mouseScreenPos;
					float zoomDeltaRaw = -delta.magnitude * Mathf.Sign(Mathf.Abs(delta.x) > Mathf.Abs(delta.y) ? delta.x : -delta.y);
					float zoomDelta = zoomDeltaRaw / Screen.width * zoomSpeed * 5 * zoomPrev;
					targetZoom = zoomPrev + zoomDelta;
				}
				else if (CanMiddleMouseZoom())
				{
					float deltaZoom = -InputHelper.MouseScrollDelta.y * zoomPrev * zoomSpeed * 0.1f;
					targetZoom = zoomPrev + deltaZoom;
				}

				SetZoom(targetZoom);

				if (zoomPrev != activeView.OrthoSize)
				{
					ContextMenu.CloseContextMenu();

					if (zoomToMouse && CanMove && !isDragZoomingCamera)
					{
						Vector2 mouseWorldPosAfterZoom = camera.ScreenToWorldPoint(mouseScreenPos);
						MovePosition(mouseWorldPosAfterPanning - mouseWorldPosAfterZoom);
					}
				}

				if (InputHelper.IsMouseDownThisFrame(MouseButton.Right) && KeyboardShortcuts.CameraActionKeyHeld && !isMovingCamera)
				{
					isDragZoomingCamera = true;
					dragZoomMousePrev = mouseScreenPos;
				}
			}

			if (InputHelper.IsMouseUpThisFrame(MouseButton.Right))
			{
				isDragZoomingCamera = false;
			}
		}

		// shift scroll reserved for adjusting spacing when placing multiple elements
		static bool CanMiddleMouseZoom() => !(InputHelper.ShiftIsHeld && Project.ActiveProject.controller.IsPlacingElements) && InputHelper.IsMouseInGameWindow();

		static void MovePosition(Vector2 delta)
		{
			activeView.Pos += delta;

			UpdateCameraState();
		}

		static void SetZoom(float zoom)
		{
			activeView.OrthoSize = Math.Clamp(zoom, zoomRange.x, zoomRange.y);

			UpdateCameraState();
		}

		static void UpdateCameraState()
		{
			Vector2 pos2D = activeView.Pos;
			camT.position = new Vector3(pos2D.x, pos2D.y, -10);
			camera.orthographicSize = activeView.OrthoSize;
		}

		static ViewState GetActiveViewState()
		{
			ViewState activeOld = activeView;
			ViewState activeViewNew;

			if (UIDrawer.ActiveMenu == UIDrawer.MenuType.ChipCustomization)
			{
				if (activeOld != customizeView)
				{
					customizeView = new ViewState();
				}

				activeViewNew = customizeView;
			}
			else if (UIDrawer.ActiveMenu == UIDrawer.MenuType.MainMenu)
			{
				activeViewNew = mainMenuView;
			}
			else
			{
				DevChipInstance viewedChip = Project.ActiveProject.ViewedChip;
				string viewedChipKey = viewedChip.ChipName;

				if (!chipViewStateLookup.TryGetValue(viewedChipKey, out activeViewNew))
				{
					activeViewNew = GetViewForChip(viewedChip);
					chipViewStateLookup.Add(viewedChipKey, activeViewNew);
				}
			}

			return activeViewNew;
		}

		// Calculate view params to fit given chip into screen (or as much as possible within max zoom setting)
		public static ViewState GetViewForChip(DevChipInstance viewedChip)
		{
			Bounds2D bounds = Bounds2D.CreateEmpty();
			foreach (IMoveable element in viewedChip.Elements)
			{
				bounds = Bounds2D.Grow(bounds, element.SelectionBoundingBox);
			}

			foreach (WireInstance wire in viewedChip.Wires)
			{
				float wireWidth = (int)wire.bitCount * DrawSettings.WireThickness;
				for (int i = 1; i < wire.WirePointCount - 1; i++)
				{
					Vector2 a = wire.GetWirePoint(i);
					Vector2 b = wire.GetWirePoint(i + 1);
					Vector2 dir = (b - a).normalized;
					Vector2 perp = new(-dir.y, dir.x);
					bounds = Bounds2D.Grow(bounds, a + perp * wireWidth / 2);
					bounds = Bounds2D.Grow(bounds, a - perp * wireWidth / 2);
				}
			}

			ViewState view = new();
			if (Mathf.Max(bounds.Size.x, bounds.Size.y) < DrawSettings.GridSize) return view;

			// Set cam orthoSize to fit contents of chip on screen
			view.OrthoSize = Mathf.Max(bounds.Height, bounds.Width * 9 / 16f) * 0.5f;
			view.OrthoSize += view.OrthoSize * 0.1f; // Padding
			view.OrthoSize = Mathf.Clamp(view.OrthoSize, zoomRange.x, zoomRange.y);

			// Move cam down slightly from bounds centre to account for bottom region of screen blocked by chip bar
			const float uiScreenHeight = UI.Width * 9 / 16f;
			float bottomBarWorldHeight = BottomBarUI.barHeight / uiScreenHeight * view.OrthoSize * 2;
			view.Pos = bounds.Centre + Vector2.down * bottomBarWorldHeight / 2;
			return view;
		}

		static void AdjustCameraToScreen()
		{
			if (camera == null) return;

			float screenAspect = (float)Screen.width / Screen.height;
			float targetAspect = 16f / 9f; // Designed for 16:9 aspect ratio

			// Expand camera size if screen is wider than target
			if (screenAspect > targetAspect)
			{
				float scaleFactor = screenAspect / targetAspect;
				activeView.OrthoSize = StartupOrthoSize * scaleFactor;
			}
			else
			{
				activeView.OrthoSize = StartupOrthoSize;
			}

			UpdateCameraState();
		}


		public static void NotifyChipNameChanged(string nameNew)
		{
			chipViewStateLookup[nameNew] = activeView;
		}

		public class ViewState
		{
			public float OrthoSize = StartupOrthoSize;
			public Vector2 Pos = Vector2.zero;
		}
	}
}