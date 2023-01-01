// Copyright (C) 2015-2023 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Neo.IO
{
    internal class ByteArrayComparer : IComparer<byte[]>
    {
        public static readonly ByteArrayComparer Default = new(1);
        public static readonly ByteArrayComparer Reverse = new(-1);

        private readonly int direction;

        private ByteArrayComparer(int direction)
        {
            this.direction = direction;
        }

        public int Compare(byte[] x, byte[] y)
        {
            return direction > 0
                ? CompareInternal(x, y)
                : -CompareInternal(x, y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CompareInternal(byte[] x, byte[] y)
        {
            int length = Math.Min(x.Length, y.Length);
            for (int i = 0; i < length; i++)
            {
                int r = x[i].CompareTo(y[i]);
                if (r != 0) return r;
            }
            return x.Length.CompareTo(y.Length);
        }
    }
}
