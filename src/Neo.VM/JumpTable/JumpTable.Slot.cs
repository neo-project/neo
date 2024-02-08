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

using Neo.VM;
using Neo.VM.Types;
using System;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    public partial class JumpTable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void INITSSLOT(ExecutionEngine engine, Instruction instruction)
        {
            if (engine.CurrentContext!.StaticFields != null)
                throw new InvalidOperationException($"{instruction.OpCode} cannot be executed twice.");
            if (instruction.TokenU8 == 0)
                throw new InvalidOperationException($"The operand {instruction.TokenU8} is invalid for OpCode.{instruction.OpCode}.");
            engine.CurrentContext.StaticFields = new Slot(instruction.TokenU8, engine.ReferenceCounter);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void INITSLOT(ExecutionEngine engine, Instruction instruction)
        {
            if (engine.CurrentContext!.LocalVariables != null || engine.CurrentContext.Arguments != null)
                throw new InvalidOperationException($"{instruction.OpCode} cannot be executed twice.");
            if (instruction.TokenU16 == 0)
                throw new InvalidOperationException($"The operand {instruction.TokenU16} is invalid for OpCode.{instruction.OpCode}.");
            if (instruction.TokenU8 > 0)
            {
                engine.CurrentContext.LocalVariables = new Slot(instruction.TokenU8, engine.ReferenceCounter);
            }
            if (instruction.TokenU8_1 > 0)
            {
                var items = new StackItem[instruction.TokenU8_1];
                for (var i = 0; i < instruction.TokenU8_1; i++)
                {
                    items[i] = engine.Pop();
                }
                engine.CurrentContext.Arguments = new Slot(items, engine.ReferenceCounter);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LDSFLD0(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.StaticFields, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LDSFLD1(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.StaticFields, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LDSFLD2(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.StaticFields, 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LDSFLD3(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.StaticFields, 3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LDSFLD4(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.StaticFields, 4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LDSFLD5(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.StaticFields, 5);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LDSFLD6(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.StaticFields, 6);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LDSFLD(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.StaticFields, instruction.TokenU8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void STSFLD0(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.StaticFields, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void STSFLD1(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.StaticFields, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void STSFLD2(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.StaticFields, 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void STSFLD3(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.StaticFields, 3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void STSFLD4(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.StaticFields, 4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void STSFLD5(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.StaticFields, 5);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void STSFLD6(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.StaticFields, 6);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void STSFLD(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.StaticFields, instruction.TokenU8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LDLOC0(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.LocalVariables, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LDLOC1(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.LocalVariables, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LDLOC2(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.LocalVariables, 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LDLOC3(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.LocalVariables, 3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LDLOC4(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.LocalVariables, 4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LDLOC5(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.LocalVariables, 5);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LDLOC6(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.LocalVariables, 6);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LDLOC(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.LocalVariables, instruction.TokenU8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void STLOC0(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.LocalVariables, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void STLOC1(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.LocalVariables, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void STLOC2(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.LocalVariables, 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void STLOC3(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.LocalVariables, 3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void STLOC4(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.LocalVariables, 4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void STLOC5(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.LocalVariables, 5);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void STLOC6(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.LocalVariables, 6);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void STLOC(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.LocalVariables, instruction.TokenU8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LDARG0(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.Arguments, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LDARG1(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.Arguments, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LDARG2(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.Arguments, 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LDARG3(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.Arguments, 3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LDARG4(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.Arguments, 4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LDARG5(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.Arguments, 5);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LDARG6(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.Arguments, 6);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LDARG(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.Arguments, instruction.TokenU8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void STARG0(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.Arguments, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void STARG1(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.Arguments, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void STARG2(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.Arguments, 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void STARG3(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.Arguments, 3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void STARG4(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.Arguments, 4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void STARG5(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.Arguments, 5);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void STARG6(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.Arguments, 6);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void STARG(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.Arguments, instruction.TokenU8);
        }

        #region Execute methods

        public virtual void ExecuteStoreToSlot(ExecutionEngine engine, Slot? slot, int index)
        {
            if (slot is null)
                throw new InvalidOperationException("Slot has not been initialized.");
            if (index < 0 || index >= slot.Count)
                throw new InvalidOperationException($"Index out of range when storing to slot: {index}");
            slot[index] = engine.Pop();
        }

        public virtual void ExecuteLoadFromSlot(ExecutionEngine engine, Slot? slot, int index)
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
