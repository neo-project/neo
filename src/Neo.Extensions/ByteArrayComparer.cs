// Copyright (C) 2015-2025 The Neo Project.
//
// ByteArrayComparer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Neo.Extensions
{
    /// <summary>
    /// Defines methods to support the comparison of two <see cref="byte"/>[].
    /// </summary>
    public class ByteArrayComparer : IComparer<byte[]>
    {
        public static readonly ByteArrayComparer Default = new(1);
        public static readonly ByteArrayComparer Reverse = new(-1);

        private readonly int _direction;

        private ByteArrayComparer(int direction)
        {
            _direction = direction;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(byte[]? x, byte[]? y)
        {
            if (ReferenceEquals(x, y)) return 0;

            if (x is null) // y must not be null
                return -y!.Length * _direction;

            if (y is null) // x must not be null
                return x.Length * _direction;

            // Note: if "SequenceCompareTo" is "int.MinValue * -1", it
            // will overflow "int.MaxValue". Seeing how "int.MinValue * -1"
            // value would be "int.MaxValue + 1"
            return unchecked(x.AsSpan().SequenceCompareTo(y.AsSpan()) * _direction);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(object? x, object? y)
        {
            if (ReferenceEquals(x, y)) return 0;

            if (x is not null and not byte[])
                throw new ArgumentException($"Unable to cast '{x.GetType().FullName}' to '{typeof(byte[]).FullName}'.");

            if (y is not null and not byte[])
                throw new ArgumentException($"Unable to cast '{y.GetType().FullName}' to '{typeof(byte[]).FullName}'.");

            return Compare(x as byte[], y as byte[]);
        }
    }
}
