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
		}, new[] { "application/json", "text/json", "text/plain" });
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
