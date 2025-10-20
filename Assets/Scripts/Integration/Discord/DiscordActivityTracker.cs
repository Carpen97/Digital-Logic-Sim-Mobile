#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
#define DISCORD_SUPPORTED
#endif

using UnityEngine;
using DLS.Game;
using DLS.Graphics;

namespace DLS.Integration.Discord
{
	/// <summary>
	/// Tracks game state and automatically updates Discord Rich Presence.
	/// Polls game state every 15 seconds (Discord rate limit).
	/// </summary>
	public class DiscordActivityTracker : MonoBehaviour
	{
		[Header("Update Settings")]
		[Tooltip("How often to check for game state changes (seconds). Discord limit: 15s")]
		[SerializeField] private float updateInterval = 15f;

		[Tooltip("Force update on first frame (recommended)")]
		[SerializeField] private bool updateOnStart = true;

		private float timeSinceLastUpdate = 0f;
		
		// Cache previous state to avoid unnecessary updates
		private string lastDetails = "";
		private string lastState = "";
		private string lastLargeIcon = "";
		private string lastSmallIcon = "";

		void Start()
		{
			#if DISCORD_SUPPORTED
			if (updateOnStart)
			{
				// Update immediately on start
				UpdateDiscordActivity();
			}
			#endif
		}

		void Update()
		{
			#if DISCORD_SUPPORTED
			timeSinceLastUpdate += Time.deltaTime;

			// Check if it's time to update
			if (timeSinceLastUpdate >= updateInterval)
			{
				timeSinceLastUpdate = 0f;
				UpdateDiscordActivity();
			}
			#endif
		}

		#if DISCORD_SUPPORTED
		/// <summary>
		/// Read current game state and update Discord presence if changed
		/// </summary>
		private void UpdateDiscordActivity()
		{
			// Check if Discord is available
			if (DiscordRichPresenceManager.Instance == null)
				return;

			// Default values
			string details = "Digital Logic Sim";
			string state = "In Menu";
			string largeIcon = "dls_logo";
			string smallIcon = null;
			string largeIconText = "Digital Logic Sim";
			string smallIconText = null;

			// ===== PRIORITY 1: Level Mode (highest priority) =====
			if (DLS.Game.LevelsIntegration.LevelManager.Instance != null && DLS.Game.LevelsIntegration.LevelManager.Instance.IsActive)
			{
				// User is playing a level
				var levelDef = DLS.Game.LevelsIntegration.LevelManager.Instance.Current;
				string levelName = levelDef?.name ?? "Unknown Level";
				
				details = $"Level: {levelName}";
				state = "Solving puzzle";
				smallIcon = "icon_level";
				smallIconText = "Level Mode";
			}
			// ===== PRIORITY 2: Viewing Leaderboard Solution =====
			else if (Project.ActiveProject != null && Project.ActiveProject.isViewingLeaderboardSolution)
			{
				// User is viewing someone else's solution
				string username = Project.ActiveProject.leaderboardSolutionUserName;
				if (string.IsNullOrEmpty(username))
					username = "Anonymous";

				details = "Viewing solution";
				state = $"By: {username}";
				smallIcon = "icon_leaderboard";
				smallIconText = "Leaderboard";
			}
			// ===== PRIORITY 3: Sandbox Mode (editing chip) =====
			else if (Project.ActiveProject != null)
			{
				// User is in sandbox mode
				string chipName = Project.ActiveProject.ActiveDevChipName ?? "Untitled";
				
				details = $"Building: {chipName}";
				state = "Sandbox Mode";
				smallIcon = "icon_sandbox";
				smallIconText = "Sandbox";
			}
			// ===== PRIORITY 4: Menu States =====
			else
			{
				// User is in menus
				var activeMenu = UIDrawer.ActiveMenu;

				switch (activeMenu)
				{
					case UIDrawer.MenuType.MainMenu:
						details = "In Main Menu";
						state = "Browsing";
						smallIcon = "icon_menu";
						break;

					case UIDrawer.MenuType.Levels:
						details = "Browsing Levels";
						state = "Level Select";
						smallIcon = "icon_menu";
						break;

					case UIDrawer.MenuType.ChipCustomization:
						details = "Customizing Chip";
						state = "Design Menu";
						smallIcon = "icon_menu";
						break;

					case UIDrawer.MenuType.Preferences:
						details = "In Settings";
						state = "Configuring";
						smallIcon = "icon_menu";
						break;

					case UIDrawer.MenuType.None:
						// User is in chip view but no active project? (edge case)
						details = "Digital Logic Sim";
						state = "In Game";
						break;

					default:
						details = "Digital Logic Sim";
						state = "In Menu";
						smallIcon = "icon_menu";
						break;
				}
			}

			// ===== OPTIMIZATION: Only update if state changed =====
			bool stateChanged = 
				details != lastDetails || 
				state != lastState || 
				largeIcon != lastLargeIcon || 
				smallIcon != lastSmallIcon;

			if (stateChanged)
			{
				// Update Discord
				DiscordRichPresenceManager.Instance.SetPresence(
					details: details,
					state: state,
					largeImageKey: largeIcon,
					smallImageKey: smallIcon,
					largeImageText: largeIconText,
					smallImageText: smallIconText
				);

				// Cache new state
				lastDetails = details;
				lastState = state;
				lastLargeIcon = largeIcon;
				lastSmallIcon = smallIcon;

				// Debug log (optional, remove in production)
				Debug.Log($"[Discord] Updated presence: {details} | {state}");
			}
		}

		/// <summary>
		/// Force an immediate update (useful for event-driven updates)
		/// </summary>
		public void ForceUpdate()
		{
			timeSinceLastUpdate = updateInterval; // Will trigger update on next frame
		}
		#endif
	}
}
