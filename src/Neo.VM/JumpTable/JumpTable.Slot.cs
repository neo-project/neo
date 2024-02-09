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
        public virtual void InitSSlot(ExecutionEngine engine, Instruction instruction)
        {
            if (engine.CurrentContext!.StaticFields != null)
                throw new InvalidOperationException($"{instruction.OpCode} cannot be executed twice.");
            if (instruction.TokenU8 == 0)
                throw new InvalidOperationException($"The operand {instruction.TokenU8} is invalid for OpCode.{instruction.OpCode}.");
            engine.CurrentContext.StaticFields = new Slot(instruction.TokenU8, engine.ReferenceCounter);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void InitSlot(ExecutionEngine engine, Instruction instruction)
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
        public virtual void LdSFld0(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.StaticFields, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdSFld1(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.StaticFields, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdSFld2(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.StaticFields, 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdSFld3(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.StaticFields, 3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdSFld4(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.StaticFields, 4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdSFld5(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.StaticFields, 5);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdSFld6(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.StaticFields, 6);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdSFld(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.StaticFields, instruction.TokenU8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StSFld0(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.StaticFields, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StSFld1(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.StaticFields, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StSFld2(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.StaticFields, 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StSFld3(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.StaticFields, 3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StSFld4(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.StaticFields, 4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StSFld5(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.StaticFields, 5);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StSFld6(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.StaticFields, 6);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StSFld(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.StaticFields, instruction.TokenU8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdLoc0(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.LocalVariables, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdLoc1(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.LocalVariables, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdLoc2(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.LocalVariables, 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdLoc3(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.LocalVariables, 3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdLoc4(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.LocalVariables, 4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdLoc5(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.LocalVariables, 5);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdLoc6(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.LocalVariables, 6);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdLoc(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.LocalVariables, instruction.TokenU8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StLoc0(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.LocalVariables, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StLoc1(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.LocalVariables, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StLoc2(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.LocalVariables, 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StLoc3(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.LocalVariables, 3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StLoc4(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.LocalVariables, 4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StLoc5(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.LocalVariables, 5);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StLoc6(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.LocalVariables, 6);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StLoc(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.LocalVariables, instruction.TokenU8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdArg0(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.Arguments, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdArg1(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.Arguments, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdArg2(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.Arguments, 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdArg3(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.Arguments, 3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdArg4(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.Arguments, 4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdArg5(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.Arguments, 5);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdArg6(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.Arguments, 6);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdArg(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.Arguments, instruction.TokenU8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StArg0(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.Arguments, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StArg1(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.Arguments, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StArg2(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.Arguments, 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StArg3(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.Arguments, 3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StArg4(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.Arguments, 4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StArg5(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.Arguments, 5);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StArg6(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.Arguments, 6);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StArg(ExecutionEngine engine, Instruction instruction)
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
