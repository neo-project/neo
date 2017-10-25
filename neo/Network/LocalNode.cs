using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Neo.Core;
using Neo.IO;
using Neo.IO.Caching;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Network
{
    public class LocalNode : IDisposable
    {
        public static event EventHandler<InventoryReceivingEventArgs> InventoryReceiving;
        public static event EventHandler<IInventory> InventoryReceived;

        public const uint ProtocolVersion = 0;
        private const int ConnectedMax = 10;
        private const int UnconnectedMax = 1000;
        public const int MemoryPoolSize = 30000;

        private static readonly Dictionary<UInt256, Transaction> mem_pool = new Dictionary<UInt256, Transaction>();
        private readonly HashSet<Transaction> temp_pool = new HashSet<Transaction>();
        internal static readonly HashSet<UInt256> KnownHashes = new HashSet<UInt256>();
        internal readonly RelayCache RelayCache = new RelayCache(100);

        private static readonly HashSet<IPEndPoint> unconnectedPeers = new HashSet<IPEndPoint>();
        private static readonly HashSet<IPEndPoint> badPeers = new HashSet<IPEndPoint>();
        internal readonly List<RemoteNode> connectedPeers = new List<RemoteNode>();

        internal static readonly HashSet<IPAddress> LocalAddresses = new HashSet<IPAddress>();
        internal ushort Port;
        internal readonly uint Nonce;
        private TcpListener listener;
        private IWebHost ws_host;
        private Thread connectThread;
        private Thread poolThread;
        private readonly AutoResetEvent new_tx_event = new AutoResetEvent(false);
        private int started = 0;
        private int disposed = 0;
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public bool GlobalMissionsEnabled { get; set; } = true;
        public int RemoteNodeCount => connectedPeers.Count;
        public bool ServiceEnabled { get; set; } = true;
        public bool UpnpEnabled { get; set; } = false;
        public string UserAgent { get; set; }

        static LocalNode()
        {
            LocalAddresses.UnionWith(NetworkInterface.GetAllNetworkInterfaces().SelectMany(p => p.GetIPProperties().UnicastAddresses).Select(p => p.Address.MapToIPv6()));
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
            if (Blockchain.Default != null)
            {
                this.poolThread = new Thread(AddTransactionLoop)
                {
                    IsBackground = true,
                    Name = "LocalNode.AddTransactionLoop"
                };
            }
            this.UserAgent = string.Format("/NEO:{0}/", GetType().GetTypeInfo().Assembly.GetName().Version.ToString(3));
            Blockchain.PersistCompleted += Blockchain_PersistCompleted;
        }

        private async void AcceptPeers()
        {
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                Socket socket;
                try
                {
                    socket = await listener.AcceptSocketAsync();
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (SocketException)
                {
                    continue;
                }
                TcpRemoteNode remoteNode = new TcpRemoteNode(this, socket);
                OnConnected(remoteNode);
            }
        }

        private static bool AddTransaction(Transaction tx)
        {
            if (Blockchain.Default == null) return false;
            lock (mem_pool)
            {
                if (mem_pool.ContainsKey(tx.Hash)) return false;
                if (Blockchain.Default.ContainsTransaction(tx.Hash)) return false;
                if (!tx.Verify(mem_pool.Values)) return false;
                mem_pool.Add(tx.Hash, tx);
                CheckMemPool();
            }
            return true;
        }

        private void AddTransactionLoop()
        {
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                new_tx_event.WaitOne();
                Transaction[] transactions;
                lock (temp_pool)
                {
                    if (temp_pool.Count == 0) continue;
                    transactions = temp_pool.ToArray();
                    temp_pool.Clear();
                }
                ConcurrentBag<Transaction> verified = new ConcurrentBag<Transaction>();
                lock (mem_pool)
                {
                    transactions = transactions.Where(p => !mem_pool.ContainsKey(p.Hash) && !Blockchain.Default.ContainsTransaction(p.Hash)).ToArray();
                    if (transactions.Length == 0) continue;

                    Transaction[] tmpool = mem_pool.Values.Concat(transactions).ToArray();

                    transactions.AsParallel().ForAll(tx =>
                    {
                        if (tx.Verify(tmpool))
                            verified.Add(tx);
                    });

                    if (verified.Count == 0) continue;

                    foreach (Transaction tx in verified)
                        mem_pool.Add(tx.Hash, tx);

                    CheckMemPool();
                }
                RelayDirectly(verified);
                if (InventoryReceived != null)
                    foreach (Transaction tx in verified)
                        InventoryReceived(this, tx);
            }
        }

        public static void AllowHashes(IEnumerable<UInt256> hashes)
        {
            lock (KnownHashes)
            {
                KnownHashes.ExceptWith(hashes);
            }
        }

        private void Blockchain_PersistCompleted(object sender, Block block)
        {
            Transaction[] remain;
            lock (mem_pool)
            {
                foreach (Transaction tx in block.Transactions)
                {
                    mem_pool.Remove(tx.Hash);
                }
                if (mem_pool.Count == 0) return;

                remain = mem_pool.Values.ToArray();
                mem_pool.Clear();
            }
            lock (temp_pool)
            {
                temp_pool.UnionWith(remain);
            }
            new_tx_event.Set();
        }

        private static void CheckMemPool()
        {
            if (mem_pool.Count <= MemoryPoolSize) return;
            UInt256[] hashes = mem_pool.Values.AsParallel().OrderBy(p => p.NetworkFee / p.Size).Take(mem_pool.Count - MemoryPoolSize).Select(p => p.Hash).ToArray();
            foreach (UInt256 hash in hashes)
                mem_pool.Remove(hash);
        }

        public async Task ConnectToPeerAsync(string hostNameOrAddress, int port)
        {
            IPAddress ipAddress;
            if (IPAddress.TryParse(hostNameOrAddress, out ipAddress))
            {
                ipAddress = ipAddress.MapToIPv6();
            }
            else
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
                ipAddress = entry.AddressList.FirstOrDefault(p => p.AddressFamily == AddressFamily.InterNetwork || p.IsIPv6Teredo)?.MapToIPv6();
                if (ipAddress == null) return;
            }
            await ConnectToPeerAsync(new IPEndPoint(ipAddress, port));
        }

        public async Task ConnectToPeerAsync(IPEndPoint remoteEndpoint)
        {
            if (remoteEndpoint.Port == Port && LocalAddresses.Contains(remoteEndpoint.Address)) return;
            lock (unconnectedPeers)
            {
                unconnectedPeers.Remove(remoteEndpoint);
            }
            lock (connectedPeers)
            {
                if (connectedPeers.Any(p => remoteEndpoint.Equals(p.ListenerEndpoint)))
                    return;
            }
            TcpRemoteNode remoteNode = new TcpRemoteNode(this, remoteEndpoint);
            if (await remoteNode.ConnectAsync())
            {
                OnConnected(remoteNode);
            }
        }

        private void ConnectToPeersLoop()
        {
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                int connectedCount = connectedPeers.Count;
                int unconnectedCount = unconnectedPeers.Count;
                if (connectedCount < ConnectedMax)
                {
                    Task[] tasks = { };
                    if (unconnectedCount > 0)
                    {
                        IPEndPoint[] endpoints;
                        lock (unconnectedPeers)
                        {
                            endpoints = unconnectedPeers.Take(ConnectedMax - connectedCount).ToArray();
                        }
                        tasks = endpoints.Select(p => ConnectToPeerAsync(p)).ToArray();
                    }
                    else if (connectedCount > 0)
                    {
                        lock (connectedPeers)
                        {
                            foreach (RemoteNode node in connectedPeers)
                                node.RequestPeers();
                        }
                    }
                    else
                    {
                        tasks = Settings.Default.SeedList.OfType<string>().Select(p => p.Split(':')).Select(p => ConnectToPeerAsync(p[0], int.Parse(p[1]))).ToArray();
                    }
                    try
                    {
                        Task.WaitAll(tasks, cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
                for (int i = 0; i < 50 && !cancellationTokenSource.IsCancellationRequested; i++)
                {
                    Thread.Sleep(100);
                }
            }
        }

        public static bool ContainsTransaction(UInt256 hash)
        {
            lock (mem_pool)
            {
                return mem_pool.ContainsKey(hash);
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
            {
                cancellationTokenSource.Cancel();
                if (started > 0)
                {
                    Blockchain.PersistCompleted -= Blockchain_PersistCompleted;
                    if (listener != null) listener.Stop();
                    if (!connectThread.ThreadState.HasFlag(ThreadState.Unstarted)) connectThread.Join();
                    lock (unconnectedPeers)
                    {
                        if (unconnectedPeers.Count < UnconnectedMax)
                        {
                            lock (connectedPeers)
                            {
                                unconnectedPeers.UnionWith(connectedPeers.Select(p => p.ListenerEndpoint).Where(p => p != null).Take(UnconnectedMax - unconnectedPeers.Count));
                            }
                        }
                    }
                    RemoteNode[] nodes;
                    lock (connectedPeers)
                    {
                        nodes = connectedPeers.ToArray();
                    }
                    Task.WaitAll(nodes.Select(p => Task.Run(() => p.Disconnect(false))).ToArray());
                    new_tx_event.Set();
                    if (poolThread?.ThreadState.HasFlag(ThreadState.Unstarted) == false)
                        poolThread.Join();
                    new_tx_event.Dispose();
                }
            }
        }

        public static IEnumerable<Transaction> GetMemoryPool()
        {
            lock (mem_pool)
            {
                foreach (Transaction tx in mem_pool.Values)
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

        public static Transaction GetTransaction(UInt256 hash)
        {
            lock (mem_pool)
            {
                if (!mem_pool.TryGetValue(hash, out Transaction tx))
                    return null;
                return tx;
            }
        }

        private static bool IsIntranetAddress(IPAddress address)
        {
            byte[] data = address.MapToIPv4().GetAddressBytes();
            Array.Reverse(data);
            uint value = data.ToUInt32(0);
            return (value & 0xff000000) == 0x0a000000 || (value & 0xff000000) == 0x7f000000 || (value & 0xfff00000) == 0xac100000 || (value & 0xffff0000) == 0xc0a80000 || (value & 0xffff0000) == 0xa9fe0000;
        }

        public static void LoadState(Stream stream)
        {
            lock (unconnectedPeers)
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
        }

        private void OnConnected(RemoteNode remoteNode)
        {
            lock (connectedPeers)
            {
                connectedPeers.Add(remoteNode);
            }
            remoteNode.Disconnected += RemoteNode_Disconnected;
            remoteNode.InventoryReceived += RemoteNode_InventoryReceived;
            remoteNode.PeersReceived += RemoteNode_PeersReceived;
            remoteNode.StartProtocol();
        }

        private async Task ProcessWebSocketAsync(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest) return;
            WebSocket ws = await context.WebSockets.AcceptWebSocketAsync();
            WebSocketRemoteNode remoteNode = new WebSocketRemoteNode(this, ws, new IPEndPoint(context.Connection.RemoteIpAddress, context.Connection.RemotePort));
            OnConnected(remoteNode);
        }

        public bool Relay(IInventory inventory)
        {
            if (inventory is MinerTransaction) return false;
            lock (KnownHashes)
            {
                if (!KnownHashes.Add(inventory.Hash)) return false;
            }
            InventoryReceivingEventArgs args = new InventoryReceivingEventArgs(inventory);
            InventoryReceiving?.Invoke(this, args);
            if (args.Cancel) return false;
            if (inventory is Block block)
            {
                if (Blockchain.Default == null) return false;
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
            bool relayed = RelayDirectly(inventory);
            InventoryReceived?.Invoke(this, inventory);
            return relayed;
        }

        public bool RelayDirectly(IInventory inventory)
        {
            bool relayed = false;
            lock (connectedPeers)
            {
                RelayCache.Add(inventory);
                foreach (RemoteNode node in connectedPeers)
                    relayed |= node.Relay(inventory);
            }
            return relayed;
        }

        private void RelayDirectly(IReadOnlyCollection<Transaction> transactions)
        {
            lock (connectedPeers)
            {
                foreach (RemoteNode node in connectedPeers)
                    node.Relay(transactions);
            }
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
                lock (connectedPeers)
                {
                    if (remoteNode.ListenerEndpoint != null)
                    {
                        unconnectedPeers.Remove(remoteNode.ListenerEndpoint);
                    }
                    connectedPeers.Remove(remoteNode);
                }
            }
        }

        private void RemoteNode_InventoryReceived(object sender, IInventory inventory)
        {
            if (inventory is Transaction tx && tx.Type != TransactionType.ClaimTransaction && tx.Type != TransactionType.IssueTransaction)
            {
                if (Blockchain.Default == null) return;
                lock (KnownHashes)
                {
                    if (!KnownHashes.Add(inventory.Hash)) return;
                }
                InventoryReceivingEventArgs args = new InventoryReceivingEventArgs(inventory);
                InventoryReceiving?.Invoke(this, args);
                if (args.Cancel) return;
                lock (temp_pool)
                {
                    temp_pool.Add(tx);
                }
                new_tx_event.Set();
            }
            else
            {
                Relay(inventory);
            }
        }

        private void RemoteNode_PeersReceived(object sender, IPEndPoint[] peers)
        {
            lock (unconnectedPeers)
            {
                if (unconnectedPeers.Count < UnconnectedMax)
                {
                    lock (badPeers)
                    {
                        lock (connectedPeers)
                        {
                            unconnectedPeers.UnionWith(peers);
                            unconnectedPeers.ExceptWith(badPeers);
                            unconnectedPeers.ExceptWith(connectedPeers.Select(p => p.ListenerEndpoint));
                        }
                    }
                }
            }
        }

        public IPEndPoint[] GetUnconnectedPeers()
        {
            lock (unconnectedPeers)
            {
                return unconnectedPeers.ToArray();
            }
        }

        public IPEndPoint[] GetBadPeers()
        {
            lock (badPeers)
            {
                return badPeers.ToArray();
            }
        }

        public static void SaveState(Stream stream)
        {
            IPEndPoint[] peers;
            lock (unconnectedPeers)
            {
                peers = unconnectedPeers.Take(UnconnectedMax).ToArray();
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

        public void Start(int port = 0, int ws_port = 0)
        {
            if (Interlocked.Exchange(ref started, 1) == 0)
            {
                Task.Run(async () =>
                {
                    if ((port > 0 || ws_port > 0)
                        && UpnpEnabled
                        && LocalAddresses.All(p => !p.IsIPv4MappedToIPv6 || IsIntranetAddress(p))
                        && await UPnP.DiscoverAsync())
                    {
                        try
                        {
                            LocalAddresses.Add(await UPnP.GetExternalIPAsync());
                            if (port > 0)
                                await UPnP.ForwardPortAsync(port, ProtocolType.Tcp, "NEO");
                            if (ws_port > 0)
                                await UPnP.ForwardPortAsync(ws_port, ProtocolType.Tcp, "NEO WebSocket");
                        }
                        catch { }
                    }
                    connectThread.Start();
                    poolThread?.Start();
                    if (port > 0)
                    {
                        listener = new TcpListener(IPAddress.Any, port);
                        try
                        {
                            listener.Start();
                            Port = (ushort)port;
                            AcceptPeers();
                        }
                        catch (SocketException) { }
                    }
                    if (ws_port > 0)
                    {
                        ws_host = new WebHostBuilder().UseKestrel().UseUrls($"http://*:{ws_port}").Configure(app => app.UseWebSockets().Run(ProcessWebSocketAsync)).Build();
                        ws_host.Start();
                    }
                });
            }
        }

        public void SynchronizeMemoryPool()
        {
            lock (connectedPeers)
            {
                foreach (RemoteNode node in connectedPeers)
                    node.RequestMemoryPool();
            }
        }
    }
}
