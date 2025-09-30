using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Threading;

namespace DLS.Online
{
    /// <summary>
    /// Handles leaderboard operations: saving scores and retrieving top scores.
    /// </summary>
    public static class LeaderboardService
    {
        private const string COLLECTION_NAME = "scores";

        /// <summary>
        /// Save a score for a level. Optionally include screenshot and solution JSON.
        /// </summary>
        /// <param name="levelId">The level identifier</param>
        /// <param name="score">The score (lower is better)</param>
        /// <param name="optionalScreenshotPng">Optional screenshot as PNG bytes</param>
        /// <param name="optionalSolutionJson">Optional solution as JSON string</param>
        public static async Task SaveScoreAsync(string levelId, int score, 
            byte[] optionalScreenshotPng = null, string optionalSolutionJson = null)
        {
            try
            {
                Debug.Log($"[Leaderboard] Saving score for level {levelId}: {score}");
                
                // Skip Firebase operations in Editor to avoid crashes
                #if UNITY_EDITOR
                Debug.Log($"[Leaderboard] Editor mode - simulating score save for level {levelId} with score {score}");
                await Task.Delay(100); // Simulate network delay
                Debug.Log("[Leaderboard] Score save simulated successfully in Editor");
                return;
                #else
                
                // Add timeout to the entire operation
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                {
                    try
                    {
                        await SaveScoreInternalAsync(levelId, score, optionalScreenshotPng, optionalSolutionJson, cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.LogWarning("[Leaderboard] SaveScoreAsync timed out after 30 seconds");
                        throw new TimeoutException("Leaderboard save operation timed out");
                    }
                }
                #endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Leaderboard] Failed to save score: {ex.Message}");
                throw;
            }
        }
        
        private static async Task SaveScoreInternalAsync(string levelId, int score, 
            byte[] optionalScreenshotPng, string optionalSolutionJson, CancellationToken cancellationToken)
        {
            try
            {
                Debug.Log($"[Leaderboard] Starting Firebase save process...");
                
                // Ensure Firebase is initialized
                await FirebaseBootstrap.InitializeAsync();
                if (!FirebaseBootstrap.IsInitialized)
                {
                    throw new InvalidOperationException("Firebase not initialized");
                }

                // Get Firestore instance
                var db = FirebaseFirestore.DefaultInstance;
                if (db == null)
                {
                    throw new InvalidOperationException("Firestore not available");
                }

                // Create document reference
                var docRef = db.Collection(COLLECTION_NAME).Document();
                string docId = docRef.Id;
                Debug.Log($"[Leaderboard] Document ID: {docId}");

                // Prepare simple data without ServerTimestamp to avoid Android issues
                var data = new Dictionary<string, object>
                {
                    { "levelId", levelId },
                    { "userId", FirebaseBootstrap.UserId },
                    { "score", score },
                    { "submittedAt", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") }
                };

                // Handle optional data
                if (optionalScreenshotPng != null && optionalScreenshotPng.Length > 0)
                {
                    string imagePath = $"screenshots/{levelId}/{docId}.png";
                    data["solutionImagePath"] = imagePath;
                }

                if (!string.IsNullOrEmpty(optionalSolutionJson))
                {
                    string jsonPath = $"solutions/{levelId}/{docId}.json";
                    data["solutionJsonPath"] = jsonPath;
                }

                Debug.Log($"[Leaderboard] Writing to Firestore with {data.Count} fields...");
                
                // Use a simple approach with timeout
                var writeTask = docRef.SetAsync(data);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
                var completedTask = await Task.WhenAny(writeTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    Debug.LogWarning("[Leaderboard] Firestore write timed out after 15 seconds");
                    throw new TimeoutException("Firestore write operation timed out");
                }
                
                await writeTask;
                Debug.Log($"[Leaderboard] Successfully saved score for level {levelId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Leaderboard] Failed to save score: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get the top N scores for a level, ordered by lowest score first, then by newest submission.
        /// </summary>
        /// <param name="levelId">The level identifier</param>
        /// <param name="limit">Maximum number of scores to return (default: 10)</param>
        /// <returns>List of score entries</returns>
        public static async Task<List<ScoreEntry>> GetTopScoresAsync(string levelId, int limit = 10)
        {
            try
            {
                Debug.Log($"[Leaderboard] Getting top {limit} scores for level {levelId}");

                // Skip Firebase operations in Editor to avoid crashes
                #if UNITY_EDITOR
                Debug.Log($"[Leaderboard] Editor mode - simulating score retrieval for level {levelId}");
                await Task.Delay(100); // Simulate network delay
                
                // Return mock data for Editor
                var mockScores = new List<ScoreEntry>
                {
                    new ScoreEntry
                    {
                        id = "mock_1",
                        levelId = levelId,
                        userId = "anon",
                        score = 10,
                        submittedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                        solutionJsonPath = null,
                        solutionImagePath = null
                    },
                    new ScoreEntry
                    {
                        id = "mock_2", 
                        levelId = levelId,
                        userId = "anon",
                        score = 15,
                        submittedAtUtc = DateTime.UtcNow.AddMinutes(-10),
                        solutionJsonPath = null,
                        solutionImagePath = null
                    }
                };
                
                Debug.Log($"[Leaderboard] Returning {mockScores.Count} mock scores in Editor");
                return mockScores;
                #else
                // Ensure Firebase is initialized
                await FirebaseBootstrap.InitializeAsync();
                if (!FirebaseBootstrap.IsInitialized)
                {
                    throw new InvalidOperationException("Firebase not initialized");
                }

                var db = FirebaseFirestore.DefaultInstance;
                if (db == null)
                {
                    throw new InvalidOperationException("Firestore not available");
                }

                // Query scores for the level, ordered by score ascending, then by submittedAt descending
                var query = db.Collection(COLLECTION_NAME)
                    .WhereEqualTo("levelId", levelId)
                    .OrderBy("score")
                    .OrderByDescending("submittedAt")
                    .Limit(limit);

                var snapshot = await query.GetSnapshotAsync();

                var scores = new List<ScoreEntry>();

                foreach (var doc in snapshot.Documents)
                {
                    try
                    {
                        var data = doc.ToDictionary();
                        var scoreEntry = new ScoreEntry
                        {
                            id = doc.Id,
                            levelId = GetStringValue(data, "levelId"),
                            userId = GetStringValue(data, "userId"),
                            score = GetIntValue(data, "score"),
                            submittedAtUtc = GetTimestampValue(data, "submittedAt"),
                            solutionJsonPath = GetStringValue(data, "solutionJsonPath"),
                            solutionImagePath = GetStringValue(data, "solutionImagePath")
                        };

                        scores.Add(scoreEntry);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[Leaderboard] Failed to parse document {doc.Id}: {ex.Message}");
                    }
                }

                Debug.Log($"[Leaderboard] Retrieved {scores.Count} scores for level {levelId}");
                return scores;
                #endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Leaderboard] Failed to get scores: {ex.Message}");
                
                // Check if this is a composite index error
                if (ex.Message.Contains("index") || ex.Message.Contains("composite"))
                {
                    Debug.LogError("[Leaderboard] This query requires a composite index. " +
                        "Check the Firebase console for the index creation URL.");
                }
                
                throw;
            }
        }

        // Helper methods for safe data extraction
        private static string GetStringValue(Dictionary<string, object> data, string key)
        {
            return data.TryGetValue(key, out var value) && value != null ? value.ToString() : null;
        }

        private static int GetIntValue(Dictionary<string, object> data, string key)
        {
            if (data.TryGetValue(key, out var value) && value != null)
            {
                if (value is int intValue) return intValue;
                if (int.TryParse(value.ToString(), out int parsedValue)) return parsedValue;
            }
            return 0;
        }

        private static DateTime GetTimestampValue(Dictionary<string, object> data, string key)
        {
            try
            {
                if (data.TryGetValue(key, out var value))
                {
                    if (value is Timestamp timestamp)
                    {
                        return timestamp.ToDateTime().ToUniversalTime();
                    }
                    else if (value is DateTime dateTime)
                    {
                        return dateTime.ToUniversalTime();
                    }
                    else if (value != null)
                    {
                        Debug.LogWarning($"[Leaderboard] Unexpected timestamp type for {key}: {value.GetType()}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Leaderboard] Failed to parse timestamp for {key}: {ex.Message}");
            }
            return DateTime.UtcNow;
        }
    }
}
