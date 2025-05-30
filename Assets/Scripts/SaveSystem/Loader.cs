using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using DLS.Description;
using DLS.Game;

namespace DLS.SaveSystem
{
	public static class Loader
	{
		public static AppSettings LoadAppSettings()
		{
			if (File.Exists(SavePaths.AppSettingsPath))
			{
				string settingsString = File.ReadAllText(SavePaths.AppSettingsPath);
				return Serializer.DeserializeAppSettings(settingsString);
			}

			return AppSettings.Default();
		}

		public static Project LoadProject(string projectName)
		{
			ProjectDescription projectDescription = LoadProjectDescription(projectName);
			ChipLibrary chipLibrary = LoadChipLibrary(projectDescription);
			return new Project(projectDescription, chipLibrary);
		}

		public static bool ProjectExists(string projectName)
		{
			string path = SavePaths.GetProjectDescriptionPath(projectName);
			return File.Exists(path);
		}

		public static ProjectDescription LoadProjectDescription(string projectName)
		{
			string path = SavePaths.GetProjectDescriptionPath(projectName);
			if (!File.Exists(path)) throw new Exception("No project description found at " + path);

			ProjectDescription desc = Serializer.DeserializeProjectDescription(File.ReadAllText(path));
			desc.ProjectName = projectName; // Enforce name = directory name (in case player modifies manually -- operations like deleting projects rely on this)

			for (int i = 0; i < desc.StarredList.Count; i++)
			{
				StarredItem starred = desc.StarredList[i];
				starred.CacheDisplayStrings();
				desc.StarredList[i] = starred;
			}

			foreach (ChipCollection collection in desc.ChipCollections)
			{
				collection.UpdateDisplayStrings();
			}

			UpgradeHelper.ApplyVersionChangesToProject(ref desc); // Apply changes necessary to ProjectDesc file for modded game.
			return desc;
		}

		// Get list of saved project descriptions (ordered by last save time)
		public static ProjectDescription[] LoadAllProjectDescriptions()
		{
			List<ProjectDescription> projectDescriptions = new();

			foreach (string dir in Directory.EnumerateDirectories(SavePaths.ProjectsPath))
			{
				try
				{
					string projectName = Path.GetFileName(dir);
					projectDescriptions.Add(LoadProjectDescription(projectName));
				}
				catch (Exception)
				{
					// Ignore invalid project directory
				}
			}

			projectDescriptions.Sort((a, b) => b.LastSaveTime.CompareTo(a.LastSaveTime));
			return projectDescriptions.ToArray();
		}

		static ChipLibrary LoadChipLibrary(ProjectDescription projectDescription)
		{
			string chipDirectoryPath = SavePaths.GetChipsPath(projectDescription.ProjectName);
            PinBitCount[] loadedPinBitCounts = projectDescription.pinBitCounts;
			ChipDescription[] PinDescriptions = new ChipDescription[loadedPinBitCounts.Length * 2];
            ChipDescription[] loadedChips = new ChipDescription[projectDescription.AllCustomChipNames.Length];

			if (!Directory.Exists(chipDirectoryPath) && loadedChips.Length > 0) throw new DirectoryNotFoundException(chipDirectoryPath);

			ChipDescription[] builtinChips = BuiltinChipCreator.CreateAllBuiltinChipDescriptions(projectDescription);
			HashSet<string> customChipNameHashset = new(ChipDescription.NameComparer);

            for (int i = 0; i < loadedPinBitCounts.Length; i ++)
            {
                PinBitCount pinBitCounts = loadedPinBitCounts[i];
                PinDescription[] outPin = new[] { new PinDescription("OUT", 1, UnityEngine.Vector2.zero, pinBitCounts, PinColour.Red, PinValueDisplayMode.Off) };
                PinDescription[] inPin = new[] { new PinDescription("IN", 0, UnityEngine.Vector2.zero, pinBitCounts, PinColour.Red, PinValueDisplayMode.Off) };


                ChipDescription InChipDesc = new ChipDescription
                {
                    Name = "IN-" + pinBitCounts.BitCount,
                    NameLocation = NameDisplayLocation.Hidden,
                    ChipType = ChipType.In_Pin,
                    Colour = UnityEngine.Color.clear,
                    SubChips = Array.Empty<SubChipDescription>(),
                    InputPins = Array.Empty<PinDescription>(),
                    OutputPins = outPin,
                    Displays = null,
                    Size = UnityEngine.Vector2.zero,
                    Wires = Array.Empty<WireDescription>()
                };

				ChipDescription OutChipDesc = new ChipDescription
				{
					Name = "OUT-" + pinBitCounts.BitCount,
					NameLocation = NameDisplayLocation.Hidden,
					ChipType = ChipType.Out_Pin,
					Colour = UnityEngine.Color.clear,
					SubChips = Array.Empty<SubChipDescription>(),
					InputPins = inPin,
					OutputPins = Array.Empty<PinDescription>(),
					Displays = null,
					Size = UnityEngine.Vector2.zero,
					Wires = Array.Empty<WireDescription>()
				};

                PinDescriptions[i*2] = InChipDesc;
                PinDescriptions[i*2 + 1] = OutChipDesc;
            }

			builtinChips = builtinChips.Concat(PinDescriptions).ToArray();

            for (int i = 0; i < projectDescription.AllCustomChipNames.Length; i++)
			{
				string chipPath = Path.Combine(chipDirectoryPath, projectDescription.AllCustomChipNames[i] + ".json");
				string chipSaveString = File.ReadAllText(chipPath);

				ChipDescription chipDesc = Serializer.DeserializeChipDescription(chipSaveString);
				loadedChips[i] = chipDesc;
				customChipNameHashset.Add(chipDesc.Name);
			}

            // If built-in chip name conflicts with a custom chip, the built-in chip must have been added in a newer version.
            // In that case, simply exclude the built-in chip. TODO: warn player that they should rename their chip if they want access to new builtin version
            builtinChips = builtinChips.Where(b => !customChipNameHashset.Contains(b.Name)).ToArray();

			UpgradeHelper.ApplyVersionChanges(loadedChips, builtinChips);
			return new ChipLibrary(loadedChips, builtinChips);
		}
	}
}