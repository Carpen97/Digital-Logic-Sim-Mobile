using System.Linq;
using Random = System.Random;

namespace DLS.Game
{
	public static class IDGenerator
	{
		static Random rng;

		// Generate a random ID (greater than zero) which is guaranteed to be unique (in context of current chip)
		public static int GenerateNewElementID(DevChipInstance devChip)
		{
			while (true)
			{
				int candidateID = GetRandomID();
				bool isUnique = devChip.Elements.All(x => x.ID != candidateID);
				if (isUnique) return candidateID;
			}
		}

		static int GetRandomID()
		{
			const int minValue = 1;
			rng ??= new Random();
			return rng.Next(minValue, int.MaxValue);
		}

		/// <summary>Generate a unique group ID for component grouping. Always non-zero.</summary>
		public static int GenerateNewGroupId(DevChipInstance devChip)
		{
			while (true)
			{
				int candidateID = GetRandomID();
				bool isUnique = !devChip.Elements.Any(x =>
					(x is SubChipInstance s && s.GroupId == candidateID) ||
					(x is DevPinInstance d && d.GroupId == candidateID));
				if (isUnique) return candidateID;
			}
		}
	}
}