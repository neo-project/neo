using AntShares.Core;
using AntShares.Data;
using AntShares.Network;
using AntShares.Services;
using AntShares.Wallets;
using System;
using System.Linq;
using System.Security;
using System.Threading.Tasks;

namespace AntShares.Miner
{
    internal class MainService : ConsoleServiceBase
    {
        private LocalNode localnode;
        private MinerWallet wallet;
        private BlockConsensusContext context;

        protected override string Prompt => "ant";
        public override string ServiceName => "AntSharesMiner";

        private async void Blockchain_PersistCompleted(object sender, Block block)
        {
            context.Reset();
            await SendConsensusRequestAsync();
        }

        private void LocalNode_NewInventory(object sender, Inventory inventory)
        {
            if (inventory.InventoryType != InventoryType.ConsRequest)
                return;
            BlockConsensusRequest request = (BlockConsensusRequest)inventory;
            if (request.Verify() != VerificationResult.OK) return;
            context.AddRequest(request, wallet);
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
        private async void OnOpenWalletCommand(string path)
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
            await localnode.WaitForNodesAsync();
            context = new BlockConsensusContext(wallet.PublicKey);
            await SendConsensusRequestAsync();
        }

        protected internal override void OnStart()
        {
            Blockchain.RegisterBlockchain(new LevelDBBlockchain());
            Blockchain.Default.PersistCompleted += Blockchain_PersistCompleted;
            LocalNode.NewInventory += LocalNode_NewInventory;
            localnode = new LocalNode();
            localnode.Start();
        }

        protected internal override void OnStop()
        {
            LocalNode.NewInventory -= LocalNode_NewInventory;
            Blockchain.Default.PersistCompleted -= Blockchain_PersistCompleted;
            localnode.Dispose();
            Blockchain.Default.Dispose();
        }

        private async Task SendConsensusRequestAsync()
        {
            if (!context.Valid) return;
            BlockConsensusRequest request = context.CreateRequest(wallet);
            request.Script = wallet.Sign(request);
            await localnode.RelayAsync(request);
        }
    }
}
