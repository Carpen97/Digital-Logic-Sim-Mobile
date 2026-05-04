using System;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Auth;
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
	/// Popup for uploading level scores. Three main states:
	/// - Not logged in (incl. anonymous): Create Account / Login / Upload as Anonymous
	/// - Logged in with email but no username (old accounts): Set Username / Upload as Anonymous
	/// - Has username: Signed in as X, Change username, Upload
	/// </summary>
	public static class UserNameInputPopup
	{
		enum ScreenState
		{
			MainChoice,       // Case 1, 1b, or 2 main screen
			CreateAccountForm,
			LoginForm,
			ClaimUsernameForm,  // Logged in, no username - first-time claim
			ChangeUsernameForm,
			ChangeUsernameConfirm
		}

		// ---------- State ----------
		static ScreenState _screenState = ScreenState.MainChoice;
		static string _validationMessage = "";
		static bool _hasClaimedUsername = false;
		static bool _isAuthenticated = false;  // Has Firebase auth (email or linked), regardless of username
		static bool _isLoadingProfile = false;
		static string _currentUsername = "";

		// Create Account state
		static string _createAccountError = "";
		static bool _createAccountInitialFocusDone = false;
		static bool _createAccountInProgress = false;

		// Login state
		static string _loginError = "";
		static bool _loginInitialFocusDone = false;
		static bool _loginInProgress = false;

		// Claim Username state (logged in, no username)
		static string _claimUsernameError = "";
		static bool _claimUsernameInitialized = false;
		static bool _claimUsernameInProgress = false;

		// Change Username state
		static string _changeUsernameOriginal = "";
		static string _changeUsernameNewName = "";
		static string _changeUsernameError = "";
		static bool _changeUsernameInitialized = false;
		static bool _changeUsernameInProgress = false;

		// UI handles (separate from MainMenu to avoid state conflict)
		static readonly UIHandle ID_LevelCreateAccount_Email = new("LevelCreateAccount_Email");
		static readonly UIHandle ID_LevelCreateAccount_Password = new("LevelCreateAccount_Password");
		static readonly UIHandle ID_LevelCreateAccount_Confirm = new("LevelCreateAccount_Confirm");
		static readonly UIHandle ID_LevelCreateAccount_Username = new("LevelCreateAccount_Username");
		static readonly UIHandle ID_LevelLogin_Email = new("LevelLogin_Email");
		static readonly UIHandle ID_LevelLogin_Password = new("LevelLogin_Password");
		static readonly UIHandle ID_LevelClaimUsername_Input = new("LevelClaimUsername_Input");
		static readonly UIHandle ID_LevelChangeUsername_Input = new("LevelChangeUsername_Input");

		static Action<string, bool> _onConfirm; // userName (empty = anonymous), shareSolution
		static Action _onCancel;
		static bool _shareSolution = false;

		// ---------- Public API ----------
		public static void Open(Action<string, bool> onConfirm, Action onCancel = null)
		{
			_onConfirm = onConfirm;
			_onCancel = onCancel;
			_shareSolution = false;

			_screenState = ScreenState.MainChoice;
			_validationMessage = "";
			_createAccountError = "";
			_changeUsernameError = "";
			_isLoadingProfile = true;

			_ = LoadUserProfileAsync();
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.UserNameInput);
		}

		static async Task LoadUserProfileAsync()
		{
			try
			{
				_isLoadingProfile = true;
				await FirebaseBootstrap.InitializeAsync();
				var user = FirebaseAuth.DefaultInstance?.CurrentUser;
				// Only consider "logged in" when user has a real account (email/linked), not anonymous.
				// App auto-signs in anonymously on startup - those users should see Create Account / Login.
				_isAuthenticated = user != null && !user.IsAnonymous;

				var profile = await UserAuthService.GetCurrentUserProfileAsync();

				if (profile != null && !string.IsNullOrEmpty(profile.username))
				{
					_hasClaimedUsername = true;
					_currentUsername = profile.username;
				}
				else
				{
					_hasClaimedUsername = false;
					_currentUsername = "";
				}
			}
			catch (Exception ex)
			{
				Debug.LogError($"[UserNameInputPopup] Failed to load profile: {ex.Message}");
				_hasClaimedUsername = false;
				_currentUsername = "";
			}
			_isLoadingProfile = false;
		}

		public static void DrawMenu()
		{
			MenuHelper.DrawBackgroundOverlay();

			if (_screenState == ScreenState.CreateAccountForm)
			{
				DrawCreateAccountForm();
				return;
			}
			if (_screenState == ScreenState.LoginForm)
			{
				DrawLoginForm();
				return;
			}
			if (_screenState == ScreenState.ClaimUsernameForm)
			{
				DrawClaimUsernameForm();
				return;
			}
			if (_screenState == ScreenState.ChangeUsernameForm)
			{
				DrawChangeUsernameForm();
				return;
			}
			if (_screenState == ScreenState.ChangeUsernameConfirm)
			{
				DrawChangeUsernameConfirm();
				return;
			}

			DrawMainChoice();
		}

		static void DrawMainChoice()
		{
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			float w = Seb.Vis.UI.UI.Width;
			float h = Seb.Vis.UI.UI.Height;
			Vector2 centre = Seb.Vis.UI.UI.Centre;

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();

				if (_isLoadingProfile)
				{
					Seb.Vis.UI.UI.DrawText("Loading...", theme.FontRegular, theme.FontSizeRegular * 1.2f, centre, Anchor.Centre, ColHelper.MakeCol255(255, 255, 100));
					MenuHelper.DrawReservedMenuPanel(panelID, Seb.Vis.UI.UI.GetCurrentBoundsScope());
					return;
				}

				float buttonWidth = w * 0.6f;
				float spacing = 1f;
				Vector2 buttonSize = new Vector2(buttonWidth, 2.5f);

				// Position content higher on screen (top third)
				float titleOffsetFromTop = h * 0.12f;
				Vector2 titlePos = new Vector2(centre.x, h - titleOffsetFromTop);

				if (_hasClaimedUsername)
				{
					// Case 2: Signed in as [username]
					Color headerCol = ColHelper.MakeCol255(44, 92, 62);
					Seb.Vis.UI.UI.DrawText("Upload Score", theme.FontBold, theme.FontSizeRegular * 1.5f, titlePos, Anchor.CentreTop, headerCol);

					Vector2 signedInPos = titlePos + Vector2.down * 6f;
					Seb.Vis.UI.UI.DrawText($"Signed in as {_currentUsername}", theme.FontRegular, theme.FontSizeRegular, signedInPos, Anchor.CentreTop, Color.white);

					Vector2 checkboxPos = signedInPos + Vector2.down * 5f;
					float checkboxSize = 2.5f;
					bool sharePressed = Seb.Vis.UI.UI.Button(
						_shareSolution ? "[X] Share Solution" : "[ ] Share Solution",
						MenuHelper.Theme.ButtonTheme,
						checkboxPos,
						new Vector2(buttonWidth * 0.8f, checkboxSize),
						true, false, false,
						MenuHelper.Theme.ButtonTheme.buttonCols,
						Anchor.CentreTop);
					if (sharePressed) _shareSolution = !_shareSolution;

					Vector2 buttonStart = checkboxPos + Vector2.down * (checkboxSize + 5f);
					int idx = Seb.Vis.UI.UI.VerticalButtonGroup(
						new[] { "CHANGE USERNAME", "UPLOAD", "CANCEL" },
						theme.MainMenuButtonTheme,
						buttonStart,
						buttonSize,
						false, true, spacing);

					if (idx == 0)
					{
						_screenState = ScreenState.ChangeUsernameForm;
						_changeUsernameOriginal = _currentUsername;
						_changeUsernameInitialized = false;
						_changeUsernameError = "";
						_ = PreloadChangeUsernameAsync();
					}
					else if (idx == 1)
					{
						_onConfirm?.Invoke(_currentUsername, _shareSolution);
						UIDrawer.SetActiveMenu(UIDrawer.MenuType.LevelValidationResult);
					}
					else if (idx == 2)
					{
						_onCancel?.Invoke();
						UIDrawer.SetActiveMenu(UIDrawer.MenuType.LevelValidationResult);
					}
				}
				else if (_isAuthenticated)
				{
					// Case 1b: Logged in but no username (existing users, or created account without username)
					Color headerCol = ColHelper.MakeCol255(44, 92, 62);
					Seb.Vis.UI.UI.DrawText("Upload Score", theme.FontBold, theme.FontSizeRegular * 1.5f, titlePos, Anchor.CentreTop, headerCol);

					Vector2 msgPos = titlePos + Vector2.down * 5f;
					string msg = "You're logged in but don't have a username yet. Set one to upload with your name, or upload as Anonymous.";
					float charWidth = theme.FontSizeRegular * 0.6f;
					float maxTextWidth = w * 0.55f;
					int maxCharsPerLine = Mathf.Max(20, Mathf.FloorToInt(maxTextWidth / charWidth));
					string wrappedMsg = Seb.Vis.UI.UI.LineBreakByCharCount(msg, maxCharsPerLine);
					Seb.Vis.UI.UI.DrawText(wrappedMsg, theme.FontRegular, theme.FontSizeRegular * 0.9f, msgPos, Anchor.CentreTop, ColHelper.MakeCol255(200, 200, 200));

					float gapBeforeCheckbox = 2f;
					float textBottom = Seb.Vis.UI.UI.PrevBounds.Bottom;
					Vector2 checkboxPos = new Vector2(centre.x, textBottom - gapBeforeCheckbox);
					float checkboxSize = 2f;
					bool sharePressed = Seb.Vis.UI.UI.Button(
						_shareSolution ? "[X] Share Solution" : "[ ] Share Solution",
						MenuHelper.Theme.ButtonTheme,
						checkboxPos,
						new Vector2(buttonWidth * 0.7f, checkboxSize),
						true, false, false,
						MenuHelper.Theme.ButtonTheme.buttonCols,
						Anchor.CentreTop);
					if (sharePressed) _shareSolution = !_shareSolution;

					float gapBeforeButtons = 4f;
					Vector2 buttonStart = checkboxPos + Vector2.down * (checkboxSize + gapBeforeButtons);
					int idx = Seb.Vis.UI.UI.VerticalButtonGroup(
						new[] { "SET USERNAME", "UPLOAD AS ANONYMOUS", "CANCEL" },
						theme.MainMenuButtonTheme,
						buttonStart,
						buttonSize,
						false, true, spacing);

					if (idx == 0)
					{
						_screenState = ScreenState.ClaimUsernameForm;
						_claimUsernameError = "";
						_claimUsernameInitialized = false;
						Seb.Vis.UI.UI.GetInputFieldState(ID_LevelClaimUsername_Input).SetText("");
					}
					else if (idx == 1)
					{
						_onConfirm?.Invoke("", _shareSolution);
						UIDrawer.SetActiveMenu(UIDrawer.MenuType.LevelValidationResult);
					}
					else if (idx == 2)
					{
						_onCancel?.Invoke();
						UIDrawer.SetActiveMenu(UIDrawer.MenuType.LevelValidationResult);
					}
				}
				else
				{
					// Case 1: Not logged in
					Color headerCol = ColHelper.MakeCol255(44, 92, 62);
					Seb.Vis.UI.UI.DrawText("Upload Score", theme.FontBold, theme.FontSizeRegular * 1.5f, titlePos, Anchor.CentreTop, headerCol);

					Vector2 msgPos = titlePos + Vector2.down * 5f;
					string msg = "To upload your score with a username, create an account or log in first. You can also upload as Anonymous.";
					float charWidth = theme.FontSizeRegular * 0.6f;
					float maxTextWidth = w * 0.55f;
					int maxCharsPerLine = Mathf.Max(20, Mathf.FloorToInt(maxTextWidth / charWidth));
					string wrappedMsg = Seb.Vis.UI.UI.LineBreakByCharCount(msg, maxCharsPerLine);
					Seb.Vis.UI.UI.DrawText(wrappedMsg, theme.FontRegular, theme.FontSizeRegular * 0.9f, msgPos, Anchor.CentreTop, ColHelper.MakeCol255(200, 200, 200));

					float gapBeforeCheckbox = 2f;
					float textBottom = Seb.Vis.UI.UI.PrevBounds.Bottom;
					Vector2 checkboxPos = new Vector2(centre.x, textBottom - gapBeforeCheckbox);
					float checkboxSize = 2f;
					bool sharePressed = Seb.Vis.UI.UI.Button(
						_shareSolution ? "[X] Share Solution" : "[ ] Share Solution",
						MenuHelper.Theme.ButtonTheme,
						checkboxPos,
						new Vector2(buttonWidth * 0.7f, checkboxSize),
						true, false, false,
						MenuHelper.Theme.ButtonTheme.buttonCols,
						Anchor.CentreTop);
					if (sharePressed) _shareSolution = !_shareSolution;

					float gapBeforeButtons = 4f;
					float gridWidth = w * 0.7f;
					float rowSpacing = 2.5f;
					Vector2 row1Pos = new Vector2(centre.x, Seb.Vis.UI.UI.PrevBounds.Bottom - gapBeforeButtons);
					int row1Idx = Seb.Vis.UI.UI.HorizontalButtonGroup(
						new[] { "CREATE ACCOUNT", "LOGIN" },
						theme.MainMenuButtonTheme,
						row1Pos,
						gridWidth,
						spacing, 0, Anchor.CentreTop);
					Vector2 row2Pos = new Vector2(centre.x, Seb.Vis.UI.UI.PrevBounds.Bottom - rowSpacing);
					int row2Idx = Seb.Vis.UI.UI.HorizontalButtonGroup(
						new[] { "UPLOAD AS ANONYMOUS", "CANCEL" },
						theme.MainMenuButtonTheme,
						row2Pos,
						gridWidth,
						spacing, 0, Anchor.CentreTop);

					int idx = row1Idx >= 0 ? row1Idx : (row2Idx >= 0 ? row2Idx + 2 : -1);

					if (idx == 0)
					{
						_screenState = ScreenState.CreateAccountForm;
						_createAccountError = "";
						_createAccountInitialFocusDone = false;
						Seb.Vis.UI.UI.GetInputFieldState(ID_LevelCreateAccount_Email).SetText("user@example.com");
						Seb.Vis.UI.UI.GetInputFieldState(ID_LevelCreateAccount_Username).SetText("");
					}
					else if (idx == 1)
					{
						_screenState = ScreenState.LoginForm;
						_loginError = "";
						_loginInitialFocusDone = false;
						Seb.Vis.UI.UI.GetInputFieldState(ID_LevelLogin_Email).SetText("user@example.com");
					}
					else if (idx == 2)
					{
						_onConfirm?.Invoke("", _shareSolution);
						UIDrawer.SetActiveMenu(UIDrawer.MenuType.LevelValidationResult);
					}
					else if (idx == 3)
					{
						_onCancel?.Invoke();
						UIDrawer.SetActiveMenu(UIDrawer.MenuType.LevelValidationResult);
					}
				}

				MenuHelper.DrawReservedMenuPanel(panelID, Seb.Vis.UI.UI.GetCurrentBoundsScope());
			}
		}

		static void DrawCreateAccountForm()
		{
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			float h = Seb.Vis.UI.UI.Height;
			Vector2 centre = Seb.Vis.UI.UI.Centre;
			float titleOffsetFromTop = h * 0.12f;
			Vector2 titlePos = new Vector2(centre.x, h - titleOffsetFromTop);

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();
				InputFieldTheme inputTheme = theme.ChipNameInputField;
				inputTheme.fontSize = theme.FontSizeRegular;

				Seb.Vis.UI.UI.DrawText("Create Account", theme.FontRegular, theme.FontSizeRegular * 1.1f, titlePos, Anchor.CentreTop, Color.white);
				Vector2 pos = titlePos + Vector2.down * 5f;

				Seb.Vis.UI.UI.DrawText("Email", theme.FontRegular, theme.FontSizeRegular * 0.8f, pos, Anchor.CentreTop, Color.white);
				pos += Vector2.down * 1.5f;
				bool forceFocus = !_createAccountInitialFocusDone;
				var emailState = Seb.Vis.UI.UI.InputField(ID_LevelCreateAccount_Email, inputTheme, pos, new Vector2(30, 3), "user@example.com", Anchor.CentreTop, 1f, null, forceFocus);
				_createAccountInitialFocusDone = true;
				pos += Vector2.down * 4;

				Seb.Vis.UI.UI.DrawText("Password", theme.FontRegular, theme.FontSizeRegular * 0.8f, pos, Anchor.CentreTop, Color.white);
				pos += Vector2.down * 1.5f;
				var passState = Seb.Vis.UI.UI.InputField(ID_LevelCreateAccount_Password, inputTheme, pos, new Vector2(30, 3), "", Anchor.CentreTop, 1f, null, false);
				pos += Vector2.down * 4;

				Seb.Vis.UI.UI.DrawText("Confirm Password", theme.FontRegular, theme.FontSizeRegular * 0.8f, pos, Anchor.CentreTop, Color.white);
				pos += Vector2.down * 1.5f;
				var confirmState = Seb.Vis.UI.UI.InputField(ID_LevelCreateAccount_Confirm, inputTheme, pos, new Vector2(30, 3), "", Anchor.CentreTop, 1f, null, false);
				pos += Vector2.down * 4;

				Seb.Vis.UI.UI.DrawText("Username (optional)", theme.FontRegular, theme.FontSizeRegular * 0.8f, pos, Anchor.CentreTop, Color.white);
				pos += Vector2.down * 1.5f;
				var usernameState = Seb.Vis.UI.UI.InputField(ID_LevelCreateAccount_Username, inputTheme, pos, new Vector2(30, 3), "", Anchor.CentreTop, 1f, null, false);
				pos += Vector2.down * 5;

				if (!string.IsNullOrEmpty(_createAccountError))
					Seb.Vis.UI.UI.DrawText(_createAccountError, theme.FontRegular, theme.FontSizeRegular * 0.7f, pos, Anchor.CentreTop, Color.red);
				pos += Vector2.down * 3;

				int btn = Seb.Vis.UI.UI.HorizontalButtonGroup(new[] { "CANCEL", "CREATE" }, theme.MainMenuButtonTheme, pos, 40, UILayoutHelper.DefaultSpacing, 0, Anchor.CentreTop);

				if (btn == 0 || KeyboardShortcuts.CancelShortcutTriggered)
				{
					_screenState = ScreenState.MainChoice;
				}
				else if (btn == 1 && !_createAccountInProgress)
				{
					var email = emailState.text?.Trim() ?? "";
					var password = passState.text ?? "";
					var confirm = confirmState.text ?? "";
					var username = usernameState.text?.Trim() ?? "";
					if (string.IsNullOrEmpty(email)) _createAccountError = "Email required.";
					else if (password.Length < 6) _createAccountError = "Password must be at least 6 characters.";
					else if (password != confirm) _createAccountError = "Passwords do not match.";
					else if (!string.IsNullOrEmpty(username) && !ValidateUsername(username))
						_createAccountError = "Username must be 3-20 characters (letters, numbers, spaces, hyphens, underscores).";
					else
						_ = LevelCreateAccountAsync(email, password, username);
				}

				MenuHelper.DrawReservedMenuPanel(panelID, Seb.Vis.UI.UI.GetCurrentBoundsScope());
			}
		}

		static async Task LevelCreateAccountAsync(string email, string password, string username)
		{
			_createAccountInProgress = true;
			_createAccountError = "";
			try
			{
				await FirebaseBootstrap.InitializeAsync();
				var auth = FirebaseAuth.DefaultInstance;
				if (auth == null) { _createAccountError = "Auth not available."; return; }

				var currentUser = auth.CurrentUser;
				bool isAnonymous = currentUser != null && currentUser.IsAnonymous;

				if (isAnonymous)
				{
					var credential = EmailAuthProvider.GetCredential(email, password);
					var result = await currentUser.LinkWithCredentialAsync(credential);
					if (result?.User == null) { _createAccountError = "Link failed."; return; }
				}
				else
				{
					var result = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
					if (result?.User == null) { _createAccountError = "Create failed."; return; }
				}

				FirebaseBootstrap.RefreshUserIdFromAuth();

				if (!string.IsNullOrWhiteSpace(username) && FirebaseBootstrap.UserId != "anon")
				{
					var claimResult = await UserAuthService.ClaimUsernameAsync(username);
					if (!claimResult.success)
						_createAccountError = claimResult.error ?? "Username claim failed.";
				}

				if (string.IsNullOrEmpty(_createAccountError))
				{
					_hasClaimedUsername = !string.IsNullOrWhiteSpace(username);
					_currentUsername = username ?? "";
					_screenState = ScreenState.MainChoice;
					_onConfirm?.Invoke(_currentUsername, _shareSolution);
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.LevelValidationResult);
				}
			}
			catch (Exception ex)
			{
				_createAccountError = ex.Message ?? "Create account failed.";
				Debug.LogError($"[UserNameInputPopup] Create account failed: {ex.Message}");
			}
			_createAccountInProgress = false;
		}

		static void DrawLoginForm()
		{
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			float h = Seb.Vis.UI.UI.Height;
			Vector2 centre = Seb.Vis.UI.UI.Centre;
			float titleOffsetFromTop = h * 0.12f;
			Vector2 titlePos = new Vector2(centre.x, h - titleOffsetFromTop);

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();
				InputFieldTheme inputTheme = theme.ChipNameInputField;
				inputTheme.fontSize = theme.FontSizeRegular;

				Seb.Vis.UI.UI.DrawText("Login", theme.FontRegular, theme.FontSizeRegular * 1.1f, titlePos, Anchor.CentreTop, Color.white);
				Vector2 pos = titlePos + Vector2.down * 5f;

				Seb.Vis.UI.UI.DrawText("Email", theme.FontRegular, theme.FontSizeRegular * 0.8f, pos, Anchor.CentreTop, Color.white);
				pos += Vector2.down * 1.5f;
				bool forceFocus = !_loginInitialFocusDone;
				var emailState = Seb.Vis.UI.UI.InputField(ID_LevelLogin_Email, inputTheme, pos, new Vector2(30, 3), "user@example.com", Anchor.CentreTop, 1f, null, forceFocus);
				_loginInitialFocusDone = true;
				pos += Vector2.down * 4;

				Seb.Vis.UI.UI.DrawText("Password", theme.FontRegular, theme.FontSizeRegular * 0.8f, pos, Anchor.CentreTop, Color.white);
				pos += Vector2.down * 1.5f;
				var passState = Seb.Vis.UI.UI.InputField(ID_LevelLogin_Password, inputTheme, pos, new Vector2(30, 3), "", Anchor.CentreTop, 1f, null, false);
				pos += Vector2.down * 5;

				if (!string.IsNullOrEmpty(_loginError))
					Seb.Vis.UI.UI.DrawText(_loginError, theme.FontRegular, theme.FontSizeRegular * 0.7f, pos, Anchor.CentreTop, Color.red);
				pos += Vector2.down * 3;

				int btn = Seb.Vis.UI.UI.HorizontalButtonGroup(new[] { "CANCEL", "LOGIN" }, theme.MainMenuButtonTheme, pos, 40, UILayoutHelper.DefaultSpacing, 0, Anchor.CentreTop);

				if (btn == 0 || KeyboardShortcuts.CancelShortcutTriggered)
				{
					_screenState = ScreenState.MainChoice;
				}
				else if (btn == 1 && !_loginInProgress)
				{
					var email = emailState.text?.Trim() ?? "";
					var password = passState.text ?? "";
					if (string.IsNullOrEmpty(email)) _loginError = "Email required.";
					else if (string.IsNullOrEmpty(password)) _loginError = "Password required.";
					else
						_ = LevelLoginAsync(email, password);
				}

				MenuHelper.DrawReservedMenuPanel(panelID, Seb.Vis.UI.UI.GetCurrentBoundsScope());
			}
		}

		static async Task LevelLoginAsync(string email, string password)
		{
			_loginInProgress = true;
			_loginError = "";
			try
			{
				await FirebaseBootstrap.InitializeAsync();
				var auth = FirebaseAuth.DefaultInstance;
				if (auth == null) { _loginError = "Auth not available."; return; }

				var result = await auth.SignInWithEmailAndPasswordAsync(email, password);
				if (result?.User != null)
				{
					FirebaseBootstrap.RefreshUserIdFromAuth();
					UserAuthService.ClearCache();
					await LoadUserProfileAsync();
					_screenState = ScreenState.MainChoice;
				}
				else
					_loginError = "Login failed.";
			}
			catch (Exception ex)
			{
				_loginError = ex.Message ?? "Login failed.";
				Debug.LogError($"[UserNameInputPopup] Login failed: {ex.Message}");
			}
			_loginInProgress = false;
		}

		static async Task PreloadChangeUsernameAsync()
		{
			try
			{
				var profile = await UserAuthService.GetCurrentUserProfileAsync();
				string original = (profile != null && !string.IsNullOrEmpty(profile.username)) ? profile.username : "";
				_changeUsernameOriginal = original;
				Seb.Vis.UI.UI.GetInputFieldState(ID_LevelChangeUsername_Input).SetText(original ?? "");
			}
			catch { _changeUsernameOriginal = ""; }
		}

		static void DrawClaimUsernameForm()
		{
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			float w = Seb.Vis.UI.UI.Width;
			float h = Seb.Vis.UI.UI.Height;
			Vector2 centre = Seb.Vis.UI.UI.Centre;
			float titleOffsetFromTop = h * 0.12f;
			Vector2 titlePos = new Vector2(centre.x, h - titleOffsetFromTop);

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();

				Seb.Vis.UI.UI.DrawText("Set Username", theme.FontRegular, theme.FontSizeRegular * 1.1f, titlePos, Anchor.CentreTop, Color.white);
				Vector2 pos = titlePos + Vector2.down * 5f;

				if (!_claimUsernameInitialized)
				{
					Seb.Vis.UI.UI.GetInputFieldState(ID_LevelClaimUsername_Input).SetText("");
					_claimUsernameInitialized = true;
				}

				InputFieldTheme inputTheme = theme.ChipNameInputField;
				inputTheme.fontSize = theme.FontSizeRegular;
				Seb.Vis.UI.UI.DrawText("Username (3–20 characters)", theme.FontRegular, theme.FontSizeRegular * 0.8f, pos, Anchor.CentreTop, Color.white);
				pos += Vector2.down * (h * 0.02f);
				float inputHeight = h * 0.032f;
				InputFieldState usernameState;
				if (_claimUsernameInProgress)
				{
					using (Seb.Vis.UI.UI.BeginDisabledScope(true))
						usernameState = Seb.Vis.UI.UI.InputField(ID_LevelClaimUsername_Input, inputTheme, pos, new Vector2(w * 0.6f, inputHeight), "Enter username...", Anchor.CentreTop, 1f, null, false);
				}
				else
				{
					usernameState = Seb.Vis.UI.UI.InputField(ID_LevelClaimUsername_Input, inputTheme, pos, new Vector2(w * 0.6f, inputHeight), "Enter username...", Anchor.CentreTop, 1f, null, false);
				}
				pos += Vector2.down * (inputHeight + h * 0.06f);

				if (!string.IsNullOrEmpty(_claimUsernameError))
				{
					Seb.Vis.UI.UI.DrawText(_claimUsernameError, theme.FontRegular, theme.FontSizeRegular * 0.8f, pos, Anchor.CentreTop, Color.red);
					pos += Vector2.down * (h * 0.03f);
				}
				pos += Vector2.down * (h * 0.05f);

				int btn = Seb.Vis.UI.UI.HorizontalButtonGroup(new[] { "CANCEL", "SET USERNAME" }, theme.MainMenuButtonTheme, new Vector2(centre.x, pos.y), w * 0.6f, UILayoutHelper.DefaultSpacing, 0, Anchor.CentreTop);

				if (btn == 0 || KeyboardShortcuts.CancelShortcutTriggered)
				{
					_screenState = ScreenState.MainChoice;
				}
				else if (btn == 1 && !_claimUsernameInProgress)
				{
					string newName = (usernameState.text ?? "").Trim();
					if (!ValidateUsername(newName))
						_claimUsernameError = "Username must be 3–20 characters (letters, numbers, spaces, hyphens, underscores).";
					else
						_ = LevelClaimUsernameAsync(newName);
				}

				MenuHelper.DrawReservedMenuPanel(panelID, Seb.Vis.UI.UI.GetCurrentBoundsScope());
			}
		}

		static async Task LevelClaimUsernameAsync(string username)
		{
			_claimUsernameInProgress = true;
			_claimUsernameError = "";
			try
			{
				var result = await UserAuthService.ClaimUsernameAsync(username);
				if (result.success)
				{
					_hasClaimedUsername = true;
					_currentUsername = username;
					_onConfirm?.Invoke(username, _shareSolution);
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.LevelValidationResult);
				}
				else
					_claimUsernameError = result.error ?? "Failed to claim username.";
			}
			catch (Exception ex)
			{
				_claimUsernameError = ex.Message ?? "Failed to claim username.";
				Debug.LogError($"[UserNameInputPopup] Claim username failed: {ex.Message}");
			}
			_claimUsernameInProgress = false;
		}

		static void DrawChangeUsernameForm()
		{
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			float w = Seb.Vis.UI.UI.Width;
			float h = Seb.Vis.UI.UI.Height;
			Vector2 centre = Seb.Vis.UI.UI.Centre;
			float titleOffsetFromTop = h * 0.12f;
			Vector2 titlePos = new Vector2(centre.x, h - titleOffsetFromTop);

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();

				Seb.Vis.UI.UI.DrawText("Change Username", theme.FontRegular, theme.FontSizeRegular * 1.1f, titlePos, Anchor.CentreTop, Color.white);
				Vector2 pos = titlePos + Vector2.down * 5f;

				if (!_changeUsernameInitialized)
				{
					Seb.Vis.UI.UI.GetInputFieldState(ID_LevelChangeUsername_Input).SetText(_changeUsernameOriginal ?? "");
					_changeUsernameInitialized = true;
				}

				InputFieldTheme inputTheme = theme.ChipNameInputField;
				inputTheme.fontSize = theme.FontSizeRegular;
				Seb.Vis.UI.UI.DrawText("Username", theme.FontRegular, theme.FontSizeRegular * 0.8f, pos, Anchor.CentreTop, Color.white);
				pos += Vector2.down * (h * 0.02f);
				float inputHeight = h * 0.032f;
				InputFieldState usernameState;
				if (_changeUsernameInProgress)
				{
					using (Seb.Vis.UI.UI.BeginDisabledScope(true))
						usernameState = Seb.Vis.UI.UI.InputField(ID_LevelChangeUsername_Input, inputTheme, pos, new Vector2(w * 0.6f, inputHeight), "Enter username...", Anchor.CentreTop, 1f, null, false);
				}
				else
				{
					usernameState = Seb.Vis.UI.UI.InputField(ID_LevelChangeUsername_Input, inputTheme, pos, new Vector2(w * 0.6f, inputHeight), "Enter username...", Anchor.CentreTop, 1f, null, false);
				}
				pos += Vector2.down * (inputHeight + h * 0.06f);

				if (!string.IsNullOrEmpty(_changeUsernameError))
				{
					Seb.Vis.UI.UI.DrawText(_changeUsernameError, theme.FontRegular, theme.FontSizeRegular * 0.8f, pos, Anchor.CentreTop, Color.red);
					pos += Vector2.down * (h * 0.03f);
				}
				pos += Vector2.down * (h * 0.05f);

				int btn = Seb.Vis.UI.UI.HorizontalButtonGroup(new[] { "CANCEL", "CONFIRM" }, theme.MainMenuButtonTheme, new Vector2(centre.x, pos.y), w * 0.6f, UILayoutHelper.DefaultSpacing, 0, Anchor.CentreTop);

				if (btn == 0 || KeyboardShortcuts.CancelShortcutTriggered)
				{
					_screenState = ScreenState.MainChoice;
				}
				else if (btn == 1 && !_changeUsernameInProgress)
				{
					string newName = (usernameState.text ?? "").Trim();
					if (!ValidateUsername(newName))
						_changeUsernameError = "Username must be 3-20 characters (letters, numbers, spaces, hyphens, underscores).";
					else if (string.Equals(newName, _changeUsernameOriginal, StringComparison.OrdinalIgnoreCase))
					{
						_screenState = ScreenState.MainChoice;
						_currentUsername = _changeUsernameOriginal;
					}
					else
					{
						_changeUsernameNewName = newName;
						_screenState = ScreenState.ChangeUsernameConfirm;
						_changeUsernameError = "";
					}
				}

				MenuHelper.DrawReservedMenuPanel(panelID, Seb.Vis.UI.UI.GetCurrentBoundsScope());
			}
		}

		static void DrawChangeUsernameConfirm()
		{
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			float h = Seb.Vis.UI.UI.Height;
			Vector2 centre = Seb.Vis.UI.UI.Centre;
			float titleOffsetFromTop = h * 0.1f;
			Vector2 titlePos = new Vector2(centre.x, h - titleOffsetFromTop);

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();

				Seb.Vis.UI.UI.DrawText("Change Username?", theme.FontRegular, theme.FontSizeRegular * 1.2f, titlePos, Anchor.CentreTop, ColHelper.MakeCol255(255, 165, 0));
				Vector2 msgPos = titlePos + Vector2.down * 8f;
				string msg = $"Change from \"{_changeUsernameOriginal}\" to \"{_changeUsernameNewName}\"?\n\nThis will update all your existing solutions.";
				Seb.Vis.UI.UI.DrawText(msg, theme.FontRegular, theme.FontSizeRegular, msgPos, Anchor.CentreTop, Color.white);

				Vector2 btnPos = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * 4f;
				int btn = Seb.Vis.UI.UI.HorizontalButtonGroup(new[] { "CANCEL", "CONFIRM CHANGE" }, theme.MainMenuButtonTheme, new Vector2(centre.x, btnPos.y), Seb.Vis.UI.UI.Width * 0.6f, UILayoutHelper.DefaultSpacing, 0, Anchor.CentreTop);

				if (btn == 0 || KeyboardShortcuts.CancelShortcutTriggered)
				{
					_screenState = ScreenState.ChangeUsernameForm;
				}
				else if (btn == 1)
				{
					_ = ConfirmChangeUsernameAsync();
				}

				MenuHelper.DrawReservedMenuPanel(panelID, Seb.Vis.UI.UI.GetCurrentBoundsScope());
			}
		}

		static async Task ConfirmChangeUsernameAsync()
		{
			_changeUsernameInProgress = true;
			_changeUsernameError = "";
			try
			{
				var result = await UserAuthService.ChangeUsernameAsync(_changeUsernameNewName);
				if (result.success)
				{
					_currentUsername = _changeUsernameNewName;
					_changeUsernameOriginal = _changeUsernameNewName;
					_screenState = ScreenState.MainChoice;
				}
				else
				{
					_changeUsernameError = result.error;
					_screenState = ScreenState.ChangeUsernameForm;
				}
			}
			catch (Exception ex)
			{
				_changeUsernameError = ex.Message;
				_screenState = ScreenState.ChangeUsernameForm;
			}
			_changeUsernameInProgress = false;
		}

		static bool ValidateUsername(string userName)
		{
			if (string.IsNullOrEmpty(userName)) return false;
			if (userName.Length < 3 || userName.Length > 20) return false;
			foreach (char c in userName)
			{
				if (!char.IsLetterOrDigit(c) && c != ' ' && c != '-' && c != '_')
					return false;
			}
			string lower = userName.ToLower();
			return lower != "anonymous" && lower != "guest" && lower != "admin";
		}
	}
}
