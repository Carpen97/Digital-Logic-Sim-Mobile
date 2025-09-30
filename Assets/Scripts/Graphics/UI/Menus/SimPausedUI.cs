using DLS.Game;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;
using static DLS.Graphics.DrawSettings;

namespace DLS.Graphics
{
	public static class SimPausedUI
	{
		static int stepCountPrev;
		static string stepString;
		
		public static void DrawPausedBanner()
		{
			UI.DrawPanel(UI.TopLeft, new Vector2(UI.Width, InfoBarHeight*2.1f), ActiveUITheme.InfoBarCol, Anchor.TopLeft);
			Bounds2D panelBounds = UI.PrevBounds;

			UI.DrawText("\t  Simulation Paused <color=#886600ff> \n(Tap here to advance one step)", MenuHelper.Theme.FontBold, MenuHelper.Theme.FontSizeRegular, panelBounds.Centre, Anchor.TextCentre, Color.yellow);

			if (stepCountPrev != Project.ActiveProject.simPausedSingleStepCounter || string.IsNullOrEmpty(stepString))
			{
				stepCountPrev = Project.ActiveProject.simPausedSingleStepCounter;
				stepString = Project.ActiveProject.simPausedSingleStepCounter + "";
			}
			#if UNITY_ANDROID || UNITY_IOS
			Vector2 frameLabelPos = panelBounds.CentreTop + Vector2.right * UI.Width * 0.27f + Vector2.down * UI.Height * 0.08f;
			UI.DrawText(stepString, ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular*1.2f, frameLabelPos, Anchor.TextCentreRight, Color.white * 0.8f);
			#else
			Vector2 frameLabelPos = panelBounds.CentreRight + Vector2.left * 1f;
			UI.DrawText(stepString, ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular, frameLabelPos, Anchor.TextCentreRight, Color.white * 0.8f);
			#endif
		}
	}
}
