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
using Neo.SmartContract;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Consensus
{
    public sealed class ConsensusService : UntypedActor
    {
        public class Start { }
        internal class Timer { public uint Height; public byte ViewNumber; }

        private readonly ConsensusContext context = new ConsensusContext();
        private readonly NeoSystem system;
        private readonly Wallet wallet;
        private DateTime block_received_time;

<<<<<<< HEAD
        //===================================
        //Opt consensus values
        //Opt variables
        public DateTime? dInit;
        //private DateTime block_last_time;
        private bool firstIter = true;
        int fixedNumberOfBlocksToGetAvg = 5;
        int fixedFirstChangeViewTimeOut = 30;
        List<double> blockTimes = new List<double>();
        //===================================

        public ConsensusService(LocalNode localNode, Wallet wallet)
=======
        public ConsensusService(NeoSystem system, Wallet wallet)
>>>>>>> bd5c707f58f0acef06ed69596865481480e6a554
        {
            this.system = system;
            this.wallet = wallet;
        }

        private bool AddTransaction(Transaction tx, bool verify)
        {
            if (context.Snapshot.ContainsTransaction(tx.Hash) ||
                (verify && !tx.Verify(context.Snapshot, context.Transactions.Values)) ||
                !CheckPolicy(tx))
            {
                Log($"reject tx: {tx.Hash}{Environment.NewLine}{tx.ToArray().ToHexString()}", LogLevel.Warning);
                RequestChangeView();
                return false;
            }
            context.Transactions[tx.Hash] = tx;
            Console.WriteLine("(CS-AD) context.TransactionHashes.Length:" + context.TransactionHashes.Length);
            Console.WriteLine("(CS-AD) context.Transactions.Count:" + context.Transactions.Count);

            if (context.TransactionHashes.Length == context.Transactions.Count)
            {
<<<<<<< HEAD

                if (Blockchain.GetConsensusAddress(Blockchain.Default.GetValidators(context.Transactions.Values).ToArray()).Equals(context.NextConsensus))
=======
                if (Blockchain.GetConsensusAddress(context.Snapshot.GetValidators(context.Transactions.Values).ToArray()).Equals(context.NextConsensus))
>>>>>>> bd5c707f58f0acef06ed69596865481480e6a554
                {
                    Log($"send prepare response");
                    context.State |= ConsensusState.SignatureSent;
                    context.Signatures[context.MyIndex] = context.MakeHeader().Sign(context.KeyPair);
                    SignAndRelay(context.MakePrepareResponse(context.Signatures[context.MyIndex]));
                    Console.WriteLine($"Signed and relayed");
                    CheckSignatures();
                    Console.WriteLine($"Signature checked");
                }
                else
                {
                    Console.WriteLine("(CS-AD) ELSE");
                    RequestChangeView();
                    return false;
                }
            }
            return true;
        }

<<<<<<< HEAD
        private void Blockchain_PersistUnlocked(object sender, Block block)
        {
            Log($"persist block: {block.Hash}");
            block_received_time = DateTime.Now;

            /*
            //===========================================================
            // Opt blocks
            if (dInit != DateTime.MinValue)
            {
                blockTimes.Add((double)(block_received_time - dInit).GetValueOrDefault().TotalSeconds);

                if (blockTimes.Count() > fixedNumberOfBlocksToGetAvg)
                    blockTimes.RemoveAt(0);
            }    
            //===========================================================
            */

            InitializeConsensus(0);
=======
        private void ChangeTimer(TimeSpan delay)
        {
            Context.System.Scheduler.ScheduleTellOnce(delay, Self, new Timer
            {
                Height = context.BlockIndex,
                ViewNumber = context.ViewNumber
            }, ActorRefs.NoSender);
>>>>>>> bd5c707f58f0acef06ed69596865481480e6a554
        }

        private void CheckExpectedView(byte view_number)
        {
            if (context.ViewNumber == view_number) return;
            if (context.ExpectedView.Count(p => p == view_number) >= context.M)
            {
                InitializeConsensus(view_number);
            }
        }

        private bool CheckPolicy(Transaction tx)
        {
<<<<<<< HEAD

            foreach (PolicyPlugin plugin in PolicyPlugin.Instances)
=======
            foreach (IPolicyPlugin plugin in Plugin.Policies)
>>>>>>> bd5c707f58f0acef06ed69596865481480e6a554
                if (!plugin.CheckPolicy(tx))
                    return false;
            return true;
        }

        private void CheckSignatures()
        {
            if (context.Signatures.Count(p => p != null) >= context.M && context.TransactionHashes.All(p => context.Transactions.ContainsKey(p)))
            {
                Contract contract = Contract.CreateMultiSigContract(context.M, context.Validators);
                Block block = context.MakeHeader();
                ContractParametersContext sc = new ContractParametersContext(block);
                for (int i = 0, j = 0; i < context.Validators.Length && j < context.M; i++)
                    if (context.Signatures[i] != null)
                    {
                        sc.AddSignature(contract, context.Validators[i], context.Signatures[i]);
                        j++;
                    }
                sc.Verifiable.Witnesses = sc.GetWitnesses();
                block.Transactions = context.TransactionHashes.Select(p => context.Transactions[p]).ToArray();
                Log($"relay block: {block.Hash}");
                system.LocalNode.Tell(new LocalNode.Relay { Inventory = block });
                context.State |= ConsensusState.BlockSent;
            }
        }

<<<<<<< HEAD
        public void Dispose()
        {
            Log("OnStop");
            if (timer != null) timer.Dispose();
            if (started)
            {
                Blockchain.PersistUnlocked -= Blockchain_PersistUnlocked;
                LocalNode.InventoryReceiving -= LocalNode_InventoryReceiving;
                LocalNode.InventoryReceived -= LocalNode_InventoryReceived;
            }
        }

        private void FillContext()
        {
            List<Transaction> transactions = LocalNode.GetMemoryPool().Where(p => CheckPolicy(p)).ToList();
            if (transactions.Count >= Settings.Default.MaxTransactionsPerBlock)
                transactions = transactions.OrderByDescending(p => p.NetworkFee / p.Size).Take(Settings.Default.MaxTransactionsPerBlock - 1).ToList();
=======
        private void FillContext()
        {
            IEnumerable<Transaction> mem_pool = Blockchain.Singleton.GetMemoryPool().Where(p => CheckPolicy(p));
            foreach (IPolicyPlugin plugin in Plugin.Policies)
                mem_pool = plugin.Filter(mem_pool);
            List<Transaction> transactions = mem_pool.ToList();
>>>>>>> bd5c707f58f0acef06ed69596865481480e6a554
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
                if (!context.Snapshot.ContainsTransaction(tx.Hash))
                {
                    context.Nonce = nonce;
                    transactions.Insert(0, tx);
                    break;
                }
            }
            context.TransactionHashes = transactions.Select(p => p.Hash).ToArray();
            context.Transactions = transactions.ToDictionary(p => p.Hash);
            context.NextConsensus = Blockchain.GetConsensusAddress(context.Snapshot.GetValidators(transactions).ToArray());
        }

        private static ulong GetNonce()
        {
            byte[] nonce = new byte[sizeof(ulong)];
            Random rand = new Random();
            rand.NextBytes(nonce);
            return nonce.ToUInt64(0);
        }



        private void InitializeConsensus(byte view_number)
        {
            if (view_number == 0)
                context.Reset(wallet);
            else
                context.ChangeView(view_number);
            if (context.MyIndex < 0) return;
            Log($"initialize: height={context.BlockIndex} view={view_number} index={context.MyIndex} role={(context.MyIndex == context.PrimaryIndex ? ConsensusState.Primary : ConsensusState.Backup)}");
            if (context.MyIndex == context.PrimaryIndex)
            {
<<<<<<< HEAD
                dInit = DateTime.MinValue;

                if (view_number == 0)
                    context.Reset(wallet);
                else
                    context.ChangeView(view_number);
                if (context.MyIndex < 0) return;
                Log($"initialize: height={context.BlockIndex} view={view_number} index={context.MyIndex} role={(context.MyIndex == context.PrimaryIndex ? ConsensusState.Primary : ConsensusState.Backup)}");

                //=================================================
                double predictedBlockTimeBasedOnAvg = 3; //Empirically, the minimum time that is being needed for generating a block
                if (blockTimes.Count() > 0)
                {
                    predictedBlockTimeBasedOnAvg = Math.Ceiling(Math.Max(Math.Min(blockTimes.Average(), predictedBlockTimeBasedOnAvg), 10)) - 1;
                    //Console.WriteLine($"(CS-IC) MaxMin:{predictedBlockTimeBasedOnAvg} Min:{Math.Min(blockTimes.Average(), 3)}");
                }
                predictedBlockTimeBasedOnAvg = 3;
                TimeSpan span = DateTime.Now + TimeSpan.FromSeconds(predictedBlockTimeBasedOnAvg) - block_received_time;
                //First iter means that consensus are starting, genesis block, or some unpected incident
                //Give some for for nodes start and receive prepare request from speaker
                if (firstIter)
                {
                    firstIter = false;
                    //Wait until all nodes are on
                    span = TimeSpan.FromSeconds(-5);
                }

                //if (view_number == 0)
                //{
                //    block_last_time = DateTime.Now;
                //}
                //=================================================



                if (context.MyIndex == context.PrimaryIndex)
                {

                    context.State |= ConsensusState.Primary;
                    timer_height = context.BlockIndex;
                    timer_view = view_number;


                    if (span >= Blockchain.TimePerBlock)
                        timer.Change(0, Timeout.Infinite);
                    else
                        timer.Change(Blockchain.TimePerBlock - span, Timeout.InfiniteTimeSpan);

                    //Console.WriteLine($"(CS-IC) AVG:{blockTimes.Average()} count:{blockTimes.Count()}");
                    Console.WriteLine($"(CS-IC) block_received_time={block_received_time} DateTime.Now={DateTime.Now} diff={DateTime.Now - block_received_time}");
                    Console.WriteLine("(CS-IC) Blockchain.TimePerBlock: " + Blockchain.TimePerBlock);
                    Console.WriteLine("(CS-IC) span.TotalSeconds: " + span.TotalSeconds);
                    Console.WriteLine("(CS-IC) predictedBlockTimeBasedOnAvg: " + predictedBlockTimeBasedOnAvg);
                    Console.WriteLine("(CS-IC) context.BlockIndex: " + context.BlockIndex);
                    Console.WriteLine("(CS-IC) view_number:" + view_number);
                    Console.WriteLine("(CS-IC) PRIMARY");

=======
                context.State |= ConsensusState.Primary;
                if (!context.State.HasFlag(ConsensusState.SignatureSent))
                {
                    FillContext();
>>>>>>> bd5c707f58f0acef06ed69596865481480e6a554
                }
                if (context.TransactionHashes.Length > 1)
                {
<<<<<<< HEAD

                    Console.WriteLine("(CS-IC) context.BlockIndex: " + context.BlockIndex);
                    Console.WriteLine("(CS-IC) view_number:" + view_number);
                    Console.WriteLine("(CS-IC) TimeSpan with normal formula would be:" + TimeSpan.FromSeconds(Blockchain.SecondsPerBlock << (view_number + 1)));
                    Console.WriteLine("(CS-IC) BACKUP");


                    context.State = ConsensusState.Backup;
                    timer_height = context.BlockIndex;
                    timer_view = view_number;

                    //Time waiting until next change view should be triggered
                    //timer.Change(TimeSpan.FromSeconds(fixedFirstChangeViewTimeOut), Timeout.InfiniteTimeSpan);


                    //timer.Change(TimeSpan.FromSeconds((Blockchain.SecondsPerBlock << (view_number + 1) ) + 2) , Timeout.InfiniteTimeSpan);
                    //TimeSpan span = DateTime.Now - block_received_time;
                    //if (span >= Blockchain.TimePerBlock)
                    //    timer.Change(0, Timeout.Infinite);
                    //else
                    //    timer.Change(Blockchain.TimePerBlock - span - 0.5, Timeout.InfiniteTimeSpan);
                    //0.5 in an adjustment because sometime will be used



                }
            }
        }

        /*
                private void InitializeConsensus(byte view_number)
                {
                    lock (context)
                    {
                        if (view_number == 0)
                            context.Reset(wallet);
                        else
                            context.ChangeView(view_number);
                        if (context.MyIndex < 0) return;
                        Log($"initialize: height={context.BlockIndex} view={view_number} index={context.MyIndex} role={(context.MyIndex == context.PrimaryIndex ? ConsensusState.Primary : ConsensusState.Backup)}");

            string message = "(CONSENSUS_SERVICE) Blockchain.TimePerBlock " +  Blockchain.TimePerBlock;
            Console.WriteLine(message); 



                        if (context.MyIndex == context.PrimaryIndex)
                        {
                            context.State |= ConsensusState.Primary;
                            if (!context.State.HasFlag(ConsensusState.SignatureSent))
                            {
                                FillContext();
                            }
                            if (context.TransactionHashes.Length > 1)
                            {
                                InvPayload invPayload = InvPayload.Create(InventoryType.TX, context.TransactionHashes.Skip(1).ToArray());
                                foreach (RemoteNode node in localNode.GetRemoteNodes())
                                    node.EnqueueMessage("inv", invPayload);
                            }
                            timer_height = context.BlockIndex;
                            timer_view = view_number;
                            TimeSpan span = DateTime.Now - block_received_time;
                            if (span >= Blockchain.TimePerBlock)
                                timer.Change(0, Timeout.Infinite);
                            else
                                timer.Change(Blockchain.TimePerBlock - span, Timeout.InfiniteTimeSpan);

            message = "(CONSENSUS_SERVICE) span " + span;
            Console.WriteLine(message);

            Console.WriteLine("(CONSENSUS_SERVICE) Timeout.InfiniteTimeSpan " +  Timeout.InfiniteTimeSpan);
            Console.WriteLine("(CONSENSUS_SERVICE) Blockchain.TimePerBlock " +  Blockchain.TimePerBlock);
            Console.WriteLine("(CONSENSUS_SERVICE) context.BlockIndex inside conseus service" +  context.BlockIndex); 

                        }
                        else
                        {
            Console.WriteLine("(CONSENSUS_SERVICE) view_number " +  view_number);
            Console.WriteLine("(CONSENSUS_SERVICE) TimeSpan.FromSeconds(Blockchain.SecondsPerBlock << (view_number + 1)) " +  TimeSpan.FromSeconds(Blockchain.SecondsPerBlock << (view_number + 1)));


                            context.State = ConsensusState.Backup;
                            timer_height = context.BlockIndex;
                            timer_view = view_number;
                            //timer.Change(TimeSpan.FromSeconds(Blockchain.SecondsPerBlock << (view_number + 1)), Timeout.InfiniteTimeSpan);
                    TimeSpan span = DateTime.Now - block_received_time;
                            if (span >= Blockchain.TimePerBlock)
                                timer.Change(0, Timeout.Infinite);
                            else
                                timer.Change(Blockchain.TimePerBlock - span, Timeout.InfiniteTimeSpan);

                        }
                    }
                }

        */
        private void LocalNode_InventoryReceived(object sender, IInventory inventory)
        {
            ConsensusPayload payload = inventory as ConsensusPayload;
            if (payload != null)
            {
                lock (context)
                {
                    if (payload.ValidatorIndex == context.MyIndex) return;

                    if (payload.Version != ConsensusContext.Version)
                        return;
                    if (payload.PrevHash != context.PrevHash || payload.BlockIndex != context.BlockIndex)
                    {
                        // Request blocks

                        if (Blockchain.Default?.Height + 1 < payload.BlockIndex)
                        {
                            Log($"chain sync: expected={payload.BlockIndex} current: {Blockchain.Default?.Height} nodes={localNode.RemoteNodeCount}");

                            localNode.RequestGetBlocks();
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
=======
                    foreach (InvPayload payload in InvPayload.CreateGroup(InventoryType.TX, context.TransactionHashes.Skip(1).ToArray()))
                        system.LocalNode.Tell(Message.Create("inv", payload));
>>>>>>> bd5c707f58f0acef06ed69596865481480e6a554
                }
                TimeSpan span = DateTime.Now - block_received_time;
                if (span >= Blockchain.TimePerBlock)
                    ChangeTimer(TimeSpan.Zero);
                else
                    ChangeTimer(Blockchain.TimePerBlock - span);
            }
            else
            {
<<<<<<< HEAD
                lock (context)
                {
                    if (!context.State.HasFlag(ConsensusState.Backup) || !context.State.HasFlag(ConsensusState.RequestReceived) || context.State.HasFlag(ConsensusState.SignatureSent) || context.State.HasFlag(ConsensusState.ViewChanging))
                        return;
                    if (context.Transactions.ContainsKey(tx.Hash)) return;
                    if (!context.TransactionHashes.Contains(tx.Hash)) return;
                    Console.WriteLine("(CS-LN-IR) LocalNode_InventoryReceiving:");
                    AddTransaction(tx, true);
                    e.Cancel = true;
                }
=======
                context.State = ConsensusState.Backup;
                ChangeTimer(TimeSpan.FromSeconds(Blockchain.SecondsPerBlock << (view_number + 1)));
>>>>>>> bd5c707f58f0acef06ed69596865481480e6a554
            }
        }

        private void Log(string message, LogLevel level = LogLevel.Info)
        {
            Plugin.Log(nameof(ConsensusService), level, message);
        }

        private void OnChangeViewReceived(ConsensusPayload payload, ChangeView message)
        {
            Log($"{nameof(OnChangeViewReceived)}: height={payload.BlockIndex} view={message.ViewNumber} index={payload.ValidatorIndex} nv={message.NewViewNumber}");

            if (message.NewViewNumber <= context.ExpectedView[payload.ValidatorIndex])
                return;
            context.ExpectedView[payload.ValidatorIndex] = message.NewViewNumber;
            CheckExpectedView(message.NewViewNumber);
        }

        private void OnConsensusPayload(ConsensusPayload payload)
        {
            if (payload.ValidatorIndex == context.MyIndex) return;
            if (payload.Version != ConsensusContext.Version)
                return;
            if (payload.PrevHash != context.PrevHash || payload.BlockIndex != context.BlockIndex)
            {
                if (context.Snapshot.Height + 1 < payload.BlockIndex)
                {
                    Log($"chain sync: expected={payload.BlockIndex} current: {context.Snapshot.Height} nodes={LocalNode.Singleton.ConnectedCount}", LogLevel.Warning);
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
            block_received_time = DateTime.Now;
            InitializeConsensus(0);
        }

        private void OnPrepareRequestReceived(ConsensusPayload payload, PrepareRequest message)
        {
            Log($"{nameof(OnPrepareRequestReceived)}: height={payload.BlockIndex} view={message.ViewNumber} index={payload.ValidatorIndex} tx={message.TransactionHashes.Length}");
            if (!context.State.HasFlag(ConsensusState.Backup) || context.State.HasFlag(ConsensusState.RequestReceived))
                return;
            if (payload.ValidatorIndex != context.PrimaryIndex) return;
            if (payload.Timestamp <= context.Snapshot.GetHeader(context.PrevHash).Timestamp || payload.Timestamp > DateTime.Now.AddMinutes(10).ToTimestamp())
            {
                Log($"Timestamp incorrect: {payload.Timestamp}", LogLevel.Warning);
                return;
            }

            //After receiving message from speaker start timeout if consensus not reached in time
            timer.Change(TimeSpan.FromSeconds(fixedFirstChangeViewTimeOut), Timeout.InfiniteTimeSpan);

            context.State |= ConsensusState.RequestReceived;
            context.Timestamp = payload.Timestamp;
            context.Nonce = message.Nonce;
            context.NextConsensus = message.NextConsensus;
            context.TransactionHashes = message.TransactionHashes;
            context.Transactions = new Dictionary<UInt256, Transaction>();
            if (!Crypto.Default.VerifySignature(context.MakeHeader().GetHashData(), message.Signature, context.Validators[payload.ValidatorIndex].EncodePoint(false))) return;
            context.Signatures = new byte[context.Validators.Length][];
            context.Signatures[payload.ValidatorIndex] = message.Signature;
<<<<<<< HEAD
            Dictionary<UInt256, Transaction> mempool = LocalNode.GetMemoryPool().ToDictionary(p => p.Hash);

            Console.WriteLine($"(CONSENSUS SERVICE): mem_pool.Count: {mempool.Count} Transactions.Count: {context.Transactions.Count} context.TransactionHashes.Length: {context.TransactionHashes.Length}");

=======
            Dictionary<UInt256, Transaction> mempool = Blockchain.Singleton.GetMemoryPool().ToDictionary(p => p.Hash);
>>>>>>> bd5c707f58f0acef06ed69596865481480e6a554
            foreach (UInt256 hash in context.TransactionHashes.Skip(1))
            {

                if (mempool.TryGetValue(hash, out Transaction tx))
                    if (!AddTransaction(tx, false))
                        return;
            }
            if (!AddTransaction(message.MinerTransaction, true)) return;
            if (context.Transactions.Count < context.TransactionHashes.Length)
            {
                UInt256[] hashes = context.TransactionHashes.Where(i => !context.Transactions.ContainsKey(i)).ToArray();
                system.TaskManager.Tell(new TaskManager.RestartTasks
                {
                    Payload = InvPayload.Create(InventoryType.TX, hashes)
                });
            }
        }

        private void OnPrepareResponseReceived(ConsensusPayload payload, PrepareResponse message)
        {
            Log($"{nameof(OnPrepareResponseReceived)}: height={payload.BlockIndex} view={message.ViewNumber} index={payload.ValidatorIndex}");
            if (context.State.HasFlag(ConsensusState.BlockSent)) return;
            if (context.Signatures[payload.ValidatorIndex] != null) return;
            Block header = context.MakeHeader();
            if (header == null || !Crypto.Default.VerifySignature(header.GetHashData(), message.Signature, context.Validators[payload.ValidatorIndex].EncodePoint(false))) return;
            context.Signatures[payload.ValidatorIndex] = message.Signature;
            Console.WriteLine("(CONSENSUS SERVICE) OnPrepareResponseReceived - Checking signature");
            CheckSignatures();
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
<<<<<<< HEAD
                if (timer_height != context.BlockIndex || timer_view != context.ViewNumber) return;
                Log($"timeout: height={timer_height} view={timer_view} state={context.State}");
                if (context.State.HasFlag(ConsensusState.Primary) && !context.State.HasFlag(ConsensusState.RequestSent))
                {
                    Log($"send prepare request: height={timer_height} view={timer_view}");
                    context.State |= ConsensusState.RequestSent;
                    if (!context.State.HasFlag(ConsensusState.SignatureSent))
                    {
                        //opt time self-adjustement should start calculuss from here, when actions are triggered
                        dInit = DateTime.Now;

                        FillContext();
                        context.Timestamp = Math.Max(DateTime.Now.ToTimestamp(), Blockchain.Default.GetHeader(context.PrevHash).Timestamp + 1);
                        context.Signatures[context.MyIndex] = context.MakeHeader().Sign(context.KeyPair);
                    }
                    SignAndRelay(context.MakePrepareRequest());

                    //===========================================================
                    // Opt blocks
                    if (dInit != DateTime.MinValue && timer_view == 0)
                    {
                        blockTimes.Add((double)(DateTime.Now - dInit).GetValueOrDefault().TotalSeconds);

                        if (blockTimes.Count() > fixedNumberOfBlocksToGetAvg)
                            blockTimes.RemoveAt(0);
                    }
                    //===========================================================

                    //Time waiting for other signatures from Backups
                    timer.Change(TimeSpan.FromSeconds(fixedFirstChangeViewTimeOut), Timeout.InfiniteTimeSpan);
                }
                else if ((context.State.HasFlag(ConsensusState.Primary) && context.State.HasFlag(ConsensusState.RequestSent)) || context.State.HasFlag(ConsensusState.Backup))
=======
                case Start _:
                    OnStart();
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
            Log("OnStart");
            InitializeConsensus(0);
        }

        private void OnTimer(Timer timer)
        {
            if (timer.Height != context.BlockIndex || timer.ViewNumber != context.ViewNumber) return;
            Log($"timeout: height={timer.Height} view={timer.ViewNumber} state={context.State}");
            if (context.State.HasFlag(ConsensusState.Primary) && !context.State.HasFlag(ConsensusState.RequestSent))
            {
                Log($"send prepare request: height={timer.Height} view={timer.ViewNumber}");
                context.State |= ConsensusState.RequestSent;
                if (!context.State.HasFlag(ConsensusState.SignatureSent))
>>>>>>> bd5c707f58f0acef06ed69596865481480e6a554
                {
                    context.Timestamp = Math.Max(DateTime.Now.ToTimestamp(), context.Snapshot.GetHeader(context.PrevHash).Timestamp + 1);
                    context.Signatures[context.MyIndex] = context.MakeHeader().Sign(context.KeyPair);
                }
                SignAndRelay(context.MakePrepareRequest());
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
            if (!context.State.HasFlag(ConsensusState.Backup) || !context.State.HasFlag(ConsensusState.RequestReceived) || context.State.HasFlag(ConsensusState.SignatureSent) || context.State.HasFlag(ConsensusState.ViewChanging))
                return;
            if (context.Transactions.ContainsKey(transaction.Hash)) return;
            if (!context.TransactionHashes.Contains(transaction.Hash)) return;
            AddTransaction(transaction, true);
        }

        protected override void PostStop()
        {
            Log("OnStop");
            context.Dispose();
            base.PostStop();
        }

        public static Props Props(NeoSystem system, Wallet wallet)
        {
            return Akka.Actor.Props.Create(() => new ConsensusService(system, wallet)).WithMailbox("consensus-service-mailbox");
        }

        private void RequestChangeView()
        {
            context.State |= ConsensusState.ViewChanging;
            context.ExpectedView[context.MyIndex]++;
            Log($"request change view: height={context.BlockIndex} view={context.ViewNumber} nv={context.ExpectedView[context.MyIndex]} state={context.State}");
<<<<<<< HEAD
=======
            ChangeTimer(TimeSpan.FromSeconds(Blockchain.SecondsPerBlock << (context.ExpectedView[context.MyIndex] + 1)));
>>>>>>> bd5c707f58f0acef06ed69596865481480e6a554
            SignAndRelay(context.MakeChangeView());
            timer.Change(TimeSpan.FromSeconds(fixedFirstChangeViewTimeOut), Timeout.InfiniteTimeSpan);
            CheckExpectedView(context.ExpectedView[context.MyIndex]);

            //Time waiting until next change view
            //timer.Change(TimeSpan.FromSeconds(30), Timeout.InfiniteTimeSpan);
        }

        private void SignAndRelay(ConsensusPayload payload)
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
<<<<<<< HEAD
            sc.Verifiable.Scripts = sc.GetScripts();

            /*
            //================================================
            //Check if any wait is needed in order to carve desired block time
            TimeSpan span = DateTime.Now - block_last_time;
            while ( span  <= Blockchain.TimePerBlock)
            {
                span = DateTime.Now - block_last_time;
            }             
            //================================================
            */

            localNode.RelayDirectly(payload);
=======
            sc.Verifiable.Witnesses = sc.GetWitnesses();
            system.LocalNode.Tell(new LocalNode.SendDirectly { Inventory = payload });
>>>>>>> bd5c707f58f0acef06ed69596865481480e6a554
        }
    }

    internal class ConsensusServiceMailbox : PriorityMailbox
    {
        public ConsensusServiceMailbox(Akka.Actor.Settings settings, Config config)
            : base(settings, config)
        {
<<<<<<< HEAD
            Log("OnStart");
            started = true;
            Blockchain.PersistUnlocked += Blockchain_PersistUnlocked;
            LocalNode.InventoryReceiving += LocalNode_InventoryReceiving;
            LocalNode.InventoryReceived += LocalNode_InventoryReceived;
            InitializeConsensus(0);
=======
        }

        protected override bool IsHighPriority(object message)
        {
            switch (message)
            {
                case ConsensusPayload _:
                case ConsensusService.Timer _:
                case Blockchain.PersistCompleted _:
                    return true;
                default:
                    return false;
            }
>>>>>>> bd5c707f58f0acef06ed69596865481480e6a554
        }
    }
}