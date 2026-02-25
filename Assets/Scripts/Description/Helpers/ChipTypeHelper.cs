using System;
using System.Collections.Generic;
using UnityEngine.TextCore.Text;

namespace DLS.Description
{
	public static class ChipTypeHelper
	{
		const string mulSymbol = "\u00d7";

		static readonly Dictionary<ChipType, string> Names = new()
		{
			// ---- Basic Chips ----
			{ ChipType.Nand, "NAND" },
			{ ChipType.Clock, "CLOCK" },
			{ ChipType.Pulse, "PULSE" },
			{ ChipType.TriStateBuffer, "3-STATE BUFFER" },
			{ ChipType.Constant_8Bit, "CONST" },
			{ ChipType.Detector, "DETECTOR" },
		// ---- Memory ----
		{ ChipType.dev_Ram_8Bit, "RAM-8" },
		{ ChipType.Rom_256x16, $"ROM 256{mulSymbol}16" },
		{ ChipType.Rom_2x8, $"ROM 2{mulSymbol}8_Variant" },
		{ ChipType.Rom_4x4, $"ROM 4{mulSymbol}4_Variant" },
		{ ChipType.Rom_16x1, $"ROM 16{mulSymbol}1_Variant" },
		{ ChipType.Rom_1x16, $"ROM 1{mulSymbol}16_Variant" },
            { ChipType.EEPROM_256x16, $"EEPROM 256{mulSymbol}16" },

		// ---- Displays -----
		{ ChipType.DisplayRGB, "RGB DISPLAY" },
		{ ChipType.DisplayRGBTouch, "TOUCHSCREEN RGB DISPLAY" },
		{ ChipType.DisplayDot, "DOT DISPLAY" },
		{ ChipType.SevenSegmentDisplay, "7-SEGMENT" },
	{ ChipType.DisplayLED, "LED" },
	{ ChipType.DisplayRGBLED, "RGB LED" },
	{ ChipType.TextDisplay, "TEXT DISPLAY" },
	{ ChipType.Label, "LABEL" },

		{ ChipType.Buzzer, "BUZZER" },
		{ ChipType.Speaker, "SPEAKER" },
		{ ChipType.SpeakerV2, "SPEAKER V2" },

		{ ChipType.SPS, "SPS" },
			{ ChipType.RTC, "RTC" },

			// ---- Not really chips (but convenient to treat them as such anyway) ----

			// ---- Inputs/Outputs ----
			{ ChipType.Key, "KEY" },
            { ChipType.Button, "BUTTON" },
			{ ChipType.Toggle, "DIPSWITCH" },

		};


		public static string GetName(ChipType type) => Names[type];

		public static bool IsBusType(ChipType type) => IsBusOriginType(type) || IsBusTerminusType(type);

		public static bool IsBusOriginType(ChipType type) => type is ChipType.Bus;

		public static bool IsBusTerminusType(ChipType type) => type is ChipType.BusTerminus;

		public static bool IsRomType(ChipType type) => type == ChipType.Rom_256x16 || type == ChipType.Rom_2x8 || type == ChipType.Rom_4x4 || type == ChipType.Rom_16x1 || type == ChipType.Rom_1x16 || type == ChipType.EEPROM_256x16;

	/// <summary>
	/// Returns true for chip types that are not allowed in level solutions (anywhere in the hierarchy, including inside custom chips).
	/// Must match the set used by ChipInteractionController.IsSpecialChipDisabledInLevel and ShouldHideChipInLevel for consistency.
	/// </summary>
	public static bool IsDisabledInLevels(ChipType type)
	{
		// In/Out pins as subchips: custom chips may not add extra pins beyond level-provided ones
		if (type == ChipType.In_Pin || type == ChipType.Out_Pin) return true;
		// All ROM variants (including those not in IsSpecialChipDisabledInLevel)
		if (IsRomType(type)) return true;
		// Special chips disabled in level mode (same as ChipInteractionController.IsSpecialChipDisabledInLevel)
		return type == ChipType.dev_Ram_8Bit ||
		       type == ChipType.SevenSegmentDisplay ||
		       type == ChipType.DisplayRGB ||
		       type == ChipType.DisplayRGBTouch ||
		       type == ChipType.DisplayDot ||
		       type == ChipType.DisplayLED ||
		       type == ChipType.DisplayRGBLED ||
		       type == ChipType.Pulse ||
		       type == ChipType.Clock ||
		       type == ChipType.Key ||
		       type == ChipType.Button ||
		       type == ChipType.Toggle ||
		       type == ChipType.Detector ||
		       type == ChipType.Buzzer ||
		       type == ChipType.RTC ||
		       type == ChipType.SPS ||
		       type == ChipType.Constant_8Bit;
	}

	public static bool IsTextDisplayType(ChipType type) => type == ChipType.TextDisplay;

		/// <summary>
		/// Gets the display name for a chip type, with special handling for ROM variants
		/// </summary>
		public static string GetDisplayName(ChipType type)
		{
			// All ROM variants display as "ROM 256×16" to the user
			if (IsRomType(type))
			{
				return $"ROM 256{mulSymbol}16";
			}
			
			// For all other chips, use the regular name
			return GetName(type);
		}

		public static (bool isInput, bool isOutput, PinBitCount numBits) IsInputOrOutputPin(ChipDescription chip)
		{
			return chip.ChipType switch
			{
				ChipType.In_Pin => (true, false, chip.OutputPins[0].BitCount),
                ChipType.Out_Pin => (false, true, chip.InputPins[0].BitCount),
                _ => (false, false, new PinBitCount { BitCount = 1 })
			};
		}

		public static string GetDevPinName(bool isInput, PinBitCount numBits)
		{
			return (isInput ? "IN-" : "OUT-") + numBits.BitCount.ToString();
		}

		public static string GetBusName(PinBitCount numBits)
		{
			return "BUS-" + numBits.ToString();
		}

        public static string GetBusTerminusName(PinBitCount numBits)
        {
            return "BUS-TERMINUS-" + numBits.ToString();
        }


        public static bool IsDevPin(ChipType chipType)
		{
			return chipType == ChipType.In_Pin || chipType == ChipType.Out_Pin;
		}
		public static bool IsClickableDisplayType(ChipType type) {
			// Return true for any chiptype that is a clickable display 

			return type == ChipType.Button || type == ChipType.Toggle || type == ChipType.DisplayRGBTouch;
		}

		public static bool IsInternalDataModifiable(ChipType type) {
			return type == ChipType.EEPROM_256x16 || type == ChipType.Toggle;
		}

		public static bool IsMergeSplitChip(ChipType chipType)
		{
			return chipType == ChipType.Split_Pin || chipType == ChipType.Merge_Pin;
		}
	}
} 