using AntShares.Core;
using AntShares.IO;
using AntShares.Network.Payloads;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AntShares.Network
{
    public class RemoteNode : IDisposable
    {
        public event EventHandler<bool> Disconnected;
        internal event EventHandler<Block> NewBlock;
        internal event EventHandler<IPEndPoint[]> NewPeers;

        private HashSet<InventoryVector> missions = new HashSet<InventoryVector>();
        private LocalNode localNode;
        private TcpClient tcp;
        private BinaryReader reader;
        private BinaryWriter writer;
        private bool connected = false;
        private int disposed = 0;

        public IPEndPoint RemoteEndpoint { get; private set; }

        public VersionPayload Version { get; private set; }

        internal RemoteNode(LocalNode localNode, IPEndPoint remoteEndpoint)
        {
            this.localNode = localNode;
            this.tcp = new TcpClient();
            this.RemoteEndpoint = remoteEndpoint;
        }

        internal RemoteNode(LocalNode localNode, TcpClient tcp)
        {
            this.localNode = localNode;
            this.tcp = tcp;
            OnConnected();
        }

        private void CheckMissions()
        {
            //TODO: 检查是否存在下载任务并执行
            //在收到某些特定的包后触发本函数
        }

        internal async Task ConnectAsync()
        {
            try
            {
                await tcp.ConnectAsync(RemoteEndpoint.Address, RemoteEndpoint.Port);
            }
            catch (SocketException)
            {
                Disconnect(true);
                return;
            }
            OnConnected();
            await StartProtocolAsync();
        }

        public void Disconnect(bool error)
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
            {
                if (reader != null)
                    reader.Close();
                if (writer != null)
                    lock (writer)
                        writer.Close();
                tcp.Close();
                if (Disconnected != null)
                {
                    Disconnected(this, error);
                }
            }
        }

        public void Dispose()
        {
            Disconnect(false);
        }

        private void OnAddrMessageReceived(AddrPayload payload)
        {
            IPEndPoint[] peers = payload.AddressList.Select(p => p.GetIPEndPoint()).Where(p => !p.Equals(localNode.LocalEndpoint)).ToArray();
            if (NewPeers != null && peers.Length > 0)
            {
                NewPeers(this, peers);
            }
        }

        private void OnBlockMessageReceived(Block block)
        {
            if (NewBlock != null)
            {
                NewBlock(this, block);
            }
        }

        private void OnConnected()
        {
            reader = new BinaryReader(tcp.GetStream(), Encoding.UTF8, true);
            writer = new BinaryWriter(tcp.GetStream(), Encoding.UTF8, true);
            connected = true;
        }

        private void OnGetAddrMessageReceived()
        {
            AddrPayload payload;
            lock (localNode.connectedPeers)
            {
                payload = AddrPayload.Create(localNode.connectedPeers.Take(10).Select(p => NetworkAddressWithTime.Create(p.Value.RemoteEndpoint, p.Value.Version.Services, p.Value.Version.Timestamp)).ToArray());
            }
            SendMessage("addr", payload);
        }

        private void OnInvMessageReceived(InvPayload payload)
        {
            InventoryVector[] vectors;
            lock (LocalNode.KnownHashes)
            {
                vectors = payload.Inventories.Where(p => !LocalNode.KnownHashes.Contains(p.Hash)).ToArray();
            }
            foreach (var group in vectors.GroupBy(p => p.Type))
            {
                switch (group.Key)
                {
                    case InventoryType.MSG_TX:
                        InventoryVector[] tx_vectors;
                        lock (LocalNode.MemoryPool)
                        {
                            tx_vectors = group.Where(p => !LocalNode.MemoryPool.ContainsKey(p.Hash)).ToArray();
                        }
                        tx_vectors = tx_vectors.Where(p => !Blockchain.Default.ContainsTransaction(p.Hash)).ToArray();
                        missions.UnionWith(tx_vectors);
                        break;
                    case InventoryType.MSG_BLOCK:
                        missions.UnionWith(group.Where(p => !Blockchain.Default.ContainsBlock(p.Hash)));
                        break;
                }
            }
            CheckMissions();
        }

        private void OnMessageReceived(Message message)
        {
            switch (message.Command)
            {
                case "addr":
                    OnAddrMessageReceived(message.Payload.AsSerializable<AddrPayload>());
                    break;
                case "block":
                    OnBlockMessageReceived(message.Payload.AsSerializable<Block>());
                    break;
                case "getaddr":
                    OnGetAddrMessageReceived();
                    break;
                case "inv":
                    OnInvMessageReceived(message.Payload.AsSerializable<InvPayload>());
                    break;
                case "verack":
                case "version":
                default:
                    Disconnect(true);
                    break;
            }
        }

        private void ReceiveLoop()
        {
            while (disposed == 0)
            {
                Message message = ReceiveMessage();
                if (message == null)
                    break;
                try
                {
                    OnMessageReceived(message);
                }
                catch (FormatException)
                {
                    Disconnect(true);
                    break;
                }
            }
        }

        private Message ReceiveMessage()
        {
            try
            {
                return reader.ReadSerializable<Message>();
            }
            catch (FormatException)
            {
                Disconnect(true);
            }
            catch (IOException)
            {
                Disconnect(true);
            }
            return null;
        }

        private async Task<Message> ReceiveMessageAsync()
        {
            return await Task.Run(() =>
            {
                return ReceiveMessage();
            });
        }

        internal async Task RelayAsync(InventoryType type, UInt256 hash)
        {
            await SendMessageAsync("inv", InvPayload.Create(new InventoryVector
            {
                Type = type,
                Hash = hash
            }));
        }

        internal async Task RequestPeersAsync()
        {
            await SendMessageAsync("getaddr");
        }

        private bool SendMessage(Message message)
        {
            if (!connected)
                throw new InvalidOperationException();
            if (disposed > 0)
                return false;
            try
            {
                lock (writer)
                {
                    writer.Write(message);
                }
                return true;
            }
            catch (ObjectDisposedException) { }
            catch (IOException)
            {
                Disconnect(true);
            }
            return false;
        }

        private bool SendMessage(string command, ISerializable payload = null)
        {
            return SendMessage(Message.Create(command, payload));
        }

        private async Task<bool> SendMessageAsync(Message message)
        {
            return await Task.Run(() =>
            {
                return SendMessage(message);
            });
        }

        private async Task<bool> SendMessageAsync(string command, ISerializable payload = null)
        {
            return await SendMessageAsync(Message.Create(command, payload));
        }

        internal async Task StartProtocolAsync()
        {
            if (!await SendMessageAsync("version", VersionPayload.Create(localNode.LocalEndpoint.Port, localNode.UserAgent, 0)))
                return;
            Message message = await ReceiveMessageAsync();
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
            if (RemoteEndpoint != null && RemoteEndpoint.Port != Version.Port)
            {
                Disconnect(true);
                return;
            }
            if (RemoteEndpoint == null)
            {
                IPEndPoint remoteEndpoint = new IPEndPoint(((IPEndPoint)tcp.Client.RemoteEndPoint).Address, Version.Port);
                lock (localNode.pendingPeers)
                {
                    lock (localNode.connectedPeers)
                    {
                        if (localNode.pendingPeers.All(p => p.RemoteEndpoint != remoteEndpoint) && !localNode.connectedPeers.ContainsKey(remoteEndpoint))
                        {
                            RemoteEndpoint = remoteEndpoint;
                        }
                    }
                }
                if (RemoteEndpoint == null)
                {
                    Disconnect(false);
                    return;
                }
            }
            if (!await SendMessageAsync("verack"))
                return;
            message = await ReceiveMessageAsync();
            if (message == null)
                return;
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
            Thread thread = new Thread(ReceiveLoop);
            thread.Name = string.Format("ReceiveLoop@{0}", RemoteEndpoint);
            thread.Start();
        }
    }
}
