using Neo.IO;
using Neo.Ledger;
using Neo.Persistence;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.SmartContract.Native
{
    public sealed class PolicyContract : NativeContract
    {
        public override string ServiceName => "Neo.Native.Policy";
        public override ContractPropertyState Properties => ContractPropertyState.HasStorage;

        private const byte Prefix_MaxFreeTransactionSize = 22;
        private const byte Prefix_FeePerByte = 10;
        private const byte Prefix_BlockedAccounts = 15;

        protected override StackItem Main(ApplicationEngine engine, string operation, VM.Types.Array args)
        {
            switch (operation)
            {
                case "getMaxFreeTransactionSize":
                    return GetMaxFreeTransactionSize(engine.Snapshot);
                case "getFeePerByte":
                    return GetFeePerByte(engine.Snapshot);
                case "getBlockedAccounts":
                    return GetBlockedAccounts(engine.Snapshot).Select(p => (StackItem)p.ToArray()).ToArray();
                case "setMaxFreeTransactionSize":
                    return SetMaxFreeTransactionSize(engine.Snapshot, (uint)args[0].GetBigInteger());
                case "setFeePerByte":
                    return SetFeePerByte(engine.Snapshot, (long)args[0].GetBigInteger());
                case "blockAccount":
                    return BlockAccount(engine.Snapshot, new UInt160(args[0].GetByteArray()));
                default:
                    return base.Main(engine, operation, args);
            }
        }

        internal override bool Initialize(ApplicationEngine engine)
        {
            if (!base.Initialize(engine)) return false;
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_MaxFreeTransactionSize), new StorageItem
            {
                Value = BitConverter.GetBytes(256u)
            });
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_FeePerByte), new StorageItem
            {
                Value = BitConverter.GetBytes(1000L)
            });
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_BlockedAccounts), new StorageItem
            {
                Value = new UInt160[0].ToByteArray()
            });
            return true;
        }

        private uint GetMaxFreeTransactionSize(Snapshot snapshot)
        {
            return BitConverter.ToUInt32(snapshot.Storages[CreateStorageKey(Prefix_MaxFreeTransactionSize)].Value, 0);
        }

        private long GetFeePerByte(Snapshot snapshot)
        {
            return BitConverter.ToInt64(snapshot.Storages[CreateStorageKey(Prefix_FeePerByte)].Value, 0);
        }

        private UInt160[] GetBlockedAccounts(Snapshot snapshot)
        {
            return snapshot.Storages[CreateStorageKey(Prefix_BlockedAccounts)].Value.AsSerializableArray<UInt160>();
        }

        private bool SetMaxFreeTransactionSize(Snapshot snapshot, uint value)
        {
            StorageItem storage = snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_MaxFreeTransactionSize));
            storage.Value = BitConverter.GetBytes(value);
            return true;
        }

        private bool SetFeePerByte(Snapshot snapshot, long value)
        {
            StorageItem storage = snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_FeePerByte));
            storage.Value = BitConverter.GetBytes(value);
            return true;
        }

        private bool BlockAccount(Snapshot snapshot, UInt160 account)
        {
            StorageKey key = CreateStorageKey(Prefix_BlockedAccounts);
            StorageItem storage = snapshot.Storages[key];
            HashSet<UInt160> accounts = new HashSet<UInt160>(storage.Value.AsSerializableArray<UInt160>());
            if (!accounts.Add(account)) return false;
            storage = snapshot.Storages.GetAndChange(key);
            storage.Value = accounts.ToArray().ToByteArray();
            return true;
        }
    }
}
