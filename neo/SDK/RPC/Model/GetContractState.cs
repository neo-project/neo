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
        public Manifest Manifest { get; set; }

    }

    public class Manifest
    {
        [JsonProperty(PropertyName = "groups")]
        public ContractGroup[] Groups { get; set; }

        [JsonProperty(PropertyName = "features")]
        public object[] Features { get; set; }

        [JsonProperty(PropertyName = "abi")]
        public object[] Abi { get; set; }

        [JsonProperty(PropertyName = "permissions")]
        public object[] Permissions { get; set; }

        [JsonProperty(PropertyName = "trusts")]
        public object[] Trusts { get; set; }

        [JsonProperty(PropertyName = "safeMethods")]
        public object[] SafeMethods { get; set; }

    }

    public class ContractGroup
    {
        [JsonProperty(PropertyName = "pubKey")]
        public string PubKey { get; set; }

        [JsonProperty(PropertyName = "signature")]
        public string Signature { get; set; }

    }

}
