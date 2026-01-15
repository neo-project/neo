// Copyright (C) 2015-2026 The Neo Project.
//
// CryptoLib.BLS12_381.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.BLS12_381;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using StackItem = Neo.VM.Types.StackItem;
using StackItemType = Neo.VM.Types.StackItemType;
using VMArray = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native
{
    partial class CryptoLib
    {
        private const int Bls12381MultiExpMaxPairs = 128;
        private const int Bls12381PairingMaxPairs = Bls12381MultiExpMaxPairs;
        private static readonly int Bls12FieldElementLength = Fp.Size + 16;
        private static readonly int Bls12G1EncodedLength = 2 * Bls12FieldElementLength;
        private static readonly int Bls12G2EncodedLength = 4 * Bls12FieldElementLength;
        private static readonly int Bls12ScalarLength = Scalar.Size;
        private static readonly BigInteger Bls12ScalarModulus = BigInteger.Parse(
            "73EDA753299D7D483339D80809A1D80553BDA402FFFE5BFEFFFFFFFF00000001",
            NumberStyles.AllowHexSpecifier);

        /// <summary>
        /// Serialize a bls12381 point.
        /// </summary>
        /// <param name="g">The point to be serialized.</param>
        /// <returns></returns>
        [ContractMethod(CpuFee = 1 << 19)]
        public static byte[] Bls12381Serialize(InteropInterface g)
        {
            return g.GetInterface<object>() switch
            {
                G1Affine p => p.ToCompressed(),
                G1Projective p => new G1Affine(p).ToCompressed(),
                G2Affine p => p.ToCompressed(),
                G2Projective p => new G2Affine(p).ToCompressed(),
                Gt p => p.ToArray(),
                _ => throw new ArgumentException("BLS12-381 type mismatch")
            };
        }

        /// <summary>
        /// Serialize a bls12381 point using Ethereum encoding (EIP-2537).
        /// </summary>
        /// <param name="g">The point to be serialized.</param>
        /// <returns></returns>
        [ContractMethod(Hardfork.HF_Faun, CpuFee = 1 << 19)]
        public static byte[] Bls12381SerializeEthereum(InteropInterface g)
        {
            return SerializeEthereumPoint(g.GetInterface<object>());
        }

        /// <summary>
        /// Deserialize a bls12381 point.
        /// </summary>
        /// <param name="data">The point as byte array.</param>
        /// <returns></returns>
        [ContractMethod(CpuFee = 1 << 19)]
        public static InteropInterface Bls12381Deserialize(byte[] data)
        {
            return data.Length switch
            {
                48 => new InteropInterface(G1Affine.FromCompressed(data)),
                96 => new InteropInterface(G2Affine.FromCompressed(data)),
                576 => new InteropInterface(Gt.FromBytes(data)),
                _ => throw new ArgumentException("Invalid BLS12-381 point length"),
            };
        }

        /// <summary>
        /// Deserialize a bls12381 point using Ethereum encoding (EIP-2537).
        /// </summary>
        /// <param name="data">The point as byte array.</param>
        /// <returns></returns>
        [ContractMethod(Hardfork.HF_Faun, CpuFee = 1 << 19)]
        public static InteropInterface Bls12381DeserializeEthereum(byte[] data)
        {
            return data.Length switch
            {
                var size when size == Bls12G1EncodedLength => new InteropInterface(DeserializeEthereumG1(data)),
                var size when size == Bls12G2EncodedLength => new InteropInterface(DeserializeEthereumG2(data)),
                _ => throw new ArgumentException("Invalid BLS12-381 point length"),
            };
        }

        /// <summary>
        /// Determines whether the specified points are equal.
        /// </summary>
        /// <param name="x">The first point.</param>
        /// <param name="y">Teh second point.</param>
        /// <returns><c>true</c> if the specified points are equal; otherwise, <c>false</c>.</returns>
        [ContractMethod(CpuFee = 1 << 5)]
        public static bool Bls12381Equal(InteropInterface x, InteropInterface y)
        {
            return (x.GetInterface<object>(), y.GetInterface<object>()) switch
            {
                (G1Affine p1, G1Affine p2) => p1.Equals(p2),
                (G1Projective p1, G1Projective p2) => p1.Equals(p2),
                (G2Affine p1, G2Affine p2) => p1.Equals(p2),
                (G2Projective p1, G2Projective p2) => p1.Equals(p2),
                (Gt p1, Gt p2) => p1.Equals(p2),
                _ => throw new ArgumentException("BLS12-381 type mismatch")
            };
        }

        /// <summary>
        /// Serialize a list of BLS12-381 points or (point, scalar) pairs.
        /// </summary>
        /// <param name="points">List of points or (point, scalar) pairs.</param>
        /// <returns></returns>
        [ContractMethod(Hardfork.HF_Faun, CpuFee = 1 << 19)]
        public static byte[] Bls12381SerializeList(VMArray points)
        {
            if (points.Count == 0)
                throw new ArgumentException("at least one element is required");

            var result = new List<byte>();
            if (points[0].Type == StackItemType.InteropInterface)
            {
                foreach (StackItem item in points)
                {
                    var point = GetInteropPoint(item);
                    result.AddRange(Bls12381Serialize(new InteropInterface(point)));
                }
                return result.ToArray();
            }

            foreach (StackItem item in points)
            {
                if (item is not VMArray pair)
                    throw new ArgumentException("pair must be Array or Struct");
                if (pair.Count != 2)
                    throw new ArgumentException("pair must contain point and scalar");

                var scalarItem = pair[1];
                if (scalarItem.Type != StackItemType.Integer)
                    throw new ArgumentException("scalar must be bigint");
                var point = GetInteropPoint(pair[0]);
                result.AddRange(Bls12381Serialize(new InteropInterface(point)));
                result.AddRange(SerializeScalarLittleEndian(scalarItem.GetInteger()));
            }

            return result.ToArray();
        }

        /// <summary>
        /// Serialize a list of BLS12-381 points or (point, scalar) pairs using Ethereum encoding (EIP-2537).
        /// </summary>
        /// <param name="points">List of points or (point, scalar) pairs.</param>
        /// <returns></returns>
        [ContractMethod(Hardfork.HF_Faun, CpuFee = 1 << 19)]
        public static byte[] Bls12381SerializeEthereumList(VMArray points)
        {
            if (points.Count == 0)
                throw new ArgumentException("at least one element is required");

            var result = new List<byte>();
            if (points[0].Type == StackItemType.InteropInterface)
            {
                foreach (StackItem item in points)
                {
                    var point = GetInteropPoint(item);
                    result.AddRange(SerializeEthereumPoint(point));
                }
                return result.ToArray();
            }

            foreach (StackItem item in points)
            {
                if (item is not VMArray pair)
                    throw new ArgumentException("pair must be Array or Struct");
                if (pair.Count != 2)
                    throw new ArgumentException("pair must contain point and scalar");

                var scalarItem = pair[1];
                if (scalarItem.Type != StackItemType.Integer)
                    throw new ArgumentException("scalar must be bigint");
                var point = GetInteropPoint(pair[0]);
                result.AddRange(SerializeEthereumPoint(point));
                result.AddRange(SerializeScalarBigEndian(scalarItem.GetInteger()));
            }

            return result.ToArray();
        }

        /// <summary>
        /// Deserialize a list of BLS12-381 points from compressed encoding.
        /// </summary>
        /// <param name="data">The serialized points.</param>
        /// <returns></returns>
        [ContractMethod(Hardfork.HF_Faun, CpuFee = 1 << 19)]
        public static InteropInterface[] Bls12381DeserializeList(byte[] data)
        {
            if (data.Length == 0)
                throw new ArgumentException("deserializer requires at least one pair");
            int pairSize = 48 + 96;
            if (data.Length % pairSize != 0)
                throw new ArgumentException($"length must be a multiple of {pairSize}");

            int pairCount = data.Length / pairSize;
            var result = new InteropInterface[pairCount * 2];
            int offset = 0;
            int index = 0;
            int currentSize = 48;
            int nextSize = 96;
            while (offset < data.Length)
            {
                ReadOnlySpan<byte> slice = data.AsSpan(offset, currentSize);
                result[index++] = currentSize switch
                {
                    48 => new InteropInterface(G1Affine.FromCompressed(slice)),
                    96 => new InteropInterface(G2Affine.FromCompressed(slice)),
                    _ => throw new ArgumentException("Invalid BLS12-381 point length")
                };
                offset += currentSize;
                (currentSize, nextSize) = (nextSize, currentSize);
            }
            return result;
        }

        /// <summary>
        /// Deserialize a list of BLS12-381 points from Ethereum encoding (EIP-2537).
        /// </summary>
        /// <param name="data">The serialized points.</param>
        /// <returns></returns>
        [ContractMethod(Hardfork.HF_Faun, CpuFee = 1 << 19)]
        public static InteropInterface[] Bls12381DeserializeEthereumList(byte[] data)
        {
            if (data.Length == 0)
                throw new ArgumentException("deserializer requires at least one pair");
            int pairSize = Bls12G1EncodedLength + Bls12G2EncodedLength;
            if (data.Length % pairSize != 0)
                throw new ArgumentException($"length must be a multiple of {pairSize}");

            int pairCount = data.Length / pairSize;
            var result = new InteropInterface[pairCount * 2];
            int offset = 0;
            int index = 0;
            int currentSize = Bls12G1EncodedLength;
            int nextSize = Bls12G2EncodedLength;
            while (offset < data.Length)
            {
                ReadOnlySpan<byte> slice = data.AsSpan(offset, currentSize);
                result[index++] = currentSize switch
                {
                    var size when size == Bls12G1EncodedLength => new InteropInterface(DeserializeEthereumG1(slice)),
                    var size when size == Bls12G2EncodedLength => new InteropInterface(DeserializeEthereumG2(slice)),
                    _ => throw new ArgumentException("Invalid BLS12-381 point length")
                };
                offset += currentSize;
                (currentSize, nextSize) = (nextSize, currentSize);
            }
            return result;
        }

        /// <summary>
        /// Deserialize G1 point and scalar pairs from compressed encoding.
        /// </summary>
        /// <param name="pairs">The serialized point and scalar pairs.</param>
        /// <returns></returns>
        [ContractMethod(Hardfork.HF_Faun, CpuFee = 1 << 19)]
        public static object[] Bls12381DeserializeG1ScalarPairs(byte[] pairs)
        {
            if (pairs.Length == 0)
                throw new ArgumentException("deserializer requires at least one pair");
            int pairSize = 48 + Bls12ScalarLength;
            if (pairs.Length % pairSize != 0)
                throw new ArgumentException($"length must be a multiple of {pairSize}");

            var result = new object[pairs.Length / pairSize];
            int offset = 0;
            int index = 0;
            while (offset < pairs.Length)
            {
                var g1 = G1Affine.FromCompressed(pairs.AsSpan(offset, 48));
                offset += 48;
                var scalar = ParseScalarLittleEndian(pairs.AsSpan(offset, Bls12ScalarLength));
                offset += Bls12ScalarLength;
                result[index++] = new object[] { new InteropInterface(g1), scalar };
            }
            return result;
        }

        /// <summary>
        /// Deserialize G2 point and scalar pairs from compressed encoding.
        /// </summary>
        /// <param name="pairs">The serialized point and scalar pairs.</param>
        /// <returns></returns>
        [ContractMethod(Hardfork.HF_Faun, CpuFee = 1 << 19)]
        public static object[] Bls12381DeserializeG2ScalarPairs(byte[] pairs)
        {
            if (pairs.Length == 0)
                throw new ArgumentException("deserializer requires at least one pair");
            int pairSize = 96 + Bls12ScalarLength;
            if (pairs.Length % pairSize != 0)
                throw new ArgumentException($"length must be a multiple of {pairSize}");

            var result = new object[pairs.Length / pairSize];
            int offset = 0;
            int index = 0;
            while (offset < pairs.Length)
            {
                var g2 = G2Affine.FromCompressed(pairs.AsSpan(offset, 96));
                offset += 96;
                var scalar = ParseScalarLittleEndian(pairs.AsSpan(offset, Bls12ScalarLength));
                offset += Bls12ScalarLength;
                result[index++] = new object[] { new InteropInterface(g2), scalar };
            }
            return result;
        }

        /// <summary>
        /// Deserialize G1 point and scalar pairs from Ethereum encoding (EIP-2537).
        /// </summary>
        /// <param name="pairs">The serialized point and scalar pairs.</param>
        /// <returns></returns>
        [ContractMethod(Hardfork.HF_Faun, CpuFee = 1 << 19)]
        public static object[] Bls12381DeserializeEthereumG1ScalarPairs(byte[] pairs)
        {
            if (pairs.Length == 0)
                throw new ArgumentException("deserializer requires at least one pair");
            int pairSize = Bls12G1EncodedLength + Bls12ScalarLength;
            if (pairs.Length % pairSize != 0)
                throw new ArgumentException($"length must be a multiple of {pairSize}");

            var result = new object[pairs.Length / pairSize];
            int offset = 0;
            int index = 0;
            while (offset < pairs.Length)
            {
                var g1 = DeserializeEthereumG1(pairs.AsSpan(offset, Bls12G1EncodedLength));
                offset += Bls12G1EncodedLength;
                var scalar = ParseScalarBigEndian(pairs.AsSpan(offset, Bls12ScalarLength));
                offset += Bls12ScalarLength;
                result[index++] = new object[] { new InteropInterface(g1), scalar };
            }
            return result;
        }

        /// <summary>
        /// Deserialize G2 point and scalar pairs from Ethereum encoding (EIP-2537).
        /// </summary>
        /// <param name="pairs">The serialized point and scalar pairs.</param>
        /// <returns></returns>
        [ContractMethod(Hardfork.HF_Faun, CpuFee = 1 << 19)]
        public static object[] Bls12381DeserializeEthereumG2ScalarPairs(byte[] pairs)
        {
            if (pairs.Length == 0)
                throw new ArgumentException("deserializer requires at least one pair");
            int pairSize = Bls12G2EncodedLength + Bls12ScalarLength;
            if (pairs.Length % pairSize != 0)
                throw new ArgumentException($"length must be a multiple of {pairSize}");

            var result = new object[pairs.Length / pairSize];
            int offset = 0;
            int index = 0;
            while (offset < pairs.Length)
            {
                var g2 = DeserializeEthereumG2(pairs.AsSpan(offset, Bls12G2EncodedLength));
                offset += Bls12G2EncodedLength;
                var scalar = ParseScalarBigEndian(pairs.AsSpan(offset, Bls12ScalarLength));
                offset += Bls12ScalarLength;
                result[index++] = new object[] { new InteropInterface(g2), scalar };
            }
            return result;
        }

        /// <summary>
        /// Add operation of two points.
        /// </summary>
        /// <param name="x">The first point.</param>
        /// <param name="y">The second point.</param>
        /// <returns></returns>
        [ContractMethod(CpuFee = 1 << 19)]
        public static InteropInterface Bls12381Add(InteropInterface x, InteropInterface y)
        {
            return (x.GetInterface<object>(), y.GetInterface<object>()) switch
            {
                (G1Affine p1, G1Affine p2) => new(new G1Projective(p1) + p2),
                (G1Affine p1, G1Projective p2) => new(p1 + p2),
                (G1Projective p1, G1Affine p2) => new(p1 + p2),
                (G1Projective p1, G1Projective p2) => new(p1 + p2),
                (G2Affine p1, G2Affine p2) => new(new G2Projective(p1) + p2),
                (G2Affine p1, G2Projective p2) => new(p1 + p2),
                (G2Projective p1, G2Affine p2) => new(p1 + p2),
                (G2Projective p1, G2Projective p2) => new(p1 + p2),
                (Gt p1, Gt p2) => new(p1 + p2),
                _ => throw new ArgumentException("BLS12-381 type mismatch")
            };
        }

        /// <summary>
        /// Multi exponentiation of (point, scalar) pairs.
        /// </summary>
        /// <param name="pairs">Array of (point, scalar) pairs.</param>
        /// <returns></returns>
        [ContractMethod(Hardfork.HF_Faun, CpuFee = 1 << 23)]
        public static InteropInterface Bls12381MultiExp(VMArray pairs)
        {
            if (pairs.Count == 0)
                throw new ArgumentException("bls12381 multi exponent requires at least one pair");
            if (pairs.Count > Bls12381MultiExpMaxPairs)
                throw new ArgumentException($"bls12381 multi exponent supports at most {Bls12381MultiExpMaxPairs} pairs");

            int group = 0;
            bool hasAccumulator = false;
            G1Projective accG1 = default;
            G2Projective accG2 = default;

            foreach (StackItem item in pairs)
            {
                if (item is not VMArray pair)
                    throw new ArgumentException("bls12381 multi exponent pair must be Array or Struct");
                if (pair.Count != 2)
                    throw new ArgumentException("bls12381 multi exponent pair must contain point and scalar");
                if (pair[0].Type != StackItemType.InteropInterface)
                    throw new ArgumentException("bls12381 multi exponent requires interop points");

                var point = GetInteropPoint(pair[0]);
                group = EnsureGroupType(group, point);

                if (pair[1].Type != StackItemType.Integer)
                    throw new ArgumentException("invalid multiplier");
                var scalar = pair[1].GetInteger();
                if (scalar.IsZero) continue;

                if (group < 0)
                {
                    var res = MultiplyG1(point, scalar);
                    accG1 = hasAccumulator ? accG1 + res : res;
                }
                else
                {
                    var res = MultiplyG2(point, scalar);
                    accG2 = hasAccumulator ? accG2 + res : res;
                }
                hasAccumulator = true;
            }

            if (!hasAccumulator)
                throw new ArgumentException("bls12381 multi exponent requires at least one valid pair");
            return group < 0 ? new InteropInterface(accG1) : new InteropInterface(accG2);
        }

        /// <summary>
        /// Mul operation of gt point and multiplier
        /// </summary>
        /// <param name="x">The point</param>
        /// <param name="mul">Multiplier,32 bytes,little-endian</param>
        /// <param name="neg">negative number</param>
        /// <returns></returns>
        [ContractMethod(CpuFee = 1 << 21)]
        public static InteropInterface Bls12381Mul(InteropInterface x, byte[] mul, bool neg)
        {
            Scalar X = neg ? -Scalar.FromBytes(mul) : Scalar.FromBytes(mul);
            return x.GetInterface<object>() switch
            {
                G1Affine p => new(p * X),
                G1Projective p => new(p * X),
                G2Affine p => new(p * X),
                G2Projective p => new(p * X),
                Gt p => new(p * X),
                _ => throw new ArgumentException("BLS12-381 type mismatch")
            };
        }

        /// <summary>
        /// Pairing operation of g1 and g2
        /// </summary>
        /// <param name="g1">The g1 point.</param>
        /// <param name="g2">The g2 point.</param>
        /// <returns></returns>
        [ContractMethod(CpuFee = 1 << 23)]
        public static InteropInterface Bls12381Pairing(InteropInterface g1, InteropInterface g2)
        {
            G1Affine g1a = g1.GetInterface<object>() switch
            {
                G1Affine g => g,
                G1Projective g => new(g),
                _ => throw new ArgumentException("BLS12-381 type mismatch")
            };
            G2Affine g2a = g2.GetInterface<object>() switch
            {
                G2Affine g => g,
                G2Projective g => new(g),
                _ => throw new ArgumentException("BLS12-381 type mismatch")
            };
            return new(Bls12.Pairing(in g1a, in g2a));
        }

        /// <summary>
        /// Pairing check for a list of (G1, G2) points.
        /// </summary>
        /// <param name="points">Array of points in (G1, G2) order.</param>
        /// <returns></returns>
        [ContractMethod(Hardfork.HF_Faun, CpuFee = 1 << 23)]
        public static bool Bls12381PairingList(VMArray points)
        {
            if (points.Count == 0)
                throw new ArgumentException("bls12381 pairing requires at least one pair");
            if (points.Count % 2 != 0)
                throw new ArgumentException("bls12381 pairing requires an even number of elements");
            if (points.Count / 2 > Bls12381PairingMaxPairs)
                throw new ArgumentException($"bls12381 pairing supports at most {Bls12381PairingMaxPairs} pairs");

            Gt accumulator = Gt.Identity;
            for (int i = 0; i < points.Count; i += 2)
            {
                if (points[i].Type != StackItemType.InteropInterface || points[i + 1].Type != StackItemType.InteropInterface)
                    throw new ArgumentException("bls12381 pairing requires interop points");

                var g1 = GetInteropPoint(points[i]);
                var g2 = GetInteropPoint(points[i + 1]);

                G1Affine g1a = g1 switch
                {
                    G1Affine p => p,
                    G1Projective p => new G1Affine(p),
                    _ => throw new ArgumentException("interop must contain bls12381 point")
                };
                G2Affine g2a = g2 switch
                {
                    G2Affine p => p,
                    G2Projective p => new G2Affine(p),
                    _ => throw new ArgumentException("interop must contain bls12381 point")
                };

                accumulator += Bls12.Pairing(in g1a, in g2a);
            }
            return accumulator.IsIdentity;
        }

        private static object GetInteropPoint(StackItem item)
        {
            if (item is not InteropInterface interop)
                throw new ArgumentException("not a bls12381 point");
            var point = interop.GetInterface<object>();
            return point ?? throw new ArgumentException("not a bls12381 point");
        }

        private static int EnsureGroupType(int current, object point)
        {
            int incoming = point switch
            {
                G1Affine => -1,
                G1Projective => -1,
                G2Affine => 1,
                G2Projective => 1,
                _ => throw new ArgumentException("invalid bls12381 point type")
            };
            if (current == 0 || current == incoming)
                return incoming;
            throw new ArgumentException("can't mix groups");
        }

        private static G1Projective MultiplyG1(object point, BigInteger scalar)
        {
            var s = ScalarFromBigInteger(scalar);
            return point switch
            {
                G1Affine p => p * s,
                G1Projective p => p * s,
                _ => throw new ArgumentException("invalid bls12381 point type")
            };
        }

        private static G2Projective MultiplyG2(object point, BigInteger scalar)
        {
            var s = ScalarFromBigInteger(scalar);
            return point switch
            {
                G2Affine p => p * s,
                G2Projective p => p * s,
                _ => throw new ArgumentException("invalid bls12381 point type")
            };
        }

        private static Scalar ScalarFromBigInteger(BigInteger value)
        {
            bool neg = value.Sign < 0;
            if (neg) value = BigInteger.Negate(value);
            value %= Bls12ScalarModulus;
            byte[] bytes = value.ToByteArray(isUnsigned: true, isBigEndian: false);
            if (bytes.Length > Bls12ScalarLength)
                throw new ArgumentException("invalid multiplier");
            var padded = new byte[Bls12ScalarLength];
            System.Buffer.BlockCopy(bytes, 0, padded, 0, bytes.Length);
            var scalar = Scalar.FromBytes(padded);
            return neg ? -scalar : scalar;
        }

        private static byte[] SerializeEthereumPoint(object? point)
        {
            return point switch
            {
                G1Affine p => SerializeEthereumG1(p),
                G1Projective p => SerializeEthereumG1(new G1Affine(p)),
                G2Affine p => SerializeEthereumG2(p),
                G2Projective p => SerializeEthereumG2(new G2Affine(p)),
                _ => throw new ArgumentException("invalid bls12381 point type")
            };
        }

        private static byte[] SerializeEthereumG1(G1Affine point)
        {
            if (point.IsIdentity)
                return new byte[Bls12G1EncodedLength];
            var bytes = point.ToUncompressed();
            bytes[0] &= 0b0001_1111;
            return AddPadding(bytes);
        }

        private static byte[] SerializeEthereumG2(G2Affine point)
        {
            if (point.IsIdentity)
                return new byte[Bls12G2EncodedLength];
            var bytes = point.ToUncompressed();
            bytes[0] &= 0b0001_1111;
            return G2ToEthereum(bytes);
        }

        private static G1Affine DeserializeEthereumG1(ReadOnlySpan<byte> data)
        {
            if (IsAllZero(data))
                return G1Affine.Identity;
            var unpadded = RemovePadding(data);
            return G1Affine.FromUncompressed(unpadded);
        }

        private static G2Affine DeserializeEthereumG2(ReadOnlySpan<byte> data)
        {
            if (IsAllZero(data))
                return G2Affine.Identity;
            var unpadded = G2FromEthereum(data);
            return G2Affine.FromUncompressed(unpadded);
        }

        private static byte[] SerializeScalarLittleEndian(BigInteger scalar)
        {
            byte[] raw = BigInteger.Abs(scalar).ToByteArray(isUnsigned: true, isBigEndian: false);
            if (raw.Length > Bls12ScalarLength)
                throw new ArgumentException("scalar must be bigint");
            var padded = new byte[Bls12ScalarLength];
            System.Buffer.BlockCopy(raw, 0, padded, 0, raw.Length);
            return padded;
        }

        private static byte[] SerializeScalarBigEndian(BigInteger scalar)
        {
            byte[] raw = BigInteger.Abs(scalar).ToByteArray(isUnsigned: true, isBigEndian: true);
            if (raw.Length > Bls12ScalarLength)
                throw new ArgumentException("scalar must be bigint");
            var padded = new byte[Bls12ScalarLength];
            System.Buffer.BlockCopy(raw, 0, padded, Bls12ScalarLength - raw.Length, raw.Length);
            return padded;
        }

        private static BigInteger ParseScalarLittleEndian(ReadOnlySpan<byte> data)
        {
            if (data.Length != Bls12ScalarLength)
                throw new ArgumentException($"invalid multiplier: {Bls12ScalarLength}-bytes scalar is expected, got {data.Length}");
            try
            {
                var scalar = Scalar.FromBytes(data);
                return new BigInteger(scalar.ToArray(), isUnsigned: true, isBigEndian: false);
            }
            catch (FormatException e)
            {
                throw new ArgumentException("can't deserialize scalar", e);
            }
        }

        private static BigInteger ParseScalarBigEndian(ReadOnlySpan<byte> data)
        {
            if (data.Length != Bls12ScalarLength)
                throw new ArgumentException($"invalid multiplier: {Bls12ScalarLength}-bytes scalar is expected, got {data.Length}");
            var value = new BigInteger(data, isUnsigned: true, isBigEndian: true);
            value %= Bls12ScalarModulus;
            return value;
        }

        private static byte[] AddPadding(ReadOnlySpan<byte> data)
        {
            if (data.Length % Fp.Size != 0)
                throw new ArgumentException("invalid field element length");
            int count = data.Length / Fp.Size;
            var result = new byte[count * Bls12FieldElementLength];
            for (int i = 0; i < count; i++)
            {
                data.Slice(i * Fp.Size, Fp.Size)
                    .CopyTo(result.AsSpan((i + 1) * Bls12FieldElementLength - Fp.Size, Fp.Size));
            }
            return result;
        }

        private static byte[] RemovePadding(ReadOnlySpan<byte> data)
        {
            if (data.Length % Bls12FieldElementLength != 0)
                throw new ArgumentException("invalid buf length");
            int count = data.Length / Bls12FieldElementLength;
            var result = new byte[count * Fp.Size];
            for (int i = 0; i < count; i++)
            {
                int start = i * Bls12FieldElementLength;
                for (int j = start; j < start + Bls12FieldElementLength - Fp.Size; j++)
                {
                    if (data[j] != 0)
                        throw new ArgumentException("bls12-381 field element overflow");
                }
                data.Slice(start + Bls12FieldElementLength - Fp.Size, Fp.Size)
                    .CopyTo(result.AsSpan(i * Fp.Size, Fp.Size));
            }
            return result;
        }

        private static byte[] G2FromEthereum(ReadOnlySpan<byte> data)
        {
            var buf = RemovePadding(data);
            for (int offset = 0; offset < buf.Length; offset += 2 * Fp.Size)
            {
                int j = offset + Fp.Size;
                for (int i = 0; i < Fp.Size; i++)
                {
                    (buf[offset + i], buf[j + i]) = (buf[j + i], buf[offset + i]);
                }
            }
            return buf;
        }

        private static byte[] G2ToEthereum(ReadOnlySpan<byte> data)
        {
            var buf = AddPadding(data);
            for (int offset = Bls12FieldElementLength - Fp.Size; offset < buf.Length; offset += 2 * Bls12FieldElementLength)
            {
                int j = offset + Bls12FieldElementLength;
                for (int i = 0; i < Fp.Size; i++)
                {
                    (buf[offset + i], buf[j + i]) = (buf[j + i], buf[offset + i]);
                }
            }
            return buf;
        }

        private static bool IsAllZero(ReadOnlySpan<byte> data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] != 0)
                    return false;
            }
            return true;
        }
    }
}
