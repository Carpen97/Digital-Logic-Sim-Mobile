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

	static readonly string[] shapeOptions =
	{
		"Shape: Rectangle",
		"Shape: Hexagon", 
		"Shape: Triangle",
		"Shape: Custom"
	};

		static readonly string[] hexagonRotationOptions =
		{
			"Rotation: 0°",
			"Rotation: 30°"
		};

		static readonly string[] triangleRotationOptions =
		{
			"Rotation: 0°",
			"Rotation: 90°",
			"Rotation: 180°",
			"Rotation: 270°"
		};

        // ---- State ----
        static SubChipInstance[] subChipsWithDisplays;
		static string displayLabelString;
		static string colHexCodeString;

		static readonly UIHandle ID_DisplaysScrollView = new("CustomizeMenu_DisplaysScroll");
		static readonly UIHandle ID_ColourPicker = new("CustomizeMenu_ChipCol");
		static readonly UIHandle ID_ColourHexInput = new("CustomizeMenu_ChipColHexInput");
		static readonly UIHandle ID_NameDisplayOptions = new("CustomizeMenu_NameDisplayOptions");
		static readonly UIHandle ID_CachingOptions = new("CustomizeMenu_CachingOptions");
		static readonly UIHandle ID_ShapeOptions = new("CustomizeMenu_ShapeOptions");
		static readonly UIHandle ID_RotationOptions = new("CustomizeMenu_RotationOptions");
		static readonly Seb.Vis.UI.UI.ScrollViewDrawElementFunc drawDisplayScrollEntry = DrawDisplayScroll;
        public static readonly UIHandle ID_LayoutOptions = new("CustomizeMenu_LayoutOptions");
		static readonly Func<string, bool> hexStringInputValidator = ValidateHexStringInput;
		public static bool isCustomLayout;
		
		// Collapsible right panel state
		static bool rightPanelExpanded = false; // Start minimized
		static readonly UIHandle ID_RightPanelToggle = new("CustomizeMenu_RightPanelToggle");
		
		// Custom polygon panel state
		static bool customPolygonPanelExpanded = true;
		static readonly UIHandle ID_CustomPolygonPanelToggle = new("CustomizeMenu_CustomPolygonPanelToggle");
		
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

			// Custom polygon panel (only show when custom polygon is selected)
			if (ChipSaveMenu.ActiveCustomizeDescription.ShapeType == ChipShapeType.CustomPolygon)
			{
				DrawCustomPolygonPanel(width, pw, pad, theme);
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

            // ---- Chip shape UI ----
            int shapeMode = Seb.Vis.UI.UI.WheelSelector(ID_ShapeOptions, shapeOptions, NextPos(), new Vector2(pw, DrawSettings.ButtonHeight * 1.5f), theme.OptionsWheel, Anchor.TopLeft);
            if (ChipSaveMenu.ActiveCustomizeDescription.ShapeType != (ChipShapeType)shapeMode)
            {
                ChipSaveMenu.ActiveCustomizeDescription.ShapeType = (ChipShapeType)shapeMode;
                
                // Initialize CustomPolygon data if switching to custom polygon
                if ((ChipShapeType)shapeMode == ChipShapeType.CustomPolygon && ChipSaveMenu.ActiveCustomizeDescription.CustomPolygon == null)
                {
                    ChipSaveMenu.ActiveCustomizeDescription.CustomPolygon = new CustomPolygonData();
                }
                
                RelocatePinsForNewShape((ChipShapeType)shapeMode);
            }

            // ---- Chip rotation UI ----
            string[] rotationOptions = GetRotationOptions((ChipShapeType)shapeMode);
            if (rotationOptions.Length > 1)
            {
                int rotationMode = Seb.Vis.UI.UI.WheelSelector(ID_RotationOptions, rotationOptions, NextPos(), new Vector2(pw, DrawSettings.ButtonHeight * 1.5f), theme.OptionsWheel, Anchor.TopLeft);
                ChipSaveMenu.ActiveCustomizeDescription.ShapeRotation = GetRotationValue((ChipShapeType)shapeMode, rotationMode);
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

            // Init shape mode
            WheelSelectorState shapeWheelState = Seb.Vis.UI.UI.GetWheelSelectorState(ID_ShapeOptions);
            shapeWheelState.index = (int)ChipSaveMenu.ActiveCustomizeDescription.ShapeType;

            // Init rotation mode
            WheelSelectorState rotationWheelState = Seb.Vis.UI.UI.GetWheelSelectorState(ID_RotationOptions);
            rotationWheelState.index = GetRotationIndex(ChipSaveMenu.ActiveCustomizeDescription.ShapeType, ChipSaveMenu.ActiveCustomizeDescription.ShapeRotation);
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

        static string[] GetRotationOptions(ChipShapeType shapeType)
        {
            switch (shapeType)
            {
                case ChipShapeType.Rectangle:
                    return new string[0]; // No rotation options for rectangle
                case ChipShapeType.Hexagon:
                    return hexagonRotationOptions;
                case ChipShapeType.Triangle:
                    return triangleRotationOptions;
                default:
                    return new string[0];
            }
        }

        static float GetRotationValue(ChipShapeType shapeType, int rotationIndex)
        {
            switch (shapeType)
            {
                case ChipShapeType.Hexagon:
                    return rotationIndex * 30f; // 0° or 30°
                case ChipShapeType.Triangle:
                    return rotationIndex * 90f; // 0°, 90°, 180°, 270°
                default:
                    return 0f;
            }
        }

        static int GetRotationIndex(ChipShapeType shapeType, float rotation)
        {
            switch (shapeType)
            {
                case ChipShapeType.Hexagon:
                    return Mathf.RoundToInt(rotation / 30f);
                case ChipShapeType.Triangle:
                    return Mathf.RoundToInt(rotation / 90f);
                default:
                    return 0;
            }
        }

        static void RelocatePinsForNewShape(ChipShapeType newShapeType)
        {
            var chip = ChipSaveMenu.ActiveCustomizeChip;
            Vector2 chipHalfSize = chip.Size / 2f;
            Vector2 chipCenter = chip.Position;

            // Relocate input pins
            for (int i = 0; i < chip.InputPins.Length; i++)
            {
                var pin = chip.InputPins[i];
                RelocatePin(pin, newShapeType, chipCenter, chipHalfSize);
            }

            // Relocate output pins
            for (int i = 0; i < chip.OutputPins.Length; i++)
            {
                var pin = chip.OutputPins[i];
                RelocatePin(pin, newShapeType, chipCenter, chipHalfSize);
            }
        }

        static void RelocatePin(PinInstance pin, ChipShapeType newShapeType, Vector2 chipCenter, Vector2 chipHalfSize)
        {
            // Get the current pin position (relative to chip center)
            Vector2 currentRelativePos;
            
            // Get position from description for custom shapes, or calculate from world position for builtin shapes
            if (pin.parent is SubChipInstance subchip && subchip.Description.ShapeType != ChipShapeType.Rectangle)
            {
                // Custom shape - get position from description
                currentRelativePos = GetPinPositionFromDescription(pin);
            }
            else
            {
                // Builtin shape - calculate from world position
                Vector2 currentWorldPos = pin.GetWorldPos();
                currentRelativePos = currentWorldPos - chipCenter;
            }

            // Project this position onto the nearest edge of the new shape
            Vector2 newRelativePos = ProjectOntoShapeEdge(newShapeType, ChipSaveMenu.ActiveCustomizeDescription.ShapeRotation, currentRelativePos, chipHalfSize);
            
            // Update the pin's position in the description
            UpdatePinPositionInDescription(pin, newRelativePos);
        }

        static Vector2 ProjectOntoShapeEdge(ChipShapeType shapeType, float rotation, Vector2 localPos, Vector2 chipHalfSize)
        {
            switch (shapeType)
            {
                case ChipShapeType.Rectangle:
                    return ProjectOntoRectangleEdge(localPos, chipHalfSize);
                case ChipShapeType.Hexagon:
                    return ProjectOntoHexagonEdge(localPos, chipHalfSize, rotation);
                case ChipShapeType.Triangle:
                    return ProjectOntoTriangleEdge(localPos, chipHalfSize, rotation);
                case ChipShapeType.CustomPolygon:
                    return ProjectOntoCustomPolygonEdge(localPos, chipHalfSize, rotation);
                default:
                    return localPos;
            }
        }

        static Vector2 ProjectOntoRectangleEdge(Vector2 localPos, Vector2 chipHalfSize)
        {
            // Find the closest edge and project onto it
            float distTop = Mathf.Abs(localPos.y - chipHalfSize.y);
            float distBottom = Mathf.Abs(localPos.y + chipHalfSize.y);
            float distRight = Mathf.Abs(localPos.x - chipHalfSize.x);
            float distLeft = Mathf.Abs(localPos.x + chipHalfSize.x);

            float minDist = Mathf.Min(distTop, distBottom, distRight, distLeft);

            if (minDist == distTop)
                return new Vector2(Mathf.Clamp(localPos.x, -chipHalfSize.x, chipHalfSize.x), chipHalfSize.y);
            else if (minDist == distBottom)
                return new Vector2(Mathf.Clamp(localPos.x, -chipHalfSize.x, chipHalfSize.x), -chipHalfSize.y);
            else if (minDist == distRight)
                return new Vector2(chipHalfSize.x, Mathf.Clamp(localPos.y, -chipHalfSize.y, chipHalfSize.y));
            else
                return new Vector2(-chipHalfSize.x, Mathf.Clamp(localPos.y, -chipHalfSize.y, chipHalfSize.y));
        }

        static Vector2 ProjectOntoHexagonEdge(Vector2 localPos, Vector2 chipHalfSize, float rotation)
        {
            // Use the same projection logic as CustomizationSceneDrawer
            float rotationRad = rotation * Mathf.Deg2Rad;
            Vector2[] vertices = new Vector2[6];
            
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI / 3f + rotationRad;
                vertices[i] = new Vector2(
                    Mathf.Cos(angle) * chipHalfSize.x,
                    Mathf.Sin(angle) * chipHalfSize.y
                );
            }
            
            // Find the closest edge by projecting onto each edge
            float closestDistance = float.MaxValue;
            Vector2 bestProjectedPoint = Vector2.zero;
            
            for (int i = 0; i < 6; i++)
            {
                int next = (i + 1) % 6;
                Vector2 edgeStart = vertices[i];
                Vector2 edgeEnd = vertices[next];
                Vector2 edgeDirection = edgeEnd - edgeStart;
                
                Vector2 toMouse = localPos - edgeStart;
                float projectionLength = Vector2.Dot(toMouse, edgeDirection.normalized);
                projectionLength = Mathf.Clamp(projectionLength, 0f, edgeDirection.magnitude);
                
                Vector2 projectedPoint = edgeStart + edgeDirection.normalized * projectionLength;
                float distance = Vector2.Distance(localPos, projectedPoint);
                
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    bestProjectedPoint = projectedPoint;
                }
            }
            
            return bestProjectedPoint;
        }

        static Vector2 ProjectOntoTriangleEdge(Vector2 localPos, Vector2 chipHalfSize, float rotation)
        {
            // Use the same projection logic as CustomizationSceneDrawer
            float rotationRad = rotation * Mathf.Deg2Rad;
            Vector2[] vertices = new Vector2[3];
            
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 2f * Mathf.PI / 3f + rotationRad;
                vertices[i] = new Vector2(
                    Mathf.Cos(angle) * chipHalfSize.x,
                    Mathf.Sin(angle) * chipHalfSize.y
                );
            }
            
            // Find the closest edge by projecting onto each edge
            float closestDistance = float.MaxValue;
            Vector2 bestProjectedPoint = Vector2.zero;
            
            for (int i = 0; i < 3; i++)
            {
                int next = (i + 1) % 3;
                Vector2 edgeStart = vertices[i];
                Vector2 edgeEnd = vertices[next];
                Vector2 edgeDirection = edgeEnd - edgeStart;
                
                Vector2 toMouse = localPos - edgeStart;
                float projectionLength = Vector2.Dot(toMouse, edgeDirection.normalized);
                projectionLength = Mathf.Clamp(projectionLength, 0f, edgeDirection.magnitude);
                
                Vector2 projectedPoint = edgeStart + edgeDirection.normalized * projectionLength;
                float distance = Vector2.Distance(localPos, projectedPoint);
                
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    bestProjectedPoint = projectedPoint;
                }
            }
            
            return bestProjectedPoint;
        }

        static Vector2 ProjectOntoCustomPolygonEdge(Vector2 localPos, Vector2 chipHalfSize, float rotation)
        {
            CustomPolygonData polygon = ChipSaveMenu.ActiveCustomizeDescription.CustomPolygon;
            if (polygon == null || polygon.Vertices == null || polygon.Vertices.Length < 3)
            {
                // Fallback to rectangle
                return ProjectOntoRectangleEdge(localPos, chipHalfSize);
            }

            float rotationRad = rotation * Mathf.Deg2Rad;
            
            // Convert normalized vertices to local positions
            Vector2[] vertices = new Vector2[polygon.Vertices.Length];
            for (int i = 0; i < polygon.Vertices.Length; i++)
            {
                Vector2 normalized = polygon.Vertices[i].ToVector2();
                Vector2 scaled = new Vector2(normalized.x * chipHalfSize.x, normalized.y * chipHalfSize.y);
                vertices[i] = RotateVector(scaled, rotationRad);
            }

            // Find the closest edge
            float closestDistance = float.MaxValue;
            Vector2 bestProjectedPoint = Vector2.zero;

            for (int i = 0; i < vertices.Length; i++)
            {
                int next = (i + 1) % vertices.Length;
                
                // Check if this edge is curved
                if (polygon.Edges != null && i < polygon.Edges.Length && polygon.Edges[i].IsCurved)
                {
                    // Project onto curved edge
                    Vector2 projected = ProjectOntoQuadraticBezier(vertices[i], vertices[next], polygon.Edges[i], localPos);
                    float distance = Vector2.Distance(localPos, projected);
                    
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        bestProjectedPoint = projected;
                    }
                }
                else
                {
                    // Project onto straight edge
                    Vector2 edgeStart = vertices[i];
                    Vector2 edgeEnd = vertices[next];
                    Vector2 edgeDirection = edgeEnd - edgeStart;
                    
                    Vector2 toMouse = localPos - edgeStart;
                    float projectionLength = Vector2.Dot(toMouse, edgeDirection.normalized);
                    projectionLength = Mathf.Clamp(projectionLength, 0f, edgeDirection.magnitude);
                    
                    Vector2 projectedPoint = edgeStart + edgeDirection.normalized * projectionLength;
                    float distance = Vector2.Distance(localPos, projectedPoint);
                    
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        bestProjectedPoint = projectedPoint;
                    }
                }
            }

            return bestProjectedPoint;
        }

        static Vector2 ProjectOntoQuadraticBezier(Vector2 start, Vector2 end, PolygonEdge edge, Vector2 point)
        {
            // Calculate control point
            Vector2 edgeMidpoint = (start + end) * 0.5f;
            Vector2 edgeDirection = end - start;
            Vector2 perpendicular = new Vector2(-edgeDirection.y, edgeDirection.x).normalized;
            Vector2 controlPoint = edgeMidpoint + perpendicular * edge.CurveStrength;

            // Sample the curve and find the closest point
            float closestDist = float.MaxValue;
            Vector2 closestPoint = start;
            int samples = 20;

            for (int i = 0; i <= samples; i++)
            {
                float t = i / (float)samples;
                Vector2 curvePoint = QuadraticBezier(start, controlPoint, end, t);
                float dist = Vector2.Distance(point, curvePoint);

                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestPoint = curvePoint;
                }
            }

            return closestPoint;
        }

        static Vector2 QuadraticBezier(Vector2 p0, Vector2 p1, Vector2 p2, float t)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            
            Vector2 point = uu * p0;
            point += 2 * u * t * p1;
            point += tt * p2;
            
            return point;
        }

        static Vector2 RotateVector(Vector2 vector, float rotationRad)
        {
            float cos = Mathf.Cos(rotationRad);
            float sin = Mathf.Sin(rotationRad);
            return new Vector2(
                vector.x * cos - vector.y * sin,
                vector.x * sin + vector.y * cos
            );
        }


        static void AddPolygonVertex(CustomPolygonData polygon)
        {
            // Add a new vertex between the first and second vertex
            int insertIndex = 1;
            Vector2 pos1 = polygon.Vertices[0].ToVector2();
            Vector2 pos2 = polygon.Vertices[1].ToVector2();
            Vector2 newPos = (pos1 + pos2) * 0.5f;

            // Create new arrays with one more element
            PolygonVertex[] newVertices = new PolygonVertex[polygon.Vertices.Length + 1];
            PolygonEdge[] newEdges = new PolygonEdge[polygon.Vertices.Length + 1];

            // Copy existing vertices
            for (int i = 0; i < insertIndex; i++)
            {
                newVertices[i] = polygon.Vertices[i];
                newEdges[i] = polygon.Edges[i];
            }

            // Insert new vertex
            newVertices[insertIndex] = new PolygonVertex(newPos.x, newPos.y);
            newEdges[insertIndex] = new PolygonEdge();

            // Copy remaining vertices
            for (int i = insertIndex; i < polygon.Vertices.Length; i++)
            {
                newVertices[i + 1] = polygon.Vertices[i];
                newEdges[i + 1] = polygon.Edges[i];
            }

            polygon.Vertices = newVertices;
            polygon.Edges = newEdges;
        }

        static void RemovePolygonVertex(CustomPolygonData polygon)
        {
            if (polygon.Vertices.Length <= 3) return; // Don't remove if only 3 vertices left

            // Remove the last vertex
            PolygonVertex[] newVertices = new PolygonVertex[polygon.Vertices.Length - 1];
            PolygonEdge[] newEdges = new PolygonEdge[polygon.Vertices.Length - 1];

            for (int i = 0; i < newVertices.Length; i++)
            {
                newVertices[i] = polygon.Vertices[i];
                newEdges[i] = polygon.Edges[i];
            }

            polygon.Vertices = newVertices;
            polygon.Edges = newEdges;
        }

        static void DrawCustomPolygonPanel(float width, float pw, float pad, DrawSettings.UIThemeDLS theme)
        {
            // Position the panel to the right of existing panels
            // Main panel width + right panel width + padding
            float existingPanelsWidth = width + width + pad * 2; // Main panel + right panel + padding
            Vector2 panelPos = new Vector2(existingPanelsWidth, Seb.Vis.UI.UI.Height - 4f - pad); // Bottom right
            Vector2 panelSize = new Vector2(width, 4f);
            
            if (customPolygonPanelExpanded)
            {
                // Draw the panel background
                Seb.Vis.UI.UI.DrawPanel(panelPos, panelSize, theme.MenuPanelCol, Anchor.TopLeft);
                
                // Panel header
                Color labelCol = ColHelper.Darken(theme.MenuPanelCol, 0.01f);
                Vector2 headerPos = panelPos + Vector2.down * pad;
                
                // Collapse button
                if (Seb.Vis.UI.UI.Button("POLYGON -", theme.ButtonTheme, headerPos, new Vector2(8, DrawSettings.ButtonHeight * 0.8f), true, true, false, theme.ButtonTheme.buttonCols, Anchor.TopLeft))
                {
                    customPolygonPanelExpanded = false;
                }
                
                // Panel content
                Vector2 contentPos = headerPos + Vector2.down * (DrawSettings.ButtonHeight * 0.8f + pad);
                DrawCustomPolygonEditor(pw, contentPos, pad);
            }
            else
            {
                // Minimized button
                Vector2 minimizedButtonPos = panelPos + Vector2.down * pad;
                Vector2 minimizedButtonSize = new Vector2(8, DrawSettings.ButtonHeight * 0.8f);
                
                if (Seb.Vis.UI.UI.Button("POLYGON +", theme.ButtonTheme, minimizedButtonPos, minimizedButtonSize, true, true, false, theme.ButtonTheme.buttonCols, Anchor.TopLeft))
                {
                    customPolygonPanelExpanded = true;
                }
            }
        }

        static void DrawCustomPolygonEditor(float panelWidth, Vector2 startPos, float pad)
        {
            CustomPolygonData polygon = ChipSaveMenu.ActiveCustomizeDescription.CustomPolygon;
            if (polygon == null) return;

            Vector2 currentPos = startPos;

            // Display current vertex count
            Seb.Vis.UI.UI.DrawText($"Vertices: {polygon.Vertices.Length}", UIThemeLibrary.DefaultFont, UIThemeLibrary.FontSizeDefault, currentPos, Anchor.TopLeft, Color.white);
            currentPos += Vector2.down * (UIThemeLibrary.FontSizeDefault + pad);

            // Add/Remove vertex buttons
            Vector2 btnSize = new Vector2(panelWidth / 2 - 5, DrawSettings.ButtonHeight);
            Vector2 leftBtnPos = currentPos;
            Vector2 rightBtnPos = leftBtnPos + new Vector2(panelWidth / 2 + 5, 0);

            // Add vertex button
            if (Seb.Vis.UI.UI.Button("Add Vertex", MenuHelper.Theme.ButtonTheme, leftBtnPos, btnSize, true, true, false, MenuHelper.Theme.ButtonTheme.buttonCols, Anchor.TopLeft))
            {
                AddPolygonVertex(polygon);
            }

            // Remove vertex button (only if more than 3 vertices)
            if (polygon.Vertices.Length > 3)
            {
                if (Seb.Vis.UI.UI.Button("Remove Vertex", MenuHelper.Theme.ButtonTheme, rightBtnPos, btnSize, true, true, false, MenuHelper.Theme.ButtonTheme.buttonCols, Anchor.TopLeft))
                {
                    RemovePolygonVertex(polygon);
                }
            }

            currentPos += Vector2.down * (DrawSettings.ButtonHeight + pad);

            // Instructions
            Seb.Vis.UI.UI.DrawText("Click vertices to drag them", UIThemeLibrary.DefaultFont, UIThemeLibrary.FontSizeSmall, currentPos, Anchor.TopLeft, new Color(0.7f, 0.7f, 0.7f));
            currentPos += Vector2.down * (UIThemeLibrary.FontSizeSmall + pad * 0.5f);
            Seb.Vis.UI.UI.DrawText("Click edge midpoints to curve", UIThemeLibrary.DefaultFont, UIThemeLibrary.FontSizeSmall, currentPos, Anchor.TopLeft, new Color(0.7f, 0.7f, 0.7f));
        }
		
		static void UpdatePinPositionInDescription(PinInstance pin, Vector2 newPosition)
		{
			// Find and update the pin's position in the chip description
			if (pin.parent is SubChipInstance subchip)
			{
				var chipDesc = subchip.Description;
				
				// Search in input pins
				if (chipDesc.InputPins != null)
				{
					for (int i = 0; i < chipDesc.InputPins.Length; i++)
					{
						if (chipDesc.InputPins[i].ID == pin.ID)
						{
							var pinDesc = chipDesc.InputPins[i];
							pinDesc.Position = newPosition;
							chipDesc.InputPins[i] = pinDesc;
							return;
						}
					}
				}
				
				// Search in output pins
				if (chipDesc.OutputPins != null)
				{
					for (int i = 0; i < chipDesc.OutputPins.Length; i++)
					{
						if (chipDesc.OutputPins[i].ID == pin.ID)
						{
							var pinDesc = chipDesc.OutputPins[i];
							pinDesc.Position = newPosition;
							chipDesc.OutputPins[i] = pinDesc;
							return;
						}
					}
				}
			}
		}
		
		static Vector2 GetPinPositionFromDescription(PinInstance pin)
		{
			// Find the pin's position in the chip description
			if (pin.parent is SubChipInstance subchip)
			{
				var chipDesc = subchip.Description;
				
				// Search in input pins
				if (chipDesc.InputPins != null)
				{
					foreach (var pinDesc in chipDesc.InputPins)
					{
						if (pinDesc.ID == pin.ID)
						{
							return pinDesc.Position;
						}
					}
				}
				
				// Search in output pins
				if (chipDesc.OutputPins != null)
				{
					foreach (var pinDesc in chipDesc.OutputPins)
					{
						if (pinDesc.ID == pin.ID)
						{
							return pinDesc.Position;
						}
					}
				}
			}
			
			return Vector2.zero;
		}

    }
}
