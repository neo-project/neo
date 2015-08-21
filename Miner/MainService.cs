using AntShares.Core;
using AntShares.Data;
using AntShares.Network;
using AntShares.Services;
using AntShares.Threading;
using System;
using System.Threading;

namespace AntShares
{
    internal class MainService : ConsoleServiceBase
    {
        private LevelDBBlockchain blockchain;
        private LocalNode localnode;
        private CancellableTask task;

        protected override string Prompt => "ant";
        public override string ServiceName => "AntSharesMiner";

        private void Blockchain_PersistCompleted(object sender, EventArgs e)
        {
            task.Cancel();
            task.Run();
        }

        private void Mine(CancellationToken token)
        {
            //TODO: 挖矿
            //1. 首先，统计当前的矿工列表，看自己是否在名单中；如果不在名单中，则直接放弃本轮挖矿动作；
            //2. 基于本地区块链的当前区块开始构造共识数据；
            //3. 将共识数据广播到矿工网络；
            //4. 组合所有其它矿工的共识数据；
            //5. 签名并广播；
            //6. 广播最终共识后的区块；
        }

        protected internal override void OnStart()
        {
            blockchain = new LevelDBBlockchain();
            blockchain.PersistCompleted += Blockchain_PersistCompleted;
            Blockchain.RegisterBlockchain(blockchain);
            localnode = new LocalNode();
            localnode.Start();
            task = new CancellableTask(Mine);
            task.Run();
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
