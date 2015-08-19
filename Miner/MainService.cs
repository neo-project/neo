using AntShares.Core;
using AntShares.Data;
using AntShares.Network;
using AntShares.Services;
using System;

namespace AntShares
{
    internal class MainService : ConsoleServiceBase
    {
        private LevelDBBlockchain blockchain;
        private LocalNode localnode;

        protected override string Prompt => "ant";
        public override string ServiceName => "AntSharesMiner";

        private void Blockchain_PersistCompleted(object sender, EventArgs e)
        {
            //TODO: 挖矿
            //1. 首先，统计当前的矿工列表，看自己是否在名单中；
            //2. 基于本地区块链的当前区块开始挖；
            //3. 如果收到新的区块广播，则停止当前的挖矿工作，重新从第一步开始；
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
            localnode.Dispose();
            blockchain.Dispose();
        }
    }
}
