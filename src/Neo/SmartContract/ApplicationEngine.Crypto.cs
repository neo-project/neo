// Copyright (C) 2015-2024 The Neo Project.
//
// ApplicationEngine.Crypto.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.Network.P2P;
using System;
using System.Collections.Generic;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        protected internal readonly static Dictionary<byte, ECCurve> ECCurveSelection = new(){
            { 0x00, ECCurve.Secp256r1 },
            { 0x01, ECCurve.Secp256k1 },
        };

        /// <summary>
        /// The price of System.Crypto.CheckSig.
        /// </summary>
        public const long CheckSigPrice = 1 << 15;

        /// <summary>
        /// The <see cref="InteropDescriptor"/> of System.Crypto.CheckSig.
        /// Checks the signature for the current script container.
        /// </summary>
        public static readonly InteropDescriptor System_Crypto_CheckSig = Register("System.Crypto.CheckSig", nameof(CheckSig), CheckSigPrice, CallFlags.None);

        /// <summary>
        /// The <see cref="InteropDescriptor"/> of System.Crypto.CheckSigV2.
        /// Checks the secp256k1 or other non-secp256r1 signature for the current script container.
        /// </summary>
        public static readonly InteropDescriptor System_Crypto_CheckSigV2 = Register("System.Crypto.CheckSigV2", nameof(CheckSigV2), CheckSigPrice, CallFlags.None);

        /// <summary>
        /// The <see cref="InteropDescriptor"/> of System.Crypto.CheckMultisig.
        /// Checks the signatures for the current script container.
        /// </summary>
        public static readonly InteropDescriptor System_Crypto_CheckMultisig = Register("System.Crypto.CheckMultisig", nameof(CheckMultisig), 0, CallFlags.None);

        /// <summary>
        /// The <see cref="InteropDescriptor"/> of System.Crypto.CheckMultisigV2.
        /// Checks the secp256k1 or other non-secp256r1 signatures for the current script container.
        /// </summary>
        public static readonly InteropDescriptor System_Crypto_CheckMultisigV2 = Register("System.Crypto.CheckMultisigV2", nameof(CheckMultisigV2), 0, CallFlags.None);

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

        protected internal bool CheckSigV2(byte eccurve, byte hash, byte[] pubkey, byte[] signature)
        {
            try
            {
                if (!Enum.IsDefined(typeof(Hasher), hash))
                    throw new ArgumentOutOfRangeException("Invalid hasher");
                Hasher hasher = (Hasher)hash;
                if (!ECCurveSelection.TryGetValue(eccurve, out ECCurve curve))
                    throw new ArgumentOutOfRangeException("Invalid EC curve");
                return Crypto.VerifySignature(ScriptContainer.GetSignData(ProtocolSettings.Network), signature, pubkey, curve, hasher);
            }
            catch (Exception)
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
            AddGas(CheckSigPrice * n * ExecFeeFactor);
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

        protected internal bool CheckMultisigV2(byte eccurve, byte hash, byte[][] pubkeys, byte[][] signatures)
        {
            byte[] message = ScriptContainer.GetSignData(ProtocolSettings.Network);
            int m = signatures.Length, n = pubkeys.Length;
            if (n == 0 || m == 0 || m > n) throw new ArgumentException();
            AddGas(CheckSigPrice * n * ExecFeeFactor);
            try
            {
                if (!Enum.IsDefined(typeof(Hasher), hash))
                    throw new ArgumentOutOfRangeException("Invalid hasher");
                Hasher hasher = (Hasher)hash;
                if (!ECCurveSelection.TryGetValue(eccurve, out ECCurve curve))
                    throw new ArgumentOutOfRangeException("Invalid EC curve");

                for (int i = 0, j = 0; i < m && j < n;)
                {
                    if (Crypto.VerifySignature(message, signatures[i], pubkeys[j], curve, hasher))
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
    }
}
