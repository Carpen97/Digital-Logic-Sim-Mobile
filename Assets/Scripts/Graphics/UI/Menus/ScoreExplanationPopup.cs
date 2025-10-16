using UnityEngine;
using Seb.Helpers;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using static DLS.Graphics.DrawSettings;

namespace DLS.Graphics
{
	public static class ScoreExplanationPopup
	{
		// UI constants (matching other popups)
		const float OkBtnWidthFrac = 0.30f;
		const float OkBtnHeightMul = 1.5f;

		// ---------- Public API ----------
		public static void Open()
		{
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.ScoreExplanation);
		}

		// ---------- UI Drawing ----------
		public static void DrawMenu()
		{
			// Dimmed backdrop (same as other popups)
			MenuHelper.DrawBackgroundOverlay();

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				Draw.ID panelBG = Seb.Vis.UI.UI.ReservePanel();
				Draw.ID titleBG = Seb.Vis.UI.UI.ReservePanel();

				// --- Title banner ---
				Vector2 titlePos = Seb.Vis.UI.UI.CentreTop + Vector2.down * 8f;
				string title = "Score Explanation";
				Color headerCol = ColHelper.MakeCol255(44, 92, 62);
				Seb.Vis.UI.UI.DrawText(title, ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular * 2f, titlePos, Anchor.TextCentre, headerCol);
				//Seb.Vis.UI.UI.ModifyPanel(titleBG, Bounds2D.Grow(Seb.Vis.UI.UI.PrevBounds, 3f), Color.clear);

			// Content text (emphasizes nested NAND counting)
			string contentText = @"Your score is the TOTAL number of NAND gates used in your
solution, counting recursively through all nested custom chips.

• Lower scores are better
• Each NAND gate counts as 1 point
• Custom chips contribute their internal NAND count

Example: Using a custom chip containing 3 NAND gates counts
as 3 points, not 1. The scoring counts ALL NAND gates at every
nesting level.

NAND gates are fundamental building blocks that can be used
to create any other logic gate.";

				Vector2 textPos = Seb.Vis.UI.UI.PrevBounds.CentreBottom + Vector2.down * 1f;
				Seb.Vis.UI.UI.DrawText(contentText, ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular*0.95f, textPos, Anchor.CentreTop, Color.white);

				// --- Footer: Close button ---
				float closeWidth = Seb.Vis.UI.UI.Width * OkBtnWidthFrac;
				float closeHeight = ButtonHeight * OkBtnHeightMul;
				Vector2 buttonsTopLeft = Seb.Vis.UI.UI.PrevBounds.CentreBottom + Vector2.left * closeWidth / 2f;

				var res = MenuHelper.DrawOKButton(buttonsTopLeft, closeWidth, closeHeight, true);
				if (res == MenuHelper.CancelConfirmResult.Confirm) UIDrawer.SetActiveMenu(UIDrawer.MenuType.LevelValidationResult);

				// Panel BG spanning everything drawn in this scope
				MenuHelper.DrawReservedMenuPanel(panelBG, Seb.Vis.UI.UI.GetCurrentBoundsScope());
			}
		}
	}
}
