using Akka.Actor;
using Akka.Configuration;
using Akka.IO;
using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Actors;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using System;
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
        private bool ack = true;
        private BloomFilter bloom_filter;
        private bool verack = false;

        public IPEndPoint Listener => new IPEndPoint(Remote.Address, ListenerPort);
        public override int ListenerPort => Version?.Port ?? 0;
        public VersionPayload Version { get; private set; }

        public RemoteNode(NeoSystem system, object connection, IPEndPoint remote, IPEndPoint local)
            : base(connection, remote, local)
        {
            this.system = system;
            this.protocol = Context.ActorOf(ProtocolHandler.Props(system));
            LocalNode.Singleton.RemoteNodes.TryAdd(Self, this);
            SendMessage(Message.Create("version", VersionPayload.Create(LocalNode.Singleton.ListenerPort, LocalNode.Nonce, LocalNode.UserAgent, Blockchain.Singleton.Height)));
        }

        private void CheckMessageQueue()
        {
            if (!verack || !ack) return;
            Queue<Message> queue = message_queue_high;
            if (queue.Count == 0) queue = message_queue_low;
            if (queue.Count == 0) return;
            SendMessage(queue.Dequeue());
        }

        private void EnqueueMessage(string command, ISerializable payload = null)
        {
            EnqueueMessage(Message.Create(command, payload));
        }

        private void EnqueueMessage(Message message)
        {
            bool is_single = false;
            switch (message.Command)
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
            switch (message.Command)
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
                case ProtocolHandler.SetVersion setVersion:
                    OnSetVersion(setVersion.Version);
                    break;
                case ProtocolHandler.SetVerack _:
                    OnSetVerack();
                    break;
                case ProtocolHandler.SetFilter setFilter:
                    OnSetFilter(setFilter.Filter);
                    break;
            }
        }

        private void OnRelay(IInventory inventory)
        {
            if (Version?.Relay != true) return;
            if (inventory.InventoryType == InventoryType.TX)
            {
                if (bloom_filter != null && !bloom_filter.Test((Transaction)inventory))
                    return;
            }
            EnqueueMessage("inv", InvPayload.Create(inventory.InventoryType, inventory.Hash));
        }

        private void OnSend(IInventory inventory)
        {
            if (Version?.Relay != true) return;
            if (inventory.InventoryType == InventoryType.TX)
            {
                if (bloom_filter != null && !bloom_filter.Test((Transaction)inventory))
                    return;
            }
            EnqueueMessage(inventory.InventoryType.ToString().ToLower(), inventory);
        }

        private void OnSetFilter(BloomFilter filter)
        {
            bloom_filter = filter;
        }

        private void OnSetVerack()
        {
            verack = true;
            system.TaskManager.Tell(new TaskManager.Register { Version = Version });
            CheckMessageQueue();
        }

        private void OnSetVersion(VersionPayload version)
        {
            this.Version = version;
            if (version.Nonce == LocalNode.Nonce)
            {
                Disconnect(true);
                return;
            }
            if (LocalNode.Singleton.RemoteNodes.Values.Where(p => p != this).Any(p => p.Remote.Address.Equals(Remote.Address) && p.Version?.Nonce == version.Nonce))
            {
                Disconnect(true);
                return;
            }
            SendMessage(Message.Create("verack"));
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
            if (msg_buffer.Count < sizeof(uint)) return null;
            uint magic = msg_buffer.Slice(0, sizeof(uint)).ToArray().ToUInt32(0);
            if (magic != Message.Magic)
                throw new FormatException();
            if (msg_buffer.Count < Message.HeaderSize) return null;
            int length = msg_buffer.Slice(16, sizeof(int)).ToArray().ToInt32(0);
            if (length > Message.PayloadMaxSize)
                throw new FormatException();
            length += Message.HeaderSize;
            if (msg_buffer.Count < length) return null;
            Message message = msg_buffer.Slice(0, length).ToArray().AsSerializable<Message>();
            msg_buffer = msg_buffer.Slice(length).Compact();
            return message;
        }
    }

    internal class RemoteNodeMailbox : PriorityMailbox
    {
        public RemoteNodeMailbox(Akka.Actor.Settings settings, Config config)
            : base(settings, config)
        {
        }

        protected override bool IsHighPriority(object message)
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
