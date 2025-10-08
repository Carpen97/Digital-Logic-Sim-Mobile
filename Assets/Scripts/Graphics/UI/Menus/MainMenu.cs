using System;
using System.Linq;
using DLS.Description;
using DLS.Game;
using DLS.SaveSystem;
using DLS.Simulation;
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

		#if UNITY_ANDROID || UNITY_IOS
		static readonly string[] SettingsWheelFullScreenOptions = { "AUTO","WINDOWED", "MAXIMIZED", "BORDERLESS", "EXCLUSIVE" };
		static readonly FullScreenMode[] FullScreenModes = { FullScreenMode.Windowed, FullScreenMode.MaximizedWindow, FullScreenMode.FullScreenWindow, FullScreenMode.ExclusiveFullScreen };
		#else
		static readonly string[] SettingsWheelFullScreenOptions = { "WINDOWED", "MAXIMIZED", "BORDERLESS", "EXCLUSIVE" };
		static readonly FullScreenMode[] FullScreenModes = { FullScreenMode.Windowed, FullScreenMode.MaximizedWindow, FullScreenMode.FullScreenWindow, FullScreenMode.ExclusiveFullScreen };
		#endif
		static readonly string[] SettingsWheelOrientationOptions = { "LEFT LANDSCAPE", "RIGHT LANDSCAPE"};
		static readonly string[] SettingsWheelBottomBarScrollingOptions = { "ARROWS","ARROWS (inverted)", "OFF"};
		static readonly string[] SettingsWheelUIScalingOptions = { "SMALL","MEDIUM", "LARGE"};
		static readonly string[] SettingsWheelVSyncOptions = { "DISABLED", "ENABLED" };

		static readonly Func<string, bool> projectNameValidator = ProjectNameValidator;
		static readonly Seb.Vis.UI.UI.ScrollViewDrawContentFunc loadProjectScrollViewDrawer = DrawAllProjectsInScrollView;


		static readonly string[] menuButtonNames =
		{
			FormatButtonString("New Project"),
			FormatButtonString("Open Project"),
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
		}
		}

		public static void OnMenuOpened()
		{
			activeMenuScreen = MenuScreen.Main;
			activePopup = PopupKind.None;
			selectedProjectIndex = -1;
		}

		static void DrawMainScreen()
		{
			if (activePopup != PopupKind.None) return;

			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;

			#if UNITY_ANDROID || UNITY_IOS
			float buttonWidth = 40;
			int buttonIndex = Seb.Vis.UI.UI.VerticalButtonGroup(menuButtonNames, theme.MainMenuButtonTheme, Seb.Vis.UI.UI.Centre + Vector2.up * 8, new Vector2(buttonWidth, 0.5f), false, true, 1);
			#else
			float buttonWidth = 15;
			int buttonIndex = Seb.Vis.UI.UI.VerticalButtonGroup(menuButtonNames, theme.MainMenuButtonTheme, Seb.Vis.UI.UI.Centre + Vector2.up * 6, new Vector2(buttonWidth, 0), false, true, 1);
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
			else if (buttonIndex == 2 || KeyboardShortcuts.MainMenu_SettingsShortcutTriggered) // Settings
			{
				EditedAppSettings = Main.ActiveAppSettings;
				activeMenuScreen = MenuScreen.Settings;
				OnSettingsMenuOpened();
			}
			else if (buttonIndex == 3) // About
			{
				activeMenuScreen = MenuScreen.About;
			}
			else if (buttonIndex == 4 || KeyboardShortcuts.MainMenu_QuitShortcutTriggered) // Quit
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
			Seb.Vis.UI.UI.GetInputFieldState(ID_ProjectNameInput).ClearText();
			activeMenuScreen = MenuScreen.Main;
			activePopup = PopupKind.None;
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

			float regionWidth = 30;
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
			Seb.Vis.UI.UI.DrawText(displayMessage, theme.FontRegular, theme.FontSizeRegular * 0.7f, pos + Vector2.up * (popupSize.y / 2 - 3), Anchor.TopCentre, Color.red);
			
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
					foreach (string log in projectCreationDebugLogs)
					{
						Color logColor = Color.white;
						if (log.Contains("Error") || log.Contains("Exception"))
							logColor = new Color(1f, 0.3f, 0.3f);
						else if (log.Contains("null"))
							logColor = new Color(1f, 0.7f, 0.3f);
						
						Seb.Vis.UI.UI.DrawText(log, theme.FontRegular, theme.FontSizeRegular * 0.5f, topLeft, Anchor.TopLeft, logColor);
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

		static void DrawAboutScreen()
		{
			ButtonTheme theme = DrawSettings.ActiveUITheme.MainMenuButtonTheme;
			
			// Add a bounds scope for the logos
			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
			string about_text = 
			"This project is based on Digital Logic Sim, created by Sebastian Lague.\n" +
			"This mobile version was ported and adapted by David Carpenfelt to make it accessible on Android devices.\n\n" +
			"If you need inspiration for how to play the game, check out Sebastian's YouTube playlist:\n";

			Seb.Vis.UI.UI.DrawText(about_text, theme.font, theme.fontSize*0.6f, Seb.Vis.UI.UI.Centre + Vector2.up * 10, Anchor.Centre, Color.white);
			
			// Draw YouTube logo using PNG texture
			Debug.Log($"[MainMenu] Attempting to load YouTube logo from: UI/Logos/YouTube");
			// Try loading from the actual location - Assets/UI/Logos/
			Texture2D youtubeLogo = Resources.Load<Texture2D>("UI/Logos/YouTube");
			Debug.Log($"[MainMenu] YouTube Logo Loaded: {(youtubeLogo != null ? youtubeLogo.name : "NULL")}");
			
			// Debug: List all available resources
			UnityEngine.Object[] allResources = Resources.LoadAll("UI/Logos");
			Debug.Log($"[MainMenu] Available resources in UI/Logos: {allResources.Length}");
			foreach (UnityEngine.Object obj in allResources)
			{
				Debug.Log($"[MainMenu] Found resource: {obj.name} ({obj.GetType().Name})");
			}
			
			// Try alternative paths
			Debug.Log($"[MainMenu] Trying alternative paths...");
			Texture2D altYoutube = Resources.Load<Texture2D>("YouTube");
			Debug.Log($"[MainMenu] Alternative YouTube path result: {(altYoutube != null ? altYoutube.name : "NULL")}");
			
			// Try loading from root Resources folder
			UnityEngine.Object[] rootResources = Resources.LoadAll("");
			Debug.Log($"[MainMenu] Root resources count: {rootResources.Length}");
			foreach (UnityEngine.Object obj in rootResources)
			{
				if (obj.name.ToLower().Contains("youtube") || obj.name.ToLower().Contains("discord"))
				{
					Debug.Log($"[MainMenu] Found potential logo: {obj.name} ({obj.GetType().Name})");
				}
			}
			if (youtubeLogo != null)
			{
				Vector2 youtubePos = Seb.Vis.UI.UI.Centre + Vector2.up * 2;
				Vector2 youtubeSize = new Vector2(3, 3);
				Debug.Log($"[MainMenu] Drawing YouTube Logo at Pos: {youtubePos}, Size: {youtubeSize}");
				Seb.Vis.UI.UI.DrawTextureDirect(youtubePos, youtubeSize, youtubeLogo, Color.white);
			}
			
			if (Seb.Vis.UI.UI.Button("Link to YouTube", theme, Seb.Vis.UI.UI.Centre + Vector2.up * 2, Vector2.zero, true, true, true, theme.buttonCols))
			{
				BackToMain();
				Application.OpenURL("https://www.youtube.com/watch?v=QZwneRb-zqA&list=PLFt_AvWsXl0dPhqVsKt1Ni_46ARyiCGSq");
			}

			string feedback_text = 
			"If you'd like to give feedback please visit this discord thread\n";

			Seb.Vis.UI.UI.DrawText(feedback_text, theme.font, theme.fontSize*0.6f, Seb.Vis.UI.UI.CentreBottom + Vector2.up * 24, Anchor.Centre, Color.white);
			
			// Draw Discord logo using PNG texture
			Texture2D discordLogo = Resources.Load<Texture2D>("UI/Logos/Discord");
			Debug.Log($"[MainMenu] Discord Logo Loaded: {(discordLogo != null ? discordLogo.name : "NULL")}");
			if (discordLogo != null)
			{
				Vector2 discordPos = Seb.Vis.UI.UI.CentreBottom + Vector2.up * 18;
				Vector2 discordSize = new Vector2(3, 3);
				Debug.Log($"[MainMenu] Drawing Discord Logo at Pos: {discordPos}, Size: {discordSize}");
				Seb.Vis.UI.UI.DrawTextureDirect(discordPos, discordSize, discordLogo, Color.white);
			}
			
			if (Seb.Vis.UI.UI.Button("Link to Discord", theme, Seb.Vis.UI.UI.CentreBottom + Vector2.up * 18, Vector2.zero, true, true, true, theme.buttonCols))
			{
				BackToMain();
				Application.OpenURL("https://discord.com/channels/1361307968276136007/1366859789711315106");

			}

			if (Seb.Vis.UI.UI.Button("Back", theme, Seb.Vis.UI.UI.CentreBottom + Vector2.up * 10, Vector2.zero, true, true, true, theme.buttonCols))
			{
				BackToMain();
			}
			} // End bounds scope
		}


		static void DrawVersionInfo()
		{
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			Seb.Vis.UI.UI.DrawPanel(Seb.Vis.UI.UI.BottomLeft, new Vector2(Seb.Vis.UI.UI.Width, 4), ColHelper.MakeCol255(37, 37, 43), Anchor.BottomLeft);

			float pad = 1;
			Color col = new(1, 1, 1, 0.5f);
			Color modColor = new(0.98f, 0.76f, 0.26f);
			Color mobileColor = new(0.26f, 0.76f, 0.98f);

            Vector2 versionPos = Seb.Vis.UI.UI.PrevBounds.CentreLeft + Vector2.right * pad;
			Vector2 datePos = Seb.Vis.UI.UI.PrevBounds.CentreRight + Vector2.left * pad;
			Vector2 moddedPos = Seb.Vis.UI.UI.PrevBounds.Centre + Vector2.up * 3.8f+ Vector2.right * pad*8.7f;
			Vector2 mobilePos = Seb.Vis.UI.UI.PrevBounds.Centre + Vector2.up * 3.8f+ Vector2.left * pad*9.9f;

			Seb.Vis.UI.UI.DrawText(authorString, theme.FontRegular, theme.FontSizeRegular, versionPos, Anchor.TextCentreLeft, col);
			Seb.Vis.UI.UI.DrawText(versionString, theme.FontRegular, theme.FontSizeRegular, datePos, Anchor.TextCentreRight, col);
			if (activeMenuScreen == MenuScreen.Main)
			{
            	Seb.Vis.UI.UI.DrawText(moddedString, theme.FontRegular, theme.FontSizeRegular, moddedPos, Anchor.TextCentreLeft, modColor);
            	Seb.Vis.UI.UI.DrawText(mobileString, theme.FontRegular, theme.FontSizeRegular, mobilePos, Anchor.TextCentreRight, mobileColor);
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
	}
	}
}
