// Copyright (C) 2015-2025 The Neo Project.
//
// JumpTable.Control.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    /// <summary>
    /// Partial class for performing bitwise and logical operations on integers within a jump table.
    /// </summary>
    /// <remarks>
    /// For binary operations x1 and x2, x1 is the first pushed onto the evaluation stack (the second popped from the stack),
    /// x2 is the second pushed onto the evaluation stack (the first popped from the stack)
    /// </remarks>
    public partial class JumpTable
    {
        /// <summary>
        /// No operation. Does nothing.
        /// <see cref="OpCode.NOP"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Nop(ExecutionEngine engine, Instruction instruction)
        {
        }

        /// <summary>
        /// Jumps to the specified offset from the current instruction pointer,
        /// where the offset is obtained from the first operand of the instruction and interpreted as a signed byte.
        /// <see cref="OpCode.JMP"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction containing the offset as the first operand.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Jmp(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteJumpOffset(engine, instruction.TokenI8);
        }

        /// <summary>
        /// Jumps to the specified offset from the current instruction pointer,
        /// where the offset is obtained from the first operand of the instruction and interpreted as a 32-bit signed integer.
        /// <see cref="OpCode.JMP_L"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction containing the offset as the first operand.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Jmp_L(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteJumpOffset(engine, instruction.TokenI32);
        }

        /// <summary>
        /// Jumps to the specified offset from the current instruction pointer
        /// if the boolean result of popping the evaluation stack is true.
        /// The offset is obtained from the instruction's first operand interpreted as a signed byte.
        /// <see cref="OpCode.JMPIF"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction containing the offset as the first operand.</param>
        /// <remarks>Pop 1, Push 0</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JmpIf(ExecutionEngine engine, Instruction instruction)
        {
            if (engine.Pop().GetBoolean())
                ExecuteJumpOffset(engine, instruction.TokenI8);
        }

        /// <summary>
        /// Jumps to the specified offset from the current instruction pointer
        /// if the boolean result of popping the evaluation stack is true.
        /// The offset is obtained from the instruction's first operand interpreted as a 32-bit signed integer.
        /// <see cref="OpCode.JMPIF_L"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction containing the offset as the first operand.</param>
        /// <remarks>Pop 1, Push 0</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JmpIf_L(ExecutionEngine engine, Instruction instruction)
        {
            if (engine.Pop().GetBoolean())
                ExecuteJumpOffset(engine, instruction.TokenI32);
        }

        /// <summary>
        /// Jumps to the specified offset from the current instruction pointer
        /// if the boolean result of popping the evaluation stack is false.
        /// The offset is obtained from the instruction's first operand interpreted as a signed byte.
        /// <see cref="OpCode.JMPIFNOT"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction containing the offset as the first operand.</param>
        /// <remarks>Pop 1, Push 0</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JmpIfNot(ExecutionEngine engine, Instruction instruction)
        {
            if (!engine.Pop().GetBoolean())
                ExecuteJumpOffset(engine, instruction.TokenI8);
        }

        /// <summary>
        /// Jumps to the specified offset from the current instruction pointer
        /// if the boolean result of popping the evaluation stack is false.
        /// The offset is obtained from the instruction's first operand interpreted as a 32-bit signed integer.
        /// <see cref="OpCode.JMPIFNOT_L"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction containing the offset as the first operand.</param>
        /// <remarks>Pop 1, Push 0</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JmpIfNot_L(ExecutionEngine engine, Instruction instruction)
        {
            if (!engine.Pop().GetBoolean())
                ExecuteJumpOffset(engine, instruction.TokenI32);
        }

        /// <summary>
        /// Jumps to the specified offset from the current instruction pointer
        /// if the two integers popped from the evaluation stack are equal.
        /// The offset is obtained from the instruction's first operand interpreted as a signed byte.
        /// <see cref="OpCode.JMPEQ"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction containing the offset as the first operand.</param>
        /// <remarks>Pop 2, Push 0</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JmpEq(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 == x2)
                ExecuteJumpOffset(engine, instruction.TokenI8);
        }

        /// <summary>
        /// Jumps to the specified offset from the current instruction pointer
        /// if the two integers popped from the evaluation stack are equal.
        /// The offset is obtained from the instruction's first operand interpreted as a 32-bit signed integer.
        /// <see cref="OpCode.JMPEQ_L"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction containing the offset as the first operand.</param>
        /// <remarks>Pop 2, Push 0</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JmpEq_L(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 == x2)
                ExecuteJumpOffset(engine, instruction.TokenI32);
        }

        /// <summary>
        /// Jumps to the specified offset from the current instruction pointer
        /// if the two integers popped from the evaluation stack are not equal.
        /// The offset is obtained from the instruction's first operand interpreted as a signed byte.
        /// <see cref="OpCode.JMPNE"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction containing the offset as the first operand.</param>
        /// <remarks>Pop 2, Push 0</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JmpNe(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 != x2)
                ExecuteJumpOffset(engine, instruction.TokenI8);
        }

        /// <summary>
        /// Jumps to the specified offset from the current instruction pointer
        /// if the two integers popped from the evaluation stack are not equal.
        /// The offset is obtained from the instruction's first operand interpreted as a 32-bit signed integer.
        /// <see cref="OpCode.JMPNE_L"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction containing the offset as the first operand.</param>
        /// <remarks>Pop 2, Push 0</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JmpNe_L(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 != x2)
                ExecuteJumpOffset(engine, instruction.TokenI32);
        }

        /// <summary>
        /// Jumps to the specified offset from the current instruction pointer
        /// if the first integer pushed onto the evaluation stack is greater than the second integer.
        /// The offset is obtained from the instruction's first operand interpreted as a signed byte.
        /// <see cref="OpCode.JMPGT"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction containing the offset as the first operand.</param>
        /// <remarks>Pop 2, Push 0</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JmpGt(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 > x2)
                ExecuteJumpOffset(engine, instruction.TokenI8);
        }

        /// <summary>
        /// Jumps to the specified offset from the current instruction pointer
        /// if the first integer pushed onto the evaluation stack is greater than the second integer.
        /// The offset is obtained from the instruction's first operand interpreted as a 32-bit signed integer.
        /// <see cref="OpCode.JMPGT_L"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction containing the offset as the first operand.</param>
        /// <remarks>Pop 2, Push 0</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JmpGt_L(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 > x2)
                ExecuteJumpOffset(engine, instruction.TokenI32);
        }

        /// <summary>
        /// Jumps to the specified offset from the current instruction pointer
        /// if the first integer pushed onto the evaluation stack is greater than or equal to the second integer.
        /// The offset is obtained from the instruction's first operand interpreted as a signed byte.
        /// <see cref="OpCode.JMPGE"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction containing the offset as the first operand.</param>
        /// <remarks>Pop 2, Push 0</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JmpGe(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 >= x2)
                ExecuteJumpOffset(engine, instruction.TokenI8);
        }

        /// <summary>
        /// Jumps to the specified offset from the current instruction pointer
        /// if the first integer pushed onto the evaluation stack is greater than or equal to the second integer.
        /// The offset is obtained from the instruction's first operand interpreted as a 32-bit signed integer.
        /// <see cref="OpCode.JMPGE_L"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction containing the offset as the first operand.</param>
        /// <remarks>Pop 2, Push 0</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JmpGe_L(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 >= x2)
                ExecuteJumpOffset(engine, instruction.TokenI32);
        }

        /// <summary>
        /// Jumps to the specified offset from the current instruction pointer
        /// if the first integer pushed onto the evaluation stack is less than the second integer.
        /// The offset is obtained from the instruction's first operand interpreted as a signed byte.
        /// <see cref="OpCode.JMPLT"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction containing the offset as the first operand.</param>
        /// <remarks>Pop 2, Push 0</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JmpLt(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 < x2)
                ExecuteJumpOffset(engine, instruction.TokenI8);
        }

        /// <summary>
        /// Jumps to the specified offset from the current instruction pointer
        /// if the first integer pushed onto the evaluation stack is less than the second integer.
        /// The offset is obtained from the instruction's first operand interpreted as a 32-bit signed integer.
        /// <see cref="OpCode.JMPLT_L"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction containing the offset as the first operand.</param>
        /// <remarks>Pop 2, Push 0</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JmpLt_L(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 < x2)
                ExecuteJumpOffset(engine, instruction.TokenI32);
        }

        /// <summary>
        /// Jumps to the specified offset from the current instruction pointer
        /// if the first integer pushed onto the evaluation stack is less than or equal to the second integer.
        /// The offset is obtained from the instruction's first operand interpreted as a signed byte.
        /// <see cref="OpCode.JMPLE"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction containing the offset as the first operand.</param>
        /// <remarks>Pop 2, Push 0</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JmpLe(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 <= x2)
                ExecuteJumpOffset(engine, instruction.TokenI8);
        }

        /// <summary>
        /// Jumps to the specified offset from the current instruction pointer
        /// if the first integer pushed onto the evaluation stack is less than or equal to the second integer.
        /// The offset is obtained from the instruction's first operand interpreted as a 32-bit signed integer.
        /// <see cref="OpCode.JMPLE_L"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction containing the offset as the first operand.</param>
        /// <remarks>Pop 2, Push 0</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JmpLe_L(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 <= x2)
                ExecuteJumpOffset(engine, instruction.TokenI32);
        }

        /// <summary>
        /// Calls a method specified by the offset from the current instruction pointer.
        /// The offset is obtained from the instruction's first operand interpreted as a signed byte.
        /// <see cref="OpCode.CALL"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction containing the offset as the first operand.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Call(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteCall(engine, checked(engine.CurrentContext!.InstructionPointer + instruction.TokenI8));
        }

        /// <summary>
        /// Calls a method specified by the offset from the current instruction pointer.
        /// The offset is obtained from the instruction's first operand interpreted as a 32-bit signed integer.
        /// <see cref="OpCode.CALL_L"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction containing the offset as the first operand.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Call_L(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteCall(engine, checked(engine.CurrentContext!.InstructionPointer + instruction.TokenI32));
        }

        /// <summary>
        /// Calls a method specified by the pointer pushed onto the evaluation stack.
        /// It verifies if the pointer belongs to the current script.
        /// <see cref="OpCode.CALLA"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The current instruction.</param>
        /// <remarks>Pop 1, Push 0</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void CallA(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop<Pointer>();
            if (x.Script != engine.CurrentContext!.Script)
                throw new InvalidOperationException("Pointers can't be shared between scripts");
            ExecuteCall(engine, x.Position);
        }

        /// <summary>
        /// Calls the function described by the token.
        /// <see cref="OpCode.CALLT"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The current instruction.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void CallT(ExecutionEngine engine, Instruction instruction)
        {
            throw new InvalidOperationException($"Token not found: {instruction.TokenU16}");
        }

        /// <summary>
        /// Aborts the execution by turning the virtual machine state to FAULT immediately, and the exception cannot be caught.
        /// <see cref="OpCode.ABORT"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The current instruction.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Abort(ExecutionEngine engine, Instruction instruction)
        {
            throw new Exception($"{OpCode.ABORT} is executed.");
        }

        /// <summary>
        /// Pop the top value of the stack. If it's false, exit vm execution and set vm state to FAULT.
        /// <see cref="OpCode.ASSERT"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The current instruction.</param>
        /// <remarks>Pop 1, Push 0</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Assert(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop().GetBoolean();
            if (!x)
                throw new Exception($"{OpCode.ASSERT} is executed with false result.");
        }

        /// <summary>
        /// Pop the top value of the stack, and throw it.
        /// <see cref="OpCode.THROW"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The current instruction.</param>
        /// <remarks>Pop 1, Push 0</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Throw(ExecutionEngine engine, Instruction instruction)
        {
            ExecuteThrow(engine, engine.Pop());
        }

        /// <summary>
        /// Initiates a try block with the specified catch and finally offsets.
        /// If there's no catch block, set CatchOffset to 0. If there's no finally block, set FinallyOffset to 0.
        /// where the catch offset is obtained from the first operand of the instruction and interpreted as a signed byte，
        /// the catch offset is obtained from the second operand of the instruction and interpreted as a signed byte.
        /// <see cref="OpCode.TRY"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The current instruction.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Try(ExecutionEngine engine, Instruction instruction)
        {
            int catchOffset = instruction.TokenI8;
            int finallyOffset = instruction.TokenI8_1;
            ExecuteTry(engine, catchOffset, finallyOffset);
        }

        /// <summary>
        /// Initiates a try block with the specified catch and finally offsets.
        /// If there's no catch block, set CatchOffset to 0. If there's no finally block, set FinallyOffset to 0.
        /// where the catch offset is obtained from the first operand of the instruction and interpreted as a 32-bit signed integer，
        /// the catch offset is obtained from the second operand of the instruction and interpreted as a 32-bit signed integer.
        /// <see cref="OpCode.TRY_L"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The current instruction.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Try_L(ExecutionEngine engine, Instruction instruction)
        {
            var catchOffset = instruction.TokenI32;
            var finallyOffset = instruction.TokenI32_1;
            ExecuteTry(engine, catchOffset, finallyOffset);
        }

        /// <summary>
        /// Ensures that the appropriate surrounding finally blocks are executed,
        /// then unconditionally transfers control to the specific target instruction represented as a 1-byte signed offset
        /// from the beginning of the current instruction.
        /// <see cref="OpCode.ENDTRY"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The current instruction.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void EndTry(ExecutionEngine engine, Instruction instruction)
        {
            var endOffset = instruction.TokenI8;
            ExecuteEndTry(engine, endOffset);
        }

        /// <summary>
        /// Ensures that the appropriate surrounding finally blocks are executed,
        /// then unconditionally transfers control to the specific target instruction represented as a 4-byte signed offset
        /// from the beginning of the current instruction.
        /// <see cref="OpCode.ENDTRY_L"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The current instruction.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void EndTry_L(ExecutionEngine engine, Instruction instruction)
        {
            var endOffset = instruction.TokenI32;
            ExecuteEndTry(engine, endOffset);
        }

        /// <summary>
        /// Ends the finally block. If no exception occurs or is caught,
        /// the VM jumps to the target instruction specified by ENDTRY/ENDTRY_L.
        /// Otherwise, the VM rethrows the exception to the upper layer.
        /// <see cref="OpCode.ENDFINALLY"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The current instruction.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void EndFinally(ExecutionEngine engine, Instruction instruction)
        {
            if (engine.CurrentContext!.TryStack is null)
                throw new InvalidOperationException($"The corresponding TRY block cannot be found.");
            if (!engine.CurrentContext.TryStack.TryPop(out var currentTry))
                throw new InvalidOperationException($"The corresponding TRY block cannot be found.");

            if (engine.UncaughtException is null)
                engine.CurrentContext.InstructionPointer = currentTry.EndPointer;
            else
                ExecuteThrow(engine, engine.UncaughtException);

            engine.isJumping = true;
        }

        /// <summary>
        /// Returns from the current method.
        /// <see cref="OpCode.RET"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The current instruction.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Ret(ExecutionEngine engine, Instruction instruction)
        {
            var context_pop = engine.InvocationStack.Pop();
            var stack_eval = engine.InvocationStack.Count == 0 ? engine.ResultStack : engine.InvocationStack.Peek().EvaluationStack;
            if (context_pop.EvaluationStack != stack_eval)
            {
                if (context_pop.RVCount >= 0 && context_pop.EvaluationStack.Count != context_pop.RVCount)
                    // This exception indicates a mismatch between the expected and actual number of stack items.
                    // It typically occurs due to compilation errors caused by potential issues in the compiler, resulting in either too many or too few
                    // items left on the stack compared to what was anticipated by the return value count.
                    // When you run into this problem, try to reach core-devs at https://github.com/neo-project/neo for help.
                    throw new InvalidOperationException($"Return value count mismatch: expected {context_pop.RVCount}, but got {context_pop.EvaluationStack.Count} items on the evaluation stack");
                context_pop.EvaluationStack.CopyTo(stack_eval);
            }
            if (engine.InvocationStack.Count == 0)
                engine.State = VMState.HALT;
            engine.UnloadContext(context_pop);
            engine.isJumping = true;
        }

        /// <summary>
        /// Calls to an interop service.
        /// <see cref="OpCode.SYSCALL"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The current instruction.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Syscall(ExecutionEngine engine, Instruction instruction)
        {
            throw new InvalidOperationException($"Syscall not found: {instruction.TokenU32}");
        }

        #region Execute methods

        /// <summary>
        /// Executes a call operation by loading a new execution context at the specified position.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="position">The position to load the new execution context.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void ExecuteCall(ExecutionEngine engine, int position)
        {
            engine.LoadContext(engine.CurrentContext!.Clone(position));
        }

        /// <summary>
        /// Executes the end of a try block, either popping it from the try stack or transitioning to the finally block.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="endOffset">The offset to the end of the try block.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void ExecuteEndTry(ExecutionEngine engine, int endOffset)
        {
            if (engine.CurrentContext!.TryStack is null)
                throw new InvalidOperationException($"The corresponding TRY block cannot be found.");
            if (!engine.CurrentContext.TryStack.TryPeek(out var currentTry))
                throw new InvalidOperationException($"The corresponding TRY block cannot be found.");
            if (currentTry.State == ExceptionHandlingState.Finally)
                throw new InvalidOperationException($"The opcode {OpCode.ENDTRY} can't be executed in a FINALLY block.");

            var endPointer = checked(engine.CurrentContext.InstructionPointer + endOffset);
            if (currentTry.HasFinally)
            {
                currentTry.State = ExceptionHandlingState.Finally;
                currentTry.EndPointer = endPointer;
                engine.CurrentContext.InstructionPointer = currentTry.FinallyPointer;
            }
            else
            {
                engine.CurrentContext.TryStack.Pop();
                engine.CurrentContext.InstructionPointer = endPointer;
            }
            engine.isJumping = true;
        }

        /// <summary>
        /// Executes a jump operation to the specified position.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="position">The position to jump to.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void ExecuteJump(ExecutionEngine engine, int position)
        {
            if (position < 0 || position >= engine.CurrentContext!.Script.Length)
                throw new ArgumentOutOfRangeException($"Jump out of range for position: {position}");
            engine.CurrentContext.InstructionPointer = position;
            engine.isJumping = true;
        }

        /// <summary>
        /// Executes a jump operation with the specified offset from the current instruction pointer.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="offset">The offset from the current instruction pointer.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void ExecuteJumpOffset(ExecutionEngine engine, int offset)
        {
            ExecuteJump(engine, checked(engine.CurrentContext!.InstructionPointer + offset));
        }

        /// <summary>
        /// Executes a try block operation with the specified catch and finally offsets.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="catchOffset">The catch block offset.</param>
        /// <param name="finallyOffset">The finally block offset.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void ExecuteTry(ExecutionEngine engine, int catchOffset, int finallyOffset)
        {
            if (catchOffset == 0 && finallyOffset == 0)
                throw new InvalidOperationException($"catchOffset and finallyOffset can't be 0 in a TRY block");
            if (engine.CurrentContext!.TryStack is null)
                engine.CurrentContext.TryStack = new Stack<ExceptionHandlingContext>();
            else if (engine.CurrentContext.TryStack.Count >= engine.Limits.MaxTryNestingDepth)
                throw new InvalidOperationException("MaxTryNestingDepth exceed.");
            var catchPointer = catchOffset == 0 ? -1 : checked(engine.CurrentContext.InstructionPointer + catchOffset);
            var finallyPointer = finallyOffset == 0 ? -1 : checked(engine.CurrentContext.InstructionPointer + finallyOffset);
            engine.CurrentContext.TryStack.Push(new ExceptionHandlingContext(catchPointer, finallyPointer));
        }

        /// <summary>
        /// Executes a throw operation, handling any surrounding try-catch-finally blocks.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="ex">The exception to throw.</param>
        public virtual void ExecuteThrow(ExecutionEngine engine, StackItem? ex)
        {
            engine.UncaughtException = ex;

            var pop = 0;
            foreach (var executionContext in engine.InvocationStack)
            {
                if (executionContext.TryStack != null)
                {
                    while (executionContext.TryStack.TryPeek(out var tryContext))
                    {
                        if (tryContext.State == ExceptionHandlingState.Finally || (tryContext.State == ExceptionHandlingState.Catch && !tryContext.HasFinally))
                        {
                            executionContext.TryStack.Pop();
                            continue;
                        }
                        for (var i = 0; i < pop; i++)
                        {
                            engine.UnloadContext(engine.InvocationStack.Pop());
                        }
                        if (tryContext.State == ExceptionHandlingState.Try && tryContext.HasCatch)
                        {
                            tryContext.State = ExceptionHandlingState.Catch;
                            engine.Push(engine.UncaughtException!);
                            executionContext.InstructionPointer = tryContext.CatchPointer;
                            engine.UncaughtException = null;
                        }
                        else
                        {
                            tryContext.State = ExceptionHandlingState.Finally;
                            executionContext.InstructionPointer = tryContext.FinallyPointer;
                        }
                        engine.isJumping = true;
                        return;
                    }
                }
                ++pop;
            }

            throw new VMUnhandledException(engine.UncaughtException!);
        }

        #endregion
    }
}
