using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DLS.Description;
using DLS.Game;
using DLS.SaveSystem;
using DLS.Simulation;
using DLS.Online;
using Firebase.Auth;
using Seb.Helpers;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	public static class MainMenu
	{
		public const int MaxProjectNameLength = 20;
		const bool capitalize = true;

	static MenuScreen activeMenuScreen = MenuScreen.Main;
	static PopupKind activePopup = PopupKind.None;
	static AppSettings EditedAppSettings;
	static string projectCreationErrorMessage = "";
	static List<string> projectCreationDebugLogs = new List<string>();
	static int selectedPatchNoteIndex = 0; // Track which patch note version is selected
	static PatchNotesData patchNotesData;

	// Project Sharing state
	static bool _projectSharingSignedIn;
	static bool _projectSharingIsGuest;
	static string _projectSharingAuthError;
	static bool _projectSharingCreateAccountInitialFocusDone;
	static bool _projectSharingLoginInitialFocusDone;
	static int _projectSharingUploadSelectedIndex;
	static bool _projectSharingUploadIsPublic = true;
	static bool _projectSharingUploadIncludeLevels = true;
	static bool _projectSharingUploadInProgress;
	static string _projectSharingUploadStatus;
	static List<LibraryService.LibraryEntry> _projectSharingLibraryEntries;
	static bool _projectSharingLibraryLoading;
	static string _projectSharingImportStatus;
	static string _projectSharingImportInProgressId;
	static LibraryService.LibraryEntry _projectSharingImportSelectedEntry;
	static LibraryService.LibraryFilterMode _projectSharingImportFilter = LibraryService.LibraryFilterMode.Public;
	static LibraryService.LibrarySortOrder _projectSharingImportSortOrder = LibraryService.LibrarySortOrder.Newest;
	static List<LibraryService.LibraryEntry> _projectSharingMyProjectsEntries;
	static bool _projectSharingMyProjectsLoading;
	static LibraryService.LibraryFilterMode _projectSharingMyProjectsFilter = LibraryService.LibraryFilterMode.All;
	static LibraryService.LibraryEntry _projectSharingDeleteConfirmEntry;
	static LibraryService.LibraryEntry _projectSharingEditEntry;
	static LibraryService.LibraryEntry _projectSharingMyProjectsSelectedEntry;
	static string _projectSharingUploadPendingProjectName;
	static string _projectSharingLoggedInAs;
	static bool _projectSharingLoggedInAsLoadRequested;
	static bool _projectSharingAuthSyncRequested;
	static bool _projectSharingChangeUsernameInitialized;
	static string _projectSharingChangeUsernameOriginal;
	static bool _projectSharingChangeUsernameHasUsername;
	static bool _projectSharingChangeUsernameShowConfirm;
	static string _projectSharingChangeUsernameNewName;
	static string _projectSharingChangeUsernameError;
	static bool _projectSharingChangeUsernameInProgress;
	static string _projectSharingUploadProjectDisplayNameDefault;
	static bool _projectSharingUploadProjectDisplayNameInitialized;
	static bool _projectSharingEditProjectDisplayNameInitialized;
	static bool _projectSharingEditIsPublic;

	static readonly UIHandle ID_ProjectNameInput = new("MainMenu_ProjectNameInputField");
	static readonly UIHandle ID_DisplayResolutionWheel = new("MainMenu_DisplayResolutionWheel");
	static readonly UIHandle ID_DisplayWidthWheel = new("MainMenu_DisplayWidthWheel");
	static readonly UIHandle ID_DisplayHeightWheel = new("MainMenu_DisplayHeightWheel");
	static readonly UIHandle ID_FullscreenWheel = new("MainMenu_FullscreenWheel");
	static readonly UIHandle ID_Orientation = new("MainMenu_OrientationWheel");
	static readonly UIHandle ID_ShowScrollButtons = new("MainMenu_ShowScrollButtonsnWheel");
	static readonly UIHandle ID_UIScaling = new("MainMenu_UIScalingWheel");
	static readonly UIHandle ID_ProjectsScrollView = new("MainMenu_ProjectsScrollView");
	static readonly UIHandle ID_ErrorLogsScrollView = new("MainMenu_ErrorLogsScrollView");
	static readonly UIHandle ID_PatchNotesScrollView = new("MainMenu_PatchNotesScrollView");
	static readonly UIHandle ID_ProjectSharing_CreateAccountEmail = new("ProjectSharing_CreateAccountEmail");
	static readonly UIHandle ID_ProjectSharing_CreateAccountPassword = new("ProjectSharing_CreateAccountPassword");
	static readonly UIHandle ID_ProjectSharing_CreateAccountConfirm = new("ProjectSharing_CreateAccountConfirm");
	static readonly UIHandle ID_ProjectSharing_CreateAccountUsername = new("ProjectSharing_CreateAccountUsername");
	static readonly UIHandle ID_ProjectSharing_LoginEmail = new("ProjectSharing_LoginEmail");
	static readonly UIHandle ID_ProjectSharing_LoginPassword = new("ProjectSharing_LoginPassword");
	static readonly UIHandle ID_ProjectSharing_LibraryScrollView = new("ProjectSharing_LibraryScrollView");
	static readonly UIHandle ID_ProjectSharing_UploadProjectDisplayName = new("ProjectSharing_UploadProjectDisplayName");
	static readonly UIHandle ID_ProjectSharing_EditProjectDisplayName = new("ProjectSharing_EditProjectDisplayName");
	static readonly UIHandle ID_ProjectSharing_MyProjectsScrollView = new("ProjectSharing_MyProjectsScrollView");
	static readonly UIHandle ID_ProjectSharing_UploadScrollView = new("ProjectSharing_UploadScrollView");
	static readonly UIHandle ID_ProjectSharing_ChangeUsername = new("ProjectSharing_ChangeUsername");

		#if UNITY_ANDROID || UNITY_IOS
		static readonly string[] SettingsWheelFullScreenOptions = { "AUTO","WINDOWED", "MAXIMIZED", "BORDERLESS", "EXCLUSIVE" };
		static readonly FullScreenMode[] FullScreenModes = { FullScreenMode.Windowed, FullScreenMode.MaximizedWindow, FullScreenMode.FullScreenWindow, FullScreenMode.ExclusiveFullScreen };
		#else
		static readonly string[] SettingsWheelFullScreenOptions = { "WINDOWED", "MAXIMIZED", "BORDERLESS", "EXCLUSIVE" };
		static readonly FullScreenMode[] FullScreenModes = { FullScreenMode.Windowed, FullScreenMode.MaximizedWindow, FullScreenMode.FullScreenWindow, FullScreenMode.ExclusiveFullScreen };
		#endif
		static readonly string[] SettingsWheelOrientationOptions = { "LEFT LANDSCAPE", "RIGHT LANDSCAPE"};
		static readonly string[] SettingsWheelBottomBarScrollingOptions = { "ARROWS", "ARROWS (inverted)", "TOUCH DRAG (no arrows)", "OFF" };
		static readonly string[] SettingsWheelUIScalingOptions = { "SMALL","MEDIUM", "LARGE"};
		static readonly string[] SettingsWheelVSyncOptions = { "DISABLED", "ENABLED" };
		#if !UNITY_ANDROID && !UNITY_IOS
		static readonly string[] SettingsWheelDiscordOptions = { "OFF", "ON" };
		#endif

		static readonly Func<string, bool> projectNameValidator = ProjectNameValidator;
		static readonly Seb.Vis.UI.UI.ScrollViewDrawContentFunc loadProjectScrollViewDrawer = DrawAllProjectsInScrollView;


		static readonly string[] menuButtonNames =
		{
			FormatButtonString("New Project"),
			FormatButtonString("Open Project"),
			FormatButtonString("Project Sharing"),
			FormatButtonString("Settings"),
			FormatButtonString("About"),
			FormatButtonString("Quit")
		};

		#if UNITY_ANDROID || UNITY_IOS
		static readonly string[] openProjectButtonNames =
		{
			FormatButtonString("Back"),
			FormatButtonString("Delete"),
			FormatButtonString("Copy"),
			FormatButtonString("Rename"),
			FormatButtonString("Open"),
			FormatButtonString("Import"),
			FormatButtonString("Export")
		};
		#else 
		static readonly string[] openProjectButtonNames =
		{
			FormatButtonString("Back"),
			FormatButtonString("Delete"),
			FormatButtonString("Duplicate"),
			FormatButtonString("Rename"),
			FormatButtonString("Open")
		};
		#endif

		static readonly Vector2Int[] Resolutions =
		{
			new(960, 540),
			new(1280, 720),
			new(1920, 1080),
			new(2280, 1080),
			new(2560, 1080),
			new(1920, 1440),
			new(1440, 1920),
			new(2560, 1440)
		};

		static readonly int[] WidthOptions =
		{
			800,
			854,
			960,
			1024,
			1280,
			1440,
			1600,
			1920,
			2048,
			2280,
			2340,
			2400,
			2560,
			3200
		};


		static readonly int[] HeightOptions =
		{
			480,
			540,
			600,
			720,
			1080,
			1200,
			1440,
			1536,
			1600
		};


		static readonly string[] ResolutionNames = Resolutions.Select(r => ResolutionToString(r)).ToArray();
		static readonly string[] WidthNames = WidthOptions.Select(w => $"{w}").ToArray();
		static readonly string[] HeightNames = HeightOptions.Select(h => $"{h}").ToArray();
		static readonly string[] FullScreenResName = Resolutions.Select(r => ResolutionToString(Main.FullScreenResolution)).ToArray();

		static readonly string[] WidthName = WidthOptions.Select(w => $"{Main.FullScreenResolution.x}").ToArray();
		static readonly string[] HeightName = HeightOptions.Select(h => $"{Main.FullScreenResolution.y}").ToArray();
		static readonly string[] projectSharingAuthButtonNames =
		{
			FormatButtonString("Create Account"),
			FormatButtonString("Login"),
			FormatButtonString("Sign in as Guest")
		};

		static readonly string[] projectSharingMainButtonNames =
		{
			FormatButtonString("Export project"),
			FormatButtonString("Import projects"),
			FormatButtonString("My projects"),
			FormatButtonString("Change username"),
			FormatButtonString("Log Out")
		};
		static readonly string[] projectSharingMainButtonNamesGuest =
		{
			FormatButtonString("Export project"),
			FormatButtonString("Import projects"),
			FormatButtonString("My projects"),
			FormatButtonString("Log Out")
		};

		static readonly string[] settingsButtonGroupNames = { "EXIT", "APPLY" };
		static readonly bool[] settingsButtonGroupStates = new bool[settingsButtonGroupNames.Length];

		static readonly bool[] openProjectButtonStates = new bool[openProjectButtonNames.Length];

		static ProjectDescription[] allProjectDescriptions;
		static string[] allProjectNames;
		static (bool compatible, string message)[] projectCompatibilities;

		static int selectedProjectIndex;
		static readonly string authorString = "Created by: Sebastian Lague";
		static readonly string mobileString = $"Mobile port: David Carpenfelt";
		static readonly string versionString = $"Version: {Main.DLSVersion} ({Main.LastUpdatedString})";
		static readonly string moddedString = $"ComEdit: {Main.DLSVersion_ModdedID} ({Main.LastUpdatedModdedString})";
		static string SelectedProjectName => allProjectDescriptions[selectedProjectIndex].ProjectName;

		static string FormatButtonString(string s) => capitalize ? s.ToUpper() : s;

	static string WrapText(string text, int maxCharsPerLine)
	{
		if (string.IsNullOrEmpty(text)) return text;
		
		string[] words = text.Split(' ');
		System.Text.StringBuilder result = new System.Text.StringBuilder();
		System.Text.StringBuilder currentLine = new System.Text.StringBuilder();
		
		foreach (string word in words)
		{
			// Check if adding this word would exceed the limit
			if (currentLine.Length > 0 && currentLine.Length + word.Length + 1 > maxCharsPerLine)
			{
				// Start a new line
				result.AppendLine(currentLine.ToString());
				currentLine.Clear();
			}
			
			// Add word to current line
			if (currentLine.Length > 0)
			{
				currentLine.Append(" ");
			}
			currentLine.Append(word);
		}
		
		// Add the last line
		if (currentLine.Length > 0)
		{
			result.Append(currentLine.ToString());
		}
		
		return result.ToString();
	}

		public static void Draw()
		{
			Simulator.UpdateInPausedState();
			
			if (KeyboardShortcuts.CancelShortcutTriggered && activePopup == PopupKind.None)
			{
				BackToMain();
			}

			Seb.Vis.UI.UI.DrawFullscreenPanel(ColHelper.MakeCol255(47, 47, 53));
			const string title = "DIGITAL LOGIC SIM";
			const float titleFontSize = 11.5f;
			const float titleHeight = 24;
			const float shaddowOffset = -0.33f;
			Color shadowCol = ColHelper.MakeCol255(87, 94, 230);

			Seb.Vis.UI.UI.DrawText(title, FontType.Born2bSporty, titleFontSize, Seb.Vis.UI.UI.Centre + Vector2.up * (titleHeight + shaddowOffset), Anchor.CentreTop, shadowCol);
			Seb.Vis.UI.UI.DrawText(title, FontType.Born2bSporty, titleFontSize, Seb.Vis.UI.UI.Centre + Vector2.up * titleHeight, Anchor.CentreTop, Color.white);
			DrawVersionInfo();

			switch (activeMenuScreen)
			{
				case MenuScreen.Main:
					DrawMainScreen();
					break;
				case MenuScreen.LoadProject:
					DrawLoadProjectScreen();
					break;
				case MenuScreen.Settings:
					DrawSettingsScreen();
					break;
				case MenuScreen.ProjectSharing:
					DrawProjectSharingScreen();
					break;
				case MenuScreen.About:
					DrawAboutScreen();
					break;
			}

		switch (activePopup)
		{
			case PopupKind.DeleteConfirmation:
				DrawDeleteProjectConfirmationPopup();
				break;
			case PopupKind.OverwriteConfirmation:
				DrawOverwriteProjectConfirmationPopup();
				break;
			case PopupKind.NamePopup_RenameProject:
				DrawNamePopup();
				break;
			case PopupKind.NamePopup_DuplicateProject:
				DrawNamePopup();
				break;
			case PopupKind.NamePopup_NewProject:
				DrawNamePopup();
				break;
		case PopupKind.ProjectCreationError:
			DrawProjectCreationErrorPopup();
			break;
		case PopupKind.PatchNotes:
			DrawPatchNotesPopup();
			break;
		case PopupKind.ProjectSharing_CreateAccount:
			DrawProjectSharingCreateAccountPopup();
			break;
		case PopupKind.ProjectSharing_Login:
			DrawProjectSharingLoginPopup();
			break;
		case PopupKind.ProjectSharing_UploadConfirm:
			DrawProjectSharingUploadConfirmPopup();
			break;
		case PopupKind.ProjectSharing_ImportList:
			DrawProjectSharingImportListPopup();
			break;
		case PopupKind.ProjectSharing_MyProjects:
			DrawProjectSharingMyProjectsPopup();
			break;
		case PopupKind.ProjectSharing_UploadDisplayName:
			DrawProjectSharingUploadDisplayNamePopup();
			break;
		case PopupKind.ProjectSharing_DeleteConfirm:
			DrawProjectSharingDeleteConfirmPopup();
			break;
		case PopupKind.ProjectSharing_EditEntry:
			DrawProjectSharingEditEntryPopup();
			break;
		case PopupKind.ProjectSharing_ChangeUsername:
			DrawProjectSharingChangeUsernamePopup();
			break;
	}
	}

		public static void OnMenuOpened()
		{
			activeMenuScreen = MenuScreen.Main;
			activePopup = PopupKind.None;
			selectedProjectIndex = -1;
			
		// Load patch notes data when menu opens
		if (patchNotesData == null)
		{
			// Force reload to get latest patch notes (remove after testing)
			PatchNotesLoader.ForceReload();
			patchNotesData = PatchNotesLoader.LoadPatchNotes();
		}
		}

		static void DrawMainScreen()
		{
			if (activePopup != PopupKind.None) return;

			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;

			#if UNITY_ANDROID || UNITY_IOS
			float buttonWidth = 40;
			const float buttonHeight = 0.5f;
			int buttonIndex = Seb.Vis.UI.UI.VerticalButtonGroup(menuButtonNames, theme.MainMenuButtonTheme, Seb.Vis.UI.UI.Centre + Vector2.up * (10f + buttonHeight * 0.5f), new Vector2(buttonWidth, buttonHeight), false, true, 1);
			#else
			float buttonWidth = 15;
			const float halfButtonOffset = 1f; // move group up by ~half a button height
			int buttonIndex = Seb.Vis.UI.UI.VerticalButtonGroup(menuButtonNames, theme.MainMenuButtonTheme, Seb.Vis.UI.UI.Centre + Vector2.up * (7f + halfButtonOffset), new Vector2(buttonWidth, 0), false, true, 1);
			#endif

			if (buttonIndex == 0 || KeyboardShortcuts.MainMenu_NewProjectShortcutTriggered) // New project
			{
				RefreshLoadedProjects();
				activePopup = PopupKind.NamePopup_NewProject;
				// Set default text for new project
				Seb.Vis.UI.UI.GetInputFieldState(ID_ProjectNameInput).SetText("TEST");
			}
			else if (buttonIndex == 1 || KeyboardShortcuts.MainMenu_OpenProjectShortcutTriggered) // Load project
			{
				RefreshLoadedProjects();
				selectedProjectIndex = -1;
				activeMenuScreen = MenuScreen.LoadProject;
			}
			else if (buttonIndex == 2) // Project Sharing
			{
				activeMenuScreen = MenuScreen.ProjectSharing;
			}
			else if (buttonIndex == 3 || KeyboardShortcuts.MainMenu_SettingsShortcutTriggered) // Settings
			{
				EditedAppSettings = Main.ActiveAppSettings;
				activeMenuScreen = MenuScreen.Settings;
				OnSettingsMenuOpened();
			}
			else if (buttonIndex == 4) // About
			{
				activeMenuScreen = MenuScreen.About;
			}
			else if (buttonIndex == 5 || KeyboardShortcuts.MainMenu_QuitShortcutTriggered) // Quit
			{
				Quit();
			}
		}

		static void DrawLoadProjectScreen()
		{
			const int backButtonIndex = 0;
			const int deleteButtonIndex = 1;
			const int duplicateButtonIndex = 2;
			const int renameButtonIndex = 3;
			const int openButtonIndex = 4;
			const int importButtonIndex = 5;
			const int exportButtonIndex = 6;
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;

			Vector2 pos = Seb.Vis.UI.UI.Centre + new Vector2(0, -1);
			#if UNITY_ANDROID || UNITY_IOS
			Vector2 size = new(80, 32);
			#else
			Vector2 size = new(68, 32);
			#endif


			Seb.Vis.UI.UI.DrawScrollView(ID_ProjectsScrollView, pos, size, Anchor.Centre, theme.ScrollTheme, loadProjectScrollViewDrawer);
			ButtonTheme buttonTheme = DrawSettings.ActiveUITheme.MainMenuButtonTheme;

			bool projectSelected = selectedProjectIndex >= 0 && selectedProjectIndex < allProjectDescriptions.Length;
			bool compatibleProject = projectSelected && projectCompatibilities[selectedProjectIndex].compatible;

			for (int i = 0; i < openProjectButtonStates.Length; i++)
			{
				bool buttonEnabled = activePopup == PopupKind.None &&
				(compatibleProject
	 			|| i == backButtonIndex
	 			|| i == importButtonIndex
	 			|| (i == deleteButtonIndex && projectSelected)
	 			|| (i == exportButtonIndex && projectSelected)); 
				openProjectButtonStates[i] = buttonEnabled;
			}

			Vector2 buttonRegionPos = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * DrawSettings.VerticalButtonSpacing;
			int buttonIndex = Seb.Vis.UI.UI.HorizontalButtonGroup(openProjectButtonNames, openProjectButtonStates, buttonTheme, buttonRegionPos, Seb.Vis.UI.UI.PrevBounds.Width, UILayoutHelper.DefaultSpacing, 0, Anchor.TopLeft);

			if (projectSelected && !compatibleProject)
			{
				Vector2 errorMessagePos = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * (DrawSettings.DefaultButtonSpacing * 2);
				Seb.Vis.UI.UI.DrawText(projectCompatibilities[selectedProjectIndex].message, buttonTheme.font, buttonTheme.fontSize, errorMessagePos, Anchor.TopLeft, Color.yellow);
			}

			// ---- Handle button input ----
			if (buttonIndex == backButtonIndex) BackToMain();
			else if (buttonIndex == deleteButtonIndex) activePopup = PopupKind.DeleteConfirmation;
			else if (buttonIndex == duplicateButtonIndex) activePopup = PopupKind.NamePopup_DuplicateProject;
			else if (buttonIndex == renameButtonIndex) activePopup = PopupKind.NamePopup_RenameProject;
			else if (buttonIndex == openButtonIndex) Main.CreateOrLoadProject(SelectedProjectName, string.Empty);
			else if (buttonIndex == importButtonIndex) Main.ImportProject();
			else if (buttonIndex == exportButtonIndex) Main.ExportProject(SelectedProjectName); 
		}
		
		public static void ExportProject(string projectName)
		{
			AndroidIO.ExportProjectToZip(projectName);
		}

	public static void ShowOverwriteConfirmationPopup(){
		activePopup = PopupKind.OverwriteConfirmation;
	}

	public static void ShowProjectCreationError(string errorMessage)
	{
		projectCreationErrorMessage = errorMessage;
		activePopup = PopupKind.ProjectCreationError;
	}

	public static void ShowProjectCreationError(string errorMessage, List<string> debugLogs)
	{
		projectCreationErrorMessage = errorMessage;
		projectCreationDebugLogs = debugLogs != null ? new List<string>(debugLogs) : new List<string>();
		activePopup = PopupKind.ProjectCreationError;
	}

		static bool ProjectNameValidator(string inputString) => inputString.Length <= 20 && !SaveUtils.NameContainsForbiddenChar(inputString);

		static void DrawAllProjectsInScrollView(Vector2 topLeft, float width, bool isLayoutPass)
		{
			float spacing = 0;
			bool enabled = activePopup == PopupKind.None;

			for (int i = 0; i < allProjectDescriptions.Length; i++)
			{
				ProjectDescription desc = allProjectDescriptions[i];
				bool selected = i == selectedProjectIndex;
				ButtonTheme buttonTheme = selected ? DrawSettings.ActiveUITheme.ProjectSelectionButtonSelected : DrawSettings.ActiveUITheme.ProjectSelectionButton;
				if (!projectCompatibilities[i].compatible) buttonTheme.textCols.normal.a = 0.5f;

				if (Seb.Vis.UI.UI.Button(desc.ProjectName, buttonTheme, topLeft, new Vector2(width, 0), enabled, false, true, buttonTheme.buttonCols,  Anchor.TopLeft))
				{
					selectedProjectIndex = i;
				}

				topLeft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * spacing;
			}
		}


		public static void RefreshLoadedProjects()
		{

			Debug.Log(SavePaths.ProjectsPath);
			allProjectDescriptions = Loader.LoadAllProjectDescriptions();
			allProjectNames = allProjectDescriptions.Select(d => d.ProjectName).ToArray();
			projectCompatibilities = allProjectDescriptions.Select(d => CanOpenProject(d)).ToArray();
		}

		static (bool canOpen, string failureReason) CanOpenProject(ProjectDescription projectDescription)
		{
			try
			{
				Main.Version earliestCompatible = Main.Version.Parse(projectDescription.DLSVersion_EarliestCompatible);
				Main.Version currentVersion = Main.DLSVersion;

				// In case project was made with a newer version of the sim, check if this version is able to open it
				bool canOpen = currentVersion.ToInt() >= earliestCompatible.ToInt();

				string failureReason = canOpen ? string.Empty : $"This project requires version {earliestCompatible} or later.";
				return (canOpen, failureReason);
			}
			catch
			{
				Debug.Log("Incompatible project: " + projectDescription.ProjectName);
				return (false, "Unrecognized project format");
			}
		}

	static void BackToMain()
	{
		// Invalidate Project Sharing username cache when leaving - user may have changed it elsewhere (e.g. level score popup)
		if (activeMenuScreen == MenuScreen.ProjectSharing)
		{
			_projectSharingLoggedInAsLoadRequested = false;
			_projectSharingAuthSyncRequested = false;
		}
		Seb.Vis.UI.UI.GetInputFieldState(ID_ProjectNameInput).ClearText();
		activeMenuScreen = MenuScreen.Main;
		activePopup = PopupKind.None;
		
		// Hide About menu logo GameObjects when leaving About screen
		if (AboutMenuUIController.Instance != null)
		{
			AboutMenuUIController.Instance.HideLogos();
		}
	}


		static void OnSettingsMenuOpened()
		{
			#if !UNITY_ANDROID && !UNITY_IOS
			// Desktop: Automatically select whichever resolution option is closest to current window size
			WheelSelectorState resolutionWheelState = Seb.Vis.UI.UI.GetWheelSelectorState(ID_DisplayResolutionWheel);
			int closestMatchError = int.MaxValue;
			for (int i = 0; i < Resolutions.Length; i++)
			{
				int matchError = Mathf.Min(Mathf.Abs(Screen.width - Resolutions[i].x), Mathf.Abs(Screen.height - Resolutions[i].y));
				if (matchError < closestMatchError)
				{
					closestMatchError = matchError;
					resolutionWheelState.index = i;
				}
			}
			#endif

			// Automatically set curr fullscreen mode
			WheelSelectorState fullscreenWheelState = Seb.Vis.UI.UI.GetWheelSelectorState(ID_FullscreenWheel);
			for (int i = 0; i < FullScreenModes.Length; i++)
			{
				if (Screen.fullScreenMode == FullScreenModes[i])
				{
					fullscreenWheelState.index = i;
					break;
				}
			}

			#if UNITY_ANDROID || UNITY_IOS
			// Automatically set curr orientation mode
			WheelSelectorState orientationWheelState = Seb.Vis.UI.UI.GetWheelSelectorState(ID_Orientation);
			if(Screen.orientation == ScreenOrientation.LandscapeLeft){
				orientationWheelState.index= 0;
			}else{
				orientationWheelState.index= 1;
			}

			WheelSelectorState UIScalingWheelState = Seb.Vis.UI.UI.GetWheelSelectorState(ID_UIScaling);
			UIScalingWheelState.index = EditedAppSettings.UIScaling;
			#endif
		}

		static void DrawSettingsScreen()
		{
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;

			float regionWidth = 40;
			Vector2 wheelSize = new(16, 2.5f);
			#if UNITY_ANDROID || UNITY_IOS	
			regionWidth = 70;
			wheelSize = new(40, 3.5f);
			#endif
			float labelOriginLeft = Seb.Vis.UI.UI.Centre.x - regionWidth / 2;
			float elementOriginRight = Seb.Vis.UI.UI.Centre.x + regionWidth / 2;
			Vector2 pos = new(labelOriginLeft, Seb.Vis.UI.UI.Centre.y+10);
			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				Draw.ID backgroundPanelID = Seb.Vis.UI.UI.ReservePanel();

				// -- Resolution --
				bool resEnabled = !EditedAppSettings.AutoResolution; //EditedAppSettings.fullscreenMode == FullScreenMode.Windowed;
				//Seb.Vis.UI.UI.DrawText("Resolution", theme.FontRegular, theme.FontSizeRegular, pos, Anchor.CentreLeft, Color.white);
				//string[] resNames = resEnabled ? ResolutionNames : FullScreenResName;
				//int resIndex = Seb.Vis.UI.UI.WheelSelector(ID_DisplayResolutionWheel, resNames, new Vector2(elementOriginRight, pos.y), wheelSize, theme.OptionsWheel, Anchor.CentreRight, enabled: resEnabled);
				//EditedAppSettings.ResolutionX = Resolutions[resIndex].x;
				//EditedAppSettings.ResolutionY = Resolutions[resIndex].y;

				//bool resEnabled = EditedAppSettings.fullscreenMode == FullScreenMode.Windowed;


				//#if !(UNITY_ANDROID || UNITY_IOS)
				// -- Full screen --
				Seb.Vis.UI.UI.DrawText("Fullscreen", theme.FontRegular, theme.FontSizeRegular, pos, Anchor.CentreLeft, Color.white);
				int fullScreenSettingIndex = Seb.Vis.UI.UI.WheelSelector(ID_FullscreenWheel, SettingsWheelFullScreenOptions, new Vector2(elementOriginRight, pos.y), wheelSize, theme.OptionsWheel, Anchor.CentreRight);
				if (fullScreenSettingIndex == 0){
					EditedAppSettings.AutoResolution = true;
					EditedAppSettings.fullscreenMode = FullScreenModes[1];
				} else {
					EditedAppSettings.AutoResolution = false;
					EditedAppSettings.fullscreenMode = FullScreenModes[fullScreenSettingIndex-1];
				}
				//#endif

				#if UNITY_ANDROID || UNITY_IOS
				pos += Vector2.down * 4;
				Seb.Vis.UI.UI.DrawText("Width", theme.FontRegular, theme.FontSizeRegular, pos, Anchor.CentreLeft, Color.white);
				string[] widthOptions = resEnabled ? WidthNames : WidthName;
				int widthIndex = Seb.Vis.UI.UI.WheelSelector(ID_DisplayWidthWheel, widthOptions, new Vector2(elementOriginRight, pos.y), wheelSize, theme.OptionsWheel, Anchor.CentreRight, enabled: resEnabled);
				EditedAppSettings.ResolutionX = WidthOptions[widthIndex];

				pos += Vector2.down * 4;
				Seb.Vis.UI.UI.DrawText("Height", theme.FontRegular, theme.FontSizeRegular, pos, Anchor.CentreLeft, Color.white);
				string[] heightOptions = resEnabled ? HeightNames : HeightName;
				int heightIndex = Seb.Vis.UI.UI.WheelSelector(ID_DisplayHeightWheel, heightOptions, new Vector2(elementOriginRight, pos.y), wheelSize, theme.OptionsWheel, Anchor.CentreRight, enabled: resEnabled);
				EditedAppSettings.ResolutionY = HeightOptions[heightIndex];
				#else
				// Desktop: Single Resolution setting like original PC version
				pos += Vector2.down * 4;
				Seb.Vis.UI.UI.DrawText("Resolution", theme.FontRegular, theme.FontSizeRegular, pos, Anchor.CentreLeft, Color.white);
				string[] resNames = resEnabled ? ResolutionNames : FullScreenResName;
				int resIndex = Seb.Vis.UI.UI.WheelSelector(ID_DisplayResolutionWheel, resNames, new Vector2(elementOriginRight, pos.y), wheelSize, theme.OptionsWheel, Anchor.CentreRight, enabled: resEnabled);
				EditedAppSettings.ResolutionX = Resolutions[resIndex].x;
				EditedAppSettings.ResolutionY = Resolutions[resIndex].y;
				#endif

				// -- Vsync --
				pos += Vector2.down * 4;
				Seb.Vis.UI.UI.DrawText("VSync", theme.FontRegular, theme.FontSizeRegular, pos, Anchor.CentreLeft, Color.white);
				int vsyncSetting = Seb.Vis.UI.UI.WheelSelector(EditedAppSettings.VSyncEnabled ? 1 : 0, SettingsWheelVSyncOptions, new Vector2(elementOriginRight, pos.y), wheelSize, theme.OptionsWheel, Anchor.CentreRight);
				EditedAppSettings.VSyncEnabled = vsyncSetting == 1;

				#if !UNITY_ANDROID && !UNITY_IOS
				// -- Discord Rich Presence (PC only) --
				pos += Vector2.down * 4;
				Seb.Vis.UI.UI.DrawText("Discord Integration", theme.FontRegular, theme.FontSizeRegular, pos, Anchor.CentreLeft, Color.white);
				int discordSetting = Seb.Vis.UI.UI.WheelSelector(EditedAppSettings.EnableDiscordRichPresence ? 1 : 0, SettingsWheelDiscordOptions, new Vector2(elementOriginRight, pos.y), wheelSize, theme.OptionsWheel, Anchor.CentreRight);
				bool newDiscordSetting = discordSetting == 1;
				if (newDiscordSetting != EditedAppSettings.EnableDiscordRichPresence)
				{
					EditedAppSettings.EnableDiscordRichPresence = newDiscordSetting;
					Debug.Log($"[Discord] Setting changed to: {(newDiscordSetting ? "ON" : "OFF")}");
					
					// Update Discord manager if it exists
					if (DLS.Integration.Discord.DiscordRichPresenceManager.Instance != null)
					{
						if (newDiscordSetting)
						{
							DLS.Integration.Discord.DiscordRichPresenceManager.Instance.Enable();
						}
						else
						{
							DLS.Integration.Discord.DiscordRichPresenceManager.Instance.Disable();
						}
					}
				}
				#endif

				#if UNITY_ANDROID || UNITY_IOS
				pos += Vector2.down * 4;
				Seb.Vis.UI.UI.DrawText("Orientation", theme.FontRegular, theme.FontSizeRegular, pos, Anchor.CentreLeft, Color.white);
				int orientation = Seb.Vis.UI.UI.WheelSelector(ID_Orientation, SettingsWheelOrientationOptions, new Vector2(elementOriginRight, pos.y), wheelSize, theme.OptionsWheel, Anchor.CentreRight);
				EditedAppSettings.orientationIsLeftLandscape = orientation==0;

				pos += Vector2.down * 4;
				Seb.Vis.UI.UI.DrawText("Hotbar scrolling", theme.FontRegular, theme.FontSizeRegular, pos, Anchor.CentreLeft, Color.white);
				EditedAppSettings.showScrollingButtons = Seb.Vis.UI.UI.WheelSelector(ID_ShowScrollButtons, SettingsWheelBottomBarScrollingOptions, new Vector2(elementOriginRight, pos.y), wheelSize, theme.OptionsWheel, Anchor.CentreRight);
				
				pos += Vector2.down * 4;
				Seb.Vis.UI.UI.DrawText("UI Scaling", theme.FontRegular, theme.FontSizeRegular, pos, Anchor.CentreLeft, Color.white);
				EditedAppSettings.UIScaling = Seb.Vis.UI.UI.WheelSelector(ID_UIScaling, SettingsWheelUIScalingOptions, new Vector2(elementOriginRight, pos.y), wheelSize, theme.OptionsWheel, Anchor.CentreRight);
				#endif
				
				// Background panel
				Seb.Vis.UI.UI.ModifyPanel(backgroundPanelID, Seb.Vis.UI.UI.GetCurrentBoundsScope().Centre, Seb.Vis.UI.UI.GetCurrentBoundsScope().Size + Vector2.one * 3, ColHelper.MakeCol255(37, 37, 43));
			}

			Vector2 buttonPos = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * DrawSettings.VerticalButtonSpacing;
			settingsButtonGroupStates[0] = true;
			settingsButtonGroupStates[1] = true;

			int buttonIndex = Seb.Vis.UI.UI.HorizontalButtonGroup(settingsButtonGroupNames, settingsButtonGroupStates, theme.MainMenuButtonTheme, buttonPos, Seb.Vis.UI.UI.PrevBounds.Width, UILayoutHelper.DefaultSpacing, 0, Anchor.TopLeft);

			if (buttonIndex == 0)
			{
				BackToMain();
			}
			else if (buttonIndex == 1)
			{
				Main.SaveAndApplyAppSettings(EditedAppSettings);
				//DrawSettingsScreen();
				//UIDrawer.Draw();
			}
		}

		static void DrawNamePopup()
		{
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;

			Seb.Vis.UI.UI.StartNewLayer();
			Seb.Vis.UI.UI.DrawFullscreenPanel(theme.MenuBackgroundOverlayCol);

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();

				InputFieldTheme inputTheme = theme.ChipNameInputField;

				Vector2 charSize = Seb.Vis.UI.UI.CalculateTextSize("M", inputTheme.fontSize, inputTheme.font);
				Vector2 padding = new(2, 2);
				Vector2 inputFieldSize = new Vector2(charSize.x * MaxProjectNameLength, charSize.y) + padding * 2;


				InputFieldState state = Seb.Vis.UI.UI.InputField(ID_ProjectNameInput, inputTheme, Seb.Vis.UI.UI.Centre, inputFieldSize, "", Anchor.Centre, padding.x, projectNameValidator, true);

				string projectName = state.text;
				bool validProjectName = !string.IsNullOrWhiteSpace(projectName) && SaveUtils.ValidFileName(projectName);
				bool projectNameAlreadyExists = false;
				foreach (string existingProjectName in allProjectNames)
				{
					projectNameAlreadyExists |= string.Equals(projectName, existingProjectName, StringComparison.CurrentCultureIgnoreCase);
				}

				bool canCreateProject = validProjectName && !projectNameAlreadyExists;

				Vector2 buttonsRegionSize = new(inputFieldSize.x, 5);
				Vector2 buttonsRegionCentre = UILayoutHelper.CalculateCentre(Seb.Vis.UI.UI.PrevBounds.BottomLeft, buttonsRegionSize, Anchor.TopLeft);
				(Vector2 size, Vector2 centre) layoutCancel = UILayoutHelper.HorizontalLayout(2, 0, buttonsRegionCentre, buttonsRegionSize);
				(Vector2 size, Vector2 centre) layoutConfirm = UILayoutHelper.HorizontalLayout(2, 1, buttonsRegionCentre, buttonsRegionSize);

				bool cancelButton = Seb.Vis.UI.UI.Button("CANCEL", theme.MainMenuButtonTheme, layoutCancel.centre, new Vector2(layoutCancel.size.x, 0), true, false, true, theme.ButtonTheme.buttonCols);
				bool confirmButton = Seb.Vis.UI.UI.Button("CONFIRM", theme.MainMenuButtonTheme, layoutConfirm.centre, new Vector2(layoutConfirm.size.x, 0), canCreateProject, false, true,theme.ButtonTheme.buttonCols);

				if (cancelButton || KeyboardShortcuts.CancelShortcutTriggered)
				{
					state.ClearText();
					activePopup = PopupKind.None;
				}

				if (confirmButton || KeyboardShortcuts.ConfirmShortcutTriggered)
				{
					state.ClearText();
					PopupKind kind = activePopup;
					activePopup = PopupKind.None;
					OnNamePopupConfirmed(kind, projectName);
				}

				Seb.Vis.UI.UI.ModifyPanel(panelID, Seb.Vis.UI.UI.GetCurrentBoundsScope().Centre, Seb.Vis.UI.UI.GetCurrentBoundsScope().Size + Vector2.one * 2, ColHelper.MakeCol255(37, 37, 43));
			}
		}

		static void OnNamePopupConfirmed(PopupKind kind, string name)
		{
			if (kind is PopupKind.NamePopup_RenameProject or PopupKind.NamePopup_DuplicateProject)
			{
				if (kind is PopupKind.NamePopup_RenameProject) Saver.RenameProject(SelectedProjectName, name);
				if (kind is PopupKind.NamePopup_DuplicateProject) Saver.DuplicateProject(SelectedProjectName, name);

				RefreshLoadedProjects();
				selectedProjectIndex = 0; // the modified project will now be at top of list
				Seb.Vis.UI.UI.GetScrollbarState(ID_ProjectsScrollView).scrollY = 0; // scroll to top so selection is visible
			}
			else if (kind is PopupKind.NamePopup_NewProject)
			{
				Main.CreateOrLoadProject(name);
			}
		}
		static void DrawOverwriteProjectConfirmationPopup()
		{
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;

			Seb.Vis.UI.UI.StartNewLayer();
			Seb.Vis.UI.UI.DrawFullscreenPanel(theme.MenuBackgroundOverlayCol);

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();
				Seb.Vis.UI.UI.DrawText("Project name already exist. Are you sure you want to overwrite?", theme.FontRegular, theme.FontSizeRegular, Seb.Vis.UI.UI.Centre, Anchor.Centre, Color.yellow);

				Vector2 buttonRegionTopLeft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * DrawSettings.VerticalButtonSpacing;
				float buttonRegionWidth = Seb.Vis.UI.UI.PrevBounds.Width;
				int buttonIndex = Seb.Vis.UI.UI.HorizontalButtonGroup(new[] { "CANCEL", "OVERWRITE" }, theme.MainMenuButtonTheme, buttonRegionTopLeft, buttonRegionWidth, DrawSettings.HorizontalButtonSpacing, 0, Anchor.TopLeft);
				Seb.Vis.UI.UI.ModifyPanel(panelID, Seb.Vis.UI.UI.GetCurrentBoundsScope().Centre, Seb.Vis.UI.UI.GetCurrentBoundsScope().Size + Vector2.one * 2, ColHelper.MakeCol255(37, 37, 43));

				if (buttonIndex == 0) // Cancel
				{
					activePopup = PopupKind.None;

				}
				else if (buttonIndex == 1) 
				{
					Saver.FinishImport();
					selectedProjectIndex = -1;
					RefreshLoadedProjects();
					activePopup = PopupKind.None;
				}
			}
		}

	static void DrawDeleteProjectConfirmationPopup()
	{
		DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;

		Seb.Vis.UI.UI.StartNewLayer();
		Seb.Vis.UI.UI.DrawFullscreenPanel(theme.MenuBackgroundOverlayCol);

		using (Seb.Vis.UI.UI.BeginBoundsScope(true))
		{
			Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();
			Seb.Vis.UI.UI.DrawText("Are you sure you want to delete this project?", theme.FontRegular, theme.FontSizeRegular, Seb.Vis.UI.UI.Centre, Anchor.Centre, Color.yellow);

			Vector2 buttonRegionTopLeft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * DrawSettings.VerticalButtonSpacing;
			float buttonRegionWidth = Seb.Vis.UI.UI.PrevBounds.Width;
			int buttonIndex = Seb.Vis.UI.UI.HorizontalButtonGroup(new[] { "CANCEL", "DELETE" }, theme.MainMenuButtonTheme, buttonRegionTopLeft, buttonRegionWidth, DrawSettings.HorizontalButtonSpacing, 0, Anchor.TopLeft);
			Seb.Vis.UI.UI.ModifyPanel(panelID, Seb.Vis.UI.UI.GetCurrentBoundsScope().Centre, Seb.Vis.UI.UI.GetCurrentBoundsScope().Size + Vector2.one * 2, ColHelper.MakeCol255(37, 37, 43));

			if (buttonIndex == 0) // Cancel
			{
				activePopup = PopupKind.None;
			}
			else if (buttonIndex == 1) // Delete
			{
				Saver.DeleteProject(SelectedProjectName);
				selectedProjectIndex = -1;
				RefreshLoadedProjects();
				activePopup = PopupKind.None;
			}
		}
	}

	static void DrawProjectCreationErrorPopup()
	{
		DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;

		Seb.Vis.UI.UI.StartNewLayer();
		Seb.Vis.UI.UI.DrawFullscreenPanel(theme.MenuBackgroundOverlayCol);

		using (Seb.Vis.UI.UI.BeginBoundsScope(true))
		{
			Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();
			
			#if UNITY_ANDROID || UNITY_IOS
			Vector2 popupSize = new(70, 40);
			#else
			Vector2 popupSize = new(60, 35);
			#endif
			
			Vector2 pos = Seb.Vis.UI.UI.Centre;
			
		// Error message at top
		string displayMessage = "Failed to create/open project:\n" + projectCreationErrorMessage;
		Seb.Vis.UI.UI.DrawText(displayMessage, theme.FontRegular, theme.FontSizeRegular * 0.7f, pos + Vector2.up * (popupSize.y / 2 - 3), Anchor.CentreTop, Color.red);
			
			// Debug logs section
			if (projectCreationDebugLogs != null && projectCreationDebugLogs.Count > 0)
			{
				Vector2 logPos = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * 2;
				Seb.Vis.UI.UI.DrawText("Debug Logs:", theme.FontRegular, theme.FontSizeRegular * 0.6f, logPos, Anchor.TopLeft, Color.yellow);
				
				Vector2 scrollViewPos = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * 1;
				Vector2 scrollViewSize = new(popupSize.x - 4, 20);
				
				Seb.Vis.UI.UI.DrawScrollView(ID_ErrorLogsScrollView, scrollViewPos, scrollViewSize, Anchor.TopLeft, theme.ScrollTheme, (topLeft, width, isLayoutPass) =>
				{
					float spacing = 0.5f;
					// Calculate max characters per line based on available width
					int maxCharsPerLine = (int)(width / (theme.FontSizeRegular * 0.5f * 0.6f));
					
					foreach (string log in projectCreationDebugLogs)
					{
						Color logColor = Color.white;
						if (log.Contains("Error") || log.Contains("Exception"))
							logColor = new Color(1f, 0.3f, 0.3f);
						else if (log.Contains("null"))
							logColor = new Color(1f, 0.7f, 0.3f);
						
						// Wrap text to fit within the scroll view width
						string wrappedLog = WrapText(log, maxCharsPerLine);
						Seb.Vis.UI.UI.DrawText(wrappedLog, theme.FontRegular, theme.FontSizeRegular * 0.5f, topLeft, Anchor.TopLeft, logColor);
						topLeft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * spacing;
					}
				});
			}

			Vector2 buttonPos = pos + Vector2.down * (popupSize.y / 2 - 3);
			
			if (Seb.Vis.UI.UI.Button("OK", theme.MainMenuButtonTheme, buttonPos, Vector2.zero, true, true, true, theme.MainMenuButtonTheme.buttonCols) || KeyboardShortcuts.CancelShortcutTriggered)
			{
				activePopup = PopupKind.None;
				projectCreationErrorMessage = "";
				projectCreationDebugLogs.Clear();
			}
			
			Seb.Vis.UI.UI.ModifyPanel(panelID, Seb.Vis.UI.UI.GetCurrentBoundsScope().Centre, Seb.Vis.UI.UI.GetCurrentBoundsScope().Size + Vector2.one * 2, ColHelper.MakeCol255(37, 37, 43));
		}
	}

		// Project Sharing layout - screen-relative (fractions of Width/Height for responsive mobile)
		const float ProjectSharing_ButtonWidthFrac = 0.55f;
		const float ProjectSharing_ButtonGroupOffsetFrac = 0.14f;
		const float ProjectSharing_LoggedInOffsetFrac = 0.26f;
		// Popup layout
		const float Popup_TitleOffsetFrac = 0.18f;
		const float Popup_ScrollOffsetFrac = 0.12f;
		const float Popup_ScrollWidthFrac = 0.52f;
		const float Popup_ScrollHeightFrac = 0.28f;
		const float ProjectSharing_ListPopupContentMinFrac = 0.2f;
		const float ProjectSharing_ListPopupContentMaxFrac = 0.8f;
		const float ProjectSharing_ListPopupContentSpanFrac = ProjectSharing_ListPopupContentMaxFrac - ProjectSharing_ListPopupContentMinFrac;
		const float ProjectSharing_ListPopupWidthFrac = 0.8f;
		const float ProjectSharing_ListPopupHeightFrac = ProjectSharing_ListPopupContentSpanFrac;
		const float ProjectSharing_ListPopupBottomExtensionFrac = 0.08f + (ProjectSharing_ListPopupButtonsTopGapFrac - Popup_SpacingFrac);
		const float ProjectSharing_ListPopupPanelPadding = 2f;
		const float ProjectSharing_ListPopupTitleTopInsetFrac = 0.02f;
		const float ProjectSharing_ListPopupFilterTopInsetFrac = 0.095f;
		const float ProjectSharing_ListPopupScrollTopInsetFrac = 0.14f;
		const float ProjectSharing_ListPopupScrollHeightFrac = 0.46f;
		const float ProjectSharing_ListPopupButtonsTopGapFrac = 0.06f;
		const float ProjectSharing_ListPopupTitleFontScale = 1f;
		const float ProjectSharing_ListPopupItemFontScale = 0.9f;
		const float ProjectSharing_ListPopupWheelFontScale = 0.9f;
		const float Popup_SpacingFrac = 0.035f;
		const float Popup_RowHeightFrac = 0.028f;
		const float Popup_ProjectRowGapFrac = 0.012f;
		const float Popup_SettingsToButtonsGapFrac = 0.022f;

		static void DrawProjectSharingScreen()
		{
			if (activePopup != PopupKind.None) return;

			// Sync with actual Firebase auth when showing "not signed in" - user may have logged in elsewhere (e.g. level upload)
			if (!_projectSharingSignedIn && !_projectSharingAuthSyncRequested)
			{
				_projectSharingAuthSyncRequested = true;
				_ = ProjectSharingSyncAuthStateAsync();
			}

			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			float w = Seb.Vis.UI.UI.Width;
			float h = Seb.Vis.UI.UI.Height;

			float buttonWidth = w * ProjectSharing_ButtonWidthFrac;
			Vector2 buttonGroupPos = Seb.Vis.UI.UI.Centre + Vector2.up * (h * ProjectSharing_ButtonGroupOffsetFrac);

			if (!_projectSharingSignedIn)
			{
				// Auth choice: Create Account, Login, Sign in as Guest
				int authButtonIndex = Seb.Vis.UI.UI.VerticalButtonGroup(projectSharingAuthButtonNames, theme.MainMenuButtonTheme, buttonGroupPos, new Vector2(buttonWidth, 0.5f), false, true, 1);

				if (authButtonIndex == 0)
				{
					_projectSharingAuthError = "";
					_projectSharingCreateAccountInitialFocusDone = false;
					Seb.Vis.UI.UI.GetInputFieldState(ID_ProjectSharing_CreateAccountEmail).SetText("user@example.com");
					Seb.Vis.UI.UI.GetInputFieldState(ID_ProjectSharing_CreateAccountUsername).SetText("");
					activePopup = PopupKind.ProjectSharing_CreateAccount;
					_ = ProjectSharingPreloadCreateAccountProfileAsync();
				}
				else if (authButtonIndex == 1)
				{
					_projectSharingAuthError = "";
					_projectSharingLoginInitialFocusDone = false;
					Seb.Vis.UI.UI.GetInputFieldState(ID_ProjectSharing_LoginEmail).SetText("user@example.com");
					activePopup = PopupKind.ProjectSharing_Login;
				}
				else if (authButtonIndex == 2)
					_ = ProjectSharingSignInAsGuestAsync();

				if (!string.IsNullOrEmpty(_projectSharingAuthError))
				{
					Vector2 errPos = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * 2;
					Seb.Vis.UI.UI.DrawText(_projectSharingAuthError, theme.FontRegular, theme.FontSizeRegular * 0.7f, errPos, Anchor.TopLeft, Color.red);
				}
			}
			else
			{
				// Load username from UserProfile when first showing signed-in state
				if (!_projectSharingLoggedInAsLoadRequested)
				{
					_projectSharingLoggedInAsLoadRequested = true;
					_ = ProjectSharingLoadLoggedInAsAsync();
				}
				string loggedInAs = !string.IsNullOrEmpty(_projectSharingLoggedInAs) ? _projectSharingLoggedInAs : "Guest";
				Vector2 loggedInPos = Seb.Vis.UI.UI.Centre + Vector2.up * (h * ProjectSharing_LoggedInOffsetFrac);
				Seb.Vis.UI.UI.DrawText($"Logged in as {loggedInAs}", theme.FontRegular, theme.FontSizeRegular, loggedInPos, Anchor.CentreTop, Color.white);

				// Main content: Export, Import, My projects, (Change username when not guest), Log Out
				string[] mainButtons = _projectSharingIsGuest ? projectSharingMainButtonNamesGuest : projectSharingMainButtonNames;
				int mainButtonIndex = Seb.Vis.UI.UI.VerticalButtonGroup(mainButtons, theme.MainMenuButtonTheme, buttonGroupPos, new Vector2(buttonWidth, 0.5f), false, true, 1);

				if (mainButtonIndex == 0)
					ProjectSharingOpenExport();
				else if (mainButtonIndex == 1)
					ProjectSharingOpenImport();
				else if (mainButtonIndex == 2)
					ProjectSharingOpenMyProjects();
				else if (mainButtonIndex == 3 && !_projectSharingIsGuest)
				{
					_projectSharingChangeUsernameInitialized = false;
					_projectSharingChangeUsernameShowConfirm = false;
					_projectSharingChangeUsernameError = "";
					_projectSharingChangeUsernameInProgress = false;
					activePopup = PopupKind.ProjectSharing_ChangeUsername;
					_ = ProjectSharingChangeUsernamePreloadProfileAsync();
				}
				else if (mainButtonIndex == (_projectSharingIsGuest ? 3 : 4))
					ProjectSharingLogOut();
			}

			// Back button: position below the last button with same spacing as between other buttons
			Vector2 backButtonPos = new Vector2(Seb.Vis.UI.UI.Centre.x, Seb.Vis.UI.UI.PrevBounds.Bottom - DrawSettings.VerticalButtonSpacing);
			if (Seb.Vis.UI.UI.Button("Back", theme.MainMenuButtonTheme, backButtonPos, Vector2.zero, true, true, true, theme.MainMenuButtonTheme.buttonCols, Anchor.CentreTop))
				BackToMain();
		}

		static async Task ProjectSharingSyncAuthStateAsync()
		{
			try
			{
				// Delay before Firebase init on Windows build to reduce uWS crash (firebase-unity-sdk#1291).
				// Skip delay in Editor for faster testing.
				if (!Application.isEditor)
					await Task.Delay(2000);
				await FirebaseBootstrap.InitializeAsync();
				var user = FirebaseAuth.DefaultInstance?.CurrentUser;
				if (user != null && !user.IsAnonymous)
				{
					_projectSharingSignedIn = true;
					_projectSharingIsGuest = false;
				}
			}
			catch { /* ignore - user will see auth buttons */ }
		}

		static async Task ProjectSharingSignInAsGuestAsync()
		{
			_projectSharingAuthError = "";
			_projectSharingIsGuest = true;
			try
			{
				await FirebaseBootstrap.InitializeAsync();
				if (FirebaseBootstrap.IsInitialized && FirebaseBootstrap.UserId != "anon")
					_projectSharingSignedIn = true;
				else
					_projectSharingSignedIn = true; // still show main content for anon
			}
			catch (Exception ex)
			{
				_projectSharingAuthError = ex.Message;
				Debug.LogError($"[ProjectSharing] Sign in as guest failed: {ex.Message}");
			}
		}

		static void ProjectSharingOpenExport()
		{
			RefreshLoadedProjects();
			if (allProjectDescriptions.Length == 0)
			{
				_projectSharingAuthError = "No projects to upload.";
				return;
			}
			_projectSharingUploadSelectedIndex = 0;
			_projectSharingUploadIsPublic = true;
			_projectSharingUploadInProgress = false;
			_projectSharingUploadStatus = "";
			activePopup = PopupKind.ProjectSharing_UploadConfirm;
		}

		static void ProjectSharingOpenImport()
		{
			_projectSharingLibraryEntries = null;
			_projectSharingLibraryLoading = true;
			_projectSharingImportStatus = "Loading...";
			_projectSharingImportSelectedEntry = null;
			activePopup = PopupKind.ProjectSharing_ImportList;
			_ = ProjectSharingLoadLibraryAsync();
		}

		static async Task ProjectSharingOpenUploadOptionsAsync()
		{
			_projectSharingUploadProjectDisplayNameDefault = _projectSharingUploadPendingProjectName;
			try
			{
				var existing = await LibraryService.GetExistingEntryByProjectNameAsync(_projectSharingUploadPendingProjectName);
				if (existing != null && !string.IsNullOrEmpty(existing.projectDisplayName))
					_projectSharingUploadProjectDisplayNameDefault = existing.projectDisplayName;
			}
			catch { /* use project name */ }
			_projectSharingUploadProjectDisplayNameInitialized = false;
			activePopup = PopupKind.ProjectSharing_UploadDisplayName;
		}

		static async Task ProjectSharingLoadMyProjectsAsync()
		{
			try
			{
				var entries = await LibraryService.GetEntriesAsync(LibraryService.LibraryFilterMode.Private, LibraryService.LibrarySortOrder.Newest, 100);
				if (_projectSharingMyProjectsFilter == LibraryService.LibraryFilterMode.Public)
					entries = entries.Where(e => e.isPublic).ToList();
				else if (_projectSharingMyProjectsFilter == LibraryService.LibraryFilterMode.Private)
					entries = entries.Where(e => !e.isPublic).ToList();
				_projectSharingMyProjectsEntries = entries;
				// Clear selection if it's no longer in the list
				if (_projectSharingMyProjectsSelectedEntry != null && !entries.Any(e => e.id == _projectSharingMyProjectsSelectedEntry.id))
					_projectSharingMyProjectsSelectedEntry = null;
			}
			catch (Exception ex)
			{
				_projectSharingMyProjectsEntries = new List<LibraryService.LibraryEntry>();
				_projectSharingMyProjectsSelectedEntry = null;
				Debug.LogWarning($"[ProjectSharing] Load my projects failed: {ex.Message}");
			}
			_projectSharingMyProjectsLoading = false;
		}

		static void ProjectSharingOpenMyProjects()
		{
			_projectSharingMyProjectsEntries = null;
			_projectSharingMyProjectsLoading = true;
			_projectSharingMyProjectsSelectedEntry = null;
			activePopup = PopupKind.ProjectSharing_MyProjects;
			_ = ProjectSharingLoadMyProjectsAsync();
		}

		static async Task ProjectSharingLoadLibraryAsync()
		{
			try
			{
				var entries = await LibraryService.GetEntriesAsync(_projectSharingImportFilter, _projectSharingImportSortOrder, 50);
				_projectSharingLibraryEntries = entries;
				_projectSharingImportStatus = entries.Count == 0
					? (_projectSharingImportFilter == LibraryService.LibraryFilterMode.Private ? "No projects of yours found." : _projectSharingImportFilter == LibraryService.LibraryFilterMode.Public ? "No public projects found." : "No projects found.")
					: "";
				if (_projectSharingImportSelectedEntry != null && !entries.Any(e => e.id == _projectSharingImportSelectedEntry.id))
					_projectSharingImportSelectedEntry = null;
			}
			catch (Exception ex)
			{
				_projectSharingImportStatus = "Error: " + ex.Message;
				_projectSharingLibraryEntries = new List<LibraryService.LibraryEntry>();
				_projectSharingImportSelectedEntry = null;
			}
			_projectSharingLibraryLoading = false;
		}

		static void ProjectSharingLogOut()
		{
			try
			{
				FirebaseAuth.DefaultInstance?.SignOut();
				FirebaseBootstrap.ResetAfterSignOut();
				UserAuthService.ClearCache();
			}
			catch { /* ignore */ }
			_projectSharingSignedIn = false;
			_projectSharingIsGuest = false;
			_projectSharingAuthError = "";
			_projectSharingLoggedInAs = null;
			_projectSharingLoggedInAsLoadRequested = false;
		}

		static async Task ProjectSharingLoadLoggedInAsAsync()
		{
			try
			{
				_projectSharingLoggedInAs = await LibraryService.GetCurrentUserAuthorNameAsync();
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.LogWarning($"[MainMenu] Failed to load logged-in username: {ex.Message}");
				_projectSharingLoggedInAs = "Guest";
			}
		}

		static async Task ProjectSharingChangeUsernamePreloadProfileAsync()
		{
			try
			{
				var profile = await UserAuthService.GetCurrentUserProfileAsync();
				_projectSharingChangeUsernameHasUsername = profile != null && !string.IsNullOrEmpty(profile.username);
				_projectSharingChangeUsernameOriginal = _projectSharingChangeUsernameHasUsername ? profile.username : "";
				// Update input field when profile loads (may run before or after first Draw)
				Seb.Vis.UI.UI.GetInputFieldState(ID_ProjectSharing_ChangeUsername).SetText(_projectSharingChangeUsernameOriginal ?? "");
			}
			catch
			{
				_projectSharingChangeUsernameHasUsername = false;
				_projectSharingChangeUsernameOriginal = "";
			}
		}

		/// <summary>
		/// Pre-fills the Create Account username field from levels profile (if user has one).
		/// </summary>
		static async Task ProjectSharingPreloadCreateAccountProfileAsync()
		{
			try
			{
				await FirebaseBootstrap.InitializeAsync();
				if (!FirebaseBootstrap.IsInitialized || FirebaseBootstrap.UserId == "anon") return;
				var profile = await UserAuthService.GetCurrentUserProfileAsync();
				if (profile != null && !string.IsNullOrEmpty(profile.username))
					Seb.Vis.UI.UI.GetInputFieldState(ID_ProjectSharing_CreateAccountUsername).SetText(profile.username);
			}
			catch { /* ignore - pre-fill is best-effort */ }
		}

		static void DrawProjectSharingCreateAccountPopup()
		{
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			Seb.Vis.UI.UI.StartNewLayer();
			Seb.Vis.UI.UI.DrawFullscreenPanel(theme.MenuBackgroundOverlayCol);

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();
				InputFieldTheme inputTheme = theme.ChipNameInputField;
				inputTheme.fontSize = theme.FontSizeRegular;
				Vector2 centre = Seb.Vis.UI.UI.Centre;

				Seb.Vis.UI.UI.DrawText("Create Account", theme.FontRegular, theme.FontSizeRegular * 1.1f, centre + Vector2.up * 12, Anchor.CentreTop, Color.white);
				Vector2 pos = centre + Vector2.up * 8;

				Seb.Vis.UI.UI.DrawText("Email", theme.FontRegular, theme.FontSizeRegular * 0.8f, pos, Anchor.CentreTop, Color.white);
				pos += Vector2.down * 1.5f;
				bool createAccountForceEmailFocus = !_projectSharingCreateAccountInitialFocusDone;
				var emailState = Seb.Vis.UI.UI.InputField(ID_ProjectSharing_CreateAccountEmail, inputTheme, pos, new Vector2(30, 3), "user@example.com", Anchor.CentreTop, 1f, null, createAccountForceEmailFocus);
				_projectSharingCreateAccountInitialFocusDone = true;
				pos += Vector2.down * 4;

				Seb.Vis.UI.UI.DrawText("Password", theme.FontRegular, theme.FontSizeRegular * 0.8f, pos, Anchor.CentreTop, Color.white);
				pos += Vector2.down * 1.5f;
				var passState = Seb.Vis.UI.UI.InputField(ID_ProjectSharing_CreateAccountPassword, inputTheme, pos, new Vector2(30, 3), "", Anchor.CentreTop, 1f, null, false);
				pos += Vector2.down * 4;

				Seb.Vis.UI.UI.DrawText("Confirm Password", theme.FontRegular, theme.FontSizeRegular * 0.8f, pos, Anchor.CentreTop, Color.white);
				pos += Vector2.down * 1.5f;
				var confirmState = Seb.Vis.UI.UI.InputField(ID_ProjectSharing_CreateAccountConfirm, inputTheme, pos, new Vector2(30, 3), "", Anchor.CentreTop, 1f, null, false);
				pos += Vector2.down * 4;

				Seb.Vis.UI.UI.DrawText("Username (optional)", theme.FontRegular, theme.FontSizeRegular * 0.8f, pos, Anchor.CentreTop, Color.white);
				pos += Vector2.down * 1.5f;
				var usernameState = Seb.Vis.UI.UI.InputField(ID_ProjectSharing_CreateAccountUsername, inputTheme, pos, new Vector2(30, 3), "", Anchor.CentreTop, 1f, null, false);
				pos += Vector2.down * 5;

				if (!string.IsNullOrEmpty(_projectSharingAuthError))
					Seb.Vis.UI.UI.DrawText(_projectSharingAuthError, theme.FontRegular, theme.FontSizeRegular * 0.7f, pos, Anchor.CentreTop, Color.red);
				pos += Vector2.down * 3;

				int btn = Seb.Vis.UI.UI.HorizontalButtonGroup(new[] { "CANCEL", "CREATE" }, theme.MainMenuButtonTheme, pos, 40, UILayoutHelper.DefaultSpacing, 0, Anchor.CentreTop);

				if (btn == 0 || KeyboardShortcuts.CancelShortcutTriggered)
				{
					activePopup = PopupKind.None;
					_projectSharingAuthError = "";
				}
				else if (btn == 1)
				{
					var email = emailState.text?.Trim() ?? "";
					var password = passState.text ?? "";
					var confirm = confirmState.text ?? "";
					var username = usernameState.text?.Trim() ?? "";
					if (string.IsNullOrEmpty(email)) _projectSharingAuthError = "Email required.";
					else if (password.Length < 6) _projectSharingAuthError = "Password must be at least 6 characters.";
					else if (password != confirm) _projectSharingAuthError = "Passwords do not match.";
					else
						_ = ProjectSharingCreateAccountAsync(email, password, username);
				}

				Seb.Vis.UI.UI.ModifyPanel(panelID, Seb.Vis.UI.UI.GetCurrentBoundsScope().Centre, Seb.Vis.UI.UI.GetCurrentBoundsScope().Size + Vector2.one * 2, ColHelper.MakeCol255(37, 37, 43));
			}
		}

		static void DrawProjectSharingLoginPopup()
		{
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			Seb.Vis.UI.UI.StartNewLayer();
			Seb.Vis.UI.UI.DrawFullscreenPanel(theme.MenuBackgroundOverlayCol);

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();
				InputFieldTheme inputTheme = theme.ChipNameInputField;
				inputTheme.fontSize = theme.FontSizeRegular;
				Vector2 centre = Seb.Vis.UI.UI.Centre;

				Seb.Vis.UI.UI.DrawText("Login", theme.FontRegular, theme.FontSizeRegular * 1.1f, centre + Vector2.up * 12, Anchor.CentreTop, Color.white);
				Vector2 pos = centre + Vector2.up * 8;

				Seb.Vis.UI.UI.DrawText("Email", theme.FontRegular, theme.FontSizeRegular * 0.8f, pos, Anchor.CentreTop, Color.white);
				pos += Vector2.down * 1.5f;
				bool loginForceEmailFocus = !_projectSharingLoginInitialFocusDone;
				var emailState = Seb.Vis.UI.UI.InputField(ID_ProjectSharing_LoginEmail, inputTheme, pos, new Vector2(30, 3), "", Anchor.CentreTop, 1f, null, loginForceEmailFocus);
				_projectSharingLoginInitialFocusDone = true;
				pos += Vector2.down * 4;

				Seb.Vis.UI.UI.DrawText("Password", theme.FontRegular, theme.FontSizeRegular * 0.8f, pos, Anchor.CentreTop, Color.white);
				pos += Vector2.down * 1.5f;
				var passState = Seb.Vis.UI.UI.InputField(ID_ProjectSharing_LoginPassword, inputTheme, pos, new Vector2(30, 3), "", Anchor.CentreTop, 1f, null, false);
				pos += Vector2.down * 5;

				if (!string.IsNullOrEmpty(_projectSharingAuthError))
					Seb.Vis.UI.UI.DrawText(_projectSharingAuthError, theme.FontRegular, theme.FontSizeRegular * 0.7f, pos, Anchor.CentreTop, Color.red);
				pos += Vector2.down * 3;

				int btn = Seb.Vis.UI.UI.HorizontalButtonGroup(new[] { "CANCEL", "LOGIN" }, theme.MainMenuButtonTheme, pos, 40, UILayoutHelper.DefaultSpacing, 0, Anchor.CentreTop);

				if (btn == 0 || KeyboardShortcuts.CancelShortcutTriggered)
				{
					activePopup = PopupKind.None;
					_projectSharingAuthError = "";
				}
				else if (btn == 1)
				{
					var email = emailState.text?.Trim() ?? "";
					var password = passState.text ?? "";
					if (string.IsNullOrEmpty(email)) _projectSharingAuthError = "Email required.";
					else if (string.IsNullOrEmpty(password)) _projectSharingAuthError = "Password required.";
					else
						_ = ProjectSharingLoginAsync(email, password);
				}

				Seb.Vis.UI.UI.ModifyPanel(panelID, Seb.Vis.UI.UI.GetCurrentBoundsScope().Centre, Seb.Vis.UI.UI.GetCurrentBoundsScope().Size + Vector2.one * 2, ColHelper.MakeCol255(37, 37, 43));
			}
		}

		static async Task ProjectSharingCreateAccountAsync(string email, string password, string username)
		{
			_projectSharingAuthError = "";
			try
			{
				await FirebaseBootstrap.InitializeAsync();
				var auth = FirebaseAuth.DefaultInstance;
				if (auth == null) { _projectSharingAuthError = "Auth not available."; return; }

				// Link anonymous account if signed in as Guest; otherwise create new account
				var currentUser = auth.CurrentUser;
				bool isAnonymous = currentUser != null && currentUser.IsAnonymous;
				bool authSuccess = false;

				if (isAnonymous)
				{
					var credential = EmailAuthProvider.GetCredential(email, password);
					var result = await currentUser.LinkWithCredentialAsync(credential);
					authSuccess = result?.User != null;
				}
				else
				{
					var result = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
					authSuccess = result?.User != null;
				}

				if (authSuccess)
				{
					FirebaseBootstrap.RefreshUserIdFromAuth();

					// Claim or update username if provided (skip in Editor with anon)
					if (!string.IsNullOrWhiteSpace(username) && FirebaseBootstrap.UserId != "anon")
					{
						var validation = UserAuthService.ValidateUsername(username);
						if (validation.isValid)
						{
							var existingProfile = await UserAuthService.GetCurrentUserProfileAsync();
							if (existingProfile == null || string.IsNullOrEmpty(existingProfile.username))
							{
								var claimResult = await UserAuthService.ClaimUsernameAsync(username);
								if (!claimResult.success)
									_projectSharingAuthError = claimResult.error ?? "Username claim failed.";
							}
							else if (!string.Equals(existingProfile.username, username, StringComparison.OrdinalIgnoreCase))
							{
								var changeResult = await UserAuthService.ChangeUsernameAsync(username);
								if (!changeResult.success)
									_projectSharingAuthError = changeResult.error ?? "Username update failed.";
							}
						}
						else
							_projectSharingAuthError = validation.error ?? "Invalid username.";
					}

					if (string.IsNullOrEmpty(_projectSharingAuthError))
					{
						_projectSharingIsGuest = false;
						_projectSharingSignedIn = true;
						activePopup = PopupKind.None;
					}
				}
			}
			catch (Exception ex)
			{
				_projectSharingAuthError = ex.Message ?? "Create account failed.";
				Debug.LogError($"[ProjectSharing] Create account failed: {ex.Message}");
			}
		}

		static async Task ProjectSharingLoginAsync(string email, string password)
		{
			_projectSharingAuthError = "";
			try
			{
				await FirebaseBootstrap.InitializeAsync();
				var auth = FirebaseAuth.DefaultInstance;
				if (auth == null) { _projectSharingAuthError = "Auth not available."; return; }
				var result = await auth.SignInWithEmailAndPasswordAsync(email, password);
				if (result?.User != null)
				{
					FirebaseBootstrap.RefreshUserIdFromAuth();
					_projectSharingIsGuest = false;
					_projectSharingSignedIn = true;
					activePopup = PopupKind.None;
				}
			}
			catch (Exception ex)
			{
				_projectSharingAuthError = ex.Message ?? "Login failed.";
				Debug.LogError($"[ProjectSharing] Login failed: {ex.Message}");
			}
		}

		static readonly Seb.Vis.UI.UI.ScrollViewDrawContentFunc projectSharingUploadScrollDrawer = DrawProjectSharingUploadScrollContent;
		static void DrawProjectSharingUploadScrollContent(Vector2 topLeft, float width, bool isLayoutPass)
		{
			if (allProjectDescriptions == null) return;
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			ButtonTheme baseButtonTheme = theme.ProjectSelectionButton;
			baseButtonTheme.fontSize *= ProjectSharing_ListPopupItemFontScale;
			ButtonTheme selectedButtonTheme = theme.ProjectSelectionButtonSelected;
			selectedButtonTheme.fontSize *= ProjectSharing_ListPopupItemFontScale;
			for (int i = 0; i < allProjectDescriptions.Length; i++)
			{
				var desc = allProjectDescriptions[i];
				bool selected = i == _projectSharingUploadSelectedIndex;
				var btnTheme = selected ? selectedButtonTheme : baseButtonTheme;
				if (Seb.Vis.UI.UI.Button(desc.ProjectName, btnTheme, topLeft, new Vector2(width, 0), true, false, true, btnTheme.buttonCols, Anchor.TopLeft))
					_projectSharingUploadSelectedIndex = i;
				float rowGap = Seb.Vis.UI.UI.Height * Popup_ProjectRowGapFrac;
				topLeft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * rowGap;
			}
		}

		static void DrawProjectSharingUploadConfirmPopup()
		{
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			Seb.Vis.UI.UI.StartNewLayer();
			Seb.Vis.UI.UI.DrawFullscreenPanel(theme.MenuBackgroundOverlayCol);

			float w = Seb.Vis.UI.UI.Width;
			float h = Seb.Vis.UI.UI.Height;
			Vector2 centre = Seb.Vis.UI.UI.Centre;
			float popupTopY = centre.y + h * (ProjectSharing_ListPopupContentMaxFrac - 0.5f);

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();

				Seb.Vis.UI.UI.DrawText("Upload project", theme.FontRegular, theme.FontSizeRegular * ProjectSharing_ListPopupTitleFontScale, new Vector2(centre.x, popupTopY - h * ProjectSharing_ListPopupTitleTopInsetFrac), Anchor.CentreTop, Color.white);

				Vector2 scrollSize = new(w * ProjectSharing_ListPopupWidthFrac, h * ProjectSharing_ListPopupScrollHeightFrac);
				Vector2 scrollPos = new Vector2(centre.x, popupTopY - h * ProjectSharing_ListPopupScrollTopInsetFrac);
				Seb.Vis.UI.UI.DrawScrollView(ID_ProjectSharing_UploadScrollView, scrollPos, scrollSize, Anchor.CentreTop, theme.ScrollTheme, projectSharingUploadScrollDrawer);
				Vector2 pos = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * (h * ProjectSharing_ListPopupButtonsTopGapFrac);

				if (!string.IsNullOrEmpty(_projectSharingUploadStatus))
				{
					Vector2 statusPos = new Vector2(centre.x, pos.y);
					Seb.Vis.UI.UI.DrawText(_projectSharingUploadStatus, theme.FontRegular, theme.FontSizeRegular * 0.8f, statusPos, Anchor.CentreTop, _projectSharingUploadStatus.StartsWith("Error") ? Color.red : Color.green);
					pos += Vector2.down * (h * Popup_SpacingFrac);
				}

				bool canUpload = !_projectSharingUploadInProgress && allProjectDescriptions != null && _projectSharingUploadSelectedIndex >= 0 && _projectSharingUploadSelectedIndex < allProjectDescriptions.Length;
				int btn = Seb.Vis.UI.UI.HorizontalButtonGroup(new[] { "CANCEL", "UPLOAD" }, new[] { true, canUpload }, theme.MainMenuButtonTheme, new Vector2(centre.x, pos.y), w * ProjectSharing_ListPopupWidthFrac, UILayoutHelper.DefaultSpacing, 0, Anchor.CentreTop);

				if (btn == 0 || KeyboardShortcuts.CancelShortcutTriggered)
				{
					activePopup = PopupKind.None;
					_projectSharingUploadStatus = "";
				}
				else if (btn == 1 && canUpload)
				{
					_projectSharingUploadPendingProjectName = allProjectDescriptions[_projectSharingUploadSelectedIndex].ProjectName;
					_ = ProjectSharingOpenUploadOptionsAsync();
				}

				Vector2 panelSize = new Vector2(w * ProjectSharing_ListPopupWidthFrac, h * (ProjectSharing_ListPopupHeightFrac + ProjectSharing_ListPopupBottomExtensionFrac)) + Vector2.one * ProjectSharing_ListPopupPanelPadding;
				Vector2 panelCentre = centre + Vector2.down * (h * ProjectSharing_ListPopupBottomExtensionFrac * 0.5f);
				Seb.Vis.UI.UI.ModifyPanel(panelID, panelCentre, panelSize, ColHelper.MakeCol255(37, 37, 43));
			}
		}

		static readonly string[] ProjectSharingPublicPrivateOptions = { "Public", "Private" };

		static void DrawProjectSharingUploadDisplayNamePopup()
		{
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			Seb.Vis.UI.UI.StartNewLayer();
			Seb.Vis.UI.UI.DrawFullscreenPanel(theme.MenuBackgroundOverlayCol);

			float w = Seb.Vis.UI.UI.Width;
			float h = Seb.Vis.UI.UI.Height;
			Vector2 centre = Seb.Vis.UI.UI.Centre;

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();

				Seb.Vis.UI.UI.DrawText($"Upload \"{_projectSharingUploadPendingProjectName}\"", theme.FontRegular, theme.FontSizeRegular * 1.1f, centre + Vector2.up * (h * Popup_TitleOffsetFrac), Anchor.CentreTop, Color.white);

				if (!_projectSharingUploadProjectDisplayNameInitialized)
				{
					Seb.Vis.UI.UI.GetInputFieldState(ID_ProjectSharing_UploadProjectDisplayName).SetText(_projectSharingUploadProjectDisplayNameDefault ?? "");
					_projectSharingUploadProjectDisplayNameInitialized = true;
				}

				InputFieldTheme inputTheme = theme.ChipNameInputField;
				inputTheme.fontSize = theme.FontSizeRegular;
				Vector2 pos = centre + Vector2.up * (h * (Popup_TitleOffsetFrac - 0.06f));
				Seb.Vis.UI.UI.DrawText("Project display name", theme.FontRegular, theme.FontSizeRegular * 0.8f, pos, Anchor.CentreTop, Color.white);
				pos += Vector2.down * (h * Popup_SpacingFrac);
				float inputHeight = h * 0.032f;
				var projectDisplayNameState = Seb.Vis.UI.UI.InputField(ID_ProjectSharing_UploadProjectDisplayName, inputTheme, pos, new Vector2(w * Popup_ScrollWidthFrac, inputHeight), "", Anchor.CentreTop, 1f, null, false);
				pos += Vector2.down * (inputHeight + h * 0.055f);
				float regionWidth = w * Popup_ScrollWidthFrac;
				float rowLeft = centre.x - regionWidth / 2f;
				float rowRight = centre.x + regionWidth / 2f;
				float wheelWidth = regionWidth * 0.35f;
				float rowHeight = h * Popup_RowHeightFrac;

				// Row 1: Visibility (label left, wheel right)
				Seb.Vis.UI.UI.DrawText("Visibility", theme.FontRegular, theme.FontSizeRegular * 0.8f, new Vector2(rowLeft, pos.y), Anchor.CentreLeft, Color.white);
				int visIdx = Seb.Vis.UI.UI.WheelSelector(_projectSharingUploadIsPublic ? 0 : 1, ProjectSharingPublicPrivateOptions, new Vector2(rowRight, pos.y), new Vector2(wheelWidth, rowHeight), theme.OptionsWheel, Anchor.CentreRight);
				_projectSharingUploadIsPublic = visIdx == 0;
				pos += Vector2.down * (rowHeight + h * Popup_SpacingFrac);

				// Row 2: Levels (label left, wheel right)
				Seb.Vis.UI.UI.DrawText("Levels", theme.FontRegular, theme.FontSizeRegular * 0.8f, new Vector2(rowLeft, pos.y), Anchor.CentreLeft, Color.white);
				int levelsIdx = Seb.Vis.UI.UI.WheelSelector(_projectSharingUploadIncludeLevels ? 0 : 1, ProjectSharingPublicPrivateOptions, new Vector2(rowRight, pos.y), new Vector2(wheelWidth, rowHeight), theme.OptionsWheel, Anchor.CentreRight);
				_projectSharingUploadIncludeLevels = levelsIdx == 0;
				pos += Vector2.down * (rowHeight + h * Popup_SettingsToButtonsGapFrac);

				int btn = Seb.Vis.UI.UI.HorizontalButtonGroup(new[] { "CANCEL", "CONFIRM" }, theme.MainMenuButtonTheme, new Vector2(centre.x, pos.y), w * Popup_ScrollWidthFrac, UILayoutHelper.DefaultSpacing, 0, Anchor.CentreTop);

				if (btn == 0 || KeyboardShortcuts.CancelShortcutTriggered)
				{
					activePopup = PopupKind.ProjectSharing_UploadConfirm;
				}
				else if (btn == 1)
				{
					var projectDisplayName = (projectDisplayNameState.text ?? "").Trim();
					_projectSharingUploadInProgress = true;
					_projectSharingUploadStatus = "Uploading...";
					_ = ProjectSharingUploadAsync(_projectSharingUploadPendingProjectName, projectDisplayName, _projectSharingUploadIsPublic, _projectSharingUploadIncludeLevels);
					activePopup = PopupKind.ProjectSharing_UploadConfirm;
				}

				Seb.Vis.UI.UI.ModifyPanel(panelID, Seb.Vis.UI.UI.GetCurrentBoundsScope().Centre, Seb.Vis.UI.UI.GetCurrentBoundsScope().Size + Vector2.one * 2, ColHelper.MakeCol255(37, 37, 43));
			}
		}

		static async Task ProjectSharingUploadAsync(string projectName, string projectDisplayName, bool isPublic, bool includeLevels)
		{
			try
			{
				await LibraryService.UploadProjectAsync(projectName, projectDisplayName, isPublic, includeLevels);
				_projectSharingUploadStatus = "Upload complete!";
				_projectSharingUploadInProgress = false;
				await Task.Delay(1500);
				activePopup = PopupKind.None;
				_projectSharingUploadStatus = "";
			}
			catch (Exception ex)
			{
				_projectSharingUploadStatus = ex.Message.Contains("offline", StringComparison.OrdinalIgnoreCase)
					? "No internet connection. Please check your network and try again."
					: "Error: " + ex.Message;
				_projectSharingUploadInProgress = false;
			}
		}

		static readonly Seb.Vis.UI.UI.ScrollViewDrawContentFunc projectSharingImportScrollDrawer = DrawProjectSharingImportScrollContent;
		static void DrawProjectSharingImportScrollContent(Vector2 topLeft, float width, bool isLayoutPass)
		{
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			ButtonTheme listButtonTheme = theme.MainMenuButtonTheme;
			listButtonTheme.fontSize *= ProjectSharing_ListPopupItemFontScale;
			if (_projectSharingLibraryLoading)
			{
				float vOffset = width * 0.08f; // screen-relative offset
				Vector2 textPos = new Vector2(topLeft.x + width / 2, topLeft.y + vOffset);
				Seb.Vis.UI.UI.DrawText("Loading...", theme.FontRegular, theme.FontSizeRegular, textPos, Anchor.Centre, Color.white);
				return;
			}
			if (_projectSharingLibraryEntries == null || _projectSharingLibraryEntries.Count == 0)
			{
				string status = _projectSharingImportStatus ?? "No projects.";
				float vOffset = width * 0.08f;
				Vector2 textPos = new Vector2(topLeft.x + width / 2, topLeft.y + vOffset);
				Seb.Vis.UI.UI.DrawText(WrapText(status, 45), theme.FontRegular, theme.FontSizeRegular, textPos, Anchor.Centre, Color.white);
				return;
			}
			float rowH = 3.5f;
			foreach (var entry in _projectSharingLibraryEntries)
			{
				bool importing = entry.id == _projectSharingImportInProgressId;
				bool selected = _projectSharingImportSelectedEntry != null && _projectSharingImportSelectedEntry.id == entry.id;
				string title = !string.IsNullOrEmpty(entry.projectDisplayName) ? entry.projectDisplayName : entry.projectName;
				string label = $"{title} by {entry.displayName}";
				if (entry.downloadCount > 0)
					label += $" ({entry.downloadCount} downloads)";
				if (importing) label += " (importing...)";
				if (selected) label = "► " + label;
				var buttonCols = selected ? new ButtonTheme.StateCols(new Color(0.3f, 0.35f, 0.45f), new Color(0.4f, 0.45f, 0.55f), new Color(0.25f, 0.3f, 0.4f), Color.gray) : theme.MainMenuButtonTheme.buttonCols;
				if (Seb.Vis.UI.UI.Button(label, listButtonTheme, topLeft, new Vector2(width, rowH), true, false, true, buttonCols, Anchor.TopLeft))
				{
					_projectSharingImportSelectedEntry = entry;
				}
				float rowGap = Seb.Vis.UI.UI.Height * Popup_ProjectRowGapFrac;
				topLeft = new Vector2(topLeft.x, Seb.Vis.UI.UI.PrevBounds.BottomLeft.y + rowGap);
			}
		}

		static async Task ProjectSharingImportAsync(string projectId)
		{
			try
			{
				var success = await LibraryService.ImportProjectAsync(projectId);
				if (success)
				{
					RefreshLoadedProjects();
					_projectSharingImportStatus = "Imported!";
					_projectSharingImportInProgressId = null;
					await Task.Delay(800);
					activePopup = PopupKind.None;
				}
				else
				{
					_projectSharingImportStatus = "Import failed.";
					_projectSharingImportInProgressId = null;
				}
			}
			catch (Exception ex)
			{
				_projectSharingImportStatus = "Error: " + ex.Message;
				_projectSharingImportInProgressId = null;
			}
		}

		static readonly string[] ProjectSharingImportFilterOptions = { "Public", "Private", "All" };
		static readonly string[] ProjectSharingImportSortOptions = { "Newest", "Popular", "A B C" };

		static string _projectSharingMyProjectsSyncInProgressId;

		static readonly Seb.Vis.UI.UI.ScrollViewDrawContentFunc projectSharingMyProjectsScrollDrawer = DrawProjectSharingMyProjectsScrollContent;
		static void DrawProjectSharingMyProjectsScrollContent(Vector2 topLeft, float width, bool isLayoutPass)
		{
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			ButtonTheme listButtonTheme = theme.MainMenuButtonTheme;
			listButtonTheme.fontSize *= ProjectSharing_ListPopupItemFontScale;
			if (_projectSharingMyProjectsLoading)
			{
				float vOffset = width * 0.08f;
				Seb.Vis.UI.UI.DrawText("Loading...", theme.FontRegular, theme.FontSizeRegular, topLeft + new Vector2(width / 2, vOffset), Anchor.Centre, Color.white);
				return;
			}
			if (_projectSharingMyProjectsEntries == null || _projectSharingMyProjectsEntries.Count == 0)
			{
				float vOffset = width * 0.08f;
				Seb.Vis.UI.UI.DrawText("No projects.", theme.FontRegular, theme.FontSizeRegular, topLeft + new Vector2(width / 2, vOffset), Anchor.Centre, Color.white);
				return;
			}
			float rowH = 3.5f;
			foreach (var entry in _projectSharingMyProjectsEntries)
			{
				bool syncing = entry.id == _projectSharingMyProjectsSyncInProgressId;
				bool selected = _projectSharingMyProjectsSelectedEntry != null && _projectSharingMyProjectsSelectedEntry.id == entry.id;
				string title = !string.IsNullOrEmpty(entry.projectDisplayName) ? entry.projectDisplayName : entry.projectName;
				string label = $"{title}";
				if (!string.IsNullOrEmpty(entry.displayName))
					label += $" ({entry.displayName})";
				label += entry.isPublic ? " [Public]" : " [Private]";
				if (syncing)
					label += " (syncing...)";
				if (selected)
					label = "► " + label;

				var buttonCols = selected ? new ButtonTheme.StateCols(new Color(0.3f, 0.35f, 0.45f), new Color(0.4f, 0.45f, 0.55f), new Color(0.25f, 0.3f, 0.4f), Color.gray) : theme.MainMenuButtonTheme.buttonCols;
				if (Seb.Vis.UI.UI.Button(label, listButtonTheme, topLeft, new Vector2(width, rowH), true, false, true, buttonCols, Anchor.TopLeft))
				{
					_projectSharingMyProjectsSelectedEntry = entry;
				}
				float rowGap = Seb.Vis.UI.UI.Height * Popup_ProjectRowGapFrac;
				topLeft = new Vector2(topLeft.x, Seb.Vis.UI.UI.PrevBounds.BottomLeft.y + rowGap);
			}
		}

		static async Task ProjectSharingSyncAsync(LibraryService.LibraryEntry entry)
		{
			try
			{
				await LibraryService.UploadProjectAsync(entry.projectName, entry.projectDisplayName, entry.isPublic);
				_projectSharingMyProjectsSyncInProgressId = null;
				_projectSharingMyProjectsLoading = true;
				_ = ProjectSharingLoadMyProjectsAsync();
			}
			catch (Exception ex)
			{
				_projectSharingMyProjectsSyncInProgressId = null;
				Debug.LogWarning($"[ProjectSharing] Sync failed: {ex.Message}");
			}
		}

		static void DrawProjectSharingMyProjectsPopup()
		{
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			Seb.Vis.UI.UI.StartNewLayer();
			Seb.Vis.UI.UI.DrawFullscreenPanel(theme.MenuBackgroundOverlayCol);

			float w = Seb.Vis.UI.UI.Width;
			float h = Seb.Vis.UI.UI.Height;
			Vector2 centre = Seb.Vis.UI.UI.Centre;
			float popupTopY = centre.y + h * (ProjectSharing_ListPopupContentMaxFrac - 0.5f);

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();

				Seb.Vis.UI.UI.DrawText("My projects", theme.FontRegular, theme.FontSizeRegular * ProjectSharing_ListPopupTitleFontScale, new Vector2(centre.x, popupTopY - h * ProjectSharing_ListPopupTitleTopInsetFrac), Anchor.CentreTop, Color.white);

				float filterY = popupTopY - h * ProjectSharing_ListPopupFilterTopInsetFrac;
				float filterRegionWidth = w * ProjectSharing_ListPopupWidthFrac;
				float wheelWidth = filterRegionWidth * 0.35f;
				WheelSelectorTheme listWheelTheme = theme.OptionsWheel;
				listWheelTheme.OverrideFontSize(theme.FontSizeRegular * ProjectSharing_ListPopupWheelFontScale);
				int filterIdx = (int)_projectSharingMyProjectsFilter;
				int newFilterIdx = Seb.Vis.UI.UI.WheelSelector(filterIdx, ProjectSharingImportFilterOptions, new Vector2(centre.x, filterY), new Vector2(wheelWidth, 2.7f), listWheelTheme, Anchor.Centre);
				if (newFilterIdx != filterIdx)
				{
					_projectSharingMyProjectsFilter = (LibraryService.LibraryFilterMode)newFilterIdx;
					_projectSharingMyProjectsLoading = true;
					_ = ProjectSharingLoadMyProjectsAsync();
				}

				Vector2 scrollSize = new(w * ProjectSharing_ListPopupWidthFrac, h * ProjectSharing_ListPopupScrollHeightFrac);
				Vector2 scrollPos = new Vector2(centre.x, popupTopY - h * ProjectSharing_ListPopupScrollTopInsetFrac);
				Seb.Vis.UI.UI.DrawScrollView(ID_ProjectSharing_MyProjectsScrollView, scrollPos, scrollSize, Anchor.CentreTop, theme.ScrollTheme, projectSharingMyProjectsScrollDrawer);

				Vector2 buttonRowPos = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * (h * ProjectSharing_ListPopupButtonsTopGapFrac);
				float buttonRowWidth = w * ProjectSharing_ListPopupWidthFrac;
				bool hasSelection = _projectSharingMyProjectsSelectedEntry != null;
				bool syncing = hasSelection && _projectSharingMyProjectsSelectedEntry.id == _projectSharingMyProjectsSyncInProgressId;
				bool hasLocal = hasSelection && Loader.ProjectExists(_projectSharingMyProjectsSelectedEntry.projectName);
				bool editEnabled = hasSelection && !syncing && _projectSharingEditEntry == null && _projectSharingDeleteConfirmEntry == null;
				bool deleteEnabled = hasSelection && !syncing && _projectSharingEditEntry == null && _projectSharingDeleteConfirmEntry == null;
				bool syncEnabled = hasSelection && !syncing && hasLocal && _projectSharingEditEntry == null && _projectSharingDeleteConfirmEntry == null;

				int actionBtn = Seb.Vis.UI.UI.HorizontalButtonGroup(
					new[] { "EDIT", "DELETE", "SYNC", "CLOSE" },
					new[] { editEnabled, deleteEnabled, syncEnabled, true },
					theme.MainMenuButtonTheme,
					new Vector2(centre.x, buttonRowPos.y),
					buttonRowWidth,
					UILayoutHelper.DefaultSpacing,
					0,
					Anchor.CentreTop);

				if (actionBtn == 0 && editEnabled)
				{
					_projectSharingEditEntry = _projectSharingMyProjectsSelectedEntry;
					_projectSharingEditIsPublic = _projectSharingMyProjectsSelectedEntry.isPublic;
					_projectSharingEditProjectDisplayNameInitialized = false;
					activePopup = PopupKind.ProjectSharing_EditEntry;
				}
				else if (actionBtn == 1 && deleteEnabled)
				{
					_projectSharingDeleteConfirmEntry = _projectSharingMyProjectsSelectedEntry;
					activePopup = PopupKind.ProjectSharing_DeleteConfirm;
				}
				else if (actionBtn == 2 && syncEnabled)
				{
					_projectSharingMyProjectsSyncInProgressId = _projectSharingMyProjectsSelectedEntry.id;
					_ = ProjectSharingSyncAsync(_projectSharingMyProjectsSelectedEntry);
				}
				else if (actionBtn == 3 || KeyboardShortcuts.CancelShortcutTriggered)
				{
					activePopup = PopupKind.None;
				}

				Vector2 panelSize = new Vector2(w * ProjectSharing_ListPopupWidthFrac, h * (ProjectSharing_ListPopupHeightFrac + ProjectSharing_ListPopupBottomExtensionFrac)) + Vector2.one * ProjectSharing_ListPopupPanelPadding;
				Vector2 panelCentre = centre + Vector2.down * (h * ProjectSharing_ListPopupBottomExtensionFrac * 0.5f);
				Seb.Vis.UI.UI.ModifyPanel(panelID, panelCentre, panelSize, ColHelper.MakeCol255(37, 37, 43));
			}
		}

		static void DrawProjectSharingDeleteConfirmPopup()
		{
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			Seb.Vis.UI.UI.StartNewLayer();
			Seb.Vis.UI.UI.DrawFullscreenPanel(theme.MenuBackgroundOverlayCol);

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();
				string msg = _projectSharingDeleteConfirmEntry != null
					? $"Are you sure you want to delete \"{_projectSharingDeleteConfirmEntry.projectName}\"?"
					: "Are you sure you want to delete this project?";
				Seb.Vis.UI.UI.DrawText(msg, theme.FontRegular, theme.FontSizeRegular, Seb.Vis.UI.UI.Centre, Anchor.Centre, Color.yellow);

				Vector2 buttonRegionTopLeft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * DrawSettings.VerticalButtonSpacing;
				float buttonRegionWidth = Seb.Vis.UI.UI.PrevBounds.Width;
				int buttonIndex = Seb.Vis.UI.UI.HorizontalButtonGroup(new[] { "CANCEL", "DELETE" }, theme.MainMenuButtonTheme, buttonRegionTopLeft, buttonRegionWidth, DrawSettings.HorizontalButtonSpacing, 0, Anchor.TopLeft);

				if (buttonIndex == 0 || KeyboardShortcuts.CancelShortcutTriggered)
				{
					_projectSharingDeleteConfirmEntry = null;
					activePopup = PopupKind.ProjectSharing_MyProjects;
				}
				else if (buttonIndex == 1 && _projectSharingDeleteConfirmEntry != null)
				{
					var entry = _projectSharingDeleteConfirmEntry;
					_projectSharingDeleteConfirmEntry = null;
					activePopup = PopupKind.ProjectSharing_MyProjects;
					_ = ProjectSharingDeleteEntryAsync(entry.id);
				}

				Seb.Vis.UI.UI.ModifyPanel(panelID, Seb.Vis.UI.UI.GetCurrentBoundsScope().Centre, Seb.Vis.UI.UI.GetCurrentBoundsScope().Size + Vector2.one * 2, ColHelper.MakeCol255(37, 37, 43));
			}
		}

		static async Task ProjectSharingDeleteEntryAsync(string documentId)
		{
			try
			{
				await LibraryService.DeleteEntryAsync(documentId);
				_projectSharingMyProjectsLoading = true;
				_ = ProjectSharingLoadMyProjectsAsync();
			}
			catch (Exception ex)
			{
				Debug.LogWarning($"[ProjectSharing] Delete failed: {ex.Message}");
			}
		}

		static void DrawProjectSharingEditEntryPopup()
		{
			if (_projectSharingEditEntry == null)
			{
				_projectSharingEditEntry = null;
				activePopup = PopupKind.ProjectSharing_MyProjects;
				return;
			}

			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			Seb.Vis.UI.UI.StartNewLayer();
			Seb.Vis.UI.UI.DrawFullscreenPanel(theme.MenuBackgroundOverlayCol);

			float w = Seb.Vis.UI.UI.Width;
			float h = Seb.Vis.UI.UI.Height;
			Vector2 centre = Seb.Vis.UI.UI.Centre;

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();

				Seb.Vis.UI.UI.DrawText($"Edit \"{_projectSharingEditEntry.projectName}\"", theme.FontRegular, theme.FontSizeRegular * 1.1f, centre + Vector2.up * (h * Popup_TitleOffsetFrac), Anchor.CentreTop, Color.white);

				if (!_projectSharingEditProjectDisplayNameInitialized)
				{
					string defaultDisplayName = !string.IsNullOrEmpty(_projectSharingEditEntry.projectDisplayName) ? _projectSharingEditEntry.projectDisplayName : _projectSharingEditEntry.projectName;
					Seb.Vis.UI.UI.GetInputFieldState(ID_ProjectSharing_EditProjectDisplayName).SetText(defaultDisplayName ?? "");
					_projectSharingEditProjectDisplayNameInitialized = true;
				}

				InputFieldTheme inputTheme = theme.ChipNameInputField;
				inputTheme.fontSize = theme.FontSizeRegular;
				Vector2 pos = centre + Vector2.up * (h * (Popup_TitleOffsetFrac - 0.06f));
				Seb.Vis.UI.UI.DrawText("Project display name", theme.FontRegular, theme.FontSizeRegular * 0.8f, pos, Anchor.CentreTop, Color.white);
				pos += Vector2.down * (h * Popup_SpacingFrac);
				float inputHeight = h * 0.032f;
				var projectDisplayNameState = Seb.Vis.UI.UI.InputField(ID_ProjectSharing_EditProjectDisplayName, inputTheme, pos, new Vector2(w * Popup_ScrollWidthFrac, inputHeight), "", Anchor.CentreTop, 1f, null, false);
				pos += Vector2.down * (inputHeight + h * 0.04f);
				Seb.Vis.UI.UI.DrawText("Visibility", theme.FontRegular, theme.FontSizeRegular * 0.8f, pos, Anchor.CentreTop, Color.white);
				pos += Vector2.down * 2f;
				float wheelWidth = w * Popup_ScrollWidthFrac * 0.35f;
				int visIdx = Seb.Vis.UI.UI.WheelSelector(_projectSharingEditIsPublic ? 0 : 1, ProjectSharingPublicPrivateOptions, new Vector2(centre.x, pos.y), new Vector2(wheelWidth, 2.7f), theme.OptionsWheel, Anchor.Centre);
				_projectSharingEditIsPublic = visIdx == 0;
				pos += Vector2.down * 5f;

				int btn = Seb.Vis.UI.UI.HorizontalButtonGroup(new[] { "CANCEL", "SAVE" }, theme.MainMenuButtonTheme, new Vector2(centre.x, pos.y), w * Popup_ScrollWidthFrac, UILayoutHelper.DefaultSpacing, 0, Anchor.CentreTop);

				if (btn == 0 || KeyboardShortcuts.CancelShortcutTriggered)
				{
					_projectSharingEditEntry = null;
					_projectSharingEditProjectDisplayNameInitialized = false;
					activePopup = PopupKind.ProjectSharing_MyProjects;
				}
				else if (btn == 1)
				{
					var projectDisplayName = (projectDisplayNameState.text ?? "").Trim();
					var entry = _projectSharingEditEntry;
					_projectSharingEditEntry = null;
					activePopup = PopupKind.ProjectSharing_MyProjects;
					_ = ProjectSharingUpdateEntryAsync(entry.id, projectDisplayName, _projectSharingEditIsPublic);
				}

				Seb.Vis.UI.UI.ModifyPanel(panelID, Seb.Vis.UI.UI.GetCurrentBoundsScope().Centre, Seb.Vis.UI.UI.GetCurrentBoundsScope().Size + Vector2.one * 2, ColHelper.MakeCol255(37, 37, 43));
			}
		}

		static async Task ProjectSharingUpdateEntryAsync(string documentId, string projectDisplayName, bool isPublic)
		{
			try
			{
				await LibraryService.UpdateEntryAsync(documentId, projectDisplayName, isPublic);
				_projectSharingMyProjectsLoading = true;
				_ = ProjectSharingLoadMyProjectsAsync();
			}
			catch (Exception ex)
			{
				Debug.LogWarning($"[ProjectSharing] Update failed: {ex.Message}");
			}
		}

		static bool ProjectSharingValidateUsername(string userName)
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

		static void DrawProjectSharingChangeUsernamePopup()
		{
			if (_projectSharingChangeUsernameShowConfirm)
			{
				DrawProjectSharingChangeUsernameConfirmDialog();
				return;
			}

			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			Seb.Vis.UI.UI.StartNewLayer();
			Seb.Vis.UI.UI.DrawFullscreenPanel(theme.MenuBackgroundOverlayCol);

			float w = Seb.Vis.UI.UI.Width;
			float h = Seb.Vis.UI.UI.Height;
			Vector2 centre = Seb.Vis.UI.UI.Centre;

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();

				Seb.Vis.UI.UI.DrawText("Change Username", theme.FontRegular, theme.FontSizeRegular * 1.1f, centre + Vector2.up * (h * Popup_TitleOffsetFrac), Anchor.CentreTop, Color.white);

				string subtitle = _projectSharingChangeUsernameHasUsername
					? "Choose a username that will be displayed on your projects"
					: "Claim a username to identify yourself on shared projects";
				Seb.Vis.UI.UI.DrawText(subtitle, theme.FontRegular, theme.FontSizeRegular * 0.8f, centre + Vector2.up * (h * (Popup_TitleOffsetFrac - 0.04f)), Anchor.CentreTop, ColHelper.MakeCol255(200, 200, 200));

				if (!_projectSharingChangeUsernameInitialized)
				{
					Seb.Vis.UI.UI.GetInputFieldState(ID_ProjectSharing_ChangeUsername).SetText(_projectSharingChangeUsernameOriginal ?? "");
					_projectSharingChangeUsernameInitialized = true;
				}

				InputFieldTheme inputTheme = theme.ChipNameInputField;
				inputTheme.fontSize = theme.FontSizeRegular;
				Vector2 pos = centre + Vector2.up * (h * (Popup_TitleOffsetFrac - 0.1f));
				Seb.Vis.UI.UI.DrawText("Username", theme.FontRegular, theme.FontSizeRegular * 0.8f, pos, Anchor.CentreTop, Color.white);
				pos += Vector2.down * (h * Popup_SpacingFrac);
				float inputHeight = h * 0.032f;
				InputFieldState usernameState;
				if (_projectSharingChangeUsernameInProgress)
				{
					using (Seb.Vis.UI.UI.BeginDisabledScope(true))
					{
						usernameState = Seb.Vis.UI.UI.InputField(ID_ProjectSharing_ChangeUsername, inputTheme, pos, new Vector2(w * Popup_ScrollWidthFrac, inputHeight), "Enter username...", Anchor.CentreTop, 1f, null, false);
					}
				}
				else
				{
					usernameState = Seb.Vis.UI.UI.InputField(ID_ProjectSharing_ChangeUsername, inputTheme, pos, new Vector2(w * Popup_ScrollWidthFrac, inputHeight), "Enter username...", Anchor.CentreTop, 1f, null, false);
				}
				pos += Vector2.down * (inputHeight + h * 0.04f);

				if (!string.IsNullOrEmpty(_projectSharingChangeUsernameError))
				{
					Seb.Vis.UI.UI.DrawText(_projectSharingChangeUsernameError, theme.FontRegular, theme.FontSizeRegular * 0.8f, pos, Anchor.CentreTop, Color.red);
					pos += Vector2.down * (h * 0.04f);
				}

				pos += Vector2.down * (h * 0.08f); // Extra spacing between input/error and buttons
				int btn = Seb.Vis.UI.UI.HorizontalButtonGroup(new[] { "CANCEL", "CONFIRM" }, theme.MainMenuButtonTheme, new Vector2(centre.x, pos.y), w * Popup_ScrollWidthFrac, UILayoutHelper.DefaultSpacing, 0, Anchor.CentreTop);

				if (btn == 0 || KeyboardShortcuts.CancelShortcutTriggered)
				{
					activePopup = PopupKind.None;
				}
				else if (btn == 1 && !_projectSharingChangeUsernameInProgress)
				{
					string newName = (usernameState.text ?? "").Trim();
					if (!ProjectSharingValidateUsername(newName))
					{
						_projectSharingChangeUsernameError = "Username must be 3-20 characters (letters, numbers, spaces, hyphens, underscores)";
					}
					else if (_projectSharingChangeUsernameHasUsername && string.Equals(newName, _projectSharingChangeUsernameOriginal, StringComparison.OrdinalIgnoreCase))
					{
						activePopup = PopupKind.None;
					}
					else if (_projectSharingChangeUsernameHasUsername && newName != _projectSharingChangeUsernameOriginal)
					{
						_projectSharingChangeUsernameNewName = newName;
						_projectSharingChangeUsernameShowConfirm = true;
						_projectSharingChangeUsernameError = "";
					}
					else
					{
						_projectSharingChangeUsernameError = "";
						_ = ProjectSharingClaimUsernameAsync(newName);
					}
				}

				Seb.Vis.UI.UI.ModifyPanel(panelID, Seb.Vis.UI.UI.GetCurrentBoundsScope().Centre, Seb.Vis.UI.UI.GetCurrentBoundsScope().Size + Vector2.one * 2, ColHelper.MakeCol255(37, 37, 43));
			}
		}

		static void DrawProjectSharingChangeUsernameConfirmDialog()
		{
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			Seb.Vis.UI.UI.StartNewLayer();
			Seb.Vis.UI.UI.DrawFullscreenPanel(theme.MenuBackgroundOverlayCol);

			float w = Seb.Vis.UI.UI.Width;
			float h = Seb.Vis.UI.UI.Height;
			Vector2 centre = Seb.Vis.UI.UI.Centre;

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();

				Seb.Vis.UI.UI.DrawText("Change Username?", theme.FontRegular, theme.FontSizeRegular * 1.2f, centre + Vector2.up * (h * Popup_TitleOffsetFrac), Anchor.CentreTop, ColHelper.MakeCol255(255, 165, 0));
				Vector2 msgPos = centre + Vector2.up * (h * (Popup_TitleOffsetFrac - 0.08f));
				string msg = $"Change from \"{_projectSharingChangeUsernameOriginal}\" to \"{_projectSharingChangeUsernameNewName}\"?\n\nThis will update all your existing projects.";
				Seb.Vis.UI.UI.DrawText(msg, theme.FontRegular, theme.FontSizeRegular, msgPos, Anchor.CentreTop, Color.white);

				Vector2 btnPos = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * (h * 0.04f);
				int btn = Seb.Vis.UI.UI.HorizontalButtonGroup(new[] { "CANCEL", "CONFIRM CHANGE" }, theme.MainMenuButtonTheme, new Vector2(centre.x, btnPos.y), w * Popup_ScrollWidthFrac, UILayoutHelper.DefaultSpacing, 0, Anchor.CentreTop);

				if (btn == 0 || KeyboardShortcuts.CancelShortcutTriggered)
				{
					_projectSharingChangeUsernameShowConfirm = false;
					_projectSharingChangeUsernameNewName = "";
				}
				else if (btn == 1)
				{
					_ = ProjectSharingChangeUsernameConfirmAsync();
				}

				Seb.Vis.UI.UI.ModifyPanel(panelID, Seb.Vis.UI.UI.GetCurrentBoundsScope().Centre, Seb.Vis.UI.UI.GetCurrentBoundsScope().Size + Vector2.one * 2, ColHelper.MakeCol255(37, 37, 43));
			}
		}

		static async Task ProjectSharingClaimUsernameAsync(string newName)
		{
			_projectSharingChangeUsernameInProgress = true;
			_projectSharingChangeUsernameError = "";
			try
			{
				var result = await UserAuthService.ClaimUsernameAsync(newName);
				if (result.success)
				{
					_projectSharingLoggedInAs = newName;
					activePopup = PopupKind.None;
				}
				else
				{
					_projectSharingChangeUsernameError = result.error;
				}
			}
			catch (Exception ex)
			{
				_projectSharingChangeUsernameError = ex.Message;
			}
			_projectSharingChangeUsernameInProgress = false;
		}

		static async Task ProjectSharingChangeUsernameConfirmAsync()
		{
			_projectSharingChangeUsernameInProgress = true;
			_projectSharingChangeUsernameError = "";
			try
			{
				var result = await UserAuthService.ChangeUsernameAsync(_projectSharingChangeUsernameNewName);
				if (result.success)
				{
					_projectSharingLoggedInAs = _projectSharingChangeUsernameNewName;
					_projectSharingChangeUsernameOriginal = _projectSharingChangeUsernameNewName;
					_projectSharingChangeUsernameShowConfirm = false;
					_projectSharingChangeUsernameNewName = "";
					activePopup = PopupKind.None;
				}
				else
				{
					_projectSharingChangeUsernameError = result.error;
					_projectSharingChangeUsernameShowConfirm = false;
					_projectSharingChangeUsernameNewName = "";
				}
			}
			catch (Exception ex)
			{
				_projectSharingChangeUsernameError = ex.Message;
				_projectSharingChangeUsernameShowConfirm = false;
				_projectSharingChangeUsernameNewName = "";
			}
			_projectSharingChangeUsernameInProgress = false;
		}

		static void DrawProjectSharingImportListPopup()
		{
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			Seb.Vis.UI.UI.StartNewLayer();
			Seb.Vis.UI.UI.DrawFullscreenPanel(theme.MenuBackgroundOverlayCol);

			float w = Seb.Vis.UI.UI.Width;
			float h = Seb.Vis.UI.UI.Height;
			Vector2 centre = Seb.Vis.UI.UI.Centre;
			float popupTopY = centre.y + h * (ProjectSharing_ListPopupContentMaxFrac - 0.5f);

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();

				Seb.Vis.UI.UI.DrawText("Import projects", theme.FontRegular, theme.FontSizeRegular * ProjectSharing_ListPopupTitleFontScale, new Vector2(centre.x, popupTopY - h * ProjectSharing_ListPopupTitleTopInsetFrac), Anchor.CentreTop, Color.white);

				float filterY = popupTopY - h * ProjectSharing_ListPopupFilterTopInsetFrac;
				float rowHeight = 3f;
				float filterSpacing = w * 0.04f;
				float filterRegionWidth = w * ProjectSharing_ListPopupWidthFrac;
				float wheelHeight = rowHeight * 0.9f;
				float wheelWidth = filterRegionWidth * 0.35f;
				WheelSelectorTheme listWheelTheme = theme.OptionsWheel;
				listWheelTheme.OverrideFontSize(theme.FontSizeRegular * ProjectSharing_ListPopupWheelFontScale);

				// Filter row: Public/Private/All wheel (left) | Sort wheel (right)
				int filterIdx = (int)_projectSharingImportFilter;
				int newFilterIdx = Seb.Vis.UI.UI.WheelSelector(filterIdx, ProjectSharingImportFilterOptions, new Vector2(centre.x - filterRegionWidth / 2 + wheelWidth / 2 + filterSpacing, filterY), new Vector2(wheelWidth, wheelHeight), listWheelTheme, Anchor.Centre);
				if (newFilterIdx != filterIdx)
				{
					_projectSharingImportFilter = (LibraryService.LibraryFilterMode)newFilterIdx;
					_projectSharingLibraryLoading = true;
					_ = ProjectSharingLoadLibraryAsync();
				}

				float rightX = centre.x + filterRegionWidth / 2 - wheelWidth / 2 - filterSpacing;
				int sortIdx = Seb.Vis.UI.UI.WheelSelector((int)_projectSharingImportSortOrder, ProjectSharingImportSortOptions, new Vector2(rightX, filterY), new Vector2(wheelWidth, wheelHeight), listWheelTheme, Anchor.Centre);
				var newSortOrder = (LibraryService.LibrarySortOrder)Mathf.Clamp(sortIdx, 0, 2);
				if (newSortOrder != _projectSharingImportSortOrder)
				{
					_projectSharingImportSortOrder = newSortOrder;
					_projectSharingLibraryLoading = true;
					_ = ProjectSharingLoadLibraryAsync();
				}

				Vector2 scrollSize = new(w * ProjectSharing_ListPopupWidthFrac, h * ProjectSharing_ListPopupScrollHeightFrac);
				Vector2 scrollPos = new Vector2(centre.x, popupTopY - h * ProjectSharing_ListPopupScrollTopInsetFrac);
				Seb.Vis.UI.UI.DrawScrollView(ID_ProjectSharing_LibraryScrollView, scrollPos, scrollSize, Anchor.CentreTop, theme.ScrollTheme, projectSharingImportScrollDrawer);

				Vector2 buttonRowPos = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * (h * ProjectSharing_ListPopupButtonsTopGapFrac);
				float buttonRowWidth = w * ProjectSharing_ListPopupWidthFrac;
				bool hasSelection = _projectSharingImportSelectedEntry != null;
				bool importing = !string.IsNullOrEmpty(_projectSharingImportInProgressId);
				bool importEnabled = hasSelection && !importing;

				int actionBtn = Seb.Vis.UI.UI.HorizontalButtonGroup(
					new[] { "IMPORT", "CLOSE" },
					new[] { importEnabled, true },
					theme.MainMenuButtonTheme,
					new Vector2(centre.x, buttonRowPos.y),
					buttonRowWidth,
					UILayoutHelper.DefaultSpacing,
					0,
					Anchor.CentreTop);

				if (actionBtn == 0 && importEnabled)
				{
					_projectSharingImportInProgressId = _projectSharingImportSelectedEntry.id;
					_ = ProjectSharingImportAsync(_projectSharingImportSelectedEntry.id);
				}
				else if (actionBtn == 1 || KeyboardShortcuts.CancelShortcutTriggered)
				{
					activePopup = PopupKind.None;
				}

				Vector2 panelSize = new Vector2(w * ProjectSharing_ListPopupWidthFrac, h * (ProjectSharing_ListPopupHeightFrac + ProjectSharing_ListPopupBottomExtensionFrac)) + Vector2.one * ProjectSharing_ListPopupPanelPadding;
				Vector2 panelCentre = centre + Vector2.down * (h * ProjectSharing_ListPopupBottomExtensionFrac * 0.5f);
				Seb.Vis.UI.UI.ModifyPanel(panelID, panelCentre, panelSize, ColHelper.MakeCol255(37, 37, 43));
			}
		}

		static void DrawAboutScreen()
		{
			ButtonTheme theme = DrawSettings.ActiveUITheme.MainMenuButtonTheme;
		
		// Show logo GameObjects when About menu is active AND no popup is open
		if (AboutMenuUIController.Instance != null)
		{
			if (activePopup == PopupKind.None)
			{
				AboutMenuUIController.Instance.ShowLogos();
			}
			else
			{
				AboutMenuUIController.Instance.HideLogos();
			}
		}
		
		// Layout: Text on left (0.1 to 0.65), Logos on right (0.70 to 0.90)
		float leftTextStartX = Seb.Vis.UI.UI.Width * 0.07f; // Left edge for text
		float logoTextStartX = Seb.Vis.UI.UI.Width * 0.48f; // Left edge for text
		float rightLogoX = Seb.Vis.UI.UI.Width * 0.866f; // Center of right column

			// Top section - Main about text
		string about_text_1 = "This is an extension of Sebastian Lague's project Digital-Logic-Sim.";
		about_text_1 = WrapText(about_text_1, 45) + "\n";
		string about_text_2 = "The original goal of the extension was to make the simulator available on mobile. Since then more features have also been added such as the levels system (still very much a work in progress). This version also includes changes introduced by the Community Edit (synced Feb 2026; check out Discord for more info).";
		about_text_2 = WrapText(about_text_2, 45);
		string about_text = about_text_1 + about_text_2;
		Seb.Vis.UI.UI.DrawText(about_text, theme.font, theme.fontSize*0.6f, new Vector2(leftTextStartX, Seb.Vis.UI.UI.Centre.y + 11), Anchor.TopLeft, Color.white);

		
		// YouTube section text
		string youtube_text = WrapText(
			"If you need inpiration for how to play the game or if you are curious about the origins of the project I highly recommend you check out Sebastians youtube",
			35);

		Seb.Vis.UI.UI.DrawText(youtube_text, theme.font, theme.fontSize*0.6f, new Vector2(logoTextStartX, Seb.Vis.UI.UI.Centre.y + 11), Anchor.TopLeft, Color.white);
		
		// YouTube button (empty/semi-transparent overlay over logo GameObject)
		Vector2 youtubeButtonPos = new Vector2(rightLogoX, Seb.Vis.UI.UI.Centre.y + 8);
		Vector2 buttonSize = new Vector2(8, 9); // Size for the clickable area
		
		// Create semi-transparent button colors for visibility during development
		ButtonTheme.StateCols logoButtonCols = new ButtonTheme.StateCols
		{
			normal = new Color(1, 1, 1, 0.1f),      // Slight white tint
			hover = new Color(1, 1, 0.5f, 0.3f),    // Yellow tint on hover
			pressed = new Color(0.5f, 1, 0.5f, 0.4f), // Green tint on press
			inactive = new Color(0.5f, 0.5f, 0.5f, 0.1f) // Gray tint when inactive
		};
		
		if (Seb.Vis.UI.UI.Button("", theme, youtubeButtonPos, buttonSize, true, false, false, logoButtonCols))
		{
			BackToMain();
			Application.OpenURL("https://www.youtube.com/watch?v=QZwneRb-zqA&list=PLFt_AvWsXl0dPhqVsKt1Ni_46ARyiCGSq");
		}

		// Discord section text
		string discord_text = WrapText(
			"If you want to report a bug, give feedback or have ideas for new features. Head to discord",
			35);

		Seb.Vis.UI.UI.DrawText(discord_text, theme.font, theme.fontSize*0.6f, new Vector2(logoTextStartX, Seb.Vis.UI.UI.CentreBottom.y + 21), Anchor.TopLeft, Color.white);
		
		// Discord button (empty/semi-transparent overlay over logo GameObject)
		Vector2 discordButtonPos = new Vector2(rightLogoX, Seb.Vis.UI.UI.CentreBottom.y + 19.5f);
		
		// White background for Discord button
		ButtonTheme.StateCols discordButtonCols = new ButtonTheme.StateCols
		{
			normal = new Color(1, 1, 1, 0.0f),      // White with some transparency
			hover = new Color(1, 1, 0.8f, 0.4f),    // Slight yellow tint on hover
			pressed = new Color(0.9f, 0.9f, 0.9f, 0.5f), // Slightly darker white on press
			inactive = new Color(0.7f, 0.7f, 0.7f, 0.2f) // Gray tint when inactive
		};
		
		Vector2 buttonSize2 = new Vector2(8, 7); // Size for the clickable area
		if (Seb.Vis.UI.UI.Button("", theme, discordButtonPos, buttonSize2, true, false, false, discordButtonCols))
		{
			BackToMain();
			Application.OpenURL("https://discord.com/channels/1361307968276136007/1426249925544382595");
		}

		// Back button - stays centered at bottom
		#if UNITY_ANDROID || UNITY_IOS
		Vector2 backButtonPos = Seb.Vis.UI.UI.CentreBottom + Vector2.up * 10;
		Vector2 whatsNewButtonPos = Seb.Vis.UI.UI.CentreBottom + Vector2.up * 10 + Vector2.left * 18;
		#else
		Vector2 backButtonPos = Seb.Vis.UI.UI.CentreBottom + Vector2.up * 10;
		Vector2 whatsNewButtonPos = Seb.Vis.UI.UI.CentreBottom + Vector2.up * 10 + Vector2.left * 10;
		#endif
		
		if (Seb.Vis.UI.UI.Button("What's New?", theme, whatsNewButtonPos, Vector2.zero, true, true, true, theme.buttonCols))
		{
			activePopup = PopupKind.PatchNotes;
			// Reload patch notes each time so we pick up JSON changes without restarting (avoids stale Unity/Resources cache)
			patchNotesData = null;
			PatchNotesLoader.ForceReload();
		}
		
		if (Seb.Vis.UI.UI.Button("Back", theme, backButtonPos, Vector2.zero, true, true, true, theme.buttonCols))
		{
			BackToMain();
		}
	}

		static void DrawPatchNotesPopup()
		{
			// Load (or reload) so we always show current data after opening the popup
			if (patchNotesData == null)
			{
				patchNotesData = PatchNotesLoader.LoadPatchNotes();
			}

			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;

			Seb.Vis.UI.UI.StartNewLayer();
			Seb.Vis.UI.UI.DrawFullscreenPanel(theme.MenuBackgroundOverlayCol);

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();
				
				#if UNITY_ANDROID || UNITY_IOS
				Vector2 popupSize = new(85, 45);
				#else
				Vector2 popupSize = new(70, 40);
				#endif
				
				Vector2 pos = Seb.Vis.UI.UI.Centre;
				
				// Title at top
				Seb.Vis.UI.UI.DrawText("What's New in Digital Logic Sim", theme.FontRegular, theme.FontSizeRegular * 1.2f, pos + Vector2.up * (popupSize.y / 2 - 3), Anchor.CentreTop, Color.white);
				
				// Calculate split positions
				float leftPanelWidth = popupSize.x * 0.65f;  // Patch notes detail (wider)
				float rightPanelWidth = popupSize.x * 0.30f; // Version selector (narrower)
				float panelHeight = popupSize.y - 12; // Leave space for title and buttons
				float gap = 1f; // Gap between panels
				
				// Calculate top-left positions for both panels
				float contentTop = pos.y + (popupSize.y / 2) - 7; // Below title
				float leftPanelLeft = pos.x - (popupSize.x / 2) + 2; // Left edge of popup + padding
				float rightPanelLeft = leftPanelLeft + leftPanelWidth + gap; // After left panel + gap
				
				// LEFT panel - Scrollable patch notes detail for SELECTED version
				Vector2 leftScrollViewPos = new Vector2(leftPanelLeft, contentTop);
				Vector2 leftScrollViewSize = new(leftPanelWidth - 2, panelHeight);
				
				// Get available versions and selected version
				var availableVersions = PatchNotesLoader.GetAvailableVersions();
				var selectedVersion = patchNotesData?.versions != null && selectedPatchNoteIndex >= 0 && selectedPatchNoteIndex < patchNotesData.versions.Count 
					? patchNotesData.versions[selectedPatchNoteIndex] 
					: null;
				
				Seb.Vis.UI.UI.DrawScrollView(ID_PatchNotesScrollView, leftScrollViewPos, leftScrollViewSize, Anchor.TopLeft, theme.ScrollTheme, (topLeft, width, isLayoutPass) =>
				{
					if (selectedVersion == null)
				{
					Seb.Vis.UI.UI.DrawText("No patch notes available", theme.FontRegular, theme.FontSizeRegular * 0.8f, topLeft, Anchor.TopLeft, Color.red);
					return;
				}
				
				float sectionSpacing = 1.0f; // Extra spacing before section headers
				
				// Version header
					Seb.Vis.UI.UI.DrawText($"Version {selectedVersion.version}", theme.FontRegular, theme.FontSizeRegular * 1.0f, topLeft, Anchor.TopLeft, new Color(0.98f, 0.76f, 0.26f));
					topLeft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * 0.3f;
					
					Seb.Vis.UI.UI.DrawText($"Release Date: {selectedVersion.releaseDate}", theme.FontRegular, theme.FontSizeRegular * 0.6f, topLeft, Anchor.TopLeft, new Color(0.7f, 0.7f, 0.7f));
					topLeft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * sectionSpacing;
					
					// Draw sections dynamically
					DrawPatchNotesSection("New Features:", selectedVersion.sections?.newFeatures, theme, ref topLeft, width, new Color(0.6f, 0.9f, 0.6f), sectionSpacing);
					DrawPatchNotesSection("Improvements:", selectedVersion.sections?.improvements, theme, ref topLeft, width, new Color(0.6f, 0.8f, 1f), sectionSpacing);
					DrawPatchNotesSection("Bug Fixes:", selectedVersion.sections?.bugFixes, theme, ref topLeft, width, new Color(1f, 0.6f, 0.6f), sectionSpacing);
				});
				
				// RIGHT panel - Version selector buttons
				Vector2 rightPanelPos = new Vector2(rightPanelLeft, contentTop);
				
				// Draw version selection buttons
				Vector2 versionButtonPos = rightPanelPos;
				Vector2 versionButtonSize = new Vector2(rightPanelWidth - 2, 3);
				
				for (int i = 0; i < availableVersions.Count; i++)
				{
					bool isSelected = selectedPatchNoteIndex == i;
					ButtonTheme.StateCols buttonCols = isSelected 
						? new ButtonTheme.StateCols(new Color(0.98f, 0.76f, 0.26f, 0.3f), new Color(0.98f, 0.76f, 0.26f, 0.5f), new Color(0.98f, 0.76f, 0.26f, 0.6f), Color.gray)
						: theme.MainMenuButtonTheme.buttonCols;
					
					string versionDisplayName = $"Version {availableVersions[i]}";
					if (Seb.Vis.UI.UI.Button(versionDisplayName, theme.MainMenuButtonTheme, versionButtonPos, versionButtonSize, true, false, true, buttonCols, Anchor.TopLeft))
					{
						selectedPatchNoteIndex = i;
					}
					
					versionButtonPos = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * 0.5f;
				}
				
				// Close button at bottom
				Vector2 buttonPos = pos + Vector2.down * (popupSize.y / 2 - 3);
				
				if (Seb.Vis.UI.UI.Button("Close", theme.MainMenuButtonTheme, buttonPos, Vector2.zero, true, true, true, theme.MainMenuButtonTheme.buttonCols) || KeyboardShortcuts.CancelShortcutTriggered)
				{
					activePopup = PopupKind.None;
				}
				
				Seb.Vis.UI.UI.ModifyPanel(panelID, Seb.Vis.UI.UI.GetCurrentBoundsScope().Centre, Seb.Vis.UI.UI.GetCurrentBoundsScope().Size + Vector2.one * 2, ColHelper.MakeCol255(37, 37, 43));
			}
		}
		
		static void DrawPatchNotesSection(string sectionTitle, List<string> items, DrawSettings.UIThemeDLS theme, ref Vector2 topLeft, float width, Color titleColor, float sectionSpacing)
		{
			if (items == null || items.Count == 0) return;
			
			// Section header
			Seb.Vis.UI.UI.DrawText(sectionTitle, theme.FontRegular, theme.FontSizeRegular * 0.8f, topLeft, Anchor.TopLeft, titleColor);
			topLeft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * 0.3f;
			
			// Section items
			string combinedText = string.Join("\n\n", items);
			string wrappedText = WrapText(combinedText, (int)(width / (theme.FontSizeRegular * 0.6f * 0.6f)));
			Seb.Vis.UI.UI.DrawText(wrappedText, theme.FontRegular, theme.FontSizeRegular * 0.6f, topLeft, Anchor.TopLeft, Color.white);
			topLeft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * sectionSpacing;
		}


	static void DrawVersionInfo()
	{
		DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
		Seb.Vis.UI.UI.DrawPanel(Seb.Vis.UI.UI.BottomLeft, new Vector2(Seb.Vis.UI.UI.Width, 4), ColHelper.MakeCol255(37, 37, 43), Anchor.BottomLeft);

		float pad = 1;
		Color col = new(1, 1, 1, 0.5f);
		Color modColor = new(0.98f, 0.76f, 0.26f);
		Color mobileColor = new(0.26f, 0.76f, 0.98f);

		// Bottom row (grey text)
        Vector2 versionPos = Seb.Vis.UI.UI.PrevBounds.CentreLeft + Vector2.right * pad;
		Vector2 datePos = Seb.Vis.UI.UI.PrevBounds.CentreRight + Vector2.left * pad;
		
		// Top row (mobile/ComEdit) - positioned above bottom row, same X alignment
		Vector2 mobilePos = versionPos + Vector2.up * 3.5f; // Same X as "Created by", 3.5 units up
		Vector2 moddedPos = datePos + Vector2.up * 3.5f;    // Same X as "Version", 3.5 units up

		Seb.Vis.UI.UI.DrawText(authorString, theme.FontRegular, theme.FontSizeRegular, versionPos, Anchor.TextCentreLeft, col);
		Seb.Vis.UI.UI.DrawText(versionString, theme.FontRegular, theme.FontSizeRegular, datePos, Anchor.TextCentreRight, col);
		if (activeMenuScreen == MenuScreen.Main || activeMenuScreen == MenuScreen.About)
		{
        	Seb.Vis.UI.UI.DrawText(moddedString, theme.FontRegular, theme.FontSizeRegular, moddedPos, Anchor.TextCentreRight, modColor);
        	Seb.Vis.UI.UI.DrawText(mobileString, theme.FontRegular, theme.FontSizeRegular, mobilePos, Anchor.TextCentreLeft, mobileColor);
		}
    }
        static string ResolutionToString(Vector2Int r) => $"{r.x} x {r.y}";

		static void Quit()
		{
			#if UNITY_EDITOR
				// There should be a NullReferenceException when quitting, but it does not affect the application.
				UnityEditor.EditorApplication.isPlaying = false;
			#else
				Application.Quit();
			#endif
		}

		enum MenuScreen
		{
			Main,
			LoadProject,
			Settings,
			ProjectSharing,
			About
		}

	enum PopupKind
	{
		None,
		DeleteConfirmation,
		NamePopup_RenameProject,
		NamePopup_DuplicateProject,
		NamePopup_NewProject,
		OverwriteConfirmation,
		ProjectCreationError,
		PatchNotes,
		ProjectSharing_CreateAccount,
		ProjectSharing_Login,
		ProjectSharing_UploadConfirm,
		ProjectSharing_UploadDisplayName,
		ProjectSharing_ImportList,
		ProjectSharing_MyProjects,
		ProjectSharing_DeleteConfirm,
		ProjectSharing_EditEntry,
		ProjectSharing_ChangeUsername,
	}
	}
}
