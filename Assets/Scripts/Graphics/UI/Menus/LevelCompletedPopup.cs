using System;
using UnityEngine;
using Seb.Helpers;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using DLS.Game.LevelsIntegration;
using static DLS.Graphics.DrawSettings;

namespace DLS.Graphics.UI
{
    public static class LevelCompletedPopup
    {
        // ---------- Popup state ----------
        static bool _isOpen = false;
        static System.Action _onOkCallback;

        // ---------- Public API ----------
        public static void Open(System.Action onOkCallback = null)
        {
            Debug.Log("[LevelCompletedPopup] Open called");
            _isOpen = true;
            _onOkCallback = onOkCallback;
            UIDrawer.SetActiveMenu(UIDrawer.MenuType.LevelCompleted);
        }

        public static void Close()
        {
            _isOpen = false;
            _onOkCallback = null;
            UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
        }

        public static void DrawMenu()
        {
            if (!_isOpen) return;

            // Dimmed backdrop (same as other popups)
            MenuHelper.DrawBackgroundOverlay();

            using (Seb.Vis.UI.UI.BeginBoundsScope(true))
            {
                Seb.Vis.Draw.ID panelBG = Seb.Vis.UI.UI.ReservePanel();

                // --- Title banner ---
                Vector2 titlePos = Seb.Vis.UI.UI.CentreTop + Vector2.down * 8f;
                Color headerCol = ColHelper.MakeCol255(44, 92, 62); // Green color for success
                Seb.Vis.UI.UI.DrawText("Level Completed!", ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular * 2f, titlePos, Anchor.TextCentre, headerCol);

                // --- OK Button ---
                Vector2 buttonPos = Seb.Vis.UI.UI.Centre + Vector2.down * 2f;
                Vector2 buttonSize = new Vector2(DrawSettings.ButtonHeight * 3f, DrawSettings.ButtonHeight * 1.5f);
                
                bool okPressed = Seb.Vis.UI.UI.Button(
                    "OK",
                    MenuHelper.Theme.ButtonTheme,
                    buttonPos,
                    buttonSize,
                    true,
                    false,
                    false,
                    MenuHelper.Theme.ButtonTheme.buttonCols,
                    Anchor.Centre
                );

                if (okPressed)
                {
                    _onOkCallback?.Invoke();
                    Close();
                }

                // Panel BG spanning everything drawn in this scope
                MenuHelper.DrawReservedMenuPanel(panelBG, Seb.Vis.UI.UI.GetCurrentBoundsScope());
            }
        }
    }
}
