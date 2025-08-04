// Copyright (C) 2015-2025 The Neo Project.
//
// Gt.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Neo.Cryptography.BN254
{
    /// <summary>
    /// Element of the target group Gt (multiplicative group in Fp12)
    /// </summary>
    public readonly struct Gt : IEquatable<Gt>
    {
        // For simplicity, we store this as a byte array
        // In a real implementation, this would be an Fp12 element
        private readonly byte[] data;

        public const int Size = 384; // 12 * 32 bytes

        private Gt(byte[] data)
        {
            if (data.Length != Size)
                throw new ArgumentException($"Invalid data length {data.Length}, expected {Size}");
            this.data = data;
        }

        public static ref readonly Gt Identity => ref identity;
        
        private static readonly Gt identity = new(new byte[Size]);

        public static Gt FromBytes(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length != Size)
                throw new ArgumentException($"Invalid data length {bytes.Length}, expected {Size}");
            
            return new Gt(bytes.ToArray());
        }

        public byte[] ToArray()
        {
            return (byte[])data.Clone();
        }

        public static bool operator ==(in Gt a, in Gt b)
        {
            return a.data.AsSpan().SequenceEqual(b.data);
        }

        public static bool operator !=(in Gt a, in Gt b)
        {
            return !(a == b);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is not Gt other) return false;
            return this == other;
        }

        public bool Equals(Gt other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return data.GetHashCode();
        }

        public static Gt operator +(in Gt a, in Gt b)
        {
            // Simplified addition (element-wise XOR for demonstration)
            var result = new byte[Size];
            for (int i = 0; i < Size; i++)
            {
                result[i] = (byte)(a.data[i] ^ b.data[i]);
            }
            return new Gt(result);
        }

        public static Gt operator *(in Gt a, in Scalar b)
        {
            // Simplified scalar multiplication
            return a;
        }

        public override string ToString()
        {
            return $"Gt({BitConverter.ToString(data.AsSpan(0, 16))}...)";
        }
    }
}