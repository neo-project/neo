// Copyright (C) 2015-2026 The Neo Project.
//
// VersionPayload.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.Extensions.Collections;
using Neo.Extensions.IO;
using Neo.IO;
using Neo.Network.P2P.Capabilities;
using Neo.Wallets;

namespace Neo.Network.P2P.Payloads;

/// <summary>
/// Sent when a connection is established.
/// </summary>
public class VersionPayload : ISerializable
{
    /// <summary>
    /// Indicates the maximum number of capabilities contained in a <see cref="VersionPayload"/>.
    /// </summary>
    public const int MaxCapabilities = 32;

    /// <summary>
    /// The magic number of the network.
    /// </summary>
    public uint Network;

    /// <summary>
    /// The protocol version of the node.
    /// </summary>
    public uint Version;

    /// <summary>
    /// The time when connected to the node (UTC).
    /// </summary>
    public uint Timestamp;

    /// <summary>
    /// Represents the public key associated with this node as an elliptic curve point.
    /// </summary>
    public required ECPoint NodeKey;

    /// <summary>
    /// Represents the unique identifier for the node as a 256-bit unsigned integer.
    /// </summary>
    public required UInt256 NodeId;

    /// <summary>
    /// A <see cref="string"/> used to identify the client software of the node.
    /// </summary>
    public required string UserAgent;

    /// <summary>
    /// True if allow compression
    /// </summary>
    public bool AllowCompression;

    /// <summary>
    /// The capabilities of the node.
    /// </summary>
    public required NodeCapability[] Capabilities;

    /// <summary>
    /// The digital signature of the payload.
    /// </summary>
    public required byte[] Signature;

    public int Size =>
        sizeof(uint) +              // Network
        sizeof(uint) +              // Version
        sizeof(uint) +              // Timestamp
        NodeKey.Size +              // NodeKey
        UInt256.Length +            // NodeId
        UserAgent.GetVarSize() +    // UserAgent
        Capabilities.GetVarSize() + // Capabilities
        Signature.GetVarSize();     // Signature

    /// <summary>
    /// Creates a new instance of the <see cref="VersionPayload"/> class.
    /// </summary>
    /// <param name="protocol">The <see cref="ProtocolSettings"/> of the network.</param>
    /// <param name="nodeKey">The <see cref="ECPoint"/> used to identify the node.</param>
    /// <param name="userAgent">The <see cref="string"/> used to identify the client software of the node.</param>
    /// <param name="capabilities">The capabilities of the node.</param>
    /// <returns></returns>
    public static VersionPayload Create(ProtocolSettings protocol, KeyPair nodeKey, string userAgent, params NodeCapability[] capabilities)
    {
        uint timestamp = DateTime.UtcNow.ToTimestamp();
        UInt256 nodeId = nodeKey.PublicKey.GetNodeId(protocol);

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write(protocol.Network);
        writer.Write(LocalNode.ProtocolVersion);
        writer.Write(timestamp);
        writer.Write(nodeKey.PublicKey);
        writer.Write(nodeId);
        writer.WriteVarString(userAgent);
        writer.Write(capabilities);
        byte[] signature = Crypto.Sign(ms.ToArray(), nodeKey.PrivateKey);

        var ret = new VersionPayload
        {
            Network = protocol.Network,
            Version = LocalNode.ProtocolVersion,
            Timestamp = timestamp,
            NodeKey = nodeKey.PublicKey,
            NodeId = nodeId,
            UserAgent = userAgent,
            Capabilities = capabilities,
            Signature = signature,
            // Computed
            AllowCompression = !capabilities.Any(u => u is DisableCompressionCapability)
        };

        return ret;
    }

    void ISerializable.Deserialize(ref MemoryReader reader)
    {
        Network = reader.ReadUInt32();
        Version = reader.ReadUInt32();
        Timestamp = reader.ReadUInt32();
        NodeKey = reader.ReadSerializable<ECPoint>();
        NodeId = reader.ReadSerializable<UInt256>();
        UserAgent = reader.ReadVarString(1024);

        // Capabilities
        Capabilities = new NodeCapability[reader.ReadVarInt(MaxCapabilities)];
        for (int x = 0, max = Capabilities.Length; x < max; x++)
            Capabilities[x] = NodeCapability.DeserializeFrom(ref reader);
        var capabilities = Capabilities.Where(c => c is not UnknownCapability);
        if (capabilities.Select(p => p.Type).Distinct().Count() != capabilities.Count())
            throw new FormatException("Duplicating capabilities are included");

        Signature = reader.ReadVarMemory().ToArray();
        AllowCompression = !capabilities.Any(u => u is DisableCompressionCapability);
    }

    void ISerializable.Serialize(BinaryWriter writer)
    {
        writer.Write(Network);
        writer.Write(Version);
        writer.Write(Timestamp);
        writer.Write(NodeKey);
        writer.Write(NodeId);
        writer.WriteVarString(UserAgent);
        writer.Write(Capabilities);
        writer.WriteVarBytes(Signature);
    }

    public bool Verify(ProtocolSettings protocol)
    {
        if (NodeId != NodeKey.GetNodeId(protocol)) return false;
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write(Network);
        writer.Write(Version);
        writer.Write(Timestamp);
        writer.Write(NodeKey);
        writer.Write(NodeId);
        writer.WriteVarString(UserAgent);
        writer.Write(Capabilities);
        return Crypto.VerifySignature(ms.ToArray(), Signature, NodeKey);
    }
}
