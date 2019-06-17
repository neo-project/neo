using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.SDK.Wallet
{
    /// <summary>
    /// NEP-6 Wallet File Model
    /// </summary>
    public class WalletFile
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "version")]
        public string Version { get; set; }

        [JsonProperty(PropertyName = "scrypt")]
        public ScryptParams Scrypt { get; set; }

        [JsonProperty(PropertyName = "accounts")]
        public List<Account> Accounts { get; set; }

        [JsonProperty(PropertyName = "extra")]
        public object Extra { get; set; }


        public class ScryptParams
        {
            [JsonProperty(PropertyName = "n")]
            public int N { get; set; }

            [JsonProperty(PropertyName = "r")]
            public int R { get; set; }

            [JsonProperty(PropertyName = "p")]
            public int P { get; set; }
        }

        public class Account
        {
            [JsonProperty(PropertyName = "address")]
            public string Address { get; set; }

            [JsonProperty(PropertyName = "label")]
            public string Label { get; set; }

            [JsonProperty(PropertyName = "isDefault")]
            public bool IsDefault { get; set; }

            [JsonProperty(PropertyName = "lock")]
            public bool Lock { get; set; }

            [JsonProperty(PropertyName = "key")]
            public string Key { get; set; }

            [JsonProperty(PropertyName = "contract")]
            public Contract Contract { get; set; }

            [JsonProperty(PropertyName = "extra")]
            public string Extra { get; set; }

        }

        public class Contract
        {
            [JsonProperty(PropertyName = "script")]
            public string Script { get; set; }

            [JsonProperty(PropertyName = "parameters")]
            public ContractParameter[] Parameters { get; set; }

            [JsonProperty(PropertyName = "deployed")]
            public bool Deployed { get; set; }
        }

        public class ContractParameter
        {
            [JsonProperty(PropertyName = "type")]
            public string Type { get; set; }

            [JsonProperty(PropertyName = "name")]
            public string Name { get; set; }
        }
    }

}
