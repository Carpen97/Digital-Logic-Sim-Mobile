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
	/// Draws the wire placement banner at the top of the screen when placing a wire.
	/// The banner is tappable to toggle straight wires override mode (like holding Shift on PC).
	/// </summary>
	public static class WirePlacementBanner
	{
		// Temporary override for straight wires (like holding Shift on PC)
		// Does NOT modify preferences - only active during wire placement
		private static bool straightWiresOverride = false;
		
		// Store banner bounds for touch detection
		private static Bounds2D bannerScreenBounds;
		private static bool hasBannerBounds = false;
		
		/// <summary>
		/// Gets whether straight wires should be forced ON (override active)
		/// </summary>
		public static bool ForceStraightWires => straightWiresOverride;
		
		/// <summary>
		/// Checks if a screen position is within the banner bounds
		/// </summary>
		public static bool IsTouchOverBanner(Vector2 screenPosition)
		{
			return hasBannerBounds && bannerScreenBounds.PointInBounds(screenPosition);
		}
		
		/// <summary>
		/// Reset override when wire placement ends
		/// </summary>
		public static void ResetOverride()
		{
			straightWiresOverride = false;
		}
		public static void DrawBanner()
		{
			// Only draw when actively placing a wire
			if (Project.ActiveProject?.controller?.WireToPlace == null)
			{
				// Reset override when not placing wires
				ResetOverride();
				hasBannerBounds = false;
				return;
			}

			// Banner color - yellow tint when override is active
			Color bannerColor = straightWiresOverride 
				? new Color(0.3f, 0.3f, 0.0f, 0.6f)  // Dark yellow tint
				: new Color(0, 0, 0, 0.5f);           // Normal black

			// Draw banner at top of screen (same style as eraser/level banners)
			Seb.Vis.UI.UI.DrawPanel(
				Seb.Vis.UI.UI.TopLeft, 
				new Vector2(Seb.Vis.UI.UI.Width, InfoBarHeight * 2.1f), 
				bannerColor,
				Anchor.TopLeft
			);
			Bounds2D panelBounds = Seb.Vis.UI.UI.PrevBounds;

			// Get straight wires mode text based on override and preferences
			var project = Project.ActiveProject;
			bool prefForcesStraight = project != null && (
				project.description.Prefs_StraightWires == 2 || // Always
				(project.description.Prefs_StraightWires == 1 && project.ShowGrid) // If Grid Shown
			);
			
			string modeText;
			string tapTextLine;
			#if UNITY_ANDROID || UNITY_IOS	
			if (straightWiresOverride)
			{
				// Override is active
				modeText = "Straight Wires: ON";
				tapTextLine = "(Tap to toggle)";
			}
			else if (prefForcesStraight)
			{
				// Preference is forcing straight wires
				modeText = "Straight Wires: ON";
				tapTextLine = "(Controlled by preference setting)";
			}
			else
			{
				// Normal mode
				modeText = "Straight Wires: OFF";
				tapTextLine = "(Tap to toggle)";
			}
			#else
				modeText = "(Hold shift for straight wires)";
				tapTextLine = "";
			#endif
			
			string mainText = "Placing Wire";
			
			// Draw main text (yellow)
			Seb.Vis.UI.UI.DrawText(
				mainText, 
				ActiveUITheme.FontBold, 
				ActiveUITheme.FontSizeRegular * 1.25f, 
				panelBounds.Centre + Vector2.up * 1.5f, 
				Anchor.TextCentre, 
				Color.yellow
			);

			// Draw mode text (centered)
			Seb.Vis.UI.UI.DrawText(
				modeText, 
				ActiveUITheme.FontBold, 
				ActiveUITheme.FontSizeRegular * 0.7f, 
				panelBounds.Centre + Vector2.down * 1.5f, // Moved down further
				Anchor.TextCentre, 
				Color.white
			);
			
			// Draw tap text (centered, below mode text)
			Seb.Vis.UI.UI.DrawText(
				tapTextLine, 
				ActiveUITheme.FontBold, 
				ActiveUITheme.FontSizeRegular * 0.7f, 
				panelBounds.Centre + Vector2.down * 4.0f, // Even further down
				Anchor.TextCentre, 
				Color.white
			);

		// Handle click/tap detection manually
		// Create hitbox for click detection
		float hitboxWidth = Seb.Vis.UI.UI.Width * 0.5f; // 50% of screen width
		Vector2 hitboxSize = new Vector2(hitboxWidth, panelBounds.Size.y);
		Vector2 hitboxPos = panelBounds.Centre;
		
		// Convert to screen space for mouse/touch detection and store for later checks
		Bounds2D hitboxBounds = Bounds2D.CreateFromCentreAndSize(hitboxPos, hitboxSize);
		bannerScreenBounds = Seb.Vis.UI.UI.UIToScreenSpace(hitboxBounds);
		hasBannerBounds = true;
		
		// Only allow toggling if preference isn't forcing straight wires
		if (!prefForcesStraight)
		{
			bool clicked = false;
			
#if UNITY_ANDROID || UNITY_IOS
			// Touch input for mobile
			if (Input.touchCount > 0)
			{
				Touch touch = Input.GetTouch(0);
				bool isOverBanner = bannerScreenBounds.PointInBounds(touch.position);
				bool touchBegan = touch.phase == TouchPhase.Began;
				clicked = isOverBanner && touchBegan;
			}
#else
			// Mouse input for editor/PC
			bool isOverBanner = InputHelper.MouseInBounds_ScreenSpace(bannerScreenBounds.Centre, bannerScreenBounds.Size);
			bool mouseDown = InputHelper.IsMouseDownThisFrame(MouseButton.Left);
			clicked = isOverBanner && mouseDown;
#endif

			if (clicked)
			{
				ToggleStraightWiresOverride();
				Debug.Log($"[WirePlacementBanner] Toggled straight wires override to: {straightWiresOverride}");
			}
		}
		}

		private static void ToggleStraightWiresOverride()
		{
			// Simple on/off toggle (like holding/releasing Shift on PC)
			straightWiresOverride = !straightWiresOverride;
		}
	}
}

