#pragma warning disable IDE0051

using Neo.Persistence;
using System;
using System.Numerics;

namespace Neo.SmartContract.Native
{
    public sealed class PolicyContract : NativeContract
    {
        public const uint DefaultExecFeeFactor = 30;
        public const uint DefaultStoragePrice = 100000;
        public const uint MaxExecFeeFactor = 1000;
        public const uint MaxStoragePrice = 10000000;

        private const byte Prefix_BlockedAccount = 15;
        private const byte Prefix_FeePerByte = 10;
        private const byte Prefix_ExecFeeFactor = 18;
        private const byte Prefix_StoragePrice = 19;

        internal PolicyContract()
        {
        }

        internal override void Initialize(ApplicationEngine engine)
        {
            engine.Snapshot.Add(CreateStorageKey(Prefix_FeePerByte), new StorageItem(1000));
            engine.Snapshot.Add(CreateStorageKey(Prefix_ExecFeeFactor), new StorageItem(DefaultExecFeeFactor));
            engine.Snapshot.Add(CreateStorageKey(Prefix_StoragePrice), new StorageItem(DefaultStoragePrice));
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public long GetFeePerByte(DataCache snapshot)
        {
            return (long)(BigInteger)snapshot[CreateStorageKey(Prefix_FeePerByte)];
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public uint GetExecFeeFactor(DataCache snapshot)
        {
            return (uint)(BigInteger)snapshot[CreateStorageKey(Prefix_ExecFeeFactor)];
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public uint GetStoragePrice(DataCache snapshot)
        {
            return (uint)(BigInteger)snapshot[CreateStorageKey(Prefix_StoragePrice)];
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public bool IsBlocked(DataCache snapshot, UInt160 account)
        {
            return snapshot.Contains(CreateStorageKey(Prefix_BlockedAccount).Add(account));
        }

        [ContractMethod(RequiredCallFlags = CallFlags.WriteStates)]
        private void SetFeePerByte(ApplicationEngine engine, long value)
        {
            if (value < 0 || value > 1_00000000) throw new ArgumentOutOfRangeException(nameof(value));
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            engine.Snapshot.GetAndChange(CreateStorageKey(Prefix_FeePerByte)).Set(value);
        }

        [ContractMethod(RequiredCallFlags = CallFlags.WriteStates)]
        private void SetExecFeeFactor(ApplicationEngine engine, uint value)
        {
            if (value == 0 || value > MaxExecFeeFactor) throw new ArgumentOutOfRangeException(nameof(value));
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            engine.Snapshot.GetAndChange(CreateStorageKey(Prefix_ExecFeeFactor)).Set(value);
        }

        [ContractMethod(RequiredCallFlags = CallFlags.WriteStates)]
        private void SetStoragePrice(ApplicationEngine engine, uint value)
        {
            if (value == 0 || value > MaxStoragePrice) throw new ArgumentOutOfRangeException(nameof(value));
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            engine.Snapshot.GetAndChange(CreateStorageKey(Prefix_StoragePrice)).Set(value);
        }

        [ContractMethod(RequiredCallFlags = CallFlags.WriteStates)]
        private bool BlockAccount(ApplicationEngine engine, UInt160 account)
        {
            if (!CheckCommittee(engine)) throw new InvalidOperationException();

            var key = CreateStorageKey(Prefix_BlockedAccount).Add(account);
            if (engine.Snapshot.Contains(key)) return false;

            engine.Snapshot.Add(key, new StorageItem(Array.Empty<byte>()));
            return true;
        }

        [ContractMethod(RequiredCallFlags = CallFlags.WriteStates)]
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
