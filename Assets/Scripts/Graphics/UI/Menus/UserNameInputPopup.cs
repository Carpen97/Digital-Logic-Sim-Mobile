using System;
using System.Threading.Tasks;
using UnityEngine;
using Seb.Helpers;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using DLS.Game;
using DLS.Online;
using static DLS.Graphics.DrawSettings;

namespace DLS.Graphics
{
    /// <summary>
    /// Popup for entering user name when uploading scores to Firebase leaderboard.
    /// Mobile-optimized with validation, username reservation, and authentication.
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
        static bool _isCheckingAvailability = false;
        static bool _hasClaimedUsername = false; // Whether user already has a username
        static bool _isLoadingProfile = false;
        static string _originalUserName = ""; // Store original username for change detection
        static bool _showNameChangeConfirmation = false;
        static string _newUserName = "";
        
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
            
            // Reset state
            _validationMessage = "";
            _uploadAsAnonymous = false;
            _shareSolution = false;
            _hasInitializedInput = false;
            _isCheckingAvailability = false;
            _hasClaimedUsername = false;
            _isLoadingProfile = true;
            
            // Load user profile asynchronously
            _ = LoadUserProfileAsync();
            
            UIDrawer.SetActiveMenu(UIDrawer.MenuType.UserNameInput);
        }
        
        /// <summary>
        /// Loads the user's profile from Firebase to check if they have a claimed username
        /// </summary>
        static async Task LoadUserProfileAsync()
        {
            try
            {
                _isLoadingProfile = true;
                _validationMessage = "🔄 Loading user profile...";
                
                var profile = await UserAuthService.GetCurrentUserProfileAsync();
                
                if (profile != null && !string.IsNullOrEmpty(profile.username))
                {
                    // User already has a claimed username
                    _userName = profile.username;
                    _originalUserName = profile.username; // Store original for change detection
                    _hasClaimedUsername = true;
                    _validationMessage = $"✓ Verified user: {profile.username}";
                    _rememberName = true; // Enable remember checkbox for claimed users
                }
                else
                {
                    // User has no claimed username, check local preferences
                    LoadSavedUserName();
                    _hasClaimedUsername = false;
                    _validationMessage = "";
                }
                
                _isLoadingProfile = false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UserNameInputPopup] Failed to load user profile: {ex.Message}");
                // Fall back to local preferences
                LoadSavedUserName();
                _hasClaimedUsername = false;
                _isLoadingProfile = false;
                _validationMessage = "";
            }
        }
        
        public static void DrawMenu()
        {
            // Dimmed backdrop (same as other popups)
            MenuHelper.DrawBackgroundOverlay();
            
            // If showing confirmation dialog, only draw that
            if (_showNameChangeConfirmation)
            {
                DrawNameChangeConfirmation();
                return;
            }
            
            // Otherwise draw the main menu
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
                string subtitle = _hasClaimedUsername 
                    ? "Your authenticated username will be used" 
                    : "Pick a username that will be displayed on the leaderboard";
                Seb.Vis.UI.UI.DrawText(subtitle, ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular, subtitlePos, Anchor.TextCentre, subtitleCol);
                
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
            
            // Disable input field when uploading anonymously or loading profile
            bool shouldDisable = _uploadAsAnonymous || _isLoadingProfile;
            
            if (shouldDisable)
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
                        null,  // No validation callback - validate only on submit
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
                    null,  // No validation callback - validate only on submit
                    true  // forceFocus = true like other working input fields
                );
            }
        }
        
        static void DrawCheckboxes()
        {
            Vector2 checkboxStart = Seb.Vis.UI.UI.PrevBounds.CentreBottom + new Vector2(0f, -6f);
            float checkboxSize = 2.5f;
            float checkboxSpacing = 1.5f;
            
            // Remember name checkbox (enabled for all users, disabled only when anonymous)
            Vector2 rememberPos = checkboxStart;
            bool rememberEnabled = !_uploadAsAnonymous;
            bool rememberPressed = Seb.Vis.UI.UI.Button(
                _rememberName ? "[X] Remember my name" : "[ ] Remember my name",
                MenuHelper.Theme.ButtonTheme,
                rememberPos,
                new Vector2(Seb.Vis.UI.UI.Width * 0.5f, checkboxSize),
                rememberEnabled,
                false,
                false,
                MenuHelper.Theme.ButtonTheme.buttonCols,
                Anchor.CentreTop
            );
            
            if (rememberPressed && rememberEnabled)
            {
                _rememberName = !_rememberName;
            }
            
            // Anonymous checkbox (disabled when user has claimed username)
            Vector2 anonymousPos = rememberPos + Vector2.down * (checkboxSize + checkboxSpacing);
            bool anonymousEnabled = !_hasClaimedUsername;
            bool anonymousPressed = Seb.Vis.UI.UI.Button(
                _uploadAsAnonymous ? "[X] Upload as Anonymous" : "[ ] Upload as Anonymous",
                MenuHelper.Theme.ButtonTheme,
                anonymousPos,
                new Vector2(Seb.Vis.UI.UI.Width * 0.5f, checkboxSize),
                anonymousEnabled,
                false,
                false,
                MenuHelper.Theme.ButtonTheme.buttonCols,
                Anchor.CentreTop
            );
            
            if (anonymousPressed && anonymousEnabled)
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
            
            // Make loading messages more prominent
            if (_validationMessage.Contains("Updating username") || _validationMessage.Contains("Claiming username") || _validationMessage.Contains("Loading"))
            {
                // Use larger font and more prominent color for loading states
                messageCol = ColHelper.MakeCol255(255, 255, 100); // Bright yellow for loading
                Seb.Vis.UI.UI.DrawText(_validationMessage, ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular * 1.2f, messagePos, Anchor.TextCentre, messageCol);
            }
            else
            {
                Seb.Vis.UI.UI.DrawText(_validationMessage, ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular, messagePos, Anchor.TextCentre, messageCol);
            }
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
            // Prevent double-click during async operations
            if (_isCheckingAvailability || _isLoadingProfile)
            {
                _validationMessage = "Please wait...";
                return;
            }
            
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
            
            // Handle username claiming for authenticated users
            if (!_uploadAsAnonymous && !_hasClaimedUsername)
            {
                // User wants to use a username but hasn't claimed one yet
                _ = ClaimUsernameAndContinueAsync(userName, shouldRemember);
                return;
            }
            
            // Handle name change for users who already have a claimed username
            if (!_uploadAsAnonymous && _hasClaimedUsername && !string.IsNullOrEmpty(_originalUserName))
            {
                if (userName != _originalUserName)
                {
                    // User wants to change their name - show confirmation dialog
                    _newUserName = userName;
                    _showNameChangeConfirmation = true;
                    return;
                }
            }
            
            // Save preference if requested (for non-authenticated mode)
            if (shouldRemember && !string.IsNullOrEmpty(userName))
            {
                SaveUserName(userName);
            }
            
            // Confirm and return to level validation report
            _onConfirm?.Invoke(userName, shouldRemember, _shareSolution);
            UIDrawer.SetActiveMenu(UIDrawer.MenuType.LevelValidationResult);
        }
        
        /// <summary>
        /// Claims a username for the user and continues with upload
        /// </summary>
        static async Task ClaimUsernameAndContinueAsync(string userName, bool shouldRemember)
        {
            try
            {
                _isCheckingAvailability = true;
                _validationMessage = "🔄 Claiming username...";
                
                var result = await UserAuthService.ClaimUsernameAsync(userName);
                
                if (result.success)
                {
                    _validationMessage = "✓ Username claimed successfully!";
                    _hasClaimedUsername = true;
                    
                    // Wait a moment to show success message
                    await Task.Delay(500);
                    
                    // Continue with upload
                    _onConfirm?.Invoke(userName, false, _shareSolution);
                    UIDrawer.SetActiveMenu(UIDrawer.MenuType.LevelValidationResult);
                }
                else
                {
                    _validationMessage = result.error;
                }
                
                _isCheckingAvailability = false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UserNameInputPopup] Failed to claim username: {ex.Message}");
                _validationMessage = "Failed to claim username. Try again.";
                _isCheckingAvailability = false;
            }
        }
        
        static bool ValidateUserName(string userName)
        {
            if (string.IsNullOrEmpty(userName)) return false;
            
            // Full validation for submission (3-20 characters)
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
        
        /// <summary>
        /// Draws the name change confirmation dialog
        /// </summary>
        static void DrawNameChangeConfirmation()
        {
            using (Seb.Vis.UI.UI.BeginBoundsScope(true))
            {
                Draw.ID dialogBG = Seb.Vis.UI.UI.ReservePanel();
                
                // Dialog title - centered at top
                Vector2 titlePos = Seb.Vis.UI.UI.CentreTop + Vector2.down * 15f;
                Color headerCol = ColHelper.MakeCol255(255, 165, 0); // Orange for warning
                Seb.Vis.UI.UI.DrawText("Change Username?", ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular * 1.5f, titlePos, Anchor.TextCentre, headerCol);
                
                // Warning message - centered below title with proper spacing
                Vector2 messagePos = titlePos + Vector2.down * 10f; // Increased spacing below title
                Color messageCol = Color.white;
                string message = $"You are changing your username from:\n\"{_originalUserName}\"\nto:\n\"{_newUserName}\"\n\nThis will update all your existing solutions.";
                Seb.Vis.UI.UI.DrawText(message, ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular, messagePos, Anchor.TextCentre, messageCol);
                
                // Buttons
                Vector2 buttonPos = Seb.Vis.UI.UI.PrevBounds.CentreBottom + new Vector2(0f, -4f);
                string[] buttonNames = { "CONFIRM CHANGE", "CANCEL" };
                bool[] buttonStates = { true, true };
                
                int buttonIndex = Seb.Vis.UI.UI.HorizontalButtonGroup(
                    buttonNames,
                    buttonStates,
                    MenuHelper.Theme.ButtonTheme,
                    buttonPos,
                    Seb.Vis.UI.UI.Width * 0.7f,
                    DrawSettings.DefaultButtonSpacing,
                    0,
                    Anchor.CentreTop
                );
                
                // Handle button results
                if (buttonIndex == 0) // CONFIRM CHANGE
                {
                    _ = ConfirmNameChangeAsync();
                }
                else if (buttonIndex == 1) // CANCEL
                {
                    _showNameChangeConfirmation = false;
                    _newUserName = "";
                    // Reset input field to original name
                    var inputState = Seb.Vis.UI.UI.GetInputFieldState(ID_UserNameInput);
                    inputState.SetText(_originalUserName, false);
                    // Go back to LevelValidationPopup
                    _onCancel?.Invoke();
                    UIDrawer.SetActiveMenu(UIDrawer.MenuType.LevelValidationResult);
                }
                
                // Dialog background
                MenuHelper.DrawReservedMenuPanel(dialogBG, Seb.Vis.UI.UI.GetCurrentBoundsScope());
            }
        }
        
        /// <summary>
        /// Confirms the name change and updates existing solutions
        /// </summary>
        static async Task ConfirmNameChangeAsync()
        {
            try
            {
                _validationMessage = "🔄 Updating username and all solutions...";
                
                // Use the new comprehensive username change method
                var result = await UserAuthService.ChangeUsernameAsync(_newUserName);
                if (!result.success)
                {
                    throw new Exception(result.error);
                }
                
                Debug.Log($"[UserNameInputPopup] Successfully updated username to: {_newUserName}");
                
                // Update local state
                _originalUserName = _newUserName;
                _userName = _newUserName;
                _showNameChangeConfirmation = false;
                _newUserName = "";
                
                _validationMessage = $"✓ Username updated to: {_userName}";
                
                // Continue with the original upload
                _onConfirm?.Invoke(_userName, _rememberName, _shareSolution);
                UIDrawer.SetActiveMenu(UIDrawer.MenuType.LevelValidationResult);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UserNameInputPopup] Failed to update username: {ex.Message}");
                _validationMessage = "Failed to update username. Please try again.";
                _showNameChangeConfirmation = false;
                _newUserName = "";
            }
        }
    }
}
