// Copyright (C) 2015-2026 The Neo Project.
//
// GovernanceExtensions.cs file belongs to the neo project and is free
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
using System.Numerics;

namespace Neo.Extensions.SmartContract;

public static class GovernanceExtensions
{
    public static IEnumerable<(UInt160 Address, BigInteger Balance)> GetAccounts(this Governance gasToken, IReadOnlyStore snapshot)
    {
        ArgumentNullException.ThrowIfNull(gasToken);

        ArgumentNullException.ThrowIfNull(snapshot);

        var kb = new KeyBuilder(TokenManagement.TokenId, TokenManagement.Prefix_AccountState)
            .Add(gasToken.GasTokenId)
            .ToArray();
        var kbLength = kb.Length;

        foreach (var (key, value) in snapshot.Find(kb, SeekDirection.Forward))
        {
            var keyBytes = key.ToArray();
            var accountHash = new UInt160(keyBytes.AsSpan(kb.Length));
            yield return new(accountHash, value.GetInteroperable<AccountState>().Balance);
        }
    }
}
