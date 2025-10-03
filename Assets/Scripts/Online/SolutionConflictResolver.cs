using System;
using System.Collections.Generic;
using System.Linq;
using DLS.Description;
using DLS.Game;

namespace DLS.Online
{
    /// <summary>
    /// Handles naming conflicts for custom chips in complete solutions.
    /// Uses user ID prefixing to avoid conflicts while preserving original names.
    /// </summary>
    public static class SolutionConflictResolver
    {
        private const string SEPARATOR = "_";
        
        /// <summary>
        /// Resolves chip name conflicts by prefixing with user ID.
        /// </summary>
        /// <param name="originalName">The original chip name</param>
        /// <param name="userId">The user ID to prefix with</param>
        /// <returns>Prefixed chip name for conflict resolution</returns>
        public static string ResolveChipNameConflict(string originalName, string userId)
        {
            if (string.IsNullOrEmpty(originalName) || string.IsNullOrEmpty(userId))
                return originalName;
                
            // Avoid double-prefixing
            if (originalName.StartsWith(userId + SEPARATOR))
                return originalName;
                
            return $"{userId}{SEPARATOR}{originalName}";
        }
        
        /// <summary>
        /// Restores the original chip name by removing the user ID prefix.
        /// </summary>
        /// <param name="prefixedName">The prefixed chip name</param>
        /// <returns>The original chip name</returns>
        public static string RestoreOriginalChipName(string prefixedName)
        {
            if (string.IsNullOrEmpty(prefixedName))
                return prefixedName;
                
            int separatorIndex = prefixedName.IndexOf(SEPARATOR);
            if (separatorIndex > 0)
            {
                return prefixedName.Substring(separatorIndex + 1);
            }
            
            return prefixedName;
        }
        
        /// <summary>
        /// Checks if a chip name is a custom chip (not builtin).
        /// </summary>
        /// <param name="chipName">The chip name to check</param>
        /// <param name="chipLibrary">The chip library to check against</param>
        /// <returns>True if the chip is custom</returns>
        public static bool IsCustomChip(string chipName, ChipLibrary chipLibrary)
        {
            if (string.IsNullOrEmpty(chipName) || chipLibrary == null)
                return false;
                
            return !chipLibrary.IsBuiltinChip(chipName);
        }
        
        /// <summary>
        /// Updates chip references in a chip description to use prefixed names.
        /// </summary>
        /// <param name="chipDescription">The chip description to update</param>
        /// <param name="userId">The user ID for prefixing</param>
        /// <param name="chipLibrary">The chip library to check for custom chips</param>
        public static void UpdateChipReferences(ChipDescription chipDescription, string userId, ChipLibrary chipLibrary)
        {
            if (chipDescription?.SubChips == null)
                return;
                
            for (int i = 0; i < chipDescription.SubChips.Length; i++)
            {
                var subChip = chipDescription.SubChips[i];
                if (IsCustomChip(subChip.Name, chipLibrary))
                {
                    subChip.Name = ResolveChipNameConflict(subChip.Name, userId);
                    chipDescription.SubChips[i] = subChip;
                }
            }
        }
        
        /// <summary>
        /// Restores chip references in a chip description to use original names.
        /// </summary>
        /// <param name="chipDescription">The chip description to update</param>
        public static void RestoreChipReferences(ChipDescription chipDescription)
        {
            if (chipDescription?.SubChips == null)
                return;
                
            for (int i = 0; i < chipDescription.SubChips.Length; i++)
            {
                var subChip = chipDescription.SubChips[i];
                subChip.Name = RestoreOriginalChipName(subChip.Name);
                chipDescription.SubChips[i] = subChip;
            }
        }
        
        /// <summary>
        /// Gets all custom chip names referenced in a chip description.
        /// </summary>
        /// <param name="chipDescription">The chip description to analyze</param>
        /// <param name="chipLibrary">The chip library to check against</param>
        /// <returns>List of custom chip names</returns>
        public static List<string> GetReferencedCustomChips(ChipDescription chipDescription, ChipLibrary chipLibrary)
        {
            var customChips = new List<string>();
            
            if (chipDescription?.SubChips == null)
                return customChips;
                
            foreach (var subChip in chipDescription.SubChips)
            {
                if (IsCustomChip(subChip.Name, chipLibrary))
                {
                    customChips.Add(subChip.Name);
                }
            }
            
            return customChips;
        }
        
        /// <summary>
        /// Recursively gets all custom chip names referenced in a chip and its sub-chips.
        /// </summary>
        /// <param name="chipDescription">The chip description to analyze</param>
        /// <param name="chipLibrary">The chip library to check against</param>
        /// <returns>Set of all custom chip names (including nested references)</returns>
        public static HashSet<string> GetAllReferencedCustomChips(ChipDescription chipDescription, ChipLibrary chipLibrary)
        {
            var allCustomChips = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            CollectCustomChipsRecursive(chipDescription, chipLibrary, allCustomChips, visited);
            
            return allCustomChips;
        }
        
        private static void CollectCustomChipsRecursive(
            ChipDescription chipDescription, 
            ChipLibrary chipLibrary, 
            HashSet<string> allCustomChips, 
            HashSet<string> visited)
        {
            if (chipDescription?.SubChips == null || visited.Contains(chipDescription.Name))
                return;
                
            visited.Add(chipDescription.Name);
            
            foreach (var subChip in chipDescription.SubChips)
            {
                if (IsCustomChip(subChip.Name, chipLibrary))
                {
                    allCustomChips.Add(subChip.Name);
                    
                    // Recursively check sub-chip if it's available in the library
                    if (chipLibrary.TryGetChipDescription(subChip.Name, out ChipDescription subChipDescription))
                    {
                        CollectCustomChipsRecursive(subChipDescription, chipLibrary, allCustomChips, visited);
                    }
                }
            }
        }
    }
}
