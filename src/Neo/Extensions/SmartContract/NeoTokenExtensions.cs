// Copyright (C) 2015-2025 The Neo Project.
//
// NeoTokenExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Neo.Extensions
{
    public static class NeoTokenExtensions
    {
        public static IEnumerable<(UInt160 Address, BigInteger Balance)> GetAccounts(this NeoToken neoToken, IReadOnlyStore snapshot)
        {
            if (neoToken is null)
                throw new ArgumentNullException(nameof(neoToken));

            if (snapshot is null)
                throw new ArgumentNullException(nameof(snapshot));

            var kb = StorageKey.Create(neoToken.Id, NeoToken.Prefix_Account);
            var kbLength = kb.Length;

            foreach (var (key, value) in snapshot.Find(kb, SeekDirection.Forward))
            {
                var keyBytes = key.ToArray();
                var addressHash = new UInt160(keyBytes.AsSpan(kbLength));
                yield return new(addressHash, value.GetInteroperable<AccountState>().Balance);
            }
        }
    }
}
