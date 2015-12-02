using AntShares.Core;
using AntShares.IO;
using AntShares.Network.Payloads;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AntShares.Network
{
    public class RemoteNode : IDisposable
    {
        internal event EventHandler<Block> BlockReceived;
        public event EventHandler<bool> Disconnected;
        internal event EventHandler<IPEndPoint[]> PeersReceived;
        internal event EventHandler<Transaction> TransactionReceived;

        private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);

        private static HashSet<UInt256> KnownHashes = new HashSet<UInt256>();
        private static HashSet<UInt256> missions_global = new HashSet<UInt256>();
        private HashSet<UInt256> missions = new HashSet<UInt256>();

        private LocalNode localNode;
        private TcpClient tcp;
        private NetworkStream stream;
        private bool connected = false;
        private int disposed = 0;

        internal VersionPayload Version { get; private set; }
        public IPEndPoint RemoteEndpoint { get; private set; }
        public IPEndPoint ListenerEndpoint { get; private set; }

        internal RemoteNode(LocalNode localNode, IPEndPoint remoteEndpoint)
        {
            this.localNode = localNode;
            this.tcp = new TcpClient(remoteEndpoint.Address.IsIPv4MappedToIPv6 ? AddressFamily.InterNetwork : remoteEndpoint.AddressFamily);
            this.ListenerEndpoint = remoteEndpoint;
        }

        internal RemoteNode(LocalNode localNode, TcpClient tcp)
        {
            this.localNode = localNode;
            this.tcp = tcp;
            OnConnected();
        }

        internal async Task ConnectAsync()
        {
            IPAddress address = ListenerEndpoint.Address;
            if (address.IsIPv4MappedToIPv6)
                address = address.MapToIPv4();
            try
            {
                await tcp.ConnectAsync(address, ListenerEndpoint.Port);
            }
            catch (SocketException)
            {
                Disconnect(true);
                return;
            }
            OnConnected();
            StartProtocol();
        }

        public void Disconnect(bool error)
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
            {
                tcp.Close();
                if (Disconnected != null)
                {
                    Disconnected(this, error);
                }
                lock (missions_global)
                {
                    foreach (UInt256 hash in missions)
                    {
                        missions_global.Remove(hash);
                    }
                }
            }
        }

        public void Dispose()
        {
            Disconnect(false);
        }

        private void OnAddrMessageReceived(AddrPayload payload)
        {
            IPEndPoint[] peers = payload.AddressList.Select(p => p.EndPoint).Where(p => !p.Equals(localNode.LocalEndpoint)).ToArray();
            if (PeersReceived != null && peers.Length > 0)
            {
                PeersReceived(this, peers);
            }
        }

        private void OnConnected()
        {
            IPEndPoint remoteEndpoint = (IPEndPoint)tcp.Client.RemoteEndPoint;
            remoteEndpoint = new IPEndPoint(remoteEndpoint.Address.MapToIPv6(), remoteEndpoint.Port);
            lock (localNode.pendingPeers)
                lock (localNode.connectedPeers)
                {
                    if (localNode.pendingPeers.All(p => p.RemoteEndpoint != remoteEndpoint) && !localNode.connectedPeers.ContainsKey(remoteEndpoint))
                    {
                        RemoteEndpoint = remoteEndpoint;
                    }
                }
            if (RemoteEndpoint == null)
            {
                Disconnect(false);
                return;
            }
            stream = tcp.GetStream();
            connected = true;
        }

        private async Task OnGetAddrMessageReceivedAsync()
        {
            if (!localNode.ServiceEnabled) return;
            AddrPayload payload;
            lock (localNode.connectedPeers)
            {
                payload = AddrPayload.Create(localNode.connectedPeers.Values.Where(p => p.ListenerEndpoint != null).Take(100).Select(p => NetworkAddressWithTime.Create(p.ListenerEndpoint, p.Version.Services, p.Version.Timestamp)).ToArray());
            }
            await SendMessageAsync("addr", payload);
        }

        private async Task OnGetBlocksMessageReceivedAsync(GetBlocksPayload payload)
        {
            if (!localNode.ServiceEnabled) return;
            if (Blockchain.Default == null) return;
            if (!Blockchain.Default.Ability.HasFlag(BlockchainAbility.BlockIndexes)) return;
            UInt256 hash = payload.HashStart.Select(p => Blockchain.Default.GetHeader(p)).Where(p => p != null).OrderBy(p => p.Height).Select(p => p.Hash).FirstOrDefault();
            if (hash == null || hash == payload.HashStop) return;
            List<UInt256> hashes = new List<UInt256>();
            do
            {
                hash = Blockchain.Default.GetNextBlockHash(hash);
                if (hash == null) break;
                hashes.Add(hash);
            } while (hash != payload.HashStop && hashes.Count < 500);
            await SendMessageAsync("inv", InvPayload.Create(InventoryType.Block, hashes.ToArray()));
        }

        private async Task OnGetDataMessageReceivedAsync(GetDataPayload payload)
        {
            foreach (InventoryVector vector in payload.Inventories.Distinct())
            {
                Inventory inventory;
                if (!localNode.RelayCache.TryGet(vector.Hash, out inventory) && !localNode.ServiceEnabled)
                    continue;
                switch (vector.Type)
                {
                    case InventoryType.TX:
                        if (inventory == null && Blockchain.Default != null)
                            inventory = Blockchain.Default.GetTransaction(vector.Hash);
                        if (inventory != null)
                            await SendMessageAsync("tx", inventory);
                        break;
                    case InventoryType.Block:
                        if (inventory == null && Blockchain.Default != null)
                            inventory = Blockchain.Default.GetBlock(vector.Hash);
                        if (inventory != null)
                            await SendMessageAsync("block", inventory);
                        break;
                }
            }
        }

        private async Task OnGetHeadersMessageReceivedAsync(GetBlocksPayload payload)
        {
            if (!localNode.ServiceEnabled) return;
            if (Blockchain.Default == null) return;
            if (!Blockchain.Default.Ability.HasFlag(BlockchainAbility.BlockIndexes)) return;
            UInt256 hash = payload.HashStart.Select(p => Blockchain.Default.GetHeader(p)).Where(p => p != null).OrderBy(p => p.Height).Select(p => p.Hash).FirstOrDefault();
            if (hash == null || hash == payload.HashStop) return;
            List<Block> headers = new List<Block>();
            do
            {
                hash = Blockchain.Default.GetNextBlockHash(hash);
                if (hash == null) break;
                headers.Add(Blockchain.Default.GetHeader(hash));
            } while (hash != payload.HashStop && headers.Count < 2000);
            await SendMessageAsync("headers", HeadersPayload.Create(headers));
        }

        private async Task OnHeadersMessageReceivedAsync(HeadersPayload payload)
        {
            if (Blockchain.Default == null) return;
            Blockchain.Default.AddHeaders(payload.Headers);
            if (Blockchain.Default.HeaderHeight < Version.StartHeight)
            {
                await SendMessageAsync("getheaders", GetBlocksPayload.Create(Blockchain.Default.GetLeafHeaderHashes()));
            }
        }

        private void OnInventoryReceived(Inventory inventory)
        {
            lock (KnownHashes)
            {
                KnownHashes.Add(inventory.Hash);
            }
            lock (missions_global)
            {
                missions_global.Remove(inventory.Hash);
            }
            missions.Remove(inventory.Hash);
            if (inventory is Block)
            {
                if (BlockReceived != null) BlockReceived(this, (Block)inventory);
            }
            else if (inventory is Transaction)
            {
                if (TransactionReceived != null) TransactionReceived(this, (Transaction)inventory);
            }
        }

        private async Task OnInvMessageReceivedAsync(InvPayload payload)
        {
            InventoryVector[] vectors = payload.Inventories.Distinct().Where(p => Enum.IsDefined(typeof(InventoryType), p.Type)).ToArray();
            lock (KnownHashes)
            {
                vectors = vectors.Where(p => !KnownHashes.Contains(p.Hash)).ToArray();
            }
            if (vectors.Length == 0) return;
            lock (missions_global)
            {
                if (localNode.GlobalMissionsEnabled)
                    vectors = vectors.Where(p => !missions_global.Contains(p.Hash)).ToArray();
                foreach (InventoryVector vector in vectors)
                {
                    missions_global.Add(vector.Hash);
                    missions.Add(vector.Hash);
                }
            }
            if (vectors.Length == 0) return;
            await SendMessageAsync("getdata", GetDataPayload.Create(vectors));
        }

        private async Task OnMessageReceivedAsync(Message message)
        {
            switch (message.Command)
            {
                case "addr":
                    OnAddrMessageReceived(message.Payload.AsSerializable<AddrPayload>());
                    break;
                case "block":
                    OnInventoryReceived(message.Payload.AsSerializable<Block>());
                    break;
                case "consrequest":
                    //OnNewInventory(message.Payload.AsSerializable<BlockConsensusRequest>());
                    break;
                case "consresponse":
                    //OnNewInventory(message.Payload.AsSerializable<BlockConsensusResponse>());
                    break;
                case "getaddr":
                    await OnGetAddrMessageReceivedAsync();
                    break;
                case "getblocks":
                    await OnGetBlocksMessageReceivedAsync(message.Payload.AsSerializable<GetBlocksPayload>());
                    break;
                case "getdata":
                    await OnGetDataMessageReceivedAsync(message.Payload.AsSerializable<GetDataPayload>());
                    break;
                case "getheaders":
                    await OnGetHeadersMessageReceivedAsync(message.Payload.AsSerializable<GetBlocksPayload>());
                    break;
                case "headers":
                    await OnHeadersMessageReceivedAsync(message.Payload.AsSerializable<HeadersPayload>());
                    break;
                case "inv":
                    await OnInvMessageReceivedAsync(message.Payload.AsSerializable<InvPayload>());
                    break;
                case "ping":
                    await OnPingMessageReceivedAsync(message.Payload);
                    break;
                case "tx":
                    OnInventoryReceived(Transaction.DeserializeFrom(message.Payload));
                    break;
                case "alert":
                case "mempool":
                    //暂时忽略
                    break;
                case "notfound":
                case "pong":
                case "reject":
                    break;
                case "verack":
                case "version":
                default:
                    Disconnect(true);
                    break;
            }
        }

        private async Task OnPingMessageReceivedAsync(byte[] payload)
        {
            await SendMessageAsync(Message.Create("pong", payload));
        }

        private async Task<Message> ReceiveMessageAsync(TimeSpan timeout)
        {
            using (CancellationTokenSource source = new CancellationTokenSource(timeout))
            {
                try
                {
                    return await Message.DeserializeFromStreamAsync(stream, timeout == Timeout.InfiniteTimeSpan ? CancellationToken.None : source.Token);
                }
                catch (ObjectDisposedException) { }
                catch (FormatException)
                {
                    Disconnect(true);
                }
                catch (IOException)
                {
                    Disconnect(true);
                }
                catch (TaskCanceledException)
                {
                    Disconnect(true);
                }
            }
            return null;
        }

        internal async Task RelayAsync(Inventory data)
        {
            await SendMessageAsync("inv", InvPayload.Create(data.InventoryType, data.Hash));
        }

        internal async Task RequestPeersAsync()
        {
            await SendMessageAsync("getaddr");
        }

        private async Task<bool> SendMessageAsync(Message message)
        {
            if (!connected)
                throw new InvalidOperationException();
            if (disposed > 0)
                return false;
            byte[] buffer = message.ToArray();
            try
            {
                await stream.WriteAsync(buffer, 0, buffer.Length);
                return true;
            }
            catch (ObjectDisposedException) { }
            catch (IOException)
            {
                Disconnect(true);
            }
            return false;
        }

        private async Task<bool> SendMessageAsync(string command, ISerializable payload = null)
        {
            return await SendMessageAsync(Message.Create(command, payload));
        }

        internal async void StartProtocol()
        {
            if (!await SendMessageAsync("version", VersionPayload.Create(localNode.LocalEndpoint?.Port ?? 0, localNode.UserAgent, Blockchain.Default?.Height ?? 0)))
                return;
            Message message = await ReceiveMessageAsync(TimeSpan.FromSeconds(30));
            if (message == null)
                return;
            if (message.Command != "version")
            {
                Disconnect(true);
                return;
            }
            try
            {
                Version = message.Payload.AsSerializable<VersionPayload>();
            }
            catch (FormatException)
            {
                Disconnect(true);
                return;
            }
            if (ListenerEndpoint != null)
            {
                if (ListenerEndpoint.Port != Version.Port)
                {
                    Disconnect(true);
                    return;
                }
            }
            else if (Version.Port > 0)
            {
                IPAddress ip = ((IPEndPoint)tcp.Client.RemoteEndPoint).Address.MapToIPv6();
                ListenerEndpoint = new IPEndPoint(ip, Version.Port);
            }
            if (!await SendMessageAsync("verack"))
                return;
            message = await ReceiveMessageAsync(TimeSpan.FromSeconds(30));
            if (message == null) return;
            if (message.Command != "verack")
            {
                Disconnect(true);
                return;
            }
            lock (localNode.pendingPeers)
            {
                lock (localNode.connectedPeers)
                {
                    localNode.connectedPeers.Add(RemoteEndpoint, this);
                }
                localNode.pendingPeers.Remove(this);
            }
            if (Blockchain.Default?.HeaderHeight < Version.StartHeight)
            {
                HashSet<UInt256> hashes = new HashSet<UInt256>(Blockchain.Default.GetLeafHeaderHashes());
                hashes.UnionWith(hashes.Select(p => Blockchain.Default.GetHeader(p).PrevBlock).ToArray());
                await SendMessageAsync("getheaders", GetBlocksPayload.Create(hashes));
            }
            while (disposed == 0)
            {
                if (Blockchain.Default != null && !Blockchain.Default.IsReadOnly)
                {
                    if (missions.Count == 0 && Blockchain.Default.Height < Version.StartHeight)
                    {
                        if (!await SendMessageAsync("getblocks", GetBlocksPayload.Create(new[] { Blockchain.Default.CurrentBlockHash })))
                            break;
                    }
                }
                TimeSpan timeout = missions.Count == 0 ? TimeSpan.FromMinutes(30) : TimeSpan.FromSeconds(60);
                message = await ReceiveMessageAsync(timeout);
                if (message == null) break;
                try
                {
                    await OnMessageReceivedAsync(message);
                }
                catch (FormatException)
                {
                    Disconnect(true);
                    break;
                }
            }
        }
    }
}
