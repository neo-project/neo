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
    public sealed class ConsensusService : UntypedActor //, IDisposable
    {
        public bool shouldStop;
        public class Start { }
        //public class Stop { }
        //public class Setup { public IActorRef _localNode; public IActorRef _taskManager; public IConsensusContext _context; }
        public class SetViewNumber { public byte ViewNumber; }
        internal class Timer { public uint Height; public byte ViewNumber; }

        private IConsensusContext context;
        //private readonly NeoSystem system;
        private IActorRef localNode;
        private IActorRef taskManager;
        private DateTime block_received_time;

        public ConsensusService(IActorRef _LocalNode, IActorRef _TaskManager, Wallet wallet)
        {
            //this.system = system;
            this.localNode = _LocalNode;
            this.taskManager = _TaskManager;
            this.context = new ConsensusContext(wallet);
            this.shouldStop = false;
        }

        public ConsensusService(IActorRef _LocalNode, IActorRef _TaskManager, IConsensusContext context)
        {
            //this.system = system;
            this.localNode = _LocalNode;
            this.taskManager = _TaskManager;
            this.context = context;
            this.shouldStop = false;
        }

        public ConsensusService()
        {
            //this.system = null;
            this.context = new ConsensusContext(null);
            this.shouldStop = false;
        }

        private bool AddTransaction(Transaction tx, bool verify)
        {
            if (context.RejectTx(tx, verify))
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
                    context.State |= ConsensusState.SignatureSent;
                    context.SignHeader();
                    localNode.Tell(new LocalNode.SendDirectly { Inventory = context.MakePrepareResponse(context.Signatures[context.MyIndex]) });
                    CheckSignatures();
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
            Context.System.Scheduler.ScheduleTellOnce(delay, Self, new Timer
            {
                Height = context.BlockIndex,
                ViewNumber = context.ViewNumber
            }, ActorRefs.NoSender);
        }

        private void CheckExpectedView(byte view_number)
        {
            if (context.ViewNumber == view_number) return;
            if (context.ExpectedView.Count(p => p == view_number) >= context.M)
            {
                InitializeConsensus(view_number);
            }
        }

        private void CheckSignatures()
        {
            if (context.Signatures.Count(p => p != null) >= context.M && context.TransactionHashes.All(p => context.Transactions.ContainsKey(p)))
            {
                Block block = context.CreateBlock();
                Log($"relay block: {block.Hash}");
                localNode.Tell(new LocalNode.Relay { Inventory = block });
                context.State |= ConsensusState.BlockSent;
            }
        }

        private void InitializeConsensus(byte view_number)
        {
            if (view_number == 0)
            {
                Console.WriteLine($"Will reset {context}");
                context.Reset();
            }
            else
                context.ChangeView(view_number);
            if (context.MyIndex < 0) return;
            if (view_number > 0)
                Log($"changeview: view={view_number} primary={context.Validators[context.GetPrimaryIndex((byte)(view_number - 1u))]}", LogLevel.Warning);
            Log($"initialize: height={context.BlockIndex} view={view_number} index={context.MyIndex} role={(context.MyIndex == context.PrimaryIndex ? ConsensusState.Primary : ConsensusState.Backup)}");
            if (context.MyIndex == context.PrimaryIndex)
            {
                Console.WriteLine($"Is primary! {context.State}");
                context.State |= ConsensusState.Primary;
                Console.WriteLine($"After setup Is primary! {context.State}");
                TimeSpan span = context.GetUtcNow() - block_received_time;
                Console.WriteLine($"span {span} block_received_time {block_received_time} timeperblock {Blockchain.TimePerBlock}");
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
            Console.WriteLine("Finished InitializeConsensus! :)");
        }

        private void Log(string message, LogLevel level = LogLevel.Info)
        {
            Plugin.Log(nameof(ConsensusService), level, message);
        }

        private void OnChangeViewReceived(ConsensusPayload payload, ChangeView message)
        {
            if (message.NewViewNumber <= context.ExpectedView[payload.ValidatorIndex])
                return;
            Log($"{nameof(OnChangeViewReceived)}: height={payload.BlockIndex} view={message.ViewNumber} index={payload.ValidatorIndex} nv={message.NewViewNumber}");
            context.ExpectedView[payload.ValidatorIndex] = message.NewViewNumber;
            CheckExpectedView(message.NewViewNumber);
        }

        private void OnConsensusPayload(ConsensusPayload payload)
        {
            if (context.State.HasFlag(ConsensusState.BlockSent)) return;
            if (payload.ValidatorIndex == context.MyIndex) return;
            if (payload.Version != ConsensusContext.Version)
                return;
            if (payload.PrevHash != context.PrevHash || payload.BlockIndex != context.BlockIndex)
            {
                if (context.SnapshotHeight + 1 < payload.BlockIndex)
                {
                    Log($"chain sync: expected={payload.BlockIndex} current: {context.SnapshotHeight} nodes={LocalNode.Singleton.ConnectedCount}", LogLevel.Warning);
                }
                return;
            }
            if (payload.ValidatorIndex >= context.Validators.Length) return;
            ConsensusMessage message;
            try
            {
                message = ConsensusMessage.DeserializeFrom(payload.Data);
            }
            catch
            {
                return;
            }
            if (message.ViewNumber != context.ViewNumber && message.Type != ConsensusMessageType.ChangeView)
                return;
            switch (message.Type)
            {
                case ConsensusMessageType.ChangeView:
                    OnChangeViewReceived(payload, (ChangeView)message);
                    break;
                case ConsensusMessageType.PrepareRequest:
                    OnPrepareRequestReceived(payload, (PrepareRequest)message);
                    break;
                case ConsensusMessageType.PrepareResponse:
                    OnPrepareResponseReceived(payload, (PrepareResponse)message);
                    break;
            }
        }

        private void OnPersistCompleted(Block block)
        {
            Log($"persist block: {block.Hash}");
            block_received_time = context.GetUtcNow();
            InitializeConsensus(0);
        }

        private void OnPrepareRequestReceived(ConsensusPayload payload, PrepareRequest message)
        {
            if (context.State.HasFlag(ConsensusState.RequestReceived)) return;
            if (payload.ValidatorIndex != context.PrimaryIndex) return;
            Log($"{nameof(OnPrepareRequestReceived)}: height={payload.BlockIndex} view={message.ViewNumber} index={payload.ValidatorIndex} tx={message.TransactionHashes.Length}");
            if (!context.State.HasFlag(ConsensusState.Backup)) return;
            if (payload.Timestamp <= context.SnapshotHeader.Timestamp || payload.Timestamp > context.GetUtcNow().AddMinutes(10).ToTimestamp())
            {
                Log($"Timestamp incorrect: {payload.Timestamp}", LogLevel.Warning);
                return;
            }
            context.State |= ConsensusState.RequestReceived;
            context.Timestamp = payload.Timestamp;
            context.Nonce = message.Nonce;
            context.NextConsensus = message.NextConsensus;
            context.TransactionHashes = message.TransactionHashes;
            context.Transactions = new Dictionary<UInt256, Transaction>();
            byte[] hashData = context.MakeHeader().GetHashData();
            if (!Crypto.Default.VerifySignature(hashData, message.Signature, context.Validators[payload.ValidatorIndex].EncodePoint(false))) return;
            for (int i = 0; i < context.Signatures.Length; i++)
                if (context.Signatures[i] != null)
                    if (!Crypto.Default.VerifySignature(hashData, context.Signatures[i], context.Validators[i].EncodePoint(false)))
                        context.Signatures[i] = null;
            context.Signatures[payload.ValidatorIndex] = message.Signature;
            Dictionary<UInt256, Transaction> mempool = Blockchain.Singleton.GetMemoryPool().ToDictionary(p => p.Hash);
            List<Transaction> unverified = new List<Transaction>();
            foreach (UInt256 hash in context.TransactionHashes.Skip(1))
            {
                if (mempool.TryGetValue(hash, out Transaction tx))
                {
                    if (!AddTransaction(tx, false))
                        return;
                }
                else
                {
                    tx = Blockchain.Singleton.GetUnverifiedTransaction(hash);
                    if (tx != null)
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
            if (context.Signatures[payload.ValidatorIndex] != null) return;
            Log($"{nameof(OnPrepareResponseReceived)}: height={payload.BlockIndex} view={message.ViewNumber} index={payload.ValidatorIndex}");
            byte[] hashData = context.MakeHeader()?.GetHashData();
            if (hashData == null)
            {
                context.Signatures[payload.ValidatorIndex] = message.Signature;
            }
            else if (Crypto.Default.VerifySignature(hashData, message.Signature, context.Validators[payload.ValidatorIndex].EncodePoint(false)))
            {
                context.Signatures[payload.ValidatorIndex] = message.Signature;
                CheckSignatures();
            }
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case Start _:
                    OnStart();
                    break;
                //case Setup setup:
                //    OnSetup(setup._localNode, setup._taskManager, setup._context);
                //    break;
                //case Stop _:
                //    OnStop();
                //    break;
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

        private void OnStart()
        {
            Console.WriteLine("Testando OnStart!!");
            Log("OnStart");
            shouldStop = false;
            InitializeConsensus(0);
        }
/*
        private void OnSetup(IActorRef _localNode, IActorRef _taskManager, IConsensusContext _context)
        {
            this.localNode = _localNode;
            this.taskManager = _taskManager;
            this.context = _context;
            Console.WriteLine("Testando OnSetup!!");
            //shouldStop = false;
            //InitializeConsensus(0);
        }

        private void OnStop()
        {
            shouldStop = true;
            Console.WriteLine("Testando OnStop!!");
            Log("Calling OnStop");
            //InitializeConsensus(0);
        }
*/
        private void OnTimer(Timer timer)
        {
            if(shouldStop == true)
                return;
            if (context.State.HasFlag(ConsensusState.BlockSent)) return;
            if (timer.Height != context.BlockIndex || timer.ViewNumber != context.ViewNumber) return;
            Log($"timeout: height={timer.Height} view={timer.ViewNumber} state={context.State}");
            if (context.State.HasFlag(ConsensusState.Primary) && !context.State.HasFlag(ConsensusState.RequestSent))
            {
                Log($"send prepare request: height={timer.Height} view={timer.ViewNumber}");
                context.State |= ConsensusState.RequestSent;
                if (!context.State.HasFlag(ConsensusState.SignatureSent))
                {
                    context.Fill();
                    context.SignHeader();
                }
                localNode.Tell(new LocalNode.SendDirectly { Inventory = context.MakePrepareRequest() });
                if (context.TransactionHashes.Length > 1)
                {
                    foreach (InvPayload payload in InvPayload.CreateGroup(InventoryType.TX, context.TransactionHashes.Skip(1).ToArray()))
                        localNode.Tell(Message.Create("inv", payload));
                }
                ChangeTimer(TimeSpan.FromSeconds(Blockchain.SecondsPerBlock << (timer.ViewNumber + 1)));
            }
            else if ((context.State.HasFlag(ConsensusState.Primary) && context.State.HasFlag(ConsensusState.RequestSent)) || context.State.HasFlag(ConsensusState.Backup))
            {
                RequestChangeView();
            }
        }

        private void OnTransaction(Transaction transaction)
        {
            if (transaction.Type == TransactionType.MinerTransaction) return;
            if (!context.State.HasFlag(ConsensusState.Backup) || !context.State.HasFlag(ConsensusState.RequestReceived) || context.State.HasFlag(ConsensusState.SignatureSent) || context.State.HasFlag(ConsensusState.ViewChanging) || context.State.HasFlag(ConsensusState.BlockSent))
                return;
            if (context.Transactions.ContainsKey(transaction.Hash)) return;
            if (!context.TransactionHashes.Contains(transaction.Hash)) return;
            AddTransaction(transaction, true);
        }

        //public void Dispose()
        //{
        //}

        protected override void PostStop()
        {
            Log("OnStop");
            context.Dispose();
            base.PostStop();
        }
/*
        public static Props Props(NeoSystem system, Wallet wallet)
        {
            return Akka.Actor.Props.Create(() => new ConsensusService(system, wallet)).WithMailbox("consensus-service-mailbox");
        }
*/
        public static Props Props(IActorRef localNode, IActorRef taskManager, Wallet wallet)
        {
            return Akka.Actor.Props.Create(() => new ConsensusService(localNode, taskManager, wallet)).WithMailbox("consensus-service-mailbox");
        }

        private void RequestChangeView()
        {
            context.State |= ConsensusState.ViewChanging;
            context.ExpectedView[context.MyIndex]++;
            Log($"request change view: height={context.BlockIndex} view={context.ViewNumber} nv={context.ExpectedView[context.MyIndex]} state={context.State}");
            ChangeTimer(TimeSpan.FromSeconds(Blockchain.SecondsPerBlock << (context.ExpectedView[context.MyIndex] + 1)));
            localNode.Tell(new LocalNode.SendDirectly { Inventory = context.MakeChangeView() });
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
