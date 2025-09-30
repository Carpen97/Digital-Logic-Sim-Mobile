using UnityEngine;
using Seb.Helpers;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using static DLS.Graphics.DrawSettings;

namespace DLS.Graphics
{
	public static class CachingExplanationPopup
	{
		// UI constants (matching other popups)
		const float OkBtnWidthFrac = 0.30f;
		const float OkBtnHeightMul = 1.5f;

		// ---------- Public API ----------
		public static void Open()
		{
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.CachingExplanation);
		}

		// ---------- UI Drawing ----------
		public static void DrawMenu()
		{
			// Dimmed backdrop (same as other popups)
			MenuHelper.DrawBackgroundOverlay();

			using (UI.BeginBoundsScope(true))
			{
				Draw.ID panelBG = UI.ReservePanel();
				Draw.ID titleBG = UI.ReservePanel();

				// --- Title banner ---
				Vector2 titlePos = UI.CentreTop + Vector2.down * 8f;
				string title = "Chip Caching Explained";
				Color headerCol = ColHelper.MakeCol255(44, 92, 62);
				UI.DrawText(title, ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular * 2f, titlePos, Anchor.TextCentre, headerCol);

				// Content text explaining caching
				string contentText = @"Chip caching is a performance optimization technique that
pre-computes and stores the output values for all possible
input combinations of a chip.

• Cached chips run much faster during simulation
• Only works with combinational (stateless) chips
• Memory usage increases with the number of input bits
• Chips with too many inputs cannot be cached

When a chip is cached, the simulator looks up pre-computed
results instead of running the chip's logic every time.
This dramatically improves performance for complex chips
that are used frequently in your circuits.

The trade-off is memory usage - each possible input
combination requires storage space.";

				// Draw content text
				Vector2 contentPos = UI.Centre + Vector2.up * 2f;
				UI.DrawText(contentText, ActiveUITheme.FontRegular, UIThemeLibrary.FontSizeSmall, contentPos, Anchor.TextCentre, Color.white);

				// --- OK button ---
				Vector2 okBtnPos = UI.CentreBottom + Vector2.up * 8f;
				Vector2 okBtnSize = new Vector2(UI.Width * OkBtnWidthFrac, ButtonHeight * OkBtnHeightMul);
				bool okPressed = UI.Button("OK", ActiveUITheme.ButtonTheme, okBtnPos, okBtnSize, true, true, false, ActiveUITheme.ButtonTheme.buttonCols, Anchor.Centre);

				if (okPressed)
				{
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.ChipCustomization);
				}

				// Draw main panel background
				Bounds2D panelBounds = UI.GetCurrentBoundsScope();
				UI.ModifyPanel(panelBG, panelBounds, ActiveUITheme.MenuPanelCol);
			}
		}
	}
}
