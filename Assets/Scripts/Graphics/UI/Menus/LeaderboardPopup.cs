using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DLS.Online;
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

            using (UI.BeginBoundsScope(true))
            {
                Draw.ID panelBG = UI.ReservePanel();
                Draw.ID titleBG = UI.ReservePanel();

                // --- Title banner ---
                Vector2 titlePos = UI.CentreTop + Vector2.down * 8f;
                string title = $"Leaderboard - {_levelId}";
                Color headerCol = ColHelper.MakeCol255(44, 92, 62);
                UI.DrawText(title, ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular * 2f, titlePos, Anchor.TextCentre, headerCol);
                UI.ModifyPanel(titleBG, Bounds2D.Grow(UI.PrevBounds, 3f), Color.clear);

                // --- Status row ---
                {
                    string statusStr = _isLoading ? "Loading scores..." : 
                                     !string.IsNullOrEmpty(_errorMessage) ? $"Error: {_errorMessage}" :
                                     _scores.Count == 0 ? "No scores yet" : $"Top {_scores.Count} scores";
                    Color statusCol = _isLoading ? Color.white : 
                                     !string.IsNullOrEmpty(_errorMessage) ? Color.red :
                                     _scores.Count == 0 ? Color.gray : ColHelper.MakeCol255(245, 212, 67);

                    Vector2 statusPos = UI.PrevBounds.CentreBottom + new Vector2(0f, -1.4f);
                    UI.DrawText(statusStr, ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular, statusPos, Anchor.TextCentre, statusCol);
                }

                // --- Table header ---
                if (!_isLoading && string.IsNullOrEmpty(_errorMessage) && _scores.Count > 0)
                {
                    DrawTableHeader();
                }

                // --- Scrollable list of scores ---
                if (!_isLoading && string.IsNullOrEmpty(_errorMessage) && _scores.Count > 0)
                {
                    float listW = UI.Width * ListWidthFrac;
                    float listH = UI.Height * ListHeightFrac;
                    Vector2 listSize = new(listW, listH);

                    var theme = DrawSettings.ActiveUITheme;

                    ScrollBarState sv = UI.DrawScrollView(
                        ID_LeaderboardPopup,
                        UI.Centre + Vector2.down*2f,
                        listSize,
                        UILayoutHelper.DefaultSpacing,
                        Anchor.Centre,
                        theme.ScrollTheme,
                        DrawRowFunc,
                        _scores.Count
                    );
                    isDraggingScrollbar = sv.isDragging;
                }

                // --- Footer: Close button ---
                float closeWidth = UI.Width * OkBtnWidthFrac;
                float closeHeight = ButtonHeight * OkBtnHeightMul;
                Vector2 buttonsTopLeft = UI.PrevBounds.CentreBottom + Vector2.left * closeWidth / 2f;

                var res = MenuHelper.DrawOKButton(buttonsTopLeft, closeWidth, closeHeight, true);
                if (res == MenuHelper.CancelConfirmResult.Confirm) UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
            }
        }

        static void DrawTableHeader()
        {
            // Calculate header position (above the scrollable list)
            float listW = UI.Width * ListWidthFrac;
            Vector2 headerStart = UI.Centre + Vector2.up * UI.Height * ListHeightFrac * 0.6f ;
            
            // Header background
            Bounds2D headerBounds = new Bounds2D(
                headerStart + Vector2.left * (listW * 0.5f),
                headerStart + Vector2.right * (listW * 0.5f) + Vector2.down * RowHeight * 0.7f
            );
            UI.DrawPanel(headerBounds, ColHelper.MakeCol255(50, 50, 50));
            
            // Header text positions (matching the row layout you fixed)
            Vector2 rankPos = headerBounds.Min + Vector2.right * 0.5f + Vector2.up * (headerBounds.Height * 0.3f);
            Vector2 scorePos = headerBounds.Min + Vector2.right * 8f + Vector2.up * (headerBounds.Height * 0.3f);
            Vector2 userPos = headerBounds.Min + Vector2.right * 20f + Vector2.up * (headerBounds.Height * 0.3f);
            Vector2 datePos = headerBounds.Min + Vector2.right * 55f + Vector2.up * (headerBounds.Height * 0.3f);
            
            // Draw header text
            UI.DrawText("Rank", ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular, rankPos, Anchor.TopLeft, Color.white);
            UI.DrawText("Score", ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular, scorePos, Anchor.TopLeft, Color.yellow);
            UI.DrawText("User", ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular, userPos, Anchor.TopLeft, Color.cyan);
            UI.DrawText("Date", ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular, datePos, Anchor.TopLeft, Color.gray);
        }

        static void DrawRow(Vector2 rowTopLeft, float width, int index, bool isLayoutPass)
        {
            if (index < 0 || index >= _scores.Count) return;

            var score = _scores[index];
            
            // Row background (copied from LevelValidationPopup)
            Color rowCol = index % 2 == 0 ? ColHelper.MakeCol255(30, 30, 30) : ColHelper.MakeCol255(40, 40, 40);
            Bounds2D rowBounds = new Bounds2D(rowTopLeft, rowTopLeft + Vector2.right * width + Vector2.down * RowHeight);
            UI.DrawPanel(rowBounds, rowCol);

            // Score data
            string rankText = $"#{index + 1}";
            string scoreText = score.score.ToString();
            string userText = TruncateUserId(score.userId);
            string dateText = score.submittedAtUtc.ToString("MM/dd HH:mm");

            // Position elements (copied from LevelValidationPopup pattern)
            Vector2 rankPos = rowBounds.Min + Vector2.right * 0.7f + Vector2.up * (rowBounds.Height * 0.5f);
            Vector2 scorePos = rowBounds.Min + Vector2.right * 9f + Vector2.up * (rowBounds.Height * 0.5f);
            Vector2 userPos = rowBounds.Min + Vector2.right * 19f + Vector2.up * (rowBounds.Height * 0.5f);
            Vector2 datePos = rowBounds.Min + Vector2.right * 50f + Vector2.up * (rowBounds.Height * 0.5f);

            // Draw text (copied from LevelValidationPopup pattern)
            UI.DrawText(rankText, ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular, rankPos, Anchor.TopLeft, Color.white);
            UI.DrawText(scoreText, ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular, scorePos, Anchor.TopLeft, Color.yellow);
            UI.DrawText(userText, ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular, userPos, Anchor.TopLeft, Color.cyan);
            UI.DrawText(dateText, ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular, datePos, Anchor.TopLeft, Color.gray);
        }


        static string TruncateUserId(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return "anon";
            if (userId.Length <= 8) return userId;
            return userId.Substring(0, 8) + "...";
        }
    }
}
