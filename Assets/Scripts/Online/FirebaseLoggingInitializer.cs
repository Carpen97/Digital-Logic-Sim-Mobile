using UnityEngine;

namespace DLS.Online
{
    /// <summary>
    /// MonoBehaviour that initializes Firebase logging configuration early in the application lifecycle.
    /// Attach this to a GameObject in your scene to automatically configure logging.
    /// </summary>
    public class FirebaseLoggingInitializer : MonoBehaviour
    {
        [Header("Logging Configuration")]
        [SerializeField] private bool _enableFirebaseFiltering = true;
        [SerializeField] private bool _configureOnAwake = true;
        [SerializeField] private bool _showConfigurationLogs = true;

        private void Awake()
        {
            if (_configureOnAwake)
            {
                ConfigureLogging();
            }
        }

        private void Start()
        {
            if (!_configureOnAwake)
            {
                ConfigureLogging();
            }
        }

        /// <summary>
        /// Configure Firebase logging to reduce verbosity.
        /// </summary>
        public void ConfigureLogging()
        {
            try
            {
                if (_showConfigurationLogs)
                {
                    Debug.Log("[FirebaseLoggingInitializer] Configuring Firebase logging...");
                }

                // Configure the logging system
                FirebaseLoggingConfig.ConfigureLogging();

                if (_showConfigurationLogs)
                {
                    Debug.Log("[FirebaseLoggingInitializer] Firebase logging configured successfully");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FirebaseLoggingInitializer] Failed to configure logging: {ex.Message}");
            }
        }

        /// <summary>
        /// Reset logging configuration (useful for testing).
        /// </summary>
        public void ResetLogging()
        {
            try
            {
                FirebaseLoggingConfig.ResetConfiguration();
                
                if (_showConfigurationLogs)
                {
                    Debug.Log("[FirebaseLoggingInitializer] Logging configuration reset");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FirebaseLoggingInitializer] Failed to reset logging: {ex.Message}");
            }
        }

        /// <summary>
        /// Toggle Firebase filtering on/off at runtime.
        /// </summary>
        public void ToggleFirebaseFiltering()
        {
            _enableFirebaseFiltering = !_enableFirebaseFiltering;
            
            if (_enableFirebaseFiltering)
            {
                ConfigureLogging();
            }
            else
            {
                ResetLogging();
            }
            
            Debug.Log($"[FirebaseLoggingInitializer] Firebase filtering: {(_enableFirebaseFiltering ? "Enabled" : "Disabled")}");
        }

        private void OnDestroy()
        {
            // Optionally reset logging when the component is destroyed
            // This is useful for testing or if you want to restore normal logging
            // Uncomment the line below if you want this behavior:
            // ResetLogging();
        }
    }
}
