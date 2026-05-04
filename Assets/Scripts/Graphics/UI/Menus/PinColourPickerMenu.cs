using DLS.Description;
using DLS.Game;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	public static class PinColourPickerMenu
	{
		static PinInstance targetPin;
		static readonly UIHandle ID_ColourPicker = new("PinColourPicker_ColourPicker");

		// Saved on open for revert-on-cancel
		static PinColour initialColour;
		static uint initialCustomColourPacked;

		/// <summary>Gets the current display colour for a pin (preset or custom).</summary>
		public static Color GetPinDisplayColour(PinInstance pin)
		{
			if (pin.CustomColourPacked != 0)
				return DrawSettings.UnpackCustomColour(pin.CustomColourPacked);
			return DrawSettings.ActiveTheme.StateHighCol[Mathf.Min((int)pin.Colour, DrawSettings.ActiveTheme.StateHighCol.Length - 1)];
		}

		public static void DrawMenu()
		{
			MenuHelper.DrawBackgroundOverlay();
			Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;

			const float pickerSize = 12f;
			const float titleToPickerSpacing = 2f;
			float centreX = Seb.Vis.UI.UI.Centre.x;
			Vector2 topPos = Seb.Vis.UI.UI.Centre + Vector2.up * (Seb.Vis.UI.UI.HalfHeight * 0.25f);

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				// Title at top (above picker, so it doesn't overlap buttons)
				Vector2 titlePos = new(centreX, topPos.y);
				Seb.Vis.UI.UI.DrawQuad(titlePos, new Vector2(pickerSize, 0.01f), new Color(0, 0, 0, 0), Anchor.CentreTop);
				Seb.Vis.UI.UI.DrawText("Set Pin Colour", theme.FontBold, theme.FontSizeRegular, titlePos, Anchor.TextCentre, Color.white * 0.8f);

				Vector2 pickerPos = new(centreX - pickerSize / 2f, topPos.y - theme.FontSizeRegular - titleToPickerSpacing);
				Color newCol = Seb.Vis.UI.UI.DrawColourPicker(ID_ColourPicker, pickerPos, pickerSize, Anchor.TopLeft);

				// Live update: apply to pin (full opacity for wires/pins)
				uint packed = DrawSettings.PackPinColour(newCol);
				targetPin.CustomColourPacked = packed;
				targetPin.Colour = PinColour.Red; // Keep enum as fallback; display uses CustomColourPacked when non-zero

				float boundsWidth = Seb.Vis.UI.UI.GetCurrentBoundsScope().Width;
				MenuHelper.CancelConfirmResult result = MenuHelper.DrawCancelConfirmButtons(Seb.Vis.UI.UI.GetCurrentBoundsScope().BottomLeft, boundsWidth * 1.2f, true);

				MenuHelper.DrawReservedMenuPanel(panelID, Seb.Vis.UI.UI.GetCurrentBoundsScope());

				if (result == MenuHelper.CancelConfirmResult.Cancel)
				{
					RevertToInitialState();
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				}
				else if (result == MenuHelper.CancelConfirmResult.Confirm)
				{
					// Live updates already applied
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				}
			}
		}

		static void RevertToInitialState()
		{
			if (targetPin == null) return;
			targetPin.Colour = initialColour;
			targetPin.CustomColourPacked = initialCustomColourPacked;
		}

		public static void OnMenuOpened()
		{
			targetPin = (PinInstance)ContextMenu.interactionContext;

			// Save initial state for revert-on-cancel
			initialColour = targetPin.Colour;
			initialCustomColourPacked = targetPin.CustomColourPacked;

			// Init colour picker to current pin colour
			Color currentCol = GetPinDisplayColour(targetPin);
			Seb.Vis.UI.UI.GetColourPickerState(ID_ColourPicker).SetRGB(currentCol);
		}
	}
}
