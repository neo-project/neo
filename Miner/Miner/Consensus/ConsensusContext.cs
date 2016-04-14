using AntShares.Core;
using AntShares.Cryptography;
using AntShares.Cryptography.ECC;
using AntShares.IO;
using AntShares.Network.Payloads;
using AntShares.Wallets;
using System.Collections.Generic;
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
        public int MinerIndex;
        public uint PrimaryIndex;
        public uint Timestamp;
        public ulong Nonce;
        public UInt256[] TransactionHashes;
        public Dictionary<UInt256, Transaction> Transactions;
        public byte[][] Signatures;
        public byte[] ExpectedView;

        public int M => Miners.Length - (Miners.Length - 1) / 3;

        public void ChangeView(byte view_number)
        {
            int p = ((int)Height - view_number) % Miners.Length;
            State &= ConsensusState.SignatureSent;
            ViewNumber = view_number;
            PrimaryIndex = p >= 0 ? (uint)p : (uint)(p + Miners.Length);
            if (State == ConsensusState.Initial)
            {
                TransactionHashes = null;
                Signatures = new byte[Miners.Length][];
            }
            _header = null;
        }

        public ConsensusPayload MakeChangeView()
        {
            return MakePayload(new ChangeView
            {
                NewViewNumber = ExpectedView[MinerIndex]
            });
        }

        private Block _header = null;
        public Block MakeHeader()
        {
            if (TransactionHashes == null) return null;
            if (_header == null)
            {
                _header = new Block
                {
                    Version = Version,
                    PrevBlock = PrevHash,
                    MerkleRoot = MerkleTree.ComputeRoot(TransactionHashes),
                    Timestamp = Timestamp,
                    Height = Height,
                    Nonce = Nonce,
                    NextMiner = Blockchain.GetMinerAddress(Blockchain.Default.GetMiners(Transactions.Values).ToArray()),
                    Transactions = new Transaction[0]
                };
            }
            return _header;
        }

        private ConsensusPayload MakePayload(ConsensusMessage message)
        {
            message.ViewNumber = ViewNumber;
            return new ConsensusPayload
            {
                Version = Version,
                PrevHash = PrevHash,
                Height = Height,
                MinerIndex = (ushort)MinerIndex,
                Timestamp = Timestamp,
                Data = message.ToArray()
            };
        }

        public ConsensusPayload MakePerpareRequest()
        {
            return MakePayload(new PerpareRequest
            {
                Nonce = Nonce,
                TransactionHashes = TransactionHashes,
                MinerTransaction = (MinerTransaction)Transactions[TransactionHashes[0]],
                Signature = Signatures[MinerIndex]
            });
        }

        public ConsensusPayload MakePerpareResponse(byte[] signature)
        {
            return MakePayload(new PerpareResponse
            {
                Signature = signature
            });
        }

        public void Reset(Wallet wallet)
        {
            State = ConsensusState.Initial;
            PrevHash = Blockchain.Default.CurrentBlockHash;
            Height = Blockchain.Default.Height + 1;
            ViewNumber = 0;
            Miners = Blockchain.Default.GetMiners();
            MinerIndex = -1;
            PrimaryIndex = Height % (uint)Miners.Length;
            TransactionHashes = null;
            Signatures = new byte[Miners.Length][];
            ExpectedView = new byte[Miners.Length];
            for (int i = 0; i < Miners.Length; i++)
            {
                if (wallet.ContainsAccount(Miners[i]))
                {
                    MinerIndex = i;
                    break;
                }
            }
            _header = null;
        }
    }
}
