using System;
using UnityEngine;

namespace DLS.SaveSystem
{
	/// <summary>
	/// Test to verify the new save format with InternalTypeId works correctly.
	/// </summary>
	public static class SaveFormatTest
	{
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void TestSaveFormat()
		{
			Debug.Log("Testing save format with InternalTypeId...");
			
			// Create a test chip
			var testChip = CreateTestChip();
			
			// Set the internal type ID
			testChip.InternalTypeId = DLS.Description.ChipTypeId.XOR;
			
			// Serialize the chip
			string serialized = Saver.CreateSerializedChipDescription(testChip);
			Debug.Log($"Serialized chip:\n{serialized}");
			
			// Deserialize the chip
			var deserializedChip = Saver.CloneChipDescription(testChip);
			
			// Verify the InternalTypeId was preserved
			if (deserializedChip.InternalTypeId == DLS.Description.ChipTypeId.XOR)
			{
				Debug.Log("✓ InternalTypeId correctly serialized and deserialized");
			}
			else
			{
				Debug.LogError($"✗ InternalTypeId serialization failed. Expected: XOR, Got: {deserializedChip.InternalTypeId}");
			}
			
			// Test backward compatibility with missing field
			TestBackwardCompatibility();
		}
		
		static void TestBackwardCompatibility()
		{
			Debug.Log("Testing backward compatibility...");
			
			// Create JSON without InternalTypeId field (simulating old save files)
			string oldFormatJson = @"{
  ""Name"": ""TestChip"",
  ""NameLocation"": 0,
  ""ChipType"": 0,
  ""ShouldBeCached"": false,
  ""Size"": {
    ""x"": 1.0,
    ""y"": 1.0
  },
  ""Colour"": {
    ""r"": 1.0,
    ""g"": 1.0,
    ""b"": 1.0,
    ""a"": 1
  },
  ""InputPins"": [],
  ""OutputPins"": [],
  ""SubChips"": [],
  ""Wires"": [],
  ""Displays"": []
}";
			
			// Try to deserialize
			try
			{
				var chip = DLS.Description.Serializer.DeserializeChipDescription(oldFormatJson);
				
				// Should default to Unknown
				if (chip.InternalTypeId == DLS.Description.ChipTypeId.Unknown)
				{
					Debug.Log("✓ Backward compatibility works - missing InternalTypeId defaults to Unknown");
				}
				else
				{
					Debug.LogError($"✗ Backward compatibility failed. Expected: Unknown, Got: {chip.InternalTypeId}");
				}
			}
			catch (Exception ex)
			{
				Debug.LogError($"✗ Backward compatibility test failed with exception: {ex.Message}");
			}
		}
		
		static DLS.Description.ChipDescription CreateTestChip()
		{
			return new DLS.Description.ChipDescription
			{
				Name = "TestChip",
				NameLocation = DLS.Description.NameDisplayLocation.Centre,
				ChipType = DLS.Description.ChipType.Custom,
				ShouldBeCached = false,
				Size = new Vector2(1.0f, 1.0f),
				Colour = Color.white,
				InputPins = new DLS.Description.PinDescription[0],
				OutputPins = new DLS.Description.PinDescription[0],
				SubChips = new DLS.Description.SubChipDescription[0],
				Wires = new DLS.Description.WireDescription[0],
				Displays = new DLS.Description.DisplayDescription[0]
			};
		}
	}
}
