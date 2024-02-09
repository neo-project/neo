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
    public partial class JumpTable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Sign(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop().GetInteger();
            engine.Push(x.Sign);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Abs(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop().GetInteger();
            engine.Push(BigInteger.Abs(x));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Negate(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop().GetInteger();
            engine.Push(-x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Inc(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop().GetInteger();
            engine.Push(x + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Dec(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop().GetInteger();
            engine.Push(x - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Add(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(x1 + x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Sub(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(x1 - x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Mul(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(x1 * x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Div(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(x1 / x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Mod(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(x1 % x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Pow(ExecutionEngine engine, Instruction instruction)
        {
            var exponent = (int)engine.Pop().GetInteger();
            engine.Limits.AssertShift(exponent);
            var value = engine.Pop().GetInteger();
            engine.Push(BigInteger.Pow(value, exponent));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Sqrt(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(engine.Pop().GetInteger().Sqrt());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void ModMul(ExecutionEngine engine, Instruction instruction)
        {
            var modulus = engine.Pop().GetInteger();
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(x1 * x2 % modulus);
        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Shl(ExecutionEngine engine, Instruction instruction)
        {
            var shift = (int)engine.Pop().GetInteger();
            engine.Limits.AssertShift(shift);
            if (shift == 0) return;
            var x = engine.Pop().GetInteger();
            engine.Push(x << shift);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Shr(ExecutionEngine engine, Instruction instruction)
        {
            var shift = (int)engine.Pop().GetInteger();
            engine.Limits.AssertShift(shift);
            if (shift == 0) return;
            var x = engine.Pop().GetInteger();
            engine.Push(x >> shift);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Not(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop().GetBoolean();
            engine.Push(!x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void BoolAnd(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetBoolean();
            var x1 = engine.Pop().GetBoolean();
            engine.Push(x1 && x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void BoolOr(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetBoolean();
            var x1 = engine.Pop().GetBoolean();
            engine.Push(x1 || x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Nz(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop().GetInteger();
            engine.Push(!x.IsZero);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NumEqual(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(x1 == x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NumNotEqual(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(x1 != x2);
        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Min(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(BigInteger.Min(x1, x2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Max(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(BigInteger.Max(x1, x2));
        }

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
