// Copyright (C) 2015-2025 The Neo Project.
//
// Murmur128.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Buffers.Binary;
using System.IO.Hashing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Neo.Cryptography;

/// <summary>
/// Computes the 128 bits murmur hash for the input data.
/// </summary>
public sealed class Murmur128 : NonCryptographicHashAlgorithm
{
    private const ulong c1 = 0x87c37b91114253d5;
    private const ulong c2 = 0x4cf5ad432745937f;
    private const int r1 = 31;
    private const int r2 = 33;
    private const uint m = 5;
    private const uint n1 = 0x52dce729;
    private const uint n2 = 0x38495ab5;

    private readonly uint _seed;
    private int _length;

    public const int HashSizeInBits = 128;

    // The Tail struct is used to store up to 16 bytes of unprocessed data
    // when computing the hash. It leverages the InlineArray attribute for
    // efficient memory usage in .NET 8.0 or greater, avoiding heap allocations
    // and improving performance for small data sizes.
    [InlineArray(16)]
    private struct Tail
    {
        private byte v0;
        public Span<byte> AsSpan(int start = 0) => MemoryMarshal.CreateSpan(ref v0, 16)[start..];
    }

    private Tail _tail = new(); // cannot be readonly here

    private int _tailLength;

    private ulong H1 { get; set; }
    private ulong H2 { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Murmur128"/> class with the specified seed.
    /// </summary>
    /// <param name="seed">The seed to be used.</param>
    public Murmur128(uint seed) : base(HashSizeInBits / 8)
    {
        _seed = seed;
        Reset();
    }

    public override void Append(ReadOnlySpan<byte> source)
    {
        _length += source.Length;
        if (_tailLength > 0)
        {
            int copyLength = Math.Min(source.Length, HashSizeInBits / 8 - _tailLength);
            source[..copyLength].CopyTo(_tail.AsSpan(_tailLength));

            _tailLength += copyLength;
            if (_tailLength == HashSizeInBits / 8)
            {
                Mix(_tail.AsSpan());
                _tailLength = 0;
                _tail.AsSpan().Clear();
            }
            source = source[copyLength..];
        }

        for (; source.Length >= 16; source = source[16..])
        {
            Mix(source);
        }

        if (source.Length > 0)
        {
            source.CopyTo(_tail.AsSpan());
            _tailLength = source.Length;
        }
    }

    protected override void GetCurrentHashCore(Span<byte> destination)
    {
        if (_tailLength > 0)
        {
            var tail = _tail.AsSpan();
            ulong k1 = BinaryPrimitives.ReadUInt64LittleEndian(tail);
            ulong k2 = BinaryPrimitives.ReadUInt64LittleEndian(tail[8..]);
            H2 ^= BitOperations.RotateLeft(k2 * c2, r2) * c1;
            H1 ^= BitOperations.RotateLeft(k1 * c1, r1) * c2;
        }

        H1 ^= (ulong)_length;
        H2 ^= (ulong)_length;

        H1 += H2;
        H2 += H1;

        H1 = FMix(H1);
        H2 = FMix(H2);

        H1 += H2;
        H2 += H1;

        // NOTE: in some implementations, H1, H2 are output in big-endian, and little-endian is used here.
        if (BinaryPrimitives.TryWriteUInt64LittleEndian(destination, H1))
            BinaryPrimitives.TryWriteUInt64LittleEndian(destination[sizeof(ulong)..], H2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Reset()
    {
        H1 = H2 = _seed;
        _length = 0;
        _tailLength = 0;
        _tail.AsSpan().Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Mix(ReadOnlySpan<byte> source)
    {
        ulong k1 = BinaryPrimitives.ReadUInt64LittleEndian(source);
        ulong k2 = BinaryPrimitives.ReadUInt64LittleEndian(source[8..]);

        H1 ^= BitOperations.RotateLeft(k1 * c1, r1) * c2;
        H1 = BitOperations.RotateLeft(H1, 27) + H2;
        H1 = H1 * m + n1;

        H2 ^= BitOperations.RotateLeft(k2 * c2, r2) * c1;
        H2 = BitOperations.RotateLeft(H2, 31) + H1;
        H2 = H2 * m + n2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong FMix(ulong h)
    {
        h = (h ^ (h >> 33)) * 0xff51afd7ed558ccd;
        h = (h ^ (h >> 33)) * 0xc4ceb9fe1a85ec53;
        return h ^ (h >> 33);
    }

    /// <summary>
    /// Resets the state and computes the 128 bits murmur hash for the input data.
    /// </summary>
    /// <param name="source">The input to compute the hash code for.</param>
    /// <returns>The computed hash code.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] ComputeHash(ReadOnlySpan<byte> source)
    {
        Reset();
        Append(source);
        return GetCurrentHash();
    }
}
