using UnityEngine;

public class IconThemeManager : MonoBehaviour
{
	public static IconThemeManager Instance { get; private set; }

	[SerializeField] private IconThemeSO squigglesTheme; // Only Squiggles Theme is used

	public IconThemeSO CurrentTheme { get; private set; }

	private void Awake()
	{
		if (Instance != null) { Destroy(gameObject); return; }
		Instance = this;
		
		// Always use Squiggles Theme
		CurrentTheme = squigglesTheme;
	}
}
