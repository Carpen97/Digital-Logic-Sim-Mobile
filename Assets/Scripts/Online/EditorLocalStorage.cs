using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using DLS.Online;

namespace DLS.Online
{
    /// <summary>
    /// Local storage system for testing Firebase behavior in the Unity Editor.
    /// Simulates Firebase operations by saving/loading data locally.
    /// </summary>
    public static class EditorLocalStorage
    {
        private static readonly string STORAGE_PATH = Path.Combine(Application.persistentDataPath, "EditorLocalStorage");
        private static readonly string SCORES_FILE = Path.Combine(STORAGE_PATH, "scores.json");
        private static readonly string SOLUTIONS_FILE = Path.Combine(STORAGE_PATH, "solutions.json");
        
        private static Dictionary<string, object> _scores = new Dictionary<string, object>();
        private static Dictionary<string, CompleteSolution> _solutions = new Dictionary<string, CompleteSolution>();
        
        /// <summary>
        /// Initialize the local storage system
        /// </summary>
        public static void Initialize()
        {
            try
            {
                if (!Directory.Exists(STORAGE_PATH))
                {
                    Directory.CreateDirectory(STORAGE_PATH);
                    Debug.Log($"[EditorLocalStorage] Created storage directory: {STORAGE_PATH}");
                }
                
                LoadScores();
                LoadSolutions();
                
                // Auto-populate with mock data if empty
                if (_scores.Count == 0)
                {
                    InitializeMockData();
                }
                
                Debug.Log("[EditorLocalStorage] Local storage initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EditorLocalStorage] Failed to initialize: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Initialize mock leaderboard data for UI testing
        /// </summary>
        private static void InitializeMockData()
        {
            Debug.Log("[EditorLocalStorage] Populating mock leaderboard data for UI testing...");
            
            // Define common level IDs
            string[] levelIds = new[] { "NOT Gate", "AND Gate", "OR Gate", "XOR Gate", "NAND Gate", "NOR Gate" };
            
            // Mock user names with variety for UI testing
            string[] userNames = new[]
            {
                "Alice",
                "Bob",
                "Charlie",
                "Diana",
                "Eve",
                "Frank",
                "Grace",
                "Henry",
                "Ivy",
                "Jack",
                "Katherine_with_a_really_long_name_to_test_truncation",
                "短名", // Short Chinese characters
                "VeryLongUserNameThatShouldBeTruncatedInTheUI",
                "Player_12345",
                "🎮Gamer42",
                "Anonymous"
            };
            
            // Create mock scores for each level
            System.Random random = new System.Random(42); // Fixed seed for consistent mock data
            
            foreach (var levelId in levelIds)
            {
                // Create 10-15 scores per level with varying quality
                int scoreCount = random.Next(10, 16);
                
                for (int i = 0; i < scoreCount; i++)
                {
                    // Generate realistic scores (lower is better)
                    int baseScore = 5 + i * 2; // Starting from 5, increasing by 2
                    int scoreVariation = random.Next(-1, 3); // Add some randomness
                    int score = Math.Max(1, baseScore + scoreVariation);
                    
                    // Pick a random username
                    string userName = userNames[random.Next(userNames.Length)];
                    
                    // Create timestamps over the past week
                    DateTime submittedAt = DateTime.UtcNow.AddHours(-random.Next(1, 168)); // Last 7 days
                    
                    // Create mock solution ID (some entries have it, some don't)
                    string solutionId = (i % 3 == 0) ? Guid.NewGuid().ToString() : null;
                    
                    var scoreData = new
                    {
                        levelId = levelId,
                        score = score,
                        userName = userName,
                        completeSolutionId = solutionId,
                        submittedAt = submittedAt.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ"),
                        userId = $"MockUser_{i}"
                    };
                    
                    var scoreId = Guid.NewGuid().ToString();
                    _scores[scoreId] = scoreData;
                }
            }
            
            SaveScores();
            Debug.Log($"[EditorLocalStorage] Created {_scores.Count} mock scores across {levelIds.Length} levels for UI testing");
        }
        
        /// <summary>
        /// Save a score locally (simulates Firebase save)
        /// </summary>
        public static void SaveScore(string levelId, int score, string userName, string completeSolutionId = null)
        {
            try
            {
                var scoreData = new
                {
                    levelId = levelId,
                    score = score,
                    userName = userName,
                    completeSolutionId = completeSolutionId,
                    submittedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ"),
                    userId = "EditorTestUser"
                };
                
                var scoreId = Guid.NewGuid().ToString();
                _scores[scoreId] = scoreData;
                
                SaveScores();
                
                Debug.Log($"[EditorLocalStorage] Saved score locally: {scoreId} for level {levelId} with score {score}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EditorLocalStorage] Failed to save score: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Save a complete solution locally (simulates Firebase save)
        /// </summary>
        public static string SaveCompleteSolution(CompleteSolution solution)
        {
            try
            {
                var solutionId = Guid.NewGuid().ToString();
                _solutions[solutionId] = solution;
                
                SaveSolutions();
                
                Debug.Log($"[EditorLocalStorage] Saved complete solution locally: {solutionId} for level {solution.LevelId}");
                return solutionId;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EditorLocalStorage] Failed to save complete solution: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Get top scores for a level (simulates Firebase query)
        /// </summary>
        public static List<object> GetTopScores(string levelId, int limit = 10)
        {
            try
            {
                var levelScores = new List<object>();
                
                foreach (var kvp in _scores)
                {
                    var scoreData = kvp.Value;
                    // Use reflection to get properties since we're using anonymous objects
                    var scoreLevelId = GetPropertyValue(scoreData, "levelId")?.ToString();
                    
                    if (scoreLevelId == levelId)
                    {
                        var scoreEntry = new Dictionary<string, object>
                        {
                            ["id"] = kvp.Key,
                            ["levelId"] = GetPropertyValue(scoreData, "levelId"),
                            ["score"] = GetPropertyValue(scoreData, "score"),
                            ["userName"] = GetPropertyValue(scoreData, "userName"),
                            ["completeSolutionId"] = GetPropertyValue(scoreData, "completeSolutionId"),
                            ["submittedAt"] = GetPropertyValue(scoreData, "submittedAt"),
                            ["userId"] = GetPropertyValue(scoreData, "userId")
                        };
                        levelScores.Add(scoreEntry);
                    }
                }
                
                // Sort by score (lower is better)
                levelScores.Sort((a, b) => 
                {
                    var scoreA = Convert.ToInt32(((Dictionary<string, object>)a)["score"]);
                    var scoreB = Convert.ToInt32(((Dictionary<string, object>)b)["score"]);
                    return scoreA.CompareTo(scoreB);
                });
                
                // Take top N
                var result = levelScores.GetRange(0, Math.Min(limit, levelScores.Count));
                
                Debug.Log($"[EditorLocalStorage] Retrieved {result.Count} scores for level {levelId}");
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EditorLocalStorage] Failed to get top scores: {ex.Message}");
                return new List<object>();
            }
        }
        
        /// <summary>
        /// Get a complete solution by ID (simulates Firebase get)
        /// </summary>
        public static CompleteSolution GetCompleteSolution(string solutionId)
        {
            try
            {
                if (_solutions.TryGetValue(solutionId, out var solution))
                {
                    Debug.Log($"[EditorLocalStorage] Retrieved complete solution: {solutionId}");
                    return solution;
                }
                else
                {
                    Debug.LogWarning($"[EditorLocalStorage] Complete solution not found: {solutionId}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EditorLocalStorage] Failed to get complete solution: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Clear all local storage
        /// </summary>
        public static void ClearAll()
        {
            try
            {
                _scores.Clear();
                _solutions.Clear();
                
                if (File.Exists(SCORES_FILE))
                    File.Delete(SCORES_FILE);
                if (File.Exists(SOLUTIONS_FILE))
                    File.Delete(SOLUTIONS_FILE);
                
                Debug.Log("[EditorLocalStorage] Cleared all local storage");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EditorLocalStorage] Failed to clear storage: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Regenerate mock data (useful for testing different UI scenarios)
        /// </summary>
        public static void RegenerateMockData()
        {
            try
            {
                Debug.Log("[EditorLocalStorage] Regenerating mock data...");
                _scores.Clear();
                InitializeMockData();
                Debug.Log("[EditorLocalStorage] Mock data regenerated successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EditorLocalStorage] Failed to regenerate mock data: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get storage statistics
        /// </summary>
        public static (int scores, int solutions) GetStats()
        {
            return (_scores.Count, _solutions.Count);
        }
        
        #region Private Methods
        
        private static void LoadScores()
        {
            try
            {
                if (File.Exists(SCORES_FILE))
                {
                    var json = File.ReadAllText(SCORES_FILE);
                    // For simplicity, we'll recreate the scores from the JSON
                    // In a real implementation, you'd deserialize the JSON properly
                    Debug.Log($"[EditorLocalStorage] Loaded scores from {SCORES_FILE}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[EditorLocalStorage] Failed to load scores: {ex.Message}");
            }
        }
        
        private static void SaveScores()
        {
            try
            {
                var json = JsonUtility.ToJson(_scores);
                File.WriteAllText(SCORES_FILE, json);
                Debug.Log($"[EditorLocalStorage] Saved scores to {SCORES_FILE}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EditorLocalStorage] Failed to save scores: {ex.Message}");
            }
        }
        
        private static void LoadSolutions()
        {
            try
            {
                if (File.Exists(SOLUTIONS_FILE))
                {
                    var json = File.ReadAllText(SOLUTIONS_FILE);
                    // For simplicity, we'll recreate the solutions from the JSON
                    // In a real implementation, you'd deserialize the JSON properly
                    Debug.Log($"[EditorLocalStorage] Loaded solutions from {SOLUTIONS_FILE}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[EditorLocalStorage] Failed to load solutions: {ex.Message}");
            }
        }
        
        private static void SaveSolutions()
        {
            try
            {
                var json = JsonUtility.ToJson(_solutions);
                File.WriteAllText(SOLUTIONS_FILE, json);
                Debug.Log($"[EditorLocalStorage] Saved solutions to {SOLUTIONS_FILE}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EditorLocalStorage] Failed to save solutions: {ex.Message}");
            }
        }
        
        private static object GetPropertyValue(object obj, string propertyName)
        {
            try
            {
                var property = obj.GetType().GetProperty(propertyName);
                return property?.GetValue(obj);
            }
            catch
            {
                return null;
            }
        }
        
        #endregion
    }
}
