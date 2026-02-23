using Newtonsoft.Json;
using UnityEngine;

namespace DLS.Description
{
	public struct SubChipDescription
	{
		public string Name;
		public int ID; // Unique within parent chip. ID > 0
		public string Label;
		/// <summary>Label position offset: x,y in [-1,1]. 0=centre, positive=right/down, negative=left/up. Default (0,1) = centred below.</summary>
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
		public Vector2 LabelOffset;
		public Vector2 Position;
		public OutputPinColourInfo[] OutputPinColourInfo;

		// Arbitrary data for specific chip types:
		// ROM: stores memory contents
		// BUS: stores id of linked bus pair (origin/terminus), and horizontal flip value (0 = no, 1 = yes)
		// KEY: stores bound key code
		// Otherwise is null
		public uint[] InternalData;

		public static readonly Vector2 DefaultLabelOffset = new(0, 1);

		public SubChipDescription(string name, int id, string label, Vector2 position, OutputPinColourInfo[] outputPinColInfo, uint[] internalData = null, Vector2? labelOffset = null)
		{
			Name = name;
			ID = id;
			Label = label;
			LabelOffset = labelOffset ?? DefaultLabelOffset;
			Position = position;
			OutputPinColourInfo = outputPinColInfo;
			InternalData = internalData;
		}
	}

	public struct OutputPinColourInfo
	{
		public PinColour PinColour;
		public int PinID;

		public OutputPinColourInfo(PinColour pinColour, int pinID)
		{
			PinColour = pinColour;
			PinID = pinID;
		}
	}
}