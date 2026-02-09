using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DLS.Levels
{
	[Serializable]
	public sealed class LocalLevelPack
	{
		public int schemaVersion = 1;
		public string packId;
		public string packName;
		public string packDescription;
		public Chapter[] chapters;
	}

	[Serializable]
	public sealed class Chapter
	{
        // id + friendly metadata
		public string chapterId;
		public string chapterName;
		public string chapterDescription;

        // V1 format: Original LevelDefinitions
		public List<LevelDefinition> levels;

        // V2 format: New cleaner format
		public List<LevelDefinitionV2> levelsV2;

		/// <summary>
		/// Get all levels in V1 format (converting V2 if necessary).
		/// This allows the rest of the system to work without changes.
		/// </summary>
		public List<LevelDefinition> GetAllLevelsAsV1()
		{
			var result = new List<LevelDefinition>();

			// Add V1 levels
			if (levels != null)
			{
				result.AddRange(levels);
			}

			// Convert and add V2 levels
			if (levelsV2 != null)
			{
				foreach (var v2Level in levelsV2)
				{
					try
					{
						// Validate V2 level first
						if (!v2Level.Validate(out string error))
						{
							Debug.LogError($"[Chapter] Invalid V2 level '{v2Level.id}': {error}");
							continue;
						}

						// Convert to V1
						var v1Level = v2Level.ToV1();
						result.Add(v1Level);
						Debug.Log($"[Chapter] Converted V2 level '{v2Level.id}' to V1 format");
					}
					catch (Exception ex)
					{
						Debug.LogError($"[Chapter] Failed to convert V2 level '{v2Level.id}': {ex.Message}");
					}
				}
			}

			return result;
		}
	}
}
