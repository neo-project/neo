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
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Neo.Extensions
{
    public class ByteArrayComparer : IComparer<byte[]>
    {
        public static readonly ByteArrayComparer Default = new(1);
        public static readonly ByteArrayComparer Reverse = new(-1);

        private readonly int _direction;

        private ByteArrayComparer(int direction)
        {
            _direction = direction;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(byte[]? x, byte[]? y)
        {
            if (ReferenceEquals(x, y)) return 0;

            if (x is null) // y must not be null
                return -y!.Length * _direction;

            if (y is null) // x must not be null
                return x.Length * _direction;


            return x.AsSpan().SequenceCompareTo(y.AsSpan()) * _direction;
        }
    }
}
