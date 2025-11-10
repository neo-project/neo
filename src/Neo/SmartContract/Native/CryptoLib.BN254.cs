// Copyright (C) 2015-2025 The Neo Project.
//
// CryptoLib.BN254.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography;
using Neo.VM.Types;
using Nethermind.MclBindings;
using System;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native
{
    partial class CryptoLib
    {
        [ContractMethod(Hardfork.HF_Gorgon, CpuFee = 1 << 19)]
        public static byte[] Bn254Serialize(InteropInterface point)
        {
            return point.GetInterface<object>() switch
            {
                Bn254G1 g1 => g1.ToArray(),
                Bn254G2 g2 => g2.ToArray(),
                _ => throw new ArgumentException("BN254 type mismatch")
            };
        }

        [ContractMethod(Hardfork.HF_Gorgon, CpuFee = 1 << 19)]
        public static InteropInterface Bn254Deserialize(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);
            BN254.EnsureInitialized();
            return data.Length switch
            {
                BN254.G1EncodedLength when BN254.TryDeserializeG1(data, out _) => new InteropInterface(new Bn254G1(data)),
                BN254.G2EncodedLength when BN254.TryDeserializeG2(data, out _) => new InteropInterface(new Bn254G2(data)),
                _ => throw new ArgumentException("Invalid BN254 point length", nameof(data))
            };
        }

        [ContractMethod(Hardfork.HF_Gorgon, CpuFee = 1 << 19)]
        public static InteropInterface Bn254Add(InteropInterface x, InteropInterface y)
        {
            var pointX = GetBn254G1(x);
            var pointY = GetBn254G1(y);

            if (!pointX.TryGetPoint(out var g1x) || !pointY.TryGetPoint(out var g1y))
                throw new ArgumentException("Invalid BN254 point data");

            mclBnG1 result = default;
            Mcl.mclBnG1_add(ref result, g1x, g1y);
            Mcl.mclBnG1_normalize(ref result, result);

            return new InteropInterface(new Bn254G1(BN254.SerializeG1(result)));
        }

        [ContractMethod(Hardfork.HF_Gorgon, CpuFee = 1 << 21)]
        public static InteropInterface Bn254Mul(InteropInterface point, byte[] scalar)
        {
            ArgumentNullException.ThrowIfNull(scalar);
            if (!BN254.TryDeserializeScalar(scalar, out var mul))
                throw new ArgumentException("Invalid BN254 scalar", nameof(scalar));

            var source = GetBn254G1(point);
            if (!source.TryGetPoint(out var g1))
                throw new ArgumentException("Invalid BN254 point data");

            mclBnG1 result = default;
            Mcl.mclBnG1_mul(ref result, g1, mul);
            Mcl.mclBnG1_normalize(ref result, result);

            return new InteropInterface(new Bn254G1(BN254.SerializeG1(result)));
        }

        [ContractMethod(Hardfork.HF_Gorgon, CpuFee = 1 << 21)]
        public static byte[] Bn254Pairing(Array pairs)
        {
            ArgumentNullException.ThrowIfNull(pairs);

            if (pairs.Count == 0)
                return BN254.Pairing(System.Array.Empty<byte>());

            byte[] buffer = new byte[pairs.Count * BN254.PairInputLength];
            for (int i = 0; i < pairs.Count; i++)
            {
                if (pairs[i] is not Array pair || pair.Count != 2)
                    throw new ArgumentException("BN254 pairing pairs must contain g1 and g2 points");

                if (pair[0] is not InteropInterface g1Interface)
                    throw new ArgumentException("BN254 pairing requires interop points");
                if (pair[1] is not InteropInterface g2Interface)
                    throw new ArgumentException("BN254 pairing requires interop points");

                var g1 = GetBn254G1(g1Interface);
                var g2 = GetBn254G2(g2Interface);

                var g1Bytes = g1.Encoded;
                var g2Bytes = g2.Encoded;
                var slice = buffer.AsSpan(i * BN254.PairInputLength, BN254.PairInputLength);
                g1Bytes.CopyTo(slice[..g1Bytes.Length]);
                g2Bytes.CopyTo(slice[g1Bytes.Length..]);
            }

            return BN254.Pairing(buffer);
        }

        [ContractMethod(Hardfork.HF_Gorgon, CpuFee = 1 << 19, Name = "bn254_add")]
        public static byte[] Bn254AddRaw(byte[] input)
        {
            ArgumentNullException.ThrowIfNull(input);
            return BN254.Add(input);
        }

        [ContractMethod(Hardfork.HF_Gorgon, CpuFee = 1 << 21, Name = "bn254_mul")]
        public static byte[] Bn254MulRaw(byte[] input)
        {
            ArgumentNullException.ThrowIfNull(input);
            return BN254.Mul(input);
        }

        [ContractMethod(Hardfork.HF_Gorgon, CpuFee = 1 << 21, Name = "bn254_pairing")]
        public static byte[] Bn254PairingRaw(byte[] input)
        {
            ArgumentNullException.ThrowIfNull(input);
            return BN254.Pairing(input);
        }

        private static Bn254G1 GetBn254G1(InteropInterface item)
        {
            if (item.GetInterface<object>() is not Bn254G1 point)
                throw new ArgumentException("BN254 type mismatch");
            return point;
        }

        private static Bn254G2 GetBn254G2(InteropInterface item)
        {
            if (item.GetInterface<object>() is not Bn254G2 point)
                throw new ArgumentException("BN254 type mismatch");
            return point;
        }

        private sealed class Bn254G1
        {
            private readonly byte[] _encoded;

            public Bn254G1(ReadOnlySpan<byte> encoded)
            {
                _encoded = encoded.ToArray();
            }

            public ReadOnlySpan<byte> Encoded => _encoded;

            public bool TryGetPoint(out mclBnG1 point)
            {
                BN254.EnsureInitialized();
                return BN254.TryDeserializeG1(Encoded, out point);
            }

            public byte[] ToArray() => (byte[])_encoded.Clone();
        }

        private sealed class Bn254G2
        {
            private readonly byte[] _encoded;

            public Bn254G2(ReadOnlySpan<byte> encoded)
            {
                _encoded = encoded.ToArray();
            }

            public ReadOnlySpan<byte> Encoded => _encoded;

            public bool TryGetPoint(out mclBnG2 point)
            {
                BN254.EnsureInitialized();
                return BN254.TryDeserializeG2(Encoded, out point);
            }

            public byte[] ToArray() => (byte[])_encoded.Clone();
        }
    }
}
