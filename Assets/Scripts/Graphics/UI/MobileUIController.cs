using System.Collections;
using DLS.Description;
using DLS.Game;
using DLS.Graphics;
using DLS.SaveSystem;
using Seb.Vis.UI;
using UnityEngine;
using DLS.Game.LevelsIntegration;	// for LevelManager
using UnityEngine.UI;


public class MobileUIController : MonoBehaviour
{
	[Header("Placement Buttons")]
	public GameObject confirmButton; 
	public GameObject cancelButton; 
	public GameObject addWirePointButton; 
	public GameObject undoButton; 
	public GameObject redoButton; 
	public GameObject wrenchTool;  
	public GameObject trashCanTool;  
	public GameObject copyTool;  
	public GameObject singleStepTool;  
	public bool isWrenchToolActive;
	public GameObject boxSelectTool;  
	public bool isBoxSelectToolActive;
	public GameObject hintTool;
	public bool isHintToolActive;
	public bool isShowingPlacementButtons;

	[Header("Level Mode")]
	public GameObject validateButton;	// round dark "Validate" button (assign in Inspector)

	private LevelManager _levelManager;

	private Image wrenchImage;
	private Image boxSelectImage;
	private Image hintImage;

	private System.Action onConfirmCallback;
	private System.Action onAddWirePointCallback;
	private System.Action onCancelCallback;

	public Text text;

	public static MobileUIController Instance { get; private set; }

	private void Awake()
	{
		Debug.Log("[MobileUIController] Awake() called");
		
		// Check if we're on a mobile platform OR in the Unity Editor (for testing mobile builds)
		bool isMobilePlatform;
		
		#if UNITY_EDITOR
		// In the editor, always enable mobile UI for testing mobile builds
		isMobilePlatform = true;
		#else
		// Check actual runtime platform for builds
		isMobilePlatform = Application.platform == RuntimePlatform.Android || 
		                   Application.platform == RuntimePlatform.IPhonePlayer;
		#endif
		
		if (!isMobilePlatform)
		{
			// Disable the parent Canvas GameObject on desktop platforms
			if (transform.parent != null)
			{
				transform.parent.gameObject.SetActive(false);
				Debug.Log("[MobileUIController] Disabled parent Canvas on desktop platform");
			}
			else
			{
				// Fallback: if for some reason there's no parent, just disable this GameObject
				gameObject.SetActive(false);
				Debug.Log("[MobileUIController] Disabled on desktop platform (no parent Canvas)");
			}
			return; // Exit Awake early for desktop platforms
		}
		else
		{
		if (Instance == null)
		{
			Instance = this;
			_levelManager = FindFirstObjectByType<LevelManager>();
			wrenchImage = wrenchTool.GetComponent<Image>();
			boxSelectImage = boxSelectTool.GetComponent<Image>();
			hintImage = hintTool.GetComponent<Image>();
			
			// Apply Squiggles Theme immediately
			Debug.Log($"[MobileUIController] Checking IconThemeManager - Instance: {(IconThemeManager.Instance != null ? "EXISTS" : "NULL")}, CurrentTheme: {(IconThemeManager.Instance?.CurrentTheme != null ? IconThemeManager.Instance.CurrentTheme.name : "NULL")}");
			
			if (IconThemeManager.Instance?.CurrentTheme != null)
			{
				ApplyTheme(IconThemeManager.Instance.CurrentTheme);
				Debug.Log("[MobileUIController] Successfully applied SquigglesTheme from IconThemeManager");
			}
			else
			{
				Debug.LogWarning("[MobileUIController] IconThemeManager not ready yet, trying fallback methods...");
				// Try fallback methods
				ApplySquigglesThemeDirectly();
			}
		}
		else
		{
			Destroy(gameObject); // Prevent multiple MobileUIControllers
		}
		}
	}

    void Update()
    {
		bool defaultState = UIDrawer.ActiveMenu is UIDrawer.MenuType.None or UIDrawer.MenuType.BottomBarMenuPopup;
		if (defaultState)
		{
			wrenchTool.SetActive(true);
			if (Project.ActiveProject != null && Project.ActiveProject.CanEditViewedChip)
			{
				boxSelectTool.SetActive(true);
				bool temp = Project.ActiveProject.controller.SelectedElements.Count > 0 && !Project.ActiveProject.controller.IsPlacingElements;
				trashCanTool.SetActive(temp);
				copyTool.SetActive(temp);
				if (!isShowingPlacementButtons)
				{
					redoButton.SetActive(true);
					undoButton.SetActive(true);
				}
				singleStepTool.SetActive(Project.ActiveProject.simPaused);
			}
			else
			{
				confirmButton.SetActive(false);
				cancelButton.SetActive(false);
				addWirePointButton.SetActive(false);
				undoButton.SetActive(false);
				redoButton.SetActive(false);
				boxSelectTool.SetActive(false);
				hintTool.SetActive(false);
				trashCanTool.SetActive(false);
				copyTool.SetActive(false);
			}
			// Show Validate button only in Level Mode, when not showing placement buttons
			if (_levelManager != null && _levelManager.IsActive && Project.ActiveProject != null && Project.ActiveProject.CanEditViewedChip && !isShowingPlacementButtons
			&& UIDrawer.ActiveMenu != UIDrawer.MenuType.BottomBarMenuPopup)
			{
				validateButton.SetActive(true);
			}
			else
			{
				validateButton.SetActive(false);
			}

			// Show Hint button only in Level Mode, when nothing is selected (no trash/copy tools visible)
			bool hasSelection = Project.ActiveProject != null && Project.ActiveProject.controller.SelectedElements.Count > 0 && !Project.ActiveProject.controller.IsPlacingElements;
			bool shouldShowHint = _levelManager != null && _levelManager.IsActive && Project.ActiveProject != null && Project.ActiveProject.CanEditViewedChip && !isShowingPlacementButtons
			&& UIDrawer.ActiveMenu != UIDrawer.MenuType.BottomBarMenuPopup && !hasSelection;
			
			// Debug logging
			if (hintTool.activeSelf != shouldShowHint)
			{
				Debug.Log($"Hint button visibility changed: {shouldShowHint} (LevelActive: {_levelManager?.IsActive}, CanEdit: {Project.ActiveProject?.CanEditViewedChip}, HasSelection: {hasSelection})");
			}
			
			hintTool.SetActive(shouldShowHint);

		}
		else if (UIDrawer.ActiveMenu is UIDrawer.MenuType.ChipCustomization)
		{
			wrenchTool.SetActive(false);
			boxSelectTool.SetActive(false);
			hintTool.SetActive(false);
			validateButton.SetActive(false);
		}
		else
		{
			HideAll();
		}

    }

	public void ApplyTheme(IconThemeSO theme)
	{
		// Always use Squiggles Theme - theme swapping removed
		if (IconThemeManager.Instance?.CurrentTheme != null)
		{
			theme = IconThemeManager.Instance.CurrentTheme;
		}
		
		// Set normal state sprites
		wrenchTool.GetComponent<Image>().sprite = theme.wrenchIcon;
		boxSelectTool.GetComponent<Image>().sprite = theme.boxSelectIcon;
		hintTool.GetComponent<Image>().sprite = theme.hintIcon;
		trashCanTool.GetComponent<Image>().sprite = theme.trashIcon;
		undoButton.GetComponent<Image>().sprite = theme.undoIcon;
		redoButton.GetComponent<Image>().sprite = theme.redoIcon;
		confirmButton.GetComponent<Image>().sprite = theme.confirmIcon;
		cancelButton.GetComponent<Image>().sprite = theme.cancelIcon;
		singleStepTool.GetComponent<Image>().sprite = theme.singleStepIcon;
		copyTool.GetComponent<Image>().sprite = theme.copyIcon;
		validateButton.GetComponent<Image>().sprite = theme.playIcon;	// reuse the checkmark

		Debug.Log($"Applied Squiggles Theme: {theme.name}");
		var buttons = new (GameObject go, Sprite toggled)[]
		{
			(wrenchTool, theme.wrenchIconToggled),
			(boxSelectTool, theme.boxSelectIconToggled),
			(hintTool, theme.hintToggled),
			(trashCanTool, theme.trashIconToggled),
			(undoButton, theme.undoIconToggled),
			(redoButton, theme.redoIconToggled),
			(confirmButton, theme.confirmIconToggled),
			(cancelButton, theme.cancelIconToggled),
			(singleStepTool, theme.singleStepIconToggled),
			(copyTool, theme.copyIconToggled),
			(validateButton, theme.playToggled),
		};

		foreach (var (go, toggledSprite) in buttons)
		{
			var button = go.GetComponent<Button>();
			if (button == null) continue;
			if (toggledSprite != null)
			{
				button.transition = Selectable.Transition.SpriteSwap;
				var spriteState = button.spriteState;
				spriteState.pressedSprite = toggledSprite;
				button.spriteState = spriteState;
			}
			else
			{
				button.transition = Selectable.Transition.ColorTint;
			}
		}
	}

	private void ApplySquigglesThemeDirectly()
	{
		// Try multiple approaches to find SquigglesTheme
		IconThemeSO squigglesTheme = null;
		
		// Method 1: Try IconThemeManager
		if (IconThemeManager.Instance?.CurrentTheme != null)
		{
			squigglesTheme = IconThemeManager.Instance.CurrentTheme;
			Debug.Log("[MobileUIController] Found SquigglesTheme from IconThemeManager");
		}
		
		// Method 2: Try Resources folder
		if (squigglesTheme == null)
		{
			squigglesTheme = Resources.Load<IconThemeSO>("SquigglesTheme");
			if (squigglesTheme != null)
			{
				Debug.Log("[MobileUIController] Found SquigglesTheme from Resources");
			}
		}
		
		// Method 3: Try to find any IconThemeSO with "Squiggles" in the name
		if (squigglesTheme == null)
		{
			var allThemes = Resources.FindObjectsOfTypeAll<IconThemeSO>();
			foreach (var theme in allThemes)
			{
				if (theme.name.ToLower().Contains("squiggles"))
				{
					squigglesTheme = theme;
					Debug.Log($"[MobileUIController] Found SquigglesTheme: {theme.name}");
					break;
				}
			}
		}
		
		// Apply the theme if found
		if (squigglesTheme != null)
		{
			ApplyTheme(squigglesTheme);
			Debug.Log("[MobileUIController] Successfully applied SquigglesTheme");
		}
		else
		{
			Debug.LogError("[MobileUIController] Could not find SquigglesTheme anywhere - buttons will use old sprites");
		}
	}

	public void HideAll(){
		confirmButton.SetActive(false);
		cancelButton.SetActive(false);
		undoButton.SetActive(false);
		redoButton.SetActive(false);
		wrenchTool.SetActive(false);
		boxSelectTool.SetActive(false);
		hintTool.SetActive(false);
		trashCanTool.SetActive(false);
		copyTool.SetActive(false);
		addWirePointButton.SetActive(false);
		validateButton.SetActive(false);
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

    public void ShowAddWireButtons(System.Action onAddWirePoint, System.Action onCancel)
	{
		onAddWirePointCallback = onAddWirePoint;
		onCancelCallback = onCancel;

		addWirePointButton.SetActive(true);
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
		addWirePointButton.SetActive(false);
		onConfirmCallback = null;
		onCancelCallback = null;
		onAddWirePointCallback = null;
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

	public void OnHintToolPress()
	{	
		isHintToolActive = !isHintToolActive;
		if(isHintToolActive){
			hintImage.color = Color.yellow;
		}
		else
			hintImage.color = Color.white;
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
	public void OnValidateButtonPressed()
	{
		if (_levelManager == null || !_levelManager.IsActive)
			return;

		var report = _levelManager.RunValidation();
		LevelValidationPopup.Open(report);

		// For now we log; next step we’ll show a popup using your existing popup infra.
		if (report.PassedAll)
		{
			// Get NAND gate count for display
			var adapter = new MobileSimulationAdapter();
			int nandCount = adapter.CountNandGates();
			Debug.Log($"[Levels] All tests passed ✅ — NAND Gates: {nandCount}");
		}
		else
		{
			Debug.Log($"[Levels] Validation failed — Stars={report.Stars}, Failures={report.Failures.Count}");
			foreach (var f in report.Failures)
				Debug.Log($"• inputs={f.Inputs} msg={f.Message}");
			foreach (var m in report.ConstraintMessages)
				Debug.Log($"• constraint: {m}");
		}
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
	public void onAddWirePointPressed()
	{
		onAddWirePointCallback?.Invoke();
	}

	public void OnCancelButtonPressed()
	{
		onCancelCallback?.Invoke();
	}
}
