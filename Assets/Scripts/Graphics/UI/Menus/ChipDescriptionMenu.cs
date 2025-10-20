using System.Collections.Generic;
using DLS.Description;
using DLS.Game;
using Seb.Helpers;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	public static class ChipDescriptionMenu
	{
		static string chipName;
		static string chipDescription;
		static UIHandle ID_Scrollbar = new("ChipDescription_Scrollbar");

		/// <summary>
		/// Opens the chip description menu for a specific chip
		/// </summary>
		/// <param name="chipName">The name of the chip to show description for</param>
		public static void OpenForChip(string chipName)
		{
			ChipDescriptionMenu.chipName = chipName;
			
			// Try to get description by chip type first
			if (Project.ActiveProject.chipLibrary.TryGetChipDescription(chipName, out ChipDescription chipDesc))
			{
				chipDescription = ChipDescriptionData.GetDescription(chipDesc.ChipType);
			}
			else
			{
				// Fallback to name-based lookup
				chipDescription = ChipDescriptionData.GetDescriptionByName(chipName);
			}
			
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.ChipDescription);
		}

		/// <summary>
		/// Opens the chip description menu for a chip type
		/// </summary>
		/// <param name="chipType">The chip type to show description for</param>
		public static void OpenForChipType(ChipType chipType)
		{
			chipName = ChipTypeHelper.GetName(chipType);
			chipDescription = ChipDescriptionData.GetDescription(chipType);
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.ChipDescription);
		}

		public static void DrawMenu()
		{
			MenuHelper.DrawBackgroundOverlay();

			// Calculate window dimensions
			const float windowWidth = 50f;
			const float windowHeight = 40f; // Increased height for more content space
			const float titleHeight = 3f;
			const float buttonHeight = 1f;
			const float margin = 1f;
			
			// Calculate content area (window minus title and button areas)
			// Account for margins: title margin + content margins + button margin
			const float contentHeight = windowHeight - titleHeight - buttonHeight - margin * 5;
			
			Vector2 windowSize = new(windowWidth, windowHeight);
			Vector2 windowPos = Seb.Vis.UI.UI.Centre;
			
			// Draw main window panel
			Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();
			Bounds2D windowBounds = Bounds2D.CreateFromCentreAndSize(windowPos, windowSize);
			
			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				// Draw title bar
				DrawTitleBar(windowBounds.TopLeft, new Vector2(windowWidth, titleHeight));
				
				// Draw content area with scrollable text
				Vector2 contentTopLeft = windowBounds.TopLeft + Vector2.down * (titleHeight + margin);
				Vector2 contentSize = new(windowWidth - margin * 2, contentHeight);
				DrawContentArea(contentTopLeft, contentSize);
				
				// Draw close button (narrower and centered with proper spacing)
				Vector2 buttonTopLeft = windowBounds.BottomLeft + Vector2.up * (buttonHeight + margin * 3.5f);
				const float buttonWidth = 15f; // Much narrower button
				Vector2 buttonSize = new(buttonWidth, buttonHeight);
				Vector2 buttonPos = new(windowBounds.Centre.x - buttonWidth/2, buttonTopLeft.y); // Center horizontally
				DrawCloseButton(buttonPos, buttonSize);
			}
			
			MenuHelper.DrawReservedMenuPanel(panelID, windowBounds, false);
		}

		static void DrawTitleBar(Vector2 topLeft, Vector2 size)
		{
			// Draw title background
			Color titleBgCol = ColHelper.MakeCol("#2D2D2D");
			Color titleTextCol = ColHelper.MakeCol("#3CD168");
			
			MenuHelper.DrawLeftAlignTextWithBackground(
				$"Chip Description: {chipName}",
				topLeft,
				size,
				Anchor.TopLeft,
				titleTextCol,
				titleBgCol,
				true
			);
		}

		static void DrawContentArea(Vector2 topLeft, Vector2 size)
		{
			// Draw content background
			Color contentBgCol = ColHelper.MakeCol("#1D1D1D");
			Seb.Vis.UI.UI.DrawPanel(topLeft, size, contentBgCol, Anchor.TopLeft);
			
			// Create scrollable text area with minimal margins for maximum content space
			Bounds2D contentBounds = Bounds2D.CreateFromTopLeftAndSize(topLeft, size);
			Bounds2D scrollBounds = Bounds2D.Shrink(contentBounds, 0.5f); // Minimal margin for text readability
			
			// Draw scrollable text using content-based scroll view
			ScrollViewTheme scrollTheme = DrawSettings.ActiveUITheme.ScrollTheme;
			Seb.Vis.UI.UI.DrawScrollView(
				ID_Scrollbar,
				scrollBounds.TopLeft,
				scrollBounds.Size,
				Anchor.TopLeft,
				scrollTheme,
				DrawDescriptionContent
			);
		}

		static void DrawDescriptionContent(Vector2 topLeft, float width, bool isLayoutPass)
		{
			// Draw the description text
			Color textCol = Color.white;
			FontType font = MenuHelper.Theme.FontRegular;
			float fontSize = MenuHelper.Theme.FontSizeRegular;
			
			// Split text into lines and draw each line with proper wrapping
			string[] lines = chipDescription.Split('\n');
			Vector2 currentPos = topLeft;
			const float lineHeight = 1.2f;
			
			foreach (string line in lines)
			{
				if (string.IsNullOrWhiteSpace(line))
				{
					// Empty line - just add spacing
					currentPos.y -= lineHeight;
					continue;
				}
				
				// Check if this is a section header (starts with specific keywords)
				bool isHeader = line.StartsWith("What it is:") || 
				               line.StartsWith("How it works:") || 
				               line.StartsWith("When to use:") || 
				               line.StartsWith("Example:") || 
				               line.StartsWith("Tips:");
				
				Color lineColor = isHeader ? ColHelper.MakeCol("#3CD168") : textCol;
				FontType lineFont = isHeader ? MenuHelper.Theme.FontBold : font;
				
				// Wrap the text to fit within the available width
				string[] wrappedLines = WrapText(line, width, lineFont, fontSize);
				
				foreach (string wrappedLine in wrappedLines)
				{
					// Draw the wrapped line
					Seb.Vis.UI.UI.DrawText(
						wrappedLine,
						lineFont,
						fontSize,
						currentPos,
						Anchor.TopLeft,
						lineColor
					);
					
					// Use PrevBounds to track the actual bounds of the drawn text
					// This is crucial for the scroll view to calculate total content height
					currentPos = Seb.Vis.UI.UI.PrevBounds.BottomLeft + Vector2.down * 0.1f; // Small spacing between lines
				}
			}
		}
		
		/// <summary>
		/// Wraps text to fit within the specified width
		/// </summary>
		static string[] WrapText(string text, float maxWidth, FontType font, float fontSize)
		{
			if (string.IsNullOrEmpty(text)) return new string[] { "" };
			
			// Calculate approximate character width (this is a rough estimate)
			float charWidth = fontSize * 0.6f; // Approximate character width
			int maxCharsPerLine = Mathf.FloorToInt(maxWidth / charWidth);
			
			if (text.Length <= maxCharsPerLine) return new string[] { text };
			
			string[] words = text.Split(' ');
			List<string> lines = new List<string>();
			string currentLine = "";
			
			foreach (string word in words)
			{
				string testLine = currentLine.Length == 0 ? word : currentLine + " " + word;
				
				if (testLine.Length <= maxCharsPerLine)
				{
					currentLine = testLine;
				}
				else
				{
					if (currentLine.Length > 0)
					{
						lines.Add(currentLine);
						currentLine = word;
					}
					else
					{
						// Word is too long, add it anyway
						lines.Add(word);
					}
				}
			}
			
			if (currentLine.Length > 0)
			{
				lines.Add(currentLine);
			}
			
			return lines.ToArray();
		}

		static void DrawCloseButton(Vector2 position, Vector2 size)
		{
			ButtonTheme buttonTheme = MenuHelper.Theme.ButtonTheme;
			
			bool closePressed = Seb.Vis.UI.UI.Button(
				"CLOSE",
				buttonTheme,
				position,
				size,
				true,
				false,
				true,
				buttonTheme.buttonCols,
				Anchor.TopLeft // Center the button at the position
			);
			
			if (closePressed || KeyboardShortcuts.CancelShortcutTriggered)
			{
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
			}
		}

		public static void OnMenuOpened()
		{
			// Reset scroll position when menu opens
			ScrollBarState scrollState = Seb.Vis.UI.UI.GetScrollbarState(ID_Scrollbar);
			scrollState.scrollY = 0f;
		}

		public static void Reset()
		{
			chipName = "";
			chipDescription = "";
		}
	}
}
