// Copyright (C) 2015-2025 The Neo Project.
//
// JumpTable.Types.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;
using System;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    /// <summary>
    /// Partial class for type operations in the execution engine within a jump table.
    /// </summary>
    public partial class JumpTable
    {
        /// <summary>
        /// Determines whether the item on top of the evaluation stack is null.
        /// <see cref="OpCode.ISNULL"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <remarks>Pop 1, Push 1</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void IsNull(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop();
            engine.Push(x.IsNull);
        }

        /// <summary>
        /// Determines whether the item on top of the evaluation stack has a specified type.
        /// <see cref="OpCode.ISTYPE"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <remarks>Pop 1, Push 1</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void IsType(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop();
            var type = (StackItemType)instruction.TokenU8;
            if (type == StackItemType.Any || !Enum.IsDefined(typeof(StackItemType), type))
                throw new InvalidOperationException($"Invalid type: {type}");
            engine.Push(x.Type == type);
        }

        /// <summary>
        /// Converts the item on top of the evaluation stack to a specified type.
        /// <see cref="OpCode.CONVERT"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <remarks>Pop 1, Push 1</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Convert(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop();
            engine.Push(x.ConvertTo((StackItemType)instruction.TokenU8));
        }

        /// <summary>
        /// Aborts execution with a specified message.
        /// <see cref="OpCode.ABORTMSG"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <remarks>Pop 1, Push 0</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void AbortMsg(ExecutionEngine engine, Instruction instruction)
        {
            var msg = engine.Pop().GetString();
            throw new Exception($"{OpCode.ABORTMSG} is executed. Reason: {msg}");
        }

        /// <summary>
        /// Asserts a condition with a specified message, throwing an exception if the condition is false.
        /// <see cref="OpCode.ASSERTMSG"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <remarks>Pop 2, Push 0</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void AssertMsg(ExecutionEngine engine, Instruction instruction)
        {
            var msg = engine.Pop().GetString();
            var x = engine.Pop().GetBoolean();
            if (!x)
                throw new Exception($"{OpCode.ASSERTMSG} is executed with false result. Reason: {msg}");
        }
    }
}
