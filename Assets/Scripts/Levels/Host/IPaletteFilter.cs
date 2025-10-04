using System.Collections.Generic;

namespace DLS.Levels.Host
{
	public interface IPaletteFilter
	{
		void SetAllowedParts(IReadOnlyList<string> allowedPartIds);
		void ClearRestrictions();
	}
}
