using System;
using System.Globalization;
using DLS.Description;
using DLS.Game;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using Unity.Android.Gradle;
using UnityEngine;

namespace DLS.Graphics
{
	public static class ProjectStatsMenu
	{
		const float entrySpacing = 0.5f;
		const float menuWidth = 55;
		const float verticalOffset = 22;

		static readonly Vector2 entrySize = new(menuWidth, DrawSettings.SelectorWheelHeight);
		public static readonly Vector2 settingFieldSize = new(entrySize.x / 3, entrySize.y);


		// ---- Stats ----
		static readonly string srscLabel /* Source engine label */ = "Steps ran since created";
		static readonly string tsscLabel = "Time spent since created";
		static readonly string createdOnLabel = "Created on";
		static readonly string chipsLabel = "Chips";
		static readonly string chipsUsedLabel = "Chips used";

		public static void DrawMenu()
		{
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
				Vector2 srscLabelRight = MenuHelper.DrawLabelSectionOfLabelInputPair(labelPosCurr, entrySize, srscLabel, labelCol * 0.75f, true);
				UI.DrawPanel(srscLabelRight, settingFieldSize, new Color(0.18f, 0.18f, 0.18f), Anchor.CentreRight);
				UI.DrawText(Project.ActiveProject.description.StepsRanSinceCreated.ToString(), theme.FontBold, theme.FontSizeRegular, srscLabelRight + new Vector2(inputTextPad - settingFieldSize.x, 0), Anchor.TextCentreLeft, Color.white);
				AddSpacing();

				Vector2 tsscLabelRight = MenuHelper.DrawLabelSectionOfLabelInputPair(labelPosCurr, entrySize, tsscLabel, labelCol * 0.75f, true);
				UI.DrawPanel(tsscLabelRight, settingFieldSize, new Color(0.18f, 0.18f, 0.18f), Anchor.CentreRight);
				UI.DrawText(FormatTime(Project.ActiveProject.description.TimeSpentSinceCreated.Elapsed), theme.FontBold, theme.FontSizeRegular, tsscLabelRight + new Vector2(inputTextPad - settingFieldSize.x, 0), Anchor.TextCentreLeft, Color.white);
				AddSpacing();

				Vector2 createdOnLabelRight = MenuHelper.DrawLabelSectionOfLabelInputPair(labelPosCurr, entrySize, createdOnLabel, labelCol * 0.75f, true);
				UI.DrawPanel(createdOnLabelRight, settingFieldSize, new Color(0.18f, 0.18f, 0.18f), Anchor.CentreRight);
				UI.DrawText(FormatTime(Project.ActiveProject.description.CreationTime), theme.FontBold, theme.FontSizeRegular, createdOnLabelRight + new Vector2(inputTextPad - settingFieldSize.x, 0), Anchor.TextCentreLeft, Color.white);
				AddSpacing();

				Vector2 chipsLabelRight = MenuHelper.DrawLabelSectionOfLabelInputPair(labelPosCurr, entrySize, chipsLabel, labelCol * 0.75f, true);
				UI.DrawPanel(chipsLabelRight, settingFieldSize, new Color(0.18f, 0.18f, 0.18f), Anchor.CentreRight);
				UI.DrawText(Project.ActiveProject.chipLibrary.allChips.Count.ToString(), theme.FontBold, theme.FontSizeRegular, chipsLabelRight + new Vector2(inputTextPad - settingFieldSize.x, 0), Anchor.TextCentreLeft, Color.white);
				AddSpacing();

				Vector2 chipsUsedLabelRight = MenuHelper.DrawLabelSectionOfLabelInputPair(labelPosCurr, entrySize, chipsUsedLabel, labelCol * 0.75f, true);
				UI.DrawPanel(chipsUsedLabelRight, settingFieldSize, new Color(0.18f, 0.18f, 0.18f), Anchor.CentreRight);
				UI.DrawText(GetChipsUsed().ToString(), theme.FontBold, theme.FontSizeRegular, chipsUsedLabelRight + new Vector2(inputTextPad - settingFieldSize.x, 0), Anchor.TextCentreLeft, Color.white);

				// Draw close
				Vector2 buttonTopLeft = new(labelPosCurr.x * 2.223f, UI.PrevBounds.Bottom - 1 * (DrawSettings.DefaultButtonSpacing * 6));
				bool result = UI.Button("CLOSE", MenuHelper.Theme.ButtonTheme, buttonTopLeft, new Vector2(menuWidth / 1.115f, 0));

				// Draw menu background
				Bounds2D menuBounds = UI.GetCurrentBoundsScope();
				MenuHelper.DrawReservedMenuPanel(panelID, menuBounds);

				// Close
				if (result)
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
			}

			return;

			void DrawHeader(string text)
			{
				AddHeaderSpacing();
				UI.DrawText(text, theme.FontBold, theme.FontSizeRegular, labelPosCurr, Anchor.TextCentreLeft, headerCol);
				AddHeaderSpacing();
			}

			void AddSpacing()
			{
				labelPosCurr.y -= entrySize.y + entrySpacing;
			}

			void AddHeaderSpacing()
			{
				labelPosCurr.y -= headerSpacing;
			}
		}
		static uint GetChipsUsed() {
			uint uses = 0;
			foreach (ChipDescription chip in Project.ActiveProject.chipLibrary.allChips)
				foreach (SubChipDescription subChip in chip.SubChips)
					uses++;

			return uses;
		}
		static string FormatTime(TimeSpan time) {
			if (time.Days == 0 && time.Hours == 0 && time.Minutes == 0)
				return $"{time.Seconds}s";
			else if (time.Days == 0 && time.Hours == 0)
				return $"{time.Minutes}m {time.Seconds}s";
			else if (time.Days == 0)
				return $"{time.Hours}h {time.Minutes}m {time.Seconds}s";
			else
				return $"{time.Days}d {time.Hours}h {time.Minutes}m {time.Seconds}s";
		}
		static string FormatTime(DateTime time) {
			return time.ToString(@"MMM dd\, yyyy", CultureInfo.InvariantCulture);
		}
	}
}