using System;
using System.Reflection;
using DLS.Description;
using DLS.Game;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	public static class FrequencyEditMenu
	{
		const int MaxFrequency = 255;
		static SubChipInstance targetChip;
		static uint frequency;
		static int bitWidthIndex; // 0=1-bit, 1=4-bit, 2=8-bit
		static bool isTransmitter;

		// Saved on open for revert-on-cancel
		static uint initialFrequency;
		static uint initialPackedColour;
		static int initialBitWidthIndex;

		static readonly UIHandle ID_FrequencyInput = new("FrequencyEdit_Frequency");
		static readonly UIHandle ID_ColourPicker = new("FrequencyEdit_ColourPicker");
		static readonly UIHandle ID_BitWidthSelector = new("FrequencyEdit_BitWidth");

		static readonly string[] BitWidthOptions = { "1-bit", "4-bit", "8-bit" };

		/// <summary>Returns colour for Transmitter/Receiver. InternalData[1]: 0 = use desc.Colour, else packed RGB (alpha always 255).</summary>
		public static Color GetWirelessColourFromStored(SubChipInstance subchip, Color defaultCol)
		{
			uint packed = subchip.InternalData != null && subchip.InternalData.Length > 1 ? subchip.InternalData[1] : 0;
			if (packed == 0) return defaultCol;
			byte r = (byte)((packed >> 16) & 0xFF);
			byte g = (byte)((packed >> 8) & 0xFF);
			byte b = (byte)(packed & 0xFF);
			return new Color32(r, g, b, 255);
		}

		static uint PackColor(Color col)
		{
			Color32 c = col;
			uint packed = (uint)((255u << 24) | (c.r << 16) | (c.g << 8) | c.b);
			return packed == 0 ? 7u : packed;
		}

		static void EnsureWirelessInternalDataLength(SubChipInstance chip)
		{
			if (chip.ChipType != ChipType.Transmitter && chip.ChipType != ChipType.Receiver) return;
			if (chip.InternalData != null && chip.InternalData.Length >= 2) return;

			uint freq = chip.InternalData != null && chip.InternalData.Length > 0 ? chip.InternalData[0] : 0;
			var newData = new uint[] { freq, 0 };
			var field = typeof(SubChipInstance).GetField("InternalData", BindingFlags.Public | BindingFlags.Instance);
			field?.SetValue(chip, newData);
		}

		static int GetBitWidthIndexFromName(string name)
		{
			if (ChipDescription.NameMatch(name, "TRANSMITTER") || ChipDescription.NameMatch(name, "RECEIVER")) return 0;
			if (ChipDescription.NameMatch(name, "TRANSMITTER-4") || ChipDescription.NameMatch(name, "RECEIVER-4")) return 1;
			if (ChipDescription.NameMatch(name, "TRANSMITTER-8") || ChipDescription.NameMatch(name, "RECEIVER-8")) return 2;
			return 0;
		}

		static string GetVariantName(bool transmitter, int bitWidthIndex)
		{
			string baseName = transmitter ? ChipTypeHelper.GetName(ChipType.Transmitter) : ChipTypeHelper.GetName(ChipType.Receiver);
			return bitWidthIndex switch { 0 => baseName, 1 => baseName + "-4", 2 => baseName + "-8", _ => baseName };
		}

		public static void DrawMenu()
		{
			MenuHelper.DrawBackgroundOverlay();
			Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;

			const float leftColumnWidth = 18f;
			const float gap = 2f;
			const float pickerSize = 12f;
			float centreX = Seb.Vis.UI.UI.Centre.x;
			float leftEdge = centreX - (leftColumnWidth + gap + pickerSize) / 2f;
			float rightEdge = leftEdge + leftColumnWidth + gap;

			Vector2 topPos = Seb.Vis.UI.UI.Centre + Vector2.up * (Seb.Vis.UI.UI.HalfHeight * 0.25f);
			float totalWidth = leftColumnWidth + gap + pickerSize;

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				// Establish top bounds
				Seb.Vis.UI.UI.DrawQuad(topPos, new Vector2(totalWidth, 0.01f), new Color(0, 0, 0, 0), Anchor.CentreTop);

				float spacing = DrawSettings.VerticalButtonSpacing;

				// Left column: title, frequency, hint, bit width
				string title = isTransmitter ? "Edit Transmitter" : "Edit Receiver";
				Vector2 titlePos = new(leftEdge, topPos.y);
				Seb.Vis.UI.UI.DrawText(title, theme.FontBold, theme.FontSizeRegular, titlePos, Anchor.TopLeft, Color.white * 0.8f);

				InputFieldTheme inputFieldTheme = DrawSettings.ActiveUITheme.ChipNameInputField;
				inputFieldTheme.fontSize = DrawSettings.ActiveUITheme.FontSizeRegular;

				float inputRowY = titlePos.y - theme.FontSizeRegular - spacing;
				const float labelToInputGap = 1f;
				float labelWidth = 8f;
				Vector2 inputSize = new(5.6f, DrawSettings.SelectorWheelHeight);
				float inputRowCentreY = inputRowY - inputSize.y / 2f;
				Seb.Vis.UI.UI.DrawText("Frequency:", theme.FontBold, theme.FontSizeRegular, new Vector2(leftEdge, inputRowCentreY), Anchor.CentreLeft, Color.white * 0.9f);

				Vector2 inputPos = new(leftEdge + labelWidth + labelToInputGap, inputRowY);
				InputFieldState state = Seb.Vis.UI.UI.InputField(ID_FrequencyInput, inputFieldTheme, inputPos, inputSize, "0", Anchor.TopLeft, 1, ValidateFrequencyInput, forceFocus: true);
				uint.TryParse(state.text, out frequency);
				frequency = (uint)Mathf.Clamp((int)frequency, 0, MaxFrequency);

				float selectorWidth = 16f;
				Vector2 bitWidthPos = new(leftEdge, inputPos.y - DrawSettings.SelectorWheelHeight - spacing);
				Seb.Vis.UI.UI.DrawText("Bit width:", theme.FontBold, theme.FontSizeRegular, bitWidthPos, Anchor.TopLeft, Color.white * 0.9f);
				int bitWidthNew = Seb.Vis.UI.UI.WheelSelector(ID_BitWidthSelector, BitWidthOptions, bitWidthPos + Vector2.down * (theme.FontSizeRegular + 0.2f), new Vector2(selectorWidth, DrawSettings.SelectorWheelHeight), MenuHelper.Theme.OptionsWheel, Anchor.TopLeft);
				if (bitWidthNew != bitWidthIndex) bitWidthIndex = bitWidthNew;

				// Right column: colour picker (takes up the space)
				Vector2 pickerPos = new(rightEdge, topPos.y);
				Color newCol = Seb.Vis.UI.UI.DrawColourPicker(ID_ColourPicker, pickerPos, pickerSize, Anchor.TopLeft);

				// Live update: apply colour (always full opacity)
				if (targetChip.InternalData != null && targetChip.InternalData.Length > 1)
					targetChip.InternalData[1] = PackColor(newCol);

				MenuHelper.CancelConfirmResult result = MenuHelper.DrawCancelConfirmButtons(Seb.Vis.UI.UI.GetCurrentBoundsScope().BottomLeft, Seb.Vis.UI.UI.GetCurrentBoundsScope().Width, true);
				MenuHelper.DrawReservedMenuPanel(panelID, Seb.Vis.UI.UI.GetCurrentBoundsScope());

				if (result == MenuHelper.CancelConfirmResult.Cancel)
				{
					RevertToInitialState();
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				}
				else if (result == MenuHelper.CancelConfirmResult.Confirm)
				{
					if (isTransmitter && !IsFrequencyAvailableForTransmitter(frequency))
					{
						SimpleMessagePopup.Open("Frequency " + frequency + " is already in use by another transmitter.", () => UIDrawer.SetActiveMenu(UIDrawer.MenuType.FrequencyEdit));
						return;
					}
					ApplyChanges();
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				}
			}
		}

		static void RevertToInitialState()
		{
			if (targetChip == null) return;
			if (targetChip.InternalData != null && targetChip.InternalData.Length > 0)
				targetChip.InternalData[0] = initialFrequency;
			if (targetChip.InternalData != null && targetChip.InternalData.Length > 1)
				targetChip.InternalData[1] = initialPackedColour;
			if (bitWidthIndex != initialBitWidthIndex)
			{
				// Revert bit width selector UI (chip not actually reverted since we didn't replace)
				bitWidthIndex = initialBitWidthIndex;
				Seb.Vis.UI.UI.GetWheelSelectorState(ID_BitWidthSelector).index = bitWidthIndex;
			}
		}

		static void ApplyChanges()
		{
			uint packedCol = targetChip.InternalData != null && targetChip.InternalData.Length > 1 ? targetChip.InternalData[1] : 0;

			if (bitWidthIndex != initialBitWidthIndex)
			{
				ReplaceWirelessChip(GetVariantName(isTransmitter, bitWidthIndex));
			}

			targetChip.InternalData[0] = frequency;
			if (targetChip.InternalData.Length > 1)
				targetChip.InternalData[1] = packedCol;

			Project.ActiveProject.NotifyFrequencyEdited(targetChip, frequency);
		}

		static void ReplaceWirelessChip(string newChipName)
		{
			var project = Project.ActiveProject;
			var devChip = project.ViewedChip;

			uint[] internalData = new uint[] { frequency, targetChip.InternalData != null && targetChip.InternalData.Length > 1 ? targetChip.InternalData[1] : 0 };
			Vector2 position = targetChip.Position;
			string label = targetChip.Label;
			int chipID = targetChip.ID;

			DeleteOutputWireConnections(devChip, targetChip);
			devChip.DeleteSubChip(targetChip);

			if (!project.chipLibrary.TryGetChipDescription(newChipName, out var newChipDescription))
			{
				UnityEngine.Debug.LogError($"Could not find chip description for {newChipName}");
				return;
			}

			var newSubChipDesc = new SubChipDescription(
				newChipName,
				chipID,
				label,
				position,
				targetChip.InitialSubChipDesc.OutputPinColourInfo,
				internalData,
				targetChip.LabelOffset,
				targetChip.Rotation
			);

			var newChip = new SubChipInstance(newChipDescription, newSubChipDesc);
			devChip.AddNewSubChip(newChip, false);

			targetChip = newChip;
		}

		static void DeleteOutputWireConnections(DevChipInstance devChip, SubChipInstance chip)
		{
			var wiresToDelete = new System.Collections.Generic.List<WireInstance>();
			foreach (var wire in devChip.Wires)
			{
				bool isConnectedToOutput = (wire.SourcePin.parent == chip && wire.SourcePin.IsSourcePin) ||
				                          (wire.TargetPin.parent == chip && wire.TargetPin.IsSourcePin);
				if (isConnectedToOutput) wiresToDelete.Add(wire);
			}
			foreach (var wire in wiresToDelete) devChip.DeleteWire(wire);
		}

		public static void OnMenuOpened()
		{
			targetChip = (SubChipInstance)ContextMenu.interactionContext;
			isTransmitter = targetChip.ChipType == ChipType.Transmitter;
			EnsureWirelessInternalDataLength(targetChip);

			initialFrequency = targetChip.InternalData != null && targetChip.InternalData.Length > 0 ? targetChip.InternalData[0] : 0;
			initialPackedColour = targetChip.InternalData != null && targetChip.InternalData.Length > 1 ? targetChip.InternalData[1] : 0;
			initialBitWidthIndex = GetBitWidthIndexFromName(targetChip.Description.Name);
			bitWidthIndex = initialBitWidthIndex;

			frequency = initialFrequency;
			Seb.Vis.UI.UI.GetInputFieldState(ID_FrequencyInput).SetText(frequency.ToString());
			Seb.Vis.UI.UI.GetWheelSelectorState(ID_BitWidthSelector).index = bitWidthIndex;

			Color currentCol = GetWirelessColourFromStored(targetChip, targetChip.Description.Colour);
			Seb.Vis.UI.UI.GetColourPickerState(ID_ColourPicker).SetRGB(currentCol);
		}

		static bool ValidateFrequencyInput(string s)
		{
			if (s.Length > 4) return false;
			if (string.IsNullOrEmpty(s)) return true;
			return uint.TryParse(s, out uint v) && v <= MaxFrequency;
		}

		static bool IsFrequencyAvailableForTransmitter(uint freq)
		{
			var chip = Project.ActiveProject.ViewedChip;
			foreach (var sub in chip.GetSubchips())
			{
				if (sub == targetChip) continue;
				if (sub.ChipType == ChipType.Transmitter && sub.InternalData != null && sub.InternalData.Length > 0 && sub.InternalData[0] == freq)
					return false;
			}
			return true;
		}
	}
}
