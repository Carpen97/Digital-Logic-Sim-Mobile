namespace DLS.Description
{
	public enum ChipType
	{
		Custom,

		// ---- Basic Chips ----
		Nand,
		TriStateBuffer,
		Clock,
		Pulse,

		// ---- Memory ----
		dev_Ram_8Bit,
		Rom_256x16,
		EEPROM_256x16,

		// ---- Displays ----
		SevenSegmentDisplay,
		DisplayRGB,
		DisplayDot,
		DisplayLED,

		// ---- Merge / Split ----
		Merge_Pin,
		Split_Pin,

		// ---- In / Out Pins ----
		In_Pin,
		Out_Pin,

        Key,

		Button,
		Toggle,

		Constant_8Bit,

        // ---- Buses ----
        Bus_1Bit,
		BusTerminus_1Bit,
		Bus_4Bit,
		BusTerminus_4Bit,
		Bus_8Bit,
		BusTerminus_8Bit,
		
		// ---- Audio ----
		Buzzer

	}
}