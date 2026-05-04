using System.Collections.Generic;
using System.Linq;
using DLS.Description;
using DLS.Graphics;
using DLS.SaveSystem;
using Seb.Helpers;
using UnityEngine;
using System;
using UnityEditor;

namespace DLS.Game
{
	public class ChipInteractionController
	{
		public readonly Project project;
		
		// Helper method to safely access MobileUIController
		private static bool IsMobileUIControllerAvailable()
		{
			#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
			return MobileUIControllerWrapper.IsWrenchToolActive;
			#else
			return false;
			#endif
		}

	static bool ShouldHideChipInLevel(ChipType chipType)
	{
		var lm = DLS.Game.LevelsIntegration.LevelManager.Instance;
		bool isLevelActive = lm != null && lm.IsActive;
		return isLevelActive
			&& (chipType == ChipType.In_Pin || chipType == ChipType.Out_Pin);
	}

	static bool IsSpecialChipDisabledInLevel(ChipType chipType)
	{
		var lm = DLS.Game.LevelsIntegration.LevelManager.Instance;
		if (lm == null || !lm.IsActive) return false;
		
		// Check if chip type is in our "special" list
		return chipType == ChipType.Rom_256x16 ||
		       chipType == ChipType.EEPROM_256x16 ||
		       chipType == ChipType.dev_Ram_8Bit ||
		       chipType == ChipType.SevenSegmentDisplay ||
		       chipType == ChipType.DisplayRGB ||
		       chipType == ChipType.DisplayRGBTouch ||
		       chipType == ChipType.DisplayDot ||
		       chipType == ChipType.DisplayLED ||
		       chipType == ChipType.DisplayRGBLED ||
		       chipType == ChipType.Pulse ||
		       chipType == ChipType.Clock ||
		       chipType == ChipType.Key ||
		       chipType == ChipType.Button ||
		       chipType == ChipType.Toggle ||
		       chipType == ChipType.Detector ||
		       chipType == ChipType.Buzzer ||
		       chipType == ChipType.RTC ||
		       chipType == ChipType.SPS ||
		       chipType == ChipType.Constant_8Bit;
	}

		static void ShowInputOutputDisabledMessage()
		{
			SimpleMessagePopup.Open("Adding input/output pins is disabled for this level");
		}
		
		static void ShowSpecialChipDisabledMessage()
		{
			SimpleMessagePopup.Open("This chip type is disabled for this level");
		}

		static void ShowNestedDisallowedMessage()
		{
			SimpleMessagePopup.Open("This chip contains components that are not allowed in levels (e.g. ROM inside it). Remove them to use it.");
		}

		// ---- Control scheme settings ----
		public bool UseDragAndDropMode => project.description.Prefs_UseDragAndDropMode;

		// ---- Selection and placement state ----
		public readonly List<IMoveable> SelectedElements = new();
		public List<WireInstance> DuplicatedWires = new();
		public WireInstance WireToPlace;
		bool isPlacingNewElements;
		float itemPlacementCurrVerticalSpacing;
		bool newElementsAreDuplicatedElements;
		Vector2 moveElementMouseStartPos;
		IMoveable[] Obstacles; // Obstacles are the non-selected items when a group of elements is being moved
		public Vector2 SelectionBoxStartPos;
		StraightLineMoveState straightLineMoveState;
		bool hasExittedMultiModeSincePlacementStart;

		// ---- Wire edit state ----
		public WireInstance wireToEdit;
		public int wireEditPointIndex = -1;
		public bool wireEditCanInsertPoint;
		Vector2 wireEditPointOld;
		public int wireEditPointSelectedIndex;
		public bool isMovingWireEditPoint;
        // private bool reselectMoveElementMouseStartPos; // TODO: Implement if needed

        public DevChipInstance ActiveDevChip => project.ViewedChip;
		public bool IsMovingSelection { get; private set; }
		public bool IsCreatingSelectionBox { get; private set; }
		#if UNITY_ANDROID || UNITY_IOS
		public Vector2 SelectionBoxCentre => (TouchInputHelper.Instance.TouchStartPosition + TouchInputHelper.Instance.TouchWorldPosition) / 2;
		public Vector2 SelectionBoxSize => Maths.Abs(TouchInputHelper.Instance.TouchStartPosition-TouchInputHelper.Instance.TouchWorldPosition);
		#else
		public Vector2 SelectionBoxCentre => (SelectionBoxStartPos + InputHelper.MousePosWorld) / 2;
		public Vector2 SelectionBoxSize => Maths.Abs(SelectionBoxStartPos - InputHelper.MousePosWorld);
		#endif

		public bool HasControl => !UIDrawer.InInputBlockingMenu() && project.CanEditViewedChip;

		// Cannot interact with elements when other elements are being moved, in a menu, or drawing a selection box
		bool CanInteract => !IsMovingSelection && !UIDrawer.InInputBlockingMenu() && !IsCreatingSelectionBox && !InteractionState.MouseIsOverUI;
		public bool IsCreatingWire => WireToPlace != null;
		public bool IsPlacingElements => isPlacingNewElements;
		public bool IsPlacingElementOrCreatingWire => isPlacingNewElements || IsCreatingWire;
		public bool IsPlacingOrMovingElementOrCreatingWire => isPlacingNewElements || IsMovingSelection || IsCreatingWire || isMovingWireEditPoint;
		public bool CanInteractWithPin => CanInteract;
		public bool CanInteractWithPinStateDisplay => CanInteract && !IsCreatingWire && Project.ActiveProject.CanEditViewedChip;
		public bool CanInteractWithPinHandle => CanInteractWithPinStateDisplay;

		bool isDraggingWirePoint;
		public bool CanInteractWithButton => CanInteract;

		public ChipInteractionController(Project project)
		{
			this.project = project;
		}

		public void Update()
		{
			#if UNITY_ANDROID || UNITY_IOS
			HandleTouchInput();
			#else
			HandleKeyboardInput();
			HandleMouseInput();
			#endif
		}

		// Don't allow interaction with wire that's currently being placed (this would allow it to try to connect to itself for example...)
		public bool CanInteractWithWire(WireInstance wire) => CanInteract && wire != WireToPlace;

		public bool CanCompleteWireConnection(WireInstance wireToConnectTo, out PinInstance endPin)
		{
			// If we're joining this wire to an existing wire, choose the appropriate source/target pin from that wire to connect to
			endPin = WireToPlace.FirstPin.IsSourcePin ? wireToConnectTo.TargetPin_BusCorrected : wireToConnectTo.SourcePin;
			return CanCompleteWireConnection(endPin, wireToConnectTo);
		}

		public bool CanCompleteWireConnection(PinInstance endPin, WireInstance wireToConnectTo = null)
		{
			if (!IsCreatingWire) return false;

			PinInstance startPin = WireToPlace.FirstPin;
			bool connectingFromWire = WireToPlace.FirstConnectionInfo.IsConnectedAtWire;
			bool connectingToWire = wireToConnectTo != null;
			WireInstance wireConnection = wireToConnectTo ?? WireToPlace.FirstConnectionInfo.connectedWire;

			// Don't allow wire-to-wire connections (ambiguous where to get signal source from, and where to carry it to)
			if (connectingFromWire && connectingToWire) return false;

			// (Maybe temporary restriction?): Don't allow sourcePin-to-wire connections (unless the wire is a bus wire).
			// This is because if the two source pins have different states, then the wire would need to be coloured differently
			// from the connection point onwards (depending on which of the conflicting states is chosen)
			if (connectingFromWire || connectingToWire)
			{
				PinInstance pinConnection = connectingToWire ? startPin : endPin;
				if (pinConnection.IsSourcePin && !wireConnection.IsBusWire) return false;
			}

			// Ensure connection is between a source and a target pin
			// (note: if connection starts or ends at a wire then it's valid regardless, since we can just pick source/target pin from that wire as needed)
			bool hasSourceAndTarget = startPin.IsSourcePin != endPin.IsSourcePin || connectingFromWire || connectingToWire;
			if (!hasSourceAndTarget || endPin.bitCount != startPin.bitCount) return false;

			// Only allow connecting bus origin and terminus if they are linked together (i.e. were created together at same time; rather than any random pair)
			// Note: could consider lifting this restriction, but need to investigate impact on simulation...
			if (startPin.IsBusPin && endPin.IsBusPin)
			{
				SubChipInstance busA = (SubChipInstance)endPin.parent;
				SubChipInstance busB = (SubChipInstance)startPin.parent;
				return busA.LinkedBusPairID == busB.ID;
			}


			return true;
		}

		public static bool IsSelected(IMoveable element) => element.IsSelected;
		
		public void Delete(IMoveable element)
		{
			DeleteElements(new List<IMoveable>(new[] { element }));
		}

	void DeleteElements(List<IMoveable> elements, bool clearSelection = true)
	{
		if (!HasControl) return;
		List<IMoveable> elementsToDelete = elements.Concat(GetNonIncludedLinkedBusElements(elements)).ToList();
		
		// Check for anchored pins and disabled chips in level mode
		bool shouldCancel = false;
		for (int i = elementsToDelete.Count - 1; i >= 0; i--)
		{
			IMoveable element = elementsToDelete[i];
			
			// Check for anchored pins (level-provided)
			if (element is DevPinInstance dp && dp.anchoredToLevel)
			{
				elementsToDelete.RemoveAt(i);
				shouldCancel = true;
				continue;
			}
			
			// Check for Input/Output pins or special chips in level mode
			ChipType chipType;
			if (element is SubChipInstance subChip)
			{
				chipType = subChip.ChipType;
			}
			else if (element is DevPinInstance devPin)
			{
				chipType = devPin.IsInputPin ? ChipType.In_Pin : ChipType.Out_Pin;
			}
			else
			{
				continue;
			}
			
			if (ShouldHideChipInLevel(chipType))
			{
				ShowInputOutputDisabledMessage();
				elementsToDelete.RemoveAt(i);
				shouldCancel = true;
				continue;
			}
			
			if (IsSpecialChipDisabledInLevel(chipType))
			{
				ShowSpecialChipDisabledMessage();
				elementsToDelete.RemoveAt(i);
				shouldCancel = true;
				continue;
			}
		}
		
		if (shouldCancel)
		{
			FinishMovingElements();
			return;
		}
		
		if (elementsToDelete.Count == 0) return;

		// Collect group IDs of deleted elements so we can ungroup remaining elements
		var groupIdsOfDeleted = new HashSet<int>();
		foreach (IMoveable e in elementsToDelete)
		{
			int g = e switch { SubChipInstance s => s.GroupId, DevPinInstance d => d.GroupId, _ => 0 };
			if (g != 0) groupIdsOfDeleted.Add(g);
		}

		ActiveDevChip.UndoController.RecordDeleteElements(elementsToDelete);

		foreach (IMoveable element in elementsToDelete)
		{
			Debug.Log($"Deleting {element}");
			if (element is SubChipInstance subChip) ActiveDevChip.DeleteSubChip(subChip);
			else if (element is DevPinInstance devPin) ActiveDevChip.DeleteDevPin(devPin);
		}

		// Clear GroupId on any remaining elements that were in the same groups, so the background disappears
		foreach (IMoveable remaining in ActiveDevChip.Elements)
		{
			int g = remaining switch { SubChipInstance s => s.GroupId, DevPinInstance d => d.GroupId, _ => 0 };
			if (g != 0 && groupIdsOfDeleted.Contains(g))
			{
				if (remaining is SubChipInstance sc) sc.GroupId = 0;
				else if (remaining is DevPinInstance dp) dp.GroupId = 0;
			}
		}

		if (clearSelection) SelectedElements.Clear();
	}

		public void DeleteSelected()
		{
			// Delete selected subchips/pins
			if (SelectedElements.Count > 0)
			{
				DeleteElements(SelectedElements);
			}
			// Delete wire under mouse
			else if (InteractionState.ElementUnderMouse is WireInstance wire && wire != wireToEdit)
			{
				DeleteWire(wire);
			}
			// Delete wire point under mouse (in wire edit mode)
			else if (wireToEdit != null && wireEditPointIndex != -1)
			{
				bool isWireToWireConnectionPoint = wireEditPointIndex == 0 || wireEditPointIndex == wireToEdit.WirePointCount - 1;
				// Can't delete the point connecting a wire to another wire
				if (!isWireToWireConnectionPoint)
				{
					foreach (WireInstance other in ActiveDevChip.Wires)
					{
						if (other.ConnectedWire == wireToEdit)
						{
							other.NotifyParentWirePointWillBeDeleted(wireEditPointIndex);
						}
					}

					wireToEdit.DeleteWirePoint(wireEditPointIndex);
					wireEditPointIndex = -1;
					isMovingWireEditPoint = false;
				}
			}
		}

		// Track elements deleted during current drag to avoid duplicate deletions
		// Use object references for wires (no ID property), and IDs for IMoveable elements
		private HashSet<object> elementsDeletedThisDrag = new HashSet<object>();

		/// <summary>
		/// Handles tap/drag when eraser mode is active - performs immediate deletion (mobile only)
		/// </summary>
		void HandleEraserModeTap()
		{
			if (!EraserModeController.IsActive) return;
			if (!HasControl) return;

			var elementUnderMouse = InteractionState.ElementUnderMouse;

			// Don't delete if tapping/dragging on empty space
			if (elementUnderMouse == null) return;

			// Get element identifier to track if we've already deleted it this drag
			object elementIdentifier = null;
			if (elementUnderMouse is IMoveable moveableElement)
			{
				elementIdentifier = moveableElement.ID;
			}
			else if (elementUnderMouse is WireInstance wire)
			{
				// Use object reference for wires (they don't have an ID property)
				elementIdentifier = wire;
			}

			// Skip if we've already deleted this element during the current drag
			if (elementIdentifier != null && elementsDeletedThisDrag.Contains(elementIdentifier))
			{
				return;
			}

			// Handle different eraser modes
			if (EraserModeController.CurrentMode == EraserModeController.EraserMode.WiresOnly)
			{
				// Only delete wires in WiresOnly mode
				if (elementUnderMouse is WireInstance wire && wire != wireToEdit)
				{
					DeleteWire(wire);
					elementsDeletedThisDrag.Add(wire);
					Debug.Log("[EraserMode] Deleted wire (WiresOnly mode)");
				}
			}
			else // DeleteAll mode
			{
				// Delete any element
				if (elementUnderMouse is IMoveable moveable)
				{
					Delete(moveable);
					elementsDeletedThisDrag.Add(moveable.ID);
					Debug.Log($"[EraserMode] Deleted {moveable.GetType().Name} (DeleteAll mode)");
				}
				else if (elementUnderMouse is WireInstance wire && wire != wireToEdit)
				{
					DeleteWire(wire);
					elementsDeletedThisDrag.Add(wire);
					Debug.Log("[EraserMode] Deleted wire (DeleteAll mode)");
				}
			}
		}

		public void DeleteWire(WireInstance wire)
		{
			if (HasControl)
			{
				ActiveDevChip.UndoController.RecordDeleteWire(wire);
				ActiveDevChip.DeleteWire(wire);
			}
		}

		public void ToggleDevPinState(DevPinInstance devPin, int bitIndex)
		{
			if (HasControl) devPin.ToggleState(bitIndex);
		}

		void HandleKeyboardInput()
		{
			// Ignore shortcuts if don't have control
			if (!HasControl) return;

			if (KeyboardShortcuts.UndoShortcutTriggered) ActiveDevChip.UndoController.TryUndo();
			else if (KeyboardShortcuts.RedoShortcutTriggered) ActiveDevChip.UndoController.TryRedo();


			if (!KeyboardShortcuts.StraightLineModeHeld) straightLineMoveState = StraightLineMoveState.None;

			if (KeyboardShortcuts.SearchShortcutTriggered)
			{
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.Search);
			}


			if (KeyboardShortcuts.DuplicateShortcutTriggered)
			{
				if (SelectedElements.Count > 0 && !IsPlacingOrMovingElementOrCreatingWire)
				{
					DuplicateSelectedElements();
				}
			}

			if (project.description.Prefs_RotationEnabled && InputHelper.IsKeyDownThisFrame(KeyCode.E) && !InputHelper.CtrlIsHeld && !IsPlacingOrMovingElementOrCreatingWire)
			{
				int steps = project.description.Prefs_RotationSteps;
				int step = steps > 0 ? 360 / steps : 15;
				if (HasRotatableSelection) RotateSelected(step);
				else CameraController.RotateCamera(step);
			}
			if (project.description.Prefs_RotationEnabled && InputHelper.IsKeyDownThisFrame(KeyCode.Q) && !InputHelper.CtrlIsHeld && !IsPlacingOrMovingElementOrCreatingWire)
			{
				int steps = project.description.Prefs_RotationSteps;
				int step = steps > 0 ? 360 / steps : 15;
				if (HasRotatableSelection) RotateSelected(-step);
				else CameraController.RotateCamera(-step);
			}

			if (KeyboardShortcuts.DeleteShortcutTriggered)
			{
				if (IsCreatingWire)
				{
					if (!WireToPlace.RemoveLastPoint())
					{
						CancelEverything();
					}
				}
				else
				{
					if (isPlacingNewElements) CancelPlacingItems();
					else DeleteSelected();
				}
			}

			if (KeyboardShortcuts.ConfirmShortcutTriggered)
			{
				ExitWireEditMode();
			}

			if (KeyboardShortcuts.CancelShortcutTriggered)
			{
				CancelEverything();
			}

        }

		// Two-finger chip rotate: (A) pivot-on-chip when one finger on chip, or (B) rigid transform when selection exists (Logic Puzzler style)
		static bool isTwoFingerRotatingChips;
		static bool twoFingerModePivotOnChip; // true = single chip pivot mode, false = selection rigid mode
		static SubChipInstance twoFingerRotateChip;
		static List<(SubChipInstance chip, Vector2 startPos, int startRotation)> twoFingerSelectionState;
		static bool twoFingerPivotIsTouch0;
		static Vector2 twoFingerPivotAnchorWorld;
		static Vector2 twoFingerOtherAnchorWorld;
		static Vector2 twoFingerChipStartPos;
		static int twoFingerChipStartRotation;
		static Vector2 twoFingerAnchorWorld1, twoFingerAnchorWorld2;

		/// <summary>True when actively doing two-finger chip rotate. Camera skips two-finger when this is true.</summary>
		public static bool IsTwoFingerRotatingChip { get; private set; }

		SubChipInstance GetSubChipAtWorldPos(Vector2 worldPos)
		{
			var chip = ActiveDevChip;
			if (chip == null) return null;
			for (int i = chip.Elements.Count - 1; i >= 0; i--)
			{
				if (chip.Elements[i] is SubChipInstance sub &&
					DevSceneDrawer.PointInsideRotatedBounds_World(sub.Position, sub.Size, sub.Rotation, worldPos))
					return sub;
			}
			return null;
		}

		/// <summary>True if chip controller should handle two-finger (finger on chip or has selection). Camera skips when this is true.</summary>
		public static bool ShouldChipControllerHandleTwoFinger(Vector2 worldPos1, Vector2 worldPos2)
		{
			var controller = Project.ActiveProject?.controller;
			if (controller == null) return false;
			if (controller.HasRotatableSelection) return true;
			return controller.GetSubChipAtWorldPos(worldPos1) != null || controller.GetSubChipAtWorldPos(worldPos2) != null;
		}

		void HandleTouchInput()
		{
			if (Input.touchCount != 2)
			{
				if (isTwoFingerRotatingChips)
					RecordTwoFingerRotateUndo();
				isTwoFingerRotatingChips = false;
				IsTwoFingerRotatingChip = false;
			}

			if (Input.touchCount == 2 && HasControl && project.description.Prefs_RotationEnabled && !isPlacingNewElements && !IsCreatingWire && !isMovingWireEditPoint)
			{
				HandleTwoFingerChipRotate();
				if (IsTwoFingerRotatingChip) return;
			}

			if(Input.touchCount == 1){
				Touch touch = Input.GetTouch(0);
				// When already in an active circuit operation (drag, place, create wire), don't let banner UI block
				// Moved/Ended—otherwise finger over banner creates a dead zone mid-drag.
				bool inActiveCircuitOperation = IsPlacingOrMovingElementOrCreatingWire;
				bool blockForUI = !inActiveCircuitOperation || touch.phase == TouchPhase.Began;
				bool overEventSystemUI = UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.fingerId);
				bool wouldReturnForUI = blockForUI && (overEventSystemUI || InteractionState.MouseIsOverUI);

				if (wouldReturnForUI) return;
				if (MobileUIControllerWrapper.IsWrenchToolActive) return;
				
				// Only block for wire placement banner when we're actually placing a wire (otherwise stale bounds create invisible dead zone)
				if (IsCreatingWire && DLS.Graphics.WirePlacementBanner.IsTouchOverBanner(touch.position)) return;
			
				if(touch.phase == TouchPhase.Began){
					HandleSingleTap();
				}else if(touch.phase == TouchPhase.Moved){
					// In eraser mode, continuously delete elements while dragging
					if (EraserModeController.IsActive)
					{
						HandleEraserModeTap();
					}
					else if (HasControl)
					{
						UpdatePositionsToMouse();
					}
				}else if(touch.phase == TouchPhase.Ended){
					HandleTouchEnd();
				}
			}
		}

		void HandleTouchEnd()
		{
			if (IsCreatingSelectionBox)
			{
				IsCreatingSelectionBox = false;
				MobileUIControllerWrapper.OnBoxSelectToolPress();

				float selectionBoxArea = Mathf.Abs(SelectionBoxSize.x * SelectionBoxSize.y);
				if (selectionBoxArea > 0.000001f)
				{
					foreach (IMoveable element in ActiveDevChip.Elements)
					{
						if (element.ShouldBeIncludedInSelectionBox(SelectionBoxCentre, SelectionBoxSize))
						{
							Select(element);
						}
					}
				}
			}
			// In drag and drop mode, auto-place components when touch ends
			else if (UseDragAndDropMode)
			{
				if (IsPlacingElements)
				{
					FinishPlacingNewElements();
				}
				else if (IsMovingSelection)
				{
					FinishMovingElements();
				}
			}
		}

		void HandleTwoFingerChipRotate()
		{
			Touch t1 = Input.GetTouch(0);
			Touch t2 = Input.GetTouch(1);
			Vector2 screen1 = t1.position;
			Vector2 screen2 = t2.position;
			Camera cam = InputHelper.WorldCam;
			Vector2 world1 = cam.ScreenToWorldPoint(screen1);
			Vector2 world2 = cam.ScreenToWorldPoint(screen2);

			SubChipInstance chip1 = GetSubChipAtWorldPos(world1);
			SubChipInstance chip2 = GetSubChipAtWorldPos(world2);

			if (chip1 == null && chip2 == null && !HasRotatableSelection) return;

			if (IsMovingSelection)
				CommitCurrentMoveForTwoFingerTransition();

			if (t1.phase == TouchPhase.Began || t2.phase == TouchPhase.Began)
			{
				SubChipInstance pivotChip = chip1 ?? chip2;
				if (pivotChip != null)
				{
					isTwoFingerRotatingChips = true;
					IsTwoFingerRotatingChip = true;
					twoFingerModePivotOnChip = true;
					twoFingerRotateChip = pivotChip;
					twoFingerSelectionState = null;
					twoFingerPivotIsTouch0 = (pivotChip == chip1);
					twoFingerPivotAnchorWorld = (pivotChip == chip1) ? world1 : world2;
					twoFingerOtherAnchorWorld = (pivotChip == chip1) ? world2 : world1;
					twoFingerChipStartPos = pivotChip.Position;
					twoFingerChipStartRotation = pivotChip.Rotation;
				}
				else if (HasRotatableSelection)
				{
					isTwoFingerRotatingChips = true;
					IsTwoFingerRotatingChip = true;
					twoFingerModePivotOnChip = false;
					twoFingerRotateChip = null;
					twoFingerAnchorWorld1 = world1;
					twoFingerAnchorWorld2 = world2;
					twoFingerSelectionState = new List<(SubChipInstance, Vector2, int)>();
					foreach (var element in SelectedElements)
						if (element is SubChipInstance sub)
							twoFingerSelectionState.Add((sub, sub.Position, sub.Rotation));
				}
			}

			if (!isTwoFingerRotatingChips) return;

			int steps = project.description.Prefs_RotationSteps;
			if (steps <= 0) steps = 0;

			if (twoFingerModePivotOnChip && twoFingerRotateChip != null)
			{
				Touch pivotTouch = twoFingerPivotIsTouch0 ? t1 : t2;
				Touch otherTouch = twoFingerPivotIsTouch0 ? t2 : t1;
				Vector2 pivotWorldNow = GetTouchWorldPos(pivotTouch);
				Vector2 otherWorldNow = GetTouchWorldPos(otherTouch);

				var result = TwoFingerRigidTransform.SolveChipPivotRotate(
					twoFingerPivotAnchorWorld, twoFingerOtherAnchorWorld,
					pivotWorldNow, otherWorldNow, steps);

				twoFingerRotateChip.Position = result.TransformPoint(twoFingerChipStartPos);
				float totalRot = (twoFingerChipStartRotation + result.RotationDeltaDegrees + 360) % 360f;
				if (totalRot < 0) totalRot += 360f;
				if (steps > 0)
				{
					int stepDeg = 360 / steps;
					twoFingerRotateChip.Rotation = (Mathf.RoundToInt(totalRot / stepDeg) * stepDeg) % 360;
				}
				else
					twoFingerRotateChip.Rotation = Mathf.RoundToInt(totalRot) % 360;
			}
			else if (twoFingerSelectionState != null)
			{
				Vector2 targetWorld1 = GetTouchWorldPos(t1);
				Vector2 targetWorld2 = GetTouchWorldPos(t2);
				var result = TwoFingerRigidTransform.SolveChipRigid(
					twoFingerAnchorWorld1, twoFingerAnchorWorld2,
					targetWorld1, targetWorld2, steps);

				int stepDeg = steps > 0 ? 360 / steps : 1;
				foreach (var (chip, startPos, startRotation) in twoFingerSelectionState)
				{
					chip.Position = result.TransformPoint(startPos);
					int totalRot = (startRotation + Mathf.RoundToInt(result.RotationDeltaDegrees) + 360) % 360;
					if (totalRot < 0) totalRot += 360;
					chip.Rotation = (Mathf.RoundToInt((float)totalRot / stepDeg) * stepDeg) % 360;
				}
			}
		}

		static Vector2 GetTouchWorldPos(Touch t)
		{
			Vector3 p = new Vector3(t.position.x, t.position.y, 0f);
			return (Vector2)InputHelper.WorldCam.ScreenToWorldPoint(p);
		}

		void RecordTwoFingerRotateUndo()
		{
			if (twoFingerModePivotOnChip && twoFingerRotateChip != null)
			{
				Vector2 originalPos = twoFingerChipStartPos;
				int originalRot = twoFingerChipStartRotation;
				Vector2 newPos = twoFingerRotateChip.Position;
				int newRot = twoFingerRotateChip.Rotation;
				if (originalPos != newPos || originalRot != newRot)
					ActiveDevChip.UndoController.RecordRigidTransformElements(
						new List<SubChipInstance> { twoFingerRotateChip },
						new[] { originalPos }, new[] { newPos },
						new[] { originalRot }, new[] { newRot });
				twoFingerRotateChip = null;
			}
			else if (twoFingerSelectionState != null)
			{
				var toRotate = twoFingerSelectionState.Select(t => t.chip).ToList();
				Vector2[] originalPositions = twoFingerSelectionState.Select(t => t.startPos).ToArray();
				Vector2[] newPositions = toRotate.Select(s => s.Position).ToArray();
				int[] originalRotations = twoFingerSelectionState.Select(t => t.startRotation).ToArray();
				int[] newRotations = toRotate.Select(s => s.Rotation).ToArray();
				bool changed = false;
				for (int i = 0; i < toRotate.Count && !changed; i++)
					changed = originalPositions[i] != newPositions[i] || originalRotations[i] != newRotations[i];
				if (changed)
					ActiveDevChip.UndoController.RecordRigidTransformElements(toRotate, originalPositions, newPositions, originalRotations, newRotations);
				twoFingerSelectionState = null;
			}
		}

		void HandleMouseInput()
		{
			if (HasControl) UpdatePositionsToMouse();

			// --- Mouse button input ---
			if (InputHelper.IsMouseDownThisFrame(MouseButton.Left)) HandleLeftMouseDown();
			if (InputHelper.IsMouseUpThisFrame(MouseButton.Left)) HandleLeftMouseUp();
			if (InputHelper.IsMouseDownThisFrame(MouseButton.Right)) HandleRightMouseDown();

			// Shift + scroll to increase vertical spacing between elements when placing multiple at a time
			// (disabled if elements were duplicated since then we want to preserve relative positions)
			if (isPlacingNewElements && !newElementsAreDuplicatedElements && InputHelper.ShiftIsHeld)
			{
				itemPlacementCurrVerticalSpacing += InputHelper.MouseScrollDelta.y * DrawSettings.GridSize;
				itemPlacementCurrVerticalSpacing = Mathf.Max(0, itemPlacementCurrVerticalSpacing);
			}
			// Ctrl + scroll to rotate selected chips or camera (when nothing selected)
			else if (project.description.Prefs_RotationEnabled && !isPlacingNewElements && !IsMovingSelection && InputHelper.CtrlIsHeld)
			{
				float scroll = InputHelper.MouseScrollDelta.y;
				if (Mathf.Abs(scroll) > 0.01f)
				{
					int steps = project.description.Prefs_RotationSteps;
					int step = steps > 0 ? 360 / steps : 15;
					int delta = scroll > 0 ? step : -step;
					if (HasRotatableSelection) RotateSelected(delta);
					else CameraController.RotateCamera(delta);
				}
			}
		}

		List<IMoveable> GetNonIncludedLinkedBusElements(List<IMoveable> elements)
		{
			List<IMoveable> nonIncludedBusPairs = new();
			HashSet<int> elementIDs = elements.Select(e => e.ID).ToHashSet();

			foreach (IMoveable element in elements)
			{
				if (element is SubChipInstance subChip && subChip.IsBus)
				{
					if (!elementIDs.Contains(subChip.LinkedBusPairID))
					{
						ActiveDevChip.TryGetSubChipByID(subChip.LinkedBusPairID, out SubChipInstance pairedBus);
						nonIncludedBusPairs.Add(pairedBus);
					}
				}
			}

			return nonIncludedBusPairs;
		}

		// Set the correct LinkedBusPairIDs on duplicated elements (still set to original IDs at this point)
		static void LinkDuplicatedBuses(List<IMoveable> duplicatedElements, IMoveable[] originalElements)
		{
			List<SubChipInstance> busOrigins = new();
			List<SubChipInstance> busTerminuses = new();

			Dictionary<int, int> lookup = new();
			foreach (IMoveable element in originalElements)
			{
				if (element is not SubChipInstance subChip) continue;
				if (ChipTypeHelper.IsBusTerminusType(subChip.ChipType)) lookup[subChip.ID] = subChip.LinkedBusPairID;
			}

			foreach (IMoveable element in duplicatedElements)
			{
				if (element is not SubChipInstance subChip) continue;
				if (ChipTypeHelper.IsBusTerminusType(subChip.ChipType)) busTerminuses.Add(subChip);
				else if (ChipTypeHelper.IsBusOriginType(subChip.ChipType)) busOrigins.Add(subChip);
			}

			foreach (SubChipInstance busOrigin in busOrigins)
			{
				int originalBusOriginId = lookup[busOrigin.LinkedBusPairID];
				foreach (SubChipInstance busTerminus in busTerminuses)
				{
					if (busTerminus.LinkedBusPairID == originalBusOriginId)
					{
						busOrigin.SetLinkedBusPair(busTerminus);
						busTerminus.SetLinkedBusPair(busOrigin);
						break;
					}
				}
			}
		}

	void DuplicateElements(List<IMoveable> elements)
	{
		if (elements.Count == 0) return;
		IMoveable[] elementsToDuplicate = elements.Concat(GetNonIncludedLinkedBusElements(elements)).ToArray();

		List<IMoveable> duplicatedElements = new(elementsToDuplicate.Length);
		Dictionary<int, int> duplicatedElementIDFromOriginalID = new();

		// Get description of each element, and start placing a copy of it
		foreach (IMoveable element in elementsToDuplicate)
		{
			// Check if this element should be disabled in level mode
			ChipType chipType;
			if (element is SubChipInstance subChip)
			{
				chipType = subChip.ChipType;
			}
			else if (element is DevPinInstance devPin)
			{
				chipType = devPin.IsInputPin ? ChipType.In_Pin : ChipType.Out_Pin;
			}
			else
			{
				// Unknown element type, skip
				continue;
			}
			
			// Check if trying to duplicate Input/Output pins in a level
			if (ShouldHideChipInLevel(chipType))
			{
				ShowInputOutputDisabledMessage();
				continue; // Skip this element
			}
			
			// Check if trying to duplicate special chips in a level
			if (IsSpecialChipDisabledInLevel(chipType))
			{
				ShowSpecialChipDisabledMessage();
				continue; // Skip this element
			}
			
			IMoveable duplicatedElement = CreateElementFromDuplicationSource(element);
			StartPlacing(duplicatedElement, element.Position, true);
			duplicatedElement.StraightLineReferencePoint = element.Position;
			duplicatedElements.Add(duplicatedElement);
			duplicatedElementIDFromOriginalID.Add(element.ID, duplicatedElement.ID);
		}

		// If no elements were duplicated (all were skipped), cancel the duplication process
		if (duplicatedElements.Count == 0)
		{
			CancelEverything();
			return;
		}

			LinkDuplicatedBuses(duplicatedElements, elementsToDuplicate);

			// ---- Duplicate wires ----
			Dictionary<WireInstance, WireInstance> duplicatedWireFromOriginal = new();
			DuplicatedWires.Clear();

			foreach (WireInstance wire in ActiveDevChip.Wires)
			{
				bool wireSourceHasBeenDuplicated = duplicatedElementIDFromOriginalID.TryGetValue(wire.SourcePin.Address.PinOwnerID, out int sourceID);
				bool wireTargetHasBeenDuplicated = duplicatedElementIDFromOriginalID.TryGetValue(wire.TargetPin.Address.PinOwnerID, out int targetID);

				if (wireSourceHasBeenDuplicated && wireTargetHasBeenDuplicated)
				{
					PinAddress duplicatedSourcePinAddress = new(sourceID, wire.SourcePin.Address.PinID);
					PinAddress duplicatedTargetPinAddress = new(targetID, wire.TargetPin.Address.PinID);

					DevChipInstance.TryFindPin(duplicatedElements, duplicatedSourcePinAddress, out PinInstance duplicatedSourcePin);
					DevChipInstance.TryFindPin(duplicatedElements, duplicatedTargetPinAddress, out PinInstance duplicatedTargetPin);

					Debug.Assert(duplicatedSourcePin != null && duplicatedTargetPin != null, "Pins not found for duplicated wire!");

					WireInstance duplicatedConnectedSourceWire = null;
					WireInstance duplicatedConnectedTargetWire = null;
					if (wire.SourceConnectionInfo.connectedWire != null) duplicatedWireFromOriginal.TryGetValue(wire.SourceConnectionInfo.connectedWire, out duplicatedConnectedSourceWire);
					if (wire.TargetConnectionInfo.connectedWire != null) duplicatedWireFromOriginal.TryGetValue(wire.TargetConnectionInfo.connectedWire, out duplicatedConnectedTargetWire);

					WireInstance.ConnectionInfo sourceConnectionInfo = new()
					{
						pin = duplicatedSourcePin,
						connectedWire = duplicatedConnectedSourceWire,
						connectionPoint = wire.SourceConnectionInfo.connectionPoint,
						wireConnectionSegmentIndex = wire.SourceConnectionInfo.wireConnectionSegmentIndex
					};

					WireInstance.ConnectionInfo targetConnectionInfo = new()
					{
						pin = duplicatedTargetPin,
						connectedWire = duplicatedConnectedTargetWire,
						connectionPoint = wire.TargetConnectionInfo.connectionPoint,
						wireConnectionSegmentIndex = wire.TargetConnectionInfo.wireConnectionSegmentIndex
					};

					Vector2[] wirePoints = new Vector2[wire.WirePointCount];
					for (int i = 0; i < wirePoints.Length; i++)
					{
						wirePoints[i] = wire.GetWirePoint(i);
					}

					WireInstance duplicatedWire = new(sourceConnectionInfo, targetConnectionInfo, wirePoints, ActiveDevChip.Wires.Count + DuplicatedWires.Count);
					duplicatedWireFromOriginal.Add(wire, duplicatedWire);
					DuplicatedWires.Add(duplicatedWire);
				}
			}

			#if !(UNITY_ANDROID || UNITY_IOS)
			// Find element closest to mouse to use as origin point for duplicated elements
			Vector2 mousePos = InputHelper.MousePosWorld;
			Vector2 closestElementPos = Vector2.zero;
			float closestDst = float.MaxValue;

			foreach (IMoveable element in elementsToDuplicate)
			{
				Vector2 pos = element is DevPinInstance pin ? pin.HandlePosition : element.Position;
				float dst = Vector2.Distance(pos, mousePos);
				if (dst < closestDst)
				{
					closestDst = dst;
					closestElementPos = pos;
				}
			}

			Vector2 offset = InputHelper.MousePosWorld - closestElementPos;
			moveElementMouseStartPos -= offset;
			#else
			Vector2 averageElementPos = Vector2.zero;
			int counter = 0;
			foreach (IMoveable element in elementsToDuplicate)
			{
				Vector2 pos = element is DevPinInstance pin ? pin.HandlePosition : element.Position;
				averageElementPos+=pos;
				counter++;
			}
			moveElementMouseStartPos  = averageElementPos/counter;
			// reselectMoveElementMouseStartPos = true; // TODO: Implement if needed
			#endif
		}

		public void DuplicateSelectedElements()
		{
			FinishMovingElements(false);
			DuplicateElements(SelectedElements);
		}

		public void RotateSelected(int deltaDegrees)
		{
			List<SubChipInstance> toRotate = SelectedElements.OfType<SubChipInstance>().ToList();
			if (toRotate.Count == 0) return;

			int steps = project.description.Prefs_RotationSteps;
			int stepDegrees = steps > 0 ? 360 / steps : 1;

			int[] originalRotations = toRotate.Select(s => s.Rotation).ToArray();
			foreach (SubChipInstance subchip in toRotate)
				subchip.RotateBy(deltaDegrees, stepDegrees);
			int[] newRotations = toRotate.Select(s => s.Rotation).ToArray();

			ActiveDevChip.UndoController.RecordRotateElements(toRotate, originalRotations, newRotations);
		}

		public bool HasRotatableSelection => project.description.Prefs_RotationEnabled && SelectedElements.OfType<SubChipInstance>().Any();

		public void Select(IMoveable element, bool addToCurrentSelection = true)
		{
			ExitWireEditMode();

			// Group-aware selection: clicking one element in a group selects the whole group (unless adding to selection)
			int elementGroupId = element switch { SubChipInstance sc => sc.GroupId, DevPinInstance dp => dp.GroupId, _ => 0 };
			if (!addToCurrentSelection && elementGroupId != 0)
			{
				ClearSelection();
				foreach (IMoveable e in ActiveDevChip.Elements)
				{
					int g = e switch { SubChipInstance s => s.GroupId, DevPinInstance d => d.GroupId, _ => 0 };
					if (g == elementGroupId)
					{
						SelectedElements.Add(e);
						e.IsSelected = true;
						e.IsValidMovePos = true;
					}
				}
				return;
			}

			if (element.IsSelected)
			{
				// If in add mode, and element already selected, then remove it from the selection
				if (addToCurrentSelection)
				{
					element.IsSelected = false;
					SelectedElements.Remove(element);
				}
			}
			else
			{
				if (!addToCurrentSelection)
				{
					ClearSelection();
				}

				SelectedElements.Add(element);
				element.IsSelected = true;
				element.IsValidMovePos = true;
			}
		}

		void ExpandSelectionToIncludeFullGroups()
		{
			var groupIdsToAdd = new HashSet<int>();
			foreach (IMoveable e in SelectedElements)
			{
				int g = e switch { SubChipInstance s => s.GroupId, DevPinInstance d => d.GroupId, _ => 0 };
				if (g != 0) groupIdsToAdd.Add(g);
			}
			foreach (IMoveable e in ActiveDevChip.Elements)
			{
				int g = e switch { SubChipInstance s => s.GroupId, DevPinInstance d => d.GroupId, _ => 0 };
				if (g != 0 && groupIdsToAdd.Contains(g) && !e.IsSelected)
				{
					SelectedElements.Add(e);
					e.IsSelected = true;
					e.IsValidMovePos = true;
				}
			}
		}

		void HandleRightMouseDown()
		{
			// Cancel placement by right-clicking
			if (IsPlacingOrMovingElementOrCreatingWire)
			{
				CancelEverything();
				InputHelper.ConsumeMouseButtonDownEvent(MouseButton.Right);
			}

			IsCreatingSelectionBox = false;
			// Don't clear selection when right-clicking on something with multi-selection —
			// context menu needs the selection intact to show "Make group"
			bool keepSelectionForContextMenu =
				SelectedElements.Count >= 2 &&
				InteractionState.ElementUnderMouse != null;
			if (!keepSelectionForContextMenu)
				ClearSelection();
		}

		void HandleSingleTap(){
			
			// Check if eraser mode is active - handle immediate deletion
			if (EraserModeController.IsActive)
			{
				// Clear the drag tracking set when starting a new tap/drag
				elementsDeletedThisDrag.Clear();
				HandleEraserModeTap();
				return; // Don't proceed with normal selection logic
			}
			
			//WorldDrawer.DrawWorld(Project.ActiveProject);
			if (wireToEdit != null)
			{
				if (wireEditPointIndex != -1){
					isMovingWireEditPoint = true;
					wireEditPointSelectedIndex = wireEditPointIndex;
					wireEditPointOld = wireToEdit.GetWirePoint(wireEditPointIndex);
				}
			}
			wireEditPointIndex = -1;
			if (IsPlacingElementOrCreatingWire)
			{
				// Place wire
				if (IsCreatingWire) //
				{
					if (TryFinishPlacingWire())
					{
						CancelPlacingItems();
						MobileUIControllerWrapper.HidePlacementButtons();
					}
					else if (CanAddWirePoint())
					{
						if(WireToPlace.WirePointCount<=1)
							WireToPlace.AddWirePoint(InputHelper.MousePosWorld);
						else
							WireToPlace.SetLastWirePoint(InputHelper.MousePosWorld);
					}
				}else{ //IsPlacing Element
					if(InteractionState.ElementUnderMouse is IMoveable element){
						StartMovingSelectedItems();
					}
				}
			}else if(MobileUIControllerWrapper.IsWrenchToolActive){
				SelectionBoxStartPos = TouchInputHelper.Instance.TouchWorldPosition;
			}
			else
			{
				Vector2 touchPosWorld = TouchInputHelper.TouchPositionWorld();
	   			// Tapping on pin
				if (InteractionState.ElementUnderMouse is PinInstance pin && HasControl){

					WireInstance.ConnectionInfo connectionInfo = new() { pin = pin };
					StartPlacingWire(connectionInfo);
					#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
					MobileUIControllerWrapper.ShowAddWireButtons(
						TryAddWirePoint,
						CancelPlacingItems
					);
					#endif
				}else if(InteractionState.ElementUnderMouse!= null && InteractionState.ElementUnderMouse.ToString() == "DLS.Game.InteractionState+UnspecifiedInteractableElement")
				{
					if(wireToEdit!=null)
						Main.Update();
				}
				// Tapping on wire
				else if (InteractionState.ElementUnderMouse is WireInstance wire && HasControl)
				{

					InteractionState.NotifyElementUnderMouse(wire);
					// Insert a point on the currently edited wire
					if (wire == wireToEdit)
					{
						(Vector2 insertionPoint, int segmentIndex) = WireLayoutHelper.GetClosestPointOnWire(wire, touchPosWorld);
						float distFromPointA  = (insertionPoint - wire.GetWirePoint(segmentIndex)).magnitude;
						float distFromPointB  = (insertionPoint - wire.GetWirePoint(segmentIndex+1)).magnitude;
						float dstFromExistingPoint = Mathf.Min(distFromPointA,distFromPointB);
						const float rBG = 0.25f;
						if (dstFromExistingPoint > rBG)
						{
							wire.InsertPoint(insertionPoint, segmentIndex);
							wireEditPointIndex = segmentIndex + 1;
						}else if (distFromPointA<distFromPointB){
							wireEditPointIndex = segmentIndex;
						}else{
							wireEditPointIndex = segmentIndex+1;
						}
					}
					// Start placing a new wire from this point on the selected wire
					else
					{
						WireInstance.ConnectionInfo connectionInfo = CreateWireToWireConnectionInfo(wire, wire.SourcePin);
						StartPlacingWire(connectionInfo);
						#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
						MobileUIControllerWrapper.ShowAddWireButtons(
							TryAddWirePoint,
							CancelPlacingItems
						);
						#endif
					}
				}
				// Tapped on selectable element: select it and prepare to start moving current selection
				else if (InteractionState.ElementUnderMouse is IMoveable element && Project.ActiveProject.CanEditViewedChip)
				{
					bool addToSelection = KeyboardShortcuts.MultiModeHeld;
					Select(element, addToSelection);

					StartMovingSelectedItems();
					// Only show placement buttons in drag and lock mode
					if (!UseDragAndDropMode)
					{
						#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
						MobileUIControllerWrapper.ShowPlacementButtons(
							FinishMovingElements,
							CancelMovingSelectedItems
						);
						#endif
					}
				}else if (InteractionState.ElementUnderMouse == null && !IsPlacingElementOrCreatingWire && !IsMovingSelection){
					// Tapped on free space
					if(MobileUIControllerWrapper.IsBoxSelectToolActive)
						IsCreatingSelectionBox = true;
					else if (SelectedElements.Count>0)
					{
						// In drag and drop mode, always clear selection when tapping empty space
						// In drag and lock mode, only clear if not currently moving
						if (UseDragAndDropMode || !IsMovingSelection)
							ClearSelection();
					}
					wireEditPointIndex = -1;
					isMovingWireEditPoint = false;
					//FinishEditingWires();
				}
				
				if (wireToEdit != null && wireEditPointIndex != -1)
				{
					isMovingWireEditPoint = true;
					wireEditPointSelectedIndex = wireEditPointIndex;
					wireEditPointOld = wireToEdit.GetWirePoint(wireEditPointIndex);
				}
				

			}
			if (wireToEdit != null)
			{
				DevSceneDrawer.DrawWireEditPoints(wireToEdit);
			}
		}

		void TryAddWirePoint(){
			WireToPlace.AddWirePoint(WireToPlace.GetWirePoint(WireToPlace.WirePointCount-1));
			#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
			MobileUIControllerWrapper.ShowAddWireButtons(
				TryAddWirePoint,
				CancelPlacingItems
			);
			#endif
		}

		void HandleLeftMouseDown()
		{
			SelectionBoxStartPos = InputHelper.MousePosWorld;
			straightLineMoveState = StraightLineMoveState.None;

			if (InteractionState.ElementUnderMouse == null) ExitWireEditMode();

			if (InteractionState.MouseIsOverUI) return;

			// Confirm placement of new item
			if (IsPlacingElementOrCreatingWire)
			{
				// Place wire
				if (IsCreatingWire) //
				{
					if (TryFinishPlacingWire())
					{
						CancelPlacingItems();
					}
					else if (CanAddWirePoint())
					{
						WireToPlace.AddWirePoint(InputHelper.MousePosWorld);
					}
				}
				// Place subchip / devpin
				else
				{
					FinishPlacingNewElements();
				}
			}
			else
			{
				// Mouse down on pin: start placing wire
				if (InteractionState.ElementUnderMouse is PinInstance pin && HasControl)
				{
					WireInstance.ConnectionInfo connectionInfo = new() { pin = pin };
					StartPlacingWire(connectionInfo);
				}
				// Mouse down on wire
				else if (InteractionState.ElementUnderMouse is WireInstance wire && HasControl)
				{
					// Insert a point on the currently edited wire
					if (wire == wireToEdit)
					{
						if (wireEditCanInsertPoint)
						{
							(Vector2 point, int segmentIndex) = WireLayoutHelper.GetClosestPointOnWire(wireToEdit, InputHelper.MousePosWorld);
							wireToEdit.InsertPoint(point, segmentIndex);
							wireEditPointIndex = segmentIndex + 1;
						}
					}
					// Start placing a new wire from this point on the selected wire
					else
					{
						WireInstance.ConnectionInfo connectionInfo = CreateWireToWireConnectionInfo(wire, wire.SourcePin);
						StartPlacingWire(connectionInfo);
					}
				}
				// Mouse down on selectable element: select it and prepare to start moving current selection
				else if (InteractionState.ElementUnderMouse is IMoveable element)
				{
					bool addToSelection = KeyboardShortcuts.MultiModeHeld;
					Select(element, addToSelection);
					StartMovingSelectedItems();
				}
				// Mouse down over nothing: clear selection
				else if (InteractionState.ElementUnderMouse == null && !IsPlacingElementOrCreatingWire)
				{
					if (!KeyboardShortcuts.MultiModeHeld) ClearSelection(); // don't clear if in 'multi-mode' (to allow box selecting multiple times)
					IsCreatingSelectionBox = true;
				}

				if (wireToEdit != null && wireEditPointIndex != -1)
				{
					isMovingWireEditPoint = true;
					wireEditPointSelectedIndex = wireEditPointIndex;
					wireEditPointOld = wireToEdit.GetWirePoint(wireEditPointIndex);
				}
			}
		}

		WireInstance.ConnectionInfo CreateWireToWireConnectionInfo(WireInstance wireToConnectTo, PinInstance pin)
		{
			Vector2 mousePos = InputHelper.MousePosWorld;
			if (project.ShouldSnapToGrid) mousePos = GridHelper.SnapToGrid(mousePos, true, true);

			// If connecting a new wire to an existing wire, the target connection point is end pos of new wire (this is mouse pos but with snapping options applied)
			// Otherwise if creating a new wire from an existing wire, connection point is at mouse pos.
			Vector2 targetPoint = WireToPlace?.GetWirePoint(WireToPlace.WirePointCount - 1) ?? mousePos;
			// Find where target connection point is closest to the target wire.
			(Vector2 bestPoint, int bestSegmentIndex) = WireLayoutHelper.GetClosestPointOnWire(wireToConnectTo, targetPoint);

			return new WireInstance.ConnectionInfo
			{
				pin = pin,
				connectedWire = wireToConnectTo,
				wireConnectionSegmentIndex = bestSegmentIndex,
				connectionPoint = bestPoint
			};
		}

		void StartPlacingWire(WireInstance.ConnectionInfo connectionInfo)
		{
			ExitWireEditMode();
			ClearSelection();
			int spawnOrder = project.ViewedChip.Wires.Count > 0 ? project.ViewedChip.Wires[^1].spawnOrder + 1 : 0;
			WireToPlace = new WireInstance(connectionInfo, spawnOrder);
		}

		void FinishMovingElements() => FinishMovingElements(true);

		void FinishMovingElements(bool clearSelection)
		{
			// -- If any elements are in invalid position, cancel the movement --
			bool hasMoved = false;

			foreach (IMoveable element in SelectedElements)
			{
				if (!element.IsValidMovePos)
				{
					CancelMovingSelectedItems();
					return;
				}

				hasMoved |= (element.MoveStartPosition != element.Position);
			}

			if (hasMoved) ActiveDevChip.UndoController.RecordMoveElements(SelectedElements);

			// -- Apply movement --
			IsMovingSelection = false;

			foreach (WireInstance wire in ActiveDevChip.Wires)
			{
				wire.ApplyMoveOffset();
			}
			#if UNITY_ANDROID || UNITY_IOS
			// In drag and drop mode, don't clear selection after placement
			// In drag and lock mode, clear selection as before
			if (clearSelection && !UseDragAndDropMode)
			{
				ClearSelection();
				#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
				MobileUIControllerWrapper.HidePlacementButtons();
				#endif
			}
			#endif
		}

		void FinishPlacingNewElements()
		{
			// ---- If any elements are in invalid position, don't allow the placement ----
			foreach (IMoveable element in SelectedElements)
			{
				if (!element.IsValidMovePos)
				{
					return;
				}
			}

			List<IMoveable> newlyPlacedElements = new(SelectedElements);

			// ---- Add newly placed elements to the chip (skip if already added, e.g. from group placement) ----
			foreach (IMoveable elementToPlace in SelectedElements)
			{
				if (ActiveDevChip.Elements.Contains(elementToPlace)) continue;
				if (elementToPlace is SubChipInstance subchip)
				{
					ActiveDevChip.AddNewSubChip(subchip, false);
				}
				else if (elementToPlace is DevPinInstance devPin) ActiveDevChip.AddNewDevPin(devPin, false);
			}

			foreach (WireInstance wire in DuplicatedWires)
			{
				ActiveDevChip.AddWire(wire, false);
				wire.ApplyMoveOffset();
			}

			ActiveDevChip.UndoController.RecordAddElements(SelectedElements, DuplicatedWires.Count > 0);
			DuplicatedWires.Clear();
			OnFinishedPlacingItems();


			if (KeyboardShortcuts.MultiModeHeld)
			{
				DuplicateElements(newlyPlacedElements);
			}

			#if UNITY_ANDROID || UNITY_IOS
				#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
				MobileUIControllerWrapper.HidePlacementButtons();
				#endif
			#endif
		}

		public void EnterWireEditMode(WireInstance wire)
		{
			#if UNITY_ANDROID || UNITY_IOS
			if (wireToEdit == wire) ExitWireEditMode();
			else {
				wireToEdit = wire;
				#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
				MobileUIControllerWrapper.ShowPlacementButtons(
					FinishEditingWires,
					DeleteCurrentWireToEditPoint
				);
				#endif
				//#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITORMobileUIControllerWrapper.ShowCancelButton(
					//DeleteCurrentWireToEditPoint
				//);#endif
			}
			#else
			if (wireToEdit == wire) ExitWireEditMode();
			else wireToEdit = wire;
			#endif
		}

		public void DeleteCurrentWireToEditPoint(){
			if(wireToEdit!=null && wireEditPointIndex != -1){
				wireToEdit.DeleteWirePoint(wireEditPointIndex);
				wireEditPointIndex = -1;
			}
		}

		void ExitWireEditMode()
		{
			if (wireToEdit != null && isMovingWireEditPoint)
			{
				wireToEdit.SetWirePoint(wireEditPointOld, wireEditPointSelectedIndex);
			}

			wireToEdit = null;
			isMovingWireEditPoint = false;
			wireEditPointIndex = -1;
			wireEditPointSelectedIndex = -1;
		}
		void FinishEditingWires()
		{
			wireToEdit = null;
			isMovingWireEditPoint = false;
			wireEditPointIndex = -1;
			wireEditPointSelectedIndex = -1;
			#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
			MobileUIControllerWrapper.HidePlacementButtons();
			#endif
		}

		void HandleLeftMouseUp()
		{
			// Place items that are being moved
			if (!IsPlacingElementOrCreatingWire)
			{
				if (IsMovingSelection)
				{
					FinishMovingElements();
				}
			}

			// Select all selectable elements inside selection box
			if (IsCreatingSelectionBox)
			{
				if (!KeyboardShortcuts.MultiModeHeld) ClearSelection();
				IsCreatingSelectionBox = false;

				float selectionBoxArea = Mathf.Abs(SelectionBoxSize.x * SelectionBoxSize.y);
				if (selectionBoxArea > 0.000001f)
				{
					foreach (IMoveable element in ActiveDevChip.Elements)
					{
						if (element.ShouldBeIncludedInSelectionBox(SelectionBoxCentre, SelectionBoxSize))
						{
							Select(element, true);
						}
					}
					// Expand selection to include full groups: if any group member was selected, add the rest
					ExpandSelectionToIncludeFullGroups();
				}
			}


			if (IsCreatingWire)
			{
				if (TryFinishPlacingWire())
				{
					CancelPlacingItems();
				}
			}

			if (wireToEdit != null)
			{
				wireEditPointSelectedIndex = -1;
				isMovingWireEditPoint = false;
			}
		}

		public void MoveSelectionAfterDuplication(){
			if (SelectedElements.Count == 0) return;

			//moveElementMouseStartPos = touchPosForDuplicationOnMobile;
			Vector2 offset = new Vector2(-0.5f, -0.5f); // Small right and down offset (tweak as needed)
			//Vector2 offset = Vector2.zero;

			foreach (IMoveable element in SelectedElements)
			{
				element.Position += offset;
				element.MoveStartPosition += offset;
			}	
			moveElementMouseStartPos += offset;
		}


		void UpdatePositionsToMouse()
		{
			Vector2 mousePos = InputHelper.MousePosWorld;
			bool snapToGrid = project.ShouldSnapToGrid;

			if (IsCreatingWire)
			{
				WireToPlace.SetLastWirePoint(mousePos);
			}
			else if (IsMovingSelection || isPlacingNewElements)
			{
				Vector2 moveOffset = mousePos - moveElementMouseStartPos;

				for (int i = 0; i < SelectedElements.Count; i++)
				{
					IMoveable element = SelectedElements[i];
					bool isBusTerminus = element is SubChipInstance s && ChipTypeHelper.IsBusTerminusType(s.ChipType);
					Vector2 multiElementOffset = isBusTerminus ? Vector2.zero : Vector2.down * (itemPlacementCurrVerticalSpacing * i);

					Vector2 totalOffset = moveOffset + multiElementOffset;

					Vector2 targetPos = element.MoveStartPosition + totalOffset;

					if (snapToGrid)
					{
						if (i == 0)
						{
							targetPos = GridHelper.SnapMovingElementToGrid(element, totalOffset, false, true);
						}
						// Snap additional selected elements relative to the first one. (Snapping each element independently results in a 'jiggling' effect)
						else
						{
							// Get snap points prior to movement
							IMoveable prevElement = SelectedElements[i - 1];
							Vector2 snapPointStartA = prevElement.MoveStartPosition + (prevElement.SnapPoint - prevElement.Position);
							Vector2 snapPointStartB = element.MoveStartPosition + (element.SnapPoint - element.Position);
							// Base curr element's snap pos on prev element, adding the (snapped) difference between their initial snap points
							Vector2 placementManualOffset = isBusTerminus ? Vector2.zero : Vector2.down * itemPlacementCurrVerticalSpacing;

							Vector2 snappedOffset = GridHelper.SnapToGrid(snapPointStartB - snapPointStartA + placementManualOffset, false, true);
							Vector2 elementSnapPointOffset = element.SnapPoint - element.Position;
							targetPos = prevElement.SnapPoint + snappedOffset - elementSnapPointOffset;
						}
					}

					// When using shift to duplicate new element, don't use straight line mode unless pressed again
					if (isPlacingNewElements && !KeyboardShortcuts.MultiModeHeld) hasExittedMultiModeSincePlacementStart = true;

					if (KeyboardShortcuts.StraightLineModeHeld && element.HasReferencePointForStraightLineMovement && (!isPlacingNewElements || hasExittedMultiModeSincePlacementStart))
					{
						Vector2 offset = targetPos - element.StraightLineReferencePoint;
						float ox = Mathf.Abs(offset.x);
						float oy = Mathf.Abs(offset.y);
						bool canChangeState = straightLineMoveState == StraightLineMoveState.None || isPlacingNewElements;
						if (Mathf.Max(ox, oy) > 0.035f && canChangeState)
						{
							straightLineMoveState = ox > oy ? StraightLineMoveState.Horizontal : StraightLineMoveState.Vertical;
						}

						if (straightLineMoveState == StraightLineMoveState.Horizontal) offset.y = 0;
						else if (straightLineMoveState == StraightLineMoveState.Vertical) offset.x = 0;
						targetPos = element.StraightLineReferencePoint + offset;
					}

					element.Position = targetPos;

					// Test if is legal position
					bool legal = true;
					foreach (IMoveable obstacle in Obstacles)
					{
						if (element.BoundingBox.Overlaps(obstacle.BoundingBox))
						{
							legal = false;
							break;
						}
					}

					element.IsValidMovePos = legal;
				}


				// Update wires when their parents are moved
				if (isPlacingNewElements)
				{
					foreach (WireInstance wire in DuplicatedWires)
					{
						Vector2 delA = wire.SourcePin.parent.Position - wire.SourcePin.parent.MoveStartPosition;
						Vector2 delB = wire.TargetPin.parent.Position - wire.TargetPin.parent.MoveStartPosition;
						// Parent chips may have been moved by slightly different amounts if snapping is enabled, so just take average
						wire.MoveOffset = (delA + delB) / 2;
					}
				}
				else
				{
					foreach (WireInstance wire in ActiveDevChip.Wires)
					{
						// If both ends of the wire are being moved, then move the entire wire
						if (IsSelected(wire.SourcePin.parent) && IsSelected(wire.TargetPin.parent))
						{
							Vector2 delA = wire.SourcePin.parent.Position - wire.SourcePin.parent.MoveStartPosition;
							Vector2 delB = wire.TargetPin.parent.Position - wire.TargetPin.parent.MoveStartPosition;
							// Parent chips may have been moved by slightly different amounts if snapping is enabled, so just take average
							wire.MoveOffset = (delA + delB) / 2;
						}
					}
				}
			} 
			else if (isMovingWireEditPoint) 
			{
				wireToEdit.SetWirePointWithSnapping(mousePos, wireEditPointSelectedIndex, wireEditPointOld);
			}
			if (wireToEdit != null && wireEditPointIndex != -1)
			{
				isMovingWireEditPoint = true;
				wireEditPointSelectedIndex = wireEditPointIndex;
				wireEditPointOld = wireToEdit.GetWirePoint(wireEditPointIndex);
			}
		}


		void StartMovingSelectedItems(bool isDuplicationOnMobileCall = false)
		{
			IsMovingSelection = true;
			#if UNITY_ANDROID || UNITY_IOS
			// Only show placement buttons in drag and lock mode
			if (!UseDragAndDropMode)
			{
				#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
				MobileUIControllerWrapper.ShowPlacementButtons(
					FinishPlacingNewElements,
					CancelPlacingItems
				);
				#endif
			}
			if(!isDuplicationOnMobileCall){
				moveElementMouseStartPos = InputHelper.MousePosWorld;
			}
			#else
			// Desktop platforms - always set the mouse start position
			moveElementMouseStartPos = InputHelper.MousePosWorld;
			#endif

			foreach (IMoveable moveableElement in SelectedElements)
			{
				moveableElement.MoveStartPosition = moveableElement.Position;
				moveableElement.StraightLineReferencePoint = moveableElement.Position;
				moveableElement.HasReferencePointForStraightLineMovement = true;
			}

			Obstacles = ActiveDevChip.Elements.Where(e => !e.IsSelected).ToArray();
		}

		bool TryFinishPlacingWire()
		{
			if (InteractionState.ElementUnderMouse is PinInstance pin)
			{
				if (CanCompleteWireConnection(pin))
				{
					WireInstance.ConnectionInfo info = new() { pin = pin };
					CompleteConnection(info);
					return true;
				}
			}
			else if (InteractionState.ElementUnderMouse is WireInstance connectionWire)
			{
				if (CanCompleteWireConnection(connectionWire, out PinInstance endPin))
				{
					WireInstance.ConnectionInfo info = CreateWireToWireConnectionInfo(connectionWire, endPin);
					CompleteConnection(info);
					return true;
				}
			}

			return false;

			void CompleteConnection(WireInstance.ConnectionInfo info)
			{
				WireToPlace.FinishPlacingWire(info);
				ActiveDevChip.AddWire(WireToPlace, false);
				ActiveDevChip.UndoController.RecordAddWire(WireToPlace);
			}
		}

		bool CanAddWirePoint()
		{
			// Can add wire point if mouse is not over anything else
			if (InteractionState.ElementUnderMouse is null) return true;

			// Can add wire point if mouse is over an existing wire, but that wire comes from same pin as current wire (might want to trace over existing wire for example)
			if (InteractionState.ElementUnderMouse is WireInstance wire)
			{
				if (wire.SourcePin == WireToPlace.FirstPin || wire.TargetPin == WireToPlace.FirstPin) return true;
			}

			return false;
		}

		public void StartPlacing(string name)
		{
			StartPlacing(project.chipLibrary.GetChipDescription(name));
		}

		public void StartPlacing(ChipDescription chipDescription)
		{
			StartPlacing(chipDescription, InputHelper.MousePosWorld, false);
		}

	public void StartPlacingGroup(GroupDescription groupDesc, Vector2 position)
	{
		bool hasSubChips = groupDesc.SubChips != null && groupDesc.SubChips.Length > 0;
		bool hasDevPins = (groupDesc.InputPins != null && groupDesc.InputPins.Length > 0) || (groupDesc.OutputPins != null && groupDesc.OutputPins.Length > 0);
		if (!hasSubChips && !hasDevPins) return;
		CancelEverything();
		var lm = LevelsIntegration.LevelManager.Instance;
		if (lm != null && lm.IsActive && hasDevPins)
		{
			ShowInputOutputDisabledMessage();
			return;
		}
		if (lm != null && lm.IsActive && hasSubChips)
		{
			foreach (var sc in groupDesc.SubChips)
			{
				if (project.chipLibrary.TryGetChipDescription(sc.Name, out var cd) &&
				    project.chipLibrary.ChipDescriptionContainsDisallowedSubchipsForLevel(cd))
				{
					ShowNestedDisallowedMessage();
					return;
				}
			}
		}
		var devChip = ActiveDevChip;
		var library = project.chipLibrary;
		var oldToNewID = new Dictionary<int, int>();
		var newSubChips = new List<SubChipInstance>();
		var newDevPins = new List<DevPinInstance>();

		// Compute anchor (center of bounding box) from subchips and dev pins
		Vector2 min = new(float.MaxValue, float.MaxValue), max = new(float.MinValue, float.MinValue);
		if (hasSubChips)
			foreach (var sc in groupDesc.SubChips)
			{
				Vector2 sz = library.TryGetChipDescription(sc.Name, out var cd) ? cd.Size : new Vector2(2f, 1f);
				min = Vector2.Min(min, sc.Position);
				max = Vector2.Max(max, sc.Position + sz);
			}
		if (groupDesc.InputPins != null)
			foreach (var p in groupDesc.InputPins)
			{
				min = Vector2.Min(min, p.Position);
				max = Vector2.Max(max, p.Position);
			}
		if (groupDesc.OutputPins != null)
			foreach (var p in groupDesc.OutputPins)
			{
				min = Vector2.Min(min, p.Position);
				max = Vector2.Max(max, p.Position);
			}
		if (min.x == float.MaxValue) min = max = Vector2.zero;
		Vector2 anchor = (min + max) / 2;
		Vector2 offset = position - anchor;

		// Create subchips with new IDs
		if (hasSubChips)
			foreach (var subDesc in groupDesc.SubChips)
			{
				if (!library.TryGetChipDescription(subDesc.Name, out ChipDescription fullDesc))
				{
					// Verilog title label: skip if LABEL not in library (e.g. level restricts to NAND-only)
					if (subDesc.Name?.Equals("LABEL", StringComparison.OrdinalIgnoreCase) == true)
					{
						Debug.Log($"[Verilog] Skipping title label (LABEL not in library) for group '{groupDesc.Name}'");
						continue;
					}
					bool hasLabel = library.allChips.Any(c => c.Name?.Equals("LABEL", StringComparison.OrdinalIgnoreCase) == true);
					Debug.LogWarning($"[Verilog] Chip '{subDesc.Name}' not found. HasLabel={hasLabel}, ChipCount={library.allChips.Count}");
					SimpleMessagePopup.Open($"Chip \"{subDesc.Name}\" not found in library");
					return;
				}
				int newID = IDGenerator.GenerateNewElementID(devChip);
				oldToNewID[subDesc.ID] = newID;
				Vector2 finalPos = subDesc.Position + offset;
				var scDesc = new SubChipDescription(subDesc.Name, newID, subDesc.Label, finalPos, subDesc.OutputPinColourInfo, subDesc.InternalData, subDesc.LabelOffset, subDesc.Rotation);
				var subChip = new SubChipInstance(fullDesc, scDesc);
				devChip.AddNewSubChip(subChip, false);
				newSubChips.Add(subChip);
				if (fullDesc.ChipType == ChipType.Label)
					Debug.Log($"[Verilog] Placed Label subchip '{subDesc.Label}' at ({finalPos.x}, {finalPos.y})");
			}

		// Create dev pins with new IDs
		if (groupDesc.InputPins != null)
			foreach (var pd in groupDesc.InputPins)
			{
				int newID = IDGenerator.GenerateNewElementID(devChip);
				oldToNewID[pd.ID] = newID;
				var pinDesc = new PinDescription(pd.Name, newID, pd.Position + offset, pd.BitCount, pd.Colour, pd.ValueDisplayMode, pd.LocalOffset, pd.face, pd.CustomColourPacked);
				var devPin = new DevPinInstance(pinDesc, true);
				devChip.AddNewDevPin(devPin, false);
				newDevPins.Add(devPin);
			}
		if (groupDesc.OutputPins != null)
			foreach (var pd in groupDesc.OutputPins)
			{
				int newID = IDGenerator.GenerateNewElementID(devChip);
				oldToNewID[pd.ID] = newID;
				var pinDesc = new PinDescription(pd.Name, newID, pd.Position + offset, pd.BitCount, pd.Colour, pd.ValueDisplayMode, pd.LocalOffset, pd.face, pd.CustomColourPacked);
				var devPin = new DevPinInstance(pinDesc, false);
				devChip.AddNewDevPin(devPin, false);
				newDevPins.Add(devPin);
			}

		// Create wires: build pin map keyed by ORIGINAL IDs from groupDesc (same as wire descriptions use)
		DuplicatedWires.Clear();
		var pinMap = new Dictionary<(int ownerId, int pinId), PinInstance>();
		int inputCount = groupDesc.InputPins?.Length ?? 0;

		if (groupDesc.SubChips != null)
			for (int i = 0; i < newSubChips.Count && i < groupDesc.SubChips.Length; i++)
			{
				int ownerId = groupDesc.SubChips[i].ID;
				var pins = newSubChips[i].AllPins;
				for (int pi = 0; pi < pins.Length; pi++)
				{
					var pin = pins[pi];
					// Use both index and PinID: VerilogImporter uses indices 0,1,2; saved groups use PinID
					pinMap[(ownerId, pi)] = pin;
					if (pin.Address.PinID != pi)
						pinMap[(ownerId, pin.Address.PinID)] = pin;
				}
			}
		if (groupDesc.InputPins != null)
			for (int i = 0; i < groupDesc.InputPins.Length && i < newDevPins.Count; i++)
				pinMap[(groupDesc.InputPins[i].ID, 0)] = newDevPins[i].Pin;
		if (groupDesc.OutputPins != null)
			for (int j = 0; j < groupDesc.OutputPins.Length && inputCount + j < newDevPins.Count; j++)
				pinMap[(groupDesc.OutputPins[j].ID, 0)] = newDevPins[inputCount + j].Pin;

		void AddWire(PinInstance src, PinInstance tgt)
		{
			Vector2 srcPos = src.GetWorldPos();
			Vector2 tgtPos = tgt.GetWorldPos();
			var srcConn = new WireInstance.ConnectionInfo { pin = src, connectedWire = null, connectionPoint = srcPos, wireConnectionSegmentIndex = -1 };
			var tgtConn = new WireInstance.ConnectionInfo { pin = tgt, connectedWire = null, connectionPoint = tgtPos, wireConnectionSegmentIndex = -1 };
			var wire = new WireInstance(srcConn, tgtConn, new Vector2[] { srcPos, tgtPos }, devChip.Wires.Count + DuplicatedWires.Count);
			DuplicatedWires.Add(wire);
		}

		if (groupDesc.Wires != null && groupDesc.Wires.Length > 0)
		{
			foreach (var wd in groupDesc.Wires)
			{
				if (!pinMap.TryGetValue((wd.SourcePinAddress.PinOwnerID, wd.SourcePinAddress.PinID), out PinInstance srcPin) ||
				    !pinMap.TryGetValue((wd.TargetPinAddress.PinOwnerID, wd.TargetPinAddress.PinID), out PinInstance tgtPin))
				{
					Debug.LogWarning($"[Verilog] Wire lookup failed: ({wd.SourcePinAddress.PinOwnerID}, {wd.SourcePinAddress.PinID}) -> ({wd.TargetPinAddress.PinOwnerID}, {wd.TargetPinAddress.PinID})");
					continue;
				}
				AddWire(srcPin, tgtPin);
			}
		}

		// Assign GroupId and enter placement
		int groupId = IDGenerator.GenerateNewGroupId(devChip);
		foreach (var s in newSubChips) s.GroupId = groupId;
		foreach (var d in newDevPins) d.GroupId = groupId;
		ClearSelection();
		foreach (var s in newSubChips) Select(s, true);
		foreach (var d in newDevPins) Select(d, true);
		isPlacingNewElements = true;
		newElementsAreDuplicatedElements = true;
		hasExittedMultiModeSincePlacementStart = false;
		moveElementMouseStartPos = position;
		foreach (var e in SelectedElements)
		{
			e.MoveStartPosition = e.Position;
			e.StraightLineReferencePoint = e.Position;
			e.HasReferencePointForStraightLineMovement = true;
		}
		Obstacles = devChip.Elements.Where(x => !SelectedElements.Contains(x)).ToArray();
		IsMovingSelection = true;
		#if UNITY_ANDROID || UNITY_IOS
		MobileUIControllerWrapper.ShowPlacementButtons(FinishPlacingNewElements, CancelPlacingItems);
		#endif
	}

	public IMoveable StartPlacing(ChipDescription chipDescription, Vector2 position, bool isDuplicating)
	{
		// Check if trying to add Input/Output pins in a level
		if (ShouldHideChipInLevel(chipDescription.ChipType))
		{
			ShowInputOutputDisabledMessage();
			return null;
		}
		
		// Check if trying to add special chips in a level
		if (IsSpecialChipDisabledInLevel(chipDescription.ChipType))
		{
			ShowSpecialChipDisabledMessage();
			return null;
		}

		// Check if custom chip contains disallowed subchips (e.g. ROM inside a custom chip) – only when IN a level
		var lm = DLS.Game.LevelsIntegration.LevelManager.Instance;
		if (lm != null && lm.IsActive && project.chipLibrary.ChipDescriptionContainsDisallowedSubchipsForLevel(chipDescription))
		{
			ShowNestedDisallowedMessage();
			return null;
		}
		
		#if UNITY_ANDROID || UNITY_IOS
		// Only show placement buttons in drag and lock mode
		if (!UseDragAndDropMode)
		{
			#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
			MobileUIControllerWrapper.ShowPlacementButtons(
				FinishPlacingNewElements,
				CancelPlacingItems
			);
			#endif
		}
		#endif
		IMoveable elementToPlace = CreateElementFromChipDescription(chipDescription);
		StartPlacing(elementToPlace, position, isDuplicating);
		return elementToPlace;
	}

		void StartPlacing(IMoveable elementToPlace, Vector2 position, bool isDuplicating)
		{
			const float busPairSpacing = DrawSettings.GridSize * 8;
			newElementsAreDuplicatedElements = isDuplicating;

			if (!isPlacingNewElements)
			{
				CancelEverything();
				isPlacingNewElements = true;
				hasExittedMultiModeSincePlacementStart = false;
				#if UNITY_ANDROID || UNITY_IOS
					StartMovingSelectedItems(isDuplicating);
				#else
					StartMovingSelectedItems();
				#endif
			}

			ChipType chipType;
			if (elementToPlace is DevPinInstance devPinInstance) chipType = devPinInstance.IsInputPin ? ChipType.In_Pin : ChipType.Out_Pin;
			else chipType = ((SubChipInstance)elementToPlace).ChipType;

			// Place bus terminus to right of bus origin
			if (ChipTypeHelper.IsBusTerminusType(chipType) && !isDuplicating)
			{
				elementToPlace.MoveStartPosition = SelectedElements[^1].MoveStartPosition + Vector2.right * busPairSpacing;
				elementToPlace.HasReferencePointForStraightLineMovement = false;
			}
			// If placing multiple elements simultaneously, place the new element below the previous one
			// (unless duplicating elements, in which case their relative positions should be preserved)
			else if (SelectedElements.Count > 0 && !isDuplicating)
			{
				float spacing = (elementToPlace.SelectionBoundingBox.Size.y + SelectedElements[^1].SelectionBoundingBox.Size.y) / 2;

				Vector2 prevElementPos = SelectedElements[^1].MoveStartPosition;
				// If prev element was bus terminus, we want the midpoint between the bus origin and terminus pair
				if (SelectedElements[^1] is SubChipInstance s && ChipTypeHelper.IsBusTerminusType(s.ChipType))
				{
					prevElementPos = (prevElementPos + SelectedElements[^2].MoveStartPosition) / 2;
				}

				elementToPlace.MoveStartPosition = prevElementPos + Vector2.down * spacing;
				elementToPlace.HasReferencePointForStraightLineMovement = false;
			}
			else
			{
				#if UNITY_ANDROID || UNITY_IOS
				#else
					moveElementMouseStartPos = InputHelper.MousePosWorld;
				#endif

				elementToPlace.MoveStartPosition = position;
				elementToPlace.StraightLineReferencePoint = position;
				elementToPlace.HasReferencePointForStraightLineMovement = isDuplicating;
			}

			Select(elementToPlace);

			// When placing bus, auto-place the corresponding bus terminus (unless duplicating an existing bus)
			if (ChipTypeHelper.IsBusOriginType(chipType) && !isDuplicating)
			{
				elementToPlace.MoveStartPosition -= Vector2.right * busPairSpacing / 2;

				ChipDescription terminusDescription = Project.ActiveProject.chipLibrary.GetTerminusDescription(((SubChipInstance)elementToPlace).OutputPins[0].bitCount);
				SubChipInstance terminus = (SubChipInstance)StartPlacing(terminusDescription, position, false);

				SubChipInstance busOrigin = (SubChipInstance)elementToPlace;
				busOrigin.SetLinkedBusPair(terminus);
				terminus.SetLinkedBusPair(busOrigin);
			}
		}

		IMoveable CreateElementFromChipDescription(ChipDescription chipDescription)
		{
			IMoveable elementToPlace;
			int instanceID = IDGenerator.GenerateNewElementID(ActiveDevChip);

			// Input/output dev pins are represented as chips for convenience
			(bool isInput, bool isOutput, PinBitCount numBits) ioPinInfo = ChipTypeHelper.IsInputOrOutputPin(chipDescription);

			if (ioPinInfo.isInput || ioPinInfo.isOutput) // Dev pin
			{
				PinDescription pinDesc = ioPinInfo.isInput ? chipDescription.OutputPins[0] : chipDescription.InputPins[0];
				pinDesc.ID = instanceID;
                elementToPlace = new DevPinInstance(pinDesc, ioPinInfo.isInput);
				
			}

			else // SubChip
			{
				SubChipDescription subChipDesc = DescriptionCreator.CreateBuiltinSubChipDescriptionForPlacement(chipDescription.ChipType, chipDescription.Name, instanceID, Vector2.zero, ActiveDevChip);
				elementToPlace = new SubChipInstance(chipDescription, subChipDesc);
			}

			return elementToPlace;
		}

		IMoveable CreateElementFromDuplicationSource(IMoveable duplicationSource)
		{
			IMoveable element;
			int instanceID = IDGenerator.GenerateNewElementID(ActiveDevChip);

			if (duplicationSource is DevPinInstance devPinSrc)
			{
				PinDescription pinDesc = DescriptionCreator.CreatePinDescription(devPinSrc);
				pinDesc.ID = instanceID;
				element = new DevPinInstance(pinDesc, devPinSrc.IsInputPin);
			}
			else
			{
				SubChipInstance srcSub = (SubChipInstance)duplicationSource;
				SubChipDescription subChipDesc = DescriptionCreator.CreateSubChipDescription(srcSub);
				subChipDesc.ID = instanceID;
				// For Transmitter: assign first free frequency to avoid conflict
				if (srcSub.ChipType == ChipType.Transmitter && subChipDesc.InternalData != null && subChipDesc.InternalData.Length > 0)
				{
					uint firstFree = DescriptionCreator.GetFirstFreeTransmitterFrequency(ActiveDevChip);
					subChipDesc.InternalData[0] = firstFree;
				}
				element = new SubChipInstance(srcSub.Description, subChipDesc);
			}

			return element;
		}

		public void CancelEverything()
		{
			CancelMovingSelectedItems();
			CancelPlacingItems();
			ClearSelection();
			IsCreatingSelectionBox = false;
			isPlacingNewElements = false;
			ExitWireEditMode();
			
			// Disable eraser mode when canceling
			EraserModeController.DisableEraserMode();
			
			// Sync MobileUIController state
			#if UNITY_ANDROID || UNITY_IOS
			if (MobileUIController.Instance != null)
			{
				MobileUIController.Instance.isEraserModeActive = false;
			}
			#endif
		}

		void ClearSelection()
		{
			foreach (IMoveable element in SelectedElements)
			{
				element.IsSelected = false;
				element.IsValidMovePos = true;
			}

			SelectedElements.Clear();
		}

		void CancelMovingSelectedItems()
		{
			if (IsMovingSelection && SelectedElements.Count > 0)
			{
				foreach (IMoveable moveableElement in SelectedElements)
				{
					moveableElement.Position = moveableElement.MoveStartPosition;
				}

				foreach (WireInstance wire in ActiveDevChip.Wires)
				{
					wire.MoveOffset = Vector2.zero;
				}
			}

			IsMovingSelection = false;

			#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR

			MobileUIControllerWrapper.HidePlacementButtons();

			#endif
		}

		/// <summary>Commit the current drag (bake wire offsets) and switch to two-finger rotate. No snap-back - chips stay where they are.</summary>
		void CommitCurrentMoveForTwoFingerTransition()
		{
			if (!IsMovingSelection || SelectedElements.Count == 0) return;

			bool hasMoved = SelectedElements.Any(e => e.MoveStartPosition != e.Position);
			if (hasMoved)
				ActiveDevChip.UndoController.RecordMoveElements(SelectedElements);

			foreach (WireInstance wire in ActiveDevChip.Wires)
				wire.ApplyMoveOffset();

			foreach (IMoveable moveableElement in SelectedElements)
				moveableElement.MoveStartPosition = moveableElement.Position;

			IsMovingSelection = false;
			#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
			MobileUIControllerWrapper.HidePlacementButtons();
			#endif
		}

		void OnFinishedPlacingItems() => OnFinishedOrCancelledPlacingItems();

		void CancelMovingSelectedItemsOnMobile()
		{
			// If canceling placement of bus terminus, destroy the linked bus origin 
			if (isPlacingNewElements)
			{
				DuplicatedWires.Clear();

				foreach (IMoveable element in SelectedElements)
				{
					if (element is SubChipInstance subChipInstance && subChipInstance.IsBus)
					{
						ActiveDevChip.TryDeleteSubChipByID(subChipInstance.LinkedBusPairID);
					}
				}

				// If canceling group placement, remove elements that were already added to the chip
				foreach (IMoveable element in SelectedElements.ToList())
				{
					if (ActiveDevChip.Elements.Contains(element))
					{
						if (element is SubChipInstance sc) ActiveDevChip.DeleteSubChip(sc);
						else if (element is DevPinInstance dp) ActiveDevChip.DeleteDevPin(dp);
					}
				}
			}

			OnFinishedOrCancelledPlacingItems();
		}
		void CancelPlacingItems()
		{
			// If canceling placement of bus terminus, destroy the linked bus origin 
			if (isPlacingNewElements)
			{
				DuplicatedWires.Clear();

				foreach (IMoveable element in SelectedElements)
				{
					if (element is SubChipInstance subChipInstance && subChipInstance.IsBus)
					{
						ActiveDevChip.TryDeleteSubChipByID(subChipInstance.LinkedBusPairID);
					}
				}

				// If canceling group placement, remove elements that were already added to the chip
				foreach (IMoveable element in SelectedElements.ToList())
				{
					if (ActiveDevChip.Elements.Contains(element))
					{
						if (element is SubChipInstance sc) ActiveDevChip.DeleteSubChip(sc);
						else if (element is DevPinInstance dp) ActiveDevChip.DeleteDevPin(dp);
					}
				}
			}

			OnFinishedOrCancelledPlacingItems();

			#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR

			MobileUIControllerWrapper.HidePlacementButtons();

			#endif
		}


		void OnFinishedOrCancelledPlacingItems()
		{
			// In drag and drop mode, don't clear selection after placing new elements
			// In drag and lock mode, clear selection as before
			if (!UseDragAndDropMode)
			{
				ClearSelection();
			}
			IsMovingSelection = false;

			isPlacingNewElements = false;
			newElementsAreDuplicatedElements = false;
			WireToPlace = null;
			itemPlacementCurrVerticalSpacing = 0;
		}

		enum StraightLineMoveState
		{
			None,
			Horizontal,
			Vertical
		}
	}
}