using DLS.Game;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_ANDROID || UNITY_EDITOR
public class MobileUIController : MonoBehaviour
{
	[Header("Placement Buttons")]
	public GameObject confirmButton; // Assign in Inspector
	public GameObject cancelButton;  // Assign in Inspector

	private System.Action onConfirmCallback;
	private System.Action onCancelCallback;

	public static MobileUIController Instance { get; private set; }

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(gameObject); // Prevent multiple MobileUIControllers
		}
	
		HidePlacementButtons(); // Already hiding buttons at start
	}

	// Call this when starting placement
	public void ShowPlacementButtons(System.Action onConfirm, System.Action onCancel)
	{
		onConfirmCallback = onConfirm;
		onCancelCallback = onCancel;

		confirmButton.SetActive(true);
		cancelButton.SetActive(true);
	}

	// Call this when placement ends
	public void HidePlacementButtons()
	{
		confirmButton.SetActive(false);
		cancelButton.SetActive(false);
		onConfirmCallback = null;
		onCancelCallback = null;
	}

	// Hook these to the button OnClick() events
	public void OnConfirmButtonPressed()
	{
		onConfirmCallback?.Invoke();
	}

	public void OnCancelButtonPressed()
	{
		onCancelCallback?.Invoke();
		HidePlacementButtons();
	}
}
#endif
