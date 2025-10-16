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
        static bool _shareSolution = false;
        static string _validationMessage = "";
        static bool _hasInitializedInput = false; // Track if input has been initialized
        
        // UI constants
        const float InputFieldHeight = 4f;
        
        // UI handles
        static readonly UIHandle ID_UserNameInput = new("UserNameInput_Field");
        static readonly UIHandle ID_RememberNameCheckbox = new("UserNameInput_Remember");
        static readonly UIHandle ID_AnonymousCheckbox = new("UserNameInput_Anonymous");
        static readonly UIHandle ID_ShareSolutionCheckbox = new("UserNameInput_ShareSolution");
        
        // Callback for when user confirms
        static Action<string, bool, bool> _onConfirm; // userName, shouldRemember, shareSolution
        static Action _onCancel;
        
        // ---------- Public API ----------
        public static void Open(Action<string, bool, bool> onConfirm, Action onCancel = null)
        {
            _onConfirm = onConfirm;
            _onCancel = onCancel;
            
            // Load saved user name if available
            LoadSavedUserName();
            
            // Reset state
            _validationMessage = "";
            _uploadAsAnonymous = false;
            _shareSolution = false; // Reset share solution state
            _hasInitializedInput = false; // Reset input initialization flag
            
            UIDrawer.SetActiveMenu(UIDrawer.MenuType.UserNameInput);
        }
        
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
                Color headerCol = ColHelper.MakeCol255(44, 92, 62);
                Seb.Vis.UI.UI.DrawText("Enter Your Name", ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular * 2f, titlePos, Anchor.TextCentre, headerCol);
                Seb.Vis.UI.UI.ModifyPanel(titleBG, Bounds2D.Grow(Seb.Vis.UI.UI.PrevBounds, 3f), Color.clear);
                
                // --- Subtitle ---
                Vector2 subtitlePos = Seb.Vis.UI.UI.PrevBounds.CentreBottom + new Vector2(0f, -1.4f);
                Color subtitleCol = ColHelper.MakeCol255(200, 200, 200);
                Seb.Vis.UI.UI.DrawText("Your name will appear on the leaderboard", ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular, subtitlePos, Anchor.TextCentre, subtitleCol);
                
                // --- Input field ---
                DrawInputField();
                
                // --- Checkboxes ---
                DrawCheckboxes();
                
                // --- Validation message ---
                DrawValidationMessage();
                
                // --- Buttons ---
                DrawButtons();
                
                // Panel background
                MenuHelper.DrawReservedMenuPanel(panelBG, Seb.Vis.UI.UI.GetCurrentBoundsScope());
            }
        }
        
        // ---------- UI Drawing ----------
        static void DrawInputField()
        {
            Vector2 inputPos = Seb.Vis.UI.UI.PrevBounds.CentreBottom + new Vector2(0f, -3f);
            Vector2 inputSize = new Vector2(Seb.Vis.UI.UI.Width * 0.6f, InputFieldHeight);
            
            // Input field
            var inputState = Seb.Vis.UI.UI.GetInputFieldState(ID_UserNameInput);
            
            // Only initialize the input field once when the popup opens
            if (!_hasInitializedInput)
            {
                if (_uploadAsAnonymous)
                {
                    inputState.SetText("Anonymous", false);
                }
                else if (!string.IsNullOrEmpty(_userName))
                {
                    inputState.SetText(_userName, false);
                }
                _hasInitializedInput = true;
            }
            else if (_uploadAsAnonymous)
            {
                // Update text if anonymous checkbox is toggled
                inputState.SetText("Anonymous", false);
            }
            
            var inputTheme = MenuHelper.Theme.ChipNameInputField;
            inputTheme.fontSize = ActiveUITheme.FontSizeRegular;
            
            // Only use disabled scope when actually anonymous
            if (_uploadAsAnonymous)
            {
                using (Seb.Vis.UI.UI.BeginDisabledScope(true))
                {
                    Seb.Vis.UI.UI.InputField(
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
                    
                    // Draw overlay for visual feedback
                    Seb.Vis.UI.UI.DrawPanel(inputPos, inputSize, new Color(0, 0, 0, 0.3f), Anchor.Centre);
                }
            }
            else
            {
                // Normal input field without disabled scope
                Seb.Vis.UI.UI.InputField(
                    ID_UserNameInput,
                    inputTheme,
                    inputPos,
                    inputSize,
                    "Enter your name...",
                    Anchor.Centre,
                    1f,
                    ValidateUserName,
                    true  // forceFocus = true like other working input fields
                );
            }
        }
        
        static void DrawCheckboxes()
        {
            Vector2 checkboxStart = Seb.Vis.UI.UI.PrevBounds.CentreBottom + new Vector2(0f, -6f);
            float checkboxSize = 2.5f;
            float checkboxSpacing = 1.5f;
            
            // Remember name checkbox
            Vector2 rememberPos = checkboxStart;
            bool rememberPressed = Seb.Vis.UI.UI.Button(
                _rememberName ? "[X] Remember my name" : "[ ] Remember my name",
                MenuHelper.Theme.ButtonTheme,
                rememberPos,
                new Vector2(Seb.Vis.UI.UI.Width * 0.5f, checkboxSize),
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
            bool anonymousPressed = Seb.Vis.UI.UI.Button(
                _uploadAsAnonymous ? "[X] Upload as Anonymous" : "[ ] Upload as Anonymous",
                MenuHelper.Theme.ButtonTheme,
                anonymousPos,
                new Vector2(Seb.Vis.UI.UI.Width * 0.5f, checkboxSize),
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
            
            // Share Solution checkbox
            Vector2 shareSolutionPos = anonymousPos + Vector2.down * (checkboxSize + checkboxSpacing);
            bool shareSolutionPressed = Seb.Vis.UI.UI.Button(
                _shareSolution ? "[X] Share Solution" : "[ ] Share Solution",
                MenuHelper.Theme.ButtonTheme,
                shareSolutionPos,
                new Vector2(Seb.Vis.UI.UI.Width * 0.5f, checkboxSize),
                true,
                false,
                false,
                MenuHelper.Theme.ButtonTheme.buttonCols,
                Anchor.CentreTop
            );
            
            if (shareSolutionPressed)
            {
                _shareSolution = !_shareSolution;
            }
        }
        
        static void DrawValidationMessage()
        {
            if (string.IsNullOrEmpty(_validationMessage)) return;
            
            Vector2 messagePos = Seb.Vis.UI.UI.PrevBounds.CentreBottom + new Vector2(0f, -1.4f);
            Color messageCol = _validationMessage.Contains("✓") ? ColHelper.MakeCol255(100, 255, 100) : ColHelper.MakeCol255(255, 100, 100);
            
            Seb.Vis.UI.UI.DrawText(_validationMessage, ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular, messagePos, Anchor.TextCentre, messageCol);
        }
        
        static void DrawButtons()
        {
            Vector2 buttonStart = Seb.Vis.UI.UI.PrevBounds.CentreBottom + new Vector2(-Seb.Vis.UI.UI.Width*0.3f, -2f);
            
            // Use the same button pattern as other menus
            MenuHelper.CancelConfirmResult result = MenuHelper.DrawCancelConfirmButtons(buttonStart, Seb.Vis.UI.UI.Width * 0.6f, true);
            
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
                var inputState = Seb.Vis.UI.UI.GetInputFieldState(ID_UserNameInput);
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
            _onConfirm?.Invoke(userName, shouldRemember, _shareSolution);
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
