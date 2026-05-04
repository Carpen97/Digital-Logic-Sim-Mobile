using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DLS.Levels
{
	/// <summary>
	/// V2 Level Definition Format - Cleaner, more flexible structure.
	/// </summary>
	[Serializable]
	public sealed class LevelDefinitionV2
	{
		public string id;
		public string name;
		public string chapterId;
		public string description;
		
		public List<PinData> inputStructure;
		public List<PinData> outputStructure;
		
		// Type of circuit: "combinational" (default) or "sequential"
		public string type = "combinational";
		
		// Setup sequence for sequential circuits (optional)
		// Format: ["XXXX_X", "XXXX_X"] - same format as tests, applied before testing
		public string[] setup;
		
		// Tests can be either:
		// - A string path to a binary .tvec file (e.g., "GeneratedTestVectors/lvl.and.1")
		// - An array of test strings in format: "X_XXXX_XXXX|XXXX"
		//   where _ separates pins, | separates inputs from outputs
		//   X is 0 or 1
	public string testsBinaryPath;      // Path to .tvec file
	public string[] testsInline;        // Inline test vectors
	public int settleSteps = 8;          // Number of simulation steps to wait for circuit to settle (especially important for sequential circuits)

	/// <summary>
	/// Convert this V2 definition to V1 format for backward compatibility.
	/// </summary>
	public LevelDefinition ToV1()
		{
			var v1 = new LevelDefinition
			{
				id = this.id,
				chapterId = this.chapterId,
				name = this.name,
				description = this.description
			};

			// Convert input structure
			if (inputStructure != null && inputStructure.Count > 0)
			{
				v1.inputCount = inputStructure.Count;
				v1.inputBitCounts = inputStructure.Select(p => p.nBits).ToArray();
				v1.inputPinLabels = inputStructure.Select(p => new LevelDefinition.PinLabel
				{
					name = p.name,
					abbr = p.GetAbbr()
				}).ToArray();
			}
			else
			{
				v1.inputCount = 0;
				v1.inputBitCounts = Array.Empty<int>();
				v1.inputPinLabels = Array.Empty<LevelDefinition.PinLabel>();
			}

			// Convert output structure
			if (outputStructure != null && outputStructure.Count > 0)
			{
				v1.outputCount = outputStructure.Count;
				v1.outputBitCounts = outputStructure.Select(p => p.nBits).ToArray();
				v1.outputPinLabels = outputStructure.Select(p => new LevelDefinition.PinLabel
				{
					name = p.name,
					abbr = p.GetAbbr()
				}).ToArray();
			}
			else
			{
				v1.outputCount = 0;
				v1.outputBitCounts = Array.Empty<int>();
				v1.outputPinLabels = Array.Empty<LevelDefinition.PinLabel>();
			}

		// Determine if sequential
		bool isSequential = type == "sequential";
		v1.isSequential = isSequential;
		
		// Set settle steps for sequential circuits
		if (isSequential)
		{
			v1.settleStepsPerVector = this.settleSteps;
		}

		// Convert tests
		if (isSequential)
			{
				// Sequential circuit - create test sequences
				var sequence = new LevelDefinition.TestSequence
				{
					name = "Main Sequence"
				};

				// Parse setup if provided
				if (setup != null && setup.Length > 0)
				{
					sequence.setup = new string[setup.Length];
					for (int i = 0; i < setup.Length; i++)
					{
						// Extract only the input part (before |)
						string setupStr = setup[i];
						int pipeIndex = setupStr.IndexOf('|');
						if (pipeIndex >= 0)
						{
							setupStr = setupStr.Substring(0, pipeIndex);
						}
						// Remove underscores
						sequence.setup[i] = setupStr.Replace("_", "").Replace(" ", "");
					}
				}

				// Parse test vectors
				if (!string.IsNullOrEmpty(testsBinaryPath))
				{
					// Binary files not yet supported for sequential - fall back to combinational
					Debug.LogWarning($"[LevelV2] Sequential level '{id}' uses binary test file - not yet supported, treating as combinational");
					v1.isSequential = false;
					v1.testVectorsFile = testsBinaryPath;
				}
				else if (testsInline != null && testsInline.Length > 0)
				{
					sequence.vectors = ParseInlineTests(testsInline);
					v1.testSequences = new LevelDefinition.TestSequence[] { sequence };
				}
				else
				{
					v1.testSequences = Array.Empty<LevelDefinition.TestSequence>();
				}
			}
			else
			{
				// Combinational circuit - use regular test vectors
				if (!string.IsNullOrEmpty(testsBinaryPath))
				{
					v1.testVectorsFile = testsBinaryPath;
				}
				else if (testsInline != null && testsInline.Length > 0)
				{
					v1.testVectors = ParseInlineTests(testsInline);
				}
				else
				{
					v1.testVectors = Array.Empty<LevelDefinition.TestVector>();
				}
			}

			return v1;
		}

	/// <summary>
	/// Parse inline test vectors in format: "X_XXXX_XXXX|XXXX"
	/// </summary>
	private LevelDefinition.TestVector[] ParseInlineTests(string[] tests)
	{
		var vectors = new List<LevelDefinition.TestVector>();
		string previousInputs = "";
		bool hasClockInput = HasClockLikeInput();

		foreach (var test in tests)
		{
			if (string.IsNullOrWhiteSpace(test)) continue;

			try
			{
				// Split by | to separate inputs and outputs
				var parts = test.Split('|');
				if (parts.Length != 2)
				{
					Debug.LogWarning($"[LevelV2] Invalid test format (missing |): {test}");
					continue;
				}

				// Remove underscores and whitespace from inputs and outputs
				string inputs = parts[0].Replace("_", "").Replace(" ", "");
				string expected = parts[1].Replace("_", "").Replace(" ", "");

				// Validate that inputs and outputs are only 0s and 1s
				if (!IsValidBitString(inputs) || !IsValidBitString(expected))
				{
					Debug.LogWarning($"[LevelV2] Invalid bit string (non-binary chars): {test}");
					continue;
				}

				// Detect clock edges: look for 0->1 transition on first input (clock)
				bool isClockEdge = false;
				if (type == "sequential" && hasClockInput && !string.IsNullOrEmpty(previousInputs) && inputs.Length > 0 && previousInputs.Length > 0)
				{
					// Check if first input (clock) transitions from 0 to 1
					isClockEdge = (previousInputs[0] == '0' && inputs[0] == '1');
				}

				int settleForVector = 0;
				if (type == "sequential")
				{
					// Clockless sequential levels (e.g. SR latch) need full settle time every vector.
					settleForVector = hasClockInput
						? (isClockEdge ? this.settleSteps : 1)
						: this.settleSteps;
				}

				vectors.Add(new LevelDefinition.TestVector
				{
					inputs = inputs,
					expected = expected,
					settleSteps = settleForVector,
					isClockEdge = isClockEdge
				});
				
				previousInputs = inputs;
			}
			catch (Exception ex)
			{
				Debug.LogWarning($"[LevelV2] Failed to parse test '{test}': {ex.Message}");
			}
		}

		return vectors.ToArray();
	}

	private bool HasClockLikeInput()
	{
		if (inputStructure == null) return false;
		for (int i = 0; i < inputStructure.Count; i++)
		{
			string name = inputStructure[i].name ?? string.Empty;
			string abbr = inputStructure[i].abbr ?? string.Empty;
			if (name.Equals("Clock", StringComparison.OrdinalIgnoreCase) ||
			    name.Equals("Clk", StringComparison.OrdinalIgnoreCase) ||
			    abbr.Equals("C", StringComparison.OrdinalIgnoreCase) ||
			    abbr.Equals("CLK", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

		/// <summary>
		/// Check if a string contains only 0s and 1s.
		/// </summary>
		private bool IsValidBitString(string s)
		{
			foreach (char c in s)
			{
				if (c != '0' && c != '1') return false;
			}
			return true;
		}

		/// <summary>
		/// Validate that this V2 definition is well-formed.
		/// </summary>
		public bool Validate(out string error)
		{
			if (string.IsNullOrEmpty(id))
			{
				error = "Missing id";
				return false;
			}

			if (string.IsNullOrEmpty(name))
			{
				error = "Missing name";
				return false;
			}

			if (inputStructure == null || inputStructure.Count == 0)
			{
				error = "Missing inputStructure";
				return false;
			}

			if (outputStructure == null || outputStructure.Count == 0)
			{
				error = "Missing outputStructure";
				return false;
			}

			// Validate type field
			if (!string.IsNullOrEmpty(type) && type != "combinational" && type != "sequential")
			{
				error = $"Invalid type '{type}' - must be 'combinational' or 'sequential'";
				return false;
			}

			// Check that all pins have names
			foreach (var pin in inputStructure)
			{
				if (string.IsNullOrEmpty(pin.name))
				{
					error = "Input pin missing name";
					return false;
				}
			}

			foreach (var pin in outputStructure)
			{
				if (string.IsNullOrEmpty(pin.name))
				{
					error = "Output pin missing name";
					return false;
				}
			}

			// Check that we have either binary path or inline tests
			bool hasBinaryTests = !string.IsNullOrEmpty(testsBinaryPath);
			bool hasInlineTests = testsInline != null && testsInline.Length > 0;

			if (!hasBinaryTests && !hasInlineTests)
			{
				error = "No tests specified (need either testsBinaryPath or testsInline)";
				return false;
			}

			// Validate setup format if present
			if (setup != null && setup.Length > 0)
			{
				int totalInputBits = inputStructure.Sum(p => p.nBits);
				
				foreach (var setupStr in setup)
				{
					if (string.IsNullOrWhiteSpace(setupStr)) continue;

					// Setup can have format "XXX|YYY" or just "XXX"
					// We only care about the input part (before |)
					string inputPart = setupStr;
					int pipeIndex = setupStr.IndexOf('|');
					if (pipeIndex >= 0)
					{
						inputPart = setupStr.Substring(0, pipeIndex);
					}

					string inputs = inputPart.Replace("_", "").Replace(" ", "");
					
					if (inputs.Length != totalInputBits)
					{
						error = $"Setup input bit count mismatch. Expected {totalInputBits}, got {inputs.Length} in setup: {setupStr}";
						return false;
					}
				}
			}

			// Validate inline tests format if present
			if (hasInlineTests)
			{
				int totalInputBits = inputStructure.Sum(p => p.nBits);
				int totalOutputBits = outputStructure.Sum(p => p.nBits);

				foreach (var test in testsInline)
				{
					if (string.IsNullOrWhiteSpace(test)) continue;

					var parts = test.Split('|');
					if (parts.Length != 2)
					{
						error = $"Invalid test format (missing |): {test}";
						return false;
					}

					string inputs = parts[0].Replace("_", "").Replace(" ", "");
					string expected = parts[1].Replace("_", "").Replace(" ", "");

					if (inputs.Length != totalInputBits)
					{
						error = $"Input bit count mismatch. Expected {totalInputBits}, got {inputs.Length} in test: {test}";
						return false;
					}

					if (expected.Length != totalOutputBits)
					{
						error = $"Output bit count mismatch. Expected {totalOutputBits}, got {expected.Length} in test: {test}";
						return false;
					}
				}
			}

			error = null;
			return true;
		}
	}
}

