using System.Collections.Generic;
using DLS.Description;
using UnityEngine;

namespace DLS.Graphics
{
	/// <summary>
	/// Controls / keybinding screen from Community Edition. Not implemented on mobile (touch-based);
	/// stub so references compile. Full implementation would require Main.ActiveShortcutSettings,
	/// MenuHelper.GetComplexStringRepresentationOfShortcut, InputHelper key logging, etc.
	/// </summary>
	public static class ControlsScreen
	{
		public static Dictionary<string, Shortcut> configurableShortcuts = new Dictionary<string, Shortcut>();

		public static void DrawControlsScreen() { }

		public static void OpenControlsScreen() { }
	}
}
