using System.Collections.Generic;
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
		public static void DrawLevelBanner()
		{
			// Safety check - should not be called if level is not active
			if (LevelManager.Instance?.Current == null)
			{
				Debug.LogWarning("[LevelBannerUI] DrawLevelBanner called but no active level");
				return;
			}

			string description = LevelManager.Instance.Current.description;
			float fontSize = MenuHelper.Theme.FontSizeRegular * 0.8f;
			FontType lineFont = MenuHelper.Theme.FontBold;
#if UNITY_ANDROID || UNITY_IOS
			string[] wrappedLines = StringHelper.WrapText(description, Seb.Vis.UI.UI.Width * 0.62f, lineFont, fontSize);
			int nLines = wrappedLines.Length;	
			float height = InfoBarHeight*1.4f;
			height += (nLines) * 1.5f;
#else
			string[] wrappedLines = StringHelper.WrapText(description, Seb.Vis.UI.UI.Width * 0.9f, lineFont, fontSize);
			int nLines = wrappedLines.Length;	
			float height = InfoBarHeight*2.1f;
			height += (nLines) * 1.5f;
#endif

			
			// Draw the banner panel with same colors as other banners
			Seb.Vis.UI.UI.DrawPanel(Seb.Vis.UI.UI.TopLeft, new Vector2(Seb.Vis.UI.UI.Width, height), new Color(0,0,0,0.5f), Anchor.TopLeft);
			Bounds2D panelBounds = Seb.Vis.UI.UI.PrevBounds;


			// Make the banner clickable for validation
			bool canValidate = Project.ActiveProject != null && Project.ActiveProject.CanEditViewedChip;
			
			// Handle click detection directly without drawing a button background
			// Create narrower hitbox (25% to 75% of screen width) while keeping visual panel full width
			float hitboxWidth = Seb.Vis.UI.UI.Width * 0.5f; // 50% of screen width (25% to 75%)
			Vector2 hitboxSize = new Vector2(hitboxWidth, panelBounds.Size.y);
			Vector2 hitboxPos = panelBounds.Centre; // Center the hitbox
			
			// Convert to screen space for mouse detection
			Bounds2D hitboxBounds = Bounds2D.CreateFromCentreAndSize(hitboxPos, hitboxSize);
			Bounds2D screenBounds = Seb.Vis.UI.UI.UIToScreenSpace(hitboxBounds);
			bool mouseOverHitbox = Seb.Helpers.InputHelper.MouseInBounds_ScreenSpace(screenBounds.Centre, screenBounds.Size);
			bool clicked = mouseOverHitbox && Seb.Helpers.InputHelper.IsMouseDownThisFrame(Seb.Helpers.MouseButton.Left);

			if (canValidate && clicked)
			{
				OnValidateButtonPressed();
			}

#if UNITY_ANDROID || UNITY_IOS
			Vector2 titlePos = Seb.Vis.UI.UI.CentreTop + Vector2.down * 2.5f + Vector2.left * 1.0f;
			Vector2 pos = Seb.Vis.UI.UI.CentreTop + Vector2.down * 4.5f;
#else
			Vector2 titlePos = panelBounds.Centre + Vector2.up * 1.5f + Vector2.left * 1.0f;
			Vector2 pos = panelBounds.Centre + Vector2.down*0.5f; 
#endif

			// Draw level title
			Seb.Vis.UI.UI.DrawText(
				$" <color=#ffffff> {LevelManager.Instance.Current.name}",
				MenuHelper.Theme.FontBold, MenuHelper.Theme.FontSizeRegular * 1.25f,
				titlePos,
				Anchor.TextCentre,
				Color.yellow
			);

			// Draw level description (closer to title for PC)

			for(int i = 0; i < wrappedLines.Length; i++)
            {
				Seb.Vis.UI.UI.DrawText($"{wrappedLines[i]}", lineFont, fontSize, pos, Anchor.TextCentre, Color.yellow);
				pos += Vector2.down * 1.5f;
            }
			
			pos += Vector2.down * .5f;
			// Draw "Press here to validate" text
			if (canValidate)
			{
				Seb.Vis.UI.UI.DrawText("Press here to validate", MenuHelper.Theme.FontBold, MenuHelper.Theme.FontSizeRegular*0.7f,pos, Anchor.TextCentre, Color.white);
			}


		}

	static void OnValidateButtonPressed()
	{
		if (LevelManager.Instance == null || !LevelManager.Instance.IsActive)
			return;

		// Save input pin states before validation
		var viewedChip = Project.ActiveProject?.ViewedChip;
		var savedStates = SaveInputPinStates(viewedChip);

		try
		{
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
		finally
		{
			// Restore input pin states after validation
			RestoreInputPinStates(viewedChip, savedStates);
		}
	}

	/// <summary>
	/// Saves the current states of all input pins.
	/// </summary>
	static Dictionary<object, bool> SaveInputPinStates(DevChipInstance viewedChip)
	{
		var originalStates = new Dictionary<object, bool>();
		
		if (viewedChip == null)
		{
			return originalStates;
		}
		
		var inputPins = viewedChip.GetInputPins();
		if (inputPins != null)
		{
			foreach (var inputPin in inputPins)
			{
				if (inputPin?.Pin != null)
				{
					originalStates[inputPin.Pin] = inputPin.Pin.PlayerInputState.FirstBitHigh();
				}
			}
		}
		
		return originalStates;
	}
	
	/// <summary>
	/// Restores the input pin states to their original values.
	/// </summary>
	static void RestoreInputPinStates(DevChipInstance viewedChip, Dictionary<object, bool> originalStates)
	{
		if (viewedChip == null || originalStates == null)
		{
			return;
		}
		
		var inputPins = viewedChip.GetInputPins();
		if (inputPins == null)
		{
			return;
		}
		
		foreach (var inputPin in inputPins)
		{
			if (inputPin?.Pin != null && originalStates.TryGetValue(inputPin.Pin, out bool originalState))
			{
				// Restore the original state using PlayerInputState
				inputPin.Pin.PlayerInputState.SetFirstBit(originalState);
			}
		}
	}
	}
}
