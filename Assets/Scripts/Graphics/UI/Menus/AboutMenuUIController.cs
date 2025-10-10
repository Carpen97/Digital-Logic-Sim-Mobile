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
			Debug.Log("[AboutMenuUIController] Logos hidden");
		}
	}
}

