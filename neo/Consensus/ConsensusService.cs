using Neo.Core;
using Neo.Cryptography;
using Neo.IO;
using Neo.Network;
using Neo.Network.Payloads;
using Neo.Plugins;
using Neo.SmartContract;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Neo.Consensus
{
    public class ConsensusService : IDisposable
    {
        private ConsensusContext context = new ConsensusContext();
        private LocalNode localNode;
        private Wallet wallet;
        private Timer timer;
        private uint timer_height;
        private byte timer_view;
        private DateTime block_received_time;
        private bool started = false;

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
        {
            this.localNode = localNode;
            this.wallet = wallet;
            this.timer = new Timer(OnTimeout, null, Timeout.Infinite, Timeout.Infinite);
        }

        private bool AddTransaction(Transaction tx, bool verify)
        {
            if (Blockchain.Default.ContainsTransaction(tx.Hash) ||
                (verify && !tx.Verify(context.Transactions.Values)) ||
                !CheckPolicy(tx))
            {
                Log($"reject tx: {tx.Hash}{Environment.NewLine}{tx.ToArray().ToHexString()}");
                RequestChangeView();
                return false;
            }
            context.Transactions[tx.Hash] = tx;
            Console.WriteLine("(CS-AD) context.TransactionHashes.Length:" + context.TransactionHashes.Length);
            Console.WriteLine("(CS-AD) context.Transactions.Count:" + context.Transactions.Count);

            if (context.TransactionHashes.Length == context.Transactions.Count)
            {

                if (Blockchain.GetConsensusAddress(Blockchain.Default.GetValidators(context.Transactions.Values).ToArray()).Equals(context.NextConsensus))
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

            foreach (PolicyPlugin plugin in PolicyPlugin.Instances)
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
                sc.Verifiable.Scripts = sc.GetScripts();
                block.Transactions = context.TransactionHashes.Select(p => context.Transactions[p]).ToArray();
                Log($"relay block: {block.Hash}");
                if (!localNode.Relay(block))
                    Log($"reject block: {block.Hash}");
                context.State |= ConsensusState.BlockSent;
            }
        }

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
                    Scripts = new Witness[0]
                };
                if (Blockchain.Default.GetTransaction(tx.Hash) == null)
                {
                    context.Nonce = nonce;
                    transactions.Insert(0, tx);
                    break;
                }
            }
            context.TransactionHashes = transactions.Select(p => p.Hash).ToArray();
            context.Transactions = transactions.ToDictionary(p => p.Hash);
            context.NextConsensus = Blockchain.GetConsensusAddress(Blockchain.Default.GetValidators(transactions).ToArray());
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
            lock (context)
            {
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

                }
                else
                {

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
                }
            }
        }

        private void LocalNode_InventoryReceiving(object sender, InventoryReceivingEventArgs e)
        {
            Transaction tx = e.Inventory as Transaction;
            if (tx != null)
            {
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
            }
        }

        protected virtual void Log(string message)
        {
        }

        private void OnChangeViewReceived(ConsensusPayload payload, ChangeView message)
        {
            Log($"{nameof(OnChangeViewReceived)}: height={payload.BlockIndex} view={message.ViewNumber} index={payload.ValidatorIndex} nv={message.NewViewNumber}");

            if (message.NewViewNumber <= context.ExpectedView[payload.ValidatorIndex])
                return;
            context.ExpectedView[payload.ValidatorIndex] = message.NewViewNumber;
            CheckExpectedView(message.NewViewNumber);
        }

        private void OnPrepareRequestReceived(ConsensusPayload payload, PrepareRequest message)
        {
            Log($"{nameof(OnPrepareRequestReceived)}: height={payload.BlockIndex} view={message.ViewNumber} index={payload.ValidatorIndex} tx={message.TransactionHashes.Length}");
            if (!context.State.HasFlag(ConsensusState.Backup) || context.State.HasFlag(ConsensusState.RequestReceived))
                return;
            if (payload.ValidatorIndex != context.PrimaryIndex) return;
            if (payload.Timestamp <= Blockchain.Default.GetHeader(context.PrevHash).Timestamp || payload.Timestamp > DateTime.Now.AddMinutes(10).ToTimestamp())
            {
                Log($"Timestamp incorrect: {payload.Timestamp}");
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
            Dictionary<UInt256, Transaction> mempool = LocalNode.GetMemoryPool().ToDictionary(p => p.Hash);

            Console.WriteLine($"(CONSENSUS SERVICE): mem_pool.Count: {mempool.Count} Transactions.Count: {context.Transactions.Count} context.TransactionHashes.Length: {context.TransactionHashes.Length}");

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
                LocalNode.AllowHashes(hashes);
                InvPayload msg = InvPayload.Create(InventoryType.TX, hashes);
                foreach (RemoteNode node in localNode.GetRemoteNodes())
                    node.EnqueueMessage("getdata", msg);
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

        private void OnTimeout(object state)
        {
            lock (context)
            {
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
                {
                    RequestChangeView();
                }
            }
        }

        private void RequestChangeView()
        {
            context.State |= ConsensusState.ViewChanging;
            context.ExpectedView[context.MyIndex]++;
            Log($"request change view: height={context.BlockIndex} view={context.ViewNumber} nv={context.ExpectedView[context.MyIndex]} state={context.State}");
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
        }

        public void Start()
        {
            Log("OnStart");
            started = true;
            Blockchain.PersistUnlocked += Blockchain_PersistUnlocked;
            LocalNode.InventoryReceiving += LocalNode_InventoryReceiving;
            LocalNode.InventoryReceived += LocalNode_InventoryReceived;
            InitializeConsensus(0);
        }
    }
}