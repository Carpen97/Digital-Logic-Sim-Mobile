using UnityEngine;
using DLS.Levels;
using DLS.Levels.Host;
using DLS.Game.LevelsIntegration;

namespace DLS.Game.LevelsIntegration
{
    /// <summary>
    /// Test script to verify multi-bit pin validation works correctly.
    /// This can be used to test the 8-bit wire level.
    /// </summary>
    public class MultiBitPinTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool runTestOnStart = false;
        [SerializeField] private bool logDetailedResults = true;

        void Start()
        {
            if (runTestOnStart)
            {
                TestMultiBitPinValidation();
            }
        }

        [ContextMenu("Test Multi-Bit Pin Validation")]
        public void TestMultiBitPinValidation()
        {
            Debug.Log("[MultiBitPinTest] Starting multi-bit pin validation test...");

            // Test 1: Create the 8-bit wire level definition
            var testLevel = Create8BitWireLevel();
            
            // Test 2: Create a simulation adapter
            var adapter = new MobileSimulationAdapter();
            var validator = new LevelValidator(adapter);

            // Test 3: Validate the level (this will fail since we don't have a real circuit)
            var report = validator.Validate(testLevel);

            // Test 4: Log results
            LogValidationResults(testLevel, report);

            Debug.Log("[MultiBitPinTest] Multi-bit pin validation test completed.");
        }

        private LevelDefinition Create8BitWireLevel()
        {
            return new LevelDefinition
            {
                id = "lvl.8bit.wire.1",
                chapterId = "ch.8bit",
                name = "8-Bit Wire",
                description = "Connect the 8-bit input directly to the 8-bit output.",
                inputCount = 1,
                outputCount = 1,
                inputBitCounts = new int[] { 8 },
                outputBitCounts = new int[] { 8 },
                inputLabels = new System.Collections.Generic.List<string> { "A" },
                outputLabels = new System.Collections.Generic.List<string> { "OUT" },
                testVectors = new LevelDefinition.TestVector[]
                {
                    new LevelDefinition.TestVector
                    {
                        inputs = "00000000",
                        expected = "00000000"
                    },
                    new LevelDefinition.TestVector
                    {
                        inputs = "11111111",
                        expected = "11111111"
                    },
                    new LevelDefinition.TestVector
                    {
                        inputs = "10101010",
                        expected = "10101010"
                    }
                },
                hints = new System.Collections.Generic.List<string> 
                { 
                    "Connect A0 to OUT0, A1 to OUT1, and so on...",
                    "This is the simplest 8-bit operation - just passing data through"
                }
            };
        }

        private void LogValidationResults(LevelDefinition level, ValidationReport report)
        {
            Debug.Log($"[MultiBitPinTest] Level: {level.name}");
            Debug.Log($"[MultiBitPinTest] Input Count: {level.inputCount}");
            Debug.Log($"[MultiBitPinTest] Output Count: {level.outputCount}");
            Debug.Log($"[MultiBitPinTest] Input Bit Counts: {(level.inputBitCounts != null ? string.Join(", ", level.inputBitCounts) : "null")}");
            Debug.Log($"[MultiBitPinTest] Output Bit Counts: {(level.outputBitCounts != null ? string.Join(", ", level.outputBitCounts) : "null")}");
            Debug.Log($"[MultiBitPinTest] Test Vectors: {level.testVectors?.Length ?? 0}");
            
            if (level.testVectors != null && logDetailedResults)
            {
                for (int i = 0; i < level.testVectors.Length; i++)
                {
                    var tv = level.testVectors[i];
                    Debug.Log($"[MultiBitPinTest] Test Vector {i}: inputs='{tv.inputs}', expected='{tv.expected}'");
                }
            }

            Debug.Log($"[MultiBitPinTest] Validation Passed: {report.PassedAll}");
            Debug.Log($"[MultiBitPinTest] Failures: {report.Failures?.Count ?? 0}");
            
            if (report.Failures != null && logDetailedResults)
            {
                foreach (var failure in report.Failures)
                {
                    Debug.Log($"[MultiBitPinTest] Failure: {failure.Message}");
                }
            }

            Debug.Log($"[MultiBitPinTest] All Test Results: {report.AllTestResults?.Count ?? 0}");
            if (report.AllTestResults != null && logDetailedResults)
            {
                foreach (var result in report.AllTestResults)
                {
                    Debug.Log($"[MultiBitPinTest] Test Result: inputs='{result.Inputs}', expected='{result.Expected}', actual='{result.Actual}', passed={result.Passed}");
                }
            }
        }

        /// <summary>
        /// Test the BitVector parsing for multi-bit strings
        /// </summary>
        [ContextMenu("Test BitVector Parsing")]
        public void TestBitVectorParsing()
        {
            Debug.Log("[MultiBitPinTest] Testing BitVector parsing...");

            // Test various 8-bit patterns
            string[] testPatterns = { "00000000", "11111111", "10101010", "01010101", "10000000", "00000001" };
            
            foreach (string pattern in testPatterns)
            {
                var bitVector = BitVector.FromString(pattern);
                string reconstructed = bitVector.ToString();
                
                Debug.Log($"[MultiBitPinTest] Pattern: '{pattern}' -> BitVector -> '{reconstructed}' (Length: {bitVector.Length})");
                
                // Verify each bit
                for (int i = 0; i < pattern.Length; i++)
                {
                    bool expected = pattern[i] == '1';
                    bool actual = bitVector[i];
                    if (expected != actual)
                    {
                        Debug.LogError($"[MultiBitPinTest] Bit mismatch at position {i}: expected {expected}, got {actual}");
                    }
                }
            }

            Debug.Log("[MultiBitPinTest] BitVector parsing test completed!");
        }
    }
}
