using UnityEngine;

namespace DLS.Description
{
	/// <summary>
	/// Serialized layout of a saved group: multiple chips with positions and wires between them.
	/// Stored in the library alongside chips; placed as a unit.
	/// </summary>
	public class GroupDescription
	{
		public string Name;
		public string DLSVersion;
		public SubChipDescription[] SubChips;
		public PinDescription[] InputPins;
		public PinDescription[] OutputPins;
		public WireDescription[] Wires;

		public bool NameMatch(string otherName) => ChipDescription.NameMatch(Name, otherName);
	}
}
