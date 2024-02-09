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
    public partial class JumpTable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Invert(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop().GetInteger();
            engine.Push(~x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void And(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(x1 & x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Or(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(x1 | x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void XOr(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            engine.Push(x1 ^ x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Equal(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop();
            var x1 = engine.Pop();
            engine.Push(x1.Equals(x2, engine.Limits));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NotEqual(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop();
            var x1 = engine.Pop();
            engine.Push(!x1.Equals(x2, engine.Limits));
        }
    }
}
