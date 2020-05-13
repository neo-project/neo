using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System.Text.RegularExpressions;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.NNS
{
    partial class NnsContract
    {
        private static readonly uint MaxResolveCount = 7;

        // only can be called by the admin of the name
        [ContractMethod(0_03000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.ByteArray, ContractParameterType.String, ContractParameterType.Integer }, ParameterNames = new[] { "name", "text", "recordType" })]
        private StackItem SetText(ApplicationEngine engine, Array args)
        {
            byte[] tokenId = args[0].GetSpan().ToArray();
            string name = System.Text.Encoding.UTF8.GetString(tokenId);
            UInt256 innerKey = GetInnerKey(tokenId);
            DomainState domainInfo = GetDomainInfo(engine.Snapshot, innerKey);
            if (domainInfo is null) return false;
            if (IsExpired(engine.Snapshot, innerKey)) return false;
            string text = args[1].GetString();
            RecordType recordType = (RecordType)(byte)args[2].GetBigInteger();
            if ((recordType == RecordType.A || recordType == RecordType.CNAME) && !IsDomain(name)) return false;
            if (!InteropService.Runtime.CheckWitnessInternal(engine, domainInfo.Operator)) return false;
            StorageKey key = CreateStorageKey(Prefix_Record, innerKey);
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(key, () => new StorageItem(new RecordInfo { Text = text, Type = recordType }));
            RecordInfo recordInfo = storage.GetInteroperable<RecordInfo>();
            recordInfo.Text = text;
            recordInfo.Type = recordType;
            return true;
        }

        // return the text and recordtype of the name
        [ContractMethod(0_03000000, ContractParameterType.String, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.ByteArray }, ParameterNames = new[] { "name" })]
        public StackItem Resolve(ApplicationEngine engine, Array args)
        {
            byte[] tokenId = args[0].GetSpan().ToArray();
            string name = System.Text.Encoding.UTF8.GetString(tokenId);
            return Resolve(engine.Snapshot, name);
        }

        public string Resolve(StoreView snapshot, string name, int resolveCount = 0)
        {
            if (resolveCount++ > MaxResolveCount)
            {
                return new RecordInfo { Text = "The count of domain redirection exceeds 100 times", Type = RecordType.ERROR }.ToString();
            }
            UInt256 innerKey = ComputeNameHash(name);
            if (IsExpired(snapshot, innerKey))
            {
                return new RecordInfo { Text = "TTL is expired", Type = RecordType.ERROR }.ToString();
            }
            StorageKey key = CreateStorageKey(Prefix_Record, innerKey);
            StorageItem storage = snapshot.Storages.TryGet(key);
            if (storage is null)
            {
                return new RecordInfo { Text = "Text does not exist", Type = RecordType.ERROR }.ToString();
            }
            RecordInfo recordInfo = storage.GetInteroperable<RecordInfo>();
            switch (recordInfo.Type)
            {
                case RecordType.CNAME:
                    return Resolve(snapshot, recordInfo.Text, resolveCount);
                case RecordType.NS:
                    return Resolve(snapshot, string.Join(".", name.Split(".")[1..]), resolveCount);
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

        public bool IsExpired(StoreView snapshot, UInt256 innerKey)
        {
            if (snapshot.Storages.TryGet(CreateStorageKey(Prefix_Root, innerKey)) != null) return false;
            var domainInfo = GetDomainInfo(snapshot, innerKey);
            if (domainInfo is null) return false;
            return snapshot.Height.CompareTo(domainInfo.TimeToLive) > 0;
        }
    }
}
