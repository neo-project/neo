using AntShares.Core;
using AntShares.Core.Scripts;
using AntShares.Implementations.Wallets.EntityFramework;
using AntShares.Miner.Consensus;
using AntShares.Network.Payloads;
using AntShares.Shell;
using AntShares.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;

namespace AntShares.Miner
{
    internal class MinerService : MainService
    {
        private ConsensusContext context = new ConsensusContext();
        private UserWallet wallet;

        private MinerTransaction CreateMinerTransaction(IEnumerable<Transaction> transactions, uint height, ulong nonce)
        {
            Fixed8 amount_in = transactions.SelectMany(p => p.References.Values.Where(o => o.AssetId == Blockchain.AntCoin.Hash)).Sum(p => p.Value);
            Fixed8 amount_out = transactions.SelectMany(p => p.Outputs.Where(o => o.AssetId == Blockchain.AntCoin.Hash)).Sum(p => p.Value);
            Fixed8 amount_sysfee = transactions.Sum(p => p.SystemFee);
            Fixed8 amount_netfee = amount_in - amount_out - amount_sysfee;
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
                }
            }
            StartMine();
            return true;
        }

        private void StartMine()
        {
            lock (context)
            {
                context.Reset();
                int my_id = -1;
                for (int i = 0; i < context.Miners.Length; i++)
                {
                    if (wallet.ContainsAccount(context.Miners[i]))
                    {
                        my_id = i;
                        break;
                    }
                }
                if (my_id < 0) return;
                int pi = ((int)context.Height - context.ViewNumber) % context.Miners.Length;
                if (pi < 0) pi += context.Miners.Length;
                if (pi == my_id)
                {
                    List<Transaction> transactions = Blockchain.Default.GetMemoryPool().ToList();
                    transactions.Insert(0, CreateMinerTransaction(transactions, context.Height, context.Nonce));
                    //TODO: Run Primary
                    //1. 填充共识上下文
                    context.State = ConsensusState.Primary;
                    context.Timestamp = DateTime.Now.ToTimestamp();
                    context.Nonce = GetNonce();
                    context.TransactionHashes = transactions.Select(p => p.Hash).ToArray();
                    context.Transactions = transactions.ToArray();
                    //2. 构造共识请求
                    ConsensusPayload payload = context.MakePerpareRequest((ushort)my_id);
                    SignatureContext sc = new SignatureContext(payload);
                    wallet.Sign(sc);
                    sc.Signable.Scripts = sc.GetScripts();
                    //3. 发送
                    var eatwarning = LocalNode.RelayAsync(payload);
                    //4. 构造区块头并签名
                    Block header = context.MakeHeader();
                    context.Signatures[my_id] = header.Sign(wallet.GetAccount(context.Miners[my_id]));
                }
                else
                {
                    //TODO: Run Backup
                }
            }
        }
    }
}
