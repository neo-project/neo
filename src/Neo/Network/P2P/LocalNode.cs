// Copyright (C) 2015-2025 The Neo Project.
//
// LocalNode.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Neo.IO;
using Neo.Network.P2P.Capabilities;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Native;
using Serilog;
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
    /// <summary>
    /// Actor used to manage the connections of the local node.
    /// </summary>
    public class LocalNode : Peer
    {
        /// <summary>
        /// Sent to <see cref="LocalNode"/> to relay an <see cref="IInventory"/>.
        /// </summary>
        public class RelayDirectly { public IInventory Inventory; }

        /// <summary>
        /// Sent to <see cref="LocalNode"/> to send an <see cref="IInventory"/>.
        /// </summary>
        public class SendDirectly { public IInventory Inventory; }

        /// <summary>
        /// Sent to <see cref="LocalNode"/> to request for an instance of <see cref="LocalNode"/>.
        /// </summary>
        public class GetInstance { }

        /// <summary>
        /// Indicates the protocol version of the local node.
        /// </summary>
        public const uint ProtocolVersion = 0;

        private const int MaxCountFromSeedList = 5;
        private readonly IPEndPoint[] SeedList;

        private readonly NeoSystem system;
        internal readonly ConcurrentDictionary<IActorRef, RemoteNode> RemoteNodes = new();

        /// <summary>
        /// Indicates the number of connected nodes.
        /// </summary>
        public int ConnectedCount => RemoteNodes.Count;

        /// <summary>
        /// Indicates the number of unconnected nodes. When the number of connections is not enough, it will automatically connect to these nodes.
        /// </summary>
        public int UnconnectedCount => UnconnectedPeers.Count;

        /// <summary>
        /// The random number used to identify the local node.
        /// </summary>
        public static readonly uint Nonce;

        /// <summary>
        /// The identifier of the client software of the local node.
        /// </summary>
        public static string UserAgent { get; set; }

        // Serilog logger instance
        private readonly ILogger _log = Log.ForContext<LocalNode>();

        static LocalNode()
        {
            Random rand = new();
            Nonce = (uint)rand.Next();
            UserAgent = $"/{Assembly.GetExecutingAssembly().GetName().Name}:{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)}/";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalNode"/> class.
        /// </summary>
        /// <param name="system">The <see cref="NeoSystem"/> object that contains the <see cref="LocalNode"/>.</param>
        public LocalNode(NeoSystem system)
        {
            this.system = system;
            _log.Information("LocalNode created (Nonce={Nonce}, UserAgent='{UserAgent}')", Nonce, UserAgent);
            SeedList = new IPEndPoint[system.Settings.SeedList.Length];

            // Start dns resolution in parallel
            string[] seedList = system.Settings.SeedList;
            _log.Information("Starting DNS resolution for {SeedCount} seed nodes...", seedList.Length);
            for (int i = 0; i < seedList.Length; i++)
            {
                int index = i;
                Task.Run(() =>
                {
                    SeedList[index] = GetIpEndPoint(seedList[index]);
                    if (SeedList[index] != null)
                        _log.Debug("Resolved seed node {Host} to {EndPoint}", seedList[index], SeedList[index]);
                    else
                        _log.Warning("Failed to resolve seed node {Host}", seedList[index]);
                });
            }
        }

        /// <summary>
        /// Packs a MessageCommand to a full Message with an optional ISerializable payload.
        /// Forwards it to <see cref="BroadcastMessage(Message)"/>.
        /// </summary>
        /// <param name="command">The message command to be packed.</param>
        /// <param name="payload">Optional payload to be Serialized along the message.</param>
        private void BroadcastMessage(MessageCommand command, ISerializable payload = null)
        {
            BroadcastMessage(Message.Create(command, payload));
        }

        /// <summary>
        /// Broadcast a message to all connected nodes.
        /// </summary>
        /// <param name="message">The message to be broadcast.</param>
        private void BroadcastMessage(Message message) => SendToRemoteNodes(message);

        /// <summary>
        /// Send message to all the RemoteNodes connected to other nodes, faster than ActorSelection.
        /// </summary>
        private void SendToRemoteNodes(object message)
        {
            // Logging every message sent might be too verbose, consider logging only specific message types if needed
            _log.Verbose("Broadcasting message {MessageType} to {NodeCount} remote nodes", message.GetType().Name, RemoteNodes.Count);
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
        /// Checks the new connection.
        /// If it is equal to the nonce of local or any remote node, it'll return false,
        /// else we'll return true and update the Listener address of the connected remote node.
        /// </summary>
        /// <param name="actor">Remote node actor.</param>
        /// <param name="node">Remote node object.</param>
        /// <returns><see langword="true"/> if the new connection is allowed; otherwise, <see langword="false"/>.</returns>
        public bool AllowNewConnection(IActorRef actor, RemoteNode node)
        {
            if (node.Version.Network != system.Settings.Network)
            {
                _log.Warning("Connection denied from {RemoteEndPoint}: Incorrect network {Network}, expected {ExpectedNetwork}",
                    node.Remote, node.Version.Network, system.Settings.Network);
                return false;
            }
            if (node.Version.Nonce == Nonce)
            {
                _log.Warning("Connection denied from {RemoteEndPoint}: Connected to self (Nonce={Nonce})", node.Remote, Nonce);
                return false;
            }

            // filter duplicate connections
            foreach (var other in RemoteNodes.Values)
            {
                if (other != node && other.Remote.Address.Equals(node.Remote.Address) && other.Version?.Nonce == node.Version.Nonce)
                {
                    _log.Warning("Connection denied from {RemoteEndPoint}: Duplicate connection detected (Nonce={Nonce})", node.Remote, node.Version.Nonce);
                    return false;
                }
            }

            if (node.Remote.Port != node.ListenerTcpPort && node.ListenerTcpPort != 0)
            {
                _log.Debug("Updating connected peer record for {ActorRef}: Listener={Listener}, Remote={Remote}", actor, node.Listener, node.Remote);
                ConnectedPeers.TryUpdate(actor, node.Listener, node.Remote);
            }

            _log.Information("Allowed new connection from {RemoteEndPoint} (UserAgent='{UserAgent}')", node.Remote, node.Version?.UserAgent);
            return true;
        }

        /// <summary>
        /// Gets the connected remote nodes.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<RemoteNode> GetRemoteNodes()
        {
            return RemoteNodes.Values;
        }

        /// <summary>
        /// Gets the unconnected nodes.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IPEndPoint> GetUnconnectedPeers()
        {
            return UnconnectedPeers;
        }

        /// <summary>
        /// Performs a broadcast with the command <see cref="MessageCommand.GetAddr"/>,
        /// which, eventually, tells all known connections.
        /// If there are no connected peers it will try with the default,
        /// respecting <see cref="MaxCountFromSeedList"/> limit.
        /// </summary>
        /// <param name="count">Number of peers that are being requested.</param>
        protected override void NeedMorePeers(int count)
        {
            count = Math.Max(count, MaxCountFromSeedList);
            _log.Debug("Need more peers (requesting {Count})", count);
            if (!ConnectedPeers.IsEmpty)
            {
                _log.Debug("Broadcasting GetAddr message");
                BroadcastMessage(MessageCommand.GetAddr);
            }
            else
            {
                var rand = new Random();
                var seeds = SeedList.Where(u => u != null).OrderBy(p => rand.Next()).Take(count).ToArray();
                _log.Information("No connected peers, attempting to connect to {SeedCount} seed nodes", seeds.Length);
                AddPeers(seeds);
            }
        }

        protected override void OnReceive(object message)
        {
            base.OnReceive(message);
            switch (message)
            {
                case Message msg:
                    // Avoid logging potentially high-frequency broadcast messages here
                    BroadcastMessage(msg);
                    break;
                case RelayDirectly relay:
                    _log.Debug("Received RelayDirectly for {InvType} {InvHash}", relay.Inventory.InventoryType, relay.Inventory.Hash);
                    OnRelayDirectly(relay.Inventory);
                    break;
                case SendDirectly send:
                    _log.Debug("Received SendDirectly for {InvType} {InvHash}", send.Inventory.InventoryType, send.Inventory.Hash);
                    OnSendDirectly(send.Inventory);
                    break;
                case GetInstance _:
                    Sender.Tell(this);
                    break;
            }
        }

        private void OnRelayDirectly(IInventory inventory)
        {
            // Logging inside the loop might be too verbose if many nodes
            _log.Verbose("Relaying inventory {InvType} {InvHash} directly...", inventory.InventoryType, inventory.Hash);
            var message = new RemoteNode.Relay { Inventory = inventory };
            if (inventory is Block block)
            {
                foreach (KeyValuePair<IActorRef, RemoteNode> kvp in RemoteNodes)
                {
                    if (block.Index > kvp.Value.LastBlockIndex)
                    {
                        _log.Verbose("Relaying block {BlockIndex} to {RemoteEndPoint}", block.Index, kvp.Value.Remote);
                        kvp.Key.Tell(message);
                    }
                }
            }
            else
            {
                SendToRemoteNodes(message); // SendToRemoteNodes already logs verbosely
            }
        }

        public NodeCapability[] GetNodeCapabilities()
        {
            var capabilities = new List<NodeCapability>
            {
                new FullNodeCapability(NativeContract.Ledger.CurrentIndex(system.StoreView))
                // Wait for 3.9
                // new ArchivalNodeCapability()
            };

            if (!EnableCompression)
            {
                capabilities.Add(new DisableCompressionCapability());
            }

            if (ListenerTcpPort > 0) capabilities.Add(new ServerCapability(NodeCapabilityType.TcpServer, (ushort)ListenerTcpPort));

            return [.. capabilities];
        }

        private void OnSendDirectly(IInventory inventory)
        {
            _log.Verbose("Sending inventory {InvType} {InvHash} directly...", inventory.InventoryType, inventory.Hash);
            SendToRemoteNodes(inventory); // SendToRemoteNodes already logs verbosely
        }

        protected override void OnTcpConnected(IActorRef connection)
        {
            _log.Information("TCP connection established: {Connection}", connection);
            connection.Tell(new RemoteNode.StartProtocol());
        }

        /// <summary>
        /// Gets a <see cref="Akka.Actor.Props"/> object used for creating the <see cref="LocalNode"/> actor.
        /// </summary>
        /// <param name="system">The <see cref="NeoSystem"/> object that contains the <see cref="LocalNode"/>.</param>
        /// <returns>The <see cref="Akka.Actor.Props"/> object used for creating the <see cref="LocalNode"/> actor.</returns>
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
