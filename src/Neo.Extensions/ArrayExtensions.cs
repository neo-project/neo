// Copyright (C) 2015-2025 The Neo Project.
//
// ArrayExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Runtime.CompilerServices;

namespace Neo.Extensions
{
    public static class ArrayExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] Repeat<T>(this T value, int count)
        {
            T[] array = new T[count];
            Array.Fill(array, value);
            return array;
        }
    }
}
