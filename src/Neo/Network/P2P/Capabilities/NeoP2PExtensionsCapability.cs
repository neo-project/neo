// Copyright (C) 2015-2025 The Neo Project.
//
// NeoP2PExtensionsCapability.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Buffers.Binary;

namespace Neo.Network.P2P.Capabilities
{
    [Flags]
    internal enum NeoP2PExtensions : byte
    {
        None = 0,
        Quic = 1 << 0,
    }

    internal readonly record struct NeoP2PExtensionsData(NeoP2PExtensions Extensions, ushort QuicPort);

    internal static class NeoP2PExtensionsCapability
    {
        private const byte CapabilityVersion = 1;
        private static ReadOnlySpan<byte> Magic => "NEOQ"u8;

        // 4 bytes magic + 1 version + 1 flags + 2 quicPort
        private const int PayloadLengthV1 = 8;

        public static UnknownCapability Create(NeoP2PExtensionsData data)
        {
            Span<byte> payload = stackalloc byte[PayloadLengthV1];
            Magic.CopyTo(payload);
            payload[4] = CapabilityVersion;
            payload[5] = (byte)data.Extensions;
            BinaryPrimitives.WriteUInt16LittleEndian(payload[6..8], data.QuicPort);

            return new UnknownCapability(NodeCapabilityType.Extension0) { Data = payload.ToArray() };
        }

        public static bool TryParse(NodeCapability[] capabilities, out NeoP2PExtensionsData data)
        {
            foreach (var capability in capabilities)
            {
                if (capability is not UnknownCapability unknown) continue;
                if (unknown.Type != NodeCapabilityType.Extension0) continue;

                if (TryParse(unknown.Data.Span, out data))
                    return true;
            }

            data = default;
            return false;
        }

        public static bool TryParse(ReadOnlySpan<byte> payload, out NeoP2PExtensionsData data)
        {
            if (payload.Length < PayloadLengthV1)
            {
                data = default;
                return false;
            }

            if (!payload[..4].SequenceEqual(Magic))
            {
                data = default;
                return false;
            }

            if (payload[4] != CapabilityVersion)
            {
                data = default;
                return false;
            }

            var extensions = (NeoP2PExtensions)payload[5];
            ushort quicPort = BinaryPrimitives.ReadUInt16LittleEndian(payload[6..8]);

            data = new NeoP2PExtensionsData(extensions, quicPort);
            return true;
        }
    }
}
