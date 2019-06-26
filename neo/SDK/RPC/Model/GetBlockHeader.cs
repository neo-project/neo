using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.SDK.RPC.Model
{
    public class GetBlockHeader
    {
        [JsonProperty(PropertyName = "hash")]
        public string Hash { get; set; }

        [JsonProperty(PropertyName = "size")]
        public uint Size { get; set; }

        [JsonProperty(PropertyName = "version")]
        public uint Version { get; set; }

        [JsonProperty(PropertyName = "previousblockhash")]
        public string PreviousBlockHash { get; set; }

        [JsonProperty(PropertyName = "merkleroot")]
        public string MerkleRoot { get; set; }

        [JsonProperty(PropertyName = "time")]
        public uint Time { get; set; }

        [JsonProperty(PropertyName = "index")]
        public uint Index { get; set; }

        [JsonProperty(PropertyName = "nextconsensus")]
        public string NextConsensus { get; set; }

        [JsonProperty(PropertyName = "witness")]
        public WitnessJson Witness { get; set; }

        [JsonProperty(PropertyName = "confirmations")]
        public int Confirmations { get; set; }

        [JsonProperty(PropertyName = "nextblockhash")]
        public string NextBlockHash { get; set; }

    }
}
