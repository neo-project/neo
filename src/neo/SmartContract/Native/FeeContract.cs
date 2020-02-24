#pragma warning disable IDE0051
#pragma warning disable IDE0060

using Neo.Ledger;
using Neo.Persistence;
using Neo.VM;
using Neo.VM.Types;
using System;
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

        internal FeeContract()
        {
        }

        internal override bool Initialize(ApplicationEngine engine)
        {
            if (!base.Initialize(engine)) return false;
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_Ratio), new StorageItem
            {
                Value = BitConverter.GetBytes(100u)
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
            return GetSyscallPrice(method, null, engine.Snapshot);
        }

        public long GetSyscallPrice(uint method, EvaluationStack stack = null, StoreView snapshot = null)
        {
            if (snapshot?.Storages.TryGet(CreateStorageKey(Prefix_Syscall, BitConverter.GetBytes(method))) != null)
            {
                return BitConverter.ToInt64(snapshot?.Storages[CreateStorageKey(Prefix_Syscall, BitConverter.GetBytes(method))].Value, 0);
            }
            return InteropService.GetPrice(method, stack);
        }

        [ContractMethod(0_00030000, ContractParameterType.Integer, ParameterTypes = new[] { ContractParameterType.Integer }, ParameterNames = new[] { "value" })]
        private StackItem SetSyscallPrice(ApplicationEngine engine, Array args)
        {
            if (!CheckValidators(engine)) return false;
            uint method = (uint)args[0].GetBigInteger();
            long value = InteropService.GetPrice(method, engine.CurrentContext.EvaluationStack) / GetRatio(engine.Snapshot);
            StorageItem storage = engine.Snapshot.Storages.TryGet(CreateStorageKey(Prefix_Syscall, BitConverter.GetBytes(method)));
            if (storage != null)
            {
                storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_Syscall, BitConverter.GetBytes(method)));
                storage.Value = BitConverter.GetBytes(value);
            }
            else
            {
                engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_Syscall, BitConverter.GetBytes(method)), new StorageItem
                {
                    Value = BitConverter.GetBytes(value)
                });
            }
            return true;
        }

        [ContractMethod(0_00010000, ContractParameterType.Integer, ParameterTypes = new[] { ContractParameterType.Integer }, ParameterNames = new[] { "value" })]
        private StackItem GetOpCodePrice(ApplicationEngine engine, Array args)
        {
            uint opCode = (uint)args[0].GetBigInteger();
            return GetOpCodePrice((OpCode)opCode, engine.Snapshot);
        }

        public long GetOpCodePrice(OpCode opCode, StoreView snapshot = null)
        {
            if (snapshot?.Storages.TryGet(CreateStorageKey(Prefix_OpCode, BitConverter.GetBytes((int)opCode))) != null)
            {
                return BitConverter.ToInt64(snapshot.Storages[CreateStorageKey(Prefix_OpCode, BitConverter.GetBytes((int)opCode))].Value, 0);
            }
            return ApplicationEngine.OpCodePrices[opCode];
        }

        [ContractMethod(0_00030000, ContractParameterType.Integer, ParameterTypes = new[] { ContractParameterType.Integer }, ParameterNames = new[] { "value" })]
        private StackItem SetOpCodePrice(ApplicationEngine engine, Array args)
        {
            if (!CheckValidators(engine)) return false;
            uint opCode = (uint)args[0].GetBigInteger();
            long value = ApplicationEngine.OpCodePrices[(OpCode)opCode] / GetRatio(engine.Snapshot);
            StorageItem storage = engine.Snapshot.Storages.TryGet(CreateStorageKey(Prefix_OpCode, BitConverter.GetBytes(opCode)));
            if (storage != null)
            {
                storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_OpCode, BitConverter.GetBytes(opCode)));
                storage.Value = BitConverter.GetBytes(value);
            }
            else
            {
                engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_OpCode, BitConverter.GetBytes(opCode)), new StorageItem
                {
                    Value = BitConverter.GetBytes(value)
                });
            }
            return true;
        }

        /**
        [ContractMethod(0_00030000, ContractParameterType.Boolean, ParameterTypes = new[] { ContractParameterType.Integer }, ParameterNames = new[] { "value" })]
        private StackItem SetOpcodePrice(ApplicationEngine engine, Array args)
        {
            if (!CheckValidators(engine)) return false;
            byte opCode = args[0].GetBigInteger().ToByteArray()[0];
            ApplicationEngine.OpCodePrices[(OpCode)opCode] / GetRatio(engine.Snapshot);
            return true;
        }
        **/

        [ContractMethod(0_00010000, ContractParameterType.Integer, SafeMethod = true)]
        private StackItem GetRatio(ApplicationEngine engine, Array args)
        {
            return GetRatio(engine.Snapshot);
        }

        public uint GetRatio(StoreView snapshot)
        {
            return BitConverter.ToUInt32(snapshot.Storages[CreateStorageKey(Prefix_Ratio)].Value, 0);
        }

        [ContractMethod(0_00030000, ContractParameterType.Boolean, ParameterTypes = new[] { ContractParameterType.Integer }, ParameterNames = new[] { "value" })]
        private StackItem SetRatio(ApplicationEngine engine, Array args)
        {
            if (!CheckValidators(engine)) return false;
            uint value = (uint)args[0].GetBigInteger();
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_Ratio));
            storage.Value = BitConverter.GetBytes(value);
            return true;
        }
    }
}
