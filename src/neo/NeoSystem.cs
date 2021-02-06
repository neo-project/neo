using Akka.Actor;
using Neo.IO.Caching;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Persistence;
using Neo.Plugins;
using System;

namespace Neo
{
    public class NeoSystem : IDisposable
    {
        public ActorSystem ActorSystem { get; } = ActorSystem.Create(nameof(NeoSystem),
            $"akka {{ log-dead-letters = off , loglevel = warning, loggers = [ \"{typeof(Utility.Logger).AssemblyQualifiedName}\" ] }}" +
            $"blockchain-mailbox {{ mailbox-type: \"{typeof(BlockchainMailbox).AssemblyQualifiedName}\" }}" +
            $"task-manager-mailbox {{ mailbox-type: \"{typeof(TaskManagerMailbox).AssemblyQualifiedName}\" }}" +
            $"remote-node-mailbox {{ mailbox-type: \"{typeof(RemoteNodeMailbox).AssemblyQualifiedName}\" }}");
        public IActorRef Blockchain { get; }
        public IActorRef LocalNode { get; }
        public IActorRef TaskManager { get; }
        /// <summary>
        /// A readonly view of the store.
        /// </summary>
        /// <remarks>
        /// It doesn't need to be disposed because the <see cref="ISnapshot"/> inside it is null.
        /// </remarks>
        public DataCache StoreView => new SnapshotCache(store);
        public MemoryPool MemPool { get; }
        public HeaderCache HeaderCache { get; } = new HeaderCache();
        internal RelayCache RelayCache { get; } = new RelayCache(100);

        private readonly string storage_engine;
        private readonly IStore store;
        private ChannelsConfig start_message = null;
        private bool suspend = false;

        static NeoSystem()
        {
            // Unify unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        public NeoSystem(string storageEngine = null, string storagePath = null)
        {
            Plugin.LoadPlugins(this);
            this.storage_engine = storageEngine;
            this.store = LoadStore(storagePath);
            this.MemPool = new MemoryPool(this);
            this.Blockchain = ActorSystem.ActorOf(Ledger.Blockchain.Props(this));
            this.LocalNode = ActorSystem.ActorOf(Network.P2P.LocalNode.Props(this));
            this.TaskManager = ActorSystem.ActorOf(Network.P2P.TaskManager.Props(this));
            foreach (var plugin in Plugin.Plugins)
                plugin.OnPluginsLoaded();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Utility.Log("UnhandledException", LogLevel.Fatal, e.ExceptionObject);
        }

        public void Dispose()
        {
            foreach (var p in Plugin.Plugins)
                p.Dispose();
            EnsureStoped(LocalNode);
            // Dispose will call ActorSystem.Terminate()
            ActorSystem.Dispose();
            ActorSystem.WhenTerminated.Wait();
            HeaderCache.Dispose();
            store.Dispose();
        }

        public void EnsureStoped(IActorRef actor)
        {
            using Inbox inbox = Inbox.Create(ActorSystem);
            inbox.Watch(actor);
            ActorSystem.Stop(actor);
            inbox.Receive(TimeSpan.FromMinutes(5));
        }

        public IStore LoadStore(string path)
        {
            return string.IsNullOrEmpty(storage_engine) || storage_engine == nameof(MemoryStore)
                ? new MemoryStore()
                : Plugin.Storages[storage_engine].GetStore(path);
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

        public void StartNode(ChannelsConfig config)
        {
            start_message = config;

            if (!suspend)
            {
                LocalNode.Tell(start_message);
                start_message = null;
            }
        }

        internal void SuspendNodeStartup()
        {
            suspend = true;
        }

        public SnapshotCache GetSnapshot()
        {
            return new SnapshotCache(store.GetSnapshot());
        }
    }
}
