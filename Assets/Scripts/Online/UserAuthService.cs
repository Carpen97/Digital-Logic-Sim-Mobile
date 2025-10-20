using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Firestore;

namespace DLS.Online
{
    /// <summary>
    /// Handles user authentication and username reservation.
    /// Prevents leaderboard impersonation by linking usernames to Firebase UIDs.
    /// 
    /// Security Model:
    /// - Each Firebase UID (anonymous auth) can claim ONE username
    /// - Usernames are unique across all users
    /// - Once claimed, a username cannot be changed (can be extended to allow changes with history)
    /// - Server-side Firestore rules enforce this binding
    /// </summary>
    public static class UserAuthService
    {
        private const string USERS_COLLECTION = "users";
        private const string USERNAMES_COLLECTION = "usernames"; // For fast username lookup
        
        // Cache for current user profile
        private static UserProfile _cachedProfile;
        private static bool _profileLoaded;
        
        /// <summary>
        /// User profile data structure stored in Firestore
        /// </summary>
        [Serializable]
        public class UserProfile
        {
            public string userId;           // Firebase UID (anonymous auth)
            public string username;         // Display name (unique, 3-20 chars)
            public string deviceId;         // Device identifier for additional security
            public DateTime createdAt;      // When the profile was created
            public DateTime lastLoginAt;    // Last login timestamp
            public string appVersion;       // App version when created
            public DateTime usernameChangedAt; // When username was last changed
            public string previousUsername; // Previous username (for history)
        }
        
        /// <summary>
        /// Username reservation record (for fast lookup)
        /// </summary>
        [Serializable]
        public class UsernameReservation
        {
            public string username;         // Lowercase username for case-insensitive lookup
            public string userId;           // Firebase UID that owns this username
            public DateTime reservedAt;     // When the username was claimed
        }
        
        /// <summary>
        /// Gets the current user's profile from cache or Firestore
        /// </summary>
        public static async Task<UserProfile> GetCurrentUserProfileAsync()
        {
            if (_profileLoaded && _cachedProfile != null)
            {
                return _cachedProfile;
            }
            
            await FirebaseBootstrap.InitializeAsync();
            if (!FirebaseBootstrap.IsInitialized)
            {
                Debug.LogError("[UserAuth] Firebase not initialized");
                return null;
            }
            
            string userId = FirebaseBootstrap.UserId;
            if (string.IsNullOrEmpty(userId) || userId == "anon")
            {
                Debug.LogWarning("[UserAuth] User not authenticated or in editor mode");
                return null;
            }
            
            try
            {
                var db = FirebaseFirestore.DefaultInstance;
                var docRef = db.Collection(USERS_COLLECTION).Document(userId);
                var snapshot = await docRef.GetSnapshotAsync();
                
                if (snapshot.Exists)
                {
                    var data = snapshot.ToDictionary();
                    _cachedProfile = new UserProfile
                    {
                        userId = GetStringValue(data, "userId"),
                        username = GetStringValue(data, "username"),
                        deviceId = GetStringValue(data, "deviceId"),
                        createdAt = GetTimestampValue(data, "createdAt"),
                        lastLoginAt = GetTimestampValue(data, "lastLoginAt"),
                        appVersion = GetStringValue(data, "appVersion"),
                        usernameChangedAt = GetTimestampValue(data, "usernameChangedAt"),
                        previousUsername = GetStringValue(data, "previousUsername")
                    };
                    
                    _profileLoaded = true;
                    Debug.Log($"[UserAuth] Loaded profile for user {userId}: {_cachedProfile.username}");
                    return _cachedProfile;
                }
                else
                {
                    Debug.Log($"[UserAuth] No profile found for user {userId}");
                    _profileLoaded = true;
                    _cachedProfile = null;
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UserAuth] Failed to load user profile: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Checks if a username is available for claiming
        /// </summary>
        public static async Task<bool> IsUsernameAvailableAsync(string username)
        {
            if (string.IsNullOrEmpty(username))
                return false;
            
            string normalizedUsername = NormalizeUsername(username);
            
            try
            {
                await FirebaseBootstrap.InitializeAsync();
                if (!FirebaseBootstrap.IsInitialized)
                {
                    Debug.LogError("[UserAuth] Firebase not initialized");
                    return false;
                }
                
                var db = FirebaseFirestore.DefaultInstance;
                var docRef = db.Collection(USERNAMES_COLLECTION).Document(normalizedUsername);
                var snapshot = await docRef.GetSnapshotAsync();
                
                bool available = !snapshot.Exists;
                Debug.Log($"[UserAuth] Username '{username}' available: {available}");
                return available;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UserAuth] Failed to check username availability: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Claims a username for the current user
        /// </summary>
        public static async Task<ClaimResult> ClaimUsernameAsync(string username)
        {
            // Validate username format
            var validation = ValidateUsername(username);
            if (!validation.isValid)
            {
                return new ClaimResult 
                { 
                    success = false, 
                    error = validation.error 
                };
            }
            
            await FirebaseBootstrap.InitializeAsync();
            if (!FirebaseBootstrap.IsInitialized)
            {
                return new ClaimResult 
                { 
                    success = false, 
                    error = "Firebase not initialized" 
                };
            }
            
            string userId = FirebaseBootstrap.UserId;
            if (string.IsNullOrEmpty(userId) || userId == "anon")
            {
                return new ClaimResult 
                { 
                    success = false, 
                    error = "User not authenticated" 
                };
            }
            
            // Check if user already has a username
            var existingProfile = await GetCurrentUserProfileAsync();
            if (existingProfile != null && !string.IsNullOrEmpty(existingProfile.username))
            {
                return new ClaimResult 
                { 
                    success = false, 
                    error = $"You already have a username: {existingProfile.username}" 
                };
            }
            
            string normalizedUsername = NormalizeUsername(username);
            
            try
            {
                var db = FirebaseFirestore.DefaultInstance;
                
                // Use a transaction to ensure atomicity
                await db.RunTransactionAsync(async (transaction) =>
                {
                    // Check if username is already taken
                    var usernameDocRef = db.Collection(USERNAMES_COLLECTION).Document(normalizedUsername);
                    var usernameSnapshot = await transaction.GetSnapshotAsync(usernameDocRef);
                    
                    if (usernameSnapshot.Exists)
                    {
                        throw new InvalidOperationException("Username already taken");
                    }
                    
                    // Reserve the username
                    var reservationData = new Dictionary<string, object>
                    {
                        { "username", normalizedUsername },
                        { "userId", userId },
                        { "reservedAt", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") }
                    };
                    transaction.Set(usernameDocRef, reservationData);
                    
                    // Create or update user profile
                    var userDocRef = db.Collection(USERS_COLLECTION).Document(userId);
                    var profileData = new Dictionary<string, object>
                    {
                        { "userId", userId },
                        { "username", username }, // Store original case
                        { "deviceId", SystemInfo.deviceUniqueIdentifier },
                        { "createdAt", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") },
                        { "lastLoginAt", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") },
                        { "appVersion", Application.version }
                    };
                    transaction.Set(userDocRef, profileData);
                    
                    return Task.CompletedTask;
                });
                
                // Update cache
                _cachedProfile = new UserProfile
                {
                    userId = userId,
                    username = username,
                    deviceId = SystemInfo.deviceUniqueIdentifier,
                    createdAt = DateTime.UtcNow,
                    lastLoginAt = DateTime.UtcNow,
                    appVersion = Application.version
                };
                _profileLoaded = true;
                
                Debug.Log($"[UserAuth] Successfully claimed username: {username}");
                return new ClaimResult { success = true };
            }
            catch (InvalidOperationException ex)
            {
                Debug.LogWarning($"[UserAuth] Username claim failed: {ex.Message}");
                return new ClaimResult 
                { 
                    success = false, 
                    error = ex.Message 
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UserAuth] Failed to claim username: {ex.Message}");
                return new ClaimResult 
                { 
                    success = false, 
                    error = "Failed to claim username. Please try again." 
                };
            }
        }
        
        /// <summary>
        /// Updates the username for the current user (changes all existing solutions)
        /// </summary>
        public static async Task<bool> UpdateUserProfileAsync(string newUsername)
        {
            // Validate new username
            var validation = ValidateUsername(newUsername);
            if (!validation.isValid)
            {
                Debug.LogError($"[UserAuth] Invalid username: {validation.error}");
                return false;
            }
            
            await FirebaseBootstrap.InitializeAsync();
            if (!FirebaseBootstrap.IsInitialized)
            {
                Debug.LogError("[UserAuth] Firebase not initialized");
                return false;
            }
            
            string userId = FirebaseBootstrap.UserId;
            if (string.IsNullOrEmpty(userId) || userId == "anon")
            {
                Debug.LogError("[UserAuth] User not authenticated");
                return false;
            }
            
            // Get current profile to check if user has a username
            var currentProfile = await GetCurrentUserProfileAsync();
            if (currentProfile == null || string.IsNullOrEmpty(currentProfile.username))
            {
                Debug.LogError("[UserAuth] User doesn't have an existing username to change");
                return false;
            }
            
            string oldUsername = currentProfile.username;
            string normalizedNewUsername = NormalizeUsername(newUsername);
            string normalizedOldUsername = NormalizeUsername(oldUsername);
            
            // Check if new username is already taken (and not by current user)
            if (normalizedNewUsername != normalizedOldUsername)
            {
                bool available = await IsUsernameAvailableAsync(newUsername);
                if (!available)
                {
                    Debug.LogError($"[UserAuth] Username '{newUsername}' is already taken");
                    return false;
                }
            }
            
            try
            {
                var db = FirebaseFirestore.DefaultInstance;
                
                // If username changed, update username reservation
                if (normalizedNewUsername != normalizedOldUsername)
                {
                    // Remove old username reservation
                    var oldUsernameDocRef = db.Collection(USERNAMES_COLLECTION).Document(normalizedOldUsername);
                    await oldUsernameDocRef.DeleteAsync();
                    
                    // Add new username reservation
                    var newUsernameDocRef = db.Collection(USERNAMES_COLLECTION).Document(normalizedNewUsername);
                    var reservationData = new Dictionary<string, object>
                    {
                        { "username", normalizedNewUsername },
                        { "userId", userId },
                        { "reservedAt", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") }
                    };
                    await newUsernameDocRef.SetAsync(reservationData);
                }
                
                // Update user profile
                var userDocRef = db.Collection(USERS_COLLECTION).Document(userId);
                var profileData = new Dictionary<string, object>
                {
                    { "username", newUsername }, // Store original case
                    { "lastLoginAt", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") }
                };
                await userDocRef.SetAsync(profileData, SetOptions.MergeAll);
                
                // Update cache
                if (_cachedProfile != null)
                {
                    _cachedProfile.username = newUsername;
                    _cachedProfile.lastLoginAt = DateTime.UtcNow;
                }
                
                Debug.Log($"[UserAuth] Successfully updated username from '{oldUsername}' to '{newUsername}'");
                
                // TODO: Update all existing solutions with new username
                // This would involve updating all score entries and complete solutions
                Debug.Log($"[UserAuth] TODO: Update all existing solutions with new username");
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UserAuth] Failed to update username: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Updates the last login timestamp for the current user
        /// </summary>
        public static async Task UpdateLastLoginAsync()
        {
            try
            {
                await FirebaseBootstrap.InitializeAsync();
                if (!FirebaseBootstrap.IsInitialized)
                    return;
                
                string userId = FirebaseBootstrap.UserId;
                if (string.IsNullOrEmpty(userId) || userId == "anon")
                    return;
                
                var db = FirebaseFirestore.DefaultInstance;
                var docRef = db.Collection(USERS_COLLECTION).Document(userId);
                
                var updateData = new Dictionary<string, object>
                {
                    { "lastLoginAt", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") }
                };
                
                await docRef.UpdateAsync(updateData);
                Debug.Log($"[UserAuth] Updated last login for user {userId}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UserAuth] Failed to update last login: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Validates a username for claiming
        /// </summary>
        public static (bool isValid, string error) ValidateUsername(string username)
        {
            if (string.IsNullOrEmpty(username))
                return (false, "Username cannot be empty");
            
            if (username.Length < 3)
                return (false, "Username must be at least 3 characters");
            
            if (username.Length > 20)
                return (false, "Username must be 20 characters or less");
            
            // Check for valid characters (letters, numbers, spaces, hyphens, underscores)
            foreach (char c in username)
            {
                if (!char.IsLetterOrDigit(c) && c != ' ' && c != '-' && c != '_')
                {
                    return (false, "Username can only contain letters, numbers, spaces, hyphens, and underscores");
                }
            }
            
            // Check for reserved names
            string lowerName = username.ToLower().Trim();
            string[] reservedNames = { "anonymous", "guest", "admin", "moderator", "system", "deleted", "unknown" };
            foreach (var reserved in reservedNames)
            {
                if (lowerName == reserved)
                {
                    return (false, $"Username '{username}' is reserved");
                }
            }
            
            // Check for inappropriate patterns (basic filter)
            if (username.Contains("  ")) // Multiple spaces
                return (false, "Username cannot contain multiple consecutive spaces");
            
            if (username.StartsWith(" ") || username.EndsWith(" "))
                return (false, "Username cannot start or end with spaces");
            
            return (true, null);
        }
        
        /// <summary>
        /// Normalizes a username for case-insensitive lookups
        /// </summary>
        private static string NormalizeUsername(string username)
        {
            return username.ToLower().Trim();
        }
        
        /// <summary>
        /// Clears the cached profile (useful for testing or logout)
        /// </summary>
        public static void ClearCache()
        {
            _cachedProfile = null;
            _profileLoaded = false;
        }
        
        /// <summary>
        /// Checks if user can change username (rate limiting)
        /// </summary>
        public static async Task<(bool canChange, string reason)> CanChangeUsernameAsync()
        {
            var profile = await GetCurrentUserProfileAsync();
            
            if (profile == null)
            {
                return (false, "No profile found");
            }
            
            // Check if user has changed username recently (e.g., within 30 days)
            if (profile.usernameChangedAt != default(DateTime))
            {
                var daysSinceLastChange = (DateTime.UtcNow - profile.usernameChangedAt).TotalDays;
                
                if (daysSinceLastChange < 30)
                {
                    int daysRemaining = 30 - (int)daysSinceLastChange;
                    return (false, $"You can change your username again in {daysRemaining} days");
                }
            }
            
            return (true, null);
        }
        
        /// <summary>
        /// Changes the user's username and updates all their leaderboard entries
        /// </summary>
        public static async Task<ClaimResult> ChangeUsernameAsync(string newUsername)
        {
            // Validate new username
            var validation = ValidateUsername(newUsername);
            if (!validation.isValid)
            {
                return new ClaimResult { success = false, error = validation.error };
            }
            
            await FirebaseBootstrap.InitializeAsync();
            if (!FirebaseBootstrap.IsInitialized)
            {
                return new ClaimResult { success = false, error = "Firebase not initialized" };
            }
            
            string userId = FirebaseBootstrap.UserId;
            if (string.IsNullOrEmpty(userId) || userId == "anon")
            {
                return new ClaimResult { success = false, error = "User not authenticated" };
            }
            
            // Get current profile
            var currentProfile = await GetCurrentUserProfileAsync();
            if (currentProfile == null)
            {
                return new ClaimResult { success = false, error = "No profile found" };
            }
            
            string oldUsername = currentProfile.username;
            string normalizedNewUsername = NormalizeUsername(newUsername);
            string normalizedOldUsername = NormalizeUsername(oldUsername);
            
            // Don't allow changing to the same username (case variations)
            if (normalizedNewUsername == normalizedOldUsername)
            {
                return new ClaimResult { success = false, error = "This is already your username" };
            }
            
            try
            {
                var db = FirebaseFirestore.DefaultInstance;
                
                // Step 1: Check if new username is available
                Debug.Log($"[UserAuth] Checking availability of '{newUsername}'");
                var newUsernameDocRef = db.Collection(USERNAMES_COLLECTION).Document(normalizedNewUsername);
                var newUsernameSnapshot = await newUsernameDocRef.GetSnapshotAsync();
                
                if (newUsernameSnapshot.Exists)
                {
                    return new ClaimResult { success = false, error = "Username already taken" };
                }
                
                // Step 2: Reserve new username
                Debug.Log($"[UserAuth] Reserving new username '{newUsername}'");
                var reservationData = new Dictionary<string, object>
                {
                    { "username", normalizedNewUsername },
                    { "userId", userId },
                    { "reservedAt", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") },
                    { "previousUsername", normalizedOldUsername }
                };
                await newUsernameDocRef.SetAsync(reservationData);
                
                // Step 3: Update user profile
                Debug.Log($"[UserAuth] Updating user profile");
                var userDocRef = db.Collection(USERS_COLLECTION).Document(userId);
                var profileUpdateData = new Dictionary<string, object>
                {
                    { "username", newUsername }, // Store original case
                    { "previousUsername", oldUsername },
                    { "usernameChangedAt", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") },
                    { "lastLoginAt", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") }
                };
                await userDocRef.UpdateAsync(profileUpdateData);
                
                // Step 4: Update all leaderboard entries
                Debug.Log($"[UserAuth] Updating leaderboard entries");
                await UpdateLeaderboardEntriesAsync(db, userId, newUsername);
                
                // Step 5: Update all complete solutions
                Debug.Log($"[UserAuth] Updating complete solutions");
                await UpdateCompleteSolutionsAsync(db, userId, newUsername);
                
                // Step 6: Delete old username reservation
                Debug.Log($"[UserAuth] Releasing old username '{oldUsername}'");
                var oldUsernameDocRef = db.Collection(USERNAMES_COLLECTION).Document(normalizedOldUsername);
                await oldUsernameDocRef.DeleteAsync();
                
                // Clear cache to force reload
                ClearCache();
                
                Debug.Log($"[UserAuth] Successfully changed username from '{oldUsername}' to '{newUsername}'");
                return new ClaimResult { success = true };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UserAuth] Failed to change username: {ex.Message}");
                Debug.LogError($"[UserAuth] Stack trace: {ex.StackTrace}");
                return new ClaimResult { success = false, error = "Failed to change username. Please try again." };
            }
        }
        
        /// <summary>
        /// Updates all scores in the leaderboard for a user
        /// </summary>
        private static async Task UpdateLeaderboardEntriesAsync(FirebaseFirestore db, string userId, string newUsername)
        {
            try
            {
                // Query all scores by this user
                var scoresQuery = db.Collection("scores").WhereEqualTo("userId", userId);
                var scoresSnapshot = await scoresQuery.GetSnapshotAsync();
                
                Debug.Log($"[UserAuth] Found {scoresSnapshot.Count} leaderboard entries to update");
                
                // Update each score
                int updatedCount = 0;
                foreach (var scoreDoc in scoresSnapshot.Documents)
                {
                    try
                    {
                        var updateData = new Dictionary<string, object>
                        {
                            { "userName", newUsername }
                        };
                        
                        // Also update verifiedUsername if it exists
                        var scoreData = scoreDoc.ToDictionary();
                        if (scoreData.ContainsKey("verifiedUsername"))
                        {
                            updateData["verifiedUsername"] = newUsername;
                        }
                        
                        await scoreDoc.Reference.UpdateAsync(updateData);
                        updatedCount++;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[UserAuth] Failed to update score {scoreDoc.Id}: {ex.Message}");
                    }
                }
                
                Debug.Log($"[UserAuth] Updated {updatedCount}/{scoresSnapshot.Count} leaderboard entries");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UserAuth] Failed to update leaderboard entries: {ex.Message}");
                // Don't throw - username change already committed, this is cleanup
            }
        }
        
        /// <summary>
        /// Updates all complete solutions for a user
        /// </summary>
        private static async Task UpdateCompleteSolutionsAsync(FirebaseFirestore db, string userId, string newUsername)
        {
            try
            {
                // Query all complete solutions by this user
                var solutionsQuery = db.Collection("completeSolutions").WhereEqualTo("userId", userId);
                var solutionsSnapshot = await solutionsQuery.GetSnapshotAsync();
                
                Debug.Log($"[UserAuth] Found {solutionsSnapshot.Count} complete solutions to update");
                
                // Update each solution
                int updatedCount = 0;
                foreach (var solutionDoc in solutionsSnapshot.Documents)
                {
                    try
                    {
                        var updateData = new Dictionary<string, object>
                        {
                            { "userName", newUsername }
                        };
                        
                        await solutionDoc.Reference.UpdateAsync(updateData);
                        updatedCount++;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[UserAuth] Failed to update solution {solutionDoc.Id}: {ex.Message}");
                    }
                }
                
                Debug.Log($"[UserAuth] Updated {updatedCount}/{solutionsSnapshot.Count} complete solutions");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UserAuth] Failed to update complete solutions: {ex.Message}");
                // Don't throw - username change already committed, this is cleanup
            }
        }
        
        // Helper methods for safe data extraction
        private static string GetStringValue(Dictionary<string, object> data, string key)
        {
            return data.TryGetValue(key, out var value) && value != null ? value.ToString() : null;
        }
        
        private static DateTime GetTimestampValue(Dictionary<string, object> data, string key)
        {
            try
            {
                if (data.TryGetValue(key, out var value))
                {
                    if (value is Timestamp timestamp)
                        return timestamp.ToDateTime().ToUniversalTime();
                    if (value is DateTime dateTime)
                        return dateTime.ToUniversalTime();
                    if (value is string stringValue && DateTime.TryParse(stringValue, out DateTime parsedDateTime))
                        return parsedDateTime.ToUniversalTime();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UserAuth] Failed to parse timestamp for {key}: {ex.Message}");
            }
            return DateTime.UtcNow;
        }
        
        /// <summary>
        /// Result of a username claim operation
        /// </summary>
        public struct ClaimResult
        {
            public bool success;
            public string error;
        }
    }
}

