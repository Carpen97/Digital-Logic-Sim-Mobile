using System;
using System.Collections.Generic;

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

        // slim LevelDefinitions
		public List<LevelDefinition> levels;
	}
}
