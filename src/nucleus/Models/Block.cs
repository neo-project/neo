namespace Neo.Models
{
    public class Block
    {
        public BlockHeader Header;
        public ConsensusData ConsensusData;
        public Transaction[] Transactions;

        public uint Version => Header.Version;
        public UInt256 PrevHash => Header.PrevHash;
        public UInt256 MerkleRoot => Header.MerkleRoot;
        public ulong Timestamp => Header.Timestamp;
        public uint Index => Header.Index;
        public UInt160 NextConsensus => Header.NextConsensus;
        public Witness Witness => Header.Witness;
    }
}
