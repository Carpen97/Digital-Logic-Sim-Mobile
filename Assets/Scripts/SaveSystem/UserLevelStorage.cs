using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DLS.Description;
using DLS.Levels;
using Newtonsoft.Json;
using UnityEngine;

namespace DLS.SaveSystem
{
	/// <summary>Stores and loads user-created levels (My levels) per project.</summary>
	public static class UserLevelStorage
	{
		public static void SaveUserLevel(LevelDefinition level, string projectName)
		{
			string dir = SavePaths.GetUserLevelsPath(projectName);
			SavePaths.EnsureDirectoryExists(dir);
			string path = SavePaths.GetUserLevelFilePath(projectName, level.id ?? level.name);
			string json = JsonConvert.SerializeObject(level, Formatting.Indented);
			File.WriteAllText(path, json);
		}

		public static LevelDefinition LoadUserLevel(string projectName, string levelId)
		{
			string path = SavePaths.GetUserLevelFilePath(projectName, levelId);
			if (!File.Exists(path)) return null;
			string json = File.ReadAllText(path);
			return JsonConvert.DeserializeObject<LevelDefinition>(json);
		}

		public static List<string> ListUserLevelIds(string projectName)
		{
			string dir = SavePaths.GetUserLevelsPath(projectName);
			if (!Directory.Exists(dir)) return new List<string>();
			return Directory.GetFiles(dir, "*.json")
				.Select(p => Path.GetFileNameWithoutExtension(p))
				.ToList();
		}

		public static bool UserLevelExists(string projectName, string levelName)
		{
			string path = SavePaths.GetUserLevelFilePath(projectName, levelName);
			return File.Exists(path);
		}

		/// <summary>Delete a user-created level. Returns true if deleted, false if not found or error.</summary>
		public static bool DeleteUserLevel(string projectName, string levelId)
		{
			string path = SavePaths.GetUserLevelFilePath(projectName, levelId);
			if (!File.Exists(path)) return false;
			try
			{
				File.Delete(path);
				return true;
			}
			catch (Exception ex)
			{
				Debug.LogError($"[UserLevelStorage] Failed to delete level {levelId}: {ex.Message}");
				return false;
			}
		}
	}
}
