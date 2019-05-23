using Neo.IO;
using Neo.IO.Json;
using Neo.Ledger;
using System;
using System.IO;
using System.Linq;

namespace Neo.SmartContract
{
    /// <summary>
    /// When a smart contract is deployed, it must explicitly declare the features and permissions it will use.
    /// When it is running, it will be limited by its declared list of features and permissions, and cannot make any behavior beyond the scope of the list.
    /// </summary>
    public class ContractManifest : ISerializable, IEquatable<ContractManifest>
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
        /// The group field can be null.
        /// </summary>
        public ContractManifestGroup[] Groups { get; set; }

        /// <summary>
        /// The features field describes what features are available for the contract.
        /// </summary>
        public ContractPropertyState Features { get; set; }

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
                    EntryPoint = new ContractMethodDescriptor()
                    {
                        Name = "Main",
                        Parameters = new ContractParameterDefinition[]
                        {
                            new ContractParameterDefinition()
                            {
                                 Name = "operation",
                                 Type = ContractParameterType.String
                            },
                            new ContractParameterDefinition()
                            {
                                 Name = "args",
                                 Type = ContractParameterType.Array
                            }
                        },
                        ReturnType = ContractParameterType.Array
                    },
                    Events = new ContractEventDescriptor[0],
                    Methods = new ContractMethodDescriptor[0]
                },
                Features = ContractPropertyState.NoProperty,
                Groups = null,
                SafeMethods = WildCardContainer<string>.CreateWildcard(),
                Trusts = WildCardContainer<UInt160>.CreateWildcard()
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
        public static ContractManifest Parse(string json) => Parse(JObject.Parse(json));

        /// <summary>
        /// Parse ContractManifest from json
        /// </summary>
        /// <param name="json">Json</param>
        /// <returns>Return ContractManifest</returns>
        public static ContractManifest Parse(JObject json)
        {
            var manifest = new ContractManifest
            {
                Abi = ContractAbi.Parse(json["abi"]),
                Groups = json.Properties.ContainsKey("groups") && json["groups"] != null ? ((JArray)json["groups"]).Select(u => ContractManifestGroup.Parse(u)).ToArray() : null,
                Permissions = new WildCardContainer<ContractPermission>(((JArray)json["permissions"]).Select(u => ContractPermission.Parse(u)).ToArray()),
                Trusts = new WildCardContainer<UInt160>(((JArray)json["trusts"]).Select(u => UInt160.Parse(u.AsString())).ToArray()),
                SafeMethods = new WildCardContainer<string>(((JArray)json["safeMethods"]).Select(u => u.AsString()).ToArray()),
            };

            if (json["features"]["storage"].AsBoolean()) manifest.Features |= ContractPropertyState.HasStorage;
            if (json["features"]["payable"].AsBoolean()) manifest.Features |= ContractPropertyState.Payable;

            return manifest;
        }

        /// <summary
        /// To json
        /// </summary>
        public JObject ToJson()
        {
            var feature = new JObject();
            feature["storage"] = Features.HasFlag(ContractPropertyState.HasStorage);
            feature["payable"] = Features.HasFlag(ContractPropertyState.Payable);

            var json = new JObject();
            json["groups"] = Groups == null ? null : new JArray(Groups.Select(u => u.ToJson()).ToArray());
            json["features"] = feature;
            json["abi"] = Abi.ToJson();
            json["permissions"] = new JArray(Permissions.Select(u => u.ToJson()).ToArray());
            json["trusts"] = new JArray(Trusts.Select(u => new JString(u.ToString())).ToArray());
            json["safeMethods"] = new JArray(SafeMethods.Select(u => new JString(u)).ToArray());

            return json;
        }

        /// <summary>
        /// Clone
        /// </summary>
        /// <returns>Return a copy of this object</returns>
        public ContractManifest Clone() => Parse(ToJson());

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

        public bool Equals(ContractManifest other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;

            if (!Hash.Equals(other.Hash)) return false;
            if ((Groups == null || other.Groups == null) && Groups != other.Groups) return false;
            if (Groups != null && !Groups.SequenceEqual(other.Groups)) return false;
            if (!Trusts.SequenceEqual(other.Trusts)) return false;
            if (!Permissions.SequenceEqual(other.Permissions)) return false;
            if (!SafeMethods.SequenceEqual(other.SafeMethods)) return false;
            if (!Abi.Equals(other.Abi)) return false;
            if (Features != other.Features) return false;

            return true;
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