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
using Neo.Cryptography.ECC;
using Neo.Network.P2P;
using System;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        /// <summary>
        /// The price of System.Crypto.CheckSig.
        /// </summary>
        public const long CheckSigPrice = 1 << 15;

        public const long Bls12381AddPrice = 1 << 15;

        public const long Bls12381MulPrice = 3 << 15;

        public const long Bls12381PairingPrice = 2 << 15;

        /// <summary>
        /// The <see cref="InteropDescriptor"/> of System.Crypto.CheckSig.
        /// Checks the signature for the current script container.
        /// </summary>
        public static readonly InteropDescriptor System_Crypto_CheckSig = Register("System.Crypto.CheckSig", nameof(CheckSig), CheckSigPrice, CallFlags.None);

        /// <summary>
        /// The <see cref="InteropDescriptor"/> of System.Crypto.CheckMultisig.
        /// Checks the signatures for the current script container.
        /// </summary>
        public static readonly InteropDescriptor System_Crypto_CheckMultisig = Register("System.Crypto.CheckMultisig", nameof(CheckMultisig), 0, CallFlags.None);


        public static readonly InteropDescriptor System_Crypto_Bls12381_Add = Register("System.Crypto.Bls12381Add", nameof(Bls12381Add), Bls12381AddPrice, CallFlags.None);

        public static readonly InteropDescriptor System_Crypto_Bls12381_Mul = Register("System.Crypto.Bls12381Mul", nameof(Bls12381Mul), Bls12381MulPrice, CallFlags.None);

        public static readonly InteropDescriptor System_Crypto_Bls12381_Pairing = Register("System.Crypto.Bls12381Pairing", nameof(Bls12381Pairing), Bls12381PairingPrice, CallFlags.None);



        /// <summary>
        /// The implementation of System.Crypto.CheckSig.
        /// Checks the signature for the current script container.
        /// </summary>
        /// <param name="pubkey">The public key of the account.</param>
        /// <param name="signature">The signature of the current script container.</param>
        /// <returns><see langword="true"/> if the signature is valid; otherwise, <see langword="false"/>.</returns>
        protected internal bool CheckSig(byte[] pubkey, byte[] signature)
        {
            try
            {
                return Crypto.VerifySignature(ScriptContainer.GetSignData(ProtocolSettings.Network), signature, pubkey, ECCurve.Secp256r1);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /// <summary>
        /// The implementation of System.Crypto.CheckMultisig.
        /// Checks the signatures for the current script container.
        /// </summary>
        /// <param name="pubkeys">The public keys of the account.</param>
        /// <param name="signatures">The signatures of the current script container.</param>
        /// <returns><see langword="true"/> if the signatures are valid; otherwise, <see langword="false"/>.</returns>
        protected internal bool CheckMultisig(byte[][] pubkeys, byte[][] signatures)
        {
            byte[] message = ScriptContainer.GetSignData(ProtocolSettings.Network);
            int m = signatures.Length, n = pubkeys.Length;
            if (n == 0 || m == 0 || m > n) throw new ArgumentException();
            AddGas(CheckSigPrice * n * exec_fee_factor);
            try
            {
                for (int i = 0, j = 0; i < m && j < n;)
                {
                    if (Crypto.VerifySignature(message, signatures[i], pubkeys[j], ECCurve.Secp256r1))
                        i++;
                    j++;
                    if (m - i > n - j)
                        return false;
                }
            }
            catch (ArgumentException)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// The implementation of System.Crypto.PointAdd.
        /// Add operation of two gt point
        /// </summary>
        /// <param name="gt1">gt1 point as byteArray</param>
        /// <param name="gt2">gt1 point as byteArray</param>
        /// <returns></returns>
        protected internal byte[] Bls12381Add(byte[] gt1, byte[] gt2)
        {
            return Bls12381.Point_Add(gt1, gt2);
        }

        /// <summary>
        /// The implementation of System.Crypto.PointMul.
        /// Mul operation of gt point and integer
        /// </summary>
        /// <param name="gt">gt point as byteArray</param>
        /// <param name="mul">the mulitiplier</param>
        /// <returns></returns>
        protected internal byte[] Bls12381Mul(byte[] gt, int mul)
        {
            return Bls12381.Point_Mul(gt, mul);
        }


        /// <summary>
        /// The implementation of System.Crypto.PointPairing.
        /// Pairing operation of g1 and g2
        /// </summary>
        /// <param name="g1_bytes"></param>
        /// <param name="g2_bytes"></param>
        /// <returns></returns>
        protected internal byte[] Bls12381Pairing(byte[] g1_bytes, byte[] g2_bytes)
        {
            return Bls12381.Point_Pairing(g1_bytes, g2_bytes);
        }
    }
}
