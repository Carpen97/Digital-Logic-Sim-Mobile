using System;
using System.Linq;
using DLS.Description;
using ChipCollection = DLS.Description.ChipCollection;
using DLS.Game;
using DLS.Game.LevelsIntegration;
using DLS.SaveSystem;
using Seb.Helpers;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	public static class BottomBarUI
	{
		#if UNITY_ANDROID || UNITY_IOS
		public const float barHeight = 5;
		#else
		public const float barHeight = 3;
		#endif
		const float padY = 0.3f;
		public const float buttonSpacing = 0.25f;
		const float buttonHeight = barHeight - padY * 2;

		const string shortcutTextCol = "<color=#666666ff>";

		const float scrollButtonWidth = 1.2f;
		public static int showScrollingButtons = 0;
		#if UNITY_ANDROID || UNITY_IOS

		static readonly string[] menuButtonNames =
		{
			$"  NEW CHIP  ",
			$"  SAVE CHIP  ",
			$"  FIND CHIP  ",
			$"  ADD SPECIAL  ",
			$"  LIBRARY  ",
			$"  STATS  ",
			$"  PREFS  ",
			$"  LEVELS  ",
			$"  QUIT  "
		};
		#else
		static readonly string[] menuButtonNames =
		{
			$"NEW CHIP     {shortcutTextCol}Ctrl+N",
			$"SAVE CHIP    {shortcutTextCol}Ctrl+S",
			$"FIND CHIP    {shortcutTextCol}Ctrl+F",
			$"ADD SPECIAL  {shortcutTextCol}Ctrl+B",
			$"LIBRARY      {shortcutTextCol}Ctrl+L",
			$"STATS        {shortcutTextCol}Ctrl+T", // Ctrl+'T' from the T in Stats
			$"PREFS        {shortcutTextCol}Ctrl+P",
			$"LEVELS       {shortcutTextCol}Ctrl+E",
			$"QUIT         {shortcutTextCol}Ctrl+Q"
		};
		#endif

		const int NewChipButtonIndex = 0;
		const int SaveChipButtonIndex = 1;
		const int FindChipButtonIndex = 2;
		const int AddSpecialButtonIndex = 3;
		const int LibraryButtonIndex = 4;
		const int StatsButtonIndex = 5;
		const int OptionsButtonIndex = 6;
		const int LevelsButtonIndex = 7; // insert before Options/Quit
		const int QuitButtonIndex = 8;

		// ---- State ----
		static float scrollX;
		static float chipBarTotalWidthLastFrame;
		static bool isDraggingChipBar;
		static float mouseDragPrev;
		static bool closeActiveCollectionMultiModeExit;

		static int toggleMenuFrame;
		static int collectionInteractFrame;
		static ChipCollection activeCollection;
		static Vector2 collectionPopupBottomLeft;
		static Bounds2D barBounds_ScreenSpace;
		
		// Nested collection expansion state
		static ChipCollection activeNestedCollection;
		static Vector2 nestedCollectionPopupBottomLeft;
		static int nestedCollectionInteractFrame;
		static float clickedItemY; // Track Y position of the clicked item for alignment
		
		// Hover state tracking to prevent flicker
		static string hoveredNestedCollectionName;
		static int hoverStartFrame;
		static int hoverDelayFrames = 30; // Frames to wait before expanding on hover

		static bool MenuButtonsAndShortcutsEnabled => Project.ActiveProject.CanEditViewedChip;

	static bool ShouldHideChipInLevel(ChipType chipType)
	{
		var lm = LevelManager.Instance;
		bool isLevelActive = lm != null && lm.IsActive;
		return isLevelActive
			&& (chipType == ChipType.In_Pin || chipType == ChipType.Out_Pin);
	}

	static bool IsSpecialChipDisabledInLevel(ChipType chipType)
	{
		var lm = LevelManager.Instance;
		if (lm == null || !lm.IsActive) return false;
		
		// Check if chip type is in our "special" list
		return chipType == ChipType.Rom_256x16 ||
		       chipType == ChipType.EEPROM_256x16 ||
		       chipType == ChipType.dev_Ram_8Bit ||
		       chipType == ChipType.SevenSegmentDisplay ||
		       chipType == ChipType.DisplayRGB ||
		       chipType == ChipType.DisplayRGBTouch ||
		       chipType == ChipType.DisplayDot ||
		       chipType == ChipType.DisplayLED ||
		       chipType == ChipType.Pulse ||
		       chipType == ChipType.Clock ||
		       chipType == ChipType.Key ||
		       chipType == ChipType.Button ||
		       chipType == ChipType.Toggle ||
		       chipType == ChipType.Detector ||
		       chipType == ChipType.Buzzer ||
		       chipType == ChipType.RTC ||
		       chipType == ChipType.SPS ||
		       chipType == ChipType.Constant_8Bit;
	}


	public static void DrawUI(Project project)
		{
			DrawBottomBar(project);

			if (UIDrawer.ActiveMenu == UIDrawer.MenuType.BottomBarMenuPopup)
			{
				DrawPopupMenu();
			}

			if (UIDrawer.ActiveMenu is UIDrawer.MenuType.BottomBarMenuPopup or UIDrawer.MenuType.None)
			{
				HandleKeyboardShortcuts();
			}
		}

		static void DrawPopupMenu()
		{
			ButtonTheme theme = DrawSettings.ActiveUITheme.MenuPopupButtonTheme;
			float menuWidth = Draw.CalculateTextBoundsSize(menuButtonNames[0].AsSpan(), theme.fontSize, theme.font).x + 1;

			Vector2 pos = new(buttonSpacing, barHeight + buttonSpacing);
			Vector2 size = new(menuWidth*1.3f, buttonHeight*1f);;
			Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				for (int i = menuButtonNames.Length - 1; i >= 0; i--)
				{
					bool buttonEnabled = MenuButtonsAndShortcutsEnabled || i is QuitButtonIndex or OptionsButtonIndex;
					string text = menuButtonNames[i];
					
					// Show "Save" instead of "Save Chip" when in a level
					if (i == SaveChipButtonIndex && LevelManager.Instance?.IsActive == true)
					{
						text = text.Replace("SAVE CHIP    ", "SAVE         ").Replace("Save Chip", "Save");
					}
					
					if (Seb.Vis.UI.UI.Button(text, theme, pos, size, buttonEnabled, false, false, theme.buttonCols, Anchor.BottomLeft))
					{
						ButtonPressed(i);
					}

					pos = Seb.Vis.UI.UI.PrevBounds.TopLeft;
				}

				Bounds2D uiBounds = Seb.Vis.UI.UI.GetCurrentBoundsScope();
				Seb.Vis.UI.UI.ModifyPanel(panelID, uiBounds.Centre, uiBounds.Size + Vector2.one * (buttonSpacing * 2), Color.white);
			}

			// Close if clicked nothing or pressed esc
			if (UIDrawer.ActiveMenu is UIDrawer.MenuType.BottomBarMenuPopup)
			{
				if (InputHelper.IsAnyMouseButtonDownThisFrame_IgnoreConsumed() && Time.frameCount != toggleMenuFrame)
				{
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				}

				if (KeyboardShortcuts.CancelShortcutTriggered)
				{
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				}
			}

			void ButtonPressed(int i)
			{
				if (i == NewChipButtonIndex) CreateNewChip();
				else if (i == SaveChipButtonIndex) OpenSaveMenu();
				else if (i == FindChipButtonIndex) OpenSearchMenu();
				else if (i == AddSpecialButtonIndex) OpenAddSpecialMenu();
				else if (i == LibraryButtonIndex) OpenLibraryMenu();
				else if (i == StatsButtonIndex) OpenStatsMenu();
				else if (i == OptionsButtonIndex) OpenPreferencesMenu();
				else if (i == LevelsButtonIndex) OpenLevelsMenu();
				else if (i == QuitButtonIndex) ExitToMainMenu();
			}
		}

		static void DrawBottomBar(Project project)
		{
			Bounds2D bounds_UISpace = new(Vector2.zero, new Vector2(Seb.Vis.UI.UI.Width, barHeight));
			barBounds_ScreenSpace = Seb.Vis.UI.UI.UIToScreenSpace(bounds_UISpace);

			bool inOtherMenu = !(UIDrawer.ActiveMenu is UIDrawer.MenuType.BottomBarMenuPopup or UIDrawer.MenuType.None);
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			// If mouse is over context menu, then ignore inputs (don't want to actually disable the buttons because the grey-out effect is distracting here)
			// Also if middle mouse is held, as may be dragging the bar so don't want to select buttons
			bool ignoreInputs = ContextMenu.HasFocus() || InputHelper.IsMouseHeld(MouseButton.Middle);
			bool isRightClick = InputHelper.IsMouseDownThisFrame(MouseButton.Right);
			if (closeActiveCollectionMultiModeExit && !KeyboardShortcuts.MultiModeHeld)
			{
				closeActiveCollectionMultiModeExit = false;
				activeCollection = null;
			}

			Seb.Vis.UI.UI.DrawPanel(bounds_UISpace, theme.StarredBarCol);
			float chipButtonsRegionStartX = Seb.Vis.UI.UI.PrevBounds.Right + buttonSpacing;
			float chipButtonRegionWidth = Seb.Vis.UI.UI.Width - chipButtonsRegionStartX;

			// Menu toggle button
			Vector2 menuButtonPos = new(buttonSpacing, padY);
			Vector2 menuButtonSize = new(1.5f, barHeight - padY * 2);
			bool menuButtonEnabled = !inOtherMenu;

			Vector2 scrollButtonSize = new(scrollButtonWidth, buttonHeight);

			if (Seb.Vis.UI.UI.Button("MENU", theme.MenuButtonTheme, menuButtonPos, menuButtonSize, menuButtonEnabled, true, false, theme.ButtonTheme.buttonCols, Anchor.BottomLeft, ignoreInputs: ignoreInputs))
			{
				UIDrawer.ToggleBottomPopupMenu();
				toggleMenuFrame = Time.frameCount;
			}


			#if UNITY_ANDROID || UNITY_IOS
			if(showScrollingButtons!=2){
				float scrollAmount = 15f; // Adjust as needed for responsiveness
				if(showScrollingButtons == 1) scrollAmount *= -1; //invert
				Vector2 leftButtonPos = new Vector2(Seb.Vis.UI.UI.PrevBounds.Right + buttonSpacing, padY);
				if (Seb.Vis.UI.UI.Button("←", theme.MenuButtonTheme, leftButtonPos, scrollButtonSize, true, true, false,theme.ButtonTheme.buttonCols, Anchor.BottomLeft))
				{
					scrollX = Mathf.Clamp(scrollX - scrollAmount, Mathf.Min(0, chipButtonRegionWidth - chipBarTotalWidthLastFrame), 0);
				}
				Vector2 rightButtonPos = new Vector2(Seb.Vis.UI.UI.PrevBounds.Right + buttonSpacing, padY);
				if (Seb.Vis.UI.UI.Button("→", theme.MenuButtonTheme, rightButtonPos, scrollButtonSize, true, true, false, theme.ButtonTheme.buttonCols, Anchor.BottomLeft))
				{
					scrollX = Mathf.Clamp(scrollX + scrollAmount, Mathf.Min(0, chipButtonRegionWidth - chipBarTotalWidthLastFrame), 0);
				}
			}
			#endif

			// Chips
			ButtonTheme buttonTheme = theme.ChipButton;

			using (Seb.Vis.UI.UI.CreateMaskScopeMinMax(new Vector2(Seb.Vis.UI.UI.PrevBounds.Right + buttonSpacing, 0), new Vector2(Seb.Vis.UI.UI.Width, barHeight)))
			{
				bool chipButtonsEnabled = !inOtherMenu && project.CanEditViewedChip;

				// -- Chip bar drag/scroll input --
				if (MouseIsOverBar())
				{
					const float scrollSensitivity = 2;
					scrollX += Maths.AbsoluteMax(InputHelper.MouseScrollDelta.x, InputHelper.MouseScrollDelta.y) * -scrollSensitivity;
					if (InputHelper.IsMouseDownThisFrame(MouseButton.Middle))
					{
						isDraggingChipBar = true;
						mouseDragPrev = Seb.Vis.UI.UI.ScreenToUISpace(InputHelper.MousePos).x;
					}
				}

				if (isDraggingChipBar)
				{
					float mouseDragNew = Seb.Vis.UI.UI.ScreenToUISpace(InputHelper.MousePos).x;
					scrollX += mouseDragNew - mouseDragPrev;
					mouseDragPrev = mouseDragNew;
					if (InputHelper.IsMouseUpThisFrame(MouseButton.Middle))
					{
						isDraggingChipBar = false;
					}
				}

				// -- Draw --
				chipButtonsRegionStartX = Seb.Vis.UI.UI.PrevBounds.Right + buttonSpacing;
				chipButtonRegionWidth = Seb.Vis.UI.UI.Width - chipButtonsRegionStartX;

				scrollX = Mathf.Clamp(scrollX, Mathf.Min(0, chipButtonRegionWidth - chipBarTotalWidthLastFrame), 0);
				float buttonPosX = chipButtonsRegionStartX + scrollX;
				float firstButtonLeft = buttonPosX;

				for (int i = 0; i < project.description.StarredList.Count; i++)
				{
					StarredItem starred = project.description.StarredList[i];
					ChipDescription desc;
					project.chipLibrary.TryGetChipDescription(starred.Name, out desc);
					bool isToggledOpenCollection = activeCollection != null && ChipDescription.NameMatch(starred.Name, activeCollection.Name);
					string buttonName = starred.GetDisplayStringForBottomBar(isToggledOpenCollection);

					float textOffsetX = 0;

					Vector2 buttonPos = new(buttonPosX, padY);
					Vector2 buttonSize = new(0.5f, buttonHeight);

					if (starred.IsCollection)
					{
						textOffsetX = -0.2f;
						buttonSize.x += -0.5f;
					}

					bool canAdd = starred.IsCollection || project.ViewedChip.CanAddSubchip(buttonName);


					if (Seb.Vis.UI.UI.Button(buttonName, buttonTheme, buttonPos, buttonSize, chipButtonsEnabled && canAdd, true, false, theme.ButtonTheme.buttonCols, Anchor.BottomLeft, textOffsetX: textOffsetX, ignoreInputs: ignoreInputs))
					{
						if (starred.IsCollection)
						{
							ChipCollection newActiveCollection = GetChipCollectionByName(starred.Name);
							// Take first item from collection without opening
							if (newActiveCollection.Chips.Count > 0 && KeyboardShortcuts.TakeFirstFromCollectionModifierHeld)
							{
								TryStartPlacing(project, newActiveCollection.Chips[0]);
								activeCollection = null;
							}
							// Open collection in popup
							else
							{
								collectionPopupBottomLeft = new Vector2(Seb.Vis.UI.UI.PrevBounds.Left, barHeight);
								activeCollection = newActiveCollection == activeCollection ? null : newActiveCollection;
								collectionInteractFrame = Time.frameCount;
								closeActiveCollectionMultiModeExit = false;
							}
						}
						else
						{
							TryStartPlacing(project, starred.Name);
							activeCollection = null;
						}
					}
					else if (isRightClick && Seb.Vis.UI.UI.MouseInsideBounds(Seb.Vis.UI.UI.PrevBounds))
					{
						ContextMenu.OpenBottomBarContextMenu(starred.Name, starred.IsCollection, false);
					}


					buttonPosX += Seb.Vis.UI.UI.PrevBounds.Width + buttonSpacing;
				}

				// Record total width of all buttons to be used as scroll bounds for the next frame
				chipBarTotalWidthLastFrame = Seb.Vis.UI.UI.PrevBounds.Right - firstButtonLeft + buttonSpacing;
			}


			DrawCollectionsPopup();
			DrawNestedCollectionsPopup();
		}


		static void DrawCollectionsPopup()
		{
			if (activeCollection == null) return;

			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			Project project = Project.ActiveProject;

			// Build combined list of all items (chips first, then nested collections, since popup displays top-to-bottom)
			var allItems = new System.Collections.Generic.List<(string name, bool isNestedCollection, int originalIndex)>();
			
			// Add chips first (will appear at bottom of popup)
			for (int i = 0; i < activeCollection.Chips.Count; i++)
			{
				allItems.Add((activeCollection.Chips[i], false, i));
			}
			
			// Add nested collections (will appear at top of popup)
			for (int i = 0; i < activeCollection.NestedCollections.Count; i++)
			{
				allItems.Add((activeCollection.NestedCollections[i].Name, true, i));
			}

			if (allItems.Count <= 0) return;

			int firstButtonIndex = allItems.Count - 1;
			int pressedIndex = -1;
			Vector2 layoutOrigin = collectionPopupBottomLeft + new Vector2(0, 0);
			bool expandLeft = layoutOrigin.x > Seb.Vis.UI.UI.HalfWidth;
			bool openedContextMenu = false;
			

			// Calculate how many items can fit in the available space
			float availableHeight = Seb.Vis.UI.UI.Height - barHeight - 0.1f;
			float itemHeight = buttonHeight + buttonSpacing;
			int maxItemsPerColumn = Mathf.FloorToInt(availableHeight / itemHeight);
			
			// Calculate total items and determine if we need two columns
			int totalItems = allItems.Count;
			bool needsTwoColumns = totalItems > maxItemsPerColumn;
			
			if (needsTwoColumns)
			{
				// Two-column layout
				int itemsInFirstColumn = Mathf.CeilToInt(totalItems / 2f);
				int itemsInSecondColumn = totalItems - itemsInFirstColumn;
				
				// Draw first column
				Vector2 firstColumnPos = layoutOrigin;
				Bounds2D firstColumnBounds = DrawTwoColumnSection(firstColumnPos, 0, itemsInFirstColumn, allItems, theme, ref openedContextMenu, project, ref pressedIndex);
				
				// Draw second column
				Vector2 secondColumnPos = new Vector2(firstColumnBounds.Right + buttonSpacing, layoutOrigin.y);
				Bounds2D secondColumnBounds = DrawTwoColumnSection(secondColumnPos, itemsInFirstColumn, itemsInSecondColumn, allItems, theme, ref openedContextMenu, project, ref pressedIndex);
				
				// Draw background panel for the entire two-column area
				Bounds2D combinedBounds = new Bounds2D(
					new Vector2(Mathf.Min(firstColumnBounds.Min.x, secondColumnBounds.Min.x), Mathf.Min(firstColumnBounds.Min.y, secondColumnBounds.Min.y)),
					new Vector2(Mathf.Max(firstColumnBounds.Max.x, secondColumnBounds.Max.x), Mathf.Max(firstColumnBounds.Max.y, secondColumnBounds.Max.y))
				);
				Bounds2D panelBounds = Bounds2D.Grow(combinedBounds, buttonSpacing * 2);
				panelBounds = new Bounds2D(new Vector2(panelBounds.Min.x, barHeight), panelBounds.Max);
				Seb.Vis.UI.UI.DrawPanel(panelBounds, theme.StarredBarCol);
			}
			else
			{
				// Single column layout
				Vector2 singleColumnPos = layoutOrigin;
				Bounds2D singleColumnBounds = DrawTwoColumnSection(singleColumnPos, 0, totalItems, allItems, theme, ref openedContextMenu, project, ref pressedIndex);
				
				// Draw background panel for the single column
				Bounds2D panelBounds = Bounds2D.Grow(singleColumnBounds, buttonSpacing * 2);
				panelBounds = new Bounds2D(new Vector2(panelBounds.Min.x, barHeight), panelBounds.Max);
				Seb.Vis.UI.UI.DrawPanel(panelBounds, theme.StarredBarCol);
			}

			if (!openedContextMenu)
			{
				if (pressedIndex != -1)
				{
					var selectedItem = allItems[pressedIndex];
					if (selectedItem.isNestedCollection)
					{
						// Handle nested collection click - open right-side expansion
						var nestedCollection = activeCollection.NestedCollections[selectedItem.originalIndex];
						if (nestedCollection.Chips.Count > 0)
						{
							// Calculate proper position based on main popup width
							float mainPopupWidth = CalculateMainPopupWidth();
							// Use the Y position of the clicked item for proper alignment
							nestedCollectionPopupBottomLeft = new Vector2(collectionPopupBottomLeft.x + mainPopupWidth, clickedItemY + buttonHeight);
							activeNestedCollection = nestedCollection == activeNestedCollection ? null : nestedCollection;
							nestedCollectionInteractFrame = Time.frameCount;
						}
					}
					else
					{
						// Handle regular chip click
						TryStartPlacing(project, activeCollection.Chips[selectedItem.originalIndex]);
						if (KeyboardShortcuts.MultiModeHeld)
						{
							closeActiveCollectionMultiModeExit = true;
						}
						else
						{
							activeCollection = null;
						}
					}
				}
				else if (KeyboardShortcuts.CancelShortcutTriggered || (InputHelper.IsAnyMouseButtonDownThisFrame_IgnoreConsumed() && Time.frameCount != collectionInteractFrame) || UIDrawer.ActiveMenu != UIDrawer.MenuType.None)
				{
					activeCollection = null;
					hoveredNestedCollectionName = null; // Clear hover state when collection closes
				}
			}
		}

		static Bounds2D DrawTwoColumnSection(Vector2 startPos, int startIndex, int count, System.Collections.Generic.List<(string name, bool isNestedCollection, int originalIndex)> allItems, DrawSettings.UIThemeDLS theme, ref bool openedContextMenu, Project project, ref int pressedIndex)
		{
			Vector2 currentPos = startPos;
			Bounds2D sectionBounds = default;

			int maxCharLength = 0;	
			for (int i = 0; i < count; i++)
			{
				int itemIndex = startIndex + i;
				if (itemIndex >= allItems.Count) break;
				var item = allItems[itemIndex];
				maxCharLength = Math.Max(item.name.Length, maxCharLength);
			}
			
			for (int i = 0; i < count; i++)
			{
				int itemIndex = startIndex + i;
				if (itemIndex >= allItems.Count) break;
				
				var item = allItems[itemIndex];
				//string displayName = item.isNestedCollection ? $"{item.name} ►" : item.name;

				string displayName = item.isNestedCollection ? $"{item.name.PadRight(maxCharLength)} ►" : item.name;
				
				// Calculate button width
				float buttonWidth = Draw.CalculateTextBoundsSize(displayName.AsSpan(), DrawSettings.ActiveUITheme.ChipButton.fontSize, DrawSettings.ActiveUITheme.ChipButton.font).x + 1;
				
				// Draw the button
				bool buttonPressed = Seb.Vis.UI.UI.Button(displayName, DrawSettings.ActiveUITheme.ChipButton, currentPos, new Vector2(buttonWidth, buttonHeight), true, false, false, DrawSettings.ActiveUITheme.ChipButton.buttonCols, Anchor.BottomLeft, true, 0.55f);
				
				// Handle nested collection expansion
				if (item.isNestedCollection)
				{
					#if UNITY_ANDROID || UNITY_IOS
					// On mobile, require click/tap for expansion
					if (buttonPressed)
					{
						pressedIndex = itemIndex;
						// Store the Y position of the clicked item for proper alignment
						clickedItemY = currentPos.y;
					}
					#else
					// On PC, expand on hover with anti-flicker logic
					bool isHovering = Seb.Vis.UI.UI.MouseInsideBounds(Seb.Vis.UI.UI.PrevBounds);
					
					// Allow hover expansion if:
					// 1. No nested collection is currently active, OR
					// 2. We're hovering over a different nested collection
					bool canHoverExpand = activeNestedCollection == null || 
						(activeNestedCollection != null && !ChipDescription.NameMatch(activeNestedCollection.Name, item.name));
					
					if (isHovering && canHoverExpand)
					{
						// Start or continue hover tracking
						if (hoveredNestedCollectionName != item.name)
						{
							hoveredNestedCollectionName = item.name;
							hoverStartFrame = Time.frameCount;
						}
						
						// Only expand after hover delay to prevent flicker
						if (Time.frameCount - hoverStartFrame >= hoverDelayFrames)
						{
							pressedIndex = itemIndex;
							// Store the Y position of the hovered item for proper alignment
							clickedItemY = currentPos.y;
						}
					}
					else if (!isHovering)
					{
						// Clear hover state when not hovering
						if (hoveredNestedCollectionName == item.name)
						{
							hoveredNestedCollectionName = null;
						}
					}
					
					// Always allow click for expansion on PC
					if (buttonPressed)
					{
						pressedIndex = itemIndex;
						// Store the Y position of the clicked item for proper alignment
						clickedItemY = currentPos.y;
					}
					#endif
				}
				else
				{
					// Regular chips always require click
					if (buttonPressed)
					{
						pressedIndex = itemIndex;
					}
					
					#if !UNITY_ANDROID && !UNITY_IOS
					// On PC, close nested collection when hovering over regular chips
					bool isHovering = Seb.Vis.UI.UI.MouseInsideBounds(Seb.Vis.UI.UI.PrevBounds);
					if (isHovering && activeNestedCollection != null)
					{
						// Close the nested collection when hovering over a regular chip
						activeNestedCollection = null;
					}
					#endif
				}
				
				// Handle right-click context menu
				if (InputHelper.IsMouseDownThisFrame(MouseButton.Right) && Seb.Vis.UI.UI.MouseInsideBounds(Seb.Vis.UI.UI.PrevBounds))
				{
					ContextMenu.OpenBottomBarContextMenu(item.name, item.isNestedCollection, true);
					openedContextMenu = true;
				}
				
				
				// Update bounds
				if (sectionBounds.Min == Vector2.zero && sectionBounds.Max == Vector2.zero)
				{
					sectionBounds = Seb.Vis.UI.UI.PrevBounds;
				}
				else
				{
					sectionBounds = new Bounds2D(
						new Vector2(Mathf.Min(sectionBounds.Min.x, Seb.Vis.UI.UI.PrevBounds.Min.x), Mathf.Min(sectionBounds.Min.y, Seb.Vis.UI.UI.PrevBounds.Min.y)),
						new Vector2(Mathf.Max(sectionBounds.Max.x, Seb.Vis.UI.UI.PrevBounds.Max.x), Mathf.Max(sectionBounds.Max.y, Seb.Vis.UI.UI.PrevBounds.Max.y))
					);
				}
				
				// Move to next position
				currentPos = Seb.Vis.UI.UI.PrevBounds.TopLeft + Vector2.up * buttonSpacing;
			}
			
			return sectionBounds;
		}


		static void DrawNestedCollectionsPopup()
		{
			if (activeNestedCollection == null || activeNestedCollection.Chips.Count <= 0) return;

			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			Project project = Project.ActiveProject;

			// Build list of items for the nested collection (all are regular chips, no nested collections)
			var allItems = new System.Collections.Generic.List<(string name, bool isNestedCollection, int originalIndex)>();
			
			// Add all chips from the nested collection
			for (int i = 0; i < activeNestedCollection.Chips.Count; i++)
			{
				allItems.Add((activeNestedCollection.Chips[i], false, i));
			}

			if (allItems.Count <= 0) return;

			int pressedIndex = -1;
			Vector2 layoutOrigin = nestedCollectionPopupBottomLeft + new Vector2(0, 0);
			bool openedContextMenu = false;

			// Calculate how many items can fit in the available space
			float availableHeight = Seb.Vis.UI.UI.Height - barHeight - 0.1f;
			float itemHeight = buttonHeight + buttonSpacing;
			int maxItemsPerColumn = Mathf.FloorToInt(availableHeight / itemHeight);
			
			// Calculate total items and determine if we need two columns
			int totalItems = allItems.Count;

			while (layoutOrigin.y - (totalItems-1) * itemHeight < BottomBarUI.barHeight)
			{
				layoutOrigin += Vector2.up * itemHeight;
			}


			// Single column layout (grows downwards)
			Vector2 singleColumnPos = layoutOrigin;
			Bounds2D singleColumnBounds = DrawNestedCollectionSection(singleColumnPos, 0, totalItems, allItems, theme, ref openedContextMenu, project, ref pressedIndex);
			Debug.Log("Pressed index: " + pressedIndex);
				
			// Draw background panel for the single column
			Bounds2D panelBounds = Bounds2D.Grow(singleColumnBounds, buttonSpacing * 2);
			panelBounds = new Bounds2D(panelBounds.Min, panelBounds.Max);
			Seb.Vis.UI.UI.DrawPanel(panelBounds, theme.StarredBarCol);

			if (!openedContextMenu)
			{
				if (pressedIndex != -1)
				{
					TryStartPlacing(project, activeNestedCollection.Chips[pressedIndex]);
					if (KeyboardShortcuts.MultiModeHeld)
					{
						closeActiveCollectionMultiModeExit = true;
					}
					else
					{
						activeNestedCollection = null;
					}
				}
				else if (KeyboardShortcuts.CancelShortcutTriggered || (InputHelper.IsAnyMouseButtonDownThisFrame_IgnoreConsumed() && Time.frameCount != nestedCollectionInteractFrame) || UIDrawer.ActiveMenu != UIDrawer.MenuType.None)
				{
					activeNestedCollection = null;
				}
			}
		}

		static Bounds2D DrawNestedCollectionSection(Vector2 startPos, int startIndex, int count, System.Collections.Generic.List<(string name, bool isNestedCollection, int originalIndex)> allItems, DrawSettings.UIThemeDLS theme, ref bool openedContextMenu, Project project, ref int pressedIndex)
		{
			Vector2 currentPos = startPos;
			Bounds2D sectionBounds = default;
			
			for (int i = 0; i < count; i++)
			{
				int itemIndex = startIndex + i;
				if (itemIndex >= allItems.Count) break;
				
				var item = allItems[itemIndex];
				string displayName = item.name; // No "►" prefix for nested collection items
				
				// Calculate button width
				float buttonWidth = Draw.CalculateTextBoundsSize(displayName.AsSpan(), DrawSettings.ActiveUITheme.ChipButton.fontSize, DrawSettings.ActiveUITheme.ChipButton.font).x + 1;
				
				// Draw the button
				bool buttonPressed = Seb.Vis.UI.UI.Button(displayName, DrawSettings.ActiveUITheme.ChipButton, currentPos, new Vector2(buttonWidth, buttonHeight), true, false, false, DrawSettings.ActiveUITheme.ChipButton.buttonCols, Anchor.TopLeft, true, 0.55f);
				
				// Handle button press
				if (buttonPressed)
				{
					pressedIndex = itemIndex;
				}
				
				// Handle right-click context menu
				if (InputHelper.IsMouseDownThisFrame(MouseButton.Right) && Seb.Vis.UI.UI.MouseInsideBounds(Seb.Vis.UI.UI.PrevBounds))
				{
					ContextMenu.OpenBottomBarContextMenu(item.name, false, true);
					openedContextMenu = true;
				}
				
				// Update bounds
				if (sectionBounds.Min == Vector2.zero && sectionBounds.Max == Vector2.zero)
				{
					sectionBounds = Seb.Vis.UI.UI.PrevBounds;
				}
				else
				{
					sectionBounds = new Bounds2D(
						new Vector2(Mathf.Min(sectionBounds.Min.x, Seb.Vis.UI.UI.PrevBounds.Min.x), Mathf.Min(sectionBounds.Min.y, Seb.Vis.UI.UI.PrevBounds.Min.y)),
						new Vector2(Mathf.Max(sectionBounds.Max.x, Seb.Vis.UI.UI.PrevBounds.Max.x), Mathf.Max(sectionBounds.Max.y, Seb.Vis.UI.UI.PrevBounds.Max.y))
					);
				}
				
				// Move to next position - GROW DOWNWARDS (not upwards like main popup)
				currentPos = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * buttonSpacing;
			}
			
			return sectionBounds;
		}


		static ChipCollection GetChipCollectionByName(string name)
		{
			foreach (ChipCollection c in Project.ActiveProject.description.ChipCollections)
			{
				if (ChipDescription.NameMatch(c.Name, name))
				{
					return c;
				}
			}

			throw new Exception("Failed to find collection with name: " + name);
		}

		static float CalculateMainPopupWidth()
		{
			if (activeCollection == null) return 0f;

			// Build the same list as in DrawCollectionsPopup
			var allItems = new System.Collections.Generic.List<(string name, bool isNestedCollection, int originalIndex)>();
			
			// Add chips first
			for (int i = 0; i < activeCollection.Chips.Count; i++)
			{
				allItems.Add((activeCollection.Chips[i], false, i));
			}
			
			// Add nested collections
			for (int i = 0; i < activeCollection.NestedCollections.Count; i++)
			{
				allItems.Add((activeCollection.NestedCollections[i].Name, true, i));
			}

			if (allItems.Count <= 0) return 0f;

			// Calculate how many items can fit in the available space
			float availableHeight = Seb.Vis.UI.UI.Height - barHeight - 0.1f;
			float itemHeight = buttonHeight + buttonSpacing;
			int maxItemsPerColumn = Mathf.FloorToInt(availableHeight / itemHeight);
			
			// Calculate total items and determine if we need two columns
			int totalItems = allItems.Count;
			bool needsTwoColumns = totalItems > maxItemsPerColumn;
			
			if (needsTwoColumns)
			{
				// Two-column layout - calculate width of both columns
				int itemsInFirstColumn = Mathf.CeilToInt(totalItems / 2f);
				int itemsInSecondColumn = totalItems - itemsInFirstColumn;
				
				// Calculate width of first column
				float firstColumnWidth = CalculateColumnWidth(allItems, 0, itemsInFirstColumn);
				
				// Calculate width of second column
				float secondColumnWidth = CalculateColumnWidth(allItems, itemsInFirstColumn, itemsInSecondColumn);
				
				// Total width is first column + spacing + second column + padding
				return firstColumnWidth + buttonSpacing + secondColumnWidth + buttonSpacing * 2;
			}
			else
			{
				// Single column layout
				float singleColumnWidth = CalculateColumnWidth(allItems, 0, totalItems);
				return singleColumnWidth + buttonSpacing * 2;
			}
		}

		static float CalculateColumnWidth(System.Collections.Generic.List<(string name, bool isNestedCollection, int originalIndex)> allItems, int startIndex, int count)
		{
			float maxWidth = 0f;

			int maxCharLength = 0;	
			for (int i = 0; i < count; i++)
			{
				int itemIndex = startIndex + i;
				if (itemIndex >= allItems.Count) break;
				var item = allItems[itemIndex];
				maxCharLength = Math.Max(item.name.Length, maxCharLength);
			}
			
			for (int i = 0; i < count; i++)
				{
					int itemIndex = startIndex + i;
					if (itemIndex >= allItems.Count) break;

					var item = allItems[itemIndex];
					//string displayName = item.isNestedCollection ? $"{itemName} ►" : itemName;
					string displayName = item.isNestedCollection ? $"{item.name.PadRight(maxCharLength)} ►" : item.name;

					// Calculate button width
					float buttonWidth = Draw.CalculateTextBoundsSize(displayName.AsSpan(), DrawSettings.ActiveUITheme.ChipButton.fontSize, DrawSettings.ActiveUITheme.ChipButton.font).x + 1;
					maxWidth = Mathf.Max(maxWidth, buttonWidth);
				}
			
			return maxWidth;
		}

		static bool MouseIsOverBar() => InputHelper.MouseInBounds_ScreenSpace(barBounds_ScreenSpace);
		
	/// <summary>
	/// Try to start placing a chip. If it's an Input/Output pin or special chip in a level, show a message instead.
	/// </summary>
	static void TryStartPlacing(Project project, string chipName)
	{
		ChipDescription desc = project.chipLibrary.GetChipDescription(chipName);
		
		// Check if trying to add Input/Output pins in a level
		if (ShouldHideChipInLevel(desc.ChipType))
		{
			ShowInputOutputDisabledMessage();
			return;
		}
		
		// Check if trying to add special chips in a level
		if (IsSpecialChipDisabledInLevel(desc.ChipType))
		{
			ShowSpecialChipDisabledMessage();
			return;
		}
		
		// Proceed with normal placement
		project.controller.StartPlacing(desc);
	}
		
	/// <summary>
	/// Shows a simple message that adding input/output pins is disabled for levels
	/// </summary>
	static void ShowInputOutputDisabledMessage()
	{
		SimpleMessagePopup.Open("Adding input/output pins is disabled for this level");
	}
	
	/// <summary>
	/// Shows a simple message that this chip type is disabled for levels
	/// </summary>
	static void ShowSpecialChipDisabledMessage()
	{
		SimpleMessagePopup.Open("This chip type is disabled for this level");
	}

	static void ExitToMainMenu()
		{
			if (Project.ActiveProject.ActiveChipHasUnsavedChanges()) UnsavedChangesPopup.OpenPopup(ExitIfTrue);
			else ExitIfTrue(true);

			static void ExitIfTrue(bool exit)
			{
				if (exit)
				{
					LevelManager.Instance?.ExitLevel();   
					Project.ActiveProject.NotifyExit();
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.MainMenu);
				}
			}
		}

		static void OpenSaveMenu()
		{
			// If in a level, save level progress instead of opening chip save menu
			if (LevelManager.Instance?.IsActive == true)
			{
				LevelManager.Instance.SaveCurrentProgress();
				Debug.Log($"[BottomBarUI] Saved level progress. HasUnsavedChanges after save: {LevelManager.Instance.HasUnsavedChanges()}");
			}
			else
			{
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.ChipSave);
			}
		}
		static void OpenSearchMenu() => UIDrawer.SetActiveMenu(UIDrawer.MenuType.Search);
		static void OpenLibraryMenu() => UIDrawer.SetActiveMenu(UIDrawer.MenuType.ChipLibrary);
		static void OpenStatsMenu() => UIDrawer.SetActiveMenu(UIDrawer.MenuType.ProjectStats);
		static void OpenPreferencesMenu() => UIDrawer.SetActiveMenu(UIDrawer.MenuType.Preferences);
		static void OpenAddSpecialMenu() => UIDrawer.SetActiveMenu(UIDrawer.MenuType.SpecialChipMaker);
		static void OpenLevelsMenu() => UIDrawer.SetActiveMenu(UIDrawer.MenuType.Levels);


		static void CreateNewChip()
		{
			Debug.Log($"[BottomBarUI] CreateNewChip: LevelManager.IsActive={LevelManager.Instance?.IsActive}, HasUnsavedChanges={LevelManager.Instance?.HasUnsavedChanges()}");
			
			// Check for level unsaved changes first
			if (LevelManager.Instance?.IsActive == true && LevelManager.Instance.HasUnsavedChanges())
			{
				Debug.Log("[BottomBarUI] CreateNewChip: Showing level unsaved changes popup");
				LevelUnsavedChangesPopup.OpenPopupForNewChip(HandleLevelUnsavedChanges);
			}
			// Then check for chip unsaved changes
			else if (Project.ActiveProject.ActiveChipHasUnsavedChanges())
			{
				Debug.Log("[BottomBarUI] CreateNewChip: Showing chip unsaved changes popup");
				UnsavedChangesPopup.OpenPopup(ConfirmNewChip);
			}
			else
			{
				Debug.Log("[BottomBarUI] CreateNewChip: No unsaved changes, proceeding directly");
				ConfirmNewChip(true);
			}

			static void HandleLevelUnsavedChanges(int option)
			{
				if (option == 0) // Cancel
				{
					// Do nothing, stay in current level
					return;
				}
				else if (option == 1) // Save and Continue
				{
					// Save level progress before creating new chip
					LevelManager.Instance?.SaveCurrentProgress();
					ConfirmNewChip(true);
				}
				else if (option == 2) // Continue without Saving
				{
					// Continue with new chip creation without saving
					ConfirmNewChip(true);
				}
			}

			static void ConfirmNewChip(bool confirm)
			{
				if (confirm)
				{
					LevelManager.Instance?.ExitLevel();   
					Project.ActiveProject.CreateBlankDevChip();
				}
			}
		}

		static void HandleKeyboardShortcuts()
		{
			if (MenuButtonsAndShortcutsEnabled)
			{
				if (KeyboardShortcuts.CreateNewChipShortcutTriggered) CreateNewChip();
				if (KeyboardShortcuts.SaveShortcutTriggered) OpenSaveMenu();
				if (KeyboardShortcuts.LibraryShortcutTriggered) OpenLibraryMenu();
			}

			if (KeyboardShortcuts.StatsShortcutTriggered) OpenStatsMenu();
			if (KeyboardShortcuts.PreferencesShortcutTriggered) OpenPreferencesMenu();
			if (KeyboardShortcuts.LevelsShortcutTriggered) OpenLevelsMenu();
			if (KeyboardShortcuts.QuitToMainMenuShortcutTriggered) ExitToMainMenu();
			if (KeyboardShortcuts.SpecialChipsShortcutTriggered) OpenAddSpecialMenu();
		}

		public static void Reset()
		{
			scrollX = 0;
			chipBarTotalWidthLastFrame = 0;
			isDraggingChipBar = false;
			activeCollection = null;
			activeNestedCollection = null;
			hoveredNestedCollectionName = null;
			clickedItemY = 0;
		}

	}
}
