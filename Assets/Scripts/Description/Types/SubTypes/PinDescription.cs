using System;
using System.Collections;
using UnityEngine;

namespace DLS.Description
{
	public struct PinDescription
	{
		public string Name;
		public int ID;
		public Vector2 Position;
		public PinBitCount BitCount;
		public PinColour Colour;
		public PinValueDisplayMode ValueDisplayMode;

		public PinDescription(string name, int id, Vector2 position, PinBitCount bitCount, PinColour colour, PinValueDisplayMode valueDisplayMode)
		{
			Name = name;
			ID = id;
			Position = position;
			BitCount = bitCount;
			Colour = colour;
			ValueDisplayMode = valueDisplayMode;
		}
	}

	public struct PinBitCount
	{
		public const int Bit1 = 1;
		public const int Bit4 = 4;
		public const int Bit8 = 8;

		public ushort BitCount;

		public PinBitCount(ushort BitCount = 1)
		{
			this.BitCount = BitCount;
		}
		public readonly BitArray GetEmptyBitArray()
		{
			return new BitArray(BitCount);
		}

        public override bool Equals(System.Object @object)
        {
			if(@object is uint number)
			{
				return BitCount == number;
			}
            return base.Equals(@object);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(BitCount);
        }

        public static bool operator ==(PinBitCount a, PinBitCount b) => a.BitCount == b.BitCount;
        public static bool operator !=(PinBitCount a, PinBitCount b) => a.BitCount != b.BitCount;

        public static bool operator ==(uint number, PinBitCount b) => number == b.BitCount;
        public static bool operator !=(uint number, PinBitCount b) => number != b.BitCount;
        public static bool operator ==(PinBitCount a, uint number) => number == a.BitCount;
        public static bool operator !=(PinBitCount a, uint number) => number != a.BitCount;

        public static bool operator ==(PinBitCount a, int number) => number == a.BitCount;
        public static bool operator !=(PinBitCount a, int number) => number != a.BitCount;


        public static implicit operator uint(PinBitCount b) => b.BitCount;
        public static implicit operator int(PinBitCount b) => b.BitCount;
		public static implicit operator ushort(PinBitCount b) => b.BitCount;
		public static implicit operator PinBitCount(Int64 b) => new PinBitCount((ushort)b);


        public static explicit operator PinBitCount(ushort number) => new PinBitCount(number);
		public static explicit operator PinBitCount(int number) => new PinBitCount((ushort)number);

    } 

    public enum PinColour
	{
		Red,
		Orange,
		Yellow,
		Green,
		Blue,
		Violet,
		Pink,
		White
	}

	public enum PinValueDisplayMode
	{
		Off,
		UnsignedDecimal,
		SignedDecimal,
		HEX
	}
}