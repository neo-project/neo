using Akka.Actor;
using Akka.IO;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
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
        public class Relay { public IInventory Inventory; }
        internal class RelayDirectly { public IInventory Inventory; }
        internal class SendDirectly { public IInventory Inventory; }

        public const uint ProtocolVersion = 0;
        private const int MaxCountFromSeedList = 5;

        private static readonly object lockObj = new object();
        private readonly NeoSystem system;
        internal readonly ConcurrentDictionary<IActorRef, RemoteNode> RemoteNodes = new ConcurrentDictionary<IActorRef, RemoteNode>();

        public int ConnectedCount => RemoteNodes.Count;
        public int UnconnectedCount => UnconnectedPeers.Count;
        public static readonly uint Nonce;
        public static string UserAgent { get; set; }

        private static LocalNode singleton;
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

        public LocalNode(NeoSystem system)
        {
            lock (lockObj)
            {
                if (singleton != null)
                    throw new InvalidOperationException();
                this.system = system;
                singleton = this;
            }
        }

        /// <summary>
        /// Packs a MessageCommand to a full Message with an optional ISerializable payload.
        /// Forwards it to <see cref="BroadcastMessage(Message message)"/>.
        /// </summary>
        /// <param name="command">The message command to be packed.</param>
        /// <param name="payload">Optional payload to be Serialized along the message.</param>
        private void BroadcastMessage(MessageCommand command, ISerializable payload = null)
        {
            BroadcastMessage(Message.Create(command, payload));
        }

        /// <summary>
        /// Broadcast a message to all connected nodes, namely <see cref="Connections"/>.
        /// </summary>
        /// <param name="message">The message to be broadcasted.</param>
        private void BroadcastMessage(Message message)
        {
            Connections.Tell(message);
        }

        private static IPEndPoint GetIPEndpointFromHostPort(string hostNameOrAddress, int port)
        {
            if (IPAddress.TryParse(hostNameOrAddress, out IPAddress ipAddress))
                return new IPEndPoint(ipAddress, port);
            IPHostEntry entry;
            try
            {
                entry = System.Net.Dns.GetHostEntry(hostNameOrAddress);
            }
            catch (SocketException)
            {
                return null;
            }
            ipAddress = entry.AddressList.FirstOrDefault(p => p.AddressFamily == AddressFamily.InterNetwork || p.IsIPv6Teredo);
            if (ipAddress == null) return null;
            return new IPEndPoint(ipAddress, port);
        }

        /// <summary>
        /// Return an amount of random seeds nodes from the default SeedList file defined on <see cref="ProtocolSettings"/>.
        /// </summary>
        /// <param name="seedsToTake">Limit of random seed nodes to be obtained, also limited by the available seeds from file.</param>
        private static IEnumerable<IPEndPoint> GetIPEndPointsFromSeedList(int seedsToTake)
        {
            if (seedsToTake > 0)
            {
                Random rand = new Random();
                foreach (string hostAndPort in ProtocolSettings.Default.SeedList.OrderBy(p => rand.Next()))
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

        public bool CheckDuplicateNonce(IActorRef remoteActor, RemoteNode remoteNode)
        {
            var version = remoteNode.Version;
            var remote = remoteNode.Remote;

            if (version.Nonce == Nonce)
            {
                if (LocalAddresses.Count < MaxConnections)
                {
                    LocalAddresses.Add(remote.Address);
                }
                return true;
            }
            if (remote == null)
            {
                return false;
            }
            foreach (var pair in RemoteNodes)
            {
                var remoteActorRef = pair.Key;
                var otherNode = pair.Value;
                if (otherNode != remoteNode && otherNode.Remote.Address.Equals(remote.Address) && otherNode.Version?.Nonce == version.Nonce)
                {
                    return true;
                }
            }
            if (remote.Port != remoteNode.ListenerTcpPort && remoteNode.ListenerTcpPort != 0)
            {
                ConnectedPeers.TryUpdate(remoteActor, remoteNode.Listener, remote);
            }

            return false;
        }

        public IEnumerable<RemoteNode> GetRemoteNodes()
        {
            return RemoteNodes.Values;
        }

        public IEnumerable<IPEndPoint> GetUnconnectedPeers()
        {
            return UnconnectedPeers;
        }

        /// <summary>
        /// Override of abstract class that is triggered when <see cref="UnconnectedPeers"/> is empty.
        /// Performs a BroadcastMessage with the command `MessageCommand.GetAddr`, which, eventually, tells all known connections.
        /// If there are no connected peers it will try with the default, respecting MaxCountFromSeedList limit.
        /// </summary>
        /// <param name="count">The count of peers required</param>
        protected override void NeedMorePeers(int count)
        {
            count = Math.Max(count, MaxCountFromSeedList);
            if (ConnectedPeers.Count > 0)
            {
                BroadcastMessage(MessageCommand.GetAddr);
            }
            else
            {
                // Will call AddPeers with default SeedList set cached on <see cref="ProtocolSettings"/>.
                // It will try to add those, sequentially, to the list of currently uncconected ones.
                AddPeers(GetIPEndPointsFromSeedList(count));
            }
        }

        public NetworkAddressWithTime[] GetRandomConnectedPeers(int count)
        {
            Random rand = new Random();
            IEnumerable<RemoteNode> peers = RemoteNodes.Values
                .Where(p => p.ListenerTcpPort > 0)
                .OrderBy(p => rand.Next())
                .Take(count);
            return peers.Select(p => NetworkAddressWithTime.Create(p.Listener.Address, p.Version.Timestamp, p.Version.Capabilities)).ToArray();
        }

        protected override void OnReceive(object message)
        {
            base.OnReceive(message);
            switch (message)
            {
                case Message msg:
                    BroadcastMessage(msg);
                    break;
                case Relay relay:
                    OnRelay(relay.Inventory);
                    break;
                case RelayDirectly relay:
                    OnRelayDirectly(relay.Inventory);
                    break;
                case SendDirectly send:
                    OnSendDirectly(send.Inventory);
                    break;
                case RelayResultReason _:
                    break;
            }
        }

        /// <summary>
        /// For Transaction type of IInventory, it will tell Transaction to the actor of Consensus.
        /// Otherwise, tell the inventory to the actor of Blockchain.
        /// There are, currently, three implementations of IInventory: TX, Block and ConsensusPayload.
        /// </summary>
        /// <param name="inventory">The inventory to be relayed.</param>
        private void OnRelay(IInventory inventory)
        {
            if (inventory is Transaction transaction)
                system.Consensus?.Tell(transaction);
            system.Blockchain.Tell(inventory);
        }

        private void OnRelayDirectly(IInventory inventory)
        {
            Connections.Tell(new RemoteNode.Relay { Inventory = inventory });
        }

        private void OnSendDirectly(IInventory inventory)
        {
            Connections.Tell(inventory);
        }

        protected override bool PreTcpConnectedCheck(IPEndPoint remote, IPEndPoint local, out Tcp.Message errorMsg)
        {
            if (MaxConnections != -1 && ConnectedPeers.Count >= MaxConnections && !TrustedIpAddresses.Contains(remote.Address))
            {
                Message reason = CreateDisconnectMessage(DisconnectReason.MaxConnectionReached);
                errorMsg = Tcp.Write.Create(ByteString.FromBytes(reason.ToArray()));
                return false;
            }

            ConnectedAddresses.TryGetValue(remote.Address, out int count);
            if (count >= MaxConnectionsPerAddress)
            {
                Message reason = CreateDisconnectMessage(DisconnectReason.MaxConnectionPerAddressReached);
                errorMsg = Tcp.Write.Create(ByteString.FromBytes(reason.ToArray()));
                return false;
            }
            errorMsg = null;
            return true;
        }

        protected override bool PreWsConnectedCheck(IPEndPoint remote, IPEndPoint local, out ArraySegment<byte> errorMsg)
        {
            ConnectedAddresses.TryGetValue(remote.Address, out int count);
            if (count >= MaxConnectionsPerAddress)
            {
                var disconnectMessage = CreateDisconnectMessage(DisconnectReason.MaxConnectionPerAddressReached);
                errorMsg = new ArraySegment<byte>(disconnectMessage.ToArray());
                return false;
            }
            errorMsg = null;
            return true;
        }

        private Message CreateDisconnectMessage(DisconnectReason reason)
        {
            byte[] data;
            switch (reason)
            {
                case DisconnectReason.MaxConnectionReached:
                case DisconnectReason.MaxConnectionPerAddressReached:
                    data = GetRandomConnectedPeers(10).ToByteArray();
                    break;
                default:
                    data = new byte[0];
                    break;
            }
            var payload = DisconnectPayload.Create(reason, data);
            var message = Message.Create(MessageCommand.Disconnect, payload);
            return message;
        }

        public static Props Props(NeoSystem system)
        {
            return Akka.Actor.Props.Create(() => new LocalNode(system));
        }

        protected override Props ProtocolProps(object connection, IPEndPoint remote, IPEndPoint local)
        {
            return RemoteNode.Props(system, connection, remote, local);
        }
    }
}
