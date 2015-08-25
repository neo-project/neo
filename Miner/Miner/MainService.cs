using AntShares.Core;
using AntShares.Data;
using AntShares.Network;
using AntShares.Services;
using AntShares.Threading;
using AntShares.Wallets;
using System;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace AntShares.Miner
{
    internal class MainService : ConsoleServiceBase
    {
        private LevelDBBlockchain blockchain;
        private LocalNode localnode;
        private MinerWallet wallet;
        private CancellableTask task;

        protected override string Prompt => "ant";
        public override string ServiceName => "AntSharesMiner";

        private void Blockchain_PersistCompleted(object sender, EventArgs e)
        {
            if (task != null)
            {
                task.Cancel();
                task.Run();
            }
        }

        private async Task Mine(CancellationToken token)
        {
            BlockConsensusContext context = BlockConsensusContext.Create(wallet.PublicKey);
            token.ThrowIfCancellationRequested();
            BlockConsensusRequest request = context.CreateRequest(wallet);
            token.ThrowIfCancellationRequested();

            //TODO: 挖矿
            //1. 将共识数据广播到矿工网络；
            //2. 组合所有其它矿工的共识数据；
            //3. 签名并广播；
            //4. 广播最终共识后的区块；
        }

        protected override bool OnCommand(string[] args)
        {
            switch (args[0].ToLower())
            {
                case "open":
                    return OnOpenCommand(args.Skip(1).ToArray());
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
        private void OnOpenWalletCommand(string path)
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
            task = new CancellableTask(Mine);
            task.Run();
        }

        protected internal override void OnStart()
        {
            blockchain = new LevelDBBlockchain();
            blockchain.PersistCompleted += Blockchain_PersistCompleted;
            Blockchain.RegisterBlockchain(blockchain);
            localnode = new LocalNode();
            localnode.Start();
        }

        protected internal override void OnStop()
        {
            blockchain.PersistCompleted -= Blockchain_PersistCompleted;
            task.Cancel();
            task.Wait();
            localnode.Dispose();
            blockchain.Dispose();
        }
    }
}
