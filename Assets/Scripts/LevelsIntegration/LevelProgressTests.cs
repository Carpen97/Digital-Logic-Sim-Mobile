using System;
using UnityEngine;
using DLS.Levels;
using DLS.Game;
using DLS.Description;

namespace DLS.Game.LevelsIntegration
{
    /// <summary>
    /// Simple tests for the LevelProgressService to verify level progress saving/loading functionality.
    /// These can be run in the Unity editor to test the level progress system.
    /// </summary>
    public static class LevelProgressTests
    {
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void RunBasicTests()
        {
            Debug.Log("Running LevelProgressService basic tests...");
            
            // Test 1: Save and load level progress
            TestSaveAndLoadProgress();
            
            // Test 2: Check if level has progress
            TestHasLevelProgress();
            
            // Test 3: Clear level progress
            TestClearLevelProgress();
            
            // Test 4: Error handling
            TestErrorHandling();
            
            Debug.Log("LevelProgressService tests completed.");
        }
        
        static void TestSaveAndLoadProgress()
        {
            Debug.Log("Testing save and load level progress...");
            
            try
            {
                // Create a mock chip instance for testing
                var testChip = CreateMockChipInstance();
                string testLevelId = "test_level_001";
                
                // Test saving progress
                LevelProgressService.SaveLevelProgress(testLevelId, testChip);
                Debug.Log("✓ Successfully saved level progress");
                
                // Test loading progress
                var loadedProgress = LevelProgressService.LoadLevelProgress(testLevelId);
                if (loadedProgress != null)
                {
                    Debug.Log("✓ Successfully loaded level progress");
                    Debug.Log($"  - Loaded chip name: {loadedProgress.Name}");
                    Debug.Log($"  - Input pins: {loadedProgress.InputPins?.Length ?? 0}");
                    Debug.Log($"  - Output pins: {loadedProgress.OutputPins?.Length ?? 0}");
                    Debug.Log($"  - SubChips: {loadedProgress.SubChips?.Length ?? 0}");
                    Debug.Log($"  - Wires: {loadedProgress.Wires?.Length ?? 0}");
                }
                else
                {
                    Debug.LogError("✗ Failed to load level progress");
                }
                
                // Clean up test data
                LevelProgressService.ClearLevelProgress(testLevelId);
            }
            catch (Exception ex)
            {
                Debug.LogError($"✗ TestSaveAndLoadProgress failed: {ex.Message}");
            }
        }
        
        static void TestHasLevelProgress()
        {
            Debug.Log("Testing has level progress check...");
            
            try
            {
                string testLevelId = "test_level_002";
                
                // Initially should not have progress
                bool hasProgress = LevelProgressService.HasLevelProgress(testLevelId);
                if (!hasProgress)
                {
                    Debug.Log("✓ Correctly detected no progress initially");
                }
                else
                {
                    Debug.LogError("✗ Incorrectly detected progress when none exists");
                }
                
                // Create and save progress
                var testChip = CreateMockChipInstance();
                LevelProgressService.SaveLevelProgress(testLevelId, testChip);
                
                // Now should have progress
                hasProgress = LevelProgressService.HasLevelProgress(testLevelId);
                if (hasProgress)
                {
                    Debug.Log("✓ Correctly detected progress after saving");
                }
                else
                {
                    Debug.LogError("✗ Failed to detect progress after saving");
                }
                
                // Clean up
                LevelProgressService.ClearLevelProgress(testLevelId);
            }
            catch (Exception ex)
            {
                Debug.LogError($"✗ TestHasLevelProgress failed: {ex.Message}");
            }
        }
        
        static void TestClearLevelProgress()
        {
            Debug.Log("Testing clear level progress...");
            
            try
            {
                string testLevelId = "test_level_003";
                
                // Save some progress
                var testChip = CreateMockChipInstance();
                LevelProgressService.SaveLevelProgress(testLevelId, testChip);
                
                // Verify it exists
                bool hasProgress = LevelProgressService.HasLevelProgress(testLevelId);
                if (!hasProgress)
                {
                    Debug.LogError("✗ Progress not saved correctly");
                    return;
                }
                
                // Clear progress
                LevelProgressService.ClearLevelProgress(testLevelId);
                
                // Verify it's cleared
                hasProgress = LevelProgressService.HasLevelProgress(testLevelId);
                if (!hasProgress)
                {
                    Debug.Log("✓ Successfully cleared level progress");
                }
                else
                {
                    Debug.LogError("✗ Failed to clear level progress");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"✗ TestClearLevelProgress failed: {ex.Message}");
            }
        }
        
        static void TestErrorHandling()
        {
            Debug.Log("Testing error handling...");
            
            try
            {
                // Test with null parameters
                LevelProgressService.SaveLevelProgress(null, null);
                LevelProgressService.SaveLevelProgress("", null);
                LevelProgressService.SaveLevelProgress("test", null);
                
                // Test loading non-existent progress
                var result = LevelProgressService.LoadLevelProgress("non_existent_level");
                if (result == null)
                {
                    Debug.Log("✓ Correctly handled non-existent level progress");
                }
                else
                {
                    Debug.LogError("✗ Incorrectly returned data for non-existent level");
                }
                
                // Test clearing non-existent progress
                LevelProgressService.ClearLevelProgress("non_existent_level");
                Debug.Log("✓ Correctly handled clearing non-existent progress");
                
                Debug.Log("✓ Error handling tests passed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"✗ TestErrorHandling failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Create a simple mock chip instance for testing
        /// </summary>
        static DevChipInstance CreateMockChipInstance()
        {
            var chip = new DevChipInstance();
            
            // Add some mock elements (this is a simplified version for testing)
            // In a real scenario, you'd add actual pins, subchips, and wires
            
            return chip;
        }
    }
}
