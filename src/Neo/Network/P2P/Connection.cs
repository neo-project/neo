// Copyright (C) 2015-2025 The Neo Project.
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
using Serilog;
using System;
using System.Net;

namespace Neo.Network.P2P
{
    /// <summary>
    /// Represents a connection of the P2P network.
    /// </summary>
    public abstract class Connection : UntypedActor
    {
        internal class Close { public bool Abort; }
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
        private readonly IActorRef tcp;
        private bool disconnected = false;

        // Serilog logger instance - initialized in constructor with context
        private readonly ILogger _log;

        /// <summary>
        /// Initializes a new instance of the <see cref="Connection"/> class.
        /// </summary>
        /// <param name="connection">The underlying connection object.</param>
        /// <param name="remote">The address of the remote node.</param>
        /// <param name="local">The address of the local node.</param>
        protected Connection(object connection, IPEndPoint remote, IPEndPoint local)
        {
            // Initialize logger in constructor since we need the dynamic values
            _log = Log.ForContext(GetType()).ForContext("Remote", remote).ForContext("Local", local);
            _log?.Debug("Connection actor created");

            Remote = remote;
            Local = local;
            timer = Context.System.Scheduler.ScheduleTellOnceCancelable(TimeSpan.FromSeconds(connectionTimeoutLimitStart), Self, new Close { Abort = true }, ActorRefs.NoSender);
            _log?.Debug("Initial connection timer scheduled ({Timeout} seconds)", connectionTimeoutLimitStart);
            switch (connection)
            {
                case IActorRef tcp:
                    this.tcp = tcp;
                    break;
                // Add handling or logging for other connection types if necessary
                default:
                    _log?.Warning("Connection created with unexpected underlying connection type: {ConnectionType}", connection?.GetType().Name ?? "null");
                    break;
            }
        }

        /// <summary>
        /// Disconnect from the remote node.
        /// </summary>
        /// <param name="abort">Indicates whether the TCP ABORT command should be sent.</param>
        public void Disconnect(bool abort = false)
        {
            _log?.Information("Disconnecting connection (Abort: {Abort})", abort);
            disconnected = true;
            if (tcp != null)
            {
                tcp.Tell(abort ? Tcp.Abort.Instance : Tcp.Close.Instance);
            }
            Context.Stop(Self);
        }

        /// <summary>
        /// Called when a TCP ACK message is received.
        /// </summary>
        protected virtual void OnAck()
        {
            _log?.Verbose("TCP Ack received");
        }

        /// <summary>
        /// Called when data is received.
        /// </summary>
        /// <param name="data">The received data.</param>
        protected abstract void OnData(ByteString data);

        protected override void OnReceive(object message)
        {
            _log?.Verbose("Connection received message: {MessageType}", message.GetType().Name);
            switch (message)
            {
                case Close close:
                    _log?.Information("Close message received (Abort: {Abort})", close.Abort);
                    Disconnect(close.Abort);
                    break;
                case Ack _:
                    OnAck();
                    break;
                case Tcp.Received received:
                    // Logging done in OnReceived
                    OnReceived(received.Data);
                    break;
                case Tcp.ConnectionClosed closed:
                    _log?.Information("TCP connection closed: {CloseReason}", closed.ToString()); // Log reason if available
                    Context.Stop(Self);
                    break;
                default:
                    _log?.Warning("Connection received unknown message type: {MessageType}", message.GetType().Name);
                    Unhandled(message);
                    break;
            }
        }

        private void OnReceived(ByteString data)
        {
            timer.CancelIfNotNull();
            timer = Context.System.Scheduler.ScheduleTellOnceCancelable(TimeSpan.FromSeconds(connectionTimeoutLimit), Self, new Close { Abort = true }, ActorRefs.NoSender);
            _log?.Verbose("Received {DataLength} bytes, rescheduling connection timer ({Timeout} seconds)", data.Count, connectionTimeoutLimit);
            try
            {
                OnData(data);
            }
            catch (Exception ex) // Catch specific exceptions if possible
            {
                _log?.Error(ex, "Error processing received data, aborting connection");
                Disconnect(true);
            }
        }

        protected override void PostStop()
        {
            _log?.Information("Connection actor stopped");
            if (!disconnected)
            {
                _log?.Debug("Sending TCP Close on PostStop because not already disconnected");
                tcp?.Tell(Tcp.Close.Instance);
            }
            timer.CancelIfNotNull();
            _log?.Debug("Connection timer cancelled on PostStop");
            base.PostStop();
        }

        /// <summary>
        /// Sends data to the remote node.
        /// </summary>
        /// <param name="data"></param>
        protected void SendData(ByteString data)
        {
            _log?.Verbose("Sending {DataLength} bytes", data.Count);
            if (tcp != null)
            {
                tcp.Tell(Tcp.Write.Create(data, Ack.Instance));
            }
            else
            {
                _log?.Warning("Attempted to SendData, but TCP actor ref is null");
            }
        }
    }
}
