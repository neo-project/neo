// Copyright (C) 2015-2026 The Neo Project.
//
// Fee.cs file belongs to the neo project and is free
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
    /// <summary>
    /// Provides methods for calculating opcode prices.
    /// </summary>
    public static class Fee
    {
        public const int OpcodePriceMultiplier = 1000;

        // Dynamic fee weights
        private static readonly long[] AppendW = { 965, 1875, 44314 };
        private static readonly long[] CatW = { 76, 11886 };
        private static readonly long[] ClearW = { 928, -211, 30857 };
        private static readonly long[] ClearItemsW = { 897, 10072 };
        private static readonly long[] ConvertArrOrStructW = { 2921, 33000 };
        private static readonly long[] ConvertByteArrayW = { 52, 155282 };
        private static readonly long[] DropW = { 934, 19393 };
        private static readonly long[] DupW = { 50, 231714 };
        private static readonly long[] HasKeyW = { 392, 2109, 18063 };
        private static readonly long[] InitSlotW = { 4023, 13745 };
        private static readonly long[] KeysW = { 13948, 84697 };
        private static readonly long[] MemcpyW = { 6, 33312 };
        private static readonly long[] NewArrayAnyW = { 1881, 85312 };
        private static readonly long[] NewArrayByteOrIntW = { 8953, 38914 };
        private static readonly long[] NewBufferW = { 63, 17891 };
        private static readonly long[] PackW = { 1659, 75925 };
        private static readonly long[] PackMapW = { 1001, 2077, 66535 };
        private static readonly long[] PickItemAnyW = { 718, 2294, 840, 20461 };
        private static readonly long[] PickItemByteArrayW = { 765, 2315, 60, 16216 };
        private static readonly long[] PopItemW = { 825, 39018 };
        private static readonly long[] RemoveW = { 919, 2263, 26938 };
        private static readonly long[] ReverseItemsArrayW = { 393, 230, 14021 };
        private static readonly long[] ReverseItemsBufferW = { 92, 29431 };
        private static readonly long[] ReverseW = { 180, 23110 };
        private static readonly long[] RollW = { 55, 16920 };
        private static readonly long[] SetitemW = { 1038, 3587, 4087, 17736 };
        private static readonly long[] SizeW = { 927, 32486 };
        private static readonly long[] StW = { 825, 48954 };
        private static readonly long[] SubstrW = { 46, 187501 };
        private static readonly long[] UnpackArrayOrStructW = { 1179, 7114 };
        private static readonly long[] UnpackMapW = { 1026, 111184 };
        private static readonly long[] ValuesW = { 3016, 3680, 100441 };
        private static readonly long[] XDropW = { 946, 58, 15029 };

        private static readonly long[] NotScaledStaticCoefficients = CreateStaticCoefficients();

        private static readonly long[] StaticCoefficients = ScaleCoefficients(NotScaledStaticCoefficients, OpcodePriceMultiplier);

        private static long[] CreateStaticCoefficients()
        {
            long[] coefficients = new long[256];
            coefficients[(byte)OpCode.PUSHINT8] = 3;
            coefficients[(byte)OpCode.PUSHINT16] = 3;
            coefficients[(byte)OpCode.PUSHINT32] = 3;
            coefficients[(byte)OpCode.PUSHINT64] = 3;
            coefficients[(byte)OpCode.PUSHINT128] = 3;
            coefficients[(byte)OpCode.PUSHINT256] = 3;
            coefficients[(byte)OpCode.PUSHT] = 1;
            coefficients[(byte)OpCode.PUSHF] = 1;
            coefficients[(byte)OpCode.PUSHA] = 2;
            coefficients[(byte)OpCode.PUSHNULL] = 1;
            coefficients[(byte)OpCode.PUSHDATA1] = 2;
            coefficients[(byte)OpCode.PUSHDATA2] = 2;
            coefficients[(byte)OpCode.PUSHDATA4] = 2;
            coefficients[(byte)OpCode.PUSHM1] = 2;
            coefficients[(byte)OpCode.PUSH0] = 2;
            coefficients[(byte)OpCode.PUSH1] = 2;
            coefficients[(byte)OpCode.PUSH2] = 2;
            coefficients[(byte)OpCode.PUSH3] = 2;
            coefficients[(byte)OpCode.PUSH4] = 2;
            coefficients[(byte)OpCode.PUSH5] = 2;
            coefficients[(byte)OpCode.PUSH6] = 2;
            coefficients[(byte)OpCode.PUSH7] = 2;
            coefficients[(byte)OpCode.PUSH8] = 2;
            coefficients[(byte)OpCode.PUSH9] = 2;
            coefficients[(byte)OpCode.PUSH10] = 2;
            coefficients[(byte)OpCode.PUSH11] = 2;
            coefficients[(byte)OpCode.PUSH12] = 2;
            coefficients[(byte)OpCode.PUSH13] = 2;
            coefficients[(byte)OpCode.PUSH14] = 2;
            coefficients[(byte)OpCode.PUSH15] = 2;
            coefficients[(byte)OpCode.PUSH16] = 2;
            coefficients[(byte)OpCode.NOP] = 1;
            coefficients[(byte)OpCode.JMP] = 1;
            coefficients[(byte)OpCode.JMPIF] = 1;
            coefficients[(byte)OpCode.JMPIFNOT] = 1;
            coefficients[(byte)OpCode.JMPEQ] = 2;
            coefficients[(byte)OpCode.JMPNE] = 2;
            coefficients[(byte)OpCode.JMPGT] = 2;
            coefficients[(byte)OpCode.JMPGE] = 2;
            coefficients[(byte)OpCode.JMPLT] = 2;
            coefficients[(byte)OpCode.JMPLE] = 2;
            coefficients[(byte)OpCode.CALL] = 3;
            coefficients[(byte)OpCode.CALLA] = 3;
            coefficients[(byte)OpCode.CALLT] = 1 << 15;
            coefficients[(byte)OpCode.ABORT] = 0;
            coefficients[(byte)OpCode.ASSERT] = 1;
            coefficients[(byte)OpCode.ABORTMSG] = 0;
            coefficients[(byte)OpCode.ASSERTMSG] = 2;
            coefficients[(byte)OpCode.THROW] = 1 << 9;
            coefficients[(byte)OpCode.TRY] = 2;
            coefficients[(byte)OpCode.ENDTRY] = 1;
            coefficients[(byte)OpCode.ENDFINALLY] = 1;
            coefficients[(byte)OpCode.RET] = 1;
            coefficients[(byte)OpCode.SYSCALL] = 0;
            coefficients[(byte)OpCode.DEPTH] = 3;
            coefficients[(byte)OpCode.DROP] = 1;
            coefficients[(byte)OpCode.NIP] = 1;
            coefficients[(byte)OpCode.XDROP] = 1;
            coefficients[(byte)OpCode.CLEAR] = 1;
            coefficients[(byte)OpCode.DUP] = 1;
            coefficients[(byte)OpCode.OVER] = 1;
            coefficients[(byte)OpCode.PICK] = 1;
            coefficients[(byte)OpCode.TUCK] = 1;
            coefficients[(byte)OpCode.SWAP] = 1;
            coefficients[(byte)OpCode.ROT] = 1;
            coefficients[(byte)OpCode.REVERSE3] = 1;
            coefficients[(byte)OpCode.REVERSE4] = 1;
            coefficients[(byte)OpCode.REVERSEN] = 1;
            coefficients[(byte)OpCode.INITSSLOT] = 1;
            coefficients[(byte)OpCode.INITSLOT] = 1;
            coefficients[(byte)OpCode.LDSFLD0] = 1;
            coefficients[(byte)OpCode.LDSFLD1] = 1;
            coefficients[(byte)OpCode.LDSFLD2] = 1;
            coefficients[(byte)OpCode.LDSFLD3] = 1;
            coefficients[(byte)OpCode.LDSFLD4] = 1;
            coefficients[(byte)OpCode.LDSFLD5] = 1;
            coefficients[(byte)OpCode.LDSFLD6] = 1;
            coefficients[(byte)OpCode.LDSFLD] = 1;
            coefficients[(byte)OpCode.STSFLD0] = 1;
            coefficients[(byte)OpCode.STSFLD1] = 1;
            coefficients[(byte)OpCode.STSFLD2] = 1;
            coefficients[(byte)OpCode.STSFLD3] = 1;
            coefficients[(byte)OpCode.STSFLD4] = 1;
            coefficients[(byte)OpCode.STSFLD5] = 1;
            coefficients[(byte)OpCode.STSFLD6] = 1;
            coefficients[(byte)OpCode.STSFLD] = 1;
            coefficients[(byte)OpCode.LDLOC0] = 1;
            coefficients[(byte)OpCode.LDLOC1] = 1;
            coefficients[(byte)OpCode.LDLOC2] = 1;
            coefficients[(byte)OpCode.LDLOC3] = 1;
            coefficients[(byte)OpCode.LDLOC4] = 1;
            coefficients[(byte)OpCode.LDLOC5] = 1;
            coefficients[(byte)OpCode.LDLOC6] = 1;
            coefficients[(byte)OpCode.LDLOC] = 1;
            coefficients[(byte)OpCode.STLOC0] = 1;
            coefficients[(byte)OpCode.STLOC1] = 1;
            coefficients[(byte)OpCode.STLOC2] = 1;
            coefficients[(byte)OpCode.STLOC3] = 1;
            coefficients[(byte)OpCode.STLOC4] = 1;
            coefficients[(byte)OpCode.STLOC5] = 1;
            coefficients[(byte)OpCode.STLOC6] = 1;
            coefficients[(byte)OpCode.STLOC] = 1;
            coefficients[(byte)OpCode.LDARG0] = 1;
            coefficients[(byte)OpCode.LDARG1] = 1;
            coefficients[(byte)OpCode.LDARG2] = 1;
            coefficients[(byte)OpCode.LDARG3] = 1;
            coefficients[(byte)OpCode.LDARG4] = 1;
            coefficients[(byte)OpCode.LDARG5] = 1;
            coefficients[(byte)OpCode.LDARG6] = 1;
            coefficients[(byte)OpCode.LDARG] = 1;
            coefficients[(byte)OpCode.STARG0] = 1;
            coefficients[(byte)OpCode.STARG1] = 1;
            coefficients[(byte)OpCode.STARG2] = 1;
            coefficients[(byte)OpCode.STARG3] = 1;
            coefficients[(byte)OpCode.STARG4] = 1;
            coefficients[(byte)OpCode.STARG5] = 1;
            coefficients[(byte)OpCode.STARG6] = 1;
            coefficients[(byte)OpCode.STARG] = 1;
            coefficients[(byte)OpCode.NEWBUFFER] = 1;
            coefficients[(byte)OpCode.MEMCPY] = 1;
            coefficients[(byte)OpCode.CAT] = 1;
            coefficients[(byte)OpCode.SUBSTR] = 1;
            coefficients[(byte)OpCode.LEFT] = 1;
            coefficients[(byte)OpCode.RIGHT] = 1;
            coefficients[(byte)OpCode.INVERT] = 3;
            coefficients[(byte)OpCode.AND] = 3;
            coefficients[(byte)OpCode.OR] = 3;
            coefficients[(byte)OpCode.XOR] = 3;
            coefficients[(byte)OpCode.EQUAL] = 2;
            coefficients[(byte)OpCode.NOTEQUAL] = 2;
            coefficients[(byte)OpCode.SIGN] = 3;
            coefficients[(byte)OpCode.ABS] = 3;
            coefficients[(byte)OpCode.NEGATE] = 3;
            coefficients[(byte)OpCode.INC] = 3;
            coefficients[(byte)OpCode.DEC] = 3;
            coefficients[(byte)OpCode.ADD] = 3;
            coefficients[(byte)OpCode.SUB] = 3;
            coefficients[(byte)OpCode.MUL] = 4;
            coefficients[(byte)OpCode.DIV] = 4;
            coefficients[(byte)OpCode.MOD] = 4;
            coefficients[(byte)OpCode.POW] = 6;
            coefficients[(byte)OpCode.SQRT] = 8;
            coefficients[(byte)OpCode.MODMUL] = 4;
            coefficients[(byte)OpCode.MODPOW] = 8;
            coefficients[(byte)OpCode.SHL] = 4;
            coefficients[(byte)OpCode.SHR] = 3;
            coefficients[(byte)OpCode.NOT] = 2;
            coefficients[(byte)OpCode.BOOLAND] = 2;
            coefficients[(byte)OpCode.BOOLOR] = 2;
            coefficients[(byte)OpCode.NZ] = 2;
            coefficients[(byte)OpCode.NUMEQUAL] = 3;
            coefficients[(byte)OpCode.NUMNOTEQUAL] = 2;
            coefficients[(byte)OpCode.LT] = 2;
            coefficients[(byte)OpCode.LE] = 1;
            coefficients[(byte)OpCode.GT] = 1;
            coefficients[(byte)OpCode.GE] = 1;
            coefficients[(byte)OpCode.MIN] = 2;
            coefficients[(byte)OpCode.MAX] = 2;
            coefficients[(byte)OpCode.WITHIN] = 2;
            coefficients[(byte)OpCode.PACKMAP] = 1;
            coefficients[(byte)OpCode.PACKSTRUCT] = 1;
            coefficients[(byte)OpCode.PACK] = 1;
            coefficients[(byte)OpCode.UNPACK] = 1;
            coefficients[(byte)OpCode.NEWARRAY0] = 2;
            coefficients[(byte)OpCode.NEWARRAY] = 1;
            coefficients[(byte)OpCode.NEWSTRUCT0] = 2;
            coefficients[(byte)OpCode.NEWSTRUCT] = 1;
            coefficients[(byte)OpCode.NEWMAP] = 2;
            coefficients[(byte)OpCode.SIZE] = 1;
            coefficients[(byte)OpCode.HASKEY] = 1;
            coefficients[(byte)OpCode.KEYS] = 1;
            coefficients[(byte)OpCode.VALUES] = 1;
            coefficients[(byte)OpCode.PICKITEM] = 1;
            coefficients[(byte)OpCode.APPEND] = 1;
            coefficients[(byte)OpCode.SETITEM] = 1;
            coefficients[(byte)OpCode.REVERSEITEMS] = 1;
            coefficients[(byte)OpCode.REMOVE] = 1;
            coefficients[(byte)OpCode.CLEARITEMS] = 1;
            coefficients[(byte)OpCode.POPITEM] = 1;
            coefficients[(byte)OpCode.ISNULL] = 1;
            coefficients[(byte)OpCode.ISTYPE] = 1;
            coefficients[(byte)OpCode.CONVERT] = 1;
            return coefficients;
        }

        /// <summary>
        /// Gets the price for an opcode since Gorgon hardfork.
        /// </summary>
        /// <param name="baseFee">The base execution fee in datoshi.</param>
        /// <param name="opcode">The opcode.</param>
        /// <param name="args">The price arguments.</param>
        /// <returns>The price in picoGAS.</returns>
        public static long OpcodeV1(long baseFee, OpCode opcode, OpcodePriceArgs? args)
        {
            if (args is null)
            {
                return baseFee * StaticPrice(opcode);
            }
            return baseFee * Math.Max(1, DynamicPrice(opcode, args.Value));
        }

        private static long StaticPrice(OpCode opcode)
        {
            return StaticCoefficients[(byte)opcode];
        }

        private static long DynamicPrice(OpCode opcode, OpcodePriceArgs args)
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

        private static long[] ScaleCoefficients(long[] src, long mul)
        {
            long[] dst = new long[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                dst[i] = src[i] * mul;
            }
            return dst;
        }

        private static long AppendGas(OpcodePriceArgs args) => AppendW[0] * args.RefsDelta + AppendW[1] * args.NClonedItems + AppendW[2];
        private static long CatGas(OpcodePriceArgs args) => CatW[0] * args.Length + CatW[1];
        private static long ClearGas(OpcodePriceArgs args) => ClearW[0] * args.RefsDelta + ClearW[1] * args.Length + ClearW[2];
        private static long ClearItemsGas(OpcodePriceArgs args) => ClearItemsW[0] * args.RefsDelta + ClearItemsW[1];
        private static long ConvertGas(OpcodePriceArgs args) => args.Type == StackItemType.Array ? ConvertArrOrStructW[0] * args.Length + ConvertArrOrStructW[1] : ConvertByteArrayW[0] * args.Length + ConvertByteArrayW[1];
        private static long DropGas(OpcodePriceArgs args) => DropW[0] * args.RefsDelta + DropW[1];
        private static long DupGas(OpcodePriceArgs args) => DupW[0] * args.Length + DupW[1];
        private static long HasKeyGas(OpcodePriceArgs args) => HasKeyW[0] * args.RefsDelta + HasKeyW[2];
        private static long InitSlotGas(OpcodePriceArgs args) => InitSlotW[0] * args.Length + InitSlotW[1];
        private static long KeysGas(OpcodePriceArgs args) => KeysW[0] * args.Length + KeysW[1];
        private static long MemcpyGas(OpcodePriceArgs args) => MemcpyW[0] * args.Length + MemcpyW[1];
        private static long NewArrayGas(OpcodePriceArgs args) => (args.Type == StackItemType.ByteString || args.Type == StackItemType.Integer) ? NewArrayByteOrIntW[0] * args.Length + NewArrayByteOrIntW[1] : NewArrayAnyW[0] * args.Length + NewArrayAnyW[1];
        private static long NewBufferGas(OpcodePriceArgs args) => NewBufferW[0] * args.Length + NewBufferW[1];
        private static long PackGas(OpcodePriceArgs args) => PackW[0] * args.Length + PackW[1];
        private static long PackMapGas(OpcodePriceArgs args) => PackMapW[0] * args.RefsDelta + PackMapW[2];
        private static long PickItemGas(OpcodePriceArgs args) => args.Type == StackItemType.ByteString ? PickItemByteArrayW[0] * args.RefsDelta + PickItemByteArrayW[2] * args.Length + PickItemByteArrayW[3] : PickItemAnyW[0] * args.RefsDelta + PickItemAnyW[2] * args.Length + PickItemAnyW[3];
        private static long PopItemGas(OpcodePriceArgs args) => PopItemW[0] * args.RefsDelta + PopItemW[1];
        private static long RemoveGas(OpcodePriceArgs args) => RemoveW[0] * args.RefsDelta + RemoveW[2];
        private static long ReverseItemsGas(OpcodePriceArgs args) => args.Type == StackItemType.Buffer ? ReverseItemsBufferW[0] * args.Length + ReverseItemsBufferW[1] : ReverseItemsArrayW[0] * args.RefsDelta + ReverseItemsArrayW[1] * args.Length + ReverseItemsArrayW[2];
        private static long ReverseGas(OpcodePriceArgs args) => ReverseW[0] * args.Length + ReverseW[1];
        private static long RollGas(OpcodePriceArgs args) => RollW[0] * args.Length + RollW[1];
        private static long SetItemGas(OpcodePriceArgs args) => SetitemW[0] * args.RefsDelta + SetitemW[1] * args.NClonedItems + SetitemW[3];
        private static long SizeGas(OpcodePriceArgs args) => SizeW[0] * args.RefsDelta + SizeW[1];
        private static long StGas(OpcodePriceArgs args) => StW[0] * args.RefsDelta + StW[1];
        private static long SubstrGas(OpcodePriceArgs args) => SubstrW[0] * args.Length + SubstrW[1];
        private static long UnpackGas(OpcodePriceArgs args) => args.Type == StackItemType.Map ? UnpackMapW[0] * args.Length + UnpackMapW[1] : UnpackArrayOrStructW[0] * args.Length + UnpackArrayOrStructW[1];
        private static long ValuesGas(OpcodePriceArgs args) => ValuesW[0] * args.Length + ValuesW[1] * args.NClonedItems + ValuesW[2];
        private static long XDropGas(OpcodePriceArgs args) => XDropW[0] * args.RefsDelta + XDropW[1] * args.Length + XDropW[2];
    }
}