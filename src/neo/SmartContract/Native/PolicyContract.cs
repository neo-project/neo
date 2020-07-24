#pragma warning disable IDE0051

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
        private const byte Prefix_PayloadMaxSize = 35;
        private const byte Prefix_MaxTransactionSize = 36;
        private const byte Prefix_MaxValidUntilBlockIncrement = 37;
        private const byte Prefix_MaxTransactionAttributes = 38;
        private const byte Prefix_MaxContractLength = 39;
        private const byte Prefix_ECDsaVerifyPrice = 40;
        private const byte Prefix_StoragePrice = 41;
        private const byte Prefix_MaxVerificationGas = 42;

        public PolicyContract()
        {
            Manifest.Features = ContractFeatures.HasStorage;
        }

        private bool CheckCommittees(ApplicationEngine engine)
        {
            UInt160 committeeMultiSigAddr = NEO.GetCommitteeAddress(engine.Snapshot);
            return engine.CheckWitnessInternal(committeeMultiSigAddr);
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
                ?.GetSerializableList<UInt160>().ToArray()
                ?? Array.Empty<UInt160>();
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public uint GetPayloadMaxSize(StoreView snapshot)
        {
            StorageItem item = snapshot.Storages.TryGet(CreateStorageKey(Prefix_PayloadMaxSize));
            if (item is null) return 0x02000000u;
            return (uint) (BigInteger) item;
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public uint GetMaxTransactionSize(StoreView snapshot)
        {
            StorageItem item = snapshot.Storages.TryGet(CreateStorageKey(Prefix_MaxTransactionSize));
            if (item is null) return 102400u;
            return (uint)(BigInteger)item;
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public uint GetMaxValidUntilBlockIncrement(StoreView snapshot)
        {
            StorageItem item = snapshot.Storages.TryGet(CreateStorageKey(Prefix_MaxValidUntilBlockIncrement));
            if (item is null) return 2102400u;
            return (uint)(BigInteger)item;
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public uint GetMaxTransactionAttributes(StoreView snapshot)
        {
            StorageItem item = snapshot.Storages.TryGet(CreateStorageKey(Prefix_MaxTransactionAttributes));
            if (item is null) return 16u;
            return (uint)(BigInteger)item;
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public uint GetMaxContractLength(StoreView snapshot)
        {
            StorageItem item = snapshot.Storages.TryGet(CreateStorageKey(Prefix_MaxContractLength));
            if (item is null) return 1048576u;
            return (uint)(BigInteger)item;
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public ulong GetECDsaVerifyPrice(StoreView snapshot)
        {
            StorageItem item = snapshot.Storages.TryGet(CreateStorageKey(Prefix_ECDsaVerifyPrice));
            if (item is null) return 0_01000000u;
            return (ulong)(BigInteger)item;
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public ulong GetStoragePrice(StoreView snapshot)
        {
            StorageItem item = snapshot.Storages.TryGet(CreateStorageKey(Prefix_StoragePrice));
            if (item is null) return 100000u;
            return (ulong)(BigInteger)item;
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public ulong GetMaxVerificationGas(StoreView snapshot)
        {
            StorageItem item = snapshot.Storages.TryGet(CreateStorageKey(Prefix_MaxVerificationGas));
            if (item is null) return 0_50000000u;
            return (ulong)(BigInteger)item;
        }

        [ContractMethod(0_03000000, CallFlags.AllowModifyStates)]
        private bool SetMaxBlockSize(ApplicationEngine engine, uint value)
        {
            if (!CheckCommittees(engine)) return false;
            if (Network.P2P.Message.PayloadMaxSize <= value) return false;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_MaxBlockSize), () => new StorageItem());
            storage.Set(value);
            return true;
        }

        [ContractMethod(0_03000000, CallFlags.AllowModifyStates)]
        private bool SetMaxTransactionsPerBlock(ApplicationEngine engine, uint value)
        {
            if (!CheckCommittees(engine)) return false;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_MaxTransactionsPerBlock), () => new StorageItem());
            storage.Set(value);
            return true;
        }

        [ContractMethod(0_03000000, CallFlags.AllowModifyStates)]
        private bool SetMaxBlockSystemFee(ApplicationEngine engine, long value)
        {
            if (!CheckCommittees(engine)) return false;
            if (value <= 4007600) return false;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_MaxBlockSystemFee), () => new StorageItem());
            storage.Set(value);
            return true;
        }

        [ContractMethod(0_03000000, CallFlags.AllowModifyStates)]
        private bool SetFeePerByte(ApplicationEngine engine, long value)
        {
            if (!CheckCommittees(engine)) return false;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_FeePerByte), () => new StorageItem());
            storage.Set(value);
            return true;
        }

        [ContractMethod(0_03000000, CallFlags.AllowModifyStates)]
        private bool BlockAccount(ApplicationEngine engine, UInt160 account)
        {
            if (!CheckCommittees(engine)) return false;
            StorageKey key = CreateStorageKey(Prefix_BlockedAccounts);
            StorageItem storage = engine.Snapshot.Storages.GetOrAdd(key, () => new StorageItem(new byte[1]));
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
            StorageItem storage = engine.Snapshot.Storages.TryGet(key);
            if (storage is null) return false;
            List<UInt160> accounts = storage.GetSerializableList<UInt160>();
            int index = accounts.IndexOf(account);
            if (index < 0) return false;
            engine.Snapshot.Storages.GetAndChange(key);
            accounts.RemoveAt(index);
            return true;
        }

        [ContractMethod(0_03000000, CallFlags.AllowModifyStates)]
        private bool SetPayloadMaxSize(ApplicationEngine engine, uint value)
        {
            if (!CheckCommittees(engine)) return false;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_PayloadMaxSize), () => new StorageItem());
            storage.Set(value);
            return true;
        }

        [ContractMethod(0_03000000, CallFlags.AllowModifyStates)]
        private bool SetMaxTransactionSize(ApplicationEngine engine, uint value)
        {
            if (!CheckCommittees(engine)) return false;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_MaxTransactionSize), () => new StorageItem());
            storage.Set(value);
            return true;
        }

        [ContractMethod(0_03000000, CallFlags.AllowModifyStates)]
        private bool SetMaxValidUntilBlockIncrement(ApplicationEngine engine, uint value)
        {
            if (!CheckCommittees(engine)) return false;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_MaxValidUntilBlockIncrement), () => new StorageItem());
            storage.Set(value);
            return true;
        }

        [ContractMethod(0_03000000, CallFlags.AllowModifyStates)]
        private bool SetMaxTransactionAttributes(ApplicationEngine engine, uint value)
        {
            if (!CheckCommittees(engine)) return false;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_MaxTransactionAttributes), () => new StorageItem());
            storage.Set(value);
            return true;
        }

        [ContractMethod(0_03000000, CallFlags.AllowModifyStates)]
        private bool SetMaxContractLength(ApplicationEngine engine, uint value)
        {
            if (!CheckCommittees(engine)) return false;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_MaxContractLength), () => new StorageItem());
            storage.Set(value);
            return true;
        }

        [ContractMethod(0_03000000, CallFlags.AllowModifyStates)]
        private bool SetECDsaVerifyPrice(ApplicationEngine engine, ulong value)
        {
            if (!CheckCommittees(engine)) return false;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_ECDsaVerifyPrice), () => new StorageItem());
            storage.Set(value);
            return true;
        }

        [ContractMethod(0_03000000, CallFlags.AllowModifyStates)]
        private bool SetStoragePrice(ApplicationEngine engine, ulong value)
        {
            if (!CheckCommittees(engine)) return false;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_StoragePrice), () => new StorageItem());
            storage.Set(value);
            return true;
        }

        [ContractMethod(0_03000000, CallFlags.AllowModifyStates)]
        private bool SetMaxVerificationGas(ApplicationEngine engine, ulong value)
        {
            if (!CheckCommittees(engine)) return false;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_MaxVerificationGas), () => new StorageItem());
            storage.Set(value);
            return true;
        }
    }
}
