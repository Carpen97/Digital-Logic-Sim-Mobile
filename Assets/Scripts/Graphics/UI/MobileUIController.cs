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
	public bool isEraserModeActive;

	[Header("Level Mode")]
	public GameObject validateButton;	// round dark "Validate" button (assign in Inspector)

	private LevelManager _levelManager;

	private Image wrenchImage;
	private Image boxSelectImage;
	private Image hintImage;
	private Image trashCanImage;

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
			trashCanImage = trashCanTool.GetComponent<Image>();
			
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
			// Check if eraser mode is active
			bool eraserModeActive = DLS.Game.EraserModeController.IsActive;
			
			// Hide wrench and multiselect tools when eraser mode is active
			wrenchTool.SetActive(!eraserModeActive);
			boxSelectTool.SetActive(!eraserModeActive);
			
			if (Project.ActiveProject != null && Project.ActiveProject.CanEditViewedChip)
			{
				// Trash can is now always visible when in edit mode
				trashCanTool.SetActive(true);
				
				// Copy tool only shows when selection exists (for normal deletion workflow)
				bool hasSelectionForCopy = Project.ActiveProject.controller.SelectedElements.Count > 0 && !Project.ActiveProject.controller.IsPlacingElements;
				copyTool.SetActive(hasSelectionForCopy);
				
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
			bool hasSelectionForHint = Project.ActiveProject != null && Project.ActiveProject.controller.SelectedElements.Count > 0 && !Project.ActiveProject.controller.IsPlacingElements;
			bool shouldShowHint = _levelManager != null && _levelManager.IsActive && Project.ActiveProject != null && Project.ActiveProject.CanEditViewedChip && !isShowingPlacementButtons
			&& UIDrawer.ActiveMenu != UIDrawer.MenuType.BottomBarMenuPopup && !hasSelectionForHint;
			
			// Debug logging
			if (hintTool.activeSelf != shouldShowHint)
			{
				Debug.Log($"Hint button visibility changed: {shouldShowHint} (LevelActive: {_levelManager?.IsActive}, CanEdit: {Project.ActiveProject?.CanEditViewedChip}, HasSelection: {hasSelectionForHint})");
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

		// Update eraser mode visual state
		UpdateEraserModeVisualState();

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
		// Check if exactly one editable component is selected and auto-open its edit menu
		if (!isWrenchToolActive && TryAutoOpenEditMenuForSingleSelection())
		{
			// Edit menu opened, don't activate wrench tool mode
			return;
		}

		// Default behavior: toggle wrench tool mode
		isWrenchToolActive = !isWrenchToolActive;
		if(isWrenchToolActive){
			isBoxSelectToolActive = false;
			boxSelectImage.color = Color.white;
			wrenchImage.color = Color.yellow;
		}
		else
			wrenchImage.color = Color.white;
	}

	/// <summary>
	/// Checks if exactly one editable component is selected and automatically opens its edit menu.
	/// Returns true if an edit menu was opened, false otherwise.
	/// </summary>
	private bool TryAutoOpenEditMenuForSingleSelection()
	{
		// Only check when wrench tool is not already active
		if (Project.ActiveProject == null || Project.ActiveProject.controller == null)
			return false;

		var selectedElements = Project.ActiveProject.controller.SelectedElements;
		
		// Only proceed if exactly one element is selected
		if (selectedElements.Count != 1)
			return false;

		var selected = selectedElements[0];

		// Handle SubChipInstance (chips like ROM, Key, Pulse, Constant, Custom, LED, Button)
		if (selected is SubChipInstance subChip)
		{
			return TryAutoOpenChipEditMenu(subChip);
		}
		
		// Handle DevPinInstance (input/output pins)
		if (selected is DevPinInstance devPin)
		{
			return TryAutoOpenPinEditMenu(devPin);
		}

		return false;
	}

	private bool TryAutoOpenChipEditMenu(SubChipInstance subChip)
	{
		// Check if we can edit the current chip
		if (!Project.ActiveProject.CanEditViewedChip)
			return false;

		// Set the interaction context so edit menus can access it
		DLS.Graphics.ContextMenu.SetInteractionContext(subChip);

		// Select the chip before opening menu
		Project.ActiveProject.controller.Select(subChip, false);

		// Always open the context menu (centered) - let user choose action from there
		DLS.Graphics.ContextMenu.OpenContextMenuCentered(subChip);
		return true;
	}

	private bool TryAutoOpenPinEditMenu(DevPinInstance devPin)
	{
		// Check if we can edit the current chip
		if (!Project.ActiveProject.CanEditViewedChip)
			return false;

		// Set the interaction context so edit menus can access it
		DLS.Graphics.ContextMenu.SetInteractionContext(devPin);

		// Select the pin before opening menu
		Project.ActiveProject.controller.Select(devPin, false);

		// Always open the context menu (centered) - let user choose action from there
		DLS.Graphics.ContextMenu.OpenContextMenuCentered(devPin);
		return true;
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
		// If placing a wire, cancel wire placement instead of toggling eraser
		if (Project.ActiveProject?.controller?.IsCreatingWire ?? false)
		{
			Project.ActiveProject.controller.CancelEverything();
			Debug.Log("[MobileUIController] Cancelled wire placement via trash can");
			return;
		}
		
		// Check if any elements are selected
		bool hasSelection = Project.ActiveProject?.controller?.SelectedElements?.Count > 0;
		
		if (hasSelection)
		{
			// Delete selected elements
			Project.ActiveProject.controller.DeleteSelected();
			Debug.Log("[MobileUIController] Deleted selected elements via trash can");
		}
		else
		{
			// Toggle eraser mode when nothing is selected
			bool currentlyActive = DLS.Game.EraserModeController.IsActive;
			
			if (!currentlyActive)
			{
				// Activate eraser mode (DeleteAll)
				DLS.Game.EraserModeController.ToggleEraserMode();
				isEraserModeActive = true;
				Debug.Log("[MobileUIController] Eraser mode activated");
			}
			else
			{
				// Deactivate eraser mode
				DLS.Game.EraserModeController.DisableEraserMode();
				isEraserModeActive = false;
				Debug.Log("[MobileUIController] Eraser mode deactivated");
			}
		}
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

	/// <summary>
	/// Updates the visual state of the trash icon based on eraser mode
	/// </summary>
	private void UpdateEraserModeVisualState()
	{
		if (trashCanImage == null) return;

		// Highlight trash icon when eraser mode is active
		if (DLS.Game.EraserModeController.IsActive)
		{
			trashCanImage.color = Color.yellow; // Yellow highlight like wrench tool
		}
		else
		{
			trashCanImage.color = Color.white; // Normal color
		}
	}
}
