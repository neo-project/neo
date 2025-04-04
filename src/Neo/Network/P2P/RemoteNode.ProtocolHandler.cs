// Copyright (C) 2015-2025 The Neo Project.
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
using Neo.IO.Caching;
using Neo.Ledger;
using Neo.Network.P2P.Capabilities;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Native;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Neo.Network.P2P
{
    public delegate bool MessageReceivedHandler(NeoSystem system, Message message);

    partial class RemoteNode
    {
        private class Timer { }
        private class PendingKnownHashesCollection : KeyedCollectionSlim<UInt256, Tuple<UInt256, DateTime>>
        {
            protected override UInt256 GetKeyForItem(Tuple<UInt256, DateTime> item)
            {
                return item.Item1;
            }
        }

        public static event MessageReceivedHandler MessageReceived
        {
            add => handlers.Add(value);
            remove => handlers.Remove(value);
        }

        private static readonly List<MessageReceivedHandler> handlers = new();
        private readonly PendingKnownHashesCollection pendingKnownHashes = new();
        private readonly HashSetCache<UInt256> knownHashes;
        private readonly HashSetCache<UInt256> sentHashes;
        private bool verack = false;
        private BloomFilter bloom_filter;

        private static readonly TimeSpan TimerInterval = TimeSpan.FromSeconds(30);
        private readonly ICancelable timer = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(TimerInterval, TimerInterval, Context.Self, new Timer(), ActorRefs.NoSender);

        private void OnMessage(Message msg)
        {
            _log.Verbose("Processing message {Command}", msg.Command);
            foreach (MessageReceivedHandler handler in handlers)
            {
                if (!handler(system, msg))
                {
                    _log.Debug("Message {Command} handling stopped by handler {HandlerName}", msg.Command, handler.Method.Name);
                    return;
                }
            }
            if (Version == null)
            {
                if (msg.Command != MessageCommand.Version)
                {
                    _log.Warning("Protocol violation: Expected Version message, got {Command}", msg.Command);
                    throw new ProtocolViolationException();
                }
                OnVersionMessageReceived((VersionPayload)msg.Payload);
                return;
            }
            if (!verack)
            {
                if (msg.Command != MessageCommand.Verack)
                {
                    _log.Warning("Protocol violation: Expected Verack message, got {Command}", msg.Command);
                    throw new ProtocolViolationException();
                }
                OnVerackMessageReceived();
                return;
            }
            switch (msg.Command)
            {
                case MessageCommand.Addr:
                    OnAddrMessageReceived((AddrPayload)msg.Payload);
                    break;
                case MessageCommand.Block:
                case MessageCommand.Extensible:
                    OnInventoryReceived((IInventory)msg.Payload);
                    break;
                case MessageCommand.FilterAdd:
                    OnFilterAddMessageReceived((FilterAddPayload)msg.Payload);
                    break;
                case MessageCommand.FilterClear:
                    OnFilterClearMessageReceived();
                    break;
                case MessageCommand.FilterLoad:
                    OnFilterLoadMessageReceived((FilterLoadPayload)msg.Payload);
                    break;
                case MessageCommand.GetAddr:
                    OnGetAddrMessageReceived();
                    break;
                case MessageCommand.GetBlocks:
                    OnGetBlocksMessageReceived((GetBlocksPayload)msg.Payload);
                    break;
                case MessageCommand.GetBlockByIndex:
                    OnGetBlockByIndexMessageReceived((GetBlockByIndexPayload)msg.Payload);
                    break;
                case MessageCommand.GetData:
                    OnGetDataMessageReceived((InvPayload)msg.Payload);
                    break;
                case MessageCommand.GetHeaders:
                    OnGetHeadersMessageReceived((GetBlockByIndexPayload)msg.Payload);
                    break;
                case MessageCommand.Headers:
                    OnHeadersMessageReceived((HeadersPayload)msg.Payload);
                    break;
                case MessageCommand.Inv:
                    OnInvMessageReceived((InvPayload)msg.Payload);
                    break;
                case MessageCommand.Mempool:
                    OnMemPoolMessageReceived();
                    break;
                case MessageCommand.Ping:
                    OnPingMessageReceived((PingPayload)msg.Payload);
                    break;
                case MessageCommand.Pong:
                    OnPongMessageReceived((PingPayload)msg.Payload);
                    break;
                case MessageCommand.Transaction:
                    if (msg.Payload.Size <= Transaction.MaxTransactionSize)
                        OnInventoryReceived((Transaction)msg.Payload);
                    else
                        _log.Warning("Transaction {TxHash} exceeds max size ({Size}/{MaxSize}), discarding.", ((Transaction)msg.Payload).Hash, msg.Payload.Size, Transaction.MaxTransactionSize);
                    break;
                case MessageCommand.Verack:
                case MessageCommand.Version:
                    _log.Warning("Protocol violation: Received unexpected {Command} message.", msg.Command);
                    throw new ProtocolViolationException();
                case MessageCommand.Alert:
                case MessageCommand.MerkleBlock:
                case MessageCommand.NotFound:
                case MessageCommand.Reject:
                    _log.Debug("Received unhandled/obsolete message {Command}", msg.Command);
                    break;
                default:
                    _log.Warning("Received unknown message command {CommandCode}", (byte)msg.Command);
                    break;
            }
        }

        private void OnAddrMessageReceived(AddrPayload payload)
        {
            _log.Debug("Received Addr message with {PeerCount} peers", payload.AddressList.Length);
            ref bool sent = ref sentCommands[(byte)MessageCommand.GetAddr];
            if (!sent)
            {
                _log.Verbose("Ignoring Addr message as GetAddr was not sent.");
                return;
            }
            sent = false;
            system.LocalNode.Tell(new Peer.Peers
            {
                EndPoints = payload.AddressList.Select(p => p.EndPoint).Where(p => p.Port > 0)
            });
        }

        private void OnFilterAddMessageReceived(FilterAddPayload payload)
        {
            _log.Debug("Adding data to bloom filter");
            bloom_filter?.Add(payload.Data);
        }

        private void OnFilterClearMessageReceived()
        {
            _log.Debug("Clearing bloom filter");
            bloom_filter = null;
        }

        private void OnFilterLoadMessageReceived(FilterLoadPayload payload)
        {
            _log.Debug("Loading new bloom filter (Size: {Size}, K: {K}, Tweak: {Tweak})", payload.Filter.Length, payload.K, payload.Tweak);
            bloom_filter = new BloomFilter(payload.Filter.Length * 8, payload.K, payload.Tweak, payload.Filter);
        }

        /// <summary>
        /// Will be triggered when a MessageCommand.GetAddr message is received.
        /// Randomly select nodes from the local RemoteNodes and tells to RemoteNode actors a MessageCommand.Addr message.
        /// The message contains a list of networkAddresses from those selected random peers.
        /// </summary>
        private void OnGetAddrMessageReceived()
        {
            _log.Debug("Received GetAddr message, preparing response");
            Random rand = new();
            IEnumerable<RemoteNode> peers = localNode.RemoteNodes.Values
                .Where(p => p.ListenerTcpPort > 0)
                .GroupBy(p => p.Remote.Address, (k, g) => g.First())
                .OrderBy(p => rand.Next())
                .Take(AddrPayload.MaxCountToSend);
            NetworkAddressWithTime[] networkAddresses = peers.Select(p => NetworkAddressWithTime.Create(p.Listener.Address, p.Version.Timestamp, p.Version.Capabilities)).ToArray();
            if (networkAddresses.Length == 0)
            {
                _log.Debug("No suitable peers found to send in Addr response");
                return;
            }
            _log.Debug("Sending Addr response with {PeerCount} peers", networkAddresses.Length);
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
            _log.Debug("Received GetBlocks message: Start={HashStart}, Count={Count}", payload.HashStart, payload.Count);
            // The default value of payload.Count is -1
            int count = payload.Count < 0 || payload.Count > InvPayload.MaxHashesCount ? InvPayload.MaxHashesCount : payload.Count;
            var snapshot = system.StoreView;
            UInt256 hash = payload.HashStart;
            TrimmedBlock state = NativeContract.Ledger.GetTrimmedBlock(snapshot, hash);
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
            if (hashes.Count == 0)
            {
                _log.Debug("No blocks found for GetBlocks request from {HashStart}", payload.HashStart);
                return;
            }
            _log.Debug("Sending Inv ({InvType}) response for GetBlocks with {HashCount} hashes", InventoryType.Block, hashes.Count);
            EnqueueMessage(Message.Create(MessageCommand.Inv, InvPayload.Create(InventoryType.Block, hashes.ToArray())));
        }

        private void OnGetBlockByIndexMessageReceived(GetBlockByIndexPayload payload)
        {
            _log.Debug("Received GetBlockByIndex message: Start={IndexStart}, Count={Count}", payload.IndexStart, payload.Count);
            uint count = payload.Count == -1 ? InvPayload.MaxHashesCount : Math.Min((uint)payload.Count, InvPayload.MaxHashesCount);
            for (uint i = payload.IndexStart, max = payload.IndexStart + count; i < max; i++)
            {
                Block block = NativeContract.Ledger.GetBlock(system.StoreView, i);
                if (block == null)
                    break;

                if (bloom_filter == null)
                {
                    EnqueueMessage(Message.Create(MessageCommand.Block, block));
                }
                else
                {
                    BitArray flags = new(block.Transactions.Select(p => bloom_filter.Test(p)).ToArray());
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
            _log.Debug("Received GetData message: Type={InvType}, Count={HashCount}", payload.Type, payload.Hashes.Length);
            var hashesToProcess = payload.Hashes.Where(p => sentHashes.Add(p)).ToList();
            _log.Verbose("Processing {ProcessCount}/{OriginalCount} hashes for GetData (duplicates/already sent ignored)", hashesToProcess.Count, payload.Hashes.Length);

            var notFound = new List<UInt256>();
            foreach (UInt256 hash in hashesToProcess)
            {
                switch (payload.Type)
                {
                    case InventoryType.TX:
                        if (system.MemPool.TryGetValue(hash, out Transaction tx))
                            EnqueueMessage(Message.Create(MessageCommand.Transaction, tx));
                        else
                            notFound.Add(hash);
                        break;
                    case InventoryType.Block:
                        Block block = NativeContract.Ledger.GetBlock(system.StoreView, hash);
                        if (block != null)
                        {
                            if (bloom_filter == null)
                            {
                                EnqueueMessage(Message.Create(MessageCommand.Block, block));
                            }
                            else
                            {
                                BitArray flags = new(block.Transactions.Select(p => bloom_filter.Test(p)).ToArray());
                                EnqueueMessage(Message.Create(MessageCommand.MerkleBlock, MerkleBlockPayload.Create(block, flags)));
                            }
                        }
                        else
                        {
                            notFound.Add(hash);
                        }
                        break;
                    default:
                        if (system.RelayCache.TryGet(hash, out IInventory inventory))
                            EnqueueMessage(Message.Create((MessageCommand)payload.Type, inventory));
                        break;
                }
            }

            if (notFound.Count > 0)
            {
                _log.Debug("Sending NotFound for {NotFoundCount} items from GetData request", notFound.Count);
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
            _log.Debug("Received GetHeaders message: Start={IndexStart}, Count={Count}", payload.IndexStart, payload.Count);
            var snapshot = system.StoreView;
            if (payload.IndexStart > NativeContract.Ledger.CurrentIndex(snapshot)) return;
            List<Header> headers = new();
            uint count = payload.Count == -1 ? HeadersPayload.MaxHeadersCount : (uint)payload.Count;
            for (uint i = 0; i < count; i++)
            {
                var header = NativeContract.Ledger.GetHeader(snapshot, payload.IndexStart + i);
                if (header == null) break;
                headers.Add(header);
            }
            if (headers.Count == 0)
            {
                _log.Debug("No headers found for GetHeaders request from {IndexStart}", payload.IndexStart);
                return;
            }
            _log.Debug("Sending Headers response with {HeaderCount} headers", headers.Count);
            EnqueueMessage(Message.Create(MessageCommand.Headers, HeadersPayload.Create(headers.ToArray())));
        }

        private void OnHeadersMessageReceived(HeadersPayload payload)
        {
            _log.Debug("Received Headers message with {HeaderCount} headers, up to index {LastIndex}", payload.Headers.Length, payload.Headers[^1].Index);
            UpdateLastBlockIndex(payload.Headers[^1].Index);
            system.Blockchain.Tell(payload.Headers);
        }

        private void OnInventoryReceived(IInventory inventory)
        {
            _log.Verbose("Received inventory for potential processing: Type={InvType}, Hash={InvHash}", inventory.InventoryType, inventory.Hash);
            if (!knownHashes.Add(inventory.Hash))
            {
                _log.Verbose("Ignoring inventory {InvHash} (already known)", inventory.Hash);
                return;
            }
            pendingKnownHashes.Remove(inventory.Hash);
            system.TaskManager.Tell(inventory);
            switch (inventory)
            {
                case Transaction transaction:
                    if (!(system.ContainsTransaction(transaction.Hash) != ContainsTransactionType.NotExist || system.ContainsConflictHash(transaction.Hash, transaction.Signers.Select(s => s.Account))))
                    {
                        _log.Debug("Forwarding tx {TxHash} to TxRouter for preverify", transaction.Hash);
                        system.TxRouter.Tell(new TransactionRouter.Preverify(transaction, true));
                    }
                    break;
                case Block block:
                    UpdateLastBlockIndex(block.Index);
                    if (block.Index > NativeContract.Ledger.CurrentIndex(system.StoreView) + InvPayload.MaxHashesCount)
                    {
                        _log.Debug("Delaying processing block {BlockIndex} (too far ahead)", block.Index);
                        return;
                    }
                    _log.Debug("Forwarding block {BlockIndex} ({BlockHash}) to Blockchain actor", block.Index, block.Hash);
                    system.Blockchain.Tell(inventory);
                    break;
                default:
                    _log.Debug("Forwarding {InvType} {InvHash} to Blockchain actor", inventory.InventoryType, inventory.Hash);
                    system.Blockchain.Tell(inventory);
                    break;
            }
        }

        private void OnInvMessageReceived(InvPayload payload)
        {
            _log.Debug("Received Inv message: Type={InvType}, Count={HashCount}", payload.Type, payload.Hashes.Length);
            UInt256[] hashes = payload.Hashes.Where(p => !pendingKnownHashes.Contains(p) && !knownHashes.Contains(p) && !sentHashes.Contains(p)).ToArray();
            if (hashes.Length < payload.Hashes.Length)
                _log.Verbose("Filtered Inv hashes: Processing {ProcessCount}/{OriginalCount} (pending/known/sent ignored)", hashes.Length, payload.Hashes.Length);

            if (hashes.Length == 0) return;
            switch (payload.Type)
            {
                case InventoryType.Block:
                    {
                        var snapshot = system.StoreView;
                        hashes = hashes.Where(p => !NativeContract.Ledger.ContainsBlock(snapshot, p)).ToArray();
                        if (hashes.Length < payload.Hashes.Length) _log.Verbose("Filtered Block Inv hashes: Processing {ProcessCount} (already in ledger ignored)", hashes.Length);
                    }
                    break;
                case InventoryType.TX:
                    {
                        var snapshot = system.StoreView;
                        hashes = hashes.Where(p => !NativeContract.Ledger.ContainsTransaction(snapshot, p)).ToArray();
                        if (hashes.Length < payload.Hashes.Length) _log.Verbose("Filtered TX Inv hashes: Processing {ProcessCount} (already in ledger ignored)", hashes.Length);
                    }
                    break;
            }
            if (hashes.Length == 0) return;
            _log.Debug("Registering {HashCount} new tasks with TaskManager for Inv type {InvType}", hashes.Length, payload.Type);
            foreach (UInt256 hash in hashes)
                pendingKnownHashes.Add(Tuple.Create(hash, TimeProvider.Current.UtcNow));
            system.TaskManager.Tell(new TaskManager.NewTasks { Payload = InvPayload.Create(payload.Type, hashes) });
        }

        private void OnMemPoolMessageReceived()
        {
            _log.Debug("Received Mempool message, preparing Inv response");
            var verifiedTx = system.MemPool.GetVerifiedTransactions().Select(p => p.Hash).ToArray();
            _log.Debug("Sending Inv for {TxCount} verified txs in response to Mempool request", verifiedTx.Length);
            foreach (InvPayload payload in InvPayload.CreateGroup(InventoryType.TX, verifiedTx))
                EnqueueMessage(Message.Create(MessageCommand.Inv, payload));
        }

        private void OnPingMessageReceived(PingPayload payload)
        {
            _log.Debug("Received Ping nonce {Nonce}, LastBlockIndex={LastBlockIndex}", payload.Nonce, payload.LastBlockIndex);
            UpdateLastBlockIndex(payload.LastBlockIndex);
            var pong = PingPayload.Create(NativeContract.Ledger.CurrentIndex(system.StoreView), payload.Nonce);
            _log.Debug("Sending Pong nonce {Nonce}, LastBlockIndex={LastBlockIndex}", pong.Nonce, pong.LastBlockIndex);
            EnqueueMessage(Message.Create(MessageCommand.Pong, pong));
        }

        private void OnPongMessageReceived(PingPayload payload)
        {
            _log.Debug("Received Pong nonce {Nonce}, LastBlockIndex={LastBlockIndex}", payload.Nonce, payload.LastBlockIndex);
            UpdateLastBlockIndex(payload.LastBlockIndex);
        }

        private void OnVerackMessageReceived()
        {
            _log.Information("Received Verack, connection established. IsFullNode={IsFullNode}", IsFullNode);
            verack = true;
            system.TaskManager.Tell(new TaskManager.Register { Version = Version });
            CheckMessageQueue();
        }

        private void OnVersionMessageReceived(VersionPayload payload)
        {
            var startHeight = payload.Capabilities.OfType<FullNodeCapability>().FirstOrDefault()?.StartHeight ?? 0;
            _log.Information("Received Version message: UserAgent='{UserAgent}', Network={Network}, StartHeight={StartHeight}, Capabilities={Capabilities}",
                payload.UserAgent, payload.Network, startHeight, string.Join(',', payload.Capabilities.Select(c => c.GetType().Name)));
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
            if (!localNode.AllowNewConnection(Self, this))
            {
                _log.Warning("Connection denied: {Reason}", "Too many connections or other policy restriction");
                Disconnect(true);
                return;
            }
            _log.Debug("Sending Verack.");
            SendMessage(Message.Create(MessageCommand.Verack));
        }

        private void OnTimer()
        {
            DateTime oneMinuteAgo = TimeProvider.Current.UtcNow.AddMinutes(-1);
            int removed = 0;
            while (pendingKnownHashes.Count > 0)
            {
                var (_, time) = pendingKnownHashes.First;
                if (oneMinuteAgo <= time) break;
                pendingKnownHashes.RemoveFirst();
                removed++;
            }
            if (removed > 0) _log.Verbose("Removed {RemovedCount} expired pending known hashes", removed);

            if (oneMinuteAgo > lastSent)
            {
                var ping = PingPayload.Create(NativeContract.Ledger.CurrentIndex(system.StoreView));
                _log.Debug("Sending Ping nonce {Nonce}, LastBlockIndex={LastBlockIndex}", ping.Nonce, ping.LastBlockIndex);
                EnqueueMessage(Message.Create(MessageCommand.Ping, ping));
            }
        }

        private void UpdateLastBlockIndex(uint lastBlockIndex)
        {
            if (lastBlockIndex > LastBlockIndex)
            {
                _log.Debug("Updating LastBlockIndex from {OldIndex} to {NewIndex}", LastBlockIndex, lastBlockIndex);
                LastBlockIndex = lastBlockIndex;
                system.TaskManager.Tell(new TaskManager.Update { LastBlockIndex = LastBlockIndex });
            }
        }
    }
}
