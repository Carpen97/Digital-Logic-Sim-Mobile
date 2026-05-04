using UnityEngine;
using UnityEngine.UI;

namespace DLS.Graphics
{
	/// <summary>
	/// Controls the About menu UI logo images (YouTube and Discord).
	/// These are just visual overlays - click interaction is handled by MainMenu's Seb.Vis.UI buttons.
	/// Visible on both PC and mobile platforms.
	/// </summary>
	public class AboutMenuUIController : MonoBehaviour
	{
		[Header("Logo Images (Visual Only)")]
		public GameObject youtubeLogo;
		public GameObject discordLogo;

		public static AboutMenuUIController Instance { get; private set; }

		private void Awake()
		{
			Debug.Log("[AboutMenuUIController] Awake() called");

			if (Instance == null)
			{
				Instance = this;
			}
			else
			{
				Destroy(gameObject);
				return;
			}

			// Start hidden
			HideLogos();

			// [Ticket 092] Debug: Check sprite references for Discord logo white rectangle issue
			DebugLogoSpriteState();
		}

		/// <summary>
		/// [Ticket 092] Debug helper: Log sprite state for both logos to diagnose white rectangle.
		/// White rectangle typically = Image with no sprite, or sprite asset missing/broken.
		/// </summary>
		private void DebugLogoSpriteState()
		{
			LogLogoState("YouTube", youtubeLogo);
			LogLogoState("Discord", discordLogo);
		}

		private void LogLogoState(string name, GameObject logoObj)
		{
			if (logoObj == null)
			{
				Debug.Log($"[AboutMenuUIController] {name}: GameObject is NULL");
				return;
			}
			var img = logoObj.GetComponent<Image>();
			if (img == null)
			{
				Debug.Log($"[AboutMenuUIController] {name}: GameObject exists but has NO Image component");
				return;
			}
			if (img.sprite == null)
			{
				Debug.LogWarning($"[AboutMenuUIController] {name}: Image.sprite is NULL - this causes white rectangle! Assign a valid sprite.");
			}
			else
			{
				Debug.Log($"[AboutMenuUIController] {name}: sprite='{img.sprite.name}', texture={(img.sprite.texture != null ? "OK" : "NULL")}");
			}
		}

		/// <summary>
		/// Show the logo images (called when About menu is active)
		/// </summary>
		public void ShowLogos()
		{
			if (youtubeLogo != null)
			{
				youtubeLogo.SetActive(true);
			}
			else
			{
				Debug.LogWarning("[AboutMenuUIController] YouTube logo reference is null!");
			}

			if (discordLogo != null)
			{
				discordLogo.SetActive(true);
			}
			else
			{
				Debug.LogWarning("[AboutMenuUIController] Discord logo reference is null!");
			}
			
			Debug.Log("[AboutMenuUIController] Logos shown");
		}

		/// <summary>
		/// Hide the logo images (called when leaving About menu)
		/// </summary>
		public void HideLogos()
		{
			if (youtubeLogo != null)
			{
				youtubeLogo.SetActive(false);
			}
			if (discordLogo != null)
			{
				discordLogo.SetActive(false);
			}
		}
	}
}

