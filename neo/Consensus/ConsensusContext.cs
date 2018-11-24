using Akka.Actor;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins;
using Neo.SmartContract;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Consensus
{
    internal class ConsensusContext : IConsensusContext
    {
        public const uint Version = 0;
        private DateTime _block_received_time;
        public DateTime block_received_time
        {
            get => _block_received_time;
            set => _block_received_time = value;
        }

        private ConsensusState _State;
        public ConsensusState State
        {
            get => _State;
            set => _State = value;
        }
        private UInt256 _PrevHash;
        public UInt256 PrevHash
        {
            get => _PrevHash;
            set => _PrevHash = value;
        }
        private uint _BlockIndex;
        public uint BlockIndex
        {
            get => _BlockIndex;
            set => _BlockIndex = value;
        }
        private byte _ViewNumber;
        public byte ViewNumber
        {
            get => _ViewNumber;
            set => _ViewNumber = value;
        }
        private Snapshot Snapshot;
        private ECPoint[] _Validators;
        public ECPoint[] Validators
        {
            get => _Validators;
            set => _Validators = value;
        }
        private int _MyIndex;
        public int MyIndex
        {
            get => _MyIndex;
            set => _MyIndex = value;
        }
        private uint _PrimaryIndex;
        public uint PrimaryIndex
        {
            get => _PrimaryIndex;
            set => _PrimaryIndex = value;
        }
        private uint _Timestamp;
        public uint Timestamp
        {
            get => _Timestamp;
            set => _Timestamp = value;
        }
        private ulong _Nonce;
        public ulong Nonce
        {
            get => _Nonce;
            set => _Nonce = value;
        }
        private UInt160 _NextConsensus;
        public UInt160 NextConsensus
        {
            get => _NextConsensus;
            set => _NextConsensus = value;
        }
        private UInt256[] _TransactionHashes;
        public UInt256[] TransactionHashes
        {
            get => _TransactionHashes;
            set => _TransactionHashes = value;
        }
        private Dictionary<UInt256, Transaction> _Transactions;
        public Dictionary<UInt256, Transaction> Transactions
        {
            get => _Transactions;
            set => _Transactions = value;
        }
        private byte[][] _Signatures;
        public byte[][] Signatures
        {
            get => _Signatures;
            set => _Signatures = value;
        }
        private byte[] _ExpectedView;
        public byte[] ExpectedView
        {
            get => _ExpectedView;
            set => _ExpectedView = value;
        }
        private KeyPair KeyPair;
        private readonly Wallet wallet;

        public int M => Validators.Length - (Validators.Length - 1) / 3;

        public ConsensusContext(Wallet wallet)
        {
            this.wallet = wallet;
        }

        public uint SnapshotHeight => Snapshot.Height;

        public Header SnapshotHeader => Snapshot.GetHeader(PrevHash);

        public bool RejectTx(Transaction tx, bool verify)
        {
            return Snapshot.ContainsTransaction(tx.Hash) ||
              (verify && !tx.Verify(Snapshot, Transactions.Values)) ||
              !Plugin.CheckPolicy(tx);
        }

        public void ChangeView(byte view_number)
        {
            State &= ConsensusState.SignatureSent;
            ViewNumber = view_number;
            PrimaryIndex = GetPrimaryIndex(view_number);
            if (State == ConsensusState.Initial)
            {
                TransactionHashes = null;
                Signatures = new byte[Validators.Length][];
            }
            if (MyIndex >= 0)
                ExpectedView[MyIndex] = view_number;
            _header = null;
        }

        public Block CreateBlock()
        {
            Block block = MakeHeader();
            if (block == null) return null;
            Contract contract = Contract.CreateMultiSigContract(M, Validators);
            ContractParametersContext sc = new ContractParametersContext(block);
            for (int i = 0, j = 0; i < Validators.Length && j < M; i++)
                if (Signatures[i] != null)
                {
                    sc.AddSignature(contract, Validators[i], Signatures[i]);
                    j++;
                }
            sc.Verifiable.Witnesses = sc.GetWitnesses();
            block.Transactions = TransactionHashes.Select(p => Transactions[p]).ToArray();
            return block;
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

        public ConsensusPayload MakeChangeView()
        {
            return MakeSignedPayload(new ChangeView
            {
                NewViewNumber = ExpectedView[MyIndex]
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
                    PrevHash = PrevHash,
                    MerkleRoot = MerkleTree.ComputeRoot(TransactionHashes),
                    Timestamp = Timestamp,
                    Index = BlockIndex,
                    ConsensusData = Nonce,
                    NextConsensus = NextConsensus,
                    Transactions = new Transaction[0]
                };
            }
            return _header;
        }

        private ConsensusPayload MakeSignedPayload(ConsensusMessage message)
        {
            message.ViewNumber = ViewNumber;
            ConsensusPayload payload = new ConsensusPayload
            {
                Version = Version,
                PrevHash = PrevHash,
                BlockIndex = BlockIndex,
                ValidatorIndex = (ushort)MyIndex,
                Timestamp = Timestamp,
                Data = message.ToArray()
            };
            SignPayload(payload);
            return payload;
        }

        public void SignHeader()
        {
            Signatures[MyIndex] = MakeHeader()?.Sign(KeyPair);
        }

        private void SignPayload(ConsensusPayload payload)
        {
            ContractParametersContext sc;
            try
            {
                sc = new ContractParametersContext(payload);
                wallet.Sign(sc);
            }
            catch (InvalidOperationException)
            {
                return;
            }
            sc.Verifiable.Witnesses = sc.GetWitnesses();
        }

        public ConsensusPayload MakePrepareRequest()
        {
            return MakeSignedPayload(new PrepareRequest
            {
                Nonce = Nonce,
                NextConsensus = NextConsensus,
                TransactionHashes = TransactionHashes,
                MinerTransaction = (MinerTransaction)Transactions[TransactionHashes[0]],
                Signature = Signatures[MyIndex]
            });
        }

        public ConsensusPayload MakePrepareResponse(byte[] signature)
        {
            return MakeSignedPayload(new PrepareResponse
            {
                Signature = signature
            });
        }

        public void Reset()
        {
            Snapshot?.Dispose();
            Snapshot = Blockchain.Singleton.GetSnapshot();
            State = ConsensusState.Initial;
            PrevHash = Snapshot.CurrentBlockHash;
            BlockIndex = Snapshot.Height + 1;
            ViewNumber = 0;
            Validators = Snapshot.GetValidators();
            MyIndex = -1;
            PrimaryIndex = BlockIndex % (uint)Validators.Length;
            TransactionHashes = null;
            Signatures = new byte[Validators.Length][];
            ExpectedView = new byte[Validators.Length];
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
            _header = null;
        }

        public void Fill()
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
                    Nonce = nonce;
                    transactions.Insert(0, tx);
                    break;
                }
            }
            TransactionHashes = transactions.Select(p => p.Hash).ToArray();
            Transactions = transactions.ToDictionary(p => p.Hash);
            NextConsensus = Blockchain.GetConsensusAddress(Snapshot.GetValidators(transactions).ToArray());
            Timestamp = Math.Max(GetUtcNow().ToTimestamp(), Snapshot.GetHeader(PrevHash).Timestamp + 1);
        }

        private static ulong GetNonce()
        {
            byte[] nonce = new byte[sizeof(ulong)];
            Random rand = new Random();
            rand.NextBytes(nonce);
            return nonce.ToUInt64(0);
        }

        public bool VerifyRequest()
        {
            if (!State.HasFlag(ConsensusState.RequestReceived))
                return false;
            if (!Blockchain.GetConsensusAddress(Snapshot.GetValidators(Transactions.Values).ToArray()).Equals(NextConsensus))
                return false;
            Transaction tx_gen = Transactions.Values.FirstOrDefault(p => p.Type == TransactionType.MinerTransaction);
            Fixed8 amount_netfee = Block.CalculateNetFee(Transactions.Values);
            if (tx_gen?.Outputs.Sum(p => p.Value) != amount_netfee) return false;
            return true;
        }

        public DateTime GetUtcNow()
        {
            return DateTime.UtcNow;
        }
    }
}
