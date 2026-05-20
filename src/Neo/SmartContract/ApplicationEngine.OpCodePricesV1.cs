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
        private static readonly long[] AssertW = { 99, 9, 1847 };
        private static readonly long[] CatW = { 8, 2706 };
        private static readonly long[] ClearW = { 93, -15, 1515 };
        private static readonly long[] ClearItemsW = { 103, 1455 };
        private static readonly long[] ConvertAnyW = { 78, 1610 };
        private static readonly long[] ConvertArrOrStructW = { 77, 134, 3218 };
        private static readonly long[] ConvertByteArrOrBufW = { 7, 3417 };
        private static readonly long[] DropW = { 99, 1486 };
        private static readonly long[] HasKeyW = { 99, 2575 };
        private static readonly long[] InitSlotW = { 67, 160, 1702 };
        private static readonly long[] IsNullW = { 97, 1236 };
        private static readonly long[] IsTypeW = { 97, 1154 };
        private static readonly long[] KeysW = { 1419, 2606 };
        private static readonly long[] MemcpyW = { 1, 3514 };
        private static readonly long[] NewArrayAnyW = { 126, 2718 };
        private static readonly long[] NewArrayByteOrIntW = { 134, 2718 };
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
        private static readonly long[] ThrowW = { 84, 1742 };
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
        public static long OpcodeV1(long baseFee, OpCode opcode, RunStats param)
        {
            long price = opcode switch
            {
                OpCode.APPEND => AppendGas(param),
                OpCode.ASSERT => AssertGas(param),
                OpCode.ASSERTMSG => AssertGas(param),
                OpCode.CAT => CatGas(param),
                OpCode.CLEAR => ClearGas(param),
                OpCode.CLEARITEMS => ClearItemsGas(param),
                OpCode.CONVERT => ConvertGas(param),
                OpCode.DROP => DropGas(param),
                OpCode.HASKEY => HasKeyGas(param),
                OpCode.INITSLOT => InitSlotGas(param),
                OpCode.INITSSLOT => InitSlotGas(param),
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
                OpCode.PACK => PackGas(param),
                OpCode.PACKMAP => PackMapGas(param),
                OpCode.PACKSTRUCT => PackGas(param),
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
                OpCode.THROW => ThrowGas(param),
                OpCode.UNPACK => UnpackGas(param),
                OpCode.VALUES => ValuesGas(param),
                OpCode.XDROP => XDropGas(param),
                _ => StaticCoefficients[(byte)opcode],
            };

            return baseFee * price;
        }

        private static long AppendGas(RunStats args) => AppendW[0] * args.RefsDelta + AppendW[1] * args.NClonedItems + AppendW[2];
        private static long AssertGas(RunStats args) => AssertW[0] * args.RefsDelta + AssertW[1] * args.Length + AssertW[2];
        private static long CatGas(RunStats args) => CatW[0] * args.Length + CatW[1];
        private static long ClearGas(RunStats args) => ClearW[0] * args.RefsDelta + ClearW[1] * args.Length + ClearW[2];
        private static long ClearItemsGas(RunStats args) => ClearItemsW[0] * args.RefsDelta + ClearItemsW[1];
        private static long ConvertGas(RunStats args) => args.Type switch
        {
            StackItemType.Any => ConvertAnyW[0] * args.RefsDelta + ConvertAnyW[1],
            StackItemType.Array => ConvertArrOrStructW[0] * args.RefsDelta + ConvertArrOrStructW[1] * args.Length + ConvertArrOrStructW[2],
            StackItemType.ByteString => ConvertByteArrOrBufW[0] * args.Length + ConvertByteArrOrBufW[1],
            _ => throw new InvalidOperationException($"Unsupported type {args.Type} for {OpCode.CONVERT} dynamic pricing."),
        };
        private static long DropGas(RunStats args) => DropW[0] * args.RefsDelta + DropW[1];
        private static long HasKeyGas(RunStats args) => HasKeyW[0] * args.RefsDelta + HasKeyW[1];
        private static long InitSlotGas(RunStats args) => InitSlotW[0] * args.RefsDelta + InitSlotW[1] * args.Length + InitSlotW[2];
        private static long IsNullGas(RunStats args) => IsNullW[0] * args.Length + IsNullW[1];
        private static long IsTypeGas(RunStats args) => IsTypeW[0] * args.Length + IsTypeW[1];
        private static long KeysGas(RunStats args) => KeysW[0] * args.Length + KeysW[1];
        private static long MemcpyGas(RunStats args) => MemcpyW[0] * args.Length + MemcpyW[1];
        private static long NewArrayGas(RunStats args) => (args.Type == StackItemType.ByteString || args.Type == StackItemType.Integer) ? NewArrayByteOrIntW[0] * args.Length + NewArrayByteOrIntW[1] : NewArrayAnyW[0] * args.Length + NewArrayAnyW[1];
        private static long NewBufferGas(RunStats args) => NewBufferW[0] * args.Length + NewBufferW[1];
        private static long PackGas(RunStats args) => PackW[0] * args.Length + PackW[1];
        private static long PackMapGas(RunStats args) => PackMapW[0] * args.RefsDelta + PackMapW[1] * args.Length + PackMapW[2];
        private static long PickItemGas(RunStats args) => PickItemW[0] * args.RefsDelta + PickItemW[1] * args.Length + PickItemW[2];
        private static long PopItemGas(RunStats args) => PopItemW[0] * args.RefsDelta + PopItemW[1];
        private static long RemoveGas(RunStats args) => args.Type == StackItemType.Map ? RemoveMapW[0] * args.RefsDelta + RemoveMapW[1] * args.Length + RemoveMapW[2] : RemoveArrOrStructW[0] * args.RefsDelta + RemoveArrOrStructW[1] * args.Length + RemoveArrOrStructW[2];
        private static long ReverseItemsGas(RunStats args) => args.Type == StackItemType.Buffer ? ReverseItemsBufW[0] * args.Length + ReverseItemsBufW[1] : ReverseItemsArrW[0] * args.RefsDelta + ReverseItemsArrW[1] * args.Length + ReverseItemsArrW[2];
        private static long ReverseGas(RunStats args) => ReverseW[0] * args.Length + ReverseW[1];
        private static long RollGas(RunStats args) => RollW[0] * args.Length + RollW[1];
        private static long SetItemGas(RunStats args) => SetitemW[0] * args.RefsDelta + SetitemW[1] * args.NClonedItems + SetitemW[2];
        private static long SizeGas(RunStats args) => SizeW[0] * args.RefsDelta + SizeW[1];
        private static long StGas(RunStats args) => StW[0] * args.RefsDelta + StW[1];
        private static long SubstrGas(RunStats args) => SubstrW[0] * args.Length + SubstrW[1];
        private static long ThrowGas(RunStats args) => ThrowW[0] * args.RefsDelta + ThrowW[1];
        private static long UnpackGas(RunStats args) => UnpackW[0] * args.Length + UnpackW[1];
        private static long ValuesGas(RunStats args) => ValuesW[0] * args.Length + ValuesW[1] * args.NClonedItems + ValuesW[2];
        private static long XDropGas(RunStats args) => XDropW[0] * args.RefsDelta + XDropW[1] * args.Length + XDropW[2];
    }
}
