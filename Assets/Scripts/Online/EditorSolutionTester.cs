using System;
using UnityEngine;
using DLS.Online;
using DLS.Description;
using DLS.Game;
using DLS.Game.LevelsIntegration;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DLS.Online
{
    /// <summary>
    /// Editor-only tool for testing solution loading without deploying to mobile.
    /// Provides a simple UI to test loading solutions from Firebase.
    /// </summary>
    public class EditorSolutionTester : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private string testSolutionId = "gx6Aty5qinR7YsXhaynl"; // Example solution ID from your logs
        [SerializeField] private string testLevelId = "NOT Gate";
        
        [Header("Quick Test Buttons")]
        [SerializeField] private bool showQuickButtons = true;
        
        
        [Header("GUI Settings")]
        [SerializeField] private bool showGUI = true;
        
        [Header("Safe Testing")]
        [SerializeField] private bool useSafeMode = true;
        
        [Header("Test Results")]
        [SerializeField] private bool isTestRunning = false;
        [SerializeField] private string lastTestResult = "";
        [SerializeField] private string lastError = "";
        
        private void OnGUI()
        {
            if (!Application.isEditor) return;
            
            // Prevent crashes when Unity loses focus
            if (!Application.isFocused && !Application.isPlaying) return;
            
            // Allow toggling GUI on/off
            if (!showGUI) return;
            
            // Create a simple GUI window for testing
            GUILayout.BeginArea(new Rect(10, 10, 1800, 1500));
            GUILayout.BeginVertical("box");
            
            // Title with much larger font
            var titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 48;
            titleStyle.fontStyle = FontStyle.Bold;
            GUILayout.Label("🔧 Editor Solution Tester", titleStyle);
            GUILayout.Space(45);
            
            // Test solution ID input with much larger fields
            var labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 24;
            GUILayout.Label("Solution ID:", labelStyle);
            testSolutionId = GUILayout.TextField(testSolutionId, GUILayout.Height(75));
            
            GUILayout.Space(15);
            GUILayout.Label("Level ID:", labelStyle);
            testLevelId = GUILayout.TextField(testLevelId, GUILayout.Height(75));
            
            GUILayout.Space(10);
            
            // Quick test buttons
            if (showQuickButtons)
            {
                var quickLabelStyle = new GUIStyle(GUI.skin.label);
                quickLabelStyle.fontSize = 42;
                quickLabelStyle.fontStyle = FontStyle.Bold;
                GUILayout.Label("Quick Tests:", quickLabelStyle);
                
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("🔥 Firebase", GUILayout.Height(105), GUILayout.Width(360)) && !isTestRunning)
                {
                    _ = TestLoadSolutionFromFirebase();
                }
                if (GUILayout.Button("🏠 Local", GUILayout.Height(105), GUILayout.Width(360)) && !isTestRunning)
                {
                    TestCreateLocalSolution();
                }
                if (GUILayout.Button("🎮 Level", GUILayout.Height(105), GUILayout.Width(360)) && !isTestRunning)
                {
                    TestExitLevelMode();
                }
                GUILayout.EndHorizontal();
                
                GUILayout.Space(30);
            }
            
            // Detailed test buttons
            if (GUILayout.Button("Test Load Solution from Firebase", GUILayout.Height(30)) && !isTestRunning)
            {
                _ = TestLoadSolutionFromFirebase();
            }
            
            if (GUILayout.Button("Test Create Local Solution", GUILayout.Height(30)) && !isTestRunning)
            {
                TestCreateLocalSolution();
            }
            
            if (GUILayout.Button("Test Exit Level Mode", GUILayout.Height(30)) && !isTestRunning)
            {
                TestExitLevelMode();
            }
            
            if (GUILayout.Button("🔄 Test Local Storage Workflow", GUILayout.Height(30)) && !isTestRunning)
            {
                TestLocalStorageWorkflow();
            }
            
            GUILayout.Space(10);
            
            // Status display with better styling
            if (isTestRunning)
            {
                var runningStyle = new GUIStyle(GUI.skin.label);
                runningStyle.fontSize = 14;
                runningStyle.normal.textColor = Color.yellow;
                GUILayout.Label("⏳ Test Running...", runningStyle);
            }
            
            if (!string.IsNullOrEmpty(lastTestResult))
            {
                var resultStyle = new GUIStyle(GUI.skin.label);
                resultStyle.fontSize = 12;
                resultStyle.normal.textColor = Color.green;
                GUILayout.Label($"✅ Result: {lastTestResult}", resultStyle);
            }
            
            if (!string.IsNullOrEmpty(lastError))
            {
                var errorStyle = new GUIStyle(GUI.skin.label);
                errorStyle.fontSize = 12;
                errorStyle.normal.textColor = Color.red;
                GUILayout.Label($"❌ Error: {lastError}", errorStyle);
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        /// <summary>
        /// Test loading a solution from Firebase
        /// </summary>
        private async System.Threading.Tasks.Task TestLoadSolutionFromFirebase()
        {
            try
            {
                isTestRunning = true;
                lastError = "";
                lastTestResult = "Starting Firebase test...";
                
                Debug.Log("[EditorSolutionTester] Starting Firebase solution load test...");
                
                // Initialize Firebase
                lastTestResult = "Initializing Firebase...";
                await FirebaseBootstrap.InitializeAsync();
                
                if (!FirebaseBootstrap.IsInitialized)
                {
                    lastError = "Firebase initialization failed";
                    lastTestResult = "Firebase initialization failed";
                    return;
                }
                
                Debug.Log($"[EditorSolutionTester] Firebase initialized, loading solution {testSolutionId}");
                lastTestResult = "Loading solution from Firebase...";
                
                // Load the solution
                var solution = await LeaderboardService.GetCompleteSolutionAsync(testSolutionId);
                
                if (solution == null)
                {
                    lastError = "Failed to load solution from Firebase";
                    lastTestResult = "Solution not found";
                    return;
                }
                
                Debug.Log($"[EditorSolutionTester] Solution loaded: {solution.LevelId} by {solution.UserName}");
                lastTestResult = $"Solution loaded: {solution.LevelId} by {solution.UserName}";
                
                // Load the solution into the project
                var project = Project.ActiveProject;
                if (project?.chipLibrary == null)
                {
                    lastError = "No active project or chip library found";
                    lastTestResult = "No project found";
                    return;
                }
                
                Debug.Log("[EditorSolutionTester] Loading solution into project...");
                lastTestResult = "Loading solution into project...";
                
                bool success = SolutionSerializer.LoadCompleteSolution(solution, project.chipLibrary);
                
                if (success)
                {
                    lastTestResult = $"Solution loaded successfully! Level: {solution.LevelId}, Score: {solution.Score}";
                    Debug.Log($"[EditorSolutionTester] Solution loaded successfully: {solution.LevelId}");
                }
                else
                {
                    lastError = "Failed to load solution into project";
                    lastTestResult = "Failed to load solution";
                }
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                lastTestResult = "Test failed with exception";
                Debug.LogError($"[EditorSolutionTester] Test failed: {ex.Message}");
                Debug.LogError($"[EditorSolutionTester] Stack trace: {ex.StackTrace}");
            }
            finally
            {
                isTestRunning = false;
            }
        }
        
        /// <summary>
        /// Test creating a local solution from current project
        /// </summary>
        private void TestCreateLocalSolution()
        {
            try
            {
                lastError = "";
                lastTestResult = "Creating local solution...";
                
                Debug.Log("[EditorSolutionTester] Starting local solution test...");
                
                if (useSafeMode)
                {
                    TestCreateLocalSolutionSafe();
                    return;
                }
                
                // Check if we have an active project
                var project = Project.ActiveProject;
                if (project == null)
                {
                    lastError = "No active project found";
                    lastTestResult = "No project found";
                    Debug.LogError("[EditorSolutionTester] No active project found");
                    return;
                }
                
                Debug.Log($"[EditorSolutionTester] Active project found: {project.description.ProjectName}");
                
                // Check chip library
                if (project.chipLibrary == null)
                {
                    lastError = "No chip library found in project";
                    lastTestResult = "No chip library found";
                    Debug.LogError("[EditorSolutionTester] No chip library found");
                    return;
                }
                
                Debug.Log("[EditorSolutionTester] Chip library found");
                
                // Check if we have a main solution (check if we have any chips in the library)
                if (project.chipLibrary == null || project.chipLibrary.allChips.Count == 0)
                {
                    lastError = "No chips found in project";
                    lastTestResult = "No chips found";
                    Debug.LogError("[EditorSolutionTester] No chips found in project");
                    return;
                }
                
                Debug.Log($"[EditorSolutionTester] Project has {project.chipLibrary.allChips.Count} chips");
                
                // Create a test solution from current project
                Debug.Log("[EditorSolutionTester] Creating complete solution...");
                var solution = SolutionSerializer.CreateCompleteSolutionFromCurrentProject(
                    testLevelId, 
                    42, // Test score
                    "EditorTestUser"
                );
                
                Debug.Log($"[EditorSolutionTester] Created local solution: {solution.LevelId}");
                lastTestResult = $"Created local solution: {solution.LevelId}";
                
                // Test serialization
                Debug.Log("[EditorSolutionTester] Testing serialization...");
                var json = SolutionSerializer.SerializeCompleteSolution(solution);
                Debug.Log($"[EditorSolutionTester] Serialized solution, JSON length: {json.Length}");
                
                // Test deserialization
                Debug.Log("[EditorSolutionTester] Testing deserialization...");
                var deserializedSolution = SolutionSerializer.DeserializeCompleteSolution(json);
                if (deserializedSolution != null)
                {
                    lastTestResult = $"Local solution test successful! JSON length: {json.Length}";
                    Debug.Log($"[EditorSolutionTester] Deserialization successful");
                }
                else
                {
                    lastError = "Deserialization failed";
                    lastTestResult = "Deserialization failed";
                    Debug.LogError("[EditorSolutionTester] Deserialization failed");
                }
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                lastTestResult = "Local test failed";
                Debug.LogError($"[EditorSolutionTester] Local test failed: {ex.Message}");
                Debug.LogError($"[EditorSolutionTester] Stack trace: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Safe mode test that doesn't require a full project setup
        /// </summary>
        private void TestCreateLocalSolutionSafe()
        {
            try
            {
                Debug.Log("[EditorSolutionTester] Running safe mode test...");
                lastTestResult = "Running safe mode test...";
                
                // Test basic serialization without project dependencies
                var testChip = new ChipDescription();
                testChip.Name = "TestChip";
                var testSolution = new CompleteSolution("TestLevel", "TestUser", 42, testChip);
                testSolution.UserName = "EditorTestUser";
                
                Debug.Log("[EditorSolutionTester] Created test solution");
                lastTestResult = "Created test solution";
                
                // Test serialization
                Debug.Log("[EditorSolutionTester] Testing serialization...");
                var json = SolutionSerializer.SerializeCompleteSolution(testSolution);
                Debug.Log($"[EditorSolutionTester] Serialized solution, JSON length: {json.Length}");
                
                // Test deserialization
                Debug.Log("[EditorSolutionTester] Testing deserialization...");
                var deserializedSolution = SolutionSerializer.DeserializeCompleteSolution(json);
                if (deserializedSolution != null)
                {
                    lastTestResult = $"Safe mode test successful! JSON length: {json.Length}";
                    Debug.Log($"[EditorSolutionTester] Safe mode test successful");
                }
                else
                {
                    lastError = "Safe mode deserialization failed";
                    lastTestResult = "Safe mode deserialization failed";
                    Debug.LogError("[EditorSolutionTester] Safe mode deserialization failed");
                }
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                lastTestResult = "Safe mode test failed";
                Debug.LogError($"[EditorSolutionTester] Safe mode test failed: {ex.Message}");
                Debug.LogError($"[EditorSolutionTester] Stack trace: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Test exiting level mode
        /// </summary>
        private void TestExitLevelMode()
        {
            try
            {
                lastError = "";
                lastTestResult = "Testing level mode exit...";
                
                if (LevelManager.Instance == null)
                {
                    lastError = "LevelManager not found";
                    lastTestResult = "LevelManager not found";
                    return;
                }
                
                bool wasActive = LevelManager.Instance.IsActive;
                string currentLevel = LevelManager.Instance.Current?.name;
                
                Debug.Log($"[EditorSolutionTester] Current level state - IsActive: {wasActive}, Current: {currentLevel}");
                
                if (wasActive)
                {
                    LevelManager.Instance.ExitLevel();
                    Debug.Log($"[EditorSolutionTester] Exited level mode");
                    lastTestResult = $"Exited level mode. Was: {currentLevel}";
                }
                else
                {
                    lastTestResult = "No active level to exit";
                }
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                lastTestResult = "Level exit test failed";
                Debug.LogError($"[EditorSolutionTester] Level exit test failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Clear test results
        /// </summary>
        [ContextMenu("Clear Test Results")]
        public void ClearTestResults()
        {
            lastTestResult = "";
            lastError = "";
            isTestRunning = false;
        }
        
        /// <summary>
        /// Set test solution ID from context menu
        /// </summary>
        [ContextMenu("Set Test Solution ID from Logs")]
        public void SetTestSolutionIdFromLogs()
        {
            // Set the solution ID from your recent logs
            testSolutionId = "gx6Aty5qinR7YsXhaynl";
            testLevelId = "NOT Gate";
            Debug.Log($"[EditorSolutionTester] Set test solution ID: {testSolutionId}");
        }
        
        /// <summary>
        /// Toggle GUI visibility to prevent crashes
        /// </summary>
        [ContextMenu("Toggle GUI Visibility")]
        public void ToggleGUIVisibility()
        {
            showGUI = !showGUI;
            Debug.Log($"[EditorSolutionTester] GUI visibility: {(showGUI ? "ON" : "OFF")}");
        }
        
        /// <summary>
        /// Hide GUI to prevent crashes
        /// </summary>
        [ContextMenu("Hide GUI (Prevent Crashes)")]
        public void HideGUI()
        {
            showGUI = false;
            Debug.Log("[EditorSolutionTester] GUI hidden to prevent crashes");
        }
        
        /// <summary>
        /// Show GUI for testing
        /// </summary>
        [ContextMenu("Show GUI (Enable Testing)")]
        public void ShowGUI()
        {
            showGUI = true;
            Debug.Log("[EditorSolutionTester] GUI shown for testing");
        }
        
        /// <summary>
        /// Test the complete local storage workflow
        /// </summary>
        private void TestLocalStorageWorkflow()
        {
            try
            {
                lastError = "";
                lastTestResult = "Testing local storage workflow...";
                
                Debug.Log("[EditorSolutionTester] Starting local storage workflow test...");
                
                // Initialize local storage
                EditorLocalStorage.Initialize();
                
                // Create a test solution
                var testChip = new ChipDescription();
                testChip.Name = "TestChip";
                var testSolution = new CompleteSolution("TestLevel", "TestUser", 42, testChip);
                testSolution.UserName = "EditorTestUser";
                
                Debug.Log("[EditorSolutionTester] Created test solution");
                lastTestResult = "Created test solution";
                
                // Save to local storage
                var solutionId = EditorLocalStorage.SaveCompleteSolution(testSolution);
                if (string.IsNullOrEmpty(solutionId))
                {
                    lastError = "Failed to save solution to local storage";
                    lastTestResult = "Save failed";
                    return;
                }
                
                Debug.Log($"[EditorSolutionTester] Saved solution with ID: {solutionId}");
                lastTestResult = $"Saved solution: {solutionId}";
                
                // Save a score that references this solution
                EditorLocalStorage.SaveScore("NOT Gate", 1, "EditorTestUser", solutionId);
                
                Debug.Log("[EditorSolutionTester] Saved score with solution reference");
                lastTestResult = $"Saved score and solution: {solutionId}";
                
                // Test loading the solution
                var loadedSolution = EditorLocalStorage.GetCompleteSolution(solutionId);
                if (loadedSolution == null)
                {
                    lastError = "Failed to load solution from local storage";
                    lastTestResult = "Load failed";
                    return;
                }
                
                Debug.Log($"[EditorSolutionTester] Loaded solution: {loadedSolution.LevelId}");
                lastTestResult = $"Workflow test successful! Solution: {loadedSolution.LevelId}";
                
                // Test getting scores
                var scores = EditorLocalStorage.GetTopScores("TestLevel", 10);
                Debug.Log($"[EditorSolutionTester] Retrieved {scores.Count} scores");
                
                // Update the test solution ID for easy testing
                testSolutionId = solutionId;
                Debug.Log($"[EditorSolutionTester] Updated test solution ID to: {solutionId}");
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                lastTestResult = "Local storage workflow test failed";
                Debug.LogError($"[EditorSolutionTester] Local storage workflow test failed: {ex.Message}");
            }
        }
    }
}
