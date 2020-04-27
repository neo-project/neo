using Neo.Cryptography;
using Neo.IO;
using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System.Linq;
using System.Text;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.NNS
{
    partial class NNSContract
    {
        // only can be called by the admin of the name
        [ContractMethod(0_03000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.String, ContractParameterType.String, ContractParameterType.Integer }, ParameterNames = new[] { "name", "text", "recordType" })]
        public StackItem SetText(ApplicationEngine engine, Array args)
        {
            string name = args[0].GetString().ToLower();
            UInt256 nameHash = new UInt256(Crypto.Hash256(Encoding.UTF8.GetBytes(name.ToLower())));
            string text = args[1].GetString();
            RecordType recordType = (RecordType)(byte)args[2].GetBigInteger();
            if (recordType == RecordType.A && IsDomain(name)) return false;

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

        // return the text and recordtype of the name
        [ContractMethod(0_03000000, ContractParameterType.String, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.String}, ParameterNames = new[] { "name"})]
        private StackItem Resolve(ApplicationEngine engine, Array args)
        {
            string name = args[0].GetString().ToLower();
            return Resolve(engine.Snapshot, name);   
        }

        public string Resolve(StoreView snapshot, string name)
        {
            UInt256 nameHash = new UInt256(Crypto.Hash256(Encoding.UTF8.GetBytes(name.ToLower())));
            StorageKey key = CreateStorageKey(Prefix_Record, nameHash);
            StorageItem storage = snapshot.Storages[key];
            if (storage is null) return "";
            RecordInfo recordInfo = storage.Value.AsSerializable<RecordInfo>();

            RecordType recordType = recordInfo.RecordType;
            if (recordType == RecordType.CNAME)
            {
                name = recordInfo.Text;
                Resolve(snapshot, name);
            }
            return recordInfo.ToString();
        }
    }
}
