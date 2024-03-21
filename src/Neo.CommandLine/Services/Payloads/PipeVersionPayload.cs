// Copyright (C) 2015-2024 The Neo Project.
//
// PipeVersionPayload.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using System;
using System.IO;
using System.Security.Cryptography;

namespace Neo.CommandLine.Services.Payloads
{
    internal sealed class PipeVersionPayload : ISerializable, IEquatable<PipeVersionPayload>
    {
        public uint Network { get; private set; }
        public int Version { get; private set; }
        public uint Nonce { get; private set; }
        public long Timestamp { get; private set; }

        public static PipeVersionPayload Create(int version, uint network) =>
            new()
            {
                Version = version,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Network = network,
                Nonce = BitConverter.ToUInt32(RandomNumberGenerator.GetBytes(sizeof(uint))),
            };

        #region ISerializable

        int ISerializable.Size =>
            sizeof(int) +   // Version
            sizeof(long) +  // Timestamp
            sizeof(uint) +  // Network
            sizeof(uint);   // Nonce

        void ISerializable.Deserialize(ref MemoryReader reader)
        {
            Version = reader.ReadInt32();

            var data = reader.ReadMemory(sizeof(long));
            Timestamp = BitConverter.ToInt64(data.Span);

            Network = reader.ReadUInt32();
            Nonce = reader.ReadUInt32();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(Timestamp);
            writer.Write(Network);
            writer.Write(Nonce);
        }

        #endregion

        #region IEquatable

        public bool Equals(PipeVersionPayload? other)
        {
            if (other is null) return false;
            return Version == other.Version &&
                Timestamp == other.Timestamp &&
                Network == other.Network &&
                Nonce == other.Nonce;
        }

        public override bool Equals(object? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other as PipeVersionPayload);
        }

        public override int GetHashCode() =>
            HashCode.Combine(Version, Timestamp, Network, Nonce);

        #endregion

        public override string ToString() =>
            string.Format(
                "Name={0} Version={1}, Timestamp={2}, Network={3}, Nonce={4}",
                GetType().Name, Version, Timestamp, Network, Nonce);
    }
}
