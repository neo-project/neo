// Copyright (C) 2015-2026 The Neo Project.
//
// BeaconPartial.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography;
using Neo.Cryptography.BLS12_381;
using Neo.IO;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    /// <summary>
    /// One consensus node's BLS12-381 G1 partial for the current DRB round.
    /// </summary>
    public sealed class BeaconPartial : ISerializable
    {
        public const int SignatureSize = RandomBeacon.G1CompressedSize;

        /// <summary>
        /// dBFT validator index (0-based). Combine uses Shamir x = index + 1.
        /// </summary>
        public byte ValidatorIndex { get; set; }

        public byte[] Signature
        {
            get;
            set
            {
                if (value is null || value.Length != SignatureSize)
                    throw new ArgumentException($"Partial signature must be {SignatureSize} bytes.", nameof(value));
                try
                {
                    G1Affine.FromCompressed(value);
                }
                catch (FormatException)
                {
                    throw;
                }
                catch (ArithmeticException ex)
                {
                    throw new FormatException("Partial signature is not a compressed G1 point.", ex);
                }
                field = value;
            }
        } = G1Affine.Generator.ToCompressed();

        public int Size => sizeof(byte) + SignatureSize;

        /// <summary>
        /// Parses <see cref="Signature"/> as a compressed G1 affine point.
        /// </summary>
        public G1Affine GetSignature()
            => G1Affine.FromCompressed(Signature);

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(ValidatorIndex);
            writer.Write(Signature);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            ValidatorIndex = reader.ReadByte();
            Signature = reader.ReadMemory(SignatureSize).ToArray();
        }
    }
}
