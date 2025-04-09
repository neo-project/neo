// Copyright (C) 2015-2025 The Neo Project.
//
// Peer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Akka.IO;
using Neo.Extensions;
using Serilog;
using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Neo.Network.P2P
{
    /// <summary>
    /// Actor used to manage the connections of the local node.
    /// </summary>
    public abstract class Peer : UntypedActor
    {
        /// <summary>
        /// Sent to <see cref="Peer"/> to add more unconnected peers.
        /// </summary>
        public class Peers
        {
            /// <summary>
            /// The unconnected peers to be added.
            /// </summary>
            public IEnumerable<IPEndPoint> EndPoints { get; init; }
        }

        /// <summary>
        /// Sent to <see cref="Peer"/> to connect to a remote node.
        /// </summary>
        public class Connect
        {
            /// <summary>
            /// The address of the remote node.
            /// </summary>
            public IPEndPoint EndPoint { get; init; }

            /// <summary>
            /// Indicates whether the remote node is trusted. A trusted node will always be connected.
            /// </summary>
            public bool IsTrusted { get; init; }
        }

        private class Timer { }

        /// <summary>
        /// The default value for enable compression.
        /// </summary>
        public const bool DefaultEnableCompression = true;

        /// <summary>
        /// The default minimum number of desired connections.
        /// </summary>
        public const int DefaultMinDesiredConnections = 10;

        /// <summary>
        /// The default maximum number of desired connections.
        /// </summary>
        public const int DefaultMaxConnections = DefaultMinDesiredConnections * 4;

        private static readonly IActorRef tcp_manager = Context.System.Tcp();
        private IActorRef tcp_listener;
        private ICancelable timer;

        private static readonly HashSet<IPAddress> localAddresses = new();
        private readonly Dictionary<IPAddress, int> ConnectedAddresses = new();

        /// <summary>
        /// A dictionary that stores the connected nodes.
        /// </summary>
        protected readonly ConcurrentDictionary<IActorRef, IPEndPoint> ConnectedPeers = new();

        /// <summary>
        /// A set that stores the peers received from other nodes.
        /// If the number of desired connections is not enough, first try to connect with the peers from this set.
        /// </summary>
        protected ImmutableHashSet<IPEndPoint> UnconnectedPeers = ImmutableHashSet<IPEndPoint>.Empty;

        /// <summary>
        /// When a TCP connection request is sent to a peer, the peer will be added to the set.
        /// If a Tcp.Connected or a Tcp.CommandFailed (with TCP.Command of type Tcp.Connect) is received, the related peer will be removed.
        /// </summary>
        protected ImmutableHashSet<IPEndPoint> ConnectingPeers = ImmutableHashSet<IPEndPoint>.Empty;

        /// <summary>
        /// A hash set to store the trusted nodes. A trusted node will always be connected.
        /// </summary>
        protected HashSet<IPAddress> TrustedIpAddresses { get; } = new();

        /// <summary>
        /// The port listened by the local Tcp server.
        /// </summary>
        public int ListenerTcpPort { get; private set; }

        /// <summary>
        /// Indicates the maximum number of connections with the same address.
        /// </summary>
        public int MaxConnectionsPerAddress { get; private set; } = 3;

        /// <summary>
        /// Indicates the minimum number of desired connections.
        /// </summary>
        public int MinDesiredConnections { get; private set; } = DefaultMinDesiredConnections;

        /// <summary>
        /// Indicates if the compression is enabled.
        /// </summary>
        public bool EnableCompression { get; private set; } = DefaultEnableCompression;

        /// <summary>
        /// Indicates the maximum number of connections.
        /// </summary>
        public int MaxConnections { get; private set; } = DefaultMaxConnections;

        /// <summary>
        /// Indicates the maximum number of unconnected peers stored in <see cref="UnconnectedPeers"/>.
        /// </summary>
        protected int UnconnectedMax { get; } = 1000;

        /// <summary>
        /// Indicates the maximum number of pending connections.
        /// </summary>
        protected virtual int ConnectingMax
        {
            get
            {
                var allowedConnecting = MinDesiredConnections * 4;
                allowedConnecting = MaxConnections != -1 && allowedConnecting > MaxConnections
                    ? MaxConnections : allowedConnecting;
                return allowedConnecting - ConnectedPeers.Count;
            }
        }

        // Serilog logger instance
        private readonly ILogger _log = Log.ForContext<Peer>(); // Note: Derived classes might want their own specific logger

        static Peer()
        {
            localAddresses.UnionWith(NetworkInterface.GetAllNetworkInterfaces().SelectMany(p => p.GetIPProperties().UnicastAddresses).Select(p => p.Address.UnMap()));
        }

        /// <summary>
        /// Tries to add a set of peers to the immutable ImmutableHashSet of UnconnectedPeers.
        /// </summary>
        /// <param name="peers">Peers that the method will try to add (union) to (with) UnconnectedPeers.</param>
        protected internal void AddPeers(IEnumerable<IPEndPoint> peers)
        {
            if (UnconnectedPeers.Count < UnconnectedMax)
            {
                // Do not select peers to be added that are already on the ConnectedPeers
                // If the address is the same, the ListenerTcpPort should be different
                var originalCount = peers.Count(); // Materialize for logging
                peers = peers.Where(p => (p.Port != ListenerTcpPort || !localAddresses.Contains(p.Address)) && !ConnectedPeers.Values.Contains(p));
                var filteredCount = peers.Count(); // Materialize again after filtering
                if (originalCount != filteredCount)
                    _log.Verbose("Filtered incoming peer list: {FilteredCount}/{OriginalCount} added (duplicates/self/connected ignored)", filteredCount, originalCount);

                if (filteredCount > 0)
                {
                    ImmutableInterlocked.Update(ref UnconnectedPeers, p => p.Union(peers));
                    _log.Debug("Added {PeerCount} peers to unconnected list. Total unconnected: {TotalUnconnectedCount}", filteredCount, UnconnectedPeers.Count);
                }
            }
            else
            {
                _log.Debug("Unconnected peer list full ({Count}/{Max}), ignoring new peers", UnconnectedPeers.Count, UnconnectedMax);
            }
        }

        /// <summary>
        /// Tries to connect the a remote peer.
        /// </summary>
        /// <param name="endPoint">The address of the remote peer.</param>
        /// <param name="isTrusted">Indicates whether the remote node is trusted. A trusted node will always be connected.</param>
        protected void ConnectToPeer(IPEndPoint endPoint, bool isTrusted = false)
        {
            endPoint = endPoint.UnMap();
            _log.Debug("Attempting connection to {EndPoint} (Trusted: {IsTrusted})", endPoint, isTrusted);

            if (endPoint.Port == ListenerTcpPort && localAddresses.Contains(endPoint.Address))
            {
                _log.Debug("Skipping connection to {EndPoint}: Cannot connect to self", endPoint);
                return;
            }

            if (isTrusted) TrustedIpAddresses.Add(endPoint.Address);

            if (ConnectedAddresses.TryGetValue(endPoint.Address, out int count) && count >= MaxConnectionsPerAddress)
            {
                _log.Debug("Skipping connection to {EndPoint}: Max connections per address ({Count}/{Max}) reached", endPoint, count, MaxConnectionsPerAddress);
                return;
            }
            if (ConnectedPeers.Values.Contains(endPoint))
            {
                _log.Debug("Skipping connection to {EndPoint}: Already connected", endPoint);
                return;
            }
            ImmutableInterlocked.Update(ref ConnectingPeers, p =>
            {
                if (p.Contains(endPoint))
                {
                    _log.Verbose("Skipping connection to {EndPoint}: Already connecting", endPoint);
                    return p;
                }
                if (p.Count >= ConnectingMax && !isTrusted)
                {
                    _log.Warning("Cannot connect to {EndPoint}: Max pending connections ({Count}/{Max}) reached", endPoint, p.Count, ConnectingMax);
                    return p;
                }
                _log.Information("Initiating TCP connection to {EndPoint}", endPoint);
                tcp_manager.Tell(new Tcp.Connect(endPoint));
                return p.Add(endPoint);
            });
        }

        private static bool IsIntranetAddress(IPAddress address)
        {
            byte[] data = address.MapToIPv4().GetAddressBytes();
            uint value = BinaryPrimitives.ReadUInt32BigEndian(data);
            return (value & 0xff000000) == 0x0a000000 || (value & 0xff000000) == 0x7f000000 || (value & 0xfff00000) == 0xac100000 || (value & 0xffff0000) == 0xc0a80000 || (value & 0xffff0000) == 0xa9fe0000;
        }

        /// <summary>
        /// Called for asking for more peers.
        /// </summary>
        /// <param name="count">Number of peers that are being requested.</param>
        protected abstract void NeedMorePeers(int count);

        protected override void OnReceive(object message)
        {
            // Base OnReceive does not log, logging specific messages
            switch (message)
            {
                case ChannelsConfig config:
                    _log.Information("Received ChannelsConfig, starting peer...");
                    OnStart(config);
                    break;
                case Timer _:
                    // Timer event might be frequent, log inside OnTimer if needed
                    OnTimer();
                    break;
                case Peers peers:
                    var peerCount = peers.EndPoints?.Count() ?? 0;
                    _log.Debug("Received Peers message with {PeerCount} endpoints", peerCount);
                    if (peerCount > 0) AddPeers(peers.EndPoints);
                    break;
                case Connect connect:
                    _log.Debug("Received Connect message for {EndPoint} (Trusted: {IsTrusted})", connect.EndPoint, connect.IsTrusted);
                    ConnectToPeer(connect.EndPoint, connect.IsTrusted);
                    break;
                case Tcp.Connected connected:
                    var remote = ((IPEndPoint)connected.RemoteAddress).UnMap();
                    var local = ((IPEndPoint)connected.LocalAddress).UnMap();
                    _log.Information("TCP connection received: Remote={RemoteEndPoint}, Local={LocalEndPoint}", remote, local);
                    OnTcpConnected(remote, local);
                    break;
                case Tcp.Bound bound:
                    _log.Information("TCP listener bound to {LocalAddress}", bound.LocalAddress);
                    tcp_listener = Sender;
                    break;
                case Tcp.CommandFailed commandFailed:
                    _log.Warning("TCP command failed: {Command}", commandFailed.Cmd);
                    OnTcpCommandFailed(commandFailed.Cmd);
                    break;
                case Terminated terminated:
                    _log.Information("Actor terminated: {ActorRef}", terminated.ActorRef);
                    OnTerminated(terminated.ActorRef);
                    break;
                default:
                    // Log unhandled messages if necessary, might indicate an issue
                    _log.Warning("Received unknown message type: {MessageType}", message.GetType().Name);
                    Unhandled(message);
                    break;
            }
        }

        private void OnStart(ChannelsConfig config)
        {
            ListenerTcpPort = config.Tcp?.Port ?? 0;
            EnableCompression = config.EnableCompression;
            MinDesiredConnections = config.MinDesiredConnections;
            MaxConnections = config.MaxConnections;
            MaxConnectionsPerAddress = config.MaxConnectionsPerAddress;
            _log.Information("Peer started. Settings: [ListenerPort={Port}] [Compression={Compression}] [MinConn={Min}] [MaxConn={Max}] [MaxPerIP={MaxIP}]",
                ListenerTcpPort, EnableCompression, MinDesiredConnections, MaxConnections, MaxConnectionsPerAddress);

            // schedule time to trigger `OnTimer` event every TimerMillisecondsInterval ms
            timer = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(TimeSpan.Zero, TimeSpan.FromSeconds(5), Context.Self, new Timer(), ActorRefs.NoSender);
            // UPnP logging
            bool upnpEnabled = (ListenerTcpPort > 0)
                && localAddresses.All(p => !p.IsIPv4MappedToIPv6 || IsIntranetAddress(p));
            if (upnpEnabled)
            {
                _log.Information("Attempting UPnP discovery...");
                if (UPnP.Discover())
                {
                    _log.Information("UPnP discovery successful.");
                    try
                    {
                        var externalIp = UPnP.GetExternalIP();
                        _log.Information("UPnP external IP: {ExternalIP}", externalIp);
                        localAddresses.Add(externalIp);

                        if (ListenerTcpPort > 0)
                        {
                            _log.Information("Attempting UPnP port forwarding for TCP port {Port}...", ListenerTcpPort);
                            UPnP.ForwardPort(ListenerTcpPort, ProtocolType.Tcp, "NEO Tcp");
                            _log.Information("UPnP port forwarding successful for TCP port {Port}", ListenerTcpPort);
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Warning(ex, "UPnP operation failed");
                    }
                }
                else
                {
                    _log.Warning("UPnP discovery failed.");
                }
            }
            else
            {
                _log.Information("UPnP skipped (Not listening or local address configuration)");
            }

            if (ListenerTcpPort > 0)
            {
                _log.Information("Attempting to bind TCP listener to {BindAddress}", config.Tcp);
                tcp_manager.Tell(new Tcp.Bind(Self, config.Tcp, options: new[] { new Inet.SO.ReuseAddress(true) }));
            }
        }

        /// <summary>
        /// Will be triggered when a Tcp.Connected message is received.
        /// If the conditions are met, the remote endpoint will be added to ConnectedPeers.
        /// Increase the connection number with the remote endpoint by one.
        /// </summary>
        /// <param name="remote">The remote endpoint of TCP connection.</param>
        /// <param name="local">The local endpoint of TCP connection.</param>
        private void OnTcpConnected(IPEndPoint remote, IPEndPoint local)
        {
            ImmutableInterlocked.Update(ref ConnectingPeers, p => p.Remove(remote));
            _log.Debug("Processing incoming TCP connection from {RemoteEndPoint}", remote);
            if (MaxConnections != -1 && ConnectedPeers.Count >= MaxConnections && !TrustedIpAddresses.Contains(remote.Address))
            {
                _log.Warning("Rejecting connection from {RemoteEndPoint}: Max connections ({Count}/{Max}) reached", remote, ConnectedPeers.Count, MaxConnections);
                Sender.Tell(Tcp.Abort.Instance);
                return;
            }

            ConnectedAddresses.TryGetValue(remote.Address, out int count);
            if (count >= MaxConnectionsPerAddress)
            {
                _log.Warning("Rejecting connection from {RemoteEndPoint}: Max connections per address ({Count}/{Max}) reached", remote, count, MaxConnectionsPerAddress);
                Sender.Tell(Tcp.Abort.Instance);
            }
            else
            {
                _log.Information("Accepting connection from {RemoteEndPoint}", remote);
                ConnectedAddresses[remote.Address] = count + 1;
                IActorRef connection = Context.ActorOf(ProtocolProps(Sender, remote, local), $"connection_{Guid.NewGuid()}");
                Context.Watch(connection);
                Sender.Tell(new Tcp.Register(connection));
                ConnectedPeers.TryAdd(connection, remote);
                _log.Debug("Connection added. Total connected: {ConnectedCount}, Connecting: {ConnectingCount}, Unconnected: {UnconnectedCount}", ConnectedPeers.Count, ConnectingPeers.Count, UnconnectedPeers.Count);
                OnTcpConnected(connection);
            }
        }

        /// <summary>
        /// Called when a Tcp connection is established.
        /// </summary>
        /// <param name="connection">The connection actor.</param>
        protected virtual void OnTcpConnected(IActorRef connection)
        {
        }

        /// <summary>
        /// Will be triggered when a Tcp.CommandFailed message is received.
        /// If it's a Tcp.Connect command, remove the related endpoint from ConnectingPeers.
        /// </summary>
        /// <param name="cmd">Tcp.Command message/event.</param>
        private void OnTcpCommandFailed(Tcp.Command cmd)
        {
            switch (cmd)
            {
                case Tcp.Connect connect:
                    var remoteEp = ((IPEndPoint)connect.RemoteAddress).UnMap();
                    _log.Warning("TCP connect command failed for {RemoteEndPoint}", remoteEp);
                    ImmutableInterlocked.Update(ref ConnectingPeers, p => p.Remove(remoteEp));
                    break;
                case Tcp.Bind bind:
                    _log.Error("TCP bind command failed for {LocalAddress}. Shutting down node.", bind.LocalAddress);
                    // Consider more graceful shutdown? Context.Stop(Self);
                    Context.System.Terminate(); // Terminate the entire actor system
                    break;
                default:
                    _log.Warning("TCP command failed: {CommandType}", cmd.GetType().Name);
                    break;
            }
        }

        private void OnTerminated(IActorRef actorRef)
        {
            if (ConnectedPeers.TryRemove(actorRef, out IPEndPoint endPoint))
            {
                _log.Information("Connection closed: {RemoteEndPoint}", endPoint);
                ConnectedAddresses.TryGetValue(endPoint.Address, out int count);
                if (count > 0) count--;
                if (count == 0)
                {
                    _log.Debug("Removing address {Address} from connected address count", endPoint.Address);
                    ConnectedAddresses.Remove(endPoint.Address);
                }
                else
                {
                    _log.Debug("Decremented connection count for address {Address} to {Count}", endPoint.Address, count);
                    ConnectedAddresses[endPoint.Address] = count;
                }
                _log.Debug("Connection removed. Total connected: {ConnectedCount}, Connecting: {ConnectingCount}, Unconnected: {UnconnectedCount}", ConnectedPeers.Count, ConnectingPeers.Count, UnconnectedPeers.Count);
            }
        }

        private void OnTimer()
        {
            _log.Verbose("Peer timer tick");
            // Check if the number of desired connections is already enough
            if (ConnectedPeers.Count >= MinDesiredConnections)
            {
                _log.Verbose("Skipping timer logic: Sufficient connections ({ConnectedCount}/{MinDesiredConnections})", ConnectedPeers.Count, MinDesiredConnections);
                return;
            }

            int needed = MinDesiredConnections - ConnectedPeers.Count;
            // If there aren't available UnconnectedPeers, it triggers an abstract implementation of NeedMorePeers
            if (UnconnectedPeers.Count == 0)
            {
                _log.Information("No unconnected peers available, requesting more ({NeededCount} needed)", needed);
                NeedMorePeers(needed);
                return; // Wait for NeedMorePeers to provide candidates
            }

            // Use new Random() for netstandard2.1
            Random rand = new Random();
            int connectCount = Math.Min(UnconnectedPeers.Count, needed);
            connectCount = Math.Min(connectCount, ConnectingMax - ConnectingPeers.Count);
            if (connectCount <= 0)
            {
                _log.Debug("Skipping timer connections: Either enough connections ({ConnectedCount}/{MinDesiredConnections}) or max connecting ({ConnectingCount}/{ConnectingMax})",
                    ConnectedPeers.Count, MinDesiredConnections, ConnectingPeers.Count, ConnectingMax);
                return;
            }

            IPEndPoint[] endpoints = UnconnectedPeers.OrderBy(u => rand.Next()).Take(connectCount).ToArray();
            _log.Information("Attempting to connect to {ConnectCount} peers from unconnected list (Needed: {NeededCount}, Available: {UnconnectedCount})", endpoints.Length, needed, UnconnectedPeers.Count);
            ImmutableInterlocked.Update(ref UnconnectedPeers, p => p.Except(endpoints));
            foreach (IPEndPoint endpoint in endpoints)
            {
                ConnectToPeer(endpoint);
            }
        }

        protected override void PostStop()
        {
            _log.Information("Peer stopping...");
            timer?.Cancel(); // Use safe cancel
            tcp_listener?.Tell(Tcp.Unbind.Instance);
            _log.Information("Peer stopped.");
            base.PostStop();
        }

        /// <summary>
        /// Gets a <see cref="Akka.Actor.Props"/> object used for creating the protocol actor.
        /// </summary>
        /// <param name="connection">The underlying connection object.</param>
        /// <param name="remote">The address of the remote node.</param>
        /// <param name="local">The address of the local node.</param>
        /// <returns>The <see cref="Akka.Actor.Props"/> object used for creating the protocol actor.</returns>
        protected abstract Props ProtocolProps(object connection, IPEndPoint remote, IPEndPoint local);
    }
}
