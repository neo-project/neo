#pragma warning disable IDE0051

using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Manifest;
using System;
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
            foreach (var account in hashes)
            {
                if (snapshot.Storages.Contains(CreateStorageKey(Prefix_BlockedAccounts).Add(account))) return true;
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

            var key = CreateStorageKey(Prefix_BlockedAccounts).Add(account);
            if (engine.Snapshot.Storages.Contains(key)) return false;

            engine.Snapshot.Storages.Add(key, new StorageItem(new byte[] { 0x01 }));
            return true;
        }

        [ContractMethod(0_03000000, CallFlags.AllowModifyStates)]
        private bool UnblockAccount(ApplicationEngine engine, UInt160 account)
        {
            if (!CheckCommittee(engine)) return false;

            var key = CreateStorageKey(Prefix_BlockedAccounts).Add(account);
            if (!engine.Snapshot.Storages.Contains(key)) return false;

            engine.Snapshot.Storages.Delete(key);
            return true;
        }
    }
}
