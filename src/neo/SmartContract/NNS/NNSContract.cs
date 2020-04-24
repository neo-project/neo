using Neo.IO;
using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.NNS
{
    public partial class NNSContract: NativeContract
    {
        public override string ServiceName => "Neo.Native.NNS";
        public override int Id => -5;
        public string Name => "NNS";
        public string Symbol => "nns";
        public byte Decimals => 0;                                                                                                                                                                                                             

        protected const byte Prefix_TotalSupply = 22;
        protected const byte Prefix_RootAddress = 23;
        protected const byte Prefix_Address = 24;
        protected const byte Prefix_SecondAddress = 25;
        protected const byte Prefix_Admin = 26;
        protected const byte Prefix_RentalPrice = 27;

        private bool CheckValidators(ApplicationEngine engine)
        {
            UInt256 prev_hash = engine.Snapshot.PersistingBlock.PrevHash;
            TrimmedBlock prev_block = engine.Snapshot.Blocks[prev_hash];
            return InteropService.Runtime.CheckWitnessInternal(engine, prev_block.NextConsensus);
        }

        // 注册根域名
        [ContractMethod(0_03000000, ContractParameterType.Boolean, ParameterTypes = new[] { ContractParameterType.String }, ParameterNames = new[] { "name" })]
        private StackItem RegisterRootName(ApplicationEngine engine, Array args)
        {
            if (!GetAdmins(engine.Snapshot).Contains(engine.CallingScriptHash)) return false;
            string name = args[0].GetString();
            StorageKey key = CreateStorageKey(Prefix_RootAddress); // root
            StorageItem storage = engine.Snapshot.Storages[key]; 
            SortedSet<string> rootNames = new SortedSet<string>(storage.Value.AsSerializableArray<string>()); // ["org", "com"]
            if (!rootNames.Add(name)) return false;
            storage = engine.Snapshot.Storages.GetAndChange(key);
            storage.Value = rootNames.ToByteArray();
            return true;
        }

        // 获取根域名列表
        [ContractMethod(0_01000000, ContractParameterType.Array, SafeMethod = true)]
        private StackItem GetRootNames(ApplicationEngine engine, Array args)
        {
            return new Array(engine.ReferenceCounter, GetRootNames(engine.Snapshot).Select(p => (StackItem)p.ToArray()));
        }

        public string[] GetRootNames(StoreView snapshot)
        {
            return snapshot.Storages[CreateStorageKey(Prefix_RootAddress)].Value.AsSerializableArray<string>();
        }

        // 获取域名列表
        [ContractMethod(0_01000000, ContractParameterType.Array, SafeMethod = true)]
        private StackItem GetNames(ApplicationEngine engine, Array args)
        {
            return new Array(engine.ReferenceCounter, GetNames(engine.Snapshot).Select(p => (StackItem)p.ToArray()));
        }

        public string[] GetNames(StoreView snapshot)
        {
            return snapshot.Storages[CreateStorageKey(Prefix_Address)].Value.AsSerializableArray<string>();
        }

        //更新一级域名有效期
        [ContractMethod(0_03000000, ContractParameterType.Boolean, ParameterTypes = new[] { ContractParameterType.String, ContractParameterType.Integer }, ParameterNames = new[] { "name", "ttl" })]
        private StackItem RenewName(ApplicationEngine engine, Array args)
        {
            string name = args[0].GetString();
            if (name.Split(".").Length != 2) return false;
            uint ttl = (uint)args[1].GetBigInteger();
            string[] names = GetNames(engine.Snapshot);
            //判断是否是有效一级域名
            if (!names.Any(p => { string[] tmp = p.Split("."); return tmp.Length >= 2 && (tmp[tmp.Length - 2] + tmp[tmp.Length - 1]).Equals(name); }))
                return false;
            StorageKey key = CreateStorageKey(Prefix_Address, Encoding.ASCII.GetBytes(name));

            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_FirstAddress, Encoding.ASCII.GetBytes(name)));
            storage.Value = BitConverter.GetBytes(ttl);
            return true;
        }

        // 获取管理员
        [ContractMethod(0_01000000, ContractParameterType.Array, SafeMethod = true)]
        private StackItem GetAdmins(ApplicationEngine engine, Array args)
        {
            return new Array(engine.ReferenceCounter, GetAdmins(engine.Snapshot).Select(p => (StackItem)p.ToArray()));
        }

        public UInt160[] GetAdmins(StoreView snapshot)
        {
            return snapshot.Storages[CreateStorageKey(Prefix_Admin)].Value.AsSerializableArray<UInt160>();
        }

        // 设置管理员
        [ContractMethod(0_03000000, ContractParameterType.Boolean, ParameterTypes = new[] { ContractParameterType.Hash160 }, ParameterNames = new[] { "address" })]
        private StackItem SetAdmin(ApplicationEngine engine, Array args)
        {
            if (!CheckValidators(engine)) return false;
            UInt160 address = new UInt160(args[0].GetSpan());
            StorageKey key = CreateStorageKey(Prefix_Admin); // root
            StorageItem storage = engine.Snapshot.Storages[key];
            SortedSet<UInt160> admins = new SortedSet<UInt160>(storage.Value.AsSerializableArray<UInt160>()); // ["a", "b"]
            if (!admins.Add(address)) return false;
            storage = engine.Snapshot.Storages.GetAndChange(key);
            storage.Value = admins.ToByteArray();
            return true;
        }

        // 获取租赁价格
        [ContractMethod(0_01000000, ContractParameterType.Integer, SafeMethod = true)]
        private StackItem GetRentalPrice(ApplicationEngine engine, Array args)
        {
            return GetRentalPrice(engine.Snapshot);
        }

        public long GetRentalPrice(StoreView snapshot)
        {
            return BitConverter.ToInt64(snapshot.Storages[CreateStorageKey(Prefix_RentalPrice)].Value, 0);
        }

        // 设置租赁价格
        [ContractMethod(0_03000000, ContractParameterType.Boolean, ParameterTypes = new[] { ContractParameterType.Integer }, ParameterNames = new[] { "value" })]
        private StackItem SetRentalPrice(ApplicationEngine engine, Array args)
        {
            if (!CheckValidators(engine)) return false;
            uint value = (uint)args[0].GetBigInteger();
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_RentalPrice));
            storage.Value = BitConverter.GetBytes(value);
            return true;
        }
    }
}
