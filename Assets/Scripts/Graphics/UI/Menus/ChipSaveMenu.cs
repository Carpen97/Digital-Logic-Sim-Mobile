using System;
using System.Collections.Generic;
using DLS.Description;
using DLS.Game;
using DLS.Game.LevelsIntegration;
using DLS.Levels;
using DLS.SaveSystem;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;
using Random = System.Random;

namespace DLS.Graphics
{
	public static class ChipSaveMenu
	{
		public const string MaxLengthChipName = "MY VERY LONG CHIP NAME";

		const int CancelButtonIndex = 0;
		const int CustomizeButtonIndex = 1;
		const int SaveButtonIndex = 2;
		const int SaveAsButtonIndex = 3;
		static readonly UIHandle ID_ChipNameField = new("SaveMenu_ChipNameField");
		static readonly Func<string, bool> chipNameValidator = ValidateChipNameInput;
		static readonly Random rng = new();

		public static SubChipInstance ActiveCustomizeChip;
		static SubChipInstance CustomizeStateBeforeEnteringCustomizeMenu;
		
		// Track where we were opened from to return there on close
		static UIDrawer.MenuType returnToMenuOnClose = UIDrawer.MenuType.None;

		static readonly string[] CancelSaveButtonNames =
		{
			"CANCEL", "CUSTOMIZE", "SAVE"
		};

		static readonly string[] CancelRenameSaveButtonNames =
		{
			"CANCEL", "CUSTOMIZE", "RENAME", "SAVE AS"
		};

		static readonly bool[] ButtonGroupInteractStates = { true, true, true, true };
		public static ChipDescription ActiveCustomizeDescription => ActiveCustomizeChip.Description;

		public static void OnMenuOpened()
		{
			ActiveCustomizeChip ??= CreateCustomizationState();
			InitUIFromDescription(ActiveCustomizeChip.Description);
		}
		
		/// <summary>
		/// Sets the menu to return to when this menu is closed.
		/// Call this before opening the ChipSave menu.
		/// </summary>
		public static void SetReturnMenu(UIDrawer.MenuType returnMenu)
		{
			returnToMenuOnClose = returnMenu;
		}

		public static (Vector2 size, float pad) GetTextInputSize()
		{
			const float textPad = 2;
			InputFieldTheme inputTheme = DrawSettings.ActiveUITheme.ChipNameInputField;
			Vector2 inputFieldSize = Seb.Vis.UI.UI.CalculateTextSize(MaxLengthChipName, inputTheme.fontSize, inputTheme.font) + new Vector2(textPad * 2, 3);
			return (inputFieldSize, textPad);
		}

		public static void DrawMenu()
		{
			MenuHelper.DrawBackgroundOverlay();

			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			InputFieldTheme inputTheme = DrawSettings.ActiveUITheme.ChipNameInputField;
			InputFieldState inputFieldState;

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();

				// -- Chip name input field --
				(Vector2 inputFieldSize, float inputFieldTextPad) = GetTextInputSize();
				inputFieldState = Seb.Vis.UI.UI.InputField(ID_ChipNameField, inputTheme, new Vector2(50, 33), inputFieldSize, "Name", Anchor.Centre, inputFieldTextPad, chipNameValidator, true);

				Vector2 buttonTopLeft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * (DrawSettings.DefaultButtonSpacing * 2);
				bool renaming = Project.ActiveProject.ChipHasBeenSavedBefore && !ChipDescription.NameMatch(inputFieldState.text, Project.ActiveProject.ViewedChip.LastSavedDescription.Name);

				bool saveButtonEnabled = IsValidSaveName(inputFieldState.text);
				ButtonGroupInteractStates[SaveButtonIndex] = saveButtonEnabled;
				ButtonGroupInteractStates[SaveAsButtonIndex] = saveButtonEnabled;
				string[] buttonGroupNames = renaming ? CancelRenameSaveButtonNames : CancelSaveButtonNames;
				int buttonIndex = Seb.Vis.UI.UI.HorizontalButtonGroup(buttonGroupNames, ButtonGroupInteractStates, theme.ButtonTheme, buttonTopLeft, Seb.Vis.UI.UI.PrevBounds.Width, DrawSettings.DefaultButtonSpacing, 0, Anchor.TopLeft);
				bool confirmShortcut = !renaming && KeyboardShortcuts.ConfirmShortcutTriggered;

				if (buttonIndex == CancelButtonIndex || KeyboardShortcuts.CancelShortcutTriggered)
				{
					Cancel();
				}
				else if (buttonIndex == CustomizeButtonIndex)
				{
					OpenCustomizationMenu();
				}
				else if (buttonIndex == SaveButtonIndex || confirmShortcut)
				{
					Save(renaming ? Project.SaveMode.Rename : Project.SaveMode.Normal);
				}
				else if (buttonIndex == SaveAsButtonIndex)
				{
					Save(Project.SaveMode.SaveAs);
				}

				Bounds2D uiBounds = Seb.Vis.UI.UI.GetCurrentBoundsScope();
				MenuHelper.DrawReservedMenuPanel(panelID, uiBounds);

				// Update customization state
				if (ActiveCustomizeChip != null)
				{
					string newName = inputFieldState.text;
					if (ActiveCustomizeDescription.Name != newName)
					{
						ActiveCustomizeDescription.Name = newName;
						Vector2 minChipSize = SubChipInstance.CalculateMinChipSize(ActiveCustomizeDescription.InputPins, ActiveCustomizeDescription.OutputPins, newName);
						Vector2 chipSizeNew = Vector2.Max(minChipSize, ActiveCustomizeDescription.Size);
						ActiveCustomizeDescription.Size = chipSizeNew;
					}
				}
			}
		}

        // Create a subchip instance based on the current dev chip (we need a subchip instance to be able to draw a preview of the chip in the customization menu)
        // The description on this subchip holds potential customizations, such as name changes, resizing, colour etc.
		// This will load custom pin layouts if available to support editting existing chips. if no custom layout will use default behaviours.
        static SubChipInstance CreateCustomizationState()
        {
            DevChipInstance viewedChip = Project.ActiveProject.ViewedChip;
            ChipDescription desc = DescriptionCreator.CreateChipDescription(viewedChip);

            // Run chip type detection and auto-complete name if appropriate
            RunChipTypeDetection(desc);

            desc.HasCustomLayout = viewedChip.HasCustomLayout;

            // Copy layout if it exists
            if (desc.HasCustomLayout && viewedChip.LastSavedDescription != null)
            {
                var savedDesc = viewedChip.LastSavedDescription;

                for (int i = 0; i < desc.InputPins.Length && i < savedDesc.InputPins.Length; i++)
                {
                    desc.InputPins[i].face = savedDesc.InputPins[i].face;
                    desc.InputPins[i].LocalOffset = savedDesc.InputPins[i].LocalOffset;
                }

                for (int i = 0; i < desc.OutputPins.Length && i < savedDesc.OutputPins.Length; i++)
                {
                    desc.OutputPins[i].face = savedDesc.OutputPins[i].face;
                    desc.OutputPins[i].LocalOffset = savedDesc.OutputPins[i].LocalOffset;
                }
            }

            return CreatePreviewSubChipInstance(desc);
        }

        static void OpenCustomizationMenu()
		{
			ActiveCustomizeChip = CreatePreviewSubChipInstance(ActiveCustomizeDescription);
			CustomizeStateBeforeEnteringCustomizeMenu = CreatePreviewSubChipInstance(Saver.CloneChipDescription(ActiveCustomizeDescription));
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.ChipCustomization);
		}

		public static void RevertCustomizationStateToBeforeEnteringCustomizeMenu()
		{
			ActiveCustomizeChip = CustomizeStateBeforeEnteringCustomizeMenu;
			CustomizeStateBeforeEnteringCustomizeMenu = CreatePreviewSubChipInstance(Saver.CloneChipDescription(ActiveCustomizeDescription));
		}

		static SubChipInstance CreatePreviewSubChipInstance(ChipDescription desc)
		{
			SubChipDescription subChipDesc = new(desc.Name, 0, string.Empty, Vector2.zero, Array.Empty<OutputPinColourInfo>());
			return new SubChipInstance(desc, subChipDesc);
		}

		public static bool ValidateChipNameInput(string nameInput) => nameInput.Length <= MaxLengthChipName.Length && !SaveUtils.NameContainsForbiddenChar(nameInput);

		static bool IsValidSaveName(string chipName)
		{
			Project project = Project.ActiveProject;

			bool validName = !string.IsNullOrWhiteSpace(chipName) && SaveUtils.ValidFileName(chipName);
			bool nameAlreadyUsed = project.chipLibrary.HasChip(chipName);
			bool isNameOfActiveChip = ChipDescription.NameMatch(project.ActiveDevChipName, chipName);

			bool isValid = validName && (!nameAlreadyUsed || isNameOfActiveChip);

			return isValid;
		}

		/// <summary>
		/// Runs chip type detection and auto-completes the name if appropriate.
		/// </summary>
		static void RunChipTypeDetection(ChipDescription desc)
		{
			try
			{
				Debug.Log("RunChipTypeDetection: Starting detection");
				
				// Check if chip has unsaved changes (reuse existing logic)
				var project = Project.ActiveProject;
				if (project == null) 
				{
					Debug.Log("RunChipTypeDetection: No active project");
					return;
				}
				
				bool hasUnsavedChanges = project.ActiveChipHasUnsavedChanges();
				Debug.Log($"RunChipTypeDetection: Has unsaved changes: {hasUnsavedChanges}");
				
				if (!hasUnsavedChanges)
				{
					// No changes, skip detection to save performance
					Debug.Log("RunChipTypeDetection: Skipping detection - no unsaved changes");
					return;
				}
				
				// Save current input pin states before detection
				var originalInputStates = SaveInputPinStates(project.ViewedChip);
				
				try
				{
					// Set up the simulation callback for circuit evaluation
					ChipTypeDetector.SetSimulationCallback(EvaluateCircuitWithSimulation);
					
					// Run detection and get suggested name
					var (detectedType, suggestedName) = ChipTypeDetector.DetectAndSuggestName(desc);
					
					// Clear the simulation callback
					ChipTypeDetector.SetSimulationCallback(null);
					
					// Update the chip's internal type ID
					desc.InternalTypeId = detectedType;
					
					// If detection found a known type and chip name is empty or generic, suggest the detected name
					if (detectedType != ChipTypeId.Unknown && 
					    (string.IsNullOrWhiteSpace(desc.Name) || 
					     desc.Name.Equals("Untitled", StringComparison.OrdinalIgnoreCase)))
					{
						Debug.Log($"RunChipTypeDetection: Auto-completing name to '{suggestedName}'");
						desc.Name = suggestedName;
					}
					else
					{
						Debug.Log($"RunChipTypeDetection: Not auto-completing name. Type: {detectedType}, Current name: '{desc.Name}'");
					}
				}
				finally
				{
					// Always restore original input pin states
					RestoreInputPinStates(project.ViewedChip, originalInputStates);
				}
			}
			catch (Exception ex)
			{
				// Log error but don't break the save flow
				Debug.LogError($"Chip type detection failed: {ex.Message}");
			}
		}
		
		/// <summary>
		/// Uses the actual simulation engine to evaluate the circuit for a given input.
		/// This reuses the same logic that level validation uses.
		/// </summary>
		static bool EvaluateCircuitWithSimulation(int inputValue)
		{
			try
			{
				// Get the current project and simulation adapter
				var project = Project.ActiveProject;
				if (project?.ViewedChip == null)
				{
					Debug.Log("No active project or viewed chip for simulation");
					return false;
				}
				
				// Create a simulation adapter (same as level validation)
				var adapter = new MobileSimulationAdapter();
				
				// Create a bit vector for the input
				var inputVector = new BitVector((ulong)inputValue, 32); // Support up to 32 bits
				
				// Apply inputs to the circuit
				adapter.ApplyInputs(inputVector);
				
				// Let the circuit settle (run simulation for a few steps)
				bool settled = adapter.SettleWithin(10, out int stepsTaken);
				if (!settled)
				{
					Debug.Log($"Circuit did not settle within 10 steps (took {stepsTaken} steps)");
				}
				
				// Read the outputs
				var outputVector = adapter.ReadOutputs();
				
				// Return the first output bit (use indexer to access bit 0)
				bool result = outputVector.Length > 0 ? outputVector[0] : false;
				Debug.Log($"Simulation result: input={inputValue}, output={result}, steps={stepsTaken}");
				
				return result;
			}
			catch (Exception ex)
			{
				Debug.LogError($"Simulation evaluation failed: {ex.Message}");
				return false;
			}
		}
		
		static void InitUIFromDescription(ChipDescription chipDesc)
		{
			// Set input field to current chip name
			InputFieldState inputFieldState = Seb.Vis.UI.UI.GetInputFieldState(ID_ChipNameField);
			inputFieldState.SetText(chipDesc.Name);
		}


		static void Save(Project.SaveMode mode)
		{
			Project.ActiveProject.SaveFromDescription(ActiveCustomizeDescription, mode);
			CloseMenu();
		}

		static void Cancel()
		{
			CloseMenu();
		}

		static void CloseMenu()
		{
			ActiveCustomizeChip = null;
			UIDrawer.SetActiveMenu(returnToMenuOnClose);
			returnToMenuOnClose = UIDrawer.MenuType.None; // Reset for next time
		}

		public static void Reset()
		{
			ActiveCustomizeChip = null;
			CustomizeStateBeforeEnteringCustomizeMenu = null;
		}

		static Color RandomInitialColour()
		{
			float h = (float)rng.NextDouble();
			float s = Mathf.Lerp(0.2f, 1, (float)rng.NextDouble());
			float v = Mathf.Lerp(0.2f, 1, (float)rng.NextDouble());
			return Color.HSVToRGB(h, s, v);
		}
		
		/// <summary>
		/// Saves the current state of all input pins in the chip.
		/// </summary>
		static Dictionary<object, bool> SaveInputPinStates(DevChipInstance viewedChip)
		{
			var originalStates = new Dictionary<object, bool>();
			
			if (viewedChip != null)
			{
				var inputPins = viewedChip.GetInputPins();
				if (inputPins != null)
				{
					foreach (var inputPin in inputPins)
					{
						if (inputPin?.Pin != null)
						{
							originalStates[inputPin.Pin] = inputPin.Pin.PlayerInputState.FirstBitHigh();
						}
					}
				}
			}
			
			Debug.Log($"SaveInputPinStates: Saved {originalStates.Count} input pin states");
			return originalStates;
		}
		
		/// <summary>
		/// Restores the input pin states to their original values.
		/// </summary>
		static void RestoreInputPinStates(DevChipInstance viewedChip, Dictionary<object, bool> originalStates)
		{
			if (viewedChip == null || originalStates == null)
			{
				return;
			}
			
			var inputPins = viewedChip.GetInputPins();
			if (inputPins == null)
			{
				return;
			}
			
			int restoredCount = 0;
			foreach (var inputPin in inputPins)
			{
				if (inputPin?.Pin != null && originalStates.TryGetValue(inputPin.Pin, out bool originalState))
				{
					// Restore the original state using PlayerInputState
					inputPin.Pin.PlayerInputState.SetFirstBit(originalState);
					restoredCount++;
				}
			}
			
			Debug.Log($"RestoreInputPinStates: Restored {restoredCount} input pin states");
		}
	}
}
