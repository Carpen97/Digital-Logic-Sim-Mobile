using DLS.Game;
using Seb.Helpers;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	public static class RebindKeyChipMenu
	{
		public const string allowedChars = "1234567890QWERTYUIOPASDFGHJKLZXCVBNM";
		static SubChipInstance keyChip;
		static string chosenKey;

		public static void DrawMenu()
		{
			MenuHelper.DrawBackgroundOverlay();
			Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;

			Vector2 pos = Seb.Vis.UI.UI.Centre + Vector2.up * (Seb.Vis.UI.UI.HalfHeight * 0.25f);

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				if (InputHelper.AnyKeyOrMouseDownThisFrame && !string.IsNullOrEmpty(InputHelper.InputStringThisFrame))
				{
					char activeChar = char.ToUpper(InputHelper.InputStringThisFrame[0]);
					if (allowedChars.Contains(activeChar))
					{
						chosenKey = activeChar.ToString();
					}
				}

				Seb.Vis.UI.UI.DrawText("Press a key to rebind\n (alphanumeric only)", theme.FontBold, theme.FontSizeRegular, pos, Anchor.TextCentre, Color.white * 0.8f);

				Seb.Vis.UI.UI.DrawPanel(Seb.Vis.UI.UI.PrevBounds.CentreBottom + Vector2.down, Vector2.one * 3.5f, new Color(0.1f, 0.1f, 0.1f), Anchor.CentreTop);
				Seb.Vis.UI.UI.DrawText(chosenKey, theme.FontBold, theme.FontSizeRegular * 1.5f, Seb.Vis.UI.UI.PrevBounds.Centre, Anchor.TextCentre, Color.white);

				MenuHelper.CancelConfirmResult result = MenuHelper.DrawCancelConfirmButtons(Seb.Vis.UI.UI.GetCurrentBoundsScope().BottomLeft, Seb.Vis.UI.UI.GetCurrentBoundsScope().Width, true);
				MenuHelper.DrawReservedMenuPanel(panelID, Seb.Vis.UI.UI.GetCurrentBoundsScope());

				if (result == MenuHelper.CancelConfirmResult.Cancel)
				{
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				}
				else if (result == MenuHelper.CancelConfirmResult.Confirm)
				{
					Project.ActiveProject.NotifyKeyChipBindingChanged(keyChip, chosenKey[0]);
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				}
			}
		}

		public static void OnMenuOpened()
		{
			keyChip = (SubChipInstance)ContextMenu.interactionContext;
			chosenKey = keyChip.activationKeyString;
		}
	}
}
