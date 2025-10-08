using System.Linq;
using DLS.Description;

namespace DLS.Game
{
	public static class BuiltinCollectionCreator
	{
		public static StarredItem[] GetDefaultStarredList()
		{
			return new StarredItem[]
			{
				new("IN/OUT", true),
				new(ChipTypeHelper.GetName(ChipType.Nand), false)
			};
		}

		public static ChipCollection[] CreateDefaultChipCollections()
		{
			return new[]
			{
				CreateChipCollection("BASIC",
					ChipType.Nand,
					ChipType.Clock,
					ChipType.Pulse,
					ChipType.Key,
					ChipType.TriStateBuffer,
					ChipType.Constant_8Bit
				),
				CreateInOutCollection(),
				CreateByNames("MERGE/SPLIT",
					"1-4BIT",
					"1-8BIT",
					"4-8BIT",
					"4-1BIT",
					"8-4BIT",
					"8-1BIT"
				),
				CreateByNames("BUS",
					"BUS-1",
					"BUS-4",
					"BUS-8"
				),
				CreateChipCollection("DISPLAY",
					ChipType.SevenSegmentDisplay,
					ChipType.DisplayDot,
					ChipType.DisplayRGB,
					ChipType.DisplayRGBTouch,
					ChipType.DisplayLED
				),
				CreateChipCollection("MEMORY",
					ChipType.Rom_256x16,
					ChipType.EEPROM_256x16,
					ChipType.dev_Ram_8Bit
				)
			};
		}

		static ChipCollection CreateInOutCollection()
		{
			var inOutCollection = CreateChipCollection("IN/OUT",
				ChipType.Button,
				ChipType.Toggle
			);
			
			// Create nested collections for each bit width
			var oneBit = inOutCollection.CreateNestedCollection("1-bit");
			oneBit.Chips.Add("IN-1");
			oneBit.Chips.Add("OUT-1");
			
			var fourBit = inOutCollection.CreateNestedCollection("4-bit");
			fourBit.Chips.Add("IN-4");
			fourBit.Chips.Add("OUT-4");
			
			var eightBit = inOutCollection.CreateNestedCollection("8-bit");
			eightBit.Chips.Add("IN-8");
			eightBit.Chips.Add("OUT-8");
			
			return inOutCollection;
		}

		static ChipCollection CreateChipCollection(string name, params ChipType[] chipTypes)
		{
			return new ChipCollection(name, chipTypes.Select(t => ChipTypeHelper.GetName(t)).ToArray());
		}

		static ChipCollection CreateByNames(string name, params string[] chipNames)
		{
			return new ChipCollection(name, chipNames);
		}

		static ChipCollection AddNames(this ChipCollection chipCollection, params string[] chipNames)
		{
			chipCollection.Chips.AddRange(chipNames);
			return chipCollection;
		}
	}
}