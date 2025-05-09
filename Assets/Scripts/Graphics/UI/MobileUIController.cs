using System.Collections;
using DLS.Description;
using DLS.Game;
using DLS.Graphics;
using DLS.SaveSystem;
using Seb.Vis.UI;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_ANDROID || UNITY_EDITOR
public class MobileUIController : MonoBehaviour
{
	[Header("Placement Buttons")]
	public GameObject confirmButton; 
	public GameObject cancelButton; 

	public GameObject undoButton; 
	public GameObject redoButton; 
	public GameObject wrenchTool;  
	public GameObject trashCanTool;  
	public GameObject copyTool;  
	public GameObject singleStepTool;  
	public bool isWrenchToolActive;
	public GameObject boxSelectTool;  
	public bool isBoxSelectToolActive;
	public bool isShowingPlacementButtons;

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
	
		HidePlacementButtons(); 

	}

    void Update()
    {
		bool defaultState = UIDrawer.ActiveMenu is UIDrawer.MenuType.None or UIDrawer.MenuType.BottomBarMenuPopup;
		if(defaultState){
			wrenchTool.SetActive(true);
			if(Project.ActiveProject.CanEditViewedChip){
				boxSelectTool.SetActive(true);
				bool temp = Project.ActiveProject.controller.SelectedElements.Count>0 && !Project.ActiveProject.controller.IsPlacingElements;
				trashCanTool.SetActive(temp);
				copyTool.SetActive(temp);
				if(!isShowingPlacementButtons){
					redoButton.SetActive(true);
					undoButton.SetActive(true);
				}
				singleStepTool.SetActive(Project.ActiveProject.simPaused);
			}else{
				confirmButton.SetActive(false);
				cancelButton.SetActive(false);
				undoButton.SetActive(false);
				redoButton.SetActive(false);
				boxSelectTool.SetActive(false);
				trashCanTool.SetActive(false);
				copyTool.SetActive(false);
			}
		}else if(UIDrawer.ActiveMenu is UIDrawer.MenuType.ChipCustomization){
			wrenchTool.SetActive(false);
			boxSelectTool.SetActive(false);
		}else{
			HideAll();
		}
    }

	public void HideAll(){
		confirmButton.SetActive(false);
		cancelButton.SetActive(false);
		undoButton.SetActive(false);
		redoButton.SetActive(false);
		wrenchTool.SetActive(false);
		boxSelectTool.SetActive(false);
		trashCanTool.SetActive(false);
		copyTool.SetActive(false);
	}

    public void ShowPlacementButtons(System.Action onConfirm, System.Action onCancel)
	{
		onConfirmCallback = onConfirm;
		onCancelCallback = onCancel;

		confirmButton.SetActive(true);
		cancelButton.SetActive(true);
		HideUndoButtons();
		isShowingPlacementButtons = true;
	}
    public void ShowCancelButton(System.Action onCancel)
	{
		onCancelCallback = onCancel;

		cancelButton.SetActive(true);
		HideUndoButtons();
		isShowingPlacementButtons = true;
	}

	public void HidePlacementButtons()
	{
		confirmButton.SetActive(false);
		cancelButton.SetActive(false);
		onConfirmCallback = null;
		onCancelCallback = null;
		isShowingPlacementButtons = false;
	}
	
	public void ShowUndoButtons(){
		StartCoroutine(ShowUndoButtonsDelayed());
	}

    public IEnumerator ShowUndoButtonsDelayed()
	{
		yield return null;
		undoButton.SetActive(true);
		redoButton.SetActive(true);
	}

	public void HideUndoButtons()
	{
		redoButton.SetActive(false);
		undoButton.SetActive(false);
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
		ShowUndoButtons();
	}
	

	public void OnTempPress()
	{
		AndroidIO.ImportChip((json) =>
		{
			if (string.IsNullOrEmpty(json))
				return;

			ChipDescription chip = null;
			try
			{
				chip = Serializer.DeserializeChipDescription(json); // safer than JsonUtility
			}
			catch (System.Exception e)
			{
				Debug.LogError($"Failed to deserialize chip: {e.Message}");
				return;
			}

			if (chip == null || string.IsNullOrEmpty(chip.Name))
			{
				Debug.LogWarning("Invalid chip data or missing name.");
				return;
			}

			Debug.Log($"[Import] Chip '{chip.Name}' loaded.");
			Project.ActiveProject.SaveFromDescription(chip);
			//Main.ActiveProject.LoadDevChipOrCreateNewIfDoesntExist(chip.Name);
			//UIDrawer.SetActiveMenu(UIDrawer.MenuType.None); // show dev chip
		});
	}
	public void OnSingleTimeStep()
	{
		Debug.Log("SINGLE STEP");
		Project.ActiveProject.advanceSingleSimStep = true;
	}

	public void OnCopyToolPress()
	{
		Project.ActiveProject.controller.DuplicateSelectedElements();
		Project.ActiveProject.controller.MoveSelectionAfterDuplication();
	}

	public void OnUndoButtonPressed()
	{
		Debug.Log("PRESSED REDO");
		Project.ActiveProject.controller.ActiveDevChip.UndoController.TryUndo();
	}

	public void OnRedoButtonPressed()
	{
		Project.ActiveProject.controller.ActiveDevChip.UndoController.TryRedo();
	}

	public void OnConfirmButtonPressed()
	{
		Debug.Log("PRESSED CONFIRM");
		onConfirmCallback?.Invoke();
	}

	public void OnCancelButtonPressed()
	{
		onCancelCallback?.Invoke();
	}
}
#endif
