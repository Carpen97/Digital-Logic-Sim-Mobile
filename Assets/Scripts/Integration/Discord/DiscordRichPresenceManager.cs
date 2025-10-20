#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
#define DISCORD_SUPPORTED
#endif

using UnityEngine;
using DLS.Description;

#if DISCORD_SUPPORTED
using DiscordRPC;
using DiscordRPC.Logging;
#endif

namespace DLS.Integration.Discord
{
	/// <summary>
	/// Manages Discord Rich Presence integration for Digital Logic Sim.
	/// PC-only feature (Windows/Mac/Linux).
	/// </summary>
	public class DiscordRichPresenceManager : MonoBehaviour
	{
		public static DiscordRichPresenceManager Instance { get; private set; }
		private const string APPLICATION_ID = "1428865072792342528";

		#if DISCORD_SUPPORTED
		private DiscordRpcClient client;
		private bool isInitialized = false;
		private bool isEnabled = false;
		#endif

		void Awake()
		{
			// Singleton pattern
			if (Instance == null)
			{
				Instance = this;
				DontDestroyOnLoad(gameObject);
			}
			else
			{
				Destroy(gameObject);
				return;
			}

			#if DISCORD_SUPPORTED
			// Initialize Discord if enabled in settings
			CheckAndInitialize();
			#else
			Debug.Log("[Discord] Rich Presence not available on this platform");
			#endif
		}

		#if DISCORD_SUPPORTED
		/// <summary>
		/// Check user settings and initialize Discord if enabled
		/// </summary>
		private void CheckAndInitialize()
		{
			if (DLS.Game.Main.ActiveAppSettings.EnableDiscordRichPresence)
			{
				InitializeDiscord();
			}
			else
			{
				Debug.Log("[Discord] Rich Presence disabled in user settings");
			}
		}

		/// <summary>
		/// Initialize Discord Rich Presence client
		/// </summary>
		private void InitializeDiscord()
		{
			if (isInitialized) return;

			try
			{
				// Create Discord RPC client
				client = new DiscordRpcClient(APPLICATION_ID);

				// Optional: Set log level (useful for debugging)
				client.Logger = new ConsoleLogger() { Level = LogLevel.Warning };

				// Initialize connection
				client.Initialize();

				isInitialized = true;
				isEnabled = true;

				Debug.Log("[Discord] Rich Presence initialized successfully");

				// Set initial presence
				SetPresence("In Main Menu", "Browsing", "dls_logo", "icon_menu");
			}
			catch (System.Exception e)
			{
				Debug.LogWarning($"[Discord] Failed to initialize Rich Presence: {e.Message}");
				Debug.LogWarning("[Discord] This is normal if Discord is not running. Continuing without Rich Presence.");
				isInitialized = false;
				isEnabled = false;
			}
		}

		/// <summary>
		/// Shutdown Discord connection
		/// </summary>
		private void ShutdownDiscord()
		{
			if (client != null && isInitialized)
			{
				try
				{
					client.ClearPresence();
					client.Dispose();
				}
				catch (System.Exception e)
				{
					Debug.LogWarning($"[Discord] Error during shutdown: {e.Message}");
				}

				isInitialized = false;
				isEnabled = false;
				Debug.Log("[Discord] Rich Presence shut down");
			}
		}

		/// <summary>
		/// Update Discord Rich Presence with new activity information
		/// </summary>
		/// <param name="details">Top line of text (e.g., "Building: 4-Bit Adder")</param>
		/// <param name="state">Second line of text (e.g., "Sandbox Mode")</param>
		/// <param name="largeImageKey">Key for large icon (uploaded to Discord app)</param>
		/// <param name="smallImageKey">Key for small icon overlay (optional)</param>
		/// <param name="largeImageText">Tooltip for large image (optional)</param>
		/// <param name="smallImageText">Tooltip for small image (optional)</param>
		public void SetPresence(
			string details, 
			string state, 
			string largeImageKey = "dls_logo", 
			string smallImageKey = null,
			string largeImageText = "Digital Logic Sim",
			string smallImageText = null)
		{
			if (!isInitialized || !isEnabled || client == null)
				return;

			try
			{
				// Sanitize strings (Discord has character limits)
				details = SanitizeString(details, 128);
				state = SanitizeString(state, 128);
				largeImageText = SanitizeString(largeImageText, 128);
				smallImageText = SanitizeString(smallImageText, 128);

				// Create Rich Presence object
				var presence = new RichPresence()
				{
					Details = details,
					State = state,
					Assets = new Assets()
					{
						LargeImageKey = largeImageKey,
						LargeImageText = largeImageText,
						SmallImageKey = smallImageKey,
						SmallImageText = smallImageText
					},
					Timestamps = Timestamps.Now // Shows elapsed time since this moment
				};

				// Send to Discord
				client.SetPresence(presence);
			}
			catch (System.Exception e)
			{
				Debug.LogWarning($"[Discord] Failed to update presence: {e.Message}");
			}
		}

		/// <summary>
		/// Clear Discord Rich Presence (hide activity)
		/// </summary>
		public void ClearPresence()
		{
			if (isInitialized && client != null)
			{
				try
				{
					client.ClearPresence();
				}
				catch (System.Exception e)
				{
					Debug.LogWarning($"[Discord] Failed to clear presence: {e.Message}");
				}
			}
		}

		/// <summary>
		/// Enable Discord Rich Presence (if disabled)
		/// </summary>
		public void Enable()
		{
			if (!isInitialized)
			{
				InitializeDiscord();
			}
			else
			{
				isEnabled = true;
			}
		}

		/// <summary>
		/// Disable Discord Rich Presence (without disposing client)
		/// </summary>
		public void Disable()
		{
			if (isEnabled)
			{
				ClearPresence();
				isEnabled = false;
			}
		}

		/// <summary>
		/// Sanitize string to meet Discord character limits and remove problematic characters
		/// </summary>
		private string SanitizeString(string input, int maxLength)
		{
			if (string.IsNullOrEmpty(input))
				return input;

			// Truncate if too long
			if (input.Length > maxLength)
			{
				input = input.Substring(0, maxLength - 3) + "...";
			}

			// Discord supports Unicode, but let's remove null characters just in case
			input = input.Replace("\0", "");

			return input;
		}

		/// <summary>
		/// Called every frame to process Discord callbacks
		/// </summary>
		void Update()
		{
			if (isInitialized && client != null)
			{
				try
				{
					// Invoke any pending Discord callbacks (handles connection events, etc.)
					client.Invoke();
				}
				catch (System.Exception e)
				{
					// Suppress errors (Discord might have disconnected)
					// Don't spam console
				}
			}
		}

		/// <summary>
		/// Clean up Discord connection when application quits
		/// </summary>
		void OnApplicationQuit()
		{
			ShutdownDiscord();
		}

		/// <summary>
		/// Handle application pause/resume (minimize/restore)
		/// </summary>
		void OnApplicationPause(bool pauseStatus)
		{
			if (pauseStatus)
			{
				// Application minimized - optionally clear or update presence
				// For now, keep showing presence even when minimized
			}
		}
		#endif
	}
}
