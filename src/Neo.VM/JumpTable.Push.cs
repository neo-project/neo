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
    public partial class JumpTable
    {
        [OpcodeMethod(OpCode.PUSHINT8)]
        [OpcodeMethod(OpCode.PUSHINT16)]
        [OpcodeMethod(OpCode.PUSHINT32)]
        [OpcodeMethod(OpCode.PUSHINT64)]
        [OpcodeMethod(OpCode.PUSHINT128)]
        [OpcodeMethod(OpCode.PUSHINT256)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PushInt(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(new BigInteger(instruction.Operand.Span));
        }

        [OpcodeMethod(OpCode.PUSHT)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PushT(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(StackItem.True);
        }

        [OpcodeMethod(OpCode.PUSHF)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PushF(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(StackItem.False);
        }

        [OpcodeMethod(OpCode.PUSHA)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PushA(ExecutionEngine engine, Instruction instruction)
        {
            var position = checked(engine.CurrentContext!.InstructionPointer + instruction.TokenI32);
            if (position < 0 || position > engine.CurrentContext.Script.Length)
                throw new InvalidOperationException($"Bad pointer address(Instruction instruction) {position}");
            engine.Push(new Pointer(engine.CurrentContext.Script, position));
        }

        [OpcodeMethod(OpCode.PUSHNULL)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PushNull(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(StackItem.Null);
        }

        [OpcodeMethod(OpCode.PUSHDATA1)]
        [OpcodeMethod(OpCode.PUSHDATA2)]
        [OpcodeMethod(OpCode.PUSHDATA4)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PushData(ExecutionEngine engine, Instruction instruction)
        {
            engine.Limits.AssertMaxItemSize(instruction.Operand.Length);
            engine.Push(instruction.Operand);
        }

        [OpcodeMethod(OpCode.PUSHM1)]
        [OpcodeMethod(OpCode.PUSH0)]
        [OpcodeMethod(OpCode.PUSH1)]
        [OpcodeMethod(OpCode.PUSH2)]
        [OpcodeMethod(OpCode.PUSH3)]
        [OpcodeMethod(OpCode.PUSH4)]
        [OpcodeMethod(OpCode.PUSH5)]
        [OpcodeMethod(OpCode.PUSH6)]
        [OpcodeMethod(OpCode.PUSH7)]
        [OpcodeMethod(OpCode.PUSH8)]
        [OpcodeMethod(OpCode.PUSH9)]
        [OpcodeMethod(OpCode.PUSH10)]
        [OpcodeMethod(OpCode.PUSH11)]
        [OpcodeMethod(OpCode.PUSH12)]
        [OpcodeMethod(OpCode.PUSH13)]
        [OpcodeMethod(OpCode.PUSH14)]
        [OpcodeMethod(OpCode.PUSH15)]
        [OpcodeMethod(OpCode.PUSH16)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Push(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push((int)instruction.OpCode - (int)OpCode.PUSH0);
        }
    }
}
