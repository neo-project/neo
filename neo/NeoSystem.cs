using Akka.Actor;
using Neo.Consensus;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.RPC.Server;
using Neo.Persistence;
using Neo.Plugins;
using Neo.Wallets;
using System;
using System.Net;

namespace Neo
{
    public class NeoSystem : IDisposable
    {
        public ActorSystem ActorSystem { get; } = ActorSystem.Create(nameof(NeoSystem),
            $"akka {{ log-dead-letters = off }}" +
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
        public long MaxGasInvoke { get; private set; }

        private readonly Store store;
        private ChannelsConfig start_message = null;
        private bool suspend = false;

        public NeoSystem(Store store)
        {
            this.store = store;
            Plugin.LoadPlugins(this);
            this.Blockchain = ActorSystem.ActorOf(Ledger.Blockchain.Props(this, store));
            this.LocalNode = ActorSystem.ActorOf(Network.P2P.LocalNode.Props(this));
            this.TaskManager = ActorSystem.ActorOf(Network.P2P.TaskManager.Props(this));
            Plugin.NotifyPluginsLoadedAfterSystemConstructed();
        }

        public void Dispose()
        {
            RpcServer?.Dispose();
            EnsureStoped(LocalNode);
            // Dispose will call ActorSystem.Terminate()
            ActorSystem.Dispose();
            ActorSystem.WhenTerminated.Wait();
        }

        public void EnsureStoped(IActorRef actor)
        {
            Inbox inbox = Inbox.Create(ActorSystem);
            inbox.Watch(actor);
            ActorSystem.Stop(actor);
            inbox.Receive(TimeSpan.FromMinutes(5));
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

        public void StartConsensus(Wallet wallet, Store consensus_store = null, bool ignoreRecoveryLogs = false)
        {
            Consensus = ActorSystem.ActorOf(ConsensusService.Props(this.LocalNode, this.TaskManager, consensus_store ?? store, wallet));
            Consensus.Tell(new ConsensusService.Start { IgnoreRecoveryLogs = ignoreRecoveryLogs }, Blockchain);
        }

        public void StartNode(ChannelsConfig config)
        {
            start_message = config;

            if (!suspend)
            {
                LocalNode.Tell(start_message);
                start_message = null;
            }
        }

        public void StartRpc(IPAddress bindAddress, int port, Wallet wallet = null, string sslCert = null, string password = null,
            string[] trustedAuthorities = null, long maxGasInvoke = default)
        {
            MaxGasInvoke = maxGasInvoke;
            // wallet is not being used
            RpcServer = new RpcServer(new RpcConfig
            {
                ListenEndPoint = new IPEndPoint(bindAddress, port),
                Ssl = new RpcConfig.SslCert
                {
                    Path = sslCert,
                    Password = password
                },
                TrustedAuthorities = trustedAuthorities
            });

            RpcServer.BindController<NeoDefaultRpcController>();

            // every param of NeoSystem type will get `this`
            RpcServer.InjectSpecialParameter(context => this);

            RpcServer.Start();
        }

        internal void SuspendNodeStartup()
        {
            suspend = true;
        }
    }
}
