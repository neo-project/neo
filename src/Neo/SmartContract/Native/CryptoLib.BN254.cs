// Copyright (C) 2015-2026 The Neo Project.
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
using Neo.Cryptography.BN254Curve;
using Neo.VM.Types;
using System;
using Bn254Math = Neo.Cryptography.BN254Curve.Bn254Pairing;

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
                Bn254Gt gt => gt.ToArray(),
                _ => throw new ArgumentException("BN254 type mismatch")
            };
        }

        [ContractMethod(Hardfork.HF_Gorgon, CpuFee = 1 << 19)]
        public static InteropInterface Bn254Deserialize(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);
            return data.Length switch
            {
                BN254.G1EncodedLength when BN254.TryDeserializeG1(data, out _) => new InteropInterface(new Bn254G1(data)),
                BN254.G2EncodedLength when BN254.TryDeserializeG2(data, out _) => new InteropInterface(new Bn254G2(data)),
                BN254.GtEncodedLength when BN254.TryDeserializeGt(data, out var gt) => new InteropInterface(new Bn254Gt(gt)),
                _ => throw new ArgumentException("Invalid BN254 point length", nameof(data))
            };
        }

        [ContractMethod(Hardfork.HF_Gorgon, CpuFee = 1 << 19)]
        public static InteropInterface Bn254Add(InteropInterface x, InteropInterface y)
        {
            var left = x.GetInterface<object>();
            var right = y.GetInterface<object>();

            // G1 group law: elliptic-curve point addition over F_p.
            if (left is Bn254G1 g1Left && right is Bn254G1 g1Right)
            {
                if (!g1Left.TryGetPoint(out var pointX) || !g1Right.TryGetPoint(out var pointY))
                    throw new ArgumentException("Invalid BN254 point data");

                return new InteropInterface(new Bn254G1(BN254.SerializeG1(pointX.Add(pointY))));
            }

            // GT group law: the target group is the order-r subgroup of F_p12*, so its group
            // operation is field multiplication. Composing pairing results this way lets a script
            // build the Groth16 left-hand product e(A,B)*e(C,D)*... before a single equality check.
            if (left is Bn254Gt gtLeft && right is Bn254Gt gtRight)
                return new InteropInterface(new Bn254Gt(gtLeft.Value.Multiply(gtRight.Value)));

            throw new ArgumentException("BN254 type mismatch");
        }

        [ContractMethod(Hardfork.HF_Gorgon, CpuFee = 1 << 5)]
        public static bool Bn254Equal(InteropInterface x, InteropInterface y)
        {
            return (x.GetInterface<object>(), y.GetInterface<object>()) switch
            {
                (Bn254G1 a, Bn254G1 b) => a.SequenceEqual(b),
                (Bn254G2 a, Bn254G2 b) => a.SequenceEqual(b),
                (Bn254Gt a, Bn254Gt b) => a.ValueEquals(b),
                _ => throw new ArgumentException("BN254 type mismatch")
            };
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

            return new InteropInterface(new Bn254G1(BN254.SerializeG1(g1.Multiply(mul))));
        }

        /// <summary>
        /// Computes the optimal-ate pairing e(g1, g2) and returns the target-group (GT) element as an
        /// interop value, mirroring <c>Bls12381Pairing</c>. The result is not a boolean: callers compose
        /// several pairings with <see cref="Bn254Add"/> (GT multiplication) and compare the product
        /// against another pairing with <see cref="Bn254Equal"/>, which is exactly the shape of a
        /// Groth16 verification. Each invocation performs a single pairing, so the cost is a fixed
        /// <c>1 &lt;&lt; 23</c> and an N-pairing check naturally costs N times this fee.
        /// </summary>
        [ContractMethod(Hardfork.HF_Gorgon, CpuFee = 1 << 23)]
        public static InteropInterface Bn254Pairing(InteropInterface g1, InteropInterface g2)
        {
            var point1 = GetBn254G1(g1);
            var point2 = GetBn254G2(g2);

            if (!point1.TryGetPoint(out var p) || !point2.TryGetPoint(out var q))
                throw new ArgumentException("Invalid BN254 point data");

            return new InteropInterface(new Bn254Gt(Bn254Math.Pairing(p, q)));
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

            public bool TryGetPoint(out EcPoint<Fp> point)
            {
                return BN254.TryDeserializeG1(Encoded, out point);
            }

            public byte[] ToArray() => (byte[])_encoded.Clone();

            public bool SequenceEqual(Bn254G1 other) => Encoded.SequenceEqual(other.Encoded);
        }

        private sealed class Bn254G2
        {
            private readonly byte[] _encoded;

            public Bn254G2(ReadOnlySpan<byte> encoded)
            {
                _encoded = encoded.ToArray();
            }

            public ReadOnlySpan<byte> Encoded => _encoded;

            public bool TryGetPoint(out EcPoint<Fp2> point)
            {
                return BN254.TryDeserializeG2(Encoded, out point);
            }

            public byte[] ToArray() => (byte[])_encoded.Clone();

            public bool SequenceEqual(Bn254G2 other) => Encoded.SequenceEqual(other.Encoded);
        }

        private sealed class Bn254Gt
        {
            private readonly Fp12 _value;

            public Bn254Gt(Fp12 value)
            {
                _value = value;
            }

            public Fp12 Value => _value;

            public byte[] ToArray() => BN254.SerializeGt(_value);

            public bool ValueEquals(Bn254Gt other) => _value.Equals(other._value);
        }
    }
}
