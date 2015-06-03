using AntShares.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AntShares.Network
{
    public class LocalNode : IDisposable
    {
        public const byte PROTOCOL_VERSION = 0x00;
        private const int CONNECTED_MAX = 100;
        private const int PENDING_MAX = CONNECTED_MAX;
        private const int UNCONNECTED_MAX = 5000;
        public const int DEFAULT_PORT = 10333;

        private static readonly string path_state = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Data", "node.dat");
        internal static readonly IPEndPoint localEndpoint = new IPEndPoint(Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(p => p.AddressFamily == AddressFamily.InterNetwork), DEFAULT_PORT);

        private static readonly string[] SeedList =
        {
            "seed1.antshares.org",
            "seed2.antshares.org",
            "seed3.antshares.org",
            "seed4.antshares.org",
            "seed5.antshares.org"
        };

        private ConcurrentSet<IPEndPoint> unconnectedPeers = new ConcurrentSet<IPEndPoint>();
        private ConcurrentDictionary<IPEndPoint, RemoteNode> pendingPeers = new ConcurrentDictionary<IPEndPoint, RemoteNode>();
        private ConcurrentDictionary<IPEndPoint, RemoteNode> connectedPeers = new ConcurrentDictionary<IPEndPoint, RemoteNode>();
        private static ConcurrentSet<IPEndPoint> badPeers = new ConcurrentSet<IPEndPoint>();

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

        public RemoteNode[] RemoteNodes
        {
            get
            {
                return connectedPeers.Values.ToArray();
            }
        }

        public LocalNode()
        {
            this.connectWorker = new Worker(string.Format("ConnectToPeersLoop@{0}", localEndpoint), ConnectToPeersLoop, true, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }

        public async Task ConnectToPeerAsync(IPEndPoint remoteEndpoint)
        {
            unconnectedPeers.Remove(remoteEndpoint);
            if (pendingPeers.ContainsKey(remoteEndpoint) || connectedPeers.ContainsKey(remoteEndpoint))
                return;
            RemoteNode remoteNode = new RemoteNode(this, remoteEndpoint);
            if (!pendingPeers.TryAdd(remoteEndpoint, remoteNode))
                return;
            remoteNode.Disconnected += RemoteNode_Disconnected;
            remoteNode.NewPeers += RemoteNode_NewPeers;
            if (await remoteNode.ConnectAsync())
            {
                connectedPeers.TryAdd(remoteEndpoint, remoteNode);
            }
            pendingPeers.TryRemove(remoteEndpoint, out remoteNode);
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
                    int connectCount = Math.Min(unconnectedCount, maxConnections - (connectedCount + pendingCount));
                    IPEndPoint[] remoteEndpoints = unconnectedPeers.Take(connectCount).ToArray();
                    foreach (IPEndPoint remoteEndpoint in remoteEndpoints)
                    {
                        if (cancel.IsCancellationRequested)
                            break;
                        tasks.Add(ConnectToPeerAsync(remoteEndpoint));
                    }
                }
                else if (connectedCount > 0)
                {
                    foreach (RemoteNode remoteNode in connectedPeers.Values)
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
                        IPAddress ipAddress = Dns.GetHostEntry(hostNameOrAddress).AddressList.FirstOrDefault();
                        if (ipAddress != null)
                        {
                            tasks.Add(ConnectToPeerAsync(new IPEndPoint(ipAddress, DEFAULT_PORT)));
                        }
                    }
                }
                Task.WaitAll(tasks.ToArray());
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
                    IPEndPoint[] peers = unconnectedPeers.Union(connectedPeers.Keys).Take(UNCONNECTED_MAX).ToArray();
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
                    foreach (RemoteNode remoteNode in pendingPeers.Values.ToArray())
                    {
                        try
                        {
                            remoteNode.Disconnect(false);
                        }
                        catch { }
                    }
                    foreach (RemoteNode remoteNode in connectedPeers.Values.ToArray())
                    {
                        try
                        {
                            remoteNode.Disconnect(false);
                        }
                        catch { }
                    }
                }
            }
        }

        private void RemoteNode_Disconnected(object sender, bool error)
        {
            RemoteNode remoteNode = (RemoteNode)sender;
            remoteNode.Disconnected -= RemoteNode_Disconnected;
            remoteNode.NewPeers -= RemoteNode_NewPeers;
            if (error)
                badPeers.Add(remoteNode.RemoteEndpoint);
            RemoteNode ignore;
            unconnectedPeers.Remove(remoteNode.RemoteEndpoint);
            pendingPeers.TryRemove(remoteNode.RemoteEndpoint, out ignore);
            connectedPeers.TryRemove(remoteNode.RemoteEndpoint, out ignore);
        }

        private void RemoteNode_NewPeers(object sender, IPEndPoint[] peers)
        {
            if (unconnectedPeers.Count < UNCONNECTED_MAX)
            {
                unconnectedPeers.UnionWith(peers.Except(badPeers).Except(connectedPeers.Keys).Except(pendingPeers.Keys));
            }
        }

        public void Start()
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
            }
        }
    }
}
