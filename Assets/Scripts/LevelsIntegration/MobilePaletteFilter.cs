using System.Collections.Generic;
using DLS.Levels.Host;

public sealed class MobilePaletteFilter : IPaletteFilter
{
	public void SetAllowedParts(IReadOnlyList<string> allowedPartIds)
	{
		// TODO: filter hotbar/palette to allowed parts
	}

	public void ClearRestrictions()
	{
		// TODO: restore full palette
	}
}
