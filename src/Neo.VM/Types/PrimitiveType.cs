// Copyright (C) 2016-2023 The Neo Project.
// 
// The neo-vm is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Neo.VM.Types
{
    /// <summary>
    /// The base class for primitive types in the VM.
    /// </summary>
    public abstract class PrimitiveType : StackItem
    {
        public abstract ReadOnlyMemory<byte> Memory { get; }

        /// <summary>
        /// The size of the VM object in bytes.
        /// </summary>
        public virtual int Size => Memory.Length;

        public override StackItem ConvertTo(StackItemType type)
        {
            if (type == Type) return this;
            return type switch
            {
                StackItemType.Integer => GetInteger(),
                StackItemType.ByteString => Memory,
                StackItemType.Buffer => new Buffer(GetSpan()),
                _ => base.ConvertTo(type)
            };
        }

        internal sealed override StackItem DeepCopy(Dictionary<StackItem, StackItem> refMap, bool asImmutable)
        {
            return this;
        }

        public abstract override bool Equals(StackItem? other);

        /// <summary>
        /// Get the hash code of the VM object, which is used for key comparison in the <see cref="Map"/>.
        /// </summary>
        /// <returns>The hash code of this VM object.</returns>
        public abstract override int GetHashCode();

        public sealed override ReadOnlySpan<byte> GetSpan()
        {
            return Memory.Span;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrimitiveType(sbyte value)
        {
            return (Integer)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrimitiveType(byte value)
        {
            return (Integer)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrimitiveType(short value)
        {
            return (Integer)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrimitiveType(ushort value)
        {
            return (Integer)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrimitiveType(int value)
        {
            return (Integer)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrimitiveType(uint value)
        {
            return (Integer)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrimitiveType(long value)
        {
            return (Integer)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrimitiveType(ulong value)
        {
            return (Integer)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrimitiveType(BigInteger value)
        {
            return (Integer)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrimitiveType(bool value)
        {
            return (Boolean)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrimitiveType(byte[] value)
        {
            return (ByteString)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrimitiveType(ReadOnlyMemory<byte> value)
        {
            return (ByteString)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrimitiveType(string value)
        {
            return (ByteString)value;
        }
    }
}
