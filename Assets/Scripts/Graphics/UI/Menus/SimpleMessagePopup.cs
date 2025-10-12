using Seb.Helpers;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	/// <summary>
	/// A simple popup for displaying a message with an OK button
	/// </summary>
	public static class SimpleMessagePopup
	{
		static string message = "";
		static System.Action onClosed;

		public static void Open(string messageText, System.Action closedCallback = null)
		{
			message = messageText;
			onClosed = closedCallback;
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.SimpleMessage);
		}

		public static void DrawMenu()
		{
			MenuHelper.DrawBackgroundOverlay();

			Vector2 textPos = Seb.Vis.UI.UI.Centre + Vector2.up * 5;

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();
				Draw.ID textBGPanelID = Seb.Vis.UI.UI.ReservePanel();
				
				Seb.Vis.UI.UI.DrawText(message, DrawSettings.ActiveUITheme.FontRegular, DrawSettings.ActiveUITheme.FontSizeRegular, textPos, Anchor.TextCentre, Color.yellow);
				Seb.Vis.UI.UI.ModifyPanel(textBGPanelID, Bounds2D.Grow(Seb.Vis.UI.UI.PrevBounds, 1.5f), ColHelper.MakeCol(0.11f));

				Vector2 topLeft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * 1;
				
				// Draw OK button
				Vector2 buttonSize = new Vector2(Seb.Vis.UI.UI.PrevBounds.Width, DrawSettings.ButtonHeight * 1.6f);
				
				Vector2 okButtonPos = topLeft;
				if (Seb.Vis.UI.UI.Button("OK", DrawSettings.ActiveUITheme.ButtonTheme, okButtonPos, buttonSize, true, false, false, DrawSettings.ActiveUITheme.ButtonTheme.buttonCols, Anchor.TopLeft))
				{
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
					onClosed?.Invoke();
				}

				MenuHelper.DrawReservedMenuPanel(panelID, Seb.Vis.UI.UI.GetCurrentBoundsScope());
			}
		}
	}
}

