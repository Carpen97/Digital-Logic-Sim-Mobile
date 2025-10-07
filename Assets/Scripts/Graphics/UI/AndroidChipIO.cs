using UnityEngine;
using System.IO;
using DLS.Description;
using DLS.Game;
using DLS.SaveSystem;
using System;
using System.IO.Compression;
using DLS.Graphics;
using Seb.Vis.UI;
using Seb.Vis;
using Seb.Helpers;
using Seb.Types;


public static class AndroidIO
{
    private static string targetProjectName;
    private static string finalPath;
    private static string extractedRoot;

    public static void ImportChip(Action<string> onJsonLoaded)
	{
		NativeFilePicker.PickFile((path) =>
		{
			if (string.IsNullOrEmpty(path))
				return;

			try
			{
				string json = File.ReadAllText(path);
				Debug.Log($"[ImportChip] Loaded JSON ({json.Length} characters)");
				onJsonLoaded?.Invoke(json);
			}
			catch (System.Exception e)
			{
				Debug.LogError($"[ImportChip] Failed to load file: {e.Message}");
			}
		},
		new[] { "application/json", "text/json", "text/plain" }
		);
	}

public static void ExportProjectToZip(string projectName)
{
	try
	{
		string projectPath = SavePaths.GetProjectPath(projectName);

		if (!Directory.Exists(projectPath))
		{
			Debug.LogError($"[ExportProject] Project folder not found: {projectPath}");
			return;
		}

		string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
		string tempZipPath = Path.Combine(Application.temporaryCachePath, $"{projectName}_{timestamp}.zip");

		if (File.Exists(tempZipPath))
			File.Delete(tempZipPath);

		// --- Zip creation ---
		ZipFile.CreateFromDirectory(projectPath, tempZipPath, System.IO.Compression.CompressionLevel.Fastest, true);

		// --- Ensure the file is flushed and available ---
		FileStream stream = File.OpenRead(tempZipPath);
		stream.Close();

		// Optional: wait a tiny bit in case OS hasn't released it
		System.Threading.Thread.Sleep(100);

		// --- Start export ---
		NativeFilePicker.ExportFile(tempZipPath, (bool success) =>
		{
			if (success)
			{
				Debug.Log($"[ExportProject] Export successful!");
			}
			else
			{
				Debug.LogWarning("[ExportProject] Export canceled or failed.");
			}

			// Clean up
			if (File.Exists(tempZipPath))
				File.Delete(tempZipPath);
		});
	}
	catch (Exception e)
	{
		Debug.LogError($"[ExportProject] Failed: {e.Message}");
	}
}


	
	public static void ImportProjectFromZip(string zipFilePath)
	{
		if (!File.Exists(zipFilePath))
		{
			Debug.LogError($"[ImportProject] File does not exist: {zipFilePath}");
			return;
		}

		try
		{
			string tempExtractPath = Path.Combine(Application.temporaryCachePath, "ImportedProject");
			if (Directory.Exists(tempExtractPath))
				Directory.Delete(tempExtractPath, true);

			ZipFile.ExtractToDirectory(zipFilePath, tempExtractPath);


			string[] projectJsonFiles = Directory.GetFiles(tempExtractPath, "ProjectDescription.json", SearchOption.AllDirectories);
			if (projectJsonFiles.Length == 0)
			{
				Debug.LogError("[ImportProject] No 'ProjectDescription.json' found in zip.");
				return;
			}

			string projectJson = projectJsonFiles[0];
			string json = File.ReadAllText(projectJson);
			var description = Serializer.DeserializeProjectDescription(json);
			targetProjectName = description.ProjectName;
			finalPath = SavePaths.GetProjectPath(targetProjectName);

			// NEW: Auto-navigate into nested folder if structure is redundant
			extractedRoot = tempExtractPath;
			string[] subDirs = Directory.GetDirectories(tempExtractPath);
			if (subDirs.Length == 1 && Path.GetFileName(subDirs[0]) == targetProjectName)
			{
				Debug.Log($"[ImportProject] Nested folder '{targetProjectName}' detected. Using as root.");
				extractedRoot = subDirs[0];
			}

			if (Directory.Exists(finalPath))
			{
				MainMenu.ShowOverwriteConfirmationPopup();
			}
			else
			{
				FinishImport();
			}

		}
		catch (Exception e)
		{
			Debug.LogError($"[ImportProject] Failed: {e.Message}");
		}

		MainMenu.RefreshLoadedProjects();
		UIDrawer.SetActiveMenu(UIDrawer.MenuType.MainMenu);
	}


	public static void FinishImport()
	{
		CopyDirectory(extractedRoot, finalPath);
		Debug.Log($"[ImportProject] Successfully imported project: {targetProjectName}");
	}


	private static void CopyDirectory(string sourceDir, string targetDir)
	{
		Directory.CreateDirectory(targetDir);

		foreach (string file in Directory.GetFiles(sourceDir))
		{
			string targetFilePath = Path.Combine(targetDir, Path.GetFileName(file));
			File.Copy(file, targetFilePath, true);
		}

		foreach (string dir in Directory.GetDirectories(sourceDir))
		{
			string targetSubDir = Path.Combine(targetDir, Path.GetFileName(dir));
			CopyDirectory(dir, targetSubDir);
		}
	}

}
