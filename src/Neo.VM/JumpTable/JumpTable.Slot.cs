// Copyright (C) 2015-2024 The Neo Project.
//
// JumpTable.Slot.cs file belongs to the neo project and is free
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
        /* TODO:
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
         */


        #region Execute methods

        private void ExecuteStoreToSlot(ExecutionEngine engine, Slot? slot, int index)
        {
            if (slot is null)
                throw new InvalidOperationException("Slot has not been initialized.");
            if (index < 0 || index >= slot.Count)
                throw new InvalidOperationException($"Index out of range when storing to slot: {index}");
            slot[index] = engine.Pop();
        }

        private void ExecuteLoadFromSlot(ExecutionEngine engine, Slot? slot, int index)
        {
            if (slot is null)
                throw new InvalidOperationException("Slot has not been initialized.");
            if (index < 0 || index >= slot.Count)
                throw new InvalidOperationException($"Index out of range when loading from slot: {index}");
            engine.Push(slot[index]);
        }

        #endregion
    }
}
