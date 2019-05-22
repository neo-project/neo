using Neo.IO;
using Neo.Ledger;
using Neo.SmartContract.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.IO;
using System.Linq;

namespace Neo.SmartContract
{
    /// <summary>
    /// When a smart contract is deployed, it must explicitly declare the features and permissions it will use.
    /// When it is running, it will be limited by its declared list of features and permissions, and cannot make any behavior beyond the scope of the list.
    /// </summary>
    public class ContractManifest : ISerializable
    {
        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.None,
            ContractResolver = new CamelCasePropertyNamesContractResolver() { }
        };

        /// <summary>
        /// Max length for a valid Contract Manifest
        /// </summary>
        public const int MaxLength = ushort.MaxValue;

        /// <summary>
        /// Contract hash
        /// </summary>
        [JsonConverter(typeof(Hash160JsonConverter))]
        public UInt160 Hash { get; set; }

        /// <summary>
        /// A group represents a set of mutually trusted contracts. A contract will trust and allow any contract in the same group to invoke it, and the user interface will not give any warnings.
        /// The group field can be null.
        /// </summary>
        public ContractManifestGroup[] Groups { get; set; }

        /// <summary>
        /// The features field describes what features are available for the contract.
        /// </summary>
        [JsonConverter(typeof(FeaturesConverter))]
        public ContractPropertyState Features { get; set; }

        /// <summary>
        /// For technical details of ABI, please refer to NEP-3: NeoContract ABI. (https://github.com/neo-project/proposals/blob/master/nep-3.mediawiki)
        /// </summary>
        public ContractAbi Abi { get; set; }

        /// <summary>
        /// The permissions field is an array containing a set of Permission objects. It describes which contracts may be invoked and which methods are called.
        /// </summary>
        [JsonConverter(typeof(WillCardJsonConverter<ContractPermission>))]
        public WildCardContainer<ContractPermission> Permissions { get; set; }

        /// <summary>
        /// The trusts field is an array containing a set of contract hashes or group public keys. It can also be assigned with a wildcard *. If it is a wildcard *, then it means that it trusts any contract.
        /// If a contract is trusted, the user interface will not give any warnings when called by the contract.
        /// </summary>
        [JsonConverter(typeof(WillCardJsonConverter<UInt160>))]
        public WildCardContainer<UInt160> Trusts { get; set; }

        /// <summary>
        /// The safemethods field is an array containing a set of method names. It can also be assigned with a wildcard *. If it is a wildcard *, then it means that all methods of the contract are safe.
        /// If a method is marked as safe, the user interface will not give any warnings when it is called by any other contract.
        /// </summary>
        [JsonConverter(typeof(WillCardJsonConverter<string>))]
        public WildCardContainer<string> SafeMethods { get; set; }

        /// <summary>
        /// Serialized size
        /// </summary>
        [JsonIgnore]
        public int Size => ToJson().GetVarSize();

        /// <summary>
        /// Create Default Contract manifest
        /// </summary>
        /// <param name="hash">Hash</param>
        /// <returns>Return default manifest for this contract</returns>
        public static ContractManifest CreateDefault(UInt160 hash)
        {
            return new ContractManifest()
            {
                Hash = hash,
                Permissions = WildCardContainer<ContractPermission>.CreateWildcard(),
                Abi = new ContractAbi()
                {
                    Hash = hash,
                    EntryPoint = new ContractMethodWithReturnDefinition()
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
                    Events = new ContractMethodDefinition[0],
                    Methods = new ContractMethodWithReturnDefinition[0]
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
        /// <returns>Return Contract manifest</returns>
        public static ContractManifest Parse(string json)
        {
            return JsonConvert.DeserializeObject<ContractManifest>(json, _jsonSettings);
        }

        /// <summary>
        /// To json
        /// </summary>
        /// <returns>Return json string</returns>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.None, _jsonSettings);
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
        public override string ToString() => ToJson();

        public void Serialize(BinaryWriter writer)
        {
            writer.WriteVarString(ToJson());
        }

        public void Deserialize(BinaryReader reader)
        {
            throw new NotImplementedException();
        }
    }
}