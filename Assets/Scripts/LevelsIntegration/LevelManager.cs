using System;
using System.Collections.Generic;
using UnityEngine;
using DLS.Levels;
using DLS.Levels.Host;
using DLS.Game;
using DLS.Description;
using DLS.SaveSystem;

namespace DLS.Game.LevelsIntegration
{
    /// <summary>
    /// Single source of truth for "level mode" + spawn/validate logic.
    /// - StartLevel() creates a fresh chip, spawns I/O, tracks locked pins, recenters camera.
    /// - ExitLevel() clears state.
    /// - RunValidation() runs LevelValidator and returns a report (also logs).
    /// - IsLockedPin() lets UI/menus block deletion of level I/O.
    /// </summary>


    public class LevelManager : MonoBehaviour
    {
        // --- Session state ---
        public bool IsActive { get; private set; }
        public LevelDefinition Current { get; private set; }

        // IDs of pins that must be locked against delete in level mode
        readonly HashSet<int> _lockedPinIds = new();

        // --- Events for UI to react to ---
        public event Action LevelStarted;
        public event Action LevelEnded;

        // --- Validation ---
        readonly ISimulationAdapter _sim = new MobileSimulationAdapter();
        public static LevelManager Instance { get; private set; }
        void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);
        }

        /// <summary>Starts the given level: fresh chip, spawn level I/O, lock them, focus camera.</summary>
        public void StartLevel(LevelDefinition def)
        {
            if (def == null)
            {
                Debug.LogError("[LevelManager] StartLevel called with null definition.");
                return;
            }

            // End any existing session (defensive)
            ExitLevel();

            Current = def;
            IsActive = true;

            // 1) Fresh world
            Project.ActiveProject.CreateBlankDevChip();
            var dev = Project.ActiveProject.ViewedChip;

            // 2) Spawn pins & remember which to lock
            var spawned = SpawnPinsForLevel(dev, def);
            _lockedPinIds.Clear();
            foreach (var p in spawned)
                _lockedPinIds.Add(p.ID);

            // 3) Fit view
            Debug.Log("Fitting level");
            var view = CameraController.GetViewForChip(dev);

            // If you want the camera at exactly (0,0), override after fit:
            view.Pos = Vector2.zero;

            // Persist it so Update() won’t replace it next frame
            CameraController.SetViewForCurrentChip(view);

            // Clear hint system cache for new level
            HintSystem.ClearCache();

            // Try to load saved progress for this level
            LoadLevelProgress(def.id);

            LevelStarted?.Invoke();
        }

        /// <summary>Ends level mode (does not modify the current chip).</summary>
        public void ExitLevel()
        {
            if (!IsActive) return;

            IsActive = false;
            Current = null;
            _lockedPinIds.Clear();

            LevelEnded?.Invoke();
        }

        /// <summary>True if the given pin is one of the level’s pre-spawned, locked pins.</summary>
        public bool IsLockedPin(PinInstance pin) =>
            IsActive && pin != null && _lockedPinIds.Contains(pin.ID);

        /// <summary>
        /// Runs validator on a clone of the current level definition.
        /// Returns the report so UI can display a popup.
        /// </summary>
        public ValidationReport RunValidation()
        {
            if (!IsActive || Current == null)
            {
                Debug.LogWarning("[LevelManager] RunValidation called while level not active.");
                // Return an empty-but-valid report instead of null to keep callers simple
                return new ValidationReport
                {
                    PassedAll = false,
                    Failures = new System.Collections.Generic.List<CaseFail>(),
                    ConstraintMessages = new System.Collections.Generic.List<string>(),
                    Stars = 0,
                    LastPartsCount = 0
                };
            }

            var validator = new LevelValidator(_sim);
            var report = validator.Validate(Clone(Current));

            	// NEW: persist progress
        	if (!string.IsNullOrEmpty(Current.id))
	        {
                // Use NAND gate count instead of parts count for scoring
                int nandGateCount = 0;
                if (_sim is MobileSimulationAdapter mobileAdapter)
                {
                    nandGateCount = mobileAdapter.CountNandGates();
                }
                LevelProgressService.MarkResult(
			        Current.id,
			        report.Stars,
			        nandGateCount,  // Use NAND gate count instead of LastPartsCount
			        report.PassedAll
		        );
                
                Debug.Log($"[Levels] stars={report.Stars} passed={report.PassedAll} nandGates={nandGateCount}");
            }
            foreach (var e in report.Failures) Debug.Log($"Failure: {e}");
            foreach (var e in report.ConstraintMessages) Debug.Log($"cm: {e}");

            return report;
        }

        // ------------------------ Internals ------------------------

        // Spawns I/O and returns the created pins (inputs + outputs)
        List<DevPinInstance> SpawnPinsForLevel(DevChipInstance dev, LevelDefinition def)
        {
            var created = new List<DevPinInstance>();

            // Resolve labels (fallbacks)
            var inLabels = (def.inputLabels != null && def.inputLabels.Count > 0)
                ? def.inputLabels
                : MakeDefaultInputs(Mathf.Max(def.inputCount, 0));

            var outLabels = (def.outputLabels != null && def.outputLabels.Count > 0)
                ? def.outputLabels
                : MakeDefaultOutputs(Mathf.Max(def.outputCount, 0));

            // Layout
            const float xLeft = -3f;
            const float xRight = 3f;
            const float gapY = 0.6f;

            float yStartIn = ((inLabels.Count - 1) * 0.5f) * gapY;
            float yStartOut = ((outLabels.Count - 1) * 0.5f) * gapY;

            // Inputs
            for (int i = 0; i < def.inputCount; i++)
            {
                int id = IDGenerator.GenerateNewElementID(dev);
                var pos = new Vector2(xLeft, yStartIn - i * gapY);
                var desc = MakePinDescription(inLabels[i], id, pos, bitCount: 1, PinColour.Red);
                var pin = new DevPinInstance(desc, isInput: true);
                pin.anchoredToLevel = true;
                dev.AddNewDevPin(pin, isLoadingFromFile: false);
                created.Add(pin);
            }

            // Outputs
            for (int i = 0; i < def.outputCount; i++)
            {
                int id = IDGenerator.GenerateNewElementID(dev);
                var pos = new Vector2(xRight, yStartOut - i * gapY);
                var desc = MakePinDescription(outLabels[i], id, pos, bitCount: 1, PinColour.Red);
                var pin = new DevPinInstance(desc, isInput: false);
                pin.anchoredToLevel = true;
                dev.AddNewDevPin(pin, isLoadingFromFile: false);
                created.Add(pin);
            }

            return created;
        }

        // Clone via JsonUtility to avoid mutation side effects
        static LevelDefinition Clone(LevelDefinition src) =>
            JsonUtility.FromJson<LevelDefinition>(JsonUtility.ToJson(src));

        static List<string> MakeDefaultInputs(int count)
        {
            var list = new List<string>(count);
            for (int i = 0; i < count; i++) list.Add(((char)('A' + i)).ToString());
            return list;
        }

        static List<string> MakeDefaultOutputs(int count)
        {
            var list = new List<string>(count);
            for (int i = 0; i < count; i++) list.Add("OUT" + (i == 0 ? "" : (i + 1).ToString()));
            return list;
        }

        static PinDescription MakePinDescription(string name, int id, Vector2 pos, ushort bitCount)
        {
            return new PinDescription
            {
                Name = name,
                ID = id,
                Position = pos,
                BitCount = new PinBitCount(bitCount),
                Colour = PinColour.White,
                ValueDisplayMode = PinValueDisplayMode.Off,
            };
        }

        static PinDescription MakePinDescription(string name, int id, Vector2 pos, ushort bitCount, PinColour color)
        {
            return new PinDescription
            {
                Name = name,
                ID = id,
                Position = pos,
                BitCount = new PinBitCount(bitCount),
                Colour = color,
                ValueDisplayMode = PinValueDisplayMode.Off,
            };
        }

        /// <summary>
        /// Check if there are unsaved changes in the current level
        /// </summary>
        public bool HasUnsavedChanges()
        {
            if (!IsActive || string.IsNullOrEmpty(Current.id)) return false;
            
            try
            {
                // Check if there's saved progress for this level
                bool hasSavedProgress = LevelProgressService.HasLevelProgress(Current.id);
                
                if (!hasSavedProgress)
                {
                    // No saved progress, check if there are any elements in the current chip
                    return Project.ActiveProject?.ViewedChip?.Elements?.Count > 0;
                }
                
                // There is saved progress, compare current state with saved state
                var savedProgress = LevelProgressService.LoadLevelProgress(Current.id);
                if (savedProgress == null) return true; // Can't load saved progress, assume changes
                
                // Create current chip description
                var currentChip = Project.ActiveProject?.ViewedChip;
                if (currentChip == null) return true; // No current chip, assume changes
                
                var currentDescription = DescriptionCreator.CreateChipDescription(currentChip);
                
                // Compare using the same logic as chip unsaved changes
                return Saver.HasUnsavedChanges(savedProgress, currentDescription);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LevelManager] Failed to check unsaved changes: {ex.Message}");
                return false; // Conservative approach - don't block user if check fails
            }
        }

        /// <summary>
        /// Save the current progress of the level
        /// </summary>
        public void SaveCurrentProgress()
        {
            if (!IsActive || string.IsNullOrEmpty(Current.id)) 
            {
                Debug.LogWarning("[LevelManager] SaveCurrentProgress called but no active level");
                return;
            }
            
            try
            {
                var currentChip = Project.ActiveProject?.ViewedChip;
                if (currentChip == null)
                {
                    Debug.LogWarning("[LevelManager] SaveCurrentProgress called but no active chip");
                    return;
                }
                
                // Save the current chip state as level progress
                LevelProgressService.SaveLevelProgress(Current.id, currentChip);
                Debug.Log($"[LevelManager] Saved progress for level: {Current.id}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LevelManager] Failed to save level progress: {ex.Message}");
            }
        }

        /// <summary>
        /// Load saved progress for a level
        /// </summary>
        private void LoadLevelProgress(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return;
            
            try
            {
                var savedProgress = LevelProgressService.LoadLevelProgress(levelId);
                if (savedProgress != null)
                {
                    // Load the saved chip state
                    var currentChip = Project.ActiveProject?.ViewedChip;
                    if (currentChip != null)
                    {
                        // Apply the saved progress to the current chip
                        // This would require implementing a method to apply a ChipDescription to a DevChipInstance
                        // For now, just log that we found saved progress
                        Debug.Log($"[LevelManager] Found saved progress for level: {levelId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LevelManager] Failed to load level progress for {levelId}: {ex.Message}");
            }
        }
    }
}
