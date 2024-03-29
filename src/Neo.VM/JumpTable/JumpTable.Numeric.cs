// Copyright (C) 2015-2024 The Neo Project.
//
// JumpTable.Numeric.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    /// <summary>
    /// Partial class for mathematical operations within a jump table.
    /// </summary>
    /// <remarks>
    /// For binary operations x1 and x2, x1 is the first pushed onto the evaluation stack (the second popped from the stack),
    /// x2 is the second pushed onto the evaluation stack (the first popped from the stack)
    /// </remarks>
    public partial class JumpTable
    {
        /// <summary>
        /// Computes the sign of the specified integer.
        /// If the value is negative, puts -1; if positive, puts 1; if zero, puts 0.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.SIGN"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Sign(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop().GetInteger();
            engine.Push(x.Sign);
        }

        /// <summary>
        /// Computes the absolute value of the specified integer.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.ABS"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Abs(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop().GetInteger();
            engine.Push(BigInteger.Abs(x));
        }

        /// <summary>
        /// Computes the negation of the specified integer.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.NEGATE"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Negate(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop().GetInteger();
            engine.Push(-x);
        }

        /// <summary>
        /// Increments the specified integer by one.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.INC"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Inc(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop().GetInteger();
            engine.Push(x + 1);
        }

        /// <summary>
        /// Decrements the specified integer by one.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.DEC"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Dec(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop().GetInteger();
            engine.Push(x - 1);
        }

        /// <summary>
        /// Computes the sum of two integers.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.ADD"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Add(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(x1 + x2);
        }

        /// <summary>
        /// Computes the difference between two integers.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.SUB"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Sub(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(x1 - x2);
        }

        /// <summary>
        /// Computes the product of two integers.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.MUL"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Mul(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(x1 * x2);
        }

        /// <summary>
        /// Computes the quotient of two integers.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.DIV"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Div(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(x1 / x2);
        }

        /// <summary>
        /// Computes the result of raising a number to the specified power.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.MOD"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Mod(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(x1 % x2);
        }

        /// <summary>
        /// Computes the square root of the specified integer.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.POW"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Pow(ExecutionEngine engine, Instruction instruction)
        {
            var exponent = (int)engine.Pop().GetInteger();
            engine.Limits.AssertShift(exponent);
            var value = engine.Pop().GetInteger();
            engine.Push(BigInteger.Pow(value, exponent));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.SQRT"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Sqrt(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(engine.Pop().GetInteger().Sqrt());
        }

        /// <summary>
        /// Computes the modular multiplication of two integers.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.MODMUL"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void ModMul(ExecutionEngine engine, Instruction instruction)
        {
            var modulus = engine.Pop().GetInteger();
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(x1 * x2 % modulus);
        }

        /// <summary>
        /// Computes the modular exponentiation of an integer.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.MODPOW"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void ModPow(ExecutionEngine engine, Instruction instruction)
        {
            var modulus = engine.Pop().GetInteger();
            var exponent = engine.Pop().GetInteger();
            var value = engine.Pop().GetInteger();
            var result = exponent == -1
                ? value.ModInverse(modulus)
                : BigInteger.ModPow(value, exponent, modulus);
            engine.Push(result);
        }

        /// <summary>
        /// Computes the left shift of an integer.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.SHL"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Shl(ExecutionEngine engine, Instruction instruction)
        {
            var shift = (int)engine.Pop().GetInteger();
            engine.Limits.AssertShift(shift);
            if (shift == 0) return;
            var x = engine.Pop().GetInteger();
            engine.Push(x << shift);
        }

        /// <summary>
        /// Computes the right shift of an integer.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.SHR"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Shr(ExecutionEngine engine, Instruction instruction)
        {
            var shift = (int)engine.Pop().GetInteger();
            engine.Limits.AssertShift(shift);
            if (shift == 0) return;
            var x = engine.Pop().GetInteger();
            engine.Push(x >> shift);
        }

        /// <summary>
        /// If the input is 0 or 1, it is flipped. Otherwise the output will be 0.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.NOT"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Not(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop().GetBoolean();
            engine.Push(!x);
        }

        /// <summary>
        /// Computes the logical AND of the top two stack items and pushes the result onto the stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.BOOLAND"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void BoolAnd(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetBoolean();
            var x1 = engine.Pop().GetBoolean();
            engine.Push(x1 && x2);
        }

        /// <summary>
        /// Computes the logical OR of the top two stack items and pushes the result onto the stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.BOOLOR"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void BoolOr(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetBoolean();
            var x1 = engine.Pop().GetBoolean();
            engine.Push(x1 || x2);
        }

        /// <summary>
        /// Determines whether the top stack item is not zero and pushes the result onto the stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.NZ"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Nz(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop().GetInteger();
            engine.Push(!x.IsZero);
        }

        /// <summary>
        /// Determines whether the top two stack items are equal and pushes the result onto the stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.NUMEQUAL"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NumEqual(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(x1 == x2);
        }

        /// <summary>
        /// Determines whether the top two stack items are not equal and pushes the result onto the stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.NUMNOTEQUAL"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NumNotEqual(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(x1 != x2);
        }

        /// <summary>
        /// Determines whether the two integer at the top of the stack, x1 are less than x2, and pushes the result onto the stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.LT"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Lt(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop();
            var x1 = engine.Pop();
            if (x1.IsNull || x2.IsNull)
                engine.Push(false);
            else
                engine.Push(x1.GetInteger() < x2.GetInteger());
        }

        /// <summary>
        /// Determines whether the two integer at the top of the stack, x1 are less than or equal to x2, and pushes the result onto the stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.LE"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Le(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop();
            var x1 = engine.Pop();
            if (x1.IsNull || x2.IsNull)
                engine.Push(false);
            else
                engine.Push(x1.GetInteger() <= x2.GetInteger());
        }

        /// <summary>
        /// Determines whether the two integer at the top of the stack, x1 are greater than x2, and pushes the result onto the stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.GT"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Gt(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop();
            var x1 = engine.Pop();
            if (x1.IsNull || x2.IsNull)
                engine.Push(false);
            else
                engine.Push(x1.GetInteger() > x2.GetInteger());
        }

        /// <summary>
        /// Determines whether the two integer at the top of the stack, x1 are greater than or equal to x2, and pushes the result onto the stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.GE"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Ge(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop();
            var x1 = engine.Pop();
            if (x1.IsNull || x2.IsNull)
                engine.Push(false);
            else
                engine.Push(x1.GetInteger() >= x2.GetInteger());
        }

        /// <summary>
        /// Computes the minimum of the top two stack items and pushes the result onto the stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.MIN"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Min(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(BigInteger.Min(x1, x2));
        }

        /// <summary>
        /// Computes the maximum of the top two stack items and pushes the result onto the stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.MAX"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Max(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(BigInteger.Max(x1, x2));
        }

        /// <summary>
        /// Determines whether the top stack item is within the range specified by the next two top stack items
        /// and pushes the result onto the stack.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <see cref="OpCode.WITHIN"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Within(ExecutionEngine engine, Instruction instruction)
        {
            var b = engine.Pop().GetInteger();
            var a = engine.Pop().GetInteger();
            var x = engine.Pop().GetInteger();
            engine.Push(a <= x && x < b);
        }
    }
}
