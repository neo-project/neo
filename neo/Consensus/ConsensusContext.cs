using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins;
using Neo.SmartContract;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Consensus
{
    internal class ConsensusContext : IConsensusContext
    {
        /// <summary>
        /// Prefix for saving consensus state.
        /// </summary>
        public const byte CN_Context = 0xf4;

        public const uint Version = 0;
        public uint BlockIndex { get; set; }
        public UInt256 PrevHash { get; set; }
        public byte ViewNumber { get; set; }
        public ECPoint[] Validators { get; set; }
        public int MyIndex { get; set; }
        public uint PrimaryIndex { get; set; }
        public uint Timestamp { get; set; }
        public ulong Nonce { get; set; }
        public UInt160 NextConsensus { get; set; }
        public UInt256[] TransactionHashes { get; set; }
        public Dictionary<UInt256, Transaction> Transactions { get; set; }
        public ConsensusPayload[] PreparationPayloads { get; set; }
        public ConsensusPayload[] CommitPayloads { get; set; }
        public ConsensusPayload[] ChangeViewPayloads { get; set; }
        public ConsensusPayload[] LastChangeViewPayloads { get; set; }
        // LastSeenMessage array stores the height of the last seen message, for each validator.
        // if this node never heard from validator i, LastSeenMessage[i] will be -1.
        public Dictionary<ECPoint, int> LastSeenMessage { get; set; }
        public Block Block { get; set; }
        public Snapshot Snapshot { get; private set; }
        private KeyPair keyPair;
        private readonly Wallet wallet;
        private readonly Store store;

        public int Size => throw new NotImplementedException();

        public ConsensusContext(Wallet wallet, Store store)
        {
            this.wallet = wallet;
            this.store = store;
        }

        public Block CreateBlock()
        {
            if (Block is null)
            {
                Block = MakeHeader();
                if (Block == null) return null;
                Contract contract = Contract.CreateMultiSigContract(this.M(), Validators);
                ContractParametersContext sc = new ContractParametersContext(Block);
                for (int i = 0, j = 0; i < Validators.Length && j < this.M(); i++)
                {
                    if (CommitPayloads[i]?.ConsensusMessage.ViewNumber != ViewNumber) continue;
                    sc.AddSignature(contract, Validators[i], CommitPayloads[i].GetDeserializedMessage<Commit>().Signature);
                    j++;
                }
                Block.Witness = sc.GetWitnesses()[0];
                Block.Transactions = TransactionHashes.Select(p => Transactions[p]).ToArray();
            }
            return Block;
        }

        public void Deserialize(BinaryReader reader)
        {
            Reset(0);
            if (reader.ReadUInt32() != Version) throw new FormatException();
            if (reader.ReadUInt32() != BlockIndex) throw new InvalidOperationException();
            ViewNumber = reader.ReadByte();
            PrimaryIndex = reader.ReadUInt32();
            Timestamp = reader.ReadUInt32();
            Nonce = reader.ReadUInt64();
            NextConsensus = reader.ReadSerializable<UInt160>();
            if (NextConsensus.Equals(UInt160.Zero))
                NextConsensus = null;
            TransactionHashes = reader.ReadSerializableArray<UInt256>();
            if (TransactionHashes.Length == 0)
                TransactionHashes = null;
            Transaction[] transactions = new Transaction[reader.ReadVarInt(Block.MaxTransactionsPerBlock)];
            if (transactions.Length == 0)
            {
                Transactions = null;
            }
            else
            {
                for (int i = 0; i < transactions.Length; i++)
                    transactions[i] = Transaction.DeserializeFrom(reader);
                Transactions = transactions.ToDictionary(p => p.Hash);
            }
            PreparationPayloads = new ConsensusPayload[reader.ReadVarInt(Blockchain.MaxValidators)];
            for (int i = 0; i < PreparationPayloads.Length; i++)
                PreparationPayloads[i] = reader.ReadBoolean() ? reader.ReadSerializable<ConsensusPayload>() : null;
            CommitPayloads = new ConsensusPayload[reader.ReadVarInt(Blockchain.MaxValidators)];
            for (int i = 0; i < CommitPayloads.Length; i++)
                CommitPayloads[i] = reader.ReadBoolean() ? reader.ReadSerializable<ConsensusPayload>() : null;
            ChangeViewPayloads = new ConsensusPayload[reader.ReadVarInt(Blockchain.MaxValidators)];
            for (int i = 0; i < ChangeViewPayloads.Length; i++)
                ChangeViewPayloads[i] = reader.ReadBoolean() ? reader.ReadSerializable<ConsensusPayload>() : null;
            LastChangeViewPayloads = new ConsensusPayload[reader.ReadVarInt(Blockchain.MaxValidators)];
            for (int i = 0; i < LastChangeViewPayloads.Length; i++)
                LastChangeViewPayloads[i] = reader.ReadBoolean() ? reader.ReadSerializable<ConsensusPayload>() : null;
        }

        public void Dispose()
        {
            Snapshot?.Dispose();
        }

        public bool Load()
        {
            byte[] data = store.Get(CN_Context, new byte[0]);
            if (data is null || data.Length == 0) return false;
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                try
                {
                    Deserialize(reader);
                }
                catch
                {
                    return false;
                }
                return true;
            }
        }

        public ConsensusPayload MakeChangeView()
        {
            return ChangeViewPayloads[MyIndex] = MakeSignedPayload(new ChangeView
            {
                Timestamp = TimeProvider.Current.UtcNow.ToTimestamp()
            });
        }

        public ConsensusPayload MakeCommit()
        {
            return CommitPayloads[MyIndex] ?? (CommitPayloads[MyIndex] = MakeSignedPayload(new Commit
            {
                Signature = MakeHeader()?.Sign(keyPair)
            }));
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
                ConsensusMessage = message
            };
            SignPayload(payload);
            return payload;
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
            payload.Witness = sc.GetWitnesses()[0];
        }

        public ConsensusPayload MakePrepareRequest()
        {
            Fill();
            return PreparationPayloads[MyIndex] = MakeSignedPayload(new PrepareRequest
            {
                Timestamp = Timestamp,
                Nonce = Nonce,
                NextConsensus = NextConsensus,
                TransactionHashes = TransactionHashes,
                MinerTransaction = (MinerTransaction)Transactions[TransactionHashes[0]]
            });
        }

        public ConsensusPayload MakeRecoveryRequest()
        {
            return MakeSignedPayload(new RecoveryRequest
            {
                Timestamp = TimeProvider.Current.UtcNow.ToTimestamp()
            });
        }

        public ConsensusPayload MakeRecoveryMessage()
        {
            PrepareRequest prepareRequestMessage = null;
            if (TransactionHashes != null)
            {
                prepareRequestMessage = new PrepareRequest
                {
                    ViewNumber = ViewNumber,
                    TransactionHashes = TransactionHashes,
                    Nonce = Nonce,
                    NextConsensus = NextConsensus,
                    MinerTransaction = (MinerTransaction)Transactions[TransactionHashes[0]],
                    Timestamp = Timestamp
                };
            }
            return MakeSignedPayload(new RecoveryMessage()
            {
                ChangeViewMessages = LastChangeViewPayloads.Where(p => p != null).Select(p => RecoveryMessage.ChangeViewPayloadCompact.FromPayload(p)).Take(this.M()).ToDictionary(p => (int)p.ValidatorIndex),
                PrepareRequestMessage = prepareRequestMessage,
                // We only need a PreparationHash set if we don't have the PrepareRequest information.
                PreparationHash = TransactionHashes == null ? PreparationPayloads.Where(p => p != null).GroupBy(p => p.GetDeserializedMessage<PrepareResponse>().PreparationHash, (k, g) => new { Hash = k, Count = g.Count() }).OrderByDescending(p => p.Count).Select(p => p.Hash).FirstOrDefault() : null,
                PreparationMessages = PreparationPayloads.Where(p => p != null).Select(p => RecoveryMessage.PreparationPayloadCompact.FromPayload(p)).ToDictionary(p => (int)p.ValidatorIndex),
                CommitMessages = this.CommitSent()
                    ? CommitPayloads.Where(p => p != null).Select(p => RecoveryMessage.CommitPayloadCompact.FromPayload(p)).ToDictionary(p => (int)p.ValidatorIndex)
                    : new Dictionary<int, RecoveryMessage.CommitPayloadCompact>()
            });
        }

        public ConsensusPayload MakePrepareResponse()
        {
            return PreparationPayloads[MyIndex] = MakeSignedPayload(new PrepareResponse
            {
                PreparationHash = PreparationPayloads[PrimaryIndex].Hash
            });
        }

        public void Reset(byte viewNumber)
        {
            if (viewNumber == 0)
            {
                Block = null;
                Snapshot?.Dispose();
                Snapshot = Blockchain.Singleton.GetSnapshot();
                PrevHash = Snapshot.CurrentBlockHash;
                BlockIndex = Snapshot.Height + 1;
                Validators = Snapshot.GetValidators();
                MyIndex = -1;
                ChangeViewPayloads = new ConsensusPayload[Validators.Length];
                LastChangeViewPayloads = new ConsensusPayload[Validators.Length];
                CommitPayloads = new ConsensusPayload[Validators.Length];
                if (LastSeenMessage == null)
                {
                    LastSeenMessage = new Dictionary<ECPoint, int>();
                    foreach (var validator in Validators)
                        LastSeenMessage[validator] = -1;
                }
                if (0 < Snapshot.Height && Snapshot.GetBlock(Snapshot.Height).NextConsensus != Snapshot.GetBlock(Snapshot.Height - 1)?.NextConsensus)
                {
                    var old_last_seen_message = LastSeenMessage;
                    LastSeenMessage = new Dictionary<ECPoint, int>();
                    foreach (var validator in Validators)
                    {
                        if (old_last_seen_message.TryGetValue(validator, out int value))
                            LastSeenMessage[validator] = value;
                        else
                            LastSeenMessage[validator] = -1;
                    }
                }
                keyPair = null;
                for (int i = 0; i < Validators.Length; i++)
                {
                    WalletAccount account = wallet?.GetAccount(Validators[i]);
                    if (account?.HasKey != true) continue;
                    MyIndex = i;
                    keyPair = account.GetKey();
                    break;
                }
            }
            else
            {
                for (int i = 0; i < LastChangeViewPayloads.Length; i++)
                    if (ChangeViewPayloads[i]?.GetDeserializedMessage<ChangeView>().NewViewNumber >= viewNumber)
                        LastChangeViewPayloads[i] = ChangeViewPayloads[i];
                    else
                        LastChangeViewPayloads[i] = null;
            }
            ViewNumber = viewNumber;
            PrimaryIndex = this.GetPrimaryIndex(viewNumber);
            Timestamp = 0;
            TransactionHashes = null;
            PreparationPayloads = new ConsensusPayload[Validators.Length];
            if (MyIndex >= 0) LastSeenMessage[Validators[MyIndex]] = (int)BlockIndex;
            _header = null;
        }

        public void Save()
        {
            store.PutSync(CN_Context, new byte[0], this.ToArray());
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(BlockIndex);
            writer.Write(ViewNumber);
            writer.Write(PrimaryIndex);
            writer.Write(Timestamp);
            writer.Write(Nonce);
            writer.Write(NextConsensus ?? UInt160.Zero);
            writer.Write(TransactionHashes ?? new UInt256[0]);
            writer.Write(Transactions?.Values.ToArray() ?? new Transaction[0]);
            writer.WriteVarInt(PreparationPayloads.Length);
            foreach (var payload in PreparationPayloads)
            {
                bool hasPayload = !(payload is null);
                writer.Write(hasPayload);
                if (!hasPayload) continue;
                writer.Write(payload);
            }
            writer.WriteVarInt(CommitPayloads.Length);
            foreach (var payload in CommitPayloads)
            {
                bool hasPayload = !(payload is null);
                writer.Write(hasPayload);
                if (!hasPayload) continue;
                writer.Write(payload);
            }
            writer.WriteVarInt(ChangeViewPayloads.Length);
            foreach (var payload in ChangeViewPayloads)
            {
                bool hasPayload = !(payload is null);
                writer.Write(hasPayload);
                if (!hasPayload) continue;
                writer.Write(payload);
            }
            writer.WriteVarInt(LastChangeViewPayloads.Length);
            foreach (var payload in LastChangeViewPayloads)
            {
                bool hasPayload = !(payload is null);
                writer.Write(hasPayload);
                if (!hasPayload) continue;
                writer.Write(payload);
            }
        }

        private void Fill()
        {
            IEnumerable<Transaction> memoryPoolTransactions = Blockchain.Singleton.MemPool.GetSortedVerifiedTransactions();
            foreach (IPolicyPlugin plugin in Plugin.Policies)
                memoryPoolTransactions = plugin.FilterForBlock(memoryPoolTransactions);
            List<Transaction> transactions = memoryPoolTransactions.ToList();
            Fixed8 amountNetFee = Block.CalculateNetFee(transactions);
            TransactionOutput[] outputs = amountNetFee == Fixed8.Zero ? new TransactionOutput[0] : new[] { new TransactionOutput
            {
                AssetId = Blockchain.UtilityToken.Hash,
                Value = amountNetFee,
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
            Timestamp = Math.Max(TimeProvider.Current.UtcNow.ToTimestamp(), this.PrevHeader().Timestamp + 1);
        }

        private static ulong GetNonce()
        {
            byte[] nonce = new byte[sizeof(ulong)];
            Random rand = new Random();
            rand.NextBytes(nonce);
            return nonce.ToUInt64(0);
        }
    }
}
