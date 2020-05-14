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
            string text = args[1].GetString();
            RecordType recordType = (RecordType)(byte)args[2].GetBigInteger();

            string name = System.Text.Encoding.UTF8.GetString(tokenId);
            UInt256 innerKey = GetInnerKey(tokenId);
            //Check whether domain is exist 
            DomainState domainInfo = GetDomainInfo(engine.Snapshot, innerKey);
            if (domainInfo is null) return false;
            //Check whether domain is expired 
            if (IsExpired(engine.Snapshot, innerKey)) return false;
            //Check whether record type is support 
            if ((recordType == RecordType.A || recordType == RecordType.CNAME) && !IsDomain(name)) return false;
            if (!InteropService.Runtime.CheckWitnessInternal(engine, domainInfo.Operator)) return false;
            //Modify text
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
            byte[] name = args[0].GetSpan().ToArray();
            return Resolve(engine.Snapshot, name);
        }

        public string Resolve(StoreView snapshot, byte[] parameter, int resolveCount = 0)
        {
            string name = System.Text.Encoding.UTF8.GetString(parameter);
            if (resolveCount++ > MaxResolveCount)
            {
                return new RecordInfo { Text = "Too many domain redirects", Type = RecordType.ERROR }.ToString();
            }
            UInt256 innerKey = GetInnerKey(parameter);
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
                    var parameter_cname = System.Text.Encoding.UTF8.GetBytes(recordInfo.Text);
                    return Resolve(snapshot, parameter_cname, resolveCount);
                case RecordType.NS:
                    var parameter_ns = System.Text.Encoding.UTF8.GetBytes(string.Join(".", name.Split(".")[1..]));
                    return Resolve(snapshot, parameter_ns, resolveCount);
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
    }
}
