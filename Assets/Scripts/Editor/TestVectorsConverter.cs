using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using DLS.Levels;

namespace DLS.Editor
{
	/// <summary>
	/// Editor utility to convert test vectors from JSON to binary format.
	/// This tool helps migrate large test vector arrays to external binary files.
	/// </summary>
	public class TestVectorsConverter : EditorWindow
	{
		private const string LEVELS_JSON_PATH = "Assets/Resources/levels.json";
		private const string TESTVECTORS_DIR = "Assets/Resources/testvectors";
		private const long SIZE_THRESHOLD = 1000; // Convert levels with 1000+ bytes of test data

		private string statusMessage = "";
		private Vector2 scrollPosition;

		[MenuItem("Tools/Digital Logic Sim/Convert Test Vectors to Binary")]
		public static void ShowWindow()
		{
			var window = GetWindow<TestVectorsConverter>("Test Vectors Converter");
			window.minSize = new Vector2(600, 400);
			window.Show();
		}

		void OnGUI()
		{
			GUILayout.Label("Test Vectors Binary Converter", EditorStyles.boldLabel);
			GUILayout.Space(10);

			EditorGUILayout.HelpBox(
				"This tool converts large test vector arrays in levels.json to compact binary files.\n\n" +
				"Benefits:\n" +
				"• Reduces file size by 90-95%\n" +
				"• Keeps levels.json readable\n" +
				"• Faster loading times\n\n" +
				"The original levels.json will be backed up before modification.",
				MessageType.Info
			);

			GUILayout.Space(10);

			if (GUILayout.Button("Analyze levels.json", GUILayout.Height(30)))
			{
				AnalyzeLevels();
			}

			GUILayout.Space(5);

			if (GUILayout.Button("Convert Large Levels to Binary", GUILayout.Height(30)))
			{
				ConvertLargeLevels();
			}

			GUILayout.Space(5);

			if (GUILayout.Button("Convert ALL Levels to Binary", GUILayout.Height(30)))
			{
				if (EditorUtility.DisplayDialog("Convert All Levels",
					"This will convert ALL levels to binary format. Are you sure?",
					"Yes, Convert All", "Cancel"))
				{
					ConvertAllLevels();
				}
			}

			GUILayout.Space(10);

			// Status message area
			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
			EditorGUILayout.TextArea(statusMessage, GUILayout.ExpandHeight(true));
			EditorGUILayout.EndScrollView();
		}

		private void AnalyzeLevels()
		{
			try
			{
				var pack = LoadLevelPack();
				if (pack == null) return;

				statusMessage = "=== ANALYSIS RESULTS ===\n\n";

				int totalLevels = 0;
				int levelsWithLargeVectors = 0;
				long totalSavings = 0;

				foreach (var chapter in pack.chapters)
				{
					foreach (var level in chapter.levels)
					{
						totalLevels++;

						if (level.testVectors == null || level.testVectors.Length == 0)
							continue;

						// Calculate current size in JSON
						int inputBits = level.inputBitCounts?.Sum() ?? level.inputCount;
						int outputBits = level.outputBitCounts?.Sum() ?? level.outputCount;
						int vectorCount = level.testVectors.Length;

						long jsonSize = EstimateJsonSize(level.testVectors);
						long binarySize = TestVectorsBinaryFormat.EstimateFileSize(vectorCount, inputBits, outputBits);
						long savings = jsonSize - binarySize;

						if (jsonSize > SIZE_THRESHOLD)
						{
							levelsWithLargeVectors++;
							totalSavings += savings;

							statusMessage += $"Level: {level.name}\n";
							statusMessage += $"  ID: {level.id}\n";
							statusMessage += $"  Vectors: {vectorCount}\n";
							statusMessage += $"  JSON size: {FormatBytes(jsonSize)}\n";
							statusMessage += $"  Binary size: {FormatBytes(binarySize)}\n";
							statusMessage += $"  Savings: {FormatBytes(savings)} ({(savings * 100 / jsonSize)}%)\n\n";
						}
					}
				}

				statusMessage += $"\n=== SUMMARY ===\n";
				statusMessage += $"Total levels: {totalLevels}\n";
				statusMessage += $"Levels with large test vectors: {levelsWithLargeVectors}\n";
				statusMessage += $"Potential savings: {FormatBytes(totalSavings)}\n";
			}
			catch (Exception ex)
			{
				statusMessage = $"Error: {ex.Message}\n{ex.StackTrace}";
			}
		}

		private void ConvertLargeLevels()
		{
			try
			{
				var pack = LoadLevelPack();
				if (pack == null) return;

				// Create backup
				BackupLevelsJson();

				// Create testvectors directory
				if (!Directory.Exists(TESTVECTORS_DIR))
				{
					Directory.CreateDirectory(TESTVECTORS_DIR);
				}

				statusMessage = "=== CONVERSION STARTED ===\n\n";
				int convertedCount = 0;

				foreach (var chapter in pack.chapters)
				{
					foreach (var level in chapter.levels)
					{
						if (level.testVectors == null || level.testVectors.Length == 0)
							continue;

						int inputBits = level.inputBitCounts?.Sum() ?? level.inputCount;
						int outputBits = level.outputBitCounts?.Sum() ?? level.outputCount;
						long jsonSize = EstimateJsonSize(level.testVectors);

						// Only convert if above threshold
						if (jsonSize > SIZE_THRESHOLD)
						{
							ConvertLevel(level, inputBits, outputBits);
							convertedCount++;
						}
					}
				}

				// Save modified levels.json
				SaveLevelPack(pack);

				statusMessage += $"\n=== CONVERSION COMPLETE ===\n";
				statusMessage += $"Converted {convertedCount} levels to binary format.\n";
				statusMessage += $"Original levels.json backed up.\n";

				AssetDatabase.Refresh();
			}
			catch (Exception ex)
			{
				statusMessage = $"Error: {ex.Message}\n{ex.StackTrace}";
			}
		}

		private void ConvertAllLevels()
		{
			try
			{
				var pack = LoadLevelPack();
				if (pack == null) return;

				// Create backup
				BackupLevelsJson();

				// Create testvectors directory
				if (!Directory.Exists(TESTVECTORS_DIR))
				{
					Directory.CreateDirectory(TESTVECTORS_DIR);
				}

				statusMessage = "=== CONVERTING ALL LEVELS ===\n\n";
				int convertedCount = 0;

				foreach (var chapter in pack.chapters)
				{
					foreach (var level in chapter.levels)
					{
						if (level.testVectors == null || level.testVectors.Length == 0)
							continue;

						int inputBits = level.inputBitCounts?.Sum() ?? level.inputCount;
						int outputBits = level.outputBitCounts?.Sum() ?? level.outputCount;

						ConvertLevel(level, inputBits, outputBits);
						convertedCount++;
					}
				}

				// Save modified levels.json
				SaveLevelPack(pack);

				statusMessage += $"\n=== CONVERSION COMPLETE ===\n";
				statusMessage += $"Converted {convertedCount} levels to binary format.\n";

				AssetDatabase.Refresh();
			}
			catch (Exception ex)
			{
				statusMessage = $"Error: {ex.Message}\n{ex.StackTrace}";
			}
		}

		private void ConvertLevel(LevelDefinition level, int inputBits, int outputBits)
		{
			// Generate binary file path
			string fileName = $"{level.id}.tvec";
			string filePath = Path.Combine(TESTVECTORS_DIR, fileName);

			// Write binary file
			TestVectorsBinaryFormat.WriteToFile(filePath, level.testVectors, inputBits, outputBits);

			// Update level definition
			level.testVectorsFile = $"testvectors/{level.id}"; // Resource path (no extension)
			level.testVectors = null; // Clear inline vectors

			statusMessage += $"Converted: {level.name} ({level.id})\n";
			statusMessage += $"  File: {fileName}\n";
			statusMessage += $"  Size: {FormatBytes(new FileInfo(filePath).Length)}\n";
		}

		private LocalLevelPack LoadLevelPack()
		{
			if (!File.Exists(LEVELS_JSON_PATH))
			{
				statusMessage = $"Error: Could not find {LEVELS_JSON_PATH}";
				return null;
			}

			string json = File.ReadAllText(LEVELS_JSON_PATH);
			var pack = JsonUtility.FromJson<LocalLevelPack>(json);

			if (pack == null || pack.chapters == null)
			{
				statusMessage = "Error: Failed to parse levels.json";
				return null;
			}

			return pack;
		}

		private void SaveLevelPack(LocalLevelPack pack)
		{
			string json = JsonUtility.ToJson(pack, prettyPrint: true);
			File.WriteAllText(LEVELS_JSON_PATH, json);
			statusMessage += "\nSaved updated levels.json\n";
		}

		private void BackupLevelsJson()
		{
			string backupPath = LEVELS_JSON_PATH + ".backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
			File.Copy(LEVELS_JSON_PATH, backupPath);
			statusMessage += $"Created backup: {Path.GetFileName(backupPath)}\n\n";
		}

		private long EstimateJsonSize(LevelDefinition.TestVector[] vectors)
		{
			// Rough estimate: each vector averages ~80 bytes in JSON
			long size = 0;
			foreach (var v in vectors)
			{
				size += v.inputs.Length + v.expected.Length + 40; // 40 for JSON overhead
			}
			return size;
		}

		private string FormatBytes(long bytes)
		{
			string[] sizes = { "B", "KB", "MB", "GB" };
			double len = bytes;
			int order = 0;
			while (len >= 1024 && order < sizes.Length - 1)
			{
				order++;
				len /= 1024;
			}
			return $"{len:0.##} {sizes[order]}";
		}
	}
}

