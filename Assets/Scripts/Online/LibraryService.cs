using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Auth;
using Firebase.Firestore;
using DLS.Description;
using DLS.Levels;
using DLS.SaveSystem;
using Newtonsoft.Json;

namespace DLS.Online
{
	/// <summary>
	/// Handles uploading projects to and downloading from the shared Library in Firestore.
	/// Each Library entry stores full project data (description, chips, level progress) as JSON.
	/// </summary>
	public static class LibraryService
	{
		private const string COLLECTION_NAME = "library";

		/// <summary>Firestore document limit is 1 MB. We stay under 900 KB to leave room for metadata.</summary>
		private const int MaxStoredSizeBytes = 900_000;

		/// <summary>Dummy entries for My Projects UI testing in Editor (no Firebase).</summary>
		static List<LibraryEntry> GetDummyEntriesForEditor(LibraryFilterMode filterMode)
		{
			var entries = new List<LibraryEntry>
			{
				new LibraryEntry { id = "dummy-1", projectName = "TEST", projectDisplayName = "Test Project", displayName = "TestUser", isPublic = false, downloadCount = 5, createdAt = "2026-02-20T10:00:00Z" },
				new LibraryEntry { id = "dummy-2", projectName = "MyCircuit", projectDisplayName = "MyCircuit", displayName = "CircuitFan", isPublic = true, downloadCount = 12, createdAt = "2026-02-21T10:00:00Z" },
				new LibraryEntry { id = "dummy-3", projectName = "ALU Project", projectDisplayName = null, displayName = "Dev123", isPublic = false, downloadCount = 0, createdAt = "2026-02-22T10:00:00Z" },
				new LibraryEntry { id = "dummy-4", projectName = "Shared Demo", projectDisplayName = "Shared Demo", displayName = "DemoAuthor", isPublic = true, downloadCount = 28, createdAt = "2026-02-23T10:00:00Z" },
				new LibraryEntry { id = "dummy-5", projectName = "Private WIP", projectDisplayName = "Work in Progress", displayName = "WorkInProgress", isPublic = false, downloadCount = 0, createdAt = "2026-02-24T10:00:00Z" }
			};
			if (filterMode == LibraryFilterMode.Public)
				return entries.Where(e => e.isPublic).ToList();
			if (filterMode == LibraryFilterMode.Private)
				return entries.Where(e => !e.isPublic).ToList();
			return entries;
		}

		/// <summary>
		/// Gets the current user's author name for library entries. Uses account identity: UserProfile.username,
		/// then Firebase Auth DisplayName/Email, then "Guest" for anonymous. Not user-editable.
		/// </summary>
		public static async Task<string> GetCurrentUserAuthorNameAsync()
		{
#if UNITY_EDITOR
			if (UseLocalStorageInEditor) return "Guest";
#endif
			try
			{
				var profile = await UserAuthService.GetCurrentUserProfileAsync();
				if (profile != null && !string.IsNullOrEmpty(profile.username))
					return profile.username;
			}
			catch { /* fall through */ }

#if !UNITY_EDITOR
			try
			{
				var user = FirebaseAuth.DefaultInstance?.CurrentUser;
				if (user != null)
				{
					if (!string.IsNullOrEmpty(user.DisplayName)) return user.DisplayName;
					if (!string.IsNullOrEmpty(user.Email)) return user.Email;
					if (user.IsAnonymous) return "Guest";
				}
			}
			catch { /* fall through */ }
#endif
			return "Guest";
		}

		/// <summary>
		/// Use local mock storage in Editor - never touch Firebase to prevent uWS::HttpSocket::upgrade crash on PC/Windows.
		/// Test Project Sharing on device (Android/iOS) or in a built Windows executable.
		/// To test Import in Editor: DLS menu → Project Sharing → Use real Firebase in Editor (testing).
		/// </summary>
		public static bool UseLocalStorageInEditor
		{
			get
			{
#if UNITY_EDITOR
				return PlayerPrefs.GetInt("DLS.UseFirebaseInEditor", 0) == 0; // 0 = mock (default), 1 = real Firebase
#else
				return false;
#endif
			}
		}

		/// <summary>
		/// Metadata for a Library entry (for browse/list display).
		/// </summary>
		public class LibraryEntry
		{
			public string id;
			public string projectName;
			/// <summary>User-chosen label for the project (shown in browse list). Falls back to projectName if null.</summary>
			public string projectDisplayName;
			/// <summary>Author name from account (UserProfile/Firebase Auth).</summary>
			public string displayName;
			public string ownerUserId;
			public bool isPublic;
			public string createdAt;
			public string updatedAt;
			public int sizeBytes;
			/// <summary>Number of times this project has been imported by users.</summary>
			public int downloadCount;
		}

		/// <summary>Sort order for library entries.</summary>
		public enum LibrarySortOrder
		{
			Newest,
			MostDownloads,
			Alphabetical
		}

		/// <summary>Filter mode for which projects to show.</summary>
		public enum LibraryFilterMode
		{
			Public,
			Private,
			All
		}

		/// <summary>
		/// Bundled project data for Firestore storage.
		/// </summary>
		[Serializable]
		public class LibraryProjectPayload
		{
			public string projectDescriptionJson;
			public Dictionary<string, string> chips;
			public string levelsProgressJson;

			public LibraryProjectPayload()
			{
				chips = new Dictionary<string, string>();
			}
		}

		/// <summary>
		/// Packs a local project into a JSON payload for upload.
		/// </summary>
		/// <param name="includeLevelProgress">When false, level solutions are not included (Private option).</param>
		public static LibraryProjectPayload PackProject(string projectName, bool includeLevelProgress = true)
		{
			var payload = new LibraryProjectPayload();

			// Project description
			var descPath = SavePaths.GetProjectDescriptionPath(projectName);
			if (!File.Exists(descPath))
				throw new FileNotFoundException($"Project description not found: {projectName}");

			payload.projectDescriptionJson = File.ReadAllText(descPath);

			// Chips
			var projectDesc = Serializer.DeserializeProjectDescription(payload.projectDescriptionJson);
			var chipsPath = SavePaths.GetChipsPath(projectName);

			if (Directory.Exists(chipsPath) && projectDesc.AllCustomChipNames != null)
			{
				foreach (var chipName in projectDesc.AllCustomChipNames)
				{
					var chipPath = Path.Combine(chipsPath, chipName + ".json");
					if (File.Exists(chipPath))
						payload.chips[chipName] = File.ReadAllText(chipPath);
				}
			}

			// Level progress (only when user opts in)
			if (includeLevelProgress)
			{
				var levelsPath = SavePaths.GetLevelsProgressPath(projectName);
				if (File.Exists(levelsPath))
					payload.levelsProgressJson = File.ReadAllText(levelsPath);
			}

			return payload;
		}

		static byte[] CompressString(string json)
		{
			var bytes = System.Text.Encoding.UTF8.GetBytes(json);
			using (var output = new MemoryStream())
			{
				using (var gzip = new GZipStream(output, System.IO.Compression.CompressionLevel.Optimal))
					gzip.Write(bytes, 0, bytes.Length);
				return output.ToArray();
			}
		}

		static string DecompressToString(byte[] compressed)
		{
			using (var input = new MemoryStream(compressed))
			using (var gzip = new GZipStream(input, CompressionMode.Decompress))
			using (var output = new MemoryStream())
			{
				gzip.CopyTo(output);
				return System.Text.Encoding.UTF8.GetString(output.ToArray());
			}
		}

		/// <summary>
		/// Unpacks a payload and writes the project to local storage.
		/// </summary>
		/// <param name="payload">The bundled project data</param>
		/// <param name="targetProjectName">Local project name (can differ from original)</param>
		public static void UnpackProject(LibraryProjectPayload payload, string targetProjectName)
		{
			var projectPath = SavePaths.GetProjectPath(targetProjectName);
			Directory.CreateDirectory(projectPath);

			// Project description (ensure ProjectName matches target)
			var desc = Serializer.DeserializeProjectDescription(payload.projectDescriptionJson);
			desc.ProjectName = targetProjectName;
			var descJson = Serializer.SerializeProjectDescription(desc);
			File.WriteAllText(SavePaths.GetProjectDescriptionPath(targetProjectName), descJson);

			// Chips
			var chipsPath = SavePaths.GetChipsPath(targetProjectName);
			Directory.CreateDirectory(chipsPath);
			if (payload.chips != null)
			{
				foreach (var kv in payload.chips)
					File.WriteAllText(Path.Combine(chipsPath, kv.Key + ".json"), kv.Value);
			}

			// Level progress
			if (!string.IsNullOrEmpty(payload.levelsProgressJson))
			{
				var metaPath = SavePaths.GetProjectMetaPath(targetProjectName);
				Directory.CreateDirectory(metaPath);
				File.WriteAllText(SavePaths.GetLevelsProgressPath(targetProjectName), payload.levelsProgressJson);
			}
		}

		/// <summary>
		/// Finds an existing library entry for this user and project name (for sync/update).
		/// </summary>
		public static async Task<LibraryEntry> GetExistingEntryByProjectNameAsync(string projectName)
		{
#if UNITY_EDITOR
			if (UseLocalStorageInEditor) return null;
#endif
			await FirebaseBootstrap.InitializeAsync();
			if (!FirebaseBootstrap.IsInitialized) return null;
			FirebaseBootstrap.EnsureFirestoreConfigured();
			var db = FirebaseFirestore.DefaultInstance;
			if (db == null) return null;
			var userId = FirebaseBootstrap.UserId;
			if (string.IsNullOrEmpty(userId) || userId == "anon") return null;

			var query = db.Collection(COLLECTION_NAME)
				.WhereEqualTo("ownerUserId", userId)
				.WhereEqualTo("projectName", projectName)
				.Limit(1);
			var snapshot = await query.GetSnapshotAsync();
			if (snapshot.Count == 0) return null;
			return CreateEntryFromDoc(snapshot[0]);
		}

		/// <summary>
		/// Uploads or updates a local project in the Library. If user already has an entry for this project name, updates it.
		/// Author name is taken from account (UserProfile/Firebase Auth), not user input.
		/// </summary>
		/// <param name="projectDisplayName">User-chosen label shown in browse list. Falls back to projectName if null/empty.</param>
		/// <param name="includeLevels">When false, level solutions are not included in the payload (Levels: Private).</param>
		public static async Task<string> UploadProjectAsync(string projectName, string projectDisplayName, bool isPublic, bool includeLevels = true)
		{
#if UNITY_EDITOR
			if (UseLocalStorageInEditor)
			{
				Debug.Log("[Library] Editor mode with local storage - skipping Firebase upload");
				await Task.Delay(100);
				return "editor-mock-id";
			}
#endif

			await FirebaseBootstrap.InitializeAsync();
			if (!FirebaseBootstrap.IsInitialized)
				throw new InvalidOperationException("Firebase not initialized");

			FirebaseBootstrap.EnsureFirestoreConfigured();
			var db = FirebaseFirestore.DefaultInstance;
			if (db == null)
				throw new InvalidOperationException("Firestore not available");

			string displayName = await GetCurrentUserAuthorNameAsync();

			var payload = PackProject(projectName, includeLevels);
			var projectDataJson = JsonConvert.SerializeObject(payload);
			var compressed = CompressString(projectDataJson);
			var projectDataStored = Convert.ToBase64String(compressed);

			if (projectDataStored.Length > MaxStoredSizeBytes)
			{
				var originalMB = projectDataJson.Length / (1024f * 1024f);
				throw new InvalidOperationException(
					$"Project is too large to upload ({originalMB:F1} MB uncompressed). " +
					$"Firestore has a 1 MB limit. Try removing large chips (e.g. ROMs with lots of data) or splitting the project.");
			}

			var existing = await GetExistingEntryByProjectNameAsync(projectName);
			var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

			string authorName = displayName ?? "Guest";
			string projDisplayName = string.IsNullOrWhiteSpace(projectDisplayName) ? projectName : projectDisplayName.Trim();

			if (existing != null)
			{
				var updates = new Dictionary<string, object>
				{
					{ "displayName", authorName },
					{ "projectDisplayName", projDisplayName },
					{ "isPublic", isPublic },
					{ "updatedAt", now },
					{ "projectDataCompressed", true },
					{ "projectData", projectDataStored }
				};
				var docRef = db.Collection(COLLECTION_NAME).Document(existing.id);
				await docRef.UpdateAsync(updates);
				Debug.Log($"[Library] Updated project {projectName} as {existing.id}");
				return existing.id;
			}

			var data = new Dictionary<string, object>
			{
				{ "projectName", projectName },
				{ "projectDisplayName", projDisplayName },
				{ "displayName", authorName },
				{ "ownerUserId", FirebaseBootstrap.UserId },
				{ "isPublic", isPublic },
				{ "createdAt", now },
				{ "updatedAt", now },
				{ "downloadCount", 0 },
				{ "downloadedBy", new Dictionary<string, object>() },
				{ "projectDataCompressed", true },
				{ "projectData", projectDataStored }
			};

			var newDocRef = db.Collection(COLLECTION_NAME).Document();
			await newDocRef.SetAsync(data);
			Debug.Log($"[Library] Uploaded project {projectName} as {newDocRef.Id}");
			return newDocRef.Id;
		}

		/// <summary>
		/// Updates an existing library entry's project display name and visibility (owner only). Author name is refreshed from account.
		/// </summary>
		public static async Task<bool> UpdateEntryAsync(string documentId, string projectDisplayName, bool isPublic)
		{
#if UNITY_EDITOR
			if (UseLocalStorageInEditor) return false;
#endif
			await FirebaseBootstrap.InitializeAsync();
			if (!FirebaseBootstrap.IsInitialized) return false;
			var db = FirebaseFirestore.DefaultInstance;
			if (db == null) return false;

			string authorName = await GetCurrentUserAuthorNameAsync();
			string projDisplayName = string.IsNullOrWhiteSpace(projectDisplayName) ? null : projectDisplayName.Trim();

			var docRef = db.Collection(COLLECTION_NAME).Document(documentId);
			var updates = new Dictionary<string, object>
			{
				{ "displayName", authorName ?? "Guest" },
				{ "projectDisplayName", projDisplayName ?? "" },
				{ "isPublic", isPublic },
				{ "updatedAt", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") }
			};
			await docRef.UpdateAsync(updates);
			Debug.Log($"[Library] Updated entry {documentId}");
			return true;
		}

		/// <summary>
		/// Updates the displayName (author) for all library entries owned by the given user.
		/// Called by UserAuthService when username changes.
		/// </summary>
		public static async Task UpdateDisplayNameForOwnerAsync(string ownerUserId, string newDisplayName)
		{
#if UNITY_EDITOR
			if (UseLocalStorageInEditor) return;
#endif
			if (string.IsNullOrEmpty(ownerUserId) || string.IsNullOrEmpty(newDisplayName)) return;
			try
			{
				await FirebaseBootstrap.InitializeAsync();
				if (!FirebaseBootstrap.IsInitialized) return;
				FirebaseBootstrap.EnsureFirestoreConfigured();
				var db = FirebaseFirestore.DefaultInstance;
				if (db == null) return;

				var query = db.Collection(COLLECTION_NAME).WhereEqualTo("ownerUserId", ownerUserId);
				var snapshot = await query.GetSnapshotAsync();
				foreach (var doc in snapshot.Documents)
				{
					await doc.Reference.UpdateAsync(new Dictionary<string, object> { { "displayName", newDisplayName } });
				}
				Debug.Log($"[Library] Updated displayName to '{newDisplayName}' for {snapshot.Count} entries owned by {ownerUserId}");
			}
			catch (Exception ex)
			{
				Debug.LogError($"[Library] Failed to update displayName for owner: {ex.Message}");
			}
		}

		/// <summary>
		/// Deletes a library entry (owner only).
		/// </summary>
		public static async Task<bool> DeleteEntryAsync(string documentId)
		{
#if UNITY_EDITOR
			if (UseLocalStorageInEditor) return false;
#endif
			await FirebaseBootstrap.InitializeAsync();
			if (!FirebaseBootstrap.IsInitialized) return false;
			FirebaseBootstrap.EnsureFirestoreConfigured();
			var db = FirebaseFirestore.DefaultInstance;
			if (db == null) return false;

			var docRef = db.Collection(COLLECTION_NAME).Document(documentId);
			await docRef.DeleteAsync();
			Debug.Log($"[Library] Deleted entry {documentId}");
			return true;
		}

		/// <summary>
		/// Fetches public Library entries for browsing.
		/// </summary>
		public static async Task<List<LibraryEntry>> GetPublicEntriesAsync(int limit = 50)
		{
#if UNITY_EDITOR
			if (UseLocalStorageInEditor)
			{
				Debug.Log("[Library] Editor mode with local storage - returning empty list");
				await Task.Delay(100);
				return new List<LibraryEntry>();
			}
#endif

			await FirebaseBootstrap.InitializeAsync();
			if (!FirebaseBootstrap.IsInitialized)
				throw new InvalidOperationException("Firebase not initialized");

			FirebaseBootstrap.EnsureFirestoreConfigured();
			var db = FirebaseFirestore.DefaultInstance;
			if (db == null)
				throw new InvalidOperationException("Firestore not available");

			var query = db.Collection(COLLECTION_NAME)
				.WhereEqualTo("isPublic", true)
				.OrderByDescending("createdAt")
				.Limit(limit);

			var snapshot = await query.GetSnapshotAsync();
			var entries = new List<LibraryEntry>();

			foreach (var doc in snapshot.Documents)
				entries.Add(CreateEntryFromDoc(doc));

			return entries;
		}

		/// <summary>
		/// Fetches Library entries with optional filters. Use this for the Import UI.
		/// </summary>
		/// <param name="filterMode">Public = public projects only; Private = my projects only; All = both merged.</param>
		/// <param name="sortOrder">How to order results.</param>
		/// <param name="limit">Max number of entries.</param>
		public static async Task<List<LibraryEntry>> GetEntriesAsync(LibraryFilterMode filterMode, LibrarySortOrder sortOrder, int limit = 50)
		{
#if UNITY_EDITOR
			if (UseLocalStorageInEditor)
			{
				await Task.Delay(100);
				var entries = GetDummyEntriesForEditor(filterMode);
				SortEntries(entries, sortOrder);
				return entries;
			}
#endif

			await FirebaseBootstrap.InitializeAsync();
			if (!FirebaseBootstrap.IsInitialized)
				throw new InvalidOperationException("Firebase not initialized");

			FirebaseBootstrap.EnsureFirestoreConfigured();
			var db = FirebaseFirestore.DefaultInstance;
			if (db == null)
				throw new InvalidOperationException("Firestore not available");

			if (filterMode == LibraryFilterMode.All)
			{
				var publicTask = FetchQueryAsync(db.Collection(COLLECTION_NAME).WhereEqualTo("isPublic", true), sortOrder, limit);
				var mineTask = FetchMineQueryAsync(db, sortOrder, limit);
				await Task.WhenAll(publicTask, mineTask);
				var publicEntries = await publicTask;
				var mineEntries = await mineTask;
				var seen = new HashSet<string>();
				var merged = new List<LibraryEntry>();
				foreach (var e in publicEntries)
				{
					if (seen.Add(e.id)) merged.Add(e);
				}
				foreach (var e in mineEntries)
				{
					if (seen.Add(e.id)) merged.Add(e);
				}
				SortEntries(merged, sortOrder);
				return merged.Take(limit).ToList();
			}

			Query query;
			if (filterMode == LibraryFilterMode.Private)
			{
				var userId = FirebaseBootstrap.UserId;
				if (string.IsNullOrEmpty(userId) || userId == "anon")
					return new List<LibraryEntry>();
				query = db.Collection(COLLECTION_NAME)
					.WhereEqualTo("ownerUserId", userId);
			}
			else
			{
				query = db.Collection(COLLECTION_NAME)
					.WhereEqualTo("isPublic", true);
			}

			return await FetchQueryAsync(query, sortOrder, limit);
		}

		/// <summary>
		/// Fetches entries without server-side OrderBy (avoids composite index requirement).
		/// Sorts in memory by sortOrder. FetchLimit should be >= limit to get correct results.
		/// </summary>
		static async Task<List<LibraryEntry>> FetchQueryAsync(Query baseQuery, LibrarySortOrder sortOrder, int limit, int fetchLimit = 100)
		{
			var query = baseQuery.Limit(fetchLimit);
			var snapshot = await query.GetSnapshotAsync();
			var entries = new List<LibraryEntry>();
			foreach (var doc in snapshot.Documents)
				entries.Add(CreateEntryFromDoc(doc));
			SortEntries(entries, sortOrder);
			return entries.Take(limit).ToList();
		}

		static async Task<List<LibraryEntry>> FetchMineQueryAsync(FirebaseFirestore db, LibrarySortOrder sortOrder, int limit)
		{
			var userId = FirebaseBootstrap.UserId;
			if (string.IsNullOrEmpty(userId) || userId == "anon")
				return new List<LibraryEntry>();
			var query = db.Collection(COLLECTION_NAME).WhereEqualTo("ownerUserId", userId);
			return await FetchQueryAsync(query, sortOrder, limit);
		}

		static void SortEntries(List<LibraryEntry> entries, LibrarySortOrder sortOrder)
		{
			if (sortOrder == LibrarySortOrder.MostDownloads)
				entries.Sort((a, b) => b.downloadCount.CompareTo(a.downloadCount));
			else if (sortOrder == LibrarySortOrder.Alphabetical)
				entries.Sort((a, b) => string.CompareOrdinal(GetDisplayTitle(a), GetDisplayTitle(b)));
			else
				entries.Sort((a, b) => string.CompareOrdinal(b.createdAt ?? "", a.createdAt ?? ""));
		}

		static string GetDisplayTitle(LibraryEntry e) => !string.IsNullOrEmpty(e.projectDisplayName) ? e.projectDisplayName : e.projectName ?? "";

		/// <summary>
		/// Fetches a single Library entry by ID and returns the full payload for import.
		/// </summary>
		public static async Task<(LibraryEntry entry, LibraryProjectPayload payload)> GetProjectAsync(string projectId)
		{
#if UNITY_EDITOR
			if (UseLocalStorageInEditor)
			{
				Debug.Log("[Library] Editor mode with local storage - cannot fetch project");
				await Task.Delay(100);
				return (null, null);
			}
#endif

			await FirebaseBootstrap.InitializeAsync();
			if (!FirebaseBootstrap.IsInitialized)
				throw new InvalidOperationException("Firebase not initialized");

			FirebaseBootstrap.EnsureFirestoreConfigured();
			var db = FirebaseFirestore.DefaultInstance;
			if (db == null)
				throw new InvalidOperationException("Firestore not available");

			var docRef = db.Collection(COLLECTION_NAME).Document(projectId);
			var snapshot = await docRef.GetSnapshotAsync();

			if (!snapshot.Exists)
				return (null, null);

			var data = snapshot.ToDictionary();

			// Check read access: public or owner
			var isPublic = data.TryGetValue("isPublic", out var pub) && pub is bool pb && pb;
			var ownerUserId = GetString(data, "ownerUserId");
			var currentUserId = FirebaseBootstrap.UserId;

			if (!isPublic && ownerUserId != currentUserId)
			{
				Debug.LogWarning("[Library] Access denied: project is private and user is not owner");
				return (null, null);
			}

			var entry = CreateEntryFromDoc(snapshot);
			entry.sizeBytes = 0;

			var projectDataStored = GetString(data, "projectData");
			if (string.IsNullOrEmpty(projectDataStored))
				return (entry, null);

			LibraryProjectPayload payload;
			var isCompressed = data.TryGetValue("projectDataCompressed", out var comp) && comp is bool c && c;
			if (isCompressed)
			{
				try
				{
					var compressed = Convert.FromBase64String(projectDataStored);
					var projectDataJson = DecompressToString(compressed);
					entry.sizeBytes = projectDataJson.Length;
					payload = JsonConvert.DeserializeObject<LibraryProjectPayload>(projectDataJson);
				}
				catch (Exception ex)
				{
					Debug.LogWarning($"[Library] Failed to decompress project data: {ex.Message}");
					return (entry, null);
				}
			}
			else
			{
				entry.sizeBytes = projectDataStored.Length;
				payload = JsonConvert.DeserializeObject<LibraryProjectPayload>(projectDataStored);
			}
			return (entry, payload);
		}

		/// <summary>
		/// Imports a project from the Library into local storage.
		/// </summary>
		/// <param name="projectId">Library document ID</param>
		/// <param name="localProjectName">Name for the local project (defaults to original if null)</param>
		public static async Task<bool> ImportProjectAsync(string projectId, string localProjectName = null)
		{
			var (entry, payload) = await GetProjectAsync(projectId);
			if (entry == null || payload == null)
				return false;

			var targetName = localProjectName ?? entry.projectName;
			if (string.IsNullOrEmpty(targetName))
				targetName = "ImportedProject";

			// Resolve name conflicts (append number if exists)
			var finalName = targetName;
			var counter = 1;
			while (Loader.ProjectExists(finalName))
			{
				finalName = $"{targetName}_{counter}";
				counter++;
			}

			UnpackProject(payload, finalName);
			_ = RecordUniqueDownloadAsync(projectId);
			Debug.Log($"[Library] Imported project as {finalName}");
			return true;
		}

		/// <summary>
		/// Records a download only if this user hasn't downloaded this project before.
		/// Keeps downloadCount as the number of unique users, preventing abuse.
		/// </summary>
		static async Task RecordUniqueDownloadAsync(string projectId)
		{
#if UNITY_EDITOR
			if (UseLocalStorageInEditor) return;
#endif
			var userId = FirebaseBootstrap.UserId;
			if (string.IsNullOrEmpty(userId)) return;

			try
			{
				FirebaseBootstrap.EnsureFirestoreConfigured();
				var db = FirebaseFirestore.DefaultInstance;
				if (db == null) return;
				var docRef = db.Collection(COLLECTION_NAME).Document(projectId);

				await db.RunTransactionAsync(async (transaction) =>
				{
					var snapshot = await transaction.GetSnapshotAsync(docRef);
					if (!snapshot.Exists) return;

					var data = snapshot.ToDictionary();
					var downloadedBy = new Dictionary<string, object>();
					if (data.TryGetValue("downloadedBy", out var dB) && dB is IDictionary<string, object> existing)
					{
						foreach (var kv in existing)
							downloadedBy[kv.Key] = kv.Value;
					}

					if (downloadedBy.ContainsKey(userId))
						return; // Already counted

					downloadedBy[userId] = true;
					var currentCount = GetInt(data, "downloadCount");
					var updates = new Dictionary<string, object>
					{
						{ "downloadedBy", downloadedBy },
						{ "downloadCount", currentCount + 1 }
					};
					transaction.Update(docRef, updates);
				});
			}
			catch (Exception ex)
			{
				Debug.LogWarning($"[Library] Failed to record unique download: {ex.Message}");
			}
		}

		private static string GetString(Dictionary<string, object> data, string key)
		{
			return data.TryGetValue(key, out var v) && v != null ? v.ToString() : null;
		}

		private static int GetInt(Dictionary<string, object> data, string key)
		{
			if (!data.TryGetValue(key, out var v) || v == null) return 0;
			if (v is long l) return (int)l;
			if (v is int i) return i;
			return int.TryParse(v.ToString(), out var parsed) ? parsed : 0;
		}

		static LibraryEntry CreateEntryFromDoc(DocumentSnapshot doc)
		{
			var data = doc.ToDictionary();
			return new LibraryEntry
			{
				id = doc.Id,
				projectName = GetString(data, "projectName"),
				projectDisplayName = GetString(data, "projectDisplayName"),
				displayName = GetString(data, "displayName"),
				ownerUserId = GetString(data, "ownerUserId"),
				isPublic = data.TryGetValue("isPublic", out var pub) && pub is bool b && b,
				createdAt = GetString(data, "createdAt"),
				updatedAt = GetString(data, "updatedAt"),
				sizeBytes = data.TryGetValue("projectData", out var pd) && pd is string s ? s.Length : 0,
				downloadCount = GetInt(data, "downloadCount")
			};
		}
	}
}
