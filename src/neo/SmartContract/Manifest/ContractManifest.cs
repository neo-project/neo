using Neo.IO;
using Neo.IO.Json;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Linq;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Manifest
{
    /// <summary>
    /// When a smart contract is deployed, it must explicitly declare the features and permissions it will use.
    /// When it is running, it will be limited by its declared list of features and permissions, and cannot make any behavior beyond the scope of the list.
    /// </summary>
    public class ContractManifest : IInteroperable
    {
        /// <summary>
        /// Max length for a valid Contract Manifest
        /// </summary>
        public const int MaxLength = ushort.MaxValue;

        /// <summary>
        /// Contract name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A group represents a set of mutually trusted contracts. A contract will trust and allow any contract in the same group to invoke it, and the user interface will not give any warnings.
        /// </summary>
        public ContractGroup[] Groups { get; set; }

        /// <summary>
        /// NEP10 - SupportedStandards
        /// </summary>
        public string[] SupportedStandards { get; set; }

        /// <summary>
        /// For technical details of ABI, please refer to NEP-3: NeoContract ABI. (https://github.com/neo-project/proposals/blob/master/nep-3.mediawiki)
        /// </summary>
        public ContractAbi Abi { get; set; }

        /// <summary>
        /// The permissions field is an array containing a set of Permission objects. It describes which contracts may be invoked and which methods are called.
        /// </summary>
        public ContractPermission[] Permissions { get; set; }

        /// <summary>
        /// The trusts field is an array containing a set of contract hashes or group public keys. It can also be assigned with a wildcard *. If it is a wildcard *, then it means that it trusts any contract.
        /// If a contract is trusted, the user interface will not give any warnings when called by the contract.
        /// </summary>
        public WildcardContainer<UInt160> Trusts { get; set; }

        /// <summary>
        /// Custom user data
        /// </summary>
        public JObject Extra { get; set; }

        void IInteroperable.FromStackItem(StackItem stackItem)
        {
            Struct @struct = (Struct)stackItem;
            Name = @struct[0].GetString();
            Groups = ((Array)@struct[1]).Select(p => p.ToInteroperable<ContractGroup>()).ToArray();
            SupportedStandards = ((Array)@struct[2]).Select(p => p.GetString()).ToArray();
            Abi = @struct[3].ToInteroperable<ContractAbi>();
            Permissions = ((Array)@struct[4]).Select(p => p.ToInteroperable<ContractPermission>()).ToArray();
            Trusts = @struct[5] switch
            {
                Null => WildcardContainer<UInt160>.CreateWildcard(),
                Array array => WildcardContainer<UInt160>.Create(array.Select(p => new UInt160(p.GetSpan())).ToArray()),
                _ => throw new ArgumentException(null, nameof(stackItem))
            };
            Extra = JObject.Parse(@struct[6].GetSpan());
        }

        public StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Struct(referenceCounter)
            {
                Name,
                new Array(referenceCounter, Groups.Select(p => p.ToStackItem(referenceCounter))),
                new Array(referenceCounter, SupportedStandards.Select(p => (StackItem)p)),
                Abi.ToStackItem(referenceCounter),
                new Array(referenceCounter, Permissions.Select(p => p.ToStackItem(referenceCounter))),
                Trusts.IsWildcard ? StackItem.Null : new Array(referenceCounter, Trusts.Select(p => (StackItem)p.ToArray())),
                Extra is null ? "null" : Extra.ToByteArray(false)
            };
        }

        /// <summary>
        /// Parse ContractManifest from json
        /// </summary>
        /// <param name="json">Json</param>
        /// <returns>Return ContractManifest</returns>
        public static ContractManifest FromJson(JObject json)
        {
            ContractManifest manifest = new ContractManifest
            {
                Name = json["name"].GetString(),
                Groups = ((JArray)json["groups"]).Select(u => ContractGroup.FromJson(u)).ToArray(),
                SupportedStandards = ((JArray)json["supportedstandards"]).Select(u => u.GetString()).ToArray(),
                Abi = ContractAbi.FromJson(json["abi"]),
                Permissions = ((JArray)json["permissions"]).Select(u => ContractPermission.FromJson(u)).ToArray(),
                Trusts = WildcardContainer<UInt160>.FromJson(json["trusts"], u => UInt160.Parse(u.GetString())),
                Extra = json["extra"]
            };
            if (string.IsNullOrEmpty(manifest.Name))
                throw new FormatException();
            _ = manifest.Groups.ToDictionary(p => p.PubKey);
            if (manifest.SupportedStandards.Any(p => string.IsNullOrEmpty(p)))
                throw new FormatException();
            _ = manifest.SupportedStandards.ToDictionary(p => p);
            _ = manifest.Permissions.ToDictionary(p => p.Contract);
            _ = manifest.Trusts.ToDictionary(p => p);
            return manifest;
        }

        /// <summary>
        /// Parse ContractManifest from json
        /// </summary>
        /// <param name="json">Json</param>
        /// <returns>Return ContractManifest</returns>
        public static ContractManifest Parse(ReadOnlySpan<byte> json)
        {
            if (json.Length > MaxLength) throw new ArgumentException(null, nameof(json));
            return FromJson(JObject.Parse(json));
        }

        public static ContractManifest Parse(string json) => Parse(Utility.StrictUTF8.GetBytes(json));

        /// <summary
        /// To json
        /// </summary>
        public JObject ToJson()
        {
            return new JObject
            {
                ["name"] = Name,
                ["groups"] = Groups.Select(u => u.ToJson()).ToArray(),
                ["supportedstandards"] = SupportedStandards.Select(u => new JString(u)).ToArray(),
                ["abi"] = Abi.ToJson(),
                ["permissions"] = Permissions.Select(p => p.ToJson()).ToArray(),
                ["trusts"] = Trusts.ToJson(),
                ["extra"] = Extra
            };
        }

        /// <summary>
        /// Return true if is valid
        /// </summary>
        /// <returns>Return true or false</returns>
        public bool IsValid(UInt160 hash)
        {
            return Groups.All(u => u.IsValid(hash));
        }
    }
}
