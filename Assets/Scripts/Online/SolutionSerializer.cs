using System;
using System.Collections.Generic;
using System.Linq;
using DLS.Description;
using DLS.Game;
using DLS.SaveSystem;
using DLS.Simulation;
using Newtonsoft.Json;
using UnityEngine;

namespace DLS.Online
{
    /// <summary>
    /// Handles serialization and deserialization of complete solutions with all custom chip definitions.
    /// </summary>
    public static class SolutionSerializer
    {
        /// <summary>
        /// Creates a complete solution from the current level state.
        /// </summary>
        /// <param name="levelId">The level identifier</param>
        /// <param name="score">The solution score</param>
        /// <param name="mainSolution">The main solution chip description</param>
        /// <param name="chipLibrary">The chip library containing all available chips</param>
        /// <returns>Complete solution with all custom chip definitions</returns>
        public static CompleteSolution CreateCompleteSolution(
            string levelId, 
            int score, 
            ChipDescription mainSolution,
            ChipLibrary chipLibrary,
            string userName = null)
        {
            if (string.IsNullOrEmpty(levelId))
                throw new ArgumentException("Level ID cannot be null or empty", nameof(levelId));
            if (mainSolution == null)
                throw new ArgumentNullException(nameof(mainSolution));
            if (chipLibrary == null)
                throw new ArgumentNullException(nameof(chipLibrary));

            var userId = FirebaseBootstrap.UserId;
            var solution = new CompleteSolution(levelId, userId, score, mainSolution);
            solution.UserName = userName ?? "Anonymous";
            
            // Get all custom chips referenced in the solution
            var customChipNames = SolutionConflictResolver.GetAllReferencedCustomChips(mainSolution, chipLibrary);
            solution.Metadata.CustomChipNames = customChipNames.ToList();
            
            // Collect all custom chip definitions
            foreach (var chipName in customChipNames)
            {
                if (chipLibrary.TryGetChipDescription(chipName, out ChipDescription chipDescription))
                {
                    // Create a copy to avoid modifying the original
                    var chipCopy = Saver.CloneChipDescription(chipDescription);
                    
                    // Update references to use prefixed names for conflict resolution
                    SolutionConflictResolver.UpdateChipReferences(chipCopy, userId, chipLibrary);
                    
                    // Store with prefixed name
                    var prefixedName = SolutionConflictResolver.ResolveChipNameConflict(chipName, userId);
                    solution.CustomChipDefinitions[prefixedName] = chipCopy;
                }
                else
                {
                    Debug.LogWarning($"[SolutionSerializer] Custom chip '{chipName}' not found in library");
                }
            }
            
            // Update main solution references to use prefixed names
            var mainSolutionCopy = Saver.CloneChipDescription(mainSolution);
            SolutionConflictResolver.UpdateChipReferences(mainSolutionCopy, userId, chipLibrary);
            solution.MainSolution = mainSolutionCopy;
            
            // Calculate metadata
            CalculateSolutionMetadata(solution, chipLibrary);
            
            return solution;
        }
        
                /// <summary>
                /// Serializes a complete solution to JSON string.
                /// </summary>
                /// <param name="solution">The complete solution to serialize</param>
                /// <returns>JSON string representation</returns>
                public static string SerializeCompleteSolution(CompleteSolution solution)
                {
                    if (solution == null)
                        throw new ArgumentNullException(nameof(solution));
                        
                    try
                    {
                        Debug.Log($"[SolutionSerializer] Serializing CompleteSolution:");
                        Debug.Log($"[SolutionSerializer] - LevelId: '{solution.LevelId}'");
                        Debug.Log($"[SolutionSerializer] - UserId: '{solution.UserId}'");
                        Debug.Log($"[SolutionSerializer] - UserName: '{solution.UserName}'");
                        Debug.Log($"[SolutionSerializer] - Score: {solution.Score}");
                        Debug.Log($"[SolutionSerializer] - MainSolution: {(solution.MainSolution != null ? $"exists (Name: {solution.MainSolution.Name})" : "null")}");
                        Debug.Log($"[SolutionSerializer] - CustomChipDefinitions: {(solution.CustomChipDefinitions != null ? $"{solution.CustomChipDefinitions.Count} items" : "null")}");
                        Debug.Log($"[SolutionSerializer] - Metadata: {(solution.Metadata != null ? "exists" : "null")}");
                        
                        // Use Newtonsoft.Json directly for complex objects with circular reference handling
                        var settings = new JsonSerializerSettings
                        {
                            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                            Formatting = Formatting.Indented
                        };
                        string json = JsonConvert.SerializeObject(solution, settings);
                        Debug.Log($"[SolutionSerializer] Newtonsoft.Json result length: {json.Length}");
                        Debug.Log($"[SolutionSerializer] Newtonsoft.Json result: {json.Substring(0, Math.Min(200, json.Length))}");
                        
                        return json;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[SolutionSerializer] Failed to serialize complete solution: {ex.Message}");
                        Debug.LogError($"[SolutionSerializer] Stack trace: {ex.StackTrace}");
                        throw;
                    }
                }
        
                /// <summary>
                /// Deserializes a complete solution from JSON string.
                /// </summary>
                /// <param name="json">The JSON string to deserialize</param>
                /// <returns>Complete solution object</returns>
                public static CompleteSolution DeserializeCompleteSolution(string json)
                {
                    if (string.IsNullOrEmpty(json))
                        throw new ArgumentException("JSON string cannot be null or empty", nameof(json));
                        
                    try
                    {
                        // Use Newtonsoft.Json directly for complex objects with circular reference handling
                        var settings = new JsonSerializerSettings
                        {
                            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                        };
                        return JsonConvert.DeserializeObject<CompleteSolution>(json, settings);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[SolutionSerializer] Failed to deserialize complete solution: {ex.Message}");
                        throw;
                    }
                }
        
        /// <summary>
        /// Creates a complete solution from the current project state.
        /// </summary>
        /// <param name="levelId">The level identifier</param>
        /// <param name="score">The solution score</param>
        /// <param name="userName">The user name for display</param>
        /// <returns>Complete solution with all custom chip definitions</returns>
        public static CompleteSolution CreateCompleteSolutionFromCurrentProject(string levelId, int score, string userName = null)
        {
            try
            {
                var project = Project.ActiveProject;
                if (project == null)
                    throw new InvalidOperationException("No active project found");
                
                if (project.ViewedChip == null)
                    throw new InvalidOperationException("No viewed chip found in project");
                
                Debug.Log($"[SolutionSerializer] Creating complete solution for level {levelId} with score {score}");
                Debug.Log($"[SolutionSerializer] Project: {project.description.ProjectName}, ViewedChip: {project.ViewedChip.SimChip?.Name ?? "null"}");
                
                var mainSolution = DescriptionCreator.CreateChipDescription(project.ViewedChip);
                if (mainSolution == null)
                    throw new InvalidOperationException("Failed to create chip description from viewed chip");
                
                Debug.Log($"[SolutionSerializer] Created main solution chip: {mainSolution.Name}");
                
                return CreateCompleteSolution(levelId, score, mainSolution, project.chipLibrary, userName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SolutionSerializer] Failed to create complete solution from current project: {ex.Message}");
                Debug.LogError($"[SolutionSerializer] Stack trace: {ex.StackTrace}");
                throw; // Re-throw to be caught by the upload method
            }
        }
        
        /// <summary>
        /// Loads a complete solution into the current project.
        /// </summary>
        /// <param name="solution">The complete solution to load</param>
        /// <param name="chipLibrary">The chip library to add custom chips to</param>
        /// <returns>True if loading was successful</returns>
        public static bool LoadCompleteSolution(CompleteSolution solution, ChipLibrary chipLibrary)
        {
            if (solution == null)
            {
                Debug.LogError("[SolutionSerializer] Complete solution is null");
                return false;
            }
            if (chipLibrary == null)
            {
                Debug.LogError("[SolutionSerializer] Chip library is null");
                return false;
            }
                
            try
            {
                Debug.Log($"[SolutionSerializer] Loading complete solution for level {solution.LevelId}");
                Debug.Log($"[SolutionSerializer] Current level state - IsActive: {DLS.Game.LevelsIntegration.LevelManager.Instance?.IsActive}, Current: {DLS.Game.LevelsIntegration.LevelManager.Instance?.Current?.name}");
                
                // Check if main solution exists
                if (solution.MainSolution == null)
                {
                    Debug.LogError("[SolutionSerializer] Main solution is null");
                    return false;
                }
                
                // Restore original chip names in the main solution
                var mainSolution = Saver.CloneChipDescription(solution.MainSolution);
                if (mainSolution == null)
                {
                    Debug.LogError("[SolutionSerializer] Failed to clone main solution");
                    return false;
                }
                
                SolutionConflictResolver.RestoreChipReferences(mainSolution);
                
                // Add all custom chip definitions to the library
                if (solution.CustomChipDefinitions != null)
                {
                    foreach (var kvp in solution.CustomChipDefinitions)
                    {
                        var prefixedName = kvp.Key;
                        var chipDescription = kvp.Value;
                        
                        if (chipDescription != null)
                        {
                            // Restore original name
                            var originalName = SolutionConflictResolver.RestoreOriginalChipName(prefixedName);
                            chipDescription.Name = originalName;
                            SolutionConflictResolver.RestoreChipReferences(chipDescription);
                            
                            // Add to library (this will handle conflicts by replacing existing)
                            chipLibrary.NotifyChipSaved(chipDescription);
                        }
                    }
                }
                
                // Load the main solution into the current project
                var project = Project.ActiveProject;
                if (project == null)
                {
                    Debug.LogError("[SolutionSerializer] No active project found");
                    return false;
                }
                
                Debug.Log($"[SolutionSerializer] Creating chip instance for {mainSolution.Name}");
                Debug.Log($"[SolutionSerializer] Main solution has {mainSolution.InputPins?.Length ?? 0} input pins, {mainSolution.OutputPins?.Length ?? 0} output pins");
                Debug.Log($"[SolutionSerializer] Main solution has {mainSolution.SubChips?.Length ?? 0} subchips, {mainSolution.Wires?.Length ?? 0} wires");
                
                // Create a new chip instance from the solution
                var (devChip, anyElementFailedToLoad) = DevChipInstance.LoadFromDescriptionTest(mainSolution, chipLibrary);
                
                if (devChip == null)
                {
                    Debug.LogError("[SolutionSerializer] Failed to create chip instance");
                    return false;
                }
                
                Debug.Log($"[SolutionSerializer] Created devChip with {devChip.Elements.Count} elements, {devChip.Wires.Count} wires");
                
                if (anyElementFailedToLoad)
                {
                    Debug.LogWarning("[SolutionSerializer] Some elements failed to load in the solution");
                }
                
                Debug.Log($"[SolutionSerializer] Setting active chip: {mainSolution.Name}");
                
                // Set as the active chip using the public method
                project.controller = new ChipInteractionController(project);
                project.LoadDevChipOrCreateNewIfDoesntExist(mainSolution.Name);
                
                Debug.Log($"[SolutionSerializer] Building simulation for {mainSolution.Name}");
                
                // Build simulation
                var simChip = Simulator.BuildSimChip(mainSolution, chipLibrary);
                if (simChip != null)
                {
                    devChip.SetSimChip(simChip);
                    Debug.Log("[SolutionSerializer] Solution loaded successfully");
                    
                    // Enter viewing mode to show the circuit like Edit Tool → View
                    Debug.Log("[SolutionSerializer] Entering viewing mode for solution");
                    project.EnterViewModeForSolution(devChip);
                }
                else
                {
                    Debug.LogWarning("[SolutionSerializer] Failed to build simulation, but chip loaded");
                }
                
                // Exit level mode to allow free editing
                if (DLS.Game.LevelsIntegration.LevelManager.Instance != null && DLS.Game.LevelsIntegration.LevelManager.Instance.IsActive)
                {
                    Debug.Log("[SolutionSerializer] Exiting level mode to allow free editing");
                    DLS.Game.LevelsIntegration.LevelManager.Instance.ExitLevel();
                    Debug.Log($"[SolutionSerializer] Level mode exited successfully - IsActive: {DLS.Game.LevelsIntegration.LevelManager.Instance.IsActive}, Current: {DLS.Game.LevelsIntegration.LevelManager.Instance.Current?.name}");
                }
                else
                {
                    Debug.Log("[SolutionSerializer] No active level to exit");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SolutionSerializer] Failed to load complete solution: {ex.Message}");
                Debug.LogError($"[SolutionSerializer] Stack trace: {ex.StackTrace}");
                return false;
            }
        }
        
        /// <summary>
        /// Calculates metadata for a complete solution.
        /// </summary>
        /// <param name="solution">The solution to calculate metadata for</param>
        /// <param name="chipLibrary">The chip library for reference</param>
        private static void CalculateSolutionMetadata(CompleteSolution solution, ChipLibrary chipLibrary)
        {
            if (solution?.MainSolution == null)
                return;
                
            try
            {
                // Count components in main solution
                solution.Metadata.TotalComponents = CountComponents(solution.MainSolution);
                solution.Metadata.WireCount = solution.MainSolution.Wires?.Length ?? 0;
                
                // Count NAND gates using the simulation adapter
                var adapter = new MobileSimulationAdapter();
                solution.Metadata.NandGateCount = adapter.CountNandGates();
                
                // Calculate solution size
                var json = SerializeCompleteSolution(solution);
                solution.Metadata.SolutionSizeBytes = System.Text.Encoding.UTF8.GetByteCount(json);
                
                Debug.Log($"[SolutionSerializer] Solution metadata: {solution.Metadata.TotalComponents} components, " +
                         $"{solution.Metadata.WireCount} wires, {solution.Metadata.NandGateCount} NAND gates, " +
                         $"{solution.Metadata.SolutionSizeBytes} bytes");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SolutionSerializer] Failed to calculate metadata: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Counts the total number of components in a chip description.
        /// </summary>
        /// <param name="chipDescription">The chip description to count</param>
        /// <returns>Total component count</returns>
        private static int CountComponents(ChipDescription chipDescription)
        {
            if (chipDescription == null)
                return 0;
                
            int count = 0;
            
            // Count sub-chips
            if (chipDescription.SubChips != null)
            {
                count += chipDescription.SubChips.Length;
            }
            
            // Count displays
            if (chipDescription.Displays != null)
            {
                count += chipDescription.Displays.Length;
            }
            
            return count;
        }
    }
}
