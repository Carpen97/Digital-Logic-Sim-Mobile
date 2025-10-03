using UnityEngine;
using DLS.Online;

namespace DLS.Online
{
    /// <summary>
    /// Simple setup script to add the EditorSolutionTester to a GameObject.
    /// This makes it easy to add the tester to any scene.
    /// </summary>
    public class EditorSolutionTesterSetup : MonoBehaviour
    {
        [Header("Setup Instructions")]
        [TextArea(5, 10)]
        public string instructions = 
            "Editor Solution Tester Setup:\n\n" +
            "1. This script adds the EditorSolutionTester component\n" +
            "2. The tester will appear as a GUI overlay in the Scene view\n" +
            "3. Use it to test solution loading without deploying to mobile\n" +
            "4. Right-click on this component for context menu options\n\n" +
            "Note: This only works in the Unity Editor!";
        
        [Header("Quick Setup")]
        [SerializeField] private bool autoSetup = true;
        
        void Start()
        {
            if (autoSetup && Application.isEditor)
            {
                SetupTester();
            }
        }
        
        [ContextMenu("Setup Editor Solution Tester")]
        public void SetupTester()
        {
            if (!Application.isEditor)
            {
                Debug.LogWarning("[EditorSolutionTesterSetup] This only works in the Unity Editor!");
                return;
            }
            
            // Add the tester component if it doesn't exist
            if (GetComponent<EditorSolutionTester>() == null)
            {
                var tester = gameObject.AddComponent<EditorSolutionTester>();
                Debug.Log("[EditorSolutionTesterSetup] Added EditorSolutionTester component");
            }
            else
            {
                Debug.Log("[EditorSolutionTesterSetup] EditorSolutionTester already exists");
            }
        }
        
        [ContextMenu("Remove Editor Solution Tester")]
        public void RemoveTester()
        {
            var tester = GetComponent<EditorSolutionTester>();
            if (tester != null)
            {
                DestroyImmediate(tester);
                Debug.Log("[EditorSolutionTesterSetup] Removed EditorSolutionTester component");
            }
        }
        
        void OnValidate()
        {
            if (autoSetup && Application.isEditor)
            {
                SetupTester();
            }
        }
    }
}
