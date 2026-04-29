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
        /// <param name="args">The price arguments.</param>
        /// <returns>The price in picoGAS.</returns>
        public static long OpcodeV1(long baseFee, OpCode opcode, OpCodePriceParams? args)
        {
            long price = opcode switch
            {
                OpCode.APPEND => AppendGas(args!.Value),
                OpCode.CAT => CatGas(args!.Value),
                OpCode.CLEAR => ClearGas(args!.Value),
                OpCode.CLEARITEMS => ClearItemsGas(args!.Value),
                OpCode.CONVERT => ConvertGas(args!.Value),
                OpCode.DROP => DropGas(args!.Value),
                OpCode.DUP => DupGas(args!.Value),
                OpCode.HASKEY => HasKeyGas(args!.Value),
                OpCode.INITSLOT => InitSlotGas(args!.Value),
                OpCode.ISNULL => IsNullGas(args!.Value),
                OpCode.ISTYPE => IsTypeGas(args!.Value),
                OpCode.KEYS => KeysGas(args!.Value),
                OpCode.LEFT => SubstrGas(args!.Value),
                OpCode.MEMCPY => MemcpyGas(args!.Value),
                OpCode.NEWARRAY => NewArrayGas(args!.Value),
                OpCode.NEWARRAY_T => NewArrayGas(args!.Value),
                OpCode.NEWBUFFER => NewBufferGas(args!.Value),
                OpCode.NEWSTRUCT => NewArrayGas(args!.Value),
                OpCode.NIP => DropGas(args!.Value),
                OpCode.OVER => DupGas(args!.Value),
                OpCode.PACK => PackGas(args!.Value),
                OpCode.PACKMAP => PackMapGas(args!.Value),
                OpCode.PACKSTRUCT => PackGas(args!.Value),
                OpCode.PICK => DupGas(args!.Value),
                OpCode.PICKITEM => PickItemGas(args!.Value),
                OpCode.POPITEM => PopItemGas(args!.Value),
                OpCode.REMOVE => RemoveGas(args!.Value),
                OpCode.REVERSEITEMS => ReverseItemsGas(args!.Value),
                OpCode.REVERSE3 => ReverseGas(args!.Value),
                OpCode.REVERSE4 => ReverseGas(args!.Value),
                OpCode.REVERSEN => ReverseGas(args!.Value),
                OpCode.RIGHT => SubstrGas(args!.Value),
                OpCode.ROLL => RollGas(args!.Value),
                OpCode.ROT => RollGas(args!.Value),
                OpCode.SETITEM => SetItemGas(args!.Value),
                OpCode.SIZE => SizeGas(args!.Value),
                OpCode.STSFLD0 => StGas(args!.Value),
                OpCode.STSFLD1 => StGas(args!.Value),
                OpCode.STSFLD2 => StGas(args!.Value),
                OpCode.STSFLD3 => StGas(args!.Value),
                OpCode.STSFLD4 => StGas(args!.Value),
                OpCode.STSFLD5 => StGas(args!.Value),
                OpCode.STSFLD6 => StGas(args!.Value),
                OpCode.STSFLD => StGas(args!.Value),
                OpCode.STLOC0 => StGas(args!.Value),
                OpCode.STLOC1 => StGas(args!.Value),
                OpCode.STLOC2 => StGas(args!.Value),
                OpCode.STLOC3 => StGas(args!.Value),
                OpCode.STLOC4 => StGas(args!.Value),
                OpCode.STLOC5 => StGas(args!.Value),
                OpCode.STLOC6 => StGas(args!.Value),
                OpCode.STLOC => StGas(args!.Value),
                OpCode.STARG0 => StGas(args!.Value),
                OpCode.STARG1 => StGas(args!.Value),
                OpCode.STARG2 => StGas(args!.Value),
                OpCode.STARG3 => StGas(args!.Value),
                OpCode.STARG4 => StGas(args!.Value),
                OpCode.STARG5 => StGas(args!.Value),
                OpCode.STARG6 => StGas(args!.Value),
                OpCode.STARG => StGas(args!.Value),
                OpCode.SUBSTR => SubstrGas(args!.Value),
                OpCode.UNPACK => UnpackGas(args!.Value),
                OpCode.VALUES => ValuesGas(args!.Value),
                OpCode.XDROP => XDropGas(args!.Value),
                _ => StaticCoefficients[(byte)opcode],
            };

            return baseFee * Math.Max(1, price);
        }

        private static long StaticPrice(OpCode opcode)
        {
            return StaticCoefficients[(byte)opcode];
        }

        private static long DynamicPrice(OpCode opcode, OpCodePriceParams args)
        {
            return opcode switch
            {
                OpCode.APPEND => AppendGas(args),
                OpCode.CAT => CatGas(args),
                OpCode.CLEAR => ClearGas(args),
                OpCode.CLEARITEMS => ClearItemsGas(args),
                OpCode.CONVERT => ConvertGas(args),
                OpCode.DROP => DropGas(args),
                OpCode.DUP => DupGas(args),
                OpCode.HASKEY => HasKeyGas(args),
                OpCode.INITSLOT => InitSlotGas(args),
                OpCode.ISNULL => IsNullGas(args),
                OpCode.ISTYPE => IsTypeGas(args),
                OpCode.KEYS => KeysGas(args),
                OpCode.LEFT => SubstrGas(args),
                OpCode.MEMCPY => MemcpyGas(args),
                OpCode.NEWARRAY => NewArrayGas(args),
                OpCode.NEWARRAY_T => NewArrayGas(args),
                OpCode.NEWBUFFER => NewBufferGas(args),
                OpCode.NEWSTRUCT => NewArrayGas(args),
                OpCode.NIP => DropGas(args),
                OpCode.OVER => DupGas(args),
                OpCode.PACK => PackGas(args),
                OpCode.PACKMAP => PackMapGas(args),
                OpCode.PACKSTRUCT => PackGas(args),
                OpCode.PICK => DupGas(args),
                OpCode.PICKITEM => PickItemGas(args),
                OpCode.POPITEM => PopItemGas(args),
                OpCode.REMOVE => RemoveGas(args),
                OpCode.REVERSEITEMS => ReverseItemsGas(args),
                OpCode.REVERSE3 => ReverseGas(args),
                OpCode.REVERSE4 => ReverseGas(args),
                OpCode.REVERSEN => ReverseGas(args),
                OpCode.RIGHT => SubstrGas(args),
                OpCode.ROLL => RollGas(args),
                OpCode.ROT => RollGas(args),
                OpCode.SETITEM => SetItemGas(args),
                OpCode.SIZE => SizeGas(args),
                OpCode.STSFLD0 => StGas(args),
                OpCode.STSFLD1 => StGas(args),
                OpCode.STSFLD2 => StGas(args),
                OpCode.STSFLD3 => StGas(args),
                OpCode.STSFLD4 => StGas(args),
                OpCode.STSFLD5 => StGas(args),
                OpCode.STSFLD6 => StGas(args),
                OpCode.STSFLD => StGas(args),
                OpCode.STLOC0 => StGas(args),
                OpCode.STLOC1 => StGas(args),
                OpCode.STLOC2 => StGas(args),
                OpCode.STLOC3 => StGas(args),
                OpCode.STLOC4 => StGas(args),
                OpCode.STLOC5 => StGas(args),
                OpCode.STLOC6 => StGas(args),
                OpCode.STLOC => StGas(args),
                OpCode.STARG0 => StGas(args),
                OpCode.STARG1 => StGas(args),
                OpCode.STARG2 => StGas(args),
                OpCode.STARG3 => StGas(args),
                OpCode.STARG4 => StGas(args),
                OpCode.STARG5 => StGas(args),
                OpCode.STARG6 => StGas(args),
                OpCode.STARG => StGas(args),
                OpCode.SUBSTR => SubstrGas(args),
                OpCode.UNPACK => UnpackGas(args),
                OpCode.VALUES => ValuesGas(args),
                OpCode.XDROP => XDropGas(args),
                _ => StaticPrice(opcode), // Fallback
            };
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
