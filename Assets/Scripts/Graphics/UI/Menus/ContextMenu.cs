using System;
using System.Collections.Generic;
using System.Linq;
using DLS.Description;
using DLS.Game;
using DLS.Game.LevelsIntegration;
using Seb.Helpers;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	public static class ContextMenu
	{
		const int pad = 10;
		const string menuDividerString = "#--#";
		static string interactionContextName;
		static bool bottomBarItemIsCollection;
		static Vector2 mouseOpenMenuPos;
		static bool shouldCenterMenu; // Flag to center menu on next draw

		static MenuEntry[] activeContextMenuEntries;
		static readonly MenuEntry dividerMenuEntry = new(menuDividerString, null, null);
		static bool wasMouseOverMenu;
		static string contextMenuHeader;

		static readonly MenuEntry[] pinColEntries = ((PinColour[])Enum.GetValues(typeof(PinColour))).Select(col =>
			new MenuEntry(Format(Enum.GetName(typeof(PinColour), col)), () => SetCol(col), CanSetCol)
		).ToArray();


		static readonly MenuEntry deleteEntry = new(Format("DELETE"), Delete, CanDelete);
		static readonly MenuEntry openChipEntry = new(Format("OPEN"), OpenChip, CanOpenChip);
		static readonly MenuEntry labelChipEntry = new(Format("LABEL"), OpenChipLabelPopup, CanLabelChip);
		static readonly MenuEntry infoEntry = new(Format("INFO"), OpenChipInfo, CanShowChipInfo);

		static readonly MenuEntry[] entries_customSubchip =
		{
			new(Format("VIEW"), EnterViewMode, CanEnterViewMode),
			openChipEntry,
			infoEntry,
			labelChipEntry,
			deleteEntry
		};

		static readonly MenuEntry[] entries_builtinSubchip =
		{
			infoEntry,
			labelChipEntry,
			deleteEntry
		};

		static readonly MenuEntry[] entries_builtinLED = entries_builtinSubchip.Concat(new[] { dividerMenuEntry }).Concat(pinColEntries).ToArray();

		static readonly MenuEntry[] entries_builtinButton = entries_builtinLED;

		static readonly MenuEntry[] entries_builtinBus =
		{
			new(Format("FLIP"), FlipBus, CanFlipBus),
			infoEntry,
			labelChipEntry,
			deleteEntry
		};

		static readonly MenuEntry[] entries_builtinKeySubchip =
		{
			new(Format("REBIND"), OpenKeyBindMenu, CanEditCurrentChip),
			infoEntry,
			labelChipEntry,
			deleteEntry
		};

		static readonly MenuEntry[] entries_builtinRomSubchip =
		{
			new(Format("EDIT"), OpenRomEditMenu, CanEditCurrentChip),
			infoEntry,
			labelChipEntry,
			deleteEntry
		};

		static readonly MenuEntry[] entries_builtinPulseChip =
		{
			new(Format("EDIT"), OpenPulseEditMenu, CanEditCurrentChip),
			infoEntry,
			labelChipEntry,
			deleteEntry
		};

        static readonly MenuEntry[] entries_builtinConstantChip =
{
            new(Format("EDIT"), OpenConstantEditMenu, CanEditCurrentChip),
            infoEntry,
            labelChipEntry,
            deleteEntry
        };



        static readonly MenuEntry[] entries_subChipOutput = pinColEntries;

		static readonly MenuEntry[] entries_inputDevPin = new[]
		{
			new(Format("EDIT"), OpenPinEditMenu, CanEditCurrentChip),
			new(Format("DELETE"), Delete, CanDelete),
			dividerMenuEntry
		}.Concat(pinColEntries).ToArray();

		static readonly MenuEntry[] entries_outputDevPin =
		{
			entries_inputDevPin[0],
			entries_inputDevPin[1]
		};

		static readonly MenuEntry[] entries_wire =
		{
			new(Format("EDIT"), EditWire, CanEditWire),
			new(Format("DELETE"), Delete, CanDelete)
		};

		static readonly MenuEntry[] entries_bottomBarChip =
		{
			openChipEntry,
			new(Format("UN-STAR"), UnstarBottomBarEntry, () => true)
		};

		static readonly MenuEntry[] entries_collectionPopupChip =
		{
			openChipEntry
		};

		static readonly MenuEntry[] entries_bottomBarCollection =
		{
			new(Format("UN-STAR"), UnstarBottomBarEntry, () => true)
		};

		public static bool IsOpen { get; private set; }
		public static IInteractable interactionContext { get; private set; }


		static string Format(string s)
		{
			s = char.ToUpper(s[0]) + s.Substring(1).ToLower();
			return s.PadRight(pad);
		}

		public static void Update()
		{
			bool inMenu = !(UIDrawer.ActiveMenu is UIDrawer.MenuType.None or UIDrawer.MenuType.BottomBarMenuPopup or UIDrawer.MenuType.ChipCustomization);
			if (inMenu)
			{
				CloseContextMenu();
			}
			else
			{

				// Draw
				if (IsOpen) DrawContextMenu(activeContextMenuEntries);

				// Close menu input
				if (InputHelper.IsMouseDownThisFrame(MouseButton.Left) || KeyboardShortcuts.CancelShortcutTriggered)
				{
					CloseContextMenu();

				}

				HandleOpenMenuInput();

			}
		}

		static void HandleOpenMenuInput()
		{
			// Open menu input
			#if UNITY_ANDROID || UNITY_IOS
			if (MobileUIControllerWrapper.IsWrenchToolActive && TouchInputHelper.TouchTapDown() &&!TouchInputHelper.Instance.isPressingUI)
			#else
			if (InputHelper.IsMouseDownThisFrame(MouseButton.Right) && !KeyboardShortcuts.CameraActionKeyHeld && !InteractionState.MouseIsOverUI)
			#endif
			{
				bool inCustomizeMenu = UIDrawer.ActiveMenu == UIDrawer.MenuType.ChipCustomization;
				IInteractable hoverElement = InteractionState.ElementUnderMouse;

				bool openSubChipContextMenu = hoverElement is SubChipInstance && !inCustomizeMenu;
				bool openDevPinContextMenu = (hoverElement is PinInstance pin && pin.parent is DevPinInstance) || hoverElement is DevPinInstance;
				bool openWireContextMenu = hoverElement is WireInstance;
				bool openSubchipOutputPinContextMenu = hoverElement is PinInstance pin2 && pin2.parent is SubChipInstance && pin2.IsSourcePin && !pin2.IsBusPin;

				if (openSubChipContextMenu || openDevPinContextMenu || openWireContextMenu || openSubchipOutputPinContextMenu)
				{
					#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
					MobileUIControllerWrapper.OnWrenchButtonPress();
					#endif
					interactionContextName = string.Empty;
					interactionContext = hoverElement;
					string headerName = string.Empty;

					if (openSubChipContextMenu)
					{
						SubChipInstance subChip = (SubChipInstance)hoverElement;
						interactionContextName = subChip.Description.Name;

						if (subChip.ChipType == ChipType.Custom)
						{
							headerName = subChip.Description.Name;
							activeContextMenuEntries = entries_customSubchip;
						}
						else // builtin type
						{
							headerName = ChipTypeHelper.IsBusType(subChip.ChipType) ? "BUS" : subChip.Description.Name;
							if (subChip.ChipType is ChipType.Key) activeContextMenuEntries = entries_builtinKeySubchip;
							else if (ChipTypeHelper.IsRomType(subChip.ChipType)) activeContextMenuEntries = entries_builtinRomSubchip;
							else if (subChip.ChipType is ChipType.Pulse) activeContextMenuEntries = entries_builtinPulseChip;
							else if (ChipTypeHelper.IsBusType(subChip.ChipType)) activeContextMenuEntries = entries_builtinBus;
							else if (subChip.ChipType == ChipType.DisplayLED) activeContextMenuEntries = entries_builtinLED;
							else if (subChip.ChipType == ChipType.Button) activeContextMenuEntries = entries_builtinButton;
							else if (subChip.ChipType == ChipType.Constant_8Bit) activeContextMenuEntries = entries_builtinConstantChip;

							else activeContextMenuEntries = entries_builtinSubchip;
						}
						#if !(UNITY_ANDROID || UNITY_IOS)
						Project.ActiveProject.controller.Select(interactionContext as IMoveable, false);
						#endif
					}
					else if (openDevPinContextMenu)
					{
						if (interactionContext is DevPinInstance devPinInstance) interactionContext = devPinInstance.Pin;

						PinInstance activePin = (PinInstance)interactionContext;
						headerName = CreatePinHeaderName(activePin.Name);
						interactionContextName = activePin.Name;
						Project.ActiveProject.controller.Select(activePin.parent, false);
						activeContextMenuEntries = activePin.IsSourcePin ? entries_inputDevPin : entries_outputDevPin;
					}
					else if (openWireContextMenu)
					{
						WireInstance wire = (WireInstance)interactionContext;
						if (wire.IsBusWire) headerName = "BUS LINE";
						else headerName = CreateWireHeaderString(wire);

						activeContextMenuEntries = entries_wire;
					}
					else if (openSubchipOutputPinContextMenu)
					{
						PinInstance pinContext = (PinInstance)interactionContext;
						headerName = CreatePinHeaderName(pinContext.Name);
						activeContextMenuEntries = entries_subChipOutput;
					}

					SetContextMenuOpen(headerName);
				}
				else
				{
					CloseContextMenu();
				}
			}
		}

		static string CreateWireHeaderString(WireInstance wire)
		{
			string pinName = wire.SourcePin.Name;
			if (string.IsNullOrWhiteSpace(pinName)) return "WIRE";

			return "WIRE: " + pinName;
		}

		static string CreatePinHeaderName(string pinName)
		{
			if (string.IsNullOrWhiteSpace(pinName)) return "PIN";

			return "PIN: " + pinName;
		}

		public static void OpenBottomBarContextMenu(string name, bool isCollection, bool isFromInsideCollection)
		{
			interactionContextName = name;
			bottomBarItemIsCollection = isCollection;
			interactionContext = null;
			SetContextMenuOpen(name);

			if (isCollection)
			{
				activeContextMenuEntries = entries_bottomBarCollection;
			}
			else
			{
				activeContextMenuEntries = isFromInsideCollection ? entries_collectionPopupChip : entries_bottomBarChip;
			}
		}

		static void SetContextMenuOpen(string header)
		{
			mouseOpenMenuPos = Seb.Vis.UI.UI.ScreenToUISpace(InputHelper.MousePos);
			contextMenuHeader = header.PadRight(pad);
			IsOpen = true;
		}


		static void DrawContextMenu(MenuEntry[] menuEntries)
		{
			Draw.StartLayer(Vector2.zero, 1, true);

			const float textOffsetX = 0.45f;
			ButtonTheme theme = DrawSettings.ActiveUITheme.MenuPopupButtonTheme;
			ButtonTheme headerTheme = DrawSettings.ActiveUITheme.MenuPopupButtonTheme;
			headerTheme.buttonCols.inactive = ColHelper.MakeCol(0.18f);
			headerTheme.textCols.inactive = Color.white;
			float menuWidth = Draw.CalculateTextBoundsSize(menuEntries[0].Text, theme.fontSize, theme.font).x + 1;
			float menuWidthHeader = Draw.CalculateTextBoundsSize(contextMenuHeader, theme.fontSize, theme.font).x + 1;
			menuWidth = Mathf.Max(menuWidth, menuWidthHeader);

			// If menu should be centered, calculate center position now (inside UI scope)
			if (shouldCenterMenu)
			{
				// Position menu in center-ish area, accounting for menu expanding downward
				// Estimate menu height: header + entries, each button is ~4 units tall on mobile
				#if UNITY_ANDROID || UNITY_IOS
				float estimatedMenuHeight = (menuEntries.Length + 1) * 4f; // 4 = buttonSize.y (2*2)
				#else
				float estimatedMenuHeight = (menuEntries.Length + 1) * 2f; // 2 = buttonSize.y
				#endif
				
				// Start from center and offset upward by half the estimated menu height
				float centerY = Seb.Vis.UI.UI.Height * 0.5f;
				float startY = centerY + estimatedMenuHeight * 0.5f;
				
				// Center X: offset left by half the menu width so menu is centered
				float centerX = Seb.Vis.UI.UI.Width * 0.5f - menuWidth * 0.5f;
				
				mouseOpenMenuPos = new Vector2(centerX, startY);
				shouldCenterMenu = false;
			}

			Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();
			#if UNITY_ANDROID || UNITY_IOS
			Vector2 buttonSize = new(menuWidth, 2*2);
			#else
			Vector2 buttonSize = new(menuWidth, 2);
			#endif
			Vector2 pos = mouseOpenMenuPos;
			if (pos.x + menuWidth > Seb.Vis.UI.UI.Width)
			{
				pos.x = Seb.Vis.UI.UI.Width - menuWidth;
			}

			bool expandDown = pos.y >= Seb.Vis.UI.UI.Height * 0.35f;
			float dirY = expandDown ? -1 : 1;
			Anchor anchor = expandDown ? Anchor.TopLeft : Anchor.BottomLeft;

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				for (int i = 0; i < menuEntries.Length; i++)
				{
					int index = expandDown ? i : menuEntries.Length - i - 1;
					MenuEntry entry = menuEntries[index];

					if (index == 0 && expandDown) DrawHeader();

					if (entry.Text == menuDividerString)
					{
						pos.y += 0.5f * dirY;
						Seb.Vis.UI.UI.DrawPanel(pos, new Vector2(menuWidth, 0.15f), ColHelper.MakeCol(0.6f), Anchor.CentreLeft);
						pos.y += 0.5f * dirY;
					}
					else
					{
						if (Seb.Vis.UI.UI.Button(entry.Text, theme, pos, buttonSize, entry.IsEnabled(), false, false, theme.buttonCols, anchor, true, textOffsetX))
						{
							entry.OnPress();
						}

						pos.y += buttonSize.y * dirY;
					}

					if (index == 0 && !expandDown) DrawHeader();
				}

				Bounds2D bounds = Seb.Vis.UI.UI.GetCurrentBoundsScope();
				Vector2 menuSize = new(menuWidth, bounds.Height);
				Seb.Vis.UI.UI.ModifyPanel(panelID, bounds.Centre, menuSize + Vector2.one * 0.5f, ColHelper.MakeCol(0.91f));
			}

			wasMouseOverMenu = Seb.Vis.UI.UI.MouseInsideBounds(Seb.Vis.UI.UI.PrevBounds);

			void DrawHeader()
			{
				Seb.Vis.UI.UI.Button(contextMenuHeader, headerTheme, pos, buttonSize, false, false, false,headerTheme.buttonCols, anchor, true, textOffsetX);
				pos.y += buttonSize.y * dirY;
			}
		}

		static bool IsCustomChip() => !Project.ActiveProject.chipLibrary.IsBuiltinChip(interactionContextName);
		static bool CanEnterViewMode() => IsCustomChip();
		static bool CanLabelChip() => Project.ActiveProject.CanEditViewedChip;
		static void EnterViewMode() => Project.ActiveProject.EnterViewMode(interactionContext as SubChipInstance);

		static bool CanDelete() => Project.ActiveProject.CanEditViewedChip;
		static bool CanFlipBus() => Project.ActiveProject.CanEditViewedChip;

		static bool CanSetCol()
		{
			if (!Project.ActiveProject.CanEditViewedChip || UIDrawer.ActiveMenu == UIDrawer.MenuType.ChipCustomization) return false;
			if (interactionContext is PinInstance pin) return pin.IsSourcePin;
			if (interactionContext is SubChipInstance subchip) return subchip.ChipType == ChipType.DisplayLED || subchip.ChipType == ChipType.Button;

			return false;
		}

		static void FlipBus()
		{
			((SubChipInstance)interactionContext).FlipBus();
		}

		static void SetCol(PinColour col)
		{
			if (interactionContext is PinInstance pin)
			{
				pin.Colour = col;
			}

			if(!(interactionContext is SubChipInstance subchip)) { return; }

			else if (subchip.ChipType == ChipType.DisplayLED)
			{
				Project.ActiveProject.NotifyLEDColourChanged(subchip, (uint)col);
			}
            else if (subchip.ChipType == ChipType.Button)
            {
                Project.ActiveProject.NotifyLEDColourChanged(subchip, (uint)col);
				subchip.OutputPins[0].Colour = col;
            }

        }

		static void OpenChipLabelPopup()
		{
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.ChipLabelPopup);
		}

		public static void EditWire()
		{
			Project.ActiveProject.controller.EnterWireEditMode((WireInstance)interactionContext);
		}

		static void Delete()
		{
			if (interactionContext is IMoveable moveable)
			{
				Project.ActiveProject.controller.Delete(moveable);
			}
			else if (interactionContext is WireInstance wire)
			{
				Project.ActiveProject.controller.DeleteWire(wire);
			}
			else if (interactionContext is PinInstance pin)
			{
				Project.ActiveProject.controller.Delete(pin.parent);
			}
		}

		static void OpenKeyBindMenu()
		{
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.RebindKeyChip);
		}

		static void OpenRomEditMenu() => UIDrawer.SetActiveMenu(UIDrawer.MenuType.RomEdit);

		static void OpenPulseEditMenu() => UIDrawer.SetActiveMenu(UIDrawer.MenuType.PulseEdit);

		static void OpenConstantEditMenu() => UIDrawer.SetActiveMenu(UIDrawer.MenuType.ConstantEdit);

		static void OpenChipInfo()
		{
			// Get the chip name from the interaction context
			string chipName = interactionContextName;
			ChipDescriptionMenu.OpenForChip(chipName);
		}

		static bool CanShowChipInfo() => true; // Always allow showing chip info

		static bool CanEditCurrentChip() => Project.ActiveProject.CanEditViewedChip;

		static bool CanEditWire() => CanEditCurrentChip();

		static void OpenPinEditMenu()
		{
			PinEditMenu.SetTargetPin((DevPinInstance)((PinInstance)interactionContext).parent);
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.PinRename);
		}

		static void OpenChip()
		{
			Project project = Project.ActiveProject;
			string chipToOpenName = interactionContextName;

			if (project.ActiveChipHasUnsavedChanges())
			{
				UnsavedChangesPopup.OpenPopup(OpenChipIfConfirmed);
			}
			else
			{
				OpenChipIfConfirmed(true);
			}

			void OpenChipIfConfirmed(bool confirm)
			{
				if (confirm)
				{
					// Check for level unsaved changes before opening chip
					if (LevelManager.Instance?.IsActive == true && LevelManager.Instance.HasUnsavedChanges())
					{
						LevelUnsavedChangesPopup.OpenPopup(OpenChipAfterLevelCheck);
					}
					else
					{
						OpenChipAfterLevelCheck(2); // Continue without saving (since there are no changes)
					}

					void OpenChipAfterLevelCheck(int option)
					{
						if (option == 0) // Cancel
						{
							// Do nothing, stay in current level
							return;
						}
						else if (option == 1) // Save and Continue
						{
							// Save level progress before opening chip
							if (LevelManager.Instance?.IsActive == true)
							{
								LevelManager.Instance.SaveCurrentProgress();
							}
							
							project.LoadDevChipOrCreateNewIfDoesntExist(chipToOpenName);
							LevelManager.Instance?.ExitLevel();
						}
						else if (option == 2) // Continue without Saving
						{
							// Open chip without saving level progress
							project.LoadDevChipOrCreateNewIfDoesntExist(chipToOpenName);
							LevelManager.Instance?.ExitLevel();
						}
					}
				}
			}
		}

		static bool CanOpenChip() => IsCustomChip() && CanEditCurrentChip();

		public static void Reset()
		{
			CloseContextMenu();
		}

		public static void CloseContextMenu()
		{
			IsOpen = false;
		}

		public static bool HasFocus() => IsOpen && wasMouseOverMenu;

		public static void UnstarBottomBarEntry()
		{
			Project.ActiveProject.SetStarred(interactionContextName, false, bottomBarItemIsCollection, true);
		}

		/// <summary>
		/// Sets the interaction context for use by edit menus (ROM, Key, Pulse, Constant, etc).
		/// This is needed when auto-opening edit menus from the wrench tool.
		/// </summary>
		public static void SetInteractionContext(IInteractable context)
		{
			interactionContext = context;
			if (context is SubChipInstance subChip)
			{
				interactionContextName = subChip.Description.Name;
			}
			else if (context is DevPinInstance devPin)
			{
				interactionContextName = devPin.Name;
			}
		}

		/// <summary>
		/// Opens the context menu centered on screen for the given chip.
		/// Used when auto-opening from wrench tool.
		/// </summary>
		public static void OpenContextMenuCentered(SubChipInstance subChip)
		{
			interactionContext = subChip;
			interactionContextName = subChip.Description.Name;
			
			// Determine which menu entries to show
			string headerName;
			if (subChip.ChipType == ChipType.Custom)
			{
				headerName = subChip.Description.Name;
				activeContextMenuEntries = entries_customSubchip;
			}
			else // builtin type
			{
				headerName = ChipTypeHelper.IsBusType(subChip.ChipType) ? "BUS" : subChip.Description.Name;
				if (subChip.ChipType is ChipType.Key) activeContextMenuEntries = entries_builtinKeySubchip;
				else if (ChipTypeHelper.IsRomType(subChip.ChipType)) activeContextMenuEntries = entries_builtinRomSubchip;
				else if (subChip.ChipType is ChipType.Pulse) activeContextMenuEntries = entries_builtinPulseChip;
				else if (ChipTypeHelper.IsBusType(subChip.ChipType)) activeContextMenuEntries = entries_builtinBus;
				else if (subChip.ChipType == ChipType.DisplayLED) activeContextMenuEntries = entries_builtinLED;
				else if (subChip.ChipType == ChipType.Button) activeContextMenuEntries = entries_builtinButton;
				else if (subChip.ChipType == ChipType.Constant_8Bit) activeContextMenuEntries = entries_builtinConstantChip;
				else activeContextMenuEntries = entries_builtinSubchip;
			}
			
			// Set flag to center menu on next draw (when UI scope is active)
			shouldCenterMenu = true;
			mouseOpenMenuPos = new Vector2(50f, 50f); // Temporary position
			
			contextMenuHeader = headerName.PadRight(pad);
			IsOpen = true;
		}

		/// <summary>
		/// Opens the context menu centered on screen for the given dev pin.
		/// Used when auto-opening from wrench tool.
		/// </summary>
		public static void OpenContextMenuCentered(DevPinInstance devPin)
		{
			// Convert DevPinInstance to PinInstance for context menu
			interactionContext = devPin.Pin;
			interactionContextName = devPin.Name;
			
			// Determine which menu entries to show
			string headerName = CreatePinHeaderName(devPin.Name);
			activeContextMenuEntries = devPin.IsInputPin ? entries_inputDevPin : entries_outputDevPin;
			
			// Set flag to center menu on next draw (when UI scope is active)
			shouldCenterMenu = true;
			mouseOpenMenuPos = new Vector2(50f, 50f); // Temporary position
			
			contextMenuHeader = headerName.PadRight(pad);
			IsOpen = true;
		}

		public readonly struct MenuEntry
		{
			public readonly string Text;
			public readonly Action OnPress;
			public readonly Func<bool> IsEnabled;

			public MenuEntry(string text, Action onPress, Func<bool> isEnabled)
			{
				Text = text;
				OnPress = onPress;
				IsEnabled = isEnabled;
			}
		}
	}
}
