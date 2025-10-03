using UnityEngine;
using DLS.Online;

namespace DLS.Online
{
    /// <summary>
    /// Simple script to create a test score with solution ID for leaderboard testing
    /// </summary>
    public class CreateTestScore : MonoBehaviour
    {
        [ContextMenu("Create Test Score with Solution")]
        public void CreateTestScoreWithSolution()
        {
            try
            {
                Debug.Log("[CreateTestScore] Creating test score with solution...");
                
                // Initialize local storage
                EditorLocalStorage.Initialize();
                
                // Create a simple test solution
                var testChip = new DLS.Description.ChipDescription();
                testChip.Name = "TestNOTGate";
                testChip.ChipType = DLS.Description.ChipType.Custom;
                testChip.Size = new Vector2(2, 1);
                testChip.Colour = Color.red;
                
                // Add input/output pins
                testChip.InputPins = new DLS.Description.PinDescription[]
                {
                    new DLS.Description.PinDescription { Name = "A", BitCount = 1 }
                };
                testChip.OutputPins = new DLS.Description.PinDescription[]
                {
                    new DLS.Description.PinDescription { Name = "Y", BitCount = 1 }
                };
                
                // Start with empty arrays
                testChip.SubChips = new DLS.Description.SubChipDescription[0];
                testChip.Wires = new DLS.Description.WireDescription[0];
                
                var testSolution = new CompleteSolution("NOT Gate", "TestUser", 1, testChip);
                testSolution.UserName = "TestUser";
                
                Debug.Log($"[CreateTestScore] Created simple test solution with {testChip.SubChips.Length} subchips, {testChip.Wires.Length} wires");
                
                // Save solution to local storage
                var solutionId = EditorLocalStorage.SaveCompleteSolution(testSolution);
                Debug.Log($"[CreateTestScore] Saved solution with ID: {solutionId}");
                
                // Save a score that references this solution
                EditorLocalStorage.SaveScore("NOT Gate", 1, "TestUser", solutionId);
                Debug.Log("[CreateTestScore] Saved score with solution reference");
                
                // Verify the score was saved with the solution ID
                var scores = EditorLocalStorage.GetTopScores("NOT Gate", 10);
                Debug.Log($"[CreateTestScore] Retrieved {scores.Count} scores for NOT Gate");
                
                if (scores.Count > 0)
                {
                    var scoreData = (System.Collections.Generic.Dictionary<string, object>)scores[0];
                    var scoreSolutionId = scoreData["completeSolutionId"]?.ToString();
                    Debug.Log($"[CreateTestScore] Score has solution ID: {scoreSolutionId}");
                    
                    if (!string.IsNullOrEmpty(scoreSolutionId))
                    {
                        Debug.Log("[CreateTestScore] ✅ Test score created successfully with solution ID!");
                    }
                    else
                    {
                        Debug.LogError("[CreateTestScore] ❌ Score was created but has no solution ID");
                    }
                }
                else
                {
                    Debug.LogError("[CreateTestScore] ❌ No scores found after creation");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CreateTestScore] Failed to create test score: {ex.Message}");
            }
        }
        
        [ContextMenu("Clear All Data")]
        public void ClearAllData()
        {
            try
            {
                Debug.Log("[CreateTestScore] Clearing all data...");
                EditorLocalStorage.ClearAll();
                Debug.Log("[CreateTestScore] All data cleared!");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CreateTestScore] Failed to clear data: {ex.Message}");
            }
        }
        
        [ContextMenu("Create Fresh Test Data")]
        public void CreateFreshTestData()
        {
            try
            {
                Debug.Log("[CreateTestScore] Creating fresh test data...");
                
                // Clear all existing data first
                EditorLocalStorage.ClearAll();
                Debug.Log("[CreateTestScore] Cleared existing data");
                
                // Create new test data
                CreateTestScoreWithSolution();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CreateTestScore] Failed to create fresh test data: {ex.Message}");
            }
        }
    }
}
