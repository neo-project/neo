// Copyright (C) 2015-2022 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography;
using Neo.Cryptography.ECC;
using System;
using System.Collections.Generic;
using Neo.Wallets;

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
        /// Computes the script hash from specified public key byte array.
        /// </summary>
        /// <param name="pubKey">The public key to be used.</param>
        /// <param name="curve">The curve to be used by the ECDSA algorithm.</param>
        /// <returns>The computed public key script hash. Zero in case of error.</returns>
        [ContractMethod(CpuFee = 1 << 15)]
        public static UInt160 PubKeyToScriptHash(byte[] pubKey, NamedCurve curve)
        {
            try
            {
                return Contract.CreateSignatureRedeemScript(ECPoint.FromBytes(pubKey, curves[curve])).ToScriptHash();
            }
            catch (ArgumentException)
            {
                return UInt160.Zero;
            }
        }

        /// <summary>
        /// Computes the address from specified script hash.
        /// </summary>
        /// <param name="scriptHash">The script hash to be used.</param>
        /// <param name="version">The version of the address.</param>
        /// <returns>The computed address of script hash. Empty in case of error.</returns>
        [ContractMethod(CpuFee = 1 << 14)]
        public static string ScriptHashToAddr(UInt160 scriptHash, byte version)
        {
            try
            {
                return scriptHash.ToAddress(version);
            }
            catch (ArgumentException)
            {
                return string.Empty;
            }
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
    }
}
