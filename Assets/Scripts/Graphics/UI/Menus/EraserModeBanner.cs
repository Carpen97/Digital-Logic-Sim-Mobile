using DLS.Game;
using Seb.Helpers;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;
using static DLS.Graphics.DrawSettings;

namespace DLS.Graphics
{
	/// <summary>
	/// Draws the eraser mode banner at the top of the screen when eraser mode is active.
	/// The banner is tappable to toggle between "Delete All" and "Wires Only" modes.
	/// </summary>
	public static class EraserModeBanner
	{
		public static void DrawBanner()
		{
			if (!EraserModeController.IsActive) return;

			// Draw banner at top of screen (same style as level banner)
			Seb.Vis.UI.UI.DrawPanel(
				Seb.Vis.UI.UI.TopLeft, 
				new Vector2(Seb.Vis.UI.UI.Width, InfoBarHeight * 2.1f), 
				new Color(0, 0, 0, 0.5f), // Semi-transparent black like level banner
				Anchor.TopLeft
			);
			Bounds2D panelBounds = Seb.Vis.UI.UI.PrevBounds;

			// Draw mode text (yellow like level banner)
			string modeText = $"Eraser Mode: {EraserModeController.GetModeText()}";
			Color textColor = Color.yellow; // Yellow like level banner
			
			Seb.Vis.UI.UI.DrawText(
				modeText, 
				ActiveUITheme.FontBold, 
				ActiveUITheme.FontSizeRegular * 1.25f, 
				panelBounds.Centre + Vector2.up * 1.5f, 
				Anchor.TextCentre, 
				textColor
			);

			// Draw tap indicator
			string tapText = "(Tap to toggle mode)";
			Color indicatorColor = Color.white;
			
			Seb.Vis.UI.UI.DrawText(
				tapText, 
				ActiveUITheme.FontBold, 
				ActiveUITheme.FontSizeRegular * 0.7f, 
				panelBounds.Centre + Vector2.down * 0.5f, 
				Anchor.TextCentre, 
				indicatorColor
			);

			// Handle click/tap detection manually (like LevelBannerUI)
			// Create hitbox for click detection
			float hitboxWidth = Seb.Vis.UI.UI.Width * 0.5f; // 50% of screen width
			Vector2 hitboxSize = new Vector2(hitboxWidth, panelBounds.Size.y);
			Vector2 hitboxPos = panelBounds.Centre;
			
			// Convert to screen space for mouse/touch detection
			Bounds2D hitboxBounds = Bounds2D.CreateFromCentreAndSize(hitboxPos, hitboxSize);
			Bounds2D screenBounds = Seb.Vis.UI.UI.UIToScreenSpace(hitboxBounds);
			
			bool clicked = false;
			
			#if UNITY_ANDROID || UNITY_IOS
			// Touch input for mobile
			if (Input.touchCount > 0)
			{
				Touch touch = Input.GetTouch(0);
				bool touchInBounds = screenBounds.PointInBounds(touch.position);
				bool touchBegan = touch.phase == TouchPhase.Began;
				clicked = touchInBounds && touchBegan;
			}
			#else
			// Mouse input for editor/PC
			bool mouseOverHitbox = InputHelper.MouseInBounds_ScreenSpace(screenBounds.Centre, screenBounds.Size);
			bool mouseDown = InputHelper.IsMouseDownThisFrame(MouseButton.Left);
			clicked = mouseOverHitbox && mouseDown;
			#endif

			if (clicked)
			{
				EraserModeController.ToggleWiresOnlyMode();
				Debug.Log($"[EraserModeBanner] Toggled mode to: {EraserModeController.GetModeText()}");
			}
		}
	}
}

