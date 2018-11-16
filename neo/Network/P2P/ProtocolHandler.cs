using Akka.Actor;
using Akka.Configuration;
using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Actors;
using Neo.IO.Caching;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Neo.Network.P2P
{
    internal class ProtocolHandler : UntypedActor
    {
        public class SetVersion { public VersionPayload Version; }
        public class SetVerack { }
        public class SetFilter { public BloomFilter Filter; }

        private readonly NeoSystem system;
        private readonly HashSet<UInt256> knownHashes = new HashSet<UInt256>();
        private readonly HashSet<UInt256> sentHashes = new HashSet<UInt256>();
        private VersionPayload version;
        private bool verack = false;
        private BloomFilter bloom_filter;

        public ProtocolHandler(NeoSystem system)
        {
            this.system = system;
        }

        protected override void OnReceive(object message)
        {
            if (!(message is Message msg)) return;
            if (version == null)
            {
                if (msg.Command != "version")
                    throw new ProtocolViolationException();
                OnVersionMessageReceived(msg.Payload.AsSerializable<VersionPayload>());
                return;
            }
            if (!verack)
            {
                if (msg.Command != "verack")
                    throw new ProtocolViolationException();
                OnVerackMessageReceived();
                return;
            }
            switch (msg.Command)
            {
                case "addr":
                    OnAddrMessageReceived(msg.Payload.AsSerializable<AddrPayload>());
                    break;
                case "block":
                    OnInventoryReceived(msg.Payload.AsSerializable<Block>());
                    break;
                case "consensus":
                    OnInventoryReceived(msg.Payload.AsSerializable<ConsensusPayload>());
                    break;
                case "filteradd":
                    OnFilterAddMessageReceived(msg.Payload.AsSerializable<FilterAddPayload>());
                    break;
                case "filterclear":
                    OnFilterClearMessageReceived();
                    break;
                case "filterload":
                    OnFilterLoadMessageReceived(msg.Payload.AsSerializable<FilterLoadPayload>());
                    break;
                case "getaddr":
                    OnGetAddrMessageReceived();
                    break;
                case "getblocks":
                    OnGetBlocksMessageReceived(msg.Payload.AsSerializable<GetBlocksPayload>());
                    break;
                case "getdata":
                    OnGetDataMessageReceived(msg.Payload.AsSerializable<InvPayload>());
                    break;
                case "getheaders":
                    OnGetHeadersMessageReceived(msg.Payload.AsSerializable<GetBlocksPayload>());
                    break;
                case "headers":
                    OnHeadersMessageReceived(msg.Payload.AsSerializable<HeadersPayload>());
                    break;
                case "inv":
                    OnInvMessageReceived(msg.Payload.AsSerializable<InvPayload>());
                    break;
                case "mempool":
                    OnMemPoolMessageReceived();
                    break;
                case "tx":
                    if (msg.Payload.Length <= Transaction.MaxTransactionSize)
                        OnInventoryReceived(Transaction.DeserializeFrom(msg.Payload));
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

        private void OnAddrMessageReceived(AddrPayload payload)
        {
            system.LocalNode.Tell(new Peer.Peers
            {
                EndPoints = payload.AddressList.Select(p => p.EndPoint)
            });
        }

        private void OnFilterAddMessageReceived(FilterAddPayload payload)
        {
            if (bloom_filter != null)
                bloom_filter.Add(payload.Data);
        }

        private void OnFilterClearMessageReceived()
        {
            bloom_filter = null;
            Context.Parent.Tell(new SetFilter { Filter = null });
        }

        private void OnFilterLoadMessageReceived(FilterLoadPayload payload)
        {
            bloom_filter = new BloomFilter(payload.Filter.Length * 8, payload.K, payload.Tweak, payload.Filter);
            Context.Parent.Tell(new SetFilter { Filter = bloom_filter });
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
            Context.Parent.Tell(Message.Create("addr", AddrPayload.Create(networkAddresses)));
        }

        private void OnGetBlocksMessageReceived(GetBlocksPayload payload)
        {
            UInt256 hash = payload.HashStart[0];
            if (hash == payload.HashStop) return;
            BlockState state = Blockchain.Singleton.Store.GetBlocks().TryGet(hash);
            if (state == null) return;
            List<UInt256> hashes = new List<UInt256>();
            for (uint i = 1; i <= InvPayload.MaxHashesCount; i++)
            {
                uint index = state.TrimmedBlock.Index + i;
                if (index > Blockchain.Singleton.Height)
                    break;
                hash = Blockchain.Singleton.GetBlockHash(index);
                if (hash == null) break;
                if (hash == payload.HashStop) break;
                hashes.Add(hash);
            }
            if (hashes.Count == 0) return;
            Context.Parent.Tell(Message.Create("inv", InvPayload.Create(InventoryType.Block, hashes.ToArray())));
        }

        private void OnGetDataMessageReceived(InvPayload payload)
        {
            UInt256[] hashes = payload.Hashes.Where(p => sentHashes.Add(p)).ToArray();
            foreach (UInt256 hash in hashes)
            {
                Blockchain.Singleton.RelayCache.TryGet(hash, out IInventory inventory);
                switch (payload.Type)
                {
                    case InventoryType.TX:
                        if (inventory == null)
                            inventory = Blockchain.Singleton.GetTransaction(hash);
                        if (inventory is Transaction)
                            Context.Parent.Tell(Message.Create("tx", inventory));
                        break;
                    case InventoryType.Block:
                        if (inventory == null)
                            inventory = Blockchain.Singleton.GetBlock(hash);
                        if (inventory is Block block)
                        {
                            if (bloom_filter == null)
                            {
                                Context.Parent.Tell(Message.Create("block", inventory));
                            }
                            else
                            {
                                BitArray flags = new BitArray(block.Transactions.Select(p => bloom_filter.Test(p)).ToArray());
                                Context.Parent.Tell(Message.Create("merkleblock", MerkleBlockPayload.Create(block, flags)));
                            }
                        }
                        break;
                    case InventoryType.Consensus:
                        if (inventory != null)
                            Context.Parent.Tell(Message.Create("consensus", inventory));
                        break;
                }
            }
        }

        private void OnGetHeadersMessageReceived(GetBlocksPayload payload)
        {
            UInt256 hash = payload.HashStart[0];
            if (hash == payload.HashStop) return;
            DataCache<UInt256, BlockState> cache = Blockchain.Singleton.Store.GetBlocks();
            BlockState state = cache.TryGet(hash);
            if (state == null) return;
            List<Header> headers = new List<Header>();
            for (uint i = 1; i <= HeadersPayload.MaxHeadersCount; i++)
            {
                uint index = state.TrimmedBlock.Index + i;
                hash = Blockchain.Singleton.GetBlockHash(index);
                if (hash == null) break;
                if (hash == payload.HashStop) break;
                Header header = cache.TryGet(hash)?.TrimmedBlock.Header;
                if (header == null) break;
                headers.Add(header);
            }
            if (headers.Count == 0) return;
            Context.Parent.Tell(Message.Create("headers", HeadersPayload.Create(headers)));
        }

        private void OnHeadersMessageReceived(HeadersPayload payload)
        {
            if (payload.Headers.Length == 0) return;
            system.Blockchain.Tell(payload.Headers, Context.Parent);
        }

        private void OnInventoryReceived(IInventory inventory)
        {
            system.TaskManager.Tell(new TaskManager.TaskCompleted { Hash = inventory.Hash }, Context.Parent);
            if (inventory is MinerTransaction) return;
            system.LocalNode.Tell(new LocalNode.Relay { Inventory = inventory });
        }

        private void OnInvMessageReceived(InvPayload payload)
        {
            UInt256[] hashes = payload.Hashes.Where(p => knownHashes.Add(p)).ToArray();
            if (hashes.Length == 0) return;
            switch (payload.Type)
            {
                case InventoryType.Block:
                    using (Snapshot snapshot = Blockchain.Singleton.GetSnapshot())
                        hashes = hashes.Where(p => !snapshot.ContainsBlock(p)).ToArray();
                    break;
                case InventoryType.TX:
                    using (Snapshot snapshot = Blockchain.Singleton.GetSnapshot())
                        hashes = hashes.Where(p => !snapshot.ContainsTransaction(p)).ToArray();
                    break;
            }
            if (hashes.Length == 0) return;
            system.TaskManager.Tell(new TaskManager.NewTasks { Payload = InvPayload.Create(payload.Type, hashes) }, Context.Parent);
        }

        private void OnMemPoolMessageReceived()
        {
            foreach (InvPayload payload in InvPayload.CreateGroup(InventoryType.TX, Blockchain.Singleton.GetMemoryPool().Select(p => p.Hash).ToArray()))
                Context.Parent.Tell(Message.Create("inv", payload));
        }

        private void OnVerackMessageReceived()
        {
            verack = true;
            Context.Parent.Tell(new SetVerack());
        }

        private void OnVersionMessageReceived(VersionPayload payload)
        {
            version = payload;
            Context.Parent.Tell(new SetVersion { Version = payload });
        }

        public static Props Props(NeoSystem system)
        {
            return Akka.Actor.Props.Create(() => new ProtocolHandler(system)).WithMailbox("protocol-handler-mailbox");
        }
    }

    internal class ProtocolHandlerMailbox : PriorityMailbox
    {
        public ProtocolHandlerMailbox(Akka.Actor.Settings settings, Config config)
            : base(settings, config)
        {
        }

        protected override bool IsHighPriority(object message)
        {
            if (!(message is Message msg)) return true;
            switch (msg.Command)
            {
                case "consensus":
                case "filteradd":
                case "filterclear":
                case "filterload":
                case "verack":
                case "version":
                case "alert":
                    return true;
                default:
                    return false;
            }
        }

        protected override bool ShallDrop(object message, IEnumerable queue)
        {
            if (!(message is Message msg)) return false;
            switch (msg.Command)
            {
                case "getaddr":
                case "getblocks":
                case "getdata":
                case "getheaders":
                case "mempool":
                    return queue.OfType<Message>().Any(p => p.Command == msg.Command);
                default:
                    return false;
            }
        }
    }
}
