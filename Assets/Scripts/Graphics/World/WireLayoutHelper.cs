using System;
using System.Collections.Generic;
using System.Numerics;
using DLS.Game;
using NUnit.Framework.Constraints;
using Seb.Helpers;
using UnityEngine;
using UnityEngine.EventSystems;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace DLS.Graphics
{
	public static class WireLayoutHelper
	{
		public static void CreateMultiBitWireLayout(WireInstance.BitWire[] bitWires, WireInstance wire, float thickness)
		{
			// At 1, wires are spaced apart by their thickness. This can cause slight slivers to appear though due to antialiasing, so it helps to smoosh them together slightly 
			// Increased overlap to eliminate gaps between individual bit wires
			const float thicknessOffsetT = 0.85f;

			// Ensure initialized
			foreach (WireInstance.BitWire bitWire in bitWires)
			{
				if (bitWire.Points == null || bitWire.Points.Length != wire.WirePointCount)
				{
					bitWire.Points = new Vector2[wire.WirePointCount];
				}
			}

			Vector2 dirPrev = Vector2.zero;
			int numBits = bitWires.Length;
			int wiresToDraw = (int)(numBits * (SubChipInstance.GetPinDepthMultiplier(wire.bitCount)));
			float offsetSign = 1;

			// Create layout
			for (int i = 0; i < wire.WirePointCount - 1; i++)
			{
				Vector2 wireCentreA = wire.GetWirePoint(i);
				Vector2 wireCentreB = wire.GetWirePoint(i + 1);
				Vector2 wireDir = (wireCentreB - wireCentreA).normalized;
				Vector2 wirePerpDir = new(-wireDir.y, wireDir.x);

				// If wire bends back past a certain threshold, swap the offset direction
				// This gives appearance of wires flipping over, rather than bending at an uncomfortable angle, which I think looks better...
				if (i > 0) offsetSign *= Flip(wireDir, dirPrev);

				for (int bitIndex = 0; bitIndex < wiresToDraw; bitIndex++)
				{
					WireInstance.BitWire bitWire = bitWires[bitIndex];
					float bitOffsetDst = (bitIndex - (wiresToDraw - 1) / 2f) * thickness * 2 * thicknessOffsetT;

					Vector2 bitWireOffset = wirePerpDir * bitOffsetDst;
					Vector2 posA = i == 0 ? wireCentreA + bitWireOffset : bitWire.Points[i];
					Vector2 posB = wireCentreB + bitWireOffset * offsetSign;

					// If there is another point after this, position the wires to align with that direction
					if (i + 1 < wire.WirePointCount - 1)
					{
						Vector2 centreNext = wire.GetWirePoint(i + 2);
						if ((centreNext - wireCentreB).sqrMagnitude > 0.001f)
						{
							Vector2 dirNext = (centreNext - wireCentreB).normalized;
							Vector2 wireDirNext = new(-dirNext.y, dirNext.x);
							Vector2 bitWireOffsetNext = wireDirNext * bitOffsetDst;
							Vector2 posNext = centreNext + bitWireOffsetNext * (offsetSign * Flip(wireDir, dirNext));

							(bool intersects, Vector2 point) intersectResult = Maths.LineIntersectsLine(posA, posB, posNext, posNext + dirNext);

							if (Mathf.Abs(Vector2.Dot(wireDir, dirNext)) < 0.995f && intersectResult.intersects)
							{
								posB += intersectResult.point - posB;
							}
						}
					}

					bitWire.Points[i] = posA;
					bitWire.Points[i + 1] = posB;
				}

				dirPrev = wireDir;
			}
		}
		
		public static Vector2 ProjectPointOnLine(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
		{
    		Vector2 lineDirection = lineEnd - lineStart;
    		Vector2 pointToLineStart = point - lineStart;
    
    		// Calculate the projection parameter t
    		float t = Vector2.Dot(pointToLineStart, lineDirection) / lineDirection.sqrMagnitude;
    
    		// Clamp t to [0, 1] to keep the projection on the line segment
    		t = Mathf.Clamp01(t);
    
    		// Return the projected point
    		return lineStart + t * lineDirection;
		}

		/// <summary>
		/// New spherical interpolation method for creating smooth multi-bit wire curves
		/// </summary>
		public static void CreateMultiBitWireLayoutSlerp(WireInstance.BitWire[] bitWires, WireInstance wire, float thickness)
		{
			// At 1, wires are spaced apart by their thickness. This can cause slight slivers to appear though due to antialiasing, so it helps to smoosh them together slightly 
			const float thicknessOffsetT = 0.85f;

			// Ensure initialized
			List<Vector2>[] pointCollection = new List<Vector2>[bitWires.Length];
			for (int i = 0; i < bitWires.Length; i++)
			{
				List<Vector2> points = new();
				pointCollection[i] = points;
			}


			int numBits = bitWires.Length;
			int wiresToDraw = (int)(numBits * (SubChipInstance.GetPinDepthMultiplier(wire.bitCount)));


			Vector2 P1 = wire.GetWirePoint(wire.WirePointCount-2);
			Vector2 P2 = wire.GetWirePoint(wire.WirePointCount-1);

			P1 = wire.GetWirePoint(0);
			P2 = wire.GetWirePoint(1);
			Vector2 wireDir = (P2 - P1).normalized;
			Vector2 wirePerpDir = new(-wireDir.y, wireDir.x);

			Vector2 P1_above = P1 + wirePerpDir * thickness * bitWires.Length * thicknessOffsetT;
			Vector2 P1_below = P1 - wirePerpDir * thickness * bitWires.Length * thicknessOffsetT;
			for (int bitIndex = 0; bitIndex < wiresToDraw; bitIndex++)
			{
				float tt = (float) bitIndex / (wiresToDraw - 1);
				Vector2 point = Vector2.Lerp(P1_below, P1_above, tt);
				pointCollection[bitIndex].Add(point);
			}

			// Create layout using spherical interpolation
			bool flip = false;
			for (int i = 0; i < wire.WirePointCount - 2; i++)
			{

				P1 = wire.GetWirePoint(i);
				P2 = wire.GetWirePoint(i + 1);
				Vector2 P3 = wire.GetWirePoint(i + 2);
				if ((P1 - P2).magnitude < 0.0001) break;


				Vector2 P2_P1 = (P1 - P2).normalized;
				Vector2 P2_P3 = (P3 - P2).normalized;

				Vector2 t1 = new(-P2_P1.y,P2_P1.x);

				bool isLeftTurn = Vector2.Dot(t1, P2_P3) < 0;

				if (Vector2.Dot(-P2_P1, P2_P3) == 1) continue; //Skip extra waypoints on a straight line

				Vector2 P2_PX = SlerpDirection(P2_P1, P2_P3, 0.5f);
				Vector2 PX = P2 + P2_PX * thickness * bitWires.Length * 1.4f * thicknessOffsetT;

				//Calculate Pa
				Vector2 Pe1 = ProjectPointOnLine(PX, P1, P2);
				Vector2 PA = PX + (Pe1 - PX) * 2;

				//Calculate Pb
				Vector2 Pe2 = ProjectPointOnLine(PX, P2, P3);
				Vector2 PB = PX + (Pe2 - PX) * 2;

				// Calculate vectors A and B
				Vector2 vectorA = (PA - PX).normalized;
				Vector2 vectorB = (PB - PX).normalized;

				if (isLeftTurn)
					flip = !flip;

				int res = 25;
				for (int t = 0; t < res; t++)
				{
					Vector2 slerpDirection;
					if (vectorA == vectorB)
						slerpDirection = vectorA;
					else
						slerpDirection = SlerpDirection(vectorA, vectorB, (float)t / (res - 1));

					for (int bitIndex = 0; bitIndex < wiresToDraw; bitIndex++)
					{
						float bitOffsetDst = thickness * bitWires.Length *2 * thicknessOffsetT;
						Vector2 P_temp = PX + bitOffsetDst * slerpDirection;
						float tt = (float) bitIndex / (wiresToDraw - 1);
						if (isLeftTurn) tt = 1 - tt;
						Vector2 point = Vector2.Lerp(PX, P_temp, tt);
						pointCollection[bitIndex].Add(point);
					}

				}
			}

			P1 = wire.GetWirePoint(wire.WirePointCount-2);
			P2 = wire.GetWirePoint(wire.WirePointCount-1);
			if ((P1-P2).magnitude>0.0001)
			{
				wireDir = (P2 - P1).normalized;
				wirePerpDir = new(-wireDir.y, wireDir.x);
				P1_below = P2 - wirePerpDir * thickness * bitWires.Length * thicknessOffsetT;
				P1_above = P2 + wirePerpDir * thickness * bitWires.Length * thicknessOffsetT;
				for (int bitIndex = 0; bitIndex < wiresToDraw; bitIndex++)
				{
					float tt = (float) bitIndex / (wiresToDraw - 1);
					Vector2 point = Vector2.Lerp(P1_below, P1_above, tt);
					pointCollection[bitIndex].Add(point);
				}
			}

			for (int i = 0; i < bitWires.Length; i++)
			{
				bitWires[i].Points = pointCollection[i].ToArray();
			}
		}

		/// <summary>
		/// Spherical interpolation between two direction vectors
		/// </summary>
		static Vector2 SlerpDirection(Vector2 from, Vector2 to, float t)
		{
			// Convert to 3D for proper slerp calculation
			Vector3 from3D = new Vector3(from.x, from.y, 0);
			Vector3 to3D = new Vector3(to.x, to.y, 0);
			
			// Normalize the vectors
			from3D.Normalize();
			to3D.Normalize();
			
			// Calculate the dot product
			float dot = Vector3.Dot(from3D, to3D);
			
			// Clamp to avoid numerical errors
			dot = Mathf.Clamp(dot, -1.0f, 1.0f);
			
			// If the vectors are very close, use linear interpolation
			if (dot > 0.9995f)
			{
				Vector3 result = Vector3.Lerp(from3D, to3D, t);
				return new Vector2(result.x, result.y).normalized;
			}
			
			// Calculate the angle between the vectors
			float theta = Mathf.Acos(dot) * t;
			Vector3 relativeVec = (to3D - from3D * dot).normalized;
			
			// Calculate the slerp result
			Vector3 result3D = from3D * Mathf.Cos(theta) + relativeVec * Mathf.Sin(theta);
			
			return new Vector2(result3D.x, result3D.y).normalized;
		}

		public static (Vector2 point, int segmentIndex) GetClosestPointOnWire(WireInstance wire, Vector2 desiredPos)
		{
			int bestSegmentIndex = 0;
			float bestSqrDst = float.MaxValue;
			Vector2 bestPoint = Vector2.zero;

			for (int i = 0; i < wire.WirePointCount - 1; i++)
			{
				Vector2 segStartPoint = wire.GetWirePoint(i);
				Vector2 segEndPoint = wire.GetWirePoint(i + 1);
				Vector2 pointOnSegment = Maths.ClosestPointOnLineSegment(desiredPos, segStartPoint, segEndPoint);

				float sqrDst = (pointOnSegment - desiredPos).sqrMagnitude;
				if (sqrDst < bestSqrDst)
				{
					bestPoint = pointOnSegment;
					bestSqrDst = sqrDst;
					bestSegmentIndex = i;
				}
			}

			return (bestPoint, bestSegmentIndex);
		}


		static float Flip(Vector2 dirA, Vector2 dirB)
		{
			// How far back to allow wire to be angled before switching sign
			// Without this, the miter would grow to infinity as wire angle approaches 180 degrees.
			// Instead, at a certain threshold we can switch from miter to 'flipping' the wire over.
			// TODO: maybe would be better to do something like insert additional point to allow for a 'cut corner' effect instead of miter, and then we could avoid this flip stuff
			const float threshold = -0.75f;
			return Vector2.Dot(dirA, dirB) < threshold ? -1 : 1;
		}
	}
}