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
    /// <summary>
    /// Partial class representing a jump table for executing specific operations related to slot manipulation.
    /// </summary>
    public partial class JumpTable
    {
        /// <summary>
        /// Initializes the static field slot in the current execution context.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.INITSSLOT"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void InitSSlot(ExecutionEngine engine, Instruction instruction)
        {
            if (engine.CurrentContext!.StaticFields != null)
                throw new InvalidOperationException($"{instruction.OpCode} cannot be executed twice.");
            if (instruction.TokenU8 == 0)
                throw new InvalidOperationException($"The operand {instruction.TokenU8} is invalid for OpCode.{instruction.OpCode}.");
            engine.CurrentContext.StaticFields = new Slot(instruction.TokenU8, engine.ReferenceCounter);
        }

        /// <summary>
        /// Initializes the local variable slot or the argument slot in the current execution context.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.INITSLOT"/>
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

        /// <summary>
        /// Loads the value at index 0 from the static field slot onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.LDSFLD0"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdSFld0(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.StaticFields, 0);
        }

        /// <summary>
        /// Loads the value at index 1 from the static field slot onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.LDSFLD1"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdSFld1(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.StaticFields, 1);
        }

        /// <summary>
        /// Loads the value at index 2 from the static field slot onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.LDSFLD2"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdSFld2(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.StaticFields, 2);
        }

        /// <summary>
        /// Loads the value at index 3 from the static field slot onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.LDSFLD3"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdSFld3(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.StaticFields, 3);
        }

        /// <summary>
        /// Loads the value at index 4 from the static field slot onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.LDSFLD4"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdSFld4(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.StaticFields, 4);
        }

        /// <summary>
        /// Loads the value at index 5 from the static field slot onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.LDSFLD5"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdSFld5(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.StaticFields, 5);
        }

        /// <summary>
        /// Loads the value at index 6 from the static field slot onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.LDSFLD6"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdSFld6(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.StaticFields, 6);
        }

        /// <summary>
        /// Loads the static field at a specified index onto the evaluation stack.
        /// The index is represented as a 1-byte unsigned integer.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.LDSFLD"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdSFld(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.StaticFields, instruction.TokenU8);
        }

        /// <summary>
        /// Stores the value at index 0 from the evaluation stack into the static field slot.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.STSFLD0"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StSFld0(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.StaticFields, 0);
        }

        /// <summary>
        /// Stores the value at index 1 from the evaluation stack into the static field slot.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.STSFLD1"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StSFld1(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.StaticFields, 1);
        }

        /// <summary>
        /// Stores the value at index 2 from the evaluation stack into the static field slot.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.STSFLD2"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StSFld2(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.StaticFields, 2);
        }

        /// <summary>
        /// Stores the value at index 3 from the evaluation stack into the static field slot.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.STSFLD3"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StSFld3(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.StaticFields, 3);
        }

        /// <summary>
        /// Stores the value at index 4 from the evaluation stack into the static field slot.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.STSFLD4"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StSFld4(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.StaticFields, 4);
        }

        /// <summary>
        /// Stores the value at index 5 from the evaluation stack into the static field slot.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.STSFLD5"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StSFld5(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.StaticFields, 5);
        }

        /// <summary>
        /// Stores the value at index 6 from the evaluation stack into the static field slot.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.STSFLD6"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StSFld6(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.StaticFields, 6);
        }

        /// <summary>
        /// Stores the value on top of the evaluation stack in the static field list at a specified index.
        /// The index is represented as a 1-byte unsigned integer.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.STSFLD"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StSFld(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.StaticFields, instruction.TokenU8);
        }

        /// <summary>
        /// Loads the local variable at index 0 onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.LDLOC0"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdLoc0(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.LocalVariables, 0);
        }

        /// <summary>
        /// Loads the local variable at index 1 onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.LDLOC1"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdLoc1(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.LocalVariables, 1);
        }

        /// <summary>
        /// Loads the local variable at index 2 onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.LDLOC2"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdLoc2(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.LocalVariables, 2);
        }

        /// <summary>
        /// Loads the local variable at index 3 onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.LDLOC3"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdLoc3(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.LocalVariables, 3);
        }

        /// <summary>
        /// Loads the local variable at index 4 onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.LDLOC4"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdLoc4(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.LocalVariables, 4);
        }

        /// <summary>
        /// Loads the local variable at index 5 onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.LDLOC5"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdLoc5(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.LocalVariables, 5);
        }

        /// <summary>
        /// Loads the local variable at index 6 onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.LDLOC6"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdLoc6(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.LocalVariables, 6);
        }

        /// <summary>
        /// Loads the local variable at a specified index onto the evaluation stack.
        /// The index is represented as a 1-byte unsigned integer.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.LDLOC"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdLoc(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.LocalVariables, instruction.TokenU8);
        }

        /// <summary>
        /// Stores the value on top of the evaluation stack in the local variable list at index 0.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.STLOC0"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StLoc0(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.LocalVariables, 0);
        }

        /// <summary>
        /// Stores the value on top of the evaluation stack in the local variable list at index 1.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.STLOC1"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StLoc1(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.LocalVariables, 1);
        }

        /// <summary>
        /// Stores the value on top of the evaluation stack in the local variable list at index 2.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.STLOC2"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StLoc2(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.LocalVariables, 2);
        }

        /// <summary>
        /// Stores the value on top of the evaluation stack in the local variable list at index 3.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.STLOC3"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StLoc3(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.LocalVariables, 3);
        }

        /// <summary>
        /// Stores the value on top of the evaluation stack in the local variable list at index 4.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.STLOC4"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StLoc4(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.LocalVariables, 4);
        }

        /// <summary>
        /// Stores the value on top of the evaluation stack in the local variable list at index 5.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.STLOC5"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StLoc5(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.LocalVariables, 5);
        }

        /// <summary>
        /// Stores the value on top of the evaluation stack in the local variable list at index 6.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.STLOC6"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StLoc6(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.LocalVariables, 6);
        }

        /// <summary>
        /// Stores the value on top of the evaluation stack in the local variable list at a specified index.
        /// The index is represented as a 1-byte unsigned integer.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.STLOC"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StLoc(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.LocalVariables, instruction.TokenU8);
        }

        /// <summary>
        /// Loads the argument at index 0 onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.LDARG0"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdArg0(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.Arguments, 0);
        }

        /// <summary>
        /// Loads the argument at index 1 onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.LDARG1"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdArg1(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.Arguments, 1);
        }

        /// <summary>
        /// Loads the argument at index 2 onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.LDARG2"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdArg2(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.Arguments, 2);
        }

        /// <summary>
        /// Loads the argument at index 3 onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.LDARG3"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdArg3(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.Arguments, 3);
        }

        /// <summary>
        /// Loads the argument at index 4 onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.LDARG4"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdArg4(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.Arguments, 4);
        }

        /// <summary>
        /// Loads the argument at index 5 onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.LDARG5"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdArg5(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.Arguments, 5);
        }

        /// <summary>
        /// Loads the argument at index 6 onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.LDARG6"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdArg6(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.Arguments, 6);
        }

        /// <summary>
        /// Loads the argument at a specified index onto the evaluation stack.
        /// The index is represented as a 1-byte unsigned integer.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.LDARG"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LdArg(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteLoadFromSlot(engine, engine.CurrentContext!.Arguments, instruction.TokenU8);
        }

        /// <summary>
        /// Stores the value on top of the evaluation stack in the argument slot at index 0.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.STARG0"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StArg0(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.Arguments, 0);
        }

        /// <summary>
        /// Stores the value on top of the evaluation stack in the argument slot at index 1.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.STARG1"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StArg1(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.Arguments, 1);
        }

        /// <summary>
        /// Stores the value on top of the evaluation stack in the argument slot at index 2.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.STARG2"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StArg2(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.Arguments, 2);
        }

        /// <summary>
        /// Stores the value on top of the evaluation stack in the argument slot at index 3.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.STARG3"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StArg3(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.Arguments, 3);
        }

        /// <summary>
        /// Stores the value on top of the evaluation stack in the argument slot at index 4.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.STARG4"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StArg4(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.Arguments, 4);
        }

        /// <summary>
        /// Stores the value on top of the evaluation stack in the argument slot at index 5.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.STARG5"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StArg5(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.Arguments, 5);
        }

        /// <summary>
        /// Stores the value on top of the evaluation stack in the argument slot at index 6.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.STARG6"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StArg6(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.Arguments, 6);
        }

        /// <summary>
        /// Stores the value on top of the evaluation stack in the argument slot at a specified index.
        /// The index is represented as a 1-byte unsigned integer.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.STARG"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StArg(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteStoreToSlot(engine, engine.CurrentContext!.Arguments, instruction.TokenU8);
        }

        #region Execute methods

        /// <summary>
        /// Executes the store operation into the specified slot at the given index.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="slot">The slot to store the value.</param>
        /// <param name="index">The index within the slot.</param>
        public virtual void ExecuteStoreToSlot(ExecutionEngine engine, Slot? slot, int index)
        {
            if (slot is null)
                throw new InvalidOperationException("Slot has not been initialized.");
            if (index < 0 || index >= slot.Count)
                throw new InvalidOperationException($"Index out of range when storing to slot: {index}");
            slot[index] = engine.Pop();
        }

        /// <summary>
        /// Executes the load operation from the specified slot at the given index onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="slot">The slot to load the value from.</param>
        /// <param name="index">The index within the slot.</param>
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
