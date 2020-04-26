using Neo.Cryptography;
using Neo.IO;
using Neo.Ledger;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Linq;
using System.Text;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.NNS
{
    partial class NNSContract
    {
        // 只有admin可以调用
        [ContractMethod(0_03000000, ContractParameterType.Boolean, ParameterTypes = new[] { ContractParameterType.String, ContractParameterType.String, ContractParameterType.Integer }, ParameterNames = new[] { "name", "text", "recordType" })]
        public StackItem SetText(ApplicationEngine engine, Array args)
        {
            string name = args[0].GetString().ToLower();
            UInt256 nameHash = new UInt256(Crypto.Hash256(Encoding.UTF8.GetBytes(name.ToLower())));
            string text = args[1].GetString();
            RecordType recordType = (RecordType)BitConverter.GetBytes((uint)args[2].GetBigInteger())[0];
            StorageKey key = CreateStorageKey(Prefix_Record, nameHash);
            StorageItem storage = engine.Snapshot.Storages[key];
            RecordInfo recordInfo = new RecordInfo { Text = text, RecordType = recordType };
            if (storage.Value is null)
            {
                engine.Snapshot.Storages.Add(key, new StorageItem
                {
                    Value = recordInfo.ToArray()
                });
            }
            else
            {
                storage = engine.Snapshot.Storages.GetAndChange(key);
                storage.Value = recordInfo.ToArray();
            }
            return true;
        }

        [ContractMethod(0_03000000, ContractParameterType.String, ParameterTypes = new[] { ContractParameterType.String}, ParameterNames = new[] { "name"})]
        public StackItem Resolve(ApplicationEngine engine, Array args)
        {
            string name = args[0].GetString().ToLower();
            UInt256 nameHash = new UInt256(Crypto.Hash256(Encoding.UTF8.GetBytes(name.ToLower())));
            StorageKey key = CreateStorageKey(Prefix_Record, nameHash);
            StorageItem storage = engine.Snapshot.Storages[key];
            if (storage.Value is null) return false;
            RecordInfo recordInfo = storage.Value.AsSerializable<RecordInfo>();
            return recordInfo.ToString();    
        }
    }
}
