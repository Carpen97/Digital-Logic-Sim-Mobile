#!/usr/bin/env python3
"""
Convert Digital Logic Sim levels from V1 format to V2 format.

Usage:
    python convert_levels_to_v2.py [input_file] [output_file]
    
    If no arguments provided:
    - Reads from Assets/Resources/levels.json
    - Outputs to levels_v2.json
    
Examples:
    python convert_levels_to_v2.py
    python convert_levels_to_v2.py levels.json levels_converted.json
    python convert_levels_to_v2.py --chapter ch.basics
"""

import json
import sys
import argparse
from pathlib import Path
from typing import Dict, List, Any, Optional


def convert_pin_label_to_pin_data(label: Dict[str, str], bit_count: int = 1) -> Dict[str, Any]:
    """
    Convert V1 PinLabel to V2 PinData.
    
    V1: {"name": "Data A", "abbr": "A"}
    V2: {"name": "Data A", "abbr": "A", "nBits": 8}
    """
    pin_data = {
        "name": label.get("name", ""),
    }
    
    # Only include abbr if it's different from name
    abbr = label.get("abbr", "")
    if abbr and abbr != pin_data["name"]:
        pin_data["abbr"] = abbr
    
    # Only include nBits if it's not 1
    if bit_count > 1:
        pin_data["nBits"] = bit_count
    
    return pin_data


def convert_test_vector_to_inline(inputs: str, expected: str, 
                                   input_bit_counts: List[int], 
                                   output_bit_counts: List[int]) -> str:
    """
    Convert V1 test vector to V2 inline format.
    
    V1: {"inputs": "00", "expected": "0"}
    V2: "0_0|0"
    
    Adds underscores between pins for readability if there are multiple pins.
    """
    # Split inputs into pins based on bit counts
    input_parts = []
    offset = 0
    for bit_count in input_bit_counts:
        input_parts.append(inputs[offset:offset + bit_count])
        offset += bit_count
    
    # Split outputs into pins based on bit counts
    output_parts = []
    offset = 0
    for bit_count in output_bit_counts:
        output_parts.append(expected[offset:offset + bit_count])
        offset += bit_count
    
    # Join with underscores for multi-pin circuits
    if len(input_parts) > 1:
        input_str = "_".join(input_parts)
    else:
        input_str = input_parts[0] if input_parts else ""
    
    if len(output_parts) > 1:
        output_str = "_".join(output_parts)
    else:
        output_str = output_parts[0] if output_parts else ""
    
    return f"{input_str}|{output_str}"


def convert_level_v1_to_v2(level: Dict[str, Any]) -> Optional[Dict[str, Any]]:
    """
    Convert a single V1 level definition to V2 format.
    
    Returns None if conversion fails.
    """
    is_sequential = level.get("isSequential", False)
    
    # Required fields
    v2_level = {
        "id": level.get("id"),
        "name": level.get("name"),
        "chapterId": level.get("chapterId"),
        "description": level.get("description", ""),
    }
    
    # Set type for sequential circuits
    if is_sequential:
        v2_level["type"] = "sequential"
    
    # Convert input structure
    input_count = level.get("inputCount", 0)
    input_bit_counts = level.get("inputBitCounts", [1] * input_count)
    input_labels = level.get("inputPinLabels", [])
    
    input_structure = []
    for i in range(input_count):
        label = input_labels[i] if i < len(input_labels) else {"name": f"Input{i}"}
        bit_count = input_bit_counts[i] if i < len(input_bit_counts) else 1
        input_structure.append(convert_pin_label_to_pin_data(label, bit_count))
    
    v2_level["inputStructure"] = input_structure
    
    # Convert output structure
    output_count = level.get("outputCount", 0)
    output_bit_counts = level.get("outputBitCounts", [1] * output_count)
    output_labels = level.get("outputPinLabels", [])
    
    output_structure = []
    for i in range(output_count):
        label = output_labels[i] if i < len(output_labels) else {"name": f"Output{i}"}
        bit_count = output_bit_counts[i] if i < len(output_bit_counts) else 1
        output_structure.append(convert_pin_label_to_pin_data(label, bit_count))
    
    v2_level["outputStructure"] = output_structure
    
    # Convert tests
    if is_sequential:
        # Sequential circuit - use testSequences
        test_sequences = level.get("testSequences", [])
        
        if test_sequences and len(test_sequences) > 0:
            # Use the first sequence (V2 only supports one sequence)
            sequence = test_sequences[0]
            
            # Convert setup if present
            setup = sequence.get("setup")
            if setup and len(setup) > 0:
                # Setup is already in the right format (just input strings)
                # Add underscores for readability if multiple pins
                setup_with_underscores = []
                for setup_input in setup:
                    # Split inputs into pins based on bit counts
                    parts = []
                    offset = 0
                    for bit_count in input_bit_counts:
                        parts.append(setup_input[offset:offset + bit_count])
                        offset += bit_count
                    
                    if len(parts) > 1:
                        setup_with_underscores.append("_".join(parts))
                    else:
                        setup_with_underscores.append(parts[0] if parts else "")
                
                v2_level["setup"] = setup_with_underscores
            
            # Convert test vectors
            vectors = sequence.get("vectors", [])
            tests_inline = []
            for tv in vectors:
                inputs = tv.get("inputs", "")
                expected = tv.get("expected", "")
                inline_str = convert_test_vector_to_inline(
                    inputs, expected, input_bit_counts, output_bit_counts
                )
                tests_inline.append(inline_str)
            
            v2_level["testsInline"] = tests_inline
        else:
            print(f"  ⚠️  Warning: Sequential level {level.get('id')} has no test sequences!")
    else:
        # Combinational circuit - use regular test vectors
        test_vectors_file = level.get("testVectorsFile")
        test_vectors = level.get("testVectors", [])
        
        if test_vectors_file:
            # Use binary file path
            v2_level["testsBinaryPath"] = test_vectors_file
        elif test_vectors:
            # Convert inline test vectors
            tests_inline = []
            for tv in test_vectors:
                inputs = tv.get("inputs", "")
                expected = tv.get("expected", "")
                inline_str = convert_test_vector_to_inline(
                    inputs, expected, input_bit_counts, output_bit_counts
                )
                tests_inline.append(inline_str)
            v2_level["testsInline"] = tests_inline
        else:
            print(f"  ⚠️  Warning: Level {level.get('id')} has no tests!")
    
    return v2_level


def convert_chapter(chapter: Dict[str, Any], chapter_filter: Optional[str] = None) -> Dict[str, Any]:
    """
    Convert a chapter, moving V1 levels to levelsV2 array.
    
    Args:
        chapter: Chapter dictionary
        chapter_filter: If provided, only convert this chapter
    
    Returns:
        Modified chapter dictionary
    """
    chapter_id = chapter.get("chapterId", "")
    
    # If filtering by chapter and this isn't the one, return unchanged
    if chapter_filter and chapter_id != chapter_filter:
        return chapter
    
    print(f"\n📦 Converting chapter: {chapter.get('chapterName', chapter_id)}")
    
    v1_levels = chapter.get("levels", [])
    v2_levels = chapter.get("levelsV2", [])
    
    if not v1_levels:
        print("  ℹ️  No V1 levels to convert")
        return chapter
    
    # Convert each V1 level
    converted_count = 0
    skipped_count = 0
    
    for level in v1_levels:
        level_id = level.get("id", "unknown")
        v2_level = convert_level_v1_to_v2(level)
        
        if v2_level:
            v2_levels.append(v2_level)
            converted_count += 1
            print(f"  ✅ Converted: {level_id}")
        else:
            skipped_count += 1
    
    # Update chapter
    chapter["levelsV2"] = v2_levels
    
    print(f"  📊 Converted: {converted_count}, Skipped: {skipped_count}")
    
    return chapter


def convert_levels_file(input_path: str, output_path: str, 
                       chapter_filter: Optional[str] = None,
                       keep_v1: bool = False):
    """
    Convert entire levels file from V1 to V2 format.
    
    Args:
        input_path: Path to input JSON file
        output_path: Path to output JSON file
        chapter_filter: If provided, only convert specified chapter
        keep_v1: If True, keep V1 levels alongside V2 (for testing)
    """
    print(f"📖 Reading from: {input_path}")
    
    with open(input_path, 'r', encoding='utf-8') as f:
        data = json.load(f)
    
    chapters = data.get("chapters", [])
    
    if not chapters:
        print("❌ No chapters found in file!")
        return
    
    # Convert each chapter
    total_converted = 0
    for i, chapter in enumerate(chapters):
        chapters[i] = convert_chapter(chapter, chapter_filter)
        
        # Count converted levels
        v2_levels = chapters[i].get("levelsV2", [])
        total_converted += len(v2_levels)
        
        # Optionally remove V1 levels
        if not keep_v1 and not chapter_filter:
            chapters[i]["levels"] = []
    
    data["chapters"] = chapters
    
    # Write output
    print(f"\n💾 Writing to: {output_path}")
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
    
    print(f"\n✅ Done! Converted {total_converted} levels to V2 format")
    
    if keep_v1:
        print("ℹ️  V1 levels kept in output (both formats present)")
    else:
        print("ℹ️  V1 levels removed from output (V2 only)")


def main():
    parser = argparse.ArgumentParser(
        description="Convert Digital Logic Sim levels from V1 to V2 format",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  # Convert all levels, output to levels_v2.json
  python convert_levels_to_v2.py
  
  # Convert specific file
  python convert_levels_to_v2.py input.json output.json
  
  # Convert only one chapter
  python convert_levels_to_v2.py --chapter ch.basics
  
  # Keep V1 levels alongside V2 (for testing)
  python convert_levels_to_v2.py --keep-v1
        """
    )
    
    parser.add_argument(
        "input_file",
        nargs="?",
        default="Assets/Resources/levels.json",
        help="Input JSON file (default: Assets/Resources/levels.json)"
    )
    
    parser.add_argument(
        "output_file",
        nargs="?",
        default="levels_v2.json",
        help="Output JSON file (default: levels_v2.json)"
    )
    
    parser.add_argument(
        "--chapter",
        "-c",
        help="Only convert specified chapter (e.g., ch.basics)"
    )
    
    parser.add_argument(
        "--keep-v1",
        "-k",
        action="store_true",
        help="Keep V1 levels in output alongside V2 (useful for testing)"
    )
    
    args = parser.parse_args()
    
    # Check if input file exists
    if not Path(args.input_file).exists():
        print(f"❌ Error: Input file not found: {args.input_file}")
        return 1
    
    try:
        convert_levels_file(
            args.input_file,
            args.output_file,
            chapter_filter=args.chapter,
            keep_v1=args.keep_v1
        )
        return 0
    except Exception as e:
        print(f"\n❌ Error: {e}")
        import traceback
        traceback.print_exc()
        return 1


if __name__ == "__main__":
    sys.exit(main())

