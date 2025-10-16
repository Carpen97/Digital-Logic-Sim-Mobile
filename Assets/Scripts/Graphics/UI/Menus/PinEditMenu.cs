using DLS.Description;
using DLS.Game;
using DLS.Game.LevelsIntegration;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	public static class PinEditMenu
	{
		const string MaxLengthPinName = "MY LONG PIN NAME";
		static DevPinInstance devPin;
		static readonly UIHandle ID_NameField = new("PinEditMenu_NameField");
		static readonly UIHandle ID_ValueDisplayMode = new("PinEditMenu_ValueDisplayMode");

		static readonly string[] CancelConfirmButtonNames =
		{
			"CANCEL", "CONFIRM"
		};

		static readonly bool[] ButtonGroupInteractStates = { true, true };

		static readonly string[] PinDecimalDisplayOptions =
		{
			"Off",
			"Unsigned",
			"Signed",
			"HEX"
		};

		public static void OnMenuOpened()
		{
			InputFieldState inputFieldState = Seb.Vis.UI.UI.GetInputFieldState(ID_NameField);
			inputFieldState.SetText(devPin.Pin.Name);
			inputFieldState.SelectAll();

			Seb.Vis.UI.UI.GetWheelSelectorState(ID_ValueDisplayMode).index = (int)devPin.pinValueDisplayMode;
		}

		public static void DrawMenu()
		{
			Seb.Vis.UI.UI.DrawFullscreenPanel(DrawSettings.ActiveUITheme.MenuBackgroundOverlayCol);
			float spacing = 0.8f;

			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			InputFieldTheme inputTheme = DrawSettings.ActiveUITheme.ChipNameInputField;
			Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				Vector2 unpaddedSize = Draw.CalculateTextBoundsSize(MaxLengthPinName, inputTheme.fontSize, inputTheme.font);
				const float padX = 2.25f;
				Vector2 inputFieldSize = unpaddedSize + new Vector2(padX, 2.25f);
				Vector2 pos = Seb.Vis.UI.UI.Centre + Vector2.up * 5;

				// Draw input field
				InputFieldState inputFieldState = Seb.Vis.UI.UI.InputField(ID_NameField, inputTheme, pos, inputFieldSize, devPin.Pin.Name, Anchor.Centre, padX / 2, ValidatePinNameInput, true);
				Bounds2D inputFieldBounds = Seb.Vis.UI.UI.PrevBounds;
				string newName = inputFieldState.text;

				// Draw value display options
				if (devPin.BitCount != PinBitCount.Bit1)
				{
					const float wheelWidth = 20.2f;

					Vector2 topLeftCurr = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * spacing;
					MenuHelper.LabeledOptionsWheel("Decimal Display", Color.white, topLeftCurr, new Vector2(inputFieldBounds.Width, DrawSettings.SelectorWheelHeight), ID_ValueDisplayMode, PinDecimalDisplayOptions, wheelWidth, true);
				}

				// Draw cancel/confirm buttons
				Vector2 buttonsTopLeft = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * spacing;
				int buttonIndex = Seb.Vis.UI.UI.HorizontalButtonGroup(CancelConfirmButtonNames, ButtonGroupInteractStates, theme.ButtonTheme, buttonsTopLeft, inputFieldBounds.Width, DrawSettings.DefaultButtonSpacing, 0, Anchor.TopLeft);

				MenuHelper.DrawReservedMenuPanel(panelID, Seb.Vis.UI.UI.GetCurrentBoundsScope());

				// Keyboard shortcuts and UI input
				if (KeyboardShortcuts.CancelShortcutTriggered || buttonIndex == 0) Cancel();
				else if (KeyboardShortcuts.ConfirmShortcutTriggered || buttonIndex == 1) Confirm(newName);
			}
		}

	static void Confirm(string newName)
	{
		devPin.Pin.Name = newName;

		if (devPin.BitCount != PinBitCount.Bit1)
		{
			devPin.pinValueDisplayMode = (PinValueDisplayMode)Seb.Vis.UI.UI.GetWheelSelectorState(ID_ValueDisplayMode).index;
		}

		// If in level mode, sync pin name back to LevelDefinition
		if (LevelManager.Instance != null && LevelManager.Instance.IsActive && devPin.anchoredToLevel)
		{
			SyncPinNameToLevelDefinition(devPin, newName);
		}

		UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
	}

	static void Cancel()
	{
		UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
	}

	public static void SetTargetPin(DevPinInstance devPin)
	{
		PinEditMenu.devPin = devPin;
	}

	static bool ValidatePinNameInput(string name) => name.Length <= MaxLengthPinName.Length;

	/// <summary>
	/// Syncs a pin's name change back to the LevelDefinition when in level mode.
	/// </summary>
	static void SyncPinNameToLevelDefinition(DevPinInstance devPin, string newName)
	{
		var levelManager = LevelManager.Instance;
		if (levelManager?.Current == null) return;

		var currentChip = Project.ActiveProject?.ViewedChip;
		if (currentChip == null) return;

		// Find the pin index by comparing with all level pins
		int pinIndex = -1;
		bool isInput = devPin.IsInputPin;

		// Collect all level pins (inputs first, then outputs)
		var levelPins = new System.Collections.Generic.List<DevPinInstance>();
		foreach (var element in currentChip.Elements)
		{
			if (element is DevPinInstance pin && pin.anchoredToLevel)
			{
				levelPins.Add(pin);
			}
		}

		// Sort: inputs first (by position Y descending), then outputs (by position Y descending)
		levelPins.Sort((a, b) =>
		{
			if (a.IsInputPin != b.IsInputPin)
				return a.IsInputPin ? -1 : 1;
			return b.Position.y.CompareTo(a.Position.y); // Descending Y order
		});

		// Find the index of our pin
		for (int i = 0; i < levelPins.Count; i++)
		{
			if (levelPins[i].ID == devPin.ID)
			{
				pinIndex = i;
				break;
			}
		}

		if (pinIndex == -1)
		{
			Debug.LogWarning($"[PinEditMenu] Could not find pin index for pin {devPin.ID}");
			return;
		}

		// Update the appropriate label list
		if (isInput)
		{
			// Count input pins before this one
			int inputIndex = 0;
			for (int i = 0; i < pinIndex; i++)
			{
				if (levelPins[i].IsInputPin) inputIndex++;
			}

			// Ensure inputLabels list exists and is large enough
			if (levelManager.Current.inputLabels == null)
			{
				levelManager.Current.inputLabels = new System.Collections.Generic.List<string>();
			}
			while (levelManager.Current.inputLabels.Count <= inputIndex)
			{
				levelManager.Current.inputLabels.Add("");
			}

			levelManager.Current.inputLabels[inputIndex] = newName;
			Debug.Log($"[PinEditMenu] Updated input label {inputIndex} to '{newName}'");
		}
		else
		{
			// Count output pins before this one (after all inputs)
			int outputIndex = 0;
			for (int i = 0; i < pinIndex; i++)
			{
				if (!levelPins[i].IsInputPin) outputIndex++;
			}

			// Ensure outputLabels list exists and is large enough
			if (levelManager.Current.outputLabels == null)
			{
				levelManager.Current.outputLabels = new System.Collections.Generic.List<string>();
			}
			while (levelManager.Current.outputLabels.Count <= outputIndex)
			{
				levelManager.Current.outputLabels.Add("");
			}

			levelManager.Current.outputLabels[outputIndex] = newName;
			Debug.Log($"[PinEditMenu] Updated output label {outputIndex} to '{newName}'");
		}
	}
}
}
