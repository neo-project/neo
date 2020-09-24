using Akka.Actor;
using Akka.Configuration;
using Akka.IO;
using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Actors;
using Neo.Ledger;
using Neo.Models;
using Neo.Network.P2P.Capabilities;
using Neo.Network.P2P.Payloads;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Neo.Network.P2P
{
    public partial class RemoteNode : Connection
    {
        internal class StartProtocol { }
        internal class Relay { public IWitnessed Inventory; }

        private readonly NeoSystem system;
        private readonly Queue<Message> message_queue_high = new Queue<Message>();
        private readonly Queue<Message> message_queue_low = new Queue<Message>();
        private ByteString msg_buffer = ByteString.Empty;
        private bool ack = true;

        public IPEndPoint Listener => new IPEndPoint(Remote.Address, ListenerTcpPort);
        public int ListenerTcpPort { get; private set; } = 0;
        public VersionPayload Version { get; private set; }
        public uint LastBlockIndex { get; private set; } = 0;
        public bool IsFullNode { get; private set; } = false;

        public RemoteNode(NeoSystem system, object connection, IPEndPoint remote, IPEndPoint local)
            : base(connection, remote, local)
        {
            this.system = system;
            LocalNode.Singleton.RemoteNodes.TryAdd(Self, this);
        }

        /// <summary>
        /// It defines the message queue to be used for dequeuing.
        /// If the high-priority message queue is not empty, choose the high-priority message queue.
        /// Otherwise, choose the low-priority message queue.
        /// Finally, it sends the first message of the queue.
        /// </summary>
        private void CheckMessageQueue()
        {
            if (!verack || !ack) return;
            Queue<Message> queue = message_queue_high;
            if (queue.Count == 0)
            {
                queue = message_queue_low;
                if (queue.Count == 0) return;
            }
            SendMessage(queue.Dequeue());
        }

        private void EnqueueMessage(MessageCommand command, ISerializable payload = null)
        {
            EnqueueMessage(Message.Create(command, payload));
        }

        /// <summary>
        /// Add message to high priority queue or low priority queue depending on the message type.
        /// </summary>
        /// <param name="message">The message to be added.</param>
        private void EnqueueMessage(Message message)
        {
            bool is_single = false;
            switch (message.Command)
            {
                case MessageCommand.Addr:
                case MessageCommand.GetAddr:
                case MessageCommand.GetBlocks:
                case MessageCommand.GetHeaders:
                case MessageCommand.Mempool:
                case MessageCommand.Ping:
                case MessageCommand.Pong:
                    is_single = true;
                    break;
            }
            Queue<Message> message_queue;
            switch (message.Command)
            {
                case MessageCommand.Alert:
                case MessageCommand.Consensus:
                case MessageCommand.FilterAdd:
                case MessageCommand.FilterClear:
                case MessageCommand.FilterLoad:
                case MessageCommand.GetAddr:
                case MessageCommand.Mempool:
                    message_queue = message_queue_high;
                    break;
                default:
                    message_queue = message_queue_low;
                    break;
            }
            if (!is_single || message_queue.All(p => p.Command != message.Command))
                message_queue.Enqueue(message);
            CheckMessageQueue();
        }

        protected override void OnAck()
        {
            ack = true;
            CheckMessageQueue();
        }

        protected override void OnData(ByteString data)
        {
            msg_buffer = msg_buffer.Concat(data);

            for (Message message = TryParseMessage(); message != null; message = TryParseMessage())
                OnMessage(message);
        }

        protected override void OnReceive(object message)
        {
            base.OnReceive(message);
            switch (message)
            {
                case Timer _:
                    RefreshPendingKnownHashes();
                    break;
                case Message msg:
                    EnqueueMessage(msg);
                    break;
                case IWitnessed inventory:
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

        private void OnRelay(IWitnessed inventory)
        {
            if (!IsFullNode) return;
            var invType = inventory.GetInventoryType();
            if (invType == InventoryType.TX)
            {
                if (bloom_filter != null && !bloom_filter.Test((Transaction)inventory))
                    return;
            }
            EnqueueMessage(MessageCommand.Inv, InvPayload.Create(invType, inventory.Hash));
        }

        private void OnSend(IWitnessed inventory)
        {
            if (!IsFullNode) return;
            var invType = inventory.GetInventoryType();
            if (invType == InventoryType.TX)
            {
                if (bloom_filter != null && !bloom_filter.Test((Transaction)inventory))
                    return;
            }
            EnqueueMessage((MessageCommand)invType, inventory);
        }

        private void OnStartProtocol()
        {
            var capabilities = new List<NodeCapability>
            {
                new FullNodeCapability(Blockchain.Singleton.Height)
            };

            if (LocalNode.Singleton.ListenerTcpPort > 0) capabilities.Add(new ServerCapability(NodeCapabilityType.TcpServer, (ushort)LocalNode.Singleton.ListenerTcpPort));
            if (LocalNode.Singleton.ListenerWsPort > 0) capabilities.Add(new ServerCapability(NodeCapabilityType.WsServer, (ushort)LocalNode.Singleton.ListenerWsPort));

            SendMessage(Message.Create(MessageCommand.Version, VersionPayload.Create(LocalNode.Nonce, LocalNode.UserAgent, capabilities.ToArray())));
        }

        protected override void PostStop()
        {
            timer.CancelIfNotNull();
            LocalNode.Singleton.RemoteNodes.TryRemove(Self, out _);
            base.PostStop();
        }

        internal static Props Props(NeoSystem system, object connection, IPEndPoint remote, IPEndPoint local)
        {
            return Akka.Actor.Props.Create(() => new RemoteNode(system, connection, remote, local)).WithMailbox("remote-node-mailbox");
        }

        private void SendMessage(Message message)
        {
            ack = false;
            SendData(ByteString.FromBytes(message.ToArray()));
        }

        private Message TryParseMessage()
        {
            var length = Message.TryDeserialize(msg_buffer, out var msg);
            if (length <= 0) return null;

            msg_buffer = msg_buffer.Slice(length).Compact();
            return msg;
        }
    }

    internal class RemoteNodeMailbox : PriorityMailbox
    {
        public RemoteNodeMailbox(Settings settings, Config config) : base(settings, config) { }

        internal protected override bool IsHighPriority(object message)
        {
            switch (message)
            {
                case Message msg:
                    switch (msg.Command)
                    {
                        case MessageCommand.Consensus:
                        case MessageCommand.FilterAdd:
                        case MessageCommand.FilterClear:
                        case MessageCommand.FilterLoad:
                        case MessageCommand.Verack:
                        case MessageCommand.Version:
                        case MessageCommand.Alert:
                            return true;
                        default:
                            return false;
                    }
                case Tcp.ConnectionClosed _:
                case Connection.Close _:
                case Connection.Ack _:
                    return true;
                default:
                    return false;
            }
        }

        internal protected override bool ShallDrop(object message, IEnumerable queue)
        {
            if (!(message is Message msg)) return false;
            switch (msg.Command)
            {
                case MessageCommand.GetAddr:
                case MessageCommand.GetBlocks:
                case MessageCommand.GetHeaders:
                case MessageCommand.Mempool:
                    return queue.OfType<Message>().Any(p => p.Command == msg.Command);
                default:
                    return false;
            }
        }
    }
}
