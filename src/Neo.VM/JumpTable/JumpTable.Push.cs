// Copyright (C) 2015-2024 The Neo Project.
//
// JumpTable.Push.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    /// <summary>
    /// Partial class for providing methods to push various data types onto the evaluation stack within a jump table.
    /// </summary>
    public partial class JumpTable
    {
        /// <summary>
        /// Pushes an 8-bit signed integer onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.PUSHINT8"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PushInt8(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(new BigInteger(instruction.Operand.Span));
        }

        /// <summary>
        /// Pushes an 16-bit signed integer onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <summary>
        /// <see cref="OpCode.PUSHINT16"/>
        public virtual void PushInt16(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(new BigInteger(instruction.Operand.Span));
        }

        /// <summary>
        /// Pushes an 32-bit signed integer onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <summary>
        /// <see cref="OpCode.PUSHINT32"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PushInt32(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(new BigInteger(instruction.Operand.Span));
        }

        /// <summary>
        /// Pushes an 64-bit signed integer onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <summary>
        /// <see cref="OpCode.PUSHINT64"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PushInt64(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(new BigInteger(instruction.Operand.Span));
        }

        /// <summary>
        /// Pushes an 128-bit signed integer onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <summary>
        /// <see cref="OpCode.PUSHINT128"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PushInt128(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(new BigInteger(instruction.Operand.Span));
        }

        /// <summary>
        /// Pushes an 256-bit signed integer onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <summary>
        /// <see cref="OpCode.PUSHINT256"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PushInt256(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(new BigInteger(instruction.Operand.Span));
        }

        /// <summary>
        /// Pushes a boolean value of true onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <summary>
        /// <see cref="OpCode.PUSHT"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PushT(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(StackItem.True);
        }

        /// <summary>
        /// Pushes a boolean value of false onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <summary>
        /// <see cref="OpCode.PUSHF"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PushF(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(StackItem.False);
        }

        /// <summary>
        /// Pushes the address of the specified instruction onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <summary>
        /// <see cref="OpCode.PUSHA"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PushA(ExecutionEngine engine, Instruction instruction)
        {
            var position = checked(engine.CurrentContext!.InstructionPointer + instruction.TokenI32);
            if (position < 0 || position > engine.CurrentContext.Script.Length)
                throw new InvalidOperationException($"Bad pointer address(Instruction instruction) {position}");
            engine.Push(new Pointer(engine.CurrentContext.Script, position));
        }

        /// <summary>
        /// Pushes a null onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <summary>
        /// <see cref="OpCode.PUSHNULL"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PushNull(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(StackItem.Null);
        }

        /// <summary>
        /// Pushes a byte array with a length prefix onto the evaluation stack.
        /// The length of the array is 1 byte.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <summary>
        /// <see cref="OpCode.PUSHDATA1"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PushData1(ExecutionEngine engine, Instruction instruction)
        {
            engine.Limits.AssertMaxItemSize(instruction.Operand.Length);
            engine.Push(instruction.Operand);
        }

        /// <summary>
        /// Pushes a byte array with a length prefix onto the evaluation stack.
        /// The length of the array is 1 bytes.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <summary>
        /// <see cref="OpCode.PUSHDATA2"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PushData2(ExecutionEngine engine, Instruction instruction)
        {
            engine.Limits.AssertMaxItemSize(instruction.Operand.Length);
            engine.Push(instruction.Operand);
        }

        /// <summary>
        /// Pushes a byte array with a length prefix onto the evaluation stack.
        /// The length of the array is 4 bytes.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <summary>
        /// <see cref="OpCode.PUSHDATA4"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PushData4(ExecutionEngine engine, Instruction instruction)
        {
            engine.Limits.AssertMaxItemSize(instruction.Operand.Length);
            engine.Push(instruction.Operand);
        }

        /// <summary>
        /// Pushes the integer value of -1 onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <summary>
        /// <see cref="OpCode.PUSHM1"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PushM1(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(-1);
        }

        /// <summary>
        /// Pushes the integer value of 0 onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <summary>
        /// <see cref="OpCode.PUSH0"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Push0(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(0);
        }

        /// <summary>
        /// Pushes the integer value of 1 onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <summary>
        /// <see cref="OpCode.PUSH1"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Push1(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(1);
        }

        /// <summary>
        /// Pushes the integer value of 2 onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <summary>
        /// <see cref="OpCode.PUSH2"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Push2(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(2);
        }

        /// <summary>
        /// Pushes the integer value of 3 onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <summary>
        /// <see cref="OpCode.PUSH3"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Push3(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(3);
        }

        /// <summary>
        /// Pushes the integer value of 4 onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <summary>
        /// <see cref="OpCode.PUSH4"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Push4(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(4);
        }

        /// <summary>
        /// Pushes the integer value of 5 onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <summary>
        /// <see cref="OpCode.PUSH5"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Push5(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(5);
        }

        /// <summary>
        /// Pushes the integer value of 6 onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <summary>
        /// <see cref="OpCode.PUSH6"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Push6(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(6);
        }

        /// <summary>
        /// Pushes the integer value of 7 onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <summary>
        /// <see cref="OpCode.PUSH7"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Push7(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(7);
        }

        /// <summary>
        /// Pushes the integer value of 8 onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <summary>
        /// <see cref="OpCode.PUSH8"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Push8(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(8);
        }

        /// <summary>
        /// Pushes the integer value of 9 onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <summary>
        /// <see cref="OpCode.PUSH9"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Push9(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(9);
        }

        /// <summary>
        /// Pushes the integer value of 10 onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <summary>
        /// <see cref="OpCode.PUSH10"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Push10(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(10);
        }

        /// <summary>
        /// Pushes the integer value of 11 onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <summary>
        /// <see cref="OpCode.PUSH11"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Push11(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(11);
        }

        /// <summary>
        /// Pushes the integer value of 12 onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <summary>
        /// <see cref="OpCode.PUSH12"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Push12(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(12);
        }

        /// <summary>
        /// Pushes the integer value of 13 onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <summary>
        /// <see cref="OpCode.PUSH13"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Push13(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(13);
        }

        /// <summary>
        /// Pushes the integer value of 14 onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <summary>
        /// <see cref="OpCode.PUSH14"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Push14(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(14);
        }

        /// <summary>
        /// Pushes the integer value of 15 onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <summary>
        /// <see cref="OpCode.PUSH15"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Push15(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(15);
        }

        /// <summary>
        /// Pushes the integer value of 16 onto the evaluation stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <summary>
        /// <see cref="OpCode.PUSH16"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Push16(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(16);
        }
    }
}
