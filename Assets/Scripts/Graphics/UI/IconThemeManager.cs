using DLS.Game;
using UnityEngine;

public class IconThemeManager : MonoBehaviour
{
	public static IconThemeManager Instance { get; private set; }

	[SerializeField] private IconThemeSO defaultTheme;
	[SerializeField] private IconThemeSO[] availableThemes;

	public IconThemeSO CurrentTheme { get; private set; }

    private int currentThemeIndex;

    void Update()
    {
	    if (Project.ActiveProject?.description == null || availableThemes.Length == 0)
		    return;

	    int index = Project.ActiveProject.description.Prefs_UIThemeMode;
	    if (index != currentThemeIndex && index >= 0 && index < availableThemes.Length)
	    {
		    currentThemeIndex = index;
		    MobileUIController.Instance.ApplyTheme(availableThemes[index]);
	    }
    }

	private void Awake()
	{
		if (Instance != null) { Destroy(gameObject); return; }
		Instance = this;
	}
}
