using UnityEngine;
using DLS.Online;

namespace DLS.Online
{
    /// <summary>
    /// Simple prefab component that can be added to any GameObject to create a solution tester.
    /// This is the easiest way to add the tester to any scene.
    /// </summary>
    public class EditorSolutionTesterPrefab : MonoBehaviour
    {
        [Header("Solution Tester Prefab")]
        [TextArea(3, 5)]
        public string instructions = 
            "This prefab automatically sets up the EditorSolutionTester.\n" +
            "Just add this component to any GameObject and the tester will appear!\n" +
            "Look for the GUI overlay in the Scene view.";
        
        [Header("Auto Setup")]
        [SerializeField] private bool autoSetupOnStart = true;
        [SerializeField] private bool showInstructions = true;
        
        void Start()
        {
            if (autoSetupOnStart && Application.isEditor)
            {
                SetupTester();
            }
        }
        
        [ContextMenu("Setup Solution Tester")]
        public void SetupTester()
        {
            if (!Application.isEditor)
            {
                Debug.LogWarning("[EditorSolutionTesterPrefab] This only works in the Unity Editor!");
                return;
            }
            
            // Remove existing tester if it exists
            var existingTester = GetComponent<EditorSolutionTester>();
            if (existingTester != null)
            {
                DestroyImmediate(existingTester);
            }
            
            // Add the tester component
            var tester = gameObject.AddComponent<EditorSolutionTester>();
            
            if (showInstructions)
            {
                Debug.Log("[EditorSolutionTesterPrefab] ✅ Solution Tester Setup Complete!");
                Debug.Log("[EditorSolutionTesterPrefab] Look for the GUI overlay in the Scene view (top-left corner)");
                Debug.Log("[EditorSolutionTesterPrefab] Use the quick test buttons: 🔥 Firebase, 🏠 Local, 🎮 Level");
            }
        }
        
        [ContextMenu("Remove Solution Tester")]
        public void RemoveTester()
        {
            var tester = GetComponent<EditorSolutionTester>();
            if (tester != null)
            {
                DestroyImmediate(tester);
                Debug.Log("[EditorSolutionTesterPrefab] ❌ Solution Tester removed!");
            }
        }
        
        [ContextMenu("Toggle Instructions")]
        public void ToggleInstructions()
        {
            showInstructions = !showInstructions;
            Debug.Log($"[EditorSolutionTesterPrefab] Instructions display: {(showInstructions ? "ON" : "OFF")}");
        }
        
        void OnValidate()
        {
            if (autoSetupOnStart && Application.isEditor)
            {
                SetupTester();
            }
        }
    }
}
