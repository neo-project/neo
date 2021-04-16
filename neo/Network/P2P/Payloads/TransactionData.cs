using Neo.IO.Json;

namespace Neo.Network.P2P.Payloads
{
    public class TransactionData
    {
        public UInt256 BlockHash { get; set; }
        
        public uint BlockIndex { get; set; }
        
        public ulong GlobalIndex { get; set; }
        
        public uint TransactionIndex { get; set; }

        public static TransactionData Create(UInt256 blockHash, uint blockIndex, ulong globalIndex, uint transactionIndex)
        {
            return new TransactionData
            {
                BlockHash = blockHash,
                BlockIndex = blockIndex,
                GlobalIndex = globalIndex,
                TransactionIndex = transactionIndex
            };
        }
        
        public virtual JObject ToJson()
        {
            return new JObject
            {
                ["blockHash"] = BlockHash.ToString(),
                ["blockIndex"] = BlockIndex,
                ["globalIndex"] = GlobalIndex,
                ["transactionIndex"] = TransactionIndex
            };
        }
    }
}