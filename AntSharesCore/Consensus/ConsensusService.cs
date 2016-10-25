using AntShares.Core;
using AntShares.Core.Scripts;
using AntShares.Network;
using AntShares.Network.Payloads;
using AntShares.Wallets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace AntShares.Consensus
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
        private string log_dictionary;
        private bool started = false;

        public ConsensusService(LocalNode localNode, Wallet wallet, string log_dictionary = null)
        {
            this.localNode = localNode;
            this.wallet = wallet;
            this.timer = new Timer(OnTimeout, null, Timeout.Infinite, Timeout.Infinite);
            this.log_dictionary = log_dictionary;
        }

        private bool AddTransaction(Transaction tx)
        {
            Log($"{nameof(AddTransaction)} hash:{tx.Hash}");
            if (context.Transactions.SelectMany(p => p.Value.GetAllInputs()).Intersect(tx.GetAllInputs()).Count() > 0 ||
                Blockchain.Default.ContainsTransaction(tx.Hash) ||
                !tx.Verify())
            {
                Log($"failed hash:{tx.Hash}");
                RequestChangeView();
                return false;
            }
            context.Transactions[tx.Hash] = tx;
            if (context.TransactionHashes.Length == context.Transactions.Count)
            {
                if (Blockchain.GetMinerAddress(Blockchain.Default.GetMiners(context.Transactions.Values).ToArray()).Equals(context.NextMiner))
                {
                    Log($"SendPerpareResponse");
                    context.State |= ConsensusState.SignatureSent;
                    context.Signatures[context.MinerIndex] = context.MakeHeader().Sign(wallet.GetAccount(context.Miners[context.MinerIndex]));
                    SignAndRelay(context.MakePerpareResponse(context.Signatures[context.MinerIndex]));
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

        private void Blockchain_PersistCompleted(object sender, Block block)
        {
            Log($"{nameof(Blockchain_PersistCompleted)} hash:{block.Hash}");
            block_received_time = DateTime.Now;
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

        private void CheckSignatures()
        {
            if (context.Signatures.Count(p => p != null) >= context.M && context.TransactionHashes.All(p => context.Transactions.ContainsKey(p)))
            {
                Log($"{nameof(CheckSignatures)} {context.Signatures.Count(p => p != null)}/{context.M}");
                Contract contract = Contract.CreateMultiSigContract(context.Miners[context.MinerIndex].EncodePoint(true).ToScriptHash(), context.M, context.Miners);
                Block block = context.MakeHeader();
                SignatureContext sc = new SignatureContext(block);
                for (int i = 0, j = 0; i < context.Miners.Length && j < context.M; i++)
                    if (context.Signatures[i] != null)
                    {
                        sc.Add(contract, context.Miners[i], context.Signatures[i]);
                        j++;
                    }
                sc.Signable.Scripts = sc.GetScripts();
                block.Transactions = context.TransactionHashes.Select(p => context.Transactions[p]).ToArray();
                Log($"RelayBlock hash:{block.Hash}");
                if (!localNode.Relay(block))
                    Log($"failed hash:{block.Hash}");
                context.State |= ConsensusState.BlockSent;
            }
        }

        private MinerTransaction CreateMinerTransaction(IEnumerable<Transaction> transactions, uint height, ulong nonce)
        {
            Fixed8 amount_netfee = Block.CalculateNetFee(transactions);
            TransactionOutput[] outputs = amount_netfee == Fixed8.Zero ? new TransactionOutput[0] : new[] { new TransactionOutput
            {
                AssetId = Blockchain.AntCoin.Hash,
                Value = amount_netfee,
                ScriptHash = wallet.GetContracts().First().ScriptHash
            } };
            return new MinerTransaction
            {
                Nonce = (uint)(nonce % (uint.MaxValue + 1ul)),
                Attributes = new TransactionAttribute[0],
                Inputs = new TransactionInput[0],
                Outputs = outputs,
                Scripts = new Script[0]
            };
        }

        public void Dispose()
        {
            Log("OnStop");
            if (timer != null) timer.Dispose();
            if (started)
            {
                Blockchain.PersistCompleted -= Blockchain_PersistCompleted;
                LocalNode.NewInventory -= LocalNode_NewInventory;
            }
        }

        private static ulong GetNonce()
        {
            byte[] nonce = new byte[sizeof(ulong)];
            Random rand = new Random();
            rand.NextBytes(nonce);
            return BitConverter.ToUInt64(nonce, 0);
        }

        private void InitializeConsensus(byte view_number)
        {
            lock (context)
            {
                if (view_number == 0)
                    context.Reset(wallet);
                else
                    context.ChangeView(view_number);
                if (context.MinerIndex < 0) return;
                Log($"{nameof(InitializeConsensus)} h:{context.Height} v:{view_number} i:{context.MinerIndex} s:{(context.MinerIndex == context.PrimaryIndex ? ConsensusState.Primary : ConsensusState.Backup)}");
                if (context.MinerIndex == context.PrimaryIndex)
                {
                    context.State |= ConsensusState.Primary;
                    timer_height = context.Height;
                    timer_view = view_number;
                    TimeSpan span = DateTime.Now - block_received_time;
                    if (span >= Blockchain.TimePerBlock)
                        timer.Change(0, Timeout.Infinite);
                    else
                        timer.Change(Blockchain.TimePerBlock - span, Timeout.InfiniteTimeSpan);
                }
                else
                {
                    context.State = ConsensusState.Backup;
                    timer_height = context.Height;
                    timer_view = view_number;
                    timer.Change(TimeSpan.FromSeconds(Blockchain.SecondsPerBlock << (view_number + 1)), Timeout.InfiniteTimeSpan);
                }
            }
        }

        private void LocalNode_NewInventory(object sender, IInventory inventory)
        {
            ConsensusPayload payload = inventory as ConsensusPayload;
            if (payload != null)
            {
                lock (context)
                {
                    if (payload.MinerIndex == context.MinerIndex) return;
                    if (payload.Version != ConsensusContext.Version || payload.PrevHash != context.PrevHash || payload.Height != context.Height)
                        return;
                    if (payload.MinerIndex >= context.Miners.Length) return;
                    ConsensusMessage message = ConsensusMessage.DeserializeFrom(payload.Data);
                    if (message.ViewNumber != context.ViewNumber && message.Type != ConsensusMessageType.ChangeView)
                        return;
                    switch (message.Type)
                    {
                        case ConsensusMessageType.ChangeView:
                            OnChangeViewReceived(payload, (ChangeView)message);
                            break;
                        case ConsensusMessageType.PerpareRequest:
                            OnPerpareRequestReceived(payload, (PerpareRequest)message);
                            break;
                        case ConsensusMessageType.PerpareResponse:
                            OnPerpareResponseReceived(payload, (PerpareResponse)message);
                            break;
                    }
                }
            }
            Transaction tx = inventory as Transaction;
            if (tx != null)
            {
                lock (context)
                {
                    if (!context.State.HasFlag(ConsensusState.Backup) || !context.State.HasFlag(ConsensusState.RequestReceived) || context.State.HasFlag(ConsensusState.SignatureSent))
                        return;
                    if (context.Transactions.ContainsKey(tx.Hash)) return;
                    if (!context.TransactionHashes.Contains(tx.Hash)) return;
                    AddTransaction(tx);
                }
            }
        }

        private void Log(string message)
        {
            DateTime now = DateTime.Now;
            string line = $"[{now.TimeOfDay:hh\\:mm\\:ss}] {message}";
            Console.WriteLine(line);
            if (string.IsNullOrEmpty(log_dictionary)) return;
            lock (log_dictionary)
            {
                Directory.CreateDirectory(log_dictionary);
                string path = Path.Combine(log_dictionary, $"{now:yyyy-MM-dd}.log");
                File.AppendAllLines(path, new[] { line });
            }
        }

        private void OnChangeViewReceived(ConsensusPayload payload, ChangeView message)
        {
            Log($"{nameof(OnChangeViewReceived)} h:{payload.Height} v:{message.ViewNumber} i:{payload.MinerIndex} nv:{message.NewViewNumber}");
            if (message.NewViewNumber <= context.ExpectedView[payload.MinerIndex])
                return;
            context.ExpectedView[payload.MinerIndex] = message.NewViewNumber;
            CheckExpectedView(message.NewViewNumber);
        }

        private void OnPerpareRequestReceived(ConsensusPayload payload, PerpareRequest message)
        {
            Log($"{nameof(OnPerpareRequestReceived)} h:{payload.Height} v:{message.ViewNumber} i:{payload.MinerIndex} tx:{message.TransactionHashes.Length}");
            if (!context.State.HasFlag(ConsensusState.Backup) || context.State.HasFlag(ConsensusState.RequestReceived))
                return;
            if (payload.MinerIndex != context.PrimaryIndex) return;
            if (payload.Timestamp <= Blockchain.Default.GetHeader(context.PrevHash).Timestamp || payload.Timestamp > DateTime.Now.AddMinutes(10).ToTimestamp())
            {
                Log($"Timestamp incorrect:{payload.Timestamp}");
                return;
            }
            context.State |= ConsensusState.RequestReceived;
            context.Timestamp = payload.Timestamp;
            context.Nonce = message.Nonce;
            context.NextMiner = message.NextMiner;
            context.TransactionHashes = message.TransactionHashes;
            context.Transactions = new Dictionary<UInt256, Transaction>();
            if (!context.MakeHeader().VerifySignature(context.Miners[payload.MinerIndex], message.Signature)) return;
            context.Signatures = new byte[context.Miners.Length][];
            context.Signatures[payload.MinerIndex] = message.Signature;
            if (!AddTransaction(message.MinerTransaction)) return;
            Dictionary<UInt256, Transaction> mempool = LocalNode.GetMemoryPool().ToDictionary(p => p.Hash);
            foreach (UInt256 hash in context.TransactionHashes.Skip(1))
                if (mempool.ContainsKey(hash))
                    if (!AddTransaction(mempool[hash]))
                        return;
            LocalNode.AllowHashes(context.TransactionHashes.Except(context.Transactions.Keys));
            if (context.Transactions.Count < context.TransactionHashes.Length)
                localNode.SynchronizeMemoryPool();
        }

        private void OnPerpareResponseReceived(ConsensusPayload payload, PerpareResponse message)
        {
            Log($"{nameof(OnPerpareResponseReceived)} h:{payload.Height} v:{message.ViewNumber} i:{payload.MinerIndex}");
            if (context.State.HasFlag(ConsensusState.BlockSent)) return;
            if (context.Signatures[payload.MinerIndex] != null) return;
            Block header = context.MakeHeader();
            if (header == null || !header.VerifySignature(context.Miners[payload.MinerIndex], message.Signature)) return;
            context.Signatures[payload.MinerIndex] = message.Signature;
            CheckSignatures();
        }

        private void OnTimeout(object state)
        {
            Log($"{nameof(OnTimeout)} h:{timer_height} v:{timer_view} state:{context.State}");
            lock (context)
            {
                if (timer_height != context.Height || timer_view != context.ViewNumber)
                {
                    Log($"ignored");
                    return;
                }
                if (context.State.HasFlag(ConsensusState.Primary) && !context.State.HasFlag(ConsensusState.RequestSent))
                {
                    Log($"SendPerpareRequest h:{timer_height} v:{timer_view}");
                    context.State |= ConsensusState.RequestSent;
                    if (!context.State.HasFlag(ConsensusState.SignatureSent))
                    {
                        context.Timestamp = Math.Max(DateTime.Now.ToTimestamp(), Blockchain.Default.GetHeader(context.PrevHash).Timestamp + 1);
                        context.Nonce = GetNonce();
                        List<Transaction> transactions = LocalNode.GetMemoryPool().ToList();
                        transactions.Insert(0, CreateMinerTransaction(transactions, context.Height, context.Nonce));
                        context.TransactionHashes = transactions.Select(p => p.Hash).ToArray();
                        context.Transactions = transactions.ToDictionary(p => p.Hash);
                        context.NextMiner = Blockchain.GetMinerAddress(Blockchain.Default.GetMiners(transactions).ToArray());
                        context.Signatures[context.MinerIndex] = context.MakeHeader().Sign(wallet.GetAccount(context.Miners[context.MinerIndex]));
                    }
                    SignAndRelay(context.MakePerpareRequest());
                    timer.Change(TimeSpan.FromSeconds(Blockchain.SecondsPerBlock << (timer_view + 1)), Timeout.InfiniteTimeSpan);
                }
                else if ((context.State.HasFlag(ConsensusState.Primary) && context.State.HasFlag(ConsensusState.RequestSent)) || context.State.HasFlag(ConsensusState.Backup))
                {
                    RequestChangeView();
                }
            }
        }

        private void RequestChangeView()
        {
            context.ExpectedView[context.MinerIndex]++;
            Log($"{nameof(RequestChangeView)} h:{context.Height} v:{context.ViewNumber} nv:{context.ExpectedView[context.MinerIndex]} state:{context.State}");
            timer.Change(TimeSpan.FromSeconds(Blockchain.SecondsPerBlock << (context.ExpectedView[context.MinerIndex] + 1)), Timeout.InfiniteTimeSpan);
            SignAndRelay(context.MakeChangeView());
            CheckExpectedView(context.ExpectedView[context.MinerIndex]);
        }

        private void SignAndRelay(ConsensusPayload payload)
        {
            SignatureContext sc;
            try
            {
                sc = new SignatureContext(payload);
            }
            catch (InvalidOperationException)
            {
                return;
            }
            wallet.Sign(sc);
            sc.Signable.Scripts = sc.GetScripts();
            localNode.Relay(payload);
        }

        public void Start()
        {
            Log("OnStart");
            started = true;
            Blockchain.PersistCompleted += Blockchain_PersistCompleted;
            LocalNode.NewInventory += LocalNode_NewInventory;
            InitializeConsensus(0);
        }
    }
}
