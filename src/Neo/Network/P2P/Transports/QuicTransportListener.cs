// Copyright (C) 2015-2025 The Neo Project.
//
// QuicTransportListener.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Net;
using System.Net.Quic;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Network.P2P.Transports
{
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("windows")]
    internal sealed class QuicTransportListener : IAsyncDisposable
    {
        private readonly QuicListener _listener;
        private readonly X509Certificate2 _certificate;

        private QuicTransportListener(QuicListener listener, X509Certificate2 certificate)
        {
            _listener = listener;
            _certificate = certificate;
        }

        public IPEndPoint ListenEndPoint => (IPEndPoint)_listener.LocalEndPoint;

        public static async Task<QuicTransportListener> ListenAsync(IPEndPoint listenEndPoint, CancellationToken cancellationToken)
        {
            var cert = QuicTransport.CreateSelfSignedCertificate();
            var listener = await QuicListener.ListenAsync(
                QuicTransport.CreateListenerOptions(listenEndPoint, cert),
                cancellationToken).ConfigureAwait(false);

            return new QuicTransportListener(listener, cert);
        }

        public async Task<QuicTransportConnection> AcceptAsync(CancellationToken cancellationToken)
        {
            var connection = await _listener.AcceptConnectionAsync(cancellationToken).ConfigureAwait(false);
            return QuicTransportConnection.FromAcceptedConnection(connection);
        }

        public async ValueTask DisposeAsync()
        {
            await _listener.DisposeAsync().ConfigureAwait(false);
            _certificate.Dispose();
        }
    }
}
