using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;

namespace DLS.Simulation
{
	// Helper class for dealing with pin state.
	// Pin state is stored as a uint32, with format:
	// Tristate flags (most significant 16 bits) | Bit states (least significant 16 bits)
	public static class BitArrayHelper
	{
		// Each bit has three possible states (tri-state logic):
		public const ushort LogicLow = 0;
		public const ushort LogicHigh = 1;
		public const ushort LogicDisconnected = 2;


		// Mask for single bit value (bit state, and tristate flag)
		public const uint SingleBitMask = 1 | (1 << 16);

        public static BitArray NibbleMaskArray = new BitArray(new byte[] { 0b1111});
        public static BitArray ByteMaskArray = new BitArray(new byte[] { 0b11111111 });
        public static BitArray ShortMaskArray = new BitArray(new byte[] { 0b11111111, 0b11111111 });
        public static BitArray IntMaskArray = new BitArray(new byte[] { 0b11111111, 0b11111111, 0b11111111, 0b11111111 });

        public static BitArray NonMutativeOR(BitArray A, BitArray B)
        {
            BitArray temp = new BitArray(A);
            temp.Or(B);
            return temp;
        }
        
        public static BitArray NonMutativeAND(BitArray A, BitArray B)
        {
            BitArray temp = new BitArray(A);
            temp.And(B);
            return temp;
        }

        public static BitArray NonMutativeNOT(BitArray A)
        {
            BitArray temp = new BitArray(A);
            temp.Not();
            return temp;
        }

        public static BitArray TrueBitArray(int length)
        {
            BitArray array = new BitArray(length);
            array.SetAll(true);
            return array;
        }
        public static BitArray FalseBitArray(int length)
        {
            BitArray array = new BitArray(length);
            array.SetAll(false);
            return array;
        }

        public static BitArray GetBitStates(BitArray state) {
            return GetFirstNBits(state, state.Length>>1);
		}

        public static BitArray GetTristateFlags(BitArray state)
        {
            BitArray array = new BitArray(state).RightShift(state.Length >> 1);
            return GetFirstNBits(array, array.Length >> 1);
        }

        public static BitArray Concatenate(BitArray a, BitArray b)
        {
            BitArray newBitArray = SetNBitsAtIndex(a, b, a.Length);
            return newBitArray;
        }

		public static void Set(ref BitArray state, BitArray bitStates, BitArray tristateFlags)
		{
            state = Concatenate(bitStates, tristateFlags);
		}



        public static void Set(ref BitArray state, BitArray other)
        {
            state = other;
        }

		public static ushort GetBitTristatedValue(BitArray state, int bitIndex)
		{
            if(state is null || state.Length<=1) return LogicDisconnected;
			bool bitState = GetBitStates(state).Get(bitIndex);
			bool tri = GetTristateFlags(state).Get(bitIndex);
			return (ushort)(bitState ? 1 : tri ? 2 : 0); // Combine to form tri-stated value: 0 = LOW, 1 = HIGH, 2 = DISCONNECTED
		}

		public static bool FirstBitHigh(BitArray state) {
            if (state.Length <=0) return false;
            return state.Get(0);
        }

        public static BitArray GetFirstNBits(BitArray state, int length)
        {
            int len = Math.Min(length, state.Count);
            BitArray n = new BitArray(length);
            for (byte i = 0; i < len; i++)
            {
                n.Set(i, state[i]);
            }
            return n;
        }


        public static byte GetFirstByteFromByteArray(BitArray state)
		{
            int len = Math.Min(8, state.Count);
            byte n = 0;
            for (byte i = 0; i < len; i++)
            {
                if (state.Get(i))
                    n |= (byte)(1 << i);
            }
            return n;
        }

        public static byte GetFirstOfMaxLengthFromByteArray(BitArray state, int maxLength)
        {
            int len = Math.Min(maxLength, state.Count);
            byte n = 0;
            for (byte i = 0; i < len; i++)
            {
                if (state.Get(i))
                    n |= (byte)(1 << i);
            }
            return n;
        }

        public static ushort GetFirstUShortFromByteArray(BitArray state)
        {
            int len = Math.Min(16, state.Count);
            ushort n = 0;
            for (byte i = 0; i < len; i++)
            {
                if (state.Get(i))
                    n |= (ushort)(1 << i);
            }
            return n;
        }

        public static ushort GetUShortAtIndexOfMaxLength(BitArray state, int index, int maxLength)
        {
            int len = Mathf.Min(maxLength + index, state.Count, 16 + index);
            ushort n = 0;
            for (int i = index; i < len ; i++)
            {
                if (state.Get(i))
                    n |= (ushort)(1 << i);
            }
            return n;
        }

        public static uint GetUIntAtIndexOfMaxLength(BitArray state, int index, int maxLength)
        {
            int len = Mathf.Min(maxLength + index, state.Count, 32 + index);
            uint n = 0;
            for (int i = index; i < len; i++)
            {
                if (state.Get(i))
                    n |= (uint)(1 << i);
            }
            return n;
        }


        public static uint GetFirstUIntFromByteArray(BitArray state)
        {
            int len = Math.Min(32, state.Count);
            uint n = 0;
            for (byte i = 0; i < len; i++)
            {
                if (state.Get(i))
                    n |= (uint)(1 << i);
            }
            return n;
        }


        public static byte GetByteOfMaxLengthStartingAtIndex(BitArray state, int index, int maxByteLength) // Gets the byte that starts at bit Index (ex GetByteStartingAtIndex(0b1001000111111111, 8) -> 0b10010001
        {
            return GetFirstOfMaxLengthFromByteArray(state.RightShift(index).And(ByteMaskArray), maxByteLength);
        }

        public static byte GetByteStartingAtIndex(BitArray state, int index) // Gets the byte that starts at bit Index (ex GetByteStartingAtIndex(0b1001000111111111, 8) -> 0b10010001
        {
			return GetFirstByteFromByteArray(state.RightShift(index).And(ByteMaskArray));
		}

		public static ushort GetShortStartingAtIndex(BitArray state, int index)
		{
            return GetFirstUShortFromByteArray(state.RightShift(index).And(ShortMaskArray));
        }

        public static uint GetIntStartingAtIndex(BitArray state, int index)
        {
            return GetFirstUIntFromByteArray(state.RightShift(index).And(IntMaskArray));
        }

		public static byte[] GetByteArrayStartingAtIndex(BitArray state, int index, int length)
		{
			byte[] bytes = new byte[length];

			for (int i = 0; i <= length; i++)
			{
				bytes[i] = GetByteStartingAtIndex(state, index + (i << 3));
			}

			return bytes;
		}

        public static byte[] GetByteArrayOfMaxLengthStartingAtIndex(BitArray state, int index, int length, int maxByteLength)
        {
            byte[] bytes = new byte[length];

            for (int i = 0; i <= length; i++)
            {
                bytes[i] = GetByteOfMaxLengthStartingAtIndex(state, index + (i * maxByteLength), maxByteLength);
            }

            return bytes;
        }

        public static BitArray GetBitArrayOfMaxLengthStartingAtIndex(BitArray state, int index, int length)
        {
            BitArray bitArray = new BitArray(length);
            int len = Mathf.Min(length + index, state.Length);
            for(int i = index;i < len;i++)
            {
                bitArray[i - index] = state[i];
            }
            return bitArray;
        }


        public static void SetBitAtIndex(ref BitArray state, int index, bool value)
		{
			state.Set(index, value);
		}

        public static BitArray SetNBitsAtIndex(BitArray state, BitArray source, int index)
        {
            BitArray bitArray = new BitArray(Mathf.Max(state.Length, source.Length + index));
            for (int i = 0; i < state.Length; i++)
            {
                bitArray.Set(i, state[i]);
            }
            for (int i = 0; i < source.Length; i++)
            {
                bitArray.Set(index + i, source[i]);
            }
            return bitArray;
        }



        public static void SetByteAtIndexWithLimit(ref BitArray state, int index, byte value, byte maxbits)
		{
			byte max = Math.Min(maxbits, (byte)8);
			for (int i = 0; i < max; i++)
			{
				state.Set(index + i, ((value >> i) & 1) == 1);
			}
		}

        public static void SetByteAtIndexWithlimitNoReplace(ref BitArray state, int index, byte value, byte maxbits)
        {
            byte max = Math.Min(maxbits, (byte)8);
            for (int i = 0; i < max; i++)
            {
                bool set = ((value >> i) & 1) == 1;
                if (set) state.Set(index + i, true);
            }
        }


        public static void SetShortAtIndex(ref BitArray state, int index, ushort value)
        {
            for (int i = 0; i < 16; i++)
            {
                state.Set(index + i, ((value >> i) & 1) == 1);
            }
        }

        public static void SetIntAtIndex(ref BitArray state, int index, uint value)
        {
            for (int i = 0; i < 32; i++)
            {
                state.Set(index + i, ((value >> i) & 1) == 1);
            }
        }

		public static void SetByteArrayAtIndex (ref BitArray state, int index, byte[] value)
		{
			for(int i = 0; i < value.Length; i++)
			{
				SetByteAtIndexWithLimit(ref state, index + (i<<3), value[i], 8);
			}
		}

        public static void SetByteArrayAtIndexWithLimit(ref BitArray state, int index, byte[] value, byte limit)
        {
            for (int i = 0; i < value.Length; i++)
            {
                SetByteAtIndexWithLimit(ref state, index + (i * limit), value[i], limit);
            }
        }

        public static void MergeManyToOne(ref BitArray target, BitArray[] source)
		{
			BitArray tValue = new BitArray(target.Length);
            BitArray tTristate = GetTristateFlags(target);
            int sourceLength = source[0].Length >> 1; // Same one everywhere
            for (int i = 0; i < source.Length; i++) {
                tValue = tValue.Or(GetBitStates(source[i]).LeftShift(i*sourceLength));
                tTristate = tTristate.Or(GetTristateFlags(source[i]).LeftShift(i * sourceLength));

            }
            Set(ref target, tValue, tTristate);
        }

		public static void SplitOneToMany(ref BitArray[] target, BitArray source)
		{
			int sourceLength = source.Length >> 1; // Divide by two (because half the array is dedicated to tristate)
			int targetArrayCount = target.Length; // Take the amount of bitarrays.
            int bitArrayLength = target[0].Length >> 1; //Same for any one.
			int sizeofvaluebytearray = bitArrayLength / 8 + (bitArrayLength % 8 == 0 ? 0 : 1);

            for (int i = 0; i <= targetArrayCount; i++)
			{
				byte[] valueByteArray = new byte[sizeofvaluebytearray];
				byte[] tristateArray = new byte[sizeofvaluebytearray];

				valueByteArray = GetByteArrayOfMaxLengthStartingAtIndex(source, i * sizeofvaluebytearray * bitArrayLength, valueByteArray.Length, bitArrayLength);
				SetByteArrayAtIndexWithLimit(ref target[i], 0, valueByteArray, (byte)bitArrayLength);

                tristateArray = GetByteArrayOfMaxLengthStartingAtIndex(source, sourceLength + i * sizeofvaluebytearray * bitArrayLength, valueByteArray.Length, bitArrayLength);
                SetByteArrayAtIndexWithLimit(ref target[i], bitArrayLength , valueByteArray, (byte)bitArrayLength);
			}
		}


		public static void Toggle(ref BitArray state, int bitIndex)
		{
            UnityEngine.Debug.Log("State length : " + state.Length + "\n BitIndex : " + bitIndex);
			BitArray bitStates = GetBitStates(state);
            bitStates.Set(bitIndex, !bitStates[bitIndex]);
            UnityEngine.Debug.Log("bitstate" + bitStates.Get(0));

			// Clear tristate flags (can't be disconnected if toggling as only input dev pins are allowed)
			Set(ref state, bitStates, FalseBitArray(bitStates.Length));
            UnityEngine.Debug.Log("state" + state.Get(0));

        }

        public static void SetAllDisconnected(ref BitArray state, int length) 
        {
            if(state == null) { state = new BitArray(length); }
            BitArray bitStates = new BitArray(state.Length >> 1);
            bitStates.SetAll(false);
            BitArray tristates = new BitArray(state.Length >> 1);
            tristates.SetAll(true);


            Set(ref state, bitStates, tristates);
        }
	}
}