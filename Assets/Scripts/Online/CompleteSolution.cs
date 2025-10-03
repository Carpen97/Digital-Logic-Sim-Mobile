using System;
using System.Collections.Generic;
using DLS.Description;

namespace DLS.Online
{
    /// <summary>
    /// Represents a complete solution with all custom chip definitions for full reproducibility.
    /// </summary>
    [Serializable]
    public class CompleteSolution
    {
        public string LevelId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public int Score { get; set; }
        public DateTime SubmittedAt { get; set; }
        
        /// <summary>
        /// The main solution chip (the level's root chip)
        /// </summary>
        public ChipDescription MainSolution { get; set; }
        
        /// <summary>
        /// All custom chip definitions used in the solution, keyed by prefixed name
        /// </summary>
        public Dictionary<string, ChipDescription> CustomChipDefinitions { get; set; }
        
        /// <summary>
        /// Solution metadata for analysis and display
        /// </summary>
        public SolutionMetadata Metadata { get; set; }

        public CompleteSolution()
        {
            CustomChipDefinitions = new Dictionary<string, ChipDescription>();
            Metadata = new SolutionMetadata();
        }

        public CompleteSolution(string levelId, string userId, int score, ChipDescription mainSolution)
        {
            LevelId = levelId;
            UserId = userId;
            Score = score;
            SubmittedAt = DateTime.UtcNow;
            MainSolution = mainSolution;
            CustomChipDefinitions = new Dictionary<string, ChipDescription>();
            Metadata = new SolutionMetadata();
        }
    }

    /// <summary>
    /// Metadata about a complete solution for analysis and display
    /// </summary>
    [Serializable]
    public class SolutionMetadata
    {
        public int NandGateCount { get; set; }
        public int TotalComponents { get; set; }
        public int WireCount { get; set; }
        public string DLSVersion { get; set; }
        public List<string> CustomChipNames { get; set; }
        public int SolutionSizeBytes { get; set; }
        public DateTime CreatedAt { get; set; }

        public SolutionMetadata()
        {
            CustomChipNames = new List<string>();
            DLSVersion = "2.1.6.8"; // Current version
            CreatedAt = DateTime.UtcNow;
        }
    }
}
