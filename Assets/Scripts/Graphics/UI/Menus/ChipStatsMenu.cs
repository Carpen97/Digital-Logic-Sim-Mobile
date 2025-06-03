using System.Runtime.CompilerServices;
using DLS.Description;
using DLS.Game;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	public static class ChipStatsMenu
	{
		const float entrySpacing = 0.5f;
		const float menuWidth = 55;
		const float verticalOffset = 22;

		static readonly Vector2 entrySize = new(menuWidth, DrawSettings.SelectorWheelHeight);
		public static readonly Vector2 settingFieldSize = new(entrySize.x / 3, entrySize.y);

		static string chip;

		// ---- Stats ----
		static readonly string usesLabel = "Uses";


		public static void DrawMenu(string chip)
		{
			//HandleKeyboardShortcuts();
			ChipStatsMenu.chip = chip;

			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			MenuHelper.DrawBackgroundOverlay();
			Draw.ID panelID = UI.ReservePanel();

			const int inputTextPad = 1;
			const float headerSpacing = 1.5f;
			Color labelCol = Color.white;
			Color headerCol = new(0.46f, 1, 0.54f);
			Vector2 topLeft = UI.Centre + new Vector2(-menuWidth / 2, verticalOffset);
			Vector2 labelPosCurr = topLeft;

			using (UI.BeginBoundsScope(true))
			{
				// Draw stats
				Vector2 usesLabelRight = MenuHelper.DrawLabelSectionOfLabelInputPair(labelPosCurr, entrySize, "Uses", labelCol * 0.75f, true);
				UI.DrawPanel(usesLabelRight, settingFieldSize, new Color(0.18f, 0.18f, 0.18f), Anchor.CentreRight);
				UI.DrawText(GetChipUses().ToString(), theme.FontBold, theme.FontSizeRegular, usesLabelRight + new Vector2(inputTextPad - settingFieldSize.x, 0), Anchor.TextCentreLeft, Color.white);

				// Draw close
				Vector2 buttonTopLeft = new(labelPosCurr.x, UI.PrevBounds.Bottom);
				bool result = UI.Button("CLOSE", MenuHelper.Theme.ButtonTheme, buttonTopLeft);

				// Draw menu background
				Bounds2D menuBounds = UI.GetCurrentBoundsScope();
				MenuHelper.DrawReservedMenuPanel(panelID, menuBounds);

				// Close
				if (result)
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
			}

			return;

			void AddSpacing()
			{
				labelPosCurr.y -= entrySize.y + entrySpacing;
			}

			void AddHeaderSpacing()
			{
				labelPosCurr.y -= headerSpacing;
			}
		}

		private static uint GetChipUses() {
			uint uses = 0;
			foreach (ChipDescription chip in Project.ActiveProject.chipLibrary.allChips)
				if (chip.Name != ChipStatsMenu.chip)
					foreach (SubChipDescription subChip in chip.SubChips)
						if (subChip.Name == ChipStatsMenu.chip) uses++;

			return uses;
		}
	}
}