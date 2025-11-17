// Copyright (C) 2015-2025 The Neo Project.
//
// ContractStateExtensions.cs file belongs to the neo project and is free
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

namespace Neo.Extensions.SmartContract;

public static class ContractStateExtensions
{
    /// <summary>
    /// Get Storage value by storage map key.
    /// </summary>
    /// <param name="contractState"></param>
    /// <param name="snapshot">Snapshot of the database.</param>
    /// <param name="storageKey">Key in the storage map.</param>
    /// <returns>Storage value of the item.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="contractState"/> or <paramref name="snapshot"/> is null</exception>
    public static StorageItem? GetStorage(this ContractState contractState, IReadOnlyStore snapshot, byte[] storageKey)
    {
        ArgumentNullException.ThrowIfNull(contractState);

        ArgumentNullException.ThrowIfNull(snapshot);

        storageKey ??= [];

        if (snapshot.TryGet(StorageKey.CreateSearchPrefix(contractState.Id, storageKey), out var value))
        {
            return value;
        }

        return null;
    }

    /// <summary>
    /// All storage items stored in the given contract.
    /// </summary>
    /// <param name="contractState"></param>
    /// <param name="snapshot">Snapshot of the database.</param>
    /// <param name="prefix">Prefix of the key.</param>
    /// <param name="seekDirection"></param>
    /// <returns>All storage of the given contract.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="contractState"/> or <paramref name="snapshot"/> is null</exception>
    public static IEnumerable<(StorageKey Key, StorageItem Value)> FindStorage(this ContractState contractState, IReadOnlyStore snapshot, byte[]? prefix = null, SeekDirection seekDirection = SeekDirection.Forward)
    {
        ArgumentNullException.ThrowIfNull(contractState);

        ArgumentNullException.ThrowIfNull(snapshot);

        prefix ??= [];

        return snapshot.Find(StorageKey.CreateSearchPrefix(contractState.Id, prefix), seekDirection);
    }

    /// <summary>
    /// All storage items stored in the given contract.
    /// </summary>
    /// <param name="contractManagement"></param>
    /// <param name="snapshot">Snapshot of the database.</param>
    /// <param name="prefix">Prefix of the key.</param>
    /// <param name="contractId">Id of the contract.</param>
    /// <param name="seekDirection"></param>
    /// <returns>All storage of the given contract.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="snapshot"/> is null</exception>
    public static IEnumerable<(StorageKey Key, StorageItem Value)> FindContractStorage(this ContractManagement contractManagement, IReadOnlyStore snapshot, int contractId, byte[]? prefix = null, SeekDirection seekDirection = SeekDirection.Forward)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        prefix ??= [];

        return snapshot.Find(StorageKey.CreateSearchPrefix(contractId, prefix), seekDirection);
    }
}
