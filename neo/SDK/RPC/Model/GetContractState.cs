using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.SDK.RPC.Model
{
    public class GetContractState
    {
        [JsonProperty(PropertyName = "hash")]
        public string Hash { get; set; }

        [JsonProperty(PropertyName = "script")]
        public string Script { get; set; }

        [JsonProperty(PropertyName = "manifest")]
        public ManifestJson Manifest { get; set; }

    }

    public class ManifestJson
    {
        [JsonProperty(PropertyName = "groups")]
        public ContractGroupJson[] Groups { get; set; }

        [JsonProperty(PropertyName = "features")]
        public FeaturesJson Features { get; set; }

        [JsonProperty(PropertyName = "abi")]
        public AbiJson Abi { get; set; }

        [JsonProperty(PropertyName = "permissions")]
        public PermissionJson[] Permissions { get; set; }

        // WildCardContainer<T>, "*" or string[]
        public object Trusts { get; set; }

        // WildCardContainer<T>, "*" or string[]
        [JsonProperty(PropertyName = "safeMethods")]
        public object SafeMethods { get; set; }

    }

    public class ContractGroupJson
    {
        [JsonProperty(PropertyName = "pubKey")]
        public string PubKey { get; set; }

        [JsonProperty(PropertyName = "signature")]
        public string Signature { get; set; }
    }

    public class FeaturesJson
    {
        [JsonProperty(PropertyName = "storage")]
        public bool Storage { get; set; }

        [JsonProperty(PropertyName = "payable")]
        public bool Payable { get; set; }
    }

    public class AbiJson
    {
        [JsonProperty(PropertyName = "hash")]
        public string Hash { get; set; }

        [JsonProperty(PropertyName = "entryPoint")]
        public MethodDescriptorJson EntryPoint { get; set; }

        [JsonProperty(PropertyName = "methods")]
        public MethodDescriptorJson[] Methods { get; set; }

        [JsonProperty(PropertyName = "events")]
        public EventDescriptorJson[] Events { get; set; }
    }

    public class MethodDescriptorJson : EventDescriptorJson
    {
        [JsonProperty(PropertyName = "returnType")]
        public string ReturnType { get; set; }
    }

    public class EventDescriptorJson
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "parameters")]
        public ParameterDefinition[] Parameters { get; set; }

    }

    public class ParameterDefinition
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
    }

    public class PermissionJson
    {
        [JsonProperty(PropertyName = "contract")]
        public string Contract { get; set; }

        // WildCardContainer<T>, "*" or string[]
        [JsonProperty(PropertyName = "methods")]
        public object Methods { get; set; }
    }



}
