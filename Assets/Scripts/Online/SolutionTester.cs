using UnityEngine;
using DLS.Online;
using DLS.Description;
using DLS.Game;

namespace DLS.Online
{
    /// <summary>
    /// Test script to verify solution creation, serialization, and loading
    /// </summary>
    public class SolutionTester : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private string testLevelId = "NOT Gate";
        [SerializeField] private int testScore = 1;
        [SerializeField] private string testUserName = "TestUser";
        
        [Header("Test Results")]
        [SerializeField] private string lastTestResult = "";
        [SerializeField] private string lastError = "";
        
        [ContextMenu("Test Complete Workflow")]
        public void TestCompleteWorkflow()
        {
            try
            {
                lastError = "";
                lastTestResult = "Testing complete workflow...";
                
                Debug.Log("[SolutionTester] Starting complete workflow test...");
                
                // Step 1: Create a test solution
                Debug.Log("[SolutionTester] Step 1: Creating test solution...");
                var testSolution = CreateTestSolution();
                if (testSolution == null)
                {
                    lastError = "Failed to create test solution";
                    return;
                }
                lastTestResult = "Created test solution";
                
                // Step 2: Serialize to JSON
                Debug.Log("[SolutionTester] Step 2: Serializing to JSON...");
                var json = SolutionSerializer.SerializeCompleteSolution(testSolution);
                if (string.IsNullOrEmpty(json))
                {
                    lastError = "Failed to serialize solution";
                    return;
                }
                lastTestResult = $"Serialized solution (JSON length: {json.Length})";
                
                // Step 3: Deserialize from JSON
                Debug.Log("[SolutionTester] Step 3: Deserializing from JSON...");
                var deserializedSolution = SolutionSerializer.DeserializeCompleteSolution(json);
                if (deserializedSolution == null)
                {
                    lastError = "Failed to deserialize solution";
                    return;
                }
                lastTestResult = "Deserialized solution successfully";
                
                // Step 4: Save to local storage
                Debug.Log("[SolutionTester] Step 4: Saving to local storage...");
                EditorLocalStorage.Initialize();
                var solutionId = EditorLocalStorage.SaveCompleteSolution(deserializedSolution);
                if (string.IsNullOrEmpty(solutionId))
                {
                    lastError = "Failed to save solution to local storage";
                    return;
                }
                lastTestResult = $"Saved to local storage (ID: {solutionId})";
                
                // Step 5: Save score with solution reference
                Debug.Log("[SolutionTester] Step 5: Saving score with solution reference...");
                EditorLocalStorage.SaveScore(testLevelId, testScore, testUserName, solutionId);
                lastTestResult = $"Saved score with solution reference (ID: {solutionId})";
                
                // Step 6: Test loading from local storage
                Debug.Log("[SolutionTester] Step 6: Testing load from local storage...");
                var loadedSolution = EditorLocalStorage.GetCompleteSolution(solutionId);
                if (loadedSolution == null)
                {
                    lastError = "Failed to load solution from local storage";
                    return;
                }
                lastTestResult = $"Loaded solution from local storage: {loadedSolution.LevelId}";
                
                // Step 7: Test applying solution to project
                Debug.Log("[SolutionTester] Step 7: Testing solution application...");
                var project = Project.ActiveProject;
                if (project?.chipLibrary != null)
                {
                    bool success = SolutionSerializer.LoadCompleteSolution(loadedSolution, project.chipLibrary);
                    if (success)
                    {
                        lastTestResult = "✅ Complete workflow test successful!";
                        Debug.Log("[SolutionTester] Complete workflow test successful!");
                    }
                    else
                    {
                        lastError = "Failed to apply solution to project";
                        return;
                    }
                }
                else
                {
                    lastTestResult = "✅ Complete workflow test successful! (No project to apply to)";
                    Debug.Log("[SolutionTester] Complete workflow test successful! (No project to apply to)");
                }
            }
            catch (System.Exception ex)
            {
                lastError = ex.Message;
                lastTestResult = "Test failed";
                Debug.LogError($"[SolutionTester] Test failed: {ex.Message}");
                Debug.LogError($"[SolutionTester] Stack trace: {ex.StackTrace}");
            }
        }
        
        [ContextMenu("Test JSON Serialization Only")]
        public void TestJsonSerialization()
        {
            try
            {
                lastError = "";
                lastTestResult = "Testing JSON serialization...";
                
                Debug.Log("[SolutionTester] Testing JSON serialization...");
                
                // Create test solution
                var testSolution = CreateTestSolution();
                if (testSolution == null)
                {
                    lastError = "Failed to create test solution";
                    return;
                }
                
                // Serialize to JSON
                var json = SolutionSerializer.SerializeCompleteSolution(testSolution);
                if (string.IsNullOrEmpty(json))
                {
                    lastError = "Failed to serialize solution";
                    return;
                }
                
                Debug.Log($"[SolutionTester] Serialized JSON: {json}");
                lastTestResult = $"JSON serialization successful (length: {json.Length})";
                
                // Deserialize from JSON
                var deserializedSolution = SolutionSerializer.DeserializeCompleteSolution(json);
                if (deserializedSolution == null)
                {
                    lastError = "Failed to deserialize solution";
                    return;
                }
                
                lastTestResult = "✅ JSON serialization test successful!";
                Debug.Log("[SolutionTester] JSON serialization test successful!");
            }
            catch (System.Exception ex)
            {
                lastError = ex.Message;
                lastTestResult = "JSON test failed";
                Debug.LogError($"[SolutionTester] JSON test failed: {ex.Message}");
            }
        }
        
        [ContextMenu("Test Create Solution from Current Project")]
        public void TestCreateSolutionFromCurrentProject()
        {
            try
            {
                Debug.Log("[SolutionTester] Testing solution creation from current project...");
                
                if (Project.ActiveProject == null)
                {
                    Debug.LogError("[SolutionTester] No active project found");
                    return;
                }
                
                var solution = SolutionSerializer.CreateCompleteSolutionFromCurrentProject("Test Level", 100, "TestUser");
                if (solution != null)
                {
                    Debug.Log($"[SolutionTester] Successfully created solution: {solution.MainSolution.Name}");
                    Debug.Log($"[SolutionTester] Solution has {solution.MainSolution.SubChips?.Length ?? 0} subchips, {solution.MainSolution.Wires?.Length ?? 0} wires");
                    lastTestResult = $"Created solution: {solution.MainSolution.Name}";
                }
                else
                {
                    Debug.LogError("[SolutionTester] Failed to create solution from current project");
                    lastTestResult = "Failed to create solution";
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SolutionTester] Failed to create solution from current project: {ex.Message}");
                lastTestResult = $"Error: {ex.Message}";
            }
        }
        
        [ContextMenu("Clear All Test Data")]
        public void ClearTestData()
        {
            try
            {
                Debug.Log("[SolutionTester] Clearing all test data...");
                EditorLocalStorage.ClearAll();
                lastTestResult = "Test data cleared";
                Debug.Log("[SolutionTester] Test data cleared!");
            }
            catch (System.Exception ex)
            {
                lastError = ex.Message;
                lastTestResult = "Failed to clear test data";
                Debug.LogError($"[SolutionTester] Failed to clear test data: {ex.Message}");
            }
        }
        
        private CompleteSolution CreateTestSolution()
        {
            try
            {
                // Create a simple test solution - just a basic chip description
                var testChip = new ChipDescription();
                testChip.Name = "TestNOTGate";
                testChip.ChipType = ChipType.Custom;
                testChip.Size = new Vector2(2, 1);
                testChip.Colour = Color.red;
                
                // Create input and output pins
                testChip.InputPins = new PinDescription[]
                {
                    new PinDescription { Name = "A", BitCount = 1 }
                };
                testChip.OutputPins = new PinDescription[]
                {
                    new PinDescription { Name = "Y", BitCount = 1 }
                };
                
                // Start with empty arrays
                testChip.SubChips = new SubChipDescription[0];
                testChip.Wires = new WireDescription[0];
                
                // Create the complete solution
                var solution = new CompleteSolution(testLevelId, testUserName, testScore, testChip);
                solution.UserName = testUserName;
                
                // Add some custom chip definitions
                solution.CustomChipDefinitions = new System.Collections.Generic.Dictionary<string, ChipDescription>
                {
                    { "CustomAND", CreateCustomANDChip() },
                    { "CustomOR", CreateCustomORChip() }
                };
                
                Debug.Log($"[SolutionTester] Created simple test solution with {solution.CustomChipDefinitions.Count} custom chips");
                Debug.Log($"[SolutionTester] Main solution has {testChip.SubChips.Length} subchips, {testChip.Wires.Length} wires");
                return solution;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SolutionTester] Failed to create test solution: {ex.Message}");
                return null;
            }
        }
        
        private ChipDescription CreateCustomANDChip()
        {
            var chip = new ChipDescription();
            chip.Name = "CustomAND";
            chip.ChipType = ChipType.Custom;
            chip.Size = new Vector2(1.5f, 1);
            chip.Colour = Color.blue;
            chip.InputPins = new PinDescription[]
            {
                new PinDescription { Name = "A", BitCount = 1 },
                new PinDescription { Name = "B", BitCount = 1 }
            };
            chip.OutputPins = new PinDescription[]
            {
                new PinDescription { Name = "Y", BitCount = 1 }
            };
            return chip;
        }
        
        private ChipDescription CreateCustomORChip()
        {
            var chip = new ChipDescription();
            chip.Name = "CustomOR";
            chip.ChipType = ChipType.Custom;
            chip.Size = new Vector2(1.5f, 1);
            chip.Colour = Color.green;
            chip.InputPins = new PinDescription[]
            {
                new PinDescription { Name = "A", BitCount = 1 },
                new PinDescription { Name = "B", BitCount = 1 }
            };
            chip.OutputPins = new PinDescription[]
            {
                new PinDescription { Name = "Y", BitCount = 1 }
            };
            return chip;
        }
    }
}
