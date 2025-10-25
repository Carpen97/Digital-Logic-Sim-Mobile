using System;
using System.Collections.Generic;
using UnityEngine;

namespace DLS.Graphics
{
	[Serializable]
	public class PatchNotesData
	{
		public List<PatchVersion> versions;
	}

	[Serializable]
	public class PatchVersion
	{
		public string version;
		public string releaseDate;
		public PatchSections sections;
	}

	[Serializable]
	public class PatchSections
	{
		public List<string> newFeatures;
		public List<string> improvements;
		public List<string> bugFixes;
	}

	// Editor format item (with metadata)
	[Serializable]
	public class PatchNoteItem
	{
		public string text;
		public bool userFacing = true;
		public string editNotes = "";
	}

	// Editor format sections
	[Serializable]
	public class PatchSectionsEditor
	{
		public List<PatchNoteItem> newFeatures;
		public List<PatchNoteItem> improvements;
		public List<PatchNoteItem> bugFixes;
	}

	// Editor format version
	[Serializable]
	public class PatchVersionEditor
	{
		public string version;
		public string releaseDate;
		public List<PatchNoteItem> newFeatures;
		public List<PatchNoteItem> improvements;
		public List<PatchNoteItem> bugFixes;
	}

	// Editor format data
	[Serializable]
	public class PatchNotesDataEditor
	{
		public List<PatchVersionEditor> versions;
	}

	public static class PatchNotesLoader
	{
		private static PatchNotesData _cachedData;
		private static bool _hasLoaded = false;

		// Debug method to force reload (remove after testing)
		public static void ForceReload()
		{
			_cachedData = null;
			_hasLoaded = false;
			Debug.Log("[PatchNotesLoader] Cache cleared - will reload from disk");
		}

		public static PatchNotesData LoadPatchNotes()
		{
			if (_hasLoaded && _cachedData != null)
			{
				return _cachedData;
			}

			try
			{
				var patchNotesAsset = Resources.Load<TextAsset>("patchNotes");
				if (patchNotesAsset == null)
				{
					Debug.LogError("[PatchNotesLoader] Could not load patchNotes.json from Resources folder");
					return CreateFallbackData();
				}

				// Try to parse as editor format first
				var editorData = JsonUtility.FromJson<PatchNotesDataEditor>(patchNotesAsset.text);
				if (editorData?.versions != null && editorData.versions.Count > 0)
				{
					// Convert editor format to game format
					_cachedData = ConvertEditorFormatToGameFormat(editorData);
					_hasLoaded = true;
					Debug.Log($"[PatchNotesLoader] Successfully loaded {_cachedData.versions?.Count ?? 0} patch note versions (editor format)");
					return _cachedData;
				}

				// Try to parse as game format
				_cachedData = JsonUtility.FromJson<PatchNotesData>(patchNotesAsset.text);
				if (_cachedData?.versions != null)
				{
					_hasLoaded = true;
					Debug.Log($"[PatchNotesLoader] Successfully loaded {_cachedData.versions?.Count ?? 0} patch note versions (game format)");
					return _cachedData;
				}

				Debug.LogError("[PatchNotesLoader] Could not parse patch notes in either format");
				return CreateFallbackData();
			}
			catch (Exception ex)
			{
				Debug.LogError($"[PatchNotesLoader] Failed to parse patch notes JSON: {ex.Message}");
				return CreateFallbackData();
			}
		}

		public static PatchVersion GetLatestVersion()
		{
			var data = LoadPatchNotes();
			if (data?.versions != null && data.versions.Count > 0)
			{
				return data.versions[0]; // Versions should be ordered with latest first
			}
			return null;
		}

		public static PatchVersion GetVersion(string versionString)
		{
			var data = LoadPatchNotes();
			if (data?.versions != null)
			{
				foreach (var version in data.versions)
				{
					if (version.version == versionString)
					{
						return version;
					}
				}
			}
			return null;
		}

		public static List<string> GetAvailableVersions()
		{
			var data = LoadPatchNotes();
			if (data?.versions != null)
			{
				var versions = new List<string>();
				foreach (var version in data.versions)
				{
					versions.Add(version.version);
				}
				return versions;
			}
			return new List<string>();
		}

		private static PatchNotesData ConvertEditorFormatToGameFormat(PatchNotesDataEditor editorData)
		{
			var gameData = new PatchNotesData
			{
				versions = new List<PatchVersion>()
			};

			foreach (var editorVersion in editorData.versions)
			{
				var gameVersion = new PatchVersion
				{
					version = editorVersion.version,
					releaseDate = editorVersion.releaseDate,
					sections = new PatchSections
					{
						newFeatures = ExtractUserFacingTexts(editorVersion.newFeatures),
						improvements = ExtractUserFacingTexts(editorVersion.improvements),
						bugFixes = ExtractUserFacingTexts(editorVersion.bugFixes)
					}
				};
				gameData.versions.Add(gameVersion);
			}

			return gameData;
		}

		private static List<string> ExtractUserFacingTexts(List<PatchNoteItem> items)
		{
			var texts = new List<string>();
			if (items != null)
			{
				foreach (var item in items)
				{
					if (item.userFacing && !string.IsNullOrEmpty(item.text))
					{
						texts.Add(item.text);
					}
				}
			}
			return texts;
		}

		private static PatchNotesData CreateFallbackData()
		{
			Debug.LogWarning("[PatchNotesLoader] Creating fallback patch notes data");
			_hasLoaded = true;
			
			return new PatchNotesData
			{
				versions = new List<PatchVersion>
				{
					new PatchVersion
					{
						version = "2.1.6.9",
						releaseDate = "October 9, 2025",
						sections = new PatchSections
						{
							newFeatures = new List<string>
							{
								"Drag and Drop Control Scheme - New intuitive chip placement mode. Simply drag chips from the bottom bar and drop them directly onto the canvas. Toggle between classic 'Drag and Lock' and new 'Drag and Drop' modes in preferences.",
								"Hierarchical Collection Organization - Create and manage sub folders within collections for better chip organization.",
								"PC Version - Full mobile features now available on PC with mouse and keyboard support.",
								"Solution Sharing - Upload and view complete solutions from leaderboard entries.",
								"User Names - Add custom names when uploading scores to leaderboards.",
								"iOS Platform Support - Full iOS support with project import/export and Firebase integration."
							},
							improvements = new List<string>
							{
								"PC Firebase Integration - Full Firebase functionality now works on PC builds (Windows, macOS, Linux). PC users can now upload scores, view leaderboards, share solutions, and set user names just like mobile users. All online features are now available across all platforms.",
								"Enhanced Level System - Expanded with more challenging levels and progressive difficulty.",
								"Improved UI Navigation - Better folder browsing and collection management.",
								"Clearer Score Explanation - Updated scoring information to better explain how nested NAND gates are counted.",
								"Auto-Open Edit Tool - Single component selected + wrench press now automatically opens the edit menu.",
								"Selectable Chapters in Levels Menu - Chapters now show educational descriptions when selected."
							},
							bugFixes = new List<string>
							{
								"Fixed iOS file picker for importing project zip files. Various stability improvements and performance optimizations."
							}
						}
					}
				}
			};
		}
	}
}
