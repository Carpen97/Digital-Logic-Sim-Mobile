using System.Collections.Generic;
using DLS.Description;
using DLS.Game;
using DLS.Levels;
using DLS.Levels.Host;
using DLS.SaveSystem;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;
using static DLS.Graphics.DrawSettings;

namespace DLS.Graphics
{
	/// <summary>
	/// Overlay for manual level recording.
	/// Combinational: Record (add one test case), Apply (replay), Done.
	/// Sequential: Record toggle (start/stop), Step Forward only during recording (each step recorded), Apply (replay sequence), Done.
	/// </summary>
	public static class LevelRecordingOverlay
	{
		const float BannerHeight = 5.5f;
		const float ButtonHeight = 2.6f;
		const float ButtonWidth = 5.5f;
		const float ButtonSpacing = 0.4f;

		static bool _active;
		static bool _positionAtBottom;
		static bool _sequentialMode;
		static string _levelName;
		static ChipDescription _cachedChipDesc;
		static bool _simWasPaused;

		// Combinational: flat list of test vectors
		static List<LevelDefinition.TestVector> _recordedVectors = new();

		// Sequential: list of test sequences; each sequence = one test case = timeline of (input, output) steps
		static List<LevelDefinition.TestSequence> _recordedSequences = new();
		static List<LevelDefinition.TestVector> _currentSequence = new();  // Building during recording
		static bool _isRecording;  // Sequential: true = capturing steps, false = idle

		static MobileSimulationAdapter _adapter;
		static int _selectedApplyIndex = -1;

		// Sequential: when Apply is pressed, we enter "review" mode - step through the applied sequence
		static bool _isReviewingSequence;
		static int _reviewStepIndex;  // 0-based; 0 = after first step applied

		public static bool IsActive => _active;

		public static void StartRecording(string levelName, bool sequential)
		{
			var project = Project.ActiveProject;
			if (project == null || project.chipViewStack.Count != 1) return;

			_levelName = levelName;
			_cachedChipDesc = DescriptionCreator.CreateChipDescription(project.ViewedChip);
			_recordedVectors.Clear();
			_recordedSequences.Clear();
			_currentSequence.Clear();
			_sequentialMode = sequential;
			_isRecording = false;
			_selectedApplyIndex = -1;
			_isReviewingSequence = false;
			_adapter = new MobileSimulationAdapter();

			_simWasPaused = project.simPaused;
			// Don't pause on open - only pause when recording or reviewing
			_active = true;
		}

		public static void StopRecording()
		{
			_active = false;
			if (Project.ActiveProject != null)
				Project.ActiveProject.description.Prefs_SimPaused = _simWasPaused;
		}

		public static void DrawOverlay()
		{
			if (!_active) return;

			Vector2 size = new(Seb.Vis.UI.UI.Width, BannerHeight);
			Vector2 panelPos;
			Anchor panelAnchor;
			if (_positionAtBottom)
			{
				panelPos = new Vector2(0, BottomBarUI.barHeight);
				panelAnchor = Anchor.BottomLeft;
			}
			else
			{
				panelPos = Seb.Vis.UI.UI.TopLeft;
				panelAnchor = Anchor.TopLeft;
			}
			Seb.Vis.UI.UI.DrawPanel(panelPos, size, ActiveUITheme.InfoBarCol, panelAnchor);
			Bounds2D bounds = Seb.Vis.UI.UI.PrevBounds;

			float x = bounds.Min.x + 1f;
			float y = bounds.Centre.y;
			Color dimCol = new Color(0.85f, 0.85f, 0.85f);
			Color titleCol = new Color(0.5f, 1f, 0.6f);
			const float lineOffset = 0.75f;

			// "Record Level" on two lines (top line first, then bottom)
			Seb.Vis.UI.UI.DrawText("Record", ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular * 0.95f, new Vector2(x, y + lineOffset), Anchor.TextCentreLeft, titleCol);
			Seb.Vis.UI.UI.DrawText("Level", ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular * 0.95f, new Vector2(x, y - lineOffset), Anchor.TextCentreLeft, titleCol);
			x += 6f;

			// Level name (separate, centered on main line)
			const int maxNameLen = 18;
			string displayName = _levelName.Length > maxNameLen ? _levelName.Substring(0, maxNameLen - 3) + "..." : _levelName;
			Seb.Vis.UI.UI.DrawText(displayName, ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular * 0.9f, new Vector2(x, y), Anchor.TextCentreLeft, titleCol);
			x += 16f;

			// Mode
			Seb.Vis.UI.UI.DrawText(_sequentialMode ? "Sequential" : "Combinational", ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular * 0.9f, new Vector2(x, y), Anchor.TextCentreLeft, dimCol);
			x += 12f;

			int recordedCount = _sequentialMode ? _recordedSequences.Count : _recordedVectors.Count;
			int stepCount = _sequentialMode ? _currentSequence.Count : 0;

			// Step count (sequential, during recording)
			if (_sequentialMode && _isRecording)
			{
				Seb.Vis.UI.UI.DrawText($"{stepCount}", ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular * 0.9f, new Vector2(x, y), Anchor.TextCentreLeft, dimCol);
				x += 6f;
			}

			// Recorded count (only when no recordings yet; otherwise navigator shows "X / Y")
			if (recordedCount == 0)
			{
				Seb.Vis.UI.UI.DrawText("Recorded: 0", ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular * 0.9f, new Vector2(x, y), Anchor.TextCentreLeft, dimCol);
				x += 10f;
			}

			// Buttons
			Vector2 btnPos = new Vector2(x, y);
			Vector2 btnSize = new Vector2(ButtonWidth, ButtonHeight);
			float btnStep = btnSize.x + ButtonSpacing;
			const float groupGap = 2f; // Spacing between button groups

			if (_sequentialMode && !_isReviewingSequence)
			{
				// Step Forward: only during recording (hidden when reviewing - we show review step controls instead)
				bool stepFwdOk = _isRecording;
				if (Seb.Vis.UI.UI.Button("Step ▶", ActiveUITheme.ButtonTheme, btnPos, btnSize, stepFwdOk, false, false, ActiveUITheme.ButtonTheme.buttonCols, Anchor.CentreLeft))
				{
					if (stepFwdOk) OnStepForward();
				}
				btnPos.x += btnStep + groupGap;
			}

			// Record: Combinational = add one; Sequential = toggle start/stop
			string recordLabel = _sequentialMode
				? (_isRecording ? "Stop" : "Start")
				: "Record";
			if (Seb.Vis.UI.UI.Button(recordLabel, ActiveUITheme.ButtonTheme, btnPos, btnSize, true, false, false, ActiveUITheme.ButtonTheme.buttonCols, Anchor.CentreLeft))
			{
				OnRecord();
			}
			btnPos.x += btnStep + groupGap;

			// Sequential + reviewing: show step-through controls (◀ Step | Step X/Y | Step ▶)
			// Sequential + recording: hide navigator/Apply (focus on recording)
			// Otherwise: test case navigator + Apply
			bool hasRecorded = recordedCount > 0;
			bool showApplyAndNav = !(_sequentialMode && _isReviewingSequence) && !_isRecording;

			if (_sequentialMode && _isReviewingSequence)
			{
				int idx = _selectedApplyIndex >= 0 ? _selectedApplyIndex : 0;
				var seq = _recordedSequences[idx];
				int totalSteps = seq.vectors?.Length ?? 0;
				int displayStep = _reviewStepIndex + 1;

				float stepBtnWidth = 6f;   // Wider buttons to avoid text overlap
				float dispWidth = 6.5f;   // Space for "1 out of 4" text
				Vector2 stepBtnSize = new(stepBtnWidth, ButtonHeight * 0.9f);

				bool canStepBack = _reviewStepIndex > 0;
				if (Seb.Vis.UI.UI.Button("◀ Step", ActiveUITheme.ButtonTheme, btnPos, stepBtnSize, canStepBack, false, false, ActiveUITheme.ButtonTheme.buttonCols, Anchor.CentreLeft))
				{
					if (canStepBack) OnReviewStepBack();
				}
				btnPos.x += stepBtnWidth + 0.3f;

				Seb.Vis.UI.UI.DrawText($"{displayStep} / {totalSteps}", ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular * 0.9f, btnPos + Vector2.right * (dispWidth / 2), Anchor.TextCentre, dimCol);
				btnPos.x += dispWidth + 0.3f;

				bool canStepFwd = _reviewStepIndex < totalSteps - 1;
				if (Seb.Vis.UI.UI.Button("Step ▶", ActiveUITheme.ButtonTheme, btnPos, stepBtnSize, canStepFwd, false, false, ActiveUITheme.ButtonTheme.buttonCols, Anchor.CentreLeft))
				{
					if (canStepFwd) OnReviewStepForward();
				}
				btnPos.x += stepBtnWidth + groupGap;

				// Exit preview - return to Apply/navigator view
				if (Seb.Vis.UI.UI.Button("Exit", ActiveUITheme.ButtonTheme, btnPos, new Vector2(5f, ButtonHeight * 0.9f), true, false, false, ActiveUITheme.ButtonTheme.buttonCols, Anchor.CentreLeft))
				{
					ExitReviewMode();
				}
				btnPos.x += 5f + groupGap;
			}
			else if (showApplyAndNav)
			{
				// Test case navigator: < [current / total] >
				if (hasRecorded)
				{
					int current = _selectedApplyIndex >= 0 ? _selectedApplyIndex : 0;
					_selectedApplyIndex = current;

					Vector2 navBtnSize = new(2.2f, ButtonHeight * 0.9f);
					Vector2 dispSize = new(4f, ButtonHeight * 0.9f);

					if (Seb.Vis.UI.UI.Button("<", ActiveUITheme.ButtonTheme, btnPos, navBtnSize, recordedCount > 1, false, false, ActiveUITheme.ButtonTheme.buttonCols, Anchor.CentreLeft))
					{
						_selectedApplyIndex = (current - 1 + recordedCount) % recordedCount;
					}
					btnPos.x += navBtnSize.x + 0.15f;

					Seb.Vis.UI.UI.DrawText($"{_selectedApplyIndex + 1} / {recordedCount}", ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular * 0.9f, btnPos + Vector2.right * (dispSize.x / 2), Anchor.TextCentre, dimCol);
					btnPos.x += dispSize.x + 0.15f;

					if (Seb.Vis.UI.UI.Button(">", ActiveUITheme.ButtonTheme, btnPos, navBtnSize, recordedCount > 1, false, false, ActiveUITheme.ButtonTheme.buttonCols, Anchor.CentreLeft))
					{
						_selectedApplyIndex = (current + 1) % recordedCount;
					}
					btnPos.x += navBtnSize.x + groupGap;
				}

				// Length of selected test case + Apply button (combinational always; sequential when not reviewing)
				int selectedSteps = 0;
				if (hasRecorded)
				{
					int idx = _selectedApplyIndex >= 0 ? _selectedApplyIndex : 0;
					selectedSteps = _sequentialMode
						? (_recordedSequences[idx].vectors?.Length ?? 0)
						: 1;
				}
				if (hasRecorded && selectedSteps > 0)
				{
					Seb.Vis.UI.UI.DrawText($"{selectedSteps}", ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular * 0.9f, new Vector2(btnPos.x, y), Anchor.TextCentreLeft, dimCol);
					btnPos.x += 6f;
				}

				if (Seb.Vis.UI.UI.Button("Apply", ActiveUITheme.ButtonTheme, btnPos, btnSize, hasRecorded, false, false, ActiveUITheme.ButtonTheme.buttonCols, Anchor.CentreLeft))
				{
					if (hasRecorded) OnApply();
				}
				btnPos.x += btnStep;
			}

			// Position / Cancel / Done
			float rightBtnWidth = 6.5f;
			float gap = 0.3f;
			Vector2 rightBtnSize = new(rightBtnWidth, ButtonHeight);

			btnPos.x = bounds.Max.x;
			if (Seb.Vis.UI.UI.Button("Done", ActiveUITheme.ButtonTheme, btnPos, rightBtnSize, true, false, false, ActiveUITheme.ButtonTheme.buttonCols, Anchor.CentreRight))
			{
				OnDone();
			}
			btnPos.x -= rightBtnWidth + gap;
			if (Seb.Vis.UI.UI.Button("Cancel", ActiveUITheme.ButtonTheme, btnPos, rightBtnSize, true, false, false, ActiveUITheme.ButtonTheme.buttonCols, Anchor.CentreRight))
			{
				StopRecording();
			}
			btnPos.x -= rightBtnWidth + gap;
			if (Seb.Vis.UI.UI.Button(_positionAtBottom ? "↑ Top" : "↓ Bottom", ActiveUITheme.ButtonTheme, btnPos, rightBtnSize, true, false, false, ActiveUITheme.ButtonTheme.buttonCols, Anchor.CentreRight))
			{
				_positionAtBottom = !_positionAtBottom;
			}
		}

		static void OnStepForward()
		{
			if (!_isRecording) return;
			var inputs = _adapter.ReadInputs();
			_adapter.SettleWithin(1, out _);
			var outputs = _adapter.ReadOutputs();
			_currentSequence.Add(new LevelDefinition.TestVector
			{
				inputs = inputs.ToString(),
				expected = outputs.ToString(),
				settleSteps = 1,
				isClockEdge = false
			});
		}

		static void OnRecord()
		{
			if (_sequentialMode)
			{
				if (_isRecording)
				{
					// Stop recording - save current sequence, unpause sim
					if (_currentSequence.Count > 0)
					{
						_recordedSequences.Add(new LevelDefinition.TestSequence
						{
							name = $"Sequence {_recordedSequences.Count + 1}",
							vectors = _currentSequence.ToArray()
						});
					}
					_currentSequence.Clear();
					_isRecording = false;
					SetSimPaused(_simWasPaused);
				}
				else
				{
					// Start recording - pause sim for controlled stepping
					_currentSequence.Clear();
					_isRecording = true;
					_isReviewingSequence = false; // Exit review if we were in it
					SetSimPaused(true);
				}
			}
			else
			{
				// Combinational: record single test case
				var inputs = _adapter.ReadInputs();
				_adapter.SettleWithin(1, out _);
				var outputs = _adapter.ReadOutputs();
				_recordedVectors.Add(new LevelDefinition.TestVector
				{
					inputs = inputs.ToString(),
					expected = outputs.ToString(),
					settleSteps = 1,
					isClockEdge = false
				});
			}
		}

		static void OnApply()
		{
			int count = _sequentialMode ? _recordedSequences.Count : _recordedVectors.Count;
			if (count == 0) return;
			int idx = _selectedApplyIndex >= 0 ? _selectedApplyIndex : 0;
			_selectedApplyIndex = idx;

			if (_sequentialMode)
			{
				// Enter review mode: apply first step only, pause sim, show step-through UI
				var seq = _recordedSequences[idx];
				if (seq.vectors == null || seq.vectors.Length == 0) return;
				var v = seq.vectors[0];
				var iv = BitVector.FromString(v.inputs);
				_adapter.ApplyInputs(iv);
				_adapter.SettleWithin(v.settleSteps > 0 ? v.settleSteps : 1, out _);
				_isReviewingSequence = true;
				_reviewStepIndex = 0;
				SetSimPaused(true);
			}
			else
			{
				ApplyTestCase(idx);
			}
		}

		static void ExitReviewMode()
		{
			_isReviewingSequence = false;
			SetSimPaused(_simWasPaused);
		}

		static void SetSimPaused(bool paused)
		{
			var project = Project.ActiveProject;
			if (project != null) project.description.Prefs_SimPaused = paused;
		}

		static void OnReviewStepBack()
		{
			if (_reviewStepIndex <= 0) return;
			_reviewStepIndex--;
			// Re-apply from start up to current step
			int idx = _selectedApplyIndex >= 0 ? _selectedApplyIndex : 0;
			var seq = _recordedSequences[idx];
			if (seq.vectors == null) return;
			for (int i = 0; i <= _reviewStepIndex; i++)
			{
				var v = seq.vectors[i];
				var iv = BitVector.FromString(v.inputs);
				_adapter.ApplyInputs(iv);
				_adapter.SettleWithin(v.settleSteps > 0 ? v.settleSteps : 1, out _);
			}
		}

		static void OnReviewStepForward()
		{
			int idx = _selectedApplyIndex >= 0 ? _selectedApplyIndex : 0;
			var seq = _recordedSequences[idx];
			if (seq.vectors == null || _reviewStepIndex >= seq.vectors.Length - 1) return;
			_reviewStepIndex++;
			var v = seq.vectors[_reviewStepIndex];
			var iv = BitVector.FromString(v.inputs);
			_adapter.ApplyInputs(iv);
			_adapter.SettleWithin(v.settleSteps > 0 ? v.settleSteps : 1, out _);
		}

		static void ApplyTestCase(int index)
		{
			if (_sequentialMode)
			{
				var seq = _recordedSequences[index];
				if (seq.vectors == null || seq.vectors.Length == 0) return;
				foreach (var v in seq.vectors)
				{
					var iv = BitVector.FromString(v.inputs);
					_adapter.ApplyInputs(iv);
					_adapter.SettleWithin(v.settleSteps > 0 ? v.settleSteps : 1, out _);
				}
			}
			else
			{
				var v = _recordedVectors[index];
				var iv = BitVector.FromString(v.inputs);
				_adapter.ApplyInputs(iv);
				_adapter.SettleWithin(v.settleSteps > 0 ? v.settleSteps : 1, out _);
			}
		}

		static void OnDone()
		{
			if (_sequentialMode)
			{
				if (_recordedSequences.Count == 0)
				{
					SimpleMessagePopup.Open("Record at least one test sequence. Press Start, step forward, then Stop.");
					return;
				}
				if (_isRecording)
				{
					SimpleMessagePopup.Open("Stop recording before saving (press Stop).");
					return;
				}
			}
			else
			{
				if (_recordedVectors.Count == 0)
				{
					SimpleMessagePopup.Open("Record at least one test case before saving.");
					return;
				}
			}

			var levelDef = _sequentialMode
				? CreateLevelMenu.BuildLevelDefinitionFromSequentialRecording(_levelName, _cachedChipDesc, new List<LevelDefinition.TestSequence>(_recordedSequences))
				: CreateLevelMenu.BuildLevelDefinitionFromRecording(_levelName, _cachedChipDesc, new List<LevelDefinition.TestVector>(_recordedVectors));

			if (levelDef != null)
			{
				string projectName = Project.ActiveProject.description.ProjectName;
				UserLevelStorage.SaveUserLevel(levelDef, projectName);
				StopRecording();
				int n = _sequentialMode ? _recordedSequences.Count : _recordedVectors.Count;
				SimpleMessagePopup.Open($"Level '{_levelName}' saved with {n} test cases.");
			}
			else
			{
				SimpleMessagePopup.Open("Failed to build level.");
			}
		}
	}
}
