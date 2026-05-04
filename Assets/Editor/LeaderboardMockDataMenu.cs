using UnityEditor;
using UnityEngine;
using DLS.Online;

namespace DLS.Editor
{
    /// <summary>
    /// Unity Editor menu items for managing leaderboard mock data
    /// </summary>
    public static class LeaderboardMockDataMenu
    {
        [MenuItem("DLS/Mock Data/Regenerate Leaderboard Data")]
        public static void RegenerateMockData()
        {
            EditorLocalStorage.RegenerateMockData();
            EditorUtility.DisplayDialog(
                "Mock Data Regenerated",
                "Leaderboard mock data has been regenerated with fresh test data.\n\n" +
                "This includes:\n" +
                "• 10-15 scores per level\n" +
                "• Varied usernames (including long names, emojis, etc.)\n" +
                "• Realistic score distribution\n" +
                "• Timestamps over the past week",
                "OK"
            );
        }
        
        [MenuItem("DLS/Mock Data/Clear All Mock Data")]
        public static void ClearAllMockData()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Clear Mock Data?",
                "This will delete all local mock leaderboard data.\n\n" +
                "The data will be regenerated next time you open the leaderboard in Play mode.",
                "Clear",
                "Cancel"
            );
            
            if (confirmed)
            {
                EditorLocalStorage.ClearAll();
                EditorUtility.DisplayDialog("Mock Data Cleared", "All mock data has been cleared.", "OK");
            }
        }
        
        [MenuItem("DLS/Mock Data/Show Mock Data Stats")]
        public static void ShowMockDataStats()
        {
            EditorLocalStorage.Initialize();
            var stats = EditorLocalStorage.GetStats();
            
            EditorUtility.DisplayDialog(
                "Mock Data Statistics",
                $"Current mock data:\n\n" +
                $"• Scores: {stats.scores}\n" +
                $"• Complete Solutions: {stats.solutions}\n\n" +
                $"Storage location:\n{Application.persistentDataPath}/EditorLocalStorage",
                "OK"
            );
        }

        const string PREF_USE_FIREBASE_IN_EDITOR = "DLS.UseFirebaseInEditor";

        [MenuItem("DLS/Project Sharing/Use real Firebase in Editor (testing)", false, 100)]
        public static void ToggleUseFirebaseInEditor()
        {
            int current = PlayerPrefs.GetInt(PREF_USE_FIREBASE_IN_EDITOR, 0);
            int next = current == 0 ? 1 : 0;
            PlayerPrefs.SetInt(PREF_USE_FIREBASE_IN_EDITOR, next);
            PlayerPrefs.Save();
            string msg = next == 1
                ? "Project Sharing will use REAL Firebase in Editor. You can test Import/Browse/Upload without building. " +
                  "If Editor crashes (uWS), try the Firebase 13.7.0 upgrade (see FIREBASE_WINDOWS_CRASH_FIX.md). Restart Play mode."
                : "Project Sharing will use mock/dummy data in Editor. Restart Play mode for the change to take effect.";
            EditorUtility.DisplayDialog("Project Sharing in Editor", msg, "OK");
        }

        [MenuItem("DLS/Project Sharing/Use real Firebase in Editor (testing)", true)]
        public static bool ToggleUseFirebaseInEditorValidate()
        {
            bool enabled = PlayerPrefs.GetInt(PREF_USE_FIREBASE_IN_EDITOR, 0) == 1;
            Menu.SetChecked("DLS/Project Sharing/Use real Firebase in Editor (testing)", enabled);
            return true;
        }
    }
}

