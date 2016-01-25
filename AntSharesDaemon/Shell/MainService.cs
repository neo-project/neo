using AntShares.Core;
using AntShares.Implementations.Blockchains.LevelDB;
using AntShares.Network;
using AntShares.Network.RPC;
using AntShares.Properties;
using AntShares.Services;
using System;

namespace AntShares.Shell
{
    internal class MainService : ConsoleServiceBase
    {
        private RpcServer rpc;

        protected LocalNode LocalNode { get; private set; }
        protected override string Prompt => "ant";
        public override string ServiceName => "AntSharesDaemon";

        protected override bool OnCommand(string[] args)
        {
            switch (args[0].ToLower())
            {
                case "show":
                    return OnShowCommand(args);
                default:
                    return base.OnCommand(args);
            }
        }

        private bool OnShowCommand(string[] args)
        {
            switch (args[1].ToLower())
            {
                case "node":
                    return OnShowNodeCommand(args);
                case "state":
                    return OnShowStateCommand(args);
                default:
                    return base.OnCommand(args);
            }
        }

        private bool OnShowNodeCommand(string[] args)
        {
            RemoteNode[] nodes = LocalNode.GetRemoteNodes();
            for (int i = 0; i < nodes.Length; i++)
            {
                Console.WriteLine($"{nodes[i].RemoteEndpoint.Address} port:{nodes[i].RemoteEndpoint.Port} listen:{nodes[i].ListenerEndpoint?.Port ?? 0} [{i + 1}/{nodes.Length}]");
            }
            return true;
        }

        private bool OnShowStateCommand(string[] args)
        {
            Console.WriteLine($"Height: {Blockchain.Default.Height}/{Blockchain.Default.HeaderHeight}, Nodes: {LocalNode.RemoteNodeCount}");
            return true;
        }

        protected internal override void OnStart(string[] args)
        {
            Blockchain.RegisterBlockchain(new LevelDBBlockchain(Settings.Default.DataDirectoryPath));
            LocalNode = new LocalNode();
            LocalNode.Start();
            if (args.Length >= 1 && args[0] == "/rpc")
            {
                rpc = new RpcServer();
                rpc.Start();
            }
        }

        protected internal override void OnStop()
        {
            if (rpc != null) rpc.Dispose();
            LocalNode.Dispose();
            Blockchain.Default.Dispose();
        }
    }
}
