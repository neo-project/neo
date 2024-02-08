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
        public virtual void SIGN(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop().GetInteger();
            engine.Push(x.Sign);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void ABS(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop().GetInteger();
            engine.Push(BigInteger.Abs(x));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NEGATE(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop().GetInteger();
            engine.Push(-x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void INC(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop().GetInteger();
            engine.Push(x + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void DEC(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop().GetInteger();
            engine.Push(x - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void ADD(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(x1 + x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void SUB(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(x1 - x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void MUL(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(x1 * x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void DIV(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(x1 / x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void MOD(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(x1 % x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void POW(ExecutionEngine engine, Instruction instruction)
        {
            var exponent = (int)engine.Pop().GetInteger();
            engine.Limits.AssertShift(exponent);
            var value = engine.Pop().GetInteger();
            engine.Push(BigInteger.Pow(value, exponent));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void SQRT(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(engine.Pop().GetInteger().Sqrt());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void MODMUL(ExecutionEngine engine, Instruction instruction)
        {
            var modulus = engine.Pop().GetInteger();
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(x1 * x2 % modulus);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void MODPOW(ExecutionEngine engine, Instruction instruction)
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
        public virtual void SHL(ExecutionEngine engine, Instruction instruction)
        {
            var shift = (int)engine.Pop().GetInteger();
            engine.Limits.AssertShift(shift);
            if (shift == 0) return;
            var x = engine.Pop().GetInteger();
            engine.Push(x << shift);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void SHR(ExecutionEngine engine, Instruction instruction)
        {
            var shift = (int)engine.Pop().GetInteger();
            engine.Limits.AssertShift(shift);
            if (shift == 0) return;
            var x = engine.Pop().GetInteger();
            engine.Push(x >> shift);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NOT(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop().GetBoolean();
            engine.Push(!x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void BOOLAND(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetBoolean();
            var x1 = engine.Pop().GetBoolean();
            engine.Push(x1 && x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void BOOLOR(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetBoolean();
            var x1 = engine.Pop().GetBoolean();
            engine.Push(x1 || x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NZ(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop().GetInteger();
            engine.Push(!x.IsZero);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NUMEQUAL(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(x1 == x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NUMNOTEQUAL(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(x1 != x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LT(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop();
            var x1 = engine.Pop();
            if (x1.IsNull || x2.IsNull)
                engine.Push(false);
            else
                engine.Push(x1.GetInteger() < x2.GetInteger());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LE(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop();
            var x1 = engine.Pop();
            if (x1.IsNull || x2.IsNull)
                engine.Push(false);
            else
                engine.Push(x1.GetInteger() <= x2.GetInteger());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void GT(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop();
            var x1 = engine.Pop();
            if (x1.IsNull || x2.IsNull)
                engine.Push(false);
            else
                engine.Push(x1.GetInteger() > x2.GetInteger());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void GE(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop();
            var x1 = engine.Pop();
            if (x1.IsNull || x2.IsNull)
                engine.Push(false);
            else
                engine.Push(x1.GetInteger() >= x2.GetInteger());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void MIN(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(BigInteger.Min(x1, x2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void MAX(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(BigInteger.Max(x1, x2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void WITHIN(ExecutionEngine engine, Instruction instruction)
        {
            var b = engine.Pop().GetInteger();
            var a = engine.Pop().GetInteger();
            var x = engine.Pop().GetInteger();
            engine.Push(a <= x && x < b);
        }
    }
}
