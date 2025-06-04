using System;
using System.Collections;
using System.Collections.Specialized;
using DLS.Description;
using Seb.Helpers;
using UnityEngine;
using static UnityEngine.Random;

namespace DLS.Simulation
{
    public struct PinStateValue
    {
        public const uint LOGIC_LOW = 0;
        public const uint LOGIC_HIGH = 1;
        public const uint LOGIC_DISCONNECTED = 2;

        public uint a; // If bitcount <= 16 : use only this -- FASTEST 

        public BitVector32 b; // If 16 < bitcount <=32 : use this too -- medium
        public BitArray BigValues; // If bitcount >= 32 use this INSTEAD  -- SLOWEST
        public BitArray BigTristates; // Goes with it
        public ushort size;

        const short maxSigned16bitValue = 0x7FFF;
        public void MakeFromPinBitCount(PinBitCount pinBitCount)
        {
            size = pinBitCount.BitCount;
            if (size <= 16)
            {
                a = 0;
            }
            else if (size <= 32)
            {
                a = 0;
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

        public void MakeFromValueAndFlags(PinBitCount pinBitCount, uint values, uint flags)
        {
            size = pinBitCount.BitCount;

            if (size <= 16)
            {
                SetShortTristateAndValue((ushort)value, (ushort)flags);
            }
            else if (size <= 32) {
                SetMedium(values, flags);
            }
            else { throw new Exception("ArgumentsException : PinBitCount should be smaller or equal to 32 bits for this operation."); }
        }
        
        public bool SmallHigh()
        {
            return (a & 1) == 1;
        }
        
        public void SetAllDisconnected()
        {
            if(size == 1)
            {
                a = 2;
                return;
            }

            if(size <= 16)
            {
                a = 0xFFFF0000;
                return;
            }

            if(size <= 32)
            {
                a = 0;
                b = new BitVector32(-1);
                return;
            }

            BigValues = new BitArray(size);
            BigTristates = BitArrayHelper.TrueBitArray(size);
        }

        public void SmallSet(uint value)
        {
            a = value;
        }

        public void SmallToggle()
        {
            a ^= 1;
        }

        public bool FirstBitHigh()
        {
            if (size <= 32) { return (a & 1) == 1; }
            else return BigValues.Get(0);
        }
        public void ToggleBit(int index)
        {
            if (size == 1) { SmallToggle(); }
            else if (size <= 32) { a ^= (uint)(1 << index); }
            else if (size > 32) BigValues.Set(index, !BigValues.Get(index));
        }

        public void SetFirstBit(bool firstBit)
        {
            if (size == 1) { SmallSet((uint)(firstBit ? 1 : 0)); }
            else if (size <= 32) {
                a = firstBit ? a | 1 : a ^ 1;
            }
            else BigValues.Set(0, firstBit);
        }

        public void SetShortValue(ushort value)
        {
            a = value | (a & 0xFFFF0000);
        }

        public void SetShortTristateAndValue(ushort tristate, ushort value)
        {
            a = (uint)(value | tristate << 16);
        }

        public void SetMediumValue(uint value)
        {
            a = value;
        }
        
        public void SetShort(uint valueAndFlags)
        {
            a = valueAndFlags;
        }

        public void SetMedium(uint value, uint tristateFlags)
        {
            a = value;
            b = new BitVector32((int)tristateFlags);
        }
        public uint GetShortValues()
        {
            return a & 0xFFFF;
        }

        public uint GetMediumValues()
        {
            return a;
        }

        public uint GetShortTristate()
        {
            return (a & 0xFFFF0000)>>16;
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


        public ushort GetSmallTristatedValue()
        {
            return (ushort)a;
        }

        public ushort GetTristatedValue(int index)
        {
            if(size == 1)
            {
                return GetSmallTristatedValue();
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
            return (ushort)(((GetShortValues() >> index) & 1) | (((GetShortTristate() >> index) & 1) << 1));
        }


        ushort GetMediumBitTristatedValue(int index)
        {
            return (ushort)((a >> index) & 1 | (GetMediumTristate() >> index & 1) << 1);
        }

        ushort GetBigBitTristatedValue(int index)
        {
            return (ushort)(BigValues.Get(index)?1:BigTristates.Get(index)?2:0);
        }

        public uint GetTristatedFlags()
        {
            if (size == 1) { return (uint)(a >> 1); }
            if (size <=16) { return a & 0xFFFF0000; }
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
            a = pinStateValue.a;
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
            if (size == 1) { return (pinStateValue.a | a); }
            else if (size <= 16) { return pinStateValue.GetShortValues() | GetShortValues(); }
            else if (size <= 32) { return pinStateValue.GetMediumValues() | GetMediumValues(); }
            return 0;
        }

        public void SetAsOr(PinStateValue pinStateValue)
        {
            if (size == 1) { a |= pinStateValue.a; }
            else if (size <= 16) { SetShortValue((ushort)pinStateValue.GetShortValues()); }
            else if (size <= 32) { SetMediumValue(a | pinStateValue.GetMediumValues()); }
            else { BigValues.Or(pinStateValue.BigValues); }

        }

        public void SetAsAnd(PinStateValue pinStateValue)
        {
            if (size == 1) { a &= pinStateValue.a; }
            else if (size <= 16) { SetShortValue((ushort)(a & pinStateValue.GetShortValues())); }
            else if (size <= 32) { SetMediumValue(a & pinStateValue.GetMediumValues()); }
            else { BigValues.And(pinStateValue.BigValues); }
        }

        public bool HandleConflictShort(PinStateValue other)
        {
            bool set;
            uint OR = a | other.a;
            uint AND = a & other.a;
            ushort bitsNew = (ushort)(Simulator.RandomBool() ? OR : AND);

            ushort mask = (ushort)(OR >> 16); // tristate flags
            bitsNew = (ushort)((bitsNew & ~mask) | ((ushort)OR & mask)); // can always accept input for tristated bits

            ushort tristateNew = (ushort)(AND >> 16);
            uint stateNew = (uint)(bitsNew | (tristateNew << 16));
            set = stateNew != a;

            SetShortTristateAndValue(tristateNew, bitsNew);

            return set;
        }

        public bool HandleConflictMedium(PinStateValue other)
        {
            bool set;
            (uint a, uint b) OR = (a | other.a, (uint)(b.Data | other.b.Data)) ;
            (uint a, uint b) AND = (a & other.a, (uint)(b.Data & other.b.Data));
            uint bitsNew = Simulator.RandomBool() ? OR.a : AND.a;

            bitsNew = (bitsNew & ~OR.b) | (OR.b);

            uint tristateNew = AND.b;

            set = bitsNew != a && (tristateNew != b.Data);

            a = bitsNew;
            b = new BitVector32((int)tristateNew);

            return set;
        }

        public bool HandleConflictBig(PinStateValue other) {
            bool set;

            (BitArray a, BitArray b) OR = (BitArrayHelper.NonMutativeOR(BigValues, other.BigValues), BitArrayHelper.NonMutativeOR(BigTristates, other.BigTristates)) ;
            (BitArray a, BitArray b) AND = (BitArrayHelper.NonMutativeAND(BigValues, other.BigValues), BitArrayHelper.NonMutativeAND(BigTristates, other.BigTristates));
            BitArray bitsNew = new BitArray(Simulator.RandomBool() ? OR.a : AND.a);

            bitsNew = BitArrayHelper.NonMutativeOR(
                BitArrayHelper.NonMutativeAND(bitsNew, BitArrayHelper.NonMutativeNOT(OR.b)),
                OR.b);

            BitArray tristatesNew = AND.b;
            set = !bitsNew.Equals(BigValues) && !tristatesNew.Equals(BigTristates);

            BigValues = bitsNew;
            BigTristates = tristatesNew;

            return set;
        }

        public bool HandleConflicts(PinStateValue other)
        {
            if (size <= 16)
            {
                return HandleConflictShort(other);
            }
            else if (size <= 32)
            {
                return HandleConflictMedium(other);
            }
            else
            {
                return HandleConflictBig(other);
            }
        }
    }
}