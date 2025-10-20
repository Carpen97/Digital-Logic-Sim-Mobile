using System;
using DLS.Description;
using DLS.Game;
using Seb.Helpers;
using Seb.Types;
using Seb.Vis;
using UnityEngine;
using System.Linq;
using Seb.Vis.UI;
using System.Collections.Generic;

namespace DLS.Graphics
{
	public static class CustomizationSceneDrawer
	{
		static Vector2Int selectedChipResizeDir;
		static Vector2 chipResizeMouseStartPos;
		static Vector2 chipResizeStartSize;

		static DisplayInteractState displayInteractState;
		public static DisplayInstance SelectedDisplay;
		static DisplayInstance DisplayUnderMouse;
		static Vector2 displayMoveMouseOffset;
		static Vector2 mouseDownPos;
		static Vector2 displayPosInitial;
		static float displayScaleInitial;
        public static PinInstance selectedPin;
        public static bool isDraggingPin;
        public static bool isPinPositionValid;
        static readonly float minPinSpacing = 0.025f;

	// Polygon editing state
	static int selectedPolygonVertex = -1;
	static int selectedPolygonEdge = -1;
	static bool isDraggingVertex = false;
	static bool isDraggingEdgeCurve = false;
	static bool isDraggingRotation = false;
	static Vector2 rotationHandleStartMouse;
	static float rotationHandleStartAngle;

 		public static bool IsResizingChip => selectedChipResizeDir != Vector2Int.zero;
		static SubChipInstance CustomizeChip => ChipSaveMenu.ActiveCustomizeChip;
		public static bool IsPlacingDisplay => displayInteractState == DisplayInteractState.Placing;

		public static void DrawCustomizationScene()
		{
			SubChipInstance chip = ChipSaveMenu.ActiveCustomizeChip;
			HandleKeyboardShortcuts();

			DevSceneDrawer.DrawSubChip(chip);
			WorldDrawer.DrawGridIfActive(ColHelper.MakeCol255(0, 0, 0, 100));

			Draw.StartLayer(Vector2.zero, 1, false);
			DevSceneDrawer.DrawSubchipDisplays(chip, null, true);

		bool chipResizeHascontrol = HandleChipResizing(chip);
		HandleDisplaySelection(!chipResizeHascontrol);

		HandlePinDragging();
		HandlePolygonEditing(chip);

		if (SelectedDisplay == null)
			{
				if (DisplayUnderMouse != null) HandleDeleteDisplayUnderMouse();
			}
			else
			{
				if (displayInteractState == DisplayInteractState.Scaling)
				{
					HandleDisplayScaling();
				}
				else
				{
					HandleDisplayMovement();
				}
			}

			// Display highlighted pin name
			if (InteractionState.ElementUnderMouse is PinInstance highlightedPin)
			{
				Draw.StartLayer(Vector2.zero, 1, false);
				DevSceneDrawer.DrawPinLabel(highlightedPin);
			}
		}

		static void HandleKeyboardShortcuts()
		{
			
		}

		public static void StartPlacingDisplay(SubChipInstance subChipToDisplay)
		{
			SelectedDisplay = new DisplayInstance();
			SelectedDisplay.Desc = new DisplayDescription(subChipToDisplay.ID, Vector2.zero, 1);
			SelectedDisplay.ChildDisplays = subChipToDisplay.Displays;

			displayInteractState = DisplayInteractState.Placing;
			displayMoveMouseOffset = Vector2.zero;
			#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
			MobileUIControllerWrapper.ShowPlacementButtons(
				confirmPlacement,
				cancelPlacement
			);
			#endif
		}

		public static void OnCustomizationMenuClosed()
		{
			selectedChipResizeDir = Vector2Int.zero;
		}

		public static void OnCustomizationMenuOpened()
		{
		}

		static void HandleDisplayScaling()
		{

			#if UNITY_ANDROID || UNITY_IOS
			Touch touch = Input.GetTouch(0);
			if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.fingerId) ||
				InteractionState.MouseIsOverUI) return;
			# endif
			Draw.StartLayer(Vector2.zero, 1, false);

			Color scaleCol = new(0.4f, 1, 0.2f);
			float deltaScale = (mouseDownPos - InputHelper.MousePosWorld).magnitude;
			deltaScale *= Vector2.Dot((InputHelper.MousePosWorld - mouseDownPos).normalized, (displayPosInitial - mouseDownPos).normalized);
			float targetScale = Mathf.Max(DrawSettings.GridSize, displayScaleInitial - deltaScale);

			if (!Project.ActiveProject.ShouldSnapToGrid)
			{
				SelectedDisplay.Desc.Scale = targetScale;
			}


			Bounds2D bounds = DevSceneDrawer.DrawDisplayWithBackground(SelectedDisplay, Vector2.zero, ChipSaveMenu.ActiveCustomizeChip);
			DrawDisplayBoundsIndicators(bounds, scaleCol);

			if (Project.ActiveProject.ShouldSnapToGrid)
			{
				float unscaledWidth = bounds.Width / SelectedDisplay.Desc.Scale;
				float scaledWidth = unscaledWidth * targetScale;

				float snappedWidth = GridHelper.SnapToGrid(scaledWidth);
				float snappedScale = snappedWidth / unscaledWidth;

				SelectedDisplay.Desc.Scale = snappedScale;
			}

			// Exit (confirm/cancel)
			bool cancel = KeyboardShortcuts.CancelShortcutTriggered || InputHelper.IsMouseDownThisFrame(MouseButton.Right);

			if (cancel)
			{
				SelectedDisplay.Desc.Position = displayPosInitial;
				SelectedDisplay.Desc.Scale = displayScaleInitial;
				CustomizeChip.Displays.Add(SelectedDisplay);
				SelectedDisplay = null;
				displayInteractState = DisplayInteractState.None;
			}
			else
			{
				bool confirm = InputHelper.IsMouseUpThisFrame(MouseButton.Left);

				if (confirm)
				{
					ChipSaveMenu.ActiveCustomizeChip.Displays.Add(SelectedDisplay);
					SelectedDisplay = null;
					displayInteractState = DisplayInteractState.None;
				}
			}
			#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
			MobileUIControllerWrapper.HidePlacementButtons();
			#endif
		}

		static void HandleDisplayMovement()
		{
			#if UNITY_ANDROID || UNITY_IOS
			if(Input.touchCount == 1){
				Touch touch = Input.GetTouch(0);
				if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.fingerId) ||
					InteractionState.MouseIsOverUI) return;
			}
			# endif
			Draw.StartLayer(Vector2.zero, 1, false);
			Vector2 targetPos = InputHelper.MousePosWorld + displayMoveMouseOffset;

			if (!Project.ActiveProject.ShouldSnapToGrid)
			{
				SelectedDisplay.Desc.Position = targetPos;
			}

			Bounds2D bounds = DevSceneDrawer.DrawDisplayWithBackground(SelectedDisplay, Vector2.zero, ChipSaveMenu.ActiveCustomizeChip);
			DrawDisplayBoundsIndicators(bounds, Color.white);

			if (Project.ActiveProject.ShouldSnapToGrid)
			{
				Vector2 snapPointOffset = bounds.TopLeft - bounds.Centre;
				Vector2 snap = GridHelper.SnapMovingElementToGrid(targetPos, snapPointOffset, true, true);
				SelectedDisplay.Desc.Position = snap;
			}

			bool cancelMovement = KeyboardShortcuts.CancelShortcutTriggered || InputHelper.IsMouseDownThisFrame(MouseButton.Right);
			bool delete = InputHelper.IsKeyDownThisFrame(KeyCode.Backspace) || InputHelper.IsKeyDownThisFrame(KeyCode.Delete);

			if (cancelMovement || delete)
			{
				SelectedDisplay.Desc.Position = displayPosInitial;
				if (!delete && displayInteractState == DisplayInteractState.Moving) CustomizeChip.Displays.Add(SelectedDisplay);
				SelectedDisplay = null;
				displayInteractState = DisplayInteractState.None;
			}
			else
			{
				#if !(UNITY_ANDROID || UNITY_IOS)
				// Confirm placement
				bool confirmPlacement = InputHelper.IsMouseDownThisFrame(MouseButton.Left);
				confirmPlacement |= displayInteractState == DisplayInteractState.Moving && InputHelper.IsMouseUpThisFrame(MouseButton.Left);

				if (confirmPlacement)
				{
					ChipSaveMenu.ActiveCustomizeChip.Displays.Add(SelectedDisplay);
					SelectedDisplay = null;
					displayInteractState = DisplayInteractState.None;
				}
				#endif
			}
		}

		static void confirmPlacement(){
			if(SelectedDisplay != null)
				ChipSaveMenu.ActiveCustomizeChip.Displays.Add(SelectedDisplay);
			SelectedDisplay = null;
			displayInteractState = DisplayInteractState.None;
			#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
			MobileUIControllerWrapper.HidePlacementButtons();
			#endif
		}

		static void cancelPlacement(){
			if(SelectedDisplay != null)
				SelectedDisplay.Desc.Position = displayPosInitial;
			SelectedDisplay = null;
			displayInteractState = DisplayInteractState.None;
			#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
			MobileUIControllerWrapper.HidePlacementButtons();
			#endif
		}

		static void HandleDeleteDisplayUnderMouse()
		{
			bool delete = InputHelper.IsKeyDownThisFrame(KeyCode.Backspace) || InputHelper.IsKeyDownThisFrame(KeyCode.Delete);
			if (delete)
			{
				CustomizeChip.Displays.Remove(DisplayUnderMouse);
				DisplayUnderMouse = null;
			}
		}

		static void HandleDisplaySelection(bool canSelect)
		{
			DisplayUnderMouse = null;
			if (!canSelect) return;

			const float v = 0.85f;
			Color mouseOverIndicatorCol = new(v, v, v, 1);
			Color mouseOverIndicatorScaleCol = new(1, 0.8f, 0.2f);

			foreach (DisplayInstance display in CustomizeChip.Displays)
			{
				Bounds2D bounds = display.LastDrawBounds;
				float displayMinAxisSize = Mathf.Min(bounds.Width, bounds.Height);

				if (displayInteractState == DisplayInteractState.None)
				{
					if (InputHelper.MouseInsideBounds_World(bounds))
					{
						DisplayUnderMouse = display;

						float cornerDst = bounds.DstToCorner(InputHelper.MousePosWorld);
						float cornerDstThresholdForScaleMode = Mathf.Min(displayMinAxisSize * 0.2f, DrawSettings.GridSize * 1.5f);
						bool enterScaleMode = cornerDst < cornerDstThresholdForScaleMode;

						if (enterScaleMode)
						{
							DrawClosestCornerDisplayBoundsIndicator(bounds, InputHelper.MousePosWorld, mouseOverIndicatorScaleCol);
						}
						else
						{
							DrawDisplayBoundsIndicators(bounds, mouseOverIndicatorCol);
						}


						if (InputHelper.IsMouseDownThisFrame(MouseButton.Left, true))
						{
							displayInteractState = enterScaleMode ? DisplayInteractState.Scaling : DisplayInteractState.Moving;
							SelectedDisplay = display;
							CustomizeChip.Displays.Remove(display); // remove from displays while moving (so can be drawn separately on top of everything else)
							displayMoveMouseOffset = display.Desc.Position - InputHelper.MousePosWorld;
							displayPosInitial = display.Desc.Position;
							displayScaleInitial = display.Desc.Scale;
							mouseDownPos = InputHelper.MousePosWorld;

							if(IsResizingChip) return;
							#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
							MobileUIControllerWrapper.ShowPlacementButtons(
								confirmPlacement,
								cancelPlacement
							);
							#endif

							return; // exit now that a display has been selected
						}
					}
				}
			}
		}

		static void DrawDisplayBoundsIndicators(Bounds2D bounds, Color col)
		{
			DrawPlacementCornerIndicator(bounds.TopLeft, Vector2.right, Vector2.down, col);
			DrawPlacementCornerIndicator(bounds.TopRight, Vector2.left, Vector2.down, col);
			DrawPlacementCornerIndicator(bounds.BottomLeft, Vector2.right, Vector2.up, col);
			DrawPlacementCornerIndicator(bounds.BottomRight, Vector2.left, Vector2.up, col);
		}

		static void DrawClosestCornerDisplayBoundsIndicator(Bounds2D bounds, Vector2 point, Color col)
		{
			Span<Vector2> corners = stackalloc Vector2[4]
			{
				bounds.TopLeft,
				bounds.TopRight,
				bounds.BottomLeft,
				bounds.BottomRight
			};

			int cornerIndex = 0;
			for (int i = 1; i < corners.Length; i++)
			{
				if ((corners[i] - point).sqrMagnitude < (corners[cornerIndex] - point).sqrMagnitude)
				{
					cornerIndex = i;
				}
			}

			float dirX = -((cornerIndex & 1) * 2 - 1);
			float dirY = cornerIndex <= 1 ? -1 : 1;
			DrawPlacementCornerIndicator(corners[cornerIndex], new Vector2(dirX, 0), new Vector2(0, dirY), col);
		}

		static void DrawPlacementCornerIndicator(Vector2 corner, Vector2 dirA, Vector2 dirB, Color col)
		{
			const float pad = 0.0f;
			const float len = DrawSettings.GridSize;
			const float thick = 0.01f;

			Vector2 origin = corner - (dirA + dirB) * pad;
			Draw.Line(origin, origin + dirA * len, thick, col);
			Draw.Line(origin, origin + dirB * len, thick, col);
		}

		static bool HandleChipResizing(SubChipInstance chip)
		{
			const float pad = 0.25f;
			const float h = 1.1f;
			const float size = 0.12f;
			bool canInteract = displayInteractState == DisplayInteractState.None;
			bool hascontrol = false;
			// Draw resize arrow handles on all sides of chip
			hascontrol |= DrawScaleHandle(chip.SelectionBoundingBox.CentreRight, Vector2Int.right);
			hascontrol |= DrawScaleHandle(chip.SelectionBoundingBox.CentreLeft, Vector2Int.left);
			hascontrol |= DrawScaleHandle(chip.SelectionBoundingBox.CentreTop, Vector2Int.up);
			hascontrol |= DrawScaleHandle(chip.SelectionBoundingBox.CentreBottom, Vector2Int.down);
			return hascontrol;

			bool DrawScaleHandle(Vector2 edge, Vector2Int dir)
			{
				Vector2 dirVec = dir;
				edge += dirVec * pad;
				Vector2 perp = new(-dirVec.y, dir.x);
				Vector2 a = edge + dirVec * size;
				Vector2 b = edge - (dir + perp * h) * size;
				Vector2 c = edge - (dir - perp * h) * size;

				bool mouseOver = canInteract && Maths.TriangleContainsPoint(InputHelper.MousePosWorld, a, b, c);
				if (mouseOver && InputHelper.IsMouseDownThisFrame(MouseButton.Left))
				{
					selectedChipResizeDir = dir;
					chipResizeMouseStartPos = InputHelper.MousePosWorld;
					chipResizeStartSize = chip.Size;
				}

				if (InputHelper.IsMouseUpThisFrame(MouseButton.Left)) selectedChipResizeDir = Vector2Int.zero;

				bool selected = canInteract && selectedChipResizeDir == dir;
				Color col = mouseOver ? ColHelper.MakeCol(0.7f) : ColHelper.MakeCol(0.3f);
				if (selected)
				{
					col = Color.white;
					Vector2 mouseDelta = InputHelper.MousePosWorld - chipResizeMouseStartPos;
					Vector2 desiredSize = chipResizeStartSize + Vector2.Scale(dir, mouseDelta) * 2;

                    //  snaps if snapping is on or if chip has pins on top or bottom
                    bool snapX = Project.ActiveProject.ShouldSnapToGrid;
                    if (!snapX && chip.HasCustomLayout)
                    {
                        bool hasXFacePins = chip.InputPins.Concat(chip.OutputPins).Any(p => p.face == 0 || p.face == 2);
                        snapX = hasXFacePins;
                    }

                    // snaps if snapping is on or if chip has pins on left or right. if default layout then forces snapping on Y
                    bool snapY = true;
                    if (chip.HasCustomLayout)
                    {
                        bool hasYFacePins = chip.InputPins.Concat(chip.OutputPins).Any(p => p.face == 1 || p.face == 3);
                        snapY = hasYFacePins || Project.ActiveProject.ShouldSnapToGrid;
                    }

                    if (snapY && dir.y != 0)
                    {
                        float deltaY = GridHelper.SnapToGrid(desiredSize.y - chip.MinSize.y);
                        desiredSize.y = chip.MinSize.y + deltaY;
                    }

                    if (snapX && dir.x != 0)
                    {
                        desiredSize.x = GridHelper.SnapToGridForceEven(desiredSize.x) - DrawSettings.ChipOutlineWidth;
                    }

                    chip.updateMinSize();
					Vector2 sizeNew = Vector2.Max(desiredSize, chip.MinSize);

					if (sizeNew != chip.Size)
					{
                        chip.Description.Size = Vector2.Max(desiredSize, chip.MinSize);
						ChipSaveMenu.ActiveCustomizeChip.UpdatePinLayout();
					}
				}
				// Highlight opposite handle to selected handle
				else if (dir == -selectedChipResizeDir)
				{
					col = Color.white;
				}

				Draw.Triangle(a, b, c, col);
				return mouseOver || selected;
			}
		}

        static void HandlePinDragging()
        {
            if (!InteractionState.MouseIsOverUI)
            {
                // Start dragging a pin
                if (InputHelper.IsMouseDownThisFrame(MouseButton.Left))
                {
                    if (InteractionState.ElementUnderMouse is PinInstance pin)
                    {
                        selectedPin = pin;
                        isDraggingPin = true;
                    }
                }

                if (isDraggingPin && selectedPin?.parent is SubChipInstance chip)
                {
                    Vector2 mouseWorld = InputHelper.MousePosWorld;
                    Vector2 chipCenter = chip.Position;
                    Vector2 localMouse = mouseWorld - chipCenter;
                    Debug.Log($"CUSTOMIZATION: mouseWorld = {mouseWorld}, chipCenter = {chipCenter}, localMouse = {localMouse}");
                    Vector2 chipHalfSize = chip.Size / 2f;

                    // DIRECTLY SET THE PIN POSITION TO THE GREEN DOT POSITION
                    // Get the projected point (the green dot position we know is correct)
                    Vector2 projectedPoint = GetProjectedPointForShape(chip.Description.ShapeType, chip.Description.ShapeRotation, localMouse, chipHalfSize);
                    
                    // Update the pin's position in the description
                    UpdatePinPositionInDescription(selectedPin, projectedPoint);
                    
                    // DEBUG: Check what's happening
                    Vector2 actualPinWorldPos = selectedPin.GetWorldPos();
                    Vector2 expectedWorldPos = chipCenter + projectedPoint;
                    Debug.Log($"ProjectedPoint: {projectedPoint}");
                    Debug.Log($"ChipCenter: {chipCenter}");
                    Debug.Log($"Expected World Pos: {expectedWorldPos}");
                    Debug.Log($"Actual Pin World Pos: {actualPinWorldPos}");
                    Debug.Log($"Updated Position in description");

                    PinInstance overlappedPin;
                    isPinPositionValid = !DoesPinOverlap(selectedPin, out overlappedPin);

                    // End drag on mouse release
                    if (InputHelper.IsMouseUpThisFrame(MouseButton.Left))
                    {
                        if (isPinPositionValid)
                        {

                            if (!ChipCustomizationMenu.isCustomLayout)
                            {
                                ChipCustomizationMenu.isCustomLayout = true;
                                Seb.Vis.UI.UI.GetWheelSelectorState(ChipCustomizationMenu.ID_LayoutOptions).index = 1;
                                ChipSaveMenu.ActiveCustomizeChip.SetCustomLayout(true);
                            }
                        }

                        isDraggingPin = false;
                        selectedPin = null;
                        isPinPositionValid = true;
                    }
                }
            }
        }

        public static void Reset()
		{
			SelectedDisplay = null;
			displayInteractState = DisplayInteractState.None;
		}

        public static bool DoesPinOverlap(PinInstance pin, out PinInstance overlappedPin)
        {
            overlappedPin = null;
            if (!(pin.parent is SubChipInstance chip)) return false;

            // Get all pins on the same chip to check pins on the same face as selectedpin
            List<PinInstance> pinsToCheck = new List<PinInstance>();
            pinsToCheck.AddRange(chip.InputPins);
            pinsToCheck.AddRange(chip.OutputPins);

            foreach (PinInstance otherPin in pinsToCheck)
            {
                if (otherPin == pin) continue;

                // Only check pins on the same face
                if (otherPin.face != pin.face) continue;

                float distanceAlongFace = Mathf.Abs(pin.LocalPosY - otherPin.LocalPosY);

                // Calculate minimum required spacing based on pin sizes
                float pinHeight = SubChipInstance.PinHeightFromBitCount(pin.bitCount);

                float otherPinHeight = SubChipInstance.PinHeightFromBitCount(otherPin.bitCount);

                // Required space is half each pin's height plus some buffer
                float requiredSpacing = (pinHeight + otherPinHeight) / 2f + minPinSpacing;

                if (distanceAlongFace < requiredSpacing)
                {
                    overlappedPin = otherPin;
                    return true;
                }
            }

            return false;
        }

        static (int face, float offset) GetClosestPinPosition(ChipShapeType shapeType, float rotation, Vector2 localMouse, Vector2 chipHalfSize)
        {
            switch (shapeType)
            {
                case ChipShapeType.Rectangle:
                    return GetClosestRectanglePinPosition(localMouse, chipHalfSize);
                case ChipShapeType.Hexagon:
                    return GetClosestHexagonPinPosition(localMouse, chipHalfSize, rotation);
                case ChipShapeType.Triangle:
                    return GetClosestTrianglePinPosition(localMouse, chipHalfSize, rotation);
                case ChipShapeType.CustomPolygon:
                    // For custom polygons, we don't need face logic since we use Position field
                    return (0, 0); // Dummy values, not used
                default:
                    return GetClosestRectanglePinPosition(localMouse, chipHalfSize);
            }
        }

        static (int face, float offset) GetClosestRectanglePinPosition(Vector2 localMouse, Vector2 chipHalfSize)
        {
            // Original rectangle logic
            float distTop = Mathf.Abs(localMouse.y - chipHalfSize.y);
            float distBottom = Mathf.Abs(localMouse.y + chipHalfSize.y);
            float distRight = Mathf.Abs(localMouse.x - chipHalfSize.x);
            float distLeft = Mathf.Abs(localMouse.x + chipHalfSize.x);

            int closestFace = 0;
            float minDist = distTop;

            if (distRight < minDist) { closestFace = 1; minDist = distRight; }
            if (distBottom < minDist) { closestFace = 2; minDist = distBottom; }
            if (distLeft < minDist) { closestFace = 3; }

            float offset = closestFace == 0 || closestFace == 2 ? localMouse.x : localMouse.y;
            return (closestFace, offset);
        }

        static (int face, float offset) GetClosestHexagonPinPosition(Vector2 localMouse, Vector2 chipHalfSize, float rotation)
        {
            // Calculate the 6 vertices of the hexagon
            float rotationRad = rotation * Mathf.Deg2Rad;
            Vector2[] vertices = new Vector2[6];
            
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI / 3f + rotationRad; // Match PinInstance.GetHexagonPinPosition
                vertices[i] = new Vector2(
                    Mathf.Cos(angle) * chipHalfSize.x,
                    Mathf.Sin(angle) * chipHalfSize.y
                );
            }
            
            // DEBUG: Draw hexagon vertices and edges
            for (int i = 0; i < 6; i++)
            {
                // Draw vertex points
                Draw.Point(vertices[i], 0.1f, Color.red);
                
                // Draw edge lines
                int next = (i + 1) % 6;
                Draw.Line(vertices[i], vertices[next], 0.02f, Color.yellow);
            }
            
            // Find the closest edge by projecting mouse position onto each edge
            int closestFace = 0;
            float closestDistance = float.MaxValue;
            float bestOffset = 0f;
            Vector2 bestProjectedPoint = Vector2.zero;
            
            for (int i = 0; i < 6; i++)
            {
                int next = (i + 1) % 6;
                Vector2 edgeStart = vertices[i];
                Vector2 edgeEnd = vertices[next];
                Vector2 edgeDirection = edgeEnd - edgeStart;
                
                // Project mouse position onto this edge
                Vector2 toMouse = localMouse - edgeStart;
                float projectionLength = Vector2.Dot(toMouse, edgeDirection.normalized);
                
                // Clamp projection to edge bounds
                float edgeLength = edgeDirection.magnitude;
                projectionLength = Mathf.Clamp(projectionLength, 0f, edgeLength);
                
                // Calculate the projected point on the edge
                Vector2 projectedPoint = edgeStart + edgeDirection.normalized * projectionLength;
                
                // Calculate distance from mouse to projected point
                float distance = Vector2.Distance(localMouse, projectedPoint);
                
                // DEBUG: Draw projection line for this edge
                Draw.Line(localMouse, projectedPoint, 0.01f, Color.cyan);
                
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestFace = i;
                    bestProjectedPoint = projectedPoint;
                    
                    // Calculate offset along the TANGENT direction (perpendicular to edge)
                    // This matches how PinInstance.GetHexagonPinPosition uses localPosY
                    float faceAngle = i * Mathf.PI / 3f + rotationRad; // Match PinInstance calculation
                    Vector2 tangent = new Vector2(-Mathf.Sin(faceAngle), Mathf.Cos(faceAngle)); // Match PinInstance exactly
                    float tangentOffset = Vector2.Dot(localMouse, tangent);
                    bestOffset = tangentOffset;
                }
            }
            
            // DEBUG: Highlight the closest edge and projection
            int nextClosest = (closestFace + 1) % 6;
            Draw.Line(vertices[closestFace], vertices[nextClosest], 0.05f, Color.green);
            Draw.Line(localMouse, bestProjectedPoint, 0.03f, Color.magenta);
            Draw.Point(bestProjectedPoint, 0.08f, Color.green);
            
            return (closestFace, bestOffset);
        }

        static (int face, float offset) GetClosestTrianglePinPosition(Vector2 localMouse, Vector2 chipHalfSize, float rotation)
        {
            // Calculate the 3 vertices of the triangle
            float rotationRad = rotation * Mathf.Deg2Rad;
            Vector2[] vertices = new Vector2[3];
            
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 2f * Mathf.PI / 3f + rotationRad; // Match PinInstance.GetTrianglePinPosition
                vertices[i] = new Vector2(
                    Mathf.Cos(angle) * chipHalfSize.x,
                    Mathf.Sin(angle) * chipHalfSize.y
                );
            }
            
            // DEBUG: Draw triangle vertices and edges
            for (int i = 0; i < 3; i++)
            {
                // Draw vertex points
                Draw.Point(vertices[i], 0.1f, Color.red);
                
                // Draw edge lines
                int next = (i + 1) % 3;
                Draw.Line(vertices[i], vertices[next], 0.02f, Color.yellow);
            }
            
            // Find the closest edge by projecting mouse position onto each edge
            int closestFace = 0;
            float closestDistance = float.MaxValue;
            float bestOffset = 0f;
            Vector2 bestProjectedPoint = Vector2.zero;
            
            for (int i = 0; i < 3; i++)
            {
                int next = (i + 1) % 3;
                Vector2 edgeStart = vertices[i];
                Vector2 edgeEnd = vertices[next];
                Vector2 edgeDirection = edgeEnd - edgeStart;
                
                // Project mouse position onto this edge
                Vector2 toMouse = localMouse - edgeStart;
                float projectionLength = Vector2.Dot(toMouse, edgeDirection.normalized);
                
                // Clamp projection to edge bounds
                float edgeLength = edgeDirection.magnitude;
                projectionLength = Mathf.Clamp(projectionLength, 0f, edgeLength);
                
                // Calculate the projected point on the edge
                Vector2 projectedPoint = edgeStart + edgeDirection.normalized * projectionLength;
                
                // Calculate distance from mouse to projected point
                float distance = Vector2.Distance(localMouse, projectedPoint);
                
                // DEBUG: Draw projection line for this edge
                Draw.Line(localMouse, projectedPoint, 0.01f, Color.cyan);
                
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestFace = i;
                    bestProjectedPoint = projectedPoint;
                    
                    // Calculate offset along the TANGENT direction (perpendicular to edge)
                    // This matches how PinInstance.GetTrianglePinPosition uses localPosY
                    float faceAngle = i * 2f * Mathf.PI / 3f + rotationRad; // Match PinInstance calculation
                    Vector2 tangent = new Vector2(-Mathf.Sin(faceAngle), Mathf.Cos(faceAngle)); // Match PinInstance exactly
                    float tangentOffset = Vector2.Dot(localMouse, tangent);
                    bestOffset = tangentOffset;
                }
            }
            
            // DEBUG: Highlight the closest edge and projection
            int nextClosest = (closestFace + 1) % 3;
            Draw.Line(vertices[closestFace], vertices[nextClosest], 0.05f, Color.green);
            Draw.Line(localMouse, bestProjectedPoint, 0.03f, Color.magenta);
            Draw.Point(bestProjectedPoint, 0.08f, Color.green);
            
            return (closestFace, bestOffset);
        }

        static float GetPinOffsetAlongFace(ChipShapeType shapeType, float rotation, int face, float offset, Vector2 chipHalfSize, float pinHeight, bool shouldSnapToGrid)
        {
            switch (shapeType)
            {
                case ChipShapeType.Rectangle:
                    return GetRectanglePinOffset(face, offset, chipHalfSize, pinHeight, shouldSnapToGrid);
                case ChipShapeType.Hexagon:
                    return GetHexagonPinOffset(face, offset, chipHalfSize, pinHeight, shouldSnapToGrid);
                case ChipShapeType.Triangle:
                    return GetTrianglePinOffset(face, offset, chipHalfSize, pinHeight, shouldSnapToGrid);
                default:
                    return GetRectanglePinOffset(face, offset, chipHalfSize, pinHeight, shouldSnapToGrid);
            }
        }

        static float GetRectanglePinOffset(int face, float offset, Vector2 chipHalfSize, float pinHeight, bool shouldSnapToGrid)
        {
            float maxOffset;
            if (face == 0 || face == 2) // Horizontal faces
            {
                maxOffset = chipHalfSize.x - pinHeight / 2f;
            }
            else // Vertical faces
            {
                maxOffset = chipHalfSize.y - pinHeight / 2f;
            }
            
            return shouldSnapToGrid ? GridHelper.ClampToGrid(offset, -maxOffset, maxOffset) :
                Mathf.Clamp(offset, -maxOffset, maxOffset);
        }

        static float GetHexagonPinOffset(int face, float offset, Vector2 chipHalfSize, float pinHeight, bool shouldSnapToGrid)
        {
            // For hexagon, calculate the actual length of the face
            // Each face of a hexagon has length = chipHalfSize.x (radius)
            float faceLength = chipHalfSize.x;
            float maxOffset = faceLength - pinHeight / 2f;
            
            return shouldSnapToGrid ? GridHelper.ClampToGrid(offset, -maxOffset, maxOffset) :
                Mathf.Clamp(offset, -maxOffset, maxOffset);
        }

        static float GetTrianglePinOffset(int face, float offset, Vector2 chipHalfSize, float pinHeight, bool shouldSnapToGrid)
        {
            // For triangle, calculate the actual length of the face
            // Each face of an equilateral triangle has length = chipHalfSize.x * sqrt(3)
            float faceLength = chipHalfSize.x * Mathf.Sqrt(3f);
            float maxOffset = faceLength - pinHeight / 2f;
            
            return shouldSnapToGrid ? GridHelper.ClampToGrid(offset, -maxOffset, maxOffset) :
                Mathf.Clamp(offset, -maxOffset, maxOffset);
        }

        static Vector2 RotateVector(Vector2 vector, float rotationRad)
        {
            float cos = Mathf.Cos(rotationRad);
            float sin = Mathf.Sin(rotationRad);
            return new Vector2(
                vector.x * cos - vector.y * sin,
                vector.x * sin + vector.y * cos
            );
        }

        static Vector2 GetProjectedPointForShape(ChipShapeType shapeType, float rotation, Vector2 localMouse, Vector2 chipHalfSize)
        {
            // Find the closest edge and get the projected point
            switch (shapeType)
            {
                case ChipShapeType.Rectangle:
                    return GetRectangleProjectedPointDirect(localMouse, chipHalfSize);
                case ChipShapeType.Hexagon:
                    return GetHexagonProjectedPointDirect(localMouse, chipHalfSize, rotation);
                case ChipShapeType.Triangle:
                    return GetTriangleProjectedPointDirect(localMouse, chipHalfSize, rotation);
                case ChipShapeType.CustomPolygon:
                    return GetCustomPolygonProjectedPointDirect(localMouse, chipHalfSize, rotation);
                default:
                    return GetRectangleProjectedPointDirect(localMouse, chipHalfSize);
            }
        }

        static Vector2 GetRectangleProjectedPoint(Vector2 localMouse, Vector2 chipHalfSize, int face)
        {
            // For rectangle, just return the mouse position clamped to the edge
            switch (face)
            {
                case 0: return new Vector2(localMouse.x, chipHalfSize.y);
                case 1: return new Vector2(chipHalfSize.x, localMouse.y);
                case 2: return new Vector2(localMouse.x, -chipHalfSize.y);
                case 3: return new Vector2(-chipHalfSize.x, localMouse.y);
                default: return localMouse;
            }
        }

        static Vector2 GetHexagonProjectedPoint(Vector2 localMouse, Vector2 chipHalfSize, int face, float rotation)
        {
            // Use the SAME method as PinInstance.GetHexagonPinPosition
            float rotationRad = rotation * Mathf.Deg2Rad;
            float angle = face * Mathf.PI / 3f + rotationRad;
            
            // Calculate the point on the hexagon edge (same as PinInstance)
            float edgeX = Mathf.Cos(angle) * chipHalfSize.x;
            float edgeY = Mathf.Sin(angle) * chipHalfSize.y;
            Vector2 edgeCenter = new Vector2(edgeX, edgeY);
            
            // Calculate the tangent direction (same as PinInstance)
            Vector2 tangent = new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle));
            
            // Project mouse onto the tangent direction
            Vector2 toMouse = localMouse - edgeCenter;
            float tangentOffset = Vector2.Dot(toMouse, tangent);
            
            // Apply the offset along the tangent (same as PinInstance)
            Vector2 offset = tangent * tangentOffset;
            
            // Move inward by the inset (same as PinInstance)
            float outlineOffset = DrawSettings.ChipOutlineWidth / 2f;
            float inset = DrawSettings.SubChipPinInset;
            Vector2 inward = new Vector2(-Mathf.Cos(angle), -Mathf.Sin(angle)) * (outlineOffset - inset);
            
            // Return the final position (same as PinInstance)
            return edgeCenter + offset + inward;
        }

        static Vector2 GetTriangleProjectedPoint(Vector2 localMouse, Vector2 chipHalfSize, int face, float rotation)
        {
            // Use the SAME method as PinInstance.GetTrianglePinPosition
            float rotationRad = rotation * Mathf.Deg2Rad;
            float angle = face * 2f * Mathf.PI / 3f + rotationRad;
            
            // Calculate the point on the triangle edge (same as PinInstance)
            float edgeX = Mathf.Cos(angle) * chipHalfSize.x;
            float edgeY = Mathf.Sin(angle) * chipHalfSize.y;
            Vector2 edgeCenter = new Vector2(edgeX, edgeY);
            
            // Calculate the tangent direction (same as PinInstance)
            Vector2 tangent = new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle));
            
            // Project mouse onto the tangent direction
            Vector2 toMouse = localMouse - edgeCenter;
            float tangentOffset = Vector2.Dot(toMouse, tangent);
            
            // Apply the offset along the tangent (same as PinInstance)
            Vector2 offset = tangent * tangentOffset;
            
            // Move inward by the inset (same as PinInstance)
            float outlineOffset = DrawSettings.ChipOutlineWidth / 2f;
            float inset = DrawSettings.SubChipPinInset;
            Vector2 inward = new Vector2(-Mathf.Cos(angle), -Mathf.Sin(angle)) * (outlineOffset - inset);
            
            // Return the final position (same as PinInstance)
            return edgeCenter + offset + inward;
        }

        static Vector2 GetHexagonProjectedPointDirect(Vector2 localMouse, Vector2 chipHalfSize, float rotation)
        {
            // Calculate the hexagon vertices
            float rotationRad = rotation * Mathf.Deg2Rad;
            Vector2[] vertices = new Vector2[6];
            
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI / 3f + rotationRad;
                vertices[i] = new Vector2(
                    Mathf.Cos(angle) * chipHalfSize.x,
                    Mathf.Sin(angle) * chipHalfSize.y
                );
            }
            
            // DEBUG: Draw hexagon vertices and edges
            for (int i = 0; i < 6; i++)
            {
                // Draw vertex points
                Draw.Point(vertices[i], 0.1f, Color.red);
                
                // Draw edge lines
                int next = (i + 1) % 6;
                Draw.Line(vertices[i], vertices[next], 0.02f, Color.yellow);
            }
            
            // Find the closest edge by projecting mouse position onto each edge
            int closestFace = 0;
            float closestDistance = float.MaxValue;
            Vector2 bestProjectedPoint = Vector2.zero;
            
            for (int i = 0; i < 6; i++)
            {
                int next = (i + 1) % 6;
                Vector2 edgeStart = vertices[i];
                Vector2 edgeEnd = vertices[next];
                Vector2 edgeDirection = edgeEnd - edgeStart;
                
                // Project mouse position onto this edge
                Vector2 toMouse = localMouse - edgeStart;
                float projectionLength = Vector2.Dot(toMouse, edgeDirection.normalized);
                projectionLength = Mathf.Clamp(projectionLength, 0f, edgeDirection.magnitude);
                
                // Calculate the projected point on the edge
                Vector2 projectedPoint = edgeStart + edgeDirection.normalized * projectionLength;
                
                // Calculate distance from mouse to projected point
                float distance = Vector2.Distance(localMouse, projectedPoint);
                
                // DEBUG: Draw projection line for this edge
                Draw.Line(localMouse, projectedPoint, 0.01f, Color.cyan);
                
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestFace = i;
                    bestProjectedPoint = projectedPoint;
                }
            }
            
            // DEBUG: Highlight the closest edge and projection
            int nextClosest = (closestFace + 1) % 6;
            Draw.Line(vertices[closestFace], vertices[nextClosest], 0.05f, Color.green);
            Draw.Line(localMouse, bestProjectedPoint, 0.03f, Color.magenta);
            Draw.Point(bestProjectedPoint, 0.08f, Color.green);
            
            // For direct positioning, just return the projected point without additional transformations
            return bestProjectedPoint;
        }

        static Vector2 GetTriangleProjectedPointDirect(Vector2 localMouse, Vector2 chipHalfSize, float rotation)
        {
            // Calculate the triangle vertices
            float rotationRad = rotation * Mathf.Deg2Rad;
            Vector2[] vertices = new Vector2[3];
            
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 2f * Mathf.PI / 3f + rotationRad;
                vertices[i] = new Vector2(
                    Mathf.Cos(angle) * chipHalfSize.x,
                    Mathf.Sin(angle) * chipHalfSize.y
                );
            }
            
            // DEBUG: Draw triangle vertices and edges
            for (int i = 0; i < 3; i++)
            {
                // Draw vertex points
                Draw.Point(vertices[i], 0.1f, Color.red);
                
                // Draw edge lines
                int next = (i + 1) % 3;
                Draw.Line(vertices[i], vertices[next], 0.02f, Color.yellow);
            }
            
            // Find the closest edge by projecting mouse position onto each edge
            int closestFace = 0;
            float closestDistance = float.MaxValue;
            Vector2 bestProjectedPoint = Vector2.zero;
            
            for (int i = 0; i < 3; i++)
            {
                int next = (i + 1) % 3;
                Vector2 edgeStart = vertices[i];
                Vector2 edgeEnd = vertices[next];
                Vector2 edgeDirection = edgeEnd - edgeStart;
                
                // Project mouse position onto this edge
                Vector2 toMouse = localMouse - edgeStart;
                float projectionLength = Vector2.Dot(toMouse, edgeDirection.normalized);
                projectionLength = Mathf.Clamp(projectionLength, 0f, edgeDirection.magnitude);
                
                // Calculate the projected point on the edge
                Vector2 projectedPoint = edgeStart + edgeDirection.normalized * projectionLength;
                
                // Calculate distance from mouse to projected point
                float distance = Vector2.Distance(localMouse, projectedPoint);
                
                // DEBUG: Draw projection line for this edge
                Draw.Line(localMouse, projectedPoint, 0.01f, Color.cyan);
                
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestFace = i;
                    bestProjectedPoint = projectedPoint;
                }
            }
            
            // DEBUG: Highlight the closest edge and projection
            int nextClosest = (closestFace + 1) % 3;
            Draw.Line(vertices[closestFace], vertices[nextClosest], 0.05f, Color.green);
            Draw.Line(localMouse, bestProjectedPoint, 0.03f, Color.magenta);
            Draw.Point(bestProjectedPoint, 0.08f, Color.green);
            
            // For direct positioning, just return the projected point without additional transformations
            return bestProjectedPoint;
        }

        static Vector2 GetCustomPolygonProjectedPointDirect(Vector2 localMouse, Vector2 chipHalfSize, float rotation)
        {
            // Get custom polygon data
            CustomPolygonData polygon = ChipSaveMenu.ActiveCustomizeChip?.Description.CustomPolygon;
            if (polygon == null || polygon.Vertices == null || polygon.Vertices.Length < 3)
            {
                // Fallback to rectangle
                return GetRectangleProjectedPointDirect(localMouse, chipHalfSize);
            }

            float rotationRad = rotation * Mathf.Deg2Rad;
            
            // Convert normalized vertices to local positions
            Vector2[] vertices = new Vector2[polygon.Vertices.Length];
            for (int i = 0; i < polygon.Vertices.Length; i++)
            {
                Vector2 normalized = polygon.Vertices[i].ToVector2();
                Vector2 scaled = new Vector2(normalized.x * chipHalfSize.x, normalized.y * chipHalfSize.y);
                vertices[i] = RotateVector(scaled, rotationRad);
            }

            // Find the closest edge
            float closestDistance = float.MaxValue;
            Vector2 bestProjectedPoint = Vector2.zero;

            for (int i = 0; i < vertices.Length; i++)
            {
                int next = (i + 1) % vertices.Length;
                
                // Check if this edge is curved
                if (polygon.Edges != null && i < polygon.Edges.Length && polygon.Edges[i].IsCurved)
                {
                    // Project onto curved edge using bezier curve
                    Vector2 edgeMidpoint = (vertices[i] + vertices[next]) * 0.5f;
                    Vector2 edgeDirection = vertices[next] - vertices[i];
                    Vector2 perpendicular = new Vector2(-edgeDirection.y, edgeDirection.x).normalized;
                    Vector2 controlPoint = edgeMidpoint + perpendicular * polygon.Edges[i].CurveStrength;

                    // Sample the curve and find the closest point
                    int samples = 20;
                    for (int s = 0; s <= samples; s++)
                    {
                        float t = s / (float)samples;
                        Vector2 curvePoint = QuadraticBezier(vertices[i], controlPoint, vertices[next], t);
                        float distance = Vector2.Distance(localMouse, curvePoint);

                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            bestProjectedPoint = curvePoint;
                        }
                    }
                }
                else
                {
                    // Project onto straight edge
                    Vector2 edgeStart = vertices[i];
                    Vector2 edgeEnd = vertices[next];
                    Vector2 edgeDirection = edgeEnd - edgeStart;
                    
                    Vector2 toMouse = localMouse - edgeStart;
                    float projectionLength = Vector2.Dot(toMouse, edgeDirection.normalized);
                    projectionLength = Mathf.Clamp(projectionLength, 0f, edgeDirection.magnitude);
                    
                    Vector2 projectedPoint = edgeStart + edgeDirection.normalized * projectionLength;
                    float distance = Vector2.Distance(localMouse, projectedPoint);
                    
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        bestProjectedPoint = projectedPoint;
                    }
                }
            }

            return bestProjectedPoint;
        }

        static Vector2 QuadraticBezier(Vector2 p0, Vector2 p1, Vector2 p2, float t)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            
            Vector2 point = uu * p0;
            point += 2 * u * t * p1;
            point += tt * p2;
            
            return point;
        }

        static Vector2 GetRectangleProjectedPointDirect(Vector2 localMouse, Vector2 chipHalfSize)
        {
            // For rectangle, find the closest edge and project onto it
            float distTop = Mathf.Abs(localMouse.y - chipHalfSize.y);
            float distBottom = Mathf.Abs(localMouse.y + chipHalfSize.y);
            float distRight = Mathf.Abs(localMouse.x - chipHalfSize.x);
            float distLeft = Mathf.Abs(localMouse.x + chipHalfSize.x);

            int closestFace = 0;
            float minDist = distTop;

            if (distRight < minDist) { closestFace = 1; minDist = distRight; }
            if (distBottom < minDist) { closestFace = 2; minDist = distBottom; }
            if (distLeft < minDist) { closestFace = 3; }

            // Return the projected point on the closest edge
            switch (closestFace)
            {
                case 0: return new Vector2(localMouse.x, chipHalfSize.y);
                case 1: return new Vector2(chipHalfSize.x, localMouse.y);
                case 2: return new Vector2(localMouse.x, -chipHalfSize.y);
                case 3: return new Vector2(-chipHalfSize.x, localMouse.y);
                default: return localMouse;
            }
        }

        static Vector2 GetEdgeDirectionForShape(ChipShapeType shapeType, float rotation, int face, Vector2 chipHalfSize)
        {
            switch (shapeType)
            {
                case ChipShapeType.Rectangle:
                    return GetRectangleEdgeDirection(face, chipHalfSize);
                case ChipShapeType.Hexagon:
                    return GetHexagonEdgeDirection(face, chipHalfSize, rotation);
                case ChipShapeType.Triangle:
                    return GetTriangleEdgeDirection(face, chipHalfSize, rotation);
                default:
                    return GetRectangleEdgeDirection(face, chipHalfSize);
            }
        }

        static Vector2 GetRectangleEdgeDirection(int face, Vector2 chipHalfSize)
        {
            switch (face)
            {
                case 0: return new Vector2(1, 0); // Top edge (left to right)
                case 1: return new Vector2(0, -1); // Right edge (top to bottom)
                case 2: return new Vector2(-1, 0); // Bottom edge (right to left)
                case 3: return new Vector2(0, 1); // Left edge (bottom to top)
                default: return new Vector2(1, 0);
            }
        }

        static Vector2 GetHexagonEdgeDirection(int face, Vector2 chipHalfSize, float rotation)
        {
            float rotationRad = rotation * Mathf.Deg2Rad;
            Vector2[] vertices = new Vector2[6];
            
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI / 3f + rotationRad;
                vertices[i] = new Vector2(
                    Mathf.Cos(angle) * chipHalfSize.x,
                    Mathf.Sin(angle) * chipHalfSize.y
                );
            }
            
            int next = (face + 1) % 6;
            return vertices[next] - vertices[face];
        }

        static Vector2 GetTriangleEdgeDirection(int face, Vector2 chipHalfSize, float rotation)
        {
            float rotationRad = rotation * Mathf.Deg2Rad;
            Vector2[] vertices = new Vector2[3];
            
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 2f * Mathf.PI / 3f + rotationRad;
                vertices[i] = new Vector2(
                    Mathf.Cos(angle) * chipHalfSize.x,
                    Mathf.Sin(angle) * chipHalfSize.y
                );
            }
            
            int next = (face + 1) % 3;
            return vertices[next] - vertices[face];
        }

        static void HandlePolygonEditing(SubChipInstance chip)
		{
			// Only handle polygon editing if we're using a custom polygon
			if (chip.Description.ShapeType != ChipShapeType.CustomPolygon || chip.Description.CustomPolygon == null)
				return;

			CustomPolygonData polygon = chip.Description.CustomPolygon;
			Vector2 mouseWorld = InputHelper.MousePosWorld;
			Vector2 chipCenter = chip.Position;
			Vector2 chipHalfSize = chip.Size / 2f;
			float rotationRad = chip.Description.ShapeRotation * Mathf.Deg2Rad;

			// Convert normalized vertices to world positions
			Vector2[] worldVertices = new Vector2[polygon.Vertices.Length];
			for (int i = 0; i < polygon.Vertices.Length; i++)
			{
				Vector2 normalizedPos = polygon.Vertices[i].ToVector2();
				Vector2 scaledPos = new Vector2(normalizedPos.x * chipHalfSize.x, normalizedPos.y * chipHalfSize.y);
				worldVertices[i] = chipCenter + RotateVector(scaledPos, rotationRad);
			}

			// Platform-specific sizing
			float vertexRadius = Application.isMobilePlatform ? 0.2f : 0.1f;
			float edgeMidpointRadius = Application.isMobilePlatform ? 0.15f : 0.075f;
			float rotationHandleRadius = Application.isMobilePlatform ? 0.25f : 0.125f;
			float rotationHandleDistance = chipHalfSize.y + 0.5f; // Distance above the chip

			// Handle dragging
			if (isDraggingVertex && selectedPolygonVertex >= 0)
			{
				// Drag vertex
				Vector2 localMouse = mouseWorld - chipCenter;
				Vector2 unrotatedLocal = RotateVector(localMouse, -rotationRad);
				Vector2 normalized = new Vector2(unrotatedLocal.x / chipHalfSize.x, unrotatedLocal.y / chipHalfSize.y);

				// Clamp to reasonable range
				normalized.x = Mathf.Clamp(normalized.x, -1.5f, 1.5f);
				normalized.y = Mathf.Clamp(normalized.y, -1.5f, 1.5f);

				polygon.Vertices[selectedPolygonVertex].X = normalized.x;
				polygon.Vertices[selectedPolygonVertex].Y = normalized.y;

				// End drag on mouse release
				if (InputHelper.IsMouseUpThisFrame(MouseButton.Left))
				{
					isDraggingVertex = false;
					selectedPolygonVertex = -1;
					// Update pin positions after vertex change
					UpdatePinPositionsForCustomPolygon(chip);
				}
			}
			else if (isDraggingEdgeCurve && selectedPolygonEdge >= 0)
			{
				// Drag edge curve
				int nextVertex = (selectedPolygonEdge + 1) % polygon.Vertices.Length;
				Vector2 edgeMidpoint = (worldVertices[selectedPolygonEdge] + worldVertices[nextVertex]) * 0.5f;
				Vector2 edgeDirection = worldVertices[nextVertex] - worldVertices[selectedPolygonEdge];
				Vector2 perpendicular = new Vector2(-edgeDirection.y, edgeDirection.x).normalized;

				// Calculate curve strength based on mouse distance from midpoint
				Vector2 toMouse = mouseWorld - edgeMidpoint;
				float curveStrength = Vector2.Dot(toMouse, perpendicular);

				polygon.Edges[selectedPolygonEdge].CurveStrength = curveStrength;
				polygon.Edges[selectedPolygonEdge].IsCurved = Mathf.Abs(curveStrength) > 0.01f;

				// End drag on mouse release
				if (InputHelper.IsMouseUpThisFrame(MouseButton.Left))
				{
					isDraggingEdgeCurve = false;
					selectedPolygonEdge = -1;
					// Update pin positions after edge curve change
					UpdatePinPositionsForCustomPolygon(chip);
				}
			}
			else if (isDraggingRotation)
			{
				// Drag rotation handle - calculate angle delta from start position
				Vector2 toMouse = mouseWorld - chipCenter;
				float currentAngle = Mathf.Atan2(toMouse.y, toMouse.x);
				float angleDelta = currentAngle - rotationHandleStartAngle;
				float newRotation = chip.Description.ShapeRotation + angleDelta * Mathf.Rad2Deg;
				
				// Keep rotation in reasonable range
				newRotation = newRotation % 360f;
				if (newRotation < 0) newRotation += 360f;
				
				chip.Description.ShapeRotation = newRotation;
				
				// Update the start angle to prevent accumulation
				rotationHandleStartAngle = currentAngle;

				// End drag on mouse release
				if (InputHelper.IsMouseUpThisFrame(MouseButton.Left))
				{
					isDraggingRotation = false;
					// Update pin positions after rotation change
					UpdatePinPositionsForCustomPolygon(chip);
				}
			}
			else
			{
				// Check for mouse down to start dragging
				if (InputHelper.IsMouseDownThisFrame(MouseButton.Left))
				{
					// Check if clicking on a vertex
					for (int i = 0; i < worldVertices.Length; i++)
					{
						if (Vector2.Distance(mouseWorld, worldVertices[i]) < vertexRadius)
						{
							selectedPolygonVertex = i;
							isDraggingVertex = true;
							break;
						}
					}

					// If not clicking a vertex, check for edge midpoints
					if (!isDraggingVertex)
					{
						for (int i = 0; i < worldVertices.Length; i++)
						{
							int next = (i + 1) % worldVertices.Length;
							Vector2 edgeMidpoint;
							
							// Calculate the actual curve midpoint if the edge is curved
							if (polygon.Edges != null && i < polygon.Edges.Length && polygon.Edges[i].IsCurved)
							{
								// For curved edges, use the midpoint of the actual curve
								Vector2 edgeMidpointStraight = (worldVertices[i] + worldVertices[next]) * 0.5f;
								Vector2 edgeDirection = worldVertices[next] - worldVertices[i];
								Vector2 perpendicular = new Vector2(-edgeDirection.y, edgeDirection.x).normalized;
								Vector2 controlPoint = edgeMidpointStraight + perpendicular * polygon.Edges[i].CurveStrength;
								
								// Get the midpoint of the bezier curve (t = 0.5)
								edgeMidpoint = QuadraticBezier(worldVertices[i], controlPoint, worldVertices[next], 0.5f);
							}
							else
							{
								// For straight edges, use the simple midpoint
								edgeMidpoint = (worldVertices[i] + worldVertices[next]) * 0.5f;
							}

							if (Vector2.Distance(mouseWorld, edgeMidpoint) < edgeMidpointRadius)
							{
								selectedPolygonEdge = i;
								isDraggingEdgeCurve = true;
								break;
							}
						}
					}

					// If not clicking vertices or edges, check for rotation handle
					if (!isDraggingVertex && !isDraggingEdgeCurve)
					{
						Vector2 rotationHandleClickPos = chipCenter + Vector2.up * rotationHandleDistance;
						if (Vector2.Distance(mouseWorld, rotationHandleClickPos) < rotationHandleRadius)
						{
							isDraggingRotation = true;
							rotationHandleStartMouse = mouseWorld;
							Vector2 toStartMouse = rotationHandleStartMouse - chipCenter;
							rotationHandleStartAngle = Mathf.Atan2(toStartMouse.y, toStartMouse.x);
						}
					}
				}
			}

			// Draw vertex handles
			for (int i = 0; i < worldVertices.Length; i++)
			{
				Color vertexColor = (selectedPolygonVertex == i && isDraggingVertex) ? Color.green : Color.yellow;
				Draw.Point(worldVertices[i], vertexRadius, vertexColor);
			}

			// Draw edge midpoint handles
			for (int i = 0; i < worldVertices.Length; i++)
			{
				int next = (i + 1) % worldVertices.Length;
				Vector2 edgeMidpoint;
				
				// Calculate the actual curve midpoint if the edge is curved
				if (polygon.Edges != null && i < polygon.Edges.Length && polygon.Edges[i].IsCurved)
				{
					// For curved edges, show the midpoint of the actual curve
					Vector2 edgeMidpointStraight = (worldVertices[i] + worldVertices[next]) * 0.5f;
					Vector2 edgeDirection = worldVertices[next] - worldVertices[i];
					Vector2 perpendicular = new Vector2(-edgeDirection.y, edgeDirection.x).normalized;
					Vector2 controlPoint = edgeMidpointStraight + perpendicular * polygon.Edges[i].CurveStrength;
					
					// Get the midpoint of the bezier curve (t = 0.5)
					edgeMidpoint = QuadraticBezier(worldVertices[i], controlPoint, worldVertices[next], 0.5f);
				}
				else
				{
					// For straight edges, use the simple midpoint
					edgeMidpoint = (worldVertices[i] + worldVertices[next]) * 0.5f;
				}
				
				Color edgeColor = (selectedPolygonEdge == i && isDraggingEdgeCurve) ? Color.green : Color.cyan;
				Draw.Point(edgeMidpoint, edgeMidpointRadius, edgeColor);
			}

			// Draw rotation handle
			Vector2 rotationHandlePos;
			if (isDraggingRotation)
			{
				// When dragging, position the handle at the mouse position
				rotationHandlePos = mouseWorld;
			}
			else
			{
				// When not dragging, position it above the chip
				rotationHandlePos = chipCenter + Vector2.up * rotationHandleDistance;
			}
			
			Color rotationColor = isDraggingRotation ? Color.green : Color.magenta;
			Draw.Point(rotationHandlePos, rotationHandleRadius, rotationColor);
			
			// Draw line from chip center to rotation handle
			Draw.Line(chipCenter, rotationHandlePos, 0.02f, rotationColor);
		}

		static void UpdatePinPositionsForCustomPolygon(SubChipInstance chip)
		{
			// Update all pins to be positioned on the custom polygon edges
			PinInstance[] allPins = chip.InputPins.Concat(chip.OutputPins).ToArray();
			Vector2 chipCenter = chip.Position;
			Vector2 chipHalfSize = chip.Size / 2f;
			float rotation = chip.Description.ShapeRotation;

			foreach (PinInstance pin in allPins)
			{
				// Project the pin's current position onto the custom polygon edges
				Vector2 localPos = GetPinPositionFromDescription(pin);
				Vector2 projectedPoint = ProjectOntoCustomPolygonEdge(localPos, chipHalfSize, rotation, chip.Description.CustomPolygon);
				UpdatePinPositionInDescription(pin, projectedPoint);
			}
		}

		static void UpdatePinPositionInDescription(PinInstance pin, Vector2 newPosition)
		{
			// Find and update the pin's position in the chip description
			if (pin.parent is SubChipInstance subchip)
			{
				var chipDesc = subchip.Description;
				
				// Search in input pins
				if (chipDesc.InputPins != null)
				{
					for (int i = 0; i < chipDesc.InputPins.Length; i++)
					{
						if (chipDesc.InputPins[i].ID == pin.ID)
						{
							var pinDesc = chipDesc.InputPins[i];
							pinDesc.Position = newPosition;
							chipDesc.InputPins[i] = pinDesc;
							return;
						}
					}
				}
				
				// Search in output pins
				if (chipDesc.OutputPins != null)
				{
					for (int i = 0; i < chipDesc.OutputPins.Length; i++)
					{
						if (chipDesc.OutputPins[i].ID == pin.ID)
						{
							var pinDesc = chipDesc.OutputPins[i];
							pinDesc.Position = newPosition;
							chipDesc.OutputPins[i] = pinDesc;
							return;
						}
					}
				}
			}
		}
		
		static Vector2 GetPinPositionFromDescription(PinInstance pin)
		{
			// Find the pin's position in the chip description
			if (pin.parent is SubChipInstance subchip)
			{
				var chipDesc = subchip.Description;
				
				// Search in input pins
				if (chipDesc.InputPins != null)
				{
					foreach (var pinDesc in chipDesc.InputPins)
					{
						if (pinDesc.ID == pin.ID)
						{
							return pinDesc.Position;
						}
					}
				}
				
				// Search in output pins
				if (chipDesc.OutputPins != null)
				{
					foreach (var pinDesc in chipDesc.OutputPins)
					{
						if (pinDesc.ID == pin.ID)
						{
							return pinDesc.Position;
						}
					}
				}
			}
			
			return Vector2.zero;
		}

		static Vector2 ProjectOntoCustomPolygonEdge(Vector2 localPos, Vector2 chipHalfSize, float rotation, CustomPolygonData polygon)
		{
			if (polygon == null) return localPos;

			float rotationRad = rotation * Mathf.Deg2Rad;
			Vector2 bestProjection = localPos;
			float bestDistance = float.MaxValue;

			// Convert normalized vertices to local positions
			Vector2[] localVertices = new Vector2[polygon.Vertices.Length];
			for (int i = 0; i < polygon.Vertices.Length; i++)
			{
				Vector2 normalizedPos = polygon.Vertices[i].ToVector2();
				Vector2 scaledPos = new Vector2(normalizedPos.x * chipHalfSize.x, normalizedPos.y * chipHalfSize.y);
				localVertices[i] = RotateVector(scaledPos, rotationRad);
			}

			// Check each edge
			for (int i = 0; i < localVertices.Length; i++)
			{
				int next = (i + 1) % localVertices.Length;
				Vector2 edgeStart = localVertices[i];
				Vector2 edgeEnd = localVertices[next];

				Vector2 projection;
				if (polygon.Edges != null && i < polygon.Edges.Length && polygon.Edges[i].IsCurved)
				{
					// Project onto curved edge
					projection = ProjectOntoQuadraticBezier(edgeStart, edgeEnd, polygon.Edges[i], localPos);
				}
				else
				{
					// Project onto straight edge
					projection = ProjectOntoLineSegment(edgeStart, edgeEnd, localPos);
				}

				float distance = Vector2.Distance(localPos, projection);
				if (distance < bestDistance)
				{
					bestDistance = distance;
					bestProjection = projection;
				}
			}

			return bestProjection;
		}

		static Vector2 ProjectOntoLineSegment(Vector2 start, Vector2 end, Vector2 point)
		{
			Vector2 line = end - start;
			float lineLength = line.magnitude;
			if (lineLength < 0.001f) return start;

			Vector2 lineDir = line / lineLength;
			Vector2 toPoint = point - start;
			float projectionLength = Vector2.Dot(toPoint, lineDir);
			projectionLength = Mathf.Clamp(projectionLength, 0f, lineLength);
			return start + lineDir * projectionLength;
		}

		static Vector2 ProjectOntoQuadraticBezier(Vector2 start, Vector2 end, PolygonEdge edge, Vector2 point)
		{
			// Calculate the control point for the bezier curve
			Vector2 edgeMidpointStraight = (start + end) * 0.5f;
			Vector2 edgeDirection = end - start;
			Vector2 perpendicular = new Vector2(-edgeDirection.y, edgeDirection.x).normalized;
			Vector2 controlPoint = edgeMidpointStraight + perpendicular * edge.CurveStrength;

			// Sample the curve to find the closest point
			int samples = 20;
			Vector2 bestPoint = start;
			float bestDistance = float.MaxValue;

			for (int i = 0; i <= samples; i++)
			{
				float t = i / (float)samples;
				Vector2 curvePoint = QuadraticBezier(start, controlPoint, end, t);
				float distance = Vector2.Distance(point, curvePoint);

				if (distance < bestDistance)
				{
					bestDistance = distance;
					bestPoint = curvePoint;
				}
			}

			return bestPoint;
		}

        enum DisplayInteractState
	{
		None,
		Moving,
		Placing,
		Scaling
	}
	}
}