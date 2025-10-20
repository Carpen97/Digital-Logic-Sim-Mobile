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

// Data structures for levels.json parsing
#if false
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
#endif

namespace DLS.Graphics
{
	public static class LevelValidationPopupOLD
	{
		// ---------- Internal row model ----------
		struct TestRow
		{
			public string Inputs;
			public string Expected;
			public string Got;       // Only reliable for failures (when provided in message)
			public bool Passed;
			public string Message;   // Failure message (e.g., "Expected X, got Y")

			// New fields for sequential circuits
			public bool IsSequence;
			public string SequenceName;
			public List<SequenceStep> SequenceSteps;
		}
		struct SequenceStep
		{
			public string Inputs;
			public string Expected;
			public string Got;
			public bool Passed;
			public bool IsClockEdge;
		}

		// ---------- Popup state ----------
		static string _title = "";
		static bool _isSuccess;
		static int _stars;
		static int _in_len;
		static int _out_len;
		// Header bit labels (first chars)
		static string[] _inputLabelChars = Array.Empty<string>();
		static string[] _outputLabelChars = Array.Empty<string>();
		// Scroll list data
		static readonly List<TestRow> _rows = new();
		static int _selectedIndex = -1;

		// Level type detection
		static bool _isSequentialLevel = false;

		// UI constants (tweak to match your other menus)
		const float ListWidthFrac = 0.90f;  // Increased from 0.72f to 0.90f
		const float ListHeightFrac = 0.73f;
		const float RowHeight = 4.2f;
		const float OkBtnWidthFrac = 0.30f;
		const float OkBtnHeightMul = 1.5f;
        // Scale applied to the validation report (header + table) only
        static float ValidationScale = 1f;
        static float _lastViewportWidth;
        static float _minAllowedScale;
        const float MinZoomFloor = 0.25f;
        const float MaxValidationScale = 2.0f;
        public static void SetValidationScale(float scale)
        {
            float desired = Mathf.Clamp(scale, MinZoomFloor, MaxValidationScale);
            // Prevent zooming out beyond the point where content becomes narrower than the viewport
            if (_contentWidth > 0f && _lastViewportWidth > 0f)
            {
                // contentWidth scales ~ linearly with ValidationScale
                // Require: contentWidth' = _contentWidth * (desired/ValidationScale) >= _lastViewportWidth
                // => desired >= (_lastViewportWidth * ValidationScale) / _contentWidth
                float minScale = (_lastViewportWidth * ValidationScale) / _contentWidth;
                desired = Mathf.Max(desired, minScale * 0.995f); // small epsilon to avoid oscillation
                _minAllowedScale = minScale;
            }
            ValidationScale = Mathf.Clamp(desired, MinZoomFloor, MaxValidationScale);
        }
        // Tweakable: extra visible width to allow horizontal scroll to reach the very end
        const float HScrollRightPad = 2f;
        const float ColumnRightPadFactor = 0.35f; // in RowHeight units
		const float LayoutYOffset = 0f; // shift whole window upwards

		static float result_w;
		static float result_x;
		static float in_w;
		static float in_x;
		static float out_w;
		static float out_x;
		static float expected_w;
		static float expected_x;
		// Horizontal scrolling state and clipping viewport for left panel
		static float _hScroll = 0f;
		static float _contentWidth = 0f;
		static float _viewportLeft = 0f;
		static float _viewportRight = 0f;
		static float _columnSpacing = 0f;
		static SliderState _hSliderState;
		// Horizontal drag gesture state (mirrors vertical content drag pattern)
		static Vector2 _hDragStartMousePos;
		static float _hDragStartScroll;
		static bool _isHDragging;
		static bool _hDragExceededThreshold;
        static bool _autoFitWidthDone;

		static float GetHeaderBlockHeight()
		{
			// Ensure enough vertical space for both title and bit-label lines at any scale
			var themeLocal = DrawSettings.ActiveUITheme;
			float titleFontSize = themeLocal.FontSizeRegular * 2f * ValidationScale;
			float labelFontSize = themeLocal.FontSizeRegular * ValidationScale;
			float titleH = Seb.Vis.UI.UI.CalculateTextSize("M", titleFontSize, themeLocal.FontBold).y;
			float labelH = Seb.Vis.UI.UI.CalculateTextSize("M", labelFontSize, themeLocal.FontRegular).y;
			// Dynamic margins: smaller when zoomed out, larger when zoomed in
			float gapRows = Mathf.Lerp(0.10f, 0.25f, Mathf.Clamp01(ValidationScale));
			float topRows = Mathf.Lerp(0.10f, 0.22f, Mathf.Clamp01(ValidationScale));
			// Make bottom padding very small when zoomed out, larger when zoomed in
			float bottomRows = Mathf.Lerp(0.00f, 0.12f, Mathf.Clamp01(ValidationScale));
			float measured =
				RowHeight * (topRows + gapRows + bottomRows) * ValidationScale +
				titleH + labelH + 1f; // +1 for crisp line
			float minBlock = RowHeight * (2f + 0.15f) * ValidationScale + 1f; // fallback minimum
			return Mathf.Max(measured, minBlock);
		}

		static void GetHeaderVerticalMetrics(out float topMarginPx, out float gapPx)
		{
			// Match GetHeaderBlockHeight dynamic behavior
			float gapRows = Mathf.Lerp(0.30f, 0.45f, Mathf.Clamp01(ValidationScale));
			float topRows = Mathf.Lerp(0.20f, 0.42f, Mathf.Clamp01(ValidationScale));
			topMarginPx = RowHeight * topRows * ValidationScale;
			gapPx = RowHeight * gapRows * ValidationScale;
		}

		static float GetLeftPadding()
		{
			// Let base left padding scale linearly with zoom, matching dot pitch growth
			const float baseRows = 0.4f; // baseline at scale 1
			return RowHeight * baseRows * Mathf.Max(ValidationScale, 0f);
		}

		static float GetFirstColumnExtraPad()
		{
			// Extra gutter for the very first column so dots don't hug the panel edge
			// Scale with ValidationScale so padding grows as we zoom in, but never smaller than base
			const float baseRows = 0.10f; // tuned for max zoom-out visual
			float rows = baseRows * Mathf.Max(1f, ValidationScale);
			const float constantExtraRows = 0.08f; // slight constant extra across all zooms
			return RowHeight * (rows + constantExtraRows);
		}

		// Firebase test state
		static bool _isUploading = false;
		static string _uploadStatus = "";

		static readonly UIHandle ID_LevelValidationPopup = new("LevelValidationPopup_Scrollbar");
        static readonly UIHandle ID_LevelValidationPopup_H = new("LevelValidationPopup_HScroll");
		static readonly UIHandle ID_InfoPanelScrollView = new("InfoPanelScrollView");
		static readonly Seb.Vis.UI.UI.ScrollViewDrawElementFunc DrawRowFunc = DrawRow;
		static readonly Seb.Vis.UI.UI.ScrollViewDrawContentFunc DrawInfoPanelContentFunc = DrawInfoPanelContent;

		static bool isDraggingScrollbar;

		// ---------- Public API ----------
		public static void Open(ValidationReport report)
		{
			_rows.Clear();
			_selectedIndex = -1;
            _autoFitWidthDone = false;
			_in_len = report.AllTestResults[0].Inputs.Length;
			_out_len = report.AllTestResults[0].Expected.Length;

			// Prepare header labels from current project's dev pins
			ComputeHeaderLabels();

			// Detect if current level is sequential
			_isSequentialLevel = LevelManager.Instance?.Current?.isSequential ?? false;

			if (report == null)
			{
				_isSuccess = false;
				_stars = 0;
				_title = "Validation error";
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.LevelValidationResult);
				return;
			}

			_isSuccess = report.PassedAll;
			_stars = report.Stars;

			// Title - count sequences, not individual steps
			if (_isSuccess)
			{
				_title = "All tests passed";
			}
			else
			{
				if (_isSequentialLevel)
				{
					// For sequential levels, count unique sequences that failed
					int failedSequences = _rows.Count(r => r.IsSequence && !r.Passed);
					_title = failedSequences > 1 ? $"{failedSequences} tests failed" : "1 test failed";
				}
				else
				{
					// For combinational levels, count individual test vectors
					_title = report.Failures.Count > 1 ? $"{report.Failures.Count} tests failed" : "1 test failed";
				}
			}

			// Handle data population based on level type
			if (_isSequentialLevel)
			{
				// Sequential levels: Use AllTestResults and group by sequence name
				if (report.AllTestResults != null && report.AllTestResults.Count > 0)
				{
					// Group results by sequence name
					var sequenceGroups = report.AllTestResults.GroupBy(r => r.SequenceName);

					foreach (var group in sequenceGroups)
					{
						var sequenceSteps = new List<SequenceStep>();
						bool sequencePassed = true;

						foreach (var result in group)
						{
							sequenceSteps.Add(new SequenceStep
							{
								Inputs = result.Inputs,
								Expected = result.Expected,
								Got = result.Actual,
								Passed = result.Passed,
								IsClockEdge = result.IsClockEdge
							});

							if (!result.Passed)
								sequencePassed = false;
						}

						_rows.Add(new TestRow
						{
							Inputs = "", // Not used for sequences
							Expected = "", // Not used for sequences
							Got = "", // Not used for sequences
							Passed = sequencePassed,
							Message = sequencePassed ? "OK" : "Sequence failed",
							IsSequence = true,
							SequenceName = group.Key,
							SequenceSteps = sequenceSteps
						});
					}
				}
				else
				{
					// Fallback for sequential levels
					_rows.Add(new TestRow
					{
						Inputs = "-",
						Expected = "-",
						Got = "",
						Passed = false,
						Message = "No test results available"
					});
				}
			}
			else
			{
				// Combinational levels: Create individual test vector rows
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
							SequenceSteps = null
						});
					}
				}
				else
				{
					// Fallback for combinational levels
					_rows.Add(new TestRow
					{
						Inputs = "-",
						Expected = "-",
						Got = "",
						Passed = false,
						Message = "No test results available"
					});
				}
			}

			// If nothing was added, show an empty state row
			if (_rows.Count == 0)
			{
				_rows.Add(new TestRow
				{
					Inputs = "-",
					Expected = "-",
					Got = "",
					Passed = _isSuccess,
					Message = _isSuccess ? "No tests to show." : "No details available."
				});
			}

			UIDrawer.SetActiveMenu(UIDrawer.MenuType.LevelValidationResult);
		}

		public static void DrawMenu()
		{
			// Dimmed backdrop (same as other popups)
			MenuHelper.DrawBackgroundOverlay();

			using (Seb.Vis.UI.UI.BeginBoundsScope(true))
			{
				Draw.ID panelBG = Seb.Vis.UI.UI.ReservePanel();
				// top title/score moved into right sidebar

				// --- Layout based on level type ---
				var theme = DrawSettings.ActiveUITheme;
                Bounds2D overallBounds = Bounds2D.CreateEmpty();

				if (_isSequentialLevel)
				{
					// Sequential levels: Info Panel | Right Sidebar (with test selector wheel)
					// Sidebar is wider (0.30 instead of 0.26) to have more space for controls
					float panelSpacing = 2f;
					float totalWidth = Seb.Vis.UI.UI.Width * ListWidthFrac - panelSpacing;
					float sidebarW = totalWidth * 0.30f;
					float infoW = totalWidth - sidebarW;
					float panelH = Seb.Vis.UI.UI.Height * ListHeightFrac;

					Vector2 infoSize = new(infoW, panelH);
					Vector2 sidebarSize = new(sidebarW, panelH);

					Vector2 rowCentre = Seb.Vis.UI.UI.Centre + Vector2.up * LayoutYOffset;
					Vector2 infoPos = new Vector2(rowCentre.x - (sidebarW + panelSpacing) * 0.5f, rowCentre.y);
					Vector2 sidebarPos = new Vector2(rowCentre.x + (infoW + panelSpacing) * 0.5f, rowCentre.y);

					// Left: Info
					DrawInfoPanel(infoPos, infoSize);

					// Right: Sidebar (includes test selector wheel)
                    DrawRightSidebar(sidebarPos, sidebarSize);

                    overallBounds = Bounds2D.Grow(
                        Bounds2D.CreateFromCentreAndSize(infoPos, infoSize),
                        Bounds2D.CreateFromCentreAndSize(sidebarPos, sidebarSize)
                    );
				}
				else
				{
					// Combinational levels: Scroll Panel | Right Sidebar
					float panelSpacing = 2f;
					float totalWidth = Seb.Vis.UI.UI.Width * ListWidthFrac - panelSpacing;
					float sidebarW = totalWidth * 0.26f;
					float scrollW = totalWidth - sidebarW;
					float panelH = Seb.Vis.UI.UI.Height * ListHeightFrac; // taller: use full list height

					Vector2 scrollSize = new(scrollW, panelH);
					Vector2 sidebarSize = new(sidebarW, panelH);

					Vector2 rowCentre = Seb.Vis.UI.UI.Centre + Vector2.up * LayoutYOffset;
					Vector2 scrollPos = new Vector2(rowCentre.x - (sidebarW + panelSpacing) * 0.5f, rowCentre.y);
					Vector2 sidebarPos = new Vector2(rowCentre.x + (scrollW + panelSpacing) * 0.5f, rowCentre.y);

					DrawCombinationalPanel(scrollPos, scrollSize);
                    DrawRightSidebar(sidebarPos, sidebarSize);

                    overallBounds = Bounds2D.Grow(
                        Bounds2D.CreateFromCentreAndSize(scrollPos, scrollSize),
                        Bounds2D.CreateFromCentreAndSize(sidebarPos, sidebarSize)
                    );
				}

                // Panel BG spanning everything drawn in this scope: lock to fixed layout (scroll-insensitive)
                if (overallBounds.Width > 0 && overallBounds.Height > 0)
                    MenuHelper.DrawReservedMenuPanel(panelBG, overallBounds);
                else
				MenuHelper.DrawReservedMenuPanel(panelBG, Seb.Vis.UI.UI.GetCurrentBoundsScope());
			}
		}

		// ---------- Row drawing ----------
		static void DrawRow(Vector2 rowTopLeft, float width, int index, bool isLayoutPass)
		{
			if (index < 0 || index >= _rows.Count) return;

			var r = _rows[index];
			bool selected = index == _selectedIndex;

			if (_isSequentialLevel)
			{
				// Sequential levels: Use sequence-based drawing
				if (r.IsSequence)
				{
					// Draw sequence as multi-line entry
					DrawSequenceRow(rowTopLeft, width, r, selected, isLayoutPass);
				}
				else
				{
					// Draw regular combinational test row (fallback for sequential)
					DrawCombinationalRow(rowTopLeft, width, r, selected, isLayoutPass);
				}
			}
			else
			{
				// Combinational levels: Always use combinational row format
				DrawCombinationalRow(rowTopLeft, width, r, selected, isLayoutPass);
			}
		}

		static void DrawSequenceRow(Vector2 rowTopLeft, float width, TestRow r, bool selected, bool isLayoutPass)
		{
			// Create a compact summary of the sequence
			string status = r.Passed ? "<color=#44ff44> [PASS]" : "<color=#ff2222> [FAIL]";

			// Count passed/failed steps
			int passedSteps = r.SequenceSteps.Count(s => s.Passed);
			int totalSteps = r.SequenceSteps.Count;

			string label = $"{status} {r.SequenceName}";

			// Truncate if too long
			if (label.Length > 80)
			{
				label = label.Substring(0, 77) + "...";
			}

            float scaledRowHeightSeq = RowHeight * ValidationScale;
			bool pressed = Seb.Vis.UI.UI.Button(
				label,
				MenuHelper.Theme.ButtonTheme,
				rowTopLeft,
                new Vector2(width, scaledRowHeightSeq),
				/*enabled*/  true,
				/*fitTextX*/ true,
				/*fitTextY*/ false,
				MenuHelper.Theme.ButtonTheme.buttonCols,
				Anchor.TopLeft,
				leftAlignText: true,
				ignoreInputs: isDraggingScrollbar
			);

			if (!isLayoutPass && pressed)
			{
				_selectedIndex = Array.IndexOf(_rows.ToArray(), r);
				RememberSelection();
			}
		}

		static void DrawCombinationalHeader(Vector2 headerTopLeft, float viewportWidth)
		{
			// Header background spanning one row height
			Draw.ID headerID = Seb.Vis.UI.UI.ReservePanel();
			Seb.Vis.UI.UI.ModifyPanel(
				headerID,
				new Bounds2D(headerTopLeft, headerTopLeft + new Vector2(viewportWidth, -GetHeaderBlockHeight())),
				ColHelper.MakeCol(0.12f)
			);

			var theme = DrawSettings.ActiveUITheme;
			// Measure header labels precisely using the same font/size as drawn
			float headerFontSize = theme.FontSizeRegular * 2f * ValidationScale;
			// RESULT shows two lines: line1 SUCCESS/FAIL, line2 (x / y)
			int totalTests = _rows.Count;
			int passedTests = _rows.Count(r => r.Passed);
			string resultMain = (totalTests > 0 && passedTests == totalTests) ? "SUCCESS" : "FAIL";
			string resultSub = $"({passedTests} / {totalTests})";
			Vector2 resMainSize = Seb.Vis.UI.UI.CalculateTextSize(resultMain, headerFontSize, theme.FontBold);
			float resultSubFontSize = theme.FontSizeRegular * ValidationScale;
			Vector2 resSubSize = Seb.Vis.UI.UI.CalculateTextSize(resultSub, resultSubFontSize, theme.FontRegular);
			float resWHeader = Mathf.Max(resMainSize.x, resSubSize.x);
			Vector2 inSize = Seb.Vis.UI.UI.CalculateTextSize("IN", headerFontSize, theme.FontBold);
			Vector2 outSize = Seb.Vis.UI.UI.CalculateTextSize("OUT", headerFontSize, theme.FontBold);
			Vector2 expSize = Seb.Vis.UI.UI.CalculateTextSize("EXPECTED", headerFontSize, theme.FontBold);

            float spacing = (1+RowHeight) * 0.75f * ValidationScale;
			_columnSpacing = spacing;
			float x = headerTopLeft.x + 1f - _hScroll; // apply horizontal scroll offset
            // Place title and labels using measured text heights so both lines fit at any scale
            float headerBlockHeightLocal = GetHeaderBlockHeight();
            float titleH = Seb.Vis.UI.UI.CalculateTextSize("M", headerFontSize, theme.FontBold).y;
            float labelH = Seb.Vis.UI.UI.CalculateTextSize("M", theme.FontSizeRegular * ValidationScale, theme.FontRegular).y;
            GetHeaderVerticalMetrics(out float topMargin, out float gap);
            // Title baseline from the top
            float labelY = headerTopLeft.y - topMargin - titleH * 0.5f;
            // Bit labels below with explicit gap
            float subLabelY = labelY - (titleH * 0.5f) - gap - (labelH * 0.5f);

		// Column widths: ensure they can host headers and the dot cells
		// Reduce result column width to avoid overlap with vertical scrollbar
		result_w = Mathf.Max(resWHeader + 2f, RowHeight * 2.6f * ValidationScale);
            // Compute dot pitch and scale-aware paddings up front
            float dotStep = RowHeight * ValidationScale;
            float dotRadiusHeader = RowHeight * 0.35f * ValidationScale;
            float leftPad = GetLeftPadding();
            float firstColPad = Mathf.Max(leftPad + GetFirstColumnExtraPad(), dotRadiusHeader + RowHeight * 0.05f * ValidationScale);
            float rightPadIn = GetLeftPadding() + RowHeight * ColumnRightPadFactor * ValidationScale;
            float rightPadOut = rightPadIn;
            float rightPadExp = GetLeftPadding(); // symmetric for EXPECTED (no right divider)
            // Content widths must include both paddings to guarantee space for all bits
            float contentInW = Mathf.Max(_in_len, 2) * dotStep + firstColPad + rightPadIn;
            float contentOutW = Mathf.Max(_out_len, 2) * dotStep + leftPad + rightPadOut;
            float contentExpW = Mathf.Max(_out_len, 2) * dotStep + leftPad + rightPadExp;
            in_w = Mathf.Max(inSize.x + 4f * ValidationScale, contentInW);
            expected_w = Mathf.Max(expSize.x + 4f * ValidationScale, contentExpW);
            // OUT column width must be at least as wide as EXPECTED
            out_w = Mathf.Max(outSize.x + 4f * ValidationScale, contentOutW);
            out_w = Mathf.Max(out_w, expected_w);

            // Positions (absolute coords) used by row rendering
            // IN (first column)
            in_x = x;
            float inCentreX = in_x + in_w * 0.5f + (firstColPad - rightPadIn) * 0.5f;
			Seb.Vis.UI.UI.DrawText(
                "IN",
                theme.FontBold,
                headerFontSize,
                new Vector2(inCentreX , labelY),
				Anchor.TextCentre,
				Color.white
			);
            // Second line: input bit initials (align with dots; use same left padding as dots)
            DrawBitLabelRow(_inputLabelChars, in_x, in_w, subLabelY, theme, firstColPad, false);
			x += in_w + spacing;

            // EXPECTED (second)
			expected_x = x;
            float expCentreX = expected_x + expected_w * 0.5f + (leftPad - rightPadExp) * 0.5f;
			Seb.Vis.UI.UI.DrawText(
                "EXPECTED",
                theme.FontBold,
                headerFontSize,
                new Vector2(expCentreX, labelY),
				Anchor.TextCentre,
				Color.white
			);	
            // Second line mirrors OUT labels; align with same padding as dots
            DrawBitLabelRow(_outputLabelChars, expected_x, expected_w, subLabelY, theme, leftPad, false);
            x += expected_w + spacing;

            // OUT (third)
            out_x = x;
            float outCentreX = out_x + out_w * 0.5f + (leftPad - rightPadOut) * 0.5f;
			Seb.Vis.UI.UI.DrawText(
				"OUT",
                theme.FontBold,
                headerFontSize,
                new Vector2(outCentreX, labelY),
				Anchor.TextCentre,
				Color.white
			);
            // Second line: output bit initials (align with dots)
            DrawBitLabelRow(_outputLabelChars, out_x, out_w, subLabelY, theme, leftPad, false);
            x += out_w + spacing;

            // RESULT (last)
            result_x = x;
            float resultCentreX = result_x + result_w * 0.5f; // no dots here; keep true column centre
            // Color by success ratio (shared across both lines)
            float ratio = (totalTests > 0) ? (passedTests / (float)totalTests) : 0f;
            Color resultColor;
            if (ratio >= 1f)
                resultColor = ColHelper.MakeCol255(68, 170, 110); // green
            else if (ratio >= 0.75f)
                resultColor = ColHelper.MakeCol255(245, 212, 67); // yellow
            else if (ratio >= 0.5f)
                resultColor = ColHelper.MakeCol255(230, 150, 50); // orange
            else
                resultColor = ColHelper.MakeCol255(190, 60, 60); // red

            // Line 1: SUCCESS/FAIL
			Seb.Vis.UI.UI.DrawText(
                resultMain,
                theme.FontBold,
                headerFontSize,
                new Vector2(resultCentreX, labelY),
				Anchor.TextCentre,
                resultColor
            );
            // Line 2: (x / y)
            Seb.Vis.UI.UI.DrawText(
                resultSub,
                theme.FontRegular,
                resultSubFontSize,
                new Vector2(resultCentreX, subLabelY),
                Anchor.TextCentre,
                resultColor
            );

            // Update content width for scrolling (add small right pad to ensure last columns become visible)
            _contentWidth = (result_x + result_w) - (headerTopLeft.x + 1f - _hScroll) + HScrollRightPad;
		}

        static void DrawCombinationalRow(Vector2 rowTopLeft, float width, TestRow r, bool selected, bool isLayoutPass)
		{
			// Background selectable strip across full row width
			StateCols cols = new StateCols(
				selected ? ColHelper.MakeCol255(100, 200, 0, 40) : ColHelper.MakeCol255(200, 200, 200, 20),
				ColHelper.MakeCol255(200, 200, 200, 50),
				ColHelper.MakeCol255(200, 200, 200, 100),
				ColHelper.MakeCol255(200, 200, 200, 150)
			);
            float scaledRowHeight = RowHeight * ValidationScale;
            float w = width - (RowHeight * 0.1f * ValidationScale);
			bool pressed = Seb.Vis.UI.UI.Button(
				string.Empty,
				MenuHelper.Theme.ButtonTheme,
				rowTopLeft,
                new Vector2(w, scaledRowHeight),
				true,
				false,
				false,
				cols,
				Anchor.TopLeft,
				leftAlignText: true,
				ignoreInputs: isDraggingScrollbar
			);

			if (!isLayoutPass && pressed)
			{
				_selectedIndex = Array.IndexOf(_rows.ToArray(), r);
				RememberSelection();
			}

			// Vertical alignment baseline for dots/circles
            float centreY = rowTopLeft.y - scaledRowHeight * 0.55f;
            float dotRadius = RowHeight * 0.35f * ValidationScale;
            float cellPadding = RowHeight * 0.05f * ValidationScale;
            float columnRightPad = RowHeight * ColumnRightPadFactor * ValidationScale;

			// RESULT indicator: use same glyphs as sequence rows (✓ for pass, ✗ for fail)
			Color resultCol = r.Passed ? ColHelper.MakeCol255(68, 170, 110) : ColHelper.MakeCol255(190, 60, 60);
			string resultGlyph = r.Passed ? "✓" : "✗";
			float glyphSize = ActiveUITheme.FontSizeRegular * 2.2f * ValidationScale;
			Seb.Vis.UI.UI.DrawText(resultGlyph, ActiveUITheme.FontBold, glyphSize, new Vector2(result_x + result_w * 0.4f, centreY), Anchor.Centre, resultCol);
			//Seb.Vis.UI.UI.DrawText(" ", new Vector2(result_x + result_w * 0.5f, centreY), dotRadius, resultCol, Anchor.Centre);

            // IN bits (extra first-column padding)
            DrawBitsInCell(r.Inputs, in_x, centreY, in_w, Mathf.Max(GetLeftPadding() + GetFirstColumnExtraPad(), dotRadius + RowHeight * 0.05f * ValidationScale), GetLeftPadding() + columnRightPad, dotRadius);
            // EXPECTED bits (second column)
            DrawBitsInCell(r.Expected, expected_x, centreY, expected_w, GetLeftPadding(), GetLeftPadding(), dotRadius);
            // OUT bits (third column)
            DrawBitsInCell(r.Got, out_x, centreY, out_w, GetLeftPadding(), GetLeftPadding() + columnRightPad, dotRadius);
		}

        static void DrawBitsInCell(string bits, float cellX, float centreY, float cellW, float leftPadding, float rightPadding, float dotRadius)
		{
			if (string.IsNullOrEmpty(bits)) return;

            float x = cellX + leftPadding;
            float xMax = cellX + cellW - rightPadding;
            float step = RowHeight * ValidationScale; // dot pitch

			for (int i = 0; i < bits.Length; i++)
			{
				if (x + dotRadius > xMax) break; // stop if would overflow the cell
				// Clip to viewport horizontally
				if ((x + dotRadius) < _viewportLeft)
				{
					x += step;
					continue;
				}
				if ((x - dotRadius) > _viewportRight)
				{
					break;
				}
                char bit = bits[i];
                Color bitColor;
                if (bit == '1')
                    bitColor = ActiveTheme.StateHighCol[0];
                else
                    bitColor = ActiveTheme.StateLowCol[0];

                // Use text glyph for correct vertical spacing that matches labels and prior visuals
                Seb.Vis.UI.UI.DrawText("●", FontType.JetbrainsMonoRegular,ActiveUITheme.FontSizeRegular*2.7f * ValidationScale, new Vector2(x, centreY), Anchor.Centre, bitColor);
				x += step;
			}
		}

        static void DrawBitLabelRow(string[] labels, float cellX, float cellW, float y, DrawSettings.UIThemeDLS theme, float leftPadding, bool centreAcrossColumn)
		{
			if (labels == null || labels.Length == 0) return;
			int count = labels.Length;
			if (count <= 0) return;

            float step = RowHeight * ValidationScale; // match dot pitch to dot spacing
            float startX;
            if (centreAcrossColumn)
            {
                float totalWidth = (count - 1) * step;
                startX = cellX + (cellW - totalWidth) * 0.5f; // centre across column
            }
            else
            {
                startX = cellX + leftPadding; // align to left padding used by dots
            }

			for (int i = 0; i < count; i++)
			{
				float x = startX + i * step;
				// Reverse order so labels align with bit order as drawn
				string ch = labels[count - 1 - i];
				if (x < _viewportLeft || x > _viewportRight) { continue; }
                Seb.Vis.UI.UI.DrawText(ch, theme.FontRegular, theme.FontSizeRegular * ValidationScale, new Vector2(x, y), Anchor.Centre, ColHelper.MakeCol255(220,220,220));
			}
		}

	static void ComputeHeaderLabels()
	{
		try
		{
			var proj = Project.ActiveProject;
			var dev = proj?.ViewedChip;
			_inputLabelChars = Array.Empty<string>();
			_outputLabelChars = Array.Empty<string>();
			if (dev == null) return;

			// Try to get labels from current level definition first (new enhanced system)
			var levelManager = LevelManager.Instance;
			var currentLevel = levelManager?.Current;
			
			if (currentLevel != null)
			{
				// Use new PinLabel system if available - use abbreviation only for header
				if (currentLevel.inputPinLabels != null && currentLevel.inputPinLabels.Length > 0)
				{
					_inputLabelChars = currentLevel.inputPinLabels.Select(l => {
						// Use abbr if available, otherwise take first 3 chars of name
						string abbr = l.abbr ?? l.name?.Substring(0, Mathf.Min(3, l.name.Length)) ?? "";
						return abbr;
					}).ToArray();
				}
				else if (currentLevel.inputLabels != null && currentLevel.inputLabels.Count > 0)
				{
					// Fallback to old string list system
					_inputLabelChars = currentLevel.inputLabels.Select(s => string.IsNullOrEmpty(s) ? "" : s.Substring(0, Mathf.Min(3, s.Length))).ToArray();
				}
				
				if (currentLevel.outputPinLabels != null && currentLevel.outputPinLabels.Length > 0)
				{
					_outputLabelChars = currentLevel.outputPinLabels.Select(l => {
						// Use abbr if available, otherwise take first 3 chars of name
						string abbr = l.abbr ?? l.name?.Substring(0, Mathf.Min(3, l.name.Length)) ?? "";
						return abbr;
					}).ToArray();
				}
				else if (currentLevel.outputLabels != null && currentLevel.outputLabels.Count > 0)
				{
					// Fallback to old string list system
					_outputLabelChars = currentLevel.outputLabels.Select(s => string.IsNullOrEmpty(s) ? "" : s.Substring(0, Mathf.Min(3, s.Length))).ToArray();
				}
			}
			
			// Fallback: use dev chip pin names if level labels not available
			if (_inputLabelChars.Length == 0 || _outputLabelChars.Length == 0)
			{
				var ins = dev.GetInputPins();
				var outs = dev.GetOutputPinsAsArray();
				if (_inputLabelChars.Length == 0 && ins != null && ins.Length > 0)
					_inputLabelChars = ins.Select(p => string.IsNullOrEmpty(p.Name) ? "" : p.Name.Substring(0, Mathf.Min(3, p.Name.Length))).ToArray();
				if (_outputLabelChars.Length == 0 && outs != null && outs.Length > 0)
					_outputLabelChars = outs.Select(p => string.IsNullOrEmpty(p.Name) ? "" : p.Name.Substring(0, Mathf.Min(3, p.Name.Length))).ToArray();
			}
		}
		catch { /* best effort only */ }
	}

		/// <summary>
		/// Calculates the visible length of a string, ignoring HTML color tags
		/// </summary>
		static int GetVisibleLength(string text)
		{
			if (string.IsNullOrEmpty(text)) return 0;

			// Remove HTML color tags to get actual visible length
			string cleanText = System.Text.RegularExpressions.Regex.Replace(text, @"<color=#[^>]*>|</color>", "");
			return cleanText.Length;
		}

	/// <summary>
	/// Converts a binary string (e.g., "1010") to colored dot symbols using the game's state colors
	/// </summary>
	static string BinaryToColoredDots(string binary)
	{
		if (string.IsNullOrEmpty(binary)) return "-";

		// Use the game's state colors - index 0 is red for high, dark red for low
		string result = "";
		foreach (char bit in binary)
		{
			if (bit == '1')
			{
				// High state - use red color (index 0 from StateHighCol)
				result += "<color=#f24d4f>●</color>";
			}
			else if (bit == '0')
			{
				// Low state - use dark red color (index 0 from StateLowCol)  
				result += "<color=#331a1a>●</color>";
			}
			else
			{
				// Non-binary character, keep as-is
				result += bit;
			}
		}
		return result;
	}

	/// <summary>
	/// Converts a binary string to colored dots with spaces between them for better alignment
	/// </summary>
	static string BinaryToColoredDotsWithSpacing(string binary)
	{
		if (string.IsNullOrEmpty(binary)) return "-";

		string result = "";
		for (int i = 0; i < binary.Length; i++)
		{
			char bit = binary[i];
			if (bit == '1')
			{
				result += "<color=#f24d4f>●</color>";
			}
			else if (bit == '0')
			{
				result += "<color=#331a1a>●</color>";
			}
			else
			{
				result += bit;
			}
			
			// Add space between dots (but not after the last one)
			if (i < binary.Length - 1)
			{
				result += " ";
			}
		}
		return result;
	}

		// ---------- Helpers ----------
		static void RememberSelection()
		{
			// Hook for future behavior (e.g., jump to a waveform, auto-zoom, etc.)
			// Currently no-op, but keeping it mirrors other menus and avoids null refs.
		}
		static Bounds2D prev;

		static void DrawCombinationalPanel(Vector2 panelPos, Vector2 panelSize)
		{
			// Draw panel background
			Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();
			Seb.Vis.UI.UI.ModifyPanel(
				panelID,
				new Bounds2D(panelPos - panelSize * 0.5f, panelPos + panelSize * 0.5f),
				ColHelper.MakeCol(0.08f)
			);

			// Position calculations
			Vector2 panelTopLeft = panelPos - new Vector2(panelSize.x * 0.5f, -panelSize.y * 0.5f);
			Vector2 headerPos = panelTopLeft + new Vector2(1f, -1f);

			// Horizontal scroll bar for whole header+table area
			float hBarHeight = 1.2f;
            float verticalScrollbarWidth = DrawSettings.ActiveUITheme.ScrollTheme.scrollBarWidth;
            // Inset by an extra pixel on the right so the scroll track doesn't merge with the outer border
            float viewportWidth = panelSize.x - verticalScrollbarWidth; // 1px left, 1px right, +1px extra right inset
            _lastViewportWidth = viewportWidth;
			_viewportLeft = headerPos.x;
			_viewportRight = headerPos.x + viewportWidth;

			// Clamp horizontal scroll if content width is known
			float maxScrollH = Mathf.Max(0f, _contentWidth - viewportWidth);
			_hScroll = Mathf.Clamp(_hScroll, 0f, maxScrollH);

            // Draw header with current scroll, clipped to viewport (scale-aware)
            float headerBlockHeight = GetHeaderBlockHeight();
			using (Seb.Vis.UI.UI.CreateMaskScope(Bounds2D.CreateFromTopLeftAndSize(headerPos, new Vector2(viewportWidth, headerBlockHeight))) )
			{
				DrawCombinationalHeader(headerPos, viewportWidth);
			}

            // After header computed _contentWidth, auto-fit width on first frame to max-zoom-out (fit-to-width)
            if (!_autoFitWidthDone && _contentWidth > 0)
            {
                float ratio = viewportWidth / _contentWidth;
                if (ratio < 1f)
                {
                    float targetScale = Mathf.Max(0.25f, ValidationScale * (ratio * 0.995f));
                    if (targetScale < ValidationScale)
                    {
                        ValidationScale = targetScale;
                        _hScroll = 0f;
                        _autoFitWidthDone = true;
                        return; // redraw next frame with new scale
                    }
                }
                else
                {
                    // Content already fits; set min zoom so user can't zoom out beyond this
                    _minAllowedScale = ValidationScale;
                }
                _autoFitWidthDone = true;
            }

			// Draw scrollable rows area (below header)
            Vector2 contentTopLeft = headerPos + Vector2.down * headerBlockHeight;
            // Rows area height accounts for scaled header height and bottom bar
            Vector2 contentSize = new(viewportWidth, Mathf.Max(0.5f, panelSize.y - headerBlockHeight - hBarHeight - 2f));
			ScrollViewTheme scrollViewTheme = DrawSettings.ActiveUITheme.ScrollTheme;
			scrollViewTheme.backgroundCol = ColHelper.MakeCol(0.12f);
			ScrollBarState sv = Seb.Vis.UI.UI.DrawScrollView(
				ID_LevelValidationPopup,
				contentTopLeft,
				contentSize,
                UILayoutHelper.DefaultSpacing * ValidationScale,
				Anchor.TopLeft,
				scrollViewTheme,
				DrawRowFunc,
				_rows.Count
			);

			prev = Seb.Vis.UI.UI.PrevBounds;
			// Clip vertical column barriers to content viewport so they don't leak out
            using (Seb.Vis.UI.UI.CreateMaskScope(Bounds2D.CreateFromTopLeftAndSize(contentTopLeft, contentSize)))
			{
				// Note: positions already include horizontal scroll offset from header computation
				DrawColumnBarrier(in_x, in_w, panelTopLeft, panelSize);
                DrawColumnBarrier(expected_x, expected_w, panelTopLeft, panelSize);
                DrawColumnBarrier(out_x, out_w, panelTopLeft, panelSize);
			}

            // Horizontal divider between header and table (scale-aware)
            float boundaryY = panelTopLeft.y - headerBlockHeight;
            // Adaptive divider thickness: thinner when zoomed out, thicker when zoomed in
            float barThickness = Mathf.Lerp(0.3f, 1.4f, Mathf.Clamp01(ValidationScale));
            float barTop = boundaryY - barThickness * 0.5f;
            float barBottom = barTop - barThickness;
            Seb.Vis.UI.UI.DrawPanel(
                new Bounds2D(
                    new Vector2(panelTopLeft.x + 1f, barTop),
                    new Vector2(panelTopLeft.x + panelSize.x - 1f, barBottom)
                ),
                ColHelper.MakeCol(0.08f)
            );

			// Horizontal scrollbar + content drag using UI helpers
			if (_contentWidth > viewportWidth)
			{
				Bounds2D hScrollViewArea = Bounds2D.CreateFromTopLeftAndSize(headerPos, new Vector2(viewportWidth, panelSize.y));
				Bounds2D hBarArea = Bounds2D.CreateFromTopLeftAndSize(new Vector2(headerPos.x, panelTopLeft.y - panelSize.y + hBarHeight * 1.6f), new Vector2(viewportWidth, hBarHeight));
                var hState = Seb.Vis.UI.UI.DrawScrollbarHorizontal(hScrollViewArea, hBarArea, _contentWidth, scrollViewTheme, ID_LevelValidationPopup_H);
				_hScroll = Mathf.Clamp(hState.scrollX, 0f, Mathf.Max(0f, _contentWidth - viewportWidth));
			}

			isDraggingScrollbar = sv.isDragging;
		}

		static void DrawRightSidebar(Vector2 panelPos, Vector2 panelSize)
		{
			// Sidebar background
			Draw.ID panelID = Seb.Vis.UI.UI.ReservePanel();
			Bounds2D sidebarBounds = new Bounds2D(panelPos - panelSize * 0.5f, panelPos + panelSize * 0.5f);
			Seb.Vis.UI.UI.ModifyPanel(panelID, sidebarBounds, ColHelper.MakeCol(0.08f));

			Vector2 topLeft = sidebarBounds.TopLeft + new Vector2(1f, -1f);
			float contentWidth = panelSize.x - 2f;

			// Message and score
			Seb.Vis.UI.UI.DrawText(_title, ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular, topLeft, Anchor.TopLeft, Color.white);
			Vector2 scorePos = topLeft + new Vector2(0f, -RowHeight * 1.1f);
			int nandCount = GetNandGateCount();
			// Draw score text
			//MenuHelper.DrawText($"Score: {nandCount}", scorePos, Anchor.TopLeft, ColHelper.MakeCol255(245, 212, 67), bold: true);
			Seb.Vis.UI.UI.DrawText($"Score: {nandCount}", ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular, scorePos, Anchor.TopLeft, Color.yellow);
			// Info button aligned to score baseline, just to its right
			Vector2 scoreSize = Seb.Vis.UI.UI.CalculateTextSize($"Score: {nandCount}", ActiveUITheme.FontSizeRegular, ActiveUITheme.FontBold);
			Vector2 infoBtnPos = new Vector2(scorePos.x + scoreSize.x + 1.8f, scorePos.y + (ActiveUITheme.FontSizeRegular * 0.3f));
			bool infoButtonPressed = Seb.Vis.UI.UI.Button(
				"info",
				MenuHelper.Theme.ButtonTheme,
				infoBtnPos,
				new Vector2(ButtonHeight * 2.2f, ButtonHeight * 1.2f),
				true,
				false,
				false,
				MenuHelper.Theme.ButtonTheme.buttonCols,
				Anchor.TopLeft
			);
			if (infoButtonPressed) ScoreExplanationPopup.Open();

			// Buttons
			float buttonWidth = contentWidth;
			float buttonHeight = ButtonHeight * 1.0f;
			float spacing = 1.2f;

			// Start position - for sequential, we have selector wheel but no zoom buttons, so same offset as combinational
			float initialOffset = -RowHeight * 1.4f;
			Vector2 nextRowPos = scorePos + new Vector2(0f, initialOffset);

			// For sequential levels: Add test selector wheel above zoom buttons
			if (_isSequentialLevel && _rows.Count > 0)
			{
				// Test selector wheel (left arrow, text, right arrow)
				float arrowBtnW = buttonHeight * 1.2f;
				float textW = buttonWidth - 2f * arrowBtnW - spacing * 2f;
				
				Vector2 leftArrowPos = nextRowPos;
				Vector2 textPos = new Vector2(nextRowPos.x + arrowBtnW + spacing, nextRowPos.y);
				Vector2 rightArrowPos = new Vector2(nextRowPos.x + arrowBtnW + spacing + textW + spacing, nextRowPos.y);

				bool leftPressed = Seb.Vis.UI.UI.Button(
					"<",
					MenuHelper.Theme.ButtonTheme,
					leftArrowPos,
					new Vector2(arrowBtnW, buttonHeight),
					_selectedIndex > 0,
					false,
					false,
					MenuHelper.Theme.ButtonTheme.buttonCols,
					Anchor.TopLeft
				);

				// Display current test name
				string testName = _selectedIndex >= 0 && _selectedIndex < _rows.Count 
					? _rows[_selectedIndex].SequenceName 
					: (_rows.Count > 0 ? _rows[0].SequenceName : "");
				
				// Truncate if too long
				const int maxChars = 20;
				if (testName.Length > maxChars)
					testName = testName.Substring(0, maxChars - 3) + "...";

				Seb.Vis.UI.UI.DrawPanel(
					Bounds2D.CreateFromTopLeftAndSize(textPos, new Vector2(textW, buttonHeight)),
					ColHelper.MakeCol(0.12f)
				);
				Seb.Vis.UI.UI.DrawText(
					testName,
					ActiveUITheme.FontRegular,
					ActiveUITheme.FontSizeRegular,
					textPos + new Vector2(textW * 0.5f, -buttonHeight * 0.5f),
					Anchor.Centre,
					Color.white
				);

				bool rightPressed = Seb.Vis.UI.UI.Button(
					">",
					MenuHelper.Theme.ButtonTheme,
					rightArrowPos,
					new Vector2(arrowBtnW, buttonHeight),
					_selectedIndex < _rows.Count - 1,
					false,
					false,
					MenuHelper.Theme.ButtonTheme.buttonCols,
					Anchor.TopLeft
				);

				if (leftPressed && _selectedIndex > 0)
				{
					_selectedIndex--;
					RememberSelection();
				}
				if (rightPressed && _selectedIndex < _rows.Count - 1)
				{
					_selectedIndex++;
					RememberSelection();
				}

				// Ensure a test is selected by default
				if (_selectedIndex < 0 && _rows.Count > 0)
				{
					_selectedIndex = 0;
					RememberSelection();
				}

				nextRowPos += Vector2.down * (buttonHeight + spacing);
			}

            // Row: scale controls (-  +) sharing the same row (only for combinational levels)
			if (!_isSequentialLevel)
			{
                float halfW = (buttonWidth - spacing) * 0.5f;
				Vector2 rowY = nextRowPos;
				Vector2 minusPos = rowY;
				Vector2 plusPos = new Vector2(rowY.x + halfW + spacing, rowY.y);

                bool minusPressed = Seb.Vis.UI.UI.Button(
					"-",
					MenuHelper.Theme.ButtonTheme,
					minusPos,
                    new Vector2(halfW, buttonHeight),
                    ValidationScale > MathF.Max(_minAllowedScale * 0.999f, MinZoomFloor),
                    false,
                    false,
					MenuHelper.Theme.ButtonTheme.buttonCols,
					Anchor.TopLeft
				);
                bool plusPressed = Seb.Vis.UI.UI.Button(
					"+",
					MenuHelper.Theme.ButtonTheme,
					plusPos,
                    new Vector2(halfW, buttonHeight),
                    ValidationScale < MaxValidationScale * 0.999f,
                    false,
                    false,
					MenuHelper.Theme.ButtonTheme.buttonCols,
					Anchor.TopLeft
				);
                if (minusPressed) SetValidationScale(ValidationScale - 0.1f);
                if (plusPressed) SetValidationScale(ValidationScale + 0.1f);

				nextRowPos += Vector2.down * (buttonHeight + spacing);
			}

			// For sequential, start right after selector wheel; for combinational, after zoom buttons
			Vector2 btnPos = nextRowPos;

			bool levelPassed = _rows.Count > 0 && _rows.All(r => r.Passed);
			bool hasValidSelection = _selectedIndex >= 0 && _selectedIndex < _rows.Count;
			bool canApplyTest = hasValidSelection && !_isSequentialLevel; // Only for combinational

			bool applyTestPressed = Seb.Vis.UI.UI.Button(
				"Apply Test",
				MenuHelper.Theme.ButtonTheme,
				btnPos,
				new Vector2(buttonWidth, buttonHeight),
				canApplyTest,
				false,
				false,
				MenuHelper.Theme.ButtonTheme.buttonCols,
				Anchor.TopLeft
			);
			btnPos += Vector2.down * (buttonHeight + spacing);

			string uploadButtonText = _isUploading ? _uploadStatus : "Upload Score";
			bool uploadPressed = Seb.Vis.UI.UI.Button(
				uploadButtonText,
				MenuHelper.Theme.ButtonTheme,
				btnPos,
				new Vector2(buttonWidth, buttonHeight),
				!_isUploading && levelPassed,
				false,
				false,
				MenuHelper.Theme.ButtonTheme.buttonCols,
				Anchor.TopLeft
			);
		btnPos += Vector2.down * (buttonHeight + spacing);

		bool leaderboardPressed = Seb.Vis.UI.UI.Button(
			"Leaderboard",
			MenuHelper.Theme.ButtonTheme,
			btnPos,
			new Vector2(buttonWidth, buttonHeight),
			true,
			false,
			false,
			MenuHelper.Theme.ButtonTheme.buttonCols,
			Anchor.TopLeft
		);
		btnPos += Vector2.down * (buttonHeight + spacing);

		bool saveAsChipPressed = Seb.Vis.UI.UI.Button(
			"Save as Chip",
			MenuHelper.Theme.ButtonTheme,
			btnPos,
			new Vector2(buttonWidth, buttonHeight),
			levelPassed,
			false,
			false,
			MenuHelper.Theme.ButtonTheme.buttonCols,
			Anchor.TopLeft
		);
		btnPos += Vector2.down * (buttonHeight + spacing);

		bool restartPressed = Seb.Vis.UI.UI.Button(
			"Restart",
			MenuHelper.Theme.ButtonTheme,
			btnPos,
			new Vector2(buttonWidth, buttonHeight),
			true,
			false,
			false,
			MenuHelper.Theme.ButtonTheme.buttonCols,
			Anchor.TopLeft
		);
		btnPos += Vector2.down * (buttonHeight + spacing);

		// Row: Levels | Next on the same row
		{
			float halfW = (buttonWidth - spacing) * 0.5f;
			Vector2 levelsPosSameRow = btnPos;
			Vector2 nextPosSameRow = new Vector2(btnPos.x + halfW + spacing, btnPos.y);

			bool levelsPressed = Seb.Vis.UI.UI.Button(
				"Levels",
				MenuHelper.Theme.ButtonTheme,
				levelsPosSameRow,
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
				nextPosSameRow,
				new Vector2(halfW, buttonHeight),
				true,
				false,
				false,
				MenuHelper.Theme.ButtonTheme.buttonCols,
				Anchor.TopLeft
			);

			// Handle actions for this row
		if (nextPressed) PlayNextLevel();
		if (levelsPressed)
		{
			// Just open the levels menu - unsaved changes check will happen when user actually selects a new level
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.Levels);
		}

			btnPos += Vector2.down * (buttonHeight + spacing);
		}

		bool closePressed = Seb.Vis.UI.UI.Button(
			"Close",
			MenuHelper.Theme.ButtonTheme,
			btnPos,
			new Vector2(buttonWidth, buttonHeight),
			true,
			false,
			false,
			MenuHelper.Theme.ButtonTheme.buttonCols,
			Anchor.TopLeft
		);

			// Actions
			if (applyTestPressed && canApplyTest) ApplySelectedTestInputs();
			if (uploadPressed && levelPassed && !_isUploading) UserNameInputPopup.Open(OnUserNameConfirmed, OnUserNameCancelled);
			if (saveAsChipPressed && levelPassed)
			{
				ChipSaveMenu.SetReturnMenu(UIDrawer.MenuType.LevelValidationResult);
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.ChipSave);
			}
			if (restartPressed)
		{
			DeleteConfirmationPopup.OpenPopup(
				"Are you sure you want to restart the current level? All progress will be lost.",
				Color.white,
				(confirmed) => { if (confirmed) RestartCurrentLevel(); }
			);
		}
			if (leaderboardPressed)
			{
				string levelId = GetCurrentLevelId();
				LeaderboardPopup.Open(levelId);
			}
			if (closePressed) UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
		}
		
		static void DrawColumnBarrier(float x, float w, Vector2 panelTopLeft, Vector2 panelSize)
        {
			Seb.Vis.UI.UI.DrawPanel(
				new Bounds2D(
					new Vector2(x + w - 1.5f, panelTopLeft.y - 1),
					new Vector2(x + w + 1.5f, panelTopLeft.y - panelSize.y + 1)
				),
				ColHelper.MakeCol(0.12f)
			);
			Seb.Vis.UI.UI.DrawPanel(
				new Bounds2D(
					new Vector2(x + w - 0.5f, panelTopLeft.y - 1),
					new Vector2(x + w + 0.5f, panelTopLeft.y - panelSize.y + 1)
				),
				ColHelper.MakeCol(0.08f)
			);
        }
	

		static void DrawInfoPanel(Vector2 panelPos, Vector2 panelSize)
		{
			// Draw info panel background
			Draw.ID infoPanelID = Seb.Vis.UI.UI.ReservePanel();
			Seb.Vis.UI.UI.ModifyPanel(
				infoPanelID,
				new Bounds2D(panelPos - panelSize * 0.5f, panelPos + panelSize * 0.5f),
				ColHelper.MakeCol(0.08f)
			);

			// Create scrollable content area with small margin from panel edges
			float margin = 1f;
			Vector2 contentPos = panelPos - panelSize * 0.5f + new Vector2(margin, panelSize.y - margin);
			Vector2 contentSize = panelSize - new Vector2(margin * 2, margin * 2);

			// Draw scrollable content using DrawScrollView with custom black background
			var theme = DrawSettings.ActiveUITheme;
			var customScrollTheme = theme.ScrollTheme;
			customScrollTheme.backgroundCol = Color.black; // Pure black background

			ScrollBarState scrollState = Seb.Vis.UI.UI.DrawScrollView(
				ID_InfoPanelScrollView,
				contentPos,
				contentSize,
				Anchor.TopLeft,
				customScrollTheme,
				DrawInfoPanelContentFunc
			);
		}

		static void DrawInfoPanelContent(Vector2 topLeft, float width, bool isLayoutPass)
		{
			// Calculate content height dynamically based on actual content
			float contentHeight = CalculateContentHeight();

			if (!isLayoutPass)
			{
				if (_selectedIndex >= 0 && _selectedIndex < _rows.Count)
				{
					// Show selected test details
					var r = _rows[_selectedIndex];
					var details = new StringBuilder();

					// Calculate cell width based on content
					// Base width on "Expected" header (8 chars) + 1 space padding on each side
					int baseCellWidth = 10; // "Expected" length + 2 spaces
					// Add space for each bit: each bit is 1 char + 1 space (except last)
					int inputBits = _in_len;
					int outputBits = _out_len;
					int maxBits = Math.Max(inputBits, outputBits);
					
					// Cell width = base width or (number of bits * 2 - 1) + 2 for padding, whichever is larger
					int contentWidth = maxBits * 2 - 1 + 2; // dots with spaces between + padding
					int cellWidth = Math.Max(baseCellWidth, contentWidth);
					
					// First header line with column titles (reordered: IN, EXPECTED, OUT)
					string inputHeader = " Input".PadRight(cellWidth);
					string expectedHeader = " Expected".PadRight(cellWidth);
					string outputHeader = " Output".PadRight(cellWidth);
					details.AppendLine($" Step |{inputHeader}|{expectedHeader}|{outputHeader}");
					
					// Second header line with bit labels (add spaces between letters for alignment with dots)
					string inputLabels = " " + string.Join(" ", _inputLabelChars.Reverse());
					string outputLabels = " " + string.Join(" ", _outputLabelChars.Reverse());
					string paddedInputLabels = inputLabels.PadRight(cellWidth);
					string paddedExpectedLabels = outputLabels.PadRight(cellWidth);
					string paddedOutputLabels = outputLabels.PadRight(cellWidth);
					details.AppendLine($"      |{paddedInputLabels}|{paddedExpectedLabels}|{paddedOutputLabels}");
					
					int stepCount = 0;
					foreach (var step in r.SequenceSteps)
					{
						string stepStatus = step.Passed ? "<color=#44ff44>✓</color>" : "<color=#ff2222>✗</color>";
						string clockIndicator = step.IsClockEdge ? " [CLK]" : "";

						// Convert binary strings to colored dots with spaces between them (reordered: IN, EXPECTED, OUT)
						string inputs = string.IsNullOrEmpty(step.Inputs) ? " -" : " " + BinaryToColoredDotsWithSpacing(step.Inputs);
						string expected = string.IsNullOrEmpty(step.Expected) ? " -" : " " + BinaryToColoredDotsWithSpacing(step.Expected);
						string got = string.IsNullOrEmpty(step.Got) ? " -" : " " + BinaryToColoredDotsWithSpacing(step.Got);

						// Apply dynamic cell width padding using PadRight for consistency (reordered: IN, EXPECTED, OUT)
						// Note: PadRight doesn't work with colored tags, so we need to manually pad
						int inputsVisLen = GetVisibleLength(inputs);
						int expectedVisLen = GetVisibleLength(expected);
						int gotVisLen = GetVisibleLength(got);
						
						string paddedInputs = inputs + new string(' ', Math.Max(0, cellWidth - inputsVisLen));
						string paddedExpected = expected + new string(' ', Math.Max(0, cellWidth - expectedVisLen));
						string paddedGot = got + " " +stepStatus + new string(' ', Math.Max(0, cellWidth - gotVisLen - 2));
						
						string stepText = $"{stepCount++}";
						stepText = stepText.PadLeft(5) + " "; // Left-pad to 5 chars, then add 1 space on right
						details.AppendLine($"{stepText}|{paddedInputs}|{paddedExpected}|{paddedGot}");
					}

					// Draw text directly without background (panel already has background)
					MenuHelper.DrawText(details.ToString(), topLeft, Anchor.TopLeft, Color.white, bold: false);
				}
				else
				{
					// Show default message when no test is selected
					string defaultMessage = "Select a test from the\nlist to view details...";

					// Draw text directly without background (panel already has background)
					MenuHelper.DrawText(defaultMessage, topLeft, Anchor.TopLeft, Color.gray, bold: false);
				}
			}
			else
			{
				// Layout pass - draw invisible content with calculated height
				if (_selectedIndex >= 0 && _selectedIndex < _rows.Count)
				{
					var r = _rows[_selectedIndex];
					var details = new StringBuilder();

					// Calculate cell width based on content (same as render pass)
					int baseCellWidth = 10;
					int inputBits = _in_len;
					int outputBits = _out_len;
					int maxBits = Math.Max(inputBits, outputBits);
					int contentWidth = maxBits * 2 - 1 + 2;
					int cellWidth = Math.Max(baseCellWidth, contentWidth);
					
					// First header line (reordered: IN, EXPECTED, OUT)
					string inputHeader = " Input".PadRight(cellWidth);
					string expectedHeader = " Expected".PadRight(cellWidth);
					string outputHeader = " Output".PadRight(cellWidth);
					details.AppendLine($" Step |{inputHeader}|{expectedHeader}|{outputHeader}");
					
					// Second header line with bit labels (add spaces between letters)
					string inputLabels = " " + string.Join(" ", _inputLabelChars.Reverse());
					string outputLabels = " " + string.Join(" ", _outputLabelChars.Reverse());
					string paddedInputLabels = inputLabels.PadRight(cellWidth);
					string paddedExpectedLabels = outputLabels.PadRight(cellWidth);
					string paddedOutputLabels = outputLabels.PadRight(cellWidth);
					details.AppendLine($"      |{paddedInputLabels}|{paddedExpectedLabels}|{paddedOutputLabels}");

					foreach (var step in r.SequenceSteps)
					{
						string stepStatus = step.Passed ? "✓" : "✗"; // Simplified for layout
						string inputs = string.IsNullOrEmpty(step.Inputs) ? " -" : " " + BinaryToColoredDotsWithSpacing(step.Inputs);
						string expected = string.IsNullOrEmpty(step.Expected) ? " -" : " " + BinaryToColoredDotsWithSpacing(step.Expected);
						string got = string.IsNullOrEmpty(step.Got) ? " -" : " " + BinaryToColoredDotsWithSpacing(step.Got);

						int inputsVisLen = GetVisibleLength(inputs);
						int expectedVisLen = GetVisibleLength(expected);
						int gotVisLen = GetVisibleLength(got);
						
						string paddedInputs = inputs + new string(' ', Math.Max(0, cellWidth - inputsVisLen));
						string paddedExpected = expected + new string(' ', Math.Max(0, cellWidth - expectedVisLen));
						string paddedGot = got + new string(' ', Math.Max(0, cellWidth - gotVisLen)) + " " + stepStatus;

						details.AppendLine($" {stepStatus}|{paddedInputs}|{paddedExpected}|{paddedGot}");
					}

					// Draw invisible text to calculate bounds
					MenuHelper.DrawText(details.ToString(), topLeft, Anchor.TopLeft, Color.clear, bold: false);
				}
				else
				{
					// Default message height - draw invisible text to calculate bounds
					string defaultMessage = "Select a test from the\nlist to view details...";

					MenuHelper.DrawText(defaultMessage, topLeft, Anchor.TopLeft, Color.clear, bold: false);
				}
			}
		}

		static float CalculateContentHeight()
		{
			if (_selectedIndex >= 0 && _selectedIndex < _rows.Count)
			{
				var r = _rows[_selectedIndex];
				if (r.SequenceSteps != null)
				{
					// Calculate height using real font metrics
					var theme = DrawSettings.ActiveUITheme;

					// Get actual line height from the font system
					Vector2 sampleTextSize = Seb.Vis.UI.UI.CalculateTextSize("M", theme.FontSizeRegular, theme.FontRegular);
					float lineHeight = sampleTextSize.y * 1.3f; // LineHeightEM from TextLayoutHelper (1.3f)

					// Two header lines + sequence steps + padding
					float headerHeight = lineHeight * 2f; // Column titles + bit labels
					float stepHeight = r.SequenceSteps.Count * lineHeight; // Each step
					float padding = 2f; // Some padding

					return headerHeight + stepHeight + padding;
				}
			}

			// Default message height using real font metrics
			var defaultTheme = DrawSettings.ActiveUITheme;
			Vector2 defaultTextSize = Seb.Vis.UI.UI.CalculateTextSize("Select a test from the\nlist to view details...", defaultTheme.FontSizeRegular, defaultTheme.FontRegular);
			return defaultTextSize.y + 1f; // Add some padding
		}


		/// <summary>
		/// Applies the selected test's inputs to the simulation and returns to the main simulation view.
		/// </summary>
		static void ApplySelectedTestInputs()
		{
			if (_selectedIndex < 0 || _selectedIndex >= _rows.Count) return;

			var selectedRow = _rows[_selectedIndex];

			// Only apply inputs for combinational levels (sequential levels don't support this)
			if (_isSequentialLevel)
			{
				Debug.Log("[LevelValidationPopup] Cannot apply test inputs for sequential levels");
				return;
			}

			try
			{
				// Parse the input string (e.g., "01", "10", "11") into a BitVector
				var inputVector = BitVector.FromString(selectedRow.Inputs);

				// Apply the inputs to the simulation
				var adapter = new MobileSimulationAdapter();
				adapter.ApplyInputs(inputVector);

				// Close the validation popup and return to the simulation
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);

				Debug.Log($"[LevelValidationPopup] Applied test inputs: {selectedRow.Inputs} for test case {_selectedIndex + 1}");
			}
			catch (Exception ex)
			{
				Debug.LogError($"[LevelValidationPopup] Failed to apply test inputs '{selectedRow.Inputs}': {ex.Message}");
			}
		}

		static int GetNandGateCount()
		{
			// Get the current level's simulation adapter to count NAND gates
			var levelManager = LevelManager.Instance;
			if (levelManager?.Current == null) return 0;

			// Use the same simulation adapter that was used for validation
			var adapter = new MobileSimulationAdapter();
			return adapter.CountNandGates();
		}

		static string ComposeFailSuffix(in TestRow r)
		{
			// Prefer explicit "Expected X, got Y" when available; else just the message.
			if (!string.IsNullOrEmpty(r.Got))
				return $"  (Expected {r.Expected}, got {r.Got})";
			return string.IsNullOrEmpty(r.Message) ? "" : $"  ({r.Message})";
		}

		// Attempts to pull "got ..." out of messages like "Expected 1010, got 1000"
		static string TryExtractGotValue(string message)
		{
			if (string.IsNullOrEmpty(message)) return "";
			// Very lightweight parse; safe against different wording
			// Look for "got " and take the trailing token
			int i = message.IndexOf("got ", StringComparison.OrdinalIgnoreCase);
			if (i >= 0)
			{
				string tail = message.Substring(i + 4).Trim();
				// stop at first whitespace or punctuation
				int cut = 0;
				while (cut < tail.Length && !char.IsWhiteSpace(tail[cut]) && tail[cut] != ',' && tail[cut] != ';' && tail[cut] != ')')
					cut++;
				return tail.Substring(0, cut);
			}
			return "";
		}

		// Attempts to pull "expected ..." out of messages like "Expected 1010, got 1000"
		static string TryExtractExpectedValue(string message)
		{
			if (string.IsNullOrEmpty(message)) return "";
			int i = message.IndexOf("Expected", StringComparison.OrdinalIgnoreCase);
			if (i >= 0)
			{
				// e.g., "Expected 1010, got 1000"
				string tail = message.Substring(i + "Expected".Length).TrimStart();
				// Take first token
				int cut = 0;
				while (cut < tail.Length && !char.IsWhiteSpace(tail[cut]) && tail[cut] != ',' && tail[cut] != ';' && tail[cut] != ')')
					cut++;
				return tail.Substring(0, cut);
			}
			return "";
		}

		// ---------- Firebase Buttons ----------
		static void DrawFirebaseButtons()
		{
			Vector2 buttonStart = prev.CentreBottom + Vector2.down * 2.5f;
			float buttonWidth = Seb.Vis.UI.UI.Width * 0.28f * 0.75f;  // Reduced button width to 75% of current size
			float buttonHeight = ButtonHeight * 1.3f; // Increased button height for better proportions
			float spacing = 1.8f * 0.5f; // Reduced spacing to half of current value

			// Check if level is passed (all rows passed)
			bool levelPassed = _rows.Count > 0 && _rows.All(r => r.Passed);
			bool hasValidSelection = _selectedIndex >= 0 && _selectedIndex < _rows.Count;
			bool canApplyTest = hasValidSelection && !_isSequentialLevel; // Only allow for combinational levels

			// Calculate grid positions (4 buttons per row) - center relative to entire popup, not just scroll view
			float totalWidth = (buttonWidth * 4) + (spacing * 3);
			float startX = Seb.Vis.UI.UI.Centre.x - totalWidth / 2f;  // Center relative to entire popup
			float startY = buttonStart.y;

			// Row 1: Apply Test, Upload Score, Levels, Next
			Vector2 applyTestPos = new Vector2(startX, startY);
			Vector2 uploadPos = new Vector2(startX + buttonWidth + spacing, startY);
			Vector2 levelsPos = new Vector2(startX + (buttonWidth + spacing) * 2, startY);
			Vector2 nextPos = new Vector2(startX + (buttonWidth + spacing) * 3, startY);

			// Row 2: Restart, Leaderboard, Save as Chip, Close
			Vector2 restartPos = new Vector2(startX, startY - buttonHeight - spacing);
			Vector2 leaderboardPos = new Vector2(startX + buttonWidth + spacing, startY - buttonHeight - spacing);
			Vector2 saveAsChipPos = new Vector2(startX + (buttonWidth + spacing) * 2, startY - buttonHeight - spacing);
			Vector2 closePos = new Vector2(startX + (buttonWidth + spacing) * 3, startY - buttonHeight - spacing);

			// Row 1: Apply Test button (position 1)
			bool applyTestPressed = Seb.Vis.UI.UI.Button(
				"Apply Test",
				MenuHelper.Theme.ButtonTheme,
				applyTestPos,
				new Vector2(buttonWidth, buttonHeight),
				canApplyTest, // Only enabled for combinational levels with valid selection
				false,
				false,
				MenuHelper.Theme.ButtonTheme.buttonCols,
				Anchor.TopLeft
			);

			// Row 1: Upload Score button (position 2)
			string uploadButtonText = _isUploading ? _uploadStatus : "Upload Score";
			bool uploadPressed = Seb.Vis.UI.UI.Button(
				uploadButtonText,
				MenuHelper.Theme.ButtonTheme,
				uploadPos,
				new Vector2(buttonWidth, buttonHeight),
				!_isUploading && levelPassed, // Disabled if uploading or level not passed
				false,
				false,
				MenuHelper.Theme.ButtonTheme.buttonCols,
				Anchor.TopLeft
			);

			// Row 1: Levels button (position 3)
			bool levelsPressed = Seb.Vis.UI.UI.Button(
				"Levels",
				MenuHelper.Theme.ButtonTheme,
				levelsPos,
				new Vector2(buttonWidth, buttonHeight),
				true,
				false,
				false,
				MenuHelper.Theme.ButtonTheme.buttonCols,
				Anchor.TopLeft
			);

			// Row 1: Next button (position 4)
			bool nextPressed = Seb.Vis.UI.UI.Button(
				"Next",
				MenuHelper.Theme.ButtonTheme,
				nextPos,
				new Vector2(buttonWidth, buttonHeight),
				true, // Always enabled
				false,
				false,
				MenuHelper.Theme.ButtonTheme.buttonCols,
				Anchor.TopLeft
			);

			// Row 2: Restart button (position 1)
			bool restartPressed = Seb.Vis.UI.UI.Button(
				"Restart",
				MenuHelper.Theme.ButtonTheme,
				restartPos,
				new Vector2(buttonWidth, buttonHeight),
				true,
				false,
				false,
				MenuHelper.Theme.ButtonTheme.buttonCols,
				Anchor.TopLeft
			);

			// Row 2: Leaderboard button (position 2)
			bool leaderboardPressed = Seb.Vis.UI.UI.Button(
				"Leaderboard",
				MenuHelper.Theme.ButtonTheme,
				leaderboardPos,
				new Vector2(buttonWidth, buttonHeight),
				true,
				false,
				false,
				MenuHelper.Theme.ButtonTheme.buttonCols,
				Anchor.TopLeft
			);

			// Row 2: Save as Chip button (position 3)
			bool saveAsChipPressed = Seb.Vis.UI.UI.Button(
				"Save as Chip",
				MenuHelper.Theme.ButtonTheme,
				saveAsChipPos,
				new Vector2(buttonWidth, buttonHeight),
				levelPassed, // Only enabled when level is completed
				false,
				false,
				MenuHelper.Theme.ButtonTheme.buttonCols,
				Anchor.TopLeft
			);

			// Row 2: Close button (position 4)
			bool closePressed = Seb.Vis.UI.UI.Button(
				"Close",
				MenuHelper.Theme.ButtonTheme,
				closePos,
				new Vector2(buttonWidth, buttonHeight),
				true,
				false,
				false,
				MenuHelper.Theme.ButtonTheme.buttonCols,
				Anchor.TopLeft
			);

			// Handle button presses
			if (applyTestPressed && canApplyTest)
			{
				ApplySelectedTestInputs();
			}

			if (uploadPressed && levelPassed && !_isUploading)
			{
				// Show user name input popup for score upload
				UserNameInputPopup.Open(OnUserNameConfirmed, OnUserNameCancelled);
			}

			if (saveAsChipPressed && levelPassed)
			{
				// Set return menu so ChipSave knows to come back here
				ChipSaveMenu.SetReturnMenu(UIDrawer.MenuType.LevelValidationResult);
				// Open the chip save menu
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.ChipSave);
			}

			if (restartPressed)
			{
				RestartCurrentLevel();
			}

			if (nextPressed)
			{
				PlayNextLevel();
			}

			if (leaderboardPressed)
			{
				string levelId = GetCurrentLevelId();
				LeaderboardPopup.Open(levelId);
			}

			if (levelsPressed)
			{
				// Just open the levels menu - unsaved changes check will happen when user actually selects a new level
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.Levels);
			}

			if (closePressed)
			{
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
			}
		}

		// ---------- User Name Input Callbacks ----------
		static void OnUserNameConfirmed(string userName, bool shouldRemember, bool shareSolution)
		{
			// Start upload with user name and solution sharing preference
			_ = UploadToLeaderboard(userName, shareSolution);
		}

		static void OnUserNameCancelled()
		{
			// User cancelled, do nothing
			Debug.Log("[LevelValidationPopup] User cancelled name input");
		}

		static async System.Threading.Tasks.Task UploadToLeaderboard(string userName = null, bool shareSolution = false)
		{
			// Ultimate safety wrapper to prevent any crashes
			try
			{
				_isUploading = true;
				_uploadStatus = "Initializing...";
				Debug.Log("[Leaderboard] Starting upload process...");

				// Simple approach: Disable solution sharing in Editor to prevent crashes
#if UNITY_EDITOR
				if (shareSolution)
				{
					Debug.Log("[Leaderboard] Editor mode - disabling solution sharing to prevent crashes");
					shareSolution = false;
				}
#endif

				// Step 1: Initialize Firebase with timeout
				_uploadStatus = "Connecting to Firebase...";
				Debug.Log("[Leaderboard] Step 1: Initializing Firebase...");

				using (var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(30)))
				{
					try
					{
						var initTask = FirebaseBootstrap.InitializeAsync();
						var timeoutTask = Task.Delay(30000, cts.Token);
						await Task.WhenAny(initTask, timeoutTask);
						if (timeoutTask.IsCompleted)
						{
							throw new OperationCanceledException("Firebase initialization timed out");
						}
						await initTask;
						Debug.Log($"[Leaderboard] Firebase initialized: {FirebaseBootstrap.IsInitialized}, UserId: {FirebaseBootstrap.UserId}");
					}
					catch (OperationCanceledException)
					{
						Debug.LogWarning("[Leaderboard] Firebase initialization timed out after 30 seconds");
						_uploadStatus = "Connection timeout - using offline mode";
						await System.Threading.Tasks.Task.Delay(1000);
						_isUploading = false;
						_uploadStatus = "";
						return;
					}
				}

				// Step 2: Calculate score
				_uploadStatus = "Calculating score...";
				Debug.Log("[Leaderboard] Step 2: Calculating score...");
				int score = CalculateLevelScore();
				Debug.Log($"[Leaderboard] Calculated score: {score}");

				// Step 3: Get level ID
				_uploadStatus = "Preparing upload...";
				Debug.Log("[Leaderboard] Step 3: Getting level ID...");
				string levelId = GetCurrentLevelId();
				Debug.Log($"[Leaderboard] Level ID: {levelId}");

				// Step 4: Upload based on sharing preference
				string completeSolutionId = null;

				if (shareSolution)
				{
					_uploadStatus = "Creating complete solution...";
					Debug.Log("[Leaderboard] Step 4: Creating complete solution...");

					// Create complete solution with all custom chip definitions
					var completeSolution = SolutionSerializer.CreateCompleteSolutionFromCurrentProject(levelId, score, userName);
					Debug.Log($"[Leaderboard] Complete solution created with {completeSolution.CustomChipDefinitions.Count} custom chips");

					_uploadStatus = "Uploading complete solution...";
					Debug.Log("[Leaderboard] Step 5: Uploading complete solution...");

					using (var uploadCts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(60)))
					{
						try
						{
							var uploadTask = LeaderboardService.SaveCompleteSolutionAsync(completeSolution, null);
							var timeoutTask = Task.Delay(60000, uploadCts.Token);
							await Task.WhenAny(uploadTask, timeoutTask);
							if (timeoutTask.IsCompleted)
							{
								throw new OperationCanceledException("Complete solution upload timed out");
							}
							completeSolutionId = await uploadTask;
							_uploadStatus = "Complete solution uploaded!";
							Debug.Log($"[Leaderboard] Complete solution uploaded successfully for level {levelId}!");
						}
						catch (OperationCanceledException)
						{
							Debug.LogWarning("[Leaderboard] Complete solution upload timed out after 60 seconds");
							_uploadStatus = "Upload timeout - please try again";
							await System.Threading.Tasks.Task.Delay(2000);
							_isUploading = false;
							_uploadStatus = "";
							return;
						}
					}
				}

				// Step 5: Upload score (with or without complete solution)
				_uploadStatus = "Uploading score...";
				Debug.Log("[Leaderboard] Step 6: Uploading score...");

				using (var uploadCts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(25)))
				{
					try
					{
						var uploadTask = LeaderboardService.SaveScoreAsync(levelId, score, null, null, userName, completeSolutionId);
						var timeoutTask = Task.Delay(25000, uploadCts.Token);
						await Task.WhenAny(uploadTask, timeoutTask);
						if (timeoutTask.IsCompleted)
						{
							throw new OperationCanceledException("Upload timed out");
						}
						await uploadTask;
						_uploadStatus = "Upload successful!";
						Debug.Log($"[Leaderboard] Score {score} uploaded successfully for level {levelId}!");
					}
					catch (OperationCanceledException)
					{
						Debug.LogWarning("[Leaderboard] Upload timed out after 25 seconds");
						_uploadStatus = "Upload timeout - please try again";
						await System.Threading.Tasks.Task.Delay(2000);
						_isUploading = false;
						_uploadStatus = "";
						return;
					}
				}

				// Reset status after a brief delay
				await System.Threading.Tasks.Task.Delay(1500);
				_isUploading = false;
				_uploadStatus = "";
			}
			catch (Exception ex)
			{
				_uploadStatus = "Upload failed!";
				Debug.LogError($"[Leaderboard] Upload failed: {ex.Message}");
				Debug.LogError($"[Leaderboard] Stack trace: {ex.StackTrace}");

				// Reset status after showing error
				await System.Threading.Tasks.Task.Delay(2000);
				_isUploading = false;
				_uploadStatus = "";
			}
		}

		static int CalculateLevelScore()
		{
			// Use the same scoring as displayed in UI: NAND gate count
			// Lower is better (fewer NAND gates = better score)
			return GetNandGateCount();
		}

		static string GetCurrentLevelId()
		{
			// Try to get the actual level name from LevelManager
			var levelManager = LevelManager.Instance;
			if (levelManager?.Current != null)
			{
				var def = levelManager.Current;

				// Use the actual level definition properties
				if (!string.IsNullOrEmpty(def.name))
				{
					return def.name;
				}

				if (!string.IsNullOrEmpty(def.id))
				{
					return def.id;
				}

				// Try to infer level type from test vectors
				if (def.testVectors != null && def.testVectors.Length > 0)
				{
					return InferLevelType(def);
				}
			}

			// Final fallback
			return "Unknown Level";
		}


		static string InferLevelType(DLS.Levels.LevelDefinition levelDef)
		{
			// Try to infer the level type from the level definition
			try
			{
				// Check if the level name or ID contains gate type information
				string nameToCheck = levelDef.name ?? levelDef.id ?? "";

				if (nameToCheck.Contains("NOT") || nameToCheck.Contains("Not"))
					return "NOT Gate";
				if (nameToCheck.Contains("AND") || nameToCheck.Contains("And"))
					return "AND Gate";
				if (nameToCheck.Contains("OR") || nameToCheck.Contains("Or"))
					return "OR Gate";
				if (nameToCheck.Contains("XOR") || nameToCheck.Contains("Xor"))
					return "XOR Gate";
				if (nameToCheck.Contains("NAND") || nameToCheck.Contains("Nand"))
					return "NAND Gate";
				if (nameToCheck.Contains("NOR") || nameToCheck.Contains("Nor"))
					return "NOR Gate";

				// Try to analyze test vectors to determine gate type
				if (levelDef.testVectors != null && levelDef.testVectors.Length > 0)
				{
					return AnalyzeTestVectors(levelDef.testVectors);
				}

				// Default fallback
				return "Logic Gate";
			}
			catch
			{
				return "Logic Gate";
			}
		}

		/// <summary>
		/// Restart the current level
		/// </summary>
		static void RestartCurrentLevel()
		{
			var levelManager = LevelManager.Instance;
			if (levelManager?.Current == null)
			{
				Debug.LogWarning("[LevelValidationPopup] No active level to restart");
				return;
			}

			// Get the current level definition
			var currentLevel = levelManager.Current;

			// Clear saved progress before restarting
			LevelProgressService.ClearLevelProgress(currentLevel.id);

			// Restart the level
			levelManager.StartLevel(currentLevel);

			// Close the validation popup
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);

			Debug.Log($"[LevelValidationPopup] Restarted level: {currentLevel.name}");
		}

		/// <summary>
		/// Play the next level in the sequence
		/// </summary>
		static void PlayNextLevel()
		{
			var levelManager = LevelManager.Instance;
			if (levelManager?.Current == null)
			{
				Debug.LogWarning("[LevelValidationPopup] No active level to get next from");
				return;
			}

			// Get the next level definition
			var nextLevel = GetNextLevelDefinition();

			if (nextLevel == null)
			{
				Debug.Log("[LevelValidationPopup] No next level available");
				// Close popup and return to levels menu
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.Levels);
				return;
			}

			// Check for unsaved changes before starting next level
			if (levelManager.IsActive && levelManager.HasUnsavedChanges())
			{
				Debug.Log("[LevelValidationPopup] PlayNextLevel: Showing level unsaved changes popup");
				LevelUnsavedChangesPopup.OpenPopup((option) => HandleNextLevelAfterUnsavedCheck(option, nextLevel));
			}
			else
			{
				Debug.Log("[LevelValidationPopup] PlayNextLevel: No unsaved changes, proceeding directly");
				StartNextLevel(nextLevel);
			}
		}

		/// <summary>
		/// Handle unsaved changes callback when navigating to next level
		/// </summary>
		static void HandleNextLevelAfterUnsavedCheck(int option, LevelDefinition nextLevel)
		{
			if (option == 0) // Cancel
			{
				// Do nothing, stay in validation popup
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.LevelValidationResult);
				return;
			}
			else if (option == 1) // Save and Continue
			{
				// Save level progress before starting next level
				var levelManager = LevelManager.Instance;
				if (levelManager?.IsActive == true)
				{
					levelManager.SaveCurrentProgress();
					Debug.Log($"[LevelValidationPopup] Saved level progress before starting next level");
				}
				StartNextLevel(nextLevel);
			}
			else if (option == 2) // Continue without Saving
			{
				StartNextLevel(nextLevel);
			}
		}

	/// <summary>
		/// Actually start the next level
		/// </summary>
		static void StartNextLevel(LevelDefinition nextLevel)
		{
			var levelManager = LevelManager.Instance;

			// Start the next level (will auto-load saved progress if it exists)
			levelManager.StartLevel(nextLevel);

			// Close the validation popup
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);

			Debug.Log($"[LevelValidationPopup] Started next level: {nextLevel.name}");
		}

		/// <summary>
		/// Get the next level definition based on the current level
		/// </summary>
		static LevelDefinition GetNextLevelDefinition()
		{
			var levelManager = LevelManager.Instance;
			if (levelManager?.Current == null) return null;

			var currentLevel = levelManager.Current;

			try
			{
				// Load the levels.json file to find the next level
				var levelsText = Resources.Load<TextAsset>("levels");
				if (levelsText == null)
				{
					Debug.LogWarning("[LevelValidationPopup] Could not find levels.json resource");
					return null;
				}

				var levelsData = JsonUtility.FromJson<LevelsData>(levelsText.text);
				if (levelsData?.chapters == null)
				{
					Debug.LogWarning("[LevelValidationPopup] Invalid levels data structure");
					return null;
				}

				// Find current level position
				for (int chapterIndex = 0; chapterIndex < levelsData.chapters.Length; chapterIndex++)
				{
					var chapter = levelsData.chapters[chapterIndex];
					if (chapter.levels == null) continue;

					for (int levelIndex = 0; levelIndex < chapter.levels.Length; levelIndex++)
					{
						var level = chapter.levels[levelIndex];
						if (level.id == currentLevel.id)
						{
							// Found current level - check for next level

							// First, try next level in same chapter
							if (levelIndex + 1 < chapter.levels.Length)
							{
								var nextLevel = chapter.levels[levelIndex + 1];
								Debug.Log($"[LevelValidationPopup] Found next level in same chapter: {nextLevel.name}");
								return nextLevel;
							}

							// If this is the last level in chapter, try first level in next chapter
							if (chapterIndex + 1 < levelsData.chapters.Length)
							{
								var nextChapter = levelsData.chapters[chapterIndex + 1];
								if (nextChapter.levels != null && nextChapter.levels.Length > 0)
								{
									var nextLevel = nextChapter.levels[0];
									Debug.Log($"[LevelValidationPopup] Found next level in next chapter: {nextLevel.name}");
									return nextLevel;
								}
							}

							// No next level found
							Debug.Log("[LevelValidationPopup] This is the last level in the game");
							return null;
						}
					}
				}

				Debug.LogWarning($"[LevelValidationPopup] Could not find current level {currentLevel.id} in levels data");
				return null;
			}
			catch (Exception ex)
			{
				Debug.LogError($"[LevelValidationPopup] Error finding next level: {ex.Message}");
				return null;
			}
		}


		static string AnalyzeTestVectors(DLS.Levels.LevelDefinition.TestVector[] testVectors)
		{
			try
			{
				// Analyze test vectors to determine gate type
				if (testVectors.Length == 0) return "Logic Gate";

				// Simple heuristic: look at input/output patterns
				var firstVector = testVectors[0];
				int inputCount = firstVector.inputs.Length;
				int outputCount = firstVector.expected.Length;

				// Single input, single output - likely a basic gate
				if (inputCount == 1 && outputCount == 1)
				{
					// Check if it's a NOT gate (inverts input)
					if (testVectors.Length >= 2)
					{
						var inputs = testVectors.Select(tv => tv.inputs).Distinct().ToArray();
						var outputs = testVectors.Select(tv => tv.expected).Distinct().ToArray();

						if (inputs.Length == 2 && outputs.Length == 2)
						{
							return "NOT Gate";
						}
					}
					return "Single Input Gate";
				}

				// Two inputs, single output - likely AND, OR, XOR, etc.
				if (inputCount == 2 && outputCount == 1)
				{
					return "Two Input Gate";
				}

				// Multiple inputs/outputs
				return "Multi-Gate Circuit";
			}
			catch
			{
				return "Logic Gate";
			}
		}

	}
}

