using AntShares.Core;
using AntShares.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace AntShares.Network
{
    public class LocalNode : IDisposable
    {
        public const UInt32 PROTOCOL_VERSION = 0;
        private const int CONNECTED_MAX = 100;
        private const int PENDING_MAX = CONNECTED_MAX;
        private const int UNCONNECTED_MAX = 5000;
        public const int DEFAULT_PORT = 10333;

        private static readonly string path_state = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Data", "node.dat");
        private static readonly string[] SeedList =
        {
            "seed1.antshares.org",
            "seed2.antshares.org",
            "seed3.antshares.org",
            "seed4.antshares.org",
            "seed5.antshares.org"
        };

        private HashSet<IPEndPoint> unconnectedPeers = new HashSet<IPEndPoint>();
        private static HashSet<IPEndPoint> badPeers = new HashSet<IPEndPoint>();
        internal HashSet<RemoteNode> pendingPeers = new HashSet<RemoteNode>();
        internal Dictionary<IPEndPoint, RemoteNode> connectedPeers = new Dictionary<IPEndPoint, RemoteNode>();

        internal readonly IPEndPoint LocalEndpoint;
        private TcpListener listener = new TcpListener(IPAddress.Any, DEFAULT_PORT);
        private Worker connectWorker;
        private int started = 0;
        private int started_sync = 0;
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
                remoteNode.NewPeers += RemoteNode_NewPeers;
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
                List<Task> tasks = new List<Task>();
                if (unconnectedCount > 0)
                {
                    IPEndPoint[] remoteEndpoints;
                    lock (unconnectedPeers)
                    {
                        remoteEndpoints = unconnectedPeers.Take(maxConnections - (connectedCount + pendingCount)).ToArray();
                    }
                    foreach (IPEndPoint remoteEndpoint in remoteEndpoints)
                    {
                        if (cancel.IsCancellationRequested)
                            break;
                        tasks.Add(ConnectToPeerAsync(remoteEndpoint));
                    }
                }
                else if (connectedCount > 0)
                {
                    RemoteNode[] remoteNodes;
                    lock (connectedPeers)
                    {
                        remoteNodes = connectedPeers.Values.ToArray();
                    }
                    foreach (RemoteNode remoteNode in remoteNodes)
                    {
                        if (cancel.IsCancellationRequested)
                            return;
                        tasks.Add(remoteNode.RequestPeersAsync());
                    }
                }
                else
                {
                    foreach (string hostNameOrAddress in SeedList)
                    {
                        tasks.Add(ConnectToPeerAsync(hostNameOrAddress));
                    }
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
                    IPEndPoint[] peers;
                    lock (unconnectedPeers)
                    {
                        lock (connectedPeers)
                        {
                            peers = unconnectedPeers.Union(connectedPeers.Keys).Take(UNCONNECTED_MAX).ToArray();
                        }
                    }
                    using (FileStream fs = new FileStream(path_state, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (BinaryWriter writer = new BinaryWriter(fs))
                    {
                        writer.Write(peers.Length);
                        foreach (IPEndPoint endpoint in peers)
                        {
                            writer.Write(endpoint.Address.GetAddressBytes().Take(4).ToArray());
                            writer.Write((UInt16)endpoint.Port);
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

        private void RemoteNode_Disconnected(object sender, bool error)
        {
            RemoteNode remoteNode = (RemoteNode)sender;
            remoteNode.Disconnected -= RemoteNode_Disconnected;
            remoteNode.NewPeers -= RemoteNode_NewPeers;
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

        public async void Start()
        {
            if (Interlocked.Exchange(ref started, 1) == 0)
            {
                if (File.Exists(path_state))
                {
                    using (FileStream fs = new FileStream(path_state, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (BinaryReader reader = new BinaryReader(fs))
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

        public async void StartSynchronize(Blockchain blockchain)
        {
            Start();
            if (Interlocked.Exchange(ref started_sync, 1) == 0)
            {
                //TODO: 开始同步区块链数据
            }
        }
    }
}
