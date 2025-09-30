using System;
using System.Collections.Generic;
using UnityEngine;
using Seb.Vis;
using Seb.Vis.UI;
using Seb.Types;
using Seb.Helpers;
using DLS.Levels; // LevelDefinition
using DLS.Game;
using DLS.Game.LevelsIntegration;   // UIDrawer / Project access

namespace DLS.Graphics
{
	public static class LevelsMenu
	{
		// -------- Config --------
		// Single pack file now:
		// Assets/Resources/levels.json (no extension in Resources.Load)
		const string LevelPackResourcePath = "levels";
		const string PlayerPrefsKey_LastIndex = "LevelsMenu.LastIdx";

		// --- Layout tuning (clean card) ---
		const float ListWidth = 80f;
		const float TitleOffsetY = -8f;
		const float TitleSizeBoost = 4f;
		const float ListHeight = 30f;
		const float GapTitleToList = 20.00f;
		const float GapListToBtns = 1.10f;
		const float BtnWFrac = 0.4f;
		const float BtnHMult = 0.95f;

		// -------- ScrollView plumbing --------
		static readonly UIHandle ID_LevelsScrollbar = new("LevelsMenu_Scrollbar");
		static readonly Seb.Vis.UI.UI.ScrollViewDrawElementFunc DrawLevelRowFunc = DrawLevelRow;
		static bool isDraggingScrollbar;

		// -------- Data --------
		[Serializable] class DefsWrapper { public LevelDefinition[] levels; }

		class LevelEntry
		{
			public string id;
			public string name;
			public string description;
			public LevelDefinition def;
		}

		static readonly List<LevelEntry> _levels = new();
		static int _selectedIndex;
		public static bool _loaded;

		// -------- Menu lifecycle hooks (UIDrawer) --------
		public static void OnMenuOpened()
		{
			LoadPack(); // always reload so Play Mode edits are reflected
			_selectedIndex = Mathf.Clamp(
				PlayerPrefs.GetInt(PlayerPrefsKey_LastIndex, _selectedIndex),
				0, Mathf.Max(0, _levels.Count - 1)
			);
		}

		// -------- Draw --------
		public static void DrawMenu()
		{
			if (KeyboardShortcuts.CancelShortcutTriggered) { Close(); return; }

			var theme = DrawSettings.ActiveUITheme;

			MenuHelper.DrawBackgroundOverlay();
			Draw.ID panelID = UI.ReservePanel();

			using (UI.BeginBoundsScope(true))
			{
				// ----- Title -----
				UI.DrawText(
					"LEVELS",
					theme.FontBold,
					theme.FontSizeRegular + TitleSizeBoost,
					UI.CentreTop + Vector2.up * TitleOffsetY,
					Anchor.TextCentre, Color.white
				);

				// Gap below title
				Vector2 topLeft = UI.TopLeft + Vector2.right * 8f + Vector2.down * (DrawSettings.DefaultButtonSpacing * GapTitleToList);

				// ----- List region (fixed size) -----
				Vector2 listSize = new(ListWidth, ListHeight);

				// Scroll view
				ScrollBarState sv = UI.DrawScrollView(
					ID_LevelsScrollbar,
					UI.Centre,
					listSize,
					UILayoutHelper.DefaultSpacing,
					Anchor.Centre,
					theme.ScrollTheme,
					DrawLevelRowFunc,
					_levels.Count
				);
				isDraggingScrollbar = sv.isDragging;

				// ----- Buttons row -----
				topLeft = UI.PrevBounds.BottomLeft + Vector2.down * (DrawSettings.DefaultButtonSpacing * GapListToBtns);

				Vector2 btnSize = new(ListWidth * BtnWFrac, DrawSettings.SelectorWheelHeight * BtnHMult);
				Vector2 playTopLeft = topLeft;
				Vector2 exitTopLeft = new(topLeft.x + btnSize.x + 8f, topLeft.y);

				bool canPlay = _levels.Count > 0;
				bool pressedPlay = UI.Button("PLAY", MenuHelper.Theme.ButtonTheme, playTopLeft, btnSize,
											 canPlay, true, false, MenuHelper.Theme.ButtonTheme.buttonCols, Anchor.TopLeft);
				bool pressedExit = UI.Button("EXIT", MenuHelper.Theme.ButtonTheme, exitTopLeft, btnSize,
											 true, true, false, MenuHelper.Theme.ButtonTheme.buttonCols, Anchor.TopLeft);

				Bounds2D bounds = UI.GetCurrentBoundsScope();
				MenuHelper.DrawReservedMenuPanel(panelID, bounds);

				if (pressedPlay) PlaySelectedLevel();
				if (pressedExit) Close();
			}
		}

		// Row renderer
		static void DrawLevelRow(Vector2 rowTopLeft, float width, int index, bool isLayoutPass)
		{
			if (_levels == null || _levels.Count == 0)
			{
				if (index == 0 && !isLayoutPass)
				{
					UI.Button("<no levels found>", MenuHelper.Theme.ButtonTheme,
						rowTopLeft, new Vector2(width, DrawSettings.SelectorWheelHeight),
						false, true, false, MenuHelper.Theme.ButtonTheme.buttonCols, Anchor.TopLeft, ignoreInputs: true);
				}
				return;
			}
			if (index < 0 || index >= _levels.Count) return;

			var l = _levels[index];
			bool selected = index == _selectedIndex;

			var progress = DLS.Levels.LevelProgressService.Get(l.id);

			const float nudgeLeft = -14f;
			const float FixedRowHeight = 4.2f;

			string name = l.name;
			int totalLength = 20;
			string centered = name.PadLeft(((totalLength - name.Length) / 2) + name.Length).PadRight(totalLength);
			string colorCode = progress.Completed ? "<color=#22aa22>" : "";
			centered = colorCode + centered;

			bool pressed = UI.Button(
				centered ?? $"Level {index + 1}",
				MenuHelper.Theme.ButtonTheme,
				rowTopLeft,
				new Vector2(width, FixedRowHeight),
				true,
				true,
				false,
				MenuHelper.Theme.ButtonTheme.buttonCols,
				Anchor.TopLeft,
				leftAlignText: false,
				textOffsetX: nudgeLeft,
				ignoreInputs: isDraggingScrollbar
			);

			if (!isLayoutPass && pressed)
			{
				_selectedIndex = index;
				RememberSelection();
			}
		}

		// -------- Load pack --------
		static void LoadPack()
		{
			_levels.Clear();

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
				var pack = JsonUtility.FromJson<DLS.Levels.LocalLevelPack>(packAsset.text);
				if (pack != null && pack.chapters != null && pack.chapters.Length > 0)
				{
					foreach (var ch in pack.chapters)
					{
						if (ch?.levels == null) continue;
						for (int i = 0; i < ch.levels.Count; i++)
						{
							var def = ch.levels[i];
							if (def == null || string.IsNullOrEmpty(def.id)) continue;
							_levels.Add(new LevelEntry
							{
								id = def.id,
								name = string.IsNullOrEmpty(def.name) ? def.id : def.name,
								description = def.description,
								def = def
							});
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
					foreach (var def in defsWrapper.levels)
					{
						if (def == null || string.IsNullOrEmpty(def.id)) continue;
						_levels.Add(new LevelEntry
						{
							id = def.id,
							name = string.IsNullOrEmpty(def.name) ? def.id : def.name,
							description = def.description,
							def = def
						});
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
					foreach (var def in arr)
					{
						if (def == null || string.IsNullOrEmpty(def.id)) continue;
						_levels.Add(new LevelEntry
						{
							id = def.id,
							name = string.IsNullOrEmpty(def.name) ? def.id : def.name,
							description = def.description,
							def = def
						});
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
			if (_levels.Count == 0)
			{
				Debug.LogWarning("[LevelsMenu] No levels to play.");
				return;
			}

			var entry = _levels[Mathf.Clamp(_selectedIndex, 0, _levels.Count - 1)];
			var def = entry.def;

			if (def == null)
			{
				Debug.LogError("[LevelsMenu] Selected LevelDefinition is null.");
				return;
			}

			var runner = GetOrCreateLevelManager();
			runner.StartLevel(def);

			Debug.Log($"[LevelsMenu] Started level: id={def.id}, name={def.name}, inputs={def.inputCount}, outputs={def.outputCount}, vectors={(def.testVectors == null ? -1 : def.testVectors.Length)}");

			Close();
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
			PlayerPrefs.Save();
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
		}

		static void RememberSelection()
		{
			PlayerPrefs.SetInt(PlayerPrefsKey_LastIndex, _selectedIndex);
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
