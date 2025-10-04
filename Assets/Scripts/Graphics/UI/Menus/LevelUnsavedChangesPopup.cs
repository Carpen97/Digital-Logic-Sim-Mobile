using System;
using Seb.Helpers;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	/// <summary>
	/// Popup for handling unsaved changes in levels.
	/// Shows different messages depending on the context (exiting level vs creating new chip).
	/// </summary>
	public static class LevelUnsavedChangesPopup
	{
		static Action<int> onClosedCallback; // 0 = cancel, 1 = save and continue, 2 = continue without saving
		static bool isCreatingNewChip; // true if this is triggered by "New chip" action

		public static void DrawMenu()
		{
			MenuHelper.DrawBackgroundOverlay();

			// Different messages based on context
			string text = isCreatingNewChip 
				? "Would you like to save your changes to the level?\n\nYour progress will be lost if you don't save."
				: "The current level has unsaved changes.\nAre you sure you want to continue?";
			
			Color textCol = new(1, 0.4f, 0.45f);
			Vector2 textPos = Seb.Vis.UI.UI.Centre + Vector2.up * 5;

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();
				Draw.ID textBGPanelID = Seb.Vis.UI.UI.ReservePanel();
				Seb.Vis.UI.UI.DrawText(text, DrawSettings.ActiveUITheme.FontRegular, DrawSettings.ActiveUITheme.FontSizeRegular, textPos, Anchor.TextCentre, textCol);
				Seb.Vis.UI.UI.ModifyPanel(textBGPanelID, Bounds2D.Grow(Seb.Vis.UI.UI.PrevBounds, 1.5f), ColHelper.MakeCol(0.11f));

				Vector2 topLeft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * 1;
				
				// Draw 3 buttons vertically: Save and Continue, Continue without Saving, Cancel
				Vector2 buttonSize = new Vector2(Seb.Vis.UI.UI.PrevBounds.Width, DrawSettings.ButtonHeight * 1.6f);
				float buttonSpacing = DrawSettings.DefaultButtonSpacing;
				
				// Save and Continue button
				Vector2 saveButtonPos = topLeft;
				if (Seb.Vis.UI.UI.Button("SAVE AND CONTINUE", DrawSettings.ActiveUITheme.ButtonTheme, saveButtonPos, buttonSize, true, false, false, DrawSettings.ActiveUITheme.ButtonTheme.buttonCols, Anchor.TopLeft))
				{
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
					onClosedCallback?.Invoke(1); // Save and continue
				}
				
				// Continue without Saving button
				Vector2 continueButtonPos = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * buttonSpacing;
				if (Seb.Vis.UI.UI.Button("CONTINUE WITHOUT SAVING", DrawSettings.ActiveUITheme.ButtonTheme, continueButtonPos, buttonSize, true, false, false, DrawSettings.ActiveUITheme.ButtonTheme.buttonCols, Anchor.TopLeft))
				{
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
					onClosedCallback?.Invoke(2); // Continue without saving
				}
				
				// Cancel button (moved to bottom)
				Vector2 cancelButtonPos = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * buttonSpacing;
				if (Seb.Vis.UI.UI.Button("CANCEL", DrawSettings.ActiveUITheme.ButtonTheme, cancelButtonPos, buttonSize, true, false, false, DrawSettings.ActiveUITheme.ButtonTheme.buttonCols, Anchor.TopLeft))
				{
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
					onClosedCallback?.Invoke(0); // Cancel
				}

				MenuHelper.DrawReservedMenuPanel(panelID, Seb.Vis.UI.UI.GetCurrentBoundsScope());
			}
		}

		/// <summary>
		/// Open popup for level unsaved changes when creating a new chip
		/// </summary>
		public static void OpenPopupForNewChip(Action<int> callback)
		{
			isCreatingNewChip = true;
			onClosedCallback = callback;
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.LevelUnsavedChanges);
		}

		/// <summary>
		/// Open popup for level unsaved changes in other contexts
		/// </summary>
		public static void OpenPopup(Action<int> callback)
		{
			isCreatingNewChip = false;
			onClosedCallback = callback;
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.LevelUnsavedChanges);
		}
	}
}
