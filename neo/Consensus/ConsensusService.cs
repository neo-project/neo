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
using System.IO;
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
        private ICancelable timer_token;
        private DateTime block_received_time;
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
        private readonly HashSet<UInt256> knownHashes = new HashSet<UInt256>();
        /// <summary>
        /// This variable is only true during OnRecoveryMessageReceived
        /// </summary>
        private bool isRecovering = false;

        public ConsensusService(IActorRef localNode, IActorRef taskManager, Store store, Wallet wallet)
            : this(localNode, taskManager, new ConsensusContext(wallet, store))
        {
        }

        public ConsensusService(IActorRef localNode, IActorRef taskManager, IConsensusContext context)
        {
            this.localNode = localNode;
            this.taskManager = taskManager;
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
                    if (context.IsPrimary() || context.WatchOnly()) return true;

                    // Timeout extension due to prepare response sent
                    // around 2*15/M=30.0/5 ~ 40% block time (for M=5)
                    ExtendTimerByFactor(2);

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
            clock_started = TimeProvider.Current.UtcNow;
            expected_delay = delay;
            timer_token.CancelIfNotNull();
            timer_token = Context.System.Scheduler.ScheduleTellOnceCancelable(delay, Self, new Timer
            {
                Height = context.BlockIndex,
                ViewNumber = context.ViewNumber
            }, ActorRefs.NoSender);
        }

        private void CheckCommits()
        {
            if (context.CommitPayloads.Count(p => p?.ConsensusMessage.ViewNumber == context.ViewNumber) >= context.M() && context.TransactionHashes.All(p => context.Transactions.ContainsKey(p)))
            {
                Block block = context.CreateBlock();
                Log($"relay block: height={block.Index} hash={block.Hash} tx={block.Transactions.Length}");
                localNode.Tell(new LocalNode.Relay { Inventory = block });
            }
        }

        private void CheckExpectedView(byte viewNumber)
        {
            if (context.ViewNumber >= viewNumber) return;
            // if there are `M` change view payloads with NewViewNumber greater than viewNumber, then, it is safe to move
            if (context.ChangeViewPayloads.Count(p => p != null && p.GetDeserializedMessage<ChangeView>().NewViewNumber >= viewNumber) >= context.M())
            {
                if (!context.WatchOnly())
                {
                    ChangeView message = context.ChangeViewPayloads[context.MyIndex]?.GetDeserializedMessage<ChangeView>();
                    // Communicate the network about my agreement to move to `viewNumber`
                    // if my last change view payload, `message`, has NewViewNumber lower than current view to change
                    if (message is null || message.NewViewNumber < viewNumber)
                        localNode.Tell(new LocalNode.SendDirectly { Inventory = context.MakeChangeView() });
                }
                InitializeConsensus(viewNumber);
            }
        }

        private void CheckPreparations()
        {
            if (context.PreparationPayloads.Count(p => p != null) >= context.M() && context.TransactionHashes.All(p => context.Transactions.ContainsKey(p)))
            {
                ConsensusPayload payload = context.MakeCommit();
                Log($"send commit");
                context.Save();
                localNode.Tell(new LocalNode.SendDirectly { Inventory = payload });
                // Set timer, so we will resend the commit in case of a networking issue
                ChangeTimer(TimeSpan.FromSeconds(Blockchain.SecondsPerBlock));
                CheckCommits();
            }
        }

        private void InitializeConsensus(byte viewNumber)
        {
            context.Reset(viewNumber);
            if (viewNumber > 0)
                Log($"changeview: view={viewNumber} primary={context.Validators[context.GetPrimaryIndex((byte)(viewNumber - 1u))]}", LogLevel.Warning);
            Log($"initialize: height={context.BlockIndex} view={viewNumber} index={context.MyIndex} role={(context.IsPrimary() ? "Primary" : context.WatchOnly() ? "WatchOnly" : "Backup")}");
            if (context.WatchOnly()) return;
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
            if (message.NewViewNumber <= context.ViewNumber)
                OnRecoveryRequestReceived(payload);

            if (context.CommitSent()) return;

            var expectedView = context.ChangeViewPayloads[payload.ValidatorIndex]?.GetDeserializedMessage<ChangeView>().NewViewNumber ?? (byte)0;
            if (message.NewViewNumber <= expectedView)
                return;

            Log($"{nameof(OnChangeViewReceived)}: height={payload.BlockIndex} view={message.ViewNumber} index={payload.ValidatorIndex} nv={message.NewViewNumber}");
            context.ChangeViewPayloads[payload.ValidatorIndex] = payload;
            CheckExpectedView(message.NewViewNumber);
        }

        private void OnCommitReceived(ConsensusPayload payload, Commit commit)
        {
            ref ConsensusPayload existingCommitPayload = ref context.CommitPayloads[payload.ValidatorIndex];
            if (existingCommitPayload != null)
            {
                if (existingCommitPayload.Hash != payload.Hash)
                    Log($"{nameof(OnCommitReceived)}: different commit from validator! height={payload.BlockIndex} index={payload.ValidatorIndex} view={commit.ViewNumber} existingView={existingCommitPayload.ConsensusMessage.ViewNumber}", LogLevel.Warning);
                return;
            }

            // Timeout extension: commit has been received with success
            // around 4*15s/M=60.0s/5=12.0s ~ 80% block time (for M=5)
            ExtendTimerByFactor(4);

            if (commit.ViewNumber == context.ViewNumber)
            {
                Log($"{nameof(OnCommitReceived)}: height={payload.BlockIndex} view={commit.ViewNumber} index={payload.ValidatorIndex} nc={context.CountCommitted()} nf={context.CountFailed()}");

                byte[] hashData = context.MakeHeader()?.GetHashData();
                if (hashData == null)
                {
                    existingCommitPayload = payload;
                }
                else if (Crypto.Default.VerifySignature(hashData, commit.Signature,
                    context.Validators[payload.ValidatorIndex].EncodePoint(false)))
                {
                    existingCommitPayload = payload;
                    CheckCommits();
                }
                return;
            }
            // Receiving commit from another view
            Log($"{nameof(OnCommitReceived)}: record commit for different view={commit.ViewNumber} index={payload.ValidatorIndex} height={payload.BlockIndex}");
            existingCommitPayload = payload;
        }

        // this function increases existing timer (never decreases) with a value proportional to `maxDelayInBlockTimes`*`Blockchain.SecondsPerBlock`
        private void ExtendTimerByFactor(int maxDelayInBlockTimes)
        {
            TimeSpan nextDelay = expected_delay - (TimeProvider.Current.UtcNow - clock_started) + TimeSpan.FromMilliseconds(maxDelayInBlockTimes * Blockchain.SecondsPerBlock * 1000.0 / context.M());
            if (!context.WatchOnly() && !context.ViewChanging() && !context.CommitSent() && (nextDelay > TimeSpan.Zero))
                ChangeTimer(nextDelay);
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
            ConsensusMessage message;
            try
            {
                message = payload.ConsensusMessage;
            }
            catch (FormatException)
            {
                return;
            }
            catch (IOException)
            {
                return;
            }
            context.LastSeenMessage[context.Validators[payload.ValidatorIndex]] = (int)payload.BlockIndex;
            foreach (IP2PPlugin plugin in Plugin.P2PPlugins)
                if (!plugin.OnConsensusMessage(payload))
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
                case RecoveryRequest _:
                    OnRecoveryRequestReceived(payload);
                    break;
                case RecoveryMessage recovery:
                    OnRecoveryMessageReceived(payload, recovery);
                    break;
            }
        }

        private void OnPersistCompleted(Block block)
        {
            Log($"persist block: height={block.Index} hash={block.Hash} tx={block.Transactions.Length}");
            block_received_time = TimeProvider.Current.UtcNow;
            knownHashes.Clear();
            InitializeConsensus(0);
        }

        private void OnRecoveryMessageReceived(ConsensusPayload payload, RecoveryMessage message)
        {
            // isRecovering is always set to false again after OnRecoveryMessageReceived
            isRecovering = true;
            int validChangeViews = 0, totalChangeViews = 0, validPrepReq = 0, totalPrepReq = 0;
            int validPrepResponses = 0, totalPrepResponses = 0, validCommits = 0, totalCommits = 0;

            Log($"{nameof(OnRecoveryMessageReceived)}: height={payload.BlockIndex} view={message.ViewNumber} index={payload.ValidatorIndex}");
            try
            {
                if (message.ViewNumber > context.ViewNumber)
                {
                    if (context.CommitSent()) return;
                    ConsensusPayload[] changeViewPayloads = message.GetChangeViewPayloads(context, payload);
                    totalChangeViews = changeViewPayloads.Length;
                    foreach (ConsensusPayload changeViewPayload in changeViewPayloads)
                        if (ReverifyAndProcessPayload(changeViewPayload)) validChangeViews++;
                }
                if (message.ViewNumber == context.ViewNumber && !context.NotAcceptingPayloadsDueToViewChanging() && !context.CommitSent())
                {
                    if (!context.RequestSentOrReceived())
                    {
                        ConsensusPayload prepareRequestPayload = message.GetPrepareRequestPayload(context, payload);
                        if (prepareRequestPayload != null)
                        {
                            totalPrepReq = 1;
                            if (ReverifyAndProcessPayload(prepareRequestPayload)) validPrepReq++;
                        }
                        else if (context.IsPrimary())
                            SendPrepareRequest();
                    }
                    ConsensusPayload[] prepareResponsePayloads = message.GetPrepareResponsePayloads(context, payload);
                    totalPrepResponses = prepareResponsePayloads.Length;
                    foreach (ConsensusPayload prepareResponsePayload in prepareResponsePayloads)
                        if (ReverifyAndProcessPayload(prepareResponsePayload)) validPrepResponses++;
                }
                if (message.ViewNumber <= context.ViewNumber)
                {
                    // Ensure we know about all commits from lower view numbers.
                    ConsensusPayload[] commitPayloads = message.GetCommitPayloadsFromRecoveryMessage(context, payload);
                    totalCommits = commitPayloads.Length;
                    foreach (ConsensusPayload commitPayload in commitPayloads)
                        if (ReverifyAndProcessPayload(commitPayload)) validCommits++;
                }
            }
            finally
            {
                Log($"{nameof(OnRecoveryMessageReceived)}: finished (valid/total) " +
                    $"ChgView: {validChangeViews}/{totalChangeViews} " +
                    $"PrepReq: {validPrepReq}/{totalPrepReq} " +
                    $"PrepResp: {validPrepResponses}/{totalPrepResponses} " +
                    $"Commits: {validCommits}/{totalCommits}");
                isRecovering = false;
            }
        }

        private void OnRecoveryRequestReceived(ConsensusPayload payload)
        {
            // We keep track of the payload hashes received in this block, and don't respond with recovery
            // in response to the same payload that we already responded to previously.
            // ChangeView messages include a Timestamp when the change view is sent, thus if a node restarts
            // and issues a change view for the same view, it will have a different hash and will correctly respond
            // again; however replay attacks of the ChangeView message from arbitrary nodes will not trigger an
            // additional recovery message response.
            if (!knownHashes.Add(payload.Hash)) return;

            Log($"On{payload.ConsensusMessage.GetType().Name}Received: height={payload.BlockIndex} index={payload.ValidatorIndex} view={payload.ConsensusMessage.ViewNumber}");
            if (context.WatchOnly()) return;
            if (!context.CommitSent())
            {
                bool shouldSendRecovery = false;
                int allowedRecoveryNodeCount = context.F();
                // Limit recoveries to be sent from an upper limit of `f` nodes
                for (int i = 1; i <= allowedRecoveryNodeCount; i++)
                {
                    var chosenIndex = (payload.ValidatorIndex + i) % context.Validators.Length;
                    if (chosenIndex != context.MyIndex) continue;
                    shouldSendRecovery = true;
                    break;
                }

                if (!shouldSendRecovery) return;
            }
            Log($"send recovery: view={context.ViewNumber}");
            localNode.Tell(new LocalNode.SendDirectly { Inventory = context.MakeRecoveryMessage() });
        }

        private void OnPrepareRequestReceived(ConsensusPayload payload, PrepareRequest message)
        {
            if (context.RequestSentOrReceived() || context.NotAcceptingPayloadsDueToViewChanging()) return;
            if (payload.ValidatorIndex != context.PrimaryIndex || message.ViewNumber != context.ViewNumber) return;
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

            // Timeout extension: prepare request has been received with success
            // around 2*15/M=30.0/5 ~ 40% block time (for M=5)
            ExtendTimerByFactor(2);

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
                if (context.CommitPayloads[i]?.ConsensusMessage.ViewNumber == context.ViewNumber)
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
            if (message.ViewNumber != context.ViewNumber) return;
            if (context.PreparationPayloads[payload.ValidatorIndex] != null || context.NotAcceptingPayloadsDueToViewChanging()) return;
            if (context.PreparationPayloads[context.PrimaryIndex] != null && !message.PreparationHash.Equals(context.PreparationPayloads[context.PrimaryIndex].Hash))
                return;

            // Timeout extension: prepare response has been received with success
            // around 2*15/M=30.0/5 ~ 40% block time (for M=5)
            ExtendTimerByFactor(2);

            Log($"{nameof(OnPrepareResponseReceived)}: height={payload.BlockIndex} view={message.ViewNumber} index={payload.ValidatorIndex}");
            context.PreparationPayloads[payload.ValidatorIndex] = payload;
            if (context.WatchOnly() || context.CommitSent()) return;
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

        private void RequestRecovery()
        {
            if (context.BlockIndex == Blockchain.Singleton.HeaderHeight + 1)
                localNode.Tell(new LocalNode.SendDirectly { Inventory = context.MakeRecoveryRequest() });
        }

        private void OnStart(Start options)
        {
            Log("OnStart");
            started = true;
            if (!options.IgnoreRecoveryLogs && context.Load())
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
            if (!context.WatchOnly())
                RequestRecovery();
        }

        private void OnTimer(Timer timer)
        {
            if (context.WatchOnly() || context.BlockSent()) return;
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
            if (!context.IsBackup() || context.NotAcceptingPayloadsDueToViewChanging() || !context.RequestSentOrReceived() || context.ResponseSent() || context.BlockSent())
                return;
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
            if (context.WatchOnly()) return;
            // Request for next view is always one view more than the current context.ViewNumber
            // Nodes will not contribute for changing to a view higher than (context.ViewNumber+1), unless they are recovered
            // The latter may happen by nodes in higher views with, at least, `M` proofs
            byte expectedView = context.ViewNumber;
            expectedView++;
            ChangeTimer(TimeSpan.FromSeconds(Blockchain.SecondsPerBlock << (expectedView + 1)));
            if ((context.CountCommitted() + context.CountFailed()) > context.F())
            {
                Log($"Skip requesting change view to nv={expectedView} because nc={context.CountCommitted()} nf={context.CountFailed()}");
                RequestRecovery();
                return;
            }
            Log($"request change view: height={context.BlockIndex} view={context.ViewNumber} nv={expectedView} nc={context.CountCommitted()} nf={context.CountFailed()}");
            localNode.Tell(new LocalNode.SendDirectly { Inventory = context.MakeChangeView() });
            CheckExpectedView(expectedView);
        }

        private bool ReverifyAndProcessPayload(ConsensusPayload payload)
        {
            if (!payload.Verify(context.Snapshot)) return false;
            OnConsensusPayload(payload);
            return true;
        }

        private void SendPrepareRequest()
        {
            Log($"send prepare request: height={context.BlockIndex} view={context.ViewNumber}");
            localNode.Tell(new LocalNode.SendDirectly { Inventory = context.MakePrepareRequest() });

            if (context.Validators.Length == 1)
                CheckPreparations();

            if (context.TransactionHashes.Length > 1)
            {
                foreach (InvPayload payload in InvPayload.CreateGroup(InventoryType.TX, context.TransactionHashes.Skip(1).ToArray()))
                    localNode.Tell(Message.Create("inv", payload));
            }
            ChangeTimer(TimeSpan.FromSeconds((Blockchain.SecondsPerBlock << (context.ViewNumber + 1)) - (context.ViewNumber == 0 ? Blockchain.SecondsPerBlock : 0)));
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

        internal protected override bool IsHighPriority(object message)
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
