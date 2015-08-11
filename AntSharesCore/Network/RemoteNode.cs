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
        internal event EventHandler<Transaction> NewTransaction;

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

        private void OnGetBlocksMessageReceived(GetBlocksPayload payload)
        {
            if (!Blockchain.Default.Ability.HasFlag(BlockchainAbility.BlockIndexes))
                return;
            UInt256 hash = payload.HashStart.Select(p =>
            {
                uint Height;
                Block Block = Blockchain.Default.GetBlockAndHeight(p, out Height);
                return new { Block, Height };
            }).Where(p => p.Block != null).OrderBy(p => p.Height).Select(p => p.Block.Hash).FirstOrDefault();
            if (hash == null) return;
            //TODO: 找到相应的Block并发送inv消息
        }

        private void OnGetDataMessageReceived(GetDataPayload payload)
        {
            var groups = payload.Inventories.Distinct().ToLookup(p => p.Type);
            if (groups.Contains(InventoryType.MSG_TX))
            {
                HashSet<UInt256> hashes = new HashSet<UInt256>();
                List<Transaction> transactions = new List<Transaction>();
                lock (LocalNode.MemoryPool.SyncRoot)
                {
                    hashes.UnionWith(groups[InventoryType.MSG_TX].Where(p => LocalNode.MemoryPool.Contains(p.Hash)).Select(p => p.Hash));
                    transactions.AddRange(hashes.Select(p => LocalNode.MemoryPool[p]));
                }
                transactions.AddRange(groups[InventoryType.MSG_TX].Where(p => !hashes.Contains(p.Hash)).Select(p => Blockchain.Default.GetTransaction(p.Hash)).Where(p => p != null));
                foreach (Transaction tx in transactions)
                {
                    SendMessage("tx", tx);
                }
            }
            if (groups.Contains(InventoryType.MSG_BLOCK))
            {
                Block[] blocks = groups[InventoryType.MSG_BLOCK].Select(p => Blockchain.Default.GetBlock(p.Hash)).Where(p => p != null).ToArray();
                foreach (Block block in blocks)
                {
                    SendMessage("block", block);
                }
            }
        }

        private void OnInvMessageReceived(InvPayload payload)
        {
            InventoryVector[] vectors;
            lock (LocalNode.KnownHashes)
            {
                vectors = payload.Inventories.Distinct().Where(p => !LocalNode.KnownHashes.Contains(p.Hash)).ToArray();
            }
            var groups = vectors.ToLookup(p => p.Type);
            InventoryVector[] tx_vectors = new InventoryVector[0];
            if (groups.Contains(InventoryType.MSG_TX))
            {
                lock (LocalNode.MemoryPool.SyncRoot)
                {
                    tx_vectors = groups[InventoryType.MSG_TX].Where(p => !LocalNode.MemoryPool.Contains(p.Hash)).ToArray();
                }
                tx_vectors = tx_vectors.Where(p => !Blockchain.Default.ContainsTransaction(p.Hash)).ToArray();
            }
            InventoryVector[] block_vectors = new InventoryVector[0];
            if (groups.Contains(InventoryType.MSG_BLOCK))
            {
                block_vectors = groups[InventoryType.MSG_BLOCK].Where(p => !Blockchain.Default.ContainsBlock(p.Hash)).ToArray();
            }
            vectors = tx_vectors.Concat(block_vectors).ToArray();
            if (vectors.Length == 0) return;
            lock (missions_global)
            {
                foreach (InventoryVector vector in vectors)
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
                    OnBlockMessageReceived(message.Payload.AsSerializable<Block>());
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
                case "inv":
                    OnInvMessageReceived(message.Payload.AsSerializable<InvPayload>());
                    break;
                case "tx":
                    OnTxMessageReceived(Transaction.DeserializeFrom(message.Payload));
                    break;
                case "verack":
                case "version":
                default:
                    Disconnect(true);
                    break;
            }
        }

        private void OnTxMessageReceived(Transaction tx)
        {
            if (NewTransaction != null)
            {
                NewTransaction(this, tx);
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
