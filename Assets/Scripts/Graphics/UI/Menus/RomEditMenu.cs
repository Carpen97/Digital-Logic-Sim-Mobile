using System;
using System.Text;
using DLS.Description;
using DLS.Game;
using DLS.SaveSystem;
using Seb.Helpers;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	public static class RomEditMenu
	{
		static int ActiveRomDataBitCount;
		static int RowCount;

	static UIHandle ID_scrollbar;
	static UIHandle ID_DataDisplayMode;
	static UIHandle ID_PinConfiguration;
	static int focusedRowIndex;
	static int gridPatternSize = 4; // Default: group by 4 bits
	static int pinConfiguration = 0; // 0=2x8, 1=1x16, 2=16x1, 3=4x4
	static int pendingPinConfiguration = -1; // -1 = no pending change, otherwise the new config to apply
	static bool showingWireDeletionWarning = false;
	static int warningPendingConfig = -1;
	static bool chipWasReplaced = false; // Track if chip was replaced (to avoid saving after replacement)
		static UIHandle[] IDS_inputRow;
		static UIHandle[][] IDS_bitButtons; // Array of arrays: [rowIndex][bitIndex]
		static UIHandle[] IDS_rowSelectButtons; // Array for row selection buttons
		static string[] rowNumberStrings;
		static int selectedRowIndex = -1; // Currently selected row (-1 = none)
		static uint copiedRowData = 0; // Copied row data

		static SubChipInstance romChip;


	static readonly string[] DataDisplayOptions =
	{
		"Unsigned\nDecimal",
		"Signed\nDecimal",
		"Binary",
		"HEX",
		"Graphical"
	};

	static readonly string[] PinConfigurationOptions =
	{
		"2x8-bit",
		"1x16-bit",
		"16x1-bit",
		"4x4-bit"
	};

		static DataDisplayMode[] allDisplayModes;

		static DataDisplayMode dataDisplayMode;
		static readonly Seb.Vis.UI.UI.ScrollViewDrawElementFunc scrollViewDrawElementFunc = DrawScrollEntry;
		static readonly Func<string, bool> inputStringValidator = ValidateInputString;

		static Bounds2D scrollViewBounds;

		static float textPad => 0.52f;
		static float height => 3.72f; // Larger touch targets for mobile
		static float leftAdjustmentOfScrollView => 12f;

		public static void DrawMenu()
		{
			// Handle wire deletion warning popup
			if (showingWireDeletionWarning)
			{
				DrawWireDeletionWarningPopup();
				return;
			}

			MenuHelper.DrawBackgroundOverlay();

			// ---- Draw ROM contents ----
			#if UNITY_ANDROID || UNITY_IOS
			scrollViewBounds = Bounds2D.CreateFromCentreAndSize(Seb.Vis.UI.UI.Centre + Vector2.left * leftAdjustmentOfScrollView, new Vector2(Seb.Vis.UI.UI.Width * 0.7f, Seb.Vis.UI.UI.Height * 0.8f));
			Seb.Vis.UI.UI.DrawPanel(Bounds2D.Grow(scrollViewBounds, 0.5f),ColHelper.MakeCol(0.23f));
			#else
			scrollViewBounds = Bounds2D.CreateFromCentreAndSize(Seb.Vis.UI.UI.Centre + Vector2.left * leftAdjustmentOfScrollView, new Vector2(Seb.Vis.UI.UI.Width * 0.68f, Seb.Vis.UI.UI.Height * 0.8f));
			Seb.Vis.UI.UI.DrawPanel(Bounds2D.Grow(scrollViewBounds, 0.5f),ColHelper.MakeCol(0.23f));
			#endif
			ScrollViewTheme scrollTheme = DrawSettings.ActiveUITheme.ScrollTheme;
			Seb.Vis.UI.UI.DrawScrollView(ID_scrollbar, scrollViewBounds.TopLeft, scrollViewBounds.Size, 0, Anchor.TopLeft, scrollTheme, scrollViewDrawElementFunc, RowCount);


			if (focusedRowIndex >= 0)
			{
				// Focus next/prev field with keyboard shortcuts
				bool changeLine = KeyboardShortcuts.ConfirmShortcutTriggered || InputHelper.IsKeyDownThisFrame(KeyCode.Tab);

				if (changeLine)
				{
					bool goPrevLine = InputHelper.ShiftIsHeld;
					int jumpToRowIndex = focusedRowIndex + (goPrevLine ? -1 : 1);

					if (jumpToRowIndex >= 0 && jumpToRowIndex < RowCount)
					{
						OnFieldLostFocus(focusedRowIndex);
						int nextFocusedRowIndex = focusedRowIndex + (goPrevLine ? -1 : 1);
						Seb.Vis.UI.UI.GetInputFieldState(IDS_inputRow[nextFocusedRowIndex]).SetFocus(true);
						focusedRowIndex = nextFocusedRowIndex;
					}
				}
			}

			// --- Draw side panel with buttons ----
			#if UNITY_ANDROID || UNITY_IOS
			Vector2 sidePanelSize = new(Seb.Vis.UI.UI.Width * 0.2f, Seb.Vis.UI.UI.Height * 0.8f); 
			#else
			Vector2 sidePanelSize = new(Seb.Vis.UI.UI.Width * 0.2f, Seb.Vis.UI.UI.Height * 0.8f);
			#endif
			Vector2 sidePanelTopLeft = scrollViewBounds.TopRight + Vector2.right * (Seb.Vis.UI.UI.Width * 0.03f) + Vector2.down * 0.8f;
			Draw.ID sidePanelID = Seb.Vis.UI.UI.ReservePanel();

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				#if UNITY_ANDROID || UNITY_IOS
				const float buttonSpacing = 0.5f; // More spacing on mobile for better touch targets
				#else
				const float buttonSpacing = 0.75f;
				#endif

			float selectorWheelHeight = DrawSettings.SelectorWheelHeight;
			selectorWheelHeight *= 2;

		// Display mode
		DataDisplayMode modeNew = (DataDisplayMode)Seb.Vis.UI.UI.WheelSelector(ID_DataDisplayMode, DataDisplayOptions, sidePanelTopLeft, new Vector2(sidePanelSize.x, selectorWheelHeight), MenuHelper.Theme.OptionsWheel, Anchor.TopLeft);
		Vector2 buttonTopleft = new(sidePanelTopLeft.x, Seb.Vis.UI.UI.PrevBounds.Bottom - buttonSpacing);

		// Pin configuration selector
		int pinConfigNew = Seb.Vis.UI.UI.WheelSelector(ID_PinConfiguration, PinConfigurationOptions, buttonTopleft, new Vector2(sidePanelSize.x, selectorWheelHeight), MenuHelper.Theme.OptionsWheel, Anchor.TopLeft);
		buttonTopleft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * buttonSpacing;

		// Handle pin configuration changes
		if (pinConfigNew != pinConfiguration)
		{
			// Store pending change for actual chip replacement
			pendingPinConfiguration = pinConfigNew;
			
			// Update UI immediately for preview (visual grouping)
			UpdateGridPatternSizeForPreview(pinConfigNew);
		}
		else
		{
			pendingPinConfiguration = -1; // No change needed
		}

			#if UNITY_ANDROID || UNITY_IOS
			// Mobile: Vertical layout - one button per row
			bool copyAll = Seb.Vis.UI.UI.Button("COPY ALL", MenuHelper.Theme.ButtonTheme, buttonTopleft, new Vector2(sidePanelSize.x, 0), true, false, true, MenuHelper.Theme.ButtonTheme.buttonCols, Anchor.TopLeft);
			buttonTopleft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * buttonSpacing;
			
			bool pasteAll = Seb.Vis.UI.UI.Button("PASTE ALL", MenuHelper.Theme.ButtonTheme, buttonTopleft, new Vector2(sidePanelSize.x, 0), true, false, true, MenuHelper.Theme.ButtonTheme.buttonCols, Anchor.TopLeft);
			buttonTopleft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * buttonSpacing;
			
			bool clearAll = Seb.Vis.UI.UI.Button("CLEAR ALL", MenuHelper.Theme.ButtonTheme, buttonTopleft, new Vector2(sidePanelSize.x, 0), true, false, true, MenuHelper.Theme.ButtonTheme.buttonCols, Anchor.TopLeft);
			buttonTopleft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * buttonSpacing;
			
			bool fillZeros = Seb.Vis.UI.UI.Button("FILL 0s", MenuHelper.Theme.ButtonTheme, buttonTopleft, new Vector2(sidePanelSize.x, 0), true, false, true, MenuHelper.Theme.ButtonTheme.buttonCols, Anchor.TopLeft);
			buttonTopleft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * buttonSpacing;
			
			bool fillOnes = Seb.Vis.UI.UI.Button("FILL 1s", MenuHelper.Theme.ButtonTheme, buttonTopleft, new Vector2(sidePanelSize.x, 0), true, false, true, MenuHelper.Theme.ButtonTheme.buttonCols, Anchor.TopLeft);
			buttonTopleft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * buttonSpacing;
			
			MenuHelper.CancelConfirmResult result = MenuHelper.DrawCancelConfirmButtons(buttonTopleft, sidePanelSize.x, false, false);
			#else
			// PC: Paired layout - two buttons per row
			int copyPasteButtonIndex = MenuHelper.DrawButtonPair("COPY ALL", "PASTE ALL", buttonTopleft, sidePanelSize.x, false);
			buttonTopleft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * buttonSpacing;
			bool clearAll = Seb.Vis.UI.UI.Button("CLEAR ALL", MenuHelper.Theme.ButtonTheme, buttonTopleft, new Vector2(sidePanelSize.x, 0), true, false, true, MenuHelper.Theme.ButtonTheme.buttonCols, Anchor.TopLeft);
			buttonTopleft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * buttonSpacing;
			
			int presetButtonIndex = MenuHelper.DrawButtonPair("FILL 0s", "FILL 1s", buttonTopleft, sidePanelSize.x, false);
			buttonTopleft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * buttonSpacing;
			MenuHelper.CancelConfirmResult result = MenuHelper.DrawCancelConfirmButtons(buttonTopleft, sidePanelSize.x, false, false);
			#endif

			MenuHelper.DrawReservedMenuPanel(sidePanelID, Seb.Vis.UI.UI.GetCurrentBoundsScope());

			// ---- Handle button inputs ----
			#if UNITY_ANDROID || UNITY_IOS
			// Mobile: Individual button handlers
			if (copyAll) CopyAll();
			else if (pasteAll) PasteAll();
			else if (clearAll) ClearAll();
			else if (fillZeros) FillWithZeros();
			else if (fillOnes) FillWithOnes();
			#else
			// PC: Paired button handlers
			if (copyPasteButtonIndex == 0) CopyAll();
			else if (copyPasteButtonIndex == 1) PasteAll();
			else if (clearAll) ClearAll();
			else if (presetButtonIndex == 0) FillWithZeros();
			else if (presetButtonIndex == 1) FillWithOnes();
			#endif

				if (result == MenuHelper.CancelConfirmResult.Cancel || KeyboardShortcuts.CancelShortcutTriggered)
				{
					// Reset selector and UI to original values if there was a pending change
					if (pendingPinConfiguration != -1)
					{
						Seb.Vis.UI.UI.GetWheelSelectorState(ID_PinConfiguration).index = pinConfiguration;
						UpdateGridPatternSize(); // Reset visual grouping to original
					}
					pendingPinConfiguration = -1; // Clear pending change
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				}
				else if (result == MenuHelper.CancelConfirmResult.Confirm)
				{
					// Apply pending pin configuration change if any
					if (pendingPinConfiguration != -1)
					{
						// Check if this change would delete output wires
						if (WouldDeleteOutputWires(pendingPinConfiguration))
						{
							// Show warning popup before proceeding
							ShowWireDeletionWarning(pendingPinConfiguration);
							return; // Don't close menu yet, wait for user decision
						}
						else
						{
							// No wires to delete, proceed directly
							HandlePinConfigurationChange(pendingPinConfiguration);
						}
					}
					
					// Only save ROM contents if chip wasn't replaced
					// (replacement already preserves the data, and simulation isn't ready yet)
					if (!chipWasReplaced)
					{
						SaveChangesToROM();
					}
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				}

				if (dataDisplayMode != modeNew)
				{
					ConvertDisplayData(dataDisplayMode, modeNew);
					dataDisplayMode = modeNew;
				}
			}
		}

		static void OnFieldLostFocus(int rowIndex)
		{
			if (rowIndex < 0) return;

			InputFieldState inputFieldOld = Seb.Vis.UI.UI.GetInputFieldState(IDS_inputRow[rowIndex]);
			inputFieldOld.SetText(AutoFormatInputString(inputFieldOld.text), focus: false);
		}

		static string AutoFormatInputString(string input)
		{
			// Try to parse string in current format
			if (!TryParseDisplayStringToUInt(input, dataDisplayMode, ActiveRomDataBitCount, out uint uintValue))
			{
				// If failed to parse in current format, fall back to trying all possible formats
				// (for example, if player enters -1 in unsigned mode, we can recognize it as signed input and convert to unsigned automatically)
				foreach (DataDisplayMode fallbackMode in allDisplayModes)
				{
					if (TryParseDisplayStringToUInt(input, fallbackMode, ActiveRomDataBitCount, out uint fallbackUIntValue))
					{
						uintValue = fallbackUIntValue;
						break;
					}
				}
			}

			return SafeUIntToDisplayString(uintValue, dataDisplayMode, ActiveRomDataBitCount);
		}

		static void CopyAll()
		{
			if (dataDisplayMode == DataDisplayMode.Graphical)
			{
				// In graphical mode, copy the selected row data
				if (selectedRowIndex >= 0 && selectedRowIndex < RowCount)
				{
					copiedRowData = romChip.InternalData[selectedRowIndex];
				}
			}
			else
			{
				// In text modes, copy all rows as before
				StringBuilder sb = new();
				for (int i = 0; i < IDS_inputRow.Length; i++)
				{
					InputFieldState state = Seb.Vis.UI.UI.GetInputFieldState(IDS_inputRow[i]);
					sb.AppendLine(state.text);
				}
				InputHelper.CopyToClipboard(sb.ToString());
			}
		}

		static void PasteAll()
		{
			if (dataDisplayMode == DataDisplayMode.Graphical)
			{
				// In graphical mode, paste copied data to selected row
				if (selectedRowIndex >= 0 && selectedRowIndex < RowCount)
				{
					romChip.InternalData[selectedRowIndex] = copiedRowData;
				}
			}
			else
			{
				// In text modes, paste all rows as before
				string[] pasteStrings = StringHelper.SplitByLine(InputHelper.GetClipboardContents());
				for (int i = 0; i < Mathf.Min(IDS_inputRow.Length, pasteStrings.Length); i++)
				{
					string pasteString = AutoFormatInputString(pasteStrings[i]);
					InputFieldState state = Seb.Vis.UI.UI.GetInputFieldState(IDS_inputRow[i]);
					state.SetText(pasteString, state.focused);
				}
			}
		}

		static void ClearAll()
		{
			for (int i = 0; i < IDS_inputRow.Length; i++)
			{
				InputFieldState state = Seb.Vis.UI.UI.GetInputFieldState(IDS_inputRow[i]);
				string zeroValue = SafeUIntToDisplayString(0, dataDisplayMode, ActiveRomDataBitCount);
				state.SetText(zeroValue, state.focused);
			}
		}


		static void FillWithZeros()
		{
			if (dataDisplayMode == DataDisplayMode.Graphical)
			{
				// In graphical mode, fill the selected row with zeros
				if (selectedRowIndex >= 0 && selectedRowIndex < RowCount)
				{
					romChip.InternalData[selectedRowIndex] = 0;
				}
			}
			else
			{
				// In text modes, fill all rows as before
				for (int i = 0; i < IDS_inputRow.Length; i++)
				{
					InputFieldState state = Seb.Vis.UI.UI.GetInputFieldState(IDS_inputRow[i]);
					string zeroValue = SafeUIntToDisplayString(0, dataDisplayMode, ActiveRomDataBitCount);
					state.SetText(zeroValue, state.focused);
				}
			}
		}

		static void FillWithOnes()
		{
			uint maxValue = (1u << ActiveRomDataBitCount) - 1;
			
			if (dataDisplayMode == DataDisplayMode.Graphical)
			{
				// In graphical mode, fill the selected row with ones
				if (selectedRowIndex >= 0 && selectedRowIndex < RowCount)
				{
					romChip.InternalData[selectedRowIndex] = maxValue;
				}
			}
			else
			{
				// In text modes, fill all rows as before
				for (int i = 0; i < IDS_inputRow.Length; i++)
				{
					InputFieldState state = Seb.Vis.UI.UI.GetInputFieldState(IDS_inputRow[i]);
					string onesValue = SafeUIntToDisplayString(maxValue, dataDisplayMode, ActiveRomDataBitCount);
					state.SetText(onesValue, state.focused);
				}
			}
		}

		static void ConvertDisplayData(DataDisplayMode modeCurr, DataDisplayMode modeNew)
		{
			for (int i = 0; i < IDS_inputRow.Length; i++)
			{
				InputFieldState state = Seb.Vis.UI.UI.GetInputFieldState(IDS_inputRow[i]);
				uint uintValue;
				
				// If coming from graphical mode, use the ROM data directly
				if (modeCurr == DataDisplayMode.Graphical)
				{
					uintValue = romChip.InternalData[i];
				}
				else
				{
					// Parse from input field text for other modes
					TryParseDisplayStringToUInt(state.text, modeCurr, ActiveRomDataBitCount, out uintValue);
					// Update the actual ROM data to keep it in sync
					romChip.InternalData[i] = uintValue;
				}
				
				// Update the input field display
				state.SetText(SafeUIntToDisplayString(uintValue, modeNew, ActiveRomDataBitCount), false);
			}
		}

		static bool ValidateInputString(string text)
		{
			if (string.IsNullOrEmpty(text)) return true;
			if (text.Length > 34) return false;

			foreach (char c in text)
			{
				if (c == ' ') continue; //ignore white space

				// If in binary mode, only 0s or 1s allowed
				if (dataDisplayMode == DataDisplayMode.Binary && c is not ('0' or '1')) return false;

				// If in graphical mode, allow dots and HTML color tags
				if (dataDisplayMode == DataDisplayMode.Graphical)
				{
					if (c == '●' || c == '<' || c == '>' || c == '/' || c == '=' || c == '#' || char.IsLetterOrDigit(c))
						continue;
					return false;
				}

				if (c == '-') continue; // allow negative sign (even in unsigned field as we'll do automatic conversion)
				if (dataDisplayMode == DataDisplayMode.HEX && Uri.IsHexDigit(c)) continue;
				if (!char.IsDigit(c)) return false;
			}

			return true;
		}

		// Convert from uint to display string with given display mode
		static string UIntToDisplayString(uint raw, DataDisplayMode displayFormat, int bitCount)
		{
			return displayFormat switch
			{
				DataDisplayMode.Binary => Convert.ToString(raw, 2).PadLeft(bitCount, '0'),
				DataDisplayMode.DecimalSigned => Maths.TwosComplement(raw, bitCount) + "",
				DataDisplayMode.DecimalUnsigned => raw + "",
				DataDisplayMode.HEX => raw.ToString("X").PadLeft(bitCount / 4, '0'),
				DataDisplayMode.Graphical => UIntToGraphicalString(raw, bitCount),
				_ => throw new NotImplementedException("Unsupported display format: " + displayFormat)
			};
		}

		// Convert string with given format to uint
		static uint DisplayStringToUInt(string displayString, DataDisplayMode stringFormat, int bitCount)
		{
			displayString = displayString.Replace(" ", string.Empty);
			uint uintVal;

			switch (stringFormat)
			{
				case DataDisplayMode.Binary:
					uintVal = Convert.ToUInt32(displayString, 2);
					break;
				case DataDisplayMode.DecimalSigned:
				{
					int signedValue = int.Parse(displayString);
					uint unsignedRange = 1u << bitCount;
					if (signedValue < 0)
					{
						uintVal = (uint)(signedValue + unsignedRange);
					}
					else
					{
						uintVal = (uint)signedValue;
					}

					break;
				}
				case DataDisplayMode.DecimalUnsigned:
					uintVal = uint.Parse(displayString);
					break;
				case DataDisplayMode.HEX:
					int value = Convert.ToInt32(displayString, 16);
					uintVal = (uint)value;
					break;
				case DataDisplayMode.Graphical:
					uintVal = GraphicalStringToUInt(displayString, bitCount);
					break;
				default:
					throw new NotImplementedException("Unsupported display format: " + stringFormat);
			}

			return uintVal;
		}

		static bool TryParseDisplayStringToUInt(string displayString, DataDisplayMode stringFormat, int bitCount, out uint raw)
		{
			try
			{
				raw = DisplayStringToUInt(displayString, stringFormat, bitCount);
				uint maxVal = (1u << bitCount) - 1;

				// If value is too large to fit in given bit-count, clamp the result and return failure
				// (note: maybe makes more sense to wrap the result, but I think it's more obvious to player what happened if it just clamps)
				if (raw > maxVal)
				{
					raw = maxVal;
					return false;
				}

				return true;
			}
			catch (Exception)
			{
				raw = 0;
				return false;
			}
		}

		/// <summary>
		/// Converts a uint value to a graphical string representation using colored dots
		/// </summary>
		static string UIntToGraphicalString(uint raw, int bitCount)
		{
			try
			{
				string binaryString = Convert.ToString(raw, 2).PadLeft(bitCount, '0');
				return BinaryToColoredDots(binaryString);
			}
			catch
			{
				// Fallback to binary if graphical mode fails
				return Convert.ToString(raw, 2).PadLeft(bitCount, '0');
			}
		}

		/// <summary>
		/// Converts a graphical string representation back to uint value
		/// </summary>
		static uint GraphicalStringToUInt(string graphicalString, int bitCount)
		{
			if (string.IsNullOrEmpty(graphicalString)) return 0;
			
			// More robust parsing of graphical strings
			string binaryString = "";
			int index = 0;
			
			while (index < graphicalString.Length)
			{
				// Look for color tags
				if (index < graphicalString.Length - 1 && graphicalString.Substring(index, 2) == "<c")
				{
					// Find the end of the color tag
					int tagEnd = graphicalString.IndexOf(">", index);
					if (tagEnd != -1)
					{
						// Extract the color value to determine if it's high or low
						string colorTag = graphicalString.Substring(index, tagEnd - index + 1);
						index = tagEnd + 1;
						
						// Look for the dot after the color tag
						if (index < graphicalString.Length && graphicalString[index] == '●')
						{
							// Check if it's a high (red) or low (dark red) dot
							if (colorTag.Contains("#f24d4f"))
							{
								binaryString += "1";
							}
							else if (colorTag.Contains("#331a1a"))
							{
								binaryString += "0";
							}
							index++;
						}
						
						// Skip closing color tag
						if (index < graphicalString.Length - 7 && graphicalString.Substring(index, 8) == "</color>")
						{
							index += 8;
						}
					}
					else
					{
						index++;
					}
				}
				else if (graphicalString[index] == '●')
				{
					// Default to low if no color context
					binaryString += "0";
					index++;
				}
				else
				{
					index++;
				}
			}
			
			// If we couldn't parse dots, try to parse as regular binary
			if (string.IsNullOrEmpty(binaryString))
			{
				string cleanString = System.Text.RegularExpressions.Regex.Replace(graphicalString, @"<[^>]*>", "");
				binaryString = cleanString;
			}
			
			try
			{
				return Convert.ToUInt32(binaryString, 2);
			}
			catch
			{
				return 0; // Fallback to 0 if parsing fails
			}
		}

		/// <summary>
		/// Converts a binary string (e.g., "1010") to colored dot symbols using the game's state colors
		/// </summary>
		static string BinaryToColoredDots(string binary)
		{
			try
			{
				if (string.IsNullOrEmpty(binary)) return "-";
				
				// Use the game's state colors - index 0 is red for high, dark red for low
				string result = "";
				foreach (char bit in binary)
				{
					if (bit == '1')
					{
						// High state - use red color (index 0 from StateHighCol)
						result += "<color=#f24d4f>●</color>";
					}
					else if (bit == '0')
					{
						// Low state - use dark red color (index 0 from StateLowCol)  
						result += "<color=#331a1a>●</color>";
					}
					else
					{
						// Non-binary character, keep as-is
						result += bit;
					}
				}
				return result;
			}
			catch
			{
				// Fallback to plain binary string if HTML generation fails
				return binary ?? "-";
			}
		}

		/// <summary>
		/// Safely converts a value to display string, handling potential HTML parsing issues
		/// </summary>
		static string SafeUIntToDisplayString(uint raw, DataDisplayMode displayFormat, int bitCount)
		{
			try
			{
				return UIntToDisplayString(raw, displayFormat, bitCount);
			}
			catch
			{
				// Fallback to binary if graphical mode fails
				if (displayFormat == DataDisplayMode.Graphical)
				{
					return Convert.ToString(raw, 2).PadLeft(bitCount, '0');
				}
				return raw.ToString();
			}
		}
	
		static void SaveChangesToROM()
		{
			// In graphical mode, the ROM data is already updated directly by ToggleBit()
			// So we don't need to read from input fields
			if (dataDisplayMode != DataDisplayMode.Graphical)
			{
				for (int i = 0; i < RowCount; i++)
				{
					string displayString = Seb.Vis.UI.UI.GetInputFieldState(IDS_inputRow[i]).text;
					TryParseDisplayStringToUInt(displayString, dataDisplayMode, ActiveRomDataBitCount, out uint newValue);
					romChip.InternalData[i] = newValue;
				}
			}
	
			Project.ActiveProject.NotifyRomContentsEdited(romChip);
		}

		static void DrawScrollEntry(Vector2 topLeft, float width, int index, bool isLayoutPass)
		{
			Vector2 panelSize = new(width, height);
			Bounds2D entryBounds = Bounds2D.CreateFromTopLeftAndSize(topLeft, panelSize);

			if (entryBounds.Overlaps(scrollViewBounds) && !isLayoutPass) // don't bother with draw stuff if outside of scroll view / in layout pass
			{
				// Alternating colour for each row
				Color col = index % 2 == 0 ? ColHelper.MakeCol(0.17f) : ColHelper.MakeCol(0.13f);
				
				if (dataDisplayMode == DataDisplayMode.Graphical)
				{
					DrawGraphicalRow(topLeft, panelSize, index, col, isLayoutPass);
				}
				else
				{
					DrawTextRow(topLeft, panelSize, index, col, isLayoutPass);
				}

				// Draw line index
				DrawRowNumber(entryBounds, index, isLayoutPass);
			}

			// Set bounding box of scroll list element 
			Seb.Vis.UI.UI.OverridePreviousBounds(entryBounds);
		}

		static void DrawRowNumber(Bounds2D entryBounds, int index, bool isLayoutPass)
		{
			if (dataDisplayMode == DataDisplayMode.Graphical)
			{
				// In graphical mode, make row numbers clickable buttons
				bool isSelected = selectedRowIndex == index;
				bool isFocused = focusedRowIndex == index;
				
				// Create button theme for row selection
				ButtonTheme rowButtonTheme = MenuHelper.Theme.ButtonTheme;
				if (isSelected)
				{
					rowButtonTheme.buttonCols.normal = new Color(0.2f, 0.2f, 0.2f, 0.0f); // Dark background
					rowButtonTheme.buttonCols.hover = new Color(0.3f, 0.3f, 0.3f, 0.9f);
					rowButtonTheme.buttonCols.pressed = new Color(0.15f, 0.15f, 0.15f, 0.9f);
				}
				else
				{
					rowButtonTheme.buttonCols.normal = new Color(0.2f, 0.2f, 0.2f, 0.0f); // Dark background
					rowButtonTheme.buttonCols.hover = new Color(0.3f, 0.3f, 0.3f, 0.9f);
					rowButtonTheme.buttonCols.pressed = new Color(0.15f, 0.15f, 0.15f, 0.9f);
				}
				
				rowButtonTheme.textCols.normal = isFocused ? new Color(0.53f, 0.8f, 0.57f) : ColHelper.MakeCol(0.32f);
				rowButtonTheme.textCols.hover = new Color(0.6f, 0.9f, 0.6f);
				rowButtonTheme.textCols.pressed = new Color(0.4f, 0.7f, 0.4f);
				rowButtonTheme.font = MenuHelper.Theme.FontBold;
				rowButtonTheme.fontSize = MenuHelper.Theme.FontSizeRegular;

				#if UNITY_ANDROID || UNITY_IOS
				Vector2 buttonSize = new Vector2(5f, entryBounds.Size.y); // Wider to accommodate "000:" format
				#else
				Vector2 buttonSize = new Vector2(3.9f, entryBounds.Size.y); // Wider to accommodate "000:" format
				#endif
				bool pressed = Seb.Vis.UI.UI.Button(
					rowNumberStrings[index],
					rowButtonTheme,
					entryBounds.CentreLeft,
					buttonSize,
					true, // enabled
					false, // fitTextX
					false, // fitTextY
					rowButtonTheme.buttonCols,
					Anchor.CentreLeft,
					leftAlignText: true,
					textOffsetX: textPad
				);
				
				if (pressed)
				{
					if(selectedRowIndex == index)
                    {
						selectedRowIndex = -1;
						focusedRowIndex = -1;
                    }
                    else
                    {
						selectedRowIndex = index;
						focusedRowIndex = index;
                    }
				}
			}
			else
			{
				// In text modes, draw as regular text
				bool isFocused = focusedRowIndex == index;
				Color lineNumCol = isFocused ? new Color(0.53f, 0.8f, 0.57f) : ColHelper.MakeCol(0.32f);
				Seb.Vis.UI.UI.DrawText(rowNumberStrings[index], MenuHelper.Theme.FontBold, MenuHelper.Theme.FontSizeRegular, entryBounds.CentreLeft + Vector2.right * textPad, Anchor.TextCentreLeft, lineNumCol);
			}
		}

		static void DrawGraphicalRow(Vector2 topLeft, Vector2 panelSize, int index, Color bgCol, bool isLayoutPass)
		{
			// Use selection highlighting if this row is selected
			if (selectedRowIndex == index)
			{
				bgCol = new Color(0.33f, 0.55f, 0.34f); // Green selection background
			}
			
			// Draw background
			Seb.Vis.UI.UI.DrawPanel(topLeft, panelSize, bgCol, Anchor.TopLeft);

			// Calculate layout - leave space for row text on the left

			#if UNITY_ANDROID || UNITY_IOS
			float rowTextWidth = 5.1f; 
			float buttonSize = height; 
			float buttonHeight = height; // Keep height for touch targets
			#else
			float rowTextWidth = 5.1f; 
			float buttonSize = height; 
			float buttonHeight = height; // Keep height for touch targets
			//float rowTextWidth = 4f; 
			//float buttonSize = 1.8f; // Smaller button width
			//float buttonHeight = 2.5f; // Keep height for touch targets
			#endif
			float buttonSpacing = 0.15f; // Tighter spacing
			float totalButtonWidth = (ActiveRomDataBitCount * buttonSize) + ((ActiveRomDataBitCount - 1) * buttonSpacing);
			float availableWidth = panelSize.x - rowTextWidth - 1.0f; // Leave margin
			
			// Start buttons after the row text area
			float startX = topLeft.x + rowTextWidth + 0.5f;
			float startY = topLeft.y + (panelSize.y - buttonHeight) * 0.5f; // Center vertically

			// Get current value for this row
			uint currentValue = romChip.InternalData[index];

			// Draw individual bit buttons
			for (int bit = 0; bit < ActiveRomDataBitCount; bit++)
			{
				// Calculate bit value - most significant bit first (left to right)
				// This matches the standard binary representation where leftmost bit is MSB
				bool bitValue = ((currentValue >> (ActiveRomDataBitCount - 1 - bit)) & 1) == 1;
				
				Vector2 buttonPos = new Vector2(startX + bit * (buttonSize), startY);
				UIHandle buttonID = IDS_bitButtons[index][bit];

				// Create alternating background colors for grid effect
				// Use both row index and bit index to create a checkerboard pattern
				// gridPatternSize determines how many bits are grouped together
				bool isAlternate = (index + bit / gridPatternSize) % 2 == 0;
				Color buttonBgCol = isAlternate ? ColHelper.MakeCol(0.17f) : ColHelper.MakeCol(0.13f);
				
				// Create button theme with alternating background but colored text based on bit value
				ButtonTheme buttonTheme = bitValue ? CreateHighBitTheme() : CreateLowBitTheme();
				buttonTheme.buttonCols.normal = buttonBgCol; // Override with alternating color
				
				bool pressed = Seb.Vis.UI.UI.Button(
					"●", // Plain dot symbol
					buttonTheme,
					buttonPos,
					new Vector2(buttonSize, buttonHeight),
					true, // enabled
					false, // fitTextX
					false, // fitTextY
					buttonTheme.buttonCols,
					Anchor.TopLeft
				);

				// Handle button press
				if (pressed)
				{
					ToggleBit(index, bit);
				}
			}
		}

		static void DrawTextRow(Vector2 topLeft, Vector2 panelSize, int index, Color bgCol, bool isLayoutPass)
		{
			UIHandle inputFieldID = IDS_inputRow[index];
			InputFieldState inputFieldState = Seb.Vis.UI.UI.GetInputFieldState(inputFieldID);

			// Highlight row if it has focus
			if (inputFieldState.focused)
			{
				if (focusedRowIndex != index)
				{
					OnFieldLostFocus(focusedRowIndex);
					focusedRowIndex = index;
				}
				bgCol = new Color(0.33f, 0.55f, 0.34f);
			}

			InputFieldTheme inputTheme = MenuHelper.Theme.ChipNameInputField;
			inputTheme.fontSize = MenuHelper.Theme.FontSizeRegular;
			inputTheme.bgCol = bgCol;
			inputTheme.focusBorderCol = Color.clear;
			#if UNITY_ANDROID || UNITY_IOS
			float rowTextWidth = 7.1f; 
			#else
			float rowTextWidth = 5f; 
			#endif
			Seb.Vis.UI.UI.InputField(inputFieldID, inputTheme, topLeft, panelSize, "0", Anchor.TopLeft, rowTextWidth, inputStringValidator);
		}

		static void ToggleBit(int rowIndex, int bitIndex)
		{
			if (rowIndex < 0 || rowIndex >= RowCount || bitIndex < 0 || bitIndex >= ActiveRomDataBitCount)
				return;

			uint currentValue = romChip.InternalData[rowIndex];
			// Toggle the bit at the same position as displayed (MSB first)
			// bitIndex 0 = leftmost bit (MSB), bitIndex 15 = rightmost bit (LSB)
			uint bitMask = 1u << (ActiveRomDataBitCount - 1 - bitIndex);
			uint newValue = currentValue ^ bitMask; // Toggle the bit
			
			romChip.InternalData[rowIndex] = newValue;
			
			// Update the display immediately
			UpdateRowDisplay(rowIndex);
		}

		static void UpdateRowDisplay(int rowIndex)
		{
			// This method will be called when we need to refresh the display after a bit toggle
			// For now, we'll rely on the next frame's draw to show the updated value
		}

		static ButtonTheme CreateHighBitTheme()
		{
			// Theme for high bits (1) - red dot, neutral background
			ButtonTheme theme = MenuHelper.Theme.ButtonTheme;
			theme.buttonCols.normal = new Color(0.0f, 0.0f, 0.0f, 0.0f); // Transparent background
			theme.buttonCols.hover = new Color(0.4f, 0.4f, 0.4f, 0.3f); // Light gray on hover
			theme.buttonCols.pressed = new Color(0.6f, 0.6f, 0.6f, 0.4f); // Darker gray when pressed
			theme.textCols.normal = new Color(0.95f, 0.3f, 0.31f); // Red text for high bits
			theme.textCols.hover = new Color(1.0f, 0.4f, 0.4f);
			theme.textCols.pressed = new Color(0.8f, 0.2f, 0.2f);
			theme.fontSize = height*1.3f;
			return theme;
		}

		static ButtonTheme CreateLowBitTheme()
		{
			// Theme for low bits (0) - dark red dot, neutral background
			ButtonTheme theme = MenuHelper.Theme.ButtonTheme;
			theme.buttonCols.normal = new Color(0.0f, 0.0f, 0.0f, 0.0f); // Transparent background
			theme.buttonCols.hover = new Color(0.4f, 0.4f, 0.4f, 0.3f); // Light gray on hover
			theme.buttonCols.pressed = new Color(0.6f, 0.6f, 0.6f, 0.4f); // Darker gray when pressed
			theme.textCols.normal = new Color(0.2f, 0.1f, 0.1f); // Dark red text for low bits
			theme.textCols.hover = new Color(0.3f, 0.15f, 0.15f);
			theme.textCols.pressed = new Color(0.15f, 0.08f, 0.08f);
			theme.fontSize = height*1.3f;
			return theme;
		}

		public static void OnMenuOpened()
		{
			romChip = (SubChipInstance)ContextMenu.interactionContext;
			RowCount = romChip.InternalData.Length;
			ActiveRomDataBitCount = 16; //

		ID_PinConfiguration = new UIHandle("ROM_PinConfiguration", romChip.ID);
		ID_scrollbar = new UIHandle("ROM_EditScrollbar", romChip.ID);
		ID_DataDisplayMode = new UIHandle("ROM_DataDisplayMode", romChip.ID);
		
		// Initialize pin configuration based on current chip type
		pinConfiguration = GetStoredPinConfiguration();
		UpdateGridPatternSize(); // Set initial visual grouping
		pendingPinConfiguration = -1; // No pending changes initially
		chipWasReplaced = false; // Reset replacement flag

		allDisplayModes = (DataDisplayMode[])Enum.GetValues(typeof(DataDisplayMode));
		focusedRowIndex = -1; // No row focused initially
		IDS_inputRow = new UIHandle[RowCount];
		IDS_bitButtons = new UIHandle[RowCount][];
		IDS_rowSelectButtons = new UIHandle[RowCount];
		for (int i = 0; i < RowCount; i++)
		{
			IDS_bitButtons[i] = new UIHandle[ActiveRomDataBitCount];
			for (int bit = 0; bit < ActiveRomDataBitCount; bit++)
			{
				IDS_bitButtons[i][bit] = new UIHandle($"ROM_bitButton_{i}_{bit}");
			}
			IDS_rowSelectButtons[i] = new UIHandle($"ROM_rowSelect_{i}");
		}
		selectedRowIndex = -1; // Reset selection
		rowNumberStrings = new string[RowCount];
		
		// Set Graphical as the default display mode (index 4)
		Seb.Vis.UI.UI.GetWheelSelectorState(ID_DataDisplayMode).index = (int)DataDisplayMode.Graphical;
		dataDisplayMode = (DataDisplayMode)Seb.Vis.UI.UI.GetWheelSelectorState(ID_DataDisplayMode).index;

			// Always use 3-digit formatting for consistency across all modes
			int lineNumberPadLength = 3;

			for (int i = 0; i < IDS_inputRow.Length; i++)
			{
				IDS_inputRow[i] = new UIHandle("ROM_rowInputField", i);
				InputFieldState state = Seb.Vis.UI.UI.GetInputFieldState(IDS_inputRow[i]);

				string displayString = SafeUIntToDisplayString(romChip.InternalData[i], dataDisplayMode, ActiveRomDataBitCount);
				state.SetText(displayString, i == focusedRowIndex);

				rowNumberStrings[i] = (i + ":").PadLeft(lineNumberPadLength + 1, '0');
			}
		}

		public static void Reset()
		{
			//dataDisplayModeIndex = 0;
		}

		/// <summary>
		/// Gets the current pin configuration index based on the ROM chip type
		/// </summary>
		static int GetStoredPinConfiguration()
		{
			return romChip.ChipType switch
			{
				ChipType.Rom_2x8 => 0,    // 2x8-bit
				ChipType.Rom_1x16 => 1,   // 1x16-bit
				ChipType.Rom_16x1 => 2,   // 16x1-bit
				ChipType.Rom_4x4 => 3,    // 4x4-bit
				_ => 0 // Default to 2x8-bit for Rom_256x16
			};
		}

		/// <summary>
		/// Updates the grid pattern size based on the current pin configuration
		/// </summary>
		static void UpdateGridPatternSize()
		{
			gridPatternSize = pinConfiguration switch
			{
				0 => 8,  // 2x8-bit: group by 8 bits
				1 => 16, // 1x16-bit: group by 16 bits (single group)
				2 => 1,  // 16x1-bit: group by 1 bit
				3 => 4,  // 4x4-bit: group by 4 bits
				_ => 8   // Default
			};
		}

		/// <summary>
		/// Updates the grid pattern size for preview (without changing actual pin configuration)
		/// </summary>
		static void UpdateGridPatternSizeForPreview(int previewPinConfig)
		{
			gridPatternSize = previewPinConfig switch
			{
				0 => 8,  // 2x8-bit: group by 8 bits
				1 => 16, // 1x16-bit: group by 16 bits (single group)
				2 => 1,  // 16x1-bit: group by 1 bit
				3 => 4,  // 4x4-bit: group by 4 bits
				_ => 8   // Default
			};
		}

		/// <summary>
		/// Handles pin configuration changes by replacing the ROM chip
		/// </summary>
		static void HandlePinConfigurationChange(int newPinConfig)
		{
			if (newPinConfig == pinConfiguration) return; // No change needed

			// Determine the new chip type
			ChipType newChipType = newPinConfig switch
			{
				0 => ChipType.Rom_2x8,    // 2x8-bit
				1 => ChipType.Rom_1x16,   // 1x16-bit
				2 => ChipType.Rom_16x1,   // 16x1-bit
				3 => ChipType.Rom_4x4,    // 4x4-bit
				_ => ChipType.Rom_2x8     // Default
			};

			// Only replace chip if the type actually changes
			if (romChip.ChipType != newChipType)
			{
				ReplaceRomChip(newChipType);
				chipWasReplaced = true; // Mark that chip was replaced
			}
			else
			{
				// Same chip type, just update configuration
				pinConfiguration = newPinConfig;
				UpdateGridPatternSize();
			}
		}

		/// <summary>
		/// Replaces the current ROM chip with a new one of the specified type
		/// </summary>
		static void ReplaceRomChip(ChipType newChipType)
		{
			var project = Project.ActiveProject;
			var devChip = project.ViewedChip;

			// Preserve ROM data, position, label, and ID
			uint[] internalData = romChip.InternalData;
			Vector2 position = romChip.Position;
			string label = romChip.Label;
			int chipID = romChip.ID;

			// Delete output wire connections (preserve input connections)
			DeleteOutputWireConnections(devChip, romChip);

			// Remove the old chip
			devChip.DeleteSubChip(romChip);

			// Get the new chip description (lookup by name)
			string newChipName = ChipTypeHelper.GetName(newChipType);
			if (!project.chipLibrary.TryGetChipDescription(newChipName, out var newChipDescription))
			{
				Debug.LogError($"Could not find chip description for {newChipType} (name: {newChipName})");
				return;
			}

			// Create new chip with preserved properties
			var newSubChipDesc = new SubChipDescription(
				newChipName,
				chipID,
				label,
				position,
				romChip.InitialSubChipDesc.OutputPinColourInfo,
				internalData
			);

			// Add the new chip
			var newRomChip = new SubChipInstance(newChipDescription, newSubChipDesc);
			devChip.AddNewSubChip(newRomChip, false);

			// Update our reference
			romChip = newRomChip;

			// Update configuration
			pinConfiguration = GetStoredPinConfiguration();
			UpdateGridPatternSize();

			// Update the wheel selector index
			Seb.Vis.UI.UI.GetWheelSelectorState(ID_PinConfiguration).index = pinConfiguration;
		}

		/// <summary>
		/// Deletes only output wire connections from a chip, preserving input connections
		/// </summary>
		static void DeleteOutputWireConnections(DevChipInstance devChip, SubChipInstance chip)
		{
			// Create a list to avoid modifying collection while iterating
			var wiresToDelete = new System.Collections.Generic.List<WireInstance>();

			foreach (var wire in devChip.Wires)
			{
				// Check if this wire is connected to an output pin of our chip
				bool isConnectedToOutput = (wire.SourcePin.parent == chip && wire.SourcePin.IsSourcePin) ||
										  (wire.TargetPin.parent == chip && wire.TargetPin.IsSourcePin);

				if (isConnectedToOutput)
				{
					wiresToDelete.Add(wire);
				}
			}

			// Delete the wires
			foreach (var wire in wiresToDelete)
			{
				devChip.DeleteWire(wire);
			}
		}

		/// <summary>
		/// Checks if changing to a new pin configuration would delete output wires
		/// </summary>
		static bool WouldDeleteOutputWires(int newPinConfig)
		{
			// Determine the new chip type
			ChipType newChipType = newPinConfig switch
			{
				0 => ChipType.Rom_2x8,    // 2x8-bit
				1 => ChipType.Rom_1x16,   // 1x16-bit
				2 => ChipType.Rom_16x1,   // 16x1-bit
				3 => ChipType.Rom_4x4,    // 4x4-bit
				_ => ChipType.Rom_2x8     // Default
			};

			// If chip type doesn't change, no wires will be deleted
			if (romChip.ChipType == newChipType) return false;

			// Check if there are any output wires connected to this ROM chip
			var devChip = Project.ActiveProject.ViewedChip;
			foreach (var wire in devChip.Wires)
			{
				bool isConnectedToOutput = (wire.SourcePin.parent == romChip && wire.SourcePin.IsSourcePin) ||
										  (wire.TargetPin.parent == romChip && wire.TargetPin.IsSourcePin);

				if (isConnectedToOutput) return true;
			}

			return false;
		}

		/// <summary>
		/// Shows the wire deletion warning popup
		/// </summary>
		static void ShowWireDeletionWarning(int newPinConfig)
		{
			showingWireDeletionWarning = true;
			warningPendingConfig = newPinConfig;
		}

		/// <summary>
		/// Draws the wire deletion warning popup
		/// </summary>
		static void DrawWireDeletionWarningPopup()
		{
			MenuHelper.DrawBackgroundOverlay();

			// Draw panel (wider to accommodate longer text like "16x1-bit to 4x4-bit")
			var panelSize = new Vector2(Seb.Vis.UI.UI.Width * 0.55f, Seb.Vis.UI.UI.Height * 0.3f);
			var panelPos = Seb.Vis.UI.UI.Centre;
			var panelID = Seb.Vis.UI.UI.ReservePanel();

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				MenuHelper.DrawReservedMenuPanel(panelID, Bounds2D.CreateFromCentreAndSize(panelPos, panelSize));

				// Draw warning text
				string currentConfig = PinConfigurationOptions[pinConfiguration];
				string newConfig = PinConfigurationOptions[warningPendingConfig];
				string warningText = $"Changing from {currentConfig} to {newConfig}\nwill delete output wire connections.\n\nDo you want to continue?";

				var textPos = Seb.Vis.UI.UI.Centre + Vector2.up * 2;
				// Use a standard color from UI theme for text (no MenuHelper.Theme.TextCol exists)
				Seb.Vis.UI.UI.DrawText(warningText, MenuHelper.Theme.FontBold, MenuHelper.Theme.FontSizeRegular, textPos, Anchor.TextCentre, Color.white);

				// Draw buttons at the bottom
				var buttonWidth = Seb.Vis.UI.UI.PrevBounds.Width;
				var buttonTopLeft = Seb.Vis.UI.UI.PrevBounds.CentreBottom + Vector2.down * (DrawSettings.DefaultButtonSpacing * 2)+Vector2.left * buttonWidth/2;

				var buttonResult = MenuHelper.DrawButtonPair("KEEP CURRENT", "CONTINUE", buttonTopLeft, buttonWidth, false);

				if (buttonResult == 0) // KEEP CURRENT
				{
					showingWireDeletionWarning = false;
					warningPendingConfig = -1;
					
					// Reset selector to original value
					Seb.Vis.UI.UI.GetWheelSelectorState(ID_PinConfiguration).index = pinConfiguration;
					UpdateGridPatternSize(); // Reset visual grouping
					pendingPinConfiguration = -1;
				}
				else if (buttonResult == 1) // CONTINUE
				{
					showingWireDeletionWarning = false;
					
					// Proceed with the change
					HandlePinConfigurationChange(warningPendingConfig);
					
					// Only save ROM contents if chip wasn't replaced
					// (replacement already preserves the data, and simulation isn't ready yet)
					if (!chipWasReplaced)
					{
						SaveChangesToROM();
					}
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				}
			}
		}

	enum DataDisplayMode
	{
		DecimalUnsigned,
		DecimalSigned,
		Binary,
		HEX,
		Graphical
	}
	}
}
