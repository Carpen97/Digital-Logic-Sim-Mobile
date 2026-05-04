using System.Collections.Generic;
using System.Linq;
using DLS.Description;

namespace DLS.Game
{
	public class ChipLibrary
	{
		public readonly List<ChipDescription> allChips = new();
		public readonly List<GroupDescription> allGroups = new();

		readonly HashSet<string> builtinChipNames = new(ChipDescription.NameComparer);
		readonly Dictionary<string, ChipDescription> descriptionFromNameLookup = new(ChipDescription.NameComparer);
		readonly Dictionary<string, GroupDescription> groupFromNameLookup = new(ChipDescription.NameComparer);

		readonly List<ChipDescription> hiddenChips = new();

		public ChipLibrary(ChipDescription[] customChips, ChipDescription[] builtinChips) : this(customChips, builtinChips, null) { }

		public ChipLibrary(ChipDescription[] customChips, ChipDescription[] builtinChips, GroupDescription[] groups)
		{
			// Add built-in chips to list of all chips
			foreach (ChipDescription chip in builtinChips)
			{
				// Bus terminus chip should not be shown to the user (it is created automatically upon placement of a bus start point)
				// ROM variants should not be shown to the user (they are accessible through the ROM editor's pin configuration selector)
				// Transmitter/Receiver -4 and -8 variants are hidden (accessible via edit menu on the 1-bit chip)
				bool hidden = ChipTypeHelper.IsBusTerminusType(chip.ChipType) || ShouldHideRomVariant(chip.ChipType) || ShouldHideWirelessVariant(chip.Name);

				AddChipToLibrary(chip, hidden);
				builtinChipNames.Add(chip.Name);
			}

			// Add custom chips to list of all chips
			foreach (ChipDescription chip in customChips)
			{
				AddChipToLibrary(chip);
			}

			// Add groups
			if (groups != null)
			{
				foreach (GroupDescription group in groups)
				{
					AddGroupToLibrary(group);
				}
			}

			RebuildChipDescriptionLookup();
		}

		void RebuildChipDescriptionLookup()
		{
			descriptionFromNameLookup.Clear();
			foreach (ChipDescription desc in allChips)
			{
				// Use TryAdd to avoid duplicate key exceptions
				if (!descriptionFromNameLookup.TryAdd(desc.Name, desc))
				{
					// If key already exists, keep the first one (prioritize visible chips)
					// This handles cases where multiple ROM variants might have the same display name
				}
			}

			foreach (ChipDescription desc in hiddenChips)
			{
				// Use TryAdd for hidden chips too, but they won't override visible ones
				descriptionFromNameLookup.TryAdd(desc.Name, desc);
			}

			groupFromNameLookup.Clear();
			foreach (GroupDescription g in allGroups)
			{
				groupFromNameLookup[g.Name] = g;
			}
		}


		public bool IsBuiltinChip(string name) => builtinChipNames.Contains(name);

		public bool HasChip(string name) => TryGetChipDescription(name, out _);

		public ChipDescription GetChipDescription(string name) => descriptionFromNameLookup[name];

		public ChipDescription GetTerminusDescription(PinBitCount bitCount)
		{
			foreach(ChipDescription desc in hiddenChips)
			{
				if(desc.ChipType == ChipType.BusTerminus && desc.InputPins[0].BitCount == bitCount)
				{
					return desc;
				}
			}

			throw new System.Exception("Bus terminus not found");
		}

		public bool TryGetChipDescription(string name, out ChipDescription description) => descriptionFromNameLookup.TryGetValue(name, out description);

		public bool HasGroup(string name) => TryGetGroupDescription(name, out _);
		public GroupDescription GetGroupDescription(string name) => groupFromNameLookup[name];
		public bool TryGetGroupDescription(string name, out GroupDescription description) => groupFromNameLookup.TryGetValue(name, out description);

		public void RemoveChip(string chipName)
		{
            allChips.RemoveAll(c => c.NameMatch(chipName));
            RebuildChipDescriptionLookup();
		}

		public void NotifyChipSaved(ChipDescription description, bool hidden = false)
		{
			// Replace chip description if already exists
			bool foundChip = false;

			for (int i = 0; i < allChips.Count; i++)
			{
				if (allChips[i].NameMatch(description.Name))
				{
					allChips[i] = description;
					foundChip = true;
					break;
				}
			}

			// Otherwise add as new description
			if (!foundChip) AddChipToLibrary(description, hidden);

			RebuildChipDescriptionLookup();
		}


		public void NotifyChipRenamed(ChipDescription description, string nameOld)
		{
			// Replace chip description
			for (int i = 0; i < allChips.Count; i++)
			{
				if (allChips[i].NameMatch(nameOld))
				{
					allChips[i] = description;
					break;
				}
			}

			RebuildChipDescriptionLookup();
		}

		public void NotifyGroupSaved(GroupDescription description)
		{
			for (int i = 0; i < allGroups.Count; i++)
			{
				if (allGroups[i].NameMatch(description.Name))
				{
					allGroups[i] = description;
					groupFromNameLookup[description.Name] = description;
					return;
				}
			}
			AddGroupToLibrary(description);
		}

		public void RemoveGroup(string groupName)
		{
			allGroups.RemoveAll(g => g.NameMatch(groupName));
			groupFromNameLookup.Remove(groupName);
		}

		public string[] GetAllGroupNames()
		{
			return allGroups.Select(g => g.Name).ToArray();
		}

		void AddGroupToLibrary(GroupDescription description)
		{
			allGroups.Add(description);
			groupFromNameLookup[description.Name] = description;
		}

		public string[] GetAllCustomChipNames()
		{
			List<string> customChipNames = new();

			foreach (ChipDescription chip in allChips)
			{
				if (!IsBuiltinChip(chip.Name))
				{
					customChipNames.Add(chip.Name);
				}
			}

			return customChipNames.ToArray();
		}

		// Returns the descriptions of all chips that use the given chip as a direct subchip
		public ChipDescription[] GetDirectParentChips(string chipName)
		{
			List<ChipDescription> parents = new();

			foreach (ChipDescription other in allChips)
			{
				if (other.SubChips == null) continue;
				if (other.SubChips.Any(subchip => ChipDescription.NameMatch(subchip.Name, chipName)))
				{
					parents.Add(other);
				}
			}

			return parents.ToArray();
		}

		void AddChipToLibrary(ChipDescription description, bool hidden = false)
		{
			if(description.ChipType != ChipType.Custom) builtinChipNames.Add(description.Name);
			if (hidden) hiddenChips.Add(description);
			else allChips.Add(description);
		}

		static bool ShouldHideRomVariant(ChipType chipType)
		{
			// Hide ROM variants - only show the original ROM 256x16 in the library
			// Users can access other pin configurations through the ROM editor's pin configuration selector
			return chipType == ChipType.Rom_2x8 ||
			       chipType == ChipType.Rom_1x16 ||
			       chipType == ChipType.Rom_4x4 ||
			       chipType == ChipType.Rom_16x1;
		}

		static bool ShouldHideWirelessVariant(string name)
		{
			return ChipDescription.NameMatch(name, "TRANSMITTER-4") || ChipDescription.NameMatch(name, "TRANSMITTER-8") ||
			       ChipDescription.NameMatch(name, "RECEIVER-4") || ChipDescription.NameMatch(name, "RECEIVER-8");
		}

		/// <summary>
		/// Returns true if the chip (or any of its subchips, recursively) contains a chip type disallowed in levels.
		/// Used to block placement of custom chips that have ROM, Button, etc. nested inside them.
		/// </summary>
		public bool ChipDescriptionContainsDisallowedSubchipsForLevel(ChipDescription desc)
		{
			if (desc == null) return false;
			var visited = new HashSet<string>(ChipDescription.NameComparer);
			return ChipDescriptionContainsDisallowedSubchipsRecursive(desc, visited);
		}

		bool ChipDescriptionContainsDisallowedSubchipsRecursive(ChipDescription desc, HashSet<string> visited)
		{
			if (desc == null) return false;

			if (desc.ChipType != ChipType.Custom)
			{
				return ChipTypeHelper.IsDisabledInLevels(desc.ChipType);
			}

			// Custom chip: recurse into subchips
			if (visited.Add(desc.Name) == false) return false; // Already visiting (circular ref)
			if (desc.SubChips == null) return false;

			foreach (var subChip in desc.SubChips)
			{
				if (TryGetChipDescription(subChip.Name, out ChipDescription subDesc))
				{
					if (ChipDescriptionContainsDisallowedSubchipsRecursive(subDesc, visited))
						return true;
				}
			}

			visited.Remove(desc.Name);
			return false;
		}
	}
}