// Copyright (C) 2015-2024 The Neo Project.
//
// JumpTable.Bitwisee.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Runtime.CompilerServices;

namespace Neo.VM
{
    /// <summary>
    /// Partial class for performing bitwise and logical operations on integers within a jump table.
    /// </summary>
    public partial class JumpTable
    {
        /// <summary>
        /// Flips all of the bits of an integer.
        /// <see cref="OpCode.INVERT"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Invert(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop().GetInteger();
            engine.Push(~x);
        }

        /// <summary>
        /// Computes the bitwise AND of two integers.
        /// <see cref="OpCode.AND"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void And(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(x1 & x2);
        }

        /// <summary>
        /// Computes the bitwise OR of two integers.
        /// <see cref="OpCode.OR"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Or(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(x1 | x2);
        }

        /// <summary>
        /// Computes the bitwise XOR (exclusive OR) of two integers.
        /// <see cref="OpCode.XOR"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void XOr(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(x1 ^ x2);
        }

        /// <summary>
        /// Determines whether two objects are equal according to the execution engine's comparison rules.
        /// <see cref="OpCode.EQUAL"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Equal(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop();
            var x1 = engine.Pop();
            engine.Push(x1.Equals(x2, engine.Limits));
        }

        /// <summary>
        /// Determines whether two objects are not equal according to the execution engine's comparison rules.
        /// <see cref="OpCode.NOTEQUAL"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NotEqual(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop();
            var x1 = engine.Pop();
            engine.Push(!x1.Equals(x2, engine.Limits));
        }
    }
}
