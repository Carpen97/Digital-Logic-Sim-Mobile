#!/usr/bin/env python3
"""
Test the level conversion from V1 to V2 format.
Validates that the conversion preserves all important data.
"""

import json
from convert_levels_to_v2 import (
    convert_pin_label_to_pin_data,
    convert_test_vector_to_inline,
    convert_level_v1_to_v2
)


def test_pin_conversion():
    """Test pin label to pin data conversion."""
    print("Testing pin conversion...")
    
    # Test 1: Simple single-bit pin
    result = convert_pin_label_to_pin_data({"name": "A", "abbr": "A"}, 1)
    assert result == {"name": "A"}, f"Failed: {result}"
    print("  ✅ Single-bit pin with matching abbr")
    
    # Test 2: Pin with different abbr
    result = convert_pin_label_to_pin_data({"name": "Data A", "abbr": "A"}, 1)
    assert result == {"name": "Data A", "abbr": "A"}, f"Failed: {result}"
    print("  ✅ Single-bit pin with different abbr")
    
    # Test 3: Multi-bit pin
    result = convert_pin_label_to_pin_data({"name": "Data", "abbr": "D"}, 8)
    assert result == {"name": "Data", "abbr": "D", "nBits": 8}, f"Failed: {result}"
    print("  ✅ Multi-bit pin")
    
    print("✅ Pin conversion tests passed!\n")


def test_test_vector_conversion():
    """Test test vector to inline format conversion."""
    print("Testing test vector conversion...")
    
    # Test 1: Simple 2-input, 1-output
    result = convert_test_vector_to_inline("00", "0", [1, 1], [1])
    assert result == "0_0|0", f"Failed: {result}"
    print("  ✅ Simple 2-input gate")
    
    # Test 2: Single input, single output
    result = convert_test_vector_to_inline("1", "0", [1], [1])
    assert result == "1|0", f"Failed: {result}"
    print("  ✅ Single input gate")
    
    # Test 3: 8-bit inputs
    result = convert_test_vector_to_inline("0000000000000000", "000000000", 
                                          [8, 8], [8, 1])
    assert result == "00000000_00000000|00000000_0", f"Failed: {result}"
    print("  ✅ 8-bit inputs with carry")
    
    # Test 4: Multiple outputs
    result = convert_test_vector_to_inline("11", "01", [1, 1], [1, 1])
    assert result == "1_1|0_1", f"Failed: {result}"
    print("  ✅ Multiple outputs")
    
    print("✅ Test vector conversion tests passed!\n")


def test_full_level_conversion():
    """Test full level conversion."""
    print("Testing full level conversion...")
    
    # Test 1: Simple NOT gate
    v1_level = {
        "id": "lvl.not.1",
        "chapterId": "ch.basics",
        "name": "NOT Gate",
        "description": "Invert the input",
        "inputCount": 1,
        "outputCount": 1,
        "inputBitCounts": [1],
        "outputBitCounts": [1],
        "inputPinLabels": [{"name": "A", "abbr": "A"}],
        "outputPinLabels": [{"name": "Y", "abbr": "Y"}],
        "testVectors": [
            {"inputs": "0", "expected": "1"},
            {"inputs": "1", "expected": "0"}
        ]
    }
    
    v2_level = convert_level_v1_to_v2(v1_level)
    
    assert v2_level["id"] == "lvl.not.1"
    assert v2_level["name"] == "NOT Gate"
    assert v2_level["inputStructure"] == [{"name": "A"}]
    assert v2_level["outputStructure"] == [{"name": "Y"}]
    assert v2_level["testsInline"] == ["0|1", "1|0"]
    print("  ✅ NOT gate conversion")
    
    # Test 2: AND gate with 2 inputs
    v1_level = {
        "id": "lvl.and.1",
        "chapterId": "ch.basics",
        "name": "AND Gate",
        "description": "Output is 1 only if both inputs are 1",
        "inputCount": 2,
        "outputCount": 1,
        "inputBitCounts": [1, 1],
        "outputBitCounts": [1],
        "inputPinLabels": [
            {"name": "B", "abbr": "B"},
            {"name": "A", "abbr": "A"}
        ],
        "outputPinLabels": [{"name": "Y", "abbr": "Y"}],
        "testVectors": [
            {"inputs": "00", "expected": "0"},
            {"inputs": "01", "expected": "0"},
            {"inputs": "10", "expected": "0"},
            {"inputs": "11", "expected": "1"}
        ]
    }
    
    v2_level = convert_level_v1_to_v2(v1_level)
    
    assert v2_level["inputStructure"] == [{"name": "B"}, {"name": "A"}]
    assert v2_level["testsInline"] == ["0_0|0", "0_1|0", "1_0|0", "1_1|1"]
    print("  ✅ AND gate conversion")
    
    # Test 3: 8-bit adder with binary file
    v1_level = {
        "id": "lvl.8bit.adder.1",
        "chapterId": "ch.8bit",
        "name": "8-Bit Adder",
        "description": "Add two 8-bit numbers",
        "inputCount": 2,
        "outputCount": 2,
        "inputBitCounts": [8, 8],
        "outputBitCounts": [8, 1],
        "inputPinLabels": [
            {"name": "Data A", "abbr": "A"},
            {"name": "Data B", "abbr": "B"}
        ],
        "outputPinLabels": [
            {"name": "Sum", "abbr": "SUM"},
            {"name": "Carry Out", "abbr": "C"}
        ],
        "testVectorsFile": "GeneratedTestVectors/lvl.8bit.adder.1"
    }
    
    v2_level = convert_level_v1_to_v2(v1_level)
    
    assert v2_level["inputStructure"] == [
        {"name": "Data A", "abbr": "A", "nBits": 8},
        {"name": "Data B", "abbr": "B", "nBits": 8}
    ]
    assert v2_level["outputStructure"] == [
        {"name": "Sum", "abbr": "SUM", "nBits": 8},
        {"name": "Carry Out", "abbr": "C"}
    ]
    assert v2_level["testsBinaryPath"] == "GeneratedTestVectors/lvl.8bit.adder.1"
    assert "testsInline" not in v2_level
    print("  ✅ 8-bit adder with binary file")
    
    # Test 4: Sequential level (should return None)
    v1_level = {
        "id": "lvl.srlatch.1",
        "chapterId": "ch.sequential",
        "name": "SR Latch",
        "description": "Sequential circuit",
        "isSequential": True,
        "inputCount": 2,
        "outputCount": 2,
        "inputBitCounts": [1, 1],
        "outputBitCounts": [1, 1],
        "inputPinLabels": [{"name": "S"}, {"name": "R"}],
        "outputPinLabels": [{"name": "Q"}, {"name": "N"}]
    }
    
    v2_level = convert_level_v1_to_v2(v1_level)
    assert v2_level is None
    print("  ✅ Sequential level correctly skipped")
    
    print("✅ Full level conversion tests passed!\n")


def test_real_level_sample():
    """Test with a real level from levels.json."""
    print("Testing with real level sample...")
    
    # This is the actual NOT gate from levels.json
    v1_level = {
        "id": "lvl.not.1",
        "chapterId": "ch.basics",
        "name": "NOT Gate",
        "description": "Output should be the inverse of input.",
        "inputCount": 1,
        "outputCount": 1,
        "inputBitCounts": [1],
        "outputBitCounts": [1],
        "inputPinLabels": [{"name": "A", "abbr": "A"}],
        "outputPinLabels": [{"name": "Y", "abbr": "Y"}],
        "testVectors": [
            {"inputs": "0", "expected": "1"},
            {"inputs": "1", "expected": "0"}
        ],
        "hints": []
    }
    
    v2_level = convert_level_v1_to_v2(v1_level)
    
    # Print the result
    print("V2 Output:")
    print(json.dumps(v2_level, indent=2))
    
    # Validate structure
    assert "id" in v2_level
    assert "name" in v2_level
    assert "chapterId" in v2_level
    assert "description" in v2_level
    assert "inputStructure" in v2_level
    assert "outputStructure" in v2_level
    assert "testsInline" in v2_level
    assert "hints" not in v2_level  # Hints are removed in V2
    
    print("✅ Real level conversion validated!\n")


def main():
    """Run all tests."""
    print("=" * 60)
    print("Level V1 → V2 Conversion Tests")
    print("=" * 60 + "\n")
    
    try:
        test_pin_conversion()
        test_test_vector_conversion()
        test_full_level_conversion()
        test_real_level_sample()
        
        print("=" * 60)
        print("🎉 All tests passed!")
        print("=" * 60)
        return 0
        
    except AssertionError as e:
        print(f"\n❌ Test failed: {e}")
        return 1
    except Exception as e:
        print(f"\n❌ Unexpected error: {e}")
        import traceback
        traceback.print_exc()
        return 1


if __name__ == "__main__":
    import sys
    sys.exit(main())

