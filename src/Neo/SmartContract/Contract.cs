// Copyright (C) 2015-2023 The Neo Project.
//
// The neo is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.SmartContract
{
    /// <summary>
    /// Represents a contract that can be invoked.
    /// </summary>
    public class Contract
    {
        /// <summary>
        /// The script of the contract.
        /// </summary>
        public byte[] Script;

        /// <summary>
        /// The parameters of the contract.
        /// </summary>
        public ContractParameterType[] ParameterList;

        private UInt160 _scriptHash;
        /// <summary>
        /// The hash of the contract.
        /// </summary>
        public virtual UInt160 ScriptHash
        {
            get
            {
                if (_scriptHash == null)
                {
                    _scriptHash = Script.ToScriptHash();
                }
                return _scriptHash;
            }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Contract"/> class.
        /// </summary>
        /// <param name="parameterList">The parameters of the contract.</param>
        /// <param name="redeemScript">The script of the contract.</param>
        /// <returns>The created contract.</returns>
        public static Contract Create(ContractParameterType[] parameterList, byte[] redeemScript)
        {
            return new Contract
            {
                Script = redeemScript,
                ParameterList = parameterList
            };
        }

        /// <summary>
        /// Constructs a special contract with empty script, will get the script with scriptHash from blockchain when doing the verification.
        /// </summary>
        /// <param name="scriptHash">The hash of the contract.</param>
        /// <param name="parameterList">The parameters of the contract.</param>
        /// <returns>The created contract.</returns>
        public static Contract Create(UInt160 scriptHash, params ContractParameterType[] parameterList)
        {
            return new Contract
            {
                Script = Array.Empty<byte>(),
                _scriptHash = scriptHash,
                ParameterList = parameterList
            };
        }

        /// <summary>
        /// Creates a multi-sig contract.
        /// </summary>
        /// <param name="m">The minimum number of correct signatures that need to be provided in order for the verification to pass.</param>
        /// <param name="publicKeys">The public keys of the contract.</param>
        /// <returns>The created contract.</returns>
        public static Contract CreateMultiSigContract(int m, IReadOnlyCollection<ECPoint> publicKeys)
        {
            return new Contract
            {
                Script = CreateMultiSigRedeemScript(m, publicKeys),
                ParameterList = Enumerable.Repeat(ContractParameterType.Signature, m).ToArray()
            };
        }

        /// <summary>
        /// Creates the script of multi-sig contract.
        /// </summary>
        /// <param name="m">The minimum number of correct signatures that need to be provided in order for the verification to pass.</param>
        /// <param name="publicKeys">The public keys of the contract.</param>
        /// <returns>The created script.</returns>
        public static byte[] CreateMultiSigRedeemScript(int m, IReadOnlyCollection<ECPoint> publicKeys)
        {
            if (!(1 <= m && m <= publicKeys.Count && publicKeys.Count <= 1024))
                throw new ArgumentException();
            using ScriptBuilder sb = new();
            sb.EmitPush(m);
            foreach (ECPoint publicKey in publicKeys.OrderBy(p => p))
            {
                sb.EmitPush(publicKey.EncodePoint(true));
            }
            sb.EmitPush(publicKeys.Count);
            sb.EmitSysCall(ApplicationEngine.System_Crypto_CheckMultisig);
            return sb.ToArray();
        }

        /// <summary>
        /// Creates a signature contract.
        /// </summary>
        /// <param name="publicKey">The public key of the contract.</param>
        /// <returns>The created contract.</returns>
        public static Contract CreateSignatureContract(ECPoint publicKey)
        {
            return new Contract
            {
                Script = CreateSignatureRedeemScript(publicKey),
                ParameterList = new[] { ContractParameterType.Signature }
            };
        }

        /// <summary>
        /// Creates the script of signature contract.
        /// </summary>
        /// <param name="publicKey">The public key of the contract.</param>
        /// <returns>The created script.</returns>
        public static byte[] CreateSignatureRedeemScript(ECPoint publicKey)
        {
            using ScriptBuilder sb = new();
            sb.EmitPush(publicKey.EncodePoint(true));
            sb.EmitSysCall(ApplicationEngine.System_Crypto_CheckSig);
            return sb.ToArray();
        }

        /// <summary>
        /// Gets the BFT address for the specified public keys.
        /// </summary>
        /// <param name="pubkeys">The public keys to be used.</param>
        /// <returns>The BFT address.</returns>
        public static UInt160 GetBFTAddress(IReadOnlyCollection<ECPoint> pubkeys)
        {
            return CreateMultiSigRedeemScript(pubkeys.Count - (pubkeys.Count - 1) / 3, pubkeys).ToScriptHash();
        }
    }
}
