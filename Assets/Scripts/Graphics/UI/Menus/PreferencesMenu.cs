using System;
using DLS.Description;
using DLS.Game;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	public static class PreferencesMenu
	{
		const float entrySpacing = 0.2f;
		#if UNITY_ANDROID || UNITY_IOS
		const float menuWidth = 80;
		#else
		const float menuWidth = 55;
		#endif
		const float verticalOffset = 26;

		public const int DisplayMode_Always = 0;
		public const int DisplayMode_OnHover = 1;
		public const int DisplayMode_TabToggle = 2;
		public const int DisplayMode_OnlyInLevel = 3;

		static readonly string[] PinDisplayOptions =
		{
			#if UNITY_ANDROID || UNITY_IOS
			"On",
			"Multi-Hover",
			"Off",
			"Only in Level"
			#else
			"Always",
			"On Hover",
			"Tab to Toggle",
			"Only in Level"
			#endif
		};

		static readonly string[] GridDisplayOptions =
		{
			"Off",
			"On"
		};

		static readonly string[] WireCurvatureOptions =
		{
			"Off",
			"Small",
			"Medium",
			"Large",
		};

		static readonly string[] MultiWireLayoutAlgorithmOptions =
		{
			"Original",
			"Slerp",
		};

		static readonly string[] SnappingOptions =
		{
			#if UNITY_ANDROID || UNITY_IOS
			"Off",
			"If Grid Shown",
			"On"
			#else
			"Hold Ctrl",
			"If Grid Shown",
			"Always"
			#endif
		};

		static readonly string[] StraightWireOptions =
		{
			#if UNITY_ANDROID || UNITY_IOS
			"Off",
			"If Grid Shown",
			"On"
			#else
			"Hold Shift",
			"If Grid Shown",
			"Always"
			#endif
		};

		static readonly string[] UIThemeOptions =
		{
			#if UNITY_ANDROID || UNITY_IOS
			"Theme 1",
			"Squiggles",
			#else
			"Theme 1",
			"Squiggles",
			"Dark",
			"Light"
			#endif
		};

		static readonly string[] SimulationStatusOptions =
		{
			"Active",
			"Paused"
		};
		static readonly string[] PinIndicators =
        {
            "Off",
            "On Hover",
			"Tab To Toggle",
			"On Disconnected",
			"Always"
        };

		static readonly string[] ControlSchemeOptions =
		{
			"Drag and Lock",
			"Drag and Drop"
		};

		static readonly Vector2 entrySize = new(menuWidth, DrawSettings.SelectorWheelHeight);
		public static readonly Vector2 settingFieldSize = new(entrySize.x / 3, entrySize.y);

		// ---- State ----
		static readonly UIHandle ID_MainPinNames = new("PREFS_MainPinNames");
		static readonly UIHandle ID_ChipPinNames = new("PREFS_ChipPinNames");
		static readonly UIHandle ID_GridDisplay = new("PREFS_GridDisplay");
		static readonly UIHandle ID_WireCurvatureDisplay = new("PREFS_WireCurvatureDisplay");
		static readonly UIHandle ID_MultiWireLayoutAlgorithm = new("PREFS_MultiWireLayoutAlgorithm");
		static readonly UIHandle ID_UIThemeDisplay = new("PREFS_UIThemeDisplay");
		static readonly UIHandle ID_Snapping = new("PREFS_Snapping");
		static readonly UIHandle ID_StraightWires = new("PREFS_StraightWires");
		static readonly UIHandle ID_SimStatus = new("PREFS_SimStatus");
		static readonly UIHandle ID_SimFrequencyField = new("PREFS_SimTickTarget");
		static readonly UIHandle ID_ClockSpeedInput = new("PREFS_ClockSpeed");
		static readonly UIHandle ID_PinIndicators = new("PREFS_PinIndicators");
		static readonly UIHandle ID_ControlScheme = new("PREFS_ControlScheme");

		// Section collapse/expand state
		static readonly UIHandle ID_DisplaySection = new("PREFS_DisplaySection");
		static readonly UIHandle ID_EditingSection = new("PREFS_EditingSection");
		static readonly UIHandle ID_SimulationSection = new("PREFS_SimulationSection");

		#if UNITY_ANDROID || UNITY_IOS
		static readonly string showGridLabel = "Show grid";
		static readonly string wireCurvatureLabel = "Wire curvature";
		static readonly string UIThemeLabel = "UI Theme";
		#else
		static readonly string showGridLabel = "Show grid" + CreateShortcutString("Ctrl+G");
		static readonly string wireCurvatureLabel = "Wire curvature";
		static readonly string UIThemeLabel = "UI Theme";
		#endif
		static readonly string simStatusLabel = "Sim Status" + CreateShortcutString("Ctrl+Space");
		static readonly Func<string, bool> integerInputValidator = ValidateIntegerInput;

		// Section collapse/expand state
		static bool displaySectionExpanded = true;
		static bool editingSectionExpanded = true;
		static bool simulationSectionExpanded = false; // Start collapsed
		
		// Track which sections were opened most recently (for limiting to max 2 open)
		static int lastOpenedSection = 0; // 0=display, 1=editing, 2=simulation

		// Helper methods for collapsible sections
		static bool IsDisplaySectionExpanded() => displaySectionExpanded;
		static bool IsEditingSectionExpanded() => editingSectionExpanded;
		static bool IsSimulationSectionExpanded() => simulationSectionExpanded;
		
		static void ToggleDisplaySection() => ToggleSection(0);
		static void ToggleEditingSection() => ToggleSection(1);
		static void ToggleSimulationSection() => ToggleSection(2);
		
		static void ToggleSection(int sectionIndex)
		{
			// Count how many sections are currently expanded
			int expandedCount = 0;
			if (displaySectionExpanded) expandedCount++;
			if (editingSectionExpanded) expandedCount++;
			if (simulationSectionExpanded) expandedCount++;
			
			// If we're trying to expand a third section, close the oldest one
			if (expandedCount >= 2 && !GetSectionExpanded(sectionIndex))
			{
				// Close the section that wasn't opened most recently
				if (lastOpenedSection != 0) displaySectionExpanded = false;
				if (lastOpenedSection != 1) editingSectionExpanded = false;
				if (lastOpenedSection != 2) simulationSectionExpanded = false;
			}
			
			// Toggle the requested section
			SetSectionExpanded(sectionIndex, !GetSectionExpanded(sectionIndex));
			
			// Update last opened if we're expanding
			if (GetSectionExpanded(sectionIndex))
			{
				lastOpenedSection = sectionIndex;
			}
		}
		
		static bool GetSectionExpanded(int sectionIndex)
		{
			return sectionIndex switch
			{
				0 => displaySectionExpanded,
				1 => editingSectionExpanded,
				2 => simulationSectionExpanded,
				_ => false
			};
		}
		
		static void SetSectionExpanded(int sectionIndex, bool expanded)
		{
			switch (sectionIndex)
			{
				case 0: displaySectionExpanded = expanded; break;
				case 1: editingSectionExpanded = expanded; break;
				case 2: simulationSectionExpanded = expanded; break;
			}
		}

		static double simAvgTicksPerSec_delayedRefreshForUI;
		static float lastSimAvgTicksPerSecRefreshTime;
		static float lastSimTickRateSetTime;
		static ProjectDescription originalProjectDesc;
		static string currentSimSpeedString = string.Empty;
		static Color currentSimSpeedStringColour;


		public static void DrawMenu(Project project)
		{
			//HandleKeyboardShortcuts();

			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			MenuHelper.DrawBackgroundOverlay();
			Draw.ID panelID = UI.ReservePanel();
			UpdateSimSpeedString(project);

			const int inputTextPad = 1;
			const float headerSpacing = 2.5f; // Increased from 1.5f to make header rows bigger
			Color labelCol = Color.white;
			Color headerCol = new(0.46f, 1, 0.54f);
			Vector2 topLeft = UI.Centre + new Vector2(-menuWidth / 2, verticalOffset);
			Vector2 labelPosCurr = topLeft;

			using (UI.BeginBoundsScope(true))
			{
				// --- Draw settings ---
				DrawCollapsibleHeader("DISPLAY:", ID_DisplaySection, IsDisplaySectionExpanded, ToggleDisplaySection);
				if (IsDisplaySectionExpanded())
				{
					DrawNextWheel("Show I/O pin names", PinDisplayOptions, ID_MainPinNames);
					DrawNextWheel("Show chip pin names", PinDisplayOptions, ID_ChipPinNames);
					DrawNextWheel(showGridLabel, GridDisplayOptions, ID_GridDisplay);
					DrawNextWheel(wireCurvatureLabel, WireCurvatureOptions, ID_WireCurvatureDisplay);
					DrawNextWheel("Multi-wire layout", MultiWireLayoutAlgorithmOptions, ID_MultiWireLayoutAlgorithm);
					DrawNextWheel(UIThemeLabel, UIThemeOptions, ID_UIThemeDisplay);
				}

				DrawCollapsibleHeader("EDITING:", ID_EditingSection, IsEditingSectionExpanded, ToggleEditingSection);
				if (IsEditingSectionExpanded())
				{
					DrawNextWheel("Show Pin indicators",PinIndicators, ID_PinIndicators);
					DrawNextWheel("Snap to grid", SnappingOptions, ID_Snapping);
					DrawNextWheel("Straight wires", StraightWireOptions, ID_StraightWires);
					DrawNextWheel("Control scheme", ControlSchemeOptions, ID_ControlScheme);
				}

				DrawCollapsibleHeader("SIMULATION:", ID_SimulationSection, IsSimulationSectionExpanded, ToggleSimulationSection);
				if (IsSimulationSectionExpanded())
				{
					MenuHelper.LabeledOptionsWheel(simStatusLabel, labelCol, labelPosCurr, entrySize, ID_SimStatus, SimulationStatusOptions, settingFieldSize.x, true);
					AddSpacing();
					MenuHelper.LabeledInputField("Steps per clock tick", labelCol, labelPosCurr, entrySize, ID_ClockSpeedInput, integerInputValidator, settingFieldSize.x, true);
					AddSpacing();
					MenuHelper.LabeledInputField("Steps per second (target)", labelCol, labelPosCurr, entrySize, ID_SimFrequencyField, integerInputValidator, settingFieldSize.x, true);
					AddSpacing();
					// Draw current simulation speed
					Vector2 tickLabelRight = MenuHelper.DrawLabelSectionOfLabelInputPair(labelPosCurr, entrySize, "Steps per second (current)", labelCol * 0.75f, true);
					UI.DrawPanel(tickLabelRight, settingFieldSize, new Color(0.18f, 0.18f, 0.18f), Anchor.CentreRight);
					UI.DrawText(currentSimSpeedString, theme.FontBold, theme.FontSizeRegular, tickLabelRight + new Vector2(inputTextPad - settingFieldSize.x, 0), Anchor.TextCentreLeft, currentSimSpeedStringColour);
				}

				// Draw cancel/confirm buttons
				Vector2 buttonTopLeft = new(labelPosCurr.x, UI.PrevBounds.Bottom);
				MenuHelper.CancelConfirmResult result = MenuHelper.DrawCancelConfirmButtons(buttonTopLeft, menuWidth, true);

				// Draw menu background
				Bounds2D menuBounds = UI.GetCurrentBoundsScope();
				MenuHelper.DrawReservedMenuPanel(panelID, menuBounds);

				// ---- Handle changes ----
				// Get values from expanded sections only
				int mainPinNamesMode = IsDisplaySectionExpanded() ? UI.GetWheelSelectorState(ID_MainPinNames).index : project.description.Prefs_MainPinNamesDisplayMode;
				int chipPinNamesMode = IsDisplaySectionExpanded() ? UI.GetWheelSelectorState(ID_ChipPinNames).index : project.description.Prefs_ChipPinNamesDisplayMode;
				int gridDisplayMode = IsDisplaySectionExpanded() ? UI.GetWheelSelectorState(ID_GridDisplay).index : project.description.Prefs_GridDisplayMode;
				int wireDisplayMode = IsDisplaySectionExpanded() ? UI.GetWheelSelectorState(ID_WireCurvatureDisplay).index : project.description.Prefs_WireCurvatureMode;
				int multiWireLayoutAlgorithm = IsDisplaySectionExpanded() ? UI.GetWheelSelectorState(ID_MultiWireLayoutAlgorithm).index : project.description.Prefs_MultiWireLayoutAlgorithm;
				int UIThemeMode = IsDisplaySectionExpanded() ? UI.GetWheelSelectorState(ID_UIThemeDisplay).index : project.description.Prefs_UIThemeMode;
				
				int pinIndicatorsMode = IsEditingSectionExpanded() ? UI.GetWheelSelectorState(ID_PinIndicators).index : project.description.Perfs_PinIndicators;
				int snappingMode = IsEditingSectionExpanded() ? UI.GetWheelSelectorState(ID_Snapping).index : project.description.Prefs_Snapping;
				int straightWireMode = IsEditingSectionExpanded() ? UI.GetWheelSelectorState(ID_StraightWires).index : project.description.Prefs_StraightWires;
				int controlSchemeMode = IsEditingSectionExpanded() ? UI.GetWheelSelectorState(ID_ControlScheme).index : (project.description.Prefs_UseDragAndDropMode ? 1 : 0);
				
				bool pauseSim = IsSimulationSectionExpanded() ? (UI.GetWheelSelectorState(ID_SimStatus).index == 1) : project.description.Prefs_SimPaused;
				InputFieldState clockSpeedInputFieldState = IsSimulationSectionExpanded() ? UI.GetInputFieldState(ID_ClockSpeedInput) : new InputFieldState();
				InputFieldState freqState = IsSimulationSectionExpanded() ? UI.GetInputFieldState(ID_SimFrequencyField) : new InputFieldState();
				
				int.TryParse(clockSpeedInputFieldState.text, out int clockSpeed);
				int.TryParse(freqState.text, out int targetSimTicksPerSecond);
				
				// Apply defaults for invalid values - use aggressive defaults to override wrong values
				if (clockSpeed <= 0 || clockSpeed < 10) clockSpeed = 250;
				if (targetSimTicksPerSecond <= 0 || targetSimTicksPerSecond < 100) targetSimTicksPerSecond = 1000;
				
				targetSimTicksPerSecond = Mathf.Max(1, targetSimTicksPerSecond);
				if (project.targetTicksPerSecond != targetSimTicksPerSecond || project.simPaused != pauseSim) lastSimTickRateSetTime = Time.time;

				// Assign changes immediately so can see them take effect in background
				project.description.Prefs_MainPinNamesDisplayMode = mainPinNamesMode;
				project.description.Prefs_ChipPinNamesDisplayMode = chipPinNamesMode;
				project.description.Prefs_GridDisplayMode = gridDisplayMode;
				project.description.Prefs_WireCurvatureMode = wireDisplayMode;
				project.description.Prefs_MultiWireLayoutAlgorithm = multiWireLayoutAlgorithm;
				project.description.Prefs_UIThemeMode = UIThemeMode;
				project.description.Prefs_Snapping = snappingMode;
				project.description.Prefs_StraightWires = straightWireMode;
				project.description.Prefs_SimTargetStepsPerSecond = targetSimTicksPerSecond;
				project.description.Prefs_SimStepsPerClockTick = clockSpeed;
				project.description.Prefs_SimPaused = pauseSim;
				project.description.Perfs_PinIndicators = pinIndicatorsMode;
				project.description.Prefs_UseDragAndDropMode = controlSchemeMode == 1;

                // Cancel / Confirm
                if (result == MenuHelper.CancelConfirmResult.Cancel)
				{
					// Restore original description
					project.description = originalProjectDesc;
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				}
				else if (result == MenuHelper.CancelConfirmResult.Confirm)
				{
					// Save changes
					Debug.Log("Saving new changes");
					Debug.Log(project.description);
					project.UpdateAndSaveProjectDescription(project.description);
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				}
			}

			return;

			int DrawNextWheel(string label, string[] options, UIHandle id)
			{
				int index = MenuHelper.LabeledOptionsWheel(label, labelCol, labelPosCurr, entrySize, id, options, settingFieldSize.x, true);
				AddSpacing();
				return index;
			}


			void DrawCollapsibleHeader(string text, UIHandle sectionID, System.Func<bool> isExpanded, System.Action toggleAction)
			{
				AddHeaderSpacing();
				
				// Draw header text
				UI.DrawText(text, theme.FontBold, theme.FontSizeRegular, labelPosCurr, Anchor.TextCentreLeft, headerCol);
				
				// Draw toggle button to the right (much bigger for easier tapping)
				Vector2 toggleButtonPos = new Vector2(labelPosCurr.x + menuWidth - 8.0f, labelPosCurr.y);
				Vector2 toggleButtonSize = new Vector2(7.5f, 2.0f); // Even bigger buttons
				string toggleText = isExpanded() ? "−" : "+";
				
				if (UI.Button(toggleText, theme.MenuButtonTheme, toggleButtonPos, toggleButtonSize, true, true, false, theme.ButtonTheme.buttonCols, Anchor.CentreLeft))
				{
					toggleAction();
				}
				
				AddHeaderSpacing();
			}

			void AddSpacing()
			{
				labelPosCurr.y -= entrySize.y + entrySpacing;
			}

			void AddHeaderSpacing()
			{
				labelPosCurr.y -= headerSpacing;
			}
		}

		public static void OnMenuOpened()
		{
			originalProjectDesc = Project.ActiveProject.description;

			// Initialize section states (DISPLAY and EDITING expanded, SIMULATION collapsed)
			displaySectionExpanded = true;
			editingSectionExpanded = true;
			simulationSectionExpanded = false;

			UpdateUIFromDescription();

			simAvgTicksPerSec_delayedRefreshForUI = Project.ActiveProject.simAvgTicksPerSec;
			lastSimAvgTicksPerSecRefreshTime = float.MinValue;
		}

		static void UpdateUIFromDescription()
		{
			if (UIDrawer.ActiveMenu != UIDrawer.MenuType.Preferences) return;

			ProjectDescription projDesc = Project.ActiveProject.description;

			UI.GetWheelSelectorState(ID_MainPinNames).index = projDesc.Prefs_MainPinNamesDisplayMode;
			UI.GetWheelSelectorState(ID_ChipPinNames).index = projDesc.Prefs_ChipPinNamesDisplayMode;
			UI.GetWheelSelectorState(ID_GridDisplay).index = projDesc.Prefs_GridDisplayMode;
			UI.GetWheelSelectorState(ID_WireCurvatureDisplay).index = projDesc.Prefs_WireCurvatureMode;
			UI.GetWheelSelectorState(ID_MultiWireLayoutAlgorithm).index = projDesc.Prefs_MultiWireLayoutAlgorithm;
			UI.GetWheelSelectorState(ID_UIThemeDisplay).index = projDesc.Prefs_UIThemeMode;
	
			// 🛠 Clamp snapping and straight wire mode indexes
			#if UNITY_ANDROID || UNITY_IOS
			UI.GetWheelSelectorState(ID_Snapping).index = Mathf.Clamp(projDesc.Prefs_Snapping, 0, SnappingOptions.Length - 1);
			UI.GetWheelSelectorState(ID_StraightWires).index = Mathf.Clamp(projDesc.Prefs_StraightWires, 0, StraightWireOptions.Length - 1);
			#else
			UI.GetWheelSelectorState(ID_Snapping).index = projDesc.Prefs_Snapping;
			UI.GetWheelSelectorState(ID_StraightWires).index = projDesc.Prefs_StraightWires;
			#endif
	
			UI.GetWheelSelectorState(ID_SimStatus).index = projDesc.Prefs_SimPaused ? 1 : 0;
			UI.GetWheelSelectorState(ID_PinIndicators).index = projDesc.Perfs_PinIndicators;
			UI.GetWheelSelectorState(ID_ControlScheme).index = projDesc.Prefs_UseDragAndDropMode ? 1 : 0;
            // -- Input fields with default value handling
            int targetStepsPerSecond = projDesc.Prefs_SimTargetStepsPerSecond;
            int stepsPerClockTick = projDesc.Prefs_SimStepsPerClockTick;
            
            // Apply defaults for invalid values (likely from older project versions)
            // Use more aggressive defaults to override clearly wrong values
            if (targetStepsPerSecond <= 0 || targetStepsPerSecond < 100) targetStepsPerSecond = 1000;
            if (stepsPerClockTick <= 0 || stepsPerClockTick < 10) stepsPerClockTick = 250;
            
            UI.GetInputFieldState(ID_SimFrequencyField).SetText(targetStepsPerSecond + "", false);
			UI.GetInputFieldState(ID_ClockSpeedInput).SetText(stepsPerClockTick + "", false);
		}


		public static void HandleKeyboardShortcuts()
		{
			bool inPrefsMenu = UIDrawer.ActiveMenu == UIDrawer.MenuType.Preferences;
			bool anyChange = false;

			if (KeyboardShortcuts.ToggleGridShortcutTriggered)
			{
				Project.ActiveProject.ToggleGridDisplay();
				anyChange = true;
			}

			if (KeyboardShortcuts.SimPauseToggleShortcutTriggered)
			{
				Project.ActiveProject.description.Prefs_SimPaused = !Project.ActiveProject.description.Prefs_SimPaused;
				anyChange = true;
			}

			if (anyChange)
			{
				if (inPrefsMenu) UpdateUIFromDescription();
				else Project.ActiveProject.SaveCurrentProjectDescription();
			}
		}

		static void UpdateSimSpeedString(Project project)
		{
			// Annoying if sim tick rate value flickers too much, so use slower refresh rate for ui
			// (but if sim target rate has been recently changed, update fast so doesn't feel laggy)
			bool slowModeSimUI = Time.time - lastSimTickRateSetTime > Project.SimulationPerformanceTimeWindowSec;
			const float slowModeRefreshDelay = 0.35f;
			const float fastModeRefreshDelay = 0.05f;
			float refreshDelay = slowModeSimUI ? slowModeRefreshDelay : fastModeRefreshDelay;

			if (Time.time > lastSimAvgTicksPerSecRefreshTime + refreshDelay || Project.ActiveProject.simPaused)
			{
				simAvgTicksPerSec_delayedRefreshForUI = project.simAvgTicksPerSec;
				lastSimAvgTicksPerSecRefreshTime = Time.time;
				currentSimSpeedString = project.simPaused ? "0" : $"{simAvgTicksPerSec_delayedRefreshForUI:0}";
				currentSimSpeedStringColour = GetSimFrequencyErrorCol();
			}
		}

		public static bool ValidateIntegerInput(string s)
		{
			if (string.IsNullOrEmpty(s)) return true;
			if (s.Contains(" ")) return false;
			return int.TryParse(s, out _);
		}

		public static Color GetSimFrequencyErrorCol()
		{
			Color frequencyErrorCol = new(0.3f, 0.92f, 0.32f);

			if (Project.ActiveProject.simPaused)
			{
				frequencyErrorCol = new Color(1, 1, 1, 0.35f);
			}
			else
			{
				int simFreqError = Mathf.RoundToInt(Project.ActiveProject.targetTicksPerSecond - (float)Project.ActiveProject.simAvgTicksPerSec);
				if (simFreqError > 10) frequencyErrorCol = new Color(0.95f, 0.25f, 0.13f);
				else if (simFreqError > 5) frequencyErrorCol = new Color(1, 0.38f, 0.27f);
				else if (simFreqError > 2) frequencyErrorCol = new Color(1, 0.7f, 0.27f);
			}

			return frequencyErrorCol;
		}

		#if UNITY_ANDROID || UNITY_IOS

		static string CreateShortcutString(string s) => "";
		#else

		static string CreateShortcutString(string s) => UI.CreateColouredText("  " +s, new Color(1, 1, 1, 0.3f));
		#endif
	}
}
