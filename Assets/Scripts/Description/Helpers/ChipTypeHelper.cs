using System;
using System.Collections.Generic;

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
			// ---- Memory ----
			{ ChipType.dev_Ram_8Bit, "RAM-8" },
			{ ChipType.Rom_256x16, $"ROM 256{mulSymbol}16" },
            { ChipType.EEPROM_256x16, $"EEPROM 256{mulSymbol}16" },

			// ---- Displays -----
			{ ChipType.DisplayRGB, "RGB DISPLAY" },
			{ ChipType.DisplayDot, "DOT DISPLAY" },
			{ ChipType.SevenSegmentDisplay, "7-SEGMENT" },
			{ ChipType.DisplayLED, "LED" },

			{ ChipType.Buzzer, "BUZZER" },

			// ---- Not really chips (but convenient to treat them as such anyway) ----

			// ---- Inputs/Outputs ----
			{ ChipType.Key, "KEY" },
            { ChipType.Button, "BUTTON" },
			{ ChipType.Toggle, "DIPSWITCH" },

			// ---- Buses ----
			{ ChipType.Bus_1Bit, "BUS-1" },
			{ ChipType.Bus_4Bit, "BUS-4" },
			{ ChipType.Bus_8Bit, "BUS-8" },
			{ ChipType.BusTerminus_1Bit, "BUS-TERMINUS-1" },
			{ ChipType.BusTerminus_4Bit, "BUS-TERMINUS-4" },
			{ ChipType.BusTerminus_8Bit, "BUS-TERMINUS-8" }
		};


		public static string GetName(ChipType type) => Names[type];

		public static bool IsBusType(ChipType type) => IsBusOriginType(type) || IsBusTerminusType(type);

		public static bool IsBusOriginType(ChipType type) => type is ChipType.Bus_1Bit or ChipType.Bus_4Bit or ChipType.Bus_8Bit;

		public static bool IsBusTerminusType(ChipType type) => type is ChipType.BusTerminus_1Bit or ChipType.BusTerminus_4Bit or ChipType.BusTerminus_8Bit;

		public static bool IsRomType(ChipType type) => type == ChipType.Rom_256x16 || type == ChipType.EEPROM_256x16;

		public static ChipType GetCorrespondingBusTerminusType(ChipType type)
		{
			return type switch
			{
				ChipType.Bus_1Bit => ChipType.BusTerminus_1Bit,
				ChipType.Bus_4Bit => ChipType.BusTerminus_4Bit,
				ChipType.Bus_8Bit => ChipType.BusTerminus_8Bit,
				_ => throw new Exception("No corresponding bus terminus found for type: " + type)
			};
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

		public static bool IsDevPin(ChipType chipType)
		{
			return chipType == ChipType.In_Pin || chipType == ChipType.Out_Pin;
		}
		public static bool IsClickableDisplayType(ChipType type) {
			// Return true for any chiptype that is a clickable display 

			return type == ChipType.Button || type == ChipType.Toggle;
		}

		public static bool IsInternalDataModifiable(ChipType type) {
			return type == ChipType.EEPROM_256x16 || type == ChipType.Toggle;
		}
	}
} 