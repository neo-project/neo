#pragma warning disable IDE0051

using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Manifest;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
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

        private const byte BlockedChunkLength = 200;

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

        [ContractMethod(0_02000000, CallFlags.AllowStates)]
        public bool IsAnyAccountBlocked(StoreView snapshot, params UInt160[] hashes)
        {
            if (hashes.Length == 0) return false;

            foreach (var (_, data) in GetBlockedAccounts(snapshot))
            {
                var blockedList = data.ToArray();

                // blockedList can't be empty

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

            // Read all for sorting

            List<UInt160> accounts = new List<UInt160>();
            foreach (var (_, data) in GetBlockedAccounts(engine.Snapshot))
            {
                if (data.Contains(account)) return false;
                accounts.AddRange(data);
            }

            // Store them

            accounts.Add(account);
            accounts.Sort();
            StoreBlockedAccounts(engine.Snapshot, accounts);
            return true;
        }

        [ContractMethod(0_03000000, CallFlags.AllowModifyStates)]
        private bool UnblockAccount(ApplicationEngine engine, UInt160 account)
        {
            if (!CheckCommittee(engine)) return false;

            // Read all

            uint last_chunk = 0;
            List<UInt160> accounts = new List<UInt160>();
            foreach (var (chunkId, data) in GetBlockedAccounts(engine.Snapshot))
            {
                accounts.AddRange(data);
                last_chunk = Math.Max(chunkId, last_chunk);
            }

            // Remove it from list

            if (accounts.Remove(account) != true) return false;

            // Store them

            var chunk = StoreBlockedAccounts(engine.Snapshot, accounts);

            if (chunk <= last_chunk)
            {
                engine.Snapshot.Storages.Delete(CreateStorageKey(Prefix_BlockedAccounts).AddBigEndian(last_chunk));
            }
            return true;
        }

        private IEnumerable<(uint chunk, List<UInt160> data)> GetBlockedAccounts(StoreView snapshot)
        {
            foreach (var (key, value) in snapshot.Storages.FindRange
                (
                CreateStorageKey(Prefix_BlockedAccounts).AddBigEndian(0u).ToArray(),
                CreateStorageKey(Prefix_BlockedAccounts).AddBigEndian(uint.MaxValue).ToArray()
                ))
            {
                var chunk = BinaryPrimitives.ReadUInt32BigEndian(key.Key.AsSpan(key.Key.Length - sizeof(uint)));
                var list = value.GetSerializableList<UInt160>();

                yield return (chunk, list);
            }
        }

        private uint StoreBlockedAccounts(StoreView snapshot, List<UInt160> accounts)
        {
            uint chunk = 0;
            for (int x = 0; x < accounts.Count; x += BlockedChunkLength, chunk++)
            {
                var entry = snapshot.Storages.GetAndChange(
                    CreateStorageKey(Prefix_BlockedAccounts).AddBigEndian(chunk),
                    () => new StorageItem(new byte[1] { 0x00 }));

                var list = entry.GetSerializableList<UInt160>();
                list.Clear();
                list.AddRange(accounts.Skip(x * BlockedChunkLength).Take(BlockedChunkLength));
            }
            return chunk;
        }
    }
}
