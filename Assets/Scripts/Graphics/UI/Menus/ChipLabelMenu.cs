using DLS.Game;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	public static class ChipLabelMenu
	{
		const string MaxLabelLength = "MY LONG LABEL TEXT";
		static SubChipInstance subChip;
		static readonly UIHandle ID_NameField = new("ChipLabelMenu_NameField");
		static SliderState sliderStateX;
		static SliderState sliderStateY;
		static bool snapToThreePositions;

		static readonly string[] CancelConfirmButtonNames =
		{
			"CANCEL", "CONFIRM"
		};

		static readonly bool[] ButtonGroupInteractStates = { true, true };

		// Slider range: X and Y both [-1, 1], 0 = centre. progressT 0.5 = 0.

		public static void OnMenuOpened()
		{
			subChip = (SubChipInstance)ContextMenu.interactionContext;

			InputFieldState inputFieldState = Seb.Vis.UI.UI.GetInputFieldState(ID_NameField);
			inputFieldState.SetText(subChip.Label);
			inputFieldState.SelectAll();

			sliderStateX.progressT = (subChip.LabelOffset.x + 1f) * 0.5f;
			sliderStateY.progressT = (subChip.LabelOffset.y + 1f) * 0.5f;
		}

		public static void DrawMenu()
		{
			Seb.Vis.UI.UI.DrawFullscreenPanel(DrawSettings.ActiveUITheme.MenuBackgroundOverlayCol);
			float spacing = 1.6f; // Doubled from 0.8f for better vertical spacing

			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			InputFieldTheme inputTheme = DrawSettings.ActiveUITheme.ChipNameInputField;
			Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				Vector2 unpaddedSize = Draw.CalculateTextBoundsSize(MaxLabelLength, inputTheme.fontSize, inputTheme.font);
				const float padX = 2.25f;
				Vector2 inputFieldSize = unpaddedSize + new Vector2(padX, 2.25f);
				Vector2 pos = Seb.Vis.UI.UI.Centre + Vector2.up * 5;

				// Draw input field
				InputFieldState inputFieldState = Seb.Vis.UI.UI.InputField(ID_NameField, inputTheme, pos, inputFieldSize, subChip.Label, Anchor.Centre, padX / 2, ValidateNameInput, true);
				Bounds2D inputFieldBounds = Seb.Vis.UI.UI.PrevBounds;
				string newName = inputFieldState.text;

				// Sliders: X and Y label offset. X: -1=left, 0=centre, 1=right. Y: -1=above, 0=centre, 1=below
				const float rowHeight = 4.4f; // Doubled from 2.2f for better vertical spacing
				Vector2 sliderSize = new(inputFieldBounds.Width, 0.5f);
				Vector2 sliderCentreX = inputFieldBounds.CentreBottom + Vector2.down * (spacing + rowHeight * 0.5f);
				Vector2 sliderCentreY = sliderCentreX + Vector2.down * rowHeight;

				const float labelToSliderGap = 1.1f;
				DrawSettings.UIThemeDLS uiTheme = DrawSettings.ActiveUITheme;
				Seb.Vis.UI.UI.DrawText("Label X", uiTheme.FontBold, uiTheme.FontSizeRegular * 0.9f, sliderCentreX + Vector2.up * (sliderSize.y * 0.5f + labelToSliderGap), Anchor.TextCentre, Color.white * 0.9f);
				Seb.Vis.UI.UI.DrawSlider(sliderCentreX, sliderSize, Anchor.Centre, ref sliderStateX);

				Seb.Vis.UI.UI.DrawText("Label Y", uiTheme.FontBold, uiTheme.FontSizeRegular * 0.9f, sliderCentreY + Vector2.up * (sliderSize.y * 0.5f + labelToSliderGap), Anchor.TextCentre, Color.white * 0.9f);
				Seb.Vis.UI.UI.DrawSlider(sliderCentreY, sliderSize, Anchor.Centre, ref sliderStateY);

				// Snap checkbox - constrains sliders to -1, 0, 1. Checkbox is label; only "Snap" is the button.
				float checkboxRowHeight = 0.6f;
				Vector2 snapRowPos = sliderCentreY + Vector2.down * (rowHeight * 0.5f + spacing);
				Vector2 checkboxTextPos = new Vector2(inputFieldBounds.Min.x, snapRowPos.y);
				Seb.Vis.UI.UI.DrawText(snapToThreePositions ? "[X] " : "[ ] ", uiTheme.FontBold, uiTheme.FontSizeRegular * 0.9f, checkboxTextPos, Anchor.CentreLeft, Color.white * 0.9f);
				Vector2 snapButtonPos = new Vector2(Seb.Vis.UI.UI.PrevBounds.Max.x + 0.3f, snapRowPos.y);
				bool snapPressed = Seb.Vis.UI.UI.Button("Snap", theme.ButtonTheme, snapButtonPos, true, Anchor.CentreLeft);
				if (snapPressed) snapToThreePositions = !snapToThreePositions;

				// Preset buttons: bottom, centre, top - set Y instantly, reset X to 0. Y: -1=above, 0=centre, 1=below
				float presetButtonGap = 0.4f;
				Vector2 presetStart = new Vector2(Seb.Vis.UI.UI.PrevBounds.Max.x + presetButtonGap, snapRowPos.y);
				if (Seb.Vis.UI.UI.Button("bottom", theme.ButtonTheme, presetStart, true, Anchor.CentreLeft))
				{
					sliderStateX.progressT = 0.5f;
					sliderStateY.progressT = 1f; // Y=1 = below
				}
				presetStart.x = Seb.Vis.UI.UI.PrevBounds.Max.x + presetButtonGap;
				if (Seb.Vis.UI.UI.Button("centre", theme.ButtonTheme, presetStart, true, Anchor.CentreLeft))
				{
					sliderStateX.progressT = 0.5f;
					sliderStateY.progressT = 0.5f; // Y=0 = centre
				}
				presetStart.x = Seb.Vis.UI.UI.PrevBounds.Max.x + presetButtonGap;
				if (Seb.Vis.UI.UI.Button("top", theme.ButtonTheme, presetStart, true, Anchor.CentreLeft))
				{
					sliderStateX.progressT = 0.5f;
					sliderStateY.progressT = 0f; // Y=-1 = above
				}

				// Apply snapping to slider states when enabled
				if (snapToThreePositions)
				{
					sliderStateX.progressT = SnapProgressT(sliderStateX.progressT);
					sliderStateY.progressT = SnapProgressT(sliderStateY.progressT);
				}

				// Draw cancel/confirm buttons - use left edge of panel (Anchor.TopLeft expects top-left corner)
				Vector2 buttonsTopLeft = new Vector2(inputFieldBounds.Min.x, snapRowPos.y - checkboxRowHeight - spacing);
				int buttonIndex = Seb.Vis.UI.UI.HorizontalButtonGroup(CancelConfirmButtonNames, ButtonGroupInteractStates, theme.ButtonTheme, buttonsTopLeft, inputFieldBounds.Width, DrawSettings.DefaultButtonSpacing, 0, Anchor.TopLeft);

				MenuHelper.DrawReservedMenuPanel(panelID, Seb.Vis.UI.UI.GetCurrentBoundsScope());

				// Keyboard shortcuts and UI input
				float valX = sliderStateX.progressT * 2f - 1f;
				float valY = sliderStateY.progressT * 2f - 1f;
				if (snapToThreePositions)
				{
					valX = SnapValue(valX);
					valY = SnapValue(valY);
				}
				if (KeyboardShortcuts.CancelShortcutTriggered || buttonIndex == 0) Cancel();
				else if (KeyboardShortcuts.ConfirmShortcutTriggered || buttonIndex == 1) Confirm(newName, new Vector2(valX, valY));
			}
		}

		static void Confirm(string newName, Vector2 labelOffset)
		{
			subChip.Label = newName;
			subChip.LabelOffset = labelOffset;
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
		}

		static void Cancel()
		{
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
		}

		static bool ValidateNameInput(string name) => name.Length <= MaxLabelLength.Length;

		/// <summary>Snap progressT [0,1] to 0, 0.5, or 1 (values -1, 0, 1).</summary>
		static float SnapProgressT(float t)
		{
			if (t < 0.25f) return 0f;
			if (t < 0.75f) return 0.5f;
			return 1f;
		}

		static float SnapValue(float v)
		{
			if (v < -0.5f) return -1f;
			if (v < 0.5f) return 0f;
			return 1f;
		}
	}
}
