using AntShares.Core;
using AntShares.Cryptography.ECC;
using AntShares.Implementations.Blockchains.LevelDB;
using AntShares.Network;
using AntShares.Properties;
using AntShares.Services;
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
    internal class MainService : ConsoleServiceBase
    {
        private LocalNode localnode;
        private MinerWallet wallet;
        private CancellationTokenSource source = new CancellationTokenSource();
        private bool stopped = false;

        protected override string Prompt => "ant";
        public override string ServiceName => "AntSharesMiner";

        protected override bool OnCommand(string[] args)
        {
            switch (args[0].ToLower())
            {
                case "open":
                    return OnOpenCommand(args.Skip(1).ToArray());
                case "show":
                    return OnShowCommand(args);
                default:
                    return base.OnCommand(args);
            }
        }

        private bool OnOpenCommand(string[] args)
        {
            if (args.Length >= 2 && string.Equals(args[0], "wallet", StringComparison.OrdinalIgnoreCase))
            {
                OnOpenWalletCommand(args.Skip(1).First());
            }
            else
            {
                Console.WriteLine("usage:\n\tOPEN WALLET <path>\n");
            }
            return true;
        }

        //TODO: 目前没有想到其它安全的方法来保存密码
        //所以只能暂时手动输入，但如此一来就不能以服务的方式启动了
        //未来再想想其它办法，比如采用智能卡之类的
        private /*async*/ void OnOpenWalletCommand(string path)
        {
            SecureString password = ReadSecureString("password");
            if (password.Length == 0)
            {
                Console.WriteLine("cancelled");
                return;
            }
            try
            {
                wallet = MinerWallet.Open(path, password);
            }
            catch
            {
                Console.WriteLine($"failed to open file \"{path}\"");
                return;
            }
        }

        private bool OnShowCommand(string[] args)
        {
            switch (args[1].ToLower())
            {
                case "state":
                    return OnShowStateCommand(args);
                default:
                    return base.OnCommand(args);
            }
        }

        private bool OnShowStateCommand(string[] args)
        {
            Console.WriteLine($"Height: {Blockchain.Default.Height}/{Blockchain.Default.HeaderHeight}, Nodes: {localnode.RemoteNodeCount}");
            return true;
        }

        protected internal override void OnStart()
        {
            Blockchain.RegisterBlockchain(new LevelDBBlockchain(Settings.Default.DataDirectoryPath));
            localnode = new LocalNode();
            localnode.Start();
            StartMine(source.Token);
        }

        protected internal override void OnStop()
        {
            source.Cancel();
            while (!stopped)
            {
                Thread.Sleep(100);
            }
            localnode.Dispose();
            Blockchain.Default.Dispose();
        }

        private async void StartMine(CancellationToken token)
        {
            while (wallet == null && !token.IsCancellationRequested)
            {
                await Task.Delay(100);
            }
            while (!token.IsCancellationRequested)
            {
                HashSet<ECPoint> miners = new HashSet<ECPoint>(Blockchain.Default.GetMiners());
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
                try
                {
                    await Task.Delay(header.Timestamp.ToDateTime() + Blockchain.TimePerBlock - DateTime.Now, token);
                }
                catch (TaskCanceledException) { }
                if (token.IsCancellationRequested) break;
                byte[] nonce = new byte[sizeof(ulong)];
                using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
                {
                    rng.GetBytes(nonce);
                }
                Transaction[] transactions = Blockchain.Default.GetMemoryPool().ToArray();
                Block block = new Block
                {
                    PrevBlock = header.Hash,
                    Timestamp = DateTime.Now.ToTimestamp(),
                    Height = header.Height + 1,
                    Nonce = BitConverter.ToUInt64(nonce, 0),
                    NextMiner = Blockchain.GetMinerAddress(Blockchain.Default.GetMiners(transactions).ToArray()),
                    Transactions = transactions
                };
                block.RebuildMerkleRoot();
                SignatureContext context = new SignatureContext(block);
                wallet.Sign(context);
                block.Script = context.GetScripts()[0];
                await localnode.RelayAsync(block);
            }
            stopped = true;
        }
    }
}
