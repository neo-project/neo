// Copyright (C) 2015-2021 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using System;
using System.IO;
using System.Numerics;
using static Neo.Helper;

namespace Neo.Cryptography.ECC
{
    /// <summary>
    /// Represents a (X,Y) coordinate pair for elliptic curve cryptography (ECC) structures.
    /// </summary>
    public class ECPoint : IComparable<ECPoint>, IEquatable<ECPoint>, ISerializable
    {
        internal ECFieldElement X, Y;
        internal readonly ECCurve Curve;
        private byte[] _compressedPoint, _uncompressedPoint;

        /// <summary>
        /// Indicates whether it is a point at infinity.
        /// </summary>
        public bool IsInfinity
        {
            get { return X == null && Y == null; }
        }

        public int Size => IsInfinity ? 1 : 33;

        private static IO.Caching.ECPointCache pointCache { get; } = new(100);

        /// <summary>
        /// Initializes a new instance of the <see cref="ECPoint"/> class with the secp256r1 curve.
        /// </summary>
        public ECPoint() : this(null, null, ECCurve.Secp256r1) { }

        internal ECPoint(ECFieldElement x, ECFieldElement y, ECCurve curve)
        {
            if ((x is null ^ y is null) || (curve is null))
                throw new ArgumentException("Exactly one of the field elements is null");
            this.X = x;
            this.Y = y;
            this.Curve = curve;
        }

        public int CompareTo(ECPoint other)
        {
            if (!Curve.Equals(other.Curve)) throw new InvalidOperationException("Invalid comparision for points with different curves");
            if (ReferenceEquals(this, other)) return 0;
            int result = X.CompareTo(other.X);
            if (result != 0) return result;
            return Y.CompareTo(other.Y);
        }

        /// <summary>
        /// Decode an <see cref="ECPoint"/> object from a sequence of byte.
        /// </summary>
        /// <param name="encoded">The sequence of byte to be decoded.</param>
        /// <param name="curve">The <see cref="ECCurve"/> object used to construct the <see cref="ECPoint"/>.</param>
        /// <returns>The decoded point.</returns>
        public static ECPoint DecodePoint(ReadOnlySpan<byte> encoded, ECCurve curve)
        {
            ECPoint p = null;
            switch (encoded[0])
            {
                case 0x02: // compressed
                case 0x03: // compressed
                    {
                        if (encoded.Length != (curve.ExpectedECPointLength + 1))
                            throw new FormatException("Incorrect length for compressed encoding");
                        byte[] compressedPoint = encoded.ToArray();
                        if (pointCache.TryGet(compressedPoint, out ECPoint inventory))
                        {
                            p = inventory;
                        }
                        else
                        {
                            int yTilde = encoded[0] & 1;
                            BigInteger X1 = new(encoded[1..], isUnsigned: true, isBigEndian: true);
                            p = DecompressPoint(yTilde, X1, curve);
                            p._compressedPoint = compressedPoint;
                            pointCache.Add(p);
                        }
                        break;
                    }
                case 0x04: // uncompressed
                    {
                        if (encoded.Length != (2 * curve.ExpectedECPointLength + 1))
                            throw new FormatException("Incorrect length for uncompressed/hybrid encoding");
                        BigInteger X1 = new(encoded[1..(1 + curve.ExpectedECPointLength)], isUnsigned: true, isBigEndian: true);
                        BigInteger Y1 = new(encoded[(1 + curve.ExpectedECPointLength)..], isUnsigned: true, isBigEndian: true);
                        p = new ECPoint(new ECFieldElement(X1, curve), new ECFieldElement(Y1, curve), curve)
                        {
                            _uncompressedPoint = encoded.ToArray()
                        };
                        break;
                    }
                default:
                    throw new FormatException("Invalid point encoding " + encoded[0]);
            }
            return p;
        }

        private static ECPoint DecompressPoint(int yTilde, BigInteger X1, ECCurve curve)
        {
            ECFieldElement x = new(X1, curve);
            ECFieldElement alpha = x * (x.Square() + curve.A) + curve.B;
            ECFieldElement beta = alpha.Sqrt();

            //
            // if we can't find a sqrt we haven't got a point on the
            // curve - run!
            //
            if (beta == null)
                throw new ArithmeticException("Invalid point compression");

            BigInteger betaValue = beta.Value;
            int bit0 = betaValue.IsEven ? 0 : 1;

            if (bit0 != yTilde)
            {
                // Use the other root
                beta = new ECFieldElement(curve.Q - betaValue, curve);
            }

            return new ECPoint(x, beta, curve);
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            ECPoint p = DeserializeFrom(reader, Curve);
            X = p.X;
            Y = p.Y;
        }

        /// <summary>
        /// Deserializes an <see cref="ECPoint"/> object from a <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader"/> for reading data.</param>
        /// <param name="curve">The <see cref="ECCurve"/> object used to construct the <see cref="ECPoint"/>.</param>
        /// <returns>The deserialized point.</returns>
        public static ECPoint DeserializeFrom(BinaryReader reader, ECCurve curve)
        {
            Span<byte> buffer = stackalloc byte[1 + curve.ExpectedECPointLength * 2];
            buffer[0] = reader.ReadByte();
            switch (buffer[0])
            {
                case 0x02:
                case 0x03:
                    {
                        if (reader.Read(buffer[1..(1 + curve.ExpectedECPointLength)]) != curve.ExpectedECPointLength)
                        {
                            throw new FormatException();
                        }
                        return DecodePoint(buffer[..(1 + curve.ExpectedECPointLength)], curve);
                    }
                case 0x04:
                    {
                        if (reader.Read(buffer[1..(1 + curve.ExpectedECPointLength * 2)]) != curve.ExpectedECPointLength * 2)
                        {
                            throw new FormatException();
                        }
                        return DecodePoint(buffer, curve);
                    }
                default:
                    throw new FormatException("Invalid point encoding " + buffer[0]);
            }
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
                byte[] yBytes = Y.Value.ToByteArray(isUnsigned: true, isBigEndian: true);
                Buffer.BlockCopy(yBytes, 0, data, 65 - yBytes.Length, yBytes.Length);
            }
            byte[] xBytes = X.Value.ToByteArray(isUnsigned: true, isBigEndian: true);
            Buffer.BlockCopy(xBytes, 0, data, 33 - xBytes.Length, xBytes.Length);
            data[0] = commpressed ? Y.Value.IsEven ? (byte)0x02 : (byte)0x03 : (byte)0x04;
            if (commpressed) _compressedPoint = data;
            else _uncompressedPoint = data;
            return data;
        }

        public bool Equals(ECPoint other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null) return false;
            if (IsInfinity && other.IsInfinity) return true;
            if (IsInfinity || other.IsInfinity) return false;
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override bool Equals(object obj)
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
                64 or 72 => DecodePoint(Concat(new byte[] { 0x04 }, bytes[^64..]), curve),
                96 or 104 => DecodePoint(Concat(new byte[] { 0x04 }, bytes[^96..^32]), curve),
                _ => throw new FormatException(),
            };
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() + Y.GetHashCode();
        }

        internal static ECPoint Multiply(ECPoint p, BigInteger k)
        {
            // floor(log2(k))
            int m = (int)k.GetBitLength();

            // width of the Window NAF
            sbyte width;

            // Required length of precomputation array
            int reqPreCompLen;

            // Determine optimal width and corresponding length of precomputation
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

            // The length of the precomputation array
            int preCompLen = 1;

            ECPoint[] preComp = new ECPoint[] { p };
            ECPoint twiceP = p.Twice();

            if (preCompLen < reqPreCompLen)
            {
                // Precomputation array must be made bigger, copy existing preComp
                // array into the larger new preComp array
                ECPoint[] oldPreComp = preComp;
                preComp = new ECPoint[reqPreCompLen];
                Array.Copy(oldPreComp, 0, preComp, 0, preCompLen);

                for (int i = preCompLen; i < reqPreCompLen; i++)
                {
                    // Compute the new ECPoints for the precomputation array.
                    // The values 1, 3, 5, ..., 2^(width-1)-1 times p are
                    // computed
                    preComp[i] = twiceP + preComp[i - 1];
                }
            }

            // Compute the Window NAF of the desired width
            sbyte[] wnaf = WindowNaf(width, k);
            int l = wnaf.Length;

            // Apply the Window NAF to p using the precomputed ECPoint values.
            ECPoint q = p.Curve.Infinity;
            for (int i = l - 1; i >= 0; i--)
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
        public static bool TryParse(string value, ECCurve curve, out ECPoint point)
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
            if (this.IsInfinity)
                return this;
            if (this.Y.Value.Sign == 0)
                return Curve.Infinity;
            ECFieldElement TWO = new(2, Curve);
            ECFieldElement THREE = new(3, Curve);
            ECFieldElement gamma = (this.X.Square() * THREE + Curve.A) / (Y * TWO);
            ECFieldElement x3 = gamma.Square() - this.X * TWO;
            ECFieldElement y3 = gamma * (this.X - x3) - this.Y;
            return new ECPoint(x3, y3, Curve);
        }

        private static sbyte[] WindowNaf(sbyte width, BigInteger k)
        {
            sbyte[] wnaf = new sbyte[k.GetBitLength() + 1];
            short pow2wB = (short)(1 << width);
            int i = 0;
            int length = 0;
            while (k.Sign > 0)
            {
                if (!k.IsEven)
                {
                    BigInteger remainder = k % pow2wB;
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
            sbyte[] wnafShort = new sbyte[length];
            Array.Copy(wnaf, 0, wnafShort, 0, length);
            return wnafShort;
        }

        public static ECPoint operator -(ECPoint x)
        {
            return new ECPoint(x.X, -x.Y, x.Curve);
        }

        public static ECPoint operator *(ECPoint p, byte[] n)
        {
            if (p == null || n == null)
                throw new ArgumentNullException();
            if (n.Length != 32)
                throw new ArgumentException(null, nameof(n));
            if (p.IsInfinity)
                return p;
            BigInteger k = new(n, isUnsigned: true, isBigEndian: true);
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
            if (x.X.Equals(y.X))
            {
                if (x.Y.Equals(y.Y))
                    return x.Twice();
                return x.Curve.Infinity;
            }
            ECFieldElement gamma = (y.Y - x.Y) / (y.X - x.X);
            ECFieldElement x3 = gamma.Square() - x.X - y.X;
            ECFieldElement y3 = gamma * (x.X - x3) - x.Y;
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
