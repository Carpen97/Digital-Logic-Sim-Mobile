using System;
using System.Collections;
using DLS.Description;
using DLS.Graphics;
using DLS.Simulation;
using UnityEngine;

namespace DLS.Game
{
	public class PinInstance : IInteractable
	{
		public readonly PinAddress Address;

		public PinBitCount bitCount;
		public readonly bool IsBusPin;
		public readonly bool IsSourcePin;

		// Pin may be attached to a chip or a devPin as its parent
		public readonly IMoveable parent;
		public PinStateValue State; // sim state
		public PinStateValue PlayerInputState;
		public PinColour Colour;
		bool faceRight;
		public float LocalPosY;
		public string Name;


		public PinInstance(PinDescription desc, PinAddress address, IMoveable parent, bool isSourcePin)
		{
			this.parent = parent;
			bitCount = desc.BitCount;
			Name = desc.Name;
			Address = address;
			IsSourcePin = isSourcePin;
			Colour = desc.Colour;

			IsBusPin = parent is SubChipInstance subchip && subchip.IsBus;
			faceRight = isSourcePin;

			State.MakeFromPinBitCount(bitCount);
			PlayerInputState.MakeFromPinBitCount(bitCount);

		}

		public Vector2 ForwardDir => faceRight ? Vector2.right : Vector2.left;


		public Vector2 GetWorldPos()
		{
			switch (parent)
			{
				case DevPinInstance devPin:
					return devPin.PinPosition;
				case SubChipInstance subchip:
				{
					Vector2 chipSize = subchip.Size;
					Vector2 chipPos = subchip.Position;

					float xLocal = (chipSize.x / 2 + DrawSettings.ChipOutlineWidth / 2 - DrawSettings.SubChipPinInset) * (faceRight ? 1 : -1);
					return chipPos + new Vector2(xLocal, LocalPosY);
				}
				default:
					throw new Exception("Parent type not supported");
			}
		}

		public void SetBusFlip(bool flipped)
		{
			faceRight = IsSourcePin ^ flipped;
		}

		public Color GetColLow() => DrawSettings.ActiveTheme.StateLowCol[(int)Colour];
		public Color GetColHigh() => DrawSettings.ActiveTheme.StateHighCol[(int)Colour];

		public Color GetStateCol(int bitIndex, bool hover = false, bool canUsePlayerState = true, bool forWires = false)
		{
			PinStateValue pinState = (IsSourcePin && canUsePlayerState) ? PlayerInputState : State; // dev input pin uses player state (so it updates even when sim is paused)
			uint state = pinState.GetTristatedValue(bitIndex);
			if (state == PinStateValue.LOGIC_DISCONNECTED) return DrawSettings.ActiveTheme.StateDisconnectedCol;
			if(forWires && bitCount >= 64) { return DrawSettings.GetFlatColour(state == PinStateValue.LOGIC_HIGH, (uint)Colour, hover); }
			return DrawSettings.GetStateColour(state == PinStateValue.LOGIC_HIGH, (uint)Colour, hover);
			
		}
		public void ChangeBitCount(int NewBitCount)
		{ 
			bitCount.BitCount = (ushort)NewBitCount;
		}
	}
}