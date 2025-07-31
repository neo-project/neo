// Copyright (C) 2015-2025 The Neo Project.
//
// Boolean.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Neo.VM.Types
{
    /// <summary>
    /// Represents a boolean (<see langword="true" /> or <see langword="false" />) value in the VM.
    /// </summary>
    [DebuggerDisplay("Type={GetType().Name}, Value={value}")]
    public class Boolean : PrimitiveType
    {
        private static readonly ReadOnlyMemory<byte> s_true = new byte[] { 1 };
        private static readonly ReadOnlyMemory<byte> s_false = new byte[] { 0 };

        private readonly bool _value;

        public override ReadOnlyMemory<byte> Memory => _value ? s_true : s_false;
        public override int Size => sizeof(bool);
        public override StackItemType Type => StackItemType.Boolean;

        /// <summary>
        /// Create a new VM object representing the boolean type.
        /// </summary>
        /// <param name="value">The initial value of the object.</param>
        internal Boolean(bool value)
        {
            _value = value;
        }

        public override bool Equals(StackItem? other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is Boolean b) return _value == b._value;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool GetBoolean()
        {
            return _value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override BigInteger GetInteger()
        {
            return _value ? BigInteger.One : BigInteger.Zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Boolean(bool value)
        {
            return value ? True : False;
        }

        public override string ToString()
        {
            return _value.ToString();
        }
    }
}
