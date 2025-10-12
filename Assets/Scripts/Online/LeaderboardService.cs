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
        private const string COMPLETE_SOLUTIONS_COLLECTION = "completeSolutions";
        
        /// <summary>
        /// Set to true to use local mock storage in Editor instead of real Firebase.
        /// Useful for offline development or testing without Firebase connectivity.
        /// IMPORTANT: Firebase crashes in Unity Editor on Windows when Android is the build target.
        /// This flag is automatically set to true when in Editor with Android/iOS build target.
        /// </summary>
        public static bool UseLocalStorageInEditor
        {
            get
            {
#if UNITY_EDITOR
                // CRITICAL: Firebase C++ SDK crashes on Windows Editor when build target is Android
                // Automatically use local storage in Editor for mobile platforms to prevent crashes
                var buildTarget = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
                return buildTarget == UnityEditor.BuildTarget.Android || 
                       buildTarget == UnityEditor.BuildTarget.iOS;
#else
                return false;
#endif
            }
            set
            {
                // Property is read-only - value determined by platform
            }
        }

        /// <summary>
        /// Save a score for a level. Optionally include screenshot and solution JSON.
        /// </summary>
        /// <param name="levelId">The level identifier</param>
        /// <param name="score">The score (lower is better)</param>
        /// <param name="optionalScreenshotPng">Optional screenshot as PNG bytes</param>
        /// <param name="optionalSolutionJson">Optional solution as JSON string</param>
        /// <param name="userName">Optional user name for display</param>
        /// <param name="completeSolutionId">Optional complete solution document ID</param>
        public static async Task SaveScoreAsync(string levelId, int score, 
            byte[] optionalScreenshotPng = null, string optionalSolutionJson = null, string userName = null, string completeSolutionId = null)
        {
            try
            {
                Debug.Log($"[Leaderboard] Saving score for level {levelId}: {score}");
                
                // Use local storage in Editor if UseLocalStorageInEditor is enabled
                #if UNITY_EDITOR
                if (UseLocalStorageInEditor)
                {
                    Debug.Log($"[Leaderboard] Editor mode with local storage enabled - using mock storage");
                    
                    // Initialize local storage if needed
                    EditorLocalStorage.Initialize();
                    
                    // Save to local storage
                    EditorLocalStorage.SaveScore(levelId, score, userName ?? "EditorUser", completeSolutionId);
                    
                    Debug.Log($"[Leaderboard] Score saved to local storage for level {levelId} with score {score}");
                    await Task.Delay(100); // Simulate network delay
                    return;
                }
                else
                {
                    Debug.Log($"[Leaderboard] Editor mode with real Firebase enabled - connecting to Firebase");
                }
                #endif
                
                // Add timeout to the entire operation
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                {
                    try
                    {
                        await SaveScoreInternalAsync(levelId, score, optionalScreenshotPng, optionalSolutionJson, userName, completeSolutionId, cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.LogWarning("[Leaderboard] SaveScoreAsync timed out after 30 seconds");
                        throw new TimeoutException("Leaderboard save operation timed out");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Leaderboard] Failed to save score: {ex.Message}");
                throw;
            }
        }
        
        private static async Task SaveScoreInternalAsync(string levelId, int score, 
            byte[] optionalScreenshotPng, string optionalSolutionJson, string userName, string completeSolutionId, CancellationToken cancellationToken)
        {
            try
            {
                Debug.Log($"[Leaderboard] Starting Firebase save process...");
                Debug.Log($"[Leaderboard] Platform: {UnityEngine.Application.platform}");
                Debug.Log($"[Leaderboard] LevelId: {levelId}, Score: {score}, UserName: {userName}");
                
                // Ensure Firebase is initialized
                Debug.Log("[Leaderboard] Calling FirebaseBootstrap.InitializeAsync()...");
                await FirebaseBootstrap.InitializeAsync();
                Debug.Log($"[Leaderboard] Firebase initialization returned. IsInitialized: {FirebaseBootstrap.IsInitialized}");
                
                if (!FirebaseBootstrap.IsInitialized)
                {
                    throw new InvalidOperationException("Firebase not initialized");
                }

                // Get Firestore instance
                Debug.Log("[Leaderboard] Attempting to get Firestore instance...");
                var db = Firebase.Firestore.FirebaseFirestore.DefaultInstance;
                Debug.Log($"[Leaderboard] Firestore instance: {(db != null ? "OK" : "NULL")}");
                
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
                    { "userName", userName ?? "Anonymous" },
                    { "score", score },
                    { "submittedAt", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") }
                };

                // Add complete solution ID if provided
                if (!string.IsNullOrEmpty(completeSolutionId))
                {
                    data["completeSolutionId"] = completeSolutionId;
                }

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
                
                // IMPORTANT: Firestore writes can crash on Windows desktop builds due to Firebase C++ SDK issue
                // Wrap in try-catch to prevent app crash
                try
                {
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
                catch (Exception writeEx)
                {
                    Debug.LogError($"[Leaderboard] Firestore write failed (this may be a known Firebase SDK issue on Windows): {writeEx.Message}");
                    Debug.LogError($"[Leaderboard] Exception type: {writeEx.GetType().Name}");
                    
                    // Re-throw to propagate the error
                    throw new InvalidOperationException($"Failed to write score to Firestore: {writeEx.Message}", writeEx);
                }            }
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

                // Use local storage in Editor if UseLocalStorageInEditor is enabled
                #if UNITY_EDITOR
                if (UseLocalStorageInEditor)
                {
                    Debug.Log($"[Leaderboard] Editor mode with local storage enabled - using mock storage");
                    
                    // Initialize local storage if needed
                    EditorLocalStorage.Initialize();
                    
                    // Get scores from local storage
                    var localScores = EditorLocalStorage.GetTopScores(levelId, limit);
                    var scoreEntries = new List<ScoreEntry>();
                    
                    foreach (var scoreData in localScores)
                    {
                        var scoreDict = (Dictionary<string, object>)scoreData;
                        
                        var scoreEntry = new ScoreEntry
                        {
                            id = scoreDict["id"]?.ToString(),
                            levelId = scoreDict["levelId"]?.ToString(),
                            userId = scoreDict["userId"]?.ToString(),
                            userName = scoreDict["userName"]?.ToString(),
                            score = Convert.ToInt32(scoreDict["score"]),
                            submittedAtUtc = DateTime.Parse(scoreDict["submittedAt"]?.ToString() ?? DateTime.UtcNow.ToString()),
                            solutionJsonPath = null,
                            solutionImagePath = null,
                            completeSolutionId = scoreDict["completeSolutionId"]?.ToString()
                        };
                        scoreEntries.Add(scoreEntry);
                    }
                    
                    Debug.Log($"[Leaderboard] Retrieved {scoreEntries.Count} scores from local storage");
                    await Task.Delay(100); // Simulate network delay
                    return scoreEntries;
                }
                else
                {
                    Debug.Log($"[Leaderboard] Editor mode with real Firebase enabled - connecting to Firebase");
                }
                #endif
                
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
                            userName = GetStringValue(data, "userName"),
                            score = GetIntValue(data, "score"),
                            submittedAtUtc = GetTimestampValue(data, "submittedAt"),
                            solutionJsonPath = GetStringValue(data, "solutionJsonPath"),
                            solutionImagePath = GetStringValue(data, "solutionImagePath"),
                            completeSolutionId = GetStringValue(data, "completeSolutionId")
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
                    else if (value is string stringValue)
                    {
                        // Handle string timestamps from Firebase (ISO 8601 format)
                        if (DateTime.TryParse(stringValue, out DateTime parsedDateTime))
                        {
                            return parsedDateTime.ToUniversalTime();
                        }
                        else
                        {
                            Debug.LogWarning($"[Leaderboard] Failed to parse timestamp string '{stringValue}' for {key}");
                        }
                    }
                    else if (value != null)
                    {
                        Debug.LogWarning($"[Leaderboard] Unexpected timestamp type for {key}: {value.GetType()}, value: {value}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Leaderboard] Failed to parse timestamp for {key}: {ex.Message}");
            }
            return DateTime.UtcNow;
        }

        /// <summary>
        /// Save a complete solution with all custom chip definitions for full reproducibility.
        /// </summary>
        /// <param name="solution">The complete solution to save</param>
        /// <param name="optionalScreenshotPng">Optional screenshot as PNG bytes</param>
        /// <returns>Task<string> representing the async operation, returns the document ID</returns>
        public static async Task<string> SaveCompleteSolutionAsync(CompleteSolution solution, 
            byte[] optionalScreenshotPng = null)
        {
            try
            {
                Debug.Log($"[Leaderboard] Saving complete solution for level {solution.LevelId}: {solution.Score}");
                
                // Use local storage in Editor if UseLocalStorageInEditor is enabled
                #if UNITY_EDITOR
                if (UseLocalStorageInEditor)
                {
                    Debug.Log($"[Leaderboard] Editor mode with local storage enabled - using mock storage for complete solution");
                    
                    // Initialize local storage if needed
                    EditorLocalStorage.Initialize();
                    
                    // Save to local storage
                    var solutionId = EditorLocalStorage.SaveCompleteSolution(solution);
                    
                    Debug.Log($"[Leaderboard] Complete solution saved to local storage with ID: {solutionId}");
                    await Task.Delay(500); // Simulate network delay
                    return solutionId;
                }
                else
                {
                    Debug.Log($"[Leaderboard] Editor mode with real Firebase enabled - connecting to Firebase");
                }
                #endif
                
                // Add timeout to the entire operation
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60))) // Longer timeout for complete solutions
                {
                    try
                    {
                        return await SaveCompleteSolutionInternalAsync(solution, optionalScreenshotPng, cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.LogWarning("[Leaderboard] SaveCompleteSolutionAsync timed out after 60 seconds");
                        throw new TimeoutException("Complete solution save operation timed out");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Leaderboard] Failed to save complete solution: {ex.Message}");
                throw;
            }
        }
        
        private static async Task<string> SaveCompleteSolutionInternalAsync(CompleteSolution solution, 
            byte[] optionalScreenshotPng, CancellationToken cancellationToken)
        {
            try
            {
                Debug.Log($"[Leaderboard] Starting Firebase complete solution save process...");
                
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
                var docRef = db.Collection(COMPLETE_SOLUTIONS_COLLECTION).Document();
                string docId = docRef.Id;
                Debug.Log($"[Leaderboard] Complete solution document ID: {docId}");

                // Debug the solution before serialization
                Debug.Log($"[Leaderboard] Solution details before serialization:");
                Debug.Log($"[Leaderboard] - LevelId: {solution.LevelId}");
                Debug.Log($"[Leaderboard] - UserId: {solution.UserId}");
                Debug.Log($"[Leaderboard] - UserName: {solution.UserName}");
                Debug.Log($"[Leaderboard] - Score: {solution.Score}");
                Debug.Log($"[Leaderboard] - MainSolution: {(solution.MainSolution != null ? "exists" : "null")}");
                Debug.Log($"[Leaderboard] - CustomChipDefinitions count: {(solution.CustomChipDefinitions?.Count ?? 0)}");
                Debug.Log($"[Leaderboard] - Metadata: {(solution.Metadata != null ? "exists" : "null")}");
                
                // Serialize the complete solution
                string solutionJson = SolutionSerializer.SerializeCompleteSolution(solution);
                Debug.Log($"[Leaderboard] Serialized JSON length: {solutionJson.Length}");
                Debug.Log($"[Leaderboard] First 200 chars of serialized JSON: {solutionJson.Substring(0, Math.Min(200, solutionJson.Length))}");
                
                // Prepare data for Firestore
                var data = new Dictionary<string, object>
                {
                    { "levelId", solution.LevelId },
                    { "userId", solution.UserId },
                    { "userName", solution.UserName ?? "Anonymous" },
                    { "score", solution.Score },
                    { "submittedAt", solution.SubmittedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") },
                    { "solutionJson", solutionJson },
                    { "metadata", new Dictionary<string, object>
                        {
                            { "nandGateCount", solution.Metadata.NandGateCount },
                            { "totalComponents", solution.Metadata.TotalComponents },
                            { "wireCount", solution.Metadata.WireCount },
                            { "dlsVersion", solution.Metadata.DLSVersion },
                            { "customChipNames", solution.Metadata.CustomChipNames },
                            { "solutionSizeBytes", solution.Metadata.SolutionSizeBytes }
                        }
                    }
                };

                // Handle optional screenshot
                if (optionalScreenshotPng != null && optionalScreenshotPng.Length > 0)
                {
                    string imagePath = $"screenshots/{solution.LevelId}/{docId}.png";
                    data["solutionImagePath"] = imagePath;
                }

                Debug.Log($"[Leaderboard] Writing complete solution to Firestore with {data.Count} fields...");
                
                // IMPORTANT: Firestore writes can crash on Windows desktop builds due to Firebase C++ SDK issue
                // Wrap in try-catch to prevent app crash
                try
                {
                    // Use a simple approach with timeout
                    var writeTask = docRef.SetAsync(data);
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                    var completedTask = await Task.WhenAny(writeTask, timeoutTask);
                    
                    if (completedTask == timeoutTask)
                    {
                        Debug.LogWarning("[Leaderboard] Complete solution Firestore write timed out after 30 seconds");
                        throw new TimeoutException("Complete solution Firestore write operation timed out");
                    }
                    
                    await writeTask;
                    Debug.Log($"[Leaderboard] Successfully saved complete solution for level {solution.LevelId}");
                    return docId;
                }
                catch (Exception writeEx)
                {
                    Debug.LogError($"[Leaderboard] Firestore write failed (this may be a known Firebase SDK issue on Windows): {writeEx.Message}");
                    Debug.LogError($"[Leaderboard] Exception type: {writeEx.GetType().Name}");
                    Debug.LogError($"[Leaderboard] Stack trace: {writeEx.StackTrace}");
                    
                    // Return a fallback ID to indicate failure but prevent crash
                    throw new InvalidOperationException($"Failed to write complete solution to Firestore (Firebase SDK issue on Windows): {writeEx.Message}", writeEx);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Leaderboard] Failed to save complete solution: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get complete solutions for a level, ordered by score (best first).
        /// </summary>
        /// <param name="levelId">The level identifier</param>
        /// <param name="limit">Maximum number of solutions to return (default: 10)</param>
        /// <returns>List of complete solutions</returns>
        public static async Task<List<CompleteSolution>> GetCompleteSolutionsAsync(string levelId, int limit = 10)
        {
            try
            {
                Debug.Log($"[Leaderboard] Getting complete solutions for level {levelId}");

                // Use local storage in Editor if UseLocalStorageInEditor is enabled
                #if UNITY_EDITOR
                if (UseLocalStorageInEditor)
                {
                    Debug.Log($"[Leaderboard] Editor mode with local storage enabled - using mock data");
                    await Task.Delay(100); // Simulate network delay
                    
                    // Return mock data for Editor
                    var mockSolutions = new List<CompleteSolution>();
                    return mockSolutions;
                }
                else
                {
                    Debug.Log($"[Leaderboard] Editor mode with real Firebase enabled - connecting to Firebase");
                }
                #endif
                
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

                // Query complete solutions for the level, ordered by score ascending
                var query = db.Collection(COMPLETE_SOLUTIONS_COLLECTION)
                    .WhereEqualTo("levelId", levelId)
                    .OrderBy("score")
                    .Limit(limit);

                var snapshot = await query.GetSnapshotAsync();

                var solutions = new List<CompleteSolution>();

                foreach (var doc in snapshot.Documents)
                {
                    try
                    {
                        var data = doc.ToDictionary();
                        string solutionJson = GetStringValue(data, "solutionJson");
                        
                        if (!string.IsNullOrEmpty(solutionJson))
                        {
                            var solution = SolutionSerializer.DeserializeCompleteSolution(solutionJson);
                            solutions.Add(solution);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[Leaderboard] Failed to parse complete solution document {doc.Id}: {ex.Message}");
                    }
                }

                Debug.Log($"[Leaderboard] Retrieved {solutions.Count} complete solutions for level {levelId}");
                return solutions;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Leaderboard] Failed to get complete solutions: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get a specific complete solution by document ID.
        /// </summary>
        /// <param name="solutionId">The solution document ID</param>
        /// <returns>Complete solution or null if not found</returns>
        public static async Task<CompleteSolution> GetCompleteSolutionAsync(string solutionId)
        {
            try
            {
                Debug.Log($"[Leaderboard] Getting complete solution {solutionId}");

                // Use local storage in Editor if UseLocalStorageInEditor is enabled
                #if UNITY_EDITOR
                if (UseLocalStorageInEditor)
                {
                    Debug.Log($"[Leaderboard] Editor mode with local storage enabled - using mock storage");
                    
                    // Initialize local storage if needed
                    EditorLocalStorage.Initialize();
                    
                    // Load from local storage
                    var solution = EditorLocalStorage.GetCompleteSolution(solutionId);
                    
                    if (solution != null)
                    {
                        Debug.Log($"[Leaderboard] Loaded solution from local storage: {solutionId}");
                    }
                    else
                    {
                        Debug.LogWarning($"[Leaderboard] Solution not found in local storage: {solutionId}");
                    }
                    
                    await Task.Delay(200); // Simulate network delay
                    return solution;
                }
                else
                {
                    Debug.Log($"[Leaderboard] Editor mode with real Firebase enabled - connecting to Firebase");
                }
                #endif
                
                // Ensure Firebase is initialized
                await FirebaseBootstrap.InitializeAsync();
                if (!FirebaseBootstrap.IsInitialized)
                {
                    Debug.LogError("[Leaderboard] Firebase not initialized");
                    throw new InvalidOperationException("Firebase not initialized");
                }

                var db = FirebaseFirestore.DefaultInstance;
                if (db == null)
                {
                    Debug.LogError("[Leaderboard] Firestore not available");
                    throw new InvalidOperationException("Firestore not available");
                }

                Debug.Log($"[Leaderboard] Querying document {solutionId} from collection {COMPLETE_SOLUTIONS_COLLECTION}");
                var docRef = db.Collection(COMPLETE_SOLUTIONS_COLLECTION).Document(solutionId);
                var snapshot = await docRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                {
                    Debug.LogWarning($"[Leaderboard] Complete solution {solutionId} not found");
                    return null;
                }

                Debug.Log($"[Leaderboard] Document exists, extracting data");
                var data = snapshot.ToDictionary();
                string solutionJson = GetStringValue(data, "solutionJson");
                
                if (string.IsNullOrEmpty(solutionJson))
                {
                    Debug.LogWarning($"[Leaderboard] Complete solution {solutionId} has no solution data");
                    return null;
                }

                Debug.Log($"[Leaderboard] Found solution JSON, length: {solutionJson.Length}");
                Debug.Log($"[Leaderboard] First 200 chars of JSON: {solutionJson.Substring(0, Math.Min(200, solutionJson.Length))}");

                var deserializedSolution = SolutionSerializer.DeserializeCompleteSolution(solutionJson);
                if (deserializedSolution == null)
                {
                    Debug.LogError("[Leaderboard] Deserialization returned null");
                    return null;
                }

                Debug.Log($"[Leaderboard] Deserialized solution - LevelId: {deserializedSolution.LevelId}, MainSolution: {(deserializedSolution.MainSolution != null ? "exists" : "null")}");
                return deserializedSolution;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Leaderboard] Failed to get complete solution {solutionId}: {ex.Message}");
                Debug.LogError($"[Leaderboard] Stack trace: {ex.StackTrace}");
                return null;
            }
        }
    }
}
