using System;
using UnityEngine;
using Firebase;

namespace DLS.Online
{
    /// <summary>
    /// Configures Firebase logging levels to reduce verbosity.
    /// This helps prevent the massive amount of JNI method lookup logs.
    /// </summary>
    public static class FirebaseLoggingConfig
    {
        private static bool _isConfigured = false;

        /// <summary>
        /// Configure Firebase logging to reduce verbosity.
        /// Should be called early in the application lifecycle.
        /// </summary>
        public static void ConfigureLogging()
        {
            if (_isConfigured)
                return;

            try
            {
                Debug.Log("[FirebaseLogging] Configuring Firebase logging levels...");

                // Install the custom log filter to reduce Firebase JNI spam
                LogFilter.Install();

                // Configure Unity's stack trace logging to reduce verbosity
                try
                {
                    Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
                    Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.ScriptOnly);
                    Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.Full);
                    
                    Debug.Log("[FirebaseLogging] Unity stack trace logging configured");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[FirebaseLogging] Could not configure Unity logging: {ex.Message}");
                }

                _isConfigured = true;
                Debug.Log("[FirebaseLogging] Firebase logging configuration completed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseLogging] Failed to configure logging: {ex.Message}");
            }
        }


        /// <summary>
        /// Reset logging configuration (useful for testing).
        /// </summary>
        public static void ResetConfiguration()
        {
            _isConfigured = false;
            LogFilter.Uninstall();
            Debug.Log("[FirebaseLogging] Logging configuration reset");
        }
    }
}
