#pragma warning disable IDE0051
#pragma warning disable IDE0060

using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract.Manifest;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Numerics;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native
{
    public sealed class FeeContract : NativeContract
    {
        public override string ServiceName => "Neo.Native.Fee";
        public override int Id => -4;

        private const byte Prefix_Ratio = 11;
        private const byte Prefix_Syscall = 12;
        private const byte Prefix_OpCode = 13;

        public FeeContract()
        {
            Manifest.Features = ContractFeatures.HasStorage;
        }

        internal override bool Initialize(ApplicationEngine engine)
        {
            if (!base.Initialize(engine)) return false;
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_Ratio), new StorageItem
            {
                Value = BitConverter.GetBytes(1u)
            });
            return true;
        }

        private bool CheckValidators(ApplicationEngine engine)
        {
            UInt256 prev_hash = engine.Snapshot.PersistingBlock.PrevHash;
            TrimmedBlock prev_block = engine.Snapshot.Blocks[prev_hash];
            return InteropService.Runtime.CheckWitnessInternal(engine, prev_block.NextConsensus);
        }

        [ContractMethod(0_00010000, ContractParameterType.Integer, ParameterTypes = new[] { ContractParameterType.Integer }, ParameterNames = new[] { "value" })]
        private StackItem GetSyscallPrice(ApplicationEngine engine, Array args)
        {
            uint method = (uint)args[0].GetBigInteger();
            return GetSyscallPrice(method, engine.Snapshot);
        }

        public long GetSyscallPrice(uint method, StoreView snapshot, EvaluationStack stack = null)
        {
            if (snapshot.Storages.TryGet(CreateStorageKey(Prefix_Syscall, BitConverter.GetBytes(method))) != null)
            {
                return BitConverter.ToInt64(snapshot.Storages[CreateStorageKey(Prefix_Syscall, BitConverter.GetBytes(method))].Value, 0) / GetRatio(snapshot);
            }
            return InteropService.GetPrice(method, stack) / GetRatio(snapshot);
        }

        [ContractMethod(0_00030000, ContractParameterType.Boolean, ParameterTypes = new[] { ContractParameterType.Array }, ParameterNames = new[] { "value" })]
        private StackItem SetSyscallPrice(ApplicationEngine engine, Array args)
        {
            if (!CheckValidators(engine)) return false;
            uint method = (uint)((Array)args[0])[0].GetBigInteger();
            long value = (long)((Array)args[0])[1].GetBigInteger();
            StorageKey key = CreateStorageKey(Prefix_Syscall, BitConverter.GetBytes(method));
            StorageItem item = engine.Snapshot.Storages.GetAndChange(key, () => new StorageItem());
            item.Value = BitConverter.GetBytes(value);
            return true;
        }

        [ContractMethod(0_00010000, ContractParameterType.Integer, ParameterTypes = new[] { ContractParameterType.Integer }, ParameterNames = new[] { "value" })]
        private StackItem GetOpCodePrice(ApplicationEngine engine, Array args)
        {
            uint opCode = (uint)args[0].GetBigInteger();
            return GetOpCodePrice((OpCode)opCode, engine.Snapshot);
        }

        public long GetOpCodePrice(OpCode opCode, StoreView snapshot)
        {
            if (snapshot.Storages.TryGet(CreateStorageKey(Prefix_OpCode, BitConverter.GetBytes((int)opCode))) != null)
            {
                return BitConverter.ToInt64(snapshot.Storages[CreateStorageKey(Prefix_OpCode, BitConverter.GetBytes((int)opCode))].Value, 0) / GetRatio(snapshot);
            }
            return ApplicationEngine.OpCodePrices[opCode] / GetRatio(snapshot);
        }

        [ContractMethod(0_00030000, ContractParameterType.Boolean, ParameterTypes = new[] { ContractParameterType.Array }, ParameterNames = new[] { "value" })]
        private StackItem SetOpCodePrice(ApplicationEngine engine, Array args)
        {
            if (!CheckValidators(engine)) return false;
            uint opCode = (uint)((Array)args[0])[0].GetBigInteger();
            long value = (long)((Array)args[0])[1].GetBigInteger();
            StorageKey key = CreateStorageKey(Prefix_OpCode, BitConverter.GetBytes(opCode));
            StorageItem item = engine.Snapshot.Storages.GetAndChange(key, () => new StorageItem());
            item.Value = BitConverter.GetBytes(value);
            return true;
        }

        [ContractMethod(0_00010000, ContractParameterType.Integer, SafeMethod = true)]
        private StackItem GetRatio(ApplicationEngine engine, Array args)
        {
            return GetRatio(engine.Snapshot);
        }

        public uint GetRatio(StoreView snapshot)
        {
            if (snapshot.Storages.TryGet(CreateStorageKey(Prefix_Ratio)) != null)
                return BitConverter.ToUInt32(snapshot.Storages[CreateStorageKey(Prefix_Ratio)].Value, 0);
            else
                return 1;
        }

        [ContractMethod(0_00030000, ContractParameterType.Boolean, ParameterTypes = new[] { ContractParameterType.Integer }, ParameterNames = new[] { "value" })]
        private StackItem SetRatio(ApplicationEngine engine, Array args)
        {
            if (!CheckValidators(engine)) return false;
            uint value = (uint)args[0].GetBigInteger();
            if (value == 0)
                return false;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_Ratio));
            storage.Value = BitConverter.GetBytes(value);
            return true;
        }
    }
}
