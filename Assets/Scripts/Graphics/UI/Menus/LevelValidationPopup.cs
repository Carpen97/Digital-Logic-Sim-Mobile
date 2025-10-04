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
using DLS.Online; // For Firebase testing
using static DLS.Graphics.DrawSettings;

namespace DLS.Graphics
{
	public static class LevelValidationPopup
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

		// Scroll list data
		static readonly List<TestRow> _rows = new();
		static int _selectedIndex = -1;
		
		// Level type detection
		static bool _isSequentialLevel = false;

		// UI constants (tweak to match your other menus)
		const float ListWidthFrac  = 0.90f;  // Increased from 0.72f to 0.90f
		const float ListHeightFrac = 0.5f;
		const float RowHeight      = 4.2f;
		const float OkBtnWidthFrac = 0.30f;
		const float OkBtnHeightMul = 1.5f;
		
		// Firebase test state
		static bool _isUploading = false;
		static string _uploadStatus = "";

		static readonly UIHandle ID_LevelValidationPopup = new("LevelValidationPopup_Scrollbar");
		static readonly Seb.Vis.UI.UI.ScrollViewDrawElementFunc DrawRowFunc = DrawRow;

		static bool isDraggingScrollbar;

		// ---------- Public API ----------
		public static void Open(ValidationReport report)
		{
			_rows.Clear();
			_selectedIndex = -1;

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
			_stars     = report.Stars;

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

			using (UI.BeginBoundsScope(true))
			{
				Draw.ID panelBG  = UI.ReservePanel();
				Draw.ID titleBG  = UI.ReservePanel();
				Draw.ID starsBG  = UI.ReservePanel();

				// --- Title banner ---
				Vector2 titlePos = UI.CentreTop + Vector2.down * 8f;
				Color headerCol = _isSuccess ? ColHelper.MakeCol255(44, 92, 62) : ColHelper.MakeCol255(155, 44, 44);
				UI.DrawText(_title, ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular * 2f, titlePos, Anchor.TextCentre, headerCol);
				UI.ModifyPanel(titleBG, Bounds2D.Grow(UI.PrevBounds, 3f), Color.clear);

				// --- Score display with info button ---
				{
					int nandCount = GetNandGateCount();
					string scoreStr = $"Score: {nandCount}";
					Color starsCol = _isSuccess ? ColHelper.MakeCol255(245, 212, 67) : Color.white;

					Vector2 scorePos = UI.PrevBounds.CentreBottom + new Vector2(0f, -1.4f);
					
					// Draw score text first
					UI.DrawText(scoreStr, ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular, scorePos, Anchor.TextCentre, starsCol);

					// Draw small info button to the right of the score text
					Vector2 infoButtonPos = scorePos + Vector2.right * (UI.Width * 0.25f);
					bool infoButtonPressed = UI.Button(
						"info",
						MenuHelper.Theme.ButtonTheme,
						infoButtonPos,
						new Vector2(ButtonHeight * 1.5f, ButtonHeight * 1.2f),
						true,
						true,
						false,
						MenuHelper.Theme.ButtonTheme.buttonCols,
						Anchor.Centre
					);

					if (infoButtonPressed)
					{
						ScoreExplanationPopup.Open();
					}

					UI.ModifyPanel(starsBG, Bounds2D.Grow(UI.PrevBounds, 1.2f), ColHelper.MakeCol(0.11f));
				}

				// --- Layout based on level type ---
				var theme = DrawSettings.ActiveUITheme;

				if (_isSequentialLevel)
				{
					// Sequential levels: Two-panel layout with 65:35 ratio
					float panelSpacing = 2f;
					float totalWidth = UI.Width * ListWidthFrac - panelSpacing;
					float leftPanelW = totalWidth * 0.58f;  // 65% for left panel (info)
					float rightPanelW = totalWidth * 0.40f; // 35% for right panel (test list)
					float panelH = UI.Height * ListHeightFrac;
					
					Vector2 leftPanelSize = new(leftPanelW, panelH);
					Vector2 rightPanelSize = new(rightPanelW, panelH);

					// --- Left panel: Info panel (larger) ---
					Vector2 leftPanelPos = UI.Centre + new Vector2(-rightPanelW * 0.5f - panelSpacing * 0.5f, 0f) + Vector2.down * 1f;
					DrawInfoPanel(leftPanelPos, leftPanelSize);

					// --- Right panel: Scrollable list of tests (smaller) ---
					Vector2 rightPanelPos = UI.Centre + new Vector2(leftPanelW * 0.5f + panelSpacing * 0.5f, 0f) + Vector2.down * 1f;
					ScrollBarState sv = UI.DrawScrollView(
						ID_LevelValidationPopup,
						rightPanelPos,
						rightPanelSize,
						UILayoutHelper.DefaultSpacing,
						Anchor.Centre,
						theme.ScrollTheme,
						DrawRowFunc,
						_rows.Count
					);
					isDraggingScrollbar = sv.isDragging;
				}
				else
				{
					// Combinational levels: Single-panel layout with header
					float panelW = UI.Width * ListWidthFrac;
					float panelH = UI.Height * ListHeightFrac * 0.8f;
					Vector2 panelSize = new(panelW, panelH);
					Vector2 panelPos = UI.Centre + Vector2.down * 1f;

					// Draw the single panel with header and scrollable content
					DrawCombinationalPanel(panelPos, panelSize);
				}

				// --- Firebase Buttons (includes OK button in grid) ---
				DrawFirebaseButtons();

				// Panel BG spanning everything drawn in this scope
				MenuHelper.DrawReservedMenuPanel(panelBG, UI.GetCurrentBoundsScope());
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
			
			bool pressed = UI.Button(
				label,
				MenuHelper.Theme.ButtonTheme,
				rowTopLeft,
				new Vector2(width, RowHeight),
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

		static void DrawCombinationalRow(Vector2 rowTopLeft, float width, TestRow r, bool selected, bool isLayoutPass)
		{
			// For combinational levels, draw in table format: Pass | IN | OUT | EXPECTED
			string status = r.Passed ? "PASS" : "FAIL";
			string statusColor = r.Passed ? "<color=#44ff44>" : "<color=#ff2222>";
			
			// Convert binary strings to colored dots
			string inputs = string.IsNullOrEmpty(r.Inputs) ? "-" : BinaryToColoredDots(r.Inputs);
			string expected = string.IsNullOrEmpty(r.Expected) ? "-" : BinaryToColoredDots(r.Expected);
			string got = string.IsNullOrEmpty(r.Got) ? "-" : BinaryToColoredDots(r.Got);
			
			// Fixed cell width of 6 spaces to match header
			const int cellWidth = 6;
			
			// Create formatted row text with fixed-width cells
			// Apply padding to the content, not the HTML tags
			string statusText = $"{statusColor}{status}</color>";
			string paddedStatus = statusText + new string(' ', Math.Max(0, cellWidth - status.Length));
			string paddedInputs = inputs + new string(' ', Math.Max(0, cellWidth - GetVisibleLength(inputs)));
			string paddedGot = got + new string(' ', Math.Max(0, cellWidth - GetVisibleLength(got)));
			string paddedExpected = expected + new string(' ', Math.Max(0, cellWidth - GetVisibleLength(expected)));
			
			string rowText = " " + paddedStatus + "| " + paddedInputs + "| " + paddedGot + "| " + paddedExpected;

			bool pressed = UI.Button(
				rowText,
				MenuHelper.Theme.ButtonTheme,
				rowTopLeft,
				new Vector2(width, RowHeight),
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

		// ---------- Helpers ----------
		static void RememberSelection()
		{
			// Hook for future behavior (e.g., jump to a waveform, auto-zoom, etc.)
			// Currently no-op, but keeping it mirrors other menus and avoids null refs.
		}

		static void DrawCombinationalPanel(Vector2 panelPos, Vector2 panelSize)
		{
			// Draw panel background
			Draw.ID panelID = UI.ReservePanel();
			UI.ModifyPanel(
				panelID,
				new Bounds2D(panelPos - panelSize * 0.5f, panelPos + panelSize * 0.5f),
				ColHelper.MakeCol(0.08f)
			);

			// Draw header row
			Vector2 headerPos = panelPos - panelSize * 0.5f + new Vector2(1f, panelSize.y - 5f);
			DrawCombinationalHeader(headerPos, panelSize);

			// Draw scrollable content area (below header)
			float headerHeight = RowHeight + 0.5f;
			Vector2 contentPos = headerPos + Vector2.down;
			Vector2 contentSize = new(panelSize.x - 2f, panelSize.y - headerHeight - 2f);

			ScrollBarState sv = UI.DrawScrollView(
				ID_LevelValidationPopup,
				contentPos,
				contentSize,
				UILayoutHelper.DefaultSpacing,
				Anchor.TopLeft,
				DrawSettings.ActiveUITheme.ScrollTheme,
				DrawRowFunc,
				_rows.Count
			);
			isDraggingScrollbar = sv.isDragging;
		}

		static void DrawCombinationalHeader(Vector2 headerPos, Vector2 panelSize)
		{
			// Draw header background
			Draw.ID headerID = UI.ReservePanel();
			UI.ModifyPanel(
				headerID,
				new Bounds2D(headerPos, headerPos + new Vector2(panelSize.x - 2f, RowHeight)),
				ColHelper.MakeCol(0.12f)
			);

			// Smaller cell width for better fit
			const int cellWidth = 6;
			
			// Draw header text with fixed-width cells
			Vector2 textPos = headerPos + new Vector2(-0.2f, RowHeight * 0.5f);
			string headerText = $" RESULT".PadRight(cellWidth+2) + "| " + 
			                   "IN".PadRight(cellWidth) + "| " + 
			                   "OUT".PadRight(cellWidth) + "| " + 
			                   "EXPECTED".PadRight(cellWidth);
			
			UI.DrawText(
				headerText,
				DrawSettings.ActiveUITheme.FontRegular,
				DrawSettings.ActiveUITheme.FontSizeRegular,
				textPos,
				Anchor.TextCentreLeft,
				Color.white
			);
		}

		static void DrawInfoPanel(Vector2 panelPos, Vector2 panelSize)
		{
			// Draw info panel background
			Draw.ID infoPanelID = UI.ReservePanel();
			UI.ModifyPanel(
				infoPanelID,
				new Bounds2D(panelPos - panelSize * 0.5f, panelPos + panelSize * 0.5f),
				ColHelper.MakeCol(0.08f)
			);

			// Draw info content - position closer to top of panel
			Vector2 contentPos = panelPos - panelSize * 0.5f + new Vector2(1f, panelSize.y - 1f);
			
			if (_selectedIndex >= 0 && _selectedIndex < _rows.Count)
			{
				// Show selected test details
				var r = _rows[_selectedIndex];
				var details = new StringBuilder();
				
	
					// Show sequence details with fixed cell width
					const int cellWidth = 10;
					details.AppendLine("  | Input    | Output   | Expected");
					
					foreach (var step in r.SequenceSteps)
					{
						string stepStatus = step.Passed ? "<color=#44ff44>✓</color>" : "<color=#ff2222>✗</color>";
						string clockIndicator = step.IsClockEdge ? " [CLK]" : "";
						
						// Convert binary strings to colored dots
						string inputs = string.IsNullOrEmpty(step.Inputs) ? "-" : " "+BinaryToColoredDots(step.Inputs);
						string expected = string.IsNullOrEmpty(step.Expected) ? "-" : " "+BinaryToColoredDots(step.Expected);
						string got = string.IsNullOrEmpty(step.Got) ? "-" : " "+BinaryToColoredDots(step.Got);
						
						// Apply fixed cell width padding
						string paddedInputs = inputs + new string(' ', Math.Max(0, cellWidth - GetVisibleLength(inputs)));
						string paddedExpected = expected + new string(' ', Math.Max(0, cellWidth - GetVisibleLength(expected)));
						string paddedGot = got + new string(' ', Math.Max(0, cellWidth - GetVisibleLength(got)));
						
						details.AppendLine($" {stepStatus}|{paddedInputs}|{paddedGot}|{paddedExpected}");
					}
				

				MenuHelper.DrawTopLeftAlignTextWithBackground(
					details.ToString(),
					contentPos,
					panelSize - new Vector2(2f, 2f),
					Anchor.TopLeft,
					Color.white,
					ColHelper.MakeCol(0.0f), // Transparent background since panel already has one
					bold: false,
					textPadX: 0.5f
				);
			}
			else
			{
				// Show default message when no test is selected
				string defaultMessage = "Select a test from the\nlist to view details...";
				
				MenuHelper.DrawTopLeftAlignTextWithBackground(
					defaultMessage,
					contentPos,
					panelSize - new Vector2(2f, 2f),
					Anchor.TopLeft,
					Color.gray,
					ColHelper.MakeCol(0.0f), // Transparent background
					bold: false,
					textPadX: 0.5f
				);
			}
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
			Vector2 buttonStart = UI.PrevBounds.CentreBottom + Vector2.down * 1f;
			float buttonWidth = UI.Width * 0.28f * 0.75f;  // Reduced button width to 75% of current size
			float buttonHeight = ButtonHeight * 1.3f; // Increased button height for better proportions
			float spacing = 1.8f * 0.5f; // Reduced spacing to half of current value

			// Check if level is passed (all rows passed)
			bool levelPassed = _rows.Count > 0 && _rows.All(r => r.Passed);
			bool hasValidSelection = _selectedIndex >= 0 && _selectedIndex < _rows.Count;
			bool canApplyTest = hasValidSelection && !_isSequentialLevel; // Only allow for combinational levels

			// Calculate grid positions (3 buttons per row) - center relative to entire popup, not just scroll view
			float totalWidth = (buttonWidth * 3) + (spacing * 2);
			float startX = UI.Centre.x - totalWidth / 2f;  // Center relative to entire popup
			float startY = buttonStart.y;

			// Row 1: Apply Test, Upload Score, Save as Chip
			Vector2 applyTestPos = new Vector2(startX, startY);
			Vector2 uploadPos = new Vector2(startX + buttonWidth + spacing, startY);
			Vector2 saveAsChipPos = new Vector2(startX + (buttonWidth + spacing) * 2, startY);

			// Row 2: Levels, Leaderboard, Close
			Vector2 levelsPos = new Vector2(startX, startY - buttonHeight - spacing);
			Vector2 leaderboardPos = new Vector2(startX + buttonWidth + spacing, startY - buttonHeight - spacing);
			Vector2 closePos = new Vector2(startX + (buttonWidth + spacing) * 2, startY - buttonHeight - spacing);

			// Row 1: Apply Test button (left)
			bool applyTestPressed = UI.Button(
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

			// Row 1: Upload Score button (middle)
			string uploadButtonText = _isUploading ? _uploadStatus : "Upload Score";
			bool uploadPressed = UI.Button(
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

			// Row 1: Save as Chip button (right)
			bool saveAsChipPressed = UI.Button(
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

			// Row 2: Levels button (left)
			bool levelsPressed = UI.Button(
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

			// Row 2: Leaderboard button (middle)
			bool leaderboardPressed = UI.Button(
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

			// Row 2: Close button (right)
			bool closePressed = UI.Button(
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
				// Open the chip save menu
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.ChipSave);
			}

			if (levelsPressed)
			{
				// Open the levels menu
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.Levels);
			}

			if (leaderboardPressed)
			{
				string levelId = GetCurrentLevelId();
				LeaderboardPopup.Open(levelId);
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

