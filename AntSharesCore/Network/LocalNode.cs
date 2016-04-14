using AntShares.Core;
using AntShares.IO;
using AntShares.IO.Caching;
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
        public static event EventHandler<Inventory> NewInventory;

        public const uint PROTOCOL_VERSION = 0;
        private const int CONNECTED_MAX = 10;
        private const int PENDING_MAX = CONNECTED_MAX;
        private const int UNCONNECTED_MAX = 1000;
#if TESTNET
        public const int DEFAULT_PORT = 20333;
#else
        public const int DEFAULT_PORT = 10333;
#endif

        //TODO: 需要搭建一批种子节点
        private static readonly string[] SeedList =
        {
            "seed1.antshares.org",
            "seed2.antshares.org",
            "seed3.antshares.org",
            "seed4.antshares.org",
            "seed5.antshares.org"
        };

        private static readonly Dictionary<UInt256, Transaction> MemoryPool = new Dictionary<UInt256, Transaction>();
        internal static readonly HashSet<UInt256> KnownHashes = new HashSet<UInt256>();
        internal readonly RelayCache RelayCache = new RelayCache(100);

        private static readonly HashSet<IPEndPoint> unconnectedPeers = new HashSet<IPEndPoint>();
        private static readonly HashSet<IPEndPoint> badPeers = new HashSet<IPEndPoint>();
        internal readonly Dictionary<IPEndPoint, RemoteNode> pendingPeers = new Dictionary<IPEndPoint, RemoteNode>();
        internal readonly List<RemoteNode> connectedPeers = new List<RemoteNode>();

        internal static readonly HashSet<IPAddress> LocalAddresses = new HashSet<IPAddress>();
        internal ushort Port;
        internal readonly uint Nonce;
        private TcpListener listener;
        private Thread connectThread;
        private Thread listenerThread;
        private int started = 0;
        private int disposed = 0;

        public bool GlobalMissionsEnabled { get; set; } = true;
        public int RemoteNodeCount => connectedPeers.Count;
        public bool ServiceEnabled { get; set; } = true;
        public bool UpnpEnabled { get; set; } = false;
        public string UserAgent { get; set; }

        static LocalNode()
        {
            try
            {
                LocalAddresses.UnionWith(Dns.GetHostEntry(Dns.GetHostName()).AddressList.Where(p => !IPAddress.IsLoopback(p)).Select(p => p.MapToIPv6()));
            }
            catch (SocketException) { }
            Blockchain.PersistCompleted += Blockchain_PersistCompleted;
        }

        public LocalNode()
        {
            Random rand = new Random();
            this.Nonce = (uint)rand.Next();
            this.connectThread = new Thread(ConnectToPeersLoop)
            {
                IsBackground = true,
                Name = "LocalNode.ConnectToPeersLoop"
            };
            this.listenerThread = new Thread(AcceptPeersLoop)
            {
                IsBackground = true,
                Name = "LocalNode.AcceptPeersLoop"
            };
            this.UserAgent = string.Format("/AntSharesCore:{0}/", Assembly.GetExecutingAssembly().GetName().Version.ToString(3));
        }

        private void AcceptPeersLoop()
        {
            while (disposed == 0)
            {
                TcpClient tcp;
                try
                {
                    tcp = listener.AcceptTcpClient();
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (SocketException)
                {
                    break;
                }
                RemoteNode remoteNode = new RemoteNode(this, tcp);
                remoteNode.Disconnected += RemoteNode_Disconnected;
                remoteNode.InventoryReceived += RemoteNode_InventoryReceived;
                remoteNode.PeersReceived += RemoteNode_PeersReceived;
                remoteNode.StartProtocol();
            }
        }

        private static bool AddTransaction(Transaction tx)
        {
            if (Blockchain.Default == null) return false;
            lock (MemoryPool)
            {
                if (MemoryPool.ContainsKey(tx.Hash)) return false;
                if (MemoryPool.Values.SelectMany(p => p.GetAllInputs()).Intersect(tx.GetAllInputs()).Count() > 0)
                    return false;
                if (Blockchain.Default.ContainsTransaction(tx.Hash)) return false;
                if (!tx.Verify()) return false;
                MemoryPool.Add(tx.Hash, tx);
                return true;
            }
        }

        private static void Blockchain_PersistCompleted(object sender, Block block)
        {
            lock (MemoryPool)
            {
                foreach (Transaction tx in block.Transactions)
                {
                    MemoryPool.Remove(tx.Hash);
                }
            }
        }

        public async Task ConnectToPeerAsync(string hostNameOrAddress)
        {
            IPHostEntry entry;
            try
            {
                entry = await Dns.GetHostEntryAsync(hostNameOrAddress);
            }
            catch (SocketException)
            {
                return;
            }
            IPAddress ipAddress = entry.AddressList.FirstOrDefault(p => p.AddressFamily == AddressFamily.InterNetwork || p.IsIPv6Teredo)?.MapToIPv6();
            if (ipAddress == null) return;
            await ConnectToPeerAsync(new IPEndPoint(ipAddress, DEFAULT_PORT));
        }

        public async Task ConnectToPeerAsync(IPEndPoint remoteEndpoint)
        {
            if (remoteEndpoint.Port == Port && LocalAddresses.Contains(remoteEndpoint.Address)) return;
            RemoteNode remoteNode;
            lock (unconnectedPeers)
            {
                unconnectedPeers.Remove(remoteEndpoint);
            }
            lock (pendingPeers)
            {
                lock (connectedPeers)
                {
                    if (pendingPeers.ContainsKey(remoteEndpoint) || connectedPeers.Any(p => remoteEndpoint.Equals(p.ListenerEndpoint)))
                        return;
                }
                remoteNode = new RemoteNode(this, remoteEndpoint);
                pendingPeers.Add(remoteEndpoint, remoteNode);
                remoteNode.Disconnected += RemoteNode_Disconnected;
                remoteNode.InventoryReceived += RemoteNode_InventoryReceived;
                remoteNode.PeersReceived += RemoteNode_PeersReceived;
            }
            await remoteNode.ConnectAsync();
        }

        private void ConnectToPeersLoop()
        {
            while (disposed == 0)
            {
                int connectedCount = connectedPeers.Count;
                int pendingCount = pendingPeers.Count;
                int unconnectedCount = unconnectedPeers.Count;
                int maxConnections = Math.Max(CONNECTED_MAX + CONNECTED_MAX / 5, PENDING_MAX);
                if (connectedCount < CONNECTED_MAX && pendingCount < PENDING_MAX && (connectedCount + pendingCount) < maxConnections)
                {
                    Task[] tasks;
                    if (unconnectedCount > 0)
                    {
                        IPEndPoint[] endpoints;
                        lock (unconnectedPeers)
                        {
                            endpoints = unconnectedPeers.Take(maxConnections - (connectedCount + pendingCount)).ToArray();
                        }
                        tasks = endpoints.Select(p => ConnectToPeerAsync(p)).ToArray();
                    }
                    else if (connectedCount > 0)
                    {
                        lock (connectedPeers)
                        {
                            tasks = connectedPeers.ToArray().Select(p => Task.Run(new Action(p.RequestPeers))).ToArray();
                        }
                    }
                    else
                    {
                        tasks = SeedList.Select(p => ConnectToPeerAsync(p)).ToArray();
                    }
                    Task.WaitAll(tasks);
                }
                for (int i = 0; i < 50 && disposed == 0; i++)
                {
                    Thread.Sleep(100);
                }
            }
        }

        public static bool ContainsTransaction(UInt256 hash)
        {
            lock (MemoryPool)
            {
                return MemoryPool.ContainsKey(hash);
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
            {
                if (started > 0)
                {
                    if (listener != null) listener.Stop();
                    if (!connectThread.ThreadState.HasFlag(ThreadState.Unstarted)) connectThread.Join();
                    if (!listenerThread.ThreadState.HasFlag(ThreadState.Unstarted)) listenerThread.Join();
                    lock (unconnectedPeers)
                    {
                        if (unconnectedPeers.Count < UNCONNECTED_MAX)
                        {
                            lock (connectedPeers)
                            {
                                unconnectedPeers.UnionWith(connectedPeers.Select(p => p.ListenerEndpoint).Where(p => p != null).Take(UNCONNECTED_MAX - unconnectedPeers.Count));
                            }
                        }
                    }
                    RemoteNode[] nodes;
                    lock (connectedPeers)
                    {
                        nodes = connectedPeers.ToArray();
                    }
                    Task.WaitAll(nodes.Select(p => Task.Run(() => p.Disconnect(false))).ToArray());
                }
            }
        }

        public static IEnumerable<Transaction> GetMemoryPool()
        {
            lock (MemoryPool)
            {
                foreach (Transaction tx in MemoryPool.Values)
                    yield return tx;
            }
        }

        public RemoteNode[] GetRemoteNodes()
        {
            lock (connectedPeers)
            {
                return connectedPeers.ToArray();
            }
        }

        private static bool IsIntranetAddress(IPAddress address)
        {
            byte[] data = address.GetAddressBytes();
            Array.Reverse(data);
            uint value = BitConverter.ToUInt32(data, 0);
            return (value & 0xff000000) == 0x0a000000 || (value & 0xfff00000) == 0xac100000 || (value & 0xffff0000) == 0xc0a80000;
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
                    unconnectedPeers.Add(new IPEndPoint(address.MapToIPv6(), port));
                }
            }
        }

        public bool Relay(Inventory inventory)
        {
            if (connectedPeers.Count == 0) return false;
            lock (KnownHashes)
            {
                if (!KnownHashes.Add(inventory.Hash)) return false;
            }
            if (inventory is Block)
            {
                if (Blockchain.Default == null) return false;
                Block block = (Block)inventory;
                if (Blockchain.Default.ContainsBlock(block.Hash)) return false;
                if (!Blockchain.Default.AddBlock(block)) return false;
            }
            else if (inventory is Transaction)
            {
                if (!AddTransaction((Transaction)inventory)) return false;
            }
            else //if (inventory is Consensus)
            {
                if (!inventory.Verify()) return false;
            }
            lock (connectedPeers)
            {
                if (connectedPeers.Count == 0) return false;
                RelayCache.Add(inventory);
                foreach (RemoteNode node in connectedPeers)
                    node.Relay(inventory);
            }
            if (NewInventory != null) NewInventory(this, inventory);
            return true;
        }

        private void RemoteNode_Disconnected(object sender, bool error)
        {
            RemoteNode remoteNode = (RemoteNode)sender;
            remoteNode.Disconnected -= RemoteNode_Disconnected;
            remoteNode.InventoryReceived -= RemoteNode_InventoryReceived;
            remoteNode.PeersReceived -= RemoteNode_PeersReceived;
            if (error && remoteNode.ListenerEndpoint != null)
            {
                lock (badPeers)
                {
                    badPeers.Add(remoteNode.ListenerEndpoint);
                }
            }
            lock (unconnectedPeers)
            {
                lock (pendingPeers)
                {
                    lock (connectedPeers)
                    {
                        if (remoteNode.ListenerEndpoint != null)
                        {
                            unconnectedPeers.Remove(remoteNode.ListenerEndpoint);
                            pendingPeers.Remove(remoteNode.ListenerEndpoint);
                        }
                        connectedPeers.Remove(remoteNode);
                    }
                }
            }
        }

        private void RemoteNode_InventoryReceived(object sender, Inventory inventory)
        {
            Relay(inventory);
        }

        private void RemoteNode_PeersReceived(object sender, IPEndPoint[] peers)
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
                                unconnectedPeers.ExceptWith(pendingPeers.Keys);
                                unconnectedPeers.ExceptWith(connectedPeers.Select(p => p.ListenerEndpoint));
                            }
                        }
                    }
                }
            }
        }

        public static void SaveState(Stream stream)
        {
            IPEndPoint[] peers;
            lock (unconnectedPeers)
            {
                peers = unconnectedPeers.Take(UNCONNECTED_MAX).ToArray();
            }
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII, true))
            {
                writer.Write(peers.Length);
                foreach (IPEndPoint endpoint in peers)
                {
                    writer.Write(endpoint.Address.MapToIPv4().GetAddressBytes());
                    writer.Write((ushort)endpoint.Port);
                }
            }
        }

        public void Start(int port = DEFAULT_PORT)
        {
            if (Interlocked.Exchange(ref started, 1) == 0)
            {
                IPAddress address = LocalAddresses.FirstOrDefault(p => p.AddressFamily == AddressFamily.InterNetwork && !IsIntranetAddress(p));
                if (address == null && UpnpEnabled && UPnP.Discover())
                {
                    try
                    {
                        address = UPnP.GetExternalIP();
                        UPnP.ForwardPort(port, ProtocolType.Tcp, "AntShares");
                        LocalAddresses.Add(address);
                    }
                    catch { }
                }
                listener = new TcpListener(IPAddress.Any, port);
                try
                {
                    listener.Start();
                    Port = (ushort)port;
                }
                catch (SocketException) { }
                connectThread.Start();
                if (Port > 0) listenerThread.Start();
            }
        }
    }
}
