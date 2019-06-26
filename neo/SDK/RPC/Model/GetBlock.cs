using Neo.IO.Json;
using Neo.Network.P2P.Payloads;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Neo.SDK.RPC.Model
{
    public class GetBlock
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
        public SDK_Witness Witness { get; set; }

        [JsonProperty(PropertyName = "consensus_data")]
        public ConsensusData ConsensusData { get; set; }

        [JsonProperty(PropertyName = "tx")]
        public SDK_Transaction[] Tx { get; set; }

        [JsonProperty(PropertyName = "confirmations")]
        public int Confirmations { get; set; }

        [JsonProperty(PropertyName = "nextblockhash")]
        public string NextBlockHash { get; set; }

    }


    public class ConsensusData
    {
        [JsonProperty(PropertyName = "primary")]
        public string Primary { get; set; }

        [JsonProperty(PropertyName = "nonce")]
        public string Nonce { get; set; }
    }



}
