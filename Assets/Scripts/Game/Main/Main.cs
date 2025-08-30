using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DLS.Description;
using DLS.Graphics;
using DLS.SaveSystem;
using UnityEngine;

namespace DLS.Game
{
	public static class Main
	{
		public static readonly Version DLSVersion = new(2, 1, 6);
		public static readonly Version DLSVersion_EarliestCompatible = new(2, 0, 0);
		public static readonly Version DLSVersion_ModdedID = new(1, 1, 2);
		public const string LastUpdatedString = "5 May 2025";
		public const string LastUpdatedModdedString = "10 August 2025";
		public static AppSettings ActiveAppSettings;

		public static Project ActiveProject { get; private set; }

		public static Vector2Int FullScreenResolution => new(Display.main.systemWidth, Display.main.systemHeight);
		public static AudioState audioState;

		public static void Init(AudioState audioState)
		{
			SavePaths.EnsureDirectoryExists(SavePaths.ProjectsPath);
			SaveAndApplyAppSettings(Loader.LoadAppSettings());
			Main.audioState = audioState;
		}

		public static void Update()
		{
			if (UIDrawer.ActiveMenu != UIDrawer.MenuType.MainMenu)
			{
				CameraController.Update();
				#if UNITY_ANDROID || UNITY_IOS 
				InteractionState.ClearFrame();
				WorldDrawer.DrawWorld(ActiveProject);
				ActiveProject.Update();
				#else
				ActiveProject.Update();
				InteractionState.ClearFrame();
				WorldDrawer.DrawWorld(ActiveProject);
				#endif
			}

			UIDrawer.Draw();

			HandleGlobalInput();
		}


		public static void SaveAndApplyAppSettings(AppSettings newSettings)
		{
			// Save new settings
			ActiveAppSettings = newSettings;
			Saver.SaveAppSettings(newSettings);

			DrawSettings.UpdateTheme();
			// Apply settings to app
			//int width = newSettings.AutoResolution ? newSettings.ResolutionX : FullScreenResolution.x;
			//int height = newSettings.AutoResolution ? newSettings.ResolutionY : FullScreenResolution.y;

			int width = newSettings.ResolutionX;
			int height = newSettings.ResolutionY;
			Screen.SetResolution(width, height, newSettings.fullscreenMode);

			QualitySettings.vSyncCount = newSettings.VSyncEnabled ? 1 : 0;
			if(Screen.orientation == ScreenOrientation.LandscapeRight && newSettings.orientationIsLeftLandscape){
				Screen.orientation = ScreenOrientation.LandscapeLeft;
			}else if (Screen.orientation == ScreenOrientation.LandscapeLeft && !newSettings.orientationIsLeftLandscape){
				Screen.orientation = ScreenOrientation.LandscapeRight;
			}
			BottomBarUI.showScrollingButtons = newSettings.showScrollingButtons;
		}

		public static void LoadMainMenu()
		{
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.MainMenu);
		}

		public static void CreateOrLoadProject(string projectName, string startupChipName = "")
		{
			if (Loader.ProjectExists(projectName)) { ActiveProject = LoadProject(projectName); Saver.SaveProjectDescription(ActiveProject.description); }
			else ActiveProject = CreateProject(projectName);

			ActiveProject.LoadDevChipOrCreateNewIfDoesntExist(startupChipName);
			ActiveProject.StartSimulation();
			ActiveProject.audioState = audioState;
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
		}

		static Project CreateProject(string projectName)
		{
			ProjectDescription initialDescription = new()
			{
				ProjectName = projectName,
				DLSVersion_LastSaved = DLSVersion.ToString(),
				DLSVersion_LastSavedModdedVersion = DLSVersion_ModdedID.ToString(),
				DLSVersion_EarliestCompatible = DLSVersion_EarliestCompatible.ToString(),
				CreationTime = DateTime.Now,
                TimeSpentSinceCreated = new(),
				Prefs_ChipPinNamesDisplayMode = PreferencesMenu.DisplayMode_OnHover,
				Prefs_MainPinNamesDisplayMode = PreferencesMenu.DisplayMode_OnHover,
				Prefs_SimTargetStepsPerSecond = 1000,
				Prefs_SimStepsPerClockTick = 250,
				Prefs_SimPaused = false,
				AllCustomChipNames = Array.Empty<string>(),
				StarredList = BuiltinCollectionCreator.GetDefaultStarredList().ToList(),
				ChipCollections = new List<ChipCollection>(BuiltinCollectionCreator.CreateDefaultChipCollections()),
				pinBitCounts = Project.PinBitCounts,
				SplitMergePairs = Project.SplitMergePairs
			};

			Saver.SaveProjectDescription(initialDescription);
			return LoadProject(projectName);
		}

		#if UNITY_ANDROID || UNITY_IOS
		public static void ImportProject()
		{
			NativeFilePicker.PickFile((path) =>
			{
				if (string.IsNullOrEmpty(path))
				{
					UnityEngine.Debug.LogWarning("[ImportProject] No file selected.");
					return;
				}

				AndroidIO.ImportProjectFromZip(path);
			}, new[] { "application/zip", "application/octet-stream" });
		}
		public static void ExportProject(string projectName)
		{
			AndroidIO.ExportProjectToZip(projectName);
		}
		#endif


		public static void OpenSaveDataFolderInFileBrowser()
		{
			try
			{
				string path = SavePaths.AllData;

				if (!Directory.Exists(path)) throw new Exception("Path does not not exist: " + path);

				path = path.Replace("\\", "/");
				string url = "file://" + (path.StartsWith("/") ? path : "/" + path);
				Application.OpenURL(url);
			}
			catch (Exception e)
			{
				UnityEngine.Debug.LogError("Error opening folder: " + e.Message);
			}
		}

		static Project LoadProject(string projectName) => Loader.LoadProject(projectName);

		static void HandleGlobalInput()
		{
			if (KeyboardShortcuts.OpenSaveDataFolderShortcutTriggered) OpenSaveDataFolderInFileBrowser();
		}

		public class Version
		{
			public readonly int Major;
			public readonly int Minor;
			public readonly int Patch;
			public readonly int Mobile;
			public Version(int major, int minor, int patch)
			{
				Major = major;
				Minor = minor;
				Patch = patch;
				Mobile = 0;
			}

			public Version(int major, int minor, int patch, int mobile)
			{
				Major = major;
				Minor = minor;
				Patch = patch;
				Mobile = mobile;
			}

			public int ToInt() => Major * 100000 + Minor * 1000 + Patch;

			public static Version Parse(string versionString)
			{
				string[] versionParts = versionString.Split('.');
				int major = int.Parse(versionParts[0]);
				int minor = int.Parse(versionParts[1]);
				int patch = int.Parse(versionParts[2]);
				if(versionParts.Length==3)
					return new Version (major, minor, patch);
				int mobile = int.Parse(versionParts[3]); //Parse one more number for mobile
				return new Version(major, minor, patch, mobile);
			}

			public static bool TryParse(string versionString, out Version version)
			{
				try
				{
					version = Parse(versionString);
					return true;
				}
				catch
				{
					version = null;
					return false;
				}
			}

			public override string ToString() => $"{Major}.{Minor}.{Patch}.{Mobile}";
		}
	}
}