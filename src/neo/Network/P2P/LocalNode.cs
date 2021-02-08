using Akka.Actor;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;

namespace Neo.Network.P2P
{
    public class LocalNode : Peer
    {
        public class GetSnapshot { }
        public class Snapshot { public IReadOnlyDictionary<IActorRef, RemoteNode> RemoteNodes { get; init; } public IReadOnlyCollection<IPEndPoint> UnconnectedPeers { get; init; } }
        public class RelayDirectly { public IInventory Inventory; }
        public class SendDirectly { public IInventory Inventory; }

        public const uint ProtocolVersion = 0;
        private const int MaxCountFromSeedList = 5;
        private readonly IPEndPoint[] SeedList;

        private readonly NeoSystem system;
        internal readonly ConcurrentDictionary<IActorRef, RemoteNode> RemoteNodes = new ConcurrentDictionary<IActorRef, RemoteNode>();

        public static readonly uint Nonce;
        public static string UserAgent { get; set; }

        static LocalNode()
        {
            Random rand = new Random();
            Nonce = (uint)rand.Next();
            UserAgent = $"/{Assembly.GetExecutingAssembly().GetName().Name}:{Assembly.GetExecutingAssembly().GetVersion()}/";
        }

        public LocalNode(NeoSystem system)
        {
            this.system = system;
            this.SeedList = new IPEndPoint[system.Settings.SeedList.Length];

            // Start dns resolution in parallel
            string[] seedList = system.Settings.SeedList;
            for (int i = 0; i < seedList.Length; i++)
            {
                int index = i;
                Task.Run(() => SeedList[index] = GetIpEndPoint(seedList[index]));
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
        private void BroadcastMessage(Message message) => SendToRemoteNodes(message);

        /// <summary>
        /// Send message to all the RemoteNodes connected to other nodes, faster than ActorSelection.
        /// </summary>
        private void SendToRemoteNodes(object message)
        {
            foreach (var connection in RemoteNodes.Keys)
            {
                connection.Tell(message);
            }
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

        internal static IPEndPoint GetIpEndPoint(string hostAndPort)
        {
            if (string.IsNullOrEmpty(hostAndPort)) return null;

            try
            {
                string[] p = hostAndPort.Split(':');
                return GetIPEndpointFromHostPort(p[0], int.Parse(p[1]));
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Check the new connection <br/>
        /// If it is equal to the Nonce of local or any remote node, it'll return false, else we'll return true and update the Listener address of the connected remote node.
        /// </summary>
        /// <param name="actor">Remote node actor</param>
        /// <param name="node">Remote node</param>
        internal bool AllowNewConnection(IActorRef actor, RemoteNode node)
        {
            if (node.Version.Magic != system.Settings.Magic) return false;
            if (node.Version.Nonce == Nonce) return false;

            // filter duplicate connections
            foreach (var other in RemoteNodes.Values)
                if (other != node && other.Remote.Address.Equals(node.Remote.Address) && other.Version?.Nonce == node.Version.Nonce)
                    return false;

            if (node.Remote.Port != node.ListenerTcpPort && node.ListenerTcpPort != 0)
                ConnectedPeers.TryUpdate(actor, node.Listener, node.Remote);

            return true;
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
            if (!ConnectedPeers.IsEmpty)
            {
                BroadcastMessage(MessageCommand.GetAddr);
            }
            else
            {
                // Will call AddPeers with default SeedList set cached on <see cref="ProtocolSettings"/>.
                // It will try to add those, sequentially, to the list of currently unconnected ones.

                Random rand = new Random();
                AddPeers(SeedList.Where(u => u != null).OrderBy(p => rand.Next()).Take(count));
            }
        }

        protected override void OnReceive(object message)
        {
            base.OnReceive(message);
            switch (message)
            {
                case GetSnapshot _:
                    Sender.Tell(new Snapshot
                    {
                        RemoteNodes = RemoteNodes,
                        UnconnectedPeers = UnconnectedPeers
                    });
                    break;
                case Message msg:
                    BroadcastMessage(msg);
                    break;
                case RelayDirectly relay:
                    OnRelayDirectly(relay.Inventory);
                    break;
                case SendDirectly send:
                    OnSendDirectly(send.Inventory);
                    break;
            }
        }

        private void OnRelayDirectly(IInventory inventory)
        {
            var message = new RemoteNode.Relay { Inventory = inventory };
            // When relaying a block, if the block's index is greater than 'LastBlockIndex' of the RemoteNode, relay the block;
            // otherwise, don't relay.
            if (inventory is Block block)
            {
                foreach (KeyValuePair<IActorRef, RemoteNode> kvp in RemoteNodes)
                {
                    if (block.Index > kvp.Value.LastBlockIndex)
                        kvp.Key.Tell(message);
                }
            }
            else
                SendToRemoteNodes(message);
        }

        private void OnSendDirectly(IInventory inventory) => SendToRemoteNodes(inventory);

        protected override void OnTcpConnected(IActorRef connection)
        {
            connection.Tell(new RemoteNode.StartProtocol());
        }

        public static Props Props(NeoSystem system)
        {
            return Akka.Actor.Props.Create(() => new LocalNode(system));
        }

        protected override Props ProtocolProps(object connection, IPEndPoint remote, IPEndPoint local)
        {
            return RemoteNode.Props(system, this, connection, remote, local);
        }
    }
}
