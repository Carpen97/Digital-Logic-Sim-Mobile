using System;
using Seb.Helpers;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	public static class UnsavedChangesPopup
	{
		static Action<bool> onClosedCallback; // false = cancel, true = continue

		public static void DrawMenu()
		{
			MenuHelper.DrawBackgroundOverlay();

			string text = "The current chip has unsaved changes.\nAre you sure you want to continue?";
			Color textCol = new(1, 0.4f, 0.45f);
			Vector2 textPos = Seb.Vis.UI.UI.Centre + Vector2.up * 5;

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();
				Draw.ID textBGPanelID = Seb.Vis.UI.UI.ReservePanel();
				Seb.Vis.UI.UI.DrawText(text, DrawSettings.ActiveUITheme.FontRegular, DrawSettings.ActiveUITheme.FontSizeRegular, textPos, Anchor.TextCentre, textCol);
				Seb.Vis.UI.UI.ModifyPanel(textBGPanelID, Bounds2D.Grow(Seb.Vis.UI.UI.PrevBounds, 1.5f), ColHelper.MakeCol(0.11f));

				Vector2 topLeft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * 1;
				MenuHelper.CancelConfirmResult button = MenuHelper.DrawCancelConfirmButtons(topLeft, Seb.Vis.UI.UI.PrevBounds.Width, false);

				MenuHelper.DrawReservedMenuPanel(panelID, Seb.Vis.UI.UI.GetCurrentBoundsScope());

				if (button == MenuHelper.CancelConfirmResult.Cancel) // cancel
				{
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
					onClosedCallback?.Invoke(false);
				}
				else if (button == MenuHelper.CancelConfirmResult.Confirm) // continue
				{
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
					onClosedCallback?.Invoke(true);
				}
			}
		}

		public static void OpenPopup(Action<bool> callback)
		{
			onClosedCallback = callback;
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.UnsavedChanges);
		}
	}
}
