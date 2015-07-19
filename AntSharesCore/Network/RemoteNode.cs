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

        private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);

        private static Dictionary<UInt256, Mission> missions_global = new Dictionary<UInt256, Mission>();
        private Dictionary<UInt256, Mission> missions = new Dictionary<UInt256, Mission>();
        private Mission mission_current = null;
        private DateTime mission_start_time = DateTime.MinValue;

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
            if (mission_current != null && DateTime.Now - mission_start_time > OneMinute)
            {
                missions.Remove(mission_current.Hash);
                mission_current = null;
            }
            if (mission_current != null || missions.Count == 0) return;
            lock (missions_global)
            {
                foreach (UInt256 hash in missions.Keys.ToArray())
                {
                    if (!missions_global.ContainsKey(hash))
                    {
                        missions.Remove(hash);
                    }
                }
                mission_current = missions.Values.Min();
                mission_current.LaunchTimes++;
            }
            mission_start_time = DateTime.Now;
            SendMessage("getdata", GetDataPayload.Create(mission_current.Type, mission_current.Hash));
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
            List<InventoryVector> mission_vectors = new List<InventoryVector>();
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
                        mission_vectors.AddRange(tx_vectors);
                        break;
                    case InventoryType.MSG_BLOCK:
                        InventoryVector[] block_vectors = group.Where(p => !Blockchain.Default.ContainsBlock(p.Hash)).ToArray();
                        mission_vectors.AddRange(block_vectors);
                        break;
                }
            }
            if (mission_vectors.Count > 0)
            {
                lock (missions_global)
                {
                    foreach (InventoryVector vector in mission_vectors)
                    {
                        if (!missions_global.ContainsKey(vector.Hash))
                        {
                            missions_global.Add(vector.Hash, new Mission
                            {
                                Hash = vector.Hash,
                                Type = vector.Type,
                                LaunchTimes = 0
                            });
                        }
                        if (!missions.ContainsKey(vector.Hash))
                        {
                            missions.Add(vector.Hash, missions_global[vector.Hash]);
                        }
                    }
                }
            }
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
                CheckMissions();
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
