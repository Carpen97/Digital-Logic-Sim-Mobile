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
		Detector,

		// ---- Memory ----
		dev_Ram_8Bit,
		Rom_256x16,      // Default 2x8-bit ROM (kept for backwards compatibility)
		Rom_2x8,         // 2x8-bit ROM (256 x 16-bit, output as 2 x 8-bit pins)
		Rom_4x4,         // 4x4-bit ROM (256 x 16-bit, output as 4 x 4-bit pins)
		Rom_16x1,        // 16x1-bit ROM (256 x 16-bit, output as 16 x 1-bit pins)
		Rom_1x16,        // 1x16-bit ROM (256 x 16-bit, output as 1 x 16-bit pin)
		EEPROM_256x16,

		// ---- Displays ----
		SevenSegmentDisplay,
		DisplayRGB,
		DisplayDot,
		DisplayLED,
		DisplayRGBTouch,

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
        Bus,
		BusTerminus,
		
		// ---- Audio ----
		Buzzer,

		// ---- Time ----
		RTC,

		// ---- Clock ----
		SPS,
	}
}