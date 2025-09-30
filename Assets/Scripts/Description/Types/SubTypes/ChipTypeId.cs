namespace DLS.Description
{
	/// <summary>
	/// Internal chip type identifiers for auto-detected logical gate types.
	/// These are immutable and set automatically during chip analysis.
	/// </summary>
	public enum ChipTypeId
	{
		Unknown = 0,
		
		// Single input gates
		NOT = 1,
		Buffer = 2,
		
		// Two input gates
		AND = 10,
		OR = 11,
		XOR = 12,
		NAND = 13,
		NOR = 14,
		XNOR = 15,
		
		// Three input gates
		AND3 = 20,
		OR3 = 21,
		NAND3 = 22,
		NOR3 = 23,
		
		// Multi-output gates (future extension)
		// Demux2 = 30,
		// Decoder2 = 31,
	}
}
