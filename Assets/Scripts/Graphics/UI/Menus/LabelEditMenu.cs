using System;
using System.Reflection;
using DLS.Description;
using DLS.Game;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	public static class LabelEditMenu
	{
		static SubChipInstance labelChip;
		static readonly UIHandle ID_TextInput = new("LabelEditMenu_TextInput");
		static readonly UIHandle ID_ColourPicker = new("LabelEditMenu_ColourPicker");
		static SliderState widthSliderState;
		static SliderState fontSizeSliderState;
		static SliderState alphaSliderState;

		// Saved on open for revert-on-cancel
		static string initialLabelText;
		static uint initialInternalData0, initialInternalData1, initialInternalData2;
		const int MaxLabelLength = 500;

		static readonly Func<string, bool> ValidateInput = s => s != null && s.Length <= MaxLabelLength;

		const float LabelWidthMin = 0.5f;
		const float LabelWidthMax = 10f;
		const float LabelWidthDefault = 0.7f;
		const float LabelFontSizeMin = 0.1f;
		const float LabelFontSizeMax = 0.5f;
		const float LabelFontSizeDefault = 0.2f;

		/// <summary>Gets label width from subchip. InternalData[1]: width×100, 0 or missing = default 0.7.</summary>
		public static float GetLabelWidthFromStored(SubChipInstance subchip)
		{
			if (subchip.InternalData == null || subchip.InternalData.Length < 2 || subchip.InternalData[1] == 0)
				return LabelWidthDefault;
			return Mathf.Clamp(subchip.InternalData[1] / 100f, LabelWidthMin, LabelWidthMax);
		}

		/// <summary>Gets label font size from subchip. InternalData[2]: fontSize×1000, 0 or missing = default 0.2.</summary>
		public static float GetLabelFontSizeFromStored(SubChipInstance subchip)
		{
			if (subchip.InternalData == null || subchip.InternalData.Length < 3 || subchip.InternalData[2] == 0)
				return LabelFontSizeDefault;
			return Mathf.Clamp(subchip.InternalData[2] / 1000f, LabelFontSizeMin, LabelFontSizeMax);
		}

		/// <summary>Returns the background colour for label chip. InternalData[0]: 0 = default, 1-6 = legacy presets, 7+ = packed RGBA.</summary>
		public static Color GetLabelColourFromStored(uint packed)
		{
			if (packed == 0) return DrawSettings.ActiveTheme.PinLabelCol;
			if (packed <= 6) return GetLabelColourPreset((int)packed); // Legacy preset indices
			// Packed format: (a<<24)|(r<<16)|(g<<8)|b
			byte r = (byte)((packed >> 16) & 0xFF);
			byte g = (byte)((packed >> 8) & 0xFF);
			byte b = (byte)(packed & 0xFF);
			byte a = (byte)((packed >> 24) & 0xFF);
			return new Color32(r, g, b, a);
		}

		/// <summary>Packs RGBA. Avoids 0 (reserved for theme default) by using 7 for transparent black.</summary>
		static uint PackColor(Color col)
		{
			Color32 c = col;
			uint packed = (uint)((c.a << 24) | (c.r << 16) | (c.g << 8) | c.b);
			return packed == 0 ? 7u : packed;  // 0 = theme default; use 7 (transparent near-black) instead
		}

		static Color GetLabelColourPreset(int index)
		{
			return index switch
			{
				1 => new Color(0.1f, 0.15f, 0.35f, 0.75f),
				2 => new Color(0.1f, 0.25f, 0.15f, 0.75f),
				3 => new Color(0.35f, 0.1f, 0.1f, 0.75f),
				4 => new Color(0.25f, 0.1f, 0.3f, 0.75f),
				5 => new Color(0.2f, 0.2f, 0.2f, 0.7f),
				6 => new Color(0.15f, 0.25f, 0.3f, 0.75f),
				_ => DrawSettings.ActiveTheme.PinLabelCol
			};
		}

		public static void DrawMenu()
		{
			Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;

			// Centre of right half - positioned high so menu sits in upper-right, well above midpoint
			float rightHalfCentreX = Seb.Vis.UI.UI.Width * 0.75f;
			Vector2 pos = new(rightHalfCentreX, Seb.Vis.UI.UI.Centre.y + Seb.Vis.UI.UI.HalfHeight * 0.55f);

			const float menuContentWidth = 34f;  // Encompasses content; symmetric bounds for title centering
			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				// Establish top bounds
				Seb.Vis.UI.UI.DrawQuad(pos, new Vector2(menuContentWidth, 0.01f), new Color(0, 0, 0, 0), Anchor.Centre);

				// Text input (left) and color picker (right) on the same row
				const float titleToContentSpacing = 2.5f;
				float rowTopY = Seb.Vis.UI.UI.PrevBounds.CentreBottom.y - titleToContentSpacing;
				const float inputWidth = 24f;
				const float inputHeight = 8f;
				const float pickerWidth = 10f;
				const float gap = 1.2f;
				const float sliderHandleSizeMult = 2.5f;  // Larger for mobile (sliders only; color picker uses default)

				InputFieldTheme inputTheme = DrawSettings.ActiveUITheme.ChipNameInputField;
				inputTheme.fontSize = DrawSettings.ActiveUITheme.FontSizeRegular;
				Vector2 inputPos = new(pos.x - (gap + pickerWidth) / 2f, rowTopY);  // centre of left (input) half
				InputFieldState state = Seb.Vis.UI.UI.InputField(ID_TextInput, inputTheme, inputPos, new Vector2(inputWidth, inputHeight), "Label", Anchor.CentreTop, 1, ValidateInput, forceFocus: true, wrapText: true);

				// Live update: apply text to chip so changes show immediately
				labelChip.Label = state.text?.TrimEnd() ?? string.Empty;

				Vector2 pickerPos = new(pos.x + (inputWidth + gap - pickerWidth) / 2f, rowTopY);  // top-left of picker (right half)
				Color newCol = Seb.Vis.UI.UI.DrawColourPicker(ID_ColourPicker, pickerPos, pickerWidth, Anchor.TopLeft);

				// Alpha slider: below color picker
				float sliderGap = 2f;
				const float sliderWidth = 16f;
				const float sliderHeight = 1f;
				const float labelOffset = 6f;
				const float valueOffset = 18f;

				float alphaRowY = rowTopY - Mathf.Max(inputHeight, pickerWidth) - sliderGap;
				float sliderLeftX = pos.x - sliderWidth / 2f;
				Vector2 alphaSliderPos = new(sliderLeftX, alphaRowY);
				Seb.Vis.UI.UI.DrawText("Alpha:", DrawSettings.ActiveUITheme.FontBold, DrawSettings.ActiveUITheme.FontSizeRegular, alphaSliderPos + Vector2.left * labelOffset, Anchor.CentreRight, Color.white * 0.9f);
				Seb.Vis.UI.UI.DrawSlider(alphaSliderPos, new Vector2(sliderWidth, sliderHeight), Anchor.TopLeft, ref alphaSliderState, sliderHandleSizeMult);
				float alphaVal = Mathf.Clamp01(alphaSliderState.progressT);
				Seb.Vis.UI.UI.DrawText($"{alphaVal:P0}", DrawSettings.ActiveUITheme.FontBold, Seb.Vis.UI.UIThemeLibrary.FontSizeSmall, alphaSliderPos + Vector2.right * valueOffset, Anchor.CentreLeft, Color.white * 0.9f);

				// Live update: apply colour to chip (RGB from picker, alpha from slider)
				if (labelChip.InternalData != null && labelChip.InternalData.Length > 0)
					labelChip.InternalData[0] = PackColor(Seb.Helpers.ColHelper.WithAlpha(newCol, alphaVal));

				// Sliders: Width and font size, centered (vertical spacing doubled)
				const float sliderRowSpacing = 5f;
				float row1Y = alphaRowY - sliderRowSpacing;
				float row2Y = row1Y - sliderRowSpacing;

				// Row 1: Width slider
				Vector2 widthSliderPos = new(sliderLeftX, row1Y);
				Seb.Vis.UI.UI.DrawText("Width:", DrawSettings.ActiveUITheme.FontBold, DrawSettings.ActiveUITheme.FontSizeRegular, widthSliderPos + Vector2.left * labelOffset, Anchor.CentreRight, Color.white * 0.9f);
				Seb.Vis.UI.UI.DrawSlider(widthSliderPos, new Vector2(sliderWidth, sliderHeight), Anchor.TopLeft, ref widthSliderState, sliderHandleSizeMult);
				float labelWidth = LabelWidthMin + widthSliderState.progressT * (LabelWidthMax - LabelWidthMin);
				Seb.Vis.UI.UI.DrawText($"{labelWidth:F1}", DrawSettings.ActiveUITheme.FontBold, Seb.Vis.UI.UIThemeLibrary.FontSizeSmall, widthSliderPos + Vector2.right * valueOffset, Anchor.CentreLeft, Color.white * 0.9f);
				if (labelChip.InternalData != null && labelChip.InternalData.Length > 1)
					labelChip.InternalData[1] = (uint)Mathf.RoundToInt(Mathf.Clamp(labelWidth, LabelWidthMin, LabelWidthMax) * 100f);

				// Row 2: Font size slider (same horizontal layout)
				Vector2 fontSizeSliderPos = new(sliderLeftX, row2Y);
				Seb.Vis.UI.UI.DrawText("Font size:", DrawSettings.ActiveUITheme.FontBold, DrawSettings.ActiveUITheme.FontSizeRegular, fontSizeSliderPos + Vector2.left * labelOffset, Anchor.CentreRight, Color.white * 0.9f);
				Seb.Vis.UI.UI.DrawSlider(fontSizeSliderPos, new Vector2(sliderWidth, sliderHeight), Anchor.TopLeft, ref fontSizeSliderState, sliderHandleSizeMult);
				float labelFontSize = LabelFontSizeMin + fontSizeSliderState.progressT * (LabelFontSizeMax - LabelFontSizeMin);
				Seb.Vis.UI.UI.DrawText($"{labelFontSize:F2}", DrawSettings.ActiveUITheme.FontBold, Seb.Vis.UI.UIThemeLibrary.FontSizeSmall, fontSizeSliderPos + Vector2.right * valueOffset, Anchor.CentreLeft, Color.white * 0.9f);
				if (labelChip.InternalData != null && labelChip.InternalData.Length > 2)
					labelChip.InternalData[2] = (uint)Mathf.RoundToInt(Mathf.Clamp(labelFontSize, LabelFontSizeMin, LabelFontSizeMax) * 1000f);

				MenuHelper.CancelConfirmResult result = MenuHelper.DrawCancelConfirmButtons(Seb.Vis.UI.UI.GetCurrentBoundsScope().BottomLeft, Seb.Vis.UI.UI.GetCurrentBoundsScope().Width, true);

				// Draw title at actual panel center (content is asymmetric: labels extend left more than values extend right)
				Bounds2D contentBounds = Seb.Vis.UI.UI.GetCurrentBoundsScope();
				Vector2 titlePos = new(contentBounds.Centre.x, pos.y);
				Seb.Vis.UI.UI.DrawText("Edit Label", theme.FontBold, theme.FontSizeRegular, titlePos, Anchor.TextCentre, Color.white * 0.8f);

				MenuHelper.DrawReservedMenuPanel(panelID, Seb.Vis.UI.UI.GetCurrentBoundsScope());

				if (result == MenuHelper.CancelConfirmResult.Cancel)
				{
					RevertToInitialState();
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				}
				else if (result == MenuHelper.CancelConfirmResult.Confirm)
				{
					// Live updates already applied; just ensure final trim and close
					labelChip.Label = state.text?.TrimEnd() ?? string.Empty;
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				}
			}
		}

		/// <summary>Expands InternalData to [colour, width×100, fontSize×1000] for old/short labels. Uses reflection to set readonly field.</summary>
		static void EnsureLabelInternalDataLength(SubChipInstance chip)
		{
			if (chip.ChipType != ChipType.Label) return;
			if (chip.InternalData != null && chip.InternalData.Length >= 3) return;

			uint col = chip.InternalData != null && chip.InternalData.Length > 0 ? chip.InternalData[0] : 0;
			uint widthStored = chip.InternalData != null && chip.InternalData.Length > 1 ? chip.InternalData[1] : 70;
			uint fontSizeStored = chip.InternalData != null && chip.InternalData.Length > 2 ? chip.InternalData[2] : 200;
			var newData = new uint[] { col, widthStored, fontSizeStored };

			var field = typeof(SubChipInstance).GetField("InternalData", BindingFlags.Public | BindingFlags.Instance);
			field?.SetValue(chip, newData);
		}

		static void RevertToInitialState()
		{
			if (labelChip == null) return;
			labelChip.Label = initialLabelText ?? string.Empty;
			if (labelChip.InternalData != null && labelChip.InternalData.Length >= 3)
			{
				labelChip.InternalData[0] = initialInternalData0;
				labelChip.InternalData[1] = initialInternalData1;
				labelChip.InternalData[2] = initialInternalData2;
			}
		}

		public static void OnMenuOpened()
		{
			labelChip = (SubChipInstance)ContextMenu.interactionContext;
			EnsureLabelInternalDataLength(labelChip);

			// Save initial state for revert-on-cancel
			initialLabelText = string.IsNullOrWhiteSpace(labelChip.Label) ? "Label" : labelChip.Label;
			initialInternalData0 = labelChip.InternalData != null && labelChip.InternalData.Length > 0 ? labelChip.InternalData[0] : 0;
			initialInternalData1 = labelChip.InternalData != null && labelChip.InternalData.Length > 1 ? labelChip.InternalData[1] : 70;
			initialInternalData2 = labelChip.InternalData != null && labelChip.InternalData.Length > 2 ? labelChip.InternalData[2] : 200;

			string text = initialLabelText;
			Seb.Vis.UI.UI.GetInputFieldState(ID_TextInput).SetText(text);
			Seb.Vis.UI.UI.GetInputFieldState(ID_TextInput).SelectAll();

			// Init colour picker to current chip colour (use same source as drawer for consistency)
			Color currentCol = DevSceneDrawer.GetLabelChipBackgroundColour(labelChip);
			Seb.Vis.UI.UI.GetColourPickerState(ID_ColourPicker).SetRGB(currentCol);

			// Init alpha slider (use alpha from loaded colour; theme default has no packed value so use currentCol.a)
			alphaSliderState.progressT = Mathf.Clamp01(currentCol.a);

			// Init width slider from stored width (0–1 range)
			float w = GetLabelWidthFromStored(labelChip);
			widthSliderState.progressT = Mathf.InverseLerp(LabelWidthMin, LabelWidthMax, w);

			// Init font size slider from stored value (0–1 range)
			float fs = GetLabelFontSizeFromStored(labelChip);
			fontSizeSliderState.progressT = Mathf.InverseLerp(LabelFontSizeMin, LabelFontSizeMax, fs);
		}
	}
}
