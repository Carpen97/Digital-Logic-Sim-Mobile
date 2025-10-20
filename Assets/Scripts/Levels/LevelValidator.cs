using System;
using System.Collections.Generic;
using System.Linq;
using DLS.Levels.Host;
using UnityEngine;

namespace DLS.Levels
{
	public sealed class LevelValidator
	{
		private readonly ISimulationAdapter _sim;
		private readonly System.Random _random = new System.Random();
		private const int MAX_COMBINATIONAL_TESTS = 40;

		public LevelValidator(ISimulationAdapter sim) => _sim = sim;

		public ValidationReport Validate(LevelDefinition def)
		{
			var report = new ValidationReport
			{
				PassedAll = true,
				Failures = new List<CaseFail>(),
				ConstraintMessages = new List<string>()
			};

			if (def.isSequential)
			{
				// Validate sequences for sequential circuits
				ValidateSequences(def, report);
			}
			else
			{
				// Existing combinational validation
				ValidateCombinational(def, report);
			}

			return report;
		}

		private void ValidateCombinational(LevelDefinition def, ValidationReport report)
		{
			var vectors = def.testVectors;

			// Initialize AllTestResults for combinational levels
			report.AllTestResults = new List<TestResult>();

			// Randomly select up to MAX_COMBINATIONAL_TESTS vectors if there are too many
			LevelDefinition.TestVector[] vectorsToTest;
			if (vectors.Length > MAX_COMBINATIONAL_TESTS)
			{
				// Use LINQ OrderBy with random guid to shuffle, then take first MAX_COMBINATIONAL_TESTS
				vectorsToTest = vectors.OrderBy(x => _random.Next()).Take(MAX_COMBINATIONAL_TESTS).ToArray();
				UnityEngine.Debug.Log($"[LevelValidator] Testing {MAX_COMBINATIONAL_TESTS} randomly selected vectors out of {vectors.Length} available");
			}
			else
			{
				vectorsToTest = vectors;
			}

			// Correctness
			foreach (var tv in vectorsToTest)
			{
				var iv = BitVector.FromString(tv.inputs);
				_sim.ApplyInputs(iv);

				// ✅ ensure propagation before sampling outputs
				_sim.SettleWithin(2, out _);   // 1–2 steps is plenty for combinational logic

				var ov = _sim.ReadOutputs();
				var expected = BitVector.FromString(tv.expected);
				bool passed = (ov.Length == expected.Length && ov.Raw == expected.Raw);

				// Create test result for this vector
				var testResult = new TestResult
				{
					Inputs = tv.inputs,
					Expected = tv.expected,
					Actual = ov.ToString(),
					Passed = passed,
					SequenceName = "", // Not used for combinational
					StepIndex = 0, // Not used for combinational
					IsClockEdge = false // Not used for combinational
				};

				report.AllTestResults.Add(testResult);

				if (!passed)
				{
					report.PassedAll = false;
					report.Failures.Add(new CaseFail(tv.inputs, $"Expected {expected}, got {ov}"));
				}
			}
		}

		private void ValidateSequences(LevelDefinition def, ValidationReport report)
		{
			if (def.testSequences == null || def.testSequences.Length == 0)
			{
				report.PassedAll = false;
				report.Failures.Add(new CaseFail("", "Sequential level requires test sequences"));
				return;
			}

			// Initialize AllTestResults
			report.AllTestResults = new List<TestResult>();

			ResetCircuitStateEnhanced(def);

			foreach (var sequence in def.testSequences)
			{
				// Apply setup vectors if provided (for circuit initialization)
				if (sequence.setup != null && sequence.setup.Length > 0)
				{
					foreach (var setupInput in sequence.setup)
					{
						var setupIv = BitVector.FromString(setupInput);
						_sim.ApplyInputs(setupIv);
						_sim.SettleWithin(def.settleStepsPerVector, out _);
					}
				}

				// Apply the entire test sequence step by step
				for (int i = 0; i < sequence.vectors.Length; i++)
				{
					var vector = sequence.vectors[i];

					// ALWAYS populate AllTestResults from the original test sequence
					// This ensures expected values always come from JSON, not simulation
					var testResult = new TestResult
					{
						Inputs = vector.inputs,
						Expected = vector.expected,
						Actual = "",  // Will be populated by simulation
						Passed = false,  // Will be determined by simulation
						SequenceName = sequence.name,
						StepIndex = i,
						IsClockEdge = vector.isClockEdge
					};

					// Apply inputs for this step
					var iv = BitVector.FromString(vector.inputs);
					_sim.ApplyInputs(iv);

					// Handle clock edges specially
					if (vector.isClockEdge)
					{
						int settleSteps = vector.settleSteps > 0 ? vector.settleSteps : def.settleStepsPerVector;
						_sim.SettleWithin(settleSteps, out _);
					}
					else
					{
						int settleSteps = vector.settleSteps > 0 ? vector.settleSteps : 1;
						_sim.SettleWithin(settleSteps, out _);
					}

					// Read outputs after settling
					var ov = _sim.ReadOutputs();
					var expected = BitVector.FromString(vector.expected);
					bool passed = (ov.Length == expected.Length && ov.Raw == expected.Raw);

					// Update with actual simulation results
					testResult.Actual = ov.ToString();
					testResult.Passed = passed;

					// Add to failures if it failed
					if (!passed)
					{
						report.PassedAll = false;
						report.Failures.Add(new CaseFail(
							vector.inputs,
							$"Sequence '{sequence.name}': Expected {expected}, got {ov}"
						));
					}

					report.AllTestResults.Add(testResult);
				}
			}
		}

		private void ResetCircuitState(LevelDefinition def)
		{
			// Try to use reflection to call ResetCircuitState if available
			var resetMethod = _sim.GetType().GetMethod("ResetCircuitState");
			if (resetMethod != null)
			{
				resetMethod.Invoke(_sim, null);
			}
			else
			{
				// Fallback: reset all inputs to 0
				var zeroInputs = new BitVector(0, 0);
				_sim.ApplyInputs(zeroInputs);
				_sim.SettleWithin(5, out _);
			}
		}

		/// <summary>
		/// Enhanced reset for SR-latches and other sequential circuits.
		/// Ensures a consistent starting state before testing.
		/// </summary>
		private void ResetCircuitStateEnhanced(LevelDefinition def)
		{
			// Try enhanced SR-latch reset first
			var resetSRMethod = _sim.GetType().GetMethod("ResetSRLatchState");
			if (resetSRMethod != null)
			{
				resetSRMethod.Invoke(_sim, null);
				return;
			}

			// Fall back to standard reset
			ResetCircuitState(def);
		}

	}

	public sealed class ValidationReport
	{
		public bool PassedAll;
		public List<CaseFail> Failures;
		public List<string> ConstraintMessages;
		public int Stars;
		public int LastPartsCount;

		// NEW: Store all test results
		public List<TestResult> AllTestResults;
	}

	public sealed class TestResult
	{
		public string Inputs;
		public string Expected;
		public string Actual;
		public bool Passed;
		public string SequenceName;
		public int StepIndex;
		public bool IsClockEdge;
	}

	public sealed class CaseFail
	{
		public readonly string Inputs;
		public readonly string Message;
		public CaseFail(string inputs, string message) { Inputs = inputs; Message = message; }
	}
}
