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
    }
}

