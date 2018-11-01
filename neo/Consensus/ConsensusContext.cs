using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Consensus
{
    internal class ConsensusContext : IDisposable
    {
        public const uint Version = 0;
        public ConsensusState State;
        //public UInt256 PrevHash;
        public uint BlockIndex;
        public byte ViewNumber;
        public Snapshot Snapshot;
        public ECPoint[] Validators;
        //public uint Timestamp;
        //public ulong Nonce;
        //public UInt160 NextConsensus;
        public UInt256[] TransactionHashes;
        public Dictionary<UInt256, Transaction> Transactions;
        public byte[][] Signatures;
        public byte[] ExpectedView;
        public int MyIndex;
        public KeyPair KeyPair;

        public int M => Validators.Length - (Validators.Length - 1) / 3;

        public void ChangeView(byte view_number)
        {
            State &= ConsensusState.SignatureSent;
            ViewNumber = view_number;
            if (State == ConsensusState.Initial)
            {
                TransactionHashes = null;
                Signatures = new byte[Validators.Length][];
            }
            if (MyIndex >= 0)
                ExpectedView[MyIndex] = view_number;
        }

        public uint GetPrimaryIndex(byte view_number)
        {
            int p = ((int)BlockIndex - view_number) % Validators.Length;
            return p >= 0 ? (uint)p : (uint)(p + Validators.Length);
        }

        public ConsensusPayload MakeChangeView()
        {
            return MakePayload(new ChangeView
            {
                NewViewNumber = ExpectedView[MyIndex]
            });
        }

        public Block _header = null;
        public Block MakeHeader()
        {
            if (TransactionHashes == null) return null;
            if (_header == null) return null;
            _header.Index = BlockIndex;
            _header.MerkleRoot = MerkleTree.ComputeRoot(TransactionHashes);
            _header.Transactions = new Transaction[0];
            return _header;
        }

        public ConsensusPayload MakePayload(ConsensusMessage message)
        {
            message.ViewNumber = ViewNumber;
            return new ConsensusPayload
            {
                Version = _header.Version,
                PrevHash = _header.PrevHash,
                BlockIndex = BlockIndex,
                ValidatorIndex = (ushort)MyIndex,
                Timestamp = _header.Timestamp,
                Data = message.ToArray()
            };
        }

        public ConsensusPayload MakePrepareResponse(byte[] signature)
        {
            return MakePayload(new PrepareResponse
            {
                Signature = signature
            });
        }

        public void Reset(Wallet wallet)
        {
            Snapshot?.Dispose();
            Snapshot = Blockchain.Singleton.GetSnapshot();
            Validators = Snapshot.GetValidators();
            State = ConsensusState.Initial;
            BlockIndex = Snapshot.Height + 1;
            ViewNumber = 0;
            TransactionHashes = null;
            _header = new Block
                          {
                              Version = Version,
                              PrevHash = Snapshot.CurrentBlockHash
                          };
            Signatures = new byte[Validators.Length][];
            ExpectedView = new byte[Validators.Length];
            MyIndex = -1;
            KeyPair = null;
            for (int i = 0; i < Validators.Length; i++)
            {
                WalletAccount account = wallet.GetAccount(Validators[i]);
                if (account?.HasKey == true)
                {
                    MyIndex = i;
                    KeyPair = account.GetKey();
                    break;
                }
            }
        }

        public void Dispose()
        {
            Snapshot?.Dispose();
        }
    }
}
