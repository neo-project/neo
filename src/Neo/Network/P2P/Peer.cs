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
using Neo.Network.P2P.Capabilities;
using Neo.Network.P2P.Payloads;
using Neo.Network.P2P.Transports;
using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Network.P2P
{
    /// <summary>
    /// Actor used to manage the connections of the local node.
    /// </summary>
    public abstract class Peer : UntypedActor, IWithUnboundedStash
    {
        public IStash Stash { get; set; } = null!;

        /// <summary>
        /// Sent to <see cref="Peer"/> to add more unconnected peers.
        /// </summary>
        /// <param name="EndPoints">The unconnected peers to be added.</param>
        public record Peers(IEnumerable<IPEndPoint> EndPoints);

        /// <summary>
        /// Sent to <see cref="Peer"/> to connect to a remote node.
        /// </summary>
        /// <param name="EndPoint">The address of the remote node.</param>
        /// <param name="IsTrusted">Indicates whether the remote node is trusted. A trusted node will always be connected.</param>
        public record Connect(IPEndPoint EndPoint, bool IsTrusted);

        internal record AdvertisedPeers(NetworkAddressWithTime[] AddressList);

        private record QuicListenerReady(QuicTransportListener Listener);
        private record QuicListenerFailed(Exception Exception);
        private record QuicInboundConnected(ITransportConnection Connection);
        private record QuicOutboundConnected(IPEndPoint Target, ITransportConnection Connection);
        private record QuicOutboundFailed(IPEndPoint Target);

        private class Timer { }

        private static readonly IActorRef s_tcpManager = Context.System.Tcp();

        private IActorRef? _tcpListener;

        private ICancelable _timer = null!;

        private static readonly HashSet<IPAddress> s_localAddresses = new();

        private readonly Dictionary<IPAddress, int> ConnectedAddresses = new();

        private readonly Dictionary<IPEndPoint, ushort> _quicPortsByTcpEndPoint = new();
        private readonly Dictionary<IPEndPoint, DateTime> _quicBackoffUntil = new();
        private QuicTransportListener? _quicListener;
        private CancellationTokenSource? _quicListenerCts;
        private Task? _quicAcceptLoop;

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
        /// The port listened by the local QUIC server (UDP).
        /// </summary>
        public int ListenerQuicPort { get; private set; }

        /// <summary>
        /// Channel configuration.
        /// </summary>
        public ChannelsConfig Config { get; private set; } = null!;

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
                var allowedConnecting = Config.MinDesiredConnections * 4;
                allowedConnecting = Config.MaxConnections != -1 && allowedConnecting > Config.MaxConnections
                    ? Config.MaxConnections : allowedConnecting;
                return allowedConnecting - ConnectedPeers.Count;
            }
        }

        static Peer()
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .SelectMany(p => p.GetIPProperties().UnicastAddresses)
                .Select(p => p.Address.UnMap());
            s_localAddresses.UnionWith(networkInterfaces);
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
                peers = peers.Where(p => (p.Port != ListenerTcpPort || !s_localAddresses.Contains(p.Address)) && !ConnectedPeers.Values.Contains(p));
                ImmutableInterlocked.Update(ref UnconnectedPeers, p => p.Union(peers));
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
            // If the address is the same, the ListenerTcpPort should be different, otherwise, return
            if (endPoint.Port == ListenerTcpPort && s_localAddresses.Contains(endPoint.Address)) return;

            if (isTrusted) TrustedIpAddresses.Add(endPoint.Address);
            // If connections with the peer greater than or equal to MaxConnectionsPerAddress, return.
            if (ConnectedAddresses.TryGetValue(endPoint.Address, out int count) && count >= Config.MaxConnectionsPerAddress)
                return;
            if (ConnectedPeers.Values.Contains(endPoint)) return;
            ImmutableInterlocked.Update(ref ConnectingPeers, p =>
            {
                if ((p.Count >= ConnectingMax && !isTrusted) || p.Contains(endPoint)) return p;
                if (Config.PreferQuic && QuicTransport.IsSupported && TryGetQuicTarget(endPoint, out var quicTarget))
                {
                    BeginQuicConnect(endPoint, quicTarget);
                }
                else
                {
                    s_tcpManager.Tell(new Tcp.Connect(endPoint));
                }
                return p.Add(endPoint);
            });
        }

        private bool TryGetQuicTarget(IPEndPoint tcpEndPoint, out IPEndPoint quicTarget)
        {
            quicTarget = default!;

            if (!_quicPortsByTcpEndPoint.TryGetValue(tcpEndPoint, out var quicPort) || quicPort == 0)
                return false;

            if (_quicBackoffUntil.TryGetValue(tcpEndPoint, out var until) && until > TimeProvider.Current.UtcNow)
                return false;

            quicTarget = new IPEndPoint(tcpEndPoint.Address, quicPort);
            return true;
        }

        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("macos")]
        [SupportedOSPlatform("windows")]
        private void BeginQuicConnect(IPEndPoint tcpEndPoint, IPEndPoint quicTarget)
        {
            var self = Self;
            Task.Run(async () =>
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    var connection = await QuicTransportConnection.ConnectAsync(quicTarget, cts.Token).ConfigureAwait(false);
                    self.Tell(new QuicOutboundConnected(tcpEndPoint, connection));
                }
                catch
                {
                    self.Tell(new QuicOutboundFailed(tcpEndPoint));
                }
            });
        }

        private static bool IsIntranetAddress(IPAddress address)
        {
            byte[] data = address.MapToIPv4().GetAddressBytes();
            uint value = BinaryPrimitives.ReadUInt32BigEndian(data);
            return (value & 0xff000000) == 0x0a000000 ||
                   (value & 0xff000000) == 0x7f000000 ||
                   (value & 0xfff00000) == 0xac100000 ||
                   (value & 0xffff0000) == 0xc0a80000 ||
                   (value & 0xffff0000) == 0xa9fe0000;
        }

        /// <summary>
        /// Called for asking for more peers.
        /// </summary>
        /// <param name="count">Number of peers that are being requested.</param>
        protected abstract void NeedMorePeers(int count);

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case ChannelsConfig config:
                    OnStart(config);
                    Stash.UnstashAll();
                    return;

                case Timer _:
                    if (Config is null)
                    {
                        Stash.Stash();
                        return;
                    }
                    OnTimer();
                    break;

                case Peers peers:
                    if (Config is null)
                    {
                        Stash.Stash();
                        return;
                    }
                    AddPeers(peers.EndPoints);
                    break;

                case AdvertisedPeers peers:
                    if (Config is null)
                    {
                        Stash.Stash();
                        return;
                    }
                    ProcessAdvertisedPeers(peers.AddressList);
                    break;

                case Connect connect:
                    if (Config is null)
                    {
                        Stash.Stash();
                        return;
                    }
                    ConnectToPeer(connect.EndPoint, connect.IsTrusted);
                    break;

                case Tcp.Connected connected:
                    if (Config is null)
                    {
                        Stash.Stash();
                        return;
                    }
                    if (connected.RemoteAddress is null)
                    {
                        Sender.Tell(Tcp.Abort.Instance);
                        break;
                    }
                    OnTcpConnected(((IPEndPoint)connected.RemoteAddress).UnMap(), ((IPEndPoint)connected.LocalAddress).UnMap());
                    break;

                case Tcp.Bound _:
                    if (Config is null)
                    {
                        Stash.Stash();
                        return;
                    }
                    _tcpListener = Sender;
                    break;

                case Tcp.CommandFailed commandFailed:
                    if (Config is null)
                    {
                        Stash.Stash();
                        return;
                    }
                    OnTcpCommandFailed(commandFailed.Cmd);
                    break;

                case Terminated terminated:
                    if (Config is null)
                    {
                        Stash.Stash();
                        return;
                    }
                    OnTerminated(terminated.ActorRef);
                    break;

                case QuicListenerReady ready:
                    if (QuicTransport.IsSupported)
                        OnQuicListenerReady(ready.Listener);
                    break;
                case QuicListenerFailed _:
                    // QUIC is optional, ignore and keep TCP behavior.
                    break;
                case QuicInboundConnected inbound:
                    OnQuicConnected(inbound.Connection);
                    break;
                case QuicOutboundConnected outbound:
                    OnQuicOutboundConnected(outbound.Target, outbound.Connection);
                    break;
                case QuicOutboundFailed failed:
                    OnQuicOutboundFailed(failed.Target);
                    break;
            }
        }

        private void OnStart(ChannelsConfig config)
        {
            ListenerTcpPort = config.Tcp?.Port ?? 0;
            ListenerQuicPort = 0;
            Config = config;

            // schedule time to trigger `OnTimer` event every TimerMillisecondsInterval ms
            _timer = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(0, 5000, Context.Self, new Timer(), ActorRefs.NoSender);
            if (ListenerTcpPort > 0)
            {
                s_tcpManager.Tell(new Tcp.Bind(Self, config.Tcp, options: [new Inet.SO.ReuseAddress(true)]));
            }

            if (Config.Quic != null && QuicTransport.IsSupported)
                StartQuicListener(Config.Quic);
        }

        private void ProcessAdvertisedPeers(NetworkAddressWithTime[] peers)
        {
            var endPoints = new List<IPEndPoint>(peers.Length);
            foreach (var peer in peers)
            {
                var endPoint = peer.EndPoint.UnMap();
                if (endPoint.Port <= 0) continue;

                endPoints.Add(endPoint);

                if (NeoP2PExtensionsCapability.TryParse(peer.Capabilities, out var extensions) &&
                    extensions.Extensions.HasFlag(NeoP2PExtensions.Quic) &&
                    extensions.QuicPort > 0)
                {
                    _quicPortsByTcpEndPoint[endPoint] = extensions.QuicPort;
                    _quicBackoffUntil.Remove(endPoint);
                }
            }

            AddPeers(endPoints);
        }

        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("macos")]
        [SupportedOSPlatform("windows")]
        private void StartQuicListener(IPEndPoint listenEndPoint)
        {
            _quicListenerCts?.Cancel();
            _quicListenerCts?.Dispose();
            _quicListenerCts = new CancellationTokenSource();

            var token = _quicListenerCts.Token;
            var self = Self;
            Task.Run(async () =>
            {
                try
                {
                    var listener = await QuicTransportListener.ListenAsync(listenEndPoint, token).ConfigureAwait(false);
                    self.Tell(new QuicListenerReady(listener));
                }
                catch (Exception ex)
                {
                    self.Tell(new QuicListenerFailed(ex));
                }
            }, token);
        }

        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("macos")]
        [SupportedOSPlatform("windows")]
        private void OnQuicListenerReady(QuicTransportListener listener)
        {
            _quicListener = listener;
            ListenerQuicPort = listener.ListenEndPoint.Port;

            if (_quicAcceptLoop != null) return;

            var token = _quicListenerCts?.Token ?? CancellationToken.None;
            var self = Self;
            _quicAcceptLoop = Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        var connection = await listener.AcceptAsync(token).ConfigureAwait(false);
                        self.Tell(new QuicInboundConnected(connection));
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    self.Tell(new QuicListenerFailed(ex));
                }
            }, token);
        }

        private void OnQuicConnected(ITransportConnection transport)
        {
            var remote = transport.RemoteEndPoint.UnMap();
            var local = transport.LocalEndPoint.UnMap();
            OnTransportConnected(transport, remote, local, remote);
        }

        private void OnQuicOutboundConnected(IPEndPoint targetTcpEndPoint, ITransportConnection transport)
        {
            ImmutableInterlocked.Update(ref ConnectingPeers, p => p.Remove(targetTcpEndPoint));
            var remote = transport.RemoteEndPoint.UnMap();
            var local = transport.LocalEndPoint.UnMap();
            OnTransportConnected(transport, remote, local, targetTcpEndPoint.UnMap());
        }

        private void OnQuicOutboundFailed(IPEndPoint targetTcpEndPoint)
        {
            _quicBackoffUntil[targetTcpEndPoint.UnMap()] = TimeProvider.Current.UtcNow.AddMinutes(5);
            s_tcpManager.Tell(new Tcp.Connect(targetTcpEndPoint));
        }

        private void OnTransportConnected(ITransportConnection transport, IPEndPoint remote, IPEndPoint local, IPEndPoint peerEndPoint)
        {
            if (Config is null) // OnStart is not called yet
            {
                _ = transport.CloseAsync(abort: true, CancellationToken.None);
                return;
            }

            if (Config.MaxConnections != -1 && ConnectedPeers.Count >= Config.MaxConnections && !TrustedIpAddresses.Contains(peerEndPoint.Address))
            {
                _ = transport.CloseAsync(abort: true, CancellationToken.None);
                return;
            }

            if (ConnectedPeers.Values.Contains(peerEndPoint))
            {
                _ = transport.CloseAsync(abort: true, CancellationToken.None);
                return;
            }

            ConnectedAddresses.TryGetValue(peerEndPoint.Address, out int count);
            if (count >= Config.MaxConnectionsPerAddress)
            {
                _ = transport.CloseAsync(abort: true, CancellationToken.None);
                return;
            }

            ConnectedAddresses[peerEndPoint.Address] = count + 1;
            var connection = Context.ActorOf(ProtocolProps(transport, remote, local), $"connection_{Guid.NewGuid()}");
            Context.Watch(connection);
            ConnectedPeers.TryAdd(connection, peerEndPoint);
            OnTcpConnected(connection);
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
            if (Config is null) // OnStart is not called yet
            {
                Sender.Tell(Tcp.Abort.Instance);
                return;
            }

            ImmutableInterlocked.Update(ref ConnectingPeers, p => p.Remove(remote));
            if (Config.MaxConnections != -1 && ConnectedPeers.Count >= Config.MaxConnections && !TrustedIpAddresses.Contains(remote.Address))
            {
                Sender.Tell(Tcp.Abort.Instance);
                return;
            }

            ConnectedAddresses.TryGetValue(remote.Address, out int count);
            if (count >= Config.MaxConnectionsPerAddress)
            {
                Sender.Tell(Tcp.Abort.Instance);
            }
            else
            {
                ConnectedAddresses[remote.Address] = count + 1;
                var connection = Context.ActorOf(ProtocolProps(Sender, remote, local), $"connection_{Guid.NewGuid()}");
                Context.Watch(connection);
                Sender.Tell(new Tcp.Register(connection));
                ConnectedPeers.TryAdd(connection, remote);
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
                    ImmutableInterlocked.Update(ref ConnectingPeers, p => p.Remove(((IPEndPoint)connect.RemoteAddress).UnMap()));
                    break;
            }
        }

        private void OnTerminated(IActorRef actorRef)
        {
            if (ConnectedPeers.TryRemove(actorRef, out IPEndPoint? endPoint))
            {
                ConnectedAddresses.TryGetValue(endPoint.Address, out int count);
                if (count > 0) count--;
                if (count == 0)
                    ConnectedAddresses.Remove(endPoint.Address);
                else
                    ConnectedAddresses[endPoint.Address] = count;
            }
        }

        private void OnTimer()
        {
            // Check if the number of desired connections is already enough
            if (ConnectedPeers.Count >= Config.MinDesiredConnections) return;

            // If there aren't available UnconnectedPeers, it triggers an abstract implementation of NeedMorePeers
            if (UnconnectedPeers.Count == 0)
                NeedMorePeers(Config.MinDesiredConnections - ConnectedPeers.Count);

            var endpoints = UnconnectedPeers.Sample(Config.MinDesiredConnections - ConnectedPeers.Count);
            ImmutableInterlocked.Update(ref UnconnectedPeers, p => p.Except(endpoints));
            foreach (var endpoint in endpoints)
            {
                ConnectToPeer(endpoint);
            }
        }

        protected override void PostStop()
        {
            _timer.CancelIfNotNull();
            _tcpListener?.Tell(Tcp.Unbind.Instance);
            _quicListenerCts?.Cancel();
            _quicListenerCts?.Dispose();
            _quicListenerCts = null;
            if (_quicListener != null)
            {
                IAsyncDisposable disposable = _quicListener;
                _ = disposable.DisposeAsync().AsTask();
                _quicListener = null;
            }
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
