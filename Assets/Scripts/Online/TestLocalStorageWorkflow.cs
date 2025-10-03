using UnityEngine;
using DLS.Online;

namespace DLS.Online
{
    /// <summary>
    /// Simple test script to create test data for the local storage workflow
    /// </summary>
    public class TestLocalStorageWorkflow : MonoBehaviour
    {
        [ContextMenu("Create Test Data")]
        public void CreateTestData()
        {
            try
            {
                Debug.Log("[TestLocalStorageWorkflow] Creating test data...");
                
                // Initialize local storage
                EditorLocalStorage.Initialize();
                
                // Create a test solution
                var testChip = new DLS.Description.ChipDescription();
                testChip.Name = "TestNOTGate";
                var testSolution = new CompleteSolution("NOT Gate", "TestUser", 1, testChip);
                testSolution.UserName = "EditorTestUser";
                
                // Save solution to local storage
                var solutionId = EditorLocalStorage.SaveCompleteSolution(testSolution);
                Debug.Log($"[TestLocalStorageWorkflow] Saved solution with ID: {solutionId}");
                
                // Save a score that references this solution
                EditorLocalStorage.SaveScore("NOT Gate", 1, "EditorTestUser", solutionId);
                Debug.Log("[TestLocalStorageWorkflow] Saved score with solution reference");
                
                // Test loading the solution
                var loadedSolution = EditorLocalStorage.GetCompleteSolution(solutionId);
                if (loadedSolution != null)
                {
                    Debug.Log($"[TestLocalStorageWorkflow] Successfully loaded solution: {loadedSolution.LevelId}");
                }
                else
                {
                    Debug.LogError("[TestLocalStorageWorkflow] Failed to load solution");
                }
                
                // Test getting scores
                var scores = EditorLocalStorage.GetTopScores("NOT Gate", 10);
                Debug.Log($"[TestLocalStorageWorkflow] Retrieved {scores.Count} scores for NOT Gate");
                
                Debug.Log("[TestLocalStorageWorkflow] Test data creation complete!");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TestLocalStorageWorkflow] Failed to create test data: {ex.Message}");
            }
        }
        
        [ContextMenu("Clear Test Data")]
        public void ClearTestData()
        {
            try
            {
                Debug.Log("[TestLocalStorageWorkflow] Clearing test data...");
                EditorLocalStorage.ClearAll();
                Debug.Log("[TestLocalStorageWorkflow] Test data cleared!");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TestLocalStorageWorkflow] Failed to clear test data: {ex.Message}");
            }
        }
    }
}
