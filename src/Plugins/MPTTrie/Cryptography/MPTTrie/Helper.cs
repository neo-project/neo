// Copyright (C) 2015-2024 The Neo Project.
//
// Helper.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.Cryptography.MPTTrie
{
    public static class Helper
    {
        public static int CompareTo(this ReadOnlySpan<byte> arr1, ReadOnlySpan<byte> arr2)
        {
            for (int i = 0; i < arr1.Length && i < arr2.Length; i++)
            {
                var r = arr1[i].CompareTo(arr2[i]);
                if (r != 0) return r;
            }
            return arr2.Length < arr1.Length ? 1 : arr2.Length == arr1.Length ? 0 : -1;
        }
    }
}
