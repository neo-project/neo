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

#nullable enable

using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using System;
using System.Collections.Generic;

namespace Neo.Extensions
{
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
        public static StorageItem? GetStorage(this ContractState contractState, DataCache snapshot, byte[] storageKey)
        {
            return snapshot.TryGet(StorageKey.CreateSearchPrefix(contractState.Id, storageKey), out var ret) ? ret : null;
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
        public static IEnumerable<(StorageKey Key, StorageItem Value)> FindStorage(this ContractState contractState, DataCache snapshot, byte[]? prefix = null, SeekDirection seekDirection = SeekDirection.Forward)
        {
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
        public static IEnumerable<(StorageKey Key, StorageItem Value)> FindContractStorage(this ContractManagement contractManagement, DataCache snapshot, int contractId, byte[]? prefix = null, SeekDirection seekDirection = SeekDirection.Forward)
        {
            prefix ??= [];
            return snapshot.Find(StorageKey.CreateSearchPrefix(contractId, prefix), seekDirection);
        }
    }
}

#nullable disable
