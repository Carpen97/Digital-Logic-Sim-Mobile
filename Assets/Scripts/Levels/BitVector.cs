using System;
using System.Text;

namespace DLS.Levels
{
	public readonly struct BitVector
	{
		public readonly int Length;
		private readonly ulong _bits; // MVP: up to 64 bits

		public BitVector(ulong bits, int length)
		{
			_bits = bits;
			Length = length;
		}

		public bool this[int i] => ((_bits >> i) & 1UL) != 0;

		public override string ToString()
		{
			var sb = new StringBuilder(Length);
			for (int i = Length - 1; i >= 0; i--)
				sb.Append(this[i] ? '1' : '0');
			return sb.ToString();
		}

		public ulong Raw => _bits;

		public static BitVector FromString(string bitString)
		{
			ulong v = 0;
			int len = bitString.Length;
			for (int i = 0; i < len; i++)
			{
				char c = bitString[i];
				if (c == '1') v |= (1UL << i);
			}
			return new BitVector(v, len);
		}
	}
}
