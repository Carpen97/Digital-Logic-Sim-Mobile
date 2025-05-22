using System.Collections.Generic;
using System.Numerics;
using DLS.Description;
using Seb.Types;
using Seb.Helpers;

namespace DLS.Game
{
    public class ClickableDisplayInstance : DisplayInstance, IClickable
    {
        public UnityEngine.Vector2 Position { get; set; }

        public Bounds2D InteractionBoundingBox { get; set; }


    }
}