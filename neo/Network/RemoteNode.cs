using Neo.Core;
using Neo.Cryptography;
using Neo.IO;
using Neo.Network.Payloads;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Network
{
    public abstract class RemoteNode : IDisposable
    {
        public event EventHandler<bool> Disconnected;
        internal event EventHandler<IInventory> InventoryReceived;
        internal event EventHandler<IPEndPoint[]> PeersReceived;

        private static readonly TimeSpan HalfMinute = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan HalfHour = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan MissionExpiration = TimeSpan.FromMinutes(1);

        private Queue<Message> message_queue_high = new Queue<Message>();
        private Queue<Message> message_queue_low = new Queue<Message>();
        private static Dictionary<UInt256, DateTime> missions_global = new Dictionary<UInt256, DateTime>();
        private HashSet<UInt256> missions = new HashSet<UInt256>();
        private DateTime mission_start = DateTime.Now.AddYears(100);

        private LocalNode localNode;
        private int disposed = 0;
        private BloomFilter bloom_filter;

        public VersionPayload Version { get; private set; }
        public IPEndPoint RemoteEndpoint { get; protected set; }
        public IPEndPoint ListenerEndpoint { get; protected set; }

        protected RemoteNode(LocalNode localNode)
        {
            this.localNode = localNode;
        }

        public virtual void Disconnect(bool error)
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
            {
                Disconnected?.Invoke(this, error);
                bool needSync = false;
                lock (missions_global)
                    lock (missions)
                        if (missions.Count > 0)
                        {
                            foreach (UInt256 hash in missions)
                                missions_global.Remove(hash);
                            needSync = true;
                        }
                if (needSync)
                    localNode.RequestGetBlocks();
            }
        }

        public void Dispose()
        {
            Disconnect(false);
        }

        public void EnqueueMessage(string command, ISerializable payload = null)
        {
            bool is_single = false;
            switch (command)
            {
                case "addr":
                case "getaddr":
                case "getblocks":
                case "getheaders":
                case "mempool":
                    is_single = true;
                    break;
            }
            Queue<Message> message_queue;
            switch (command)
            {
                case "alert":
                case "consensus":
                case "filteradd":
                case "filterclear":
                case "filterload":
                case "getaddr":
                case "mempool":
                    message_queue = message_queue_high;
                    break;
                default:
                    message_queue = message_queue_low;
                    break;
            }
            lock (message_queue)
            {
                if (!is_single || message_queue.All(p => p.Command != command))
                {
                    message_queue.Enqueue(Message.Create(command, payload));
                }
            }
        }

        private void OnAddrMessageReceived(AddrPayload payload)
        {
            IPEndPoint[] peers = payload.AddressList.Select(p => p.EndPoint).Where(p => p.Port != localNode.Port || !LocalNode.LocalAddresses.Contains(p.Address)).ToArray();
            if (peers.Length > 0) PeersReceived?.Invoke(this, peers);
        }

        private void OnFilterAddMessageReceived(FilterAddPayload payload)
        {
            if (bloom_filter != null)
                bloom_filter.Add(payload.Data);
        }

        private void OnFilterClearMessageReceived()
        {
            bloom_filter = null;
        }

        private void OnFilterLoadMessageReceived(FilterLoadPayload payload)
        {
            bloom_filter = new BloomFilter(payload.Filter.Length * 8, payload.K, payload.Tweak, payload.Filter);
        }

        private void OnGetAddrMessageReceived()
        {
            if (!localNode.ServiceEnabled) return;
            AddrPayload payload;
            lock (localNode.connectedPeers)
            {
                const int MaxCountToSend = 200;
                IEnumerable<RemoteNode> peers = localNode.connectedPeers.Where(p => p.ListenerEndpoint != null && p.Version != null);
                if (localNode.connectedPeers.Count > MaxCountToSend)
                {
                    Random rand = new Random();
                    peers = peers.OrderBy(p => rand.Next());
                }
                peers = peers.Take(MaxCountToSend);
                payload = AddrPayload.Create(peers.Select(p => NetworkAddressWithTime.Create(p.ListenerEndpoint, p.Version.Services, p.Version.Timestamp)).ToArray());
            }
            EnqueueMessage("addr", payload);
        }

        private void OnGetBlocksMessageReceived(GetBlocksPayload payload)
        {
            if (!localNode.ServiceEnabled) return;
            if (Blockchain.Default == null) return;
            UInt256 hash = payload.HashStart.Select(p => Blockchain.Default.GetHeader(p)).Where(p => p != null).OrderBy(p => p.Index).Select(p => p.Hash).FirstOrDefault();
            if (hash == null || hash == payload.HashStop) return;
            List<UInt256> hashes = new List<UInt256>();
            do
            {
                hash = Blockchain.Default.GetNextBlockHash(hash);
                if (hash == null) break;
                hashes.Add(hash);
            } while (hash != payload.HashStop && hashes.Count < 500);
            if (hashes.Count > 0)
            {
                EnqueueMessage("inv", InvPayload.Create(InventoryType.Block, hashes.ToArray()));
            }
        }

        private void OnGetDataMessageReceived(InvPayload payload)
        {
            foreach (UInt256 hash in payload.Hashes.Distinct())
            {
                IInventory inventory;
                if (!localNode.RelayCache.TryGet(hash, out inventory) && !localNode.ServiceEnabled)
                    continue;
                switch (payload.Type)
                {
                    case InventoryType.TX:
                        if (inventory == null)
                            inventory = LocalNode.GetTransaction(hash);
                        if (inventory == null && Blockchain.Default != null)
                            inventory = Blockchain.Default.GetTransaction(hash);
                        if (inventory is Transaction)
                            EnqueueMessage("tx", inventory);
                        break;
                    case InventoryType.Block:
                        if (inventory == null && Blockchain.Default != null)
                            inventory = Blockchain.Default.GetBlock(hash);
                        if (inventory is Block block)
                        {
                            BloomFilter filter = bloom_filter;
                            if (filter == null)
                            {
                                EnqueueMessage("block", inventory);
                            }
                            else
                            {
                                BitArray flags = new BitArray(block.Transactions.Select(p => TestFilter(filter, p)).ToArray());
                                EnqueueMessage("merkleblock", MerkleBlockPayload.Create(block, flags));
                            }
                        }
                        break;
                    case InventoryType.Consensus:
                        if (inventory != null)
                            EnqueueMessage("consensus", inventory);
                        break;
                }
            }
        }

        private void OnGetHeadersMessageReceived(GetBlocksPayload payload)
        {
            if (!localNode.ServiceEnabled) return;
            if (Blockchain.Default == null) return;
            UInt256 hash = payload.HashStart.Select(p => Blockchain.Default.GetHeader(p)).Where(p => p != null).OrderBy(p => p.Index).Select(p => p.Hash).FirstOrDefault();
            if (hash == null || hash == payload.HashStop) return;
            List<Header> headers = new List<Header>();
            do
            {
                hash = Blockchain.Default.GetNextBlockHash(hash);
                if (hash == null) break;
                headers.Add(Blockchain.Default.GetHeader(hash));
            } while (hash != payload.HashStop && headers.Count < 2000);
            EnqueueMessage("headers", HeadersPayload.Create(headers));
        }

        private void OnHeadersMessageReceived(HeadersPayload payload)
        {
            if (Blockchain.Default == null) return;
            Blockchain.Default.AddHeaders(payload.Headers);
            if (Blockchain.Default.HeaderHeight < Version.StartHeight)
            {
                EnqueueMessage("getheaders", GetBlocksPayload.Create(Blockchain.Default.CurrentHeaderHash));
            }
        }

        private void OnInventoryReceived(IInventory inventory)
        {
            lock (missions_global)
            {
                lock (missions)
                {
                    missions_global.Remove(inventory.Hash);
                    missions.Remove(inventory.Hash);
                    if (missions.Count == 0)
                        mission_start = DateTime.Now.AddYears(100);
                    else
                        mission_start = DateTime.Now;
                }
            }
            if (inventory is MinerTransaction) return;
            InventoryReceived?.Invoke(this, inventory);
        }

        private void OnInvMessageReceived(InvPayload payload)
        {
            if (payload.Type != InventoryType.TX && payload.Type != InventoryType.Block && payload.Type != InventoryType.Consensus)
                return;
            HashSet<UInt256> hashes = new HashSet<UInt256>(payload.Hashes);
            lock (LocalNode.KnownHashes)
            {
                hashes.RemoveWhere(p => LocalNode.KnownHashes.TryGetValue(p, out DateTime time) && time + LocalNode.HashesExpiration >= DateTime.UtcNow);
            }
            if (hashes.Count == 0) return;
            lock (missions_global)
            {
                lock (missions)
                {
                    if (localNode.GlobalMissionsEnabled)
                        hashes.RemoveWhere(p => missions_global.TryGetValue(p, out DateTime time) && time + MissionExpiration >= DateTime.UtcNow);
                    if (hashes.Count > 0)
                    {
                        if (missions.Count == 0) mission_start = DateTime.Now;
                        foreach (UInt256 hash in hashes)
                            if (!missions_global.ContainsKey(hash))
                                missions_global.Add(hash, DateTime.UtcNow);
                        missions.UnionWith(hashes);
                    }
                }
            }
            if (hashes.Count == 0) return;
            EnqueueMessage("getdata", InvPayload.Create(payload.Type, hashes.ToArray()));
        }

        private void OnMemPoolMessageReceived()
        {
            EnqueueMessage("inv", InvPayload.Create(InventoryType.TX, LocalNode.GetMemoryPool().Select(p => p.Hash).ToArray()));
        }

        private void OnMessageReceived(Message message)
        {
            switch (message.Command)
            {
                case "addr":
                    OnAddrMessageReceived(message.Payload.AsSerializable<AddrPayload>());
                    break;
                case "block":
                    OnInventoryReceived(message.Payload.AsSerializable<Block>());
                    break;
                case "consensus":
                    OnInventoryReceived(message.Payload.AsSerializable<ConsensusPayload>());
                    break;
                case "filteradd":
                    OnFilterAddMessageReceived(message.Payload.AsSerializable<FilterAddPayload>());
                    break;
                case "filterclear":
                    OnFilterClearMessageReceived();
                    break;
                case "filterload":
                    OnFilterLoadMessageReceived(message.Payload.AsSerializable<FilterLoadPayload>());
                    break;
                case "getaddr":
                    OnGetAddrMessageReceived();
                    break;
                case "getblocks":
                    OnGetBlocksMessageReceived(message.Payload.AsSerializable<GetBlocksPayload>());
                    break;
                case "getdata":
                    OnGetDataMessageReceived(message.Payload.AsSerializable<InvPayload>());
                    break;
                case "getheaders":
                    OnGetHeadersMessageReceived(message.Payload.AsSerializable<GetBlocksPayload>());
                    break;
                case "headers":
                    OnHeadersMessageReceived(message.Payload.AsSerializable<HeadersPayload>());
                    break;
                case "inv":
                    OnInvMessageReceived(message.Payload.AsSerializable<InvPayload>());
                    break;
                case "mempool":
                    OnMemPoolMessageReceived();
                    break;
                case "tx":
                    if (message.Payload.Length <= 1024 * 1024)
                        OnInventoryReceived(Transaction.DeserializeFrom(message.Payload));
                    break;
                case "verack":
                case "version":
                    Disconnect(true);
                    break;
                case "alert":
                case "merkleblock":
                case "notfound":
                case "ping":
                case "pong":
                case "reject":
                default:
                    //暂时忽略
                    break;
            }
        }

        protected abstract Task<Message> ReceiveMessageAsync(TimeSpan timeout);

        internal bool Relay(IInventory data)
        {
            if (Version?.Relay != true) return false;
            if (data.InventoryType == InventoryType.TX)
            {
                BloomFilter filter = bloom_filter;
                if (filter != null && !TestFilter(filter, (Transaction)data))
                    return false;
            }
            EnqueueMessage("inv", InvPayload.Create(data.InventoryType, data.Hash));
            return true;
        }

        internal void Relay(IEnumerable<Transaction> transactions)
        {
            if (Version?.Relay != true) return;
            BloomFilter filter = bloom_filter;
            if (filter != null)
                transactions = transactions.Where(p => TestFilter(filter, p));
            UInt256[] hashes = transactions.Select(p => p.Hash).ToArray();
            if (hashes.Length == 0) return;
            EnqueueMessage("inv", InvPayload.Create(InventoryType.TX, hashes));
        }

        internal void RequestMemoryPool()
        {
            EnqueueMessage("mempool", null);
        }

        internal void RequestPeers()
        {
            EnqueueMessage("getaddr", null);
        }

        protected abstract Task<bool> SendMessageAsync(Message message);

        internal async void StartProtocol()
        {
#if !NET47
            //There is a bug in .NET Core 2.0 that blocks async method which returns void.
            await Task.Yield();
#endif
            if (!await SendMessageAsync(Message.Create("version", VersionPayload.Create(localNode.Port, localNode.Nonce, localNode.UserAgent))))
                return;
            Message message = await ReceiveMessageAsync(HalfMinute);
            if (message == null) return;
            if (message.Command != "version")
            {
                Disconnect(true);
                return;
            }
            try
            {
                Version = message.Payload.AsSerializable<VersionPayload>();
            }
            catch (EndOfStreamException)
            {
                Disconnect(false);
                return;
            }
            catch (FormatException)
            {
                Disconnect(true);
                return;
            }
            if (Version.Nonce == localNode.Nonce)
            {
                Disconnect(true);
                return;
            }
            bool isSelf;
            lock (localNode.connectedPeers)
            {
                isSelf = localNode.connectedPeers.Where(p => p != this).Any(p => p.RemoteEndpoint.Address.Equals(RemoteEndpoint.Address) && p.Version?.Nonce == Version.Nonce);
            }
            if (isSelf)
            {
                Disconnect(false);
                return;
            }
            if (ListenerEndpoint == null && Version.Port > 0)
            {
                ListenerEndpoint = new IPEndPoint(RemoteEndpoint.Address, Version.Port);
            }
            if (!await SendMessageAsync(Message.Create("verack"))) return;
            message = await ReceiveMessageAsync(HalfMinute);
            if (message == null) return;
            if (message.Command != "verack")
            {
                Disconnect(true);
                return;
            }
            if (Blockchain.Default?.HeaderHeight < Version.StartHeight)
            {
                EnqueueMessage("getheaders", GetBlocksPayload.Create(Blockchain.Default.CurrentHeaderHash));
            }
            StartSendLoop();
            while (disposed == 0)
            {
                if (Blockchain.Default != null)
                {
                    if (missions.Count == 0 && Blockchain.Default.Height < Version.StartHeight)
                    {
                        EnqueueMessage("getblocks", GetBlocksPayload.Create(Blockchain.Default.CurrentBlockHash));
                    }
                }
                TimeSpan timeout = missions.Count == 0 ? HalfHour : OneMinute;
                message = await ReceiveMessageAsync(timeout);
                if (message == null) break;
                if (DateTime.Now - mission_start > OneMinute
                    && message.Command != "block" && message.Command != "consensus" && message.Command != "tx")
                {
                    Disconnect(false);
                    break;
                }
                try
                {
                    OnMessageReceived(message);
                }
                catch (EndOfStreamException)
                {
                    Disconnect(false);
                    break;
                }
                catch (FormatException)
                {
                    Disconnect(true);
                    break;
                }
            }
        }

        private async void StartSendLoop()
        {
#if !NET47
            //There is a bug in .NET Core 2.0 that blocks async method which returns void.
            await Task.Yield();
#endif
            while (disposed == 0)
            {
                Message message = null;
                lock (message_queue_high)
                {
                    if (message_queue_high.Count > 0)
                    {
                        message = message_queue_high.Dequeue();
                    }
                }
                if (message == null)
                {
                    lock (message_queue_low)
                    {
                        if (message_queue_low.Count > 0)
                        {
                            message = message_queue_low.Dequeue();
                        }
                    }
                }
                if (message == null)
                {
                    for (int i = 0; i < 10 && disposed == 0; i++)
                    {
                        Thread.Sleep(100);
                    }
                }
                else
                {
                    await SendMessageAsync(message);
                }
            }
        }

        private bool TestFilter(BloomFilter filter, Transaction tx)
        {
            if (filter.Check(tx.Hash.ToArray())) return true;
            if (tx.Outputs.Any(p => filter.Check(p.ScriptHash.ToArray()))) return true;
            if (tx.Inputs.Any(p => filter.Check(p.ToArray()))) return true;
            if (tx.Scripts.Any(p => filter.Check(p.ScriptHash.ToArray())))
                return true;
            if (tx.Type == TransactionType.RegisterTransaction)
            {
#pragma warning disable CS0612
                RegisterTransaction asset = (RegisterTransaction)tx;
                if (filter.Check(asset.Admin.ToArray())) return true;
#pragma warning restore CS0612
            }
            return false;
        }
    }
}
