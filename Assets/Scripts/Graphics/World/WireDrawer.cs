using DLS.Game;
using Seb.Helpers;
using Seb.Vis;
using UnityEngine;

namespace DLS.Graphics
{
	public static class WireDrawer
	{
        public static bool CurvedWires = true;


        public static float DrawWire(Vector2[] points, float thickness, Color col, Vector2 interactPos, bool isMulti = false){
			if(Project.ActiveProject.description.Prefs_WireCurvatureMode == 0 || (isMulti && Project.ActiveProject.description.Prefs_MultiWireLayoutAlgorithm == 1) ){
        		return DrawWireStraight(points, thickness, col, interactPos);
			}else{
        		return DrawWireCurved(points, thickness, col, interactPos);
			}
		}

        public static float DrawWireStraight(Vector2[] points, float thickness, Color col, Vector2 interactPos)
		{
			float interactSqrDst = float.MaxValue;
			if (points.Length == 0)
			{
				Debug.Log("No points");
				return 0;
			}
			Vector2 inA = points[0];

			for (int i = 1; i < points.Length; i++)
			{
				Vector2 inB = points[i];
				WireSegmentDraw(inA, inB, thickness, col, interactPos, ref interactSqrDst);
				inA = inB;
			}

			return interactSqrDst;
		}

		static void WireSegmentDraw(Vector2 start, Vector2 end, float thickness, Color col, Vector2 interactPos, ref float minSqrDst)
		{
			Draw.Line(start, end, thickness, col);
			float sqrDst = Maths.SqrDistanceToLineSegment(interactPos, start, end);
			if (sqrDst < minSqrDst) minSqrDst = sqrDst;
		}

		public static float DrawWireCurved(Vector2[] points, float thickness, Color col, Vector2 interactPos)
		{
			float interactSqrDst = float.MaxValue;
			Vector2 inA = points[0];

			float curveSize = 0.12f*Project.ActiveProject.description.Prefs_WireCurvatureMode;
			int resolution = 20;

			for (int i = 1; i < points.Length - 1; i++)
			{
				Vector2 inB = points[i];
				Vector2 inC = points[i + 1];
				Vector2 targetPoint = inB;
				Vector2 targetDir = (inB - inA).normalized;
				float dstToTarget = (inB - inA).magnitude;
				float dstToCurveStart = Mathf.Max(dstToTarget - curveSize, dstToTarget / 2);

				Vector2 nextTargetDir = (inC - inB).normalized;
				float nextLineLength = (inC - inB).magnitude;

				Vector2 curveStartPoint = inA + targetDir * dstToCurveStart;
				Vector2 curveEndPoint = targetPoint + nextTargetDir * Mathf.Min(curveSize, nextLineLength / 2);

				// Bezier
				for (int j = 0; j < resolution; j++)
				{
					float t = j / (resolution - 1f);
					Vector2 a = Vector2.Lerp(curveStartPoint, targetPoint, t);
					Vector2 b = Vector2.Lerp(targetPoint, curveEndPoint, t);
					Vector2 p = Vector2.Lerp(a, b, t);

					WireSegmentDraw(inA, p, thickness, col, interactPos, ref interactSqrDst);
					inA = p;
				}
			}

			WireSegmentDraw(inA, points[^1], thickness, col, interactPos, ref interactSqrDst);
			return interactSqrDst;
		}
	}
}