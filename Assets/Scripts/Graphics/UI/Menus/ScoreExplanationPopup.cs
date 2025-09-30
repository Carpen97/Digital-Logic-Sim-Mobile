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

			using (UI.BeginBoundsScope(true))
			{
				Draw.ID panelBG = UI.ReservePanel();
				Draw.ID titleBG = UI.ReservePanel();

				// --- Title banner ---
				Vector2 titlePos = UI.CentreTop + Vector2.down * 8f;
				string title = "Score Explanation";
				Color headerCol = ColHelper.MakeCol255(44, 92, 62);
				UI.DrawText(title, ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular * 2f, titlePos, Anchor.TextCentre, headerCol);
				//UI.ModifyPanel(titleBG, Bounds2D.Grow(UI.PrevBounds, 3f), Color.clear);

				// Content text (shorter, more concise version)
				string contentText = @"Your score is calculated based on the number of NAND gates 
used in your solution.

• Lower scores are better
• Each NAND gate counts as 1 point
• Try to minimize the number of NAND gates used

NAND gates are fundamental building blocks that can be used
to create any other logic gate.";

				Vector2 textPos = UI.PrevBounds.CentreBottom + Vector2.down * 5f;
				UI.DrawText(contentText, ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular, textPos, Anchor.CentreTop, Color.white);

				// --- Footer: Close button ---
				float closeWidth = UI.Width * OkBtnWidthFrac;
				float closeHeight = ButtonHeight * OkBtnHeightMul;
				Vector2 buttonsTopLeft = UI.PrevBounds.CentreBottom + Vector2.left * closeWidth / 2f;

				var res = MenuHelper.DrawOKButton(buttonsTopLeft, closeWidth, closeHeight, true);
				if (res == MenuHelper.CancelConfirmResult.Confirm) UIDrawer.SetActiveMenu(UIDrawer.MenuType.LevelValidationResult);

				// Panel BG spanning everything drawn in this scope
				MenuHelper.DrawReservedMenuPanel(panelBG, UI.GetCurrentBoundsScope());
			}
		}
	}
}
