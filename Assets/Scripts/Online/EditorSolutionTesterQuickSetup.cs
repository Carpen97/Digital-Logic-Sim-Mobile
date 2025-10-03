using UnityEngine;
using DLS.Online;

namespace DLS.Online
{
    /// <summary>
    /// Quick setup script that can be added to any GameObject to enable solution testing in the editor.
    /// This is the easiest way to add the tester to any scene.
    /// </summary>
    public class EditorSolutionTesterQuickSetup : MonoBehaviour
    {
        [Header("Quick Setup - Just Add This Component!")]
        [TextArea(3, 5)]
        public string instructions = 
            "1. Add this component to any GameObject\n" +
            "2. The EditorSolutionTester will automatically be added\n" +
            "3. Look for the GUI overlay in the Scene view\n" +
            "4. Use it to test solution loading without mobile deployment!";
        
        void Awake()
        {
            SetupTester();
        }
        
        void SetupTester()
        {
            if (!Application.isEditor)
            {
                Debug.Log("[EditorSolutionTesterQuickSetup] This only works in the Unity Editor!");
                return;
            }
            
            // Add the tester component if it doesn't exist
            if (GetComponent<EditorSolutionTester>() == null)
            {
                var tester = gameObject.AddComponent<EditorSolutionTester>();
                Debug.Log("[EditorSolutionTesterQuickSetup] ✅ EditorSolutionTester added! Look for the GUI in Scene view.");
            }
            else
            {
                Debug.Log("[EditorSolutionTesterQuickSetup] ✅ EditorSolutionTester already exists!");
            }
        }
        
        [ContextMenu("Re-setup Tester")]
        public void ReSetupTester()
        {
            // Remove existing tester
            var existingTester = GetComponent<EditorSolutionTester>();
            if (existingTester != null)
            {
                DestroyImmediate(existingTester);
            }
            
            // Add new tester
            SetupTester();
        }
        
        [ContextMenu("Remove Tester")]
        public void RemoveTester()
        {
            var tester = GetComponent<EditorSolutionTester>();
            if (tester != null)
            {
                DestroyImmediate(tester);
                Debug.Log("[EditorSolutionTesterQuickSetup] ❌ EditorSolutionTester removed!");
            }
        }
    }
}
