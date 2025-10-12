using System;
using Seb.Helpers;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	public static class DeleteConfirmationPopup
	{
		static Action<bool> onClosedCallback; // false = cancel, true = confirm delete
		static string deleteMessage;
		static Color messageColor;
		static UIDrawer.MenuType previousMenu;

		public static void DrawMenu()
		{
			MenuHelper.DrawBackgroundOverlay();

			Vector2 textPos = Seb.Vis.UI.UI.Centre + Vector2.up * 5;

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();
				Draw.ID textBGPanelID = Seb.Vis.UI.UI.ReservePanel();
				Seb.Vis.UI.UI.DrawText(deleteMessage, DrawSettings.ActiveUITheme.FontRegular, DrawSettings.ActiveUITheme.FontSizeRegular, textPos, Anchor.TextCentre, messageColor);
				Seb.Vis.UI.UI.ModifyPanel(textBGPanelID, Bounds2D.Grow(Seb.Vis.UI.UI.PrevBounds, 1.5f), ColHelper.MakeCol(0.11f));

				Vector2 topLeft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * 1;
				MenuHelper.CancelConfirmResult button = MenuHelper.DrawCancelConfirmButtons(topLeft, Seb.Vis.UI.UI.PrevBounds.Width, false);

				MenuHelper.DrawReservedMenuPanel(panelID, Seb.Vis.UI.UI.GetCurrentBoundsScope());

				if (button == MenuHelper.CancelConfirmResult.Cancel)
				{
					UIDrawer.SetActiveMenu(previousMenu);
					onClosedCallback?.Invoke(false);
				}
				else if (button == MenuHelper.CancelConfirmResult.Confirm)
				{
					UIDrawer.SetActiveMenu(previousMenu);
					onClosedCallback?.Invoke(true);
				}
			}
		}

		public static void OpenPopup(string message, Color messageColor, Action<bool> callback)
		{
			deleteMessage = message;
			DeleteConfirmationPopup.messageColor = messageColor;
			onClosedCallback = callback;
			previousMenu = UIDrawer.ActiveMenu; // Store current menu to return to
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.DeleteConfirmation);
		}

		public static UIDrawer.MenuType GetPreviousMenu()
		{
			return previousMenu;
		}
	}
}

