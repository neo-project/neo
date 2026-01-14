// Copyright (C) 2015-2026 The Neo Project.
//
// RemoteNode.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Akka.Configuration;
using Akka.IO;
using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Actors;
using Neo.IO.Caching;
using Neo.Network.P2P.Payloads;
using System.Collections;
using System.Net;

namespace Neo.Network.P2P;

/// <summary>
/// Represents a connection of the NEO network.
/// </summary>
public partial class RemoteNode : Connection
{
    internal record StartProtocol;
    internal record Relay(IInventory Inventory);

    private readonly NeoSystem _system;
    private readonly LocalNode _localNode;
    private readonly Queue<Message> _messageQueueHigh = new();
    private readonly Queue<Message> _messageQueueLow = new();
    private DateTime _lastSent = TimeProvider.Current.UtcNow;
    private readonly bool[] _sentCommands = new bool[1 << (sizeof(MessageCommand) * 8)];
    private ByteString _messageBuffer = ByteString.Empty;
    private bool _ack = true;
    private uint _lastHeightSent = 0;

    /// <summary>
    /// The address of the remote Tcp server.
    /// </summary>
    public IPEndPoint Listener => new(Remote.Address, ListenerTcpPort);

    /// <summary>
    /// The port listened by the remote Tcp server. If the remote node is not a server, this field is 0.
    /// </summary>
    public int ListenerTcpPort { get; private set; } = 0;

    /// <summary>
    /// The <see cref="VersionPayload"/> sent by the remote node.
    /// </summary>
    public VersionPayload? Version { get; private set; }

    /// <summary>
    /// The index of the last block sent by the remote node.
    /// </summary>
    public uint LastBlockIndex { get; private set; } = 0;

    /// <summary>
    /// Indicates whether the remote node is a full node.
    /// </summary>
    public bool IsFullNode { get; private set; } = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteNode"/> class.
    /// </summary>
    /// <param name="system">The <see cref="NeoSystem"/> object that contains the <paramref name="localNode"/>.</param>
    /// <param name="localNode">The <see cref="LocalNode"/> that manages the <see cref="RemoteNode"/>.</param>
    /// <param name="connection">The underlying connection object.</param>
    /// <param name="remote">The address of the remote node.</param>
    /// <param name="local">The address of the local node.</param>
    /// <param name="config">P2P settings.</param>
    public RemoteNode(NeoSystem system, LocalNode localNode, object connection, IPEndPoint remote, IPEndPoint local, ChannelsConfig config)
        : base(connection, remote, local)
    {
        _system = system;
        _localNode = localNode;
        _knownHashes = new HashSetCache<UInt256>(Math.Max(1, config.MaxKnownHashes));
        _sentHashes = new HashSetCache<UInt256>(Math.Max(1, config.MaxKnownHashes));
        localNode.RemoteNodes.TryAdd(Self, this);
    }

    /// <summary>
    /// It defines the message queue to be used for dequeuing.
    /// If the high-priority message queue is not empty, choose the high-priority message queue.
    /// Otherwise, choose the low-priority message queue.
    /// Finally, it sends the first message of the queue.
    /// </summary>
    private void CheckMessageQueue()
    {
        if (!_verack || !_ack) return;
        Queue<Message> queue = _messageQueueHigh;
        if (queue.Count == 0)
        {
            queue = _messageQueueLow;
            if (queue.Count == 0) return;
        }
        SendMessage(queue.Dequeue());
    }

    private void EnqueueMessage(MessageCommand command, ISerializable? payload = null)
    {
        EnqueueMessage(Message.Create(command, payload));
    }

    /// <summary>
    /// Add message to high priority queue or low priority queue depending on the message type.
    /// </summary>
    /// <param name="message">The message to be added.</param>
    private void EnqueueMessage(Message message)
    {
        bool is_single = message.Command switch
        {
            MessageCommand.Addr or MessageCommand.GetAddr or MessageCommand.GetBlocks or MessageCommand.GetHeaders or MessageCommand.Mempool or MessageCommand.Ping or MessageCommand.Pong => true,
            _ => false,
        };
        Queue<Message> message_queue = message.Command switch
        {
            MessageCommand.Alert or MessageCommand.Extensible or MessageCommand.FilterAdd or MessageCommand.FilterClear or MessageCommand.FilterLoad or MessageCommand.GetAddr or MessageCommand.Mempool => _messageQueueHigh,
            _ => _messageQueueLow,
        };
        if (!is_single || message_queue.All(p => p.Command != message.Command))
        {
            message_queue.Enqueue(message);
            _lastSent = TimeProvider.Current.UtcNow;
        }
        CheckMessageQueue();
    }

    protected override void OnAck()
    {
        _ack = true;
        CheckMessageQueue();
    }

    protected override void OnDisconnect(DisconnectReason reason)
    {
        if (reason != DisconnectReason.Close)
        {
            // DHT: connection dropped. Penalize the contact (do not immediately delete; allow churn).
            if (Version != null)
                _localNode.RoutingTable.MarkFailure(Version.NodeId);
        }
    }

    protected override void OnData(ByteString data)
    {
        _messageBuffer = _messageBuffer.Concat(data);

        for (var message = TryParseMessage(); message != null; message = TryParseMessage())
            OnMessage(message);
    }

    protected override void OnReceive(object message)
    {
        base.OnReceive(message);
        switch (message)
        {
            case Timer _:
                OnTimer();
                break;
            case Message msg:
                if (msg.Payload is PingPayload payload)
                {
                    if (payload.LastBlockIndex > _lastHeightSent)
                        _lastHeightSent = payload.LastBlockIndex;
                    else if (msg.Command == MessageCommand.Ping)
                        break;
                }
                EnqueueMessage(msg);
                break;
            case IInventory inventory:
                OnSend(inventory);
                break;
            case Relay relay:
                OnRelay(relay.Inventory);
                break;
            case StartProtocol _:
                OnStartProtocol();
                break;
        }
    }

    private void OnRelay(IInventory inventory)
    {
        if (!IsFullNode) return;
        if (inventory.InventoryType == InventoryType.TX)
        {
            if (_bloomFilter != null && !_bloomFilter.Test((Transaction)inventory))
                return;
        }
        EnqueueMessage(MessageCommand.Inv, InvPayload.Create(inventory.InventoryType, inventory.Hash));
    }

    private void OnSend(IInventory inventory)
    {
        if (!IsFullNode) return;
        if (inventory.InventoryType == InventoryType.TX)
        {
            if (_bloomFilter != null && !_bloomFilter.Test((Transaction)inventory))
                return;
        }
        EnqueueMessage((MessageCommand)inventory.InventoryType, inventory);
    }

    private void OnStartProtocol()
    {
        SendMessage(Message.Create(MessageCommand.Version, VersionPayload.Create(_system.Settings, _localNode.NodeKey, LocalNode.UserAgent, _localNode.GetNodeCapabilities())));
    }

    protected override void PostStop()
    {
        timer.CancelIfNotNull();
        if (_localNode.RemoteNodes.TryRemove(Self, out _))
        {
            _knownHashes.Clear();
            _sentHashes.Clear();
        }
        base.PostStop();
    }

    internal static Props Props(NeoSystem system, LocalNode localNode, object connection, IPEndPoint remote, IPEndPoint local, ChannelsConfig config)
    {
        return Akka.Actor.Props.Create(() => new RemoteNode(system, localNode, connection, remote, local, config)).WithMailbox("remote-node-mailbox");
    }

    private void SendMessage(Message message)
    {
        _ack = false;
        // Here it is possible that we dont have the Version message yet,
        // so we need to send the message uncompressed
        SendData(ByteString.FromBytes(message.ToArray(Version?.AllowCompression ?? false)));
        _sentCommands[(byte)message.Command] = true;
    }

    private Message? TryParseMessage()
    {
        var length = Message.TryDeserialize(_messageBuffer, out var msg);
        if (length <= 0) return null;

        _messageBuffer = _messageBuffer.Slice(length).Compact();
        return msg;
    }
}

internal class RemoteNodeMailbox : PriorityMailbox
{
    public RemoteNodeMailbox(Settings settings, Config config) : base(settings, config) { }

    internal protected override bool IsHighPriority(object message)
    {
        return message switch
        {
            Message msg => msg.Command switch
            {
                MessageCommand.Extensible or MessageCommand.FilterAdd or MessageCommand.FilterClear or MessageCommand.FilterLoad or MessageCommand.Verack or MessageCommand.Version or MessageCommand.Alert => true,
                _ => false,
            },
            Tcp.ConnectionClosed _ or Connection.Close _ or Connection.Ack _ => true,
            _ => false,
        };
    }

    internal protected override bool ShallDrop(object message, IEnumerable queue)
    {
        if (message is not Message msg) return false;
        return msg.Command switch
        {
            MessageCommand.GetAddr or MessageCommand.GetBlocks or MessageCommand.GetHeaders or MessageCommand.Mempool => queue.OfType<Message>().Any(p => p.Command == msg.Command),
            _ => false,
        };
    }
}
