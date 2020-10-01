#pragma warning disable IDE0051

using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Manifest;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Neo.SmartContract.Native
{
    public sealed class PolicyContract : NativeContract
    {
        public override string Name => "Policy";
        public override int Id => -3;

        private const byte Prefix_MaxTransactionsPerBlock = 23;
        private const byte Prefix_FeePerByte = 10;
        private const byte Prefix_BlockedAccounts = 15;
        private const byte Prefix_MaxBlockSize = 12;
        private const byte Prefix_MaxBlockSystemFee = 17;

        private const int MaxBlockedAccounts = 1_000;

        public PolicyContract()
        {
            Manifest.Features = ContractFeatures.HasStorage;
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public uint GetMaxTransactionsPerBlock(StoreView snapshot)
        {
            StorageItem item = snapshot.Storages.TryGet(CreateStorageKey(Prefix_MaxTransactionsPerBlock));
            if (item is null) return 512;
            return (uint)(BigInteger)item;
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public uint GetMaxBlockSize(StoreView snapshot)
        {
            StorageItem item = snapshot.Storages.TryGet(CreateStorageKey(Prefix_MaxBlockSize));
            if (item is null) return 1024 * 256;
            return (uint)(BigInteger)item;
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public long GetMaxBlockSystemFee(StoreView snapshot)
        {
            StorageItem item = snapshot.Storages.TryGet(CreateStorageKey(Prefix_MaxBlockSystemFee));
            if (item is null) return 9000 * (long)GAS.Factor; // For the transfer method of NEP5, the maximum persisting time is about three seconds.
            return (long)(BigInteger)item;
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public long GetFeePerByte(StoreView snapshot)
        {
            StorageItem item = snapshot.Storages.TryGet(CreateStorageKey(Prefix_FeePerByte));
            if (item is null) return 1000;
            return (long)(BigInteger)item;
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public UInt160[] GetBlockedAccounts(StoreView snapshot)
        {
            return snapshot.Storages.TryGet(CreateStorageKey(Prefix_BlockedAccounts))
                ?.GetSerializableList<UInt160>(MaxBlockedAccounts).ToArray()
                ?? Array.Empty<UInt160>();
        }

        public bool IsAnyAccountBlocked(StoreView snapshot, params UInt160[] hashes)
        {
            if (hashes.Length == 0) return false;

            var blockedList = snapshot.Storages.TryGet(CreateStorageKey(Prefix_BlockedAccounts))
                ?.GetSerializableList<UInt160>(MaxBlockedAccounts).ToArray()
                ?? Array.Empty<UInt160>();

            if (blockedList.Length > 0)
            {
                foreach (var acc in hashes)
                {
                    if (Array.BinarySearch(blockedList, acc) >= 0) return true;
                }
            }

            return false;
        }

        [ContractMethod(0_03000000, CallFlags.AllowModifyStates)]
        private bool SetMaxBlockSize(ApplicationEngine engine, uint value)
        {
            if (value > Message.PayloadMaxSize) throw new ArgumentOutOfRangeException(nameof(value));
            if (!CheckCommittee(engine)) return false;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_MaxBlockSize), () => new StorageItem());
            storage.Set(value);
            return true;
        }

        [ContractMethod(0_03000000, CallFlags.AllowModifyStates)]
        private bool SetMaxTransactionsPerBlock(ApplicationEngine engine, uint value)
        {
            if (value > Block.MaxTransactionsPerBlock) throw new ArgumentOutOfRangeException(nameof(value));
            if (!CheckCommittee(engine)) return false;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_MaxTransactionsPerBlock), () => new StorageItem());
            storage.Set(value);
            return true;
        }

        [ContractMethod(0_03000000, CallFlags.AllowModifyStates)]
        private bool SetMaxBlockSystemFee(ApplicationEngine engine, long value)
        {
            if (value <= 4007600) throw new ArgumentOutOfRangeException(nameof(value));
            if (!CheckCommittee(engine)) return false;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_MaxBlockSystemFee), () => new StorageItem());
            storage.Set(value);
            return true;
        }

        [ContractMethod(0_03000000, CallFlags.AllowModifyStates)]
        private bool SetFeePerByte(ApplicationEngine engine, long value)
        {
            if (value < 0 || value > 1_00000000) throw new ArgumentOutOfRangeException(nameof(value));
            if (!CheckCommittee(engine)) return false;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_FeePerByte), () => new StorageItem());
            storage.Set(value);
            return true;
        }

        [ContractMethod(0_03000000, CallFlags.AllowModifyStates)]
        private bool BlockAccount(ApplicationEngine engine, UInt160 account)
        {
            if (!CheckCommittee(engine)) return false;
            StorageKey key = CreateStorageKey(Prefix_BlockedAccounts);
            StorageItem storage = engine.Snapshot.Storages.GetOrAdd(key, () => new StorageItem(new byte[1]));

            List<UInt160> accounts = storage.GetSerializableList<UInt160>(MaxBlockedAccounts);
            if (accounts.Contains(account)) return false;
            if ((accounts.Count + 1) > MaxBlockedAccounts) throw new ArgumentException("Maximum number of blocked accounts exceeded");
            engine.Snapshot.Storages.GetAndChange(key);
            accounts.Add(account);
            accounts.Sort();
            return true;
        }

        [ContractMethod(0_03000000, CallFlags.AllowModifyStates)]
        private bool UnblockAccount(ApplicationEngine engine, UInt160 account)
        {
            if (!CheckCommittee(engine)) return false;
            StorageKey key = CreateStorageKey(Prefix_BlockedAccounts);
            StorageItem storage = engine.Snapshot.Storages.TryGet(key);
            if (storage is null) return false;
            List<UInt160> accounts = storage.GetSerializableList<UInt160>(MaxBlockedAccounts);
            int index = accounts.IndexOf(account);
            if (index < 0) return false;
            engine.Snapshot.Storages.GetAndChange(key);
            accounts.RemoveAt(index);
            return true;
        }
    }
}
