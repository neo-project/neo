using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Wallets;
using Neo.Plugins;
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

        public void Dispose()
        {
            Snapshot?.Dispose();
        }

        public uint GetPrimaryIndex(byte view_number)
        {
            int p = ((int)BlockIndex - view_number) % Validators.Length;
            return p >= 0 ? (uint)p : (uint)(p + Validators.Length);
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

        public ulong GetNonce()
        {
            byte[] nonce = new byte[sizeof(ulong)];
            Random rand = new Random();
            rand.NextBytes(nonce);
            return nonce.ToUInt64(0);
        }

        public uint GetLimitedTimestamp()
        {
            return Math.Max(DateTime.UtcNow.ToTimestamp(), Snapshot.GetHeader(_header.PrevHash).Timestamp + 1);
        }

        public uint GetTimestamp()
        {
            return Snapshot.GetHeader(_header.PrevHash).Timestamp;
        }

        public uint GetMaxTimestamp()
        {
            return DateTime.UtcNow.AddMinutes(10).ToTimestamp();
        }

        public void Fill(Wallet wallet)
        {
            IEnumerable<Transaction> mem_pool = Blockchain.Singleton.GetMemoryPool();
            foreach (IPolicyPlugin plugin in Plugin.Policies)
                mem_pool = plugin.FilterForBlock(mem_pool);
            List<Transaction> transactions = mem_pool.ToList();
            Fixed8 amount_netfee = Block.CalculateNetFee(transactions);
            TransactionOutput[] outputs = amount_netfee == Fixed8.Zero ? new TransactionOutput[0] : new[] { new TransactionOutput
            {
                AssetId = Blockchain.UtilityToken.Hash,
                Value = amount_netfee,
                ScriptHash = wallet.GetChangeAddress()
            } };
            while (true)
            {
                ulong nonce = GetNonce();
                MinerTransaction tx = new MinerTransaction
                {
                    Nonce = (uint)(nonce % (uint.MaxValue + 1ul)),
                    Attributes = new TransactionAttribute[0],
                    Inputs = new CoinReference[0],
                    Outputs = outputs,
                    Witnesses = new Witness[0]
                };
                if (!Snapshot.ContainsTransaction(tx.Hash))
                {
                    _header.ConsensusData = nonce;
                    transactions.Insert(0, tx);
                    break;
                }
            }
            TransactionHashes = transactions.Select(p => p.Hash).ToArray();
            Transactions = transactions.ToDictionary(p => p.Hash);
            _header.NextConsensus = Blockchain.GetConsensusAddress(Snapshot.GetValidators(transactions).ToArray());
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
    }
}
