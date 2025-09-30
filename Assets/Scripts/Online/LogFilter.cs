using System;
using UnityEngine;

namespace DLS.Online
{
    /// <summary>
    /// A custom log handler that filters out verbose Firebase JNI logs.
    /// This helps reduce the massive amount of logging spam from Firebase operations.
    /// </summary>
    public class LogFilter : ILogHandler
    {
        private readonly ILogHandler _defaultLogHandler;
        private readonly bool _enableFirebaseFiltering;

        public LogFilter(bool enableFirebaseFiltering = true)
        {
            _defaultLogHandler = Debug.unityLogger.logHandler;
            _enableFirebaseFiltering = enableFirebaseFiltering;
        }

        public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {
            if (_enableFirebaseFiltering && ShouldFilterLog(format))
            {
                // Don't log filtered messages
                return;
            }

            // Log other messages normally
            _defaultLogHandler.LogFormat(logType, context, format, args);
        }

        public void LogException(Exception exception, UnityEngine.Object context)
        {
            // Always log exceptions
            _defaultLogHandler.LogException(exception, context);
        }

        /// <summary>
        /// Determine if a log message should be filtered out.
        /// </summary>
        private bool ShouldFilterLog(string message)
        {
            if (string.IsNullOrEmpty(message))
                return false;

            // Filter out Firebase JNI method lookup logs
            if (message.Contains("Looking up methods for com/google/firebase"))
                return true;
            
            if (message.Contains("Looking up class com/google/firebase"))
                return true;
            
            if (message.Contains("Looking up fields for com/google/firebase"))
                return true;
            
            if (message.Contains("Method com/google/firebase"))
                return true;
            
            if (message.Contains("Field com/google/firebase"))
                return true;
            
            if (message.Contains("Class com/google/firebase"))
                return true;
            
            if (message.Contains("Firebase.Firestore.FirestoreCppPINVOKE"))
                return true;
            
            if (message.Contains("Firebase.Firestore.FirestoreProxy"))
                return true;
            
            if (message.Contains("Firebase.Firestore.FirebaseFirestore"))
                return true;
            
            // Filter out repetitive async/task logs
            if (message.Contains("System.Runtime.CompilerServices.AsyncTaskMethodBuilder") && 
                message.Contains("SetResult"))
                return true;
            
            if (message.Contains("System.Threading.Tasks.Task") && 
                message.Contains("TrySetResult"))
                return true;
            
            if (message.Contains("System.Threading.Tasks.AwaitTaskContinuation") && 
                message.Contains("RunCallback"))
                return true;
            
            if (message.Contains("System.Threading.ExecutionContext") && 
                message.Contains("RunInternal"))
                return true;
            
            if (message.Contains("System.Runtime.CompilerServices.MoveNextRunner") && 
                message.Contains("Run"))
                return true;
            
            // Filter out repetitive Unity internal logs
            if (message.Contains("UnityEngine.DebugLogHandler") && 
                message.Contains("Internal_Log"))
                return true;
            
            // Don't filter other logs
            return false;
        }

        /// <summary>
        /// Install the log filter as the default log handler.
        /// </summary>
        public static void Install()
        {
            Debug.unityLogger.logHandler = new LogFilter();
            Debug.Log("[LogFilter] Custom log filter installed to reduce Firebase JNI spam");
        }

        /// <summary>
        /// Remove the log filter and restore the default handler.
        /// </summary>
        public static void Uninstall()
        {
            Debug.unityLogger.logHandler = Debug.unityLogger.logHandler;
            Debug.Log("[LogFilter] Custom log filter removed");
        }
    }
}
