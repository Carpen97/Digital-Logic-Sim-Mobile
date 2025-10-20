using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DLS.Online;
using DLS.Game;
using Seb.Helpers;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using static DLS.Graphics.DrawSettings;

namespace DLS.Graphics
{
    public static class LeaderboardPopup
    {
        // ---------- State ----------
        static string _levelId = "";
        static List<ScoreEntry> _scores = new();
        static bool _isLoading = false;
        static string _errorMessage = "";
        static int _selectedIndex = -1;

        // UI constants (inspired by PreferencesMenu)
        const float menuWidth = 70f; // Wider panel
        const float entrySpacing = 0.2f; // Like PreferencesMenu
        const float headerSpacing = 2.0f; // Large spacing like PreferencesMenu
        const float titleSpacing = 3.0f;
        const float sectionSpacing = 1.5f;
        const float buttonSpacing = 2.0f;
        const float RowHeight = 4.2f;

        //Column horizontal spacing
        const float rankOffset = 2f;
        const float scoreOffset = 10f;
        const float userOffset = 17f;
        const float dateOffset = 50f;

        // UI handles (copied from LevelValidationPopup)
        static readonly UIHandle ID_LeaderboardPopup = new("LeaderboardPopup_Scrollbar");
        static readonly Seb.Vis.UI.UI.ScrollViewDrawElementFunc DrawRowFunc = DrawRow;
        static bool isDraggingScrollbar;
        
        // Current position tracking (inspired by PreferencesMenu)
        static Vector2 currentPos;

        // ---------- Public API ----------
        public static void Open(string levelId)
        {
            _levelId = levelId;
            _scores.Clear();
            _isLoading = true;
            _errorMessage = "";
            
            // Load scores asynchronously
            _ = LoadScoresAsync();
            
            UIDrawer.SetActiveMenu(UIDrawer.MenuType.Leaderboard);
        }

        /// <summary>
        /// Reopen the leaderboard with cached data (preserves scores, selection, and state)
        /// </summary>
        public static void ReopenWithCachedData()
        {
            if (string.IsNullOrEmpty(_levelId))
            {
                Debug.LogWarning("[Leaderboard] Cannot reopen with cached data - no level ID");
                return;
            }
            
            // Keep everything - scores, selection, and state
            _isLoading = false; // Scores are already loaded
            
            UIDrawer.SetActiveMenu(UIDrawer.MenuType.Leaderboard);
            Debug.Log($"[Leaderboard] Reopened with cached data for level {_levelId} ({_scores.Count} scores, selection: {_selectedIndex})");
        }

        static async System.Threading.Tasks.Task LoadScoresAsync()
        {
            try
            {
                Debug.Log($"[Leaderboard] Loading scores for level {_levelId}");
                _scores = await LeaderboardService.GetTopScoresAsync(_levelId, 10);
                _isLoading = false;
                Debug.Log($"[Leaderboard] Loaded {_scores.Count} scores");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Leaderboard] Failed to load scores: {ex.Message}");
                _errorMessage = ex.Message;
                _isLoading = false;
            }
        }

        // ---------- UI Drawing ----------
        public static void DrawMenu()
        {
            // Dimmed backdrop (same as other popups)
            MenuHelper.DrawBackgroundOverlay();

            using (Seb.Vis.UI.UI.BeginBoundsScope(true))
            {
                Draw.ID panelBG = Seb.Vis.UI.UI.ReservePanel();
                
                // Initialize position tracking with generous margins like PreferencesMenu
                Vector2 topLeft = Seb.Vis.UI.UI.Centre + new Vector2(-menuWidth / 2, 22f);
                currentPos = topLeft;
                Color headerCol = new(0.46f, 1, 0.54f); // Green like PreferencesMenu
                Color labelCol = Color.white;

                // --- LEADERBOARD header (centered like other popups) ---
                string title = $"LEADERBOARD - {_levelId}";
                Seb.Vis.UI.UI.DrawText(title, ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular, Seb.Vis.UI.UI.Centre + Vector2.up * 22f, Anchor.TextCentre, headerCol);
                AddHeaderSpacing(); // Large spacing like PreferencesMenu

                // --- Status row (centered) ---
                {
                    string statusStr = _isLoading ? "Loading scores..." : 
                                     !string.IsNullOrEmpty(_errorMessage) ? $"Error: {_errorMessage}" :
                                     _scores.Count == 0 ? "No scores yet" : $"Top {_scores.Count} scores";
                    Color statusCol = _isLoading ? Color.white : 
                                     !string.IsNullOrEmpty(_errorMessage) ? Color.red :
                                     _scores.Count == 0 ? Color.gray : ColHelper.MakeCol255(245, 212, 67);

                    Seb.Vis.UI.UI.DrawText(statusStr, ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular, Seb.Vis.UI.UI.Centre + Vector2.up * 19f, Anchor.TextCentre, statusCol);
                    AddHeaderSpacing(); // Large spacing between status and content
                }

                // --- Table header and scores ---
                if (!_isLoading && string.IsNullOrEmpty(_errorMessage) && _scores.Count > 0)
                {

                    AddHeaderSpacing();
                    // Draw table header
                    DrawTableHeader();

                    // Draw scrollable list - match button width
                    float listW = menuWidth; // Full width to match buttons
                    float listH = 30f; // Generous height
                    Vector2 listSize = new(listW, listH);

                    ScrollBarState sv = Seb.Vis.UI.UI.DrawScrollView(
                        ID_LeaderboardPopup,
                        currentPos,
                        listSize,
                        UILayoutHelper.DefaultSpacing,
                        Anchor.TopLeft,
                        DrawSettings.ActiveUITheme.ScrollTheme,
                        DrawRowFunc,
                        _scores.Count
                    );
                    isDraggingScrollbar = sv.isDragging;
                    currentPos.y -= listH; // Update position
                }

                // --- Footer: View and Cancel buttons using HorizontalButtonGroup for proper styling ---
                Vector2 buttonTopLeft = new(currentPos.x, Seb.Vis.UI.UI.PrevBounds.Bottom - entrySpacing*5);
                float buttonRegionWidth = menuWidth;
                
                // Use custom button names for HorizontalButtonGroup
                bool hasPublicSolution = _selectedIndex >= 0 && _selectedIndex < _scores.Count && 
                                       !string.IsNullOrEmpty(_scores[_selectedIndex].completeSolutionId);
                string[] buttonNames = { hasPublicSolution ? "VIEW" : "Solution not public", "CANCEL" };
                bool[] buttonStates = { hasPublicSolution, true };
                
                int buttonIndex = Seb.Vis.UI.UI.HorizontalButtonGroup(
                    buttonNames,
                    buttonStates,
                    MenuHelper.Theme.ButtonTheme,
                    buttonTopLeft,
                    buttonRegionWidth,
                    DrawSettings.DefaultButtonSpacing,
                    0,
                    Anchor.TopLeft
                );
                
                // Handle button results
                if (buttonIndex == 1) // CANCEL
                {
                    UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
                }
                else if (buttonIndex == 0 && hasPublicSolution) // VIEW (only when solution is public)
                {
                    ViewSelectedSolution();
                }

                // --- Draw main panel background with proper border effect ---
                Bounds2D fullPanelBounds = Seb.Vis.UI.UI.GetCurrentBoundsScope();
                MenuHelper.DrawReservedMenuPanel(panelBG, fullPanelBounds);
            }
        }

        static void DrawTableHeader()
        {
            // Clean header like PreferencesMenu - no background, just text
            
            // Draw header text with original positioning
            Vector2 rankPos = currentPos + Vector2.right * rankOffset;
            Vector2 scorePos = currentPos + Vector2.right * scoreOffset;
            Vector2 userPos = currentPos + Vector2.right * userOffset;
            Vector2 datePos = currentPos + Vector2.right * dateOffset;
            
            // Draw header text (like setting labels in PreferencesMenu)
            Seb.Vis.UI.UI.DrawText("Rank", ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular, rankPos, Anchor.TextCentreLeft, Color.white);
            Seb.Vis.UI.UI.DrawText("Score", ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular, scorePos, Anchor.TextCentreLeft, Color.yellow);
            Seb.Vis.UI.UI.DrawText("User", ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular, userPos + Vector2.right*4, Anchor.TextCentreLeft, Color.cyan);
            Seb.Vis.UI.UI.DrawText("Date", ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular, datePos + Vector2.right*4, Anchor.TextCentreLeft, Color.white);
            
            // Update current position with spacing
            currentPos.y -= DrawSettings.ButtonHeight * 0.5f;
        }

        static void DrawRow(Vector2 rowTopLeft, float width, int index, bool isLayoutPass)
        {
            if (index < 0 || index >= _scores.Count) return;

            var score = _scores[index];
            bool isSelected = index == _selectedIndex;
            
            // Use different button themes for selection (like ChipLibraryMenu)
            ButtonTheme rowTheme = isSelected ? ActiveUITheme.ChipLibraryChipToggleOn : ActiveUITheme.ChipLibraryChipToggleOff;
            
            // Create invisible button for selection only (no text)
            bool rowPressed = Seb.Vis.UI.UI.Button(
                "", // Empty text - we just want the click area
                rowTheme,
                rowTopLeft,
                new Vector2(width, RowHeight * 0.8f), // Halved row height
                true,
                false,
                false,
                rowTheme.buttonCols,
                Anchor.TopLeft,
                ignoreInputs: isDraggingScrollbar
            );

            if (rowPressed)
            {
                _selectedIndex = index;
            }

            // Score data with original colors and positions
            string rankText = $"#{index + 1}";
            string scoreText = score.score.ToString();
            string userText = GetDisplayUserName(score);
            string dateText = FormatDate(score.submittedAtUtc);

            // Position elements (matching the header positions)
            Bounds2D rowBounds = new Bounds2D(rowTopLeft, rowTopLeft + Vector2.right * width + Vector2.down * (RowHeight * 0.5f));
            Vector2 rankPos = rowBounds.Min + Vector2.right * rankOffset + Vector2.up * (rowBounds.Height * 0.5f);
            Vector2 scorePos = rowBounds.Min + Vector2.right * scoreOffset + Vector2.up * (rowBounds.Height * 0.5f);
            #if UNITY_ANDROID || UNITY_IOS
            Vector2 userPos = rowBounds.Min + Vector2.right * (userOffset +  2f) + Vector2.up * (rowBounds.Height * 0.5f);
            #else
            Vector2 userPos = rowBounds.Min + Vector2.right * (userOffset +  2f) + Vector2.up * (rowBounds.Height * 0.5f);
            #endif


            Vector2 datePos = rowBounds.Min + Vector2.right * dateOffset + Vector2.up * (rowBounds.Height * 0.5f);

            // Draw text with original colors
            Seb.Vis.UI.UI.DrawText(rankText, ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular, rankPos, Anchor.TopLeft, Color.white);
            Seb.Vis.UI.UI.DrawText(scoreText, ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular, scorePos, Anchor.TopLeft, Color.yellow);
            Seb.Vis.UI.UI.DrawText(userText, ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular, userPos, Anchor.TopLeft, Color.cyan);
            Seb.Vis.UI.UI.DrawText(dateText, ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular, datePos, Anchor.TopLeft, Color.white);
        }


        static string GetDisplayUserName(ScoreEntry score)
        {
            // Prefer userName if available, otherwise fall back to truncated userId
            if (!string.IsNullOrEmpty(score.userName))
            {
                return TruncateUserName(score.userName);
            }
            return "Anonymous";
            //return TruncateUserId(score.userId);
        }
        
        static string TruncateUserName(string userName)
        {
            if (string.IsNullOrEmpty(userName)) return "Anonymous";
            if (userName.Length <= 36) return userName; // 3x larger (12 -> 36)
            return userName.Substring(0, 36) + "...";
        }
        
        static string TruncateUserId(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return "anon";
            if (userId.Length <= 24) return userId; // 3x larger (8 -> 24)
            return userId.Substring(0, 24) + "...";
        }

        static string FormatDate(DateTime dateTime)
        {
            // Format as MM/dd HH:mm but handle different dates properly
            return dateTime.ToString("MM/dd HH:mm");
        }
        
        // Spacing helper methods (inspired by PreferencesMenu)
        static void AddSpacing()
        {
            currentPos.y -= entrySpacing;
        }
        
        static void AddHeaderSpacing()
        {
            currentPos.y -= headerSpacing;
        }
        
        static void AddTitleSpacing()
        {
            currentPos.y -= titleSpacing;
        }
        
        static void AddSectionSpacing()
        {
            currentPos.y -= sectionSpacing;
        }

        static async void ViewSelectedSolution()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _scores.Count) return;

            var selectedScore = _scores[_selectedIndex];
            Debug.Log($"[Leaderboard] Viewing solution for score ID: {selectedScore.id}");
            Debug.Log($"[Leaderboard] Selected score completeSolutionId: '{selectedScore.completeSolutionId}'");
            Debug.Log($"[Leaderboard] Selected score userName: '{selectedScore.userName}'");
            Debug.Log($"[Leaderboard] Selected score score: {selectedScore.score}");

            try
            {
                // Check if this score has a complete solution
                if (string.IsNullOrEmpty(selectedScore.completeSolutionId))
                {
                    Debug.LogWarning("[Leaderboard] No complete solution available for this score");
                    return;
                }

                Debug.Log($"[Leaderboard] Getting complete solution {selectedScore.completeSolutionId}");
                
                // Try to load the complete solution from Firebase
                var completeSolution = await LeaderboardService.GetCompleteSolutionAsync(selectedScore.completeSolutionId);
                if (completeSolution == null)
                {
                    Debug.LogWarning("[Leaderboard] No complete solution found for this score");
                    return;
                }

                // Load the solution into the current project
                var project = Project.ActiveProject;
                if (project == null)
                {
                    Debug.LogError("[Leaderboard] No active project to load solution into");
                    return;
                }

                // Load the complete solution into the project
                bool success = SolutionSerializer.LoadCompleteSolution(completeSolution, project.chipLibrary);
                if (success)
                {
                    Debug.Log("[Leaderboard] Solution loaded successfully");
                    
                    // Set leaderboard viewing state to show enhanced viewing text
                    string displayUserName = GetDisplayUserName(selectedScore);
                    project.SetLeaderboardViewingState(displayUserName, _levelId);
                    
                    UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
                }
                else
                {
                    Debug.LogError("[Leaderboard] Failed to load solution into project");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Leaderboard] Error viewing solution: {ex.Message}");
            }
        }
    }
}
