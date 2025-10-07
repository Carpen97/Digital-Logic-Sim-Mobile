using System;
using System.Collections.Generic;
using System.Linq;
using DLS.Description;
using ChipCollection = DLS.Description.ChipCollection;
using DLS.Game;
using DLS.Game.LevelsIntegration;
using Seb.Helpers;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;
using static DLS.Graphics.DrawSettings;

namespace DLS.Graphics
{
	public static class ChipLibraryMenu
	{
		const string defaultOtherChipsCollectionName = "OTHER";
		const float previewWindowHeight = 18f;

		const int deleteMessageMaxCharsPerLine = 25;
		static readonly UIHandle ID_CollectionsScrollbar = new("ChipLibrary_CollectionsScrollbar");
		static readonly UIHandle ID_StarredScrollbar = new("ChipLibrary_StarredScrollbar");
		static readonly UIHandle ID_NameInput = new("ChipLibrary_NameField");

		static readonly string[] buttonNames_moveSingleStep = { "MOVE UP", "MOVE DOWN" };
		static readonly string[] buttonNames_jump = { "JUMP UP", "JUMP DOWN" };
		static readonly string[] buttonNames_collectionJump = { "JUMP OUT", "JUMP IN" };
		static readonly string[] buttonNames_collectionMovement = { "JUMP IN", "JUMP OUT", "JUMP UP", "JUMP DOWN", "MOVE UP", "MOVE DOWN" };
		static readonly string[] buttonNames_chipAction = { "USE", "OPEN", "DELETE" };
		static readonly string[] buttonNames_collectionRenameOrDelete = { "RENAME", "DELETE" };

		static readonly string[][] buttonName_starUnstar =
		{
			new[] { "ADD TO STARRED" },
			#if UNITY_ANDROID || UNITY_IOS
			new[] { "UNSTAR" }
			#else
			new[] { "REMOVE FROM STARRED" }
			#endif
		};

		static readonly bool[] interactableStates_renameDelete = { true, true };
		static readonly bool[] interactableStates_move = { true, true };
		static readonly bool[] interactableStates_starredList = { true, true, true };
		static readonly bool[] interactable_chipActionButtons = { true, true, true };
		static readonly bool[] interactableStates_collectionMovement = { true, true, true, true, true, true };

		static readonly Seb.Vis.UI.UI.ScrollViewDrawElementFunc drawCollectionEntry = DrawCollectionEntry;

		
		static readonly Seb.Vis.UI.UI.ScrollViewDrawElementFunc drawStarredEntry = DrawStarredEntry;

		// State
		static int selectedCollectionIndex;
		static int selectedChipInCollectionIndex;
		static int selectedNestedCollectionIndex;
		static int selectedChipInNestedCollectionIndex;
		static int selectedStarredItemIndex;

		static bool creatingNewCollection;
		static bool renamingCollection;
		static bool isConfirmingChipDeletion;
		static bool isConfirmingCollectionDeletion;

		static string deleteConfirmMessage;
		static Color deleteConfirmMessageCol;
		static bool isScrolling;
		static string chipToOpenName;
		static bool wasOpenedThisFrame;

		static readonly Color deleteColWarningHigh = new(0.95f, 0.35f, 0.35f);
		static readonly Color deleteColWarningMedium = new(1f, 0.75f, 0.2f);

		// if chip is moved to another collection, it will be auto-opened. Keep track so it can be auto-closed if chip is then moved out of that collection ('just passing through')
		static ChipCollection lastAutoOpenedCollection;

		static List<ChipCollection> collections => project.description.ChipCollections;

		static Project project => Project.ActiveProject;

		public static void DrawMenu()
		{
			MenuHelper.DrawBackgroundOverlay();
			Vector2 panelEdgePadding = new(3.25f, 2.6f);

			const float interPanelSpacing = 1.5f;
			const float menuOffsetY = 1.13f;
			const float starredPanelWidthT = 0.32f;
			const float collectionPanelWidthT = 0.35f;
			const float selectedPanelWidthT = 1 - (starredPanelWidthT + collectionPanelWidthT);

			float panelWidthSum = Seb.Vis.UI.UI.Width - interPanelSpacing * 2 - panelEdgePadding.x * 2;
			float panelHeight = Seb.Vis.UI.UI.Height - panelEdgePadding.y * 2;

			Vector2 panelATopLeft = Seb.Vis.UI.UI.TopLeft + new Vector2(panelEdgePadding.x, -panelEdgePadding.y + menuOffsetY);
			Vector2 panelSizeA = new(panelWidthSum * starredPanelWidthT, panelHeight);
			Vector2 panelBTopLeft = panelATopLeft + Vector2.right * (panelSizeA.x + interPanelSpacing);
			Vector2 panelSizeB = new(panelWidthSum * collectionPanelWidthT, panelHeight);
			Vector2 panelCTopLeft = panelBTopLeft + Vector2.right * (panelSizeB.x + interPanelSpacing);
			Vector2 panelSizeC = new(panelWidthSum * selectedPanelWidthT, panelHeight);

			isScrolling = Seb.Vis.UI.UI.GetScrollbarState(ID_CollectionsScrollbar).isDragging || Seb.Vis.UI.UI.GetScrollbarState(ID_StarredScrollbar).isDragging;

			bool popupHasFocus = creatingNewCollection || renamingCollection || isConfirmingChipDeletion || isConfirmingCollectionDeletion;

			using (Seb.Vis.UI.UI.BeginDisabledScope(popupHasFocus))
			{
				DrawStarredPanel(panelATopLeft, panelSizeA);
				DrawCollectionsPanel(panelBTopLeft, panelSizeB);
				DrawSelectedItemPanel(panelCTopLeft, panelSizeC);
			}

			if (KeyboardShortcuts.CancelShortcutTriggered || (KeyboardShortcuts.LibraryShortcutTriggered && !wasOpenedThisFrame))
			{
				if (popupHasFocus) ResetPopupState();
				else ExitLibrary();
			}

			wasOpenedThisFrame = false;
		}

		static void ResetPopupState()
		{
			creatingNewCollection = false;
			renamingCollection = false;
			isConfirmingChipDeletion = false;
			isConfirmingCollectionDeletion = false;

			deleteConfirmMessage = string.Empty;
		}

		static void DrawPanelHeader(string text, Vector2 topLeft, float width)
		{
			Color textCol = ColHelper.MakeCol("#3CD168");
			Color bgCol = ColHelper.MakeCol("#1D1D1D");
			MenuHelper.DrawLeftAlignTextWithBackground(text, topLeft, new Vector2(width, 2.3f), Anchor.TopLeft, textCol, bgCol, true);
		}

		static int DrawHorizontalButtonGroup(string[] names, bool[] interactionStates, ref Vector2 topLeft, float width, float verticalSpacing = DefaultButtonSpacing)
		{
			int buttonIndex = Seb.Vis.UI.UI.HorizontalButtonGroup(names, interactionStates, ActiveUITheme.ButtonTheme, topLeft, width, DefaultButtonSpacing, 0, Anchor.TopLeft);
			topLeft.y -= Seb.Vis.UI.UI.PrevBounds.Height + verticalSpacing;
			return buttonIndex;
		}

		static void DrawStarredPanel(Vector2 topLeft, Vector2 size)
		{
			Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();
			Bounds2D panelBounds = Bounds2D.CreateFromTopLeftAndSize(topLeft, size);
			DrawPanelHeader("STARRED", topLeft, size.x);

			Bounds2D panelBoundsMinusHeader = Bounds2D.CreateFromTopLeftAndSize(Seb.Vis.UI.UI.PrevBounds.BottomLeft, new Vector2(size.x, size.y - Seb.Vis.UI.UI.PrevBounds.Height));
			Bounds2D panelContentBounds = Bounds2D.Shrink(panelBoundsMinusHeader, PanelUIPadding);

			// Reserve space for the button at the bottom
			const float buttonHeight = 0f;
			const float buttonMargin = 1.5f;
			Vector2 scrollViewSize = new Vector2(panelContentBounds.Width, panelContentBounds.Height - buttonHeight - buttonMargin * 4);
			Vector2 buttonArea = new Vector2(panelContentBounds.Width, buttonHeight);
			Vector2 buttonTopLeft = panelContentBounds.BottomLeft + Vector2.up * (buttonHeight + buttonMargin*2);

			// Draw scrollable starred list
			Seb.Vis.UI.UI.DrawScrollView(ID_StarredScrollbar, panelContentBounds.TopLeft, scrollViewSize, UILayoutHelper.DefaultSpacing, Anchor.TopLeft, ActiveUITheme.ScrollTheme, drawStarredEntry, project.description.StarredList.Count);

			// Draw ADD TO STARRED button at the bottom
			// Check if we have something selected to star/unstar
			// Add bounds checking to prevent index out of range errors
			bool hasValidCollection = selectedCollectionIndex >= 0 && selectedCollectionIndex < collections.Count;
			bool hasValidNestedCollection = hasValidCollection && selectedNestedCollectionIndex >= 0 && selectedNestedCollectionIndex < collections[selectedCollectionIndex].NestedCollections.Count;
			bool hasValidChip = hasValidCollection && selectedChipInCollectionIndex >= 0 && selectedChipInCollectionIndex < collections[selectedCollectionIndex].Chips.Count;
			bool hasValidNestedChip = hasValidNestedCollection && selectedChipInNestedCollectionIndex >= 0 && selectedChipInNestedCollectionIndex < collections[selectedCollectionIndex].NestedCollections[selectedNestedCollectionIndex].Chips.Count;
			
			bool hasCollectionSelected = hasValidCollection && selectedChipInCollectionIndex == -1 && selectedNestedCollectionIndex == -1;
			bool hasChipSelected = hasValidCollection && selectedChipInCollectionIndex != -1 && hasValidChip;
			bool hasNestedCollectionSelected = hasValidCollection && selectedNestedCollectionIndex != -1 && selectedChipInNestedCollectionIndex == -1 && hasValidNestedCollection;
			bool hasNestedChipSelected = hasValidCollection && selectedNestedCollectionIndex != -1 && selectedChipInNestedCollectionIndex != -1 && hasValidNestedChip;
			bool hasStarredItemSelected = selectedStarredItemIndex != -1;

			if (hasChipSelected || hasCollectionSelected || hasNestedCollectionSelected || hasNestedChipSelected || hasStarredItemSelected)
			{
				string buttonText = "";
				bool isStarred = false;

				if (hasChipSelected)
				{
					ChipCollection collection = collections[selectedCollectionIndex];
					string selectedChipName = collection.Chips[selectedChipInCollectionIndex];
					isStarred = project.description.IsStarred(selectedChipName, false);
					buttonText = isStarred ? "UNSTAR" : "ADD TO STARRED";
				}
				else if (hasCollectionSelected)
				{
					string collectionName = collections[selectedCollectionIndex].Name;
					isStarred = project.description.IsStarred(collectionName, true);
					buttonText = isStarred ? "UNSTAR" : "ADD TO STARRED";
				}
				else if (hasNestedCollectionSelected)
				{
					ChipCollection collection = collections[selectedCollectionIndex];
					string nestedCollectionName = collection.NestedCollections[selectedNestedCollectionIndex].Name;
					// Nested collections can't be starred directly, but we can show the option
					buttonText = "ADD TO STARRED";
				}
				else if (hasNestedChipSelected)
				{
					ChipCollection collection = collections[selectedCollectionIndex];
					ChipCollection nestedCollection = collection.NestedCollections[selectedNestedCollectionIndex];
					string selectedChipName = nestedCollection.Chips[selectedChipInNestedCollectionIndex];
					isStarred = project.description.IsStarred(selectedChipName, false);
					buttonText = isStarred ? "UNSTAR" : "ADD TO STARRED";
				}
				else if (hasStarredItemSelected)
				{
					StarredItem starredItem = project.description.StarredList[selectedStarredItemIndex];
					isStarred = true; // Already starred
					buttonText = "UNSTAR";
				}

				bool buttonPressed = Seb.Vis.UI.UI.Button(buttonText, ActiveUITheme.ButtonTheme, buttonTopLeft, buttonArea, true, false, true, ActiveUITheme.ButtonTheme.buttonCols, Anchor.TopLeft);
				
				if (buttonPressed)
				{
					if (hasChipSelected)
					{
						ChipCollection collection = collections[selectedCollectionIndex];
						string selectedChipName = collection.Chips[selectedChipInCollectionIndex];
						project.SetStarred(selectedChipName, !isStarred, false);
					}
					else if (hasCollectionSelected)
					{
						string collectionName = collections[selectedCollectionIndex].Name;
						project.SetStarred(collectionName, !isStarred, true);
					}
					else if (hasStarredItemSelected)
					{
						StarredItem starredItem = project.description.StarredList[selectedStarredItemIndex];
						project.SetStarred(starredItem.Name, false, starredItem.IsCollection);
						selectedStarredItemIndex = Mathf.Min(selectedStarredItemIndex, project.description.StarredList.Count - 1);
					}
				}
			}

			MenuHelper.DrawReservedMenuPanel(panelID, panelBounds, false);
		}

		static void DrawStarredEntry(Vector2 topLeft, float width, int index, bool isLayoutPass)
		{
			StarredItem starredItem = project.description.StarredList[index];
			ButtonTheme theme = GetButtonTheme(starredItem.IsCollection, index == selectedStarredItemIndex);

			interactableStates_starredList[0] = index < project.description.StarredList.Count - 1; // can move down
			interactableStates_starredList[1] = index > 0; // can move up

			bool entryPressed = Seb.Vis.UI.UI.Button(starredItem.Name, theme, topLeft, new Vector2(width, 2), true, false, false, theme.buttonCols, Anchor.TopLeft, true, 1, isScrolling);
			if (entryPressed)
			{
				selectedStarredItemIndex = index;
				selectedCollectionIndex = -1;
				selectedChipInCollectionIndex = -1;
			}
		}

		static void DrawCollectionsPanel(Vector2 topLeft, Vector2 size)
		{
			Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();
			Bounds2D panelBounds = Bounds2D.CreateFromTopLeftAndSize(topLeft, size);
			DrawPanelHeader("COLLECTIONS", topLeft, size.x);
			Bounds2D panelBoundsMinusHeader = Bounds2D.CreateFromTopLeftAndSize(Seb.Vis.UI.UI.PrevBounds.BottomLeft, new Vector2(size.x, size.y - Seb.Vis.UI.UI.PrevBounds.Height));
			Bounds2D panelContentBounds = Bounds2D.Shrink(panelBoundsMinusHeader, PanelUIPadding);

			// Calculate space needed for buttons and input controls
			const float buttonHeight = 0f; // Use default auto-sized button height to match other panels
			const float buttonMargin = 1.5f;
			const float inputControlsHeight = 8f; // Height for text input + CANCEL/CREATE buttons
			
			// Make scroll panel as tall as STARRED panel initially, then adjust based on input state
			float reservedSpace = buttonHeight + buttonMargin * 4; // Space for NEW COLLECTION button
			if (creatingNewCollection || renamingCollection)
			{
				reservedSpace += inputControlsHeight + buttonMargin * 2; // Additional space for input controls
			}
			
			Vector2 scrollViewSize = new Vector2(panelContentBounds.Width, panelContentBounds.Height - reservedSpace);
			Vector2 buttonArea = new Vector2(panelContentBounds.Width, buttonHeight);
			Vector2 inputControlsArea = new Vector2(panelContentBounds.Width, inputControlsHeight);
			
			// Draw scrollable collections list
			Seb.Vis.UI.UI.DrawScrollView(ID_CollectionsScrollbar, panelContentBounds.TopLeft, scrollViewSize, UILayoutHelper.DefaultSpacing, Anchor.TopLeft, ActiveUITheme.ScrollTheme, drawCollectionEntry, collections.Count);
			
			// Position NEW COLLECTION button and input controls at the bottom
			Vector2 buttonTopLeft = panelContentBounds.BottomLeft + Vector2.up * (buttonHeight + buttonMargin*2);
			if (creatingNewCollection || renamingCollection)
			{
				buttonTopLeft += Vector2.up * (inputControlsHeight + buttonMargin * 2);
			}
			Vector2 inputControlsTopLeft = panelContentBounds.BottomLeft + Vector2.up * (inputControlsHeight + buttonMargin * 2);
			
			// NEW COLLECTION button (only show when not in input mode)
			if (!renamingCollection && !creatingNewCollection)
			{
				bool createNew = Seb.Vis.UI.UI.Button("NEW COLLECTION", ActiveUITheme.ButtonTheme, buttonTopLeft, buttonArea, true, false, true, ActiveUITheme.ButtonTheme.buttonCols, Anchor.TopLeft);
				if (createNew) creatingNewCollection = true;
			}
			
			// New collection / rename collection input field
			if (creatingNewCollection || renamingCollection)
			{
				using (Seb.Vis.UI.UI.BeginDisabledScope(false))
				{
					InputFieldTheme inputTheme = MenuHelper.Theme.ChipNameInputField;
					inputTheme.fontSize = MenuHelper.Theme.FontSizeRegular;
					InputFieldState nameField = Seb.Vis.UI.UI.InputField(ID_NameInput, inputTheme, inputControlsTopLeft, new Vector2(inputControlsArea.x, 2.5f), string.Empty, Anchor.TopLeft, 1, ValidateCollectionNameInput, true);
					int button_cancelConfirm = MenuHelper.DrawButtonPair("CANCEL", renamingCollection ? "RENAME" : "CREATE", Seb.Vis.UI.UI.PrevBounds.BottomLeft, inputControlsArea.x, true, true, IsValidCollectionName(nameField.text));
					if (button_cancelConfirm == 0)
					{
						nameField.ClearText();
						creatingNewCollection = false;
						renamingCollection = false;
					}
					else if (button_cancelConfirm == 1 || KeyboardShortcuts.ConfirmShortcutTriggered)
					{
						if (creatingNewCollection)
						{
							// Check if we should create a nested collection or top-level collection
							if (selectedCollectionIndex != -1 && selectedChipInCollectionIndex == -1 && selectedNestedCollectionIndex == -1)
							{
								// Create nested collection within the selected collection
								ChipCollection selectedCollection = collections[selectedCollectionIndex];
								ChipCollection newNestedCollection = selectedCollection.CreateNestedCollection(nameField.text);
								Debug.Log($"Created nested collection '{nameField.text}' in collection '{selectedCollection.Name}'");
							}
							else
							{
								// Create top-level collection
								ChipCollection newCollection = new ChipCollection(nameField.text);
								collections.Add(newCollection);
								Debug.Log($"Created new collection '{nameField.text}'");
							}
							nameField.ClearText();
							creatingNewCollection = false;
							project.SaveCurrentProjectDescription();
						}
						else if (renamingCollection)
						{
							if (selectedCollectionIndex != -1 && selectedChipInCollectionIndex == -1 && selectedNestedCollectionIndex == -1)
							{
								// Rename top-level collection
								ChipCollection selectedCollection = collections[selectedCollectionIndex];
								selectedCollection.Name = nameField.text;
								project.RenameStarred(nameField.text, selectedCollection.Name, true);
								Debug.Log($"Renamed collection to '{nameField.text}'");
							}
							else if (selectedCollectionIndex != -1 && selectedNestedCollectionIndex != -1 && selectedChipInCollectionIndex == -1)
							{
								// Rename nested collection
								ChipCollection selectedCollection = collections[selectedCollectionIndex];
								ChipCollection selectedNestedCollection = selectedCollection.NestedCollections[selectedNestedCollectionIndex];
								selectedNestedCollection.Name = nameField.text;
								Debug.Log($"Renamed nested collection to '{nameField.text}'");
							}
							nameField.ClearText();
							renamingCollection = false;
							project.SaveCurrentProjectDescription();
						}
					}
				}
			}
			
			MenuHelper.DrawReservedMenuPanel(panelID, panelBounds, false);
		}

		static void DrawCollectionEntry(Vector2 topLeft, float width, int collectionIndex, bool isLayoutPass)
		{
			ChipCollection collection = collections[collectionIndex];
			string label = collection.GetDisplayString();

			// Collection is highlighted only if it's selected AND no nested collection or chip is selected
			bool collectionHighlighted = (collectionIndex == selectedCollectionIndex) && (selectedNestedCollectionIndex == -1) && (selectedChipInCollectionIndex == -1);
			ButtonTheme activeCollectionTheme = GetButtonTheme(true, collectionHighlighted);

			bool collectionPressed = Seb.Vis.UI.UI.Button(label, activeCollectionTheme, topLeft, new Vector2(width, 2), true, false, false, activeCollectionTheme.buttonCols, Anchor.TopLeft, true, 1, isScrolling);
			if (collectionPressed)
			{
				// Check if this collection is already selected
				bool wasAlreadySelected = (selectedCollectionIndex == collectionIndex);
				
				selectedCollectionIndex = collectionIndex;
				selectedChipInCollectionIndex = -1;
				selectedNestedCollectionIndex = -1; // Clear nested collection selection
				selectedChipInNestedCollectionIndex = -1; // Clear nested collection chip selection
				selectedStarredItemIndex = -1;
				lastAutoOpenedCollection = null;
				
				// Only toggle if the collection was already selected (not on first selection)
				if (wasAlreadySelected && !InputHelper.CtrlIsHeld) 
				{
					collection.IsToggledOpen = !collection.IsToggledOpen;
				}
			}

			const float nestedInset = 1.75f;

			if (collection.IsToggledOpen)
			{
				// Draw nested collections first
				for (int nestedIndex = 0; nestedIndex < collection.NestedCollections.Count; nestedIndex++)
				{
					ChipCollection nestedCollection = collection.NestedCollections[nestedIndex];
					string nestedLabel = nestedCollection.GetDisplayString();
					
					// Use same theme as regular collections for nested collections
					// Nested collection is highlighted only if it's selected AND no chip is selected
					bool nestedCollectionHighlighted = (nestedIndex == selectedNestedCollectionIndex) && (collectionIndex == selectedCollectionIndex) && (selectedChipInNestedCollectionIndex == -1);
					ButtonTheme nestedTheme = GetButtonTheme(true, nestedCollectionHighlighted);
					Vector2 nestedLabelPos = new(topLeft.x + nestedInset, Seb.Vis.UI.UI.PrevBounds.Bottom - UILayoutHelper.DefaultSpacing);
					bool nestedPressed = Seb.Vis.UI.UI.Button(nestedLabel, nestedTheme, nestedLabelPos, new Vector2(width - nestedInset, 2), true, false, false, nestedTheme.buttonCols, Anchor.TopLeft, true, 1, isScrolling);
					
					if (nestedPressed)
					{
						// Check if this nested collection is already selected
						bool wasAlreadySelected = (selectedNestedCollectionIndex == nestedIndex && selectedCollectionIndex == collectionIndex);
						
						// Update selection indices - this allows cross-collection selection
						selectedCollectionIndex = collectionIndex; // Update to the parent collection
						selectedNestedCollectionIndex = nestedIndex;
						selectedChipInCollectionIndex = -1;
						selectedChipInNestedCollectionIndex = -1; // Clear nested collection chip selection
						selectedStarredItemIndex = -1;
						lastAutoOpenedCollection = null;
						Debug.Log($"Selected nested collection '{nestedCollection.Name}' at index {nestedIndex}");
						
						// Only toggle if the nested collection was already selected (not on first selection)
						if (wasAlreadySelected && !InputHelper.CtrlIsHeld) 
						{
							nestedCollection.IsToggledOpen = !nestedCollection.IsToggledOpen;
						}
					}

					// Draw nested collection contents if open
					if (nestedCollection.IsToggledOpen)
					{
						const float nestedCollectionInset = 1.75f;
						for (int chipIndex = 0; chipIndex < nestedCollection.Chips.Count; chipIndex++)
						{
							string chipName = nestedCollection.Chips[chipIndex];
							// Check if this chip is selected
							bool chipHighlighted = (chipIndex == selectedChipInNestedCollectionIndex) && (nestedIndex == selectedNestedCollectionIndex) && (collectionIndex == selectedCollectionIndex);
							ButtonTheme activeChipTheme = chipHighlighted ? ActiveUITheme.ChipLibraryChipToggleOn : ActiveUITheme.ChipLibraryChipToggleOff;
							Vector2 chipLabelPos = new(topLeft.x + nestedInset + nestedCollectionInset, Seb.Vis.UI.UI.PrevBounds.Bottom - UILayoutHelper.DefaultSpacing);
							bool chipPressed = Seb.Vis.UI.UI.Button(chipName, activeChipTheme, chipLabelPos, new Vector2(width - nestedInset - nestedCollectionInset, 2), true, false, false, activeChipTheme.buttonCols, Anchor.TopLeft, true, 1, isScrolling);
							if (chipPressed)
							{
								// Update selection indices - this allows cross-collection selection
								selectedCollectionIndex = collectionIndex; // Update to the parent collection
								selectedNestedCollectionIndex = nestedIndex;
								selectedChipInNestedCollectionIndex = chipIndex;
								selectedChipInCollectionIndex = -1; // Clear main collection selection
								selectedStarredItemIndex = -1;
								lastAutoOpenedCollection = null;
							}
						}
					}
				}

				// Draw regular chips
				for (int chipIndex = 0; chipIndex < collection.Chips.Count; chipIndex++)
				{
					string chipName = collection.Chips[chipIndex];
					ButtonTheme activeChipTheme = collectionIndex == selectedCollectionIndex && chipIndex == selectedChipInCollectionIndex ? ActiveUITheme.ChipLibraryChipToggleOn : ActiveUITheme.ChipLibraryChipToggleOff;
					Vector2 chipLabelPos = new(topLeft.x + nestedInset, Seb.Vis.UI.UI.PrevBounds.Bottom - UILayoutHelper.DefaultSpacing);
					bool chipPressed = Seb.Vis.UI.UI.Button(chipName, activeChipTheme, chipLabelPos, new Vector2(width - nestedInset, 2), true, false, false,activeChipTheme.buttonCols, Anchor.TopLeft, true, 1, isScrolling);
					if (chipPressed)
					{
						bool alreadySelected = selectedChipInCollectionIndex == chipIndex && collectionHighlighted;

						if (alreadySelected) selectedChipInCollectionIndex = -1;
						else
						{
							selectedCollectionIndex = collectionIndex;
							selectedChipInCollectionIndex = chipIndex;
						}

						selectedStarredItemIndex = -1;
						selectedNestedCollectionIndex = -1;
						selectedChipInNestedCollectionIndex = -1; // Clear folder chip selection
						lastAutoOpenedCollection = null;
					}
				}
			}
		}

		static void DrawSelectedItemPanel(Vector2 topLeft, Vector2 size)
		{
			Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();
			Bounds2D panelBounds = Bounds2D.CreateFromTopLeftAndSize(topLeft, size);
			DrawPanelHeader("PREVIEW", topLeft, size.x);
			Bounds2D panelBoundsMinusHeader = Bounds2D.CreateFromTopLeftAndSize(Seb.Vis.UI.UI.PrevBounds.BottomLeft, new Vector2(size.x, size.y - Seb.Vis.UI.UI.PrevBounds.Height));
			Bounds2D panelContentBounds = Bounds2D.Shrink(panelBoundsMinusHeader, PanelUIPadding);
			topLeft = panelContentBounds.TopLeft;
			
			// Draw panel background first
			MenuHelper.DrawReservedMenuPanel(panelID, panelBounds, false);
			const float SectionSpacing = 3;

			// Add bounds checking to prevent index out of range errors
			bool hasValidCollection = selectedCollectionIndex >= 0 && selectedCollectionIndex < collections.Count;
			bool hasValidNestedCollection = hasValidCollection && selectedNestedCollectionIndex >= 0 && selectedNestedCollectionIndex < collections[selectedCollectionIndex].NestedCollections.Count;
			bool hasValidChip = hasValidCollection && selectedChipInCollectionIndex >= 0 && selectedChipInCollectionIndex < collections[selectedCollectionIndex].Chips.Count;
			bool hasValidNestedChip = hasValidNestedCollection && selectedChipInNestedCollectionIndex >= 0 && selectedChipInNestedCollectionIndex < collections[selectedCollectionIndex].NestedCollections[selectedNestedCollectionIndex].Chips.Count;
			
			bool hasCollectionSelected = hasValidCollection && selectedChipInCollectionIndex == -1 && selectedNestedCollectionIndex == -1;
			bool hasChipSelected = hasValidCollection && selectedChipInCollectionIndex != -1 && hasValidChip;
			bool hasNestedCollectionSelected = hasValidCollection && selectedNestedCollectionIndex != -1 && selectedChipInNestedCollectionIndex == -1 && hasValidNestedCollection;
			bool hasNestedChipSelected = hasValidCollection && selectedNestedCollectionIndex != -1 && selectedChipInNestedCollectionIndex != -1 && hasValidNestedChip;
			bool hasStarredItemSelected = selectedStarredItemIndex != -1;
			
			// Debug logging for nested collection selection
			if (selectedNestedCollectionIndex != -1)
			{
				Debug.Log($"Nested collection selection: index={selectedNestedCollectionIndex}, hasValidCollection={hasValidCollection}, hasValidNestedCollection={hasValidNestedCollection}, hasNestedCollectionSelected={hasNestedCollectionSelected}");
			}

			// Always draw preview window (even when empty)
			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				// ---- Draw Chip Preview First (Top Element) ----
				DrawChipPreview(panelContentBounds, hasChipSelected, hasStarredItemSelected, hasNestedChipSelected);
				
				// Add spacing below preview
				topLeft.y -= previewWindowHeight; // Space for preview window
				
				// ---- Draw single header based on selection ----
				if (hasChipSelected)
				{
					// Blue header for chip selection
					ChipCollection collection = collections[selectedCollectionIndex];
					string chipName = collection.Chips[selectedChipInCollectionIndex];
					ButtonTheme chipButtonTheme = GetButtonTheme(false, true); // Blue theme
					DrawHeader(chipName, chipButtonTheme.buttonCols.normal, chipButtonTheme.textCols.normal, ref topLeft, panelContentBounds.Width);
				}
				else if (hasCollectionSelected)
				{
					// Red header for collection selection
					string collectionName = collections[selectedCollectionIndex].Name;
					ButtonTheme collectionButtonTheme = GetButtonTheme(true, true); // Red theme
					DrawHeader(collectionName, collectionButtonTheme.buttonCols.normal, collectionButtonTheme.textCols.normal, ref topLeft, panelContentBounds.Width);
				}
				else if (hasNestedCollectionSelected)
				{
					// Same header theme as regular collections for nested collection selection
					ChipCollection collection = collections[selectedCollectionIndex];
					string nestedCollectionName = collection.NestedCollections[selectedNestedCollectionIndex].Name;
					ButtonTheme nestedButtonTheme = GetButtonTheme(true, true); // Same theme as regular collections
					DrawHeader(nestedCollectionName, nestedButtonTheme.buttonCols.normal, nestedButtonTheme.textCols.normal, ref topLeft, panelContentBounds.Width);
				}
				else if (hasNestedChipSelected)
				{
					// Blue header for nested collection chip selection
					ChipCollection collection = collections[selectedCollectionIndex];
					ChipCollection nestedCollection = collection.NestedCollections[selectedNestedCollectionIndex];
					string chipName = nestedCollection.Chips[selectedChipInNestedCollectionIndex];
					ButtonTheme chipButtonTheme = GetButtonTheme(false, true); // Blue theme
					DrawHeader(chipName, chipButtonTheme.buttonCols.normal, chipButtonTheme.textCols.normal, ref topLeft, panelContentBounds.Width);
				}
				else if (hasStarredItemSelected)
				{
					// Blue header for starred item selection
					string starredName = project.description.StarredList[selectedStarredItemIndex].Name;
					ButtonTheme starredButtonTheme = GetButtonTheme(false, true); // Blue theme
					DrawHeader(starredName, starredButtonTheme.buttonCols.normal, starredButtonTheme.textCols.normal, ref topLeft, panelContentBounds.Width);
				}
				// No header when nothing is selected
				
				// ---- Selected Chip UI ----
				if (hasChipSelected || hasCollectionSelected || hasNestedCollectionSelected || hasNestedChipSelected || hasStarredItemSelected)
				{
					if (hasChipSelected)
					{
						// ---- Draw ----
						ChipCollection collection = collections[selectedCollectionIndex];
						string selectedChipName = collection.Chips[selectedChipInCollectionIndex];
						
						// Calculate all movement possibilities for chip
						bool canJumpIn = collection.NestedCollections.Count > 0; // Can jump into nested collection if any exist
						bool canJumpOut = true; // Chips can always jump out of collections
						bool canJumpUp = selectedCollectionIndex > 0; // Can move to collection above
						bool canJumpDown = selectedCollectionIndex < collections.Count - 1; // Can move to collection below
						bool canMoveUp = selectedChipInCollectionIndex > 0; // Can move up within collection
						bool canMoveDown = selectedChipInCollectionIndex < collection.Chips.Count - 1; // Can move down within collection
						
						// Set button states
						interactableStates_collectionMovement[0] = canJumpIn; // JUMP IN
						interactableStates_collectionMovement[1] = canJumpOut; // JUMP OUT
						interactableStates_collectionMovement[2] = canJumpUp; // JUMP UP
						interactableStates_collectionMovement[3] = canJumpDown; // JUMP DOWN
						interactableStates_collectionMovement[4] = canMoveUp; // MOVE UP
						interactableStates_collectionMovement[5] = canMoveDown; // MOVE DOWN
						
						// Draw all 6 movement buttons in 3 rows (2 buttons per row)
						int buttonIndex_movement = DrawCollectionMovementButtons(buttonNames_collectionMovement, interactableStates_collectionMovement, ref topLeft, panelContentBounds.Width);
						
						// Draw chip action buttons
						ChipActionButtons(selectedChipName, ref topLeft, panelContentBounds.Width);

						// Handle movement buttons
						bool jumpIn = buttonIndex_movement == 0;
						bool jumpOut = buttonIndex_movement == 1;
						bool jumpUp = buttonIndex_movement == 2;
						bool jumpDown = buttonIndex_movement == 3;
						bool moveUp = buttonIndex_movement == 4;
						bool moveDown = buttonIndex_movement == 5;

						// ---- Handle button inputs ----
						if (jumpIn) // JUMP IN - move chip into closest nested collection above
						{
							// Find the closest nested collection above the chip's position within the current collection
							ChipCollection targetNestedCollection = null;
							int targetNestedIndex = -1;
							
							// Look for nested collections in the current collection that are above the chip's position
							// Nested collections are drawn before chips, so we need to find the closest one above
							for (int i = 0; i < collection.NestedCollections.Count; i++)
							{
								// Since nested collections are drawn before chips, any nested collection is "above" any chip
								// We'll take the last (most recently added) nested collection as the target
								targetNestedCollection = collection.NestedCollections[i];
								targetNestedIndex = i;
							}
							
							if (targetNestedCollection != null)
							{
								// Move chip into the closest nested collection above
								collection.Chips.RemoveAt(selectedChipInCollectionIndex);
								targetNestedCollection.Chips.Add(selectedChipName);
								// Stay in the same collection, just move to nested collection
								selectedNestedCollectionIndex = targetNestedIndex;
								selectedChipInCollectionIndex = -1; // No longer in main collection
								selectedChipInNestedCollectionIndex = targetNestedCollection.Chips.Count - 1;
								
								// Auto-expand the target nested collection to show the moved chip
								targetNestedCollection.IsToggledOpen = true;
								
								Debug.Log($"Moved chip '{selectedChipName}' into nested collection '{targetNestedCollection.Name}' within current collection");
								Debug.Log($"Selection state after move: selectedCollectionIndex={selectedCollectionIndex}, selectedNestedCollectionIndex={selectedNestedCollectionIndex}, selectedChipInNestedCollectionIndex={selectedChipInNestedCollectionIndex}");
							}
							else
							{
								// No nested collection above, move to main collection above
								if (selectedCollectionIndex > 0)
								{
									ChipCollection targetCollection = collections[selectedCollectionIndex - 1];
									collection.Chips.RemoveAt(selectedChipInCollectionIndex);
									targetCollection.Chips.Add(selectedChipName);
									selectedCollectionIndex = selectedCollectionIndex - 1;
									selectedChipInCollectionIndex = targetCollection.Chips.Count - 1;
									Debug.Log($"Moved chip '{selectedChipName}' into collection above (no nested collection found)");
								}
								else
								{
									Debug.Log("Cannot move chip - no collection above to move into");
								}
							}
						}
						else if (jumpOut) // JUMP OUT - move chip out of collection and place below
						{
							// Move chip out of current collection and place it below the collection
							collection.Chips.RemoveAt(selectedChipInCollectionIndex);
							
							// Find the next collection to place the chip in
							if (selectedCollectionIndex < collections.Count - 1)
							{
								// Place in the collection below
								ChipCollection targetCollection = collections[selectedCollectionIndex + 1];
								targetCollection.Chips.Add(selectedChipName);
								selectedCollectionIndex = selectedCollectionIndex + 1;
								selectedChipInCollectionIndex = targetCollection.Chips.Count - 1;
								Debug.Log($"Moved chip '{selectedChipName}' out of collection and placed in next collection");
							}
							else
							{
								// No collection below, create a new collection or place at the end
								// For now, just place it back at the end of the current collection
								collection.Chips.Add(selectedChipName);
								selectedChipInCollectionIndex = collection.Chips.Count - 1;
								Debug.Log($"Moved chip '{selectedChipName}' to end of current collection (no collection below)");
							}
						}
						else if (jumpUp) // JUMP UP - move to collection above
						{
							collection = MoveSelectedChipToNewCollection(selectedCollectionIndex - 1);
							Debug.Log($"Moved chip '{selectedChipName}' to collection above");
						}
						else if (jumpDown) // JUMP DOWN - move to collection below
						{
							collection = MoveSelectedChipToNewCollection(selectedCollectionIndex + 1);
							Debug.Log($"Moved chip '{selectedChipName}' to collection below");
						}
						else if (moveUp) // MOVE UP - move up within collection
						{
							int targetIndex = selectedChipInCollectionIndex - 1;
							collection.Chips.RemoveAt(selectedChipInCollectionIndex);
							collection.Chips.Insert(targetIndex, selectedChipName);
							selectedChipInCollectionIndex = targetIndex;
							Debug.Log($"Moved chip '{selectedChipName}' up within collection");
						}
						else if (moveDown) // MOVE DOWN - move down within collection
						{
							int targetIndex = selectedChipInCollectionIndex + 1;
							collection.Chips.RemoveAt(selectedChipInCollectionIndex);
							collection.Chips.Insert(targetIndex, selectedChipName);
							selectedChipInCollectionIndex = targetIndex;
							Debug.Log($"Moved chip '{selectedChipName}' down within collection");
						}
					}
					// ---- Selected Folder Chip UI ----
					else if (hasNestedChipSelected)
					{
						// ---- Draw ----
						ChipCollection collection = collections[selectedCollectionIndex];
						ChipCollection nestedCollection = collection.NestedCollections[selectedNestedCollectionIndex];
						string selectedChipName = nestedCollection.Chips[selectedChipInNestedCollectionIndex];
						
						// Calculate all movement possibilities for nested chip
						bool canJumpIn = selectedCollectionIndex > 0; // Can jump into collection above
						bool canJumpOut = true; // Can jump out to parent collection
						bool canJumpUp = selectedNestedCollectionIndex > 0; // Can move to nested collection above
						bool canJumpDown = selectedNestedCollectionIndex < collection.NestedCollections.Count - 1; // Can move to nested collection below
						bool canMoveUp = selectedChipInNestedCollectionIndex > 0; // Can move up within nested collection
						bool canMoveDown = selectedChipInNestedCollectionIndex < nestedCollection.Chips.Count - 1; // Can move down within nested collection
						
						// Set button states
						interactableStates_collectionMovement[0] = canJumpIn; // JUMP IN
						interactableStates_collectionMovement[1] = canJumpOut; // JUMP OUT
						interactableStates_collectionMovement[2] = canJumpUp; // JUMP UP
						interactableStates_collectionMovement[3] = canJumpDown; // JUMP DOWN
						interactableStates_collectionMovement[4] = canMoveUp; // MOVE UP
						interactableStates_collectionMovement[5] = canMoveDown; // MOVE DOWN
						
						// Draw all 6 movement buttons in 3 rows (2 buttons per row)
						int buttonIndex_movement = DrawCollectionMovementButtons(buttonNames_collectionMovement, interactableStates_collectionMovement, ref topLeft, panelContentBounds.Width);
						
						// Draw chip action buttons
						ChipActionButtons(selectedChipName, ref topLeft, panelContentBounds.Width);

						// Handle movement buttons
						bool jumpIn = buttonIndex_movement == 0;
						bool jumpOut = buttonIndex_movement == 1;
						bool jumpUp = buttonIndex_movement == 2;
						bool jumpDown = buttonIndex_movement == 3;
						bool moveUp = buttonIndex_movement == 4;
						bool moveDown = buttonIndex_movement == 5;

						// ---- Handle button inputs ----
						if (jumpIn) // JUMP IN - move chip into closest nested collection above
						{
							// Find the closest nested collection above the current chip position within the same parent
							ChipCollection targetNestedCollection = null;
							int targetNestedIndex = -1;
							
							// Look for nested collections in the parent collection above the current nested collection
							for (int i = 0; i < collection.NestedCollections.Count; i++)
							{
								if (i < selectedNestedCollectionIndex) // Only consider nested collections above the current one
								{
									targetNestedCollection = collection.NestedCollections[i];
									targetNestedIndex = i;
									break; // Take the first (closest) nested collection above
								}
							}
							
							if (targetNestedCollection != null)
							{
								// Move chip into the closest nested collection above
								nestedCollection.Chips.RemoveAt(selectedChipInNestedCollectionIndex);
								targetNestedCollection.Chips.Add(selectedChipName);
								selectedNestedCollectionIndex = targetNestedIndex;
								selectedChipInNestedCollectionIndex = targetNestedCollection.Chips.Count - 1;
								
								// Auto-expand the target nested collection to show the moved chip
								targetNestedCollection.IsToggledOpen = true;
								
								Debug.Log($"Moved nested chip '{selectedChipName}' into nested collection '{targetNestedCollection.Name}' above");
							}
							else
							{
								// No nested collection above, move to main collection above
								ChipCollection targetCollection = collections[selectedCollectionIndex - 1];
								nestedCollection.Chips.RemoveAt(selectedChipInNestedCollectionIndex);
								targetCollection.Chips.Add(selectedChipName);
								selectedCollectionIndex = selectedCollectionIndex - 1;
								selectedNestedCollectionIndex = -1;
								selectedChipInNestedCollectionIndex = -1;
								selectedChipInCollectionIndex = targetCollection.Chips.Count - 1;
								Debug.Log($"Moved nested chip '{selectedChipName}' into collection above (no nested collection found)");
							}
						}
						else if (jumpOut) // JUMP OUT - move chip to parent collection
						{
							// Move chip to the parent collection
							nestedCollection.Chips.RemoveAt(selectedChipInNestedCollectionIndex);
							collection.Chips.Add(selectedChipName);
							selectedNestedCollectionIndex = -1;
							selectedChipInNestedCollectionIndex = -1;
							selectedChipInCollectionIndex = collection.Chips.Count - 1;
							Debug.Log($"Moved nested chip '{selectedChipName}' to parent collection");
						}
						else if (jumpUp) // JUMP UP - move to previous nested collection above
						{
							// Find the previous nested collection above the current one
							ChipCollection targetNestedCollection = null;
							int targetNestedIndex = -1;
							
							// Look for nested collections above the current one within the same parent
							for (int i = selectedNestedCollectionIndex - 1; i >= 0; i--)
							{
								targetNestedCollection = collection.NestedCollections[i];
								targetNestedIndex = i;
								break; // Take the first (closest) nested collection above
							}
							
							if (targetNestedCollection != null)
							{
								// Move chip to the previous nested collection above
								nestedCollection.Chips.RemoveAt(selectedChipInNestedCollectionIndex);
								targetNestedCollection.Chips.Add(selectedChipName);
								selectedNestedCollectionIndex = targetNestedIndex;
								selectedChipInNestedCollectionIndex = targetNestedCollection.Chips.Count - 1;
								Debug.Log($"Moved nested chip '{selectedChipName}' to nested collection '{targetNestedCollection.Name}' above");
							}
							else
							{
								Debug.Log("Cannot move chip up - no nested collection above");
							}
						}
						else if (jumpDown) // JUMP DOWN - move to next nested collection below
						{
							// Find the next nested collection below the current one
							ChipCollection targetNestedCollection = null;
							int targetNestedIndex = -1;
							
							// Look for nested collections below the current one within the same parent
							for (int i = selectedNestedCollectionIndex + 1; i < collection.NestedCollections.Count; i++)
							{
								targetNestedCollection = collection.NestedCollections[i];
								targetNestedIndex = i;
								break; // Take the first (closest) nested collection below
							}
							
							if (targetNestedCollection != null)
							{
								// Move chip to the next nested collection below
								nestedCollection.Chips.RemoveAt(selectedChipInNestedCollectionIndex);
								targetNestedCollection.Chips.Add(selectedChipName);
								selectedNestedCollectionIndex = targetNestedIndex;
								selectedChipInNestedCollectionIndex = targetNestedCollection.Chips.Count - 1;
								Debug.Log($"Moved nested chip '{selectedChipName}' to nested collection '{targetNestedCollection.Name}' below");
							}
							else
							{
								Debug.Log("Cannot move chip down - no nested collection below");
							}
						}
						else if (moveUp) // MOVE UP - move up within nested collection
						{
							int targetIndex = selectedChipInNestedCollectionIndex - 1;
							nestedCollection.Chips.RemoveAt(selectedChipInNestedCollectionIndex);
							nestedCollection.Chips.Insert(targetIndex, selectedChipName);
							selectedChipInNestedCollectionIndex = targetIndex;
							Debug.Log($"Moved nested chip '{selectedChipName}' up within nested collection");
						}
						else if (moveDown) // MOVE DOWN - move down within nested collection
						{
							int targetIndex = selectedChipInNestedCollectionIndex + 1;
							nestedCollection.Chips.RemoveAt(selectedChipInNestedCollectionIndex);
							nestedCollection.Chips.Insert(targetIndex, selectedChipName);
							selectedChipInNestedCollectionIndex = targetIndex;
							Debug.Log($"Moved nested chip '{selectedChipName}' down within nested collection");
						}
					}
					// ---- Selected Nested Collection UI ----
					else if (hasNestedCollectionSelected)
					{
						// ---- Draw ----
						ChipCollection collection = collections[selectedCollectionIndex];
						ChipCollection nestedCollection = collection.NestedCollections[selectedNestedCollectionIndex];
						string nestedCollectionName = nestedCollection.Name;
						
						// Calculate all movement possibilities for nested collection
						bool canJumpIn = selectedCollectionIndex > 0; // Can nest into collection above if it exists
						bool canJumpOut = true; // Always can unnest
						bool canJumpUp = selectedCollectionIndex > 0; // Can move to collection above
						bool canJumpDown = selectedCollectionIndex < collections.Count - 1; // Can move to collection below
						bool canMoveUp = selectedNestedCollectionIndex > 0; // Can move up within nested collections
						bool canMoveDown = selectedNestedCollectionIndex < collection.NestedCollections.Count - 1; // Can move down within nested collections
						
						// Set button states
						interactableStates_collectionMovement[0] = canJumpIn; // JUMP IN
						interactableStates_collectionMovement[1] = canJumpOut; // JUMP OUT
						interactableStates_collectionMovement[2] = canJumpUp; // JUMP UP
						interactableStates_collectionMovement[3] = canJumpDown; // JUMP DOWN
						interactableStates_collectionMovement[4] = canMoveUp; // MOVE UP
						interactableStates_collectionMovement[5] = canMoveDown; // MOVE DOWN
						
						// Draw all 6 movement buttons in 3 rows (2 buttons per row)
						int buttonIndex_movement = DrawCollectionMovementButtons(buttonNames_collectionMovement, interactableStates_collectionMovement, ref topLeft, panelContentBounds.Width);
						
						// Nested collection action buttons (similar to chips)
						ChipActionButtons(nestedCollectionName, ref topLeft, panelContentBounds.Width);
						
						bool jumpIn = buttonIndex_movement == 0;
						bool jumpOut = buttonIndex_movement == 1;
						bool jumpUp = buttonIndex_movement == 2;
						bool jumpDown = buttonIndex_movement == 3;
						bool moveUp = buttonIndex_movement == 4;
						bool moveDown = buttonIndex_movement == 5;
						
						// ---- Handle button inputs ----
						if (jumpIn)
						{
							// Nest: Move nested collection into the collection above
							ChipCollection targetCollection = collections[selectedCollectionIndex - 1];
							collection.NestedCollections.RemoveAt(selectedNestedCollectionIndex);
							targetCollection.NestedCollections.Add(nestedCollection);
							nestedCollection.ParentCollection = targetCollection;
							selectedCollectionIndex = selectedCollectionIndex - 1;
							selectedNestedCollectionIndex = targetCollection.NestedCollections.Count - 1;
							Debug.Log($"Nested collection '{nestedCollectionName}' into '{targetCollection.Name}'");
						}
						else if (jumpOut)
						{
							// Unnest: Move nested collection to top level, placed below current collection
							collection.NestedCollections.RemoveAt(selectedNestedCollectionIndex);
							collections.Insert(selectedCollectionIndex + 1, nestedCollection);
							nestedCollection.ParentCollection = null;
							selectedCollectionIndex = selectedCollectionIndex + 1;
							selectedNestedCollectionIndex = -1;
							Debug.Log($"Unnested collection '{nestedCollectionName}' to top level");
						}
						else if (jumpUp)
						{
							// Move nested collection to the collection above
							ChipCollection targetCollection = collections[selectedCollectionIndex - 1];
							collection.NestedCollections.RemoveAt(selectedNestedCollectionIndex);
							targetCollection.NestedCollections.Add(nestedCollection);
							nestedCollection.ParentCollection = targetCollection;
							selectedCollectionIndex = selectedCollectionIndex - 1;
							selectedNestedCollectionIndex = targetCollection.NestedCollections.Count - 1;
							Debug.Log($"Moved nested collection '{nestedCollectionName}' to collection above");
						}
						else if (jumpDown)
						{
							// Move nested collection to the collection below
							ChipCollection targetCollection = collections[selectedCollectionIndex + 1];
							collection.NestedCollections.RemoveAt(selectedNestedCollectionIndex);
							targetCollection.NestedCollections.Add(nestedCollection);
							nestedCollection.ParentCollection = targetCollection;
							selectedCollectionIndex = selectedCollectionIndex + 1;
							selectedNestedCollectionIndex = targetCollection.NestedCollections.Count - 1;
							Debug.Log($"Moved nested collection '{nestedCollectionName}' to collection below");
						}
						else if (moveUp)
						{
							// Move nested collection up within the same collection
							ChipCollection temp = collection.NestedCollections[selectedNestedCollectionIndex];
							collection.NestedCollections[selectedNestedCollectionIndex] = collection.NestedCollections[selectedNestedCollectionIndex - 1];
							collection.NestedCollections[selectedNestedCollectionIndex - 1] = temp;
							selectedNestedCollectionIndex--;
							Debug.Log($"Moved nested collection '{nestedCollectionName}' up within collection");
						}
						else if (moveDown)
						{
							// Move nested collection down within the same collection
							ChipCollection temp = collection.NestedCollections[selectedNestedCollectionIndex];
							collection.NestedCollections[selectedNestedCollectionIndex] = collection.NestedCollections[selectedNestedCollectionIndex + 1];
							collection.NestedCollections[selectedNestedCollectionIndex + 1] = temp;
							selectedNestedCollectionIndex++;
							Debug.Log($"Moved nested collection '{nestedCollectionName}' down within collection");
						}
					}
					// ---- Selected Collection UI ----
					else if (hasCollectionSelected)
					{
						// ---- Draw ----
						ChipCollection collection = collections[selectedCollectionIndex];
						string selectedCollectionName = collection.Name;

						bool isStarred = project.description.IsStarred(collection.Name, true);

						// Calculate all movement possibilities for regular collection
						bool canJumpIn = selectedCollectionIndex > 0; // Can nest into collection above if it exists
						bool canJumpOut = false; // Regular collections can't jump out (they're already at top level)
						bool canJumpUp = selectedCollectionIndex > 0; // Can move to collection above
						bool canJumpDown = selectedCollectionIndex < collections.Count - 1; // Can move to collection below
						bool canMoveUp = selectedCollectionIndex > 0; // Can move up within top level
						bool canMoveDown = selectedCollectionIndex < collections.Count - 1; // Can move down within top level
						
						// Set button states
						interactableStates_collectionMovement[0] = canJumpIn; // JUMP IN
						interactableStates_collectionMovement[1] = canJumpOut; // JUMP OUT
						interactableStates_collectionMovement[2] = canJumpUp; // JUMP UP
						interactableStates_collectionMovement[3] = canJumpDown; // JUMP DOWN
						interactableStates_collectionMovement[4] = canMoveUp; // MOVE UP
						interactableStates_collectionMovement[5] = canMoveDown; // MOVE DOWN
						
						// Draw all 6 movement buttons in 3 rows (2 buttons per row)
						int buttonIndex_movement = DrawCollectionMovementButtons(buttonNames_collectionMovement, interactableStates_collectionMovement, ref topLeft, panelContentBounds.Width);

						bool canRenameOrDelete = !ChipDescription.NameMatch(collection.Name, defaultOtherChipsCollectionName);
						interactableStates_renameDelete[0] = canRenameOrDelete;
						interactableStates_renameDelete[1] = canRenameOrDelete;
						int buttonIndexEditCollection = DrawHorizontalButtonGroup(buttonNames_collectionRenameOrDelete, interactableStates_renameDelete, ref topLeft, panelContentBounds.Width);

						// ---- Handle button inputs ----

						if (buttonIndexEditCollection == 0) // Rename collection
						{
							Seb.Vis.UI.UI.GetInputFieldState(ID_NameInput).ClearText();
							renamingCollection = true;
						}
						else if (buttonIndexEditCollection == 1) // Delete collection
						{
							if (collection.Chips.Count == 0) DeleteSelectedCollection();
							else
							{
								deleteConfirmMessage = $"Are you sure you want to delete this collection? The chips inside of it will be moved to \"{defaultOtherChipsCollectionName}\".";
								deleteConfirmMessage = Seb.Vis.UI.UI.LineBreakByCharCount(deleteConfirmMessage, deleteMessageMaxCharsPerLine);
								deleteConfirmMessageCol = deleteColWarningMedium;
								isConfirmingCollectionDeletion = true;
							}
						}

						// Handle movement buttons
						bool jumpIn = buttonIndex_movement == 0;
						bool jumpOut = buttonIndex_movement == 1;
						bool jumpUp = buttonIndex_movement == 2;
						bool jumpDown = buttonIndex_movement == 3;
						bool moveUp = buttonIndex_movement == 4;
						bool moveDown = buttonIndex_movement == 5;
						
						if (jumpIn) // JUMP IN - nest into collection above
						{
							// Check if there's a collection above to nest into
							if (selectedCollectionIndex > 0)
							{
								ChipCollection targetCollection = collections[selectedCollectionIndex - 1];
								collections.RemoveAt(selectedCollectionIndex);
								targetCollection.NestedCollections.Add(collection);
								collection.ParentCollection = targetCollection;
								selectedCollectionIndex = selectedCollectionIndex - 1;
								selectedNestedCollectionIndex = targetCollection.NestedCollections.Count - 1;
								Debug.Log($"Nested collection '{selectedCollectionName}' into '{targetCollection.Name}'");
							}
							else
							{
								Debug.Log("Cannot nest collection - no collection above to nest into");
							}
						}
						else if (jumpOut) // JUMP OUT - not applicable for regular collections
						{
							// Regular collections can't jump out (they're already at top level)
							Debug.Log("Regular collections cannot jump out");
						}
						else if (jumpUp) // JUMP UP - move to collection above
						{
							// This is the same as MOVE UP for regular collections
							int indexStart = selectedCollectionIndex;
							int indexEnd = selectedCollectionIndex - 1;
							(collections[indexStart], collections[indexEnd]) = (collections[indexEnd], collections[indexStart]);
							selectedCollectionIndex = indexEnd;
							collection = collections[selectedCollectionIndex];
							Debug.Log($"Moved collection '{selectedCollectionName}' to position above");
						}
						else if (jumpDown) // JUMP DOWN - move to collection below
						{
							// This is the same as MOVE DOWN for regular collections
							int indexStart = selectedCollectionIndex;
							int indexEnd = selectedCollectionIndex + 1;
							(collections[indexStart], collections[indexEnd]) = (collections[indexEnd], collections[indexStart]);
							selectedCollectionIndex = indexEnd;
							collection = collections[selectedCollectionIndex];
							Debug.Log($"Moved collection '{selectedCollectionName}' to position below");
						}
						else if (moveUp) // MOVE UP - move one position up
						{
							int indexStart = selectedCollectionIndex;
							int indexEnd = selectedCollectionIndex - 1;
							(collections[indexStart], collections[indexEnd]) = (collections[indexEnd], collections[indexStart]);
							selectedCollectionIndex = indexEnd;
							collection = collections[selectedCollectionIndex];
							Debug.Log($"Moved collection '{selectedCollectionName}' up one position");
						}
						else if (moveDown) // MOVE DOWN - move one position down
						{
							int indexStart = selectedCollectionIndex;
							int indexEnd = selectedCollectionIndex + 1;
							(collections[indexStart], collections[indexEnd]) = (collections[indexEnd], collections[indexStart]);
							selectedCollectionIndex = indexEnd;
							collection = collections[selectedCollectionIndex];
							Debug.Log($"Moved collection '{selectedCollectionName}' down one position");
						}
					}
					else if (hasStarredItemSelected)
					{
						StarredItem starredItem = project.description.StarredList[selectedStarredItemIndex];
						// Buttons: move down/up
						interactableStates_move[0] = selectedStarredItemIndex > 0; // can move up
						interactableStates_move[1] = selectedStarredItemIndex < project.description.StarredList.Count - 1; // can move down
						int buttonIndexOrganize = DrawHorizontalButtonGroup(buttonNames_moveSingleStep, interactableStates_move, ref topLeft, panelContentBounds.Width);

						if (!starredItem.IsCollection)
						{
							ChipActionButtons(starredItem.Name, ref topLeft, panelContentBounds.Width);
						}

						if (buttonIndexOrganize == 0 || buttonIndexOrganize == 1) // move down/up
						{
							int fromIndex = selectedStarredItemIndex;
							int targetIndex = buttonIndexOrganize == 0 ? fromIndex - 1 : fromIndex + 1;
							(project.description.StarredList[fromIndex], project.description.StarredList[targetIndex]) = (project.description.StarredList[targetIndex], project.description.StarredList[fromIndex]);
							selectedStarredItemIndex = targetIndex;
						}
					}

					topLeft = Seb.Vis.UI.UI.GetCurrentBoundsScope().BottomLeft + Vector2.down * SectionSpacing;
				}
				else
				{
					// When nothing is selected, add some spacing to maintain constant height
					topLeft.y -= 50f; // Add space to maintain constant total height
					
					topLeft = Seb.Vis.UI.UI.GetCurrentBoundsScope().BottomLeft + Vector2.down * SectionSpacing;
				}
			}

			if (!(isConfirmingChipDeletion || isConfirmingCollectionDeletion))
			{
				using (Seb.Vis.UI.UI.BeginBoundsScope(true))
				{
					panelID = Seb.Vis.UI.UI.ReservePanel();



					// Exit library button (only show when not in input mode)
					if (!renamingCollection && !creatingNewCollection)
					{
						Vector2 exitPos = Seb.Vis.UI.UI.GetCurrentBoundsScope().BottomLeft + Vector2.up * (3f);
						bool exit = Seb.Vis.UI.UI.Button("EXIT LIBRARY", ActiveUITheme.ButtonTheme, topLeft, new Vector2(panelContentBounds.Width, 0), true, false, true, ActiveUITheme.ButtonTheme.buttonCols, Anchor.TopLeft);
						if (exit) ExitLibrary();
						topLeft += Vector2.down * (Seb.Vis.UI.UI.PrevBounds.Height + DefaultButtonSpacing * 2);
					}



					topLeft = Seb.Vis.UI.UI.GetCurrentBoundsScope().BottomLeft + Vector2.down * SectionSpacing;
				}

            }

            // Delete confirmation
            if (isConfirmingChipDeletion || isConfirmingCollectionDeletion)
			{
				using (Seb.Vis.UI.UI.BeginBoundsScope(true))
				{
					using (Seb.Vis.UI.UI.BeginDisabledScope(false))
					{
						panelID = Seb.Vis.UI.UI.ReservePanel();
						Seb.Vis.UI.UI.DrawText(deleteConfirmMessage, ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular, topLeft, Anchor.TopLeft, deleteConfirmMessageCol);
						topLeft += Vector2.down * (Seb.Vis.UI.UI.PrevBounds.Height + DefaultButtonSpacing * 3f);
						int button_cancelConfirm = MenuHelper.DrawButtonPair("CANCEL", "DELETE", topLeft, panelContentBounds.Width, false);

						if (button_cancelConfirm == 0) // cancel delete
						{
							ResetPopupState();
						}
						else if (button_cancelConfirm == 1 || KeyboardShortcuts.ConfirmShortcutTriggered) // confirm delete
						{
							if (isConfirmingChipDeletion)
							{

								if (selectedCollectionIndex != -1) // deleting from collection
								{
									ChipCollection collection = collections[selectedCollectionIndex];
									string chipName = collection.Chips[selectedChipInCollectionIndex];
									project.DeleteChip(chipName);
									selectedChipInCollectionIndex = Mathf.Min(selectedChipInCollectionIndex, collection.Chips.Count - 1);
								}
								else // deleting chip
								{
									string chipName = project.description.StarredList[selectedStarredItemIndex].Name;
									project.DeleteChip(chipName);
									selectedStarredItemIndex = Mathf.Min(selectedStarredItemIndex, project.description.StarredList.Count - 1);
								}
							}
							else if (isConfirmingCollectionDeletion)
							{
								DeleteSelectedCollection();
							}

							ResetPopupState();
						}

					}
				}
			}

			return;

			static void DrawHeader(string text, Color bgCol, Color textCol, ref Vector2 topLeft, float width, float spacingBelow = DefaultButtonSpacing)
			{
				MenuHelper.DrawCentredTextWithBackground(text, topLeft, new Vector2(width, 2), Anchor.TopLeft, textCol, bgCol);
				topLeft += Vector2.down * (Seb.Vis.UI.UI.PrevBounds.Height + spacingBelow);
			}

		static void ChipActionButtons(string selectedChipName, ref Vector2 topLeft, float width)
			{
				bool isBuiltin = project.chipLibrary.IsBuiltinChip(selectedChipName);
				bool isSpecialCustom = project.description.isPlayerAddedSpecialChip(selectedChipName); 
				interactable_chipActionButtons[0] = project.ViewedChip.CanAddSubchip(selectedChipName);
				interactable_chipActionButtons[1] = !isBuiltin;
				interactable_chipActionButtons[2] = !(isBuiltin && !isSpecialCustom);
				int chipActionIndex = DrawHorizontalButtonGroup(buttonNames_chipAction, interactable_chipActionButtons, ref topLeft, width);

				if (chipActionIndex == 0) // use
				{
					project.controller.StartPlacing(project.chipLibrary.GetChipDescription(selectedChipName));
					ExitLibrary();
				}
				else if (chipActionIndex == 1) // open
				{
					chipToOpenName = selectedChipName;
					if (project.ActiveChipHasUnsavedChanges())
					{
						UnsavedChangesPopup.OpenPopup(OpenChipIfConfirmed);
					}
					else
					{
						OpenChipIfConfirmed(true);
					}
				}
				else if (chipActionIndex == 2) // delete
				{
					isConfirmingChipDeletion = true;
					(string msg, bool warn) = CreateDeleteConfirmationMessage(selectedChipName);
					deleteConfirmMessage = msg;
					deleteConfirmMessageCol = warn ? deleteColWarningHigh : deleteColWarningMedium;
				}
			}

			static ChipCollection MoveSelectedChipToNewCollection(int newCollectionIndex)
			{
				ChipCollection collectionOld = collections[selectedCollectionIndex];
				string chipName = collectionOld.Chips[selectedChipInCollectionIndex];

				collectionOld.Chips.RemoveAt(selectedChipInCollectionIndex);
				// If this collection was opened automatically when the chip was moved to it previously, close it automatically now that chip is leaving it
				if (collectionOld == lastAutoOpenedCollection)
				{
					lastAutoOpenedCollection = null;
					collectionOld.IsToggledOpen = false;
				}

				bool movingUp = newCollectionIndex < selectedCollectionIndex;
				selectedCollectionIndex = newCollectionIndex;
				ChipCollection collectionNew = collections[selectedCollectionIndex];

				if (movingUp)
				{
					collectionNew.Chips.Add(chipName);
					selectedChipInCollectionIndex = collectionNew.Chips.Count - 1;
				}
				else
				{
					collectionNew.Chips.Insert(0, chipName);
					selectedChipInCollectionIndex = 0;
				}

				if (!collectionNew.IsToggledOpen)
				{
					lastAutoOpenedCollection = collectionNew;
					collectionNew.IsToggledOpen = true;
				}

				return collectionNew;
			}
		}


		public static void OnMenuOpened()
		{
			wasOpenedThisFrame = true;
			//Debug.Log("Overriding chip collections with defaults");
			//Project.ActiveProject.description.ChipCollections = new(Main.CreateDefaultChipCollections());

			// Ensure the mandatory "OTHER" collection exists
			if (GetDefaultCollection() == null)
			{
				collections.Add(new ChipCollection(defaultOtherChipsCollectionName));
			}


			// Automatically add any chips not in a collection to the "other" collection
			HashSet<string> chipsInCollection = new(collections.SelectMany(c => c.Chips), ChipDescription.NameComparer);
			ChipCollection defaultCollection = GetDefaultCollection();

			foreach (ChipDescription chip in project.chipLibrary.allChips)
			{
				if (!chipsInCollection.Contains(chip.Name))
				{
					defaultCollection.Chips.Add(chip.Name);
				}
			}

			// Reset state
			ResetPopupState();

			selectedStarredItemIndex = Mathf.Min(selectedStarredItemIndex, project.description.StarredList.Count - 1);
		}

		public static void Reset()
		{
			selectedStarredItemIndex = -1;
			selectedCollectionIndex = 0;
			selectedChipInCollectionIndex = -1;
			ResetPopupState();
		}

		static void DeleteSelectedCollection()
		{
			ChipCollection defaultCollection = GetDefaultCollection();
			ChipCollection collectionToDelete = collections[selectedCollectionIndex];

			foreach (string chipName in collectionToDelete.Chips)
			{
				defaultCollection.Chips.Add(chipName);
			}

			project.SetStarred(collectionToDelete.Name, false, true, false);
			collections.RemoveAt(selectedCollectionIndex);
			selectedCollectionIndex = Mathf.Max(0, selectedCollectionIndex - 1);

			project.SaveCurrentProjectDescription();
		}

		static ChipCollection GetDefaultCollection() => collections.FirstOrDefault(c => ChipDescription.NameMatch(c.Name, defaultOtherChipsCollectionName));

		static bool ValidateCollectionNameInput(string name)
		{
			return name.Length <= 24;
		}

		static bool IsValidCollectionName(string name)
		{
			if (!ValidateCollectionNameInput(name)) return false;

			if (string.IsNullOrWhiteSpace(name)) return false;

			for (int i = 0; i < collections.Count; i++)
			{
				ChipCollection collection = collections[i];
				if (i == selectedCollectionIndex) continue;

				if (ChipDescription.NameMatch(collection.Name, name)) return false;
			}

			return true;
		}

		static bool IsValidFolderName(string name)
		{
			if (!ValidateCollectionNameInput(name)) return false;

			if (string.IsNullOrWhiteSpace(name)) return false;

			// Check for duplicate nested collection names within the same collection
			if (selectedCollectionIndex != -1)
			{
				ChipCollection collection = collections[selectedCollectionIndex];
				foreach (var nestedCollection in collection.NestedCollections)
				{
					if (ChipDescription.NameMatch(nestedCollection.Name, name)) return false;
				}
			}

			return true;
		}

		static ButtonTheme GetButtonTheme(bool isCollection, bool isSelected) =>
			isCollection
				? isSelected ? ActiveUITheme.ChipLibraryCollectionToggleOn : ActiveUITheme.ChipLibraryCollectionToggleOff
				: isSelected
					? ActiveUITheme.ChipLibraryChipToggleOn
					: ActiveUITheme.ChipLibraryChipToggleOff;

		static ButtonTheme GetFolderTheme(bool isSelected)
		{
			// Create green theme for folders
			ButtonTheme folderTheme = ActiveUITheme.ChipLibraryCollectionToggleOff;
			if (isSelected)
			{
				// Green selection color for folders
				folderTheme.buttonCols.normal = ColHelper.MakeCol("#3CD168"); // Green color
				folderTheme.textCols.normal = Color.white;
			}
			else
			{
				// Default folder appearance
				folderTheme.buttonCols.normal = ColHelper.MakeCol("#2D2D2D");
				folderTheme.textCols.normal = ColHelper.MakeCol("#3CD168"); // Green text
			}
			return folderTheme;
		}

		static void OpenChipIfConfirmed(bool confirm)
		{
			if (confirm)
			{
				// Check for level unsaved changes before opening chip
				if (LevelManager.Instance?.IsActive == true && LevelManager.Instance.HasUnsavedChanges())
				{
					LevelUnsavedChangesPopup.OpenPopup(OpenChipAfterLevelCheck);
				}
				else
				{
					OpenChipAfterLevelCheck(2); // Continue without saving (since there are no changes)
				}

				void OpenChipAfterLevelCheck(int option)
				{
					if (option == 0) // Cancel
					{
						// Do nothing, stay in current level
						return;
					}
					else if (option == 1) // Save and Continue
					{
						// Save level progress before opening chip
						if (LevelManager.Instance?.IsActive == true)
						{
							LevelManager.Instance.SaveCurrentProgress();
						}
						
						project.LoadDevChipOrCreateNewIfDoesntExist(chipToOpenName);
						ExitLibrary();
						LevelManager.Instance?.ExitLevel();
					}
					else if (option == 2) // Continue without Saving
					{
						// Open chip without saving level progress
						project.LoadDevChipOrCreateNewIfDoesntExist(chipToOpenName);
						ExitLibrary();
						LevelManager.Instance?.ExitLevel();
					}
				}  
			}
			else
			{
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.ChipLibrary);
			}
		}

		static void ExitLibrary()
		{
			project.UpdateAndSaveProjectDescription();
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
		}

		static (string msg, bool warn) CreateDeleteConfirmationMessage(string chipName)
		{
			List<string> parentNames = project.chipLibrary.GetDirectParentChips(chipName).Select(c => c.Name).ToList();
			bool usedInCurrentChip = Project.ActiveProject.ViewedChip.GetSubchips().Any(s => s.Description.NameMatch(chipName));

			if (usedInCurrentChip)
			{
				parentNames.Remove(Project.ActiveProject.ViewedChip.ChipName);
				parentNames.Insert(0, "the CURRENT CHIP");
			}

			string message = "Are you sure you want to delete this chip? ";
			bool warn = parentNames.Count > 0;

			if (Project.ActiveProject.ViewedChip.LastSavedDescription?.NameMatch(chipName) == true)
			{
				message = "Are you sure you want to delete the chip that you are CURRENTLY EDITING? ";
				warn = true;
			}

			if (parentNames.Count == 0) message += "It is not used anywhere.";
			else message += CreateChipInUseWarningMessage(parentNames);

			string formattedMessage = Seb.Vis.UI.UI.LineBreakByCharCount(message, deleteMessageMaxCharsPerLine);
			return (formattedMessage, warn);

			string CreateChipInUseWarningMessage(List<string> chipsUsingCurrentChip)
			{
				int numUses = chipsUsingCurrentChip.Count;
				string usage = "It is used by";
				if (numUses == 1) return $"{usage} {FormatChipName(0)}.";
				if (numUses == 2) return $"{usage} {FormatChipName(0)} and {FormatChipName(1)}.";
				if (numUses > 2) return $"{usage} {FormatChipName(0)} and {numUses - 1} others.";
				return string.Empty;

				string FormatChipName(int index)
				{
					bool useQuotes = !(index == 0 && usedInCurrentChip);
					string formatted = useQuotes ? $"\"{chipsUsingCurrentChip[index]}\"" : chipsUsingCurrentChip[index];
					return formatted;
				}
			}
		}

		static void DrawChipPreview(Bounds2D panelContentBounds, bool hasChipSelected, bool hasStarredItemSelected, bool hasNestedChipSelected = false)
		{
			// Always draw preview window
			const float previewWidth = 28f;
			const float previewHeight = previewWindowHeight;
			const float margin = 0.7f;
			// Position preview in top-right corner
			Vector2 previewTopLeft = panelContentBounds.TopRight + Vector2.left * previewWidth + Vector2.down * margin;

			// Draw preview background (always)
			Color previewBgCol = new Color(0.1f, 0.1f, 0.1f, 0.95f);
			MenuHelper.DrawLeftAlignTextWithBackground("", previewTopLeft, new Vector2(previewWidth, previewHeight), Anchor.TopLeft, Color.white, previewBgCol, true);

			// Only draw chip content if something is selected
			if (!hasChipSelected && !hasStarredItemSelected && !hasNestedChipSelected)
			{
				return; // Empty preview window
			}

			// Get the selected chip description
			string selectedChipName;
			
			if (hasChipSelected)
			{
				ChipCollection collection = collections[selectedCollectionIndex];
				selectedChipName = collection.Chips[selectedChipInCollectionIndex];
			}
			else if (hasNestedChipSelected)
			{
				ChipCollection collection = collections[selectedCollectionIndex];
				ChipCollection nestedCollection = collection.NestedCollections[selectedNestedCollectionIndex];
				selectedChipName = nestedCollection.Chips[selectedChipInNestedCollectionIndex];
			}
			else if (hasStarredItemSelected)
			{
				StarredItem starredItem = project.description.StarredList[selectedStarredItemIndex];
				selectedChipName = starredItem.Name;
			}
			else
			{
				return; // No chip selected
			}
			
			if (!project.chipLibrary.TryGetChipDescription(selectedChipName, out ChipDescription chipDesc))
			{
				Debug.Log($"Failed to get chip description for: {selectedChipName}");
				return;
			}
			
			Debug.Log($"Drawing chip preview for: {selectedChipName}, InputPins: {chipDesc.InputPins?.Length ?? 0}, OutputPins: {chipDesc.OutputPins?.Length ?? 0}");

			// Calculate chip preview scale and position
			Vector2 chipSize = chipDesc.Size;
			Debug.Log($"Chip size: {chipSize}, previewWidth: {previewWidth}, previewHeight: {previewHeight}");
			
			// Handle zero or invalid chip size
			if (chipSize.x <= 0 || chipSize.y <= 0)
			{
				Debug.Log($"Invalid chip size: {chipSize}, using default size");
				chipSize = new Vector2(2f, 1f); // Default size for chips
			}
			
			Debug.Log($"ChipSize (after default check): {chipSize}");
			
			// For DevPins (IN/OUT), we need to account for the extended elements (state display + pin + handle)
			// Calculate the total width needed for DevPin elements
			float totalDevPinWidth = 0f;
			float devPinHeight = chipSize.y; // Store original height for DevPins
			if (chipDesc.ChipType == ChipType.In_Pin || chipDesc.ChipType == ChipType.Out_Pin)
			{
				// Estimate total width: state display + gap + pin + handle
				uint bitCount = chipDesc.InputPins?.Length > 0 ? chipDesc.InputPins[0].BitCount : 
				               chipDesc.OutputPins?.Length > 0 ? chipDesc.OutputPins[0].BitCount : 1;
				
				float stateDisplayWidth = bitCount == 1 ? 
					DrawSettings.DevPinStateDisplayRadius * 2 + DrawSettings.DevPinStateDisplayOutline * 2 :
					GridHelper.GetStateGridDimension((int)bitCount).x * DrawSettings.MultiBitPinStateDisplaySquareSize + DrawSettings.DevPinStateDisplayOutline;
				
				// Calculate pin width and height
				float pinWidth = DrawSettings.PinRadius * 2 * 0.95f;
				float pinHeight = SubChipInstance.PinHeightFromBitCount(bitCount);
				float gap = 0.5f; // Gap between state display and pin
				
				// Use body width + pin width for horizontal scaling
				totalDevPinWidth = stateDisplayWidth + gap + pinWidth;
				
				// Use the larger of body height and pin height for vertical scaling (DevPins only)
				float stateDisplayHeight = bitCount == 1 ? 
					DrawSettings.DevPinStateDisplayRadius * 2 + DrawSettings.DevPinStateDisplayOutline * 2 :
					GridHelper.GetStateGridDimension((int)bitCount).y * DrawSettings.MultiBitPinStateDisplaySquareSize + DrawSettings.DevPinStateDisplayOutline;
				
				devPinHeight = Mathf.Max(stateDisplayHeight, pinHeight);
			}
			
			// Calculate scale so that chip width OR height covers 80% of preview window
			float targetWidth = previewWidth * 0.8f; // 80% of preview width
			float targetHeight = previewHeight * 0.8f; // 80% of preview height
			
			float scaleX = targetWidth / (totalDevPinWidth > 0f ? totalDevPinWidth : chipSize.x);
			float scaleY = targetHeight / (totalDevPinWidth > 0f ? devPinHeight : chipSize.y);
			
			Debug.Log($"Scaling calculation: targetWidth={targetWidth}, targetHeight={targetHeight}, chipSize={chipSize}, totalDevPinWidth={totalDevPinWidth}, devPinHeight={devPinHeight}, scaleX={scaleX}, scaleY={scaleY}");
			
			float scale = Mathf.Min(scaleX, scaleY); // Use smaller scale so chip fits within 80% of both dimensions
			
			// Ensure minimum scale for visibility
			scale = Mathf.Max(scale, 0.1f);
			
			Vector2 scaledSize = chipSize * scale;

			// Draw chip using UI drawing methods (not game world methods)
			Color chipCol = chipDesc.Colour;
			
			// Handle transparent or invalid chip colors
			if (chipCol.a <= 0f)
			{
				Debug.Log($"Invalid chip color: {chipCol}, using default color");
				chipCol = new Color(0.2f, 0.2f, 0.2f, 1f); // Default dark gray
			}
			
			Color outlineCol = GetChipPreviewOutlineCol(chipCol);
			
			// Use UI drawing methods instead of game world methods
			// Calculate center of preview window
			Vector2 previewCenter = previewTopLeft + new Vector2(previewWidth * 0.5f, -previewHeight * 0.5f);
			
			// For DevPins, use the HandlePosition as the center (like the game)
			Vector2 drawCenter = previewCenter;
			if (chipDesc.ChipType == ChipType.In_Pin || chipDesc.ChipType == ChipType.Out_Pin)
			{
				// DevPins are centered on their HandlePosition, not between state display and pin
				// The HandlePosition is the center point that both state display and pin are positioned relative to
				drawCenter = previewCenter;
			}
			
			// Draw chip using new UI drawing methods that replicate game drawing exactly
			Debug.Log($"Drawing chip preview: center={previewCenter}, scaledSize={scaledSize}, scale={scale}, chipCol={chipCol}");
			
			// For In_Pin and Out_Pin, don't draw chip body/outline - they use DevPin-style display instead
			if (chipDesc.ChipType != ChipType.In_Pin && chipDesc.ChipType != ChipType.Out_Pin)
			{
				// Draw outline first (behind everything)
				UI_DrawChipOutline(drawCenter, chipSize, outlineCol, scale);
				
				// Draw pins second (behind chip body, like in game)
				try
				{
					DrawChipPreviewPins(chipDesc, drawCenter, scaledSize, scale);
				}
				catch (System.Exception ex)
				{
					Debug.LogError($"Exception in DrawChipPreviewPins: {ex.Message}\n{ex.StackTrace}");
				}
				
				// Draw chip body last (on top of pins, like in game)
				Debug.Log($"About to draw chip body at {drawCenter} with size {chipSize} and color {chipCol}");
				UI_DrawChipBody(drawCenter, chipSize, chipCol, scale);
			}
			else
			{
				// For In_Pin and Out_Pin, don't draw pins here - DrawDevPinStyleDisplay handles pin drawing
				// (DrawChipPreviewPins would draw pins based on chip description, but DevPins have custom positioning)
			}
			
			// Draw chip text
			DrawChipPreviewText(chipDesc, drawCenter, chipSize, scale, chipCol);
			
			// Draw chip displays
			DrawChipPreviewDisplays(chipDesc, drawCenter, chipSize, scale);
		}

		static Color GetChipPreviewOutlineCol(Color chipCol)
		{
			// Make outline darker than chip color
			return new Color(chipCol.r * 0.7f, chipCol.g * 0.7f, chipCol.b * 0.7f, chipCol.a);
		}

		static void UI_DrawChipBody(Vector2 pos, Vector2 size, Color chipCol, float scale)
		{
			Vector2 scaledSize = size * scale;
			Debug.Log($"Drawing chip body at {pos} with size {scaledSize} and color {chipCol}");
			Seb.Vis.UI.UI.DrawPanel(pos, scaledSize, chipCol, Anchor.Centre);
		}

		static void UI_DrawChipOutline(Vector2 pos, Vector2 size, Color outlineCol, float scale)
		{
			Vector2 scaledSize = size * scale;
			Vector2 outlineSize = scaledSize + Vector2.one * (DrawSettings.ChipOutlineWidth * scale);
			Seb.Vis.UI.UI.DrawPanel(pos, outlineSize, outlineCol, Anchor.Centre);
		}

		static void UI_DrawChipPin(Vector2 pos, Color pinCol, float scale)
		{
			float pinRadius = DrawSettings.PinRadius * scale;
			Debug.Log($"UI_DrawChipPin: pos={pos}, radius={pinRadius}, col={pinCol}");
			Seb.Vis.UI.UI.DrawPoint(pos, pinRadius, pinCol);
		}

		static void UI_DrawPreviewPin(PinDescription pinDesc, Vector2 pinPos, float scale)
		{
			// Replicate the game's exact pin drawing logic
			if (pinDesc.BitCount == PinBitCount.Bit1)
			{
				UI_DrawPreviewSingleBitPin(pinDesc, pinPos, scale);
			}
			else
			{
				DrawMultiBitPinPreview(pinDesc, pinPos, scale);
			}
		}

		static void UI_DrawPreviewSingleBitPin(PinDescription pinDesc, Vector2 pinPos, float scale)
		{
			// Replicate DrawSingleBitPin exactly
			float pinRadius = DrawSettings.PinRadius * scale;
			Color pinCol = ActiveTheme.PinCol;
			
			Seb.Vis.UI.UI.DrawPoint(pinPos, pinRadius, pinCol);
		}

		static void DrawMultiBitPinPreview(PinDescription pinDesc, Vector2 pinPos, float scale)
		{
			// Multi-bit pin: simple rectangle (exactly like game's DrawMultiBitPin)
			int pinFace = pinDesc.face; // Use actual pin face from description
			bool isHorizontal = pinFace == 0 || pinFace == 2; // face 0=up, 2=down are horizontal
			float pinWidth = DrawSettings.PinRadius * 2 * 0.95f * scale;
			float pinHeight = SubChipInstance.PinHeightFromBitCount(pinDesc.BitCount) * scale;
			Vector2 pinSize = isHorizontal ? new Vector2(pinHeight, pinWidth) : new Vector2(pinWidth, pinHeight);
			
			Color pinCol = ActiveTheme.PinCol;
			Seb.Vis.UI.UI.DrawQuad(pinPos, pinSize, pinCol);
			
			// Draw pin size indicator for large pins (like in game)
			if (pinDesc.BitCount >= 64)
			{
				Vector2 depthIndicatorSize = isHorizontal ? new(pinHeight, pinWidth / 8f) : new(pinWidth / 8f, pinHeight);
				// Use actual facing direction based on pin face
				Vector2 facingDir = pinFace == 1 ? Vector2.right : pinFace == 3 ? Vector2.left : 
				                   pinFace == 2 ? Vector2.down : Vector2.up;
				Seb.Vis.UI.UI.DrawQuad(pinPos + facingDir * 0.25f * pinWidth, depthIndicatorSize, ActiveTheme.PinSizeIndicatorColors[pinDesc.BitCount.GetTier()]);
			}
		}

		static void DrawChipPreviewPins(ChipDescription chipDesc, Vector2 chipPos, Vector2 chipSize, float scale)
		{
			Debug.Log($"DrawChipPreviewPins called: {chipDesc.Name}, HasCustomLayout={chipDesc.HasCustomLayout}, InputPins={chipDesc.InputPins?.Length ?? 0}, OutputPins={chipDesc.OutputPins?.Length ?? 0}");
			
			// Scale pin radius by the same factor as the chip, but divide by 2 since Seb.Vis.UI.UI.DrawPoint doubles it
			Color pinCol = ActiveTheme.PinCol;
			float pinRadius = (DrawSettings.PinRadius * scale) / 2f; // Seb.Vis.UI.UI.DrawPoint doubles the radius internally

			// For built-in chips without custom layout, simulate the automatic pin layout
			if (!chipDesc.HasCustomLayout)
			{
				// Calculate pin positions using the same logic as the game
				float[] inputPinYPositions = CalculatePinYPositions(chipDesc.InputPins, chipSize.y, scale);
				float[] outputPinYPositions = CalculatePinYPositions(chipDesc.OutputPins, chipSize.y, scale);

				// Draw input pins (face = 3, left edge)
				if (chipDesc.InputPins != null && chipDesc.InputPins.Length > 0)
				{
					Debug.Log($"Drawing {chipDesc.InputPins.Length} input pins");
					for (int i = 0; i < chipDesc.InputPins.Length; i++)
					{
						PinDescription pinDesc = chipDesc.InputPins[i];
						Vector2 pinPos = CalculatePinPosition(chipPos, chipSize, 3, inputPinYPositions[i], scale); // face 3 = left
						Debug.Log($"Input pin {i}: face=3 (left), y={inputPinYPositions[i]}, pos={pinPos}");
						
						DrawPin(pinPos, pinDesc.BitCount, pinCol, pinRadius, false);
					}
				}

				// Draw output pins (face = 1, right edge)
				if (chipDesc.OutputPins != null && chipDesc.OutputPins.Length > 0)
				{
					Debug.Log($"Drawing {chipDesc.OutputPins.Length} output pins");
					for (int i = 0; i < chipDesc.OutputPins.Length; i++)
					{
						PinDescription pinDesc = chipDesc.OutputPins[i];
						Vector2 pinPos = CalculatePinPosition(chipPos, chipSize, 1, outputPinYPositions[i], scale); // face 1 = right
						Debug.Log($"Output pin {i}: face=1 (right), y={outputPinYPositions[i]}, pos={pinPos}");
						
						DrawPin(pinPos, pinDesc.BitCount, pinCol, pinRadius, true);
					}
				}
			}
			else
			{
				// Custom layout: use the actual Position from pin descriptions
				// Draw input pins
				if (chipDesc.InputPins != null && chipDesc.InputPins.Length > 0)
				{
					Debug.Log($"Drawing {chipDesc.InputPins.Length} input pins with custom layout");
					for (int i = 0; i < chipDesc.InputPins.Length; i++)
					{
						PinDescription pinDesc = chipDesc.InputPins[i];
						Vector2 pinPos = chipPos + pinDesc.Position * scale;
						Debug.Log($"Input pin {i}: custom pos={pinPos}");
						DrawPin(pinPos, pinDesc.BitCount, pinCol, pinRadius, false);
					}
				}

				// Draw output pins
				if (chipDesc.OutputPins != null && chipDesc.OutputPins.Length > 0)
				{
					Debug.Log($"Drawing {chipDesc.OutputPins.Length} output pins with custom layout");
					for (int i = 0; i < chipDesc.OutputPins.Length; i++)
					{
						PinDescription pinDesc = chipDesc.OutputPins[i];
						Vector2 pinPos = chipPos + pinDesc.Position * scale;
						Debug.Log($"Output pin {i}: custom pos={pinPos}");
						DrawPin(pinPos, pinDesc.BitCount, pinCol, pinRadius, true);
					}
				}
			}
		}

		// Calculate Y positions for pins using the same logic as SubChipInstance.CalculatePinLayout
		static float[] CalculatePinYPositions(PinDescription[] pins, float chipHeight, float scale)
		{
			if (pins == null || pins.Length == 0) return new float[0];
			
			// If only one pin, it should be placed in the centre
			if (pins.Length == 1)
			{
				return new float[] { 0f };
			}

			// Calculate pin layout using the same logic as CalculateDefaultPinLayout
			var pinBitCounts = pins.Select(p => p.BitCount).ToArray();
			var (chipHeightRequired, pinGridY) = SubChipInstance.CalculateDefaultPinLayout(pinBitCounts);
			
			float chipTop = chipHeight / 2f;
			float startY = chipTop;
			float[] pinYPositions = new float[pins.Length];

			// First pass: layout pins without any spacing between them
			for (int i = 0; i < pins.Length; i++)
			{
				pinYPositions[i] = startY + pinGridY[i] * DrawSettings.GridSize * scale;
			}

			// Second pass: evenly distribute the remaining space between the pins
			float spaceRemaining = chipHeight - chipHeightRequired * scale;
			if (spaceRemaining > 0)
			{
				float spacingBetweenPins = spaceRemaining / (pins.Length - 1);
				for (int i = 1; i < pins.Length; i++)
				{
					pinYPositions[i] -= spacingBetweenPins * i;
				}
			}

			return pinYPositions;
		}

		// Calculate pin position based on face and Y position
		static Vector2 CalculatePinPosition(Vector2 chipPos, Vector2 chipSize, int face, float yPos, float scale)
		{
			float halfWidth = chipSize.x / 2f;
			float halfHeight = chipSize.y / 2f;
			float inset = DrawSettings.SubChipPinInset * scale; // Scale the inset
			float outlineOffset = DrawSettings.ChipOutlineWidth / 2f * scale; // Scale the outline

			float x = 0f;
			float y = yPos; // Use the calculated Y position

			switch (face)
			{
				case 0: // Top edge
					x = 0f; // Center horizontally
					y = halfHeight + outlineOffset - inset;
					break;
				case 1: // Right edge
					x = halfWidth + outlineOffset - inset;
					// y is already set correctly
					break;
				case 2: // Bottom edge
					x = 0f; // Center horizontally
					y = -halfHeight - outlineOffset + inset;
					break;
				case 3: // Left edge
					x = -halfWidth - outlineOffset + inset;
					// y is already set correctly
					break;
				default:
					throw new Exception("Invalid pin face: " + face);
			}

			return chipPos + new Vector2(x, y);
		}

		// Draw a pin (single-bit or multi-bit)
		static void DrawPin(Vector2 pinPos, PinBitCount bitCount, Color pinCol, float pinRadius, bool isOutput)
		{
			if (bitCount == PinBitCount.Bit1)
			{
				Debug.Log($"Drawing 1-bit {(isOutput ? "output" : "input")} pin at {pinPos} with radius {pinRadius}");
				Seb.Vis.UI.UI.DrawPoint(pinPos, pinRadius, pinCol);
			}
			else
			{
				// Multi-bit pin: rectangle - scale by the same factor as the radius
				float scale = (pinRadius * 2f) / DrawSettings.PinRadius; // Calculate scale factor from radius (pinRadius was already divided by 2)
				float pinWidth = DrawSettings.PinRadius * 2 * 0.95f * scale;
				float pinHeight = SubChipInstance.PinHeightFromBitCount(bitCount) * scale;
				Vector2 pinSize = new Vector2(pinWidth, pinHeight);
				Debug.Log($"Drawing multi-bit {(isOutput ? "output" : "input")} pin at {pinPos} with size {pinSize}");
				Seb.Vis.UI.UI.DrawQuad(pinPos, pinSize, pinCol);
			}
		}

		static void DrawChipPreviewText(ChipDescription chipDesc, Vector2 chipPos, Vector2 chipSize, float scale, Color chipCol)
		{
			// Draw chip text using the same logic as the game
			if (chipDesc.NameLocation == NameDisplayLocation.Hidden)
				return;

			float fontSize = DrawSettings.FontSizeChipName * scale;
			Color textCol = chipCol.r + chipCol.g + chipCol.b > 1.5f ? Color.black : Color.white;

			Vector2 textPos = chipPos;
			if (chipDesc.NameLocation == NameDisplayLocation.Top)
			{
				textPos.y += chipSize.y * 0.5f + fontSize * 0.5f;
				// Draw background band for top text
				Vector2 bandSize = new Vector2(chipSize.x + 1f, fontSize + 0.5f);
				Seb.Vis.UI.UI.DrawPanel(textPos, bandSize, chipCol, Anchor.Centre);
			}

			Seb.Vis.UI.UI.DrawText(chipDesc.Name, FontType.JetbrainsMonoRegular, fontSize, textPos, Anchor.Centre, textCol);
		}

		static void DrawChipPreviewDisplays(ChipDescription chipDesc, Vector2 chipPos, Vector2 chipSize, float scale)
		{
			// Draw chip displays if they exist (like 7-segment, RGB, etc.)
			Debug.Log($"DrawChipPreviewDisplays: HasDisplay={chipDesc.HasDisplay()}, Displays={chipDesc.Displays?.Length ?? 0}, ChipType={chipDesc.ChipType}");
			if (chipDesc.HasDisplay() && chipDesc.Displays != null)
			{
				foreach (var display in chipDesc.Displays)
				{
					// Calculate display position within chip
					Vector2 displayPos = chipPos + display.Position * scale;
					float displayScale = scale * display.Scale;
					Debug.Log($"Drawing display at {displayPos} with scale {displayScale}");
					
					// For custom chips, we need to look at the actual DisplayInstance objects
					// For built-in chips, we use the chip's main ChipType
					if (chipDesc.ChipType == ChipType.Custom)
					{
						// Custom chips: try to create a temporary DisplayInstance to get the display type
						// This is a simplified approach - in reality we'd need full access to the chip instances
						DrawCustomChipDisplay(display, displayPos, displayScale);
					}
					else
					{
						// Built-in chips: draw based on chip type
						DrawBuiltinChipDisplay(chipDesc.ChipType, displayPos, displayScale);
					}
				}
			}
			else
			{
				// Special handling for In_Pin and Out_Pin chip types - draw like DevPins in game
				if (chipDesc.ChipType == ChipType.In_Pin || chipDesc.ChipType == ChipType.Out_Pin)
				{
					Debug.Log($"Drawing DevPin-style display for {chipDesc.ChipType}");
					DrawDevPinStyleDisplay(chipDesc, chipPos, chipSize, scale);
				}
			}
		}

		static void DrawCustomChipDisplay(DisplayDescription displayDesc, Vector2 displayPos, float displayScale)
		{
			// For custom chips, we need to determine what type of display this is
			// Since we don't have access to the full DisplayInstance, we'll use heuristics
			
			// Try to get the chip description of the displayed sub-chip
			// This is a simplified approach - in reality we'd need full access to the chip instances
			
			// For now, draw a generic display that indicates it's a custom chip
			// The actual implementation would need to:
			// 1. Get the SubChipInstance for the displayDesc.SubChipID
			// 2. Look at its DisplayType
			// 3. Draw the appropriate display type
			
			// Placeholder: draw a cyan panel to indicate custom chip
			Seb.Vis.UI.UI.DrawPanel(displayPos, Vector2.one * displayScale, Color.cyan, Anchor.Centre);
			Seb.Vis.UI.UI.DrawText("CUSTOM", FontType.JetbrainsMonoRegular, displayScale * 0.3f, displayPos, Anchor.Centre, Color.white);
		}

		static void DrawDevPinStyleDisplay(ChipDescription chipDesc, Vector2 chipPos, Vector2 chipSize, float scale)
		{
			// Replicate the game's DevPin drawing for IN/OUT pins exactly
			// Determine bit count from the chip description
			uint bitCount = 1;
			
			// Determine chip type and bit count
			bool isInChip = chipDesc.ChipType == ChipType.In_Pin;
			
			if (chipDesc.InputPins != null && chipDesc.InputPins.Length > 0)
			{
				bitCount = chipDesc.InputPins[0].BitCount;
			}
			else if (chipDesc.OutputPins != null && chipDesc.OutputPins.Length > 0)
			{
				bitCount = chipDesc.OutputPins[0].BitCount;
			}
			
			Debug.Log($"Drawing DevPin display: chipType={chipDesc.ChipType}, isInChip={isInChip}, bitCount={bitCount}, chipPos={chipPos}, chipSize={chipSize}, scale={scale}");
			
			// Calculate positions to center the DevPin elements in the preview window
			Vector2 faceDir = new Vector2(isInChip ? 1 : -1, 0);
			Vector2 centerPos = chipPos; // This is the center of the preview window
			
			// Calculate state display size
			Vector2 stateGridSize = bitCount == 1 ? 
				Vector2.one * (DrawSettings.DevPinStateDisplayRadius * 2 + DrawSettings.DevPinStateDisplayOutline * 2) * scale :
				(Vector2)GridHelper.GetStateGridDimension((int)bitCount) * DrawSettings.MultiBitPinStateDisplaySquareSize * scale + Vector2.one * DrawSettings.DevPinStateDisplayOutline * scale;
			
			// Calculate pin size
			float pinRadius = DrawSettings.PinRadius * scale;
			float pinWidth = DrawSettings.PinRadius * 2 * 0.95f * scale;
			float pinHeight = SubChipInstance.PinHeightFromBitCount(bitCount) * scale;
			Vector2 pinSize = new Vector2(pinWidth, pinHeight);
			
			// Calculate total width of DevPin elements (state display + gap + pin)
			float gap = 0.5f * scale; // Gap between state display and pin
			float totalWidth = stateGridSize.x + gap + pinSize.x;
			
			// Position state display to the left of center
			float stateDisplayOffset = (totalWidth - pinSize.x - gap) * 0.5f;
			Vector2 stateDisplayPos = centerPos - faceDir * stateDisplayOffset;
			
			// Position pin to the right of center
			float pinOffset = (totalWidth - stateGridSize.x - gap) * 0.5f;
			Vector2 pinPos = centerPos + faceDir * pinOffset;
			
			// Note: Connection line removed as it's not visible in the actual game
			
			// Draw state display based on bit count (exactly like game)
			if (bitCount == 1)
			{
				// Draw single circle for 1-bit pins (like Draw1BitDevPin)
				float radius = DrawSettings.DevPinStateDisplayRadius * scale;
				Color stateCol = Color.red; // Default state color for preview
				
				// Draw outline
				Seb.Vis.UI.UI.DrawPoint(stateDisplayPos, radius + DrawSettings.DevPinStateDisplayOutline * scale, Color.black);
				// Draw main circle
				Seb.Vis.UI.UI.DrawPoint(stateDisplayPos, radius, stateCol);
			}
			else
			{
				// Draw rectangle for multi-bit pins (like DrawMultiBitDevPin)
				Vector2Int gridDim = GridHelper.GetStateGridDimension((int)bitCount);
				float squareSize = DrawSettings.MultiBitPinStateDisplaySquareSize * scale;
				
				// Calculate grid size (same as DevPinInstance.StateGridSize)
				Vector2 gridSize = new Vector2(gridDim.x, gridDim.y) * squareSize + Vector2.one * (DrawSettings.DevPinStateDisplayOutline * scale);
				
				// Draw black background rectangle
				Seb.Vis.UI.UI.DrawPanel(stateDisplayPos, gridSize, Color.black, Anchor.Centre);
				
				// Draw individual squares within the rectangle
				Vector2 topLeft = new Vector2(stateDisplayPos.x - gridSize.x / 2, stateDisplayPos.y + gridSize.y / 2);
				const float squareDisplayScaleT = 0.9f;
				Vector2 squareDrawSize = Vector2.one * (squareSize * squareDisplayScaleT);
				
				int currBitIndex = (int)bitCount - 1;
				for (int y = 0; y < gridDim.y; y++)
				{
					for (int x = 0; x < gridDim.x; x++)
					{
						Vector2 pos = topLeft + squareSize * new Vector2(x + 0.5f, -(y + 0.5f));
						Color stateCol = Color.red; // Default state color for preview
						
						// Draw square
						Seb.Vis.UI.UI.DrawQuad(pos, squareDrawSize, stateCol, Anchor.Centre);
						currBitIndex--;
					}
				}
			}
			
			// Draw the pin at the calculated position (no pin body for DevPins)
			// Use the same logic as the game: circle for 1-bit, rectangle for multi-bit
			if (bitCount == 1)
			{
				Seb.Vis.UI.UI.DrawPoint(pinPos, pinRadius, Color.black);
			}
			else
			{
				// Multi-bit pin: draw rectangle like the game
				bool isHorizontal = false;
				Vector2 multiBitPinSize = isHorizontal ? new Vector2(pinHeight, pinWidth) : new Vector2(pinWidth, pinHeight);
				Seb.Vis.UI.UI.DrawQuad(pinPos, multiBitPinSize, Color.black, Anchor.Centre);
			}
			
			// Note: Handle removed from preview as it's overkill for a preview display
		}
		
		static Vector2Int CalculateMultiBitGridDimensions(uint bitCount)
		{
			// Calculate grid dimensions for multi-bit pins (similar to game logic)
			if (bitCount <= 4) return new Vector2Int(2, 2);
			if (bitCount <= 8) return new Vector2Int(4, 2);
			if (bitCount <= 16) return new Vector2Int(4, 4);
			if (bitCount <= 32) return new Vector2Int(8, 4);
			return new Vector2Int(8, 8); // Default for larger bit counts
		}

		static void DrawBuiltinChipDisplay(ChipType chipType, Vector2 displayPos, float displayScale)
		{
			// Draw different display types based on chip type
			if (chipType == ChipType.SevenSegmentDisplay)
			{
				// Draw 7-segment display (simplified - just show "8" for preview)
				UI_DrawSevenSegmentDisplay(displayPos, displayScale, 1, 1, 1, 1, 1, 1, 1); // All segments on = "8"
			}
			else if (chipType == ChipType.DisplayRGB)
			{
				// Draw RGB display (16x16 pixel grid with colors)
				UI_DrawRGBDisplay(displayPos, displayScale);
			}
			else if (chipType == ChipType.DisplayLED)
			{
				// Draw LED display (black background with colored LED)
				UI_DrawLEDDisplay(displayPos, displayScale);
			}
			else if (chipType == ChipType.DisplayDot)
			{
				// Draw DOT display (16x16 pixel grid)
				UI_DrawDotDisplay(displayPos, displayScale);
			}
			else if (chipType == ChipType.DisplayRGBTouch)
			{
				// Draw RGB Touch display (simplified - just show a colored square with touch indicator)
				Seb.Vis.UI.UI.DrawPanel(displayPos, Vector2.one * displayScale, Color.gray, Anchor.Centre);
				// Add a small indicator for touch capability
				Seb.Vis.UI.UI.DrawCircle(displayPos + Vector2.one * displayScale * 0.3f, displayScale * 0.1f, Color.yellow, Anchor.Centre);
			}
			else if (chipType == ChipType.Button)
			{
				// Draw button display (dark gray button)
				Seb.Vis.UI.UI.DrawPanel(displayPos, Vector2.one * displayScale, new Color(0.2f, 0.2f, 0.2f), Anchor.Centre);
				Seb.Vis.UI.UI.DrawText("BUTTON", FontType.JetbrainsMonoRegular, displayScale * 0.2f, displayPos, Anchor.Centre, Color.white);
			}
			else if (chipType == ChipType.Toggle)
			{
				// Draw toggle display (blue toggle switch)
				Seb.Vis.UI.UI.DrawPanel(displayPos, Vector2.one * displayScale, new Color(0.3f, 0.5f, 0.8f), Anchor.Centre);
				Seb.Vis.UI.UI.DrawText("TOGGLE", FontType.JetbrainsMonoRegular, displayScale * 0.2f, displayPos, Anchor.Centre, Color.white);
			}
			else if (chipType == ChipType.In_Pin)
			{
				// Draw input pin display (dark gray button-like)
				Seb.Vis.UI.UI.DrawPanel(displayPos, Vector2.one * displayScale, new Color(0.2f, 0.2f, 0.2f), Anchor.Centre);
				Seb.Vis.UI.UI.DrawText("IN", FontType.JetbrainsMonoRegular, displayScale * 0.3f, displayPos, Anchor.Centre, Color.white);
			}
			else if (chipType == ChipType.Out_Pin)
			{
				// Draw output pin display (dark gray button-like)
				Seb.Vis.UI.UI.DrawPanel(displayPos, Vector2.one * displayScale, new Color(0.2f, 0.2f, 0.2f), Anchor.Centre);
				Seb.Vis.UI.UI.DrawText("OUT", FontType.JetbrainsMonoRegular, displayScale * 0.3f, displayPos, Anchor.Centre, Color.white);
			}
			// Add other display types as needed
		}

		static void UI_DrawDotDisplay(Vector2 centre, float scale)
		{
			// Draw DOT display (16x16 pixel grid) - matches game implementation exactly
			const int pixelsPerRow = 16;
			const float borderFrac = 0.95f;
			const float pixelSizeT = 0.925f;
			
			// Draw background
			Seb.Vis.UI.UI.DrawPanel(centre, Vector2.one * scale, Color.black, Anchor.Centre);
			
			float size = scale * borderFrac;
			Vector2 bottomLeft = centre - Vector2.one * size / 2;
			float pixelSize = size / pixelsPerRow;
			Vector2 pixelDrawSize = Vector2.one * (pixelSize * pixelSizeT);
			
			// Use the same color as the game
			Color col = new Color(0.1f, 0.1f, 0.1f, 1f); // Matches ColHelper.MakeCol(0.1f)
			
			// Draw ALL pixels like the game does (not just active ones)
			for (int y = 0; y < 16; y++)
			{
				for (int x = 0; x < 16; x++)
				{
					// Create a simple pattern for preview (some pixels on)
					bool pixelOn = (x + y) % 3 == 0; // Simple pattern
					
					// Use white for active pixels, gray for inactive (like the game)
					Color pixelColor = pixelOn ? Color.white : col;
					
					Vector2 pos = bottomLeft + Vector2.one * pixelSize / 2 + Vector2.right * (pixelSize * x) + Vector2.up * (pixelSize * y);
					Draw.Point(pos, pixelDrawSize.x / 2, pixelColor);
				}
			}
		}

		static void UI_DrawRGBDisplay(Vector2 centre, float scale)
		{
			// Draw RGB display (16x16 pixel grid with colors) - simplified for preview
			const int pixelsPerRow = 16;
			const float borderFrac = 0.95f;
			const float pixelSizeT = 0.925f;
			
			// Draw background
			Seb.Vis.UI.UI.DrawPanel(centre, Vector2.one * scale, Color.black, Anchor.Centre);
			
			float size = scale * borderFrac;
			Vector2 bottomLeft = centre - Vector2.one * size / 2;
			float pixelSize = size / pixelsPerRow;
			Vector2 pixelDrawSize = Vector2.one * (pixelSize * pixelSizeT);
			
			// Draw a colorful pattern for preview
			for (int y = 0; y < 16; y++)
			{
				for (int x = 0; x < 16; x++)
				{
					// Create a colorful pattern for preview
					float r = (x / 15f);
					float g = (y / 15f);
					float b = ((x + y) / 30f);
					Color pixelColor = new Color(r, g, b, 1f);
					
					Vector2 pos = bottomLeft + Vector2.one * pixelSize / 2 + Vector2.right * (pixelSize * x) + Vector2.up * (pixelSize * y);
					Seb.Vis.UI.UI.DrawQuad(pos, pixelDrawSize, pixelColor, Anchor.Centre);
				}
			}
		}

		static void UI_DrawLEDDisplay(Vector2 centre, float scale)
		{
			// Draw LED display (black background with colored LED) - matches game implementation
			const float pixelSizeT = 0.975f;
			Vector2 pixelDrawSize = Vector2.one * (scale * pixelSizeT);
			
			// Draw black background
			Seb.Vis.UI.UI.DrawPanel(centre, Vector2.one * scale, Color.black, Anchor.Centre);
			// Draw colored LED (red for preview)
			Seb.Vis.UI.UI.DrawQuad(centre, pixelDrawSize, Color.red, Anchor.Centre);
		}

		static void UI_DrawSevenSegmentDisplay(Vector2 centre, float scale, int A, int B, int C, int D, int E, int F, int G)
		{
			// Match the exact game implementation for 7-segment display
			const float targetHeightAspect = 1.75f;
			const float segmentThicknessFac = 0.165f;
			const float segmentVerticalSpacingFac = 0.07f;
			const float displayInsetFac = 0.2f;

			float boundsWidth = scale;
			float boundsHeight = boundsWidth * targetHeightAspect;
			float segmentThickness = scale * segmentThicknessFac;

			// Width of horizontal segments
			float segmentWidth = boundsWidth - segmentThickness - scale * displayInsetFac;
			// Distance between the centres of the bottom-most and top-most segments
			float segmentRegionHeight = boundsHeight - segmentThickness - scale * displayInsetFac;
			// Height of the vertical segments
			float segmentHeight = segmentRegionHeight / 2 - scale * segmentVerticalSpacingFac;

			Vector2 segmentSizeVertical = new(segmentThickness, segmentHeight);
			Vector2 segmentSizeHorizontal = new(segmentWidth, segmentThickness);
			Vector2 offsetX = Vector2.right * segmentWidth / 2;
			Vector2 offsetY = Vector2.up * segmentRegionHeight / 4;

			// Use consistent red color for all segments in preview
			Color segmentColor = Color.red;

			// Draw bounds (black background) - match game exactly
			Vector2 boundsSize = new(boundsWidth, boundsHeight);
			Seb.Vis.UI.UI.DrawPanel(centre, boundsSize, Color.black, Anchor.Centre);

			// Draw segments in the exact same order and positions as the game
			// Draw horizontal segments
			if (G == 1) Seb.Vis.UI.UI.DrawDiamond(centre, segmentSizeHorizontal, segmentColor); // mid
			if (A == 1) Seb.Vis.UI.UI.DrawDiamond(centre + Vector2.up * segmentRegionHeight / 2, segmentSizeHorizontal, segmentColor); // top
			if (D == 1) Seb.Vis.UI.UI.DrawDiamond(centre - Vector2.up * segmentRegionHeight / 2, segmentSizeHorizontal, segmentColor); // bottom

			// Draw vertical segments
			if (F == 1) Seb.Vis.UI.UI.DrawDiamond(centre - offsetX + offsetY, segmentSizeVertical, segmentColor); // left top
			if (E == 1) Seb.Vis.UI.UI.DrawDiamond(centre - offsetX - offsetY, segmentSizeVertical, segmentColor); // left bottom
			if (B == 1) Seb.Vis.UI.UI.DrawDiamond(centre + offsetX + offsetY, segmentSizeVertical, segmentColor); // right top
                        if (C == 1) Seb.Vis.UI.UI.DrawDiamond(centre + offsetX - offsetY, segmentSizeVertical, segmentColor); // right bottom
		}

		static int DrawCollectionMovementButtons(string[] buttonNames, bool[] interactableStates, ref Vector2 topLeft, float width)
		{
			int buttonIndex = -1;
			
			// Row 1: JUMP IN, JUMP OUT
			interactableStates_move[0] = interactableStates[0];
			interactableStates_move[1] = interactableStates[1];
			int row1Index = DrawHorizontalButtonGroup(new[] { buttonNames[0], buttonNames[1] }, interactableStates_move, ref topLeft, width);
			if (row1Index == 0) buttonIndex = 0; // JUMP IN
			else if (row1Index == 1) buttonIndex = 1; // JUMP OUT
			
			// Row 2: JUMP UP, JUMP DOWN
			interactableStates_move[0] = interactableStates[2];
			interactableStates_move[1] = interactableStates[3];
			int row2Index = DrawHorizontalButtonGroup(new[] { buttonNames[2], buttonNames[3] }, interactableStates_move, ref topLeft, width);
			if (row2Index == 0) buttonIndex = 2; // JUMP UP
			else if (row2Index == 1) buttonIndex = 3; // JUMP DOWN
			
			// Row 3: MOVE UP, MOVE DOWN
			interactableStates_move[0] = interactableStates[4];
			interactableStates_move[1] = interactableStates[5];
			int row3Index = DrawHorizontalButtonGroup(new[] { buttonNames[4], buttonNames[5] }, interactableStates_move, ref topLeft, width);
			if (row3Index == 0) buttonIndex = 4; // MOVE UP
			else if (row3Index == 1) buttonIndex = 5; // MOVE DOWN
			
			return buttonIndex;
		}
    }
}
