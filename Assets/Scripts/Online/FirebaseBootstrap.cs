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
            // Skip Firebase initialization in Editor to avoid crashes
            #if UNITY_EDITOR
            Debug.Log("[Firebase] Editor mode - skipping Firebase initialization to avoid crashes");
            _isInitialized = true;
            _userId = "anon";
            return Task.CompletedTask;
            #else
            if (_initializationTask != null)
                return _initializationTask;

            _initializationTask = InitializeInternalAsync();
            return _initializationTask;
            #endif
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

                // Handle authentication based on platform
#if UNITY_ANDROID || UNITY_IOS
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
#else
                // In Editor/desktop, skip authentication
                Debug.Log("[Firebase] Running in Editor/desktop - skipping authentication");
                _userId = "anon";
#endif

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

#if UNITY_ANDROID || UNITY_IOS
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
#endif
    }
}
