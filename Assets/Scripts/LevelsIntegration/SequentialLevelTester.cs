using UnityEngine;
using DLS.Levels;
using DLS.Levels.Host;
using DLS.Game.LevelsIntegration;

namespace DLS.Game.LevelsIntegration
{
    /// <summary>
    /// Test script to verify the new sequential level validation system.
    /// This can be used to test the enhanced validation functionality.
    /// </summary>
    public class SequentialLevelTester : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool runTestsOnStart = false;
        [SerializeField] private bool logDetailedResults = true;

        void Start()
        {
            if (runTestsOnStart)
            {
                TestSequentialValidation();
            }
        }

        [ContextMenu("Test Sequential Validation")]
        public void TestSequentialValidation()
        {
            Debug.Log("[SequentialLevelTester] Starting sequential validation tests...");

            // Test 1: Create a simple sequential level definition
            var testLevel = CreateTestSequentialLevel();
            
            // Test 2: Create a simulation adapter
            var adapter = new MobileSimulationAdapter();
            var validator = new LevelValidator(adapter);

            // Test 3: Validate the level (this will fail since we don't have a real circuit)
            var report = validator.Validate(testLevel);

            // Test 4: Log results
            LogValidationResults(testLevel, report);

            Debug.Log("[SequentialLevelTester] Sequential validation tests completed.");
        }

        private LevelDefinition CreateTestSequentialLevel()
        {
            return new LevelDefinition
            {
                id = "test.sequential.1",
                chapterId = "ch.test",
                name = "Test Sequential Level",
                description = "Test level for sequential validation",
                inputCount = 1,
                outputCount = 1,
                inputLabels = new System.Collections.Generic.List<string> { "CLK" },
                outputLabels = new System.Collections.Generic.List<string> { "Q" },
                isSequential = true,
                clockInputIndex = 0,
                settleStepsPerVector = 2,
                testSequences = new LevelDefinition.TestSequence[]
                {
                    new LevelDefinition.TestSequence
                    {
                        name = "Test Sequence",
                        vectors = new LevelDefinition.TestVector[]
                        {
                            new LevelDefinition.TestVector
                            {
                                inputs = "0",
                                expected = "0",
                                settleSteps = 1,
                                isClockEdge = false
                            },
                            new LevelDefinition.TestVector
                            {
                                inputs = "1",
                                expected = "1",
                                settleSteps = 2,
                                isClockEdge = true
                            }
                        }
                    }
                },
                hints = new System.Collections.Generic.List<string> { "Test hint" }
            };
        }

        private void LogValidationResults(LevelDefinition level, ValidationReport report)
        {
            Debug.Log($"[SequentialLevelTester] Level: {level.name}");
            Debug.Log($"[SequentialLevelTester] Is Sequential: {level.isSequential}");
            Debug.Log($"[SequentialLevelTester] Clock Input Index: {level.clockInputIndex}");
            Debug.Log($"[SequentialLevelTester] Test Sequences: {level.testSequences?.Length ?? 0}");
            
            if (level.testSequences != null)
            {
                for (int i = 0; i < level.testSequences.Length; i++)
                {
                    var seq = level.testSequences[i];
                    Debug.Log($"[SequentialLevelTester] Sequence {i}: {seq.name} ({seq.vectors?.Length ?? 0} vectors)");
                }
            }

            Debug.Log($"[SequentialLevelTester] Validation Passed: {report.PassedAll}");
            Debug.Log($"[SequentialLevelTester] Failures: {report.Failures?.Count ?? 0}");
            
            if (report.Failures != null && logDetailedResults)
            {
                foreach (var failure in report.Failures)
                {
                    Debug.Log($"[SequentialLevelTester] Failure: {failure.Message}");
                }
            }
        }

        /// <summary>
        /// Test the new sequential validation features
        /// </summary>
        [ContextMenu("Test Enhanced Features")]
        public void TestEnhancedFeatures()
        {
            Debug.Log("[SequentialLevelTester] Testing enhanced validation features...");

            // Test 1: Verify TestVector structure
            var testVector = new LevelDefinition.TestVector
            {
                inputs = "10",
                expected = "01",
                settleSteps = 3,
                isClockEdge = true
            };

            Debug.Log($"[SequentialLevelTester] TestVector - Inputs: {testVector.inputs}, Expected: {testVector.expected}, SettleSteps: {testVector.settleSteps}, IsClockEdge: {testVector.isClockEdge}");

            // Test 2: Verify TestSequence structure
            var testSequence = new LevelDefinition.TestSequence
            {
                name = "Test Sequence",
                vectors = new LevelDefinition.TestVector[] { testVector }
            };

            Debug.Log($"[SequentialLevelTester] TestSequence - Name: {testSequence.name}, Vectors: {testSequence.vectors.Length}");

            // Test 3: Verify LevelDefinition enhanced fields
            var level = new LevelDefinition
            {
                isSequential = true,
                clockInputIndex = 0,
                settleStepsPerVector = 2,
                maxSequenceSteps = 100,
                requireStableOutputs = true
            };

            Debug.Log($"[SequentialLevelTester] Enhanced LevelDefinition - IsSequential: {level.isSequential}, ClockIndex: {level.clockInputIndex}, SettleSteps: {level.settleStepsPerVector}");

            Debug.Log("[SequentialLevelTester] Enhanced features test completed successfully!");
        }
    }
}
