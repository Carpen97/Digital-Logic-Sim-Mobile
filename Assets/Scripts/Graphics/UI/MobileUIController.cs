using DLS.Game;
using DLS.Graphics;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_ANDROID || UNITY_EDITOR
public class MobileUIController : MonoBehaviour
{
	[Header("Placement Buttons")]
	public GameObject confirmButton; 
	public GameObject cancelButton; 
	public GameObject wrenchTool;  
	public GameObject trashCanTool;  
	public GameObject copyTool;  
	public bool isWrenchToolActive;
	public GameObject boxSelectTool;  
	public bool isBoxSelectToolActive;

	private Image wrenchImage;
	private Image boxSelectImage;

	private System.Action onConfirmCallback;
	private System.Action onCancelCallback;

	public Text text;

	public static MobileUIController Instance { get; private set; }

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			wrenchImage = wrenchTool.GetComponent<Image>();
			boxSelectImage = boxSelectTool.GetComponent<Image>();
		}
		else
		{
			Destroy(gameObject); // Prevent multiple MobileUIControllers
		}
	
		HidePlacementButtons(); // Already hiding buttons at start
	}

    void Update()
    {
		bool inMenu = !(UIDrawer.ActiveMenu is UIDrawer.MenuType.None or UIDrawer.MenuType.BottomBarMenuPopup or UIDrawer.MenuType.ChipCustomization);
		if(inMenu){
			confirmButton.SetActive(false);
			cancelButton.SetActive(false);
			wrenchTool.SetActive(false);
			boxSelectTool.SetActive(false);
			trashCanTool.SetActive(false);
			copyTool.SetActive(false);
		}else{
			wrenchTool.SetActive(true);
			boxSelectTool.SetActive(true);
			bool temp = Project.ActiveProject.controller.SelectedElements.Count>0 && !Project.ActiveProject.controller.IsPlacingElements;
			trashCanTool.SetActive(temp);
			copyTool.SetActive(temp);
		}
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

	public void OnWrenchButtonPress()
	{	
		isWrenchToolActive = !isWrenchToolActive;
		if(isWrenchToolActive){
			isBoxSelectToolActive = false;
			boxSelectImage.color = Color.white;
			wrenchImage.color = Color.yellow;
		}
		else
			wrenchImage.color = Color.white;
	}

	public void OnBoxSelectToolPress()
	{	
		isBoxSelectToolActive = !isBoxSelectToolActive;
		if(isBoxSelectToolActive){
			isWrenchToolActive = false;
			wrenchImage.color = Color.white;
			boxSelectImage.color = Color.yellow;
		}
		else
			boxSelectImage.color = Color.white;
	}
	public void OnTrashCanPress()
	{
		Project.ActiveProject.controller.DeleteSelected();
		HidePlacementButtons();
	}

	public void OnCopyToolPress()
	{
		Project.ActiveProject.controller.DuplicateSelectedElements();
		Project.ActiveProject.controller.MoveSelectionAfterDuplication();
	}

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
