using System;
using System.Collections.Generic;
using System.Linq;
using DLS.Description;
using DLS.Game;
using DLS.SaveSystem;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	public static class GroupSaveMenu
	{
		const string MaxLengthName = "MY VERY LONG GROUP NAME";
		static readonly UIHandle ID_NameField = new("GroupSaveMenu_NameField");
		static readonly Func<string, bool> nameValidator = ValidateNameInput;

		static readonly string[] CancelSaveButtonNames = { "CANCEL", "SAVE" };
		static readonly bool[] ButtonGroupInteractStates = { true, true };

		// Captured when menu opens (before CancelEverything clears selection)
		static List<IMoveable> capturedElements;

		public static void OnMenuOpened()
		{
			var controller = Project.ActiveProject.controller;
			capturedElements = controller.SelectedElements.Where(e => e is SubChipInstance or DevPinInstance).ToList();
			InputFieldState state = Seb.Vis.UI.UI.GetInputFieldState(ID_NameField);
			state.SetText("");
			state.SelectAll();
		}

		public static void DrawMenu()
		{
			MenuHelper.DrawBackgroundOverlay();
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			InputFieldTheme inputTheme = theme.ChipNameInputField;
			Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				(Vector2 inputFieldSize, float pad) = ChipSaveMenu.GetTextInputSize();
				InputFieldState inputFieldState = Seb.Vis.UI.UI.InputField(ID_NameField, inputTheme, new Vector2(50, 33), inputFieldSize, "Name", Anchor.Centre, pad, nameValidator, true);

				Vector2 buttonTopLeft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * (DrawSettings.DefaultButtonSpacing * 2);
				bool saveEnabled = IsValidGroupName(inputFieldState.text);
				ButtonGroupInteractStates[1] = saveEnabled;
				int buttonIndex = Seb.Vis.UI.UI.HorizontalButtonGroup(CancelSaveButtonNames, ButtonGroupInteractStates, theme.ButtonTheme, buttonTopLeft, Seb.Vis.UI.UI.PrevBounds.Width, DrawSettings.DefaultButtonSpacing, 0, Anchor.TopLeft);

				if (buttonIndex == 0 || KeyboardShortcuts.CancelShortcutTriggered)
				{
					Cancel();
				}
				else if (buttonIndex == 1 || KeyboardShortcuts.ConfirmShortcutTriggered)
				{
					Save(inputFieldState.text);
				}

				Bounds2D uiBounds = Seb.Vis.UI.UI.GetCurrentBoundsScope();
				MenuHelper.DrawReservedMenuPanel(panelID, uiBounds);
			}
		}

		static bool ValidateNameInput(string nameInput) => nameInput.Length <= MaxLengthName.Length && !SaveUtils.NameContainsForbiddenChar(nameInput);

		static bool IsValidGroupName(string name)
		{
			if (string.IsNullOrWhiteSpace(name) || !SaveUtils.ValidFileName(name)) return false;
			// Name must not conflict with existing chip (groups share namespace with chips)
			if (Project.ActiveProject.chipLibrary.HasChip(name)) return false;
			return true; // Allow overwriting existing group
		}

		static void Cancel()
		{
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
		}

		static void Save(string name)
		{
			var project = Project.ActiveProject;
			if (capturedElements == null || capturedElements.Count < 2) return;

			var groupDesc = DescriptionCreator.CreateGroupDescription(name.Trim(), capturedElements, project.ViewedChip);
			Saver.SaveGroup(groupDesc, project.description.ProjectName);
			project.chipLibrary.NotifyGroupSaved(groupDesc);
			project.SetStarred(name.Trim(), true, false, false);
			SearchPopup.AddRecentChip(name.Trim());

			UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
		}
	}
}
