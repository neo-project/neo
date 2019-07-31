#pragma warning disable IDE0051
#pragma warning disable IDE0060

using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Manifest;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using VMArray = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native
{
    public sealed class PolicyContract : NativeContract
    {
        public override string ServiceName => "Neo.Native.Policy";

        private const byte Prefix_MaxTransactionsPerBlock = 23;
        private const byte Prefix_FeePerByte = 10;
        private const byte Prefix_BlockedAccounts = 15;

        public PolicyContract()
        {
            Manifest.Features = ContractFeatures.HasStorage;
        }

        internal bool CheckPolicy(Transaction tx, Snapshot snapshot)
        {
            UInt160[] blockedAccounts = GetBlockedAccounts(snapshot);
            if (blockedAccounts.Intersect(tx.GetScriptHashesForVerifying(snapshot)).Any())
                return false;
            return true;
        }

        private bool CheckValidators(ApplicationEngine engine)
        {
            UInt256 prev_hash = engine.Snapshot.PersistingBlock.PrevHash;
            TrimmedBlock prev_block = engine.Snapshot.Blocks[prev_hash];
            return InteropService.CheckWitness(engine, prev_block.NextConsensus);
        }

        internal override bool Initialize(ApplicationEngine engine)
        {
            if (!base.Initialize(engine)) return false;
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_MaxTransactionsPerBlock), new StorageItem
            {
                Value = BitConverter.GetBytes(512u)
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

        [ContractMethod(0_01000000, ContractParameterType.Integer, SafeMethod = true)]
        private StackItem GetMaxTransactionsPerBlock(ApplicationEngine engine, VMArray args)
        {
            return GetMaxTransactionsPerBlock(engine.Snapshot);
        }

        public uint GetMaxTransactionsPerBlock(Snapshot snapshot)
        {
            return BitConverter.ToUInt32(snapshot.Storages[CreateStorageKey(Prefix_MaxTransactionsPerBlock)].Value, 0);
        }

        [ContractMethod(0_01000000, ContractParameterType.Integer, SafeMethod = true)]
        private StackItem GetFeePerByte(ApplicationEngine engine, VMArray args)
        {
            return GetFeePerByte(engine.Snapshot);
        }

        public long GetFeePerByte(Snapshot snapshot)
        {
            return BitConverter.ToInt64(snapshot.Storages[CreateStorageKey(Prefix_FeePerByte)].Value, 0);
        }

        [ContractMethod(0_01000000, ContractParameterType.Array, SafeMethod = true)]
        private StackItem GetBlockedAccounts(ApplicationEngine engine, VMArray args)
        {
            return GetBlockedAccounts(engine.Snapshot).Select(p => (StackItem)p.ToArray()).ToList();
        }

        public UInt160[] GetBlockedAccounts(Snapshot snapshot)
        {
            return snapshot.Storages[CreateStorageKey(Prefix_BlockedAccounts)].Value.AsSerializableArray<UInt160>();
        }

        [ContractMethod(0_03000000, ContractParameterType.Boolean, ParameterTypes = new[] { ContractParameterType.Integer }, ParameterNames = new[] { "value" })]
        private StackItem SetMaxTransactionsPerBlock(ApplicationEngine engine, VMArray args)
        {
            if (!CheckValidators(engine)) return false;
            uint value = (uint)args[0].GetBigInteger();
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_MaxTransactionsPerBlock));
            storage.Value = BitConverter.GetBytes(value);
            return true;
        }

        [ContractMethod(0_03000000, ContractParameterType.Boolean, ParameterTypes = new[] { ContractParameterType.Integer }, ParameterNames = new[] { "value" })]
        private StackItem SetFeePerByte(ApplicationEngine engine, VMArray args)
        {
            if (!CheckValidators(engine)) return false;
            long value = (long)args[0].GetBigInteger();
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_FeePerByte));
            storage.Value = BitConverter.GetBytes(value);
            return true;
        }

        [ContractMethod(0_03000000, ContractParameterType.Boolean, ParameterTypes = new[] { ContractParameterType.Hash160 }, ParameterNames = new[] { "account" })]
        private StackItem BlockAccount(ApplicationEngine engine, VMArray args)
        {
            if (!CheckValidators(engine)) return false;
            UInt160 account = new UInt160(args[0].GetByteArray());
            StorageKey key = CreateStorageKey(Prefix_BlockedAccounts);
            StorageItem storage = engine.Snapshot.Storages[key];
            HashSet<UInt160> accounts = new HashSet<UInt160>(storage.Value.AsSerializableArray<UInt160>());
            if (!accounts.Add(account)) return false;
            storage = engine.Snapshot.Storages.GetAndChange(key);
            storage.Value = accounts.ToArray().ToByteArray();
            return true;
        }

        [ContractMethod(0_03000000, ContractParameterType.Boolean, ParameterTypes = new[] { ContractParameterType.Hash160 }, ParameterNames = new[] { "account" })]
        private StackItem UnblockAccount(ApplicationEngine engine, VMArray args)
        {
            if (!CheckValidators(engine)) return false;
            UInt160 account = new UInt160(args[0].GetByteArray());
            StorageKey key = CreateStorageKey(Prefix_BlockedAccounts);
            StorageItem storage = engine.Snapshot.Storages[key];
            HashSet<UInt160> accounts = new HashSet<UInt160>(storage.Value.AsSerializableArray<UInt160>());
            if (!accounts.Remove(account)) return false;
            storage = engine.Snapshot.Storages.GetAndChange(key);
            storage.Value = accounts.ToArray().ToByteArray();
            return true;
        }
    }
}
