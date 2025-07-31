// Copyright (C) 2015-2025 The Neo Project.
//
// Breakpoint.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM;
using System;

namespace Neo.Build.Core.SmartContract.Debugger
{
    public class Breakpoint : IEquatable<Breakpoint>
    {
        public uint? BlockIndex { get; set; } = null;

        public Script Script { get; set; } = Script.Empty;

        public override int GetHashCode() =>
            HashCode.Combine(BlockIndex, Script.GetHashCode());

        private Breakpoint() { }

        public static Breakpoint Create(Script script, uint? blockIndex = null) =>
            new()
            {
                BlockIndex = blockIndex,
                Script = script,
            };

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(obj, this)) return true;
            return Equals(obj as Breakpoint);
        }

        public bool Equals(Breakpoint? other)
        {
            if (other == null) return false;
            return BlockIndex == other.BlockIndex &&
                Script.GetHashCode() == other.Script.GetHashCode();
        }
    }
}
