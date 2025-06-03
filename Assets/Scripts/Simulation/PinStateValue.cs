using System;
using System.Collections;
using System.Collections.Specialized;
using DLS.Description;
using Seb.Helpers;
using UnityEngine;

namespace DLS.Simulation
{
    public struct PinStateValue
    {
        public const uint LOGIC_LOW = 0;
        public const uint LOGIC_HIGH = 1;
        public const uint LOGIC_DISCONNECTED = 2;


        public uint singleBit; // ONLY USE FOR 1 BIT

        public BitVector32 a; // If bitcount <= 16 : use only this -- FASTEST 
        public static BitVector32.Section valueSection = BitVector32.CreateSection(short.MaxValue); // Only in use on less than 16
        public static BitVector32.Section tristateSection = BitVector32.CreateSection(short.MaxValue, valueSection); // this too

        public BitVector32 b; // If 16 < bitcount <=32 : use this too -- medium
        public BitArray BigValues; // If bitcount >= 32 use this INSTEAD  -- SLOWEST
        public BitArray BigTristates; // Goes with it
        public ushort size;

        const short maxSigned16bitValue = 0x7FFF;
        public void MakeFromPinBitCount(PinBitCount pinBitCount)
        {
            size = pinBitCount.BitCount;
            if (size <= 1)
            {
                singleBit = 0;
            }
            else if (size <= 16)
            {
                a = new BitVector32(0);
            }

            else if (size <= 32)
            {
                a = new BitVector32(0);
                b = new BitVector32(0);
            }
            else
            {
                BigValues = new BitArray(size);
                BigTristates = new BitArray(size);
            }
        }


        public void MakeFromAnother(PinStateValue source)
        {
            CopyFrom(source);
        }
        
        public bool SmallHigh()
        {
            return (singleBit & 1) == 1;
        }
        
        public void SetAllDisconnected()
        {
            if(size == 1)
            {
                singleBit = 2;
                return;
            }

            if(size <= 16)
            {
                a[valueSection] = 0;
                a[tristateSection] = -1;
                return;
            }

            if(size <= 32)
            {
                a = new BitVector32(0);
                b = new BitVector32(-1);
                return;
            }

            BigValues = new BitArray(size);
            BigTristates = BitArrayHelper.TrueBitArray(size);
        }

        public void SmallSet(uint value)
        {
            singleBit = value;
        }

        public void SmallToggle()
        {
            singleBit ^= 1;
        }

        public bool FirstBitHigh()
        {
            if (size <= 32) { return (a.Data & 1) == 1; }
            else return BigValues.Get(0);
        }
        public void ToggleBit(int index)
        {
            UnityEngine.Debug.Log("Index: " + index);
            if (size == 1) { SmallToggle(); }
            else if (size <= 16) { a[valueSection] = (a[valueSection]) ^ (1 << index); }
            else if (size <= 32) { a = new BitVector32(a.Data ^ (1 << index)); }
            else if (size >32) BigValues.Set(index, !BigValues.Get(index));
        }

        public void SetFirstBit(bool firstBit)
        {
            if (size == 1) { SmallSet((uint)(firstBit ? 1 : 0)); }
            else if (size <= 32) {
                a[valueSection] = firstBit ? a[valueSection] | 1 : a[valueSection] ^ 1;
            }
            else BigValues.Set(0, firstBit);
        }

        public void SetShortValue(ushort value)
        {
            a[valueSection] = value;
        }

        public void SetMediumValue(uint value)
        {
            a = new BitVector32((int)value);
        }
        
        public void SetShort(uint valueAndFlags)
        {
            a = new BitVector32((int)valueAndFlags);
        }

        public void SetMedium(uint value, uint tristateFlags)
        {
            a = new BitVector32((int)value);
            b = new BitVector32((int)tristateFlags);
        }
        public uint GetShortValues()
        {
            return (uint)a[valueSection];
        }

        public uint GetMediumValues()
        {
            return (uint)a.Data;
        }

        public uint GetShortTristate()
        {
            return (uint)a[tristateSection];
        }

        public uint GetMediumTristate()
        {
            return (uint)b.Data;
        }
        
        public uint GetValue()
        {
            if(size == 1) { return (uint)(SmallHigh() ? 1 : 0); }
            if(size <=16) { return GetShortValues(); }
            if(size <=32) { return GetMediumValues(); }
            return BitArrayHelper.GetFirstUIntFromByteArray(BigValues);
        }


        public ushort GetSmallTristate()
        {
            return (ushort)(singleBit & 1 );
        }
        public ushort GetTristatedValue(int index)
        {
            if(size == 1)
            {
                return GetSmallTristate();
            }

            else if (size <= 16)
            {
                return GetShortBitTristatedValue(index);
            }

            else if (size <= 32)
            {
                return GetMediumBitTristatedValue(index);
            }
            else
            {
                return GetBigBitTristatedValue(index);
            }
        }

        ushort GetShortBitTristatedValue(int index)
        {
            return (ushort)((GetShortValues() >> index) & 1 | ((GetShortTristate() >> index & 1) << 1));
        }


        ushort GetMediumBitTristatedValue(int index)
        {
            return (ushort)((GetMediumValues() >> index) & 1 | (GetMediumTristate() >> index & 1) << 1);
        }

        ushort GetBigBitTristatedValue(int index)
        {
            return (ushort)(BigValues.Get(index)?1:BigTristates.Get(index)?2:0);
        }

        public uint GetTristatedFlags()
        {
            if (size == 1) { return (uint)(singleBit >> 1); }
            if (size <=16) { return (uint)a[tristateSection]; }
            if (size <= 32) { return (uint)b.Data; }
            return BitArrayHelper.GetFirstUIntFromByteArray(BigTristates);
        }

        public void CopyFrom(PinStateValue pinStateValue)
        {
            size = pinStateValue.size;
            if(size == 1) {SmallCopyFrom(pinStateValue); }
            else if (size <= 16) { ShortCopyFrom(pinStateValue); }
            else if (size <= 32) {MediumCopyFrom(pinStateValue); }
            else {BigCopyFrom(pinStateValue); }
        }

        void SmallCopyFrom(PinStateValue pinStateValue)
        {
            singleBit = pinStateValue.singleBit;
        }

        void ShortCopyFrom(PinStateValue pinStateValue)
        {
            a = pinStateValue.a;
        }

        void MediumCopyFrom(PinStateValue pinStateValue)
        {
            a = pinStateValue.a;
            b = pinStateValue.b;
        }

        void BigCopyFrom(PinStateValue pinStateValue)
        {
            BigValues = new BitArray(pinStateValue.BigValues);
            BigTristates = new BitArray(pinStateValue.BigTristates);
        }

        public uint OR(PinStateValue pinStateValue)
        {
            if (size == 1) { return (pinStateValue.singleBit | singleBit); }
            else if (size <= 16) { return pinStateValue.GetShortValues() | GetShortValues(); }
            else if (size <= 32) { return pinStateValue.GetMediumValues() | GetMediumValues(); }
            return 0;
        }

        public void SetAsOr(PinStateValue pinStateValue)
        {
            if (size == 1) { singleBit |= pinStateValue.singleBit; }
            else if (size <= 16) { a[valueSection] |= (int)pinStateValue.GetShortValues(); }
            else if (size <= 32) { SetMediumValue(((uint)a.Data | pinStateValue.GetMediumValues())); }
            else { BigValues.Or(pinStateValue.BigValues); }

        }

        public void SetAsAnd(PinStateValue pinStateValue)
        {
            if (size == 1) { singleBit &= pinStateValue.singleBit; }
            else if (size <= 16) { a[valueSection] &= (int)pinStateValue.GetShortValues(); }
            else if (size <= 32) { SetMediumValue(((uint)a.Data & pinStateValue.GetMediumValues())); }
            else { BigValues.And(pinStateValue.BigValues); }
        }

        public void SetAsRightShift(int shift)
        {
            if (shift <= 0) { return; }
            if (size == 1) { singleBit = 0; }
            else if (size <= 16) { a[valueSection] >>= shift; }
            else if (size <= 32) { SetMediumValue((uint)(a.Data >> shift)); }
            else { BigValues.RightShift(shift); }
        }

        public void SetAsLeftShift(int shift)
        {
            if (shift <= 0) { return; }
            if (size == 1) { singleBit = 0; }
            else if (size <= 16) { a[valueSection] <<= shift; }
            else if (size <= 32) { SetMediumValue((uint)(a.Data << shift)); }
            else { BigValues.LeftShift(shift); }
        }

        internal void SetShortValue(uint v)
        {
            throw new NotImplementedException();
        }
    }
}