using UnityEngine;

namespace DLS.Game
{
	/// <summary>
	/// Controls the eraser mode state for quick deletion of components and wires.
	/// Provides toggleable modes: Off, Delete All, Wires Only.
	/// </summary>
	public static class EraserModeController
	{
		public enum EraserMode
		{
			Off,
			DeleteAll,
			WiresOnly
		}

		private static EraserMode currentMode = EraserMode.Off;

		/// <summary>
		/// Current eraser mode state
		/// </summary>
		public static EraserMode CurrentMode => currentMode;

		/// <summary>
		/// Whether eraser mode is currently active (not Off)
		/// </summary>
		public static bool IsActive => currentMode != EraserMode.Off;

		/// <summary>
		/// Toggle eraser mode between Off, DeleteAll, and WiresOnly
		/// </summary>
		public static void ToggleEraserMode()
		{
			currentMode = currentMode switch
			{
				EraserMode.Off => EraserMode.DeleteAll,
				EraserMode.DeleteAll => EraserMode.Off,
				EraserMode.WiresOnly => EraserMode.Off,
				_ => EraserMode.Off
			};

			Debug.Log($"[EraserMode] Toggled to: {currentMode}");
		}

		/// <summary>
		/// Toggle between DeleteAll and WiresOnly modes (only works when eraser is active)
		/// </summary>
		public static void ToggleWiresOnlyMode()
		{
			if (!IsActive) return;

			currentMode = currentMode switch
			{
				EraserMode.DeleteAll => EraserMode.WiresOnly,
				EraserMode.WiresOnly => EraserMode.DeleteAll,
				_ => currentMode
			};

			Debug.Log($"[EraserMode] Switched to: {currentMode}");
		}

		/// <summary>
		/// Turn off eraser mode
		/// </summary>
		public static void DisableEraserMode()
		{
			if (currentMode != EraserMode.Off)
			{
				currentMode = EraserMode.Off;
				Debug.Log("[EraserMode] Disabled");
			}
		}

		/// <summary>
		/// Get display text for current mode
		/// </summary>
		public static string GetModeText()
		{
			return currentMode switch
			{
				EraserMode.DeleteAll => "Delete All",
				EraserMode.WiresOnly => "Wires Only",
				_ => "Off"
			};
		}
	}
}

