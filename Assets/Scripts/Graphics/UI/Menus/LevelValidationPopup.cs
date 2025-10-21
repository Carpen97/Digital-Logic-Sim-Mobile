using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using DLS.Levels;
using Seb.Helpers;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using DLS.Game.LevelsIntegration; // For LevelManager.Instance.Current
using DLS.Game; // For Project class
using DLS.Online;
using static DLS.Graphics.DrawSettings;
using static Seb.Vis.UI.ButtonTheme;
using System.Security.Cryptography.X509Certificates;

// Data structures for levels.json parsing
[System.Serializable]
public class LevelsData
{
	public int schemaVersion;
	public string packId;
	public string packName;
	public string packDescription;
	public Chapter[] chapters;
}

[System.Serializable]
public class Chapter
{
	public string chapterId;
	public string chapterName;
	public string chapterDescription;
	public LevelDefinition[] levels;
}

namespace DLS.Graphics
{
	public static class LevelValidationPopup
	{
		// ---------- UI Constants ----------
		const float ListWidthFrac = 0.90f;
		const float ListHeightFrac = 0.73f;
		const float RowHeight = 4.2f;
		const float OkBtnWidthFrac = 0.30f;
		const float OkBtnHeightMul = 1.5f;
		const float LayoutYOffset = 0f; // shift whole window upwards

		// ---------- Zoom Constants ----------
		const float MinZoom = 0.5f;
		const float MaxZoom = 3.0f;
		const float ZoomStep = 0.2f;
		const float DefaultZoom = 1.0f;
		const float FitToWidthZoom = -1f; // Special value to indicate "fit to width"

		// ---------- Popup State ----------
		static string _title = "";
		static bool _isSuccess;
		static int _stars;
		static int _in_len;
		static int _out_len;
		static readonly List<TestRow> _rows = new();
		static int _selectedIndex = -1;
		static bool _isSequentialLevel = false;

		// ---------- Zoom State ----------
		static float _tableZoom = DefaultZoom;

        private static float defaultZoom;

		// ---------- Display Mode State ----------
		static int _displayMode = 0; // 0 = Binary, 1 = Graphical
		static readonly string[] _displayModeNames = { "Binary", "Graphical" };

		// ---------- Scrolling State ----------
		static float _hScroll = 0f;  // Horizontal scroll offset
		static float _contentWidth = 0f;  // Total content width (for horizontal scrolling)
		static float _headerHeight = 0f;  // Header height (excluded from vertical scroll)

		// ---------- Column Layout ----------
		static float _inColumnWidth = 0f;
		static float _expectedColumnWidth = 0f;
		static float _outColumnWidth = 0f;
		static float _resultColumnWidth = 0f;
		static float _columnSpacing = 0f;
		static float _dividerWidth = 3f; // Width of column dividers

		// ---------- Bit Group Positions ----------
		static List<float> _inputBitGroupXPositions = new List<float>();  // x positions for input bit groups
		static List<float> _expectedBitGroupXPositions = new List<float>(); // x positions for expected bit groups
		static List<float> _outputBitGroupXPositions = new List<float>();   // x positions for output bit groups
		static bool _bitGroupPositionsCalculated = false; // Flag to track if positions have been calculated

		// ---------- Internal row model ----------
		struct TestRow
		{
			public string Inputs;
			public string Expected;
			public string Got;       // Only reliable for failures (when provided in message)
			public bool Passed;
			public string Message;   // Failure message (e.g., "Expected X, got Y")
			public bool IsSequence;
			public string SequenceName;
			public List<TestRow> SequenceSteps;
		}

		// ---------- Firebase test state ----------
		static bool _isUploading = false;
		static string _uploadStatus = "";
        private static bool defaultZoomSet;
        private static float _viewportWidth;

        // ---------- UI Handles ----------


        static readonly UIHandle ID_LevelValidationPopup = new("LevelValidationPopup_Scrollbar");
		static readonly UIHandle ID_LevelValidationPopup_H = new("LevelValidationPopup_HScroll");

		// ---------- Public API ----------
		public static void Open(ValidationReport report)
		{
			_rows.Clear();
			_selectedIndex = -1;
			_bitGroupPositionsCalculated = false; // Reset flag when new data is loaded
			_in_len = report.AllTestResults[0].Inputs.Length;
			_out_len = report.AllTestResults[0].Expected.Length;

			_isSuccess = report.PassedAll;
			_stars = report.Stars;

			// Title
			if (_isSuccess)
			{
				_title = "All tests passed";
			}
			else
			{
				_title = report.Failures.Count > 1 ? $"{report.Failures.Count} tests failed" : "1 test failed";
			}

			// Populate test rows
			if (report.AllTestResults != null && report.AllTestResults.Count > 0)
			{
				foreach (var result in report.AllTestResults)
				{
					_rows.Add(new TestRow
					{
						Inputs = result.Inputs,
						Expected = result.Expected,
						Got = result.Actual,
						Passed = result.Passed,
						Message = result.Passed ? "OK" : $"Expected {result.Expected}, got {result.Actual}",
						IsSequence = false,
						SequenceName = "",
						SequenceSteps = new List<TestRow>()
					});
				}
			}

			// Reset zoom state so defaultZoom is calculated consistently
			_tableZoom = DefaultZoom;
			_hScroll = 0f;
			defaultZoomSet = false;
			Debug.Log($"[LevelValidationPopup] Opening popup - Reset _tableZoom to {_tableZoom}, defaultZoomSet = false");

			UIDrawer.SetActiveMenu(UIDrawer.MenuType.LevelValidationResult);
		}

		public static void DrawMenu()
		{
			// Dimmed backdrop
			MenuHelper.DrawBackgroundOverlay();

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				Draw.ID panelBG = Seb.Vis.UI.UI.ReservePanel();

				// Calculate panel dimensions
				float totalWidth = Seb.Vis.UI.UI.Width * ListWidthFrac;
				float totalHeight = Seb.Vis.UI.UI.Height * ListHeightFrac;
				float leftPanelWidth = totalWidth * 0.65f;  // Left panel takes 65% of total width
				float rightPanelWidth = totalWidth * 0.25f; // Right panel takes 25% of total width
				float panelSpacing = 2f;                    // Space between panels

				// Center the panels
				Vector2 centerPos = Seb.Vis.UI.UI.Centre + Vector2.up * LayoutYOffset;
				Vector2 leftPanelPos = new Vector2(centerPos.x - (rightPanelWidth + panelSpacing) * 0.5f, centerPos.y);
				Vector2 rightPanelPos = new Vector2(centerPos.x + (leftPanelWidth + panelSpacing) * 0.5f, centerPos.y);

				// Draw left panel (table area)
				DrawLeftPanel(leftPanelPos, new Vector2(leftPanelWidth, totalHeight));

				// Draw right panel (buttons)
				DrawRightPanel(rightPanelPos, new Vector2(rightPanelWidth, totalHeight));

				// Draw panel background
				Bounds2D overallBounds = Bounds2D.Grow(
					Bounds2D.CreateFromCentreAndSize(leftPanelPos, new Vector2(leftPanelWidth, totalHeight)),
					Bounds2D.CreateFromCentreAndSize(rightPanelPos, new Vector2(rightPanelWidth, totalHeight))
				);
				MenuHelper.DrawReservedMenuPanel(panelBG, overallBounds);
			}
		}

		// ---------- Panel Drawing ----------
		static void DrawLeftPanel(Vector2 panelPos, Vector2 panelSize)
		{
			// Draw clean left panel background
			Draw.ID leftPanelID = Seb.Vis.UI.UI.ReservePanel();
			Bounds2D leftPanelBounds = new Bounds2D(panelPos - panelSize * 0.5f, panelPos + panelSize * 0.5f);
			Seb.Vis.UI.UI.ModifyPanel(leftPanelID, leftPanelBounds, ColHelper.MakeCol(0.08f));

			// Calculate scroll view dimensions
			Vector2 panelTopLeft = leftPanelBounds.TopLeft + new Vector2(1f, -1f);
			float scrollBarWidth = DrawSettings.ActiveUITheme.ScrollTheme.scrollBarWidth;
			_viewportWidth = panelSize.x - 2f; // Account for panel padding
			#if UNITY_ANDROID || UNITY_IOS
			_viewportWidth -= 1.0f;
			#endif
			float viewportHeight = panelSize.y - 2f; // Account for panel padding

			// Calculate header height (two lines + padding) - scaled by zoom
			float lineHeight = ActiveUITheme.FontSizeRegular * 1.2f * _tableZoom;
			_headerHeight = lineHeight * 2f + 4f * _tableZoom; // Two lines + padding
			#if UNITY_ANDROID || UNITY_IOS
			_headerHeight *= 1.25f;
			#endif

			// Clamp scroll values - adjust for zoom
			_hScroll = Mathf.Clamp(_hScroll, 0f, Mathf.Max(0f, _contentWidth - _viewportWidth));
			float availableContentHeight = viewportHeight - _headerHeight - scrollBarWidth; // Account for horizontal scrollbar


			// Draw scrollable content area (below header, above horizontal scrollbar)
			Vector2 contentAreaTopLeft = panelTopLeft + Vector2.down * _headerHeight;
			float horizontalScrollbarHeight = scrollBarWidth;
			#if UNITY_ANDROID || UNITY_IOS
			Vector2 contentAreaSize = new Vector2(_viewportWidth, viewportHeight - _headerHeight - horizontalScrollbarHeight-1.3f);
			#else
			Vector2 contentAreaSize = new Vector2(_viewportWidth, viewportHeight - _headerHeight - horizontalScrollbarHeight-0.3f);
			#endif

			// Create scroll view for table content only
			ScrollViewTheme scrollTheme = DrawSettings.ActiveUITheme.ScrollTheme;
			scrollTheme.backgroundCol = ColHelper.MakeCol(0.12f);

			ScrollBarState sv = Seb.Vis.UI.UI.DrawScrollView(
				ID_LevelValidationPopup,
				contentAreaTopLeft,
				contentAreaSize,
				UILayoutHelper.DefaultSpacing * _tableZoom * 5,
				Anchor.TopLeft,
				scrollTheme,
				DrawTableContent,
				_rows.Count
			);
			
			// Draw header with horizontal scrolling (fixed vertical position)
			DrawHeader(panelTopLeft, _viewportWidth, viewportHeight, scrollBarWidth);

			using (Seb.Vis.UI.UI.CreateMaskScope(Bounds2D.CreateFromTopLeftAndSize(panelTopLeft, new Vector2(_viewportWidth, viewportHeight))))
            {
				DrawAllColumnDividers(panelTopLeft, _viewportWidth, viewportHeight - scrollBarWidth);
            }
			// Create horizontal scrollbar if content is wider than viewport
			if (_contentWidth > _viewportWidth)
			{
				// Position scrollbar at bottom of panel (matching backup implementation)
				Bounds2D hScrollViewArea = Bounds2D.CreateFromTopLeftAndSize(panelTopLeft, new Vector2(_viewportWidth, viewportHeight));
				Bounds2D hBarArea = Bounds2D.CreateFromTopLeftAndSize(
					new Vector2(panelTopLeft.x, panelTopLeft.y - viewportHeight + scrollBarWidth * 1.6f),
					new Vector2(_viewportWidth, scrollBarWidth)
				);


				var hScrollState = Seb.Vis.UI.UI.DrawScrollbarHorizontal(
					hScrollViewArea,
					hBarArea,
					_contentWidth,
					scrollTheme,
					ID_LevelValidationPopup_H
				);

				_hScroll = Mathf.Clamp(hScrollState.scrollX, 0f, Mathf.Max(0f, _contentWidth - _viewportWidth));
			}

			// Draw column dividers last so they cover both header and content
		}

		private static void DrawHeaderTableDivider(Vector2 panelTopLeft, float viewportWidth)
		{
			Seb.Vis.UI.UI.DrawPanel(
				new Bounds2D(
					panelTopLeft + new Vector2(0, -_headerHeight),
					panelTopLeft + new Vector2(viewportWidth, -_headerHeight + 1f)
				),
				ColHelper.MakeCol(0.08f)
			);
		}

		static void DrawRightPanel(Vector2 panelPos, Vector2 panelSize)
		{
			// Draw right panel background
			Draw.ID rightPanelID = Seb.Vis.UI.UI.ReservePanel();
			Bounds2D rightPanelBounds = new Bounds2D(panelPos - panelSize * 0.5f, panelPos + panelSize * 0.5f);
			Seb.Vis.UI.UI.ModifyPanel(rightPanelID, rightPanelBounds, ColHelper.MakeCol(0.08f));

			// Draw buttons
			Vector2 topLeft = rightPanelBounds.TopLeft + new Vector2(1f, -1f);
			float contentWidth = panelSize.x - 2f;
			float buttonHeight = ButtonHeight * 1.0f;
			float spacing = 1.2f;

			// Title and score
			Seb.Vis.UI.UI.DrawText(_title, ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular, topLeft, Anchor.TopLeft, Color.white);
			Vector2 scorePos = topLeft + new Vector2(0f, -RowHeight * 1.1f);
			int nandCount = GetNandGateCount();
			Seb.Vis.UI.UI.DrawText($"Score: {nandCount}", ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular, scorePos, Anchor.TopLeft, Color.yellow);

			// Display mode selector (above zoom buttons)
			Vector2 displayModePos = scorePos + new Vector2(0f, -RowHeight * 1.4f) + Vector2.up * (buttonHeight + spacing);;
			_displayMode = Seb.Vis.UI.UI.WheelSelector(
				new UIHandle("DisplayMode"),
				_displayModeNames,
				displayModePos,
				new Vector2(contentWidth, buttonHeight),
				MenuHelper.Theme.OptionsWheel,
				Anchor.TopLeft
			);

			// Zoom buttons (below display mode selector)
			Vector2 zoomBtnPos = displayModePos + Vector2.down * (buttonHeight + spacing);
			float thirdW = (contentWidth - spacing * 2) / 3f;
			Vector2 zoomInPos = zoomBtnPos;
			Vector2 fitWidthPos = new Vector2(zoomBtnPos.x + thirdW + spacing, zoomBtnPos.y);
			Vector2 zoomOutPos = new Vector2(zoomBtnPos.x + (thirdW + spacing) * 2, zoomBtnPos.y);

			bool zoomInPressed = Seb.Vis.UI.UI.Button(
				"+",
				MenuHelper.Theme.ButtonTheme,
				zoomInPos,
				new Vector2(thirdW, buttonHeight),
				_tableZoom < MaxZoom,
				false,
				false,
				MenuHelper.Theme.ButtonTheme.buttonCols,
				Anchor.TopLeft
			);

			bool fitWidthPressed = Seb.Vis.UI.UI.Button(
				"Fit",
				MenuHelper.Theme.ButtonTheme,
				fitWidthPos,
				new Vector2(thirdW, buttonHeight),
				true,
				false,
				false,
				MenuHelper.Theme.ButtonTheme.buttonCols,
				Anchor.TopLeft
			);

			bool zoomOutPressed = Seb.Vis.UI.UI.Button(
				"-",
				MenuHelper.Theme.ButtonTheme,
				zoomOutPos,
				new Vector2(thirdW, buttonHeight),
				_tableZoom > defaultZoom,
				false,
				false,
				MenuHelper.Theme.ButtonTheme.buttonCols,
				Anchor.TopLeft
			);

			// Handle zoom button actions
			if (zoomInPressed && _tableZoom < MaxZoom)
			{
				_tableZoom = Mathf.Min(_tableZoom + ZoomStep, MaxZoom);
				_bitGroupPositionsCalculated = false; // Recalculate positions with new zoom
			}
			if (zoomOutPressed && _tableZoom > MinZoom)
			{
				_tableZoom = Mathf.Max(_tableZoom - ZoomStep, MinZoom);
				_bitGroupPositionsCalculated = false; // Recalculate positions with new zoom
			}
			if (fitWidthPressed)
			{
				Debug.Log($"[LevelValidationPopup] Fit button pressed - Setting _tableZoom from {_tableZoom} to {defaultZoom}");
				_tableZoom = defaultZoom;
				_hScroll = 0;
				_bitGroupPositionsCalculated = false; // Recalculate positions with new zoom
			}

			// Buttons (moved down to make room for display mode and zoom buttons)
			Vector2 btnPos = displayModePos + Vector2.down * (buttonHeight + spacing) * 2; // Move down to make room

			bool levelPassed = _rows.Count > 0 && _rows.All(r => r.Passed);
			bool hasValidSelection = _selectedIndex >= 0 && _selectedIndex < _rows.Count;

			// Apply Test button
			bool applyTestPressed = Seb.Vis.UI.UI.Button(
				"Apply Test",
				MenuHelper.Theme.ButtonTheme,
				btnPos,
				new Vector2(contentWidth, buttonHeight),
				hasValidSelection,
				false,
				false,
				MenuHelper.Theme.ButtonTheme.buttonCols,
				Anchor.TopLeft
			);
			btnPos += Vector2.down * (buttonHeight + spacing);

			// Upload Score button
			bool uploadPressed = Seb.Vis.UI.UI.Button(
				"Upload Score",
				MenuHelper.Theme.ButtonTheme,
				btnPos,
				new Vector2(contentWidth, buttonHeight),
				levelPassed,
				false,
				false,
				MenuHelper.Theme.ButtonTheme.buttonCols,
				Anchor.TopLeft
			);
			btnPos += Vector2.down * (buttonHeight + spacing);

			// Leaderboard button
			bool leaderboardPressed = Seb.Vis.UI.UI.Button(
				"Leaderboard",
				MenuHelper.Theme.ButtonTheme,
				btnPos,
					new Vector2(contentWidth, buttonHeight),
				true,
				false,
				false,
				MenuHelper.Theme.ButtonTheme.buttonCols,
				Anchor.TopLeft
			);
			btnPos += Vector2.down * (buttonHeight + spacing);

			// Save as Chip button
			bool saveAsChipPressed = Seb.Vis.UI.UI.Button(
				"Save as Chip",
				MenuHelper.Theme.ButtonTheme,
				btnPos,
					new Vector2(contentWidth, buttonHeight),
				levelPassed,
				false,
				false,
				MenuHelper.Theme.ButtonTheme.buttonCols,
				Anchor.TopLeft
			);
			btnPos += Vector2.down * (buttonHeight + spacing);

			// Restart button
			bool restartPressed = Seb.Vis.UI.UI.Button(
				"Restart",
				MenuHelper.Theme.ButtonTheme,
				btnPos,
					new Vector2(contentWidth, buttonHeight),
				true,
				false,
				false,
				MenuHelper.Theme.ButtonTheme.buttonCols,
				Anchor.TopLeft
			);
			btnPos += Vector2.down * (buttonHeight + spacing);

			// Row: Levels | Next
			float halfWLevels = (contentWidth - spacing) * 0.5f;
			Vector2 levelsPos = btnPos;
			Vector2 nextPos = new Vector2(btnPos.x + halfWLevels + spacing, btnPos.y);

			bool levelsPressed = Seb.Vis.UI.UI.Button(
				"Levels",
				MenuHelper.Theme.ButtonTheme,
					levelsPos,
				new Vector2(halfWLevels, buttonHeight),
				true,
				false,
				false,
				MenuHelper.Theme.ButtonTheme.buttonCols,
				Anchor.TopLeft
			);

			bool nextPressed = Seb.Vis.UI.UI.Button(
				"Next",
				MenuHelper.Theme.ButtonTheme,
					nextPos,
				new Vector2(halfWLevels, buttonHeight),
				true,
				false,
				false,
				MenuHelper.Theme.ButtonTheme.buttonCols,
				Anchor.TopLeft
			);

			btnPos += Vector2.down * (buttonHeight + spacing);

			// Close button
			bool closePressed = Seb.Vis.UI.UI.Button(
				"Close",
				MenuHelper.Theme.ButtonTheme,
				btnPos,
					new Vector2(contentWidth, buttonHeight),
				true,
				false,
				false,
				MenuHelper.Theme.ButtonTheme.buttonCols,
				Anchor.TopLeft
			);

			// Handle button actions
			if (applyTestPressed && hasValidSelection) ApplySelectedTestInputs();
			if (uploadPressed && levelPassed) UserNameInputPopup.Open(OnUserNameConfirmed, OnUserNameCancelled);
			if (leaderboardPressed) LeaderboardPopup.Open(GetCurrentLevelId());
			if (saveAsChipPressed && levelPassed)
			{
				ChipSaveMenu.SetReturnMenu(UIDrawer.MenuType.LevelValidationResult);
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.ChipSave);
			}
			if (restartPressed) RestartCurrentLevel();
			if (levelsPressed) UIDrawer.SetActiveMenu(UIDrawer.MenuType.Levels);
			if (nextPressed) PlayNextLevel();
			if (closePressed) UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
		}

		// ---------- Helper Methods ----------
		static int GetNandGateCount()
		{
			var levelManager = LevelManager.Instance;
			if (levelManager?.Current == null) return 0;
			var adapter = new MobileSimulationAdapter();
			return adapter.CountNandGates();
		}

		static float CalculateContentWidthAtZoom(float zoom)
		{
			// This is a simplified calculation - we'll calculate the width based on the first row
			if (_rows.Count == 0) return 200f; // Default minimum width

			var firstRow = _rows[0];
			
			// Calculate text widths at the given zoom level
			float inTextWidth = Seb.Vis.UI.UI.CalculateTextSize(firstRow.Inputs, ActiveUITheme.FontSizeRegular * zoom, FontType.JetbrainsMonoRegular).x;
			float expTextWidth = Seb.Vis.UI.UI.CalculateTextSize(firstRow.Expected, ActiveUITheme.FontSizeRegular * zoom, FontType.JetbrainsMonoRegular).x;
			float outTextWidth = Seb.Vis.UI.UI.CalculateTextSize(firstRow.Got, ActiveUITheme.FontSizeRegular * zoom, FontType.JetbrainsMonoRegular).x;

			// Calculate total width including padding and dividers
			float cellPadding = 2f * zoom;
			float dividerWidth = 3f * zoom;
			float initialPadding = 1f * zoom;
			float finalPadding = cellPadding * 2;

			return initialPadding + cellPadding + inTextWidth + cellPadding + dividerWidth +
				   cellPadding/2 + expTextWidth + cellPadding + dividerWidth +
				   cellPadding/2 + outTextWidth + finalPadding;
		}

		static void DrawResultCheckmark(Vector2 center, float size, Color color)
		{
			// Draw as a solid green circle (simple and clear)
			Seb.Vis.UI.UI.DrawCircle(center, size, color, Anchor.Centre);
		}

		static void DrawResultCross(Vector2 center, float size, Color color)
		{
			// Draw as a solid red square (simple and clear)
			float squareSize = size * 1.4f;
			Bounds2D squareBounds = new Bounds2D(
				center - new Vector2(squareSize * 0.5f, squareSize * 0.5f),
				center + new Vector2(squareSize * 0.5f, squareSize * 0.5f)
			);
			Seb.Vis.UI.UI.DrawPanel(squareBounds, color);
		}

		static float DrawGraphicalBits(string bits, bool isInputColumn, float startX, float centerY, int column)
		{
			if (string.IsNullOrEmpty(bits)) return 0f;

			// Get current level to access bit counts
			var levelManager = LevelManager.Instance;
			var currentLevel = levelManager?.Current;
			if (currentLevel == null) return 0f;

			// Get bit counts for this column
			int[] bitCounts = isInputColumn ? currentLevel.inputBitCounts : currentLevel.outputBitCounts;
			if (bitCounts == null || bitCounts.Length == 0) return 0f;

			float x = startX;
			int bitOffset = 0;
			float totalWidth = 0f;

			for (int pinIndex = 0; pinIndex < bitCounts.Length; pinIndex++)
			{

				int pinBitCount = bitCounts[pinIndex];
				string pinBits;

				if (bitOffset + pinBitCount <= bits.Length)
				{
					pinBits = bits.Substring(bitOffset, pinBitCount);
				}
				else if (bitOffset < bits.Length)
				{
					string availableBits = bits.Substring(bitOffset);
					pinBits = availableBits.PadRight(pinBitCount, '0');
				}
				else
				{
					pinBits = new string('0', pinBitCount);
				}

				// Draw the bit group
				float groupWidth = DrawBitGroup(pinBits, x, centerY, column, pinIndex);
				if(pinBitCount == 1){
					x += groupWidth + 1f * _tableZoom; // Space between groups
					totalWidth += groupWidth + (pinIndex < bitCounts.Length - 1 ? 1f * _tableZoom : 0f);
				}else{
					x += groupWidth + 3f * _tableZoom; // Space between groups
					totalWidth += groupWidth + (pinIndex < bitCounts.Length - 1 ? 3f * _tableZoom : 0f);
				}
				bitOffset += pinBitCount;
			}

			return totalWidth;
		}

		static float DrawBitGroup(string bits, float x, float centerY, int column, int groupIndex)
		{

			float dotSize = 1.0f * _tableZoom; // Slightly larger for visibility
			if (bits.Length == 1)
			{
				// Draw 1-bit as a simple dot - make it more visible
				bool isHigh = bits[0] == '1';
				Color dotColor = DrawSettings.GetStateColour(isHigh, 0);
				
				x += dotSize*1.5f;
				// Use UI drawing function - draw a small square to simulate a dot
				Seb.Vis.UI.UI.DrawCircle(
					new Vector2(x, centerY),
					dotSize*1.08f,
					Color.black
				);
				Seb.Vis.UI.UI.DrawCircle(
					new Vector2(x, centerY),
					dotSize,
					dotColor
				);
				
				// Log position for header alignment if this is the first row
				if (column >= 0)
				{
					float midX = x; // Center is at x since we centered the dot
					switch (column)
					{
						case 0: _inputBitGroupXPositions.Add(midX); break;
						case 1: _expectedBitGroupXPositions.Add(midX); break;
						case 2: _outputBitGroupXPositions.Add(midX); break;
					}
				}
				
				return dotSize *2.5f;
			}
			else if (bits.Length == 8)
			{
				// Draw 8-bit as a 2x4 grid - make it more visible
				float gridSize = 5f * _tableZoom; // Slightly larger
				float squareSize = gridSize / 4f; // 2x4 grid, so 4 squares per row
				float squareSpacing = 0.12f * _tableZoom; // Smaller spacing for tighter grid

				x += gridSize*0.5f;
				
				// Draw black border background
				Seb.Vis.UI.UI.DrawPanel(
					new Bounds2D(
						new Vector2(x -gridSize*0.5f, centerY - (gridSize+squareSpacing) * 0.25f),
						new Vector2(x + gridSize*0.5f, centerY + (gridSize+squareSpacing) * 0.25f)
					),
					Color.black
				);
				
				// Draw individual bits as distinct squares
				for (int i = 0; i < 8; i++)
				{
					int row = i / 4;
					int col = i % 4;
					bool isHigh = bits[i] == '1'; 
					Color bitColor = DrawSettings.GetStateColour(isHigh, 0);
					
					// Calculate position for each square
					float squareX = x + squareSize * 0.5f + col * squareSize - gridSize *0.5f;
					float squareY = centerY + squareSize * 0.5f - row * squareSize;
					
					// Draw individual square with small gap
					Seb.Vis.UI.UI.DrawPanel(
						new Bounds2D(
							new Vector2(squareX - (squareSize - squareSpacing) * 0.5f, squareY - (squareSize - squareSpacing) * 0.5f),
							new Vector2(squareX + (squareSize - squareSpacing) * 0.5f, squareY + (squareSize - squareSpacing) * 0.5f)
						),
						bitColor
					);
				}
				
				// Log position for header alignment if this is the first row
				if (column >= 0)
				{
					float midX = x ;
					switch (column)
					{
						case 0: _inputBitGroupXPositions.Add(midX); break;
						case 1: _expectedBitGroupXPositions.Add(midX); break;
						case 2: _outputBitGroupXPositions.Add(midX); break;
					}
				}
				
				return gridSize;
			}
			else
			{
				// For other bit counts, draw as a simple horizontal line of dots
				float spacing = 0.5f * _tableZoom;
				float totalWidth = bits.Length * dotSize + (bits.Length - 1) * spacing;
				
				for (int i = 0; i < bits.Length; i++)
				{
					bool isHigh = bits[i] == '1';
					Color dotColor = DrawSettings.GetStateColour(isHigh, 0);
					
					float dotX = x + dotSize * 0.5f + i * (dotSize + spacing);
					
					// Use UI drawing for dots
					Seb.Vis.UI.UI.DrawCircle(
						new Vector2(dotX, centerY),
						dotSize,
						dotColor,
						Anchor.Centre
					);
				}
				
				// Log position for header alignment if this is the first row
				if (column >= 0)
				{
					float midX = x + totalWidth * 0.5f;
					switch (column)
					{
						case 0: _inputBitGroupXPositions.Add(midX); break;
						case 1: _expectedBitGroupXPositions.Add(midX); break;
						case 2: _outputBitGroupXPositions.Add(midX); break;
					}
				}
				
				return totalWidth;
			}
		}

		static void ApplySelectedTestInputs()
		{
			if (_selectedIndex < 0 || _selectedIndex >= _rows.Count) return;
			var selectedRow = _rows[_selectedIndex];
			try
			{
				var inputVector = BitVector.FromString(selectedRow.Inputs);
				var adapter = new MobileSimulationAdapter();
				adapter.ApplyInputs(inputVector);
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
			}
			catch (Exception ex)
			{
				Debug.LogError($"[LevelValidationPopup] Error applying test inputs: {ex.Message}");
			}
		}

		static void RestartCurrentLevel()
		{
			var levelManager = LevelManager.Instance;
			if (levelManager?.Current == null) return;
			var currentLevel = levelManager.Current;
			LevelProgressService.ClearLevelProgress(currentLevel.id);
			levelManager.StartLevel(currentLevel);
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
		}

		static string GetCurrentLevelId()
		{
			var levelManager = LevelManager.Instance;
			if (levelManager?.Current != null)
			{
				return levelManager.Current.id ?? levelManager.Current.name ?? "Unknown Level";
			}
			return "Unknown Level";
		}


		// Grid dimensions now use DLS.Game.GridHelper.GetStateGridDimension

		// ---------- Table Drawing Methods ----------

		static void DrawHeader(Vector2 headerTopLeft, float viewportWidth, float viewportHeight, float scrollBarWidth)
		{
			// Apply horizontal scroll offset
			Vector2 scrolledTopLeft = headerTopLeft + Vector2.left * _hScroll;

			// Calculate header height (two lines) - scaled by zoom
			float lineHeight = ActiveUITheme.FontSizeRegular * 1.2f * _tableZoom;
			float headerHeight = lineHeight * 2f + 4f * _tableZoom; // Two lines + padding

			// Draw header background (fixed position, clipped to viewport bounds)
			Draw.ID headerID = Seb.Vis.UI.UI.ReservePanel();
			Seb.Vis.UI.UI.ModifyPanel(
				headerID,
				new Bounds2D(headerTopLeft, headerTopLeft + new Vector2(viewportWidth, -headerHeight)),
				ColHelper.MakeCol(0.15f)
			);

			// Create clipping context for text to match panel bounds (using the same method as backup)
			using (Seb.Vis.UI.UI.CreateMaskScope(Bounds2D.CreateFromTopLeftAndSize(headerTopLeft, new Vector2(viewportWidth, headerHeight))))
			{

				DrawHeaderTableDivider(headerTopLeft, viewportWidth);

				// Line 1: Column titles (IN, EXPECTED, OUT)
				float y1 = headerTopLeft.y - lineHeight * 1.0f;
				float x = headerTopLeft.x + 1f * _tableZoom - _hScroll; // Apply horizontal scroll offset and zoom scaling

				// IN column title - scaled by zoom
				string inTitle = "IN";
				float inTitleX = x + _inColumnWidth * 0.5f;
				Seb.Vis.UI.UI.DrawText(inTitle, FontType.JetbrainsMonoRegular, ActiveUITheme.FontSizeRegular * 1.4f * _tableZoom, new Vector2(inTitleX, y1), Anchor.Centre, Color.white);
				x += _inColumnWidth + _dividerWidth * _tableZoom;

				// EXPECTED column title - scaled by zoom
				string expTitle = "EXPECTED";
				float expTitleX = x + _expectedColumnWidth * 0.5f;
				Seb.Vis.UI.UI.DrawText(expTitle, FontType.JetbrainsMonoRegular, ActiveUITheme.FontSizeRegular * 1.4f * _tableZoom, new Vector2(expTitleX, y1), Anchor.Centre, Color.white);
				x += _expectedColumnWidth + _dividerWidth * _tableZoom;

				// OUT column title - scaled by zoom
				string outTitle = "OUT";
				float outTitleX = x + _outColumnWidth * 0.5f;
				Seb.Vis.UI.UI.DrawText(outTitle, FontType.JetbrainsMonoRegular, ActiveUITheme.FontSizeRegular * 1.4f * _tableZoom, new Vector2(outTitleX, y1), Anchor.Centre, Color.white);
				x += _outColumnWidth + _dividerWidth * _tableZoom;

				// RESULT column title - scaled by zoom
				string resultTitle = "RESULT";
				float resultTitleX = x + _resultColumnWidth * 0.5f;
				Seb.Vis.UI.UI.DrawText(resultTitle, FontType.JetbrainsMonoRegular, ActiveUITheme.FontSizeRegular * 1.4f * _tableZoom, new Vector2(resultTitleX, y1), Anchor.Centre, Color.white);

				// Line 2: Pin labels and result stats
				float y2 = headerTopLeft.y - lineHeight * 2.5f;
				DrawPinLabelsInHeader(y2);
				DrawResultStats(y2, x + _resultColumnWidth * 0.5f);
			}
		}

		static void DrawResultStats(float y, float x)
		{
			// Calculate pass rate
			int totalTests = _rows.Count;
			int passedTests = _rows.Count(r => r.Passed);
			
			if (totalTests == 0) return;
			
			float passRate = (float)passedTests / totalTests;
			
			// Determine color based on pass rate
			Color statsColor;
			if (passRate >= 1.0f)
			{
				statsColor = Color.green;
			}
			else if (passRate > 0.75f)
			{
				statsColor = new Color(1.0f, 0.6f, 0.0f); // Orange
			}
			else if (passRate > 0.5f)
			{
				statsColor = Color.yellow;
			}
			else
			{
				statsColor = Color.red;
			}
			
			string statsText = $"({passedTests}/{totalTests})";
			Seb.Vis.UI.UI.DrawText(statsText, FontType.JetbrainsMonoRegular, ActiveUITheme.FontSizeRegular * _tableZoom, new Vector2(x, y), Anchor.Centre, statsColor);
		}

		static void DrawPinLabelsInHeader(float y)
		{
			// Get current level to access pin labels
			var levelManager = LevelManager.Instance;
			var currentLevel = levelManager?.Current;

			if (currentLevel == null) return;

			// Draw input pin labels using stored x-positions (adjusted for header position)
			if (currentLevel.inputPinLabels != null && currentLevel.inputPinLabels.Length > 0 && _inputBitGroupXPositions.Count > 0)
			{
				for (int i = 0; i < currentLevel.inputPinLabels.Length && i < _inputBitGroupXPositions.Count; i++)
				{
					int index = currentLevel.inputPinLabels.Length - 1 - i;
					string label = currentLevel.inputPinLabels[index].abbr; // Use abbreviation for header
					float labelX = _inputBitGroupXPositions[i]; // Adjust for header start position
					Seb.Vis.UI.UI.DrawText(label, FontType.JetbrainsMonoRegular, ActiveUITheme.FontSizeRegular * _tableZoom, new Vector2(labelX, y), Anchor.Centre, Color.white);
				}
			}

			// Draw expected pin labels using stored x-positions (adjusted for header position)
			if (currentLevel.outputPinLabels != null && currentLevel.outputPinLabels.Length > 0 && _expectedBitGroupXPositions.Count > 0)
			{
				for (int i = 0; i < currentLevel.outputPinLabels.Length && i < _expectedBitGroupXPositions.Count; i++)
				{
					int index = currentLevel.outputPinLabels.Length - 1 - i;
					string label = currentLevel.outputPinLabels[index].abbr; // Use abbreviation for header
					float labelX = _expectedBitGroupXPositions[i]; // Adjust for header start position
					Seb.Vis.UI.UI.DrawText(label, FontType.JetbrainsMonoRegular, ActiveUITheme.FontSizeRegular * _tableZoom, new Vector2(labelX, y), Anchor.Centre, Color.white);
				}
			}

			// Draw output pin labels using stored x-positions (adjusted for header position)
			if (currentLevel.outputPinLabels != null && currentLevel.outputPinLabels.Length > 0 && _outputBitGroupXPositions.Count > 0)
			{
				for (int i = 0; i < currentLevel.outputPinLabels.Length && i < _outputBitGroupXPositions.Count; i++)
				{

					int index = currentLevel.outputPinLabels.Length - 1 - i;
					string label = currentLevel.outputPinLabels[index].abbr; // Use abbreviation for header
					float labelX = _outputBitGroupXPositions[i]; // Adjust for header start position
					Seb.Vis.UI.UI.DrawText(label, FontType.JetbrainsMonoRegular, ActiveUITheme.FontSizeRegular * _tableZoom, new Vector2(labelX, y), Anchor.Centre, Color.white);
				}
			}
		}

		static string FormatBitsWithGrouping(string bits, bool isInputColumn, float start_x, int column)
		{
			if (string.IsNullOrEmpty(bits)) return "";

			// Get current level to access bit counts
			var levelManager = LevelManager.Instance;
			var currentLevel = levelManager?.Current;

			if (currentLevel == null) return bits; // Fallback to original bits

			// Get bit counts for this column
			int[] bitCounts = isInputColumn ? currentLevel.inputBitCounts : currentLevel.outputBitCounts;

			if (bitCounts == null || bitCounts.Length == 0) return bits; // Fallback to original bits

			// Split bits string into individual pin values and format with spacing
			var groups = new List<string>();
			int bitOffset = 0;
			float x = start_x;
			for (int pinIndex = 0; pinIndex < bitCounts.Length; pinIndex++)
			{
				int pinBitCount = bitCounts[pinIndex];
				string pinBits;

				if (bitOffset + pinBitCount <= bits.Length)
				{
					// Normal case: extract the expected number of bits
					pinBits = bits.Substring(bitOffset, pinBitCount);
				}
				else if (bitOffset < bits.Length)
				{
					// Partial case: take available bits and pad with zeros
					string availableBits = bits.Substring(bitOffset);
					pinBits = availableBits.PadRight(pinBitCount, '0');
				}
				else
				{
					// No bits available: pad with zeros
					pinBits = new string('0', pinBitCount);
				}
				float textWidth = Seb.Vis.UI.UI.CalculateTextSize(pinBits, ActiveUITheme.FontSizeRegular * _tableZoom, FontType.JetbrainsMonoRegular).x;
				switch (column)
				{
					case 0:
						_inputBitGroupXPositions.Add(x + textWidth * 0.5f);
						break;
					case 1:
						_expectedBitGroupXPositions.Add(x + textWidth * 0.5f);
						break;
					case 2:
						_outputBitGroupXPositions.Add(x + textWidth * 0.5f);
						break;
				}
				x += textWidth + Seb.Vis.UI.UI.CalculateTextSize("   ", ActiveUITheme.FontSizeRegular * _tableZoom, FontType.JetbrainsMonoRegular).x;
				groups.Add(pinBits);
				bitOffset += pinBitCount;
			}

			// Join groups with 3 spaces between them
			return string.Join("   ", groups);
		}

		static void DrawAllColumnDividers(Vector2 panelTopLeft, float viewportWidth, float viewportHeight)
		{
			// Apply horizontal scroll offset
			Vector2 scrolledTopLeft = panelTopLeft + Vector2.left * _hScroll;

			// Calculate divider positions - scaled by zoom
			float x1 = scrolledTopLeft.x + 1f * _tableZoom + 2f * _tableZoom + _inColumnWidth; // After IN column
			float x2 = scrolledTopLeft.x + 1f * _tableZoom + 2f * _tableZoom + _inColumnWidth + _dividerWidth * _tableZoom + _expectedColumnWidth; // After EXPECTED column
			float x3 = scrolledTopLeft.x + 1f * _tableZoom + 2f * _tableZoom + _inColumnWidth + _dividerWidth * _tableZoom + _expectedColumnWidth + _dividerWidth * _tableZoom + _outColumnWidth; // After OUT column

			float contentAreaHeight = viewportHeight - _headerHeight; // Height of scrollable content area
			float horizontalScrollbarHeight = DrawSettings.ActiveUITheme.ScrollTheme.scrollBarWidth;
			float totalHeight = _headerHeight + contentAreaHeight - horizontalScrollbarHeight; // Total height minus horizontal scrollbar

			#if UNITY_ANDROID || UNITY_IOS
			totalHeight*=1.05f;
			#endif
			// Draw first divider (between IN and EXPECTED)
			DrawColumnDivider(x1, scrolledTopLeft.y, totalHeight);
			// Draw second divider (between EXPECTED and OUT)
			DrawColumnDivider(x2, scrolledTopLeft.y, totalHeight);
			// Draw third divider (between OUT and RESULT)
			DrawColumnDivider(x3, scrolledTopLeft.y, totalHeight);
		}

		static void DrawColumnDivider(float x, float y, float height)
		{
			// Draw two-layer divider: reversed colors - scaled by zoom
			Color contentColor = ColHelper.MakeCol(0.12f); // Content background color (outer layer)
			Color panelColor = ColHelper.MakeCol(0.08f); // Panel background color (inner layer)

			float dividerWidth = _dividerWidth * _tableZoom;

			// Draw content layer (wider, outer)
			Draw.ID contentDividerID = Seb.Vis.UI.UI.ReservePanel();
			Seb.Vis.UI.UI.ModifyPanel(
				contentDividerID,
				new Bounds2D(new Vector2(x - dividerWidth * 0.5f, y - _headerHeight), new Vector2(x + dividerWidth * 0.5f, y - height)),
				contentColor
			);

			// Draw panel layer (narrower, inner, centered)
			Draw.ID panelDividerID = Seb.Vis.UI.UI.ReservePanel();
			Seb.Vis.UI.UI.ModifyPanel(
				panelDividerID,
				new Bounds2D(new Vector2(x - dividerWidth * 0.17f, y), new Vector2(x + dividerWidth * 0.17f, y - height)),
				panelColor
			);
		}

		static void DrawTableContent(Vector2 contentTopLeft, float width, int index, bool isLayoutPass)
		{
			if (index < 0 || index >= _rows.Count) return;

			if(index == 0)
            {
				_inputBitGroupXPositions.Clear();
				_expectedBitGroupXPositions.Clear();
				_outputBitGroupXPositions.Clear();
            }

			// Use full content width instead of viewport width for row drawing
			float rowWidth = Mathf.Max(_viewportWidth, _contentWidth); // Use the larger of viewport or content width

			// Bit group positions are calculated once for header alignment

			// Set column spacing - scaled by zoom
			_columnSpacing = RowHeight * 0.5f * _tableZoom;

			var row = _rows[index];
			bool selected = index == _selectedIndex;

			// Apply horizontal scroll offset
			Vector2 scrolledTopLeft = contentTopLeft + Vector2.left * _hScroll;

			// Make row clickable (using full content width)
			// Always call Button to register bounds, but only handle clicks during render pass
			bool pressed = Seb.Vis.UI.UI.Button(
				"", // Empty text, we're just using it for click detection
				MenuHelper.Theme.ButtonTheme,
				scrolledTopLeft,
				new Vector2(rowWidth, RowHeight * _tableZoom),
				true,
				false,
				false,
				MenuHelper.Theme.ButtonTheme.buttonCols,
				Anchor.TopLeft
			);

			if (!isLayoutPass && pressed)
			{
				_selectedIndex = index;
			}

			// Calculate column positions - scaled by zoom
			float x = scrolledTopLeft.x + 1f * _tableZoom; // Match header initial padding
			float centerY = scrolledTopLeft.y - RowHeight * _tableZoom * 0.5f;
			float cell_Padding = 2f * _tableZoom;


			//IN
			float x_start = x;
			x += cell_Padding/2;

			string inText = FormatBitsWithGrouping(row.Inputs, true,x,index == 0 && _displayMode == 0? 0 : -1); // <-- This logs x_values index==0, Dont do that for graphical
			float inWidth;
            if (_displayMode == 1) // Graphical representation
			{
				inWidth = DrawGraphicalBits(row.Inputs, true, x, centerY, index == 0 ? 0 : -1);
            }
            else
            {
				inWidth = Seb.Vis.UI.UI.CalculateTextSize(inText, ActiveUITheme.FontSizeRegular * _tableZoom, FontType.JetbrainsMonoRegular).x;
				Vector2 in_bit_graphics_pos = new Vector2(x + inWidth * 0.5f, centerY);
				Seb.Vis.UI.UI.DrawText(inText, FontType.JetbrainsMonoRegular, ActiveUITheme.FontSizeRegular * _tableZoom, in_bit_graphics_pos, Anchor.Centre, Color.white);
            }
			inWidth = MathF.Max(inWidth,2f*_tableZoom);
			x += inWidth;
			x += cell_Padding;

			_inColumnWidth = x - x_start;
			x += _dividerWidth * _tableZoom;

			//EXPECTED
			x_start = x;
			x += cell_Padding / 2;
			string expText = FormatBitsWithGrouping(row.Expected, false,x,index == 0&& _displayMode == 0 ? 1 : -1);
			float expTextWidth;
			if (_displayMode == 1) // Graphical representation
			{
				expTextWidth = DrawGraphicalBits(row.Expected, false, x, centerY, index == 0 ? 1 : -1);
			}
			else
			{
				expTextWidth = Seb.Vis.UI.UI.CalculateTextSize(expText, ActiveUITheme.FontSizeRegular * _tableZoom, FontType.JetbrainsMonoRegular).x;
				Vector2 exp_bit_graphics_pos = new Vector2(x + expTextWidth * 0.5f, centerY);
				Seb.Vis.UI.UI.DrawText(expText, FontType.JetbrainsMonoRegular, ActiveUITheme.FontSizeRegular * _tableZoom, exp_bit_graphics_pos, Anchor.Centre, Color.white);
			}

			expTextWidth = MathF.Max(expTextWidth,8f*_tableZoom);
			x += expTextWidth;
			x += cell_Padding;

			_expectedColumnWidth = x - x_start;
			x += _dividerWidth * _tableZoom;

			//OUT
			x_start = x;
			x += cell_Padding / 2;
			string outText = FormatBitsWithGrouping(row.Got, false,x,index == 0 && _displayMode == 0? 2 : -1);
			float outTextWidth;
			if (_displayMode == 1) // Graphical representation
			{
				outTextWidth = DrawGraphicalBits(row.Got, false, x, centerY, index == 0 ? 2 : -1);
			}
			else
			{
				outTextWidth = Seb.Vis.UI.UI.CalculateTextSize(outText, ActiveUITheme.FontSizeRegular * _tableZoom, FontType.JetbrainsMonoRegular).x;
				Vector2 out_bit_graphics_pos = new Vector2(x + outTextWidth * 0.5f, centerY);
				Seb.Vis.UI.UI.DrawText(outText, FontType.JetbrainsMonoRegular, ActiveUITheme.FontSizeRegular * _tableZoom, out_bit_graphics_pos, Anchor.Centre, Color.white);
			}
			outTextWidth = MathF.Max(outTextWidth,3f*_tableZoom);
			x += outTextWidth;
			x += cell_Padding;

			_outColumnWidth = x - x_start;
			x += _dividerWidth * _tableZoom;

			//RESULT
			x_start = x;
			x += cell_Padding / 2;
			
			// Draw checkmark or cross based on test result
			float resultIconSize = 2.0f * _tableZoom;
			Color resultColor = row.Passed ? Color.green : Color.red;
			Vector2 iconCenter = new Vector2(x + resultIconSize, centerY);
			
			if (row.Passed)
			{
				// Draw checkmark as filled circle
				DrawResultCheckmark(iconCenter, resultIconSize, resultColor);
			}
			else
			{
				// Draw cross as circle with X
				DrawResultCross(iconCenter, resultIconSize, resultColor);
			}
			
			x += resultIconSize * 2;
			x += cell_Padding / 2;

			// Ensure minimum width for "RESULT" text
			float minResultWidth = Seb.Vis.UI.UI.CalculateTextSize("RESULT", ActiveUITheme.FontSizeRegular * 1.4f * _tableZoom, FontType.JetbrainsMonoRegular).x + cell_Padding * 2;
			_resultColumnWidth = Mathf.Max(x - x_start, minResultWidth);
			
			// Update x to reflect actual column width (in case minResultWidth is larger)
			x = x_start + _resultColumnWidth;

			// Calculate total content width (horizontal scrollbar is created once in DrawLeftPanel) - scaled by zoom
			float totalContentWidth = x - scrolledTopLeft.x + cell_Padding * 2;
			#if UNITY_ANDROID || UNITY_IOS
			totalContentWidth += 3.0f;
			#endif
			_contentWidth = totalContentWidth;

            if (defaultZoomSet == false)
            {
            	// Normalize content width back to zoom 1.0, then calculate the zoom needed to fit
            	float contentWidthAtZoom1 = _contentWidth / _tableZoom;
            	defaultZoom = Mathf.Clamp(width / contentWidthAtZoom1, MinZoom, MaxZoom);
				defaultZoomSet = true;
				Debug.Log($"[LevelValidationPopup] Calculated defaultZoom: {defaultZoom} (width: {width}, contentWidth: {_contentWidth}, tableZoom: {_tableZoom}, normalized: {contentWidthAtZoom1})");
            }

		}

		// ---------- User Name Input Callbacks ----------
		static void OnUserNameConfirmed(string userName, bool shouldRemember, bool shareSolution)
		{
			// TODO: Implement upload logic
			Debug.Log($"[LevelValidationPopup] Upload confirmed: {userName}, share: {shareSolution}");
		}

		static void OnUserNameCancelled()
		{
			Debug.Log("[LevelValidationPopup] Upload cancelled");
		}

		static void PlayNextLevel()
		{
			var nextLevel = GetNextLevelDefinition();

			if (nextLevel == null)
			{
				// No next level - close popup and return to levels menu
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.Levels);
				return;
			}

			var levelManager = LevelManager.Instance;
			if (levelManager.IsActive && levelManager.HasUnsavedChanges())
			{
				// Show unsaved changes popup
				LevelUnsavedChangesPopup.OpenPopup((option) => HandleNextLevelAfterUnsavedCheck(option, nextLevel));
			}
			else
			{
				// Start the next level
				levelManager.StartLevel(nextLevel);
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
			}
		}

		static void HandleNextLevelAfterUnsavedCheck(int option, LevelDefinition nextLevel)
		{
			if (option == 0) // Cancel
			{
				return; // Do nothing, stay in current popup
			}
			else if (option == 1) // Save and continue
			{
				// Save current level progress
				var levelManager = LevelManager.Instance;
				if (levelManager != null)
				{
					levelManager.SaveCurrentProgress();
					Debug.Log("[LevelValidationPopup] Level progress saved successfully");
				}
				else
				{
					Debug.LogError("[LevelValidationPopup] LevelManager instance not found");
				}
				
				// Start the next level
				levelManager.StartLevel(nextLevel);
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
			}
			else if (option == 2) // Continue without saving
			{
				// Start the next level
				var levelManager = LevelManager.Instance;
				levelManager.StartLevel(nextLevel);
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
			}
		}

		static LevelDefinition GetNextLevelDefinition()
		{
			var levelManager = LevelManager.Instance;
			if (levelManager?.Current == null) return null;

			var currentLevel = levelManager.Current;

			try
			{
				// Load levels data
				var levelsText = Resources.Load<TextAsset>("levels");
				if (levelsText == null) return null;

				var levelsData = JsonUtility.FromJson<LevelsData>(levelsText.text);
				if (levelsData?.chapters == null) return null;

				// Find current level across all chapters
				foreach (var chapter in levelsData.chapters)
				{
					if (chapter.levels == null) continue;

					for (int i = 0; i < chapter.levels.Length; i++)
					{
						if (chapter.levels[i].id == currentLevel.id)
						{
							// Found current level, check if there's a next level in this chapter
							if (i < chapter.levels.Length - 1)
							{
								return chapter.levels[i + 1];
							}
							// If this is the last level in the chapter, look for next chapter
							else
							{
								// Find next chapter with levels
								int currentChapterIndex = Array.IndexOf(levelsData.chapters, chapter);
								for (int j = currentChapterIndex + 1; j < levelsData.chapters.Length; j++)
								{
									if (levelsData.chapters[j].levels != null && levelsData.chapters[j].levels.Length > 0)
									{
										return levelsData.chapters[j].levels[0]; // First level of next chapter
									}
								}
								return null; // No more levels
							}
						}
					}
				}

				return null; // Current level not found
			}
			catch (Exception ex)
			{
				Debug.LogError($"[LevelValidationPopup] Error getting next level: {ex.Message}");
				return null;
			}
		}
	}
}