using System;
using System.Collections.Generic;
using System.Linq;
using DLS.Description;
using DLS.Game;
using DLS.Simulation;
using Seb.Helpers;
using Seb.Vis;
using UnityEngine;
using Seb.Vis.UI;

namespace DLS.Graphics
{
	public static class ChipCustomizationMenu
	{

		static readonly string[] nameDisplayOptions =
		{
			"Name: Middle",
			"Name: Top",
			"Name: Hidden"
		};
        static readonly string[] layoutOptions =
        {
            "Layout: Default",
            "Layout: Custom"
        };

		static readonly string[] cachingOptions = { "Caching: Off", "Caching: On" };

        // ---- State ----
        static SubChipInstance[] subChipsWithDisplays;
		static string displayLabelString;
		static string colHexCodeString;

		static readonly UIHandle ID_DisplaysScrollView = new("CustomizeMenu_DisplaysScroll");
		static readonly UIHandle ID_ColourPicker = new("CustomizeMenu_ChipCol");
		static readonly UIHandle ID_ColourHexInput = new("CustomizeMenu_ChipColHexInput");
		static readonly UIHandle ID_NameDisplayOptions = new("CustomizeMenu_NameDisplayOptions");
		static readonly UIHandle ID_CachingOptions = new("CustomizeMenu_CachingOptions");
		static readonly Seb.Vis.UI.UI.ScrollViewDrawElementFunc drawDisplayScrollEntry = DrawDisplayScroll;
        public static readonly UIHandle ID_LayoutOptions = new("CustomizeMenu_LayoutOptions");
		static readonly Func<string, bool> hexStringInputValidator = ValidateHexStringInput;
		public static bool isCustomLayout;
		
		// Collapsible right panel state
		static bool rightPanelExpanded = false; // Start minimized
		static readonly UIHandle ID_RightPanelToggle = new("CustomizeMenu_RightPanelToggle");
		
		// Helper function to detect if we're moving or scaling a display
		static bool IsMovingOrScalingDisplay()
		{
			// Check if there's a selected display and we're in moving or scaling state
			return CustomizationSceneDrawer.SelectedDisplay != null;
		}
        public static void OnMenuOpened()
		{
			DevChipInstance chip = Project.ActiveProject.ViewedChip;
			subChipsWithDisplays = chip.GetSubchips().Where(c => c.Description.HasDisplay()).OrderBy(c => c.Position.x).ThenBy(c => c.Position.y).ToArray();
			CustomizationSceneDrawer.OnCustomizationMenuOpened();
			displayLabelString = $"DISPLAYS ({subChipsWithDisplays.Length}):";
            isCustomLayout = false;

            InitUIFromChipDescription();
		}

		public static void DrawMenu()
		{
			const float width = 25;
			const float pad = UILayoutHelper.DefaultSpacing;
			const float pw = width - pad * 2;

			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			
			// Left panel for main customization options - hide during display interactions
			if (!IsMovingOrScalingDisplay())
			{
				Seb.Vis.UI.UI.DrawPanel(Seb.Vis.UI.UI.TopLeft, new Vector2(width, Seb.Vis.UI.UI.Height), theme.MenuPanelCol, Anchor.TopLeft);
			}
			
			// Right panel for displays list (collapsible) - hide during any display interaction
			if (!CustomizationSceneDrawer.IsPlacingDisplay && !IsMovingOrScalingDisplay())
			{
				if (rightPanelExpanded)
				{
					// Full panel when expanded
					Seb.Vis.UI.UI.DrawPanel(Seb.Vis.UI.UI.TopRight, new Vector2(width, Seb.Vis.UI.UI.Height), theme.MenuPanelCol, Anchor.TopRight);
					
					// ---- Displays UI in right panel ----
					Color labelCol = ColHelper.Darken(theme.MenuPanelCol, 0.01f);
					Vector2 rightLabelPos = Seb.Vis.UI.UI.TopRight + Vector2.down * pad;
					
					// Make the header text itself clickable to minimize (reasonable width)
					if (Seb.Vis.UI.UI.Button("COMPONENTS -", theme.ButtonTheme, rightLabelPos, new Vector2(8, DrawSettings.ButtonHeight * 0.8f), true, true, false, theme.ButtonTheme.buttonCols, Anchor.TopRight))
					{
						rightPanelExpanded = false;
					}

					// Calculate full height scroll view - from below header to bottom of panel
					float headerHeight = DrawSettings.ButtonHeight * 0.8f + pad;
					float availableHeight = Seb.Vis.UI.UI.Height - headerHeight - pad; // Full panel height minus header and bottom padding
					float scrollViewSpacing = UILayoutHelper.DefaultSpacing;
					
					// Position scroll view below the header
					Vector2 scrollViewPos = new Vector2(Seb.Vis.UI.UI.Width - width + pad, rightLabelPos.y - headerHeight);
					Seb.Vis.UI.UI.DrawScrollView(ID_DisplaysScrollView, scrollViewPos, new Vector2(pw, availableHeight), scrollViewSpacing, Anchor.TopLeft, theme.ScrollTheme, drawDisplayScrollEntry, subChipsWithDisplays.Length);
				}
				else
				{
					// Minimized button in top right corner (reasonable width)
					Vector2 minimizedButtonPos = Seb.Vis.UI.UI.TopRight + Vector2.down * pad;
					Vector2 minimizedButtonSize = new Vector2(8, DrawSettings.ButtonHeight * 0.8f); // Reasonable width
					string minimizedButtonText = "COMPONENTS +";
					
					if (Seb.Vis.UI.UI.Button(minimizedButtonText, theme.ButtonTheme, minimizedButtonPos, minimizedButtonSize, true, true, false, theme.ButtonTheme.buttonCols, Anchor.TopRight))
					{
						rightPanelExpanded = true;
					}
				}
			}

            // ---- Cancel/confirm buttons ----
            int cancelConfirmButtonIndex = MenuHelper.DrawButtonPair("CANCEL", "CONFIRM", Seb.Vis.UI.UI.BottomLeft + Vector2.down * pad, pw, false);

			// Only show left panel content when not interacting with displays
			if (!IsMovingOrScalingDisplay())
			{
				// ---- Chip name UI ----
			theme.OptionsWheel.OverrideFontSize(2f);
			int nameDisplayMode = Seb.Vis.UI.UI.WheelSelector(ID_NameDisplayOptions, nameDisplayOptions, Seb.Vis.UI.UI.TopLeft + Vector2.down * pad, new Vector2(pw, DrawSettings.ButtonHeight * 1.5f), theme.OptionsWheel, Anchor.TopLeft);
			ChipSaveMenu.ActiveCustomizeDescription.NameLocation = (NameDisplayLocation)nameDisplayMode;
            // ---- Chip layout UI ----
            int layoutMode = Seb.Vis.UI.UI.WheelSelector(ID_LayoutOptions, layoutOptions, NextPos(), new Vector2(pw, DrawSettings.ButtonHeight * 1.5f), theme.OptionsWheel, Anchor.TopLeft);
            if (layoutMode == 0 && isCustomLayout)
            {
                // Switch to default layout
                isCustomLayout = false;
                ChipSaveMenu.ActiveCustomizeChip.SetCustomLayout(false);
                // Reset pins on the preview instance
                foreach (PinInstance pin in ChipSaveMenu.ActiveCustomizeChip.InputPins)
                {
                    pin.face = 3;
                    pin.LocalPosY = 0;
                }
                foreach (PinInstance pin in ChipSaveMenu.ActiveCustomizeChip.OutputPins)
                {
                    pin.face = 1;
                    pin.LocalPosY = 0;
                    //Reset layout
                    
                }
                ChipSaveMenu.ActiveCustomizeChip.updateMinSize();
                if (ChipSaveMenu.ActiveCustomizeChip.MinSize.x > ChipSaveMenu.ActiveCustomizeChip.Description.Size.x)
                {
                    ChipSaveMenu.ActiveCustomizeChip.Description.Size.x = ChipSaveMenu.ActiveCustomizeChip.MinSize.x;
                }
                if (ChipSaveMenu.ActiveCustomizeChip.MinSize.y > ChipSaveMenu.ActiveCustomizeChip.Description.Size.y)
                {
                    ChipSaveMenu.ActiveCustomizeChip.Description.Size.y = ChipSaveMenu.ActiveCustomizeChip.MinSize.y;
                }
                ChipSaveMenu.ActiveCustomizeChip.UpdatePinLayout();
            }
            else if (layoutMode == 1 && !isCustomLayout)
            {
                // Switch to custom layout
                isCustomLayout = true;
                ChipSaveMenu.ActiveCustomizeChip.SetCustomLayout(true);
            }

				// ---- Chip colour UI ----
				Color newCol = Seb.Vis.UI.UI.DrawColourPicker(ID_ColourPicker, NextPos(), pw, Anchor.TopLeft);
			InputFieldTheme inputTheme = MenuHelper.Theme.ChipNameInputField;
			inputTheme.fontSize = MenuHelper.Theme.FontSizeRegular;

			InputFieldState hexColInput = Seb.Vis.UI.UI.InputField(ID_ColourHexInput, inputTheme, NextPos(), new Vector2(pw, DrawSettings.ButtonHeight), "#", Anchor.TopLeft, 1, hexStringInputValidator);

			if (newCol != ChipSaveMenu.ActiveCustomizeDescription.Colour)
			{
				ChipSaveMenu.ActiveCustomizeDescription.Colour = newCol;
				UpdateChipColHexStringFromColour(newCol);
			}
			else if (colHexCodeString != hexColInput.text)
			{
				UpdateChipColFromHexString(hexColInput.text);
			}

			// ---- Chip caching UI ----
			Seb.Vis.UI.UI.DrawText("Chip Caching:", UIThemeLibrary.DefaultFont, UIThemeLibrary.FontSizeDefault, NextPos(1), Anchor.TopLeft, Color.white);
			SimChip chip = Project.ActiveProject.ViewedChip.SimChip;
			if (chip.IsCombinational())
			{
				int numberOfInputBits = chip.CalculateNumberOfInputBits();
				if (numberOfInputBits <= SimChip.MAX_NUM_INPUT_BITS_WHEN_AUTO_CACHING)
				{
					Seb.Vis.UI.UI.DrawText("This chip is being cached.", UIThemeLibrary.DefaultFont, UIThemeLibrary.FontSizeSmall, NextPos(), Anchor.TopLeft, Color.white);
				}
				else if (numberOfInputBits <= SimChip.MAX_NUM_INPUT_BITS_WHEN_USER_CACHING)
				{
					int shouldBeCachedNum = Seb.Vis.UI.UI.WheelSelector(ID_CachingOptions, cachingOptions, NextPos(), new Vector2(pw, DrawSettings.ButtonHeight * 1.5f), theme.OptionsWheel, Anchor.TopLeft);
					bool shouldBeCached = false;
					if (shouldBeCachedNum == 1) shouldBeCached = true;
					ChipSaveMenu.ActiveCustomizeDescription.ShouldBeCached = shouldBeCached;
					Seb.Vis.UI.UI.DrawText("WARNING: Caching chips", UIThemeLibrary.DefaultFont, UIThemeLibrary.FontSizeSmall, NextPos(), Anchor.TopLeft, Color.white);
					Seb.Vis.UI.UI.DrawText("with many input bits", UIThemeLibrary.DefaultFont, UIThemeLibrary.FontSizeSmall, NextPos(), Anchor.TopLeft, Color.white);
					Seb.Vis.UI.UI.DrawText("significantly increases", UIThemeLibrary.DefaultFont, UIThemeLibrary.FontSizeSmall, NextPos(), Anchor.TopLeft, Color.white);
					Seb.Vis.UI.UI.DrawText("the time required to", UIThemeLibrary.DefaultFont, UIThemeLibrary.FontSizeSmall, NextPos(), Anchor.TopLeft, Color.white);
					Seb.Vis.UI.UI.DrawText("create the cache and", UIThemeLibrary.DefaultFont, UIThemeLibrary.FontSizeSmall, NextPos(), Anchor.TopLeft, Color.white);
					Seb.Vis.UI.UI.DrawText("may also increase", UIThemeLibrary.DefaultFont, UIThemeLibrary.FontSizeSmall, NextPos(), Anchor.TopLeft, Color.white);
					Seb.Vis.UI.UI.DrawText("memory consumption!", UIThemeLibrary.DefaultFont, UIThemeLibrary.FontSizeSmall, NextPos(), Anchor.TopLeft, Color.white);
				}
				else
				{
					Seb.Vis.UI.UI.DrawText("This chip has too many input bits to be cached.", UIThemeLibrary.DefaultFont, UIThemeLibrary.FontSizeSmall, NextPos(), Anchor.TopLeft, Color.white);
				}
			}
			else
			{
				Seb.Vis.UI.UI.DrawText("Non-combinational chips", UIThemeLibrary.DefaultFont, UIThemeLibrary.FontSizeSmall, NextPos(), Anchor.TopLeft, Color.white);
				Seb.Vis.UI.UI.DrawText("can not be cached.", UIThemeLibrary.DefaultFont, UIThemeLibrary.FontSizeSmall, NextPos(), Anchor.TopLeft, Color.white);
			}

			// Add info button after caching content with proper spacing and centering
			Vector2 infoButtonPos = NextPos(1); // Add extra padding above
			infoButtonPos.x = Seb.Vis.UI.UI.TopLeft.x + width / 2f; // Center horizontally in the left panel
			Vector2 infoButtonSize = new Vector2(DrawSettings.ButtonHeight * 1.5f, DrawSettings.ButtonHeight * 0.8f);
			bool infoButtonPressed = Seb.Vis.UI.UI.Button(
				"info",
				theme.ButtonTheme,
				infoButtonPos,
				infoButtonSize,
				true,
				true,
				false,
				theme.ButtonTheme.buttonCols,
				Anchor.CentreTop
			);
			
			if (infoButtonPressed)
			{
				CachingExplanationPopup.Open();
			}

			// ---- Displays UI moved to right panel ----

			// ---- Cancel/confirm buttons positioned at bottom of left panel ----
			Vector2 buttonPos = Seb.Vis.UI.UI.BottomLeft + Vector2.up * (DrawSettings.ButtonHeight * 2 + pad * 2);
			cancelConfirmButtonIndex = MenuHelper.DrawButtonPair("CANCEL", "CONFIRM", buttonPos, pw, false);
			}

			Vector2 NextPos(float extraPadding = 0)
			{
				return Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * (pad + extraPadding);
			}

			// Cancel
			if (cancelConfirmButtonIndex == 0)
			{
				RevertChanges();
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.ChipSave);
			}
			// Confirm
			else if (cancelConfirmButtonIndex == 1)
			{
				UpdateCustomizeDescription();
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.ChipSave);
			}
		}

		static void DrawDisplayScroll(Vector2 pos, float width, int i, bool isLayoutPass)
		{
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;

			SubChipInstance subChip = subChipsWithDisplays[i];
			ChipDescription chipDesc = subChip.Description;
			string label = subChip.Label;
			string displayName = string.IsNullOrWhiteSpace(label) ? chipDesc.Name : label;

			// Don't allow adding same display multiple times
			bool enabled = CustomizationSceneDrawer.SelectedDisplay == null || subChip.ID != CustomizationSceneDrawer.SelectedDisplay.Desc.SubChipID; // display is removed from list when selected, so check manually here
			foreach (DisplayInstance d in ChipSaveMenu.ActiveCustomizeChip.Displays)
			{
				if (d.Desc.SubChipID == subChip.ID)
				{
					enabled = false;
					break;
				}
			}

			// Display selected, start placement
			if (Seb.Vis.UI.UI.Button(displayName, theme.ButtonTheme, pos, new Vector2(width, 0), enabled, false, true, theme.ButtonTheme.buttonCols, Anchor.TopLeft))
			{
				SubChipDescription subChipDesc = new(chipDesc.Name, subChipsWithDisplays[i].ID, string.Empty, Vector2.zero, null);
				SubChipInstance instance = new(chipDesc, subChipDesc);
				CustomizationSceneDrawer.StartPlacingDisplay(instance);
			}
		}

		static void RevertChanges()
		{
			ChipSaveMenu.RevertCustomizationStateToBeforeEnteringCustomizeMenu();
			InitUIFromChipDescription();
		}

		static void InitUIFromChipDescription()
		{
			// Init col picker to chip colour
			ColourPickerState chipColourPickerState = Seb.Vis.UI.UI.GetColourPickerState(ID_ColourPicker);
			Color.RGBToHSV(ChipSaveMenu.ActiveCustomizeDescription.Colour, out chipColourPickerState.hue, out chipColourPickerState.sat, out chipColourPickerState.val);
			UpdateChipColHexStringFromColour(chipColourPickerState.GetRGB());

			// Init name display mode
			WheelSelectorState nameDisplayWheelState = Seb.Vis.UI.UI.GetWheelSelectorState(ID_NameDisplayOptions);
			nameDisplayWheelState.index = (int)ChipSaveMenu.ActiveCustomizeDescription.NameLocation;

			// Init cache setting
			WheelSelectorState cacheSettingWheelState = Seb.Vis.UI.UI.GetWheelSelectorState(ID_CachingOptions);
			bool cacheBool = ChipSaveMenu.ActiveCustomizeDescription.ShouldBeCached;
			int cacheInt = 0;
			if (cacheBool) cacheInt = 1;
			cacheSettingWheelState.index = cacheInt;
      
            // Init layout mode by checking if any pins have custom positions
            isCustomLayout = Project.ActiveProject.ViewedChip.HasCustomLayout;

            WheelSelectorState layoutWheelState = Seb.Vis.UI.UI.GetWheelSelectorState(ID_LayoutOptions);
            layoutWheelState.index = isCustomLayout ? 1 : 0;
    }

		static void UpdateCustomizeDescription()
		{
			List<DisplayInstance> displays = ChipSaveMenu.ActiveCustomizeChip.Displays;
			ChipSaveMenu.ActiveCustomizeDescription.Displays = displays.Select(s => s.Desc).ToArray();
			ChipSaveMenu.ActiveCustomizeDescription.HasCustomLayout = isCustomLayout;

            //Saves pin offset and faces
            for (int i = 0; i < ChipSaveMenu.ActiveCustomizeChip.Description.InputPins.Length; i++)
				{
                ChipSaveMenu.ActiveCustomizeDescription.InputPins[i].LocalOffset = ChipSaveMenu.ActiveCustomizeChip.InputPins[i].LocalPosY;
                ChipSaveMenu.ActiveCustomizeDescription.InputPins[i].face = ChipSaveMenu.ActiveCustomizeChip.InputPins[i].face;

                ChipSaveMenu.ActiveCustomizeChip.Description.InputPins[i].LocalOffset = ChipSaveMenu.ActiveCustomizeChip.InputPins[i].LocalPosY;
                ChipSaveMenu.ActiveCustomizeChip.Description.InputPins[i].face = ChipSaveMenu.ActiveCustomizeChip.InputPins[i].face;

            }
            for (int i = 0; i < ChipSaveMenu.ActiveCustomizeChip.Description.OutputPins.Length; i++)
            {
                ChipSaveMenu.ActiveCustomizeDescription.OutputPins[i].LocalOffset = ChipSaveMenu.ActiveCustomizeChip.OutputPins[i].LocalPosY;
                ChipSaveMenu.ActiveCustomizeDescription.OutputPins[i].face = ChipSaveMenu.ActiveCustomizeChip.OutputPins[i].face;

                ChipSaveMenu.ActiveCustomizeChip.Description.OutputPins[i].LocalOffset = ChipSaveMenu.ActiveCustomizeChip.OutputPins[i].LocalPosY;
                ChipSaveMenu.ActiveCustomizeChip.Description.OutputPins[i].face = ChipSaveMenu.ActiveCustomizeChip.OutputPins[i].face;
            }
        }

		static void UpdateChipColHexStringFromColour(Color col)
		{
			int colInt = (byte)(col.r * 255) << 16 | (byte)(col.g * 255) << 8 | (byte)(col.b * 255);
			colHexCodeString = "#" + $"{colInt:X6}";
			Seb.Vis.UI.UI.GetInputFieldState(ID_ColourHexInput).SetText(colHexCodeString, false);
		}

		static void UpdateChipColFromHexString(string hexString)
		{
			colHexCodeString = hexString;
			hexString = hexString.Replace("#", "");
			hexString = hexString.PadRight(6, '0');

			if (ColHelper.TryParseHexCode(hexString, out Color col))
			{
				Seb.Vis.UI.UI.GetColourPickerState(ID_ColourPicker).SetRGB(col);
				ChipSaveMenu.ActiveCustomizeDescription.Colour = col;
			}
		}

		static bool ValidateHexStringInput(string text)
		{
			if (string.IsNullOrWhiteSpace(text)) return true;

			int numHexDigits = 0;

			for (int i = 0; i < text.Length; i++)
			{
				if (i == 0 && text[i] == '#') continue;

				if (Uri.IsHexDigit(text[i]))
				{
					numHexDigits++;
				}
				else return false;
			}

			return numHexDigits <= 6;
		}


        static void FaceSnapping(PinInstance pin, float mouseY)
            {
                if (pin.parent is SubChipInstance chip)
                {
                    Vector2 chipSize = chip.Size;
                    Vector2 chipPos = chip.Position;

                    //Calculate distances to top and bottom edges
                    float distanceToTop = Mathf.Abs(mouseY - (chipPos.y + chipSize.y / 2));
                    float distanceToBottom = Mathf.Abs(mouseY - (chipPos.y - chipSize.y / 2));

                    //Calculate distances to left and right edges
                    float distanceToLeft = Mathf.Abs(chipPos.x - chipSize.x / 2);
                    float distanceToRight = Mathf.Abs(chipPos.x + chipSize.x / 2);

                    //Determine the closest vertical edge (top or bottom)
                    float closestVerticalDistance = Mathf.Min(distanceToTop, distanceToBottom);
                    bool isTopCloser = closestVerticalDistance == distanceToTop;

                    //Determine the closest horizontal edge (left or right)
                    float closestHorizontalDistance = Mathf.Min(distanceToLeft, distanceToRight);
                    bool isLeftCloser = closestHorizontalDistance == distanceToLeft;
                    //Compare the closest of the 2 previously closests
                    if (closestVerticalDistance < closestHorizontalDistance)
                    {
                        if (isTopCloser) {pin.face = 0;}
                        else {pin.face = 2;}
                    }
                    else
                    {
                        if (isLeftCloser) {pin.face = 3;}
                        else{ pin.face = 1; }
                    }
                }
            }
    }
}
