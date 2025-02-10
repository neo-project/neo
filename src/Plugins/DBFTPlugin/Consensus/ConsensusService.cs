// Copyright (C) 2015-2025 The Neo Project.
//
// ConsensusService.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Neo.Extensions;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.DBFTPlugin.Messages;
using Neo.Plugins.DBFTPlugin.Types;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using static Neo.Ledger.Blockchain;

namespace Neo.Plugins.DBFTPlugin.Consensus
{
    partial class ConsensusService : UntypedActor
    {
        public class Start { }
        private class Timer { public uint Height; public byte ViewNumber; }

        private readonly ConsensusContext context;
        private readonly IActorRef localNode;
        private readonly IActorRef taskManager;
        private readonly IActorRef blockchain;
        private ICancelable timer_token;
        private DateTime block_received_time;
        private DateTime onPrepareReceivedTime;
        private uint onPrepareBlockIndex;
        private uint block_received_index;
        private bool started = false;

        /// <summary>
        /// This will record the information from last scheduled timer
        /// </summary>
        private DateTime clock_started = TimeProvider.Current.UtcNow;
        private TimeSpan expected_delay = TimeSpan.Zero;

        /// <summary>
        /// This will be cleared every block (so it will not grow out of control, but is used to prevent repeatedly
        /// responding to the same message.
        /// </summary>
        private readonly HashSet<UInt256> knownHashes = new();
        /// <summary>
        /// This variable is only true during OnRecoveryMessageReceived
        /// </summary>
        private bool isRecovering = false;
        private readonly Settings dbftSettings;
        private readonly NeoSystem neoSystem;

        public ConsensusService(NeoSystem neoSystem, Settings settings, Wallet wallet)
            : this(neoSystem, settings, new ConsensusContext(neoSystem, settings, wallet)) { }

        internal ConsensusService(NeoSystem neoSystem, Settings settings, ConsensusContext context)
        {
            this.neoSystem = neoSystem;
            localNode = neoSystem.LocalNode;
            taskManager = neoSystem.TaskManager;
            blockchain = neoSystem.Blockchain;
            dbftSettings = settings;
            this.context = context;
            Context.System.EventStream.Subscribe(Self, typeof(PersistCompleted));
            Context.System.EventStream.Subscribe(Self, typeof(RelayResult));
        }

        private void OnPersistCompleted(Block block)
        {
            Log($"Persisted {nameof(Block)}: height={block.Index} hash={block.Hash} tx={block.Transactions.Length} nonce={block.Nonce}");
            knownHashes.Clear();
            InitializeConsensus(0);
        }

        private void InitializeConsensus(byte viewNumber)
        {
            context.Reset(viewNumber);
            if (viewNumber > 0)
                Log($"View changed: view={viewNumber} primary={context.Validators[context.GetPrimaryIndex((byte)(viewNumber - 1u))]}", LogLevel.Warning);
            Log($"Initialize: height={context.Block.Index} view={viewNumber} index={context.MyIndex} role={(context.IsPrimary ? "Primary" : context.WatchOnly ? "WatchOnly" : "Backup")}");
            if (context.WatchOnly) return;
            if (context.IsPrimary)
            {
                if (isRecovering)
                {
                    ChangeTimer(TimeSpan.FromMilliseconds(neoSystem.Settings.MillisecondsPerBlock << (viewNumber + 1)));
                }
                else
                {
                    TimeSpan span = neoSystem.Settings.TimePerBlock;

                    if (block_received_index + 1 == context.Block.Index && onPrepareBlockIndex + 1 == context.Block.Index)
                    {
                        // Include the consensus time into the consensus intervals.
                        var diff = TimeProvider.Current.UtcNow - onPrepareReceivedTime;
                        if (diff >= span)
                            span = TimeSpan.Zero;
                        else
                            span -= diff;
                    }
                    ChangeTimer(span);
                }
            }
            else
            {
                ChangeTimer(TimeSpan.FromMilliseconds(neoSystem.Settings.MillisecondsPerBlock << (viewNumber + 1)));
            }
        }

        protected override void OnReceive(object message)
        {
            if (message is Start)
            {
                if (started) return;
                OnStart();
            }
            else
            {
                if (!started) return;
                switch (message)
                {
                    case Timer timer:
                        OnTimer(timer);
                        break;
                    case Transaction transaction:
                        OnTransaction(transaction);
                        break;
                    case PersistCompleted completed:
                        OnPersistCompleted(completed.Block);
                        break;
                    case RelayResult rr:
                        if (rr.Result == VerifyResult.Succeed && rr.Inventory is ExtensiblePayload payload && payload.Category == "dBFT")
                            OnConsensusPayload(payload);
                        break;
                }
            }
        }

        private void OnStart()
        {
            Log("OnStart");
            started = true;
            if (!dbftSettings.IgnoreRecoveryLogs && context.Load())
            {
                if (context.Transactions != null)
                {
                    blockchain.Ask<FillCompleted>(new FillMemoryPool
                    {
                        Transactions = context.Transactions.Values
                    }).Wait();
                }
                if (context.CommitSent)
                {
                    CheckPreparations();
                    return;
                }
            }
            InitializeConsensus(context.ViewNumber);
            // Issue a recovery request on start-up in order to possibly catch up with other nodes
            if (!context.WatchOnly)
                RequestRecovery();
        }

        private void OnTimer(Timer timer)
        {
            if (context.WatchOnly || context.BlockSent) return;
            if (timer.Height != context.Block.Index || timer.ViewNumber != context.ViewNumber) return;
            if (context.IsPrimary && !context.RequestSentOrReceived)
            {
                SendPrepareRequest();
            }
            else if ((context.IsPrimary && context.RequestSentOrReceived) || context.IsBackup)
            {
                if (context.CommitSent)
                {
                    // Re-send commit periodically by sending recover message in case of a network issue.
                    Log($"Sending {nameof(RecoveryMessage)} to resend {nameof(Commit)}");
                    localNode.Tell(new LocalNode.SendDirectly { Inventory = context.MakeRecoveryMessage() });
                    ChangeTimer(TimeSpan.FromMilliseconds(neoSystem.Settings.MillisecondsPerBlock << 1));
                }
                else
                {
                    var reason = ChangeViewReason.Timeout;

                    if (context.Block != null && context.TransactionHashes?.Length > context.Transactions?.Count)
                    {
                        reason = ChangeViewReason.TxNotFound;
                    }

                    RequestChangeView(reason);
                }
            }
        }

        private void SendPrepareRequest()
        {
            Log($"Sending {nameof(PrepareRequest)}: height={context.Block.Index} view={context.ViewNumber}");
            localNode.Tell(new LocalNode.SendDirectly { Inventory = context.MakePrepareRequest() });

            if (context.Validators.Length == 1)
                CheckPreparations();

            if (context.TransactionHashes.Length > 0)
            {
                foreach (InvPayload payload in InvPayload.CreateGroup(InventoryType.TX, context.TransactionHashes))
                    localNode.Tell(Message.Create(MessageCommand.Inv, payload));
            }
            ChangeTimer(TimeSpan.FromMilliseconds((neoSystem.Settings.MillisecondsPerBlock << (context.ViewNumber + 1)) - (context.ViewNumber == 0 ? neoSystem.Settings.MillisecondsPerBlock : 0)));
        }

        private void RequestRecovery()
        {
            Log($"Sending {nameof(RecoveryRequest)}: height={context.Block.Index} view={context.ViewNumber} nc={context.CountCommitted} nf={context.CountFailed}");
            localNode.Tell(new LocalNode.SendDirectly { Inventory = context.MakeRecoveryRequest() });
        }

        private void RequestChangeView(ChangeViewReason reason)
        {
            if (context.WatchOnly) return;
            // Request for next view is always one view more than the current context.ViewNumber
            // Nodes will not contribute for changing to a view higher than (context.ViewNumber+1), unless they are recovered
            // The latter may happen by nodes in higher views with, at least, `M` proofs
            byte expectedView = context.ViewNumber;
            expectedView++;
            ChangeTimer(TimeSpan.FromMilliseconds(neoSystem.Settings.MillisecondsPerBlock << (expectedView + 1)));
            if ((context.CountCommitted + context.CountFailed) > context.F)
            {
                RequestRecovery();
            }
            else
            {
                Log($"Sending {nameof(ChangeView)}: height={context.Block.Index} view={context.ViewNumber} nv={expectedView} nc={context.CountCommitted} nf={context.CountFailed} reason={reason}");
                localNode.Tell(new LocalNode.SendDirectly { Inventory = context.MakeChangeView(reason) });
                CheckExpectedView(expectedView);
            }
        }

        private bool ReverifyAndProcessPayload(ExtensiblePayload payload)
        {
            RelayResult relayResult = blockchain.Ask<RelayResult>(new Reverify { Inventories = new IInventory[] { payload } }).Result;
            if (relayResult.Result != VerifyResult.Succeed) return false;
            OnConsensusPayload(payload);
            return true;
        }

        private void OnTransaction(Transaction transaction)
        {
            if (!context.IsBackup || context.NotAcceptingPayloadsDueToViewChanging || !context.RequestSentOrReceived || context.ResponseSent || context.BlockSent)
                return;
            if (context.Transactions.ContainsKey(transaction.Hash)) return;
            if (!context.TransactionHashes.Contains(transaction.Hash)) return;
            AddTransaction(transaction, true);
        }

        private bool AddTransaction(Transaction tx, bool verify)
        {
            if (verify)
            {
                // At this step we're sure that there's no on-chain transaction that conflicts with
                // the provided tx because of the previous Blockchain's OnReceive check. Thus, we only
                // need to check that current context doesn't contain conflicting transactions.
                VerifyResult result;

                // Firstly, check whether tx has Conlicts attribute with the hash of one of the context's transactions.
                foreach (var h in tx.GetAttributes<Conflicts>().Select(attr => attr.Hash))
                {
                    if (context.TransactionHashes.Contains(h))
                    {
                        result = VerifyResult.HasConflicts;
                        Log($"Rejected tx: {tx.Hash}, {result}{Environment.NewLine}{tx.ToArray().ToHexString()}", LogLevel.Warning);
                        RequestChangeView(ChangeViewReason.TxInvalid);
                        return false;
                    }
                }
                // After that, check whether context's transactions have Conflicts attribute with tx's hash.
                foreach (var pooledTx in context.Transactions.Values)
                {
                    if (pooledTx.GetAttributes<Conflicts>().Select(attr => attr.Hash).Contains(tx.Hash))
                    {
                        result = VerifyResult.HasConflicts;
                        Log($"Rejected tx: {tx.Hash}, {result}{Environment.NewLine}{tx.ToArray().ToHexString()}", LogLevel.Warning);
                        RequestChangeView(ChangeViewReason.TxInvalid);
                        return false;
                    }
                }

                // We've ensured that there's no conlicting transactions in the context, thus, can safely provide an empty conflicting list
                // for futher verification.
                var conflictingTxs = new List<Transaction>();
                result = tx.Verify(neoSystem.Settings, context.Snapshot, context.VerificationContext, conflictingTxs);
                if (result != VerifyResult.Succeed)
                {
                    Log($"Rejected tx: {tx.Hash}, {result}{Environment.NewLine}{tx.ToArray().ToHexString()}", LogLevel.Warning);
                    RequestChangeView(result == VerifyResult.PolicyFail ? ChangeViewReason.TxRejectedByPolicy : ChangeViewReason.TxInvalid);
                    return false;
                }
            }
            context.Transactions[tx.Hash] = tx;
            context.VerificationContext.AddTransaction(tx);
            return CheckPrepareResponse();
        }

        private void ChangeTimer(TimeSpan delay)
        {
            clock_started = TimeProvider.Current.UtcNow;
            expected_delay = delay;
            timer_token.CancelIfNotNull();
            timer_token = Context.System.Scheduler.ScheduleTellOnceCancelable(delay, Self, new Timer
            {
                Height = context.Block.Index,
                ViewNumber = context.ViewNumber
            }, ActorRefs.NoSender);
        }

        // this function increases existing timer (never decreases) with a value proportional to `maxDelayInBlockTimes`*`Blockchain.MillisecondsPerBlock`
        private void ExtendTimerByFactor(int maxDelayInBlockTimes)
        {
            TimeSpan nextDelay = expected_delay - (TimeProvider.Current.UtcNow - clock_started) + TimeSpan.FromMilliseconds(maxDelayInBlockTimes * neoSystem.Settings.MillisecondsPerBlock / (double)context.M);
            if (!context.WatchOnly && !context.ViewChanging && !context.CommitSent && (nextDelay > TimeSpan.Zero))
                ChangeTimer(nextDelay);
        }

        protected override void PostStop()
        {
            Log("OnStop");
            started = false;
            Context.System.EventStream.Unsubscribe(Self);
            context.Dispose();
            base.PostStop();
        }

        public static Props Props(NeoSystem neoSystem, Settings dbftSettings, Wallet wallet)
        {
            return Akka.Actor.Props.Create(() => new ConsensusService(neoSystem, dbftSettings, wallet));
        }

        private static void Log(string message, LogLevel level = LogLevel.Info)
        {
            Utility.Log(nameof(ConsensusService), level, message);
        }
    }
}
