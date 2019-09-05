using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Neo.Consensus
{
    internal class ConsensusContext : IDisposable, ISerializable
    {
        /// <summary>
        /// Key for saving consensus state.
        /// </summary>
        private static readonly byte[] ConsensusStateKey = { 0xf4 };

        public Block Block;
        public byte ViewNumber;
        public ECPoint[] Validators;
        public int MyIndex;
        public UInt256[] TransactionHashes;
        public Dictionary<UInt256, Transaction> Transactions;
        public ConsensusPayload[] PreparationPayloads;
        public ConsensusPayload[] CommitPayloads;
        public ConsensusPayload[] ChangeViewPayloads;
        public ConsensusPayload[] LastChangeViewPayloads;
        // LastSeenMessage array stores the height of the last seen message, for each validator.
        // if this node never heard from validator i, LastSeenMessage[i] will be -1.
        public int[] LastSeenMessage;

        public Snapshot Snapshot { get; private set; }
        private KeyPair keyPair;
        private int _witnessSize;
        private readonly Wallet wallet;
        private readonly Store store;
        private readonly Random random = new Random();

        public int F => (Validators.Length - 1) / 3;
        public int M => Validators.Length - F;
        public bool IsPrimary => MyIndex == Block.ConsensusData.PrimaryIndex;
        public bool IsBackup => MyIndex >= 0 && MyIndex != Block.ConsensusData.PrimaryIndex;
        public bool WatchOnly => MyIndex < 0;
        public Header PrevHeader => Snapshot.GetHeader(Block.PrevHash);
        public int CountCommitted => CommitPayloads.Count(p => p != null);
        public int CountFailed => LastSeenMessage.Count(p => p < (((int)Block.Index) - 1));

        #region Consensus States
        public bool RequestSentOrReceived => PreparationPayloads[Block.ConsensusData.PrimaryIndex] != null;
        public bool ResponseSent => !WatchOnly && PreparationPayloads[MyIndex] != null;
        public bool CommitSent => !WatchOnly && CommitPayloads[MyIndex] != null;
        public bool BlockSent => Block.Transactions != null;
        public bool ViewChanging => !WatchOnly && ChangeViewPayloads[MyIndex]?.GetDeserializedMessage<ChangeView>().NewViewNumber > ViewNumber;
        public bool NotAcceptingPayloadsDueToViewChanging => ViewChanging && !MoreThanFNodesCommittedOrLost;
        // A possible attack can happen if the last node to commit is malicious and either sends change view after his
        // commit to stall nodes in a higher view, or if he refuses to send recovery messages. In addition, if a node
        // asking change views loses network or crashes and comes back when nodes are committed in more than one higher
        // numbered view, it is possible for the node accepting recovery to commit in any of the higher views, thus
        // potentially splitting nodes among views and stalling the network.
        public bool MoreThanFNodesCommittedOrLost => (CountCommitted + CountFailed) > F;
        #endregion

        public int Size => throw new NotImplementedException();

        public ConsensusContext(Wallet wallet, Store store)
        {
            this.wallet = wallet;
            this.store = store;
        }

        public Block CreateBlock()
        {
            Contract contract = Contract.CreateMultiSigContract(M, Validators);
            ContractParametersContext sc = new ContractParametersContext(Block);
            for (int i = 0, j = 0; i < Validators.Length && j < M; i++)
            {
                if (CommitPayloads[i]?.ConsensusMessage.ViewNumber != ViewNumber) continue;
                sc.AddSignature(contract, Validators[i], CommitPayloads[i].GetDeserializedMessage<Commit>().Signature);
                j++;
            }
            Block.Witness = sc.GetWitnesses()[0];
            Block.Transactions = TransactionHashes.Select(p => Transactions[p]).ToArray();
            return Block;
        }

        public void Deserialize(BinaryReader reader)
        {
            Reset(0);
            if (reader.ReadUInt32() != Block.Version) throw new FormatException();
            if (reader.ReadUInt32() != Block.Index) throw new InvalidOperationException();
            Block.Timestamp = reader.ReadUInt64();
            Block.NextConsensus = reader.ReadSerializable<UInt160>();
            if (Block.NextConsensus.Equals(UInt160.Zero))
                Block.NextConsensus = null;
            Block.ConsensusData = reader.ReadSerializable<ConsensusData>();
            ViewNumber = reader.ReadByte();
            TransactionHashes = reader.ReadSerializableArray<UInt256>();
            Transaction[] transactions = reader.ReadSerializableArray<Transaction>(Block.MaxTransactionsPerBlock);
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
            if (TransactionHashes.Length == 0 && !RequestSentOrReceived)
                TransactionHashes = null;
            Transactions = transactions.Length == 0 && !RequestSentOrReceived ? null : transactions.ToDictionary(p => p.Hash);
        }

        public void Dispose()
        {
            Snapshot?.Dispose();
        }

        public Block EnsureHeader()
        {
            if (TransactionHashes == null) return null;
            if (Block.MerkleRoot is null)
                Block.MerkleRoot = Block.CalculateMerkleRoot(Block.ConsensusData.Hash, TransactionHashes);
            return Block;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetPrimaryIndex(byte viewNumber)
        {
            int p = ((int)Block.Index - viewNumber) % Validators.Length;
            return p >= 0 ? (uint)p : (uint)(p + Validators.Length);
        }

        public bool Load()
        {
            byte[] data = store.Get(ConsensusStateKey);
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

        public ConsensusPayload MakeChangeView(ChangeViewReason reason)
        {
            return ChangeViewPayloads[MyIndex] = MakeSignedPayload(new ChangeView
            {
                Reason = reason,
                Timestamp = TimeProvider.Current.UtcNow.ToTimestampMS()
            });
        }

        public ConsensusPayload MakeCommit()
        {
            return CommitPayloads[MyIndex] ?? (CommitPayloads[MyIndex] = MakeSignedPayload(new Commit
            {
                Signature = EnsureHeader().Sign(keyPair)
            }));
        }

        private ConsensusPayload MakeSignedPayload(ConsensusMessage message)
        {
            message.ViewNumber = ViewNumber;
            ConsensusPayload payload = new ConsensusPayload
            {
                Version = Block.Version,
                PrevHash = Block.PrevHash,
                BlockIndex = Block.Index,
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

        /// <summary>
        /// Return the expected block size
        /// </summary>
        internal int GetExpectedBlockSize()
        {
            return GetExpectedBlockSizeWithoutTransactions(Transactions.Count) + // Base size
                Transactions.Values.Sum(u => u.Size);   // Sum Txs
        }

        /// <summary>
        /// Return the expected block size without txs
        /// </summary>
        /// <param name="expectedTransactions">Expected transactions</param>
        internal int GetExpectedBlockSizeWithoutTransactions(int expectedTransactions)
        {
            var blockSize =
                // BlockBase
                sizeof(uint) +       //Version
                UInt256.Length +     //PrevHash
                UInt256.Length +     //MerkleRoot
                sizeof(ulong) +      //Timestamp
                sizeof(uint) +       //Index
                UInt160.Length +     //NextConsensus
                1 +                  //
                _witnessSize;        //Witness

            blockSize +=
                // Block
                Block.ConsensusData.Size +                      //ConsensusData
                IO.Helper.GetVarSize(expectedTransactions + 1); //Transactions count

            return blockSize;
        }

        /// <summary>
        /// Prevent that block exceed the max size
        /// </summary>
        /// <param name="txs">Ordered transactions</param>
        internal void EnsureMaxBlockSize(IEnumerable<Transaction> txs)
        {
            uint maxBlockSize = NativeContract.Policy.GetMaxBlockSize(Snapshot);
            uint maxTransactionsPerBlock = NativeContract.Policy.GetMaxTransactionsPerBlock(Snapshot);

            // Limit Speaker proposal to the limit `MaxTransactionsPerBlock` or all available transactions of the mempool
            txs = txs.Take((int)maxTransactionsPerBlock);
            List<UInt256> hashes = new List<UInt256>();
            Transactions = new Dictionary<UInt256, Transaction>();

            // Expected block size
            var blockSize = GetExpectedBlockSizeWithoutTransactions(txs.Count());

            // Iterate transaction until reach the size
            foreach (Transaction tx in txs)
            {
                // Check if maximum block size has been already exceeded with the current selected set
                blockSize += tx.Size;
                if (blockSize > maxBlockSize) break;

                hashes.Add(tx.Hash);
                Transactions.Add(tx.Hash, tx);
            }

            TransactionHashes = hashes.ToArray();
        }

        public ConsensusPayload MakePrepareRequest()
        {
            byte[] buffer = new byte[sizeof(ulong)];
            random.NextBytes(buffer);
            Block.ConsensusData.Nonce = BitConverter.ToUInt64(buffer, 0);
            EnsureMaxBlockSize(Blockchain.Singleton.MemPool.GetSortedVerifiedTransactions());
            Block.Timestamp = Math.Max(TimeProvider.Current.UtcNow.ToTimestampMS(), PrevHeader.Timestamp + 1);

            return PreparationPayloads[MyIndex] = MakeSignedPayload(new PrepareRequest
            {
                Timestamp = Block.Timestamp,
                Nonce = Block.ConsensusData.Nonce,
                TransactionHashes = TransactionHashes
            });
        }

        public ConsensusPayload MakeRecoveryRequest()
        {
            return MakeSignedPayload(new RecoveryRequest
            {
                Timestamp = TimeProvider.Current.UtcNow.ToTimestampMS()
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
                    Timestamp = Block.Timestamp,
                    Nonce = Block.ConsensusData.Nonce,
                    TransactionHashes = TransactionHashes
                };
            }
            return MakeSignedPayload(new RecoveryMessage()
            {
                ChangeViewMessages = LastChangeViewPayloads.Where(p => p != null).Select(p => RecoveryMessage.ChangeViewPayloadCompact.FromPayload(p)).Take(M).ToDictionary(p => (int)p.ValidatorIndex),
                PrepareRequestMessage = prepareRequestMessage,
                // We only need a PreparationHash set if we don't have the PrepareRequest information.
                PreparationHash = TransactionHashes == null ? PreparationPayloads.Where(p => p != null).GroupBy(p => p.GetDeserializedMessage<PrepareResponse>().PreparationHash, (k, g) => new { Hash = k, Count = g.Count() }).OrderByDescending(p => p.Count).Select(p => p.Hash).FirstOrDefault() : null,
                PreparationMessages = PreparationPayloads.Where(p => p != null).Select(p => RecoveryMessage.PreparationPayloadCompact.FromPayload(p)).ToDictionary(p => (int)p.ValidatorIndex),
                CommitMessages = CommitSent
                    ? CommitPayloads.Where(p => p != null).Select(p => RecoveryMessage.CommitPayloadCompact.FromPayload(p)).ToDictionary(p => (int)p.ValidatorIndex)
                    : new Dictionary<int, RecoveryMessage.CommitPayloadCompact>()
            });
        }

        public ConsensusPayload MakePrepareResponse()
        {
            return PreparationPayloads[MyIndex] = MakeSignedPayload(new PrepareResponse
            {
                PreparationHash = PreparationPayloads[Block.ConsensusData.PrimaryIndex].Hash
            });
        }

        public void Reset(byte viewNumber)
        {
            if (viewNumber == 0)
            {
                Snapshot?.Dispose();
                Snapshot = Blockchain.Singleton.GetSnapshot();
                Block = new Block
                {
                    PrevHash = Snapshot.CurrentBlockHash,
                    Index = Snapshot.Height + 1,
                    NextConsensus = Blockchain.GetConsensusAddress(NativeContract.NEO.GetValidators(Snapshot).ToArray())
                };
                var pv = Validators;
                Validators = NativeContract.NEO.GetNextBlockValidators(Snapshot);
                if (_witnessSize == 0 || (pv != null && pv.Length != Validators.Length))
                {
                    // Compute the expected size of the witness
                    using (ScriptBuilder sb = new ScriptBuilder())
                    {
                        for (int x = 0; x < M; x++)
                        {
                            sb.EmitPush(new byte[64]);
                        }
                        _witnessSize = new Witness
                        {
                            InvocationScript = sb.ToArray(),
                            VerificationScript = Contract.CreateMultiSigRedeemScript(M, Validators)
                        }.Size;
                    }
                }
                MyIndex = -1;
                ChangeViewPayloads = new ConsensusPayload[Validators.Length];
                LastChangeViewPayloads = new ConsensusPayload[Validators.Length];
                CommitPayloads = new ConsensusPayload[Validators.Length];
                if (LastSeenMessage == null)
                {
                    LastSeenMessage = new int[Validators.Length];
                    for (int i = 0; i < Validators.Length; i++)
                        LastSeenMessage[i] = -1;
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
            Block.ConsensusData = new ConsensusData
            {
                PrimaryIndex = GetPrimaryIndex(viewNumber)
            };
            Block.MerkleRoot = null;
            Block.Timestamp = 0;
            Block.Transactions = null;
            TransactionHashes = null;
            PreparationPayloads = new ConsensusPayload[Validators.Length];
            if (MyIndex >= 0) LastSeenMessage[MyIndex] = (int)Block.Index;
        }

        public void Save()
        {
            store.PutSync(ConsensusStateKey, this.ToArray());
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Block.Version);
            writer.Write(Block.Index);
            writer.Write(Block.Timestamp);
            writer.Write(Block.NextConsensus ?? UInt160.Zero);
            writer.Write(Block.ConsensusData);
            writer.Write(ViewNumber);
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
    }
}
