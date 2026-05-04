using System;
using System.IO;
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

        static bool _firestoreConfigured;

        /// <summary>
        /// Configures Firestore (disables persistence on desktop) before first use.
        /// Call this before any Firestore access. Safe to call multiple times.
        /// Deferred from init to reduce uWS::HttpSocket::upgrade crash risk on Windows (firebase-unity-sdk#1291).
        /// </summary>
        public static void EnsureFirestoreConfigured()
        {
            if (!_isInitialized || _firestoreConfigured) return;
            _firestoreConfigured = true;
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
            try
            {
                var db = Firebase.Firestore.FirebaseFirestore.DefaultInstance;
                if (db != null && db.Settings.PersistenceEnabled)
                {
                    db.Settings.PersistenceEnabled = false;
                    Debug.Log("[Firebase] Firestore persistence disabled (deferred config)");
                }
            }
            catch { /* best-effort */ }
#endif
        }

        /// <summary>
        /// Call after SignOut so the next InitializeAsync performs a fresh sign-in.
        /// Used when user logs out of Project Sharing.
        /// </summary>
        public static void ResetAfterSignOut()
        {
            _initializationTask = null;
            _isInitialized = false;
            _userId = "anon";
        }

        /// <summary>
        /// Refresh UserId from the current Firebase Auth state.
        /// Call after CreateUserWithEmail or SignInWithEmail (when not using anonymous).
        /// </summary>
        public static void RefreshUserIdFromAuth()
        {
            try
            {
                var user = FirebaseAuth.DefaultInstance?.CurrentUser;
                _userId = user?.UserId ?? "anon";
            }
            catch
            {
                _userId = "anon";
            }
        }

        /// <summary>
        /// Deletes Firestore and Firebase heartbeat cache folders before init.
        /// Corrupted cache causes uWS::HttpSocket::upgrade crash on app restart (firebase-unity-sdk#1291).
        /// </summary>
        static void TryClearFirebaseCacheFolders()
        {
            try
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrEmpty(localAppData)) return;

                foreach (var folderName in new[] { "firestore", "firebase-heartbeat" })
                {
                    var path = Path.Combine(localAppData, folderName);
                    if (Directory.Exists(path))
                    {
                        try
                        {
                            Directory.Delete(path, recursive: true);
                            Debug.Log($"[Firebase] Cleared cache folder: {path}");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[Firebase] Could not clear {path}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Firebase] Cache cleanup failed: {ex.Message}");
            }
        }

        private static async Task InitializeInternalAsync()
        {
            try
            {
                // Skip Firebase in Unity Editor to prevent crashes (uWS::HttpSocket::upgrade).
                // Opt-in: DLS → Project Sharing → Use real Firebase in Editor (testing).
                if (Application.isEditor && !Application.isBatchMode)
                {
                    if (PlayerPrefs.GetInt("DLS.UseFirebaseInEditor", 0) != 1)
                    {
                        Debug.Log("[Firebase] Skipping init in Editor (mock data). Enable via DLS menu to test real Firebase.");
                        _userId = "anon";
                        _isInitialized = true;
                        return;
                    }
                    Debug.Log("[Firebase] Editor mode with real Firebase enabled - initializing...");
                }

                // Log platform information
                Debug.Log($"[Firebase] Platform: {Application.platform}");
                Debug.Log($"[Firebase] Unity Version: {Application.unityVersion}");
                Debug.Log($"[Firebase] Is Editor: {Application.isEditor}");

                // Clear Firestore/heartbeat cache on Windows (Editor or build) to prevent uWS crash on restart.
                if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
                    TryClearFirebaseCacheFolders();

                // Configure Firebase logging to reduce verbosity
                FirebaseLoggingConfig.ConfigureLogging();

                Debug.Log("[Firebase] Starting Firebase initialization...");

                // Check and fix dependencies - use main thread to reduce crash risk
                Debug.Log("[Firebase] About to call CheckAndFixDependenciesAsync...");
                var dependencyTask = FirebaseApp.CheckAndFixDependenciesAsync();
                DependencyStatus dependencyStatus;
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
                {
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(15), cts.Token);
                    var completedTask = await Task.WhenAny(dependencyTask, timeoutTask);

                    if (completedTask == timeoutTask)
                    {
                        Debug.LogWarning("[Firebase] CheckAndFixDependenciesAsync timed out after 15 seconds");
                        dependencyStatus = DependencyStatus.UnavailableOther;
                    }
                    else
                    {
                        dependencyStatus = await dependencyTask;
                    }
                }
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

                // NOTE: Firestore config is DEFERRED to first actual use (EnsureFirestoreConfigured).
                // Touching FirebaseFirestore.DefaultInstance during init can trigger uWS::HttpSocket::upgrade
                // crash on some Windows configs (firebase-unity-sdk#1291). Deferring reduces crash risk.

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
