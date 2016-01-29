using AntShares.Core;
using AntShares.Core.Scripts;
using AntShares.Cryptography.ECC;
using AntShares.Network;
using AntShares.Shell;
using AntShares.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace AntShares.Miner
{
    internal class MinerService : MainService
    {
        private MinerWallet wallet;
        private CancellationTokenSource source = new CancellationTokenSource();
        private bool stopped = false;

        private GenerationTransaction CreateGenerationTransaction(IEnumerable<Transaction> transactions, uint height, ulong nonce)
        {
            var antshares = Blockchain.Default.GetUnspentAntShares().GroupBy(p => p.ScriptHash, (k, g) => new
            {
                ScriptHash = k,
                Amount = g.Sum(p => p.Value)
            }).OrderBy(p => p.Amount).ThenBy(p => p.ScriptHash).ToArray();
            Fixed8 amount_in = transactions.SelectMany(p => p.References.Values.Where(o => o.AssetId == Blockchain.AntCoin.Hash)).Sum(p => p.Value);
            Fixed8 amount_out = transactions.SelectMany(p => p.Outputs.Where(o => o.AssetId == Blockchain.AntCoin.Hash)).Sum(p => p.Value);
            Fixed8 amount_sysfee = transactions.Sum(p => p.SystemFee);
            Fixed8 quantity = Blockchain.Default.GetQuantityIssued(Blockchain.AntCoin.Hash);
            List<TransactionOutput> outputs = new List<TransactionOutput>
            {
                new TransactionOutput
                {
                    AssetId = Blockchain.AntCoin.Hash,
                    Value = amount_in - amount_out - amount_sysfee,
                    ScriptHash = wallet.GetContracts().First().ScriptHash
                }
            };
            if (height % Blockchain.MintingInterval == 0 && antshares.Length > 0)
            {
                ulong n = nonce % (ulong)antshares.Sum(p => p.Amount).GetData();
                ulong line = 0;
                int i = -1;
                do
                {
                    line += (ulong)antshares[++i].Amount.GetData();
                } while (line <= n);
                outputs.Add(new TransactionOutput
                {
                    AssetId = Blockchain.AntCoin.Hash,
                    Value = Fixed8.FromDecimal((Blockchain.AntCoin.Amount - (quantity - amount_sysfee)).ToDecimal() * Blockchain.GenerationFactor),
                    ScriptHash = antshares[i].ScriptHash
                });
            }
            return new GenerationTransaction
            {
                Nonce = (uint)(nonce % (uint.MaxValue + 1ul)),
                Attributes = new TransactionAttribute[0],
                Inputs = new TransactionInput[0],
                Outputs = outputs.GroupBy(p => p.ScriptHash, (k, g) => new TransactionOutput
                {
                    AssetId = Blockchain.AntCoin.Hash,
                    Value = g.Sum(p => p.Value),
                    ScriptHash = k
                }).Where(p => p.Value != Fixed8.Zero).ToArray(),
                Scripts = new Script[0]
            };
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
                wallet = MinerWallet.Create(args[2], password);
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
                    wallet = MinerWallet.Open(args[2], password);
                }
                catch
                {
                    Console.WriteLine($"failed to open file \"{args[2]}\"");
                }
            }
            return true;
        }

        protected internal override void OnStart(string[] args)
        {
            base.OnStart(args);
            StartMine(source.Token);
        }

        protected internal override void OnStop()
        {
            source.Cancel();
            while (!stopped)
            {
                Thread.Sleep(100);
            }
            base.OnStop();
        }

        private async void StartMine(CancellationToken token)
        {
            while (wallet == null && !token.IsCancellationRequested)
            {
                await Task.Delay(100);
            }
            while (!token.IsCancellationRequested)
            {
                ECPoint[] miners = Blockchain.Default.GetMiners();
                bool is_miner = false;
                foreach (Account account in wallet.GetAccounts())
                {
                    if (miners.Contains(account.PublicKey))
                    {
                        is_miner = true;
                        break;
                    }
                }
                if (!is_miner)
                {
                    try
                    {
                        await Task.Delay(Blockchain.TimePerBlock, token);
                    }
                    catch (TaskCanceledException) { }
                    continue;
                }
                Block header = Blockchain.Default.GetHeader(Blockchain.Default.CurrentBlockHash);
                if (header == null) continue;
                TimeSpan timespan = header.Timestamp.ToDateTime() + Blockchain.TimePerBlock - DateTime.Now;
                if (timespan > TimeSpan.Zero)
                {
                    try
                    {
                        await Task.Delay(timespan, token);
                    }
                    catch (TaskCanceledException) { }
                    if (token.IsCancellationRequested) break;
                }
                byte[] nonce_data = new byte[sizeof(ulong)];
                using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
                {
                    rng.GetBytes(nonce_data);
                }
                ulong nonce = BitConverter.ToUInt64(nonce_data, 0);
                List<Transaction> transactions = Blockchain.Default.GetMemoryPool().ToList();
                transactions.Insert(0, CreateGenerationTransaction(transactions, header.Height + 1, nonce));
                Block block = new Block
                {
                    PrevBlock = header.Hash,
                    Timestamp = DateTime.Now.ToTimestamp(),
                    Height = header.Height + 1,
                    Nonce = nonce,
                    NextMiner = Blockchain.GetMinerAddress(Blockchain.Default.GetMiners(transactions).ToArray()),
                    Transactions = transactions.ToArray()
                };
                block.RebuildMerkleRoot();
                wallet.Sign(block, miners);
                await LocalNode.RelayAsync(block);
                while (Blockchain.Default.CurrentBlockHash != block.Hash && !token.IsCancellationRequested)
                {
                    await Task.Delay(100, token);
                }
            }
            stopped = true;
        }
    }
}
