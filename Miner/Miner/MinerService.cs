using AntShares.Core;
using AntShares.Core.Scripts;
using AntShares.Implementations.Wallets.EntityFramework;
using AntShares.Miner.Consensus;
using AntShares.Network;
using AntShares.Network.Payloads;
using AntShares.Shell;
using AntShares.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading;

namespace AntShares.Miner
{
    internal class MinerService : MainService
    {
        private ConsensusContext context = new ConsensusContext();
        private UserWallet wallet;
        private Timer timer;
        private uint timer_height;
        private byte timer_view;
        private DateTime block_received_time;

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
                Log($"SendPerpareResponse");
                context.State |= ConsensusState.SignatureSent;
                context.Signatures[context.MinerIndex] = context.MakeHeader().Sign(wallet.GetAccount(context.Miners[context.MinerIndex]));
                SignAndRelay(context.MakePerpareResponse(context.Signatures[context.MinerIndex]));
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
            if (context.Signatures.Count(p => p != null) >= context.M)
            {
                Log($"{nameof(CheckSignatures)} {context.Signatures.Count(p => p != null)}/{context.M}");
                Contract contract = MultiSigContract.Create(context.Miners[context.MinerIndex].EncodePoint(true).ToScriptHash(), context.M, context.Miners);
                Block block = context.MakeHeader();
                SignatureContext sc = new SignatureContext(block);
                for (int i = 0; i < context.Miners.Length; i++)
                    if (context.Signatures[i] != null)
                        sc.Add(contract, context.Miners[i], context.Signatures[i]);
                sc.Signable.Scripts = sc.GetScripts();
                block.Transactions = context.TransactionHashes.Select(p => context.Transactions[p]).ToArray();
                LocalNode.Relay(block);
                context.State |= ConsensusState.BlockSent;
                Log($"RelayBlock hash:{block.Hash}");
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
                    context.State = ConsensusState.Primary;
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

        private void LocalNode_NewInventory(object sender, Inventory inventory)
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

        private static void Log(string message)
        {
            Console.WriteLine($"[{DateTime.Now.TimeOfDay:hh\\:mm\\:ss}] {message}");
        }

        private void OnChangeViewReceived(ConsensusPayload payload, ChangeView message)
        {
            Log($"{nameof(OnChangeViewReceived)} h:{payload.Height} v:{message.ViewNumber} i:{payload.MinerIndex} nv:{message.NewViewNumber}");
            if (message.NewViewNumber <= context.ExpectedView[payload.MinerIndex])
                return;
            context.ExpectedView[payload.MinerIndex] = message.NewViewNumber;
            CheckExpectedView(message.NewViewNumber);
        }

        protected override bool OnCommand(string[] args)
        {
            switch (args[0].ToLower())
            {
                case "create":
                    return OnCreateCommand(args);
                case "open":
                    return OnOpenCommand(args);
                default:
                    return base.OnCommand(args);
            }
        }

        private bool OnCreateCommand(string[] args)
        {
            switch (args[1].ToLower())
            {
                case "wallet":
                    return OnCreateWalletCommand(args);
                default:
                    return base.OnCommand(args);
            }
        }

        private bool OnCreateWalletCommand(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("error");
                return true;
            }
            using (SecureString password = ReadSecureString("password"))
            using (SecureString password2 = ReadSecureString("password"))
            {
                if (!password.CompareTo(password2))
                {
                    Console.WriteLine("error");
                    return true;
                }
                wallet = UserWallet.Create(args[2], password);
                foreach (Account account in wallet.GetAccounts())
                {
                    Console.WriteLine(account.PublicKey.EncodePoint(true).ToHexString());
                }
            }
            return true;
        }

        private bool OnOpenCommand(string[] args)
        {
            switch (args[1].ToLower())
            {
                case "wallet":
                    return OnOpenWalletCommand(args);
                default:
                    return base.OnCommand(args);
            }
        }

        //TODO: 目前没有想到其它安全的方法来保存密码
        //所以只能暂时手动输入，但如此一来就不能以服务的方式启动了
        //未来再想想其它办法，比如采用智能卡之类的
        private bool OnOpenWalletCommand(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("error");
                return true;
            }
            using (SecureString password = ReadSecureString("password"))
            {
                if (password.Length == 0)
                {
                    Console.WriteLine("cancelled");
                    return true;
                }
                try
                {
                    wallet = UserWallet.Open(args[2], password);
                }
                catch
                {
                    Console.WriteLine($"failed to open file \"{args[2]}\"");
                    return true;
                }
            }
            StartMine();
            return true;
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
            context.TransactionHashes = message.TransactionHashes;
            context.Transactions = new Dictionary<UInt256, Transaction>();
            if (!context.MakeHeader().VerifySignature(context.Miners[payload.MinerIndex], message.Signature)) return;
            context.Signatures[payload.MinerIndex] = message.Signature;
            if (!AddTransaction(message.MinerTransaction)) return;
            Dictionary<UInt256, Transaction> mempool = LocalNode.GetMemoryPool().ToDictionary(p => p.Hash);
            foreach (UInt256 hash in context.TransactionHashes.Skip(1))
                if (mempool.ContainsKey(hash))
                    if (!AddTransaction(mempool[hash]))
                        return;
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

        protected internal override void OnStop()
        {
            Log($"{nameof(OnStop)}");
            if (timer != null) timer.Dispose();
            Blockchain.PersistCompleted -= Blockchain_PersistCompleted;
            LocalNode.NewInventory -= LocalNode_NewInventory;
            base.OnStop();
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
                    context.Timestamp = Math.Max(DateTime.Now.ToTimestamp(), Blockchain.Default.GetHeader(context.PrevHash).Timestamp + 1);
                    context.Nonce = GetNonce();
                    List<Transaction> transactions = LocalNode.GetMemoryPool().ToList();
                    transactions.Insert(0, CreateMinerTransaction(transactions, context.Height, context.Nonce));
                    context.TransactionHashes = transactions.Select(p => p.Hash).ToArray();
                    context.Transactions = transactions.ToDictionary(p => p.Hash);
                    context.Signatures[context.MinerIndex] = context.MakeHeader().Sign(wallet.GetAccount(context.Miners[context.MinerIndex]));
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
            LocalNode.Relay(payload);
        }

        private void StartMine()
        {
            Log($"{nameof(StartMine)}");
            ShowPrompt = false;
            timer = new Timer(OnTimeout, null, Timeout.Infinite, Timeout.Infinite);
            Blockchain.PersistCompleted += Blockchain_PersistCompleted;
            LocalNode.NewInventory += LocalNode_NewInventory;
            InitializeConsensus(0);
        }
    }
}
