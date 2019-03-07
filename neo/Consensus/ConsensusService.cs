using Akka.Actor;
using Akka.Configuration;
using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Actors;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Consensus
{
    public sealed class ConsensusService : UntypedActor
    {
        public class Start { public bool IgnoreRecoveryLogs; }
        public class SetViewNumber { public byte ViewNumber; }
        internal class Timer { public uint Height; public byte ViewNumber; }

        private readonly IConsensusContext context;
        private readonly IActorRef localNode;
        private readonly IActorRef taskManager;
        private readonly Store store;
        private ICancelable timer_token;
        private DateTime block_received_time;
        private bool started = false;
        /// <summary>
        /// This will be cleared every block (so it will not grow out of control, but is used to prevent repeatedly
        /// responding to the same message.
        /// </summary>
        private readonly HashSet<UInt256> knownHashes = new HashSet<UInt256>();
        private bool isRecovering = false;

        public ConsensusService(IActorRef localNode, IActorRef taskManager, Store store, Wallet wallet)
            : this(localNode, taskManager, store, new ConsensusContext(wallet))
        {
        }

        public ConsensusService(IActorRef localNode, IActorRef taskManager, Store store, IConsensusContext context)
        {
            this.localNode = localNode;
            this.taskManager = taskManager;
            this.store = store;
            this.context = context;
            Context.System.EventStream.Subscribe(Self, typeof(Blockchain.PersistCompleted));
        }

        private bool AddTransaction(Transaction tx, bool verify)
        {
            if (verify && !tx.Verify(context.Snapshot, context.Transactions.Values))
            {
                Log($"Invalid transaction: {tx.Hash}{Environment.NewLine}{tx.ToArray().ToHexString()}", LogLevel.Warning);
                RequestChangeView();
                return false;
            }
            if (!Plugin.CheckPolicy(tx))
            {
                Log($"reject tx: {tx.Hash}{Environment.NewLine}{tx.ToArray().ToHexString()}", LogLevel.Warning);
                RequestChangeView();
                return false;
            }
            context.Transactions[tx.Hash] = tx;
            if (context.TransactionHashes.Length == context.Transactions.Count)
            {
                if (VerifyRequest())
                {
                    // if we are the primary for this view, but acting as a backup because we recovered our own
                    // previously sent prepare request, then we don't want to send a prepare response.
                    if (context.MyIndex == context.PrimaryIndex) return true;

                    Log($"send prepare response");
                    localNode.Tell(new LocalNode.SendDirectly { Inventory = context.MakePrepareResponse() });
                    CheckPreparations();
                }
                else
                {
                    RequestChangeView();
                    return false;
                }
            }
            return true;
        }

        private void ChangeTimer(TimeSpan delay)
        {
            timer_token.CancelIfNotNull();
            timer_token = Context.System.Scheduler.ScheduleTellOnceCancelable(delay, Self, new Timer
            {
                Height = context.BlockIndex,
                ViewNumber = context.ViewNumber
            }, ActorRefs.NoSender);
        }

        private void CheckCommits()
        {
            if (context.CommitPayloads.Count(p => p != null) >= context.M() && context.TransactionHashes.All(p => context.Transactions.ContainsKey(p)))
            {
                Block block = context.CreateBlock();
                Log($"relay block: {block.Hash}");
                localNode.Tell(new LocalNode.Relay { Inventory = block });
            }
        }

        private void CheckExpectedView(byte viewNumber)
        {
            if (context.ViewNumber == viewNumber) return;
            if (context.ChangeViewPayloads.Count(p => p != null && p.GetDeserializedMessage<ChangeView>().NewViewNumber == viewNumber) >= context.M())
            {
                ChangeView message = context.ChangeViewPayloads[context.MyIndex]?.GetDeserializedMessage<ChangeView>();
                if (message is null || message.NewViewNumber < viewNumber)
                    localNode.Tell(new LocalNode.SendDirectly { Inventory = context.MakeChangeView(viewNumber) });
                InitializeConsensus(viewNumber);
            }
        }

        private void CheckPreparations()
        {
            if (context.PreparationPayloads.Count(p => p != null) >= context.M() && context.TransactionHashes.All(p => context.Transactions.ContainsKey(p)))
            {
                ConsensusPayload payload = context.MakeCommit();
                Log($"send commit");
                context.Save(store);
                localNode.Tell(new LocalNode.SendDirectly { Inventory = payload });
                // Set timer, so we will resend the commit in case of a networking issue
                ChangeTimer(TimeSpan.FromSeconds(Blockchain.SecondsPerBlock));
                CheckCommits();
            }
        }

        private byte GetLastExpectedView(int validatorIndex)
        {
            var lastPreparationPayload = context.PreparationPayloads[validatorIndex];
            if (lastPreparationPayload != null)
                return lastPreparationPayload.GetDeserializedMessage<ConsensusMessage>().ViewNumber;

            return context.ChangeViewPayloads[validatorIndex]?.GetDeserializedMessage<ChangeView>().NewViewNumber ?? (byte)0;
        }

        private void InitializeConsensus(byte viewNumber)
        {
            context.Reset(viewNumber);
            if (context.MyIndex < 0) return;
            if (viewNumber > 0)
                Log($"changeview: view={viewNumber} primary={context.Validators[context.GetPrimaryIndex((byte)(viewNumber - 1u))]}", LogLevel.Warning);
            Log($"initialize: height={context.BlockIndex} view={viewNumber} index={context.MyIndex} role={(context.IsPrimary() ? "Primary" : "Backup")}");
            if (context.IsPrimary())
            {
                if (isRecovering)
                {
                    ChangeTimer(TimeSpan.FromSeconds(Blockchain.SecondsPerBlock << (viewNumber + 1)));
                }
                else
                {
                    TimeSpan span = TimeProvider.Current.UtcNow - block_received_time;
                    if (span >= Blockchain.TimePerBlock)
                        ChangeTimer(TimeSpan.Zero);
                    else
                        ChangeTimer(Blockchain.TimePerBlock - span);
                }
            }
            else
            {
                ChangeTimer(TimeSpan.FromSeconds(Blockchain.SecondsPerBlock << (viewNumber + 1)));
            }
        }

        private void Log(string message, LogLevel level = LogLevel.Info)
        {
            Plugin.Log(nameof(ConsensusService), level, message);
        }

        private void OnChangeViewReceived(ConsensusPayload payload, ChangeView message)
        {
            // We keep track of the payload hashes received in this block, and don't respond with recovery
            // in response to the same payload that we already responded to previously.
            // ChangeView messages include a Timestamp when the change view is sent, thus if a node restarts
            // and issues a change view for the same view, it will have a different hash and will correctly respond
            // again; however replay attacks of the ChangeView message from arbitrary nodes will not trigger an
            // additonal recovery message response.
            if (!knownHashes.Add(payload.Hash)) return;
            if (message.NewViewNumber <= context.ViewNumber)
            {
                bool shouldSendRecovery = false;
                // Limit recovery to sending from `f` nodes when the request is from a lower view number.
                int allowedRecoveryNodeCount = context.F();
                for (int i = 0; i < allowedRecoveryNodeCount; i++)
                {
                    var eligibleResponders = context.Validators.Length - 1;
                    var chosenIndex = (payload.ValidatorIndex + i + message.NewViewNumber) % eligibleResponders;
                    if (chosenIndex >= payload.ValidatorIndex) chosenIndex++;
                    if (chosenIndex != context.MyIndex) continue;
                    shouldSendRecovery = true;
                    break;
                }

                if (!shouldSendRecovery) return;

                Log($"send recovery from view: {message.ViewNumber} to view: {context.ViewNumber}");
                localNode.Tell(new LocalNode.SendDirectly { Inventory = context.MakeRecoveryMessage() });
            }

            var expectedView = GetLastExpectedView(payload.ValidatorIndex);
            if (message.NewViewNumber <= expectedView)
                return;

            Log($"{nameof(OnChangeViewReceived)}: height={payload.BlockIndex} view={message.ViewNumber} index={payload.ValidatorIndex} nv={message.NewViewNumber}");
            context.ChangeViewPayloads[payload.ValidatorIndex] = payload;
            CheckExpectedView(message.NewViewNumber);
        }

        private void OnCommitReceived(ConsensusPayload payload, Commit commit)
        {
            if (context.CommitPayloads[payload.ValidatorIndex] != null) return;
            Log($"{nameof(OnCommitReceived)}: height={payload.BlockIndex} view={commit.ViewNumber} index={payload.ValidatorIndex}");
            byte[] hashData = context.MakeHeader()?.GetHashData();
            if (hashData == null)
            {
                context.CommitPayloads[payload.ValidatorIndex] = payload;
            }
            else if (Crypto.Default.VerifySignature(hashData, commit.Signature, context.Validators[payload.ValidatorIndex].EncodePoint(false)))
            {
                context.CommitPayloads[payload.ValidatorIndex] = payload;
                CheckCommits();
            }
        }

        private void OnConsensusPayload(ConsensusPayload payload)
        {
            if (context.BlockSent()) return;
            if (payload.Version != ConsensusContext.Version) return;
            if (payload.PrevHash != context.PrevHash || payload.BlockIndex != context.BlockIndex)
            {
                if (context.BlockIndex < payload.BlockIndex)
                {
                    Log($"chain sync: expected={payload.BlockIndex} current={context.BlockIndex - 1} nodes={LocalNode.Singleton.ConnectedCount}", LogLevel.Warning);
                }
                return;
            }
            if (payload.ValidatorIndex >= context.Validators.Length) return;
            ConsensusMessage message = payload.ConsensusMessage;
            if (message.ViewNumber != context.ViewNumber && message.Type != ConsensusMessageType.ChangeView &&
                                                            message.Type != ConsensusMessageType.RecoveryMessage)
                return;
            switch (message)
            {
                case ChangeView view:
                    OnChangeViewReceived(payload, view);
                    break;
                case PrepareRequest request:
                    OnPrepareRequestReceived(payload, request);
                    break;
                case PrepareResponse response:
                    OnPrepareResponseReceived(payload, response);
                    break;
                case Commit commit:
                    OnCommitReceived(payload, commit);
                    break;
                case RecoveryMessage recovery:
                    OnRecoveryMessageReceived(payload, recovery);
                    break;
            }
        }

        private void OnPersistCompleted(Block block)
        {
            Log($"persist block: {block.Hash}");
            block_received_time = TimeProvider.Current.UtcNow;
            knownHashes.Clear();
            InitializeConsensus(0);
        }

        private void OnRecoveryMessageReceived(ConsensusPayload payload, RecoveryMessage message)
        {
            if (message.ViewNumber < context.ViewNumber) return;
            Log($"{nameof(OnRecoveryMessageReceived)}: height={payload.BlockIndex} view={message.ViewNumber} index={payload.ValidatorIndex}");
            isRecovering = true;
            try
            {
                if (message.ViewNumber > context.ViewNumber)
                {
                    if (context.CommitSent()) return;
                    ConsensusPayload[] changeViewPayloads = message.GetChangeViewPayloads(context, payload);
                    foreach (ConsensusPayload changeViewPayload in changeViewPayloads)
                        ReverifyAndProcessPayload(changeViewPayload);
                }
                if (message.ViewNumber != context.ViewNumber) return;
                if (!context.CommitSent())
                {
                    if (!context.RequestSentOrReceived())
                    {
                        ConsensusPayload prepareRequestPayload = message.GetPrepareRequestPayload(context, payload);
                        if (prepareRequestPayload != null)
                            ReverifyAndProcessPayload(prepareRequestPayload);
                        else if (context.IsPrimary())
                            SendPrepareRequest();
                    }
                    ConsensusPayload[] prepareResponsePayloads = message.GetPrepareResponsePayloads(context, payload);
                    foreach (ConsensusPayload prepareResponsePayload in prepareResponsePayloads)
                        ReverifyAndProcessPayload(prepareResponsePayload);
                }
                ConsensusPayload[] commitPayloads = message.GetCommitPayloadsFromRecoveryMessage(context, payload);
                foreach (ConsensusPayload commitPayload in commitPayloads)
                    ReverifyAndProcessPayload(commitPayload);
            }
            finally
            {
                isRecovering = false;
            }
        }

        private void OnPrepareRequestReceived(ConsensusPayload payload, PrepareRequest message)
        {
            if (context.RequestSentOrReceived()) return;
            if (payload.ValidatorIndex != context.PrimaryIndex) return;
            Log($"{nameof(OnPrepareRequestReceived)}: height={payload.BlockIndex} view={message.ViewNumber} index={payload.ValidatorIndex} tx={message.TransactionHashes.Length}");
            if (message.Timestamp <= context.PrevHeader().Timestamp || message.Timestamp > TimeProvider.Current.UtcNow.AddMinutes(10).ToTimestamp())
            {
                Log($"Timestamp incorrect: {message.Timestamp}", LogLevel.Warning);
                return;
            }
            if (message.TransactionHashes.Any(p => context.Snapshot.ContainsTransaction(p)))
            {
                Log($"Invalid request: transaction already exists", LogLevel.Warning);
                return;
            }
            context.Timestamp = message.Timestamp;
            context.Nonce = message.Nonce;
            context.NextConsensus = message.NextConsensus;
            context.TransactionHashes = message.TransactionHashes;
            context.Transactions = new Dictionary<UInt256, Transaction>();
            for (int i = 0; i < context.PreparationPayloads.Length; i++)
                if (context.PreparationPayloads[i] != null)
                    if (!context.PreparationPayloads[i].GetDeserializedMessage<PrepareResponse>().PreparationHash.Equals(payload.Hash))
                        context.PreparationPayloads[i] = null;
            context.PreparationPayloads[payload.ValidatorIndex] = payload;
            byte[] hashData = context.MakeHeader().GetHashData();
            for (int i = 0; i < context.CommitPayloads.Length; i++)
                if (context.CommitPayloads[i] != null)
                    if (!Crypto.Default.VerifySignature(hashData, context.CommitPayloads[i].GetDeserializedMessage<Commit>().Signature, context.Validators[i].EncodePoint(false)))
                        context.CommitPayloads[i] = null;
            Dictionary<UInt256, Transaction> mempoolVerified = Blockchain.Singleton.MemPool.GetVerifiedTransactions().ToDictionary(p => p.Hash);

            List<Transaction> unverified = new List<Transaction>();
            foreach (UInt256 hash in context.TransactionHashes.Skip(1))
            {
                if (mempoolVerified.TryGetValue(hash, out Transaction tx))
                {
                    if (!AddTransaction(tx, false))
                        return;
                }
                else
                {
                    if (Blockchain.Singleton.MemPool.TryGetValue(hash, out tx))
                        unverified.Add(tx);
                }
            }
            foreach (Transaction tx in unverified)
                if (!AddTransaction(tx, true))
                    return;
            if (!AddTransaction(message.MinerTransaction, true)) return;
            if (context.Transactions.Count < context.TransactionHashes.Length)
            {
                UInt256[] hashes = context.TransactionHashes.Where(i => !context.Transactions.ContainsKey(i)).ToArray();
                taskManager.Tell(new TaskManager.RestartTasks
                {
                    Payload = InvPayload.Create(InventoryType.TX, hashes)
                });
            }
        }

        private void OnPrepareResponseReceived(ConsensusPayload payload, PrepareResponse message)
        {
            if (context.PreparationPayloads[payload.ValidatorIndex] != null) return;
            if (context.PreparationPayloads[context.PrimaryIndex] != null && !message.PreparationHash.Equals(context.PreparationPayloads[context.PrimaryIndex].Hash))
                return;
            Log($"{nameof(OnPrepareResponseReceived)}: height={payload.BlockIndex} view={message.ViewNumber} index={payload.ValidatorIndex}");
            context.PreparationPayloads[payload.ValidatorIndex] = payload;
            if (context.CommitSent()) return;
            if (context.RequestSentOrReceived())
                CheckPreparations();
        }

        protected override void OnReceive(object message)
        {
            if (message is Start options)
            {
                if (started) return;
                OnStart(options);
            }
            else
            {
                if (!started) return;
                switch (message)
                {
                    case SetViewNumber setView:
                        InitializeConsensus(setView.ViewNumber);
                        break;
                    case Timer timer:
                        OnTimer(timer);
                        break;
                    case ConsensusPayload payload:
                        OnConsensusPayload(payload);
                        break;
                    case Transaction transaction:
                        OnTransaction(transaction);
                        break;
                    case Blockchain.PersistCompleted completed:
                        OnPersistCompleted(completed.Block);
                        break;
                }
            }
        }

        private void OnStart(Start options)
        {
            Log("OnStart");
            started = true;
            if (!options.IgnoreRecoveryLogs && context.Load(store))
            {
                if (context.Transactions != null)
                {
                    Sender.Ask<Blockchain.FillCompleted>(new Blockchain.FillMemoryPool
                    {
                        Transactions = context.Transactions.Values
                    }).Wait();
                }
                if (context.CommitSent())
                {
                    CheckPreparations();
                    return;
                }
            }
            InitializeConsensus(0);
            // Issue a ChangeView with NewViewNumber of 0 to request recovery messages on start-up.
            if (context.BlockIndex == Blockchain.Singleton.HeaderHeight + 1)
                localNode.Tell(new LocalNode.SendDirectly { Inventory = context.MakeChangeView(0) });
        }

        private void OnTimer(Timer timer)
        {
            if (context.BlockSent()) return;
            if (timer.Height != context.BlockIndex || timer.ViewNumber != context.ViewNumber) return;
            Log($"timeout: height={timer.Height} view={timer.ViewNumber}");
            if (context.IsPrimary() && !context.RequestSentOrReceived())
            {
                SendPrepareRequest();
            }
            else if ((context.IsPrimary() && context.RequestSentOrReceived()) || context.IsBackup())
            {
                if (context.CommitSent())
                {
                    // Re-send commit periodically by sending recover message in case of a network issue.
                    Log($"send recovery to resend commit");
                    localNode.Tell(new LocalNode.SendDirectly { Inventory = context.MakeRecoveryMessage() });
                    ChangeTimer(TimeSpan.FromSeconds(Blockchain.SecondsPerBlock << 1));
                }
                else
                {
                    RequestChangeView();
                }
            }
        }

        private void OnTransaction(Transaction transaction)
        {
            if (transaction.Type == TransactionType.MinerTransaction) return;
            if (!context.IsBackup() || !context.RequestSentOrReceived() || context.ResponseSent() || context.BlockSent())
                return;
            // If we are changing view but we already have enough preparation payloads to commit in the current view,
            // we must keep on accepting transactions in the current view to be able to create the block.
            if (context.ViewChanging() &&
                context.PreparationPayloads.Count(p => p != null) < context.M()) return;
            if (context.Transactions.ContainsKey(transaction.Hash)) return;
            if (!context.TransactionHashes.Contains(transaction.Hash)) return;
            AddTransaction(transaction, true);
        }

        protected override void PostStop()
        {
            Log("OnStop");
            started = false;
            Context.System.EventStream.Unsubscribe(Self);
            context.Dispose();
            base.PostStop();
        }

        public static Props Props(IActorRef localNode, IActorRef taskManager, Store store, Wallet wallet)
        {
            return Akka.Actor.Props.Create(() => new ConsensusService(localNode, taskManager, store, wallet)).WithMailbox("consensus-service-mailbox");
        }

        private void RequestChangeView()
        {
            byte expectedView = context.ChangeViewPayloads[context.MyIndex]?.GetDeserializedMessage<ChangeView>().NewViewNumber ?? 0;
            if (expectedView < context.ViewNumber) expectedView = context.ViewNumber;
            expectedView++;
            Log($"request change view: height={context.BlockIndex} view={context.ViewNumber} nv={expectedView}");
            ChangeTimer(TimeSpan.FromSeconds(Blockchain.SecondsPerBlock << (expectedView + 1)));
            localNode.Tell(new LocalNode.SendDirectly { Inventory = context.MakeChangeView(expectedView) });
            CheckExpectedView(expectedView);
        }

        private void ReverifyAndProcessPayload(ConsensusPayload payload)
        {
            if (!payload.Verify(context.Snapshot)) return;
            OnConsensusPayload(payload);
        }

        private void SendPrepareRequest()
        {
            Log($"send prepare request: height={context.BlockIndex} view={context.ViewNumber}");
            localNode.Tell(new LocalNode.SendDirectly { Inventory = context.MakePrepareRequest() });

            if (context.TransactionHashes.Length > 1)
            {
                foreach (InvPayload payload in InvPayload.CreateGroup(InventoryType.TX, context.TransactionHashes.Skip(1).ToArray()))
                    localNode.Tell(Message.Create("inv", payload));
            }
            ChangeTimer(TimeSpan.FromSeconds(Blockchain.SecondsPerBlock << (context.ViewNumber + 1)));
        }

        private bool VerifyRequest()
        {
            if (!Blockchain.GetConsensusAddress(context.Snapshot.GetValidators(context.Transactions.Values).ToArray()).Equals(context.NextConsensus))
                return false;
            Transaction minerTx = context.Transactions.Values.FirstOrDefault(p => p.Type == TransactionType.MinerTransaction);
            Fixed8 amountNetFee = Block.CalculateNetFee(context.Transactions.Values);
            if (minerTx?.Outputs.Sum(p => p.Value) != amountNetFee) return false;
            return true;
        }
    }

    internal class ConsensusServiceMailbox : PriorityMailbox
    {
        public ConsensusServiceMailbox(Akka.Actor.Settings settings, Config config)
            : base(settings, config)
        {
        }

        protected override bool IsHighPriority(object message)
        {
            switch (message)
            {
                case ConsensusPayload _:
                case ConsensusService.SetViewNumber _:
                case ConsensusService.Timer _:
                case Blockchain.PersistCompleted _:
                    return true;
                default:
                    return false;
            }
        }
    }
}
