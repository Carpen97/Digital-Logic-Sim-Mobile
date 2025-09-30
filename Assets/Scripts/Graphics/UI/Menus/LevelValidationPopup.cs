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
		}

		// ---------- Popup state ----------
		static string _title = "";
		static bool _isSuccess;
		static int _stars;

		// Scroll list data
		static readonly List<TestRow> _rows = new();
		static int _selectedIndex = -1;

		// UI constants (tweak to match your other menus)
		const float ListWidthFrac  = 0.72f;
		const float ListHeightFrac = 0.40f;
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

			// Title
			_title = _isSuccess ? "All tests passed" : report.Failures.Count>1 ? $"{report.Failures.Count} tests failed" : $"1 test failed";

			// Build a failure lookup keyed by the input vector so we can mark pass/fail per vector.
			// If there can be duplicate input patterns, consider keying by (inputs,index).
			var failByInputs = new Dictionary<string, CaseFail>();
			if (report.Failures != null)
			{
				foreach (var f in report.Failures)
				{
					// last one wins if duplicate keys; acceptable for UI purposes
					failByInputs[f.Inputs] = f;
				}
			}

			// Try to show ALL test vectors from the active level definition
			// If the definition isn't available, fall back to just failures.
			var def = LevelManager.Instance != null ? LevelManager.Instance.Current : null;
			if (def != null && def.testVectors != null && def.testVectors.Length > 0)
			{
				foreach (var tv in def.testVectors)
				{
					if (failByInputs.TryGetValue(tv.inputs, out var cf))
					{
						_rows.Add(new TestRow
						{
							Inputs   = tv.inputs,
							Expected = tv.expected,
							Got      = TryExtractGotValue(cf.Message),
							Passed   = false,
							Message  = cf.Message
						});
					}
					else
					{
						// We don't know the actual "got" value for passing cases; we show expected and mark as passed.
						_rows.Add(new TestRow
						{
							Inputs   = tv.inputs,
							Expected = tv.expected,
							Got      = "",          // unknown for passes
							Passed   = true,
							Message  = "OK"
						});
					}
				}
			}
			else
			{
				// Fallback: we only know about failures
				if (report.Failures != null && report.Failures.Count > 0)
				{
					foreach (var f in report.Failures)
					{
						_rows.Add(new TestRow
						{
							Inputs   = f.Inputs,
							Expected = TryExtractExpectedValue(f.Message),
							Got      = TryExtractGotValue(f.Message),
							Passed   = false,
							Message  = f.Message
						});
					}
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

				// --- Scrollable list of tests ---
				float listW = UI.Width  * ListWidthFrac ;
				float listH = UI.Height * ListHeightFrac;
				Vector2 listSize = new(listW, listH);

				var theme = DrawSettings.ActiveUITheme;

				ScrollBarState sv = UI.DrawScrollView(
					ID_LevelValidationPopup,
					UI.Centre,
					listSize,
					UILayoutHelper.DefaultSpacing,
					Anchor.Centre,
					theme.ScrollTheme,
					DrawRowFunc,
					_rows.Count
				);
				isDraggingScrollbar = sv.isDragging;

				/*
				// --- Selected row details (optional, compact) ---
				if (_selectedIndex >= 0 && _selectedIndex < _rows.Count)
				{
					var r = _rows[_selectedIndex];
					var details = new StringBuilder();
					details.AppendLine(r.Passed ? "Result: OK" : "Result: FAIL");
					details.AppendLine($"Inputs:   {r.Inputs}");
					details.AppendLine($"Expected: {r.Expected}");
					if (!string.IsNullOrEmpty(r.Got)) details.AppendLine($"Got:      {r.Got}");
					if (!string.IsNullOrEmpty(r.Message) && !r.Passed) details.AppendLine(r.Message);

					// Place details just below the list
					//Vector2 detailTopLeft = UI.PrevBounds.CentreBottom + new Vector2(-listW * 0.5f, -2.0f);
					Vector2 detailTopLeft = UI.PrevBounds.TopRight + Vector2.right * 2f;
					float detailHeight = Mathf.Min(UI.Height * 0.18f, 12f);

					MenuHelper.DrawLeftAlignTextWithBackground(
						details.ToString(),
						detailTopLeft + new Vector2(listW / 2f, -detailHeight / 2f),
						new Vector2(listW, detailHeight),
						Anchor.Centre,
						Color.white,
						ColHelper.MakeCol(0.08f),
						bold: false,
						textPadX: 1.0f
					);
				}
				*/

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

			// Compose a compact one-line label
			// Example: "[OK]   0101  →  1110"
			//          "[FAIL] 0101  →  1110  (Expected 1110, got 0110)"
			string status = r.Passed ? "<color=#44ff44> [PASS]" : "<color=#ff2222> [FAIL]";
			string arrow  = " → ";
			string extra  = r.Passed ? "" : ComposeFailSuffix(r);

			string label = $"{status} {r.Inputs}{arrow}{r.Expected}{extra}";

			// Visually nudge text to feel centered
			// const float nudgeLeft = -28f; // TODO: Implement visual nudge if needed

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
				//textOffsetX: nudgeLeft,
				ignoreInputs: isDraggingScrollbar
			);

			if (!isLayoutPass && pressed)
			{
				_selectedIndex = index;
				RememberSelection();
			}
		}

		// ---------- Helpers ----------
		static void RememberSelection()
		{
			// Hook for future behavior (e.g., jump to a waveform, auto-zoom, etc.)
			// Currently no-op, but keeping it mirrors other menus and avoids null refs.
		}

		/// <summary>
		/// Applies the selected test's inputs to the simulation and returns to the main simulation view.
		/// </summary>
		static void ApplySelectedTestInputs()
		{
			if (_selectedIndex < 0 || _selectedIndex >= _rows.Count) return;

			var selectedRow = _rows[_selectedIndex];
			
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
			float buttonWidth = UI.Width * 0.28f;  // Further increased button width for better fit
			float buttonHeight = ButtonHeight * 1.3f; // Increased button height for better proportions
			float spacing = 1.8f; // Further increased spacing for better separation

			// Check if level is passed (all rows passed)
			bool levelPassed = _rows.Count > 0 && _rows.All(r => r.Passed);
			bool hasValidSelection = _selectedIndex >= 0 && _selectedIndex < _rows.Count;

			// Calculate grid positions (2x2 grid) - ensure buttons fit within popup bounds
			float totalWidth = (buttonWidth * 2) + spacing;
			float startX = UI.PrevBounds.Centre.x - totalWidth / 2f;
			float startY = buttonStart.y;

			// Top row buttons
			Vector2 uploadPos = new Vector2(startX, startY);
			Vector2 leaderboardPos = new Vector2(startX + buttonWidth + spacing, startY);

			// Bottom row buttons  
			Vector2 testInputPos = new Vector2(startX, startY - buttonHeight - spacing);
			Vector2 okPos = new Vector2(startX + buttonWidth + spacing, startY - buttonHeight - spacing);

			// Upload to Leaderboard button (top-left)
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

			// Show Leaderboard button (top-right)
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

			// Test Input button (bottom-left)
			bool testInputPressed = UI.Button(
				"Apply Test",
				MenuHelper.Theme.ButtonTheme,
				testInputPos,
				new Vector2(buttonWidth, buttonHeight),
				hasValidSelection, // Only enabled if a test is selected
				false,
				false,
				MenuHelper.Theme.ButtonTheme.buttonCols,
				Anchor.TopLeft
			);

			// Close button (bottom-right)
			bool okPressed = UI.Button(
				"Close",
				MenuHelper.Theme.ButtonTheme,
				okPos,
				new Vector2(buttonWidth, buttonHeight),
				true,
				false,
				false,
				MenuHelper.Theme.ButtonTheme.buttonCols,
				Anchor.TopLeft
			);

			// Handle button presses
			if (uploadPressed && levelPassed && !_isUploading)
			{
				_ = UploadToLeaderboard();
			}

			if (leaderboardPressed)
			{
				string levelId = GetCurrentLevelId();
				LeaderboardPopup.Open(levelId);
			}

			if (testInputPressed && hasValidSelection)
			{
				ApplySelectedTestInputs();
			}

			if (okPressed)
			{
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
			}
		}

        static async System.Threading.Tasks.Task UploadToLeaderboard()
        {
            try
            {
                _isUploading = true;
                _uploadStatus = "Initializing...";
                Debug.Log("[Leaderboard] Starting upload process...");
                
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
                
                // Step 4: Upload score using LeaderboardService (has Editor bypasses)
                _uploadStatus = "Uploading score...";
                Debug.Log("[Leaderboard] Step 4: Uploading score...");
                
                using (var uploadCts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(25)))
                {
                    try
                    {
                        var uploadTask = LeaderboardService.SaveScoreAsync(levelId, score);
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
            // Simple scoring: lower is better (fewer steps = better score)
            // You can implement more complex scoring logic here
            return _rows.Count(r => r.Passed) * 10; // Example: 10 points per passed test
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

