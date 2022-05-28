// Copyright (C) 2015-2021 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography;
using Neo.Cryptography.BLS12_381;
using Neo.Cryptography.ECC;
using System;
using System.Collections.Generic;

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// A native contract library that provides cryptographic algorithms.
    /// </summary>
    public sealed class CryptoLib : NativeContract
    {
        private static readonly Dictionary<NamedCurve, ECCurve> curves = new()
        {
            [NamedCurve.secp256k1] = ECCurve.Secp256k1,
            [NamedCurve.secp256r1] = ECCurve.Secp256r1
        };

        internal CryptoLib() { }

        /// <summary>
        /// Computes the hash value for the specified byte array using the ripemd160 algorithm.
        /// </summary>
        /// <param name="data">The input to compute the hash code for.</param>
        /// <returns>The computed hash code.</returns>
        [ContractMethod(CpuFee = 1 << 15, Name = "ripemd160")]
        public static byte[] RIPEMD160(byte[] data)
        {
            return data.RIPEMD160();
        }

        /// <summary>
        /// Computes the hash value for the specified byte array using the sha256 algorithm.
        /// </summary>
        /// <param name="data">The input to compute the hash code for.</param>
        /// <returns>The computed hash code.</returns>
        [ContractMethod(CpuFee = 1 << 15)]
        public static byte[] Sha256(byte[] data)
        {
            return data.Sha256();
        }

        /// <summary>
        /// Computes the hash value for the specified byte array using the murmur32 algorithm.
        /// </summary>
        /// <param name="data">The input to compute the hash code for.</param>
        /// <param name="seed">The seed of the murmur32 hash function</param>
        /// <returns>The computed hash code.</returns>
        [ContractMethod(CpuFee = 1 << 13)]
        public static byte[] Murmur32(byte[] data, uint seed)
        {
            using Murmur32 murmur = new(seed);
            return murmur.ComputeHash(data);
        }

        /// <summary>
        /// Verifies that a digital signature is appropriate for the provided key and message using the ECDSA algorithm.
        /// </summary>
        /// <param name="message">The signed message.</param>
        /// <param name="pubkey">The public key to be used.</param>
        /// <param name="signature">The signature to be verified.</param>
        /// <param name="curve">The curve to be used by the ECDSA algorithm.</param>
        /// <returns><see langword="true"/> if the signature is valid; otherwise, <see langword="false"/>.</returns>
        [ContractMethod(CpuFee = 1 << 15)]
        public static bool VerifyWithECDsa(byte[] message, byte[] pubkey, byte[] signature, NamedCurve curve)
        {
            try
            {
                return Crypto.VerifySignature(message, signature, pubkey, curves[curve]);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /// <summary>
        /// The implementation of System.Crypto.PointAdd.
        /// Add operation of two gt points.
        /// </summary>
        /// <param name="gt1">Gt1 point as byteArray</param>
        /// <param name="gt2">Gt1 point as byteArray</param>
        /// <returns></returns>
        [ContractMethod(CpuFee = 1 << 19)]
        public static byte[] Bls12381Add(byte[] gt1, byte[] gt2)
        {
            return GObject.Add(new GObject(gt1), new GObject(gt2)).ToByteArray();
        }

        /// <summary>
        /// The implementation of System.Crypto.PointMul.
        /// Mul operation of gt point and mulitiplier
        /// </summary>
        /// <param name="gt">Gt point as byteArray</param>
        /// <param name="mul">Mulitiplier</param>
        /// <returns></returns>
        [ContractMethod(CpuFee = 1 << 21)]
        public static byte[] Bls12381Mul(byte[] gt, long mul)
        {
            GObject p = mul < 0 ? new GObject(gt).Neg() : new GObject(gt);
            var x = System.Convert.ToUInt64(Math.Abs(mul));
            return GObject.Mul(p, x).ToByteArray();
        }

        /// <summary>
        /// The implementation of System.Crypto.PointPairing.
        /// Pairing operation of g1 and g2
        /// </summary>
        /// <param name="g1Binary">Gt point1 as byteArray</param>
        /// <param name="g2Binary">Gt point2 as byteArray</param>
        /// <returns></returns>
        [ContractMethod(CpuFee = 1 << 23)]
        public static byte[] Bls12381Pairing(byte[] g1Binary, byte[] g2Binary)
        {
            return GObject.Pairing(new GObject(g1Binary), new GObject(g2Binary)).ToByteArray();
        }
    }
}
