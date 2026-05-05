// Copyright (C) 2015-2026 The Neo Project.
//
// ApplicationEngine.OpCodePricesV1.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM;
using Neo.VM.Types;
using System;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        public const int OpcodePriceMultiplier = 1000;

        // Dynamic fee weights
        private static readonly long[] AppendW = { 97, 192, 2715 };
        private static readonly long[] CatW = { 8, 2706 };
        private static readonly long[] ClearW = { 93, -15, 1515 };
        private static readonly long[] ClearItemsW = { 103, 1455 };
        private static readonly long[] ConvertArrOrStructW = { 318, 3435 };
        private static readonly long[] ConvertByteArrOrBufW = { 7, 3417 };
        private static readonly long[] DropW = { 99, 1486 };
        private static readonly long[] DupW = { 7, 3142 };
        private static readonly long[] HasKeyW = { 99, 2575 };
        private static readonly long[] InitSlotW = { 155, 2156 };
        private static readonly long[] IsNullW = { 97, 1236 };
        private static readonly long[] IsTypeW = { 97, 1154 };
        private static readonly long[] KeysW = { 1419, 2606 };
        private static readonly long[] MemcpyW = { 1, 3514 };
        private static readonly long[] NewArrayAnyW = { 126, 2718 };
        private static readonly long[] NewArrayByteOrIntW = { 849, 2718 };
        private static readonly long[] NewBufferW = { 6, 2507 };
        private static readonly long[] PackW = { 173, 2649 };
        private static readonly long[] PackMapW = { 80, 5281, 3481 };
        private static readonly long[] PickItemW = { 91, 6, 2751 };
        private static readonly long[] PopItemW = { 91, 3078 };
        private static readonly long[] RemoveArrOrStructW = { 98, 9, 1991 };
        private static readonly long[] RemoveMapW = { 96, 706, 6776 };
        private static readonly long[] ReverseItemsArrW = { 98, 19, 2043 };
        private static readonly long[] ReverseItemsBufW = { 9, 1690 };
        private static readonly long[] ReverseW = { 19, 1702 };
        private static readonly long[] RollW = { 5, 1910 };
        private static readonly long[] SetitemW = { 99, 350, 2942 };
        private static readonly long[] SizeW = { 100, 2693 };
        private static readonly long[] StW = { 98, 1599 };
        private static readonly long[] SubstrW = { 7, 2908 };
        private static readonly long[] UnpackW = { 254, 2604 };
        private static readonly long[] ValuesW = { 307, 369, 9868 };
        private static readonly long[] XDropW = { 98, 6, 1791 };

        private static readonly long[] StaticCoefficients;

        /// <summary>
        /// Gets the price for an opcode since Gorgon hardfork.
        /// </summary>
        /// <param name="baseFee">The base execution fee in datoshi.</param>
        /// <param name="opcode">The opcode.</param>
        /// <param name="param">The price parameters.</param>
        /// <returns>The price in picoGAS.</returns>
        public static long OpcodeV1(long baseFee, OpCode opcode, OpCodePriceParams param)
        {
            long price = opcode switch
            {
                OpCode.APPEND => AppendGas(param),
                OpCode.CAT => CatGas(param),
                OpCode.CLEAR => ClearGas(param),
                OpCode.CLEARITEMS => ClearItemsGas(param),
                OpCode.CONVERT => ConvertGas(param),
                OpCode.DROP => DropGas(param),
                OpCode.DUP => DupGas(param),
                OpCode.HASKEY => HasKeyGas(param),
                OpCode.INITSLOT => InitSlotGas(param),
                OpCode.ISNULL => IsNullGas(param),
                OpCode.ISTYPE => IsTypeGas(param),
                OpCode.KEYS => KeysGas(param),
                OpCode.LEFT => SubstrGas(param),
                OpCode.MEMCPY => MemcpyGas(param),
                OpCode.NEWARRAY => NewArrayGas(param),
                OpCode.NEWARRAY_T => NewArrayGas(param),
                OpCode.NEWBUFFER => NewBufferGas(param),
                OpCode.NEWSTRUCT => NewArrayGas(param),
                OpCode.NIP => DropGas(param),
                OpCode.OVER => DupGas(param),
                OpCode.PACK => PackGas(param),
                OpCode.PACKMAP => PackMapGas(param),
                OpCode.PACKSTRUCT => PackGas(param),
                OpCode.PICK => DupGas(param),
                OpCode.PICKITEM => PickItemGas(param),
                OpCode.POPITEM => PopItemGas(param),
                OpCode.REMOVE => RemoveGas(param),
                OpCode.REVERSEITEMS => ReverseItemsGas(param),
                OpCode.REVERSE3 => ReverseGas(param),
                OpCode.REVERSE4 => ReverseGas(param),
                OpCode.REVERSEN => ReverseGas(param),
                OpCode.RIGHT => SubstrGas(param),
                OpCode.ROLL => RollGas(param),
                OpCode.ROT => RollGas(param),
                OpCode.SETITEM => SetItemGas(param),
                OpCode.SIZE => SizeGas(param),
                OpCode.STSFLD0 => StGas(param),
                OpCode.STSFLD1 => StGas(param),
                OpCode.STSFLD2 => StGas(param),
                OpCode.STSFLD3 => StGas(param),
                OpCode.STSFLD4 => StGas(param),
                OpCode.STSFLD5 => StGas(param),
                OpCode.STSFLD6 => StGas(param),
                OpCode.STSFLD => StGas(param),
                OpCode.STLOC0 => StGas(param),
                OpCode.STLOC1 => StGas(param),
                OpCode.STLOC2 => StGas(param),
                OpCode.STLOC3 => StGas(param),
                OpCode.STLOC4 => StGas(param),
                OpCode.STLOC5 => StGas(param),
                OpCode.STLOC6 => StGas(param),
                OpCode.STLOC => StGas(param),
                OpCode.STARG0 => StGas(param),
                OpCode.STARG1 => StGas(param),
                OpCode.STARG2 => StGas(param),
                OpCode.STARG3 => StGas(param),
                OpCode.STARG4 => StGas(param),
                OpCode.STARG5 => StGas(param),
                OpCode.STARG6 => StGas(param),
                OpCode.STARG => StGas(param),
                OpCode.SUBSTR => SubstrGas(param),
                OpCode.UNPACK => UnpackGas(param),
                OpCode.VALUES => ValuesGas(param),
                OpCode.XDROP => XDropGas(param),
                _ => StaticCoefficients[(byte)opcode],
            };

            return baseFee * Math.Max(1, price);
        }

        private static long AppendGas(OpCodePriceParams args) => AppendW[0] * args.RefsDelta + AppendW[1] * args.NClonedItems + AppendW[2];
        private static long CatGas(OpCodePriceParams args) => CatW[0] * args.Length + CatW[1];
        private static long ClearGas(OpCodePriceParams args) => ClearW[0] * args.RefsDelta + ClearW[1] * args.Length + ClearW[2];
        private static long ClearItemsGas(OpCodePriceParams args) => ClearItemsW[0] * args.RefsDelta + ClearItemsW[1];
        private static long ConvertGas(OpCodePriceParams args) => args.Type == StackItemType.Array || args.Type == StackItemType.Struct ? ConvertArrOrStructW[0] * args.Length + ConvertArrOrStructW[1] : ConvertByteArrOrBufW[0] * args.Length + ConvertByteArrOrBufW[1];
        private static long DropGas(OpCodePriceParams args) => DropW[0] * args.RefsDelta + DropW[1];
        private static long DupGas(OpCodePriceParams args) => DupW[0] * args.Length + DupW[1];
        private static long HasKeyGas(OpCodePriceParams args) => HasKeyW[0] * args.RefsDelta + HasKeyW[1];
        private static long InitSlotGas(OpCodePriceParams args) => InitSlotW[0] * args.Length + InitSlotW[1];
        private static long IsNullGas(OpCodePriceParams args) => IsNullW[0] * args.Length + IsNullW[1];
        private static long IsTypeGas(OpCodePriceParams args) => IsTypeW[0] * args.Length + IsTypeW[1];
        private static long KeysGas(OpCodePriceParams args) => KeysW[0] * args.Length + KeysW[1];
        private static long MemcpyGas(OpCodePriceParams args) => MemcpyW[0] * args.Length + MemcpyW[1];
        private static long NewArrayGas(OpCodePriceParams args) => (args.Type == StackItemType.ByteString || args.Type == StackItemType.Integer) ? NewArrayByteOrIntW[0] * args.Length + NewArrayByteOrIntW[1] : NewArrayAnyW[0] * args.Length + NewArrayAnyW[1];
        private static long NewBufferGas(OpCodePriceParams args) => NewBufferW[0] * args.Length + NewBufferW[1];
        private static long PackGas(OpCodePriceParams args) => PackW[0] * args.Length + PackW[1];
        private static long PackMapGas(OpCodePriceParams args) => PackMapW[0] * args.RefsDelta + PackMapW[1] * args.Length + PackMapW[2];
        private static long PickItemGas(OpCodePriceParams args) => PickItemW[0] * args.RefsDelta + PickItemW[1] * args.Length + PickItemW[2];
        private static long PopItemGas(OpCodePriceParams args) => PopItemW[0] * args.RefsDelta + PopItemW[1];
        private static long RemoveGas(OpCodePriceParams args) => args.Type == StackItemType.Map ? RemoveMapW[0] * args.RefsDelta + RemoveMapW[1] * args.Length + RemoveMapW[2] : RemoveArrOrStructW[0] * args.RefsDelta + RemoveArrOrStructW[1] * args.Length + RemoveArrOrStructW[2];
        private static long ReverseItemsGas(OpCodePriceParams args) => args.Type == StackItemType.Buffer ? ReverseItemsBufW[0] * args.Length + ReverseItemsBufW[1] : ReverseItemsArrW[0] * args.RefsDelta + ReverseItemsArrW[1] * args.Length + ReverseItemsArrW[2];
        private static long ReverseGas(OpCodePriceParams args) => ReverseW[0] * args.Length + ReverseW[1];
        private static long RollGas(OpCodePriceParams args) => RollW[0] * args.Length + RollW[1];
        private static long SetItemGas(OpCodePriceParams args) => SetitemW[0] * args.RefsDelta + SetitemW[1] * args.NClonedItems + SetitemW[2];
        private static long SizeGas(OpCodePriceParams args) => SizeW[0] * args.RefsDelta + SizeW[1];
        private static long StGas(OpCodePriceParams args) => StW[0] * args.RefsDelta + StW[1];
        private static long SubstrGas(OpCodePriceParams args) => SubstrW[0] * args.Length + SubstrW[1];
        private static long UnpackGas(OpCodePriceParams args) => UnpackW[0] * args.Length + UnpackW[1];
        private static long ValuesGas(OpCodePriceParams args) => ValuesW[0] * args.Length + ValuesW[1] * args.NClonedItems + ValuesW[2];
        private static long XDropGas(OpCodePriceParams args) => XDropW[0] * args.RefsDelta + XDropW[1] * args.Length + XDropW[2];
    }
}
