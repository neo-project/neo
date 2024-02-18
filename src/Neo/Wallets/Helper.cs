// Copyright (C) 2015-2024 The Neo Project.
//
// Helper.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography;
using Neo.IO;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using System;
using static Neo.SmartContract.Helper;

namespace Neo.Wallets
{
    /// <summary>
    /// A helper class related to wallets.
    /// </summary>
    public static class Helper
    {
        /// <summary>
        /// Signs an <see cref="IVerifiable"/> with the specified private key.
        /// </summary>
        /// <param name="verifiable">The <see cref="IVerifiable"/> to sign.</param>
        /// <param name="key">The private key to be used.</param>
        /// <param name="network">The magic number of the NEO network.</param>
        /// <returns>The signature for the <see cref="IVerifiable"/>.</returns>
        public static byte[] Sign(this IVerifiable verifiable, KeyPair key, uint network)
        {
            return Crypto.Sign(verifiable.GetSignData(network), key.PrivateKey, key.PublicKey.EncodePoint(false)[1..]);
        }

        /// <summary>
        /// Converts the specified script hash to an address.
        /// </summary>
        /// <param name="scriptHash">The script hash to convert.</param>
        /// <param name="version">The address version.</param>
        /// <returns>The converted address.</returns>
        public static string ToAddress(this UInt160 scriptHash, byte version)
        {
            Span<byte> data = stackalloc byte[21];
            data[0] = version;
            scriptHash.ToArray().CopyTo(data[1..]);
            return Base58.Base58CheckEncode(data);
        }

        /// <summary>
        /// Converts the specified address to a script hash.
        /// </summary>
        /// <param name="address">The address to convert.</param>
        /// <param name="version">The address version.</param>
        /// <returns>The converted script hash.</returns>
        public static UInt160 ToScriptHash(this string address, byte version)
        {
            byte[] data = address.Base58CheckDecode();
            if (data.Length != 21)
                throw new FormatException();
            if (data[0] != version)
                throw new FormatException();
            return new UInt160(data.AsSpan(1));
        }

        internal static byte[] XOR(byte[] x, byte[] y)
        {
            if (x.Length != y.Length) throw new ArgumentException();
            byte[] r = new byte[x.Length];
            for (int i = 0; i < r.Length; i++)
                r[i] = (byte)(x[i] ^ y[i]);
            return r;
        }

        /// <summary>
        /// Calculates the network fee for the specified transaction.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="tx">The transaction to calculate.</param>
        /// <param name="settings">Thr protocol settings to use.</param>
        /// <param name="accountScript">Function to retrive the script's account from a hash.</param>
        /// <returns>The network fee of the transaction.</returns>
        public static long CalculateNetworkFee(DataCache snapshot, Transaction tx, ProtocolSettings settings, Func<UInt160, byte[]> accountScript)
        {
            UInt160[] hashes = tx.GetScriptHashesForVerifying(snapshot);

            // base size for transaction: includes const_header + signers + attributes + script + hashes
            long maxExecutionCost = ApplicationEngine.TestModeGas;
            int size = Transaction.HeaderSize + tx.Signers.GetVarSize() + tx.Attributes.GetVarSize() + tx.Script.GetVarSize() + IO.Helper.GetVarSize(hashes.Length);
            uint exec_fee_factor = NativeContract.Policy.GetExecFeeFactor(snapshot);
            long networkFee = 0;
            int index = -1;
            foreach (UInt160 hash in hashes)
            {
                index++;
                byte[] witnessScript = accountScript(hash);
                byte[] invocationScript = null;

                if (tx.Witnesses != null && witnessScript is null)
                {
                    // Try to find the script in the witnesses
                    Witness witness = tx.Witnesses[index];
                    witnessScript = witness?.VerificationScript.ToArray();

                    if (witnessScript is null || witnessScript.Length == 0)
                    {
                        // Then it's a contract-based witness, so try to get the corresponding invocation script for it
                        invocationScript = witness?.InvocationScript.ToArray();
                    }
                }

                if (witnessScript is null || witnessScript.Length == 0)
                {
                    var contract = NativeContract.ContractManagement.GetContract(snapshot, hash);
                    if (contract is null)
                        throw new ArgumentException($"The smart contract or address {hash} is not found");
                    var md = contract.Manifest.Abi.GetMethod("verify", -1);
                    if (md is null)
                        throw new ArgumentException($"The smart contract {contract.Hash} haven't got verify method");
                    if (md.ReturnType != ContractParameterType.Boolean)
                        throw new ArgumentException("The verify method doesn't return boolean value.");
                    if (md.Parameters.Length > 0 && invocationScript is null)
                        throw new ArgumentException("The verify method requires parameters that need to be passed via the witness' invocation script.");

                    // Empty verification and non-empty invocation scripts
                    var invSize = invocationScript?.GetVarSize() ?? Array.Empty<byte>().GetVarSize();
                    size += Array.Empty<byte>().GetVarSize() + invSize;

                    // Check verify cost
                    using ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Verification, tx, snapshot.CreateSnapshot(), settings: settings, gas: maxExecutionCost);
                    engine.LoadContract(contract, md, CallFlags.ReadOnly);
                    if (invocationScript != null) engine.LoadScript(invocationScript, configureState: p => p.CallFlags = CallFlags.None);
                    if (engine.Execute() == VMState.FAULT) throw new ArgumentException($"Smart contract {contract.Hash} verification fault.");
                    if (!engine.ResultStack.Pop().GetBoolean()) throw new ArgumentException($"Smart contract {contract.Hash} returns false.");

                    maxExecutionCost -= engine.GasConsumed;
                    if (maxExecutionCost <= 0) throw new InvalidOperationException("Insufficient GAS.");
                    networkFee += engine.GasConsumed;
                }
                else if (IsSignatureContract(witnessScript))
                {
                    size += 67 + witnessScript.GetVarSize();
                    networkFee += exec_fee_factor * SignatureContractCost();
                }
                else if (IsMultiSigContract(witnessScript, out int m, out int n))
                {
                    int size_inv = 66 * m;
                    size += IO.Helper.GetVarSize(size_inv) + size_inv + witnessScript.GetVarSize();
                    networkFee += exec_fee_factor * MultiSignatureContractCost(m, n);
                }
                // We can support more contract types in the future.
            }
            networkFee += size * NativeContract.Policy.GetFeePerByte(snapshot);
            foreach (TransactionAttribute attr in tx.Attributes)
            {
                networkFee += attr.CalculateNetworkFee(snapshot, tx);
            }
            return networkFee;
        }
    }
}
