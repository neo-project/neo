// Copyright (C) 2015-2025 The Neo Project.
//
// ECPoint.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using Neo.Extensions;
using Neo.IO;
using Neo.IO.Caching;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Neo.Cryptography.ECC
{
    /// <summary>
    /// Represents a (X,Y) coordinate pair for elliptic curve cryptography (ECC) structures.
    /// </summary>
    public class ECPoint : IComparable<ECPoint>, IEquatable<ECPoint>, ISerializable, ISerializableSpan
    {
        internal ECFieldElement? X, Y;
        internal readonly ECCurve Curve;
        private byte[]? _compressedPoint, _uncompressedPoint;

        /// <summary>
        /// Indicates whether it is a point at infinity.
        /// </summary>
        public bool IsInfinity
        {
            get { return X == null && Y == null; }
        }

        public int Size => IsInfinity ? 1 : 33;

        private static ECPointCache PointCacheK1 { get; } = new(1000);
        private static ECPointCache PointCacheR1 { get; } = new(1000);

        /// <summary>
        /// Initializes a new instance of the <see cref="ECPoint"/> class with the secp256r1 curve.
        /// </summary>
        public ECPoint() : this(null, null, ECCurve.Secp256r1) { }

        internal ECPoint(ECFieldElement? x, ECFieldElement? y, ECCurve curve)
        {
            if (x is null ^ y is null)
                throw new ArgumentException("Exactly one of the field elements is null");
            X = x;
            Y = y;
            Curve = curve;
        }

        public int CompareTo(ECPoint? other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            if (!Curve.Equals(other.Curve)) throw new InvalidOperationException("Invalid comparision for points with different curves");
            if (ReferenceEquals(this, other)) return 0;
            if (IsInfinity) return other.IsInfinity ? 0 : -1;
            if (other.IsInfinity) return IsInfinity ? 0 : 1;

            var result = X!.CompareTo(other.X!);
            if (result != 0) return result;
            return Y!.CompareTo(other.Y!);
        }

        /// <summary>
        /// Decode an <see cref="ECPoint"/> object from a sequence of byte.
        /// </summary>
        /// <param name="encoded">The sequence of byte to be decoded.</param>
        /// <param name="curve">The <see cref="ECCurve"/> object used to construct the <see cref="ECPoint"/>.</param>
        /// <returns>The decoded point.</returns>
        public static ECPoint DecodePoint(ReadOnlySpan<byte> encoded, ECCurve curve)
        {
            switch (encoded[0])
            {
                case 0x02: // compressed
                case 0x03: // compressed
                    {
                        if (encoded.Length != (curve.ExpectedECPointLength + 1))
                            throw new FormatException("Incorrect length for compressed encoding");
                        return DecompressPoint(encoded, curve);
                    }
                case 0x04: // uncompressed
                    {
                        if (encoded.Length != (2 * curve.ExpectedECPointLength + 1))
                            throw new FormatException("Incorrect length for uncompressed/hybrid encoding");
                        var x1 = new BigInteger(encoded[1..(1 + curve.ExpectedECPointLength)], isUnsigned: true, isBigEndian: true);
                        var y1 = new BigInteger(encoded[(1 + curve.ExpectedECPointLength)..], isUnsigned: true, isBigEndian: true);
                        return new ECPoint(new ECFieldElement(x1, curve), new ECFieldElement(y1, curve), curve)
                        {
                            _uncompressedPoint = encoded.ToArray()
                        };
                    }
                default:
                    throw new FormatException("Invalid point encoding " + encoded[0]);
            }
        }

        private static ECPoint DecompressPoint(ReadOnlySpan<byte> encoded, ECCurve curve)
        {
            ECPointCache pointCache;
            if (curve == ECCurve.Secp256k1) pointCache = PointCacheK1;
            else if (curve == ECCurve.Secp256r1) pointCache = PointCacheR1;
            else throw new FormatException("Invalid curve " + curve);

            var compressedPoint = encoded.ToArray();
            if (!pointCache.TryGet(compressedPoint, out var p))
            {
                var yTilde = encoded[0] & 1;
                var x1 = new BigInteger(encoded[1..], isUnsigned: true, isBigEndian: true);
                p = DecompressPoint(yTilde, x1, curve);
                p._compressedPoint = compressedPoint;
                pointCache.Add(p);
            }
            return p;
        }

        private static ECPoint DecompressPoint(int yTilde, BigInteger X1, ECCurve curve)
        {
            var x = new ECFieldElement(X1, curve);
            var alpha = x * (x.Square() + curve.A) + curve.B;
            var beta = alpha.Sqrt() ?? throw new ArithmeticException("Invalid point compression");
            var betaValue = beta.Value;
            var bit0 = betaValue.IsEven ? 0 : 1;

            if (bit0 != yTilde)
            {
                // Use the other root
                beta = new ECFieldElement(curve.Q - betaValue, curve);
            }

            return new ECPoint(x, beta, curve);
        }

        void ISerializable.Deserialize(ref MemoryReader reader)
        {
            var p = DeserializeFrom(ref reader, Curve);
            X = p.X;
            Y = p.Y;
        }

        /// <summary>
        /// Deserializes an <see cref="ECPoint"/> object from a <see cref="MemoryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="MemoryReader"/> for reading data.</param>
        /// <param name="curve">The <see cref="ECCurve"/> object used to construct the <see cref="ECPoint"/>.</param>
        /// <returns>The deserialized point.</returns>
        public static ECPoint DeserializeFrom(ref MemoryReader reader, ECCurve curve)
        {
            var size = reader.Peek() switch
            {
                0x02 or 0x03 => 1 + curve.ExpectedECPointLength,
                0x04 => 1 + curve.ExpectedECPointLength * 2,
                _ => throw new FormatException("Invalid point encoding " + reader.Peek())
            };
            return DecodePoint(reader.ReadMemory(size).Span, curve);
        }

        /// <summary>
        /// Encodes an <see cref="ECPoint"/> object to a byte array.
        /// </summary>
        /// <param name="commpressed">Indicates whether to encode it in a compressed format.</param>
        /// <returns>The encoded point.</returns>
        /// <remarks>Note: The return should't be modified because it could be cached.</remarks>
        public byte[] EncodePoint(bool commpressed)
        {
            if (IsInfinity) return new byte[1];
            byte[] data;
            if (commpressed)
            {
                if (_compressedPoint != null) return _compressedPoint;
                data = new byte[33];
            }
            else
            {
                if (_uncompressedPoint != null) return _uncompressedPoint;
                data = new byte[65];
                var yBytes = Y!.Value.ToByteArray(isUnsigned: true, isBigEndian: true);
                Buffer.BlockCopy(yBytes, 0, data, 65 - yBytes.Length, yBytes.Length);
            }
            var xBytes = X!.Value.ToByteArray(isUnsigned: true, isBigEndian: true);
            Buffer.BlockCopy(xBytes, 0, data, 33 - xBytes.Length, xBytes.Length);
            data[0] = commpressed ? Y!.Value.IsEven ? (byte)0x02 : (byte)0x03 : (byte)0x04;
            if (commpressed) _compressedPoint = data;
            else _uncompressedPoint = data;
            return data;
        }

        public bool Equals(ECPoint? other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null) return false;
            if (!Curve.Equals(other.Curve)) return false;
            if (IsInfinity && other.IsInfinity) return true;
            if (IsInfinity || other.IsInfinity) return false;
            return X!.Equals(other.X) && Y!.Equals(other.Y);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as ECPoint);
        }

        /// <summary>
        /// Constructs an <see cref="ECPoint"/> object from a byte array.
        /// </summary>
        /// <param name="bytes">The byte array to be used to construct the object.</param>
        /// <param name="curve">The <see cref="ECCurve"/> object used to construct the <see cref="ECPoint"/>.</param>
        /// <returns>The decoded point.</returns>
        public static ECPoint FromBytes(byte[] bytes, ECCurve curve)
        {
            return bytes.Length switch
            {
                33 or 65 => DecodePoint(bytes, curve),
                64 or 72 => DecodePoint([.. new byte[] { 0x04 }, .. bytes[^64..]], curve),
                96 or 104 => DecodePoint([.. new byte[] { 0x04 }, .. bytes[^96..^32]], curve),
                _ => throw new FormatException(),
            };
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Curve.GetHashCode(), X?.GetHashCode() ?? 0, Y?.GetHashCode() ?? 0);
        }

        internal static ECPoint Multiply(ECPoint p, BigInteger k)
        {
            // floor(log2(k))
            var m = (int)VM.Utility.GetBitLength(k);

            // width of the Window NAF
            sbyte width;

            // Required length of precomputing array
            int reqPreCompLen;

            // Determine optimal width and corresponding length of precomputing array
            // array based on literature values
            if (m < 13)
            {
                width = 2;
                reqPreCompLen = 1;
            }
            else if (m < 41)
            {
                width = 3;
                reqPreCompLen = 2;
            }
            else if (m < 121)
            {
                width = 4;
                reqPreCompLen = 4;
            }
            else if (m < 337)
            {
                width = 5;
                reqPreCompLen = 8;
            }
            else if (m < 897)
            {
                width = 6;
                reqPreCompLen = 16;
            }
            else if (m < 2305)
            {
                width = 7;
                reqPreCompLen = 32;
            }
            else
            {
                width = 8;
                reqPreCompLen = 127;
            }

            // The length of the precomputing array
            var preCompLen = 1;
            var preComp = new ECPoint[] { p };
            var twiceP = p.Twice();

            if (preCompLen < reqPreCompLen)
            {
                // Precomputing array must be made bigger, copy existing preComp
                // array into the larger new preComp array
                var oldPreComp = preComp;
                preComp = new ECPoint[reqPreCompLen];
                Array.Copy(oldPreComp, 0, preComp, 0, preCompLen);

                for (var i = preCompLen; i < reqPreCompLen; i++)
                {
                    // Compute the new ECPoints for the precomputing array.
                    // The values 1, 3, 5, ..., 2^(width-1)-1 times p are
                    // computed
                    preComp[i] = twiceP + preComp[i - 1];
                }
            }

            // Compute the Window NAF of the desired width
            var wnaf = WindowNaf(width, k);
            var l = wnaf.Length;

            // Apply the Window NAF to p using the precomputed ECPoint values.
            var q = p.Curve.Infinity;
            for (var i = l - 1; i >= 0; i--)
            {
                q = q.Twice();

                if (wnaf[i] != 0)
                {
                    if (wnaf[i] > 0)
                    {
                        q += preComp[(wnaf[i] - 1) / 2];
                    }
                    else
                    {
                        // wnaf[i] < 0
                        q -= preComp[(-wnaf[i] - 1) / 2];
                    }
                }
            }

            return q;
        }

        /// <summary>
        /// Parse the <see cref="ECPoint"/> object from a <see cref="string"/>.
        /// </summary>
        /// <param name="value">The <see cref="string"/> to be parsed.</param>
        /// <param name="curve">The <see cref="ECCurve"/> object used to construct the <see cref="ECPoint"/>.</param>
        /// <returns>The parsed point.</returns>
        public static ECPoint Parse(string value, ECCurve curve)
        {
            return DecodePoint(value.HexToBytes(), curve);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(EncodePoint(true));
        }

        /// <summary>
        /// Gets a ReadOnlySpan that represents the current value.
        /// </summary>
        /// <returns>A ReadOnlySpan that represents the current value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> GetSpan()
        {
            return EncodePoint(true).AsSpan();
        }

        public override string ToString()
        {
            return EncodePoint(true).ToHexString();
        }

        /// <summary>
        /// Try parse the <see cref="ECPoint"/> object from a <see cref="string"/>.
        /// </summary>
        /// <param name="value">The <see cref="string"/> to be parsed.</param>
        /// <param name="curve">The <see cref="ECCurve"/> object used to construct the <see cref="ECPoint"/>.</param>
        /// <param name="point">The parsed point.</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> was converted successfully; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse(string value, ECCurve curve, [NotNullWhen(true)] out ECPoint? point)
        {
            try
            {
                point = Parse(value, curve);
                return true;
            }
            catch (FormatException)
            {
                point = null;
                return false;
            }
        }

        internal ECPoint Twice()
        {
            if (IsInfinity)
                return this;
            if (Y!.Value.Sign == 0)
                return Curve.Infinity;
            var two = new ECFieldElement(2, Curve);
            var three = new ECFieldElement(3, Curve);
            var gamma = (X!.Square() * three + Curve.A) / (Y * two);
            var x3 = gamma.Square() - X! * two;
            var y3 = gamma * (X! - x3) - Y;
            return new ECPoint(x3, y3, Curve);
        }

        private static sbyte[] WindowNaf(sbyte width, BigInteger k)
        {
            var wnaf = new sbyte[VM.Utility.GetBitLength(k) + 1];
            var pow2wB = (short)(1 << width);
            var i = 0;
            var length = 0;
            while (k.Sign > 0)
            {
                if (!k.IsEven)
                {
                    var remainder = k % pow2wB;
                    if (remainder.TestBit(width - 1))
                    {
                        wnaf[i] = (sbyte)(remainder - pow2wB);
                    }
                    else
                    {
                        wnaf[i] = (sbyte)remainder;
                    }
                    k -= wnaf[i];
                    length = i;
                }
                else
                {
                    wnaf[i] = 0;
                }
                k >>= 1;
                i++;
            }
            length++;
            var wnafShort = new sbyte[length];
            Array.Copy(wnaf, 0, wnafShort, 0, length);
            return wnafShort;
        }

        public static ECPoint operator -(ECPoint x)
        {
            return new ECPoint(x.X, -x.Y!, x.Curve);
        }

        public static ECPoint operator *(ECPoint p, byte[] n)
        {
            if (n.Length != 32)
                throw new ArgumentException(null, nameof(n));
            if (p.IsInfinity)
                return p;
            var k = new BigInteger(n, isUnsigned: true, isBigEndian: true);
            if (k.Sign == 0)
                return p.Curve.Infinity;
            return Multiply(p, k);
        }

        public static ECPoint operator +(ECPoint x, ECPoint y)
        {
            if (x.IsInfinity)
                return y;
            if (y.IsInfinity)
                return x;
            if (x.X!.Equals(y.X))
            {
                if (x.Y!.Equals(y.Y))
                    return x.Twice();
                return x.Curve.Infinity;
            }
            var gamma = (y.Y! - x.Y!) / (y.X! - x.X!);
            var x3 = gamma.Square() - x.X! - y.X!;
            var y3 = gamma * (x.X! - x3) - x.Y!;
            return new ECPoint(x3, y3, x.Curve);
        }

        public static ECPoint operator -(ECPoint x, ECPoint y)
        {
            if (y.IsInfinity)
                return x;
            return x + (-y);
        }
    }
}

#nullable disable
