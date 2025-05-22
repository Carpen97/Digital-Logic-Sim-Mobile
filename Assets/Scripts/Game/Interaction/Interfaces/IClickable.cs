using System.Numerics;
using Seb.Types;

namespace DLS.Game
{
	public interface IClickable : IInteractable
	{
        Vector2 Position { get; set; }
        Bounds2D InteractionBoundingBox { get; set; }
    }
}