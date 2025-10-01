using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using DLS.Game;        // Project.ActiveProject
using DLS.SaveSystem;  // SavePaths helpers
using DLS.Description; // ChipDescription, Serializer

namespace DLS.Levels
{
		/// <summary>
		/// Persists per-level progress:
		/// - completed (ever passed)
		/// - best stars
		/// - best NAND gate count (lowest)
		/// - completedAt (ISO, optional)
		/// - progressState (serialized chip state for work-in-progress)
		/// Safe to call from anywhere; loads on first use.
		/// </summary>
	public static class LevelProgressService
	{
		// ---- Schema ----
		[Serializable]
		class DB
		{
			public int version = CurrentVersion;
			public List<Entry> levels = new();
		}

	[Serializable]
	class Entry
	{
		public string id;
		public bool completed;
		public int stars;
		public int bestParts = -1;      // -1 ⇒ unknown (now represents NAND gate count)
		public string completedAt = ""; // ISO-8601 UTC
		public string progressState = ""; // Serialized chip state for level progress
	}

		/// <summary>Immutable snapshot returned to callers.</summary>
		public struct Snapshot
		{
			public bool Completed;
			public int Stars;
			public int BestParts; // -1 if none (now represents NAND gate count)
		}

		public static event Action OnProgressChanged;

		// ---- Runtime state ----
		const int CurrentVersion = 1;
		static Dictionary<string, Entry> _map = new Dictionary<string, Entry>();
		static bool _loaded;
		static string _loadedForProjectName;   // track which project the in-memory map belongs to
		static string CurrentProjectName => Project.ActiveProject?.description.ProjectName ?? "_NoProject";
		static string FilePath => SavePaths.GetLevelsProgressPath(CurrentProjectName);

		// ---- Public API ----

		public static Snapshot Get(string id)
		{
			EnsureLoaded();
			if (string.IsNullOrEmpty(id) || !_map.TryGetValue(id, out var e))
				return default;
			return new Snapshot
			{
				Completed = e.completed,
				Stars = e.stars,
				BestParts = e.bestParts
			};
		}

		/// <summary>
		/// Update progress for a level based on a validation run.
		/// Non-regressive: stars only increase; best NAND gate count only improves (lower).
		/// </summary>
		public static void MarkResult(string id, int stars, int lastPartsCount, bool passed)
		{
			EnsureLoaded();
			if (string.IsNullOrEmpty(id)) return;

			if (!_map.TryGetValue(id, out var e))
			{
				e = new Entry { id = id };
				_map[id] = e;
			}

			bool changed = false;

			if (passed)
			{
				if (!e.completed) { e.completed = true; changed = true; }
				if (stars > e.stars) { e.stars = stars; changed = true; }

				// Improve best NAND gate count only if we have a valid count (>= 0)
				if (lastPartsCount >= 0 && (e.bestParts < 0 || lastPartsCount < e.bestParts))
				{
					e.bestParts = lastPartsCount;
					changed = true;
				}

				e.completedAt = DateTime.UtcNow.ToString("o");
			}
			// On fail we keep the best so far; no regression.

			if (changed)
			{
				SaveNow();
				OnProgressChanged?.Invoke();
			}
		}

		public static void Reset(string id)
		{
			EnsureLoaded();
			if (string.IsNullOrEmpty(id)) return;
			if (_map.Remove(id))
			{
				SaveNow();
				OnProgressChanged?.Invoke();
			}
		}

		public static void ResetAll()
		{
			EnsureLoaded();
			if (_map.Count == 0) return;
			_map.Clear();
			SaveNow();
			OnProgressChanged?.Invoke();
		}

		/// <summary>
		/// Save the current level progress state (chip placements, connections, etc.)
		/// </summary>
		public static void SaveLevelProgress(string levelId, DevChipInstance currentChip)
		{
			if (string.IsNullOrEmpty(levelId) || currentChip == null) return;

			EnsureLoaded();
			
			try
			{
				// Create chip description from current state
				var chipDescription = DescriptionCreator.CreateChipDescription(currentChip);
				
				// Serialize to JSON
				var progressStateJson = Serializer.SerializeChipDescription(chipDescription);
				
				// Get or create entry
				if (!_map.TryGetValue(levelId, out var entry))
				{
					entry = new Entry { id = levelId };
					_map[levelId] = entry;
				}
				
				// Update progress state
				entry.progressState = progressStateJson;
				
				// Save to file
				SaveNow();
				
				Debug.Log($"[LevelProgressService] Saved progress state for level {levelId}");
			}
			catch (Exception ex)
			{
				Debug.LogError($"[LevelProgressService] Failed to save level progress: {ex.Message}");
			}
		}

		/// <summary>
		/// Load the saved level progress state for a level
		/// </summary>
		public static ChipDescription LoadLevelProgress(string levelId)
		{
			if (string.IsNullOrEmpty(levelId)) return null;

			EnsureLoaded();
			
			if (!_map.TryGetValue(levelId, out var entry) || string.IsNullOrEmpty(entry.progressState))
			{
				return null;
			}
			
			try
			{
				// Deserialize from JSON
				var chipDescription = Serializer.DeserializeChipDescription(entry.progressState);
				Debug.Log($"[LevelProgressService] Loaded progress state for level {levelId}");
				return chipDescription;
			}
			catch (Exception ex)
			{
				Debug.LogError($"[LevelProgressService] Failed to load level progress: {ex.Message}");
				return null;
			}
		}

		/// <summary>
		/// Check if a level has saved progress state
		/// </summary>
		public static bool HasLevelProgress(string levelId)
		{
			if (string.IsNullOrEmpty(levelId)) return false;
			
			EnsureLoaded();
			return _map.TryGetValue(levelId, out var entry) && !string.IsNullOrEmpty(entry.progressState);
		}

		/// <summary>
		/// Clear the progress state for a level (but keep completion stats)
		/// </summary>
		public static void ClearLevelProgress(string levelId)
		{
			if (string.IsNullOrEmpty(levelId)) return;
			
			EnsureLoaded();
			if (_map.TryGetValue(levelId, out var entry))
			{
				entry.progressState = "";
				SaveNow();
				Debug.Log($"[LevelProgressService] Cleared progress state for level {levelId}");
			}
		}

		/// <summary>
		/// Force reload the progress cache from disk (useful when file is modified externally)
		/// </summary>
		public static void ForceReloadCache()
		{
			_loaded = false;
			_loadedForProjectName = null;
			_map.Clear();
			Debug.Log("[LevelProgressService] Forced cache reload");
		}

		// ---- IO ----

		static void EnsureLoaded()
		{


			if (_loaded && _loadedForProjectName == CurrentProjectName)
				return;

			_map = new Dictionary<string, Entry>();
			_loaded = true;
			_loadedForProjectName = CurrentProjectName;

			try
			{
				if (!File.Exists(FilePath))
				{
					SaveNow(); // create empty file
					return;
				}

				string json = File.ReadAllText(FilePath);
				if (string.IsNullOrWhiteSpace(json))
				{
					SaveNow();
					return;
				}

				var db = JsonUtility.FromJson<DB>(json) ?? new DB();
				if (db.levels != null)
				{
					_map.Clear();
					foreach (var e in db.levels)
					{
						if (e != null && !string.IsNullOrEmpty(e.id))
							_map[e.id] = e;
					}
				}
			}
			catch (Exception ex)
			{
				Debug.LogWarning($"[LevelProgressService] Load failed: {ex.Message}\nStarting fresh.");
				_map.Clear();
				SaveNow();
			}
		}

		static void SaveNow()
		{
			try
			{
				var db = new DB { version = CurrentVersion, levels = new List<Entry>(_map.Values) };
				string json = JsonUtility.ToJson(db, prettyPrint: true);
				Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
				File.WriteAllText(FilePath, json);
			}
			catch (Exception ex)
			{
				Debug.LogWarning($"[LevelProgressService] Save failed: {ex.Message}");
			}
		}
	}
}
