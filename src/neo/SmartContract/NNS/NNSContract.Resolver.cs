using Neo.Cryptography;
using Neo.IO;
using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
            UInt256 nameHash = ComputeNameHash(name);
            if (isExpired(engine.Snapshot, nameHash)) return false;

            string text = args[1].GetString();
            RecordType recordType = (RecordType)(byte)args[2].GetBigInteger();
            if ((recordType == RecordType.A || recordType == RecordType.CNAME) && IsDomain(name)) return false;

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
        [ContractMethod(0_03000000, ContractParameterType.String, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.String }, ParameterNames = new[] { "name" })]
        private StackItem Resolve(ApplicationEngine engine, Array args)
        {
            string name = args[0].GetString().ToLower();
            return Resolve(engine.Snapshot, name);
        }

        public string Resolve(StoreView snapshot, string name, int deepCount = 0)
        {
            if (deepCount++ > 100) // TBD
            {
                return new RecordInfo { Text = "The count of domain redirection exceeds 100 times", RecordType = RecordType.ERROR }.ToString();
            }

            UInt256 nameHash = ComputeNameHash(name);
            if (isExpired(snapshot, nameHash))
            {
                return new RecordInfo { Text = "TTL is expired", RecordType = RecordType.ERROR }.ToString();
            }

            StorageKey key = CreateStorageKey(Prefix_Record, nameHash);
            StorageItem storage = snapshot.Storages[key];
            if (storage is null)
            {
                return new RecordInfo { Text = "Name does not exist", RecordType = RecordType.ERROR }.ToString();
            }
            RecordInfo recordInfo = storage.Value.AsSerializable<RecordInfo>();

            RecordType recordType = recordInfo.RecordType;
            switch (recordType)
            {
                case RecordType.CNAME:
                    name = recordInfo.Text;
                    Resolve(snapshot, name, deepCount);
                    break;
                case RecordType.NS:
                    name = string.Join(".", name.Split(".")[1..]);
                    Resolve(snapshot, name, deepCount);
                    break;
            }
            return recordInfo.ToString();
        }

        public bool IsDomain(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string pattern = @"^(?=^.{3,255}$)[a-zA-Z0-9][-a-zA-Z0-9]{0,62}(\.[a-zA-Z0-9][-a-zA-Z0-9]{0,62}){1,3}$";
            Regex regex = new Regex(pattern);
            return regex.Match(name).Success;
        }

        public bool IsRootDomain(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string pattern = @"^[a-zA-Z]{0,62}$";
            Regex regex = new Regex(pattern);
            return regex.Match(name).Success;
        }

        public bool isExpired(StoreView snapshot, UInt256 nameHash)
        {
            var domainInfo = GetDomainInfo(snapshot, nameHash);
            return TimeProvider.Current.UtcNow.ToTimestampMS() - domainInfo.TimeToLive > 0;
        }
    }
}
