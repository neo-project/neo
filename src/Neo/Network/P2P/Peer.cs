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
using Neo.Collections;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Neo.Network.P2P;

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

    private class Timer { }

    private static readonly IActorRef s_tcpManager = Context.System.Tcp();

    private IActorRef? _tcpListener;

    private ICancelable _timer = null!;

    private static readonly HashSet<IPAddress> s_localAddresses = new();

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
            s_tcpManager.Tell(new Tcp.Connect(endPoint));
            return p.Add(endPoint);
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
        }
    }

    private void OnStart(ChannelsConfig config)
    {
        ListenerTcpPort = config.Tcp?.Port ?? 0;
        Config = config;

        // schedule time to trigger `OnTimer` event every TimerMillisecondsInterval ms
        _timer = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(0, 5000, Context.Self, new Timer(), ActorRefs.NoSender);
        if ((ListenerTcpPort > 0)
            && s_localAddresses.All(p => !p.IsIPv4MappedToIPv6 || IsIntranetAddress(p))
            && UPnP.Discover())
        {
            try
            {
                s_localAddresses.Add(UPnP.GetExternalIP());

                if (ListenerTcpPort > 0) UPnP.ForwardPort(ListenerTcpPort, ProtocolType.Tcp, "NEO Tcp");
            }
            catch { }
        }
        if (ListenerTcpPort > 0)
        {
            s_tcpManager.Tell(new Tcp.Bind(Self, config.Tcp, options: [new Inet.SO.ReuseAddress(true)]));
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
