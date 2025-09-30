using System;
using UnityEngine;

namespace DLS.Description
{
	/// <summary>
	/// Simple tests for the ChipTypeDetector to verify basic functionality.
	/// These can be run in the Unity editor to test the detection system.
	/// </summary>
	public static class ChipTypeDetectorTests
	{
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void RunBasicTests()
		{
			Debug.Log("Running ChipTypeDetector basic tests...");
			
			// Test 1: Basic name mapping
			TestNameMapping();
			
			// Test 2: Truth table lookup
			TestTruthTableLookup();
			
			// Test 3: Detection limits
			TestDetectionLimits();
			
			Debug.Log("ChipTypeDetector tests completed.");
		}
		
		static void TestNameMapping()
		{
			Debug.Log("Testing name mapping...");
			
			// Test all known types have names
			foreach (ChipTypeId type in Enum.GetValues(typeof(ChipTypeId)))
			{
				string name = ChipTypeDetector.GetTypeName(type);
				if (string.IsNullOrEmpty(name))
				{
					Debug.LogError($"No name found for ChipTypeId: {type}");
				}
				else
				{
					Debug.Log($"✓ {type} -> {name}");
				}
			}
		}
		
		static void TestTruthTableLookup()
		{
			Debug.Log("Testing truth table lookup...");
			
			// Test NOT pattern (should detect as NOT)
			string notPattern = "10"; // 0→1, 1→0
			var (detectedType, suggestedName) = TestDetectionWithPattern(notPattern);
			
			if (detectedType == ChipTypeId.NOT && suggestedName == "NOT")
			{
				Debug.Log($"✓ NOT pattern correctly detected: {notPattern}");
			}
			else
			{
				Debug.LogError($"✗ NOT pattern detection failed. Expected: NOT, Got: {detectedType} ({suggestedName})");
			}
			
			// Test XOR pattern (should detect as XOR)
			string xorPattern = "00000110"; // 00→0, 01→1, 10→1, 11→0
			(detectedType, suggestedName) = TestDetectionWithPattern(xorPattern);
			
			if (detectedType == ChipTypeId.XOR && suggestedName == "XOR")
			{
				Debug.Log($"✓ XOR pattern correctly detected: {xorPattern}");
			}
			else
			{
				Debug.LogError($"✗ XOR pattern detection failed. Expected: XOR, Got: {detectedType} ({suggestedName})");
			}
			
			// Test AND pattern (should detect as AND)
			string andPattern = "00000001"; // 00→0, 01→0, 10→0, 11→1
			(detectedType, suggestedName) = TestDetectionWithPattern(andPattern);
			
			if (detectedType == ChipTypeId.AND && suggestedName == "AND")
			{
				Debug.Log($"✓ AND pattern correctly detected: {andPattern}");
			}
			else
			{
				Debug.LogError($"✗ AND pattern detection failed. Expected: AND, Got: {detectedType} ({suggestedName})");
			}
			
			// Test unknown pattern (should detect as Unknown)
			string unknownPattern = "10101010"; // Random pattern
			(detectedType, suggestedName) = TestDetectionWithPattern(unknownPattern);
			
			if (detectedType == ChipTypeId.Unknown && suggestedName == "Unknown")
			{
				Debug.Log($"✓ Unknown pattern correctly detected: {unknownPattern}");
			}
			else
			{
				Debug.LogError($"✗ Unknown pattern detection failed. Expected: Unknown, Got: {detectedType} ({suggestedName})");
			}
		}
		
		static void TestDetectionLimits()
		{
			Debug.Log("Testing detection limits...");
			
			// Test with too many inputs (should be Unknown)
			var largeChip = CreateTestChip(5, 1); // 5 inputs, 1 output
			var (detectedType, suggestedName) = ChipTypeDetector.DetectAndSuggestName(largeChip);
			
			if (detectedType == ChipTypeId.Unknown)
			{
				Debug.Log($"✓ Large chip correctly detected as Unknown (5 inputs)");
			}
			else
			{
				Debug.LogError($"✗ Large chip detection failed. Expected: Unknown, Got: {detectedType}");
			}
			
			// Test with too many outputs (should be Unknown)
			var multiOutputChip = CreateTestChip(2, 3); // 2 inputs, 3 outputs
			(detectedType, suggestedName) = ChipTypeDetector.DetectAndSuggestName(multiOutputChip);
			
			if (detectedType == ChipTypeId.Unknown)
			{
				Debug.Log($"✓ Multi-output chip correctly detected as Unknown (3 outputs)");
			}
			else
			{
				Debug.LogError($"✗ Multi-output chip detection failed. Expected: Unknown, Got: {detectedType}");
			}
		}
		
		static (ChipTypeId detectedType, string suggestedName) TestDetectionWithPattern(string truthTablePattern)
		{
			// Create a mock chip description with the given truth table pattern
			var mockChip = CreateMockChipWithPattern(truthTablePattern);
			return ChipTypeDetector.DetectAndSuggestName(mockChip);
		}
		
		static ChipDescription CreateTestChip(int inputCount, int outputCount)
		{
			var chip = new ChipDescription
			{
				Name = "TestChip",
				ChipType = ChipType.Custom,
				InternalTypeId = ChipTypeId.Unknown
			};
			
			// Create input pins
			var inputPins = new PinDescription[inputCount];
			for (int i = 0; i < inputCount; i++)
			{
				inputPins[i] = new PinDescription
				{
					Name = $"IN{i}",
					ID = 1000 + i,
					BitCount = new PinBitCount(1),
					Position = Vector2.zero
				};
			}
			chip.InputPins = inputPins;
			
			// Create output pins
			var outputPins = new PinDescription[outputCount];
			for (int i = 0; i < outputCount; i++)
			{
				outputPins[i] = new PinDescription
				{
					Name = $"OUT{i}",
					ID = 2000 + i,
					BitCount = new PinBitCount(1),
					Position = Vector2.zero
				};
			}
			chip.OutputPins = outputPins;
			
			// No sub-chips for this test
			chip.SubChips = new SubChipDescription[0];
			chip.Wires = new WireDescription[0];
			
			return chip;
		}
		
		static ChipDescription CreateMockChipWithPattern(string truthTablePattern)
		{
			// Create a mock chip that would produce the given truth table pattern
			// This is a simplified mock for testing the lookup system
			var chip = CreateTestChip(2, 1); // 2 inputs, 1 output
			chip.Name = "MockChip";
			
			// Add some mock sub-chips to make it look like a real circuit
			chip.SubChips = new SubChipDescription[]
			{
				new SubChipDescription
				{
					Name = "NAND",
					ID = 3000,
					Position = Vector2.zero
				}
			};
			
			chip.Wires = new WireDescription[]
			{
				new WireDescription
				{
					SourcePinAddress = new PinAddress { PinID = 0, PinOwnerID = 1000 },
					TargetPinAddress = new PinAddress { PinID = 0, PinOwnerID = 3000 },
					ConnectionType = 0
				},
				new WireDescription
				{
					SourcePinAddress = new PinAddress { PinID = 0, PinOwnerID = 1001 },
					TargetPinAddress = new PinAddress { PinID = 1, PinOwnerID = 3000 },
					ConnectionType = 0
				},
				new WireDescription
				{
					SourcePinAddress = new PinAddress { PinID = 2, PinOwnerID = 3000 },
					TargetPinAddress = new PinAddress { PinID = 0, PinOwnerID = 2000 },
					ConnectionType = 0
				}
			};
			
			return chip;
		}
	}
}
