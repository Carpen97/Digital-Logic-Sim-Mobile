using System;
using System.Collections.Generic;
using System.Text;
using DLS.Description;
using DLS.Game;
using DLS.Levels;
using DLS.Levels.Host;
using DLS.SaveSystem;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;
using static DLS.Graphics.DrawSettings;

namespace DLS.Graphics
{
	/// <summary>
	/// Overlay menu for creating a user level from the current chip.
	/// Step 1: Name, record mode (all combinations vs manual), optional sampling for large input spaces.
	/// </summary>
	public static class CreateLevelMenu
	{
		const string MaxLengthName = "MY VERY LONG LEVEL NAME";
		const int RecordMode_AllCombinations = 0;
		const int RecordMode_Manual = 1;

		const long LargeSpaceThreshold = 1000;
		static readonly string[] CircuitTypeOptions = { "Combinational", "Sequential" };
		static readonly string[] RecordModeOptions = { "All combinations", "Manually record" };
		static readonly string[] SampleRateOptions = { "1 in 10", "1 in 50", "1 in 100", "1 in 500" };
		static readonly int[] SampleRateValues = { 10, 50, 100, 500 };

		static readonly UIHandle ID_NameField = new("CreateLevel_NameField");
		static readonly UIHandle ID_CircuitType = new("CreateLevel_CircuitType");
		static readonly UIHandle ID_RecordMode = new("CreateLevel_RecordMode");
		static readonly UIHandle ID_SampleRate = new("CreateLevel_SampleRate");
		static readonly Func<string, bool> nameValidator = ValidateNameInput;

		static ChipDescription cachedChipDesc;
		static int totalInputBits;
		static long totalCombinations;
		static int circuitTypeIndex;   // 0 = Combinational, 1 = Sequential
		static int recordModeIndex;
		static int sampleRateIndex;

		public static void OnMenuOpened()
		{
			var project = Project.ActiveProject;
			cachedChipDesc = DescriptionCreator.CreateChipDescription(project.ViewedChip);
			totalInputBits = ChipDescriptionHelper.CountTotalInputWidth(cachedChipDesc);
			totalCombinations = totalInputBits < 63 ? (1L << totalInputBits) : long.MaxValue;
			circuitTypeIndex = 0;
			recordModeIndex = 0;
			sampleRateIndex = totalCombinations > LargeSpaceThreshold ? 2 : 0; // Default 1 in 100 if large
			var state = Seb.Vis.UI.UI.GetInputFieldState(ID_NameField);
			state.SetText("");
			state.SelectAll();
		}

		public static void DrawMenu()
		{
			MenuHelper.DrawBackgroundOverlay();
			var theme = ActiveUITheme;
			Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();

			const float menuWidth = 55;
			const float spacing = 0.5f;
			Vector2 entrySize = new(menuWidth, DrawSettings.SelectorWheelHeight);
			Vector2 topLeft = Seb.Vis.UI.UI.Centre + new Vector2(-menuWidth / 2, 20);
			Color labelCol = Color.white;

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				// Header
				Seb.Vis.UI.UI.DrawText("CREATE LEVEL", theme.FontBold, theme.FontSizeRegular + 1, topLeft, Anchor.TextCentreLeft, new Color(0.46f, 1, 0.54f));
				topLeft += Vector2.down * (entrySize.y + spacing);

				// Name input
				(Vector2 inputFieldSize, float pad) = ChipSaveMenu.GetTextInputSize();
				InputFieldState nameState = Seb.Vis.UI.UI.InputField(ID_NameField, theme.ChipNameInputField, topLeft, inputFieldSize, "Level name", Anchor.TopLeft, pad, nameValidator, true);
				topLeft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * spacing;

				// Circuit type (Combinational vs Sequential)
				int newCircuitType = MenuHelper.LabeledOptionsWheel("Circuit type:", labelCol, topLeft, entrySize, ID_CircuitType, CircuitTypeOptions, entrySize.x * 0.55f, true);
				if (newCircuitType != circuitTypeIndex)
				{
					circuitTypeIndex = newCircuitType;
					if (circuitTypeIndex == 1) recordModeIndex = RecordMode_Manual; // Sequential requires manual recording
				}
				topLeft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * spacing;

				// Record mode (Sequential always uses manual; Combinational offers both)
				bool isSequential = circuitTypeIndex == 1;
				if (isSequential)
				{
					recordModeIndex = RecordMode_Manual;
					// Static text to avoid WheelSelector state collision when switching circuit type
					Vector2 centreRight = MenuHelper.DrawLabelSectionOfLabelInputPair(topLeft, entrySize, "Record mode:", labelCol, true);
					MenuHelper.DrawText("Manually record", centreRight + Vector2.left * (entrySize.x * 0.55f / 2), Anchor.TextCentre, new Color(0.85f, 0.85f, 0.85f));
					Seb.Vis.UI.UI.OverridePreviousBounds(Bounds2D.CreateFromTopLeftAndSize(topLeft, entrySize));
				}
				else
				{
					int newRecordMode = MenuHelper.LabeledOptionsWheel("Record mode:", labelCol, topLeft, entrySize, ID_RecordMode, RecordModeOptions, entrySize.x * 0.55f, true);
					if (newRecordMode != recordModeIndex) recordModeIndex = newRecordMode;
				}
				topLeft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * spacing;

				// Combination count (when Record all)
				if (recordModeIndex == RecordMode_AllCombinations)
				{
					topLeft += Vector2.down * (spacing * 3); // Extra space below record mode
					string countText = totalCombinations >= long.MaxValue || totalCombinations > 1_000_000
						? $"{totalCombinations / 1_000_000}M+ combinations"
						: $"{totalCombinations:N0} combinations ({totalInputBits} input bits)";
					MenuHelper.DrawText(countText, topLeft + Vector2.right * 2, Anchor.TextCentreLeft, new Color(0.8f, 0.8f, 0.8f));
					topLeft += Vector2.down * (entrySize.y * 0.7f + spacing);

					// Sampling (when space is large)
					if (totalCombinations > LargeSpaceThreshold)
					{
						long sampledCount = totalCombinations / SampleRateValues[sampleRateIndex];
						int newSampleRate = MenuHelper.LabeledOptionsWheel("Sample (if large):", labelCol, topLeft, entrySize, ID_SampleRate, SampleRateOptions, entrySize.x / 3, true);
						if (newSampleRate != sampleRateIndex) sampleRateIndex = newSampleRate;
						topLeft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * spacing * 0.5f;
						string sampledText = $"→ ~{sampledCount:N0} test cases";
						MenuHelper.DrawText(sampledText, topLeft + Vector2.right * 2, Anchor.TextCentreLeft, new Color(0.7f, 0.9f, 0.7f));
						topLeft += Vector2.down * (entrySize.y * 0.7f + spacing);
					}
				}
				else
				{
					topLeft += Vector2.down * (spacing * 3); // Extra space below record mode
					MenuHelper.DrawText("You will record test cases manually in the next step.", topLeft + Vector2.right * 2, Anchor.TextCentreLeft, new Color(0.8f, 0.8f, 0.8f));
					topLeft += Vector2.down * (entrySize.y * 0.5f + spacing); // Tighter gap to buttons
				}

				// Buttons
				Vector2 buttonTopLeft = topLeft + Vector2.down * spacing;
				string[] buttonNames = { "CANCEL", "CONTINUE" };
				bool[] buttonEnabled = { true, IsValidLevelName(nameState.text) };
				int buttonIndex = Seb.Vis.UI.UI.HorizontalButtonGroup(buttonNames, buttonEnabled, theme.ButtonTheme, buttonTopLeft, menuWidth, DrawSettings.DefaultButtonSpacing, 0, Anchor.TopLeft);

				if (buttonIndex == 0 || KeyboardShortcuts.CancelShortcutTriggered)
				{
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				}
				else if ((buttonIndex == 1 || KeyboardShortcuts.ConfirmShortcutTriggered) && IsValidLevelName(nameState.text))
				{
					OnContinue(nameState.text.Trim(), recordModeIndex, sampleRateIndex, circuitTypeIndex);
				}

				Bounds2D uiBounds = Seb.Vis.UI.UI.GetCurrentBoundsScope();
				MenuHelper.DrawReservedMenuPanel(panelID, uiBounds);
			}
		}

		static bool ValidateNameInput(string s) => s.Length <= MaxLengthName.Length && !SaveUtils.NameContainsForbiddenChar(s);

		static bool IsValidLevelName(string name)
		{
			if (string.IsNullOrWhiteSpace(name)) return false;
			// User level names - no conflict check with built-in levels for now (level library separate)
			return SaveUtils.ValidFileName(name.Trim());
		}

		static void OnContinue(string levelName, int mode, int sampleIdx, int circuitType)
		{
			if (mode == RecordMode_AllCombinations)
			{
				int sampleRate = totalCombinations > LargeSpaceThreshold ? SampleRateValues[sampleIdx] : 1;
				var (levelDef, error) = RecordAllCombinations(levelName, sampleRate);
				if (levelDef != null)
				{
					string projectName = Project.ActiveProject.description.ProjectName;
					UserLevelStorage.SaveUserLevel(levelDef, projectName);
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
					SimpleMessagePopup.Open($"Level '{levelName}' created with {levelDef.testVectors.Length} test cases. Saved to My levels.");
				}
				else
				{
					SimpleMessagePopup.Open($"Recording failed: {error}");
				}
			}
			else
			{
				bool sequential = circuitType == 1;
				LevelRecordingOverlay.StartRecording(levelName, sequential);
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
			}
		}

		static (LevelDefinition level, string error) RecordAllCombinations(string levelName, int sampleRate)
		{
			var project = Project.ActiveProject;
			if (project == null || project.chipViewStack.Count != 1)
				return (null, "Not in edit mode");

			var adapter = new MobileSimulationAdapter();
			var vectors = new List<LevelDefinition.TestVector>();

			long total = totalCombinations > int.MaxValue ? int.MaxValue : (long)(int)totalCombinations;
			int step = Math.Max(1, sampleRate);
			int recorded = 0;

			for (long combo = 0; combo < total; combo += step)
			{
				string inputStr = ComboToBitString((ulong)combo, totalInputBits);
				var iv = BitVector.FromString(inputStr);
				adapter.ApplyInputs(iv);
				if (!adapter.SettleWithin(2, out _))
				{
					return (null, "Simulation failed to settle");
				}
				var ov = adapter.ReadOutputs();
				vectors.Add(new LevelDefinition.TestVector
				{
					inputs = inputStr,
					expected = ov.ToString(),
					settleSteps = 1,
					isClockEdge = false
				});
				recorded++;
			}

			var levelDef = BuildLevelDefinition(levelName, vectors);
			return (levelDef, null);
		}

		static string ComboToBitString(ulong value, int numBits)
		{
			var sb = new StringBuilder(numBits);
			for (int i = numBits - 1; i >= 0; i--)
				sb.Append((value & (1UL << i)) != 0 ? '1' : '0');
			return sb.ToString();
		}

		/// <summary>Build LevelDefinition for combinational manual recording.</summary>
		public static LevelDefinition BuildLevelDefinitionFromRecording(string levelName, ChipDescription chip, List<LevelDefinition.TestVector> vectors)
		{
			return BuildLevelDefinitionImpl(levelName, chip, vectors);
		}

		/// <summary>Build LevelDefinition for sequential manual recording (TestSequence format).</summary>
		public static LevelDefinition BuildLevelDefinitionFromSequentialRecording(string levelName, ChipDescription chip, List<LevelDefinition.TestSequence> sequences)
		{
			string id = "user." + SaveUtils.SanitizeFileName(levelName).ToLowerInvariant().Replace(" ", "_");
			var inputLabels = new LevelDefinition.PinLabel[chip.InputPins.Length];
			for (int i = 0; i < chip.InputPins.Length; i++)
			{
				var p = chip.InputPins[i];
				inputLabels[i] = new LevelDefinition.PinLabel { name = p.Name ?? $"IN{i}", abbr = p.Name?.Length > 3 ? p.Name.Substring(0, 3) : (p.Name ?? $"I{i}") };
			}
			var outputLabels = new LevelDefinition.PinLabel[chip.OutputPins.Length];
			for (int i = 0; i < chip.OutputPins.Length; i++)
			{
				var p = chip.OutputPins[i];
				outputLabels[i] = new LevelDefinition.PinLabel { name = p.Name ?? $"OUT{i}", abbr = p.Name?.Length > 3 ? p.Name.Substring(0, 3) : (p.Name ?? $"O{i}") };
			}
			var inputBitCounts = new int[chip.InputPins.Length];
			for (int i = 0; i < chip.InputPins.Length; i++) inputBitCounts[i] = chip.InputPins[i].BitCount.BitCount;
			var outputBitCounts = new int[chip.OutputPins.Length];
			for (int i = 0; i < chip.OutputPins.Length; i++) outputBitCounts[i] = chip.OutputPins[i].BitCount.BitCount;

			return new LevelDefinition
			{
				id = id,
				name = levelName,
				description = "",
				chapterId = "user",
				inputCount = chip.InputPins.Length,
				outputCount = chip.OutputPins.Length,
				inputBitCounts = inputBitCounts,
				outputBitCounts = outputBitCounts,
				inputPinLabels = inputLabels,
				outputPinLabels = outputLabels,
				isSequential = true,
				settleStepsPerVector = 2,
				testSequences = sequences.ToArray()
			};
		}

		static LevelDefinition BuildLevelDefinition(string levelName, List<LevelDefinition.TestVector> vectors)
		{
			return BuildLevelDefinitionImpl(levelName, cachedChipDesc, vectors);
		}

		static LevelDefinition BuildLevelDefinitionImpl(string levelName, ChipDescription chip, List<LevelDefinition.TestVector> vectors)
		{
			string id = "user." + SaveUtils.SanitizeFileName(levelName).ToLowerInvariant().Replace(" ", "_");

			var inputLabels = new LevelDefinition.PinLabel[chip.InputPins.Length];
			for (int i = 0; i < chip.InputPins.Length; i++)
			{
				var p = chip.InputPins[i];
				inputLabels[i] = new LevelDefinition.PinLabel
				{
					name = p.Name ?? $"IN{i}",
					abbr = p.Name?.Length > 3 ? p.Name.Substring(0, 3) : (p.Name ?? $"I{i}")
				};
			}

			var outputLabels = new LevelDefinition.PinLabel[chip.OutputPins.Length];
			for (int i = 0; i < chip.OutputPins.Length; i++)
			{
				var p = chip.OutputPins[i];
				outputLabels[i] = new LevelDefinition.PinLabel
				{
					name = p.Name ?? $"OUT{i}",
					abbr = p.Name?.Length > 3 ? p.Name.Substring(0, 3) : (p.Name ?? $"O{i}")
				};
			}

			var inputBitCounts = new int[chip.InputPins.Length];
			for (int i = 0; i < chip.InputPins.Length; i++)
				inputBitCounts[i] = chip.InputPins[i].BitCount.BitCount;

			var outputBitCounts = new int[chip.OutputPins.Length];
			for (int i = 0; i < chip.OutputPins.Length; i++)
				outputBitCounts[i] = chip.OutputPins[i].BitCount.BitCount;

			return new LevelDefinition
			{
				id = id,
				name = levelName,
				description = "",
				chapterId = "user",
				inputCount = chip.InputPins.Length,
				outputCount = chip.OutputPins.Length,
				inputBitCounts = inputBitCounts,
				outputBitCounts = outputBitCounts,
				inputPinLabels = inputLabels,
				outputPinLabels = outputLabels,
				testVectors = vectors.ToArray(),
				isSequential = false
			};
		}
	}
}
