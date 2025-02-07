// Copyright (C) 2015-2025 The Neo Project.
//
// StorageKeyEqualityComparer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.SmartContract;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Neo.Persistence
{
    /// <summary>
    /// Defines methods to support the comparison of <see cref="StorageKey"/> for equality.
    /// </summary>
    public class StorageKeyEqualityComparer : IEqualityComparer<StorageKey>
    {
        public static readonly StorageKeyEqualityComparer Instance = new();

        /// <inheritdoc />
        public bool Equals([AllowNull] StorageKey x, [AllowNull] StorageKey y) =>
            ReferenceEquals(x, y) || ByteArrayEqualityComparer.Default.Equals(x?.ToArray(), x?.ToArray());

        /// <inheritdoc />
        public int GetHashCode([DisallowNull] StorageKey obj) =>
            ByteArrayEqualityComparer.Default.GetHashCode(obj?.ToArray());
    }
}
