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

        // UI constants (copied from LevelValidationPopup)
        const float ListWidthFrac = 0.72f;
        const float ListHeightFrac = 0.40f;  // Reduced to make room for button
        const float RowHeight = 6.0f;        // Increased row spacing
        const float OkBtnWidthFrac = 0.30f;
        const float OkBtnHeightMul = 1.5f;

        // UI handles (copied from LevelValidationPopup)
        static readonly UIHandle ID_LeaderboardPopup = new("LeaderboardPopup_Scrollbar");
        static readonly Seb.Vis.UI.UI.ScrollViewDrawElementFunc DrawRowFunc = DrawRow;
        static bool isDraggingScrollbar;

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
                Draw.ID titleBG = Seb.Vis.UI.UI.ReservePanel();

                // --- Title banner ---
                Vector2 titlePos = Seb.Vis.UI.UI.CentreTop + Vector2.down * 8f;
                string title = $"Leaderboard - {_levelId}";
                Color headerCol = ColHelper.MakeCol255(44, 92, 62);
                Seb.Vis.UI.UI.DrawText(title, ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular * 2f, titlePos, Anchor.TextCentre, headerCol);
                Seb.Vis.UI.UI.ModifyPanel(titleBG, Bounds2D.Grow(Seb.Vis.UI.UI.PrevBounds, 3f), Color.clear);

                // --- Status row ---
                {
                    string statusStr = _isLoading ? "Loading scores..." : 
                                     !string.IsNullOrEmpty(_errorMessage) ? $"Error: {_errorMessage}" :
                                     _scores.Count == 0 ? "No scores yet" : $"Top {_scores.Count} scores";
                    Color statusCol = _isLoading ? Color.white : 
                                     !string.IsNullOrEmpty(_errorMessage) ? Color.red :
                                     _scores.Count == 0 ? Color.gray : ColHelper.MakeCol255(245, 212, 67);

                    Vector2 statusPos = Seb.Vis.UI.UI.PrevBounds.CentreBottom + new Vector2(0f, -1.4f);
                    Seb.Vis.UI.UI.DrawText(statusStr, ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular, statusPos, Anchor.TextCentre, statusCol);
                }

                // --- Table header ---
                if (!_isLoading && string.IsNullOrEmpty(_errorMessage) && _scores.Count > 0)
                {
                    DrawTableHeader();
                }

                // --- Scrollable list of scores ---
                if (!_isLoading && string.IsNullOrEmpty(_errorMessage) && _scores.Count > 0)
                {
                    float listW = Seb.Vis.UI.UI.Width * ListWidthFrac;
                    float listH = Seb.Vis.UI.UI.Height * ListHeightFrac;
                    Vector2 listSize = new(listW, listH);

                    var theme = DrawSettings.ActiveUITheme;

                    ScrollBarState sv = Seb.Vis.UI.UI.DrawScrollView(
                        ID_LeaderboardPopup,
                        Seb.Vis.UI.UI.Centre + Vector2.down*2f,
                        listSize,
                        UILayoutHelper.DefaultSpacing,
                        Anchor.Centre,
                        theme.ScrollTheme,
                        DrawRowFunc,
                        _scores.Count
                    );
                    isDraggingScrollbar = sv.isDragging;
                }

                // --- Footer: View and Close buttons ---
                float buttonWidth = Seb.Vis.UI.UI.Width * OkBtnWidthFrac;
                float buttonHeight = ButtonHeight * OkBtnHeightMul;
                float buttonSpacing = 2f;
                float totalWidth = (buttonWidth * 2) + buttonSpacing;
                Vector2 buttonsStart = Seb.Vis.UI.UI.PrevBounds.CentreBottom + Vector2.left * totalWidth / 2f;

                // View button (left)
                Vector2 viewButtonPos = buttonsStart;
                bool viewPressed = Seb.Vis.UI.UI.Button(
                    "View",
                    MenuHelper.Theme.ButtonTheme,
                    viewButtonPos,
                    new Vector2(buttonWidth, buttonHeight),
                    _selectedIndex >= 0 && _selectedIndex < _scores.Count,
                    false,
                    false,
                    MenuHelper.Theme.ButtonTheme.buttonCols,
                    Anchor.TopLeft
                );

                // Close button (right)
                Vector2 closeButtonPos = buttonsStart + Vector2.right * (buttonWidth + buttonSpacing);
                bool closePressed = Seb.Vis.UI.UI.Button(
                    "Close",
                    MenuHelper.Theme.ButtonTheme,
                    closeButtonPos,
                    new Vector2(buttonWidth, buttonHeight),
                    true,
                    false,
                    false,
                    MenuHelper.Theme.ButtonTheme.buttonCols,
                    Anchor.TopLeft
                );

                if (viewPressed && _selectedIndex >= 0 && _selectedIndex < _scores.Count)
                {
                    ViewSelectedSolution();
                }

                if (closePressed)
                {
                    UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
                }
            }
        }

        static void DrawTableHeader()
        {
            // Calculate header position (above the scrollable list)
            float listW = Seb.Vis.UI.UI.Width * ListWidthFrac;
            Vector2 headerStart = Seb.Vis.UI.UI.Centre + Vector2.up * Seb.Vis.UI.UI.Height * ListHeightFrac * 0.6f ;
            
            // Header background
            Bounds2D headerBounds = new Bounds2D(
                headerStart + Vector2.left * (listW * 0.5f),
                headerStart + Vector2.right * (listW * 0.5f) + Vector2.down * RowHeight * 0.7f
            );
            Seb.Vis.UI.UI.DrawPanel(headerBounds, ColHelper.MakeCol255(50, 50, 50));
            
            // Header text positions (matching the row layout you fixed)
            Vector2 rankPos = headerBounds.Min + Vector2.right * 0.5f + Vector2.up * (headerBounds.Height * 0.3f);
            Vector2 scorePos = headerBounds.Min + Vector2.right * 8f + Vector2.up * (headerBounds.Height * 0.3f);
            Vector2 userPos = headerBounds.Min + Vector2.right * 20f + Vector2.up * (headerBounds.Height * 0.3f);
            Vector2 datePos = headerBounds.Min + Vector2.right * 55f + Vector2.up * (headerBounds.Height * 0.3f);
            
            // Draw header text
            Seb.Vis.UI.UI.DrawText("Rank", ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular, rankPos, Anchor.TopLeft, Color.white);
            Seb.Vis.UI.UI.DrawText("Score", ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular, scorePos, Anchor.TopLeft, Color.yellow);
            Seb.Vis.UI.UI.DrawText("User", ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular, userPos, Anchor.TopLeft, Color.cyan);
            Seb.Vis.UI.UI.DrawText("Date", ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular, datePos, Anchor.TopLeft, Color.gray);
        }

        static void DrawRow(Vector2 rowTopLeft, float width, int index, bool isLayoutPass)
        {
            if (index < 0 || index >= _scores.Count) return;

            var score = _scores[index];
            bool isSelected = index == _selectedIndex;
            
            // Row background (copied from LevelValidationPopup)
            Color rowCol = index % 2 == 0 ? ColHelper.MakeCol255(30, 30, 30) : ColHelper.MakeCol255(40, 40, 40);
            if (isSelected)
            {
                rowCol = ColHelper.MakeCol255(60, 100, 60); // Highlight selected row
            }
            Bounds2D rowBounds = new Bounds2D(rowTopLeft, rowTopLeft + Vector2.right * width + Vector2.down * RowHeight);
            Seb.Vis.UI.UI.DrawPanel(rowBounds, rowCol);

            // Score data
            string rankText = $"#{index + 1}";
            string scoreText = score.score.ToString();
            string userText = GetDisplayUserName(score);
            string dateText = score.submittedAtUtc.ToString("MM/dd HH:mm");

            // Position elements (copied from LevelValidationPopup pattern)
            Vector2 rankPos = rowBounds.Min + Vector2.right * 0.7f + Vector2.up * (rowBounds.Height * 0.5f);
            Vector2 scorePos = rowBounds.Min + Vector2.right * 9f + Vector2.up * (rowBounds.Height * 0.5f);
            Vector2 userPos = rowBounds.Min + Vector2.right * 19f + Vector2.up * (rowBounds.Height * 0.5f);
            Vector2 datePos = rowBounds.Min + Vector2.right * 50f + Vector2.up * (rowBounds.Height * 0.5f);

            // Draw text (copied from LevelValidationPopup pattern)
            Seb.Vis.UI.UI.DrawText(rankText, ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular, rankPos, Anchor.TopLeft, Color.white);
            Seb.Vis.UI.UI.DrawText(scoreText, ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular, scorePos, Anchor.TopLeft, Color.yellow);
            Seb.Vis.UI.UI.DrawText(userText, ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular, userPos, Anchor.TopLeft, Color.cyan);
            Seb.Vis.UI.UI.DrawText(dateText, ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular, datePos, Anchor.TopLeft, Color.gray);

            // Add invisible button for row selection
            if (!isLayoutPass)
            {
                bool rowPressed = Seb.Vis.UI.UI.Button(
                    "", // Empty text - we just want the click area
                    MenuHelper.Theme.ButtonTheme,
                    rowTopLeft,
                    new Vector2(width, RowHeight),
                    true,
                    false,
                    false,
                    MenuHelper.Theme.ButtonTheme.buttonCols,
                    Anchor.TopLeft,
                    ignoreInputs: isDraggingScrollbar
                );

                if (rowPressed)
                {
                    _selectedIndex = index;
                }
            }
        }


        static string GetDisplayUserName(ScoreEntry score)
        {
            // Prefer userName if available, otherwise fall back to truncated userId
            if (!string.IsNullOrEmpty(score.userName))
            {
                return TruncateUserName(score.userName);
            }
            return TruncateUserId(score.userId);
        }
        
        static string TruncateUserName(string userName)
        {
            if (string.IsNullOrEmpty(userName)) return "Anonymous";
            if (userName.Length <= 12) return userName;
            return userName.Substring(0, 12) + "...";
        }
        
        static string TruncateUserId(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return "anon";
            if (userId.Length <= 8) return userId;
            return userId.Substring(0, 8) + "...";
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
