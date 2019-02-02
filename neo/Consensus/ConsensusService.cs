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
        public class Start { }
        public class SetViewNumber { public byte ViewNumber; }
        internal class Timer { public uint Height; public byte ViewNumber; }

        private const byte ContextSerializationPrefix = 0xf4;

        private readonly IConsensusContext context;
        private readonly IActorRef localNode;
        private readonly IActorRef taskManager;
        private readonly Store store;
        private ICancelable timer_token;
        private DateTime block_received_time;
        private bool started = false;
        private readonly Wallet wallet;

        public ConsensusService(IActorRef localNode, IActorRef taskManager, Store store, Wallet wallet)
            : this(localNode, taskManager, store, new ConsensusContext(wallet))
        {
            this.wallet = wallet;
        }

        public ConsensusService(IActorRef localNode, IActorRef taskManager, Store store, IConsensusContext context)
        {
            this.localNode = localNode;
            this.taskManager = taskManager;
            this.store = store;
            this.context = context;
        }


        private bool AddTransaction(Transaction tx, bool verify)
        {
            if (verify && !context.VerifyTransaction(tx))
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
                if (context.VerifyRequest())
                {
                    Log($"send prepare response");
                    context.State |= ConsensusState.ResponseSent;
                    context.Preparations[context.MyIndex] = context.Preparations[context.PrimaryIndex];
                    localNode.Tell(new LocalNode.SendDirectly { Inventory = context.MakePrepareResponse(context.Preparations[context.MyIndex]) });
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
            if (context.Commits.Count(p => p != null) >= context.M && context.TransactionHashes.All(p => context.Transactions.ContainsKey(p)))
            {
                Block block = context.CreateBlock();
                Log($"relay block: {block.Hash}");
                localNode.Tell(new LocalNode.Relay { Inventory = block });
                context.State |= ConsensusState.BlockSent;
            }
        }

        private void CheckExpectedView(byte view_number)
        {
            if (context.ViewNumber == view_number) return;
            if (context.ExpectedView.Count(p => p == view_number) >= context.M)
            {
                InitializeConsensus(view_number);
            }
        }

        private void CheckPreparations()
        {
            if (context.Preparations.Count(p => p != null) >= context.M && context.TransactionHashes.All(p => context.Transactions.ContainsKey(p)))
            {
                ConsensusPayload payload = context.MakeCommit();
                Log($"send commit");
                context.State |= ConsensusState.CommitSent;
                store.Put(ContextSerializationPrefix, new byte[0], context.ToArray());
                localNode.Tell(new LocalNode.SendDirectly { Inventory = payload });
                // Set timer, so we will resend the commit in case of a networking issue
                ChangeTimer(TimeSpan.FromSeconds(Blockchain.SecondsPerBlock));
                CheckCommits();
            }
        }

        private void InitializeConsensus(byte view_number)
        {
            context.Reset(view_number);
            if (context.MyIndex < 0) return;
            if (view_number > 0)
                Log($"changeview: view={view_number} primary={context.Validators[context.GetPrimaryIndex((byte)(view_number - 1u))]}", LogLevel.Warning);
            Log($"initialize: height={context.BlockIndex} view={view_number} index={context.MyIndex} role={(context.MyIndex == context.PrimaryIndex ? ConsensusState.Primary : ConsensusState.Backup)}");
            if (context.MyIndex == context.PrimaryIndex)
            {
                context.State |= ConsensusState.Primary;
                TimeSpan span = TimeProvider.Current.UtcNow - block_received_time;
                if (span >= Blockchain.TimePerBlock)
                    ChangeTimer(TimeSpan.Zero);
                else
                    ChangeTimer(Blockchain.TimePerBlock - span);
            }
            else
            {
                context.State = ConsensusState.Backup;
                ChangeTimer(TimeSpan.FromSeconds(Blockchain.SecondsPerBlock << (view_number + 1)));
            }
        }

        private void Log(string message, LogLevel level = LogLevel.Info)
        {
            Plugin.Log(nameof(ConsensusService), level, message);
        }

        private void OnChangeViewReceived(ConsensusPayload payload, ChangeView message)
        {
            if (message.NewViewNumber < context.ViewNumber)
            {
                // If we are at a higher view, we can send the regeneration msg.
                Log($"send recovery from view: {message.ViewNumber} to view: {context.ViewNumber}");
                localNode.Tell(new LocalNode.SendDirectly { Inventory = context.MakeRecoveryMessage() });
                // Note: In the future, we may want to limit how many nodes will send a regeneration msg
                return;
            }
            if (message.NewViewNumber <= context.ExpectedView[payload.ValidatorIndex])
                return;
            if (context.State.HasFlag(ConsensusState.CommitSent))
                return;

            Log($"{nameof(OnChangeViewReceived)}: height={payload.BlockIndex} view={message.ViewNumber} index={payload.ValidatorIndex} nv={message.NewViewNumber}");
            context.ExpectedView[payload.ValidatorIndex] = message.NewViewNumber;
            context.ChangeViewWitnessInvocationScripts[payload.ValidatorIndex] = payload.Witness.InvocationScript;
            context.ChangeViewTimestamps[payload.ValidatorIndex] = payload.Timestamp;
            CheckExpectedView(message.NewViewNumber);
        }

        private void OnCommitReceived(ConsensusPayload payload, Commit commit)
        {
            if (context.Commits[payload.ValidatorIndex] != null) return;
            Log($"{nameof(OnCommitReceived)}: height={payload.BlockIndex} view={commit.ViewNumber} index={payload.ValidatorIndex}");
            byte[] hashData = context.MakeHeader()?.GetHashData();
            if (hashData == null)
            {
                context.Commits[payload.ValidatorIndex] = commit.Signature;
            }
            else if (Crypto.Default.VerifySignature(hashData, commit.Signature, context.Validators[payload.ValidatorIndex].EncodePoint(false)))
            {
                context.Commits[payload.ValidatorIndex] = commit.Signature;
                CheckCommits();
            }
        }

        private bool PerformBasicConsensusPayloadPreChecks(ConsensusPayload payload)
        {
            if (payload.ValidatorIndex == context.MyIndex) return false;
            if (payload.PrevHash != context.PrevHash || payload.BlockIndex != context.BlockIndex)
            {
                if (context.BlockIndex < payload.BlockIndex)
                {
                    Log($"chain sync: expected={payload.BlockIndex} current={context.BlockIndex - 1} nodes={LocalNode.Singleton.ConnectedCount}", LogLevel.Warning);
                }
                return false;
            }
            if (payload.ValidatorIndex >= context.Validators.Length) return false;
            return true;
        }

        private void OnConsensusPayload(ConsensusPayload payload)
        {
            if (context.State.HasFlag(ConsensusState.BlockSent)) return;
            if (payload.Version != ConsensusContext.Version)
                return;
            if (!PerformBasicConsensusPayloadPreChecks(payload)) return;
            ConsensusMessage message;
            try
            {
                message = ConsensusMessage.DeserializeFrom(payload.Data);
            }
            catch
            {
                return;
            }
            if (message.ViewNumber != context.ViewNumber && message.Type != ConsensusMessageType.ChangeView &&
                                                            message.Type != ConsensusMessageType.RecoveryMessage)
                return;
            switch (message)
            {
                case ChangeView view:
                    OnChangeViewReceived(payload, view);
                    break;
                case RecoveryMessage regeneration:
                    OnRecoveryMessageReceived(payload, regeneration);
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
            }
        }

        private void OnPersistCompleted(Block block)
        {
            Log($"persist block: {block.Hash}");
            block_received_time = TimeProvider.Current.UtcNow;
            InitializeConsensus(0);
        }

        private bool ReverifyPrepareRequest(ConsensusContext consensusContext, RecoveryMessage message, Snapshot snapshot,
            out ConsensusPayload prepareRequestPayload, out PrepareRequest prepareRequest)
        {
            prepareRequest = new PrepareRequest
            {
                ViewNumber = consensusContext.ViewNumber,
                Nonce = message.Nonce,
                NextConsensus = message.NextConsensus,
                TransactionHashes = message.TransactionHashes,
                MinerTransaction = message.MinerTransaction
            };
            var prepareRequestTimestamp = message.PrepareTimestamps[consensusContext.PrimaryIndex];
            prepareRequestPayload = consensusContext.RegenerateSignedPayload(prepareRequest,
                (ushort) consensusContext.PrimaryIndex,
                message.PrepareWitnessInvocationScripts[consensusContext.PrimaryIndex], prepareRequestTimestamp);

            bool result = prepareRequestPayload.Verify(snapshot) &&
                   PerformBasicConsensusPayloadPreChecks(prepareRequestPayload);

            if (!result)
            {
                prepareRequestPayload = null;
                prepareRequest = null;
            }
            return result;
        }

        private void HandleRecoveryInCurrentView(RecoveryMessage message, Snapshot snap)
        {
            if (context.State.HasFlag(ConsensusState.BlockSent)) return;

                ConsensusPayload prepareRequestPayload = null;
                UInt256 preparationHash;
                // if we are already on the right view number the only thing we might want to do is accept more
                // Preparation messages if they can be reconstructed from the regeneration message.
                if (message.PrepareWitnessInvocationScripts[context.PrimaryIndex] != null
                    && ReverifyPrepareRequest((ConsensusContext) context, message, snap, out prepareRequestPayload, out var prepareRequest))
                {
                    OnPrepareRequestReceived(prepareRequestPayload, prepareRequest);
                    preparationHash = prepareRequestPayload.Hash;
                }
                else
                {
                    if (message.PreparationHash == null) return;
                    preparationHash = message.PreparationHash;
                }

                for (int i = 0; i < context.Validators.Length; i++)
                {
                    // If we are missing this preparation.
                    if (context.Preparations[i] != null) continue;
                    if (i == context.PrimaryIndex) continue;
                    // If the recovery message has this preparations
                    if (message.PrepareWitnessInvocationScripts[i] == null) continue;
                    var prepareResponseMsg = new PrepareResponse { PreparationHash = preparationHash };
                    prepareResponseMsg.ViewNumber = context.ViewNumber;
                    var regeneratedPrepareResponse = ((ConsensusContext) context).RegenerateSignedPayload(
                        prepareResponseMsg, (ushort) i, message.PrepareWitnessInvocationScripts[i],
                        message.PrepareTimestamps[i]);
                    if (regeneratedPrepareResponse.Verify(snap) && PerformBasicConsensusPayloadPreChecks(regeneratedPrepareResponse))
                        OnPrepareResponseReceived(regeneratedPrepareResponse, prepareResponseMsg);
                }
        }

        private void OnRecoveryMessageReceived(ConsensusPayload payload, RecoveryMessage message)
        {
            if (context.State.HasFlag(ConsensusState.CommitSent)) return;
            if (context.BlockIndex > payload.BlockIndex) return;
            if (context.ViewNumber > message.ViewNumber) return;
            Snapshot snap =  Blockchain.Singleton.GetSnapshot();
            if (payload.BlockIndex > snap.Height + 1) return;

            Log($"{nameof(OnRecoveryMessageReceived)}: height={payload.BlockIndex} view={message.ViewNumber} index={payload.ValidatorIndex}");

            if (context.ViewNumber == message.ViewNumber)
            {
                HandleRecoveryInCurrentView(message, snap);
                return;
            }

            var tempContext = new ConsensusContext(wallet);
            // Have to Reset to 0 first to handle initializion of the context
            tempContext.Reset(0, snap);
            tempContext.Reset(message.ViewNumber, snap);
            if (message.TransactionHashes != null)
            {
                tempContext.Nonce = message.Nonce;
                tempContext.TransactionHashes = message.TransactionHashes;
                tempContext.NextConsensus = message.NextConsensus;
                tempContext.Transactions = new Dictionary<UInt256, Transaction>
                {
                    [message.TransactionHashes[0]] = message.MinerTransaction
                };
                tempContext.Timestamp = message.PrepareTimestamps[context.PrimaryIndex];
            }

            ConsensusPayload prepareRequestPayload = null;
            PrepareRequest prepareRequest = null;

            UInt256 preparationHash;
            if (message.PrepareWitnessInvocationScripts[context.PrimaryIndex] != null
                && ReverifyPrepareRequest(tempContext, message, snap, out prepareRequestPayload, out prepareRequest))
                preparationHash = prepareRequestPayload.Hash;
            else
                preparationHash = message.PreparationHash;

            var prepareResponses = new List<(ConsensusPayload, PrepareResponse)>();
            var verifiedChangeViewWitnessInvocationScripts = new byte[context.Validators.Length][];
            var verifiedChangeViewTimeStamps = new uint[context.Validators.Length];
            var changeViewMsg = new ChangeView
            {
                ViewNumber = 0,
                NewViewNumber = message.ViewNumber
            };
            int validChangeViewCount = 0;
            for (int i = 0; i < context.Validators.Length; i++)
            {
                if (message.ChangeViewWitnessInvocationScripts[i] != null)
                {
                    // Regenerate the ChangeView message
                    var regeneratedChangeView = tempContext.RegenerateSignedPayload(changeViewMsg, (ushort) i,
                        message.ChangeViewWitnessInvocationScripts[i], message.ChangeViewTimestamps[i]);
                    if (regeneratedChangeView.Verify(snap))
                    {
                        verifiedChangeViewWitnessInvocationScripts[i] = message.ChangeViewWitnessInvocationScripts[i];
                        verifiedChangeViewTimeStamps[i] = message.ChangeViewTimestamps[i];
                        validChangeViewCount++;
                    }
                }

                if (i == context.PrimaryIndex) continue;
                if (message.PrepareWitnessInvocationScripts[i] == null) continue;

                var prepareResponseMsg = new PrepareResponse { PreparationHash = preparationHash };
                var regeneratedPrepareResponse = tempContext.RegenerateSignedPayload(prepareResponseMsg, (ushort) i,
                    message.PrepareWitnessInvocationScripts[i], message.PrepareTimestamps[i]);
                if (regeneratedPrepareResponse.Verify(snap))
                    prepareResponses.Add((regeneratedPrepareResponse, prepareResponseMsg));
            }

            // As long as we had enough valid change view messages to prove we should really move to this view number.
            if (validChangeViewCount >= context.M)
            {
                Log($"regenerating view: {message.ViewNumber}");
                context.Reset(message.ViewNumber, snap);
                for (int i = 0; i < context.Validators.Length; i++)
                {
                    if (verifiedChangeViewWitnessInvocationScripts[i] != null)
                    {
                        context.ChangeViewWitnessInvocationScripts[i] = verifiedChangeViewWitnessInvocationScripts[i];
                        context.ChangeViewTimestamps[i] = verifiedChangeViewTimeStamps[i];
                        context.ExpectedView[i] = message.ViewNumber;
                    }
                }

                if (prepareRequestPayload != null)
                {
                    Log($"regenerating prepare request");
                    if (PerformBasicConsensusPayloadPreChecks(prepareRequestPayload))
                        OnPrepareRequestReceived(prepareRequestPayload, prepareRequest);
                }

                if (prepareResponses.Count > 0)
                {
                    Log($"regenerating preparations: {prepareResponses.Count}");
                    foreach (var (prepareRespPayload, prepareResp) in prepareResponses)
                        if (PerformBasicConsensusPayloadPreChecks(prepareRespPayload))
                            OnPrepareResponseReceived(prepareRespPayload, prepareResp);
                }
            }
        }

        private void OnPrepareRequestReceived(ConsensusPayload payload, PrepareRequest message)
        {
            if (context.State.HasFlag(ConsensusState.RequestReceived)) return;
            if (payload.ValidatorIndex != context.PrimaryIndex) return;
            Log($"{nameof(OnPrepareRequestReceived)}: height={payload.BlockIndex} view={message.ViewNumber} index={payload.ValidatorIndex} tx={message.TransactionHashes.Length}");
            if (!context.State.HasFlag(ConsensusState.Backup)) return;
            if (payload.Timestamp <= context.PrevHeader.Timestamp || payload.Timestamp > TimeProvider.Current.UtcNow.AddMinutes(10).ToTimestamp())
            {
                Log($"Timestamp incorrect: {payload.Timestamp}", LogLevel.Warning);
                return;
            }
            if (message.TransactionHashes.Any(p => context.TransactionExists(p)))
            {
                Log($"Invalid request: transaction already exists", LogLevel.Warning);
                return;
            }
            context.State |= ConsensusState.RequestReceived;
            context.Timestamp = payload.Timestamp;
            context.Nonce = message.Nonce;
            context.NextConsensus = message.NextConsensus;
            context.TransactionHashes = message.TransactionHashes;
            context.Transactions = new Dictionary<UInt256, Transaction>();
            for (int i = 0; i < context.Preparations.Length; i++)
                if (context.Preparations[i] != null)
                    if (!context.Preparations[i].Equals(payload.Hash))
                        context.Preparations[i] = null;
            context.Preparations[payload.ValidatorIndex] = payload.Hash;
            context.PreparationWitnessInvocationScripts[payload.ValidatorIndex] = payload.Witness.InvocationScript;
            context.PreparationTimestamps[payload.ValidatorIndex] = payload.Timestamp;
            byte[] hashData = context.MakeHeader().GetHashData();
            for (int i = 0; i < context.Commits.Length; i++)
                if (context.Commits[i] != null)
                    if (!Crypto.Default.VerifySignature(hashData, context.Commits[i], context.Validators[i].EncodePoint(false)))
                        context.Commits[i] = null;
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
            if (context.Preparations[payload.ValidatorIndex] != null) return;
            if (context.Preparations[context.PrimaryIndex] != null && !message.PreparationHash.Equals(context.Preparations[context.PrimaryIndex]))
                return;
            Log($"{nameof(OnPrepareResponseReceived)}: height={payload.BlockIndex} view={message.ViewNumber} index={payload.ValidatorIndex}");
            if (context.State.HasFlag(ConsensusState.CommitSent)) return;
            context.Preparations[payload.ValidatorIndex] = message.PreparationHash;
            context.PreparationWitnessInvocationScripts[payload.ValidatorIndex] = payload.Witness.InvocationScript;
            context.PreparationTimestamps[payload.ValidatorIndex] = payload.Timestamp;
            if (context.State.HasFlag(ConsensusState.RequestSent) || context.State.HasFlag(ConsensusState.RequestReceived))
                CheckPreparations();
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

        private void OnStart()
        {
            Log("OnStart");
            started = true;
            byte[] data = store.Get(ContextSerializationPrefix, new byte[0]);
            if (data != null)
            {
                using (MemoryStream ms = new MemoryStream(data, false))
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    context.Deserialize(reader);
                }
            }
            if (context.State.HasFlag(ConsensusState.CommitSent) && context.BlockIndex == Blockchain.Singleton.Height + 1)
            {
                CheckPreparations();
            }
            else
                InitializeConsensus(0);
        }

        private void OnTimer(Timer timer)
        {
            if (context.State.HasFlag(ConsensusState.BlockSent)) return;
            if (timer.Height != context.BlockIndex || timer.ViewNumber != context.ViewNumber) return;
            Log($"timeout: height={timer.Height} view={timer.ViewNumber} state={context.State}");
            if (context.State.HasFlag(ConsensusState.Primary) && !context.State.HasFlag(ConsensusState.RequestSent))
            {
                Log($"send prepare request: height={timer.Height} view={timer.ViewNumber}");
                context.Fill();
                ConsensusPayload request = context.MakePrepareRequest();
                localNode.Tell(new LocalNode.SendDirectly { Inventory = request });
                context.State |= ConsensusState.RequestSent;
                context.Preparations[context.MyIndex] = request.Hash;
                context.PreparationWitnessInvocationScripts[context.MyIndex] = request.Witness.InvocationScript;
                context.PreparationTimestamps[context.MyIndex] = request.Timestamp;

                if (context.TransactionHashes.Length > 1)
                {
                    foreach (InvPayload payload in InvPayload.CreateGroup(InventoryType.TX, context.TransactionHashes.Skip(1).ToArray()))
                        localNode.Tell(Message.Create("inv", payload));
                }
                ChangeTimer(TimeSpan.FromSeconds(Blockchain.SecondsPerBlock << (timer.ViewNumber + 1)));
            }
            else if ((context.State.HasFlag(ConsensusState.Primary) && context.State.HasFlag(ConsensusState.RequestSent)) || context.State.HasFlag(ConsensusState.Backup))
            {
                if (context.State.HasFlag(ConsensusState.CommitSent))
                {
                    // Re-send commit periodically in case of a network issue.
                    ConsensusPayload payload = context.MakeCommit();
                    Log($"resend commit");
                    localNode.Tell(new LocalNode.SendDirectly { Inventory = payload });
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
            if (!context.State.HasFlag(ConsensusState.Backup) || !context.State.HasFlag(ConsensusState.RequestReceived) || context.State.HasFlag(ConsensusState.ResponseSent) || context.State.HasFlag(ConsensusState.ViewChanging) || context.State.HasFlag(ConsensusState.BlockSent))
                return;
            if (context.Transactions.ContainsKey(transaction.Hash)) return;
            if (!context.TransactionHashes.Contains(transaction.Hash)) return;
            AddTransaction(transaction, true);
        }

        protected override void PostStop()
        {
            Log("OnStop");
            started = false;
            context.Dispose();
            base.PostStop();
        }

        public static Props Props(IActorRef localNode, IActorRef taskManager, Store store, Wallet wallet)
        {
            return Akka.Actor.Props.Create(() => new ConsensusService(localNode, taskManager, store, wallet)).WithMailbox("consensus-service-mailbox");
        }

        private void RequestChangeView()
        {
            context.State |= ConsensusState.ViewChanging;
            context.ExpectedView[context.MyIndex]++;
            Log($"request change view: height={context.BlockIndex} view={context.ViewNumber} nv={context.ExpectedView[context.MyIndex]} state={context.State}");
            ChangeTimer(TimeSpan.FromSeconds(Blockchain.SecondsPerBlock << (context.ExpectedView[context.MyIndex] + 1)));
            var changeViewRequest = context.MakeChangeView();
            context.ChangeViewWitnessInvocationScripts[context.MyIndex] = changeViewRequest.Witness.InvocationScript;
            context.ChangeViewTimestamps[context.MyIndex] = changeViewRequest.Timestamp;
            localNode.Tell(new LocalNode.SendDirectly { Inventory = changeViewRequest });
            CheckExpectedView(context.ExpectedView[context.MyIndex]);
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
