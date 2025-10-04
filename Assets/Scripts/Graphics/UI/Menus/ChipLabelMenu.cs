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

		static readonly string[] CancelConfirmButtonNames =
		{
			"CANCEL", "CONFIRM"
		};

		static readonly bool[] ButtonGroupInteractStates = { true, true };

		public static void OnMenuOpened()
		{
			subChip = (SubChipInstance)ContextMenu.interactionContext;

			InputFieldState inputFieldState = Seb.Vis.UI.UI.GetInputFieldState(ID_NameField);
			inputFieldState.SetText(subChip.Label);
			inputFieldState.SelectAll();
		}

		public static void DrawMenu()
		{
			Seb.Vis.UI.UI.DrawFullscreenPanel(DrawSettings.ActiveUITheme.MenuBackgroundOverlayCol);
			float spacing = 0.8f;

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

				// Draw cancel/confirm buttons
				Vector2 buttonsTopLeft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * spacing;
				int buttonIndex = Seb.Vis.UI.UI.HorizontalButtonGroup(CancelConfirmButtonNames, ButtonGroupInteractStates, theme.ButtonTheme, buttonsTopLeft, inputFieldBounds.Width, DrawSettings.DefaultButtonSpacing, 0, Anchor.TopLeft);

				MenuHelper.DrawReservedMenuPanel(panelID, Seb.Vis.UI.UI.GetCurrentBoundsScope());

				// Keyboard shortcuts and UI input
				if (KeyboardShortcuts.CancelShortcutTriggered || buttonIndex == 0) Cancel();
				else if (KeyboardShortcuts.ConfirmShortcutTriggered || buttonIndex == 1) Confirm(newName);
			}
		}

		static void Confirm(string newName)
		{
			subChip.Label = newName;
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
		}

		static void Cancel()
		{
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
		}

		static bool ValidateNameInput(string name) => name.Length <= MaxLabelLength.Length;
	}
}
