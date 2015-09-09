using AntShares.Core;
using AntShares.IO;
using AntShares.Miner;
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
        internal event EventHandler<Inventory> NewInventory;
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
            if (mission_current != null) return;
            if (missions.Count == 0)
            {
                if (!Blockchain.Default.IsReadOnly && Blockchain.Default.Height < Version.StartHeight)
                {
                    //TODO: 改为headers-first模式下载区块链
                    SendMessage("getblocks", GetBlocksPayload.Create(Blockchain.Default.CurrentBlockHash));
                }
            }
            else
            {
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

        private void OnGetBlocksMessageReceived(GetBlocksPayload payload)
        {
            if (!Blockchain.Default.Ability.HasFlag(BlockchainAbility.BlockIndexes))
                return;
            UInt256 hash = payload.HashStart.Select(p => new
            {
                Hash = p,
                Height = Blockchain.Default.GetBlockHeight(p)
            }).Where(p => p.Height >= 0).OrderBy(p => p.Height).Select(p => p.Hash).FirstOrDefault();
            if (hash == null || hash == payload.HashStop) return;
            List<UInt256> hashes = new List<UInt256>();
            do
            {
                hash = Blockchain.Default.GetNextBlockHash(hash);
                if (hash == null) break;
                hashes.Add(hash);
            } while (hash != payload.HashStop && hashes.Count < 500);
            SendMessage("inv", InvPayload.Create(InventoryType.Block, hashes.ToArray()));
        }

        private void OnGetDataMessageReceived(GetDataPayload payload)
        {
            foreach (InventoryVector vector in payload.Inventories.Distinct())
            {
                Inventory data;
                if (localNode.RelayCache.TryGet(vector.Hash, out data))
                {
                    SendMessage(vector.Type.GetCommandName(), data);
                    continue;
                }
                switch (vector.Type)
                {
                    case InventoryType.Block:
                        {
                            Block block = Blockchain.Default.GetBlock(vector.Hash);
                            if (block != null)
                            {
                                SendMessage("block", block);
                            }
                        }
                        break;
                    case InventoryType.TX:
                        {
                            Transaction tx = Blockchain.Default.GetTransaction(vector.Hash);
                            if (tx != null)
                            {
                                SendMessage("tx", tx);
                            }
                        }
                        break;
                }
            }
        }

        private void OnGetHeadersMessageReceived(GetBlocksPayload payload)
        {
            if (!Blockchain.Default.Ability.HasFlag(BlockchainAbility.BlockIndexes))
                return;
            UInt256 hash = payload.HashStart.Select(p => new
            {
                Hash = p,
                Height = Blockchain.Default.GetBlockHeight(p)
            }).Where(p => p.Height >= 0).OrderBy(p => p.Height).Select(p => p.Hash).FirstOrDefault();
            if (hash == null || hash == payload.HashStop) return;
            List<BlockHeader> headers = new List<BlockHeader>();
            do
            {
                hash = Blockchain.Default.GetNextBlockHash(hash);
                if (hash == null) break;
                headers.Add(Blockchain.Default.GetHeader(hash));
            } while (hash != payload.HashStop && headers.Count < 2000);
            SendMessage("headers", HeadersPayload.Create(headers));
        }

        private void OnInvMessageReceived(InvPayload payload)
        {
            IEnumerable<InventoryVector> vectors = payload.Inventories.Distinct().Where(p => Enum.IsDefined(typeof(InventoryType), p.Type));
            lock (LocalNode.KnownHashes)
            {
                vectors = vectors.Where(p => !LocalNode.KnownHashes.Contains(p.Hash)).ToArray();
            }
            vectors = vectors.Where(p => p.Type != InventoryType.TX || !Blockchain.Default.ContainsTransaction(p.Hash));
            vectors = vectors.Where(p => p.Type != InventoryType.Block || !Blockchain.Default.ContainsBlock(p.Hash));
            InventoryVector[] vectors_list = vectors.ToArray();
            if (vectors_list.Length == 0) return;
            lock (missions_global)
            {
                foreach (InventoryVector vector in vectors_list)
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

        private void OnMessageReceived(Message message)
        {
            switch (message.Command)
            {
                case "addr":
                    OnAddrMessageReceived(message.Payload.AsSerializable<AddrPayload>());
                    break;
                case "block":
                    OnNewInventory(message.Payload.AsSerializable<Block>());
                    break;
                case "consrequest":
                    OnNewInventory(message.Payload.AsSerializable<BlockConsensusRequest>());
                    break;
                case "consresponse":
                    OnNewInventory(message.Payload.AsSerializable<BlockConsensusResponse>());
                    break;
                case "getaddr":
                    OnGetAddrMessageReceived();
                    break;
                case "getblocks":
                    OnGetBlocksMessageReceived(message.Payload.AsSerializable<GetBlocksPayload>());
                    break;
                case "getdata":
                    OnGetDataMessageReceived(message.Payload.AsSerializable<GetDataPayload>());
                    break;
                case "getheaders":
                    OnGetHeadersMessageReceived(message.Payload.AsSerializable<GetBlocksPayload>());
                    break;
                case "inv":
                    OnInvMessageReceived(message.Payload.AsSerializable<InvPayload>());
                    break;
                case "ping":
                    OnPingMessageReceived(message.Payload);
                    break;
                case "tx":
                    OnNewInventory(Transaction.DeserializeFrom(message.Payload));
                    break;
                case "alert":
                case "headers":
                case "pong":
                    //暂时忽略
                    break;
                case "verack":
                case "version":
                default:
                    Disconnect(true);
                    break;
            }
        }

        private void OnNewInventory(Inventory inventory)
        {
            if (NewInventory != null)
            {
                NewInventory(this, inventory);
            }
        }

        private void OnPingMessageReceived(byte[] payload)
        {
            SendMessage(Message.Create("pong", payload));
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

        internal async Task RelayAsync(Inventory data)
        {
            await SendMessageAsync("inv", InvPayload.Create(data.InventoryType, data.Hash));
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
            if (!await SendMessageAsync("version", VersionPayload.Create(localNode.LocalEndpoint.Port, localNode.UserAgent, Blockchain.Default.Height)))
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
