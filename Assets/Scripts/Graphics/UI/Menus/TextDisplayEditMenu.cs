using System;
using System.Text;
using DLS.Description;
using DLS.Game;
using Seb.Helpers;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	public static class TextDisplayEditMenu
	{
	const int MaxStringLength = 20; // Maximum characters per string
	const int StringCount = 256; // 256 programmable strings (0-255)
	
	static UIHandle ID_scrollbar;
	static UIHandle ID_widthInput;
	static UIHandle ID_heightInput;
	static UIHandle ID_fontSizeInput;
	static int focusedRowIndex;
		static UIHandle[] IDS_inputRow;
		static string[] rowNumberStrings;
		static int selectedRowIndex = -1;
		static string copiedString = "";

		static SubChipInstance textDisplayChip;
		static string[] textStrings; // Decoded text strings from InternalData

		static readonly Func<string, bool> inputStringValidator = ValidateInputString;
	static readonly Func<string, bool> sizeInputValidator = ValidateSizeInput;
		static Bounds2D scrollViewBounds;

		static float textPad => 0.52f;
		static float height => 3.72f; // Larger touch targets for mobile
		static float leftAdjustmentOfScrollView => 12f;

		public static void DrawMenu()
		{
			MenuHelper.DrawBackgroundOverlay();

			// ---- Draw TextDisplay contents ----
			#if UNITY_ANDROID || UNITY_IOS
			scrollViewBounds = Bounds2D.CreateFromCentreAndSize(Seb.Vis.UI.UI.Centre + Vector2.left * leftAdjustmentOfScrollView, new Vector2(Seb.Vis.UI.UI.Width * 0.7f, Seb.Vis.UI.UI.Height * 0.8f));
			Seb.Vis.UI.UI.DrawPanel(Bounds2D.Grow(scrollViewBounds, 0.5f), ColHelper.MakeCol(0.23f));
			#else
			scrollViewBounds = Bounds2D.CreateFromCentreAndSize(Seb.Vis.UI.UI.Centre + Vector2.left * leftAdjustmentOfScrollView, new Vector2(Seb.Vis.UI.UI.Width * 0.68f, Seb.Vis.UI.UI.Height * 0.8f));
			Seb.Vis.UI.UI.DrawPanel(Bounds2D.Grow(scrollViewBounds, 0.5f), ColHelper.MakeCol(0.23f));
			#endif
			
			ScrollViewTheme scrollTheme = DrawSettings.ActiveUITheme.ScrollTheme;
			Seb.Vis.UI.UI.DrawScrollView(ID_scrollbar, scrollViewBounds.TopLeft, scrollViewBounds.Size, 0, Anchor.TopLeft, scrollTheme, DrawScrollEntry, StringCount);

			if (focusedRowIndex >= 0)
			{
				// Focus next/prev field with keyboard shortcuts
				bool changeLine = KeyboardShortcuts.ConfirmShortcutTriggered || InputHelper.IsKeyDownThisFrame(KeyCode.Tab);

				if (changeLine)
				{
					bool goPrevLine = InputHelper.ShiftIsHeld;
					int jumpToRowIndex = focusedRowIndex + (goPrevLine ? -1 : 1);

					if (jumpToRowIndex >= 0 && jumpToRowIndex < StringCount)
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
				const float buttonSpacing = 0.5f;
				#else
				const float buttonSpacing = 0.75f;
				#endif

				Vector2 buttonTopleft = sidePanelTopLeft;

				#if UNITY_ANDROID || UNITY_IOS
				// Mobile: Vertical layout - one button per row
				bool copyRow = Seb.Vis.UI.UI.Button("COPY ROW", MenuHelper.Theme.ButtonTheme, buttonTopleft, new Vector2(sidePanelSize.x, 0), true, false, true, MenuHelper.Theme.ButtonTheme.buttonCols, Anchor.TopLeft);
				buttonTopleft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * buttonSpacing;
				
				bool pasteRow = Seb.Vis.UI.UI.Button("PASTE ROW", MenuHelper.Theme.ButtonTheme, buttonTopleft, new Vector2(sidePanelSize.x, 0), true, false, true, MenuHelper.Theme.ButtonTheme.buttonCols, Anchor.TopLeft);
				buttonTopleft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * buttonSpacing;
				
				bool clearAll = Seb.Vis.UI.UI.Button("CLEAR ALL", MenuHelper.Theme.ButtonTheme, buttonTopleft, new Vector2(sidePanelSize.x, 0), true, false, true, MenuHelper.Theme.ButtonTheme.buttonCols, Anchor.TopLeft);
				buttonTopleft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * buttonSpacing;
				
				bool fillNumbers = Seb.Vis.UI.UI.Button("FILL #s", MenuHelper.Theme.ButtonTheme, buttonTopleft, new Vector2(sidePanelSize.x, 0), true, false, true, MenuHelper.Theme.ButtonTheme.buttonCols, Anchor.TopLeft);
				buttonTopleft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * buttonSpacing;

				MenuHelper.CancelConfirmResult result = MenuHelper.DrawCancelConfirmButtons(buttonTopleft, sidePanelSize.x, false, false);
				#else
				// PC: Paired layout
				int copyPasteButtonIndex = MenuHelper.DrawButtonPair("COPY ROW", "PASTE ROW", buttonTopleft, sidePanelSize.x, false);
				buttonTopleft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * buttonSpacing;
				
				bool clearAll = Seb.Vis.UI.UI.Button("CLEAR ALL", MenuHelper.Theme.ButtonTheme, buttonTopleft, new Vector2(sidePanelSize.x, 0), true, false, true, MenuHelper.Theme.ButtonTheme.buttonCols, Anchor.TopLeft);
				buttonTopleft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * buttonSpacing;
				
			bool fillNumbers = Seb.Vis.UI.UI.Button("FILL NUMBERS", MenuHelper.Theme.ButtonTheme, buttonTopleft, new Vector2(sidePanelSize.x, 0), true, false, true, MenuHelper.Theme.ButtonTheme.buttonCols, Anchor.TopLeft);
			buttonTopleft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * buttonSpacing;
			
			MenuHelper.CancelConfirmResult result = MenuHelper.DrawCancelConfirmButtons(buttonTopleft, sidePanelSize.x, false, false);
			
			// ---- Size Adjustment Panel (below cancel/confirm) ----
			buttonTopleft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * buttonSpacing;
			DrawSizeAdjustmentPanel(buttonTopleft, sidePanelSize.x, buttonSpacing);
			#endif

			MenuHelper.DrawReservedMenuPanel(sidePanelID, Seb.Vis.UI.UI.GetCurrentBoundsScope());

				// ---- Handle button inputs ----
				#if UNITY_ANDROID || UNITY_IOS
				if (copyRow) CopyRow();
				else if (pasteRow) PasteRow();
				else if (clearAll) ClearAll();
				else if (fillNumbers) FillNumbers();
				#else
				if (copyPasteButtonIndex == 0) CopyRow();
				else if (copyPasteButtonIndex == 1) PasteRow();
				else if (clearAll) ClearAll();
				else if (fillNumbers) FillNumbers();
				#endif

				if (result == MenuHelper.CancelConfirmResult.Cancel || KeyboardShortcuts.CancelShortcutTriggered)
				{
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				}
				else if (result == MenuHelper.CancelConfirmResult.Confirm)
				{
					SaveChangesToTextDisplay();
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				}
			}
		}

		static void OnFieldLostFocus(int rowIndex)
		{
			if (rowIndex < 0) return;

			InputFieldState inputFieldOld = Seb.Vis.UI.UI.GetInputFieldState(IDS_inputRow[rowIndex]);
			string text = inputFieldOld.text;
			
			// Truncate if too long
			if (text.Length > MaxStringLength)
			{
				text = text.Substring(0, MaxStringLength);
			}
			
			inputFieldOld.SetText(text, focus: false);
			textStrings[rowIndex] = text;
		}

		static void CopyRow()
		{
			if (selectedRowIndex >= 0 && selectedRowIndex < StringCount)
			{
				copiedString = textStrings[selectedRowIndex];
			}
			else if (focusedRowIndex >= 0 && focusedRowIndex < StringCount)
			{
				copiedString = textStrings[focusedRowIndex];
			}
		}

		static void PasteRow()
		{
			if (selectedRowIndex >= 0 && selectedRowIndex < StringCount)
			{
				textStrings[selectedRowIndex] = copiedString;
				InputFieldState state = Seb.Vis.UI.UI.GetInputFieldState(IDS_inputRow[selectedRowIndex]);
				state.SetText(copiedString, state.focused);
			}
			else if (focusedRowIndex >= 0 && focusedRowIndex < StringCount)
			{
				textStrings[focusedRowIndex] = copiedString;
				InputFieldState state = Seb.Vis.UI.UI.GetInputFieldState(IDS_inputRow[focusedRowIndex]);
				state.SetText(copiedString, state.focused);
			}
		}

		static void ClearAll()
		{
			for (int i = 0; i < StringCount; i++)
			{
				textStrings[i] = "";
				InputFieldState state = Seb.Vis.UI.UI.GetInputFieldState(IDS_inputRow[i]);
				state.SetText("", state.focused);
			}
		}

		static void FillNumbers()
		{
			for (int i = 0; i < StringCount; i++)
			{
				textStrings[i] = i.ToString();
				InputFieldState state = Seb.Vis.UI.UI.GetInputFieldState(IDS_inputRow[i]);
				state.SetText(i.ToString(), state.focused);
			}
		}

		static bool ValidateInputString(string text)
		{
			if (string.IsNullOrEmpty(text)) return true;
			if (text.Length > MaxStringLength) return false;

			// Only allow printable ASCII characters
			foreach (char c in text)
			{
				if (c < 32 || c > 126) return false;
			}

			return true;
		}

	static void SaveChangesToTextDisplay()
	{
		// Update text strings from input fields
		for (int i = 0; i < StringCount; i++)
		{
			string displayString = Seb.Vis.UI.UI.GetInputFieldState(IDS_inputRow[i]).text;
			if (displayString.Length > MaxStringLength)
			{
				displayString = displayString.Substring(0, MaxStringLength);
			}
			textStrings[i] = displayString;
		}

		// Encode strings into InternalData array
		EncodeStringsToInternalData();
		
		// DEBUG: Check what's in InternalData[0] after encoding
		if (textDisplayChip.InternalData != null && textDisplayChip.InternalData.Length > 0)
		{
			uint data0 = textDisplayChip.InternalData[0];
			uint fontSize = (data0 >> 24) & 0xFF;
			UnityEngine.Debug.Log($"[TextDisplay Save] InternalData[0]=0x{data0:X8}, FontSize={fontSize}%");
		}
		
		Project.ActiveProject.NotifyRomContentsEdited(textDisplayChip);
	}

		static void DrawScrollEntry(Vector2 topLeft, float width, int index, bool isLayoutPass)
		{
			Vector2 panelSize = new(width, height);
			Bounds2D entryBounds = Bounds2D.CreateFromTopLeftAndSize(topLeft, panelSize);

			if (entryBounds.Overlaps(scrollViewBounds) && !isLayoutPass)
			{
				// Alternating colour for each row
				Color col = index % 2 == 0 ? ColHelper.MakeCol(0.17f) : ColHelper.MakeCol(0.13f);
				
				DrawTextRow(topLeft, panelSize, index, col, isLayoutPass);

				// Draw line index
				DrawRowNumber(entryBounds, index, isLayoutPass);
			}

			// Set bounding box of scroll list element 
			Seb.Vis.UI.UI.OverridePreviousBounds(entryBounds);
		}

		static void DrawRowNumber(Bounds2D entryBounds, int index, bool isLayoutPass)
		{
			bool isFocused = focusedRowIndex == index;
			Color lineNumCol = isFocused ? new Color(0.53f, 0.8f, 0.57f) : ColHelper.MakeCol(0.32f);
			Seb.Vis.UI.UI.DrawText(rowNumberStrings[index], MenuHelper.Theme.FontBold, MenuHelper.Theme.FontSizeRegular, entryBounds.CentreLeft + Vector2.right * textPad, Anchor.TextCentreLeft, lineNumCol);
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
			
			Seb.Vis.UI.UI.InputField(inputFieldID, inputTheme, topLeft, panelSize, "", Anchor.TopLeft, rowTextWidth, inputStringValidator);
		}

		public static void OnMenuOpened()
		{
		textDisplayChip = (SubChipInstance)ContextMenu.interactionContext;
		
		ID_scrollbar = new UIHandle("TextDisplay_EditScrollbar", textDisplayChip.ID);
		ID_widthInput = new UIHandle("TextDisplay_WidthInput", textDisplayChip.ID);
		ID_heightInput = new UIHandle("TextDisplay_HeightInput", textDisplayChip.ID);
		ID_fontSizeInput = new UIHandle("TextDisplay_FontSizeInput", textDisplayChip.ID);
		
		focusedRowIndex = -1;
		IDS_inputRow = new UIHandle[StringCount];
		rowNumberStrings = new string[StringCount];
		textStrings = new string[StringCount];
		
		// Initialize size input fields
		int widthGrids = Mathf.RoundToInt(textDisplayChip.Description.Size.x / DrawSettings.GridSize);
		int heightGrids = Mathf.RoundToInt(textDisplayChip.Description.Size.y / DrawSettings.GridSize);
		Seb.Vis.UI.UI.GetInputFieldState(ID_widthInput).SetText(widthGrids.ToString());
		Seb.Vis.UI.UI.GetInputFieldState(ID_heightInput).SetText(heightGrids.ToString());
		
		// Initialize font size (stored in InternalData[0], default to 100%)
		int fontSizePercent = GetFontSizePercent();
		Seb.Vis.UI.UI.GetInputFieldState(ID_fontSizeInput).SetText(fontSizePercent.ToString());
		
		// Decode strings from InternalData
		DecodeStringsFromInternalData();

			// Always use 3-digit formatting for consistency
			int lineNumberPadLength = 3;

			for (int i = 0; i < StringCount; i++)
			{
				IDS_inputRow[i] = new UIHandle("TextDisplay_rowInputField", i);
				InputFieldState state = Seb.Vis.UI.UI.GetInputFieldState(IDS_inputRow[i]);

				state.SetText(textStrings[i], i == focusedRowIndex);
				rowNumberStrings[i] = (i + ":").PadLeft(lineNumberPadLength + 1, '0');
			}
		}

		public static void Reset()
		{
			// Reset state if needed
		}

		/// <summary>
		/// Decodes text strings from the chip's InternalData array.
		/// Each string is stored as: [length byte][char bytes...]
		/// Multiple strings are packed sequentially.
		/// </summary>
		static void DecodeStringsFromInternalData()
		{
			// Initialize with empty strings
			for (int i = 0; i < StringCount; i++)
			{
				textStrings[i] = "";
			}

			if (textDisplayChip.InternalData == null || textDisplayChip.InternalData.Length == 0)
			{
				return;
			}

		// Decode packed strings from InternalData
		// Format: Each uint contains up to 4 bytes (chars)
		// First byte of each string is its length
		// IMPORTANT: Byte 3 of InternalData[0] is reserved for font size - skip it!
		int dataIndex = 0;
		int byteOffset = 0; // 0-3 within current uint

		for (int stringIndex = 0; stringIndex < StringCount && dataIndex < textDisplayChip.InternalData.Length; stringIndex++)
		{
			// Read length byte (check for skip BEFORE reading)
			if (dataIndex >= textDisplayChip.InternalData.Length) break;
			
			// Skip byte 3 of the first uint (font size byte) BEFORE reading
			if (dataIndex == 0 && byteOffset == 3)
			{
				byteOffset = 0;
				dataIndex = 1;
				if (dataIndex >= textDisplayChip.InternalData.Length) break;
			}
			
			uint currentData = textDisplayChip.InternalData[dataIndex];
			byte length = (byte)((currentData >> (byteOffset * 8)) & 0xFF);
			
			byteOffset++;
			if (byteOffset >= 4) { byteOffset = 0; dataIndex++; }

			// Read string characters
			StringBuilder sb = new StringBuilder();
			for (int charIdx = 0; charIdx < length && charIdx < MaxStringLength; charIdx++)
			{
				if (dataIndex >= textDisplayChip.InternalData.Length) break;
				
				// Skip byte 3 of the first uint (font size byte) BEFORE reading
				if (dataIndex == 0 && byteOffset == 3)
				{
					byteOffset = 0;
					dataIndex = 1;
					if (dataIndex >= textDisplayChip.InternalData.Length) break;
				}
				
				currentData = textDisplayChip.InternalData[dataIndex];
				byte charByte = (byte)((currentData >> (byteOffset * 8)) & 0xFF);
				
				if (charByte >= 32 && charByte <= 126)
				{
					sb.Append((char)charByte);
				}
				
				byteOffset++;
				if (byteOffset >= 4) { byteOffset = 0; dataIndex++; }
			}
			
			textStrings[stringIndex] = sb.ToString();
		}
	}

	/// <summary>
	/// Encodes text strings into the chip's InternalData array.
	/// Format: Each string is stored as [length byte][char bytes...]
	/// Note: InternalData is readonly, so we can only modify its contents, not replace the array.
	/// </summary>
	static void EncodeStringsToInternalData()
	{
		if (textDisplayChip.InternalData == null || textDisplayChip.InternalData.Length == 0)
		{
			UnityEngine.Debug.LogError("TextDisplay InternalData is null or empty - cannot encode strings");
			return;
		}

		// Preserve the font size setting in InternalData[0] (high byte)
		uint fontSizePreserved = textDisplayChip.InternalData[0] & 0xFF000000;
		UnityEngine.Debug.Log($"[TextDisplay Encode] Before clear: InternalData[0]=0x{textDisplayChip.InternalData[0]:X8}, Preserved=0x{fontSizePreserved:X8}");

		// Clear the existing array by setting all elements to 0
		for (int i = 0; i < textDisplayChip.InternalData.Length; i++)
		{
			textDisplayChip.InternalData[i] = 0;
		}
		
		// Restore the font size setting
		textDisplayChip.InternalData[0] = fontSizePreserved;
		UnityEngine.Debug.Log($"[TextDisplay Encode] After restore: InternalData[0]=0x{textDisplayChip.InternalData[0]:X8}");

	// Pack strings into InternalData
	// IMPORTANT: Byte 3 of InternalData[0] is reserved for font size - skip it!
	int dataIndex = 0;
	int byteOffset = 0;
	uint currentData = fontSizePreserved; // Start with preserved font size!

	for (int stringIndex = 0; stringIndex < StringCount; stringIndex++)
	{
		string str = textStrings[stringIndex];
		byte length = (byte)Math.Min(str.Length, MaxStringLength);

		// Skip byte 3 of the first uint (font size byte) BEFORE writing
		if (dataIndex == 0 && byteOffset == 3)
		{
			textDisplayChip.InternalData[0] = currentData; // Save with font size preserved
			currentData = 0;
			byteOffset = 0;
			dataIndex = 1;
		}

		// Write length byte
		currentData |= ((uint)length) << (byteOffset * 8);
		byteOffset++;
		if (byteOffset >= 4)
		{
			textDisplayChip.InternalData[dataIndex] = currentData;
			currentData = 0; // Reset for next uint (no font size to preserve)
			byteOffset = 0;
			dataIndex++;
		}

		// Write string characters
		for (int charIdx = 0; charIdx < length; charIdx++)
		{
			if (dataIndex >= textDisplayChip.InternalData.Length) break;
			
			// Skip byte 3 of the first uint (font size byte) BEFORE writing
			if (dataIndex == 0 && byteOffset == 3)
			{
				textDisplayChip.InternalData[0] = currentData; // Save with font size preserved
				currentData = 0;
				byteOffset = 0;
				dataIndex = 1;
			}
			
			byte charByte = (byte)str[charIdx];
			currentData |= ((uint)charByte) << (byteOffset * 8);
			
			byteOffset++;
			if (byteOffset >= 4)
			{
				textDisplayChip.InternalData[dataIndex] = currentData;
				currentData = 0;
				byteOffset = 0;
				dataIndex++;
			}
		}
	}

		// Write any remaining data
		if (byteOffset > 0 && dataIndex < textDisplayChip.InternalData.Length)
		{
			textDisplayChip.InternalData[dataIndex] = currentData;
		}
		
		UnityEngine.Debug.Log($"[TextDisplay Encode] Final InternalData[0]=0x{textDisplayChip.InternalData[0]:X8}");
		}

	static void DrawSizeAdjustmentPanel(Vector2 topLeft, float panelWidth, float spacing)
	{
		// Draw section label
		Seb.Vis.UI.UI.DrawText("CHIP SIZE:", MenuHelper.Theme.FontBold, MenuHelper.Theme.FontSizeRegular, topLeft, Anchor.TopLeft, Color.white);
		topLeft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * (spacing * 0.5f);

		// Get current size in grid units
		int currentWidthGrids = Mathf.RoundToInt(textDisplayChip.Description.Size.x / DrawSettings.GridSize);
		int currentHeightGrids = Mathf.RoundToInt(textDisplayChip.Description.Size.y / DrawSettings.GridSize);
		
		// Width input
		Seb.Vis.UI.UI.DrawText("Width (grid units):", MenuHelper.Theme.FontBold, MenuHelper.Theme.FontSizeRegular, topLeft, Anchor.TopLeft, ColHelper.MakeCol(0.7f));
		topLeft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * 0.3f;
		
		InputFieldTheme inputTheme = MenuHelper.Theme.ChipNameInputField;
		inputTheme.fontSize = MenuHelper.Theme.FontSizeRegular;
		Vector2 inputSize = new Vector2(panelWidth, DrawSettings.ButtonHeight);
		
		InputFieldState widthState = Seb.Vis.UI.UI.InputField(ID_widthInput, inputTheme, topLeft, inputSize, currentWidthGrids.ToString(), Anchor.TopLeft, 1, sizeInputValidator);
		topLeft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * spacing;
		
		// Height input
		Seb.Vis.UI.UI.DrawText("Height (grid units):", MenuHelper.Theme.FontBold, MenuHelper.Theme.FontSizeRegular, topLeft, Anchor.TopLeft, ColHelper.MakeCol(0.7f));
		topLeft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * 0.3f;
		
		InputFieldState heightState = Seb.Vis.UI.UI.InputField(ID_heightInput, inputTheme, topLeft, inputSize, currentHeightGrids.ToString(), Anchor.TopLeft, 1, sizeInputValidator);
		topLeft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * spacing;
		
		// Font size input
		Seb.Vis.UI.UI.DrawText("Font Size (%):", MenuHelper.Theme.FontBold, MenuHelper.Theme.FontSizeRegular, topLeft, Anchor.TopLeft, ColHelper.MakeCol(0.7f));
		topLeft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * 0.3f;
		
		InputFieldState fontSizeState = Seb.Vis.UI.UI.InputField(ID_fontSizeInput, inputTheme, topLeft, inputSize, "100", Anchor.TopLeft, 1, sizeInputValidator);
		
		// Apply size changes when user types
		if (int.TryParse(widthState.text, out int newWidthGrids) && int.TryParse(heightState.text, out int newHeightGrids))
		{
			// Clamp to reasonable values
			newWidthGrids = Mathf.Clamp(newWidthGrids, 6, 30);
			newHeightGrids = Mathf.Clamp(newHeightGrids, 3, 20);
			
			// Apply if changed
			if (newWidthGrids != currentWidthGrids || newHeightGrids != currentHeightGrids)
			{
				textDisplayChip.Description.Size = new Vector2(
					newWidthGrids * DrawSettings.GridSize,
					newHeightGrids * DrawSettings.GridSize
				);
			}
		}
		
		// Apply font size changes
		if (int.TryParse(fontSizeState.text, out int newFontSizePercent))
		{
			newFontSizePercent = Mathf.Clamp(newFontSizePercent, 25, 300); // 25% to 300%
			SetFontSizePercent(newFontSizePercent);
		}
	}

	static bool ValidateSizeInput(string s)
	{
		if (string.IsNullOrEmpty(s)) return true;
		if (s.Length > 3) return false; // Max 3 digits (up to 999)
		return int.TryParse(s, out _);
	}

	/// <summary>
	/// Gets the font size percentage stored in InternalData[0] (bits 24-31)
	/// </summary>
	static int GetFontSizePercent()
	{
		if (textDisplayChip.InternalData == null || textDisplayChip.InternalData.Length == 0)
			return 100;
		
		// Font size is stored in the high byte of InternalData[0]
		uint fontSizeValue = (textDisplayChip.InternalData[0] >> 24) & 0xFF;
		int result = fontSizeValue == 0 ? 100 : (int)fontSizeValue;
		
		// DEBUG
		UnityEngine.Debug.Log($"[TextDisplay GetFontSize] InternalData[0]=0x{textDisplayChip.InternalData[0]:X8}, FontSizeByte={fontSizeValue}, Result={result}%");
		
		return result; // Default to 100% if not set
	}

	/// <summary>
	/// Sets the font size percentage in InternalData[0] (bits 24-31)
	/// </summary>
	static void SetFontSizePercent(int percent)
	{
		if (textDisplayChip.InternalData == null || textDisplayChip.InternalData.Length == 0)
			return;
		
		// Clear the high byte and set new value
		textDisplayChip.InternalData[0] = (textDisplayChip.InternalData[0] & 0x00FFFFFF) | ((uint)percent << 24);
	}

	/// <summary>
	/// Gets the font size multiplier (e.g., 100% = 1.0, 150% = 1.5)
	/// </summary>
	public static float GetFontSizeMultiplier(SubChipInstance textDisplayChip)
	{
		if (textDisplayChip?.InternalData == null || textDisplayChip.InternalData.Length == 0)
			return 1.0f;
		
		uint fontSizeValue = (textDisplayChip.InternalData[0] >> 24) & 0xFF;
		int percent = fontSizeValue == 0 ? 100 : (int)fontSizeValue;
		return percent / 100f;
	}
	}
}

