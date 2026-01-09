// Copyright (C) 2015-2026 The Neo Project.
//
// Helper.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.Extensions.IO;
using Neo.Network.P2P.Payloads;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace Neo.Network.P2P;

/// <summary>
/// A helper class for <see cref="IVerifiable"/>.
/// </summary>
public static class Helper
{
    private const int SignDataLength = sizeof(uint) + UInt256.Length;

    /// <summary>
    /// Calculates the hash of a <see cref="IVerifiable"/>.
    /// </summary>
    /// <param name="verifiable">The <see cref="IVerifiable"/> object to hash.</param>
    /// <returns>The hash of the object.</returns>
    public static UInt256 CalculateHash(this IVerifiable verifiable)
    {
        using MemoryStream ms = new();
        using BinaryWriter writer = new(ms);
        verifiable.SerializeUnsigned(writer);
        writer.Flush();
        return new UInt256(ms.ToArray().Sha256());
    }

    /// <summary>
    /// Tries to get the hash of the transaction.
    /// If this IVerifiable is not valid, the hash may be <see langword="null"/>.
    /// </summary>
    /// <param name="verifiable">The <see cref="IVerifiable"/> object to hash.</param>
    /// <param name="hash">The hash of the transaction.</param>
    /// <returns>
    /// <see langword="true"/> if the hash was successfully retrieved; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool TryGetHash(this IVerifiable verifiable, [NotNullWhen(true)] out UInt256? hash)
    {
        try
        {
            hash = verifiable.Hash;
            return true;
        }
        catch
        {
            hash = null;
            return false;
        }
    }

    /// <summary>
    /// Gets the data of a <see cref="IVerifiable"/> object to be hashed.
    /// </summary>
    /// <param name="verifiable">The <see cref="IVerifiable"/> object to hash.</param>
    /// <param name="network">The magic number of the network.</param>
    /// <returns>The data to hash.</returns>
    public static byte[] GetSignData(this IVerifiable verifiable, uint network) => GetSignData(verifiable.Hash, network);

    /// <summary>
    /// Gets the data to be hashed.
    /// </summary>
    /// <param name="messageHash">Message.</param>
    /// <param name="network">The magic number of the network.</param>
    /// <returns>The data to hash.</returns>
    public static byte[] GetSignData(this UInt256 messageHash, uint network)
    {
        /* Same as:
        using MemoryStream ms = new();
        using BinaryWriter writer = new(ms);
        writer.Write(network);
        writer.Write(verifiable.Hash);
        writer.Flush();
        return ms.ToArray();
        */

        var buffer = new byte[SignDataLength];

        BinaryPrimitives.WriteUInt32LittleEndian(buffer, network);
        messageHash.Serialize(buffer.AsSpan(sizeof(uint)));

        return buffer;
    }

    /// <summary>
    /// Computes a unique node identifier based on the specified public key and protocol settings.
    /// </summary>
    /// <remarks>The returned node identifier is deterministic for a given public key and protocol settings.
    /// This method is typically used to generate consistent node IDs for distributed hash table (DHT) operations in the
    /// NEO network.</remarks>
    /// <param name="pubkey">The public key of the node for which to generate the identifier.</param>
    /// <param name="protocol">The protocol settings that provide network-specific information used in the identifier calculation.</param>
    /// <returns>A 256-bit hash value that uniquely identifies the node within the specified network.</returns>
    public static UInt256 GetNodeId(this ECPoint pubkey, ProtocolSettings protocol)
    {
        const string prefix = "NEO_DHT_NODEID";
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.WriteFixedString(prefix, prefix.Length);
        writer.Write(protocol.Network);
        writer.Write(pubkey);
        return Crypto.Hash256(ms.ToArray());
    }
}
