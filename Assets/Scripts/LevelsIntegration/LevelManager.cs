using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using DLS.Levels;
using DLS.Levels.Host;
using DLS.Game;
using DLS.Description;
using DLS.SaveSystem;
using DLS.Simulation;

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

            // 2) Spawn pins 
            SpawnPinsForLevel(dev, def);

            // 3) Fit view
            Debug.Log("Fitting level");
            var view = CameraController.GetViewForChip(dev);
            view.Pos = Vector2.zero;
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

            LevelEnded?.Invoke();
        }


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

        /// <summary>
        /// Generates test vectors by exhaustively testing all possible input combinations.
        /// Returns JSON string containing the test vectors.
        /// Supports multi-bit pins by using inputBitCounts and outputBitCounts.
        /// </summary>
        public string GenerateTestVectors()
        {
            if (!IsActive || Current == null)
            {
                Debug.LogWarning("[LevelManager] GenerateTestVectors called while level not active.");
                return null;
            }

            int inputCount = Current.inputCount;
            int outputCount = Current.outputCount;
            
            // Calculate total input bits (sum of all input pin bit counts)
            int totalInputBits = 0;
            if (Current.inputBitCounts != null && Current.inputBitCounts.Length > 0)
            {
                totalInputBits = Current.inputBitCounts.Sum();
            }
            else
            {
                // Fallback to old behavior if inputBitCounts not available
                totalInputBits = inputCount;
            }
            
            // Calculate total output bits (sum of all output pin bit counts)
            int totalOutputBits = 0;
            if (Current.outputBitCounts != null && Current.outputBitCounts.Length > 0)
            {
                totalOutputBits = Current.outputBitCounts.Sum();
            }
            else
            {
                // Fallback to old behavior if outputBitCounts not available
                totalOutputBits = outputCount;
            }
            
            // Calculate total combinations (2^totalInputBits)
            int totalCombinations = 1 << totalInputBits; // 2^totalInputBits
            const int maxTestVectors = 100;
            
            Debug.Log($"[LevelManager] Generating all {totalCombinations} test vectors for {inputCount} input pins ({totalInputBits} total bits), {outputCount} output pins ({totalOutputBits} total bits)");
            if (Current.inputBitCounts != null && Current.inputBitCounts.Length > 0)
            {
                Debug.Log($"[LevelManager] Input bit counts: [{string.Join(", ", Current.inputBitCounts)}]");
            }
            if (Current.outputBitCounts != null && Current.outputBitCounts.Length > 0)
            {
                Debug.Log($"[LevelManager] Output bit counts: [{string.Join(", ", Current.outputBitCounts)}]");
            }
            
            // ⚠️ CRITICAL: Sort circuit pins to match the level definition's expected order
            // The circuit's pin order might differ from the level definition if the user:
            // - Deleted and recreated pins in a different order
            // - Loaded a saved circuit with pins in a different order
            var dev = Project.ActiveProject.ViewedChip;
            var circuitInputPins = dev.GetInputPins();
            var circuitOutputPins = dev.GetOutputPins().ToArray();
            
            // Sort input pins by the level definition's expected order
            var sortedInputPins = SortPinsByLevelOrder(circuitInputPins, Current.inputPinLabels);
            var sortedOutputPins = SortPinsByLevelOrder(circuitOutputPins, Current.outputPinLabels);
            
            Debug.Log($"[LevelManager] Sorted input pins: [{string.Join(", ", sortedInputPins.Select(p => p.Name))}]");
            Debug.Log($"[LevelManager] Sorted output pins: [{string.Join(", ", sortedOutputPins.Select(p => p.Name))}]");
            
            // List to store all test vectors
            var allTestVectors = new List<TestVectorData>();
            
            // Loop through all possible input combinations
            for (int i = 0; i < totalCombinations; i++)
            {
                // Convert index to binary input string
                string inputBits = "";
                for (int bit = totalInputBits - 1; bit >= 0; bit--)
                {
                    inputBits += ((i >> bit) & 1) == 1 ? "1" : "0";
                }
                
                // Apply inputs to circuit (using SORTED pins)
                var inputVector = BitVector.FromString(inputBits);
                ApplyInputsInOrder(inputVector, sortedInputPins);
                
                // Let circuit settle
                _sim.SettleWithin(5, out _);
                
                // Read outputs (using SORTED pins)
                var outputVector = ReadOutputsInOrder(sortedOutputPins);
                string outputBits = outputVector.ToString();
                
                // Store test vector
                allTestVectors.Add(new TestVectorData
                {
                    inputs = inputBits,
                    expected = outputBits
                });
            }
            
            // Randomly sample up to maxTestVectors from all generated test vectors
            var testVectors = new List<TestVectorData>();
            if (allTestVectors.Count <= maxTestVectors)
            {
                // Use all test vectors if we have fewer than the limit
                testVectors = allTestVectors;
                Debug.Log($"[LevelManager] Using all {allTestVectors.Count} test vectors (under limit of {maxTestVectors})");
            }
            else
            {
                // Randomly sample maxTestVectors from all test vectors
                var random = new System.Random();
                var shuffledIndices = Enumerable.Range(0, allTestVectors.Count).OrderBy(x => random.Next()).Take(maxTestVectors);
                
                foreach (int index in shuffledIndices)
                {
                    testVectors.Add(allTestVectors[index]);
                }
                Debug.Log($"[LevelManager] Randomly sampled {maxTestVectors} test vectors from {allTestVectors.Count} total combinations");
            }
            
            // Create wrapper object for JSON export
            var wrapper = new TestVectorsWrapper
            {
                id = Current.id,
                name = Current.name,
                chapterId = Current.chapterId,
                description = Current.description,
                inputCount = inputCount,
                outputCount = outputCount,
                inputLabels = Current.inputLabels,
                outputLabels = Current.outputLabels,
                inputBitCounts = Current.inputBitCounts,
                outputBitCounts = Current.outputBitCounts,
                inputPinLabels = Current.inputPinLabels,
                outputPinLabels = Current.outputPinLabels,
                testVectors = testVectors.ToArray()
            };
            
            // Convert to JSON
            string json = JsonUtility.ToJson(wrapper, true);
            
            Debug.Log($"[LevelManager] Generated {testVectors.Count} test vectors");
            
            // Save to file
            #if UNITY_EDITOR
            try
            {
                string directoryPath = "Assets/GeneratedTestVectors";
                if (!System.IO.Directory.Exists(directoryPath))
                {
                    System.IO.Directory.CreateDirectory(directoryPath);
                    Debug.Log($"[LevelManager] Created directory: {directoryPath}");
                }
                
                // Save JSON version
                string jsonFileName = $"{Current.id}_testvectors.json";
                string jsonFilePath = System.IO.Path.Combine(directoryPath, jsonFileName);
                System.IO.File.WriteAllText(jsonFilePath, json);
                Debug.Log($"[LevelManager] Test vectors saved to: {jsonFilePath}");
                
                // Also save binary version for direct use
                string binaryFileName = $"{Current.id}.tvec";
                string binaryFilePath = System.IO.Path.Combine(directoryPath, binaryFileName);
                
                // Convert TestVectorData to LevelDefinition.TestVector
                var levelTestVectors = testVectors.Select(tv => new DLS.Levels.LevelDefinition.TestVector
                {
                    inputs = tv.inputs,
                    expected = tv.expected,
                    settleSteps = 0,
                    isClockEdge = false
                }).ToArray();
                
                TestVectorsBinaryFormat.WriteToFile(binaryFilePath, levelTestVectors, totalInputBits, totalOutputBits);
                Debug.Log($"[LevelManager] Binary test vectors saved to: {binaryFilePath}");
                
                // Refresh Unity's asset database to show the new files
                UnityEditor.AssetDatabase.Refresh();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LevelManager] Failed to save test vectors: {e.Message}");
            }
            #endif
            
            return json;
        }
        
        // Data structures for test vector generation
        [Serializable]
        private class TestVectorsWrapper
        {
            public string id;
            public string name;
            public string chapterId;
            public string description;
            public int inputCount;
            public int outputCount;
            public System.Collections.Generic.List<string> inputLabels;
            public System.Collections.Generic.List<string> outputLabels;
            public int[] inputBitCounts;
            public int[] outputBitCounts;
            public DLS.Levels.LevelDefinition.PinLabel[] inputPinLabels;
            public DLS.Levels.LevelDefinition.PinLabel[] outputPinLabels;
            public TestVectorData[] testVectors;
        }
        
        [Serializable]
        private class TestVectorData
        {
            public string inputs;
            public string expected;
        }
        
        /// <summary>
        /// Sorts circuit pins to match the level definition's expected order.
        /// This ensures generated test vectors use the correct bit positions.
        /// </summary>
        private DevPinInstance[] SortPinsByLevelOrder(IEnumerable<DevPinInstance> circuitPins, LevelDefinition.PinLabel[] expectedOrder)
        {
            var circuitPinsList = circuitPins.ToList();
            var sortedPins = new List<DevPinInstance>();
            
            // For each expected pin label, find the matching circuit pin
            foreach (var expectedLabel in expectedOrder)
            {
                var matchingPin = circuitPinsList.FirstOrDefault(p => 
                    p.Name.Equals(expectedLabel.name, StringComparison.OrdinalIgnoreCase));
                
                if (matchingPin != null)
                {
                    sortedPins.Add(matchingPin);
                }
                else
                {
                    Debug.LogWarning($"[LevelManager] Could not find circuit pin matching level definition: '{expectedLabel.name}'");
                }
            }
            
            // Warn if there are extra pins in the circuit that don't match the level definition
            if (sortedPins.Count != circuitPinsList.Count)
            {
                var extraPins = circuitPinsList.Where(p => !sortedPins.Contains(p)).Select(p => p.Name);
                Debug.LogWarning($"[LevelManager] Circuit has extra pins not in level definition: [{string.Join(", ", extraPins)}]");
            }
            
            return sortedPins.ToArray();
        }
        
        /// <summary>
        /// Applies input values to pins in the specified order (not using the default GetInputPins order).
        /// </summary>
        private void ApplyInputsInOrder(BitVector inputVector, DevPinInstance[] orderedPins)
        {
            int bitOffset = 0;
            
            foreach (var pin in orderedPins)
            {
                var pinBitCount = pin.Pin.bitCount.BitCount;
                
                if (pinBitCount == 1)
                {
                    // Single bit pin
                    if (bitOffset < inputVector.Length)
                    {
                        pin.Pin.PlayerInputState.SetFirstBit(inputVector[bitOffset]);
                        bitOffset++;
                    }
                }
                else
                {
                    // Multi-bit pin
                    ulong pinValue = 0;
                    for (int bitIndex = 0; bitIndex < pinBitCount && bitOffset < inputVector.Length; bitIndex++)
                    {
                        if (inputVector[bitOffset])
                        {
                            pinValue |= (1UL << bitIndex);
                        }
                        bitOffset++;
                    }
                    
                    // Set the value based on bit count
                    if (pinBitCount <= 16)
                    {
                        pin.Pin.PlayerInputState.SetShortValue((ushort)pinValue);
                    }
                    else if (pinBitCount <= 32)
                    {
                        pin.Pin.PlayerInputState.SetMediumValue((uint)pinValue);
                    }
                    else
                    {
                        // For >32 bits, just set the first 32 bits
                        pin.Pin.PlayerInputState.SetMediumValue((uint)pinValue);
                    }
                }
            }
        }
        
        /// <summary>
        /// Reads output values from pins in the specified order (not using the default GetOutputPins order).
        /// </summary>
        private BitVector ReadOutputsInOrder(DevPinInstance[] orderedPins)
        {
            var root = Project.ActiveProject.rootSimChip;
            if (root == null) return new BitVector(0, 0);
            
            ulong raw = 0UL;
            int bitOffset = 0;
            
            foreach (var pin in orderedPins)
            {
                var sPin = root.GetSimPinFromAddress(pin.Pin.Address);
                var pinBitCount = pin.Pin.bitCount.BitCount;
                
                if (pinBitCount == 1)
                {
                    // Single bit pin
                    if (sPin.State.FirstBitHigh())
                        raw |= (1UL << bitOffset);
                    bitOffset++;
                }
                else
                {
                    // Multi-bit pin
                    uint pinValue = 0;
                    if (pinBitCount <= 16)
                    {
                        pinValue = sPin.State.GetShortValues();
                    }
                    else if (pinBitCount <= 32)
                    {
                        pinValue = sPin.State.GetMediumValues();
                    }
                    else
                    {
                        // For >32 bits, just read the first 32 bits
                        pinValue = sPin.State.GetMediumValues();
                    }
                    
                    // Extract individual bits
                    for (int bitIndex = 0; bitIndex < pinBitCount; bitIndex++)
                    {
                        if ((pinValue & (1U << bitIndex)) != 0)
                        {
                            raw |= (1UL << bitOffset);
                        }
                        bitOffset++;
                    }
                }
            }
            
            return new BitVector(raw, bitOffset);
        }

        // ------------------------ Internals ------------------------

        // Spawns I/O and returns the created pins (inputs + outputs)
        void SpawnPinsForLevel(DevChipInstance dev, LevelDefinition def)
        {

            // Resolve labels (fallbacks)
            var inLabels = def.inputPinLabels;
            var outLabels = def.outputPinLabels;
            // Layout
            const float xLeft = -3f;
            const float xRight = 3f;
            const float gapY = 0.6f;

            float yStartIn = ((inLabels.Length - 1) * 0.5f) * gapY;
            float yStartOut = ((outLabels.Length - 1) * 0.5f) * gapY;

            // Inputs
            for (int i = 0; i < def.inputCount; i++)
            {
                int id = IDGenerator.GenerateNewElementID(dev);
                var pos = new Vector2(xLeft, yStartIn - i * gapY);
                
                // Get bit count for this input pin (default to 1 if not specified)
                int bitCount = 1;
                if (def.inputBitCounts != null && i < def.inputBitCounts.Length)
                {
                    bitCount = def.inputBitCounts[i];
                }
                
                var desc = MakePinDescription(inLabels[i].name, id, pos, bitCount: (ushort)bitCount, PinColour.Red);
                var pin = new DevPinInstance(desc, isInput: true);
                pin.anchoredToLevel = true;
                dev.AddNewDevPin(pin, isLoadingFromFile: false);
            }

            // Outputs
            for (int i = 0; i < def.outputCount; i++)
            {
                int id = IDGenerator.GenerateNewElementID(dev);
                var pos = new Vector2(xRight, yStartOut - i * gapY);
                
                // Get bit count for this output pin (default to 1 if not specified)
                int bitCount = 1;
                if (def.outputBitCounts != null && i < def.outputBitCounts.Length)
                {
                    bitCount = def.outputBitCounts[i];
                }
                
                var desc = MakePinDescription(outLabels[i].name, id, pos, bitCount: (ushort)bitCount, PinColour.Green);
                var pin = new DevPinInstance(desc, isInput: false);
                pin.anchoredToLevel = true;
                dev.AddNewDevPin(pin, isLoadingFromFile: false);
            }
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
            if (!IsActive || Current == null || string.IsNullOrEmpty(Current?.id)) return false;
            
            try
            {
                var currentChip = Project.ActiveProject?.ViewedChip;
                if (currentChip == null) return false;
                
                // For levels, we need special logic because levels automatically create I/O pins
                // Check if there's saved progress for this level
                bool hasSavedProgress = LevelProgressService.HasLevelProgress(Current.id);
                
                if (!hasSavedProgress)
                {
                    // No saved progress - check if user has added any custom elements (beyond the level's I/O pins)
                    // Count only elements that are NOT input/output pins (which are automatically created by the level)
                    int customElements = 0;
                    foreach (var element in currentChip.Elements)
                    {
                        if (element is not DevPinInstance)
                        {
                            customElements++;
                        }
                    }
                    return customElements > 0;
                }
                else
                {
                    // There is saved progress - compare current state with saved state
                    var savedProgress = LevelProgressService.LoadLevelProgress(Current.id);
                    if (savedProgress == null) return true; // Can't load saved progress, assume changes
                    
                    var currentDescription = DescriptionCreator.CreateChipDescription(currentChip);
                    return Saver.HasUnsavedChanges(savedProgress, currentDescription);
                }
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
            if (!IsActive || Current == null || string.IsNullOrEmpty(Current.id)) 
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
                
                // Create the current chip description to save
                var currentDescription = DescriptionCreator.CreateChipDescription(currentChip);
                
                // Save the current chip state as level progress
                LevelProgressService.SaveLevelProgress(Current.id, currentChip);
                
                // Update the chip's LastSavedDescription so HasUnsavedChanges works correctly
                currentChip.LastSavedDescription = currentDescription;
                
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
                    Debug.Log($"[LevelManager] Found saved progress for level: {levelId}");
                    
                    // Apply the saved chip description to the current chip
                    var project = Project.ActiveProject;
                    if (project != null)
                    {
                        // Load the saved chip description into a DevChipInstance
                        var (devChip, anyElementFailedToLoad) = DevChipInstance.LoadFromDescriptionTest(savedProgress, project.chipLibrary);
                        
                        if (devChip != null)
                        {
                            // Build the simulation chip for the loaded progress
                            var simChip = Simulator.BuildSimChip(devChip.LastSavedDescription, project.chipLibrary);
                            devChip.SetSimChip(simChip);
                            
                            // Replace the current chip with the loaded progress chip
                            // We need to access the private SetNewActiveDevChip method, so we'll use reflection
                            var method = typeof(Project).GetMethod("SetNewActiveDevChip", 
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            method?.Invoke(project, new object[] { devChip });
                            
                            Debug.Log($"[LevelManager] Successfully loaded saved progress for level {levelId}");
                        }
                        else
                        {
                            Debug.LogError($"[LevelManager] Failed to create chip instance from saved progress for level {levelId}");
                        }
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
