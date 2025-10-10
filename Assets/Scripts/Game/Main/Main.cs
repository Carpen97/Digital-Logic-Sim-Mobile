using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DLS.Description;
using DLS.Graphics;
using DLS.SaveSystem;
using DLS.Game.LevelsIntegration;
using DLS.Online;
using DLS.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace DLS.Game
{
	public static class Main
	{
		public static readonly Version DLSVersion = new(2, 1, 6, 9);
		public static readonly Version DLSVersion_EarliestCompatible = new(2, 0, 0);
		public static readonly CEVersion DLSVersion_ModdedID = new(1, 1, 2);
		public const string LastUpdatedString = "7 Oct 2025";
		public const string LastUpdatedModdedString = "10 Aug 2025";
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
		List<string> debugLogs = new List<string>();
		
		try
		{
			debugLogs.Add($"[Main] CreateOrLoadProject called with projectName: '{projectName}', startupChipName: '{startupChipName}'");
			UnityEngine.Debug.Log(debugLogs[debugLogs.Count - 1]);
			
			debugLogs.Add($"[Main] audioState is null: {audioState == null}");
			UnityEngine.Debug.Log(debugLogs[debugLogs.Count - 1]);
			
			if (Loader.ProjectExists(projectName)) 
			{ 
				debugLogs.Add($"[Main] Project exists, loading: {projectName}");
				UnityEngine.Debug.Log(debugLogs[debugLogs.Count - 1]);
				
			ActiveProject = LoadProject(projectName);
			debugLogs.Add($"[Main] ActiveProject loaded, is null: {ActiveProject == null}");
			UnityEngine.Debug.Log(debugLogs[debugLogs.Count - 1]);
			
			if (ActiveProject != null)
			{
				Saver.SaveProjectDescription(ActiveProject.description);
				debugLogs.Add($"[Main] Project description saved");
				UnityEngine.Debug.Log(debugLogs[debugLogs.Count - 1]);
			}
			else
			{
				string errorLog = $"[Main] ActiveProject is null!";
				debugLogs.Add(errorLog);
				UnityEngine.Debug.LogError(errorLog);
			}
			}
			else 
			{
				debugLogs.Add($"[Main] Project doesn't exist, creating: {projectName}");
				UnityEngine.Debug.Log(debugLogs[debugLogs.Count - 1]);
				
				ActiveProject = CreateProject(projectName);
				debugLogs.Add($"[Main] ActiveProject created, is null: {ActiveProject == null}");
				UnityEngine.Debug.Log(debugLogs[debugLogs.Count - 1]);
			}

			if (ActiveProject == null)
			{
				throw new Exception("Failed to create or load project - ActiveProject is null");
			}

			debugLogs.Add($"[Main] Loading dev chip or creating new: {startupChipName}");
			UnityEngine.Debug.Log(debugLogs[debugLogs.Count - 1]);
			ActiveProject.LoadDevChipOrCreateNewIfDoesntExist(startupChipName);
			
			debugLogs.Add($"[Main] Starting simulation");
			UnityEngine.Debug.Log(debugLogs[debugLogs.Count - 1]);
			ActiveProject.StartSimulation();
			
			debugLogs.Add($"[Main] Setting audioState (audioState is null: {audioState == null})");
			UnityEngine.Debug.Log(debugLogs[debugLogs.Count - 1]);
			ActiveProject.audioState = audioState;
			
			debugLogs.Add($"[Main] Setting menu to None");
			UnityEngine.Debug.Log(debugLogs[debugLogs.Count - 1]);
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
			
			debugLogs.Add($"[Main] CreateOrLoadProject completed successfully");
			UnityEngine.Debug.Log(debugLogs[debugLogs.Count - 1]);
		}
		catch (Exception ex)
		{
			debugLogs.Add($"[ERROR] Exception type: {ex.GetType().Name}");
			debugLogs.Add($"[ERROR] Message: {ex.Message}");
			debugLogs.Add($"[ERROR] Stack trace: {ex.StackTrace}");
			
			UnityEngine.Debug.LogError($"[Main] CreateOrLoadProject failed at line: {new System.Diagnostics.StackTrace(ex, true).GetFrame(0)?.GetFileLineNumber()}");
			UnityEngine.Debug.LogError($"[Main] Exception type: {ex.GetType().Name}");
			UnityEngine.Debug.LogError($"[Main] Message: {ex.Message}");
			UnityEngine.Debug.LogError($"[Main] Stack trace: {ex.StackTrace}");
			
			// Show error popup to user with debug logs
			string userFriendlyMessage = GetUserFriendlyErrorMessage(ex);
			DLS.Graphics.MainMenu.ShowProjectCreationError(userFriendlyMessage, debugLogs);
			
			// Make sure we go back to main menu
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.MainMenu);
		}
	}

		static Project CreateProject(string projectName)
		{
			try
			{
				UnityEngine.Debug.Log($"[Main] CreateProject called with projectName: '{projectName}'");
				
				ProjectDescription initialDescription = new()
				{
					ProjectName = projectName ?? "Untitled",
					DLSVersion_LastSaved = DLSVersion?.ToString() ?? "2.1.7.0",
					DLSVersion_LastSavedModdedVersion = DLSVersion_ModdedID?.ToString() ?? "1.1.2",
					DLSVersion_EarliestCompatible = DLSVersion_EarliestCompatible?.ToString() ?? "2.0.0",
					CreationTime = DateTime.Now,
					TimeSpentSinceCreated = new(),
					Prefs_ChipPinNamesDisplayMode = PreferencesMenu.DisplayMode_OnHover,
					Prefs_MainPinNamesDisplayMode = PreferencesMenu.DisplayMode_OnHover,
					Prefs_SimTargetStepsPerSecond = 1000,
					Prefs_SimStepsPerClockTick = 250,
					Prefs_SimPaused = false,
					// Prefs_UIThemeMode removed - only Squiggles Theme is used
					Prefs_GridDisplayMode = 1,
					Prefs_UseDragAndDropMode = true,
					AllCustomChipNames = Array.Empty<string>(),
					StarredList = BuiltinCollectionCreator.GetDefaultStarredList()?.ToList() ?? new List<StarredItem>(),
					ChipCollections = new List<ChipCollection>(BuiltinCollectionCreator.CreateDefaultChipCollections() ?? Array.Empty<ChipCollection>()),
					pinBitCounts = Project.PinBitCounts ?? new List<PinBitCount>(),
					SplitMergePairs = Project.SplitMergePairs ?? new List<KeyValuePair<PinBitCount, PinBitCount>>()
				};

				UnityEngine.Debug.Log($"[Main] ProjectDescription created, checking for null properties...");
				UnityEngine.Debug.Log($"[Main] StarredList is null: {initialDescription.StarredList == null}");
				UnityEngine.Debug.Log($"[Main] ChipCollections is null: {initialDescription.ChipCollections == null}");
				UnityEngine.Debug.Log($"[Main] pinBitCounts is null: {initialDescription.pinBitCounts == null}");
				UnityEngine.Debug.Log($"[Main] SplitMergePairs is null: {initialDescription.SplitMergePairs == null}");
				
				// Check nested properties
				if (initialDescription.StarredList != null)
				{
					UnityEngine.Debug.Log($"[Main] StarredList count: {initialDescription.StarredList.Count}");
					for (int i = 0; i < initialDescription.StarredList.Count && i < 3; i++)
					{
						var item = initialDescription.StarredList[i];
						UnityEngine.Debug.Log($"[Main] StarredList[{i}] Name: '{item.Name}'");
					}
				}
				
				if (initialDescription.ChipCollections != null)
				{
					UnityEngine.Debug.Log($"[Main] ChipCollections count: {initialDescription.ChipCollections.Count}");
					for (int i = 0; i < initialDescription.ChipCollections.Count && i < 3; i++)
					{
						var collection = initialDescription.ChipCollections[i];
						UnityEngine.Debug.Log($"[Main] ChipCollections[{i}] is null: {collection == null}");
						if (collection != null)
						{
							UnityEngine.Debug.Log($"[Main] ChipCollections[{i}].Name: '{collection.Name}'");
							UnityEngine.Debug.Log($"[Main] ChipCollections[{i}].Chips is null: {collection.Chips == null}");
						}
					}
				}

				UnityEngine.Debug.Log($"[Main] Saving project description for: {projectName}");
				Saver.SaveProjectDescription(initialDescription);
				
				UnityEngine.Debug.Log($"[Main] Loading project: {projectName}");
				return LoadProject(projectName);
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.LogError($"[Main] CreateProject failed for '{projectName}': {ex.Message}");
				UnityEngine.Debug.LogError($"[Main] Stack trace: {ex.StackTrace}");
				throw; // Re-throw to maintain existing behavior
			}
		}

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
		},
		#if UNITY_IOS
		new[] { "public.archive", "public.zip-archive", "com.pkware.zip-archive" }
		#else
		new[] { "application/zip", "application/octet-stream" }
		#endif
		);
	}
		public static void ExportProject(string projectName)
		{
			AndroidIO.ExportProjectToZip(projectName);
		}


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

	static string GetUserFriendlyErrorMessage(Exception ex)
	{
		string message = ex.Message;
		
		// Check for common error patterns and provide helpful messages
		if (message.Contains("DirectoryNotFoundException") || message.Contains("Could not find") || message.Contains("does not exist"))
		{
			return "Could not find the project directory. The file system may not be accessible.";
		}
		else if (message.Contains("UnauthorizedAccessException") || message.Contains("Access") || message.Contains("denied"))
		{
			return "Permission denied. The app may not have access to save files.\n\nTry restarting the app or checking iOS settings.";
		}
		else if (message.Contains("IOException") || message.Contains("write") || message.Contains("read"))
		{
			return "File system error. Unable to read or write project files.\n\nTry restarting the app.";
		}
		else if (message.Contains("JSON") || message.Contains("Serializ") || message.Contains("Deserializ"))
		{
			return "Failed to save/load project data.\n\nThe project file may be corrupted.";
		}
		else
		{
			// Return a shortened version of the actual error
			string shortMessage = message.Length > 150 ? message.Substring(0, 150) + "..." : message;
			return $"An error occurred:\n\n{shortMessage}\n\nCheck Unity logs for details.";
		}
	}

		static void HandleGlobalInput()
		{
			if (KeyboardShortcuts.OpenSaveDataFolderShortcutTriggered) OpenSaveDataFolderInFileBrowser();
			
			// Handle level validation shortcut for PC testing
			if (DLS.Game.KeyboardShortcuts.ValidateLevelShortcutTriggered)
			{
				// Only trigger if we're in a level and no menu is open
				if (LevelManager.Instance?.IsActive == true && UIDrawer.ActiveMenu == UIDrawer.MenuType.None)
				{
					UnityEngine.Debug.Log("[Main] Ctrl+V pressed - triggering level validation");
					var report = LevelManager.Instance.RunValidation();
					LevelValidationPopup.Open(report);
				}
			}
			
			// Handle clear level progress shortcut for testing
			if (DLS.Game.KeyboardShortcuts.ClearLevelProgressShortcutTriggered)
			{
				// Only trigger if we're in a level and no menu is open
				if (LevelManager.Instance?.IsActive == true && UIDrawer.ActiveMenu == UIDrawer.MenuType.None)
				{
					UnityEngine.Debug.Log("[Main] Ctrl+C pressed - clearing level progress");
					LevelProgressService.ClearLevelProgress(LevelManager.Instance.Current.id);
					UnityEngine.Debug.Log($"[Main] Cleared progress for level: {LevelManager.Instance.Current.id}");
				}
			}
			
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
		public class CEVersion
		{
			public readonly int Major;
			public readonly int Minor;
			public readonly int Patch;
			public readonly int Mobile;
			public CEVersion(int major, int minor, int patch)
			{
				Major = major;
				Minor = minor;
				Patch = patch;
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

			public override string ToString() => $"{Major}.{Minor}.{Patch}";
		}
	}
}