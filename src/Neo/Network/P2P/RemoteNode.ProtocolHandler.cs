// Copyright (C) 2015-2026 The Neo Project.
//
// RemoteNode.ProtocolHandler.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Neo.Cryptography;
using Neo.Factories;
using Neo.IO.Caching;
using Neo.Ledger;
using Neo.Network.P2P.Capabilities;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Native;
using System.Collections;
using System.Net;

namespace Neo.Network.P2P;

public delegate bool MessageReceivedHandler(NeoSystem system, Message message);

partial class RemoteNode
{
    private class Timer { }
    private class PendingKnownHashesCollection : KeyedCollectionSlim<UInt256, Tuple<UInt256, DateTime>>
    {
        protected override UInt256 GetKeyForItem(Tuple<UInt256, DateTime> item) => item.Item1;
    }

    public static event MessageReceivedHandler MessageReceived
    {
        add => s_handlers.Add(value);
        remove => s_handlers.Remove(value);
    }

    private static readonly List<MessageReceivedHandler> s_handlers = new();
    private readonly PendingKnownHashesCollection _pendingKnownHashes = new();
    private readonly HashSetCache<UInt256> _knownHashes;
    private readonly HashSetCache<UInt256> _sentHashes;
    private bool _verack = false;
    private BloomFilter? _bloomFilter;

    private static readonly TimeSpan TimerInterval = TimeSpan.FromSeconds(30);
    private readonly ICancelable timer = Context.System.Scheduler
        .ScheduleTellRepeatedlyCancelable(TimerInterval, TimerInterval, Context.Self, new Timer(), ActorRefs.NoSender);

    private void OnMessage(Message msg)
    {
        foreach (MessageReceivedHandler handler in s_handlers)
            if (!handler(_system, msg))
                return;
        if (Version == null)
        {
            if (msg.Command != MessageCommand.Version)
                throw new ProtocolViolationException();
            OnVersionMessageReceived((VersionPayload)msg.Payload!);
            return;
        }
        if (!_verack)
        {
            if (msg.Command != MessageCommand.Verack)
                throw new ProtocolViolationException();
            OnVerackMessageReceived();
            return;
        }
        switch (msg.Command)
        {
            case MessageCommand.Addr:
                OnAddrMessageReceived((AddrPayload)msg.Payload!);
                break;
            case MessageCommand.Block:
            case MessageCommand.Extensible:
                OnInventoryReceived((IInventory)msg.Payload!);
                break;
            case MessageCommand.FilterAdd:
                OnFilterAddMessageReceived((FilterAddPayload)msg.Payload!);
                break;
            case MessageCommand.FilterClear:
                OnFilterClearMessageReceived();
                break;
            case MessageCommand.FilterLoad:
                OnFilterLoadMessageReceived((FilterLoadPayload)msg.Payload!);
                break;
            case MessageCommand.GetAddr:
                OnGetAddrMessageReceived();
                break;
            case MessageCommand.GetBlocks:
                OnGetBlocksMessageReceived((GetBlocksPayload)msg.Payload!);
                break;
            case MessageCommand.GetBlockByIndex:
                OnGetBlockByIndexMessageReceived((GetBlockByIndexPayload)msg.Payload!);
                break;
            case MessageCommand.GetData:
                OnGetDataMessageReceived((InvPayload)msg.Payload!);
                break;
            case MessageCommand.GetHeaders:
                OnGetHeadersMessageReceived((GetBlockByIndexPayload)msg.Payload!);
                break;
            case MessageCommand.Headers:
                OnHeadersMessageReceived((HeadersPayload)msg.Payload!);
                break;
            case MessageCommand.Inv:
                OnInvMessageReceived((InvPayload)msg.Payload!);
                break;
            case MessageCommand.Mempool:
                OnMemPoolMessageReceived();
                break;
            case MessageCommand.Ping:
                OnPingMessageReceived((PingPayload)msg.Payload!);
                break;
            case MessageCommand.Pong:
                OnPongMessageReceived((PingPayload)msg.Payload!);
                break;
            case MessageCommand.Transaction:
                if (msg.Payload!.Size <= Transaction.MaxTransactionSize)
                    OnInventoryReceived((Transaction)msg.Payload);
                break;
            case MessageCommand.Verack:
            case MessageCommand.Version:
                throw new ProtocolViolationException();
            case MessageCommand.Alert:
            case MessageCommand.MerkleBlock:
            case MessageCommand.NotFound:
            case MessageCommand.Reject:
            default: break;
        }
    }

    private void OnAddrMessageReceived(AddrPayload payload)
    {
        ref bool sent = ref _sentCommands[(byte)MessageCommand.GetAddr];
        if (!sent) return;
        sent = false;
        var endPoints = payload.AddressList
            .Select(p => p.EndPoint)
            .Where(p => p.Port > 0)
            .ToArray();
        _system.LocalNode.Tell(new Peer.Peers(endPoints));
    }

    private void OnFilterAddMessageReceived(FilterAddPayload payload)
    {
        _bloomFilter?.Add(payload.Data);
    }

    private void OnFilterClearMessageReceived()
    {
        _bloomFilter = null;
    }

    private void OnFilterLoadMessageReceived(FilterLoadPayload payload)
    {
        _bloomFilter = new BloomFilter(payload.Filter.Length * 8, payload.K, payload.Tweak, payload.Filter);
    }

    /// <summary>
    /// Will be triggered when a MessageCommand.GetAddr message is received.
    /// Randomly select nodes from the local RemoteNodes and tells to RemoteNode actors a MessageCommand.Addr message.
    /// The message contains a list of networkAddresses from those selected random peers.
    /// </summary>
    private void OnGetAddrMessageReceived()
    {
        IEnumerable<RemoteNode> peers = _localNode.RemoteNodes.Values
            .Where(p => p.ListenerTcpPort > 0)
            .GroupBy(p => p.Remote.Address, (k, g) => g.First())
            .OrderBy(p => RandomNumberFactory.NextInt32())
            .Take(AddrPayload.MaxCountToSend);
        NetworkAddressWithTime[] networkAddresses = peers.Select(p => NetworkAddressWithTime.Create(p.Listener.Address, p.Version!.Timestamp, p.Version.Capabilities)).ToArray();
        if (networkAddresses.Length == 0) return;
        EnqueueMessage(Message.Create(MessageCommand.Addr, AddrPayload.Create(networkAddresses)));
    }

    /// <summary>
    /// Will be triggered when a MessageCommand.GetBlocks message is received.
    /// Tell the specified number of blocks' hashes starting with the requested HashStart until payload.Count or MaxHashesCount
    /// Responses are sent to RemoteNode actor as MessageCommand.Inv Message.
    /// </summary>
    /// <param name="payload">A GetBlocksPayload including start block Hash and number of blocks requested.</param>
    private void OnGetBlocksMessageReceived(GetBlocksPayload payload)
    {
        // The default value of payload.Count is -1
        int count = payload.Count < 0 || payload.Count > InvPayload.MaxHashesCount ? InvPayload.MaxHashesCount : payload.Count;
        var snapshot = _system.StoreView;
        UInt256? hash = payload.HashStart;
        TrimmedBlock? state = NativeContract.Ledger.GetTrimmedBlock(snapshot, hash);
        if (state == null) return;
        uint currentHeight = NativeContract.Ledger.CurrentIndex(snapshot);
        List<UInt256> hashes = new();
        for (uint i = 1; i <= count; i++)
        {
            uint index = state.Index + i;
            if (index > currentHeight)
                break;
            hash = NativeContract.Ledger.GetBlockHash(snapshot, index);
            if (hash == null) break;
            hashes.Add(hash);
        }
        if (hashes.Count == 0) return;
        EnqueueMessage(Message.Create(MessageCommand.Inv, InvPayload.Create(InventoryType.Block, hashes.ToArray())));
    }

    private void OnGetBlockByIndexMessageReceived(GetBlockByIndexPayload payload)
    {
        uint count = payload.Count == -1 ? InvPayload.MaxHashesCount : Math.Min((uint)payload.Count, InvPayload.MaxHashesCount);
        for (uint i = payload.IndexStart, max = payload.IndexStart + count; i < max; i++)
        {
            Block? block = NativeContract.Ledger.GetBlock(_system.StoreView, i);
            if (block == null)
                break;

            if (_bloomFilter == null)
            {
                EnqueueMessage(Message.Create(MessageCommand.Block, block));
            }
            else
            {
                BitArray flags = new(block.Transactions.Select(p => _bloomFilter.Test(p)).ToArray());
                EnqueueMessage(Message.Create(MessageCommand.MerkleBlock, MerkleBlockPayload.Create(block, flags)));
            }
        }
    }

    /// <summary>
    /// Will be triggered when a MessageCommand.GetData message is received.
    /// The payload includes an array of hash values.
    /// For different payload.Type (Tx, Block, Consensus),
    /// get the corresponding (Txs, Blocks, Consensus) and tell them to RemoteNode actor.
    /// </summary>
    /// <param name="payload">The payload containing the requested information.</param>
    private void OnGetDataMessageReceived(InvPayload payload)
    {
        var notFound = new List<UInt256>();
        foreach (var hash in payload.Hashes.Where(_sentHashes.TryAdd))
        {
            switch (payload.Type)
            {
                case InventoryType.TX:
                    if (_system.MemPool.TryGetValue(hash, out Transaction? tx))
                        EnqueueMessage(Message.Create(MessageCommand.Transaction, tx));
                    else
                        notFound.Add(hash);
                    break;
                case InventoryType.Block:
                    Block? block = NativeContract.Ledger.GetBlock(_system.StoreView, hash);
                    if (block != null)
                    {
                        if (_bloomFilter == null)
                        {
                            EnqueueMessage(Message.Create(MessageCommand.Block, block));
                        }
                        else
                        {
                            BitArray flags = new(block.Transactions.Select(p => _bloomFilter.Test(p)).ToArray());
                            EnqueueMessage(Message.Create(MessageCommand.MerkleBlock, MerkleBlockPayload.Create(block, flags)));
                        }
                    }
                    else
                    {
                        notFound.Add(hash);
                    }
                    break;
                default:
                    if (_system.RelayCache.TryGet(hash, out IInventory? inventory))
                        EnqueueMessage(Message.Create((MessageCommand)payload.Type, inventory));
                    break;
            }
        }

        if (notFound.Count > 0)
        {
            foreach (InvPayload entry in InvPayload.CreateGroup(payload.Type, notFound))
                EnqueueMessage(Message.Create(MessageCommand.NotFound, entry));
        }
    }

    /// <summary>
    /// Will be triggered when a MessageCommand.GetHeaders message is received.
    /// Tell the specified number of blocks' headers starting with the requested IndexStart to RemoteNode actor.
    /// A limit set by HeadersPayload.MaxHeadersCount is also applied to the number of requested Headers, namely payload.Count.
    /// </summary>
    /// <param name="payload">A GetBlockByIndexPayload including start block index and number of blocks' headers requested.</param>
    private void OnGetHeadersMessageReceived(GetBlockByIndexPayload payload)
    {
        var snapshot = _system.StoreView;
        if (payload.IndexStart > NativeContract.Ledger.CurrentIndex(snapshot)) return;
        var headers = new List<Header>();
        uint count = payload.Count == -1 ? HeadersPayload.MaxHeadersCount : (uint)payload.Count;
        for (uint i = 0; i < count; i++)
        {
            uint index = payload.IndexStart + i;
            Header? header = _system.HeaderCache[index];
            if (header == null)
            {
                header = NativeContract.Ledger.GetHeader(snapshot, index);
                if (header == null) break;
            }
            headers.Add(header);
        }
        if (headers.Count == 0) return;
        EnqueueMessage(Message.Create(MessageCommand.Headers, HeadersPayload.Create(headers.ToArray())));
    }

    private void OnHeadersMessageReceived(HeadersPayload payload)
    {
        UpdateLastBlockIndex(payload.Headers[^1].Index);
        _system.Blockchain.Tell(payload.Headers);
    }

    private void OnInventoryReceived(IInventory inventory)
    {
        if (!_knownHashes.TryAdd(inventory.Hash)) return;
        _pendingKnownHashes.Remove(inventory.Hash);
        _system.TaskManager.Tell(inventory);
        switch (inventory)
        {
            case Transaction transaction:
                if (!(_system.ContainsTransaction(transaction.Hash) != ContainsTransactionType.NotExist || _system.ContainsConflictHash(transaction.Hash, transaction.Signers.Select(s => s.Account))))
                    _system.TxRouter.Tell(new TransactionRouter.Preverify(transaction, true));
                break;
            case Block block:
                UpdateLastBlockIndex(block.Index);
                if (block.Index > NativeContract.Ledger.CurrentIndex(_system.StoreView) + InvPayload.MaxHashesCount) return;
                _system.Blockchain.Tell(inventory);
                break;
            default:
                _system.Blockchain.Tell(inventory);
                break;
        }
    }

    private void OnInvMessageReceived(InvPayload payload)
    {
        UInt256[] hashes;
        var source = payload.Hashes
            .Where(p => !_pendingKnownHashes.Contains(p) && !_knownHashes.Contains(p) && !_sentHashes.Contains(p));
        switch (payload.Type)
        {
            case InventoryType.Block:
                {
                    var snapshot = _system.StoreView;
                    hashes = source.Where(p => !NativeContract.Ledger.ContainsBlock(snapshot, p)).ToArray();
                    break;
                }
            case InventoryType.TX:
                {
                    var snapshot = _system.StoreView;
                    hashes = source.Where(p => !NativeContract.Ledger.ContainsTransaction(snapshot, p)).ToArray();
                    break;
                }
            default:
                {
                    hashes = source.ToArray();
                    break;
                }
        }
        if (hashes.Length == 0) return;
        foreach (var hash in hashes)
            _pendingKnownHashes.TryAdd(Tuple.Create(hash, TimeProvider.Current.UtcNow));
        _system.TaskManager.Tell(new TaskManager.NewTasks(InvPayload.Create(payload.Type, hashes)));
    }

    private void OnMemPoolMessageReceived()
    {
        foreach (InvPayload payload in InvPayload.CreateGroup(InventoryType.TX, _system.MemPool.GetVerifiedTransactions().Select(p => p.Hash).ToArray()))
            EnqueueMessage(Message.Create(MessageCommand.Inv, payload));
    }

    private void OnPingMessageReceived(PingPayload payload)
    {
        UpdateLastBlockIndex(payload.LastBlockIndex);

        // Refresh routing table liveness on inbound Ping.
        _localNode.RoutingTable.MarkSuccess(Version!.NodeId);

        EnqueueMessage(Message.Create(MessageCommand.Pong, PingPayload.Create(NativeContract.Ledger.CurrentIndex(_system.StoreView), payload.Nonce)));
    }

    private void OnPongMessageReceived(PingPayload payload)
    {
        UpdateLastBlockIndex(payload.LastBlockIndex);

        // DHT: Pong means our probe succeeded, strongly refresh liveness.
        _localNode.RoutingTable.MarkSuccess(Version!.NodeId);
    }

    private void OnVerackMessageReceived()
    {
        _verack = true;
        _system.TaskManager.Tell(new TaskManager.Register(Version!));

        // DHT: a verack means the handshake is complete and the remote identity (NodeId) has been verified.
        // Feed the remote contact into the local RoutingTable.
        var nodeId = Version!.NodeId;

        // Record both:
        //  - Observed endpoint: what we actually connected to (may be NAT-mapped; not necessarily dialable)
        //  - Advertised endpoint: what the peer claims to be listening on (dialable candidate)
        _localNode.RoutingTable.Update(nodeId, new OverlayEndpoint(TransportProtocol.Tcp, Remote, EndpointKind.Observed));
        if (ListenerTcpPort > 0)
            _localNode.RoutingTable.Update(nodeId, new OverlayEndpoint(TransportProtocol.Tcp, Listener, EndpointKind.Advertised));

        _localNode.RoutingTable.MarkSuccess(nodeId);

        CheckMessageQueue();
    }

    private void OnVersionMessageReceived(VersionPayload payload)
    {
        if (!payload.Verify(_system.Settings)) throw new ProtocolViolationException();
        Version = payload;
        foreach (NodeCapability capability in payload.Capabilities)
        {
            switch (capability)
            {
                case FullNodeCapability fullNodeCapability:
                    IsFullNode = true;
                    LastBlockIndex = fullNodeCapability.StartHeight;
                    break;
                case ServerCapability serverCapability:
                    if (serverCapability.Type == NodeCapabilityType.TcpServer)
                        ListenerTcpPort = serverCapability.Port;
                    break;
            }
        }
        if (!_localNode.AllowNewConnection(Self, this))
        {
            Disconnect(true);
            return;
        }
        SendMessage(Message.Create(MessageCommand.Verack));
    }

    private void OnTimer()
    {
        var oneMinuteAgo = TimeProvider.Current.UtcNow.AddMinutes(-1);
        while (_pendingKnownHashes.Count > 0)
        {
            var (_, time) = _pendingKnownHashes.FirstOrDefault!;
            if (oneMinuteAgo <= time) break;
            if (!_pendingKnownHashes.RemoveFirst()) break;
        }
        if (oneMinuteAgo > _lastSent)
            EnqueueMessage(Message.Create(MessageCommand.Ping, PingPayload.Create(NativeContract.Ledger.CurrentIndex(_system.StoreView))));
    }

    private void UpdateLastBlockIndex(uint lastBlockIndex)
    {
        if (lastBlockIndex > LastBlockIndex)
        {
            LastBlockIndex = lastBlockIndex;
            _system.TaskManager.Tell(new TaskManager.Update(LastBlockIndex));
        }
    }
}
