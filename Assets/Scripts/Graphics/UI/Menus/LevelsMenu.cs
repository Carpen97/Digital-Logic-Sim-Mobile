using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Seb.Vis;
using Seb.Vis.UI;
using Seb.Types;
using Seb.Helpers;
using DLS.Levels; // LevelDefinition
using DLS.Game;
using DLS.Game.LevelsIntegration;   // UIDrawer / Project access
using static DLS.Graphics.DrawSettings;

namespace DLS.Graphics
{
	public static class LevelsMenu
	{
		// -------- Config --------
		const string LevelPackResourcePath = "levels";
		const string PlayerPrefsKey_LastIndex = "LevelsMenu.LastIdx";

		// Panel layout constants
		const float interPanelSpacing = 1.5f;
		const float menuOffsetY = 1.13f;
		const float levelsPanelWidthT = 0.45f;
		const float selectionPanelWidthT = 0.45f;
		const float previewWindowHeight = 22f;

		// -------- ScrollView plumbing --------
		static readonly UIHandle ID_LevelsScrollbar = new("LevelsMenu_LevelsScrollbar");
		static readonly UIHandle ID_PreviewScrollbar = new("LevelsMenu_PreviewScrollbar");
		static readonly Seb.Vis.UI.UI.ScrollViewDrawElementFunc drawLevelPackEntry = DrawLevelPackEntry;
		static readonly Seb.Vis.UI.UI.ScrollViewDrawContentFunc drawPreviewContent = DrawPreviewContent;
		static bool isScrolling;
		static bool disableHover; // Disable hover on touch devices to prevent phantom hover states

	// -------- Data --------
	[Serializable] class DefsWrapper { public LevelDefinition[] levels; }
	[Serializable] public class LocalLevelPack { public LevelPackChapter[] chapters; }
	[Serializable] public class LevelPackChapter { public string chapterId; public string chapterName; public string chapterDescription; public List<LevelDefinition> levels; }

	class LevelPackEntry
	{
		public string name;
		public string description;
		public List<LevelDefinition> levels;
		public bool isToggledOpen;
	}

		class LevelEntry
		{
			public string id;
			public string name;
			public string description;
			public LevelDefinition def;
		}

		static readonly List<LevelPackEntry> _levelPacks = new();
		static readonly List<LevelEntry> _allLevels = new();
		static int _selectedLevelPackIndex = -1;
		static int _selectedLevelIndex = -1;
		public static bool _loaded;

	// -------- Menu lifecycle hooks (UIDrawer) --------
	public static void OnMenuOpened()
	{
		LoadPack(); // always reload so Play Mode edits are reflected
		
		// Try to find the next incomplete level
		bool foundIncompleteLevel = false;
		
		for (int packIdx = 0; packIdx < _levelPacks.Count && !foundIncompleteLevel; packIdx++)
		{
			var pack = _levelPacks[packIdx];
			for (int levelIdx = 0; levelIdx < pack.levels.Count && !foundIncompleteLevel; levelIdx++)
			{
				var level = pack.levels[levelIdx];
				if (!IsLevelCompleted(level.id))
				{
					// Found first incomplete level - select it
					_selectedLevelPackIndex = packIdx;
					_selectedLevelIndex = levelIdx;
					pack.isToggledOpen = true; // Expand the chapter to show the selected level
					foundIncompleteLevel = true;
				}
			}
		}
		
		// If all levels are completed or no levels found, try to restore from PlayerPrefs
		if (!foundIncompleteLevel)
		{
			_selectedLevelPackIndex = Mathf.Clamp(
				PlayerPrefs.GetInt(PlayerPrefsKey_LastIndex + "_Pack", 0),
				0, Mathf.Max(0, _levelPacks.Count - 1)
			);
			_selectedLevelIndex = Mathf.Clamp(
				PlayerPrefs.GetInt(PlayerPrefsKey_LastIndex + "_Level", -1),
				-1, Mathf.Max(0, _levelPacks.Count > 0 && _selectedLevelPackIndex >= 0 ? _levelPacks[_selectedLevelPackIndex].levels.Count - 1 : 0)
			);
			
			// Expand the selected pack if a level is selected
			if (_selectedLevelPackIndex >= 0 && _selectedLevelPackIndex < _levelPacks.Count && _selectedLevelIndex >= 0)
			{
				_levelPacks[_selectedLevelPackIndex].isToggledOpen = true;
			}
		}
		
		// Update the selected level to populate _allLevels
		UpdateSelectedLevel();
	}

		// -------- Draw --------
		public static void DrawMenu()
		{
			if (KeyboardShortcuts.CancelShortcutTriggered) { Close(); return; }

			MenuHelper.DrawBackgroundOverlay();
			Vector2 panelEdgePadding = new(3.25f, 2.6f);

			float panelWidthSum = Seb.Vis.UI.UI.Width - interPanelSpacing - panelEdgePadding.x * 2;
			float panelHeight = Seb.Vis.UI.UI.Height - panelEdgePadding.y * 2;

			Vector2 panelATopLeft = Seb.Vis.UI.UI.TopLeft +Vector2.right * Seb.Vis.UI.UI.Width * (0.5f-levelsPanelWidthT) + new Vector2(panelEdgePadding.x, -panelEdgePadding.y + menuOffsetY);
			Vector2 panelSizeA = new(panelWidthSum * levelsPanelWidthT, panelHeight);
			Vector2 panelBTopLeft = panelATopLeft + Vector2.right * (panelSizeA.x + interPanelSpacing);
			Vector2 panelSizeB = new(panelWidthSum * selectionPanelWidthT, panelHeight);

			isScrolling = Seb.Vis.UI.UI.GetScrollbarState(ID_LevelsScrollbar).isDragging;
			
			// On touch devices, disable hover when not actively touching to prevent phantom hover states
			#if UNITY_ANDROID || UNITY_IOS
			disableHover = Input.touchCount == 0; // Disable hover when no fingers on screen
			#else
			disableHover = false; // Enable hover on desktop/PC
			#endif

			DrawLevelsPanel(panelATopLeft, panelSizeA);
			DrawSelectionPanel(panelBTopLeft, panelSizeB);
		}

		static void DrawLevelsPanel(Vector2 topLeft, Vector2 size)
		{
			Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();
			Bounds2D panelBounds = Bounds2D.CreateFromTopLeftAndSize(topLeft, size);
			DrawPanelHeader("LEVELS", topLeft, size.x);

			Bounds2D panelBoundsMinusHeader = Bounds2D.CreateFromTopLeftAndSize(Seb.Vis.UI.UI.PrevBounds.BottomLeft, new Vector2(size.x, size.y - Seb.Vis.UI.UI.PrevBounds.Height));
			Bounds2D panelContentBounds = Bounds2D.Shrink(panelBoundsMinusHeader, PanelUIPadding);

			Seb.Vis.UI.UI.DrawScrollView(ID_LevelsScrollbar, panelContentBounds.TopLeft, panelContentBounds.Size, UILayoutHelper.DefaultSpacing, Anchor.TopLeft, ActiveUITheme.ScrollTheme, drawLevelPackEntry, _levelPacks.Count);
			MenuHelper.DrawReservedMenuPanel(panelID, panelBounds, false);
		}

		static void DrawSelectionPanel(Vector2 topLeft, Vector2 size)
		{
			Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();
			Bounds2D panelBounds = Bounds2D.CreateFromTopLeftAndSize(topLeft, size);
			DrawPanelHeader("SELECTION", topLeft, size.x);

			Bounds2D panelBoundsMinusHeader = Bounds2D.CreateFromTopLeftAndSize(Seb.Vis.UI.UI.PrevBounds.BottomLeft, new Vector2(size.x, size.y - Seb.Vis.UI.UI.PrevBounds.Height));
			Bounds2D panelContentBounds = Bounds2D.Shrink(panelBoundsMinusHeader, PanelUIPadding);

			// Draw preview window first
			Vector2 bottomLeft = DrawLevelPreview(panelContentBounds);

			// Always draw level name banner (even when nothing selected)
			DrawLevelNameBanner(panelContentBounds, bottomLeft);

			// Draw action buttons
			DrawActionButtons(panelContentBounds);

			MenuHelper.DrawReservedMenuPanel(panelID, panelBounds, false);
		}

		static void DrawPanelHeader(string text, Vector2 topLeft, float width)
		{
			Color textCol = ColHelper.MakeCol("#3CD168");
			Color bgCol = ColHelper.MakeCol("#1D1D1D");
			MenuHelper.DrawLeftAlignTextWithBackground(text, topLeft, new Vector2(width, 2.3f), Anchor.TopLeft, textCol, bgCol, true);
		}

		static void DrawLevelPackEntry(Vector2 topLeft, float width, int packIndex, bool isLayoutPass)
		{
			if (packIndex < 0 || packIndex >= _levelPacks.Count) return;

			var pack = _levelPacks[packIndex];
			bool packHighlighted = packIndex == _selectedLevelPackIndex;
			ButtonTheme activePackTheme = GetButtonTheme(true, packHighlighted);

			// Add arrow indicator for expandable packs
			string displayName = pack.name;
			if (pack.levels.Count > 0)
			{
				displayName = (pack.isToggledOpen ? "▼ " : "▶ ") + pack.name;
			}

		bool packPressed = Seb.Vis.UI.UI.Button(displayName, activePackTheme, topLeft, new Vector2(width, 2), true, false, false, activePackTheme.buttonCols, Anchor.TopLeft, true, 1, isScrolling || disableHover);
		if (packPressed)
		{
			bool wasAlreadySelected = _selectedLevelPackIndex == packIndex;
			_selectedLevelPackIndex = packIndex;
			_selectedLevelIndex = -1; // Clear level selection when selecting a chapter
			
			// Update to show chapter description when clicking on chapter
			UpdateSelectedLevel();
			
			// Toggle open/closed state
			// If already selected and open, close it
			// If already selected and closed, open it
			// If newly selected, open it
			if (wasAlreadySelected)
			{
				pack.isToggledOpen = !pack.isToggledOpen;
			}
			else
			{
				pack.isToggledOpen = true;
			}
		}

			const float nestedInset = 1.75f;

			if (pack.isToggledOpen)
			{
				for (int levelIndex = 0; levelIndex < pack.levels.Count; levelIndex++)
				{
					var level = pack.levels[levelIndex];
					ButtonTheme activeLevelTheme = packIndex == _selectedLevelPackIndex && levelIndex == _selectedLevelIndex ? ActiveUITheme.ChipLibraryChipToggleOn : ActiveUITheme.ChipLibraryChipToggleOff;
					Vector2 levelLabelPos = new(topLeft.x + nestedInset, Seb.Vis.UI.UI.PrevBounds.Bottom - UILayoutHelper.DefaultSpacing);
					
					// Add green tickmark if level is completed, or dot if has progress
					string levelDisplayName = level.name;
					if (IsLevelCompleted(level.id))
					{
						levelDisplayName += " ✓";
					}
					else if (HasLevelProgress(level.id))
					{
						levelDisplayName += " ●";
					}
					
				bool levelPressed = Seb.Vis.UI.UI.Button(levelDisplayName, activeLevelTheme, levelLabelPos, new Vector2(width - nestedInset, 2), true, false, false, activeLevelTheme.buttonCols, Anchor.TopLeft, true, 1, isScrolling || disableHover);
				if (levelPressed)
				{
					bool alreadySelected = _selectedLevelIndex == levelIndex && packHighlighted;

					if (!alreadySelected) 
					{
						_selectedLevelPackIndex = packIndex;
						_selectedLevelIndex = levelIndex;
						// Update the global level list for the selected level
						UpdateSelectedLevel();
					}
					// If already selected, do nothing (keep it selected)
				}
				}
			}
		}

	static Vector2 DrawLevelPreview(Bounds2D panelContentBounds)
	{
		// Always draw preview window
		const float previewWidth = 39;
		const float previewHeight = previewWindowHeight;
		const float margin = 0.4f;
		// Position preview in top-right corner
		Vector2 previewTopLeft = panelContentBounds.TopRight + Vector2.left * previewWidth + Vector2.down * margin;

		// Draw preview background (always)
		Color previewBgCol = new Color(0.1f, 0.1f, 0.1f, 0.95f);

		Vector2 ret;
		
		// Check if we have a selected level or chapter
		if (_allLevels.Count > 0)
		{
			var selectedEntry = _allLevels[0]; // Always use index 0 since _allLevels only contains the current selection
			
			// Draw panel background
			Seb.Vis.UI.UI.DrawPanel(previewTopLeft, new Vector2(previewWidth, previewHeight), previewBgCol, Anchor.TopLeft);
			Bounds2D bgBounds = Seb.Vis.UI.UI.PrevBounds;
			
			// Create scrollable content area
			const float textPadX = 1.5f;
			const float textPadYTop = 2f; // More padding at the top
			Vector2 scrollViewPos = bgBounds.TopLeft + Vector2.right * textPadX + Vector2.down * textPadYTop;
			Vector2 scrollViewSize = new Vector2(previewWidth - textPadX * 2, previewHeight - textPadYTop * 2);
			
			// Draw scrollable text content
			Seb.Vis.UI.UI.DrawScrollView(ID_PreviewScrollbar, scrollViewPos, scrollViewSize, Anchor.TopLeft, ActiveUITheme.ScrollTheme, drawPreviewContent);
			
			Seb.Vis.UI.UI.OverridePreviousBounds(bgBounds);
			ret = Seb.Vis.UI.UI.PrevBounds.BottomLeft;
			return ret;
		}
		else
		{
			// No level selected - show empty preview
			MenuHelper.DrawLeftAlignTextWithBackground("", previewTopLeft, new Vector2(previewWidth, previewHeight), Anchor.TopLeft, Color.white, previewBgCol, true);
			ret = Seb.Vis.UI.UI.PrevBounds.BottomLeft;
			return ret;
		}
	}

	static void DrawPreviewContent(Vector2 topLeft, float width, bool isLayoutPass)
	{
		// Check if we have a selected level or chapter
		if (_allLevels.Count > 0)
		{
			var selectedEntry = _allLevels[0]; // Always use index 0 since _allLevels only contains the current selection
			
			if (!string.IsNullOrEmpty(selectedEntry.description))
			{
				float fontSize = ActiveUITheme.FontSizeRegular * 0.75f; // Smaller font for better fit
				
				// Calculate approximate characters per line based on panel width
				float charWidth = fontSize * 0.6f; // Approximate character width
				int maxCharsPerLine = Mathf.Max(1, Mathf.FloorToInt(width / charWidth));
				
				// Apply text wrapping
				string wrappedText = Seb.Vis.UI.UI.LineBreakByCharCount(selectedEntry.description, maxCharsPerLine);
				Seb.Vis.UI.UI.DrawText(wrappedText, ActiveUITheme.FontRegular, fontSize, topLeft, Anchor.TopLeft, Color.white);
			}
		}
	}

	static void DrawLevelNameBanner(Bounds2D panelContentBounds, Vector2 previewBottomLeft)
	{
		Vector2 bannerPos = previewBottomLeft + Vector2.down * (1.5f); // Normal spacing
		Vector2 bannerSize = new(panelContentBounds.Width, 4f); // Doubled height from 2f to 4f

		// Default red banner color
		Color bannerCol = new Color(0.8f, 0.2f, 0.2f, 1f); // Red color for "No Selection"
		Color textCol = Color.white;
		
		string bannerText = "No Level Selected";
		
		if (_allLevels.Count > 0)
		{
			var selectedEntry = _allLevels[0]; // Always use index 0 since _allLevels only contains the current selection
			bannerText = selectedEntry.name;
			
			// Check if this is a chapter-only selection (no level)
			bool isChapterOnly = selectedEntry.def == null && string.IsNullOrEmpty(selectedEntry.id);
			
			if (isChapterOnly)
			{
				// Chapter selected - use blue color
				bannerCol = new Color(0.2f, 0.5f, 0.9f, 1f); // Blue color for chapters
			}
			else if (IsLevelCompleted(selectedEntry.id))
			{
				// Level completed - use green color
				bannerCol = new Color(0.2f, 0.8f, 0.2f, 1f); // Green color
			}
			else
			{
				// Level selected but not completed - use yellow/orange color
				bannerCol = new Color(0.9f, 0.7f, 0.2f, 1f); // Yellow/orange color
			}
		}
		
		MenuHelper.DrawCentredTextWithBackground(bannerText, bannerPos, bannerSize, Anchor.TopLeft, textCol, bannerCol, true);
	}

	static void DrawActionButtons(Bounds2D panelContentBounds)
	{
		// Position buttons to flow naturally after the banner
		Vector2 buttonPos = panelContentBounds.CentreBottom + Vector2.up * (3 * 5f);
		Vector2 buttonSize = new(panelContentBounds.Width * 0.9f, 4f); // Double height, wider
		const float buttonSpacing = 1f; // Normal spacing between buttons

		// Check if we have a level selected (not just a chapter)
		bool hasLevelSelected = _allLevels.Count > 0 && _allLevels[0].def != null;
		bool canPlay = hasLevelSelected;
		bool hasProgress = canPlay && HasLevelProgress(_allLevels[0].id);
			
			// Center buttons horizontally
			Vector2 centeredButtonPos = new(panelContentBounds.Centre.x, buttonPos.y);
			
			// If level has progress, show Continue and Restart buttons
			if (hasProgress)
			{
				// Calculate positions for two buttons side by side
				float halfWidth = (buttonSize.x - buttonSpacing) / 2f;
				Vector2 continuePos = new(panelContentBounds.Centre.x - halfWidth / 2f - buttonSpacing / 2f, buttonPos.y);
				Vector2 restartPos = new(panelContentBounds.Centre.x + halfWidth / 2f + buttonSpacing / 2f, buttonPos.y);
				
				bool pressedContinue = Seb.Vis.UI.UI.Button("CONTINUE", ActiveUITheme.ButtonTheme, continuePos, new Vector2(halfWidth, buttonSize.y), canPlay, false, false, ActiveUITheme.ButtonTheme.buttonCols, Anchor.CentreTop);
				bool pressedRestart = Seb.Vis.UI.UI.Button("RESTART", ActiveUITheme.ButtonTheme, restartPos, new Vector2(halfWidth, buttonSize.y), canPlay, false, false, ActiveUITheme.ButtonTheme.buttonCols, Anchor.CentreTop);
				
				// LEADERBOARD and HALL OF FAME side by side in the middle
				Vector2 leaderboardPos = new(panelContentBounds.Centre.x - halfWidth / 2f - buttonSpacing / 2f, continuePos.y - buttonSize.y - buttonSpacing);
				Vector2 hallOfFamePos = new(panelContentBounds.Centre.x + halfWidth / 2f + buttonSpacing / 2f, continuePos.y - buttonSize.y - buttonSpacing);
				
				bool pressedLeaderboard = Seb.Vis.UI.UI.Button("LEADERBOARD", ActiveUITheme.ButtonTheme, leaderboardPos, new Vector2(halfWidth, buttonSize.y), canPlay, false, false, ActiveUITheme.ButtonTheme.buttonCols, Anchor.CentreTop);
				bool pressedHallOfFame = Seb.Vis.UI.UI.Button("HALL OF FAME", ActiveUITheme.ButtonTheme, hallOfFamePos, new Vector2(halfWidth, buttonSize.y), true, false, false, ActiveUITheme.ButtonTheme.buttonCols, Anchor.CentreTop);
				
				// EXIT button at the bottom
				Vector2 exitPos = new(panelContentBounds.Centre.x, leaderboardPos.y - buttonSize.y - buttonSpacing);
				bool pressedExit = Seb.Vis.UI.UI.Button("EXIT", ActiveUITheme.ButtonTheme, exitPos, buttonSize, true, false, false, ActiveUITheme.ButtonTheme.buttonCols, Anchor.CentreTop);

				if (pressedContinue) PlaySelectedLevel(continueFromSave: true);
				if (pressedRestart) PlaySelectedLevel(continueFromSave: false);
				if (pressedLeaderboard) OpenLeaderboard();
				if (pressedHallOfFame) OpenHallOfFame();
				if (pressedExit) Close();
			}
			else
			{
				// No progress, show normal Play button
				bool pressedPlay = Seb.Vis.UI.UI.Button("PLAY", ActiveUITheme.ButtonTheme, centeredButtonPos, buttonSize, canPlay, false, false, ActiveUITheme.ButtonTheme.buttonCols, Anchor.CentreTop);
				
				// LEADERBOARD and HALL OF FAME side by side in the middle
				float halfWidth = (buttonSize.x - buttonSpacing) / 2f;
				Vector2 leaderboardPos = new(panelContentBounds.Centre.x - halfWidth / 2f - buttonSpacing / 2f, centeredButtonPos.y - buttonSize.y - buttonSpacing);
				Vector2 hallOfFamePos = new(panelContentBounds.Centre.x + halfWidth / 2f + buttonSpacing / 2f, centeredButtonPos.y - buttonSize.y - buttonSpacing);
				
				bool pressedLeaderboard = Seb.Vis.UI.UI.Button("LEADERBOARD", ActiveUITheme.ButtonTheme, leaderboardPos, new Vector2(halfWidth, buttonSize.y), canPlay, false, false, ActiveUITheme.ButtonTheme.buttonCols, Anchor.CentreTop);
				bool pressedHallOfFame = Seb.Vis.UI.UI.Button("HALL OF FAME", ActiveUITheme.ButtonTheme, hallOfFamePos, new Vector2(halfWidth, buttonSize.y), true, false, false, ActiveUITheme.ButtonTheme.buttonCols, Anchor.CentreTop);
				
				// EXIT button at the bottom
				Vector2 exitPos = new(panelContentBounds.Centre.x, leaderboardPos.y - buttonSize.y - buttonSpacing);
				bool pressedExit = Seb.Vis.UI.UI.Button("EXIT", ActiveUITheme.ButtonTheme, exitPos, buttonSize, true, false, false, ActiveUITheme.ButtonTheme.buttonCols, Anchor.CentreTop);

				if (pressedPlay) PlaySelectedLevel(continueFromSave: false);
				if (pressedLeaderboard) OpenLeaderboard();
				if (pressedHallOfFame) OpenHallOfFame();
				if (pressedExit) Close();
			}
		}

		static ButtonTheme GetButtonTheme(bool isLevelPack, bool isSelected) =>
			isLevelPack
				? isSelected ? ActiveUITheme.ChipLibraryCollectionToggleOn : ActiveUITheme.ChipLibraryCollectionToggleOff
				: isSelected
					? ActiveUITheme.ChipLibraryChipToggleOn
					: ActiveUITheme.ChipLibraryChipToggleOff;

	static void UpdateSelectedLevel()
	{
		// Clear the all levels list and repopulate with the selected level OR chapter
		_allLevels.Clear();
		
		// Check if we have a specific level selected
		if (_selectedLevelPackIndex >= 0 && _selectedLevelPackIndex < _levelPacks.Count && 
			_selectedLevelIndex >= 0 && _selectedLevelIndex < _levelPacks[_selectedLevelPackIndex].levels.Count)
		{
			// Level is selected - add the level entry
			var selectedLevelDef = _levelPacks[_selectedLevelPackIndex].levels[_selectedLevelIndex];
			_allLevels.Add(new LevelEntry
			{
				id = selectedLevelDef.id,
				name = selectedLevelDef.name,
				description = selectedLevelDef.description,
				def = selectedLevelDef
			});
		}
		else if (_selectedLevelPackIndex >= 0 && _selectedLevelPackIndex < _levelPacks.Count)
		{
			// Only chapter is selected (no specific level) - add chapter entry with description
			var selectedPack = _levelPacks[_selectedLevelPackIndex];
			_allLevels.Add(new LevelEntry
			{
				id = "", // No level ID for chapter-only selection
				name = selectedPack.name,
				description = selectedPack.description,
				def = null // No level definition for chapter-only selection
			});
		}
	}

	static void OpenLeaderboard()
	{
		// Get the current level ID for the leaderboard
		string levelId = GetCurrentLevelId();
		LeaderboardPopup.Open(levelId);
	}
	
	static void OpenHallOfFame()
	{
		// Open the Hall of Fame menu
		HallOfFameMenu.Open();
	}
	
	static string GetCurrentLevelId()
		{
			// Get the current level ID from the selected level
			if (_allLevels.Count > 0)
			{
				var selectedLevel = _allLevels[0]; // Always use index 0 since _allLevels only contains the current selection
				
				// Use the actual level definition properties
				if (!string.IsNullOrEmpty(selectedLevel.name))
				{
					return selectedLevel.name;
				}
				
				if (!string.IsNullOrEmpty(selectedLevel.id))
				{
					return selectedLevel.id;
				}
			}
			
			// Fallback
			return "Unknown Level";
		}

		// -------- Load pack --------
		static void LoadPack()
		{
			_levelPacks.Clear();
			_allLevels.Clear();

			// Force Unity to reload the resource (clears cache)
			Resources.UnloadAsset(Resources.Load<TextAsset>(LevelPackResourcePath));
			var packAsset = Resources.Load<TextAsset>(LevelPackResourcePath);
			if (packAsset == null)
			{
				Debug.LogError($"[LevelsMenu] Could not find Resources/{LevelPackResourcePath}.json");
				_loaded = true;
				return;
			}

			// Try: LevelPack (with chapters)
			try
			{
				var pack = JsonUtility.FromJson<LocalLevelPack>(packAsset.text);
				if (pack != null && pack.chapters != null && pack.chapters.Length > 0)
				{
					foreach (var ch in pack.chapters)
					{
						if (ch?.levels == null) continue;
						
						var levelPack = new LevelPackEntry
						{
							name = ch.chapterName, // Use chapterName from JSON
							description = ch.chapterDescription ?? "", // Use chapterDescription from JSON
							levels = new List<LevelDefinition>(),
							isToggledOpen = false
						};
						
						foreach (var def in ch.levels)
						{
							if (def == null || string.IsNullOrEmpty(def.id)) continue;
							
							levelPack.levels.Add(def);
						}
						
						// Allow chapters with empty levels arrays (for placeholder chapters like "Coming Soon")
						_levelPacks.Add(levelPack);
						Debug.Log($"[LevelsMenu] Added level pack: {levelPack.name} with {levelPack.levels.Count} levels");
					}
					_loaded = true;
					return;
				}
			}
			catch (Exception e)
			{
				Debug.LogWarning($"[LevelsMenu] Pack parse (LocalLevelPack) failed, will try fallbacks. {e.Message}");
			}

			// Fallback: { "levels": [ LevelDefinition ] }
			try
			{
				var defsWrapper = JsonUtility.FromJson<DefsWrapper>(packAsset.text);
				if (defsWrapper?.levels != null && defsWrapper.levels.Length > 0)
				{
					var defaultPack = new LevelPackEntry
					{
						name = "All Levels",
						description = "",
						levels = new List<LevelDefinition>(),
						isToggledOpen = false
					};
					
					foreach (var def in defsWrapper.levels)
					{
						if (def == null || string.IsNullOrEmpty(def.id)) continue;
						defaultPack.levels.Add(def);
					}
					
					if (defaultPack.levels.Count > 0)
					{
						_levelPacks.Add(defaultPack);
						Debug.Log($"[LevelsMenu] Added default level pack (wrapper): {defaultPack.name} with {defaultPack.levels.Count} levels");
					}
					_loaded = true;
					return;
				}
			}
			catch (Exception e)
			{
				Debug.LogWarning($"[LevelsMenu] Pack parse (wrapper) failed. {e.Message}");
			}

			// Fallback: top-level array [ LevelDefinition ]
			try
			{
				var arr = JsonHelper.FromJson<LevelDefinition>(packAsset.text);
				if (arr != null && arr.Length > 0)
				{
					var defaultPack = new LevelPackEntry
					{
						name = "All Levels",
						description = "",
						levels = new List<LevelDefinition>(),
						isToggledOpen = false
					};
					
					foreach (var def in arr)
					{
						if (def == null || string.IsNullOrEmpty(def.id)) continue;
						defaultPack.levels.Add(def);
					}
					
					if (defaultPack.levels.Count > 0)
					{
						_levelPacks.Add(defaultPack);
						Debug.Log($"[LevelsMenu] Added default level pack (array): {defaultPack.name} with {defaultPack.levels.Count} levels");
					}
					_loaded = true;
					return;
				}
			}
			catch (Exception e)
			{
				Debug.LogError($"[LevelsMenu] Failed to parse levels pack in any supported format. {e.Message}");
			}

			_loaded = true;
		}

		// -------- Play flow --------
		static void PlaySelectedLevel(bool continueFromSave = false)
		{
			if (_selectedLevelPackIndex < 0 || _selectedLevelPackIndex >= _levelPacks.Count ||
				_selectedLevelIndex < 0 || _selectedLevelIndex >= _levelPacks[_selectedLevelPackIndex].levels.Count)
			{
				Debug.LogWarning("[LevelsMenu] No level selected to play.");
				return;
			}

			var selectedLevelDef = _levelPacks[_selectedLevelPackIndex].levels[_selectedLevelIndex];

			if (selectedLevelDef == null)
			{
				Debug.LogError("[LevelsMenu] Selected LevelDefinition is null.");
				return;
			}

			// Check for level unsaved changes before starting new level
			var runner = GetOrCreateLevelManager();
			Debug.Log($"[LevelsMenu] StartLevel: runner.IsActive={runner.IsActive}, HasUnsavedChanges={runner.HasUnsavedChanges()}");
			
			if (runner.IsActive && runner.HasUnsavedChanges())
			{
				Debug.Log("[LevelsMenu] StartLevel: Showing level unsaved changes popup");
				LevelUnsavedChangesPopup.OpenPopup(StartLevelAfterCheck);
			}
			else
			{
				Debug.Log("[LevelsMenu] StartLevel: No unsaved changes, proceeding directly");
				StartLevelAfterCheck(2); // Continue without saving (since there are no changes)
			}

			void StartLevelAfterCheck(int option)
			{
				if (option == 0) // Cancel
				{
					// Do nothing, stay in current level
					return;
				}
				else if (option == 1) // Save and Continue
				{
					// Save level progress before starting new level
					if (runner.IsActive)
					{
						runner.SaveCurrentProgress();
					}
					
					// Start level - if restarting, clear progress first
					if (!continueFromSave)
					{
						LevelProgressService.ClearLevelProgress(selectedLevelDef.id);
					}
					runner.StartLevel(selectedLevelDef);
					Debug.Log($"[LevelsMenu] Started level: id={selectedLevelDef.id}, name={selectedLevelDef.name}, continueFromSave={continueFromSave}");
					Close();
				}
				else if (option == 2) // Continue without Saving
				{
					// Start level - if restarting, clear progress first
					if (!continueFromSave)
					{
						LevelProgressService.ClearLevelProgress(selectedLevelDef.id);
					}
					runner.StartLevel(selectedLevelDef);
					Debug.Log($"[LevelsMenu] Started level: id={selectedLevelDef.id}, name={selectedLevelDef.name}, continueFromSave={continueFromSave}");
					Close();
				}
			}
		}

		static LevelManager GetOrCreateLevelManager()
		{
			var runner = UnityEngine.Object.FindFirstObjectByType<LevelManager>();
			if (runner != null) return runner;
			var go = new GameObject("LevelManager");
			UnityEngine.Object.DontDestroyOnLoad(go);
			return go.AddComponent<LevelManager>();
		}

		// -------- Utilities --------
		static void Close()
		{
			PlayerPrefs.SetInt(PlayerPrefsKey_LastIndex + "_Pack", _selectedLevelPackIndex);
			PlayerPrefs.SetInt(PlayerPrefsKey_LastIndex + "_Level", _selectedLevelIndex);
			PlayerPrefs.Save();
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
		}

		static void RememberSelection()
		{
			PlayerPrefs.SetInt(PlayerPrefsKey_LastIndex + "_Pack", _selectedLevelPackIndex);
			PlayerPrefs.SetInt(PlayerPrefsKey_LastIndex + "_Level", _selectedLevelIndex);
		}

	/// <summary>
	/// Check if a level has been completed using LevelProgressService
	/// </summary>
	static bool IsLevelCompleted(string levelId)
	{
		if (string.IsNullOrEmpty(levelId)) return false;
		
		try
		{
			var progress = LevelProgressService.Get(levelId);
			return progress.Completed;
		}
		catch (Exception ex)
		{
			Debug.LogWarning($"[LevelsMenu] Failed to check completion for level {levelId}: {ex.Message}");
			return false;
		}
	}

	/// <summary>
	/// Check if a level has saved progress (completed or has some work saved)
	/// </summary>
	static bool HasLevelProgress(string levelId)
	{
		if (string.IsNullOrEmpty(levelId)) return false;
		
		try
		{
			var progress = LevelProgressService.Get(levelId);
			// Has progress if it's completed OR has a saved state
			return progress.Completed || LevelProgressService.HasLevelProgress(levelId);
		}
		catch (Exception ex)
		{
			Debug.LogWarning($"[LevelsMenu] Failed to check progress for level {levelId}: {ex.Message}");
			return false;
		}
	}

		// Unity’s JsonUtility can’t parse top-level arrays; helper wraps/unwraps.
		static class JsonHelper
		{
			[Serializable] private class Wrapper<T> { public T[] Items; }
			public static T[] FromJson<T>(string json)
			{
				string wrapped = "{\"Items\":" + json + "}";
				return JsonUtility.FromJson<Wrapper<T>>(wrapped)?.Items;
			}
		}
	}
}
