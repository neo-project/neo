using Akka.Actor;
using Neo.IO;
using Neo.IO.Caching;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;

namespace Neo.Network.P2P
{
    public class LocalNode : Peer
    {
        public class Register { }
        public class InventoryReceived { public IInventory Inventory; }
        public class Broadcast { public Message Message; }
        public class Relay { public IInventory Inventory; }
        internal class RelayDirectly { public IInventory Inventory; }

        public const uint ProtocolVersion = 0;
        protected override int ConnectedMax => 10;
        protected override int UnconnectedMax => 1000;

        public readonly IActorRef Blockchain;
        internal readonly IActorRef TaskManager = Context.ActorOf<TaskManager>();
        internal readonly ConcurrentDictionary<IActorRef, RemoteNode> RemoteNodes = new ConcurrentDictionary<IActorRef, RemoteNode>();
        internal readonly RelayCache RelayCache = new RelayCache(100);
        private readonly HashSet<IActorRef> subscribers = new HashSet<IActorRef>();

        public int ConnectedCount => RemoteNodes.Count;
        public int UnconnectedCount => UnconnectedPeers.Count;
        public static readonly uint Nonce;
        public static string UserAgent { get; set; }

        private static LocalNode singleton { get; set; }
        public static LocalNode Singleton
        {
            get
            {
                while (singleton == null) Thread.Sleep(10);
                return singleton;
            }
        }

        static LocalNode()
        {
            Random rand = new Random();
            Nonce = (uint)rand.Next();
            UserAgent = $"/{Assembly.GetExecutingAssembly().GetName().Name}:{Assembly.GetExecutingAssembly().GetVersion()}/";
        }

        public LocalNode(Store store)
        {
            lock (GetType())
            {
                if (singleton != null)
                    throw new InvalidOperationException();
                this.Blockchain = Context.ActorOf(Ledger.Blockchain.Props(store));
                singleton = this;
            }
        }

        private void BroadcastMessage(string command, ISerializable payload = null)
        {
            BroadcastMessage(Message.Create(command, payload));
        }

        private void BroadcastMessage(Message message)
        {
            Connections.Tell(new RemoteNode.Send { Message = message });
        }

        private void Distribute(object message)
        {
            foreach (IActorRef subscriber in subscribers)
                subscriber.Tell(message);
        }

        private static IPEndPoint GetIPEndpointFromHostPort(string hostNameOrAddress, int port)
        {
            if (IPAddress.TryParse(hostNameOrAddress, out IPAddress ipAddress))
                return new IPEndPoint(ipAddress, port);
            IPHostEntry entry;
            try
            {
                entry = Dns.GetHostEntry(hostNameOrAddress);
            }
            catch (SocketException)
            {
                return null;
            }
            ipAddress = entry.AddressList.FirstOrDefault(p => p.AddressFamily == AddressFamily.InterNetwork || p.IsIPv6Teredo);
            if (ipAddress == null) return null;
            return new IPEndPoint(ipAddress, port);
        }

        private static IEnumerable<IPEndPoint> GetIPEndPointsFromSeedList(int seedsToTake)
        {
            if (seedsToTake > 0)
            {
                Random rand = new Random();
                foreach (string hostAndPort in Settings.Default.SeedList.OrderBy(p => rand.Next()))
                {
                    if (seedsToTake == 0) break;
                    string[] p = hostAndPort.Split(':');
                    IPEndPoint seed;
                    try
                    {
                        seed = GetIPEndpointFromHostPort(p[0], int.Parse(p[1]));
                    }
                    catch (AggregateException)
                    {
                        continue;
                    }
                    if (seed == null) continue;
                    seedsToTake--;
                    yield return seed;
                }
            }
        }

        public IEnumerable<RemoteNode> GetRemoteNodes()
        {
            return RemoteNodes.Values;
        }

        protected override void NeedMorePeers(int count)
        {
            count = Math.Max(count, 5);
            if (ConnectedPeers.Count > 0)
            {
                BroadcastMessage("getaddr");
            }
            else
            {
                AddPeers(GetIPEndPointsFromSeedList(count));
            }
        }

        private void OnConsensusPayload(ConsensusPayload payload)
        {
            RelayCache.Add(payload);
            OnRelayDirectly(payload);
        }

        private void OnInventoryReceived(IInventory inventory)
        {
            switch (inventory)
            {
                case MinerTransaction _:
                    return;
                case Transaction transaction:
                    Blockchain.Tell(new Blockchain.NewTransaction { Transaction = transaction });
                    break;
                case Block block:
                    Blockchain.Tell(new Blockchain.NewBlock { Block = block });
                    break;
                case ConsensusPayload payload:
                    if (!payload.Verify(Ledger.Blockchain.Singleton.Snapshot)) return;
                    OnConsensusPayload(payload);
                    break;
            }
            Distribute(new InventoryReceived { Inventory = inventory });
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case Register _:
                    OnRegister();
                    break;
                case Broadcast broadcast:
                    BroadcastMessage(broadcast.Message);
                    break;
                case Relay relay:
                    OnInventoryReceived(relay.Inventory);
                    break;
                case RelayDirectly relay:
                    OnRelayDirectly(relay.Inventory);
                    break;
                case RemoteNode.InventoryReceived received:
                    OnInventoryReceived(received.Inventory);
                    break;
                case Blockchain.RelayResult _:
                    break;
                case Terminated terminated:
                    subscribers.Remove(terminated.ActorRef);
                    base.OnReceive(message);
                    break;
                default:
                    base.OnReceive(message);
                    break;
            }
        }

        private void OnRegister()
        {
            subscribers.Add(Sender);
            Context.Watch(Sender);
        }

        private void OnRelayDirectly(IInventory inventory)
        {
            Connections.Tell(new RemoteNode.Relay { Inventory = inventory });
        }

        public static Props Props(Store store)
        {
            return Akka.Actor.Props.Create(() => new LocalNode(store));
        }

        protected override Props ProtocolProps(IActorRef tcp, IPEndPoint remote, IPEndPoint local)
        {
            return RemoteNode.Props(tcp, remote, local);
        }
    }
}
