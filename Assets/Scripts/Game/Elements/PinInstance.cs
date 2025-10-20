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
		public int face;
        public int ID;

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
			
			State.SetAllDisconnected();
            ID = desc.ID;
            LocalPosY = desc.LocalOffset;
            
            // Set face based on isSourcePin (original logic for builtin chips)
            // Source pins (outputs) go on right side (face 1), input pins go on left side (face 3)
            face = faceRight ? 1 : 3;
            desc.face = face; // Update the description to match
            
            // For custom shapes, use the Position field from description
            // For builtin shapes, keep face+offset system
            if (parent is SubChipInstance subChip && subChip.Description.ShapeType != ChipShapeType.Rectangle)
            {
                // Custom shapes use Position field (relative to chip center)
                Debug.Log($"Pin {Name}: Using Position = {desc.Position} for custom shape");
            }
            else
            {
                // Builtin shapes use face+offset system
                Debug.Log($"Pin {Name}: Using face+offset system, face = {face}, offset = {LocalPosY}");
            }
            
			State.MakeFromPinBitCount(bitCount);
			PlayerInputState.MakeFromPinBitCount(bitCount);
		}

        public Vector2 ForwardDir => faceRight ? Vector2.right : Vector2.left;
        
        
        public Vector2 FacingDir
        {
            get
            {
                if (parent is SubChipInstance subchip)
                {
                    return GetFacingDirForShape(subchip.Description.ShapeType, subchip.Description.ShapeRotation, face);
                }
                // Default rectangle behavior for other cases
                return face == 1 ? Vector2.right : face == 3 ? Vector2.left : face == 2 ? Vector2.down : Vector2.up;
            }
        }

        public Vector2 GetWorldPos()
        {
            switch (parent)
            {
                case DevPinInstance devPin:
                    return devPin.PinPosition;
                case SubChipInstance subchip:
                    {
                        // For custom shapes, use Position field from description
                        if (subchip.Description.ShapeType != ChipShapeType.Rectangle)
                        {
                            // Get the Position from the description (relative to chip center)
                            Vector2 relativePos = GetPositionFromDescription();
                            Vector2 worldPos = subchip.Position + relativePos;
                            Debug.Log($"MAIN GAME: Pin {Name}: Using Position = {relativePos}, chipPos = {subchip.Position}, worldPos = {worldPos}");
                            return worldPos;
                        }
                        
                        // For builtin shapes, use face+offset system
                        Vector2 chipSize = subchip.Size;
                        Vector2 chipPos = subchip.Position;

                        float halfWidth = chipSize.x / 2f;
                        float halfHeight = chipSize.y / 2f;
                        float inset = DrawSettings.SubChipPinInset;
                        float outlineOffset = DrawSettings.ChipOutlineWidth / 2f;

                        float x = 0f;
                        float y = 0f;

                        // Handle different shapes
                        ChipShapeType shapeType = subchip.Description.ShapeType;
                        float rotation = subchip.Description.ShapeRotation;
                        
                        switch (shapeType)
                        {
                            case ChipShapeType.Rectangle:
                                GetRectanglePinPosition(face, LocalPosY, halfWidth, halfHeight, outlineOffset, inset, out x, out y);
                                break;
                            case ChipShapeType.Hexagon:
                                GetHexagonPinPosition(face, LocalPosY, halfWidth, halfHeight, outlineOffset, inset, rotation, out x, out y);
                                break;
                            case ChipShapeType.Triangle:
                                GetTrianglePinPosition(face, LocalPosY, halfWidth, halfHeight, outlineOffset, inset, rotation, out x, out y);
                                break;
                            default:
                                GetRectanglePinPosition(face, LocalPosY, halfWidth, halfHeight, outlineOffset, inset, out x, out y);
                                break;
                        }

                        return chipPos + new Vector2(x, y);
                    }
                default:
                    throw new Exception("Parent type not supported");
            }
        }
        
        Vector2 GetPositionFromDescription()
        {
            // Find the pin description in the chip's description
            if (parent is SubChipInstance subchip)
            {
                var chipDesc = subchip.Description;
                
                // Search in input pins
                if (chipDesc.InputPins != null)
                {
                    foreach (var pinDesc in chipDesc.InputPins)
                    {
                        if (pinDesc.ID == ID)
                        {
                            return pinDesc.Position;
                        }
                    }
                }
                
                // Search in output pins
                if (chipDesc.OutputPins != null)
                {
                    foreach (var pinDesc in chipDesc.OutputPins)
                    {
                        if (pinDesc.ID == ID)
                        {
                            return pinDesc.Position;
                        }
                    }
                }
            }
            
            return Vector2.zero;
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

		static void GetRectanglePinPosition(int face, float localPosY, float halfWidth, float halfHeight, float outlineOffset, float inset, out float x, out float y)
		{
			switch (face)
			{
				case 0: // Top edge (Y fixed)
					x = localPosY;
					y = halfHeight + outlineOffset - inset;
					break;
				case 1: // Right edge (X fixed)
					x = halfWidth + outlineOffset - inset;
					y = localPosY;
					break;
				case 2: // Bottom edge (Y fixed)
					x = localPosY;
					y = -halfHeight - outlineOffset + inset;
					break;
				case 3: // Left edge (X fixed)
					x = -halfWidth - outlineOffset + inset;
					y = localPosY;
					break;
				default:
					throw new Exception("Invalid rectangle pin face: " + face);
			}
		}

		static void GetHexagonPinPosition(int face, float localPosY, float halfWidth, float halfHeight, float outlineOffset, float inset, float rotation, out float x, out float y)
		{
			// Hexagon has 6 faces (0-5)
			if (face < 0 || face > 5)
				throw new Exception("Invalid hexagon pin face: " + face);

			// Calculate the position on the hexagon edge
			float angle = face * Mathf.PI / 3f; // 60 degrees per face
			float rotationRad = rotation * Mathf.Deg2Rad;
			angle += rotationRad;

			// Calculate the point on the hexagon edge
			float edgeX = Mathf.Cos(angle) * halfWidth;
			float edgeY = Mathf.Sin(angle) * halfHeight;

			// Calculate the tangent direction for offset
			Vector2 tangent = new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle));
			
			// Apply the offset along the tangent
			Vector2 offset = tangent * localPosY;
			
			// Move inward by the inset
			Vector2 inward = new Vector2(-Mathf.Cos(angle), -Mathf.Sin(angle)) * (outlineOffset - inset);
			
			x = edgeX + offset.x + inward.x;
			y = edgeY + offset.y + inward.y;
		}

		static void GetTrianglePinPosition(int face, float localPosY, float halfWidth, float halfHeight, float outlineOffset, float inset, float rotation, out float x, out float y)
		{
			// Triangle has 3 faces (0-2)
			if (face < 0 || face > 2)
				throw new Exception("Invalid triangle pin face: " + face);

			// Calculate the position on the triangle edge
			float angle = face * 2f * Mathf.PI / 3f; // 120 degrees per face
			float rotationRad = rotation * Mathf.Deg2Rad;
			angle += rotationRad;

			// Calculate the point on the triangle edge
			float edgeX = Mathf.Cos(angle) * halfWidth;
			float edgeY = Mathf.Sin(angle) * halfHeight;

			// Calculate the tangent direction for offset
			Vector2 tangent = new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle));
			
			// Apply the offset along the tangent
			Vector2 offset = tangent * localPosY;
			
			// Move inward by the inset
			Vector2 inward = new Vector2(-Mathf.Cos(angle), -Mathf.Sin(angle)) * (outlineOffset - inset);
			
			x = edgeX + offset.x + inward.x;
			y = edgeY + offset.y + inward.y;
		}

		static Vector2 GetFacingDirForShape(ChipShapeType shapeType, float rotation, int face)
		{
			switch (shapeType)
			{
				case ChipShapeType.Rectangle:
					return GetRectangleFacingDir(face);
				case ChipShapeType.Hexagon:
					return GetHexagonFacingDir(face, rotation);
				case ChipShapeType.Triangle:
					return GetTriangleFacingDir(face, rotation);
				default:
					return GetRectangleFacingDir(face);
			}
		}

		static Vector2 GetRectangleFacingDir(int face)
		{
			switch (face)
			{
				case 0: return Vector2.up;
				case 1: return Vector2.right;
				case 2: return Vector2.down;
				case 3: return Vector2.left;
				default: return Vector2.up;
			}
		}

		static Vector2 GetHexagonFacingDir(int face, float rotation)
		{
			float angle = face * Mathf.PI / 3f + rotation * Mathf.Deg2Rad;
			return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
		}

		static Vector2 GetTriangleFacingDir(int face, float rotation)
		{
			float angle = face * 2f * Mathf.PI / 3f + rotation * Mathf.Deg2Rad;
			return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
		}
	}
}