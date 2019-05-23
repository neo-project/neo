using Neo.IO;
using Neo.IO.Json;
using System;
using System.IO;
using System.Linq;

namespace Neo.SmartContract.Manifest
{
    /// <summary>
    /// When a smart contract is deployed, it must explicitly declare the features and permissions it will use.
    /// When it is running, it will be limited by its declared list of features and permissions, and cannot make any behavior beyond the scope of the list.
    /// </summary>
    public class ContractManifest : ISerializable
    {
        /// <summary>
        /// Max length for a valid Contract Manifest
        /// </summary>
        public const int MaxLength = ushort.MaxValue;

        /// <summary>
        /// Serialized size
        /// </summary>
        public int Size => ToJson().ToString().GetVarSize();

        /// <summary>
        /// Contract hash
        /// </summary>
        public UInt160 Hash => Abi.Hash;

        /// <summary>
        /// A group represents a set of mutually trusted contracts. A contract will trust and allow any contract in the same group to invoke it, and the user interface will not give any warnings.
        /// </summary>
        public ContractGroup[] Groups { get; set; }

        /// <summary>
        /// The features field describes what features are available for the contract.
        /// </summary>
        public ContractFeatures Features { get; set; }

        /// <summary>
        /// For technical details of ABI, please refer to NEP-3: NeoContract ABI. (https://github.com/neo-project/proposals/blob/master/nep-3.mediawiki)
        /// </summary>
        public ContractAbi Abi { get; set; }

        /// <summary>
        /// The permissions field is an array containing a set of Permission objects. It describes which contracts may be invoked and which methods are called.
        /// </summary>
        public WildCardContainer<ContractPermission> Permissions { get; set; }

        /// <summary>
        /// The trusts field is an array containing a set of contract hashes or group public keys. It can also be assigned with a wildcard *. If it is a wildcard *, then it means that it trusts any contract.
        /// If a contract is trusted, the user interface will not give any warnings when called by the contract.
        /// </summary>
        public WildCardContainer<UInt160> Trusts { get; set; }

        /// <summary>
        /// The safemethods field is an array containing a set of method names. It can also be assigned with a wildcard *. If it is a wildcard *, then it means that all methods of the contract are safe.
        /// If a method is marked as safe, the user interface will not give any warnings when it is called by any other contract.
        /// </summary>
        public WildCardContainer<string> SafeMethods { get; set; }

        /// <summary>
        /// Create Default Contract manifest
        /// </summary>
        /// <param name="hash">Hash</param>
        /// <returns>Return default manifest for this contract</returns>
        public static ContractManifest CreateDefault(UInt160 hash)
        {
            return new ContractManifest()
            {
                Permissions = WildCardContainer<ContractPermission>.CreateWildcard(),
                Abi = new ContractAbi()
                {
                    Hash = hash,
                    EntryPoint = ContractMethodDescriptor.DefaultEntryPoint,
                    Events = new ContractEventDescriptor[0],
                    Methods = new ContractMethodDescriptor[0]
                },
                Features = ContractFeatures.NoProperty,
                Groups = new ContractGroup[0],
                SafeMethods = WildCardContainer<string>.Create(),
                Trusts = WildCardContainer<UInt160>.Create()
            };
        }

        /// <summary>
        /// Return true if is allowed
        /// </summary>
        /// <param name="manifest">Manifest</param>
        /// <param name="method">Method</param>
        /// <returns>Return true or false</returns>
        public bool CanCall(ContractManifest manifest, string method)
        {
            if (Groups != null && manifest.Groups != null && Groups.Any(a => manifest.Groups.Any(b => a.PubKey.Equals(b.PubKey))))
            {
                // Same group
                return true;
            }

            if (manifest.Trusts != null && !manifest.Trusts.IsWildcard && !manifest.Trusts.Contains(Hash))
            {
                // null == * wildcard
                // You don't have rights in the contract
                return false;
            }

            return Permissions == null || Permissions.IsWildcard || Permissions.Any(u => u.IsAllowed(manifest.Hash, method));
        }

        /// <summary>
        /// Parse ContractManifest from json
        /// </summary>
        /// <param name="json">Json</param>
        /// <returns>Return ContractManifest</returns>
        public static ContractManifest FromJson(JObject json)
        {
            var manifest = new ContractManifest
            {
                Abi = ContractAbi.FromJson(json["abi"]),
                Groups = ((JArray)json["groups"])?.Select(u => ContractGroup.FromJson(u)).ToArray() ?? new ContractGroup[0],
                Permissions = WildCardContainer<ContractPermission>.FromJson(json["permissions"], ContractPermission.FromJson) ?? WildCardContainer<ContractPermission>.CreateWildcard(),
                Trusts = WildCardContainer<UInt160>.FromJson(json["trusts"], u => UInt160.Parse(u.AsString())) ?? WildCardContainer<UInt160>.Create(),
                SafeMethods = WildCardContainer<string>.FromJson(json["safeMethods"], u => u.AsString()) ?? WildCardContainer<string>.Create()
            };

            if (json["features"]["storage"].AsBoolean()) manifest.Features |= ContractFeatures.HasStorage;
            if (json["features"]["payable"].AsBoolean()) manifest.Features |= ContractFeatures.Payable;

            return manifest;
        }

        /// <summary>
        /// Parse ContractManifest from json
        /// </summary>
        /// <param name="json">Json</param>
        /// <returns>Return ContractManifest</returns>
        public static ContractManifest Parse(string json) => FromJson(JObject.Parse(json));

        /// <summary
        /// To json
        /// </summary>
        public JObject ToJson()
        {
            var feature = new JObject();
            feature["storage"] = Features.HasFlag(ContractFeatures.HasStorage);
            feature["payable"] = Features.HasFlag(ContractFeatures.Payable);

            var json = new JObject();
            json["groups"] = new JArray(Groups.Select(u => u.ToJson()).ToArray());
            json["features"] = feature;
            json["abi"] = Abi.ToJson();
            json["permissions"] = Permissions.ToJson();
            json["trusts"] = Trusts.ToJson();
            json["safeMethods"] = SafeMethods.ToJson();

            return json;
        }

        /// <summary>
        /// Clone
        /// </summary>
        /// <returns>Return a copy of this object</returns>
        public ContractManifest Clone() => FromJson(ToJson());

        /// <summary>
        /// String representation
        /// </summary>
        /// <returns>Return json string</returns>
        public override string ToString() => ToJson().ToString();

        public void Serialize(BinaryWriter writer)
        {
            writer.WriteVarString(ToJson().ToString());
        }

        public void Deserialize(BinaryReader reader)
        {
            var manifest = Parse(reader.ReadVarString(MaxLength));

            Groups = manifest.Groups;
            Trusts = manifest.Trusts;
            Permissions = manifest.Permissions;
            SafeMethods = manifest.SafeMethods;
            Abi = manifest.Abi;
            Features = manifest.Features;
        }

        /// <summary>
        /// Return true if is valid
        /// </summary>
        /// <returns>Return true or false</returns>
        public bool IsValid()
        {
            return Abi != null && Trusts != null && SafeMethods != null && Permissions != null && (Groups == null || Groups.All(u => u.IsValid(Hash)));
        }
    }
}