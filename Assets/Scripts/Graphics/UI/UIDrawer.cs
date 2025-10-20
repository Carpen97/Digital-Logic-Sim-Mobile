using System.Diagnostics;
using DLS.Game;
using DLS.Game.LevelsIntegration;
using DLS.Simulation;
using Seb.Vis.UI;

namespace DLS.Graphics
{
	public static class UIDrawer
	{
		public enum MenuType
		{
			None,
			ChipSave,
			ChipLibrary,
			BottomBarMenuPopup,
			ChipCustomization,
			Preferences,
			PinRename,
			MainMenu,
			RebindKeyChip,
			RomEdit,
			ChipStats,
			CollectionStats,
			ProjectStats,
			PulseEdit,
			ConstantEdit,
			UnsavedChanges,
			LevelUnsavedChanges,
			Search,
			ChipLabelPopup,
			SpecialChipMaker,
			Overwrite,
			DeleteConfirmation,
			Levels,
			LevelValidationResult,
			LevelCompleted,
			Leaderboard,
			HallOfFame,
			ScoreExplanation,
			CachingExplanation,
			UserNameInput,
			SimpleMessage,
			ChipDescription
		}

		static MenuType activeMenuOld;

		public static MenuType ActiveMenu { get; private set; }

        public static void Draw()
		{
			NotifyIfActiveMenuChanged();

			using (Seb.Vis.UI.UI.CreateFixedAspectUIScope(drawLetterbox: true))
			{
				if (ActiveMenu is MenuType.MainMenu)
				{
					DrawAppMenus();
				}
				else
				{
					DrawProjectMenus(Project.ActiveProject);
				}
			}

			InteractionState.MouseIsOverUI = Seb.Vis.UI.UI.IsMouseOverUIThisFrame;
		}

		static void DrawAppMenus()
		{
			MainMenu.Draw();
		}

	static void DrawProjectMenus(Project project)
	{
		MenuType menuToDraw = ActiveMenu; // cache state in case it changes while drawing/updating the menus
		
		// DEBUG: Log active menu if it's one we're interested in
		if (menuToDraw == MenuType.UserNameInput || menuToDraw == MenuType.LevelValidationResult)
		{
			UnityEngine.Debug.Log($"[UIDrawer] DrawProjectMenus - menuToDraw: {menuToDraw}");
		}

		if (menuToDraw != MenuType.ChipCustomization) BottomBarUI.DrawUI(project);

			bool aMenuIsOpen = true;
			
			if (menuToDraw == MenuType.DeleteConfirmation) DeleteConfirmationPopup.DrawMenu();
			else if (menuToDraw == MenuType.ChipSave) ChipSaveMenu.DrawMenu();
			else if (menuToDraw == MenuType.ChipLibrary) ChipLibraryMenu.DrawMenu();
			else if (menuToDraw == MenuType.ChipCustomization) ChipCustomizationMenu.DrawMenu();
			else if (menuToDraw == MenuType.Preferences) PreferencesMenu.DrawMenu(project);
			else if (menuToDraw == MenuType.PinRename) PinEditMenu.DrawMenu();
			else if (menuToDraw == MenuType.RebindKeyChip) RebindKeyChipMenu.DrawMenu();
			else if (menuToDraw == MenuType.RomEdit) RomEditMenu.DrawMenu();
			else if (menuToDraw == MenuType.ChipStats) ChipStatsMenu.DrawMenu(); 
			else if (menuToDraw == MenuType.CollectionStats) CollectionStatsMenu.DrawMenu();
			else if (menuToDraw == MenuType.ProjectStats) ProjectStatsMenu.DrawMenu();
			else if (menuToDraw == MenuType.UnsavedChanges) UnsavedChangesPopup.DrawMenu();
			else if (menuToDraw == MenuType.LevelUnsavedChanges) LevelUnsavedChangesPopup.DrawMenu();
			else if (menuToDraw == MenuType.Overwrite) ConfirmOverwritePopup.DrawMenu();
			else if (menuToDraw == MenuType.Search) SearchPopup.DrawMenu();
			else if (menuToDraw == MenuType.ChipLabelPopup) ChipLabelMenu.DrawMenu();
			else if (menuToDraw == MenuType.PulseEdit) PulseEditMenu.DrawMenu();
			else if (menuToDraw == MenuType.ConstantEdit)  ConstantEditMenu.DrawMenu();
			else if (menuToDraw == MenuType.SpecialChipMaker) SpecialChipMakerMenu.DrawMenu();
			else if (menuToDraw == MenuType.Levels) LevelsMenu.DrawMenu();
			else if (menuToDraw == MenuType.LevelValidationResult) LevelValidationPopup.DrawMenu();
			else if (menuToDraw == MenuType.Leaderboard) LeaderboardPopup.DrawMenu();
			else if (menuToDraw == MenuType.HallOfFame) HallOfFameMenu.DrawMenu();
			else if (menuToDraw == MenuType.ScoreExplanation) ScoreExplanationPopup.DrawMenu();
		else if (menuToDraw == MenuType.CachingExplanation) CachingExplanationPopup.DrawMenu();
		else if (menuToDraw == MenuType.UserNameInput)
		{
			UnityEngine.Debug.Log($"[UIDrawer] About to call UserNameInputPopup.DrawMenu()");
			UserNameInputPopup.DrawMenu();
			UnityEngine.Debug.Log($"[UIDrawer] UserNameInputPopup.DrawMenu() completed");
		}
			else if (menuToDraw == MenuType.SimpleMessage) SimpleMessagePopup.DrawMenu();
			else if (menuToDraw == MenuType.ChipDescription) ChipDescriptionMenu.DrawMenu();
			else
			{
				bool showSimPausedBanner = project.simPaused;
				bool showLevelBanner = LevelManager.Instance.IsActive;
				bool showEraserBanner = DLS.Game.EraserModeController.IsActive;
				bool showWirePlacementBanner = project.controller?.IsCreatingWire ?? false;
				
				// Priority order: WirePlacement > Eraser > SimPaused > Level
				if (showWirePlacementBanner) WirePlacementBanner.DrawBanner();
				else if (showEraserBanner) EraserModeBanner.DrawBanner();
				else if (showSimPausedBanner) SimPausedUI.DrawPausedBanner();
				else if (showLevelBanner) LevelBannerUI.DrawLevelBanner();
				
				if (project.chipViewStack.Count > 1) ViewedChipsBar.DrawViewedChipsBanner(project, showSimPausedBanner);
				if (SimChip.isCreatingACache) CreateCacheUI.DrawCreatingCacheInfo();
				
				aMenuIsOpen = false;
			}
			// Cancel current caching process when a menu gets opened
			if(aMenuIsOpen)
				SimChip.AbortCache();

			ContextMenu.Update();
		}

		public static bool InInputBlockingMenu() => !(ActiveMenu is MenuType.None or MenuType.BottomBarMenuPopup or MenuType.ChipCustomization);

		static void NotifyIfActiveMenuChanged()
		{
			// UI Changed -- notify opened
			if (ActiveMenu != activeMenuOld)
			{
				if (activeMenuOld == MenuType.ChipCustomization) CustomizationSceneDrawer.OnCustomizationMenuClosed();

				if (ActiveMenu == MenuType.ChipSave) ChipSaveMenu.OnMenuOpened();
				else if (ActiveMenu == MenuType.ChipLibrary) ChipLibraryMenu.OnMenuOpened();
				else if (ActiveMenu == MenuType.ChipCustomization) ChipCustomizationMenu.OnMenuOpened();
				else if (ActiveMenu == MenuType.PinRename) PinEditMenu.OnMenuOpened();
				else if (ActiveMenu == MenuType.Preferences) PreferencesMenu.OnMenuOpened();
				else if (ActiveMenu == MenuType.MainMenu) MainMenu.OnMenuOpened();
				else if (ActiveMenu == MenuType.RebindKeyChip) RebindKeyChipMenu.OnMenuOpened();
				else if (ActiveMenu == MenuType.RomEdit) RomEditMenu.OnMenuOpened();
				else if (ActiveMenu == MenuType.Search) SearchPopup.OnMenuOpened();
				else if (ActiveMenu == MenuType.ChipLabelPopup) ChipLabelMenu.OnMenuOpened();
				else if (ActiveMenu == MenuType.PulseEdit) PulseEditMenu.OnMenuOpened();
				else if (ActiveMenu == MenuType.ProjectStats) ProjectStatsMenu.OnMenuOpened();
                else if (ActiveMenu == MenuType.ConstantEdit) ConstantEditMenu.OnMenuOpened();
				else if (ActiveMenu == MenuType.SpecialChipMaker) SpecialChipMakerMenu.OnMenuOpened();
				else if (ActiveMenu == MenuType.HallOfFame) HallOfFameMenu.OnMenuOpened();
				else if (ActiveMenu == MenuType.Levels) LevelsMenu.OnMenuOpened();
				else if (ActiveMenu == MenuType.ChipDescription) ChipDescriptionMenu.OnMenuOpened();


				if (InInputBlockingMenu() && Project.ActiveProject != null && Project.ActiveProject.controller != null)
				{
					Project.ActiveProject.controller.CancelEverything();
				}

				activeMenuOld = ActiveMenu;
			}
		}

		public static void ToggleBottomPopupMenu()
		{
			SetActiveMenu(ActiveMenu is MenuType.None ? MenuType.BottomBarMenuPopup : MenuType.None);
		}

	public static void SetActiveMenu(MenuType type)
	{
		// DEBUG: Log menu changes for UserNameInput and LevelValidationResult
		if (type == MenuType.UserNameInput || type == MenuType.LevelValidationResult || 
		    ActiveMenu == MenuType.UserNameInput || ActiveMenu == MenuType.LevelValidationResult)
		{
			UnityEngine.Debug.Log($"[UIDrawer] SetActiveMenu: {ActiveMenu} -> {type}");
		}
		ActiveMenu = type;
	}


		public static void Reset()
		{
			SetActiveMenu(MenuType.None);
			activeMenuOld = MenuType.None;
			ContextMenu.Reset();
			Seb.Vis.UI.UI.ResetAllStates();
			BottomBarUI.Reset();
			ChipSaveMenu.Reset();
			RomEditMenu.Reset();
			ChipLibraryMenu.Reset();
			SearchPopup.Reset();
			ChipDescriptionMenu.Reset();
		}
	}
}
