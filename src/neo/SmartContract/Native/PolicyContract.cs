#pragma warning disable IDE0051

using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using System;
using System.Numerics;

namespace Neo.SmartContract.Native
{
    public sealed class PolicyContract : NativeContract
    {
        public const uint DefaultExecFeeFactor = 30;
        public const uint DefaultStoragePrice = 100000;
        private const uint MaxExecFeeFactor = 1000;
        private const uint MaxStoragePrice = 10000000;

        private const byte Prefix_MaxTransactionsPerBlock = 23;
        private const byte Prefix_FeePerByte = 10;
        private const byte Prefix_BlockedAccount = 15;
        private const byte Prefix_MaxBlockSize = 12;
        private const byte Prefix_MaxBlockSystemFee = 17;
        private const byte Prefix_ExecFeeFactor = 18;
        private const byte Prefix_StoragePrice = 19;

        internal PolicyContract()
        {
        }

        [ContractMethod(0_01000000, CallFlags.ReadStates)]
        public uint GetMaxTransactionsPerBlock(DataCache snapshot)
        {
            StorageItem item = snapshot.TryGet(CreateStorageKey(Prefix_MaxTransactionsPerBlock));
            if (item is null) return 512;
            return (uint)(BigInteger)item;
        }

        [ContractMethod(0_01000000, CallFlags.ReadStates)]
        public uint GetMaxBlockSize(DataCache snapshot)
        {
            StorageItem item = snapshot.TryGet(CreateStorageKey(Prefix_MaxBlockSize));
            if (item is null) return 1024 * 256;
            return (uint)(BigInteger)item;
        }

        [ContractMethod(0_01000000, CallFlags.ReadStates)]
        public long GetMaxBlockSystemFee(DataCache snapshot)
        {
            StorageItem item = snapshot.TryGet(CreateStorageKey(Prefix_MaxBlockSystemFee));
            if (item is null) return 9000 * (long)GAS.Factor; // For the transfer method of NEP5, the maximum persisting time is about three seconds.
            return (long)(BigInteger)item;
        }

        [ContractMethod(0_01000000, CallFlags.ReadStates)]
        public long GetFeePerByte(DataCache snapshot)
        {
            StorageItem item = snapshot.TryGet(CreateStorageKey(Prefix_FeePerByte));
            if (item is null) return 1000;
            return (long)(BigInteger)item;
        }

        [ContractMethod(0_01000000, CallFlags.ReadStates)]
        public uint GetExecFeeFactor(DataCache snapshot)
        {
            StorageItem item = snapshot.TryGet(CreateStorageKey(Prefix_ExecFeeFactor));
            if (item is null) return DefaultExecFeeFactor;
            return (uint)(BigInteger)item;
        }

        [ContractMethod(0_01000000, CallFlags.ReadStates)]
        public uint GetStoragePrice(DataCache snapshot)
        {
            StorageItem item = snapshot.TryGet(CreateStorageKey(Prefix_StoragePrice));
            if (item is null) return DefaultStoragePrice;
            return (uint)(BigInteger)item;
        }

        [ContractMethod(0_01000000, CallFlags.ReadStates)]
        public bool IsBlocked(DataCache snapshot, UInt160 account)
        {
            return snapshot.Contains(CreateStorageKey(Prefix_BlockedAccount).Add(account));
        }

        [ContractMethod(0_03000000, CallFlags.WriteStates)]
        private void SetMaxBlockSize(ApplicationEngine engine, uint value)
        {
            if (value > Message.PayloadMaxSize) throw new ArgumentOutOfRangeException(nameof(value));
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            StorageItem storage = engine.Snapshot.GetAndChange(CreateStorageKey(Prefix_MaxBlockSize), () => new StorageItem());
            storage.Set(value);
        }

        [ContractMethod(0_03000000, CallFlags.WriteStates)]
        private void SetMaxTransactionsPerBlock(ApplicationEngine engine, uint value)
        {
            if (value > Block.MaxTransactionsPerBlock) throw new ArgumentOutOfRangeException(nameof(value));
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            StorageItem storage = engine.Snapshot.GetAndChange(CreateStorageKey(Prefix_MaxTransactionsPerBlock), () => new StorageItem());
            storage.Set(value);
        }

        [ContractMethod(0_03000000, CallFlags.WriteStates)]
        private void SetMaxBlockSystemFee(ApplicationEngine engine, long value)
        {
            if (value <= 4007600) throw new ArgumentOutOfRangeException(nameof(value));
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            StorageItem storage = engine.Snapshot.GetAndChange(CreateStorageKey(Prefix_MaxBlockSystemFee), () => new StorageItem());
            storage.Set(value);
        }

        [ContractMethod(0_03000000, CallFlags.WriteStates)]
        private void SetFeePerByte(ApplicationEngine engine, long value)
        {
            if (value < 0 || value > 1_00000000) throw new ArgumentOutOfRangeException(nameof(value));
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            StorageItem storage = engine.Snapshot.GetAndChange(CreateStorageKey(Prefix_FeePerByte), () => new StorageItem());
            storage.Set(value);
        }

        [ContractMethod(0_03000000, CallFlags.WriteStates)]
        private void SetExecFeeFactor(ApplicationEngine engine, uint value)
        {
            if (value == 0 || value > MaxExecFeeFactor) throw new ArgumentOutOfRangeException(nameof(value));
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            StorageItem storage = engine.Snapshot.GetAndChange(CreateStorageKey(Prefix_ExecFeeFactor), () => new StorageItem());
            storage.Set(value);
        }

        [ContractMethod(0_03000000, CallFlags.WriteStates)]
        private void SetStoragePrice(ApplicationEngine engine, uint value)
        {
            if (value == 0 || value > MaxStoragePrice) throw new ArgumentOutOfRangeException(nameof(value));
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            StorageItem storage = engine.Snapshot.GetAndChange(CreateStorageKey(Prefix_StoragePrice), () => new StorageItem());
            storage.Set(value);
        }

        [ContractMethod(0_03000000, CallFlags.WriteStates)]
        private bool BlockAccount(ApplicationEngine engine, UInt160 account)
        {
            if (!CheckCommittee(engine)) throw new InvalidOperationException();

            var key = CreateStorageKey(Prefix_BlockedAccount).Add(account);
            if (engine.Snapshot.Contains(key)) return false;

            engine.Snapshot.Add(key, new StorageItem(new byte[] { 0x01 }));
            return true;
        }

        [ContractMethod(0_03000000, CallFlags.WriteStates)]
        private bool UnblockAccount(ApplicationEngine engine, UInt160 account)
        {
            if (!CheckCommittee(engine)) throw new InvalidOperationException();

            var key = CreateStorageKey(Prefix_BlockedAccount).Add(account);
            if (!engine.Snapshot.Contains(key)) return false;

            engine.Snapshot.Delete(key);
            return true;
        }
    }
}
