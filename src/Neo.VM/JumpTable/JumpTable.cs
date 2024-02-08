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

namespace Neo.VM
{
    public partial class JumpTable
    {
        public delegate void DelAction(ExecutionEngine engine, Instruction instruction);
        protected readonly DelAction[] Table = new DelAction[byte.MaxValue];

        public DelAction this[OpCode opCode]
        {
            get
            {
                return Table[(byte)opCode];
            }
            set
            {
                Table[(byte)opCode] = value;
            }
        }

        public JumpTable()
        {
            // Fill defined

            foreach (var mi in GetType().GetMethods())
            {
                if (Enum.TryParse<OpCode>(mi.Name, false, out var opCode))
                {
                    if (Table[(byte)opCode] is not null)
                    {
                        throw new InvalidOperationException($"Opcode {opCode} is already defined.");
                    }

                    Table[(byte)opCode] = (DelAction)mi.CreateDelegate(typeof(DelAction), this);
                }
            }

            // Fill with undefined

            for (var x = 0; x < Table.Length; x++)
            {
                if (Table[x] is not null) continue;

                Table[x] = (engine, instruction) =>
                {
                    throw new InvalidOperationException($"Opcode {instruction.OpCode} is undefined.");
                };
            }

            /* TODO:

            switch (instruction.OpCode)
            {
                //Slot
                case OpCode.INITSSLOT:
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
