using System;
using UnityEngine;
using Seb.Helpers;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using DLS.Game;
using static DLS.Graphics.DrawSettings;

namespace DLS.Graphics
{
    /// <summary>
    /// Popup for entering user name when uploading scores to Firebase leaderboard.
    /// Mobile-optimized with validation and preference storage.
    /// </summary>
    public static class UserNameInputPopup
    {
        // ---------- State ----------
        static string _userName = "";
        static bool _rememberName = false;
        static bool _uploadAsAnonymous = false;
        static string _validationMessage = "";
        
        // UI constants
        const float InputFieldHeight = 4f;
        
        // UI handles
        static readonly UIHandle ID_UserNameInput = new("UserNameInput_Field");
        static readonly UIHandle ID_RememberNameCheckbox = new("UserNameInput_Remember");
        static readonly UIHandle ID_AnonymousCheckbox = new("UserNameInput_Anonymous");
        
        // Callback for when user confirms
        static Action<string, bool> _onConfirm;
        static Action _onCancel;
        
        // ---------- Public API ----------
        public static void Open(Action<string, bool> onConfirm, Action onCancel = null)
        {
            _onConfirm = onConfirm;
            _onCancel = onCancel;
            
            // Load saved user name if available
            LoadSavedUserName();
            
            // Reset state
            _validationMessage = "";
            _uploadAsAnonymous = false;
            
            UIDrawer.SetActiveMenu(UIDrawer.MenuType.UserNameInput);
        }
        
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
                Color headerCol = ColHelper.MakeCol255(44, 92, 62);
                UI.DrawText("Enter Your Name", ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular * 2f, titlePos, Anchor.TextCentre, headerCol);
                UI.ModifyPanel(titleBG, Bounds2D.Grow(UI.PrevBounds, 3f), Color.clear);
                
                // --- Subtitle ---
                Vector2 subtitlePos = UI.PrevBounds.CentreBottom + new Vector2(0f, -1.4f);
                Color subtitleCol = ColHelper.MakeCol255(200, 200, 200);
                UI.DrawText("Your name will appear on the leaderboard", ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular, subtitlePos, Anchor.TextCentre, subtitleCol);
                
                // --- Input field ---
                DrawInputField();
                
                // --- Checkboxes ---
                DrawCheckboxes();
                
                // --- Validation message ---
                DrawValidationMessage();
                
                // --- Buttons ---
                DrawButtons();
                
                // Panel background
                MenuHelper.DrawReservedMenuPanel(panelBG, UI.GetCurrentBoundsScope());
            }
        }
        
        // ---------- UI Drawing ----------
        static void DrawInputField()
        {
            Vector2 inputPos = UI.PrevBounds.CentreBottom + new Vector2(0f, -3f);
            Vector2 inputSize = new Vector2(UI.Width * 0.6f, InputFieldHeight);
            
            // Input field
            var inputState = UI.GetInputFieldState(ID_UserNameInput);
            if (_uploadAsAnonymous)
            {
                inputState.SetText("Anonymous", false);
            }
            else if (string.IsNullOrEmpty(inputState.text) && !string.IsNullOrEmpty(_userName))
            {
                inputState.SetText(_userName, false);
            }
            
            // Draw input field
            var inputTheme = MenuHelper.Theme.ChipNameInputField;
            inputTheme.fontSize = ActiveUITheme.FontSizeRegular;
            
            UI.InputField(
                ID_UserNameInput,
                inputTheme,
                inputPos,
                inputSize,
                "Enter your name...",
                Anchor.Centre,
                1f,
                ValidateUserName,
                false
            );
            
            // Disable input when anonymous is selected
            if (_uploadAsAnonymous)
            {
                UI.DrawPanel(inputPos, inputSize, new Color(0, 0, 0, 0.3f), Anchor.Centre);
            }
        }
        
        static void DrawCheckboxes()
        {
            Vector2 checkboxStart = UI.PrevBounds.CentreBottom + new Vector2(0f, -6f);
            float checkboxSize = 2.5f;
            float checkboxSpacing = 1.5f;
            
            // Remember name checkbox
            Vector2 rememberPos = checkboxStart;
            bool rememberPressed = UI.Button(
                _rememberName ? "[X] Remember my name" : "[ ] Remember my name",
                MenuHelper.Theme.ButtonTheme,
                rememberPos,
                new Vector2(UI.Width * 0.5f, checkboxSize),
                true,
                false,
                false,
                MenuHelper.Theme.ButtonTheme.buttonCols,
                Anchor.CentreTop
            );
            
            if (rememberPressed)
            {
                _rememberName = !_rememberName;
            }
            
            // Anonymous checkbox
            Vector2 anonymousPos = rememberPos + Vector2.down * (checkboxSize + checkboxSpacing);
            bool anonymousPressed = UI.Button(
                _uploadAsAnonymous ? "[X] Upload as Anonymous" : "[ ] Upload as Anonymous",
                MenuHelper.Theme.ButtonTheme,
                anonymousPos,
                new Vector2(UI.Width * 0.5f, checkboxSize),
                true,
                false,
                false,
                MenuHelper.Theme.ButtonTheme.buttonCols,
                Anchor.CentreTop
            );
            
            if (anonymousPressed)
            {
                _uploadAsAnonymous = !_uploadAsAnonymous;
                if (_uploadAsAnonymous)
                {
                    _rememberName = false; // Can't remember anonymous
                }
            }
        }
        
        static void DrawValidationMessage()
        {
            if (string.IsNullOrEmpty(_validationMessage)) return;
            
            Vector2 messagePos = UI.PrevBounds.CentreBottom + new Vector2(0f, -1.4f);
            Color messageCol = _validationMessage.Contains("✓") ? ColHelper.MakeCol255(100, 255, 100) : ColHelper.MakeCol255(255, 100, 100);
            
            UI.DrawText(_validationMessage, ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular, messagePos, Anchor.TextCentre, messageCol);
        }
        
        static void DrawButtons()
        {
            Vector2 buttonStart = UI.PrevBounds.CentreBottom + new Vector2(-UI.Width*0.3f, -2f);
            
            // Use the same button pattern as other menus
            MenuHelper.CancelConfirmResult result = MenuHelper.DrawCancelConfirmButtons(buttonStart, UI.Width * 0.6f, true);
            
            // Handle button presses
            if (result == MenuHelper.CancelConfirmResult.Cancel)
            {
                _onCancel?.Invoke();
                UIDrawer.SetActiveMenu(UIDrawer.MenuType.LevelValidationResult);
            }
            else if (result == MenuHelper.CancelConfirmResult.Confirm)
            {
                HandleUpload();
            }
        }
        
        // ---------- Logic ----------
        static void HandleUpload()
        {
            string userName = "";
            bool shouldRemember = false;
            
            if (_uploadAsAnonymous)
            {
                userName = "";
                shouldRemember = false;
            }
            else
            {
                var inputState = UI.GetInputFieldState(ID_UserNameInput);
                userName = inputState.text?.Trim() ?? "";
                shouldRemember = _rememberName;
            }
            
            // Validate if not anonymous
            if (!_uploadAsAnonymous && !ValidateUserName(userName))
            {
                _validationMessage = "Please enter a valid name (3-20 characters)";
                return;
            }
            
            // Save preference if requested
            if (shouldRemember && !string.IsNullOrEmpty(userName))
            {
                SaveUserName(userName);
            }
            
            // Confirm and return to level validation report
            _onConfirm?.Invoke(userName, shouldRemember);
            UIDrawer.SetActiveMenu(UIDrawer.MenuType.LevelValidationResult);
        }
        
        static bool ValidateUserName(string userName)
        {
            if (string.IsNullOrEmpty(userName)) return false;
            if (userName.Length < 3 || userName.Length > 20) return false;
            
            // Check for valid characters (letters, numbers, spaces, hyphens, underscores)
            foreach (char c in userName)
            {
                if (!char.IsLetterOrDigit(c) && c != ' ' && c != '-' && c != '_')
                {
                    return false;
                }
            }
            
            // Check for reserved names
            string lowerName = userName.ToLower();
            if (lowerName == "anonymous" || lowerName == "guest" || lowerName == "admin")
            {
                return false;
            }
            
            return true;
        }
        
        static void LoadSavedUserName()
        {
            try
            {
                var project = Project.ActiveProject;
                if (project?.description != null)
                {
                    _userName = project.description.Prefs_UserName ?? "";
                    _rememberName = project.description.Prefs_RememberUserName;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UserNameInputPopup] Failed to load saved user name: {ex.Message}");
            }
        }
        
        static void SaveUserName(string userName)
        {
            try
            {
                var project = Project.ActiveProject;
                if (project?.description != null)
                {
                    project.description.Prefs_UserName = userName;
                    project.description.Prefs_RememberUserName = true;
                    project.UpdateAndSaveProjectDescription(project.description);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UserNameInputPopup] Failed to save user name: {ex.Message}");
            }
        }
    }
}
