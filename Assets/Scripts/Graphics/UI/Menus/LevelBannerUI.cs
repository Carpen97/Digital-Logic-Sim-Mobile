using DLS.Game;
using DLS.Game.LevelsIntegration;
using DLS.SaveSystem;
using Seb.Helpers;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;
using static DLS.Graphics.DrawSettings;

namespace DLS.Graphics
{
	public static class LevelBannerUI
	{
		static int stepCountPrev;
		static string stepString;
		
		public static void DrawLevelBanner()
		{
			// Safety check - should not be called if level is not active
			if (LevelManager.Instance?.Current == null)
			{
				Debug.LogWarning("[LevelBannerUI] DrawLevelBanner called but no active level");
				return;
			}
			
			// Draw the banner panel with same colors as other banners
			Seb.Vis.UI.UI.DrawPanel(Seb.Vis.UI.UI.TopLeft, new Vector2(Seb.Vis.UI.UI.Width, InfoBarHeight*2.1f), new Color(0,0,0,0.5f), Anchor.TopLeft);
			Bounds2D panelBounds = Seb.Vis.UI.UI.PrevBounds;

			// Make the banner clickable for validation
			bool canValidate = Project.ActiveProject != null && Project.ActiveProject.CanEditViewedChip;
			
			// Create custom transparent grey color scheme
			var transparentGreyCols = new Seb.Vis.UI.ButtonTheme.StateCols
			{
				normal = new Color(0.2f, 0.2f, 0.2f, 0.3f),      // Transparent dark grey
				hover = new Color(0.3f, 0.3f, 0.3f, 0.4f),       // Slightly lighter when hovered
				pressed = new Color(0.4f, 0.4f, 0.4f, 0.5f),     // Even lighter when pressed
				inactive = new Color(0.1f, 0.1f, 0.1f, 0.2f)     // Darker when inactive
			};
			
			bool hoveringButton = Seb.Vis.UI.UI.Button("", ActiveUITheme.MenuButtonTheme, panelBounds.BottomLeft, panelBounds.Size, true, false, false, transparentGreyCols, Anchor.BottomLeft);
			if (canValidate && hoveringButton)
			{
				OnValidateButtonPressed();
			}

			// Draw level title
			Seb.Vis.UI.UI.DrawText($" <color=#ffffff> {LevelManager.Instance.Current.name}", MenuHelper.Theme.FontBold, MenuHelper.Theme.FontSizeRegular*1.25f, panelBounds.Centre + Vector2.up*1.5f, Anchor.TextCentre, Color.yellow);
			
			// Draw level description (closer to title for PC)
			Seb.Vis.UI.UI.DrawText($"{LevelManager.Instance.Current.description}", MenuHelper.Theme.FontBold, MenuHelper.Theme.FontSizeRegular*0.8f, panelBounds.Centre + Vector2.down*0.5f, Anchor.TextCentre, Color.yellow);
			
			// Draw "Press here to validate" text
			if (canValidate)
			{
				Seb.Vis.UI.UI.DrawText("Press here to validate", MenuHelper.Theme.FontBold, MenuHelper.Theme.FontSizeRegular*0.7f, panelBounds.Centre + Vector2.down*2.5f, Anchor.TextCentre, Color.white);
			}

		}

		static void OnValidateButtonPressed()
		{
			if (LevelManager.Instance == null || !LevelManager.Instance.IsActive)
				return;

			var report = LevelManager.Instance.RunValidation();
			LevelValidationPopup.Open(report);

			// Log results like the mobile version
			if (report.PassedAll)
			{
				// Get NAND gate count for display
				var adapter = new MobileSimulationAdapter();
				int nandCount = adapter.CountNandGates();
				Debug.Log($"[Levels] All tests passed ✅ — NAND Gates: {nandCount}");
			}
			else
			{
				Debug.Log($"[Levels] Validation failed — Stars={report.Stars}, Failures={report.Failures.Count}");
				foreach (var f in report.Failures)
					Debug.Log($"• inputs={f.Inputs} msg={f.Message}");
				foreach (var m in report.ConstraintMessages)
					Debug.Log($"• constraint: {m}");
			}
		}
	}
}
