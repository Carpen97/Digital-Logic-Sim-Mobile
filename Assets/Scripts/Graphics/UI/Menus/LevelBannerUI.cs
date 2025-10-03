using DLS.Game;
using DLS.Game.LevelsIntegration;
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
			
			UI.DrawPanel(UI.TopLeft, new Vector2(UI.Width, InfoBarHeight*2.1f), new Color(0,0,0,0.5f), Anchor.TopLeft);
			Bounds2D panelBounds = UI.PrevBounds;

			UI.DrawText($" <color=#ffffff> {LevelManager.Instance.Current.name}", MenuHelper.Theme.FontBold, MenuHelper.Theme.FontSizeRegular*1.25f, panelBounds.Centre + Vector2.up*2f, Anchor.TextCentre, Color.yellow);
			UI.DrawText($"{LevelManager.Instance.Current.description}", MenuHelper.Theme.FontBold, MenuHelper.Theme.FontSizeRegular*0.8f, panelBounds.Centre + Vector2.down*2f, Anchor.TextCentre, Color.yellow);

			if (stepCountPrev != Project.ActiveProject.simPausedSingleStepCounter || string.IsNullOrEmpty(stepString))
			{
				stepCountPrev = Project.ActiveProject.simPausedSingleStepCounter;
				stepString = Project.ActiveProject.simPausedSingleStepCounter + "";
			}

			Vector2 frameLabelPos = panelBounds.CentreRight + Vector2.left * 1;
			UI.DrawText(stepString, ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular, frameLabelPos, Anchor.TextCentreRight, Color.white * 0.8f);
		}
	}
}
