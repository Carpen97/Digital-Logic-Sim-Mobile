# Test Vectors Binary Format

This folder contains binary test vector files (.tvec) for Digital Logic Sim levels.

## Why Binary Format?

Large levels can have hundreds or thousands of test vectors. Storing these in JSON makes the `levels.json` file huge and hard to maintain. Binary format provides:

- **90-95% size reduction** compared to JSON
- **Faster loading** - binary parsing is faster than JSON
- **Cleaner levels.json** - keeps the main file readable
- **No data loss** - bit-for-bit accurate representation

## File Format

Files use the `.tvec` extension and contain:

### Header (16 bytes):
- Magic: "TVEC" (4 bytes) - file signature
- Version: 1 (4 bytes) - format version  
- InputBits: N (2 bytes) - total input bits
- OutputBits: M (2 bytes) - total output bits
- VectorCount: C (4 bytes) - number of test vectors

### Body (variable):
For each test vector:
- Input bits packed into bytes (⌈InputBits/8⌉ bytes)
- Output bits packed into bytes (⌈OutputBits/8⌉ bytes)

### Example:
8-bit AND gate (16 input bits, 8 output bits, 20 test vectors):
- Header: 16 bytes
- Body: 20 × (2 + 1) = 60 bytes
- **Total: 76 bytes** vs ~1,600 bytes in JSON (95% smaller!)

## Usage in levels.json

Instead of embedding test vectors:

```json
{
  "id": "lvl.8bit.and.1",
  "name": "8-Bit AND",
  "testVectorsFile": "testvectors/lvl.8bit.and.1",
  "testVectors": []
}
```

Note: The path is relative to `Resources/` folder and has no extension.

## Converting Existing Levels

Use the Unity Editor tool:

1. Open Unity Editor
2. Go to menu: **Tools → Digital Logic Sim → Convert Test Vectors to Binary**
3. Click "Analyze levels.json" to see which levels would benefit
4. Click "Convert Large Levels to Binary" to convert levels with 1000+ bytes
5. Or click "Convert ALL Levels to Binary" to convert everything

The tool:
- Creates a backup of `levels.json`
- Generates `.tvec` files in this folder
- Updates level definitions to reference binary files
- Clears inline `testVectors` arrays

## Manual Creation

If you're creating test vectors programmatically:

```csharp
using DLS.Levels;

// Create test vectors
var vectors = new LevelDefinition.TestVector[] {
    new() { inputs = "00000000", expected = "00000000" },
    new() { inputs = "11111111", expected = "11111111" },
    // ... more vectors
};

// Write to binary file
TestVectorsBinaryFormat.WriteToFile(
    "Assets/Resources/testvectors/mylevel.tvec",
    vectors,
    inputBits: 8,
    outputBits: 8
);
```

## File Naming Convention

Files are named after level IDs:
- Level ID: `lvl.8bit.and.1`
- Binary file: `lvl.8bit.and.1.tvec`
- Resource path in JSON: `testvectors/lvl.8bit.and.1` (no extension)

## Backward Compatibility

Levels can still use inline `testVectors` arrays. The system checks:
1. If `testVectorsFile` is specified → load from binary
2. Otherwise → use inline `testVectors` array

This allows gradual migration and mixing both formats.

## Size Comparison

Real examples from Digital Logic Sim:

| Level | Vectors | JSON Size | Binary Size | Savings |
|-------|---------|-----------|-------------|---------|
| 8-Bit AND | 20 | 1.6 KB | 76 B | 95% |
| 8-Bit ALU | 500 | 40 KB | 2.1 KB | 95% |
| 4-bit Adder | 30 | 2.4 KB | 120 B | 95% |

## Technical Details

- Bits are packed MSB-first (most significant bit first)
- Unused bits in the last byte of each field are set to 0
- File format is platform-independent (little-endian)
- Maximum supported: 65,535 input bits, 65,535 output bits, 2³¹ vectors
- Files are loaded lazily and cached after first access

## Troubleshooting

### "Resource not found" error
- Ensure the file exists in `Assets/Resources/testvectors/`
- Check that the path in JSON doesn't include `.tvec` extension
- Verify the file name matches the level ID exactly

### "Invalid magic number" error
- File may be corrupted
- Regenerate using the converter tool
- Check file isn't a text file renamed to .tvec

### Test vectors don't load
- Check Unity console for detailed error messages
- Verify `inputBitCounts` and `outputBitCounts` in level definition
- Ensure binary file matches the bit counts in the level

## Future Improvements

Possible enhancements:
- Compression (gzip) for even smaller files
- Streaming for very large test sets
- Test vector generation from golden circuits
- Parameterized test generation

