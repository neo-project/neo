// Copyright (C) 2015-2026 The Neo Project.
//
// Connection.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Akka.IO;
using Neo.Exceptions;
using System.Net;

namespace Neo.Network.P2P;

/// <summary>
/// Represents a connection of the P2P network.
/// </summary>
public abstract class Connection : UntypedActor
{
    internal class Close { public DisconnectReason Reason; }
    internal class Ack : Tcp.Event { public static Ack Instance = new(); }

    /// <summary>
    /// connection initial timeout (in seconds) before any package has been accepted.
    /// </summary>
    private const int connectionTimeoutLimitStart = 10;

    /// <summary>
    /// connection timeout (in seconds) after every `OnReceived(ByteString data)` event.
    /// </summary>
    private const int connectionTimeoutLimit = 60;

    /// <summary>
    /// The address of the remote node.
    /// </summary>
    public IPEndPoint Remote { get; }

    /// <summary>
    /// The address of the local node.
    /// </summary>
    public IPEndPoint Local { get; }

    private ICancelable timer;
    private readonly IActorRef? tcp;
    private bool disconnected = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="Connection"/> class.
    /// </summary>
    /// <param name="connection">The underlying connection object.</param>
    /// <param name="remote">The address of the remote node.</param>
    /// <param name="local">The address of the local node.</param>
    protected Connection(object connection, IPEndPoint remote, IPEndPoint local)
    {
        Remote = remote;
        Local = local;
        timer = Context.System.Scheduler.ScheduleTellOnceCancelable(TimeSpan.FromSeconds(connectionTimeoutLimitStart), Self, new Close { Reason = DisconnectReason.Timeout }, ActorRefs.NoSender);
        switch (connection)
        {
            case IActorRef tcp:
                this.tcp = tcp;
                break;
        }
    }

    /// <summary>
    /// Disconnect from the remote node.
    /// </summary>
    /// <param name="reason">The reason for the disconnection.</param>
    public void Disconnect(DisconnectReason reason = DisconnectReason.Close)
    {
        disconnected = true;
        tcp?.Tell(reason == DisconnectReason.Close ? Tcp.Close.Instance : Tcp.Abort.Instance);
        OnDisconnect(reason);
        Context.Stop(Self);
    }

    /// <summary>
    /// Called when a TCP ACK message is received.
    /// </summary>
    protected virtual void OnAck()
    {
    }

    /// <summary>
    /// Invoked when a disconnect operation occurs, allowing derived classes to handle cleanup or custom logic.
    /// </summary>
    /// <remarks>Override this method in a derived class to implement custom behavior when a disconnect
    /// occurs. This method is called regardless of whether the disconnect is graceful or due to an abort.</remarks>
    /// <param name="reason">The reason for the disconnection.</param>
    protected virtual void OnDisconnect(DisconnectReason reason)
    {
    }

    /// <summary>
    /// Called when data is received.
    /// </summary>
    /// <param name="data">The received data.</param>
    protected abstract void OnData(ByteString data);

    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case Close close:
                Disconnect(close.Reason);
                break;
            case Ack _:
                OnAck();
                break;
            case Tcp.Received received:
                OnReceived(received.Data);
                break;
            case Tcp.ConnectionClosed _:
                Context.Stop(Self);
                break;
        }
    }

    private void OnReceived(ByteString data)
    {
        timer.CancelIfNotNull();
        timer = Context.System.Scheduler.ScheduleTellOnceCancelable(TimeSpan.FromSeconds(connectionTimeoutLimit), Self, new Close { Reason = DisconnectReason.Timeout }, ActorRefs.NoSender);
        data.TryCatch<ByteString, Exception>(OnData, (_, _) => Disconnect(DisconnectReason.ProtocolViolation));
    }

    protected override void PostStop()
    {
        if (!disconnected)
            tcp?.Tell(Tcp.Close.Instance);
        timer.CancelIfNotNull();
        base.PostStop();
    }

    /// <summary>
    /// Sends data to the remote node.
    /// </summary>
    /// <param name="data"></param>
    protected void SendData(ByteString data)
    {
        tcp?.Tell(Tcp.Write.Create(data, Ack.Instance));
    }
}
