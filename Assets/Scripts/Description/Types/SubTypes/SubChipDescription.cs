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
		/// <summary>Instance rotation in degrees: 0, 90, 180, or 270. Default 0 for existing saves.</summary>
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
		public int Rotation;
		public OutputPinColourInfo[] OutputPinColourInfo;

		// Arbitrary data for specific chip types:
		// ROM: stores memory contents
		// BUS: stores id of linked bus pair (origin/terminus), and horizontal flip value (0 = no, 1 = yes)
		// KEY: stores bound key code
		// Otherwise is null
		public uint[] InternalData;

		public static readonly Vector2 DefaultLabelOffset = new(0, 1);

		public SubChipDescription(string name, int id, string label, Vector2 position, OutputPinColourInfo[] outputPinColInfo, uint[] internalData = null, Vector2? labelOffset = null, int rotation = 0)
		{
			Name = name;
			ID = id;
			Label = label;
			LabelOffset = labelOffset ?? DefaultLabelOffset;
			Position = position;
			Rotation = rotation;
			OutputPinColourInfo = outputPinColInfo;
			InternalData = internalData;
		}
	}

	public struct OutputPinColourInfo
	{
		public PinColour PinColour;
		public int PinID;
		/// <summary>Custom RGB packed as (255&lt;&lt;24)|(r&lt;&lt;16)|(g&lt;&lt;8)|b. 0 = use PinColour preset.</summary>
		public uint CustomColourPacked;

		public OutputPinColourInfo(PinColour pinColour, int pinID, uint customColourPacked = 0)
		{
			PinColour = pinColour;
			PinID = pinID;
			CustomColourPacked = customColourPacked;
		}
	}
}