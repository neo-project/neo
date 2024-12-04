// Copyright (C) 2015-2024 The Neo Project.
//
// LedgerContractExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.Persistence;
using Neo.Plugins.RestServer.Models.Blockchain;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.Wallets;
using System;
using System.Collections.Generic;

namespace Neo.Plugins.RestServer.Extensions
{
    internal static class LedgerContractExtensions
    {
        private const byte _prefix_block = 5;
        private const byte _prefix_transaction = 11;
        private const byte _prefix_account = 20;
        //private const byte _prefix_totalsupply = 11;

        public static IEnumerable<(StorageKey key, StorageItem value)> GetStorageByPrefix(this ContractState contractState, DataCache snapshot, byte[] prefix)
        {
            ArgumentNullException.ThrowIfNull(contractState, nameof(contractState));
            ArgumentNullException.ThrowIfNull(snapshot, nameof(snapshot));
            if (prefix?.Length == 0)
                throw new ArgumentNullException(nameof(prefix));
            foreach (var (key, value) in snapshot.Find(StorageKey.CreateSearchPrefix(contractState.Id, prefix)))
                yield return (key, value);
        }

        public static StorageItem? GetStorageByKey(this ContractState contractState, DataCache snapshot, byte[] storageKey)
        {
            ArgumentNullException.ThrowIfNull(contractState, nameof(contractState));
            ArgumentNullException.ThrowIfNull(snapshot, nameof(snapshot));
            if (storageKey?.Length == 0)
                throw new ArgumentNullException(nameof(storageKey));
            foreach (var (key, value) in snapshot.Find(StorageKey.CreateSearchPrefix(contractState.Id, storageKey)))
                if (key.Key.Span.SequenceEqual(storageKey))
                    return value;
            return default;
        }

        public static IEnumerable<(StorageKey key, StorageItem value)> GetStorage(this ContractState contractState, DataCache snapshot)
        {
            ArgumentNullException.ThrowIfNull(contractState, nameof(contractState));
            ArgumentNullException.ThrowIfNull(snapshot, nameof(snapshot));
            return ListContractStorage(snapshot, contractState.Id);
        }

        public static IEnumerable<(StorageKey key, StorageItem value)> ListContractStorage(DataCache snapshot, int contractId)
        {
            ArgumentNullException.ThrowIfNull(snapshot, nameof(snapshot));
            if (contractId < 0)
                throw new ArgumentOutOfRangeException(nameof(contractId));
            foreach (var (key, value) in snapshot.Find(StorageKey.CreateSearchPrefix(contractId, ReadOnlySpan<byte>.Empty)))
                yield return (key, value);
        }

        public static IEnumerable<TrimmedBlock> ListBlocks(this LedgerContract ledger, DataCache snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot, nameof(snapshot));
            var kb = new KeyBuilder(ledger.Id, _prefix_block);
            var prefixKey = kb.ToArray();
            foreach (var (key, value) in snapshot.Seek(prefixKey, SeekDirection.Forward))
                if (key.ToArray().AsSpan().StartsWith(prefixKey))
                    yield return value.Value.AsSerializable<TrimmedBlock>();
                else
                    yield break;
        }

        public static IEnumerable<TransactionState> ListTransactions(this LedgerContract ledger, DataCache snapshot, uint page, uint pageSize)
        {
            ArgumentNullException.ThrowIfNull(snapshot, nameof(snapshot));
            var kb = new KeyBuilder(ledger.Id, _prefix_transaction);
            var prefixKey = kb.ToArray();
            uint index = 1;
            foreach (var (key, value) in snapshot.Seek(prefixKey, SeekDirection.Forward))
            {
                if (key.ToArray().AsSpan().StartsWith(prefixKey))
                {
                    if (index >= page && index < (pageSize + page))
                        yield return value.GetInteroperable<TransactionState>();
                    index++;
                }
                else
                    yield break;
            }
        }

        public static IEnumerable<AccountDetails> ListAccounts(this GasToken gasToken, DataCache snapshot, ProtocolSettings protocolSettings)
        {
            ArgumentNullException.ThrowIfNull(snapshot, nameof(snapshot));
            var kb = new KeyBuilder(gasToken.Id, _prefix_account);
            var prefixKey = kb.ToArray();
            foreach (var (key, value) in snapshot.Seek(prefixKey, SeekDirection.Forward))
            {
                if (key.ToArray().AsSpan().StartsWith(prefixKey))
                {
                    var addressHash = new UInt160(key.ToArray().AsSpan(5));
                    yield return new AccountDetails()
                    {
                        ScriptHash = addressHash,
                        Address = addressHash.ToAddress(protocolSettings.AddressVersion),
                        Balance = value.GetInteroperable<AccountState>().Balance,
                        Decimals = NativeContract.GAS.Decimals
                    };
                }
                else
                    yield break;
            }
        }

        public static IEnumerable<AccountDetails> ListAccounts(this NeoToken neoToken, DataCache snapshot, ProtocolSettings protocolSettings)
        {
            ArgumentNullException.ThrowIfNull(snapshot, nameof(snapshot));
            var kb = new KeyBuilder(neoToken.Id, _prefix_account);
            var prefixKey = kb.ToArray();
            foreach (var (key, value) in snapshot.Seek(prefixKey, SeekDirection.Forward))
            {
                if (key.ToArray().AsSpan().StartsWith(prefixKey))
                {
                    var addressHash = new UInt160(key.ToArray().AsSpan(5));
                    yield return new AccountDetails()
                    {
                        ScriptHash = addressHash,
                        Address = addressHash.ToAddress(protocolSettings.AddressVersion),
                        Balance = value.GetInteroperable<AccountState>().Balance,
                        Decimals = NativeContract.NEO.Decimals
                    };
                }
                else
                    yield break;
            }
        }
    }
}
