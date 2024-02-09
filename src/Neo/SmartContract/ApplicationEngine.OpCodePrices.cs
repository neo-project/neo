// Copyright (C) 2015-2024 The Neo Project.
//
// ApplicationEngine.OpCodePrices.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        /// <summary>
        /// The prices of all the opcodes.
        /// </summary>
        public static readonly long[] OpCodePrices = new long[byte.MaxValue];

        /// <summary>
        /// Init OpCodePrices
        /// </summary>
        static ApplicationEngine()
        {
            OpCodePrices[(byte)OpCode.PUSHINT8] = 1 << 0;
            OpCodePrices[(byte)OpCode.PUSHINT16] = 1 << 0;
            OpCodePrices[(byte)OpCode.PUSHINT32] = 1 << 0;
            OpCodePrices[(byte)OpCode.PUSHINT64] = 1 << 0;
            OpCodePrices[(byte)OpCode.PUSHINT128] = 1 << 2;
            OpCodePrices[(byte)OpCode.PUSHINT256] = 1 << 2;
            OpCodePrices[(byte)OpCode.PUSHT] = 1 << 0;
            OpCodePrices[(byte)OpCode.PUSHF] = 1 << 0;
            OpCodePrices[(byte)OpCode.PUSHA] = 1 << 2;
            OpCodePrices[(byte)OpCode.PUSHNULL] = 1 << 0;
            OpCodePrices[(byte)OpCode.PUSHDATA1] = 1 << 3;
            OpCodePrices[(byte)OpCode.PUSHDATA2] = 1 << 9;
            OpCodePrices[(byte)OpCode.PUSHDATA4] = 1 << 12;
            OpCodePrices[(byte)OpCode.PUSHM1] = 1 << 0;
            OpCodePrices[(byte)OpCode.PUSH0] = 1 << 0;
            OpCodePrices[(byte)OpCode.PUSH1] = 1 << 0;
            OpCodePrices[(byte)OpCode.PUSH2] = 1 << 0;
            OpCodePrices[(byte)OpCode.PUSH3] = 1 << 0;
            OpCodePrices[(byte)OpCode.PUSH4] = 1 << 0;
            OpCodePrices[(byte)OpCode.PUSH5] = 1 << 0;
            OpCodePrices[(byte)OpCode.PUSH6] = 1 << 0;
            OpCodePrices[(byte)OpCode.PUSH7] = 1 << 0;
            OpCodePrices[(byte)OpCode.PUSH8] = 1 << 0;
            OpCodePrices[(byte)OpCode.PUSH9] = 1 << 0;
            OpCodePrices[(byte)OpCode.PUSH10] = 1 << 0;
            OpCodePrices[(byte)OpCode.PUSH11] = 1 << 0;
            OpCodePrices[(byte)OpCode.PUSH12] = 1 << 0;
            OpCodePrices[(byte)OpCode.PUSH13] = 1 << 0;
            OpCodePrices[(byte)OpCode.PUSH14] = 1 << 0;
            OpCodePrices[(byte)OpCode.PUSH15] = 1 << 0;
            OpCodePrices[(byte)OpCode.PUSH16] = 1 << 0;
            OpCodePrices[(byte)OpCode.NOP] = 1 << 0;
            OpCodePrices[(byte)OpCode.JMP] = 1 << 1;
            OpCodePrices[(byte)OpCode.JMP_L] = 1 << 1;
            OpCodePrices[(byte)OpCode.JMPIF] = 1 << 1;
            OpCodePrices[(byte)OpCode.JMPIF_L] = 1 << 1;
            OpCodePrices[(byte)OpCode.JMPIFNOT] = 1 << 1;
            OpCodePrices[(byte)OpCode.JMPIFNOT_L] = 1 << 1;
            OpCodePrices[(byte)OpCode.JMPEQ] = 1 << 1;
            OpCodePrices[(byte)OpCode.JMPEQ_L] = 1 << 1;
            OpCodePrices[(byte)OpCode.JMPNE] = 1 << 1;
            OpCodePrices[(byte)OpCode.JMPNE_L] = 1 << 1;
            OpCodePrices[(byte)OpCode.JMPGT] = 1 << 1;
            OpCodePrices[(byte)OpCode.JMPGT_L] = 1 << 1;
            OpCodePrices[(byte)OpCode.JMPGE] = 1 << 1;
            OpCodePrices[(byte)OpCode.JMPGE_L] = 1 << 1;
            OpCodePrices[(byte)OpCode.JMPLT] = 1 << 1;
            OpCodePrices[(byte)OpCode.JMPLT_L] = 1 << 1;
            OpCodePrices[(byte)OpCode.JMPLE] = 1 << 1;
            OpCodePrices[(byte)OpCode.JMPLE_L] = 1 << 1;
            OpCodePrices[(byte)OpCode.CALL] = 1 << 9;
            OpCodePrices[(byte)OpCode.CALL_L] = 1 << 9;
            OpCodePrices[(byte)OpCode.CALLA] = 1 << 9;
            OpCodePrices[(byte)OpCode.CALLT] = 1 << 15;
            OpCodePrices[(byte)OpCode.ABORT] = 0;
            OpCodePrices[(byte)OpCode.ABORTMSG] = 0;
            OpCodePrices[(byte)OpCode.ASSERT] = 1 << 0;
            OpCodePrices[(byte)OpCode.ASSERTMSG] = 1 << 0;
            OpCodePrices[(byte)OpCode.THROW] = 1 << 9;
            OpCodePrices[(byte)OpCode.TRY] = 1 << 2;
            OpCodePrices[(byte)OpCode.TRY_L] = 1 << 2;
            OpCodePrices[(byte)OpCode.ENDTRY] = 1 << 2;
            OpCodePrices[(byte)OpCode.ENDTRY_L] = 1 << 2;
            OpCodePrices[(byte)OpCode.ENDFINALLY] = 1 << 2;
            OpCodePrices[(byte)OpCode.RET] = 0;
            OpCodePrices[(byte)OpCode.SYSCALL] = 0;
            OpCodePrices[(byte)OpCode.DEPTH] = 1 << 1;
            OpCodePrices[(byte)OpCode.DROP] = 1 << 1;
            OpCodePrices[(byte)OpCode.NIP] = 1 << 1;
            OpCodePrices[(byte)OpCode.XDROP] = 1 << 4;
            OpCodePrices[(byte)OpCode.CLEAR] = 1 << 4;
            OpCodePrices[(byte)OpCode.DUP] = 1 << 1;
            OpCodePrices[(byte)OpCode.OVER] = 1 << 1;
            OpCodePrices[(byte)OpCode.PICK] = 1 << 1;
            OpCodePrices[(byte)OpCode.TUCK] = 1 << 1;
            OpCodePrices[(byte)OpCode.SWAP] = 1 << 1;
            OpCodePrices[(byte)OpCode.ROT] = 1 << 1;
            OpCodePrices[(byte)OpCode.ROLL] = 1 << 4;
            OpCodePrices[(byte)OpCode.REVERSE3] = 1 << 1;
            OpCodePrices[(byte)OpCode.REVERSE4] = 1 << 1;
            OpCodePrices[(byte)OpCode.REVERSEN] = 1 << 4;
            OpCodePrices[(byte)OpCode.INITSSLOT] = 1 << 4;
            OpCodePrices[(byte)OpCode.INITSLOT] = 1 << 6;
            OpCodePrices[(byte)OpCode.LDSFLD0] = 1 << 1;
            OpCodePrices[(byte)OpCode.LDSFLD1] = 1 << 1;
            OpCodePrices[(byte)OpCode.LDSFLD2] = 1 << 1;
            OpCodePrices[(byte)OpCode.LDSFLD3] = 1 << 1;
            OpCodePrices[(byte)OpCode.LDSFLD4] = 1 << 1;
            OpCodePrices[(byte)OpCode.LDSFLD5] = 1 << 1;
            OpCodePrices[(byte)OpCode.LDSFLD6] = 1 << 1;
            OpCodePrices[(byte)OpCode.LDSFLD] = 1 << 1;
            OpCodePrices[(byte)OpCode.STSFLD0] = 1 << 1;
            OpCodePrices[(byte)OpCode.STSFLD1] = 1 << 1;
            OpCodePrices[(byte)OpCode.STSFLD2] = 1 << 1;
            OpCodePrices[(byte)OpCode.STSFLD3] = 1 << 1;
            OpCodePrices[(byte)OpCode.STSFLD4] = 1 << 1;
            OpCodePrices[(byte)OpCode.STSFLD5] = 1 << 1;
            OpCodePrices[(byte)OpCode.STSFLD6] = 1 << 1;
            OpCodePrices[(byte)OpCode.STSFLD] = 1 << 1;
            OpCodePrices[(byte)OpCode.LDLOC0] = 1 << 1;
            OpCodePrices[(byte)OpCode.LDLOC1] = 1 << 1;
            OpCodePrices[(byte)OpCode.LDLOC2] = 1 << 1;
            OpCodePrices[(byte)OpCode.LDLOC3] = 1 << 1;
            OpCodePrices[(byte)OpCode.LDLOC4] = 1 << 1;
            OpCodePrices[(byte)OpCode.LDLOC5] = 1 << 1;
            OpCodePrices[(byte)OpCode.LDLOC6] = 1 << 1;
            OpCodePrices[(byte)OpCode.LDLOC] = 1 << 1;
            OpCodePrices[(byte)OpCode.STLOC0] = 1 << 1;
            OpCodePrices[(byte)OpCode.STLOC1] = 1 << 1;
            OpCodePrices[(byte)OpCode.STLOC2] = 1 << 1;
            OpCodePrices[(byte)OpCode.STLOC3] = 1 << 1;
            OpCodePrices[(byte)OpCode.STLOC4] = 1 << 1;
            OpCodePrices[(byte)OpCode.STLOC5] = 1 << 1;
            OpCodePrices[(byte)OpCode.STLOC6] = 1 << 1;
            OpCodePrices[(byte)OpCode.STLOC] = 1 << 1;
            OpCodePrices[(byte)OpCode.LDARG0] = 1 << 1;
            OpCodePrices[(byte)OpCode.LDARG1] = 1 << 1;
            OpCodePrices[(byte)OpCode.LDARG2] = 1 << 1;
            OpCodePrices[(byte)OpCode.LDARG3] = 1 << 1;
            OpCodePrices[(byte)OpCode.LDARG4] = 1 << 1;
            OpCodePrices[(byte)OpCode.LDARG5] = 1 << 1;
            OpCodePrices[(byte)OpCode.LDARG6] = 1 << 1;
            OpCodePrices[(byte)OpCode.LDARG] = 1 << 1;
            OpCodePrices[(byte)OpCode.STARG0] = 1 << 1;
            OpCodePrices[(byte)OpCode.STARG1] = 1 << 1;
            OpCodePrices[(byte)OpCode.STARG2] = 1 << 1;
            OpCodePrices[(byte)OpCode.STARG3] = 1 << 1;
            OpCodePrices[(byte)OpCode.STARG4] = 1 << 1;
            OpCodePrices[(byte)OpCode.STARG5] = 1 << 1;
            OpCodePrices[(byte)OpCode.STARG6] = 1 << 1;
            OpCodePrices[(byte)OpCode.STARG] = 1 << 1;
            OpCodePrices[(byte)OpCode.NEWBUFFER] = 1 << 8;
            OpCodePrices[(byte)OpCode.MEMCPY] = 1 << 11;
            OpCodePrices[(byte)OpCode.CAT] = 1 << 11;
            OpCodePrices[(byte)OpCode.SUBSTR] = 1 << 11;
            OpCodePrices[(byte)OpCode.LEFT] = 1 << 11;
            OpCodePrices[(byte)OpCode.RIGHT] = 1 << 11;
            OpCodePrices[(byte)OpCode.INVERT] = 1 << 2;
            OpCodePrices[(byte)OpCode.AND] = 1 << 3;
            OpCodePrices[(byte)OpCode.OR] = 1 << 3;
            OpCodePrices[(byte)OpCode.XOR] = 1 << 3;
            OpCodePrices[(byte)OpCode.EQUAL] = 1 << 5;
            OpCodePrices[(byte)OpCode.NOTEQUAL] = 1 << 5;
            OpCodePrices[(byte)OpCode.SIGN] = 1 << 2;
            OpCodePrices[(byte)OpCode.ABS] = 1 << 2;
            OpCodePrices[(byte)OpCode.NEGATE] = 1 << 2;
            OpCodePrices[(byte)OpCode.INC] = 1 << 2;
            OpCodePrices[(byte)OpCode.DEC] = 1 << 2;
            OpCodePrices[(byte)OpCode.ADD] = 1 << 3;
            OpCodePrices[(byte)OpCode.SUB] = 1 << 3;
            OpCodePrices[(byte)OpCode.MUL] = 1 << 3;
            OpCodePrices[(byte)OpCode.DIV] = 1 << 3;
            OpCodePrices[(byte)OpCode.MOD] = 1 << 3;
            OpCodePrices[(byte)OpCode.POW] = 1 << 6;
            OpCodePrices[(byte)OpCode.SQRT] = 1 << 6;
            OpCodePrices[(byte)OpCode.MODMUL] = 1 << 5;
            OpCodePrices[(byte)OpCode.MODPOW] = 1 << 11;
            OpCodePrices[(byte)OpCode.SHL] = 1 << 3;
            OpCodePrices[(byte)OpCode.SHR] = 1 << 3;
            OpCodePrices[(byte)OpCode.NOT] = 1 << 2;
            OpCodePrices[(byte)OpCode.BOOLAND] = 1 << 3;
            OpCodePrices[(byte)OpCode.BOOLOR] = 1 << 3;
            OpCodePrices[(byte)OpCode.NZ] = 1 << 2;
            OpCodePrices[(byte)OpCode.NUMEQUAL] = 1 << 3;
            OpCodePrices[(byte)OpCode.NUMNOTEQUAL] = 1 << 3;
            OpCodePrices[(byte)OpCode.LT] = 1 << 3;
            OpCodePrices[(byte)OpCode.LE] = 1 << 3;
            OpCodePrices[(byte)OpCode.GT] = 1 << 3;
            OpCodePrices[(byte)OpCode.GE] = 1 << 3;
            OpCodePrices[(byte)OpCode.MIN] = 1 << 3;
            OpCodePrices[(byte)OpCode.MAX] = 1 << 3;
            OpCodePrices[(byte)OpCode.WITHIN] = 1 << 3;
            OpCodePrices[(byte)OpCode.PACKMAP] = 1 << 11;
            OpCodePrices[(byte)OpCode.PACKSTRUCT] = 1 << 11;
            OpCodePrices[(byte)OpCode.PACK] = 1 << 11;
            OpCodePrices[(byte)OpCode.UNPACK] = 1 << 11;
            OpCodePrices[(byte)OpCode.NEWARRAY0] = 1 << 4;
            OpCodePrices[(byte)OpCode.NEWARRAY] = 1 << 9;
            OpCodePrices[(byte)OpCode.NEWARRAY_T] = 1 << 9;
            OpCodePrices[(byte)OpCode.NEWSTRUCT0] = 1 << 4;
            OpCodePrices[(byte)OpCode.NEWSTRUCT] = 1 << 9;
            OpCodePrices[(byte)OpCode.NEWMAP] = 1 << 3;
            OpCodePrices[(byte)OpCode.SIZE] = 1 << 2;
            OpCodePrices[(byte)OpCode.HASKEY] = 1 << 6;
            OpCodePrices[(byte)OpCode.KEYS] = 1 << 4;
            OpCodePrices[(byte)OpCode.VALUES] = 1 << 13;
            OpCodePrices[(byte)OpCode.PICKITEM] = 1 << 6;
            OpCodePrices[(byte)OpCode.APPEND] = 1 << 13;
            OpCodePrices[(byte)OpCode.SETITEM] = 1 << 13;
            OpCodePrices[(byte)OpCode.REVERSEITEMS] = 1 << 13;
            OpCodePrices[(byte)OpCode.REMOVE] = 1 << 4;
            OpCodePrices[(byte)OpCode.CLEARITEMS] = 1 << 4;
            OpCodePrices[(byte)OpCode.POPITEM] = 1 << 4;
            OpCodePrices[(byte)OpCode.ISNULL] = 1 << 1;
            OpCodePrices[(byte)OpCode.ISTYPE] = 1 << 1;
            OpCodePrices[(byte)OpCode.CONVERT] = 1 << 13;
        }
    }
}
