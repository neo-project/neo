using Akka.Actor;
using Akka.Configuration;
using Akka.IO;
using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Actors;
using Neo.Ledger;
using Neo.Network.P2P.Capabilities;
using Neo.Network.P2P.Payloads;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Neo.Network.P2P
{
    public class RemoteNode : Connection
    {
        internal class Relay { public IInventory Inventory; }

        private readonly NeoSystem system;
        private readonly IActorRef protocol;
        private readonly Queue<Message> message_queue_high = new Queue<Message>();
        private readonly Queue<Message> message_queue_low = new Queue<Message>();
        private ByteString msg_buffer = ByteString.Empty;
        private BloomFilter bloom_filter;
        private bool ack = true;
        private bool verack = false;

        public IPEndPoint Listener => new IPEndPoint(Remote.Address, ListenerTcpPort);
        public int ListenerTcpPort { get; private set; } = 0;
        public VersionPayload Version { get; private set; }
        public uint LastBlockIndex { get; private set; } = 0;
        public bool IsFullNode { get; private set; } = false;

        public RemoteNode(NeoSystem system, object connection, IPEndPoint remote, IPEndPoint local)
            : base(connection, remote, local)
        {
            this.system = system;
            this.protocol = Context.ActorOf(ProtocolHandler.Props(system));
            LocalNode.Singleton.RemoteNodes.TryAdd(Self, this);

            var capabilities = new List<NodeCapability>
            {
                new FullNodeCapability(Blockchain.Singleton.Height)
            };

            if (LocalNode.Singleton.ListenerTcpPort > 0) capabilities.Add(new ServerCapability(NodeCapabilityType.TcpServer, (ushort)LocalNode.Singleton.ListenerTcpPort));
            if (LocalNode.Singleton.ListenerWsPort > 0) capabilities.Add(new ServerCapability(NodeCapabilityType.WsServer, (ushort)LocalNode.Singleton.ListenerWsPort));

            SendMessage(Message.Create(MessageCommand.Version, VersionPayload.Create(LocalNode.Nonce, LocalNode.UserAgent, capabilities.ToArray())));
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
                protocol.Tell(message);
        }

        protected override void OnReceive(object message)
        {
            base.OnReceive(message);
            switch (message)
            {
                case Message msg:
                    EnqueueMessage(msg);
                    break;
                case IInventory inventory:
                    OnSend(inventory);
                    break;
                case Relay relay:
                    OnRelay(relay.Inventory);
                    break;
                case VersionPayload payload:
                    OnVersionPayload(payload);
                    break;
                case MessageCommand.Verack:
                    OnVerack();
                    break;
                case ProtocolHandler.SetFilter setFilter:
                    OnSetFilter(setFilter.Filter);
                    break;
                case PingPayload payload:
                    OnPingPayload(payload);
                    break;
            }
        }

        private void OnPingPayload(PingPayload payload)
        {
            if (payload.LastBlockIndex > LastBlockIndex)
            {
                LastBlockIndex = payload.LastBlockIndex;
                system.TaskManager.Tell(new TaskManager.Update { LastBlockIndex = LastBlockIndex });
            }
        }

        private void OnRelay(IInventory inventory)
        {
            if (!IsFullNode) return;
            if (inventory.InventoryType == InventoryType.TX)
            {
                if (bloom_filter != null && !bloom_filter.Test((Transaction)inventory))
                    return;
            }
            EnqueueMessage(MessageCommand.Inv, InvPayload.Create(inventory.InventoryType, inventory.Hash));
        }

        private void OnSend(IInventory inventory)
        {
            if (!IsFullNode) return;
            if (inventory.InventoryType == InventoryType.TX)
            {
                if (bloom_filter != null && !bloom_filter.Test((Transaction)inventory))
                    return;
            }
            EnqueueMessage((MessageCommand)inventory.InventoryType, inventory);
        }

        private void OnSetFilter(BloomFilter filter)
        {
            bloom_filter = filter;
        }

        private void OnVerack()
        {
            verack = true;
            system.TaskManager.Tell(new TaskManager.Register { Version = Version });
            CheckMessageQueue();
        }

        private void OnVersionPayload(VersionPayload version)
        {
            Version = version;
            foreach (NodeCapability capability in version.Capabilities)
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
            if (version.Nonce == LocalNode.Nonce || version.Magic != ProtocolSettings.Default.Magic)
            {
                Disconnect(true);
                return;
            }
            if (LocalNode.Singleton.RemoteNodes.Values.Where(p => p != this).Any(p => p.Remote.Address.Equals(Remote.Address) && p.Version?.Nonce == version.Nonce))
            {
                Disconnect(true);
                return;
            }
            SendMessage(Message.Create(MessageCommand.Verack));
        }

        protected override void PostStop()
        {
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

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(ex =>
            {
                Disconnect(true);
                return Directive.Stop;
            }, loggingEnabled: false);
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
                case Tcp.ConnectionClosed _:
                case Connection.Timer _:
                case Connection.Ack _:
                    return true;
                default:
                    return false;
            }
        }
    }
}
