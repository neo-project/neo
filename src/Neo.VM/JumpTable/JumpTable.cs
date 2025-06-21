// Copyright (C) 2015-2025 The Neo Project.
//
// JumpTable.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    public partial class JumpTable
    {
        /// <summary>
        /// Default JumpTable
        /// </summary>
        public static readonly JumpTable Default = new();

        public delegate void DelAction(ExecutionEngine engine, Instruction instruction);
        protected readonly DelAction[] Table = new DelAction[byte.MaxValue];

        public DelAction this[OpCode opCode]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Table[(byte)opCode];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { Table[(byte)opCode] = value; }
        }

        /// <summary>
        /// Pre-compiled jump table constructor - eliminates reflection overhead
        /// </summary>
        public JumpTable()
        {
            // Initialize all entries to InvalidOpcode first
            for (var x = 0; x < Table.Length; x++)
            {
                Table[x] = InvalidOpcode;
            }

            // Pre-compiled opcode dispatch table - no reflection needed
            InitializeOpcodeTable();
        }

        /// <summary>
        /// Initialize the opcode table with direct method assignments
        /// This replaces the expensive reflection-based approach
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitializeOpcodeTable()
        {
            // Constants
            Table[(byte)OpCode.PUSHINT8] = PushInt8;
            Table[(byte)OpCode.PUSHINT16] = PushInt16;
            Table[(byte)OpCode.PUSHINT32] = PushInt32;
            Table[(byte)OpCode.PUSHINT64] = PushInt64;
            Table[(byte)OpCode.PUSHINT128] = PushInt128;
            Table[(byte)OpCode.PUSHINT256] = PushInt256;
            Table[(byte)OpCode.PUSHT] = PushT;
            Table[(byte)OpCode.PUSHF] = PushF;
            Table[(byte)OpCode.PUSHA] = PushA;
            Table[(byte)OpCode.PUSHNULL] = PushNull;
            Table[(byte)OpCode.PUSHDATA1] = PushData1;
            Table[(byte)OpCode.PUSHDATA2] = PushData2;
            Table[(byte)OpCode.PUSHDATA4] = PushData4;
            Table[(byte)OpCode.PUSHM1] = PushM1;
            Table[(byte)OpCode.PUSH0] = Push0;
            Table[(byte)OpCode.PUSH1] = Push1;
            Table[(byte)OpCode.PUSH2] = Push2;
            Table[(byte)OpCode.PUSH3] = Push3;
            Table[(byte)OpCode.PUSH4] = Push4;
            Table[(byte)OpCode.PUSH5] = Push5;
            Table[(byte)OpCode.PUSH6] = Push6;
            Table[(byte)OpCode.PUSH7] = Push7;
            Table[(byte)OpCode.PUSH8] = Push8;
            Table[(byte)OpCode.PUSH9] = Push9;
            Table[(byte)OpCode.PUSH10] = Push10;
            Table[(byte)OpCode.PUSH11] = Push11;
            Table[(byte)OpCode.PUSH12] = Push12;
            Table[(byte)OpCode.PUSH13] = Push13;
            Table[(byte)OpCode.PUSH14] = Push14;
            Table[(byte)OpCode.PUSH15] = Push15;
            Table[(byte)OpCode.PUSH16] = Push16;

            // Flow control
            Table[(byte)OpCode.NOP] = Nop;
            Table[(byte)OpCode.JMP] = Jmp;
            Table[(byte)OpCode.JMP_L] = Jmp_L;
            Table[(byte)OpCode.JMPIF] = JmpIf;
            Table[(byte)OpCode.JMPIF_L] = JmpIf_L;
            Table[(byte)OpCode.JMPIFNOT] = JmpIfNot;
            Table[(byte)OpCode.JMPIFNOT_L] = JmpIfNot_L;
            Table[(byte)OpCode.JMPEQ] = JmpEq;
            Table[(byte)OpCode.JMPEQ_L] = JmpEq_L;
            Table[(byte)OpCode.JMPNE] = JmpNe;
            Table[(byte)OpCode.JMPNE_L] = JmpNe_L;
            Table[(byte)OpCode.JMPGT] = JmpGt;
            Table[(byte)OpCode.JMPGT_L] = JmpGt_L;
            Table[(byte)OpCode.JMPGE] = JmpGe;
            Table[(byte)OpCode.JMPGE_L] = JmpGe_L;
            Table[(byte)OpCode.JMPLT] = JmpLt;
            Table[(byte)OpCode.JMPLT_L] = JmpLt_L;
            Table[(byte)OpCode.JMPLE] = JmpLe;
            Table[(byte)OpCode.JMPLE_L] = JmpLe_L;
            Table[(byte)OpCode.CALL] = Call;
            Table[(byte)OpCode.CALL_L] = Call_L;
            Table[(byte)OpCode.CALLA] = CallA;
            Table[(byte)OpCode.CALLT] = CallT;
            Table[(byte)OpCode.ABORT] = Abort;
            Table[(byte)OpCode.ASSERT] = Assert;
            Table[(byte)OpCode.THROW] = Throw;
            Table[(byte)OpCode.TRY] = Try;
            Table[(byte)OpCode.TRY_L] = Try_L;
            Table[(byte)OpCode.ENDTRY] = EndTry;
            Table[(byte)OpCode.ENDTRY_L] = EndTry_L;
            Table[(byte)OpCode.ENDFINALLY] = EndFinally;
            Table[(byte)OpCode.RET] = Ret;
            Table[(byte)OpCode.SYSCALL] = Syscall;

            // Stack
            Table[(byte)OpCode.DEPTH] = Depth;
            Table[(byte)OpCode.DROP] = Drop;
            Table[(byte)OpCode.NIP] = Nip;
            Table[(byte)OpCode.XDROP] = XDrop;
            Table[(byte)OpCode.CLEAR] = Clear;
            Table[(byte)OpCode.DUP] = Dup;
            Table[(byte)OpCode.OVER] = Over;
            Table[(byte)OpCode.PICK] = Pick;
            Table[(byte)OpCode.TUCK] = Tuck;
            Table[(byte)OpCode.SWAP] = Swap;
            Table[(byte)OpCode.ROT] = Rot;
            Table[(byte)OpCode.ROLL] = Roll;
            Table[(byte)OpCode.REVERSE3] = Reverse3;
            Table[(byte)OpCode.REVERSE4] = Reverse4;
            Table[(byte)OpCode.REVERSEN] = ReverseN;

            // Slot
            Table[(byte)OpCode.INITSSLOT] = InitSSlot;
            Table[(byte)OpCode.INITSLOT] = InitSlot;
            Table[(byte)OpCode.LDSFLD0] = LdSFld0;
            Table[(byte)OpCode.LDSFLD1] = LdSFld1;
            Table[(byte)OpCode.LDSFLD2] = LdSFld2;
            Table[(byte)OpCode.LDSFLD3] = LdSFld3;
            Table[(byte)OpCode.LDSFLD4] = LdSFld4;
            Table[(byte)OpCode.LDSFLD5] = LdSFld5;
            Table[(byte)OpCode.LDSFLD6] = LdSFld6;
            Table[(byte)OpCode.LDSFLD] = LdSFld;
            Table[(byte)OpCode.STSFLD0] = StSFld0;
            Table[(byte)OpCode.STSFLD1] = StSFld1;
            Table[(byte)OpCode.STSFLD2] = StSFld2;
            Table[(byte)OpCode.STSFLD3] = StSFld3;
            Table[(byte)OpCode.STSFLD4] = StSFld4;
            Table[(byte)OpCode.STSFLD5] = StSFld5;
            Table[(byte)OpCode.STSFLD6] = StSFld6;
            Table[(byte)OpCode.STSFLD] = StSFld;
            Table[(byte)OpCode.LDLOC0] = LdLoc0;
            Table[(byte)OpCode.LDLOC1] = LdLoc1;
            Table[(byte)OpCode.LDLOC2] = LdLoc2;
            Table[(byte)OpCode.LDLOC3] = LdLoc3;
            Table[(byte)OpCode.LDLOC4] = LdLoc4;
            Table[(byte)OpCode.LDLOC5] = LdLoc5;
            Table[(byte)OpCode.LDLOC6] = LdLoc6;
            Table[(byte)OpCode.LDLOC] = LdLoc;
            Table[(byte)OpCode.STLOC0] = StLoc0;
            Table[(byte)OpCode.STLOC1] = StLoc1;
            Table[(byte)OpCode.STLOC2] = StLoc2;
            Table[(byte)OpCode.STLOC3] = StLoc3;
            Table[(byte)OpCode.STLOC4] = StLoc4;
            Table[(byte)OpCode.STLOC5] = StLoc5;
            Table[(byte)OpCode.STLOC6] = StLoc6;
            Table[(byte)OpCode.STLOC] = StLoc;
            Table[(byte)OpCode.LDARG0] = LdArg0;
            Table[(byte)OpCode.LDARG1] = LdArg1;
            Table[(byte)OpCode.LDARG2] = LdArg2;
            Table[(byte)OpCode.LDARG3] = LdArg3;
            Table[(byte)OpCode.LDARG4] = LdArg4;
            Table[(byte)OpCode.LDARG5] = LdArg5;
            Table[(byte)OpCode.LDARG6] = LdArg6;
            Table[(byte)OpCode.LDARG] = LdArg;
            Table[(byte)OpCode.STARG0] = StArg0;
            Table[(byte)OpCode.STARG1] = StArg1;
            Table[(byte)OpCode.STARG2] = StArg2;
            Table[(byte)OpCode.STARG3] = StArg3;
            Table[(byte)OpCode.STARG4] = StArg4;
            Table[(byte)OpCode.STARG5] = StArg5;
            Table[(byte)OpCode.STARG6] = StArg6;
            Table[(byte)OpCode.STARG] = StArg;

            // Splice
            Table[(byte)OpCode.NEWBUFFER] = NewBuffer;
            Table[(byte)OpCode.MEMCPY] = Memcpy;
            Table[(byte)OpCode.CAT] = Cat;
            Table[(byte)OpCode.SUBSTR] = SubStr;
            Table[(byte)OpCode.LEFT] = Left;
            Table[(byte)OpCode.RIGHT] = Right;

            // Bitwise logic
            Table[(byte)OpCode.INVERT] = Invert;
            Table[(byte)OpCode.AND] = And;
            Table[(byte)OpCode.OR] = Or;
            Table[(byte)OpCode.XOR] = XOr;
            Table[(byte)OpCode.EQUAL] = Equal;
            Table[(byte)OpCode.NOTEQUAL] = NotEqual;

            // Arithmetic
            Table[(byte)OpCode.SIGN] = Sign;
            Table[(byte)OpCode.ABS] = Abs;
            Table[(byte)OpCode.NEGATE] = Negate;
            Table[(byte)OpCode.INC] = Inc;
            Table[(byte)OpCode.DEC] = Dec;
            Table[(byte)OpCode.ADD] = Add;
            Table[(byte)OpCode.SUB] = Sub;
            Table[(byte)OpCode.MUL] = Mul;
            Table[(byte)OpCode.DIV] = Div;
            Table[(byte)OpCode.MOD] = Mod;
            Table[(byte)OpCode.POW] = Pow;
            Table[(byte)OpCode.SQRT] = Sqrt;
            Table[(byte)OpCode.MODMUL] = ModMul;
            Table[(byte)OpCode.MODPOW] = ModPow;
            Table[(byte)OpCode.SHL] = Shl;
            Table[(byte)OpCode.SHR] = Shr;
            Table[(byte)OpCode.NOT] = Not;
            Table[(byte)OpCode.BOOLAND] = BoolAnd;
            Table[(byte)OpCode.BOOLOR] = BoolOr;
            Table[(byte)OpCode.NZ] = Nz;
            Table[(byte)OpCode.NUMEQUAL] = NumEqual;
            Table[(byte)OpCode.NUMNOTEQUAL] = NumNotEqual;
            Table[(byte)OpCode.LT] = Lt;
            Table[(byte)OpCode.LE] = Le;
            Table[(byte)OpCode.GT] = Gt;
            Table[(byte)OpCode.GE] = Ge;
            Table[(byte)OpCode.MIN] = Min;
            Table[(byte)OpCode.MAX] = Max;
            Table[(byte)OpCode.WITHIN] = Within;

            // Compound-type
            Table[(byte)OpCode.PACKMAP] = PackMap;
            Table[(byte)OpCode.PACKSTRUCT] = PackStruct;
            Table[(byte)OpCode.PACK] = Pack;
            Table[(byte)OpCode.UNPACK] = Unpack;
            Table[(byte)OpCode.NEWARRAY0] = NewArray0;
            Table[(byte)OpCode.NEWARRAY] = NewArray;
            Table[(byte)OpCode.NEWARRAY_T] = NewArray_T;
            Table[(byte)OpCode.NEWSTRUCT0] = NewStruct0;
            Table[(byte)OpCode.NEWSTRUCT] = NewStruct;
            Table[(byte)OpCode.NEWMAP] = NewMap;
            Table[(byte)OpCode.SIZE] = Size;
            Table[(byte)OpCode.HASKEY] = HasKey;
            Table[(byte)OpCode.KEYS] = Keys;
            Table[(byte)OpCode.VALUES] = Values;
            Table[(byte)OpCode.PICKITEM] = PickItem;
            Table[(byte)OpCode.APPEND] = Append;
            Table[(byte)OpCode.SETITEM] = SetItem;
            Table[(byte)OpCode.REVERSEITEMS] = ReverseItems;
            Table[(byte)OpCode.REMOVE] = Remove;
            Table[(byte)OpCode.CLEARITEMS] = ClearItems;
            Table[(byte)OpCode.POPITEM] = PopItem;

            // Types
            Table[(byte)OpCode.ISNULL] = IsNull;
            Table[(byte)OpCode.ISTYPE] = IsType;
            Table[(byte)OpCode.CONVERT] = Convert;

            // Extensions
            Table[(byte)OpCode.ABORTMSG] = AbortMsg;
            Table[(byte)OpCode.ASSERTMSG] = AssertMsg;
        }

        public virtual void InvalidOpcode(ExecutionEngine engine, Instruction instruction)
        {
            throw new InvalidOperationException($"Opcode {instruction.OpCode} is undefined.");
        }
    }
}
