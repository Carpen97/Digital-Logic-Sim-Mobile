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

		// ---------- Popup State ----------
		static string _title = "";
		static bool _isSuccess;
		static int _stars;
		static int _in_len;
		static int _out_len;
		static readonly List<TestRow> _rows = new();
		static int _selectedIndex = -1;
		static bool _isSequentialLevel = false;

		// ---------- Scrolling State ----------
		static float _hScroll = 0f;  // Horizontal scroll offset
		static float _vScroll = 0f;  // Vertical scroll offset
		static float _contentWidth = 0f;  // Total content width (for horizontal scrolling)
		static float _contentHeight = 0f; // Total content height (for vertical scrolling)
		static float _headerHeight = 0f;  // Header height (excluded from vertical scroll)

		// ---------- Column Layout ----------
		static float _inColumnWidth = 0f;
		static float _expectedColumnWidth = 0f;
		static float _outColumnWidth = 0f;
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
			float viewportWidth = panelSize.x - 2f; // Account for panel padding
			float viewportHeight = panelSize.y - 2f; // Account for panel padding

			// Calculate header height (two lines + padding)
			float lineHeight = ActiveUITheme.FontSizeRegular * 1.2f;
			_headerHeight = lineHeight * 2f + 4f; // Two lines + padding

			// Calculate content dimensions (placeholder values for now)
			_contentWidth = Mathf.Max(viewportWidth, 200f); // Minimum content width
			_contentHeight = _rows.Count * RowHeight; // Height based on number of rows

			// Clamp scroll values
			_hScroll = Mathf.Clamp(_hScroll, 0f, Mathf.Max(0f, _contentWidth - viewportWidth));
			float availableContentHeight = viewportHeight - _headerHeight - scrollBarWidth; // Account for horizontal scrollbar
			_vScroll = Mathf.Clamp(_vScroll, 0f, Mathf.Max(0f, _contentHeight - availableContentHeight));


			// Draw scrollable content area (below header, above horizontal scrollbar)
			Vector2 contentAreaTopLeft = panelTopLeft + Vector2.down * _headerHeight;
			float horizontalScrollbarHeight = scrollBarWidth;
			Vector2 contentAreaSize = new Vector2(viewportWidth, viewportHeight - _headerHeight - horizontalScrollbarHeight-0.3f);

			// Create scroll view for table content only
			ScrollViewTheme scrollTheme = DrawSettings.ActiveUITheme.ScrollTheme;
			scrollTheme.backgroundCol = ColHelper.MakeCol(0.12f);

			ScrollBarState sv = Seb.Vis.UI.UI.DrawScrollView(
				ID_LevelValidationPopup,
				contentAreaTopLeft,
				contentAreaSize,
				UILayoutHelper.DefaultSpacing,
				Anchor.TopLeft,
				scrollTheme,
				DrawTableContent,
				_rows.Count
			);
			
			// Draw header with horizontal scrolling (fixed vertical position)
			DrawHeader(panelTopLeft, viewportWidth);
			DrawHeaderTableDivider(panelTopLeft, viewportWidth);

			// Create horizontal scrollbar if content is wider than viewport
			Debug.Log($"[LevelValidationPopup] SCROLLBAR CHECK: Content width: {_contentWidth}, Viewport width: {viewportWidth}");
			Debug.Log($"[LevelValidationPopup] SCROLLBAR CHECK: Condition (_contentWidth > viewportWidth): {_contentWidth > viewportWidth}");
			if (_contentWidth > viewportWidth)
			{
				Debug.Log($"[LevelValidationPopup] Creating horizontal scrollbar - ENTERING SCROLLBAR CREATION");
				// Position scrollbar at bottom of panel (matching backup implementation)
				Bounds2D hScrollViewArea = Bounds2D.CreateFromTopLeftAndSize(panelTopLeft, new Vector2(viewportWidth, viewportHeight));
				Bounds2D hBarArea = Bounds2D.CreateFromTopLeftAndSize(
					new Vector2(panelTopLeft.x, panelTopLeft.y - viewportHeight + scrollBarWidth * 1.6f),
					new Vector2(viewportWidth, scrollBarWidth)
				);

				Debug.Log($"[LevelValidationPopup] Scrollbar areas - ViewArea: {hScrollViewArea}, BarArea: {hBarArea}");

				var hScrollState = Seb.Vis.UI.UI.DrawScrollbarHorizontal(
					hScrollViewArea,
					hBarArea,
					_contentWidth,
					scrollTheme,
					ID_LevelValidationPopup_H
				);

				_hScroll = Mathf.Clamp(hScrollState.scrollX, 0f, Mathf.Max(0f, _contentWidth - viewportWidth));
				Debug.Log($"[LevelValidationPopup] Scrollbar created successfully, _hScroll set to: {_hScroll}");
			}
			else
			{
				Debug.Log($"[LevelValidationPopup] No horizontal scrollbar needed - content fits in viewport");
			}

			// Draw column dividers last so they cover both header and content
			DrawAllColumnDividers(panelTopLeft, viewportWidth, viewportHeight - scrollBarWidth);
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

			// Buttons
			Vector2 btnPos = scorePos + new Vector2(0f, -RowHeight * 1.4f);

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
			float halfW = (contentWidth - spacing) * 0.5f;
			Vector2 levelsPos = btnPos;
			Vector2 nextPos = new Vector2(btnPos.x + halfW + spacing, btnPos.y);

			bool levelsPressed = Seb.Vis.UI.UI.Button(
				"Levels",
				MenuHelper.Theme.ButtonTheme,
					levelsPos,
				new Vector2(halfW, buttonHeight),
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
				new Vector2(halfW, buttonHeight),
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

		static void DrawHeader(Vector2 headerTopLeft, float viewportWidth)
		{
			// Apply horizontal scroll offset
			Vector2 scrolledTopLeft = headerTopLeft + Vector2.left * _hScroll;
			Debug.Log($"[LevelValidationPopup] DrawHeader: _hScroll={_hScroll}, headerTopLeft={headerTopLeft}, scrolledTopLeft={scrolledTopLeft}");

			// Calculate header height (two lines)
			float lineHeight = ActiveUITheme.FontSizeRegular * 1.2f;
			float headerHeight = lineHeight * 2f + 4f; // Two lines + padding

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
				// Line 1: Column titles (IN, EXPECTED, OUT)
				float y1 = headerTopLeft.y - lineHeight * 1.0f;
				float x = headerTopLeft.x + 1f - _hScroll; // Apply horizontal scroll offset (matching backup approach)

				// IN column title
				string inTitle = "IN";
				float inTitleX = x + _inColumnWidth * 0.5f;
				Seb.Vis.UI.UI.DrawText(inTitle, FontType.JetbrainsMonoRegular, ActiveUITheme.FontSizeRegular * 1.4f, new Vector2(inTitleX, y1), Anchor.Centre, Color.white);
				x += _inColumnWidth + _dividerWidth;

				// EXPECTED column title
				string expTitle = "EXPECTED";
				float expTitleX = x + _expectedColumnWidth * 0.5f;
				Seb.Vis.UI.UI.DrawText(expTitle, FontType.JetbrainsMonoRegular, ActiveUITheme.FontSizeRegular * 1.4f, new Vector2(expTitleX, y1), Anchor.Centre, Color.white);
				x += _expectedColumnWidth + _dividerWidth;

				// OUT column title
				string outTitle = "OUT";
				float outTitleX = x + _outColumnWidth * 0.5f;
				Seb.Vis.UI.UI.DrawText(outTitle, FontType.JetbrainsMonoRegular, ActiveUITheme.FontSizeRegular * 1.4f, new Vector2(outTitleX, y1), Anchor.Centre, Color.white);

				// Line 2: Pin labels (using stored x-positions for alignment)
				float y2 = headerTopLeft.y - lineHeight * 2.5f;
				DrawPinLabelsInHeader(headerTopLeft.x + 1f - _hScroll, y2);
			}
		}


		static void DrawPinLabelsInHeader(float startX, float y)
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
					string label = currentLevel.inputPinLabels[i].abbr; // Use abbreviation for header
					float labelX = _inputBitGroupXPositions[i]; // Adjust for header start position
					Seb.Vis.UI.UI.DrawText(label, FontType.JetbrainsMonoRegular, ActiveUITheme.FontSizeRegular, new Vector2(labelX, y), Anchor.Centre, Color.white);
				}
			}

			// Draw expected pin labels using stored x-positions (adjusted for header position)
			if (currentLevel.outputPinLabels != null && currentLevel.outputPinLabels.Length > 0 && _expectedBitGroupXPositions.Count > 0)
			{
				for (int i = 0; i < currentLevel.outputPinLabels.Length && i < _expectedBitGroupXPositions.Count; i++)
				{
					string label = currentLevel.outputPinLabels[i].abbr; // Use abbreviation for header
					float labelX = _expectedBitGroupXPositions[i]; // Adjust for header start position
					Seb.Vis.UI.UI.DrawText(label, FontType.JetbrainsMonoRegular, ActiveUITheme.FontSizeRegular, new Vector2(labelX, y), Anchor.Centre, Color.white);
				}
			}

			// Draw output pin labels using stored x-positions (adjusted for header position)
			if (currentLevel.outputPinLabels != null && currentLevel.outputPinLabels.Length > 0 && _outputBitGroupXPositions.Count > 0)
			{
				for (int i = 0; i < currentLevel.outputPinLabels.Length && i < _outputBitGroupXPositions.Count; i++)
				{
					string label = currentLevel.outputPinLabels[i].abbr; // Use abbreviation for header
					float labelX = _outputBitGroupXPositions[i]; // Adjust for header start position
					Seb.Vis.UI.UI.DrawText(label, FontType.JetbrainsMonoRegular, ActiveUITheme.FontSizeRegular, new Vector2(labelX, y), Anchor.Centre, Color.white);
				}
			}
		}

		static void DrawPinLabelsForRow(float y)
		{
			// Get current level to access pin labels
			var levelManager = LevelManager.Instance;
			var currentLevel = levelManager?.Current;

			if (currentLevel == null) return;

			// Draw input pin labels using stored x-positions
			if (currentLevel.inputPinLabels != null && currentLevel.inputPinLabels.Length > 0 && _inputBitGroupXPositions.Count > 0)
			{
				for (int i = 0; i < currentLevel.inputPinLabels.Length && i < _inputBitGroupXPositions.Count; i++)
				{
					string label = currentLevel.inputPinLabels[i].abbr; // Use abbreviation for header
					float labelX = _inputBitGroupXPositions[i]; // Use exact x-position from bit group
					Seb.Vis.UI.UI.DrawText(label, FontType.JetbrainsMonoRegular, ActiveUITheme.FontSizeRegular, new Vector2(labelX, y), Anchor.Centre, Color.white);
				}
			}

			// Draw expected pin labels using stored x-positions
			if (currentLevel.outputPinLabels != null && currentLevel.outputPinLabels.Length > 0 && _expectedBitGroupXPositions.Count > 0)
			{
				for (int i = 0; i < currentLevel.outputPinLabels.Length && i < _expectedBitGroupXPositions.Count; i++)
				{
					string label = currentLevel.outputPinLabels[i].abbr; // Use abbreviation for header
					float labelX = _expectedBitGroupXPositions[i]; // Use exact x-position from bit group
					Seb.Vis.UI.UI.DrawText(label, FontType.JetbrainsMonoRegular, ActiveUITheme.FontSizeRegular, new Vector2(labelX, y), Anchor.Centre, Color.white);
				}
			}

			// Draw output pin labels using stored x-positions
			if (currentLevel.outputPinLabels != null && currentLevel.outputPinLabels.Length > 0 && _outputBitGroupXPositions.Count > 0)
			{
				for (int i = 0; i < currentLevel.outputPinLabels.Length && i < _outputBitGroupXPositions.Count; i++)
				{
					string label = currentLevel.outputPinLabels[i].abbr; // Use abbreviation for header
					float labelX = _outputBitGroupXPositions[i]; // Use exact x-position from bit group
					Seb.Vis.UI.UI.DrawText(label, FontType.JetbrainsMonoRegular, ActiveUITheme.FontSizeRegular, new Vector2(labelX, y), Anchor.Centre, Color.white);
				}
			}
		}

		static void DrawPinLabels(float startX, float y)
		{
			// Get current level to access pin labels
			var levelManager = LevelManager.Instance;
			var currentLevel = levelManager?.Current;

			if (currentLevel == null) return;

			// Draw input pin labels using stored x-positions
			if (currentLevel.inputPinLabels != null && currentLevel.inputPinLabels.Length > 0 && _inputBitGroupXPositions.Count > 0)
			{
				for (int i = 0; i < currentLevel.inputPinLabels.Length && i < _inputBitGroupXPositions.Count; i++)
				{
					string label = currentLevel.inputPinLabels[i].abbr; // Use abbreviation for header
					float labelX = _inputBitGroupXPositions[i]; // Use exact x-position from bit group
					Seb.Vis.UI.UI.DrawText(label, FontType.JetbrainsMonoRegular, ActiveUITheme.FontSizeRegular, new Vector2(labelX, y), Anchor.Centre, Color.white);
				}
			}

			// Draw expected pin labels using stored x-positions
			if (currentLevel.outputPinLabels != null && currentLevel.outputPinLabels.Length > 0 && _expectedBitGroupXPositions.Count > 0)
			{
				for (int i = 0; i < currentLevel.outputPinLabels.Length && i < _expectedBitGroupXPositions.Count; i++)
				{
					string label = currentLevel.outputPinLabels[i].abbr; // Use abbreviation for header
					float labelX = _expectedBitGroupXPositions[i]; // Use exact x-position from bit group
					Seb.Vis.UI.UI.DrawText(label, FontType.JetbrainsMonoRegular, ActiveUITheme.FontSizeRegular, new Vector2(labelX, y), Anchor.Centre, Color.white);
				}
			}

			// Draw output pin labels using stored x-positions
			if (currentLevel.outputPinLabels != null && currentLevel.outputPinLabels.Length > 0 && _outputBitGroupXPositions.Count > 0)
			{
				for (int i = 0; i < currentLevel.outputPinLabels.Length && i < _outputBitGroupXPositions.Count; i++)
				{
					string label = currentLevel.outputPinLabels[i].abbr; // Use abbreviation for header
					float labelX = _outputBitGroupXPositions[i]; // Use exact x-position from bit group
					Seb.Vis.UI.UI.DrawText(label, FontType.JetbrainsMonoRegular, ActiveUITheme.FontSizeRegular, new Vector2(labelX, y), Anchor.Centre, Color.white);
				}
			}
		}

		static float CalculateGroupWidth(int bitCount)
		{
			// Calculate width of a single character in the font
			float charWidth = Seb.Vis.UI.UI.CalculateTextSize("0", ActiveUITheme.FontSizeRegular, FontType.JetbrainsMonoRegular).x;

			// Group width = number of bits * character width
			return bitCount * charWidth;
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
				float textWidth = Seb.Vis.UI.UI.CalculateTextSize(pinBits, ActiveUITheme.FontSizeRegular, FontType.JetbrainsMonoRegular).x;
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
				x += textWidth + Seb.Vis.UI.UI.CalculateTextSize("   ", ActiveUITheme.FontSizeRegular, FontType.JetbrainsMonoRegular).x;
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

			// Calculate divider positions
			float x1 = scrolledTopLeft.x + 2 + _inColumnWidth; // After IN column
			float x2 = scrolledTopLeft.x + 2 + _inColumnWidth + _dividerWidth + _expectedColumnWidth; // After EXPECTED column

			float contentAreaHeight = viewportHeight - _headerHeight; // Height of scrollable content area
			float horizontalScrollbarHeight = DrawSettings.ActiveUITheme.ScrollTheme.scrollBarWidth;
			float totalHeight = _headerHeight + contentAreaHeight - horizontalScrollbarHeight; // Total height minus horizontal scrollbar

			// Draw first divider (between IN and EXPECTED)
			DrawColumnDivider(x1, scrolledTopLeft.y, totalHeight);

			// Draw second divider (between EXPECTED and OUT)
			DrawColumnDivider(x2, scrolledTopLeft.y, totalHeight);
		}

		static void DrawColumnDivider(float x, float y, float height)
		{
			// Draw two-layer divider: reversed colors
			Color contentColor = ColHelper.MakeCol(0.12f); // Content background color (outer layer)
			Color panelColor = ColHelper.MakeCol(0.08f); // Panel background color (inner layer)

			// Draw content layer (wider, outer)
			Draw.ID contentDividerID = Seb.Vis.UI.UI.ReservePanel();
			Seb.Vis.UI.UI.ModifyPanel(
				contentDividerID,
				new Bounds2D(new Vector2(x - 1.5f, y - _headerHeight), new Vector2(x + 1.5f, y - height)),
				contentColor
			);

			// Draw panel layer (narrower, inner, centered)
			Draw.ID panelDividerID = Seb.Vis.UI.UI.ReservePanel();
			Seb.Vis.UI.UI.ModifyPanel(
				panelDividerID,
				new Bounds2D(new Vector2(x - 0.5f, y), new Vector2(x + 0.5f, y - height)),
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
			float rowWidth = _contentWidth;

			// Bit group positions are calculated once for header alignment

			// Set column spacing
			_columnSpacing = RowHeight * 0.5f;

			var row = _rows[index];
			bool selected = index == _selectedIndex;

			// Apply horizontal scroll offset
			Vector2 scrolledTopLeft = contentTopLeft + Vector2.left * _hScroll;

			// Draw row background (using full content width)
			Color rowColor = selected ? new Color(0.2f, 0.4f, 0.2f, 0.3f) : new Color(0.1f, 0.1f, 0.1f, 0.1f);
			Draw.ID rowID = Seb.Vis.UI.UI.ReservePanel();
			Seb.Vis.UI.UI.ModifyPanel(
				rowID,
				new Bounds2D(scrolledTopLeft, scrolledTopLeft + new Vector2(rowWidth, -RowHeight)),
				rowColor
			);

			// Calculate column positions
			float x = scrolledTopLeft.x;
			float centerY = scrolledTopLeft.y - RowHeight * 0.5f;
			float cell_Padding = 2f;


			//IN
			float x_start = x;
			x += cell_Padding;

			string inText = FormatBitsWithGrouping(row.Inputs, true,x,index == 0 ? 0 : -1);
			float inTextWidth = Seb.Vis.UI.UI.CalculateTextSize(inText, ActiveUITheme.FontSizeRegular, FontType.JetbrainsMonoRegular).x;
			Vector2 in_bit_graphics_pos = new Vector2(x + inTextWidth * 0.5f, centerY);
			Seb.Vis.UI.UI.DrawText(inText, FontType.JetbrainsMonoRegular, ActiveUITheme.FontSizeRegular, in_bit_graphics_pos, Anchor.Centre, Color.white);
			x += inTextWidth;
			x += cell_Padding;

			_inColumnWidth = x - x_start;
			x += _dividerWidth;

			//EXPECTED
			x_start = x;
			x += cell_Padding / 2;
			string expText = FormatBitsWithGrouping(row.Expected, false,x,index == 0 ? 1 : -1);
			float expTextWidth = Seb.Vis.UI.UI.CalculateTextSize(expText, ActiveUITheme.FontSizeRegular, FontType.JetbrainsMonoRegular).x;

			Vector2 exp_bit_graphics_pos = new Vector2(x + expTextWidth * 0.5f, centerY);
			Seb.Vis.UI.UI.DrawText(expText, FontType.JetbrainsMonoRegular, ActiveUITheme.FontSizeRegular, exp_bit_graphics_pos, Anchor.Centre, Color.white);
			x += expTextWidth;
			x += cell_Padding;

			_expectedColumnWidth = x - x_start;
			x += _dividerWidth;

			//OUT
			x_start = x;
			x += cell_Padding / 2;
			string outText = FormatBitsWithGrouping(row.Got, false,x,index == 0 ? 2 : -1);
			float outTextWidth = Seb.Vis.UI.UI.CalculateTextSize(outText, ActiveUITheme.FontSizeRegular, FontType.JetbrainsMonoRegular).x;

			Vector2 out_bit_graphics_pos = new Vector2(x + outTextWidth * 0.5f, centerY);
			Seb.Vis.UI.UI.DrawText(outText, FontType.JetbrainsMonoRegular, ActiveUITheme.FontSizeRegular, out_bit_graphics_pos, Anchor.Centre, Color.white);
			x += outTextWidth;

			_outColumnWidth = x - x_start;

			// Labels are drawn in header, not per row

			// Calculate total content width (horizontal scrollbar is created once in DrawLeftPanel)
			//float totalContentWidth = _inColumnWidth + _dividerWidth + _expectedColumnWidth + _dividerWidth + _outColumnWidth;
			float totalContentWidth = x - scrolledTopLeft.x + cell_Padding * 2;
			_contentWidth = totalContentWidth;
			Debug.Log($"[LevelValidationPopup] Row {index}: IN={_inColumnWidth}, EXP={_expectedColumnWidth}, OUT={_outColumnWidth}, Total={totalContentWidth}");

			// FOR TESTING: Force a very large content width to ensure scrollbar appears
			// _contentWidth = 1000f; // Much larger than any reasonable viewport
			// Debug.Log($"[LevelValidationPopup] TESTING: Forced content width to {_contentWidth}");
			// Debug.Log($"[LevelValidationPopup] TESTING: This should definitely trigger horizontal scrollbar!");

			// Make row clickable (using full content width)
			if (!isLayoutPass)
			{
				bool pressed = Seb.Vis.UI.UI.Button(
					"", // Empty text, we're just using it for click detection
					MenuHelper.Theme.ButtonTheme,
					scrolledTopLeft,
					new Vector2(rowWidth, RowHeight),
					true,
					false,
					false,
					MenuHelper.Theme.ButtonTheme.buttonCols,
					Anchor.TopLeft
				);

				if (pressed)
				{
					_selectedIndex = index;
				}
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
				// TODO: Implement save functionality
				Debug.Log("[LevelValidationPopup] Save functionality not implemented yet");
				return;
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