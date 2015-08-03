using AntShares.Core;
using AntShares.Network.Payloads;
using AntShares.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AntShares.Network
{
    public class LocalNode : IDisposable
    {
        public static event EventHandler<Block> NewBlock;
        public static event EventHandler<Transaction> NewTransaction;

        public const UInt32 PROTOCOL_VERSION = 0;
        private const int CONNECTED_MAX = 100;
        private const int PENDING_MAX = CONNECTED_MAX;
        private const int UNCONNECTED_MAX = 5000;
        public const int DEFAULT_PORT = 10333;

        //TODO: 需要搭建一批种子节点
        private static readonly string[] SeedList =
        {
            "seed1.antshares.org",
            "seed2.antshares.org",
            "seed3.antshares.org",
            "seed4.antshares.org",
            "seed5.antshares.org"
        };

        internal static Dictionary<UInt256, Transaction> MemoryPool = new Dictionary<UInt256, Transaction>();
        internal static HashSet<UInt256> KnownHashes = new HashSet<UInt256>();

        private static HashSet<IPEndPoint> unconnectedPeers = new HashSet<IPEndPoint>();
        private static HashSet<IPEndPoint> badPeers = new HashSet<IPEndPoint>();
        internal HashSet<RemoteNode> pendingPeers = new HashSet<RemoteNode>();
        internal Dictionary<IPEndPoint, RemoteNode> connectedPeers = new Dictionary<IPEndPoint, RemoteNode>();

        internal readonly IPEndPoint LocalEndpoint;
        private TcpListener listener = new TcpListener(IPAddress.Any, DEFAULT_PORT);
        private Worker connectWorker;
        private int started = 0;
        private int disposed = 0;

        public int RemoteNodeCount
        {
            get
            {
                return connectedPeers.Count;
            }
        }

        public string UserAgent { get; set; }

        public LocalNode(int port = 0)
        {
            if (port == 0)
                port = DEFAULT_PORT;
            this.LocalEndpoint = new IPEndPoint(Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(p => p.AddressFamily == AddressFamily.InterNetwork), port);
            this.connectWorker = new Worker(string.Format("ConnectToPeersLoop@{0}", LocalEndpoint), ConnectToPeersLoop, true, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
            this.UserAgent = string.Format("/AntSharesCore:{0}/", Assembly.GetExecutingAssembly().GetName().Version.ToString(3));
        }

        public async Task ConnectToPeerAsync(string hostNameOrAddress)
        {
            IPHostEntry entry = await Dns.GetHostEntryAsync(hostNameOrAddress);
            IPAddress ipAddress = entry.AddressList.FirstOrDefault();
            await ConnectToPeerAsync(new IPEndPoint(ipAddress, DEFAULT_PORT));
        }

        public async Task ConnectToPeerAsync(IPEndPoint remoteEndpoint)
        {
            RemoteNode remoteNode;
            lock (unconnectedPeers)
            {
                unconnectedPeers.Remove(remoteEndpoint);
            }
            lock (pendingPeers)
            {
                lock (connectedPeers)
                {
                    if (pendingPeers.Any(p => p.RemoteEndpoint == remoteEndpoint) || connectedPeers.ContainsKey(remoteEndpoint))
                        return;
                }
                remoteNode = new RemoteNode(this, remoteEndpoint);
                pendingPeers.Add(remoteNode);
                remoteNode.Disconnected += RemoteNode_Disconnected;
                remoteNode.NewBlock += RemoteNode_NewBlock;
                remoteNode.NewPeers += RemoteNode_NewPeers;
                remoteNode.NewTransaction += RemoteNode_NewTransaction;
            }
            await remoteNode.ConnectAsync();
        }

        private void ConnectToPeersLoop(CancellationToken cancel)
        {
            int connectedCount = connectedPeers.Count;
            int pendingCount = pendingPeers.Count;
            int unconnectedCount = unconnectedPeers.Count;
            int maxConnections = Math.Max(CONNECTED_MAX + 20, PENDING_MAX);
            if (connectedCount < CONNECTED_MAX && pendingCount < PENDING_MAX && (connectedCount + pendingCount) < maxConnections)
            {
                Task[] tasks;
                if (unconnectedCount > 0)
                {
                    lock (unconnectedPeers)
                    {
                        tasks = unconnectedPeers.Take(maxConnections - (connectedCount + pendingCount)).Select(p => ConnectToPeerAsync(p)).ToArray();
                    }
                }
                else if (connectedCount > 0)
                {
                    lock (connectedPeers)
                    {
                        tasks = connectedPeers.Values.Select(p => p.RequestPeersAsync()).ToArray();
                    }
                }
                else
                {
                    tasks = SeedList.Select(p => ConnectToPeerAsync(p)).ToArray();
                }
                try
                {
                    Task.WaitAll(tasks.ToArray());
                }
                catch (AggregateException) { };
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
            {
                if (started > 0)
                {
                    listener.Stop();
                }
                connectWorker.Dispose();
                if (started > 0)
                {
                    lock (unconnectedPeers)
                    {
                        if (unconnectedPeers.Count < UNCONNECTED_MAX)
                        {
                            lock (connectedPeers)
                            {
                                unconnectedPeers.UnionWith(connectedPeers.Keys.Take(UNCONNECTED_MAX - unconnectedPeers.Count));
                            }
                        }
                    }
                    lock (connectedPeers)
                    {
                        foreach (RemoteNode remoteNode in connectedPeers.Values.ToArray())
                        {
                            remoteNode.Disconnect(false);
                        }
                    }
                }
            }
        }

        public RemoteNode[] GetRemoteNodes()
        {
            lock (connectedPeers)
            {
                return connectedPeers.Values.ToArray();
            }
        }

        public static void LoadState(Stream stream)
        {
            unconnectedPeers.Clear();
            using (BinaryReader reader = new BinaryReader(stream, Encoding.ASCII, true))
            {
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    IPAddress address = new IPAddress(reader.ReadBytes(4));
                    int port = reader.ReadUInt16();
                    unconnectedPeers.Add(new IPEndPoint(address, port));
                }
            }
        }

        public async Task<bool> RelayAsync(Block block)
        {
            lock (KnownHashes)
            {
                KnownHashes.Add(block.Hash);
            }
            if (connectedPeers.Count == 0) return false;
            RemoteNode[] remoteNodes;
            lock (connectedPeers)
            {
                remoteNodes = connectedPeers.Values.ToArray();
            }
            if (remoteNodes.Length == 0) return false;
            await Task.WhenAll(remoteNodes.Select(p => p.RelayAsync(InventoryType.MSG_BLOCK, block.Hash)));
            return true;
        }

        public async Task<bool> RelayAsync(Transaction tx)
        {
            lock (KnownHashes)
            {
                KnownHashes.Add(tx.Hash);
            }
            lock (MemoryPool)
            {
                if (!MemoryPool.ContainsKey(tx.Hash))
                {
                    //TODO: 清理内存池
                    //如果交易无效或者已经在区块链中，那么就永远没有机会将它从内存池中移除
                    //需要有一种机制能够定期从池中将旧的交易清理掉
                    MemoryPool.Add(tx.Hash, tx);
                }
            }
            if (connectedPeers.Count == 0) return false;
            RemoteNode[] remoteNodes;
            lock (connectedPeers)
            {
                remoteNodes = connectedPeers.Values.ToArray();
            }
            if (remoteNodes.Length == 0) return false;
            await Task.WhenAll(remoteNodes.Select(p => p.RelayAsync(InventoryType.MSG_TX, tx.Hash)));
            return true;
        }

        private void RemoteNode_Disconnected(object sender, bool error)
        {
            RemoteNode remoteNode = (RemoteNode)sender;
            remoteNode.Disconnected -= RemoteNode_Disconnected;
            remoteNode.NewBlock -= RemoteNode_NewBlock;
            remoteNode.NewPeers -= RemoteNode_NewPeers;
            remoteNode.NewTransaction -= RemoteNode_NewTransaction;
            if (error)
            {
                lock (badPeers)
                {
                    badPeers.Add(remoteNode.RemoteEndpoint);
                }
            }
            lock (unconnectedPeers)
            {
                lock (pendingPeers)
                {
                    lock (connectedPeers)
                    {
                        unconnectedPeers.Remove(remoteNode.RemoteEndpoint);
                        pendingPeers.Remove(remoteNode);
                        connectedPeers.Remove(remoteNode.RemoteEndpoint);
                    }
                }
            }
        }

        private void RemoteNode_NewBlock(object sender, Block block)
        {
            lock (LocalNode.KnownHashes)
            {
                if (!LocalNode.KnownHashes.Add(block.Hash))
                    return;
            }
            if (Blockchain.Default.ContainsBlock(block.Hash))
                return;
            VerificationResult vr = block.Verify();
            if ((vr & ~(VerificationResult.Incapable | VerificationResult.LackOfInformation)) > 0)
                return;
            if (NewBlock != null)
                NewBlock(this, block);
            RelayAsync(block).Void();
        }

        private void RemoteNode_NewPeers(object sender, IPEndPoint[] peers)
        {
            lock (unconnectedPeers)
            {
                if (unconnectedPeers.Count < UNCONNECTED_MAX)
                {
                    lock (badPeers)
                    {
                        lock (pendingPeers)
                        {
                            lock (connectedPeers)
                            {
                                unconnectedPeers.UnionWith(peers);
                                unconnectedPeers.ExceptWith(badPeers);
                                unconnectedPeers.ExceptWith(pendingPeers.Select(p => p.RemoteEndpoint));
                                unconnectedPeers.ExceptWith(connectedPeers.Keys);
                            }
                        }
                    }
                }
            }
        }

        private void RemoteNode_NewTransaction(object sender, Transaction tx)
        {
            lock (KnownHashes)
            {
                if (!KnownHashes.Add(tx.Hash))
                    return;
            }
            if (Blockchain.Default.ContainsTransaction(tx.Hash))
                return;
            VerificationResult vr = tx.Verify();
            if ((vr & ~(VerificationResult.Incapable | VerificationResult.LackOfInformation)) > 0)
                return;
            if (NewTransaction != null)
                NewTransaction(this, tx);
            RelayAsync(tx).Void();
        }

        public static void SaveState(Stream stream)
        {
            IPEndPoint[] peers;
            lock (unconnectedPeers)
            {
                peers = unconnectedPeers.Take(UNCONNECTED_MAX).ToArray();
            }
            if (peers.Length == 0) return;
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII, true))
            {
                writer.Write(peers.Length);
                foreach (IPEndPoint endpoint in peers)
                {
                    writer.Write(endpoint.Address.GetAddressBytes().Take(4).ToArray());
                    writer.Write((ushort)endpoint.Port);
                }
            }
        }

        public async void Start()
        {
            if (Interlocked.Exchange(ref started, 1) == 0)
            {
                connectWorker.Start();
                listener.Start();
                while (disposed == 0)
                {
                    RemoteNode remoteNode = new RemoteNode(this, await listener.AcceptTcpClientAsync());
                    lock (pendingPeers)
                    {
                        pendingPeers.Add(remoteNode);
                    }
                    remoteNode.StartProtocolAsync().Void();
                }
            }
        }
    }
}
