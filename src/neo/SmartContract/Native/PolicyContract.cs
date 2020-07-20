#pragma warning disable IDE0051

using Neo.IO;
using Neo.Ledger;
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

        public PolicyContract()
        {
            Manifest.Features = ContractFeatures.HasStorage;
        }

        private bool CheckCommittees(ApplicationEngine engine)
        {
            UInt160 committeeMultiSigAddr = NEO.GetCommitteeAddress(engine.Snapshot);
            return engine.CheckWitnessInternal(committeeMultiSigAddr);
        }

        internal override void Initialize(ApplicationEngine engine)
        {
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_MaxBlockSize), new StorageItem
            {
                Value = BitConverter.GetBytes(1024u * 256u)
            });
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_MaxTransactionsPerBlock), new StorageItem
            {
                Value = BitConverter.GetBytes(512u)
            });
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_MaxBlockSystemFee), new StorageItem
            {
                Value = BitConverter.GetBytes(9000 * (long)GAS.Factor) // For the transfer method of NEP5, the maximum persisting time is about three seconds.
            });
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_FeePerByte), new StorageItem
            {
                Value = BitConverter.GetBytes(1000L)
            });
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_BlockedAccounts), new StorageItem
            {
                Value = new UInt160[0].ToByteArray()
            });
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public uint GetMaxTransactionsPerBlock(StoreView snapshot)
        {
            return (uint)(BigInteger)snapshot.Storages[CreateStorageKey(Prefix_MaxTransactionsPerBlock)];
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public uint GetMaxBlockSize(StoreView snapshot)
        {
            return (uint)(BigInteger)snapshot.Storages[CreateStorageKey(Prefix_MaxBlockSize)];
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public long GetMaxBlockSystemFee(StoreView snapshot)
        {
            return (long)(BigInteger)snapshot.Storages[CreateStorageKey(Prefix_MaxBlockSystemFee)];
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public long GetFeePerByte(StoreView snapshot)
        {
            return (long)(BigInteger)snapshot.Storages[CreateStorageKey(Prefix_FeePerByte)];
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public UInt160[] GetBlockedAccounts(StoreView snapshot)
        {
            return snapshot.Storages[CreateStorageKey(Prefix_BlockedAccounts)].GetSerializableList<UInt160>().ToArray();
        }

        [ContractMethod(0_03000000, CallFlags.AllowModifyStates)]
        private bool SetMaxBlockSize(ApplicationEngine engine, uint value)
        {
            if (!CheckCommittees(engine)) return false;
            if (Network.P2P.Message.PayloadMaxSize <= value) return false;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_MaxBlockSize));
            storage.Set(value);
            return true;
        }

        [ContractMethod(0_03000000, CallFlags.AllowModifyStates)]
        private bool SetMaxTransactionsPerBlock(ApplicationEngine engine, uint value)
        {
            if (!CheckCommittees(engine)) return false;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_MaxTransactionsPerBlock));
            storage.Set(value);
            return true;
        }

        [ContractMethod(0_03000000, CallFlags.AllowModifyStates)]
        private bool SetMaxBlockSystemFee(ApplicationEngine engine, long value)
        {
            if (!CheckCommittees(engine)) return false;
            if (value <= 4007600) return false;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_MaxBlockSystemFee));
            storage.Set(value);
            return true;
        }

        [ContractMethod(0_03000000, CallFlags.AllowModifyStates)]
        private bool SetFeePerByte(ApplicationEngine engine, long value)
        {
            if (!CheckCommittees(engine)) return false;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_FeePerByte));
            storage.Set(value);
            return true;
        }

        [ContractMethod(0_03000000, CallFlags.AllowModifyStates)]
        private bool BlockAccount(ApplicationEngine engine, UInt160 account)
        {
            if (!CheckCommittees(engine)) return false;
            StorageKey key = CreateStorageKey(Prefix_BlockedAccounts);
            StorageItem storage = engine.Snapshot.Storages[key];
            List<UInt160> accounts = storage.GetSerializableList<UInt160>();
            if (accounts.Contains(account)) return false;
            engine.Snapshot.Storages.GetAndChange(key);
            accounts.Add(account);
            return true;
        }

        [ContractMethod(0_03000000, CallFlags.AllowModifyStates)]
        private bool UnblockAccount(ApplicationEngine engine, UInt160 account)
        {
            if (!CheckCommittees(engine)) return false;
            StorageKey key = CreateStorageKey(Prefix_BlockedAccounts);
            StorageItem storage = engine.Snapshot.Storages[key];
            List<UInt160> accounts = storage.GetSerializableList<UInt160>();
            int index = accounts.IndexOf(account);
            if (index < 0) return false;
            engine.Snapshot.Storages.GetAndChange(key);
            accounts.RemoveAt(index);
            return true;
        }
    }
}
