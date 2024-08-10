// Copyright (C) 2015-2024 The Neo Project.
//
// ConsensusContext.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins.DBFTPlugin.Messages;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Plugins.DBFTPlugin.Consensus
{
    public partial class ConsensusContext : IDisposable, ISerializable
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
        public ExtensiblePayload[] PreparationPayloads;
        public ExtensiblePayload[] CommitPayloads;
        public ExtensiblePayload[] ChangeViewPayloads;
        public ExtensiblePayload[] LastChangeViewPayloads;
        // LastSeenMessage array stores the height of the last seen message, for each validator.
        // if this node never heard from validator i, LastSeenMessage[i] will be -1.
        public Dictionary<ECPoint, uint> LastSeenMessage { get; private set; }

        /// <summary>
        /// Store all verified unsorted transactions' senders' fee currently in the consensus context.
        /// </summary>
        public TransactionVerificationContext VerificationContext = new();

        public SnapshotCache Snapshot { get; private set; }
        private KeyPair keyPair;
        private int _witnessSize;
        private readonly NeoSystem neoSystem;
        private readonly Settings dbftSettings;
        private readonly Wallet wallet;
        private readonly IStore store;
        private Dictionary<UInt256, ConsensusMessage> cachedMessages;

        public int F => (Validators.Length - 1) / 3;
        public int M => Validators.Length - F;
        public bool IsPrimary => MyIndex == Block.PrimaryIndex;
        public bool IsBackup => MyIndex >= 0 && MyIndex != Block.PrimaryIndex;
        public bool WatchOnly => MyIndex < 0;
        public Header PrevHeader => NativeContract.Ledger.GetHeader(Snapshot, Block.PrevHash);
        public int CountCommitted => CommitPayloads.Count(p => p != null);
        public int CountFailed
        {
            get
            {
                if (LastSeenMessage == null) return 0;
                return Validators.Count(p => !LastSeenMessage.TryGetValue(p, out var value) || value < (Block.Index - 1));
            }
        }
        public bool ValidatorsChanged
        {
            get
            {
                if (NativeContract.Ledger.CurrentIndex(Snapshot) == 0) return false;
                UInt256 hash = NativeContract.Ledger.CurrentHash(Snapshot);
                TrimmedBlock currentBlock = NativeContract.Ledger.GetTrimmedBlock(Snapshot, hash);
                TrimmedBlock previousBlock = NativeContract.Ledger.GetTrimmedBlock(Snapshot, currentBlock.Header.PrevHash);
                return currentBlock.Header.NextConsensus != previousBlock.Header.NextConsensus;
            }
        }

        #region Consensus States
        public bool RequestSentOrReceived => PreparationPayloads[Block.PrimaryIndex] != null;
        public bool ResponseSent => !WatchOnly && PreparationPayloads[MyIndex] != null;
        public bool CommitSent => !WatchOnly && CommitPayloads[MyIndex] != null;
        public bool BlockSent => Block.Transactions != null;
        public bool ViewChanging => !WatchOnly && GetMessage<ChangeView>(ChangeViewPayloads[MyIndex])?.NewViewNumber > ViewNumber;
        // NotAcceptingPayloadsDueToViewChanging imposes nodes to not accept some payloads if View is Changing,
        // i.e: OnTransaction function will not process any transaction; OnPrepareRequestReceived will also return;
        // as well as OnPrepareResponseReceived and also similar logic for recovering.
        // On the other hand, if more than MoreThanFNodesCommittedOrLost is true, we keep accepting those payloads.
        // This helps the node to still commit, even while almost changing view.
        public bool NotAcceptingPayloadsDueToViewChanging => ViewChanging && !MoreThanFNodesCommittedOrLost;
        // A possible attack can happen if the last node to commit is malicious and either sends change view after his
        // commit to stall nodes in a higher view, or if he refuses to send recovery messages. In addition, if a node
        // asking change views loses network or crashes and comes back when nodes are committed in more than one higher
        // numbered view, it is possible for the node accepting recovery to commit in any of the higher views, thus
        // potentially splitting nodes among views and stalling the network.
        public bool MoreThanFNodesCommittedOrLost => (CountCommitted + CountFailed) > F;
        #endregion

        public int Size => throw new NotImplementedException();

        public ConsensusContext(NeoSystem neoSystem, Settings settings, Wallet wallet)
        {
            this.wallet = wallet;
            this.neoSystem = neoSystem;
            dbftSettings = settings;

            if (dbftSettings.IgnoreRecoveryLogs == false)
                store = neoSystem.LoadStore(settings.RecoveryLogs);
        }

        public Block CreateBlock()
        {
            EnsureHeader();
            Contract contract = Contract.CreateMultiSigContract(M, Validators);
            ContractParametersContext sc = new ContractParametersContext(neoSystem.StoreView, Block.Header, dbftSettings.Network);
            for (int i = 0, j = 0; i < Validators.Length && j < M; i++)
            {
                if (GetMessage(CommitPayloads[i])?.ViewNumber != ViewNumber) continue;
                sc.AddSignature(contract, Validators[i], GetMessage<Commit>(CommitPayloads[i]).Signature.ToArray());
                j++;
            }
            Block.Header.Witness = sc.GetWitnesses()[0];
            Block.Transactions = TransactionHashes.Select(p => Transactions[p]).ToArray();
            return Block;
        }

        public ExtensiblePayload CreatePayload(ConsensusMessage message, ReadOnlyMemory<byte> invocationScript = default)
        {
            ExtensiblePayload payload = new ExtensiblePayload
            {
                Category = "dBFT",
                ValidBlockStart = 0,
                ValidBlockEnd = message.BlockIndex,
                Sender = GetSender(message.ValidatorIndex),
                Data = message.ToArray(),
                Witness = invocationScript.IsEmpty ? null : new Witness
                {
                    InvocationScript = invocationScript,
                    VerificationScript = Contract.CreateSignatureRedeemScript(Validators[message.ValidatorIndex])
                }
            };
            cachedMessages.TryAdd(payload.Hash, message);
            return payload;
        }

        public void Dispose()
        {
            Snapshot?.Dispose();
        }

        public Block EnsureHeader()
        {
            if (TransactionHashes == null) return null;
            Block.Header.MerkleRoot ??= MerkleTree.ComputeRoot(TransactionHashes);
            return Block;
        }

        public bool Load()
        {
            byte[] data = store?.TryGet(ConsensusStateKey);
            if (data is null || data.Length == 0) return false;
            MemoryReader reader = new(data);
            try
            {
                Deserialize(ref reader);
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (Exception exception)
            {
                Utility.Log(nameof(ConsensusContext), LogLevel.Debug, exception.ToString());
                return false;
            }
            return true;
        }

        public void Reset(byte viewNumber)
        {
            if (viewNumber == 0)
            {
                Snapshot?.Dispose();
                Snapshot = neoSystem.GetSnapshotCache();
                uint height = NativeContract.Ledger.CurrentIndex(Snapshot);
                Block = new Block
                {
                    Header = new Header
                    {
                        PrevHash = NativeContract.Ledger.CurrentHash(Snapshot),
                        Index = height + 1,
                        NextConsensus = Contract.GetBFTAddress(
                            NeoToken.ShouldRefreshCommittee(height + 1, neoSystem.Settings.CommitteeMembersCount) ?
                            NativeContract.NEO.ComputeNextBlockValidators(Snapshot, neoSystem.Settings) :
                            NativeContract.NEO.GetNextBlockValidators(Snapshot, neoSystem.Settings.ValidatorsCount))
                    }
                };
                var pv = Validators;
                Validators = NativeContract.NEO.GetNextBlockValidators(Snapshot, neoSystem.Settings.ValidatorsCount);
                if (_witnessSize == 0 || (pv != null && pv.Length != Validators.Length))
                {
                    // Compute the expected size of the witness
                    using (ScriptBuilder sb = new())
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
                ChangeViewPayloads = new ExtensiblePayload[Validators.Length];
                LastChangeViewPayloads = new ExtensiblePayload[Validators.Length];
                CommitPayloads = new ExtensiblePayload[Validators.Length];
                if (ValidatorsChanged || LastSeenMessage is null)
                {
                    var previous_last_seen_message = LastSeenMessage;
                    LastSeenMessage = new Dictionary<ECPoint, uint>();
                    foreach (var validator in Validators)
                    {
                        if (previous_last_seen_message != null && previous_last_seen_message.TryGetValue(validator, out var value))
                            LastSeenMessage[validator] = value;
                        else
                            LastSeenMessage[validator] = height;
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
                cachedMessages = new Dictionary<UInt256, ConsensusMessage>();
            }
            else
            {
                for (int i = 0; i < LastChangeViewPayloads.Length; i++)
                    if (GetMessage<ChangeView>(ChangeViewPayloads[i])?.NewViewNumber >= viewNumber)
                        LastChangeViewPayloads[i] = ChangeViewPayloads[i];
                    else
                        LastChangeViewPayloads[i] = null;
            }
            ViewNumber = viewNumber;
            Block.Header.PrimaryIndex = GetPrimaryIndex(viewNumber);
            Block.Header.MerkleRoot = null;
            Block.Header.Timestamp = 0;
            Block.Header.Nonce = 0;
            Block.Transactions = null;
            TransactionHashes = null;
            PreparationPayloads = new ExtensiblePayload[Validators.Length];
            if (MyIndex >= 0) LastSeenMessage[Validators[MyIndex]] = Block.Index;
        }

        public void Save()
        {
            store?.PutSync(ConsensusStateKey, this.ToArray());
        }

        public void Deserialize(ref MemoryReader reader)
        {
            Reset(0);
            if (reader.ReadUInt32() != Block.Version) throw new FormatException();
            if (reader.ReadUInt32() != Block.Index) throw new InvalidOperationException();
            Block.Header.Timestamp = reader.ReadUInt64();
            Block.Header.Nonce = reader.ReadUInt64();
            Block.Header.PrimaryIndex = reader.ReadByte();
            Block.Header.NextConsensus = reader.ReadSerializable<UInt160>();
            if (Block.NextConsensus.Equals(UInt160.Zero))
                Block.Header.NextConsensus = null;
            ViewNumber = reader.ReadByte();
            TransactionHashes = reader.ReadSerializableArray<UInt256>(ushort.MaxValue);
            Transaction[] transactions = reader.ReadSerializableArray<Transaction>(ushort.MaxValue);
            PreparationPayloads = reader.ReadNullableArray<ExtensiblePayload>(neoSystem.Settings.ValidatorsCount);
            CommitPayloads = reader.ReadNullableArray<ExtensiblePayload>(neoSystem.Settings.ValidatorsCount);
            ChangeViewPayloads = reader.ReadNullableArray<ExtensiblePayload>(neoSystem.Settings.ValidatorsCount);
            LastChangeViewPayloads = reader.ReadNullableArray<ExtensiblePayload>(neoSystem.Settings.ValidatorsCount);
            if (TransactionHashes.Length == 0 && !RequestSentOrReceived)
                TransactionHashes = null;
            Transactions = transactions.Length == 0 && !RequestSentOrReceived ? null : transactions.ToDictionary(p => p.Hash);
            VerificationContext = new TransactionVerificationContext();
            if (Transactions != null)
            {
                foreach (Transaction tx in Transactions.Values)
                    VerificationContext.AddTransaction(tx);
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Block.Version);
            writer.Write(Block.Index);
            writer.Write(Block.Timestamp);
            writer.Write(Block.Nonce);
            writer.Write(Block.PrimaryIndex);
            writer.Write(Block.NextConsensus ?? UInt160.Zero);
            writer.Write(ViewNumber);
            writer.Write(TransactionHashes ?? Array.Empty<UInt256>());
            writer.Write(Transactions?.Values.ToArray() ?? Array.Empty<Transaction>());
            writer.WriteNullableArray(PreparationPayloads);
            writer.WriteNullableArray(CommitPayloads);
            writer.WriteNullableArray(ChangeViewPayloads);
            writer.WriteNullableArray(LastChangeViewPayloads);
        }
    }
}
