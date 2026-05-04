using UnityEngine;

namespace DLS.Game
{
	/// <summary>Computes rigid (rotate+translate, no scale) transforms from two-finger touch for camera and chip rotation.</summary>
	public static class TwoFingerRigidTransform
	{
		/// <summary>Result of solving for camera state: position and rotation that maps anchor world points to current screen positions.</summary>
		public readonly struct CameraSolveResult
		{
			public readonly Vector2 Pos;
			public readonly float RotationDegrees;

			public CameraSolveResult(Vector2 pos, float rotationDegrees)
			{
				Pos = pos;
				RotationDegrees = rotationDegrees;
			}
		}

		/// <summary>Result of solving for chip transform. Apply via: newPos = PivotTarget + Rotate(pos - Pivot, RotationDeltaDegrees).</summary>
		public readonly struct ChipSolveResult
		{
			public readonly Vector2 Pivot;
			public readonly Vector2 PivotTarget;
			public readonly float RotationDeltaDegrees;

			public ChipSolveResult(Vector2 pivot, Vector2 pivotTarget, float rotationDeltaDegrees)
			{
				Pivot = pivot;
				PivotTarget = pivotTarget;
				RotationDeltaDegrees = rotationDeltaDegrees;
			}

			public Vector2 TransformPoint(Vector2 pos)
			{
				float rad = RotationDeltaDegrees * Mathf.Deg2Rad;
				Vector2 delta = pos - Pivot;
				Vector2 rotated = new(delta.x * Mathf.Cos(rad) - delta.y * Mathf.Sin(rad), delta.x * Mathf.Sin(rad) + delta.y * Mathf.Cos(rad));
				return PivotTarget + rotated;
			}
		}

		/// <summary>Snap rotation to nearest step. Steps = Prefs_RotationSteps (0 = Free, no snapping).</summary>
		public static float SnapRotation(float degrees, int steps)
		{
			if (steps <= 0) return degrees;
			int stepDegrees = 360 / steps;
			int snapped = Mathf.RoundToInt(degrees / stepDegrees) * stepDegrees;
			float result = snapped % 360f;
			if (result < 0) result += 360f;
			return result;
		}

		/// <summary>Compute camera position and rotation so anchorWorld1→screen1 and anchorWorld2→screen2. OrthoSize fixed (no zoom).</summary>
		public static CameraSolveResult SolveCameraRigid(
			Vector2 anchorWorld1, Vector2 anchorWorld2,
			Vector2 screen1, Vector2 screen2,
			float orthoSize, float currentRotationDegrees)
		{
			// Unproject screen positions to view space (camera at origin, no rotation)
			Vector2 v1 = ScreenToView(screen1, orthoSize);
			Vector2 v2 = ScreenToView(screen2, orthoSize);

			Vector2 viewDelta = v2 - v1;
			Vector2 worldDelta = anchorWorld2 - anchorWorld1;

			float viewAngle = Mathf.Atan2(viewDelta.y, viewDelta.x);
			float worldAngle = Mathf.Atan2(worldDelta.y, worldDelta.x);
			float rotationRad = worldAngle - viewAngle;
			float rotationDeg = rotationRad * Mathf.Rad2Deg;

			// Rotate v1 by the solved rotation, then camera pos = anchorWorld1 - rotatedV1
			float cos = Mathf.Cos(rotationRad);
			float sin = Mathf.Sin(rotationRad);
			Vector2 rotatedV1 = new(v1.x * cos - v1.y * sin, v1.x * sin + v1.y * cos);
			Vector2 pos = anchorWorld1 - rotatedV1;

			return new CameraSolveResult(pos, rotationDeg);
		}

		/// <summary>Solve chip transform with pivot-on-chip: pivot finger = translation center + rotation pivot. rotationSteps=0 for free.</summary>
		public static ChipSolveResult SolveChipPivotRotate(
			Vector2 anchorPivot, Vector2 anchorOther,
			Vector2 currentPivot, Vector2 currentOther,
			int rotationSteps)
		{
			float anchorAngle = Mathf.Atan2(anchorOther.y - anchorPivot.y, anchorOther.x - anchorPivot.x);
			float currentAngle = Mathf.Atan2(currentOther.y - currentPivot.y, currentOther.x - currentPivot.x);
			float rotationRad = currentAngle - anchorAngle;
			float rotationDeg = rotationRad * Mathf.Rad2Deg;

			float snappedDelta = rotationSteps > 0 ? Mathf.RoundToInt(rotationDeg / (360f / rotationSteps)) * (360f / rotationSteps) : rotationDeg;

			return new ChipSolveResult(anchorPivot, currentPivot, snappedDelta);
		}

		/// <summary>Compute rigid transform (translation, rotation delta) mapping anchor world points to target world points. rotationSteps=0 for free (no snapping).</summary>
		public static ChipSolveResult SolveChipRigid(
			Vector2 anchorWorld1, Vector2 anchorWorld2,
			Vector2 targetWorld1, Vector2 targetWorld2,
			int rotationSteps)
		{
			Vector2 srcDelta = anchorWorld2 - anchorWorld1;
			Vector2 tgtDelta = targetWorld2 - targetWorld1;

			float srcAngle = Mathf.Atan2(srcDelta.y, srcDelta.x);
			float tgtAngle = Mathf.Atan2(tgtDelta.y, tgtDelta.x);
			float rotationRad = tgtAngle - srcAngle;
			float rotationDeg = rotationRad * Mathf.Rad2Deg;

			float snappedDelta = rotationSteps > 0 ? Mathf.RoundToInt(rotationDeg / (360f / rotationSteps)) * (360f / rotationSteps) : rotationDeg;

			// T(p) = target1 + R*(p - anchor1). Pivot=anchor1, PivotTarget=target1.
			return new ChipSolveResult(anchorWorld1, targetWorld1, snappedDelta);
		}

		static Vector2 ScreenToView(Vector2 screenPos, float orthoSize)
		{
			float aspect = (float)Screen.width / Screen.height;
			float ndcX = 2f * screenPos.x / Screen.width - 1f;
			float ndcY = 2f * screenPos.y / Screen.height - 1f;
			return new Vector2(ndcX * orthoSize * aspect, ndcY * orthoSize);
		}
	}
}
