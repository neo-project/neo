// Copyright (C) 2015-2026 The Neo Project.
//
// ExtendedKey.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;
using ECCurve = Neo.Cryptography.ECC.ECCurve;
using ECPoint = Neo.Cryptography.ECC.ECPoint;

namespace Neo.Wallets.BIP32;

public class ExtendedKey
{
    public byte[] PrivateKey { get; }
    public ECPoint PublicKey { get; }
    public byte[] ChainCode { get; }

    ExtendedKey(byte[] key, byte[] chainCode, ECCurve curve)
    {
        PrivateKey = key;
        PublicKey = curve.G * key;
        ChainCode = chainCode;
    }

    public static ExtendedKey Create(byte[] seed, ECCurve? curve = null)
    {
        byte[] I = HMACSHA512.HashData("Bitcoin seed"u8, seed);
        byte[] IL = I[..32];
        byte[] IR = I[32..];
        return new ExtendedKey(IL, IR, curve ?? ECCurve.Secp256r1);
    }

    public static ExtendedKey Create(byte[] seed, string path, ECCurve? curve = null)
    {
        KeyPath keyPath = KeyPath.Parse(path);
        ExtendedKey extKey = Create(seed, curve);
        foreach (uint index in keyPath.Indices)
            extKey = extKey.Derive(index);
        return extKey;
    }

    public ExtendedKey Derive(uint index)
    {
        Span<byte> data = stackalloc byte[37];
        if (index >= 0x80000000)
        {
            data[0] = 0;
            PrivateKey.CopyTo(data[1..]);
        }
        else
        {
            PublicKey.EncodePoint(true).CopyTo(data);
        }
        BinaryPrimitives.WriteUInt32BigEndian(data[33..], index);
        byte[] I = HMACSHA512.HashData(ChainCode, data);
        ReadOnlySpan<byte> IL = I.AsSpan(..32);
        byte[] IR = I[32..];
        byte[] childKey = AddModN(IL, PrivateKey, PublicKey.Curve.N);
        return new ExtendedKey(childKey, IR, PublicKey.Curve);
    }

    static byte[] AddModN(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b, BigInteger n)
    {
        BigInteger aInt = new(a, isUnsigned: true, isBigEndian: true);
        // Check if parse256(IL) >= n (BIP32 requirement)
        if (aInt >= n)
            throw new InvalidOperationException("Derived child private key is invalid.");

        BigInteger bInt = new(b, isUnsigned: true, isBigEndian: true);
        BigInteger r = (aInt + bInt) % n;
        if (r.IsZero)
            throw new InvalidOperationException("Derived child private key is invalid.");

        byte[] result = new byte[32];
        Span<byte> tmp = stackalloc byte[32];
        r.TryWriteBytes(tmp, out int bytesWritten, isUnsigned: true, isBigEndian: true);
        tmp[..bytesWritten].CopyTo(result.AsSpan(32 - bytesWritten));
        return result;
    }
}
