using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using DLS.Online;
using DLS.Levels;
using DLS.Game;
using Seb.Helpers;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using static DLS.Graphics.DrawSettings;

namespace DLS.Graphics
{
	/// <summary>
	/// Hall of Fame menu - shows aggregated leaderboard data across all levels.
	/// Displays top performers, personal stats, and celebrates achievements.
	/// </summary>
	public static class HallOfFameMenu
	{
		// ========== DATA STRUCTURES ==========
		
		/// <summary>
		/// Aggregated player statistics across all levels
		/// </summary>
		public class PlayerStats
		{
			public string userName;
			public int totalScore;          // Sum of all scores
			public float averageScore;      // Average score across levels
			public int levelsCompleted;     // Number of levels completed
			public int firstPlaces;         // Number of #1 positions
			public int topThreePlacements;  // Number of top 3 positions
			public Dictionary<string, int> levelScores; // levelId -> score
			public Dictionary<string, int> levelRankings; // levelId -> rank (1-based)
			
			public PlayerStats()
			{
				levelScores = new Dictionary<string, int>();
				levelRankings = new Dictionary<string, int>();
			}
		}
		
		/// <summary>
		/// Level champion information
		/// </summary>
		public class LevelChampion
		{
			public string levelId;
			public string levelName;
			public string userName;
			public int score;
			public DateTime submittedAt;
		}
		
		/// <summary>
		/// View mode for the Hall of Fame
		/// </summary>
		public enum ViewMode
		{
			TopPlayers,      // Show top players across all levels
			YourStats,       // Show current player's statistics
			LevelRecords     // Show champions for each level
		}
		
		// ========== STATE ==========
		
		static ViewMode _currentView = ViewMode.TopPlayers;
		static bool _isLoading = false;
		static string _errorMessage = "";
		static int _selectedIndex = -1;
		
		// Aggregated data
		static List<PlayerStats> _topPlayers = new();
		static PlayerStats _currentPlayerStats = null;
		static List<LevelChampion> _levelChampions = new();
		static Dictionary<string, List<ScoreEntry>> _allLevelScores = new(); // Cache of scores per level
		
		// Levels data
		static List<string> _allLevelIds = new();
		static Dictionary<string, string> _levelIdToName = new(); // levelId -> displayName
		
		// UI constants
		const float menuWidth = 75f;
		const float entrySpacing = 0.2f;
		const float headerSpacing = 2.0f;
		const float sectionSpacing = 1.5f;
		const float RowHeight = 3.0f;
		
		// UI handles
		static readonly UIHandle ID_HallOfFameScrollbar = new("HallOfFame_Scrollbar");
		static readonly UIHandle ID_LevelRecordsScrollbar = new("LevelRecords_Scrollbar");
		static readonly Seb.Vis.UI.UI.ScrollViewDrawElementFunc DrawRowFunc = DrawRow;
		static readonly Seb.Vis.UI.UI.ScrollViewDrawElementFunc DrawLevelRecordRowFunc = DrawLevelRecordRow;
		static bool isDraggingScrollbar;
		static Vector2 currentPos;
		
		// ========== PUBLIC API ==========
		
		public static void Open()
		{
			_currentView = ViewMode.TopPlayers;
			_isLoading = true;
			_errorMessage = "";
			_selectedIndex = -1;
			
			// Load all levels data
			LoadLevelsData();
			
			// Load Hall of Fame data asynchronously
			_ = LoadHallOfFameDataAsync();
			
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.HallOfFame);
		}
		
		public static void OnMenuOpened()
		{
			// Called by UIDrawer when menu is opened
			_currentView = ViewMode.TopPlayers;
			_isLoading = true;
			_errorMessage = "";
			_selectedIndex = -1;
			
			// Load all levels data
			LoadLevelsData();
			
			// Load Hall of Fame data asynchronously
			_ = LoadHallOfFameDataAsync();
		}
		
		public static void Close()
		{
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
		}
		
		// ========== DATA LOADING ==========
		
		/// <summary>
		/// Load level definitions from levels.json
		/// </summary>
		static void LoadLevelsData()
		{
			try
			{
				_allLevelIds.Clear();
				_levelIdToName.Clear();
				
				// Load levels.json
				TextAsset levelsAsset = Resources.Load<TextAsset>("levels");
				if (levelsAsset == null)
				{
					Debug.LogError("[HallOfFame] Failed to load levels.json");
					return;
				}
				
				// Parse the level pack structure
				var levelPack = JsonUtility.FromJson<LevelsMenu.LocalLevelPack>(levelsAsset.text);
				
				if (levelPack != null && levelPack.chapters != null)
				{
					foreach (var chapter in levelPack.chapters)
					{
						if (chapter.levels != null)
						{
							foreach (var level in chapter.levels)
							{
								_allLevelIds.Add(level.id);
								_levelIdToName[level.id] = level.name;
							}
						}
					}
				}
				
				Debug.Log($"[HallOfFame] Loaded {_allLevelIds.Count} levels");
			}
			catch (Exception ex)
			{
				Debug.LogError($"[HallOfFame] Failed to load levels data: {ex.Message}");
			}
		}
		
		/// <summary>
		/// Load and aggregate leaderboard data from all levels
		/// </summary>
		static async Task LoadHallOfFameDataAsync()
		{
			try
			{
				Debug.Log("[HallOfFame] Loading Hall of Fame data...");
				_isLoading = true;
				_errorMessage = "";
				
				// Clear previous data
				_topPlayers.Clear();
				_levelChampions.Clear();
				_allLevelScores.Clear();
				_currentPlayerStats = null;
				
			// Fetch scores for all levels (limit to top 10 per level to avoid overload)
			Dictionary<string, PlayerStats> playerStatsMap = new();
			
			// Create a snapshot of level IDs to avoid collection modification errors
			var levelIdsSnapshot = _allLevelIds.ToList();
			
			// Load scores for each level
			foreach (string levelId in levelIdsSnapshot)
				{
					try
					{
						// Use the display name (level.name) instead of internal ID (level.id) for Firebase queries
						string levelDisplayName = GetLevelDisplayName(levelId);
						Debug.Log($"[HallOfFame] Loading scores for level {levelId} (display name: {levelDisplayName})");
						var scores = await LeaderboardService.GetTopScoresAsync(levelDisplayName, 10);
						_allLevelScores[levelId] = scores;
						
						// Aggregate player statistics
						for (int rank = 0; rank < scores.Count; rank++)
						{
							var score = scores[rank];
							string playerName = GetPlayerName(score);
							
							if (!playerStatsMap.ContainsKey(playerName))
							{
								playerStatsMap[playerName] = new PlayerStats
								{
									userName = playerName
								};
							}
							
							var stats = playerStatsMap[playerName];
							
							// Update stats
							if (!stats.levelScores.ContainsKey(levelId))
							{
								stats.levelScores[levelId] = score.score;
								stats.levelRankings[levelId] = rank + 1; // 1-based ranking
								stats.levelsCompleted++;
								stats.totalScore += score.score;
								
								// Track placements
								if (rank == 0) stats.firstPlaces++;
								if (rank < 3) stats.topThreePlacements++;
							}
							else
							{
								// Keep best score if player appears multiple times
								if (score.score < stats.levelScores[levelId])
								{
									int oldScore = stats.levelScores[levelId];
									stats.totalScore = stats.totalScore - oldScore + score.score;
									stats.levelScores[levelId] = score.score;
									stats.levelRankings[levelId] = rank + 1;
								}
							}
						}
						
						// Track level champion (rank 1) - only if not already added
						if (scores.Count > 0 && !_levelChampions.Any(c => c.levelId == levelId))
						{
							var champion = scores[0];
							_levelChampions.Add(new LevelChampion
							{
								levelId = levelId,
								levelName = GetLevelName(levelId),
								userName = GetPlayerName(champion),
								score = champion.score,
								submittedAt = champion.submittedAtUtc
							});
						}
					}
					catch (Exception ex)
					{
						Debug.LogWarning($"[HallOfFame] Failed to load scores for level {levelId}: {ex.Message}");
					}
				}
				
			// Calculate average scores
			var statsSnapshot = playerStatsMap.Values.ToList();
			foreach (var stats in statsSnapshot)
				{
					if (stats.levelsCompleted > 0)
					{
						stats.averageScore = (float)stats.totalScore / stats.levelsCompleted;
					}
				}
				
				// Sort players by multiple criteria:
				// 1. Most levels completed (descending)
				// 2. Lowest total score (ascending)
				_topPlayers = playerStatsMap.Values
					.OrderByDescending(p => p.levelsCompleted)
					.ThenBy(p => p.totalScore)
					.Take(20)
					.ToList();
				
				// Find current player's stats
				string currentPlayerName = GetCurrentPlayerName();
				if (!string.IsNullOrEmpty(currentPlayerName) && playerStatsMap.ContainsKey(currentPlayerName))
				{
					_currentPlayerStats = playerStatsMap[currentPlayerName];
				}
				
				Debug.Log($"[HallOfFame] Loaded data: {_topPlayers.Count} top players, {_levelChampions.Count} champions");
				_isLoading = false;
			}
			catch (Exception ex)
			{
				Debug.LogError($"[HallOfFame] Failed to load Hall of Fame data: {ex.Message}");
				_errorMessage = $"Failed to load: {ex.Message}";
				_isLoading = false;
			}
		}
		
		// ========== UI DRAWING ==========
		
		public static void DrawMenu()
		{
			// Handle keyboard shortcuts
			if (KeyboardShortcuts.CancelShortcutTriggered)
			{
				Close();
				return;
			}
			
			// Dimmed backdrop
			MenuHelper.DrawBackgroundOverlay();
			
			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				Draw.ID panelBG = Seb.Vis.UI.UI.ReservePanel();
				
				// Initialize position
				Vector2 topLeft = Seb.Vis.UI.UI.Centre + new Vector2(-menuWidth / 2, 25f);
				currentPos = topLeft;
				
				// --- HALL OF FAME Header ---
				Color headerCol = ColHelper.MakeCol("#FFD700"); // Gold color
				Seb.Vis.UI.UI.DrawText("HALL OF FAME", ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular * 1.2f, 
					Seb.Vis.UI.UI.Centre + Vector2.up * 25f, Anchor.TextCentre, headerCol);
				AddHeaderSpacing();
				
				// --- View Mode Tabs ---
				DrawViewModeTabs();
				AddHeaderSpacing();
				
				// --- Status ---
				if (_isLoading)
				{
					Seb.Vis.UI.UI.DrawText("Loading Hall of Fame data...", ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular,
						Seb.Vis.UI.UI.Centre + Vector2.up * 15f, Anchor.TextCentre, Color.white);
				}
				else if (!string.IsNullOrEmpty(_errorMessage))
				{
					Seb.Vis.UI.UI.DrawText($"Error: {_errorMessage}", ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular,
						Seb.Vis.UI.UI.Centre + Vector2.up * 15f, Anchor.TextCentre, Color.red);
				}
				else
				{
					// Draw content based on view mode
					DrawCurrentView();
				}
				
				// --- Close Button ---
				DrawCloseButton();
				
				// --- Draw panel background ---
				Bounds2D fullPanelBounds = Seb.Vis.UI.UI.GetCurrentBoundsScope();
				MenuHelper.DrawReservedMenuPanel(panelBG, fullPanelBounds);
			}
		}
		
		static void DrawViewModeTabs()
		{
			string[] tabNames = { "TOP PLAYERS", "YOUR STATS", "LEVEL RECORDS" };
			bool[] tabStates = { true, true, true };
			
			float tabRegionWidth = menuWidth * 0.9f;
			Vector2 tabTopLeft = Seb.Vis.UI.UI.Centre + Vector2.up * 21f + Vector2.left * (tabRegionWidth / 2);
			
			// Highlight current tab by disabling it (makes it look selected)
			tabStates[(int)_currentView] = false;
			
			int selectedTab = Seb.Vis.UI.UI.HorizontalButtonGroup(
				tabNames,
				tabStates,
				MenuHelper.Theme.ButtonTheme,
				tabTopLeft,
				tabRegionWidth,
				DrawSettings.DefaultButtonSpacing,
				0,
				Anchor.TopLeft
			);
			
			if (selectedTab >= 0)
			{
				_currentView = (ViewMode)selectedTab;
				_selectedIndex = -1; // Reset selection when changing views
			}
			
			// Update position
			currentPos.y = tabTopLeft.y - 2.5f;
		}
		
		static void DrawCurrentView()
		{
			switch (_currentView)
			{
				case ViewMode.TopPlayers:
					DrawTopPlayersView();
					break;
				case ViewMode.YourStats:
					DrawYourStatsView();
					break;
				case ViewMode.LevelRecords:
					DrawLevelRecordsView();
					break;
			}
		}
		
		static void DrawTopPlayersView()
		{
			if (_topPlayers.Count == 0)
			{
				Seb.Vis.UI.UI.DrawText("No players yet! Be the first to complete levels.", 
					ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular,
					currentPos + Vector2.right * (menuWidth / 2) + Vector2.down * 5f, Anchor.TextCentre, Color.gray);
				return;
			}
			
			// Draw table header
			Vector2 headerPos = currentPos + Vector2.down * 0.5f;
			Seb.Vis.UI.UI.DrawText("Rank", ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular, 
				headerPos + Vector2.right * 2f, Anchor.TextCentreLeft, Color.white);
			Seb.Vis.UI.UI.DrawText("Player", ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular,
				headerPos + Vector2.right * 10f, Anchor.TextCentreLeft, Color.cyan);
			Seb.Vis.UI.UI.DrawText("Levels", ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular,
				headerPos + Vector2.right * 35f, Anchor.TextCentreLeft, Color.white);
			Seb.Vis.UI.UI.DrawText("Total Score", ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular,
				headerPos + Vector2.right * 50f, Anchor.TextCentreLeft, Color.yellow);
			
			currentPos.y -= 2.5f;
			
			// Draw scrollable list
			float listW = menuWidth;
			float listH = 25f;
			Vector2 listSize = new(listW, listH);
			
			ScrollBarState sv = Seb.Vis.UI.UI.DrawScrollView(
				ID_HallOfFameScrollbar,
				currentPos,
				listSize,
				UILayoutHelper.DefaultSpacing,
				Anchor.TopLeft,
				ActiveUITheme.ScrollTheme,
				DrawRowFunc,
				_topPlayers.Count
			);
			isDraggingScrollbar = sv.isDragging;
			currentPos.y -= listH;
		}
		
		static void DrawYourStatsView()
		{
			if (_currentPlayerStats == null)
			{
				Seb.Vis.UI.UI.DrawText("No stats yet! Complete some levels to see your statistics.", 
					ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular,
					currentPos + Vector2.right * (menuWidth / 2), Anchor.TextCentre, Color.gray);
				return;
			}
			
			var stats = _currentPlayerStats;
			currentPos.y -= 1f;
			
			// Player name
			Color goldCol = ColHelper.MakeCol("#FFD700");
			Seb.Vis.UI.UI.DrawText($"Player: {stats.userName}", ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular,
				currentPos + Vector2.right * 2f, Anchor.TopLeft, goldCol);
			currentPos.y -= 2.5f;
			
			// Statistics
			DrawStatLine("First Place Finishes:", $"{stats.firstPlaces}", goldCol);
			DrawStatLine("Top 3 Placements:", $"{stats.topThreePlacements}", ColHelper.MakeCol("#C0C0C0"));
			DrawStatLine("Levels Completed:", $"{stats.levelsCompleted} / {_allLevelIds.Count}", Color.cyan);
			DrawStatLine("Total Score:", $"{stats.totalScore}", Color.yellow);
			
		}
		
		static void DrawLevelRecordsView()
		{
			if (_levelChampions.Count == 0)
			{
				Seb.Vis.UI.UI.DrawText("No records yet! Complete levels to set records.", 
					ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular,
					currentPos + Vector2.right * (menuWidth / 2) + Vector2.down * 5f, Anchor.TextCentre, Color.gray);
				return;
			}
			
			// Draw table header
			Vector2 headerPos = currentPos + Vector2.down * 0.5f;
			Seb.Vis.UI.UI.DrawText("Level", ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular,
				headerPos + Vector2.right * 2f, Anchor.TextCentreLeft, Color.white);
			Seb.Vis.UI.UI.DrawText("Champion", ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular,
				headerPos + Vector2.right * 30f, Anchor.TextCentreLeft, Color.cyan);
			Seb.Vis.UI.UI.DrawText("Score", ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular,
				headerPos + Vector2.right * 58f, Anchor.TextCentreLeft, Color.yellow);
			
			currentPos.y -= 2.5f;
			
			// Draw scrollable list
			float listW = menuWidth;
			float listH = 25f;
			Vector2 listSize = new(listW, listH);
			
			ScrollBarState sv = Seb.Vis.UI.UI.DrawScrollView(
				ID_LevelRecordsScrollbar,
				currentPos,
				listSize,
				UILayoutHelper.DefaultSpacing,
				Anchor.TopLeft,
				ActiveUITheme.ScrollTheme,
				DrawLevelRecordRowFunc,
				_levelChampions.Count
			);
			isDraggingScrollbar = sv.isDragging;
			currentPos.y -= listH;
		}
		
		static void DrawRow(Vector2 rowTopLeft, float width, int index, bool isLayoutPass)
		{
			if (index < 0 || index >= _topPlayers.Count) return;
			
			var player = _topPlayers[index];
			bool isSelected = index == _selectedIndex;
			
			// Calculate shared rank and background color
			var (rankNumber, shouldShowRank, bgColor) = CalculateRankAndBackground(index);
			
			// Use button theme for selection, but override background color
			ButtonTheme rowTheme = isSelected ? ActiveUITheme.ChipLibraryChipToggleOn : ActiveUITheme.ChipLibraryChipToggleOff;
			
			// Create custom button theme with alternating background
			ButtonTheme customTheme = new ButtonTheme
			{
				buttonCols = new ButtonTheme.StateCols(bgColor, bgColor, bgColor, bgColor), // Use our alternating color
				font = rowTheme.font,
				fontSize = rowTheme.fontSize,
				textCols = rowTheme.textCols
			};
			
			// Button for selection with custom background
			bool rowPressed = Seb.Vis.UI.UI.Button(
				"",
				customTheme,
				rowTopLeft,
				new Vector2(width, RowHeight),
				true,
				false,
				false,
				customTheme.buttonCols,
				Anchor.TopLeft,
				ignoreInputs: isDraggingScrollbar
			);
			
			if (rowPressed)
			{
				_selectedIndex = index;
			}
			
			// Row data
			Bounds2D rowBounds = new Bounds2D(rowTopLeft, rowTopLeft + Vector2.right * width + Vector2.down * RowHeight);
			Vector2 center = new Vector2(rowBounds.Min.x, rowBounds.Centre.y);
			
			// Rank with medal for top 3 (only show if shouldShowRank is true)
			string rankText = "";
			if (shouldShowRank)
			{
				rankText = $"#{rankNumber}";
			}
			
			Color rankCol = rankNumber == 1 ? ColHelper.MakeCol("#FFD700") : 
							rankNumber == 2 ? ColHelper.MakeCol("#C0C0C0") :
							rankNumber == 3 ? ColHelper.MakeCol("#CD7F32") : Color.white;
			
			if (shouldShowRank)
			{
				Seb.Vis.UI.UI.DrawText(rankText, ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular,
					center + Vector2.right * 2f, Anchor.CentreLeft, rankCol);
			}
			
			// Player name
			Seb.Vis.UI.UI.DrawText(TruncateString(player.userName, 25), ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular,
				center + Vector2.right * 10f, Anchor.CentreLeft, Color.cyan);
			
			// Levels completed
			Seb.Vis.UI.UI.DrawText($"{player.levelsCompleted}", ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular,
				center + Vector2.right * 35f, Anchor.CentreLeft, Color.white);
			
			// Total score
			Seb.Vis.UI.UI.DrawText($"{player.totalScore}", ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular,
				center + Vector2.right * 50f, Anchor.CentreLeft, Color.yellow);
		}
		
		static void DrawLevelRecordRow(Vector2 rowTopLeft, float width, int index, bool isLayoutPass)
		{
			if (index < 0 || index >= _levelChampions.Count) return;
			
			var champion = _levelChampions[index];
			
			// Calculate alternating background color
			Color bgColor = (index % 2 == 0) ? 
				new Color(0.15f, 0.15f, 0.15f, 0.8f) : // Dark grey
				new Color(0.25f, 0.25f, 0.25f, 0.8f);  // Light grey
			
			// Draw background panel
			Seb.Vis.UI.UI.DrawPanel(rowTopLeft, new Vector2(width, RowHeight), bgColor, Anchor.TopLeft);
			
			// Row data
			Bounds2D rowBounds = new Bounds2D(rowTopLeft, rowTopLeft + Vector2.right * width + Vector2.down * RowHeight);
			Vector2 center = new Vector2(rowBounds.Min.x, rowBounds.Centre.y);
			
			// Level name (no emoji)
			Seb.Vis.UI.UI.DrawText(TruncateString(champion.levelName, 20), ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular,
				center + Vector2.right * 2f, Anchor.CentreLeft, Color.white);

			// Champion name
			Color goldCol = Color.cyan;
			Seb.Vis.UI.UI.DrawText(TruncateString(champion.userName, 20), ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular,
				center + Vector2.right * 30f, Anchor.CentreLeft, goldCol);
			
			// Score
			Seb.Vis.UI.UI.DrawText($"{champion.score}", ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular,
				center + Vector2.right * 58f, Anchor.CentreLeft, Color.yellow);
		}
		
		static void DrawStatLine(string label, string value, Color valueColor)
		{
			Seb.Vis.UI.UI.DrawText(label, ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular,
				currentPos + Vector2.right * 3f, Anchor.TopLeft, Color.white);
			Seb.Vis.UI.UI.DrawText(value, ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular,
				currentPos + Vector2.right * 40f, Anchor.TopLeft, valueColor);
			currentPos.y -= 2f;
		}
		
		static void DrawCloseButton()
		{
			Vector2 buttonTopLeft = new(currentPos.x + menuWidth * 0.35f, currentPos.y - 3f);
			float buttonWidth = menuWidth * 0.3f;
			
			if (Seb.Vis.UI.UI.Button(
				"CLOSE",
				MenuHelper.Theme.ButtonTheme,
				buttonTopLeft,
				new Vector2(buttonWidth, DrawSettings.ButtonHeight),
				true, false, false,
				MenuHelper.Theme.ButtonTheme.buttonCols,
				Anchor.TopLeft))
			{
				Close();
			}
		}
		
		// ========== HELPER METHODS ==========
		
		static string GetPlayerName(ScoreEntry score)
		{
			return string.IsNullOrEmpty(score.userName) ? "Anonymous" : score.userName;
		}
		
		static string GetLevelDisplayName(string levelId)
		{
			return _levelIdToName.TryGetValue(levelId, out string displayName) ? displayName : levelId;
		}
		
		static (int rankNumber, bool shouldShowRank, Color bgColor) CalculateRankAndBackground(int index)
		{
			if (index >= _topPlayers.Count) return (0, false, Color.gray);
			
			var currentPlayer = _topPlayers[index];
			int rankNumber = index + 1;
			bool shouldShowRank = true;
			
			// Check if this player has the same score as the previous player
			if (index > 0)
			{
				var previousPlayer = _topPlayers[index - 1];
				if (currentPlayer.levelsCompleted == previousPlayer.levelsCompleted && 
					currentPlayer.totalScore == previousPlayer.totalScore)
				{
					// Same score as previous player - don't show rank number
					shouldShowRank = false;
					rankNumber = index; // Use the same rank as previous player
				}
			}
			
			// Calculate alternating background color
			// Count how many unique score groups we've seen
			int uniqueScoreGroups = 0;
			for (int i = 0; i <= index; i++)
			{
				if (i == 0 || (_topPlayers[i].levelsCompleted != _topPlayers[i-1].levelsCompleted || 
					_topPlayers[i].totalScore != _topPlayers[i-1].totalScore))
				{
					uniqueScoreGroups++;
				}
			}
			
			// Alternate background based on unique score groups
			Color bgColor = (uniqueScoreGroups % 2 == 1) ? 
				new Color(0.15f, 0.15f, 0.15f, 0.8f) : // Dark grey
				new Color(0.25f, 0.25f, 0.25f, 0.8f);  // Light grey
			
			return (rankNumber, shouldShowRank, bgColor);
		}
		
		static string GetCurrentPlayerName()
		{
			// Get the current user's display name
			var project = Project.ActiveProject;
			if (project != null && !string.IsNullOrEmpty(project.description.Prefs_UserName))
			{
				return project.description.Prefs_UserName;
			}
			return "";
		}
		
		static string GetLevelName(string levelId)
		{
			return _levelIdToName.TryGetValue(levelId, out string name) ? name : levelId;
		}
		
		static string TruncateString(string text, int maxLength)
		{
			if (string.IsNullOrEmpty(text)) return "";
			if (text.Length <= maxLength) return text;
			return text.Substring(0, maxLength - 3) + "...";
		}
		
		static void AddHeaderSpacing()
		{
			currentPos.y -= headerSpacing;
		}
		
		static void AddSectionSpacing()
		{
			currentPos.y -= sectionSpacing;
		}
	}
}

