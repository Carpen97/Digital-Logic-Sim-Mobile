using UnityEngine;

public class IconThemeManager : MonoBehaviour
{
	public static IconThemeManager Instance { get; private set; }

	[SerializeField] private IconThemeSO squigglesTheme; // Only Squiggles Theme is used

	public IconThemeSO CurrentTheme { get; private set; }

	private void Awake()
	{
		Debug.Log($"[IconThemeManager] Awake() called - squigglesTheme: {(squigglesTheme != null ? squigglesTheme.name : "NULL")}");
		
		if (Instance != null) { 
			Debug.LogWarning("[IconThemeManager] Instance already exists, destroying duplicate");
			Destroy(gameObject); 
			return; 
		}
		Instance = this;
		
		// Always use Squiggles Theme
		CurrentTheme = squigglesTheme;
		Debug.Log($"[IconThemeManager] Initialized - CurrentTheme: {(CurrentTheme != null ? CurrentTheme.name : "NULL")}");
	}
}
