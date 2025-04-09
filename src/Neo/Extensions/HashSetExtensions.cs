// Copyright (C) 2015-2025 The Neo Project.
//
// HashSetExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO.Caching;
using System.Collections.Generic;

namespace Neo.Extensions
{
    /// <summary>
    /// A helper class that provides common functions.
    /// </summary>
    public static class HashSetExtensions
    {
        internal static void Remove(this HashSet<UInt256> set, HashSetUInt256Cache other)
        {
            if (set.Count > other.Count)
            {
                set.ExceptWith(other);
            }
            else
            {
                set.RemoveWhere(other.Contains);
            }
        }
    }
}
