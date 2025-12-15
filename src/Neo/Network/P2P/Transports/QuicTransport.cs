// Copyright (C) 2015-2025 The Neo Project.
//
// QuicTransport.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Neo.Network.P2P.Transports
{
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("windows")]
    internal static class QuicTransport
    {
        internal const string TargetHost = "neo-p2p";
        internal static readonly SslApplicationProtocol ApplicationProtocol = new(TargetHost);

        [SupportedOSPlatformGuard("linux")]
        [SupportedOSPlatformGuard("macos")]
        [SupportedOSPlatformGuard("windows")]
        public static bool IsSupported => QuicListener.IsSupported;

        public static X509Certificate2 CreateSelfSignedCertificate()
        {
            // Prefer RSA here for broad compatibility across TLS providers (including Windows Schannel / MsQuic).
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest($"CN={TargetHost}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
            request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
                new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, // Server Authentication
                false));

            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddDnsName(TargetHost);
            request.CertificateExtensions.Add(sanBuilder.Build());

            var notBefore = DateTimeOffset.UtcNow.AddDays(-1);
            var notAfter = notBefore.AddYears(1);
            using var cert = request.CreateSelfSigned(notBefore, notAfter);

            // Re-import as PFX so the private key is available to the OS TLS stack (notably on Windows).
            var pfx = cert.Export(X509ContentType.Pfx);
            if (OperatingSystem.IsWindows())
            {
                // MsQuic/Schannel can be picky about how the private key is loaded. Prefer machine key storage,
                // with a fallback to the user key store when machine storage isn't available.
                var machineFlags = X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet;
                var userFlags = X509KeyStorageFlags.Exportable | X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.PersistKeySet;

                try
                {
                    var loaded = X509CertificateLoader.LoadPkcs12(pfx, password: null, keyStorageFlags: machineFlags);
                    if (loaded.HasPrivateKey)
                        return loaded;
                    loaded.Dispose();
                }
                catch (CryptographicException)
                {
                }

                var fallback = X509CertificateLoader.LoadPkcs12(pfx, password: null, keyStorageFlags: userFlags);
                if (!fallback.HasPrivateKey)
                    throw new CryptographicException("Failed to create a QUIC server certificate with a usable private key.");
                return fallback;
            }

            return X509CertificateLoader.LoadPkcs12(
                pfx,
                password: null,
                keyStorageFlags: X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);
        }

        public static QuicListenerOptions CreateListenerOptions(IPEndPoint listenEndPoint, X509Certificate2 certificate)
        {
            return new QuicListenerOptions
            {
                ListenEndPoint = listenEndPoint,
                ApplicationProtocols = new List<SslApplicationProtocol> { ApplicationProtocol },
                ConnectionOptionsCallback = (_, _, _) => ValueTask.FromResult(new QuicServerConnectionOptions
                {
                    DefaultCloseErrorCode = 0,
                    DefaultStreamErrorCode = 0,
                    MaxInboundBidirectionalStreams = 256,
                    ServerAuthenticationOptions = CreateServerAuthenticationOptions(certificate),
                })
            };
        }

        public static QuicClientConnectionOptions CreateClientOptions(IPEndPoint remoteEndPoint)
        {
            return new QuicClientConnectionOptions
            {
                RemoteEndPoint = remoteEndPoint,
                DefaultCloseErrorCode = 0,
                DefaultStreamErrorCode = 0,
                MaxInboundBidirectionalStreams = 256,
                ClientAuthenticationOptions = CreateClientAuthenticationOptions(),
            };
        }

        private static SslServerAuthenticationOptions CreateServerAuthenticationOptions(X509Certificate2 certificate)
        {
            return new SslServerAuthenticationOptions
            {
                ApplicationProtocols = new List<SslApplicationProtocol> { ApplicationProtocol },
                ServerCertificate = certificate,
            };
        }

        private static SslClientAuthenticationOptions CreateClientAuthenticationOptions()
        {
            return new SslClientAuthenticationOptions
            {
                ApplicationProtocols = new List<SslApplicationProtocol> { ApplicationProtocol },
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                TargetHost = TargetHost,
                RemoteCertificateValidationCallback = ValidateServerCertificate,
            };
        }

        private static bool ValidateServerCertificate(object _, X509Certificate? certificate, X509Chain? __, SslPolicyErrors sslPolicyErrors)
        {
            if (certificate is not X509Certificate2 cert)
                return false;

            // Expect a self-signed cert for now (encrypts transport, but does not provide strong peer authentication).
            // Keep at least basic checks so we don't fully disable validation.
            if (sslPolicyErrors is not (SslPolicyErrors.None or SslPolicyErrors.RemoteCertificateChainErrors))
                return false;

            var now = DateTimeOffset.UtcNow;
            if (now < cert.NotBefore || now > cert.NotAfter)
                return false;

            if (!cert.Subject.Contains($"CN={TargetHost}", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }
    }
}
