using AntShares.Core;
using AntShares.Cryptography;
using AntShares.Cryptography.ECC;
using AntShares.IO;
using AntShares.Network.Payloads;
using System.IO;
using System.Linq;

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
        public UInt256[] TransactionHashes;
        public Transaction[] Transactions;
        public byte[][] Signatures;

        public Block MakeHeader()
        {
            return new Block
            {
                Version = Version,
                PrevBlock = PrevHash,
                MerkleRoot = MerkleTree.ComputeRoot(TransactionHashes),
                Timestamp = Timestamp,
                Height = Height,
                Nonce = Nonce,
                NextMiner = Blockchain.GetMinerAddress(Blockchain.Default.GetMiners(Transactions).ToArray()),
                Transactions = new Transaction[0]
            };
        }

        public ConsensusPayload MakePerpareRequest(ushort miner_index)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter w = new BinaryWriter(ms))
            {
                w.Write((byte)ConsensusMessageType.PerpareRequest);
                w.Write(Nonce);
                w.Write(TransactionHashes);
                w.Write(Transactions[0]);
                w.Flush();
                return new ConsensusPayload
                {
                    Version = Version,
                    PrevHash = PrevHash,
                    Height = Height,
                    MinerIndex = miner_index,
                    Timestamp = Timestamp,
                    Data = ms.ToArray()
                };
            }
        }

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
