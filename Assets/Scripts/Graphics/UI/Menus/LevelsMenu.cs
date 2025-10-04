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
		const float previewWindowHeight = 18f;

		// -------- ScrollView plumbing --------
		static readonly UIHandle ID_LevelsScrollbar = new("LevelsMenu_LevelsScrollbar");
		static readonly Seb.Vis.UI.UI.ScrollViewDrawElementFunc drawLevelPackEntry = DrawLevelPackEntry;
		static bool isScrolling;

		// -------- Data --------
		[Serializable] class DefsWrapper { public LevelDefinition[] levels; }
		[Serializable] public class LocalLevelPack { public LevelPackChapter[] chapters; }
		[Serializable] public class LevelPackChapter { public string chapterName; public List<LevelDefinition> levels; }

		class LevelPackEntry
		{
			public string name;
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
			_selectedLevelPackIndex = Mathf.Clamp(
				PlayerPrefs.GetInt(PlayerPrefsKey_LastIndex + "_Pack", _selectedLevelPackIndex),
				0, Mathf.Max(0, _levelPacks.Count - 1)
			);
			_selectedLevelIndex = Mathf.Clamp(
				PlayerPrefs.GetInt(PlayerPrefsKey_LastIndex + "_Level", _selectedLevelIndex),
				0, Mathf.Max(0, _levelPacks.Count > 0 && _selectedLevelPackIndex >= 0 ? _levelPacks[_selectedLevelPackIndex].levels.Count - 1 : 0)
			);
			
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

			bool packPressed = Seb.Vis.UI.UI.Button(displayName, activePackTheme, topLeft, new Vector2(width, 2), true, false, false, activePackTheme.buttonCols, Anchor.TopLeft, true, 1, isScrolling);
			if (packPressed)
			{
				_selectedLevelPackIndex = packIndex;
				_selectedLevelIndex = -1;
				// Clear the preview when selecting a pack without a specific level
				UpdateSelectedLevel();
				// If holding control, select without toggling
				if (!InputHelper.CtrlIsHeld) pack.isToggledOpen = !pack.isToggledOpen;
			}

			const float nestedInset = 1.75f;

			if (pack.isToggledOpen)
			{
				for (int levelIndex = 0; levelIndex < pack.levels.Count; levelIndex++)
				{
					var level = pack.levels[levelIndex];
					ButtonTheme activeLevelTheme = packIndex == _selectedLevelPackIndex && levelIndex == _selectedLevelIndex ? ActiveUITheme.ChipLibraryChipToggleOn : ActiveUITheme.ChipLibraryChipToggleOff;
					Vector2 levelLabelPos = new(topLeft.x + nestedInset, Seb.Vis.UI.UI.PrevBounds.Bottom - UILayoutHelper.DefaultSpacing);
					
					// Add green tickmark if level is completed
					string levelDisplayName = level.name;
					if (IsLevelCompleted(level.id))
					{
						levelDisplayName += " ✓";
					}
					
					bool levelPressed = Seb.Vis.UI.UI.Button(levelDisplayName, activeLevelTheme, levelLabelPos, new Vector2(width - nestedInset, 2), true, false, false, activeLevelTheme.buttonCols, Anchor.TopLeft, true, 1, isScrolling);
					if (levelPressed)
					{
						bool alreadySelected = _selectedLevelIndex == levelIndex && packHighlighted;

						if (alreadySelected) 
						{
							_selectedLevelIndex = -1;
						}
						else
						{
							_selectedLevelPackIndex = packIndex;
							_selectedLevelIndex = levelIndex;
							// Update the global level list for the selected level
							UpdateSelectedLevel();
						}
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
			
			// Check if we have a selected level
			if (_allLevels.Count > 0)
			{
				var selectedLevel = _allLevels[0]; // Always use index 0 since _allLevels only contains the current selection
				MenuHelper.DrawTopLeftAlignTextWithBackground(selectedLevel.description, previewTopLeft, new Vector2(previewWidth, previewHeight), Anchor.TopLeft, Color.white, previewBgCol, true, 1f, true);
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

		static void DrawLevelNameBanner(Bounds2D panelContentBounds, Vector2 previewBottomLeft)
		{
			Vector2 bannerPos = previewBottomLeft + Vector2.down * (1.5f); // Normal spacing
			Vector2 bannerSize = new(panelContentBounds.Width, 2f);

			// Default red banner color
			Color bannerCol = new Color(0.8f, 0.2f, 0.2f, 1f); // Red color
			Color textCol = Color.white;
			
			string bannerText = "No Level Selected";
			
			if (_allLevels.Count > 0)
			{
				var selectedLevel = _allLevels[0]; // Always use index 0 since _allLevels only contains the current selection
				bannerText = selectedLevel.name;
				
				// Change banner color to green if level is completed
				if (IsLevelCompleted(selectedLevel.id))
				{
					bannerCol = new Color(0.2f, 0.8f, 0.2f, 1f); // Green color
				}
			}
			
			MenuHelper.DrawCentredTextWithBackground(bannerText, bannerPos, bannerSize, Anchor.TopLeft, textCol, bannerCol);
		}

		static void DrawActionButtons(Bounds2D panelContentBounds)
		{
			// Position buttons to flow naturally after the banner
			//Vector2 buttonPos = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * 1.5f; // After preview + banner + spacing
			Vector2 buttonPos = panelContentBounds.CentreBottom + Vector2.up * (3 * 5f) ;
			Vector2 buttonSize = new(panelContentBounds.Width * 0.9f, 4f); // Double height, wider
			const float buttonSpacing = 1f; // Normal spacing between buttons

			bool canPlay = _allLevels.Count > 0;
			
			// Center buttons horizontally
			Vector2 centeredButtonPos = new(panelContentBounds.Centre.x, buttonPos.y);
			
			bool pressedPlay = Seb.Vis.UI.UI.Button("PLAY", ActiveUITheme.ButtonTheme, centeredButtonPos, buttonSize, canPlay, false, false, ActiveUITheme.ButtonTheme.buttonCols, Anchor.CentreTop);
			
			Vector2 leaderboardPos = centeredButtonPos + Vector2.down * (buttonSize.y + buttonSpacing);
			bool pressedLeaderboard = Seb.Vis.UI.UI.Button("LEADERBOARD", ActiveUITheme.ButtonTheme, leaderboardPos, buttonSize, canPlay, false, false, ActiveUITheme.ButtonTheme.buttonCols, Anchor.CentreTop);
			
			Vector2 exitPos = leaderboardPos + Vector2.down * (buttonSize.y + buttonSpacing);
			bool pressedExit = Seb.Vis.UI.UI.Button("EXIT", ActiveUITheme.ButtonTheme, exitPos, buttonSize, true, false, false, ActiveUITheme.ButtonTheme.buttonCols, Anchor.CentreTop);

			if (pressedPlay) PlaySelectedLevel();
			if (pressedLeaderboard) OpenLeaderboard();
			if (pressedExit) Close();
		}

		static ButtonTheme GetButtonTheme(bool isLevelPack, bool isSelected) =>
			isLevelPack
				? isSelected ? ActiveUITheme.ChipLibraryCollectionToggleOn : ActiveUITheme.ChipLibraryCollectionToggleOff
				: isSelected
					? ActiveUITheme.ChipLibraryChipToggleOn
					: ActiveUITheme.ChipLibraryChipToggleOff;

		static void UpdateSelectedLevel()
		{
			// Clear the all levels list and repopulate with the selected level
			_allLevels.Clear();
			
			if (_selectedLevelPackIndex >= 0 && _selectedLevelPackIndex < _levelPacks.Count && 
				_selectedLevelIndex >= 0 && _selectedLevelIndex < _levelPacks[_selectedLevelPackIndex].levels.Count)
			{
				var selectedLevelDef = _levelPacks[_selectedLevelPackIndex].levels[_selectedLevelIndex];
				_allLevels.Add(new LevelEntry
				{
					id = selectedLevelDef.id,
					name = selectedLevelDef.name,
					description = selectedLevelDef.description,
					def = selectedLevelDef
				});
			}
		}

		static void OpenLeaderboard()
		{
			// Get the current level ID for the leaderboard
			string levelId = GetCurrentLevelId();
			LeaderboardPopup.Open(levelId);
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
							levels = new List<LevelDefinition>(),
							isToggledOpen = false
						};
						
						foreach (var def in ch.levels)
						{
							if (def == null || string.IsNullOrEmpty(def.id)) continue;
							levelPack.levels.Add(def);
						}
						
						if (levelPack.levels.Count > 0)
						{
							_levelPacks.Add(levelPack);
							Debug.Log($"[LevelsMenu] Added level pack: {levelPack.name} with {levelPack.levels.Count} levels");
						}
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
		static void PlaySelectedLevel()
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
					
					runner.StartLevel(selectedLevelDef);
					Debug.Log($"[LevelsMenu] Started level: id={selectedLevelDef.id}, name={selectedLevelDef.name}, inputs={selectedLevelDef.inputCount}, outputs={selectedLevelDef.outputCount}, vectors={(selectedLevelDef.testVectors == null ? -1 : selectedLevelDef.testVectors.Length)}");
					Close();
				}
				else if (option == 2) // Continue without Saving
				{
					// Start new level without saving current progress
					runner.StartLevel(selectedLevelDef);
					Debug.Log($"[LevelsMenu] Started level: id={selectedLevelDef.id}, name={selectedLevelDef.name}, inputs={selectedLevelDef.inputCount}, outputs={selectedLevelDef.outputCount}, vectors={(selectedLevelDef.testVectors == null ? -1 : selectedLevelDef.testVectors.Length)}");
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
