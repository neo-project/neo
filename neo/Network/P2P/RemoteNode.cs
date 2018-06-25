using Akka.Actor;
using Akka.IO;
using Neo.Cryptography;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Neo.Network.P2P
{
    public class RemoteNode : Connection
    {
        internal class Send { public Message Message; }
        internal class Relay { public IInventory Inventory; }
        internal class InventoryReceived { public IInventory Inventory; }

        private ByteString msg_buffer = ByteString.Empty;
        private BloomFilter bloom_filter;
        private bool verack = false;

        public IPEndPoint Listener => new IPEndPoint(Remote.Address, ListenerPort);
        public override int ListenerPort => Version?.Port ?? 0;
        public VersionPayload Version { get; private set; }

        public RemoteNode(IActorRef tcp, IPEndPoint remote, IPEndPoint local)
            : base(tcp, remote, local)
        {
            LocalNode.Singleton.RemoteNodes.TryAdd(Self, this);
            SendMessageInternal(Message.Create("version", VersionPayload.Create(LocalNode.Singleton.ListenerPort, LocalNode.Nonce, LocalNode.UserAgent, Blockchain.Singleton.Snapshot.Height)));
        }

        internal static Props Props(IActorRef tcp, IPEndPoint remote, IPEndPoint local)
        {
            return Akka.Actor.Props.Create(() => new RemoteNode(tcp, remote, local));
        }

        protected override void OnData(ByteString data)
        {
            msg_buffer = msg_buffer.Concat(data);
            if (msg_buffer.Count < sizeof(uint)) return;
            uint magic = msg_buffer.Slice(0, sizeof(uint)).ToArray().ToUInt32(0);
            if (magic != Message.Magic)
                throw new FormatException();
            if (msg_buffer.Count < Message.HeaderSize) return;
            int length = msg_buffer.Slice(16, sizeof(int)).ToArray().ToInt32(0);
            if (length > Message.PayloadMaxSize)
                throw new FormatException();
            length += Message.HeaderSize;
            if (msg_buffer.Count < length) return;
            Message message = msg_buffer.Slice(0, length).ToArray().AsSerializable<Message>();
            OnMessage(message);
            msg_buffer = msg_buffer.Slice(length).Compact();
        }

        private void OnMessage(Message message)
        {
            if (Version == null)
            {
                if (message.Command != "version")
                    throw new ProtocolViolationException();
                OnVersionMessageReceived(message.Payload.AsSerializable<VersionPayload>());
                return;
            }
            if (!verack)
            {
                if (message.Command != "verack")
                    throw new ProtocolViolationException();
                OnVerackMessageReceived();
                return;
            }
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
                    bloom_filter = null;
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
                    throw new ProtocolViolationException();
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

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case Send send:
                    SendMessage(send.Message);
                    break;
                case Relay relay:
                    OnRelay(relay.Inventory);
                    break;
                default:
                    base.OnReceive(message);
                    break;
            }
        }

        private void OnRelay(IInventory inventory)
        {
            if (Version?.Relay != true) return;
            if (inventory.InventoryType == InventoryType.TX)
            {
                if (bloom_filter != null && !TestFilter(bloom_filter, (Transaction)inventory))
                    return;
            }
            SendMessage("inv", InvPayload.Create(inventory.InventoryType, inventory.Hash));
        }

        private void OnAddrMessageReceived(AddrPayload payload)
        {
            Context.Parent.Tell(new Peer.Peers
            {
                EndPoints = payload.AddressList.Select(p => p.EndPoint)
            });
        }

        private void OnFilterAddMessageReceived(FilterAddPayload payload)
        {
            if (bloom_filter != null)
                bloom_filter.Add(payload.Data);
        }

        private void OnFilterLoadMessageReceived(FilterLoadPayload payload)
        {
            bloom_filter = new BloomFilter(payload.Filter.Length * 8, payload.K, payload.Tweak, payload.Filter);
        }

        private void OnGetAddrMessageReceived()
        {
            Random rand = new Random();
            IEnumerable<RemoteNode> peers = LocalNode.Singleton.RemoteNodes.Values
                .Where(p => p.ListenerPort > 0)
                .GroupBy(p => p.Remote.Address, (k, g) => g.First())
                .OrderBy(p => rand.Next())
                .Take(AddrPayload.MaxCountToSend);
            NetworkAddressWithTime[] networkAddresses = peers.Select(p => NetworkAddressWithTime.Create(p.Listener, p.Version.Services, p.Version.Timestamp)).ToArray();
            if (networkAddresses.Length == 0) return;
            SendMessage("addr", AddrPayload.Create(networkAddresses));
        }

        private void OnGetBlocksMessageReceived(GetBlocksPayload payload)
        {
            UInt256 hash = payload.HashStart[0];
            if (hash == payload.HashStop) return;
            BlockState state = Blockchain.Singleton.Snapshot.Blocks.TryGet(hash);
            if (state == null) return;
            List<UInt256> hashes = new List<UInt256>();
            for (uint i = 1; i <= InvPayload.MaxHashesCount; i++)
            {
                uint index = state.TrimmedBlock.Index + i;
                if (index > Blockchain.Singleton.Snapshot.Height)
                    break;
                hash = Blockchain.Singleton.GetBlockHash(index);
                if (hash == null) break;
                hashes.Add(hash);
            }
            if (hashes.Count == 0) return;
            SendMessage("inv", InvPayload.Create(InventoryType.Block, hashes.ToArray()));
        }

        private void OnGetDataMessageReceived(InvPayload payload)
        {
            foreach (UInt256 hash in payload.Hashes.Distinct())
            {
                LocalNode.Singleton.RelayCache.TryGet(hash, out IInventory inventory);
                switch (payload.Type)
                {
                    case InventoryType.TX:
                        if (inventory == null)
                            inventory = Blockchain.Singleton.GetTransaction(hash);
                        if (inventory != null)
                            SendMessage("tx", inventory);
                        break;
                    case InventoryType.Block:
                        if (inventory == null)
                            inventory = Blockchain.Singleton.GetBlock(hash);
                        if (inventory != null)
                        {
                            if (bloom_filter == null)
                            {
                                SendMessage("block", inventory);
                            }
                            else
                            {
                                Block block = (Block)inventory;
                                BitArray flags = new BitArray(block.Transactions.Select(p => TestFilter(bloom_filter, p)).ToArray());
                                SendMessage("merkleblock", MerkleBlockPayload.Create(block, flags));
                            }
                        }
                        break;
                    case InventoryType.Consensus:
                        if (inventory != null)
                            SendMessage("consensus", inventory);
                        break;
                }
            }
        }

        private void OnGetHeadersMessageReceived(GetBlocksPayload payload)
        {
            UInt256 hash = payload.HashStart[0];
            if (hash == payload.HashStop) return;
            BlockState state = Blockchain.Singleton.Snapshot.Blocks.TryGet(hash);
            if (state == null) return;
            List<Header> headers = new List<Header>();
            for (uint i = 1; i <= HeadersPayload.MaxHeadersCount; i++)
            {
                uint index = state.TrimmedBlock.Index + i;
                hash = Blockchain.Singleton.GetBlockHash(index);
                if (hash == null) break;
                Header header = Blockchain.Singleton.Snapshot.GetHeader(hash);
                if (header == null) break;
                headers.Add(header);
            }
            if (headers.Count == 0) return;
            SendMessage("headers", HeadersPayload.Create(headers));
        }

        private void OnHeadersMessageReceived(HeadersPayload payload)
        {
            if (payload.Headers.Length == 0) return;
            Version.StartHeight = Math.Max(Version.StartHeight, payload.Headers[payload.Headers.Length - 1].Index);
            LocalNode.Singleton.Blockchain.Tell(new Blockchain.NewHeaders
            {
                Headers = payload.Headers
            });
        }

        private void OnInventoryReceived(IInventory inventory)
        {
            LocalNode.Singleton.TaskManager.Tell(new TaskManager.TaskCompleted { Hash = inventory.Hash });
            switch (inventory)
            {
                case MinerTransaction _:
                    return;
                case Block block:
                    Version.StartHeight = Math.Max(Version.StartHeight, block.Index);
                    break;
            }
            Context.Parent.Tell(new InventoryReceived { Inventory = inventory });
        }

        private void OnInvMessageReceived(InvPayload payload)
        {
            if (payload.Type != InventoryType.TX && payload.Type != InventoryType.Block && payload.Type != InventoryType.Consensus)
                return;
            LocalNode.Singleton.TaskManager.Tell(new TaskManager.NewTasks { Payload = payload });
        }

        private void OnMemPoolMessageReceived()
        {
            foreach (InvPayload payload in InvPayload.CreateGroup(InventoryType.TX, Blockchain.Singleton.GetMemoryPool().Select(p => p.Hash).ToArray()))
                SendMessage("inv", payload);
        }

        private void OnVerackMessageReceived()
        {
            verack = true;
            LocalNode.Singleton.TaskManager.Tell(new TaskManager.Register { Version = Version });
        }

        private void OnVersionMessageReceived(VersionPayload payload)
        {
            Version = payload;
            if (payload.Nonce == LocalNode.Nonce)
                throw new ProtocolViolationException();
            if (LocalNode.Singleton.RemoteNodes.Values.Where(p => p != this).Any(p => p.Remote.Address.Equals(Remote.Address) && p.Version?.Nonce == payload.Nonce))
                throw new ProtocolViolationException();
            SendMessageInternal(Message.Create("verack"));
        }

        protected override void PostStop()
        {
            LocalNode.Singleton.RemoteNodes.TryRemove(Self, out _);
            base.PostStop();
        }

        private void SendMessage(string command, ISerializable payload = null)
        {
            SendMessage(Message.Create(command, payload));
        }

        private void SendMessage(Message message)
        {
            if (Version == null || !verack) return;
            SendMessageInternal(message);
        }

        private void SendMessageInternal(Message message)
        {
            SendData(ByteString.FromBytes(message.ToArray()));
        }

        private static bool TestFilter(BloomFilter filter, Transaction tx)
        {
            if (filter.Check(tx.Hash.ToArray())) return true;
            if (tx.Outputs.Any(p => filter.Check(p.ScriptHash.ToArray()))) return true;
            if (tx.Inputs.Any(p => filter.Check(p.ToArray()))) return true;
            if (tx.Witnesses.Any(p => filter.Check(p.ScriptHash.ToArray())))
                return true;
#pragma warning disable CS0612
            if (tx is RegisterTransaction asset)
                if (filter.Check(asset.Admin.ToArray())) return true;
#pragma warning restore CS0612
            return false;
        }
    }
}
