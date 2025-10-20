using System;
using UnityEngine;

namespace DLS.Description
{
	public class ChipDescription
	{
		// ---- Name Comparison ----
		public const StringComparison NameComparison = StringComparison.OrdinalIgnoreCase;
		public static readonly StringComparer NameComparer = StringComparer.OrdinalIgnoreCase;

		// ---- Data ----
		public string DLSVersion;
		public string Name;
		public NameDisplayLocation NameLocation;
		public ChipType ChipType;
		public ChipTypeId InternalTypeId;
		public bool CanBeCached;
		public bool ShouldBeCached;
		public Vector2 Size;
		public Color Colour;
		public PinDescription[] InputPins;
		public PinDescription[] OutputPins;
		public SubChipDescription[] SubChips;
		public WireDescription[] Wires;
		public DisplayDescription[] Displays;
		public bool HasCustomLayout = false;
		public ChipShapeType ShapeType = ChipShapeType.Rectangle;
		public float ShapeRotation = 0f; // Rotation in degrees
		public CustomPolygonData CustomPolygon = null; // Only used when ShapeType == CustomPolygon

		// ---- Convenience Functions ----
		public bool HasDisplay() => Displays != null && Displays.Length > 0;
		public bool NameMatch(string otherName) => NameMatch(Name, otherName);
		public static bool NameMatch(string a, string b) => string.Equals(a, b, NameComparison);
	}

	public enum NameDisplayLocation
	{
		Centre,
		Top,
		Hidden
	}

	public enum ChipShapeType
	{
		Rectangle,
		Hexagon,
		Triangle,
		CustomPolygon
	}

	/// <summary>
	/// Defines a custom polygon shape with vertices and optional curved edges
	/// </summary>
	[Serializable]
	public class CustomPolygonData
	{
		public PolygonVertex[] Vertices;
		public PolygonEdge[] Edges;

		public CustomPolygonData()
		{
			// Default: square (4 vertices)
			Vertices = new PolygonVertex[]
			{
				new PolygonVertex(0f, 1f),      // Top
				new PolygonVertex(1f, 0f),      // Right
				new PolygonVertex(0f, -1f),     // Bottom
				new PolygonVertex(-1f, 0f)      // Left
			};
			Edges = new PolygonEdge[4];
			for (int i = 0; i < 4; i++)
			{
				Edges[i] = new PolygonEdge();
			}
		}
	}

	/// <summary>
	/// Represents a vertex in a custom polygon (normalized coordinates -1 to 1)
	/// </summary>
	[Serializable]
	public class PolygonVertex
	{
		public float X;
		public float Y;

		public PolygonVertex() { }

		public PolygonVertex(float x, float y)
		{
			X = x;
			Y = y;
		}

		public Vector2 ToVector2() => new Vector2(X, Y);
	}

	/// <summary>
	/// Represents an edge between two vertices, can be straight or curved
	/// </summary>
	[Serializable]
	public class PolygonEdge
	{
		public bool IsCurved = false;
		public float CurveControlX = 0f;  // Control point X (relative to edge midpoint)
		public float CurveControlY = 0f;  // Control point Y (relative to edge midpoint)
		public float CurveStrength = 0f;  // How much the curve deviates from straight line

		public PolygonEdge() { }
	}
}