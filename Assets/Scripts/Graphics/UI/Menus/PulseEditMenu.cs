using System;
using DLS.Game;
using Seb.Helpers;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	public static class PulseEditMenu
	{
		static SubChipInstance pulseChip;
		static uint pulseWidth;

		static readonly UIHandle ID_PulseWidthInput = new("PulseChipEdit_PulseWidth");
		static readonly Func<string, bool> integerInputValidator = ValidatePulseWidthInput;

		public static void DrawMenu()
		{
			MenuHelper.DrawBackgroundOverlay();
			Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;

			Vector2 pos = Seb.Vis.UI.UI.Centre + Vector2.up * (Seb.Vis.UI.UI.HalfHeight * 0.25f);

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				Seb.Vis.UI.UI.DrawText("Pulse Width (ticks)", theme.FontBold, theme.FontSizeRegular, pos, Anchor.TextCentre, Color.white * 0.8f);

				InputFieldTheme inputFieldTheme = DrawSettings.ActiveUITheme.ChipNameInputField;
				inputFieldTheme.fontSize = DrawSettings.ActiveUITheme.FontSizeRegular;

				Vector2 size = new(5.6f, DrawSettings.SelectorWheelHeight);
				Vector2 inputPos = Seb.Vis.UI.UI.PrevBounds.CentreBottom + Vector2.down * DrawSettings.VerticalButtonSpacing;
				InputFieldState state = Seb.Vis.UI.UI.InputField(ID_PulseWidthInput, inputFieldTheme, inputPos, size, string.Empty, Anchor.CentreTop, 1, integerInputValidator, forceFocus: true);
				uint.TryParse(state.text, out pulseWidth);

				MenuHelper.CancelConfirmResult result = MenuHelper.DrawCancelConfirmButtons(Seb.Vis.UI.UI.GetCurrentBoundsScope().BottomLeft, Seb.Vis.UI.UI.GetCurrentBoundsScope().Width, true);
				MenuHelper.DrawReservedMenuPanel(panelID, Seb.Vis.UI.UI.GetCurrentBoundsScope());

				if (result == MenuHelper.CancelConfirmResult.Cancel)
				{
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				}
				else if (result == MenuHelper.CancelConfirmResult.Confirm)
				{
					Project.ActiveProject.NotifyPulseWidthChanged(pulseChip, pulseWidth);
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				}
			}
		}

		public static void OnMenuOpened()
		{
			pulseChip = (SubChipInstance)ContextMenu.interactionContext;
			pulseWidth = pulseChip.InternalData[0];
			Seb.Vis.UI.UI.GetInputFieldState(ID_PulseWidthInput).SetText(pulseWidth.ToString());
		}

		public static bool ValidatePulseWidthInput(string s)
		{
			if (s.Length > 4) return false;
			if (string.IsNullOrEmpty(s)) return true;
			if (s.Contains(" ")) return false;
			return int.TryParse(s, out _);
		}
	}
}
