using Akka.Actor;
using Neo.Consensus;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.RPC;
using Neo.Persistence;
using Neo.Plugins;
using Neo.Wallets;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Akka;

namespace Neo
{
    public class NeoSystem : IDisposable
    {
        private Peer.Start start_message = null;
        private bool suspend = false;

        public ActorSystem ActorSystem { get; } = ActorSystem.Create(nameof(NeoSystem),
            $"akka {{ log-dead-letters = off }}" +
            $"akka.coordinated-shutdown.phases {{\n" +
            $"  wait-for-neo-shutdown {{\n" +
            $"    Timeout = 120 s\n" +
            $"    depends-on = [before-actor-system-terminate]\n" +
            $"  }}\n" +
            $"  actor-system-terminate.depends-on = [wait-for-neo-shutdown]\n" +
            $"}}" +
            $"blockchain-mailbox {{ mailbox-type: \"{typeof(BlockchainMailbox).AssemblyQualifiedName}\" }}" +
            $"task-manager-mailbox {{ mailbox-type: \"{typeof(TaskManagerMailbox).AssemblyQualifiedName}\" }}" +
            $"remote-node-mailbox {{ mailbox-type: \"{typeof(RemoteNodeMailbox).AssemblyQualifiedName}\" }}" +
            $"protocol-handler-mailbox {{ mailbox-type: \"{typeof(ProtocolHandlerMailbox).AssemblyQualifiedName}\" }}" +
            $"consensus-service-mailbox {{ mailbox-type: \"{typeof(ConsensusServiceMailbox).AssemblyQualifiedName}\" }}");
        public IActorRef Blockchain { get; }
        public IActorRef LocalNode { get; }
        internal IActorRef TaskManager { get; }
        public IActorRef Consensus { get; private set; }
        public RpcServer RpcServer { get; private set; }

        public class NeoSystemShutdownReason : CoordinatedShutdown.Reason
        {
            public static CoordinatedShutdown.Reason Instance = new NeoSystemShutdownReason();

            private NeoSystemShutdownReason()
            {
            }
        }

        public NeoSystem(Store store)
        {
            this.Blockchain = ActorSystem.ActorOf(Ledger.Blockchain.Props(this, store));
            this.LocalNode = ActorSystem.ActorOf(Network.P2P.LocalNode.Props(this));
            this.TaskManager = ActorSystem.ActorOf(Network.P2P.TaskManager.Props(this));
            Plugin.LoadPlugins(this);
            // NOTE: The user can add additional tasks to further delay shutdown.
            CoordinatedShutdown.Get(ActorSystem).AddTask("wait-for-neo-shutdown", "wait-for-blockchain-idle",
                Neo.Ledger.Blockchain.Singleton.ShutdownAndWaitForIdle);

        }

        public void Dispose()
        {
            RpcServer?.Dispose();
            ActorSystem.Stop(LocalNode);
            CoordinatedShutdown.Get(ActorSystem).Run(NeoSystemShutdownReason.Instance).Wait();
            ActorSystem.Dispose();
        }

        internal void ResumeNodeStartup()
        {
            suspend = false;
            if (start_message != null)
            {
                LocalNode.Tell(start_message);
                start_message = null;
            }
        }

        public void StartConsensus(Wallet wallet)
        {
            Consensus = ActorSystem.ActorOf(ConsensusService.Props(this.LocalNode, this.TaskManager, wallet));
            Consensus.Tell(new ConsensusService.Start());
        }

        public void StartNode(int port = 0, int wsPort = 0, int minDesiredConnections = Peer.DefaultMinDesiredConnections,
            int maxConnections = Peer.DefaultMaxConnections)
        {
            start_message = new Peer.Start
            {
                Port = port,
                WsPort = wsPort,
                MinDesiredConnections = minDesiredConnections,
                MaxConnections = maxConnections
            };
            if (!suspend)
            {
                LocalNode.Tell(start_message);
                start_message = null;
            }
        }

        public void StartRpc(IPAddress bindAddress, int port, Wallet wallet = null, string sslCert = null, string password = null,
            string[] trustedAuthorities = null, Fixed8 maxGasInvoke = default(Fixed8))
        {
            RpcServer = new RpcServer(this, wallet, maxGasInvoke);
            RpcServer.Start(bindAddress, port, sslCert, password, trustedAuthorities);
        }

        internal void SuspendNodeStartup()
        {
            suspend = true;
        }
    }
}
