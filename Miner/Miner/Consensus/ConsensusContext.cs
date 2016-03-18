using AntShares.Core;
using AntShares.Cryptography.ECC;

namespace AntShares.Miner.Consensus
{
    internal class ConsensusContext
    {
        public const uint Version = 0;
        public ConsensusState State;
        public UInt256 PrevHash;
        public uint Height;
        public byte ViewNumber;
        public ECPoint[] Miners;
        public uint Timestamp;
        public ulong Nonce;
        public MinerTransaction MinerTransaction;
        public UInt256[] TransactionHashes;
        public byte[][] Signatures;

        public void Reset()
        {
            State = ConsensusState.Initial;
            PrevHash = Blockchain.Default.CurrentBlockHash;
            Height = Blockchain.Default.Height + 1;
            ViewNumber = 0;
            Miners = Blockchain.Default.GetMiners();
            Signatures = new byte[Miners.Length][];
        }
    }
}
