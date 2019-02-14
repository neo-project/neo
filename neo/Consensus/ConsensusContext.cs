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
        public const uint Version = 0;
        public ConsensusState State { get; set; }
        public UInt256 PrevHash { get; set; }
        public uint BlockIndex { get; set; }
        public byte ViewNumber { get; set; }
        public ECPoint[] Validators { get; set; }
        public int MyIndex { get; set; }
        public uint PrimaryIndex { get; set; }
        public ConsensusPayload[] ChangeViewPayloads { get; set; }
        public uint Timestamp { get; set; }
        public ulong Nonce { get; set; }
        public UInt160 NextConsensus { get; set; }
        public UInt256[] TransactionHashes { get; set; }
        public Dictionary<UInt256, Transaction> Transactions { get; set; }
        public ConsensusPayload[] PreparationPayloads { get; set; }
        public byte[][] Commits { get; set; }
        private Snapshot snapshot;
        private KeyPair keyPair;
        private readonly Wallet wallet;

        public int F => (Validators.Length - 1) / 3;
        public int M => Validators.Length - F;
        public Header PrevHeader => snapshot.GetHeader(PrevHash);
        public int Size => throw new NotImplementedException();

        public bool TransactionExists(UInt256 hash) => snapshot.ContainsTransaction(hash);
        public bool VerifyTransaction(Transaction tx) => tx.Verify(snapshot, Transactions.Values);

        public ConsensusContext(Wallet wallet)
        {
            this.wallet = wallet;
        }

        public Block CreateBlock()
        {
            Block block = MakeHeader();
            if (block == null) return null;
            Contract contract = Contract.CreateMultiSigContract(M, Validators);
            ContractParametersContext sc = new ContractParametersContext(block);
            for (int i = 0, j = 0; i < Validators.Length && j < M; i++)
                if (Commits[i] != null)
                {
                    sc.AddSignature(contract, Validators[i], Commits[i]);
                    j++;
                }
            sc.Verifiable.Witnesses = sc.GetWitnesses();
            block.Transactions = TransactionHashes.Select(p => Transactions[p]).ToArray();
            return block;
        }

        public void Deserialize(BinaryReader reader)
        {
            if (reader.ReadUInt32() != Version) return;
            State = (ConsensusState)reader.ReadByte();
            PrevHash = reader.ReadSerializable<UInt256>();
            BlockIndex = reader.ReadUInt32();
            ViewNumber = reader.ReadByte();
            Validators = reader.ReadSerializableArray<ECPoint>();
            MyIndex = reader.ReadInt32();
            PrimaryIndex = reader.ReadUInt32();
            var numChangeViews = reader.ReadVarInt();
            if (numChangeViews > 0)
            {
                ChangeViewPayloads = new ConsensusPayload[numChangeViews];
                for (int i = 0; i < ChangeViewPayloads.Length; i++)
                    ChangeViewPayloads[i] = reader.ReadBoolean() ? reader.ReadSerializable<ConsensusPayload>() : null;
            }
            Timestamp = reader.ReadUInt32();
            Nonce = reader.ReadUInt64();
            NextConsensus = reader.ReadSerializable<UInt160>();
            if (NextConsensus.Equals(UInt160.Zero))
                NextConsensus = null;
            TransactionHashes = reader.ReadSerializableArray<UInt256>();
            if (TransactionHashes.Length == 0)
                TransactionHashes = null;
            Transaction[] transactions = new Transaction[reader.ReadVarInt()];
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
            var numPreparationPayloads = reader.ReadVarInt();
            if (numPreparationPayloads > 0)
            {
                PreparationPayloads = new ConsensusPayload[numPreparationPayloads];
                for (int i = 0; i < ChangeViewPayloads.Length; i++)
                    PreparationPayloads[i] = reader.ReadBoolean() ? reader.ReadSerializable<ConsensusPayload>() : null;
            }
            Commits = new byte[reader.ReadVarInt()][];
            for (int i = 0; i < Commits.Length; i++)
            {
                Commits[i] = reader.ReadVarBytes();
                if (Commits[i].Length == 0)
                    Commits[i] = null;
            }
        }

        public void Dispose()
        {
            snapshot?.Dispose();
        }

        public uint GetPrimaryIndex(byte viewNumber)
        {
            int p = ((int)BlockIndex - viewNumber) % Validators.Length;
            return p >= 0 ? (uint)p : (uint)(p + Validators.Length);
        }

        public ConsensusPayload MakeChangeView(byte newViewNumber)
        {
            // Change view messages use the current time as their timestamp. This allows receiving nodes to ensure
            // they only respond once to a specific ChangeView request (it thus prevents other nodes repeatedly
            // broadcasting the ChangeView message to cause CN nodes to repeatedly send recovery messages).
            return MakeSignedPayload(new ChangeView
            {
                NewViewNumber = newViewNumber,
                Timestamp = TimeProvider.Current.UtcNow.ToTimestamp()
            });
        }

        public ConsensusPayload MakeCommit()
        {
            if (Commits[MyIndex] == null)
                Commits[MyIndex] = MakeHeader()?.Sign(keyPair);
            return MakeSignedPayload(new Commit
            {
                Signature = Commits[MyIndex]
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
                ConsensusMessage = message
            };
            SignPayload(payload);
            return payload;
        }

        public ConsensusPayload RegenerateSignedPayload(ConsensusMessage message, ushort validatorIndex,
            byte[] witnessInvocationScript)
        {
            ConsensusPayload payload = new ConsensusPayload
            {
                Version = Version,
                PrevHash = PrevHash,
                BlockIndex = BlockIndex,
                ValidatorIndex = validatorIndex,
                ConsensusMessage = message
            };
            Witness[] witnesses = new Witness[1];
            witnesses[0] = new Witness
            {
                InvocationScript = witnessInvocationScript,
                VerificationScript = Contract.CreateSignatureRedeemScript(Validators[validatorIndex])
            };
            ((IVerifiable) payload).Witnesses = witnesses;

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
            sc.Verifiable.Witnesses = sc.GetWitnesses();
        }

        public ConsensusPayload MakePrepareRequest()
        {
            return MakeSignedPayload(new PrepareRequest
            {
                Nonce = Nonce,
                NextConsensus = NextConsensus,
                TransactionHashes = TransactionHashes,
                MinerTransaction = (MinerTransaction)Transactions[TransactionHashes[0]]
            });
        }

        public ConsensusPayload MakeRecoveryMessage()
        {
            var changeViewPayloads = TransactionHashes == null || PreparationPayloads.Count(p => p != null) < M
                ? ChangeViewPayloads : null;

            PrepareRequest prepareRequestMessage = null;
            if (TransactionHashes != null)
            {
                prepareRequestMessage = new PrepareRequest
                {
                    TransactionHashes = TransactionHashes,
                    Nonce = Nonce,
                    NextConsensus = NextConsensus,
                    MinerTransaction = (MinerTransaction) (TransactionHashes == null ? null : Transactions?[TransactionHashes[0]]),
                    Timestamp = Timestamp
                };
            }
            return MakeSignedPayload(new RecoveryMessage()
            {
                ChangeViewMessages = (changeViewPayloads ?? new ConsensusPayload[0]).Where(p => p != null).Select(p => RecoveryMessage.ChangeViewPayloadCompact.FromPayload(p)).ToDictionary(p => (int)p.ValidatorIndex),
                PrepareRequestMessage = prepareRequestMessage,
                // We only need a PreparationHash set if we don't have the PrepareRequest information.
                PreparationHash = TransactionHashes == null ? PreparationPayloads.Where(p => p != null).GroupBy(p => p.GetDeserializedMessage<PrepareResponse>().PreparationHash, (k, g) => new { Hash = k, Count = g.Count() }).OrderByDescending(p => p.Count).Select(p => p.Hash).FirstOrDefault() : null,
                PreparationMessages = PreparationPayloads.Where(p => p != null).Select(p => RecoveryMessage.PreparationPayloadCompact.FromPayload(p)).ToDictionary(p => (int)p.ValidatorIndex),
                CommitMessages = State.HasFlag(ConsensusState.CommitSent)
                    ? Commits.Select((s, i) => new RecoveryMessage.CommitPayloadCompact { Signature = s, ValidatorIndex = (ushort)i }).Where(p => p.Signature != null).ToDictionary(p => (int)p.ValidatorIndex)
                    : null
            });
        }

        public ConsensusPayload MakePrepareResponse(UInt256 preparation)
        {
            return MakeSignedPayload(new PrepareResponse
            {
                PreparationHash = preparation
            });
        }

        public void Reset(byte viewNumber, Snapshot newSnapshot=null)
        {
            if (viewNumber == 0)
            {
                snapshot?.Dispose();
                snapshot = newSnapshot ?? Blockchain.Singleton.GetSnapshot();
                PrevHash = snapshot.CurrentBlockHash;
                BlockIndex = snapshot.Height + 1;
                Validators = snapshot.GetValidators();
                MyIndex = -1;
                ChangeViewPayloads = new ConsensusPayload[Validators.Length];
                keyPair = null;
                for (int i = 0; i < Validators.Length; i++)
                {
                    WalletAccount account = wallet.GetAccount(Validators[i]);
                    if (account?.HasKey == true)
                    {
                        MyIndex = i;
                        keyPair = account.GetKey();
                        break;
                    }
                }
            }
            State = ConsensusState.Initial;
            ViewNumber = viewNumber;
            PrimaryIndex = GetPrimaryIndex(viewNumber);
            Timestamp = 0;
            TransactionHashes = null;
            PreparationPayloads = new ConsensusPayload[Validators.Length];
            Commits = new byte[Validators.Length][];
            _header = null;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write((byte)State);
            writer.Write(PrevHash);
            writer.Write(BlockIndex);
            writer.Write(ViewNumber);
            writer.Write(Validators);
            writer.Write(MyIndex);
            writer.Write(PrimaryIndex);
            if (ChangeViewPayloads == null)
                writer.WriteVarInt(0);
            else
            {
                writer.WriteVarInt(ChangeViewPayloads.Length);
                foreach (var payload in ChangeViewPayloads)
                {
                    bool hasPayload = !(payload is null);
                    writer.Write(hasPayload);
                    if (!hasPayload) continue;
                    writer.Write(payload);
                }
            }
            writer.Write(Timestamp);
            writer.Write(Nonce);
            writer.Write(NextConsensus ?? UInt160.Zero);
            writer.Write(TransactionHashes ?? new UInt256[0]);
            writer.Write(Transactions?.Values.ToArray() ?? new Transaction[0]);
            if (PreparationPayloads == null)
                writer.WriteVarInt(0);
            else
            {
                writer.WriteVarInt(PreparationPayloads.Length);
                foreach (var payload in PreparationPayloads)
                {
                    bool hasPayload = !(payload is null);
                    writer.Write(hasPayload);
                    if (!hasPayload) continue;
                    writer.Write(payload);
                }
            }
            writer.WriteVarInt(Commits.Length);
            foreach (byte[] commit in Commits)
                if (commit is null)
                    writer.WriteVarInt(0);
                else
                    writer.WriteVarBytes(commit);
        }

        public void Fill()
        {
            IEnumerable<Transaction> mem_pool = Blockchain.Singleton.MemPool.GetSortedVerifiedTransactions();
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
                if (!snapshot.ContainsTransaction(tx.Hash))
                {
                    Nonce = nonce;
                    transactions.Insert(0, tx);
                    break;
                }
            }
            TransactionHashes = transactions.Select(p => p.Hash).ToArray();
            Transactions = transactions.ToDictionary(p => p.Hash);
            NextConsensus = Blockchain.GetConsensusAddress(snapshot.GetValidators(transactions).ToArray());
            Timestamp = Math.Max(TimeProvider.Current.UtcNow.ToTimestamp(), PrevHeader.Timestamp + 1);
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
            if (!Blockchain.GetConsensusAddress(snapshot.GetValidators(Transactions.Values).ToArray()).Equals(NextConsensus))
                return false;
            Transaction tx_gen = Transactions.Values.FirstOrDefault(p => p.Type == TransactionType.MinerTransaction);
            Fixed8 amount_netfee = Block.CalculateNetFee(Transactions.Values);
            if (tx_gen?.Outputs.Sum(p => p.Value) != amount_netfee) return false;
            return true;
        }
    }
}
