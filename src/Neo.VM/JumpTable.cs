// Copyright (C) 2015-2024 The Neo Project.
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
using System.Reflection;

namespace Neo.VM
{
    public partial class JumpTable
    {
        public delegate void DelAction(ExecutionEngine engine, Instruction instruction);
        protected readonly DelAction[] _table = new DelAction[byte.MaxValue];

        /// <summary>
        /// Get Method
        /// </summary>
        /// <param name="opCode">OpCode</param>
        /// <returns>Action</returns>
        public DelAction GetMethod(OpCode opCode)
        {
            return _table[(byte)opCode];
        }

        public JumpTable()
        {
            // Fill defined

            foreach (var mi in GetType().GetMethods())
            {
                foreach (var attr in mi.GetCustomAttributes<OpcodeMethodAttribute>(true))
                {
                    if (_table[(byte)attr.OpCode] is not null)
                    {
                        throw new InvalidOperationException($"Opcode {attr.OpCode} is already defined.");
                    }

                    _table[(byte)attr.OpCode] = (DelAction)mi.CreateDelegate(typeof(DelAction), this);
                }
            }

            // Fill with undefined

            for (int x = 0; x < _table.Length; x++)
            {
                if (_table[x] is not null) continue;

                _table[x] = (engine, instruction) =>
                {
                    throw new InvalidOperationException($"Opcode {instruction.OpCode} is undefined.");
                };
            }

            /* TODO:

            switch (instruction.OpCode)
            {
                // Stack ops
                case OpCode.DEPTH:
                    Depth(instruction);
                    break;
                case OpCode.DROP:
                    Drop(instruction);
                    break;
                case OpCode.NIP:
                    Nip(instruction);
                    break;
                case OpCode.XDROP:
                    XDrop(instruction);
                    break;
                case OpCode.CLEAR:
                    Clear(instruction);
                    break;
                case OpCode.DUP:
                    Dup(instruction);
                    break;
                case OpCode.OVER:
                    Over(instruction);
                    break;
                case OpCode.PICK:
                    Pick(instruction);
                    break;
                case OpCode.TUCK:
                    Tuck(instruction);
                    break;
                case OpCode.SWAP:
                    Swap(instruction);
                    break;
                case OpCode.ROT:
                    Rot(instruction);
                    break;
                case OpCode.ROLL:
                    Roll(instruction);
                    break;
                case OpCode.REVERSE3:
                    Reverse3(instruction);
                    break;
                case OpCode.REVERSE4:
                    Reverse4(instruction);
                    break;
                case OpCode.REVERSEN:
                    ReverseN(instruction);
                    break;

                //Slot
                case OpCode.INITSSLOT:
                    InitSSlot(instruction);
                    break;
                case OpCode.INITSLOT:
                    InitSlot(instruction);
                    break;
                case OpCode.LDSFLD0:
                case OpCode.LDSFLD1:
                case OpCode.LDSFLD2:
                case OpCode.LDSFLD3:
                case OpCode.LDSFLD4:
                case OpCode.LDSFLD5:
                case OpCode.LDSFLD6:
                    LdSFldM(instruction);
                    break;
                case OpCode.LDSFLD:
                    LdSFld(instruction);
                    break;
                case OpCode.STSFLD0:
                case OpCode.STSFLD1:
                case OpCode.STSFLD2:
                case OpCode.STSFLD3:
                case OpCode.STSFLD4:
                case OpCode.STSFLD5:
                case OpCode.STSFLD6:
                    StSFldM(instruction);
                    break;
                case OpCode.STSFLD:
                    StSFld(instruction);
                    break;
                case OpCode.LDLOC0:
                case OpCode.LDLOC1:
                case OpCode.LDLOC2:
                case OpCode.LDLOC3:
                case OpCode.LDLOC4:
                case OpCode.LDLOC5:
                case OpCode.LDLOC6:
                    LdLocM(instruction);
                    break;
                case OpCode.LDLOC:
                    LdLoc(instruction);
                    break;
                case OpCode.STLOC0:
                case OpCode.STLOC1:
                case OpCode.STLOC2:
                case OpCode.STLOC3:
                case OpCode.STLOC4:
                case OpCode.STLOC5:
                case OpCode.STLOC6:
                    StLocM(instruction);
                    break;
                case OpCode.STLOC:
                    StLoc(instruction);
                    break;
                case OpCode.LDARG0:
                case OpCode.LDARG1:
                case OpCode.LDARG2:
                case OpCode.LDARG3:
                case OpCode.LDARG4:
                case OpCode.LDARG5:
                case OpCode.LDARG6:
                    LdArgM(instruction);
                    break;
                case OpCode.LDARG:
                    LdArg(instruction);
                    break;
                case OpCode.STARG0:
                case OpCode.STARG1:
                case OpCode.STARG2:
                case OpCode.STARG3:
                case OpCode.STARG4:
                case OpCode.STARG5:
                case OpCode.STARG6:
                    StArgM(instruction);
                    break;
                case OpCode.STARG:
                    StArg(instruction);
                    break;

                // Bitwise logic
                case OpCode.INVERT:
                    Invert(instruction);
                    break;
                case OpCode.AND:
                    And(instruction);
                    break;
                case OpCode.OR:
                    Or(instruction);
                    break;
                case OpCode.XOR:
                    Xor(instruction);
                    break;
                case OpCode.EQUAL:
                    Equal(instruction);
                    break;
                case OpCode.NOTEQUAL:
                    NotEqual(instruction);
                    break;

                // Numeric
                case OpCode.SIGN:
                    Sign(instruction);
                    break;
                case OpCode.ABS:
                    Abs(instruction);
                    break;
                case OpCode.NEGATE:
                    Negate(instruction);
                    break;
                case OpCode.INC:
                    Inc(instruction);
                    break;
                case OpCode.DEC:
                    Dec(instruction);
                    break;
                case OpCode.ADD:
                    Add(instruction);
                    break;
                case OpCode.SUB:
                    Sub(instruction);
                    break;
                case OpCode.MUL:
                    Mul(instruction);
                    break;
                case OpCode.DIV:
                    Div(instruction);
                    break;
                case OpCode.MOD:
                    Mod(instruction);
                    break;
                case OpCode.POW:
                    Pow(instruction);
                    break;
                case OpCode.SQRT:
                    Sqrt(instruction);
                    break;
                case OpCode.MODMUL:
                    ModMul(instruction);
                    break;
                case OpCode.MODPOW:
                    ModPow(instruction);
                    break;
                case OpCode.SHL:
                    Shl(instruction);
                    break;
                case OpCode.SHR:
                    Shr(instruction);
                    break;
                case OpCode.NOT:
                    Not(instruction);
                    break;
                case OpCode.BOOLAND:
                    BoolAnd(instruction);
                    break;
                case OpCode.BOOLOR:
                    BoolOr(instruction);
                    break;
                case OpCode.NZ:
                    Nz(instruction);
                    break;
                case OpCode.NUMEQUAL:
                    NumEqual(instruction);
                    break;
                case OpCode.NUMNOTEQUAL:
                    NumNotEqual(instruction);
                    break;
                case OpCode.LT:
                    Lt(instruction);
                    break;
                case OpCode.LE:
                    Le(instruction);
                    break;
                case OpCode.GT:
                    Gt(instruction);
                    break;
                case OpCode.GE:
                    Ge(instruction);
                    break;
                case OpCode.MIN:
                    Min(instruction);
                    break;
                case OpCode.MAX:
                    Max(instruction);
                    break;
                case OpCode.WITHIN:
                    Within(instruction);
                    break;

                // Compound-type
                case OpCode.PACKMAP:
                    PackMap(instruction);
                    break;
                case OpCode.PACKSTRUCT:
                    PackStruct(instruction);
                    break;
                case OpCode.PACK:
                    Pack(instruction);
                    break;
                case OpCode.UNPACK:
                    Unpack(instruction);
                    break;
                case OpCode.NEWARRAY0:
                    NewArray0(instruction);
                    break;
                case OpCode.NEWARRAY:
                case OpCode.NEWARRAY_T:
                    NewArray_T(instruction);
                    break;
                case OpCode.NEWSTRUCT0:
                    NewStruct0(instruction);
                    break;
                case OpCode.NEWSTRUCT:
                    NewStruct(instruction);
                    break;
                case OpCode.NEWMAP:
                    NewMap(instruction);
                    break;
                case OpCode.SIZE:
                    Size(instruction);
                    break;
                case OpCode.HASKEY:
                    HasKey(instruction);
                    break;
                case OpCode.KEYS:
                    Keys(instruction);
                    break;
                case OpCode.VALUES:
                    Values(instruction);
                    break;
                case OpCode.PICKITEM:
                    PickItem(instruction);
                    break;
                case OpCode.APPEND:
                    Append(instruction);
                    break;
                case OpCode.SETITEM:
                    SetItem(instruction);
                    break;
                case OpCode.REVERSEITEMS:
                    ReverseItems(instruction);
                    break;
                case OpCode.REMOVE:
                    Remove(instruction);
                    break;
                case OpCode.CLEARITEMS:
                    ClearItems(instruction);
                    break;
                case OpCode.POPITEM:
                    PopItem(instruction);
                    break;
            }
            */
        }

    }
}
