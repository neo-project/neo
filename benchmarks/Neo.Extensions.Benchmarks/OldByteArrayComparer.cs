// Copyright (C) 2015-2024 The Neo Project.
//
// OldByteArrayComparer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Runtime.CompilerServices;

namespace Neo.Extensions
{
    public class OldByteArrayComparer : IComparer<byte[]>
    {
        public static readonly OldByteArrayComparer Default = new(1);
        public static readonly OldByteArrayComparer Reverse = new(-1);

        private readonly int _direction;

        internal OldByteArrayComparer(int direction)
        {
            _direction = direction;
        }

        public int Compare(byte[]? x, byte[]? y)
        {
            if (x == y) return 0;
            if (x is null && y is not null)
                return _direction > 0 ? -y.Length : y.Length;
            if (y is null && x is not null)
                return _direction > 0 ? x.Length : -x.Length;
            return _direction > 0 ?
                CompareInternal(x!, y!) :
                -CompareInternal(x!, y!);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CompareInternal(byte[] x, byte[] y)
        {
            var length = Math.Min(x.Length, y.Length);
            for (var i = 0; i < length; i++)
            {
                var r = x[i].CompareTo(y[i]);
                if (r != 0) return r;
            }
            return x.Length.CompareTo(y.Length);
        }
    }
}
