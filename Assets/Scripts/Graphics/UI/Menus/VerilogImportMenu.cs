using System;
using System.IO;
using DLS.Description;
using DLS.Game;
using DLS.SaveSystem;
using Seb.Helpers;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;
using static DLS.Graphics.DrawSettings;

namespace DLS.Graphics
{
	/// <summary>
	/// Menu to import circuits from Verilog (.v) files.
	/// </summary>
	public static class VerilogImportMenu
	{
		public static void DrawMenu()
		{
			MenuHelper.DrawBackgroundOverlay();
			Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				const float menuWidth = 40f;
				Vector2 topLeft = new(Seb.Vis.UI.UI.Centre.x - menuWidth / 2, Seb.Vis.UI.UI.Centre.y + 5f);

				Seb.Vis.UI.UI.DrawText("IMPORT FROM VERILOG", ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular + 1, topLeft + Vector2.right * (menuWidth / 2), Anchor.TextCentre, new Color(0.5f, 0.8f, 1f));
				topLeft += Vector2.down * 3f;

				if (Seb.Vis.UI.UI.Button("IMPORT FROM FILE (.v)", ActiveUITheme.MenuPopupButtonTheme, topLeft, new Vector2(menuWidth, DrawSettings.ButtonHeight), true, false, false, ActiveUITheme.MenuPopupButtonTheme.buttonCols, Anchor.TopLeft))
					PickVerilogFile();

				Vector2 exitTopLeft = topLeft + Vector2.down * (DrawSettings.ButtonHeight + 1f);
				if (MenuHelper.DrawOKButton(exitTopLeft, menuWidth, DrawSettings.ButtonHeight, false, true, "EXIT", true, false) == MenuHelper.CancelConfirmResult.Confirm)
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);

				MenuHelper.DrawReservedMenuPanel(panelID, Seb.Vis.UI.UI.GetCurrentBoundsScope());
			}

			if (KeyboardShortcuts.CancelShortcutTriggered)
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
		}

		static void ImportCircuit(GroupDescription group)
		{
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
			var project = Project.ActiveProject;
			project.controller.StartPlacingGroup(group, InputHelper.MousePosWorld);
		}

		static void PickVerilogFile()
		{
			NativeFilePicker.PickFile((path) =>
			{
				if (string.IsNullOrEmpty(path))
					return;

				try
				{
					string content = File.ReadAllText(path);
					var chipLibrary = Project.ActiveProject?.chipLibrary;
					GroupDescription group = VerilogImporter.ImportFromVerilog(content, chipLibrary, out string error);
					if (group != null)
					{
						ImportCircuit(group);
					}
					else
					{
						Debug.LogError($"[Verilog Import] {error}");
					}
				}
				catch (Exception ex)
				{
					Debug.LogError($"[Verilog Import] Failed to read file: {ex.Message}");
				}
			},
			// Use .v/.vh extensions so Editor filter shows Verilog files (not text/plain -> *.plain)
			new[] { "v", "vh", "*" }
			);
		}
	}
}
