using System;

namespace DLS.Online
{
    /// <summary>
    /// Represents a score entry in the leaderboard.
    /// </summary>
    [Serializable]
    public class ScoreEntry
    {
        public string id;               // Firestore document ID
        public string levelId;
        public string userId;
        public string userName;         // Display name for leaderboard, may be null/empty for anonymous
        public int score;
        public DateTime submittedAtUtc;
        public string solutionJsonPath;   // Storage path, may be null
        public string solutionImagePath;  // Storage path, may be null
        public string completeSolutionId; // Complete solution document ID, may be null

        public ScoreEntry()
        {
            id = "";
            levelId = "";
            userId = "";
            userName = "";
            score = 0;
            submittedAtUtc = DateTime.UtcNow;
            solutionJsonPath = null;
            solutionImagePath = null;
        }

        public ScoreEntry(string id, string levelId, string userId, int score, DateTime submittedAtUtc, 
                         string solutionJsonPath = null, string solutionImagePath = null, string userName = null)
        {
            this.id = id;
            this.levelId = levelId;
            this.userId = userId;
            this.userName = userName ?? "";
            this.score = score;
            this.submittedAtUtc = submittedAtUtc;
            this.solutionJsonPath = solutionJsonPath;
            this.solutionImagePath = solutionImagePath;
        }
    }
}
