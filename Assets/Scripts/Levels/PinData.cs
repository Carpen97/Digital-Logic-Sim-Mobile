using System;

namespace DLS.Levels
{
	/// <summary>
	/// Pin structure for V2 level format.
	/// Defines a single input or output pin with optional multi-bit support and positioning.
	/// </summary>
	[Serializable]
	public sealed class PinData
	{
		public string name;          // Display name (e.g., "Data A")
		public string abbr;          // Abbreviation for reports (optional, defaults to name)
		public int nBits = 1;        // Number of bits (defaults to 1)
		public float[] pos;          // Optional position override [x, y]

		/// <summary>
		/// Get the abbreviation, falling back to name if not specified.
		/// </summary>
		public string GetAbbr() => string.IsNullOrEmpty(abbr) ? name : abbr;
	}
}

