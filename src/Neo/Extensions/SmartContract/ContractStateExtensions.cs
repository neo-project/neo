// Copyright (C) 2015-2024 The Neo Project.
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
using System;
using System.Collections.Generic;

namespace Neo.Extensions.SmartContract
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
        /// <exception cref="ArgumentNullException"></exception>
        public static StorageItem GetStorage(this ContractState contractState, DataCache snapshot, byte[] storageKey)
        {
            if (contractState is null)
                throw new ArgumentNullException(nameof(contractState));

            if (snapshot is null)
                throw new ArgumentNullException(nameof(snapshot));

            if (storageKey?.Length == 0)
                throw new ArgumentNullException(nameof(storageKey));

            return snapshot.TryGet(StorageKey.CreateSearchPrefix(contractState.Id, storageKey));
        }

        /// <summary>
        /// Get all storage of the given contract.
        /// </summary>
        /// <param name="contractState"></param>
        /// <param name="snapshot">Snapshot of the database.</param>
        /// <returns>All storage of the given contract.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<(StorageKey Key, StorageItem Value)> GetStorage(this ContractState contractState, DataCache snapshot)
        {
            if (contractState is null)
                throw new ArgumentNullException(nameof(contractState));

            if (snapshot is null)
                throw new ArgumentNullException(nameof(snapshot));

            return snapshot.Find(StorageKey.CreateSearchPrefix(contractState.Id, []));
        }

        /// <summary>
        /// Get all storage of the given contract.
        /// </summary>
        /// <param name="contractManagement"></param>
        /// <param name="snapshot">Snapshot of the database.</param>
        /// <param name="contractId">Id of the contract.</param>
        /// <returns>All storage of the given contract.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<(StorageKey Key, StorageItem Value)> GetContractStorage(this ContractManagement contractManagement, DataCache snapshot, int contractId)
        {
            if (snapshot is null)
                throw new ArgumentNullException(nameof(snapshot));

            return snapshot.Find(StorageKey.CreateSearchPrefix(contractId, []));
        }
    }
}
