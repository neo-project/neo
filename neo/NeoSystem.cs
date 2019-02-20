using Akka;
using Akka.Actor;
using Neo.Consensus;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.RPC;
using Neo.Persistence;
using Neo.Plugins;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace Neo
{
    public class NeoSystem : IDisposable
    {
        private Peer.Start start_message = null;
        private bool suspend = false;

        public ActorSystem ActorSystem { get; } = ActorSystem.Create(nameof(NeoSystem),
            $"akka {{ log-dead-letters = off }}" +
            $"akka.coordinated-shutdown.phases {{\n" +
            $"  wait-for-network-shutdown {{\n" +
            $"    Timeout = 60 s\n" +
            $"    depends-on = [{CoordinatedShutdown.PhaseBeforeActorSystemTerminate}]\n" +
            $"  }}\n" +
            $"  wait-for-neo-shutdown {{\n" +
            $"    Timeout = 60 s\n" +
            $"    depends-on = [wait-for-network-shutdown]\n" +
            $"  }}\n" +
            $"  actor-system-terminate.depends-on = [wait-for-neo-shutdown]\n" +
            $"}}" +
            $"blockchain-mailbox {{ mailbox-type: \"{typeof(BlockchainMailbox).AssemblyQualifiedName}\" }}" +
            $"task-manager-mailbox {{ mailbox-type: \"{typeof(TaskManagerMailbox).AssemblyQualifiedName}\" }}" +
            $"remote-node-mailbox {{ mailbox-type: \"{typeof(RemoteNodeMailbox).AssemblyQualifiedName}\" }}" +
            $"protocol-handler-mailbox {{ mailbox-type: \"{typeof(ProtocolHandlerMailbox).AssemblyQualifiedName}\" }}" +
            $"consensus-service-mailbox {{ mailbox-type: \"{typeof(ConsensusServiceMailbox).AssemblyQualifiedName}\" }}");
        public IActorRef WatchActor { get; }
        public IActorRef Blockchain { get; }
        public IActorRef LocalNode { get; }
        internal IActorRef TaskManager { get; }
        public IActorRef Consensus { get; private set; }
        public RpcServer RpcServer { get; private set; }

        private class ShutdownReason : CoordinatedShutdown.Reason
        {
            public static CoordinatedShutdown.Reason Instance = new ShutdownReason();

            private ShutdownReason()
            {
            }
        }

        public class ShutdownWatcher : UntypedActor
        {
            public class Watch { public IActorRef ActorRef; };
            public Dictionary<IActorRef, SemaphoreSlim> ActorSemaphores;
            private object lockObj = new object();
            private static ShutdownWatcher singleton;
            public static ShutdownWatcher Singleton
            {
                get
                {
                    while (singleton == null) Thread.Sleep(10);
                    return singleton;
                }
            }

            public ShutdownWatcher() { lock (lockObj) singleton = this; }

            protected override void OnReceive(object message)
            {
                if (message is Terminated t && ActorSemaphores.TryGetValue(t.ActorRef, out SemaphoreSlim semWait))
                    semWait.Release(short.MaxValue);
                else if (message is Watch w)
                {
                    Context.Watch(w.ActorRef);
                    ActorSemaphores.Add(w.ActorRef, new SemaphoreSlim(0));
                }
            }

            public static Props Props() { return Akka.Actor.Props.Create(() => new ShutdownWatcher()); }
        }

        public NeoSystem(Store store)
        {
            this.WatchActor = ActorSystem.ActorOf(ShutdownWatcher.Props());
            this.Blockchain = ActorSystem.ActorOf(Ledger.Blockchain.Props(this, store));
            WatchActor.Tell(new ShutdownWatcher.Watch { ActorRef = this.Blockchain });
            this.LocalNode = ActorSystem.ActorOf(Network.P2P.LocalNode.Props(this));
            WatchActor.Tell(new ShutdownWatcher.Watch { ActorRef = this.LocalNode });
            this.TaskManager = ActorSystem.ActorOf(Network.P2P.TaskManager.Props(this));
            WatchActor.Tell(new ShutdownWatcher.Watch { ActorRef = this.TaskManager });
            Plugin.LoadPlugins(this);

            // NOTE: The user can add additional tasks to these shutdown phases to further delay shutdown.
            var coordinatedShutdown = CoordinatedShutdown.Get(ActorSystem);
            coordinatedShutdown.AddTask("wait-for-network-shutdown", "wait-for-localnode-stopped",
                async () =>
                {
                    if (!(Consensus is null))
                    {
                        ActorSystem.Stop(Consensus);
                        await ShutdownWatcher.Singleton.ActorSemaphores[Consensus].WaitAsync();
                    }
                    ActorSystem.Stop(TaskManager);
                    await ShutdownWatcher.Singleton.ActorSemaphores[TaskManager].WaitAsync();
                    ActorSystem.Stop(LocalNode);
                    await ShutdownWatcher.Singleton.ActorSemaphores[LocalNode].WaitAsync();
                    return Done.Instance;
                });
            coordinatedShutdown.AddTask("wait-for-neo-shutdown", "wait-for-blockchain-stopped",
                async () =>
                {
                    ActorSystem.Stop(Blockchain);
                    await ShutdownWatcher.Singleton.ActorSemaphores[Blockchain].WaitAsync();
                    return Done.Instance;
                });
        }

        public void Dispose()
        {
            RpcServer?.Dispose();
            CoordinatedShutdown.Get(ActorSystem).Run(ShutdownReason.Instance).Wait();
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
            WatchActor.Tell(new ShutdownWatcher.Watch { ActorRef = Consensus });
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
