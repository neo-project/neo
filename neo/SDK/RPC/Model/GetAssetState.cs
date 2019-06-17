using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.SDK.RPC.Model
{
    public class GetAssetState
    {
        [JsonProperty(PropertyName = "version")]
        public int Version { get; set; }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "name")]
        public AssetName[] Name { get; set; }

        [JsonProperty(PropertyName = "amount")]
        public string Amount { get; set; }

        [JsonProperty(PropertyName = "available")]
        public string Available { get; set; }

        [JsonProperty(PropertyName = "precision")]
        public int Precision { get; set; }

        [JsonProperty(PropertyName = "owner")]
        public string Owner { get; set; }

        [JsonProperty(PropertyName = "admin")]
        public string Admin { get; set; }

        [JsonProperty(PropertyName = "issuer")]
        public string Issuer { get; set; }

        [JsonProperty(PropertyName = "expiration")]
        public int Expiration { get; set; }

        [JsonProperty(PropertyName = "frozen")]
        public bool Frozen { get; set; }

    }

    public class AssetName
    {
        [JsonProperty(PropertyName = "lang")]
        public string Lang { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

    }
}
