using Akka.Actor;
using Neo.Consensus;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.RPC;
using Neo.Persistence;
using Neo.Wallets;
using System;

namespace Neo
{
    public class NeoSystem : IDisposable
    {
        private readonly ActorSystem actorSystem = ActorSystem.Create(nameof(NeoSystem),
            $"akka {{ log-dead-letters = off }}" +
            $"blockchain-mailbox {{ mailbox-type: \"{typeof(BlockchainMailbox).AssemblyQualifiedName}\" }}" +
            $"task-manager-mailbox {{ mailbox-type: \"{typeof(TaskManagerMailbox).AssemblyQualifiedName}\" }}" +
            $"remote-node-mailbox {{ mailbox-type: \"{typeof(RemoteNodeMailbox).AssemblyQualifiedName}\" }}" +
            $"protocol-handler-mailbox {{ mailbox-type: \"{typeof(ProtocolHandlerMailbox).AssemblyQualifiedName}\" }}" +
            $"consensus-service-mailbox {{ mailbox-type: \"{typeof(ConsensusServiceMailbox).AssemblyQualifiedName}\" }}");
        public readonly IActorRef Blockchain;
        public readonly IActorRef LocalNode;
        internal readonly IActorRef TaskManager;
        private IActorRef consensus;
        private RpcServer rpcServer;

        public NeoSystem(Store store)
        {
            this.Blockchain = actorSystem.ActorOf(Ledger.Blockchain.Props(this, store));
            this.LocalNode = actorSystem.ActorOf(Network.P2P.LocalNode.Props(this));
            this.TaskManager = actorSystem.ActorOf(Network.P2P.TaskManager.Props(this));
        }

        public void Dispose()
        {
            rpcServer?.Dispose();
            actorSystem.Dispose();
        }

        public void StartConsensus(Wallet wallet)
        {
            consensus = actorSystem.ActorOf(ConsensusService.Props(this, wallet));
            consensus.Tell(new ConsensusService.Start());
        }

        public void StartNode(int port)
        {
            LocalNode.Tell(new Peer.Start { Port = port });
        }

        public void StartRpc(int port)
        {
            rpcServer = new RpcServer(this);
            rpcServer.Start(port);
        }
    }
}
