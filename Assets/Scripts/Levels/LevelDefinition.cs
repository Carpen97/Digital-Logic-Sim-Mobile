using System;
using UnityEngine;

namespace DLS.Levels
{
	[Serializable]
	public sealed class LevelDefinition
	{
		public string id;
		public string chapterId;
		public string name;
		public string description;
	public int inputCount;
	public int outputCount;
	public System.Collections.Generic.List<string> inputLabels;
	public System.Collections.Generic.List<string> outputLabels;
	
	// Multi-bit pin support
	public int[] inputBitCounts;  // Bit count for each input pin (defaults to 1 if not specified)
	public int[] outputBitCounts; // Bit count for each output pin (defaults to 1 if not specified)
	
	// Enhanced label system with both long descriptive names and short abbreviations
	[Serializable] public struct PinLabel {
		public string name;  // Long descriptive name (e.g., "A > B")
		public string abbr;  // Short abbreviation for report header (max 3 chars, e.g., "GT")
	}
	public PinLabel[] inputPinLabels;
	public PinLabel[] outputPinLabels;
		
		// Enhanced test data structures
		[Serializable] public struct TestVector { 
			public string inputs; 
			public string expected; 
			public int settleSteps;
			public bool isClockEdge;
		}
		public TestVector[] testVectors;
		
		// New sequential circuit support
		public bool isSequential = false;
		public int clockInputIndex = -1;  // Which input is the clock (if any)
		public int settleStepsPerVector = 2;  // How many steps to settle per vector
		public int maxSequenceSteps = 100;
		public bool requireStableOutputs = true;
		
	[Serializable] public struct TestSequence { 
		public string name;  // e.g., "Reset sequence", "Count up sequence"
		public string[] setup;  // Optional: Input patterns to apply before testing (for initialization)
		public TestVector[] vectors;  // Sequence of input/output pairs
	}
	public TestSequence[] testSequences;
	
	// Support for external binary test vector files
	public string testVectorsFile;  // Path to binary file (relative to Resources folder, without extension)
	
	// Cached test vectors loaded from binary file
	private TestVector[] _cachedBinaryVectors;
	private bool _binaryVectorsLoaded;
	
	/// <summary>
	/// Get test vectors - loads from binary file if specified, otherwise uses testVectors array.
	/// </summary>
	public TestVector[] GetTestVectors()
	{
		// If we have a binary file specified, load from it
		if (!string.IsNullOrEmpty(testVectorsFile))
		{
			if (!_binaryVectorsLoaded)
			{
				_cachedBinaryVectors = TestVectorsBinaryFormat.ReadFromResource(testVectorsFile);
				_binaryVectorsLoaded = true;
			}
			return _cachedBinaryVectors ?? System.Array.Empty<TestVector>();
		}
		
		// Otherwise use the inline testVectors array
		return testVectors ?? System.Array.Empty<TestVector>();
	}
		
		public System.Collections.Generic.List<string> hints;
	}
}
