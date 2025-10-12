using System;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using System.Threading;

namespace DLS.Online
{
    /// <summary>
    /// Handles Firebase initialization and authentication.
    /// On mobile devices, signs in anonymously. In Editor, uses "anon" user.
    /// </summary>
    public static class FirebaseBootstrap
    {
        private static Task _initializationTask;
        private static bool _isInitialized;
        private static string _userId = "anon";

        /// <summary>
        /// Safe to call multiple times. Returns the same task if already initializing.
        /// </summary>
        public static Task InitializeAsync()
        {
            if (_initializationTask != null)
                return _initializationTask;

            _initializationTask = InitializeInternalAsync();
            return _initializationTask;
        }

        /// <summary>
        /// True if Firebase has been successfully initialized.
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        /// <summary>
        /// User ID for the current session. "anon" in Editor, actual UID on mobile.
        /// </summary>
        public static string UserId => _userId;

        private static async Task InitializeInternalAsync()
        {
            try
            {
                // Log platform information
                Debug.Log($"[Firebase] Platform: {Application.platform}");
                Debug.Log($"[Firebase] Unity Version: {Application.unityVersion}");
                Debug.Log($"[Firebase] Is Editor: {Application.isEditor}");
                
                // Configure Firebase logging to reduce verbosity
                FirebaseLoggingConfig.ConfigureLogging();
                
                Debug.Log("[Firebase] Starting Firebase initialization...");

                // Check and fix dependencies with timeout
                Debug.Log("[Firebase] About to call CheckAndFixDependenciesAsync...");
                var dependencyTask = FirebaseApp.CheckAndFixDependenciesAsync();
                var dependencyStatus = await Task.Run(async () => 
                {
                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                    {
                        try
                        {
                            // Use Task.WhenAny for timeout instead of WaitAsync
                            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10), cts.Token);
                            var completedTask = await Task.WhenAny(dependencyTask, timeoutTask);
                            
                            if (completedTask == timeoutTask)
                            {
                                Debug.LogWarning("[Firebase] CheckAndFixDependenciesAsync timed out after 10 seconds");
                                return DependencyStatus.UnavailableOther;
                            }
                            
                            return await dependencyTask;
                        }
                        catch (OperationCanceledException)
                        {
                            Debug.LogWarning("[Firebase] CheckAndFixDependenciesAsync timed out after 10 seconds");
                            return DependencyStatus.UnavailableOther;
                        }
                    }
                });
                Debug.Log($"[Firebase] CheckAndFixDependenciesAsync completed: {dependencyStatus}");
                
                if (dependencyStatus != DependencyStatus.Available)
                {
                    Debug.LogError($"[Firebase] Firebase dependencies not available: {dependencyStatus}");
                    _userId = "anon"; // Fallback to anonymous user
                    _isInitialized = true; // Mark as initialized to prevent retries
                    return;
                }

                Debug.Log("[Firebase] Firebase dependencies are available");

                // Initialize Firebase app
                Debug.Log("[Firebase] About to get FirebaseApp.DefaultInstance...");
                FirebaseApp app = FirebaseApp.DefaultInstance;
                Debug.Log($"[Firebase] FirebaseApp.DefaultInstance retrieved: {app != null}");
                
                if (app == null)
                {
                    Debug.LogError("[Firebase] Failed to initialize Firebase app");
                    _userId = "anon"; // Fallback to anonymous user
                    _isInitialized = true; // Mark as initialized to prevent retries
                    return;
                }

                Debug.Log("[Firebase] Firebase app initialized successfully");

                // IMPORTANT: Disable Firestore persistence on Windows to prevent crashes
                // Source: https://github.com/firebase/quickstart-unity/issues/1284
                #if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
                try
                {
                    Debug.Log("[Firebase] Configuring Firestore settings for desktop platform...");
                    var db = Firebase.Firestore.FirebaseFirestore.DefaultInstance;
                    if (db != null)
                    {
                        Debug.Log("[Firebase] Disabling Firestore persistence to prevent Windows crashes...");
                        db.Settings.PersistenceEnabled = false;
                        Debug.Log("[Firebase] Firestore persistence disabled successfully");
                        
                        // Also clear any existing persistence cache that might be corrupted
                        Debug.Log("[Firebase] Clearing persistence cache...");
                        await db.ClearPersistenceAsync();
                        Debug.Log("[Firebase] Persistence cache cleared");
                    }
                    else
                    {
                        Debug.LogWarning("[Firebase] Firestore instance is null, skipping persistence configuration");
                    }
                }
                catch (Exception firestoreEx)
                {
                    Debug.LogWarning($"[Firebase] Failed to configure Firestore settings (non-critical): {firestoreEx.Message}");
                    // Continue anyway - this is a best-effort fix
                }
                #endif

                // Handle authentication - now enabled for all non-Editor platforms
                Debug.Log("[Firebase] About to start anonymous authentication...");
                try
                {
                    await SignInAnonymouslyAsync();
                    Debug.Log("[Firebase] Anonymous authentication completed");
                }
                catch (Exception authEx)
                {
                    Debug.LogError($"[Firebase] Authentication failed, using fallback: {authEx.Message}");
                    _userId = "anon"; // Fallback to anonymous user
                }

                _isInitialized = true;
                Debug.Log($"[Firebase] Initialization complete. UserId: {_userId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Firebase] Initialization failed: {ex.Message}");
                _userId = "anon"; // Fallback to anonymous user
                _isInitialized = true; // Mark as initialized to prevent retries
                // Don't re-throw to prevent the app from crashing
            }
        }

        private static async Task SignInAnonymouslyAsync()
        {
            try
            {
                Debug.Log("[Firebase] Starting anonymous authentication...");
                
                Debug.Log("[Firebase] About to get FirebaseAuth.DefaultInstance...");
                var auth = FirebaseAuth.DefaultInstance;
                Debug.Log($"[Firebase] FirebaseAuth.DefaultInstance retrieved: {auth != null}");
                
                Debug.Log("[Firebase] About to call SignInAnonymouslyAsync...");
                
                // Add timeout to authentication
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
                {
                    try
                    {
                        var authTask = auth.SignInAnonymouslyAsync();
                        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(15), cts.Token);
                        var completedTask = await Task.WhenAny(authTask, timeoutTask);
                        
                        if (completedTask == timeoutTask)
                        {
                            Debug.LogWarning("[Firebase] Anonymous authentication timed out after 15 seconds");
                            _userId = "anon";
                            return;
                        }
                        
                        var result = await authTask;
                        Debug.Log($"[Firebase] SignInAnonymouslyAsync completed: {result != null}");
                        
                        if (result?.User != null)
                        {
                            _userId = result.User.UserId;
                            Debug.Log($"[Firebase] Anonymous authentication successful. UID: {_userId}");
                        }
                        else
                        {
                            Debug.LogError("[Firebase] Anonymous authentication failed - no user returned");
                            _userId = "anon";
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.LogWarning("[Firebase] Anonymous authentication timed out after 15 seconds");
                        _userId = "anon";
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Firebase] Anonymous authentication failed: {ex.Message}");
                Debug.LogError($"[Firebase] Authentication exception type: {ex.GetType().Name}");
                Debug.LogError($"[Firebase] Authentication stack trace: {ex.StackTrace}");
                _userId = "anon";
                // Don't re-throw to prevent the app from crashing
            }
        }
    }
}
