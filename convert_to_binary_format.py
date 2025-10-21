#!/usr/bin/env python3
"""
Convert levels.json to use binary test vector format.
Creates levels_binary_format.json with testVectorsFile references.
"""

import json
import sys

def convert_level(level):
    """Convert a single level to binary format."""
    # Check if level has test vectors
    if 'testVectors' in level and level['testVectors'] and len(level['testVectors']) > 0:
        # Add testVectorsFile reference
        level['testVectorsFile'] = f"testvectors/{level['id']}"
        # Clear testVectors array
        level['testVectors'] = []
        return True  # Indicates conversion was done
    return False  # No conversion needed

def main():
    # Read original levels.json
    print("Reading Assets/Resources/levels.json...")
    with open('Assets/Resources/levels.json', 'r', encoding='utf-8') as f:
        data = json.load(f)
    
    # Track statistics
    total_levels = 0
    converted_levels = 0
    
    # Process each chapter
    for chapter in data.get('chapters', []):
        for level in chapter.get('levels', []):
            total_levels += 1
            if convert_level(level):
                converted_levels += 1
                print(f"  Converted: {level['id']}")
    
    # Write new file
    output_file = 'levels_binary_format.json'
    print(f"\nWriting {output_file}...")
    with open(output_file, 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
    
    print(f"\n✅ Conversion complete!")
    print(f"   Total levels: {total_levels}")
    print(f"   Converted: {converted_levels}")
    print(f"   Output: {output_file}")
    print(f"\nNext steps:")
    print(f"1. Generate binary files for each level using 'Generate Vectors' button")
    print(f"2. Move .tvec files to Assets/Resources/testvectors/")
    print(f"3. Replace levels.json with {output_file}")

if __name__ == '__main__':
    main()

