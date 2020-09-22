namespace Neo.Models
{
    public class BlockHeader
    {
        public uint Version;
        public UInt256 PrevHash;
        public UInt256 MerkleRoot;
        public ulong Timestamp;
        public uint Index;
        public UInt160 NextConsensus;
        public Witness Witness;
    }
}
