// Copyright (C) 2015-2023 The Neo Project.
//
// The neo is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets.NEP6;
using Org.BouncyCastle.Crypto.Generators;
using System;
using System.Text;
using static Neo.SmartContract.Helper;
using static Neo.Wallets.Helper;
using ECPoint = Neo.Cryptography.ECC.ECPoint;

namespace Neo.Wallets;

/// <summary>
/// The base class of wallets.
/// </summary>
public abstract partial class Wallet
{
    /// <summary>
    /// Decodes a private key from the specified NEP-2 string.
    /// </summary>
    /// <param name="nep2">The NEP-2 string to be decoded.</param>
    /// <param name="passphrase">The passphrase of the private key.</param>
    /// <param name="version">The address version of NEO system.</param>
    /// <param name="N">The N field of the <see cref="ScryptParameters"/> to be used.</param>
    /// <param name="r">The R field of the <see cref="ScryptParameters"/> to be used.</param>
    /// <param name="p">The P field of the <see cref="ScryptParameters"/> to be used.</param>
    /// <returns>The decoded private key.</returns>
    public static byte[] GetPrivateKeyFromNEP2(string nep2, string passphrase, byte version, int N = 16384, int r = 8, int p = 8)
    {
        byte[] passphrasedata = Encoding.UTF8.GetBytes(passphrase);
        try
        {
            return GetPrivateKeyFromNEP2(nep2, passphrasedata, version, N, r, p);
        }
        finally
        {
            passphrasedata.AsSpan().Clear();
        }
    }

    /// <summary>
    /// Decodes a private key from the specified NEP-2 string.
    /// </summary>
    /// <param name="nep2">The NEP-2 string to be decoded.</param>
    /// <param name="passphrase">The passphrase of the private key.</param>
    /// <param name="version">The address version of NEO system.</param>
    /// <param name="N">The N field of the <see cref="ScryptParameters"/> to be used.</param>
    /// <param name="r">The R field of the <see cref="ScryptParameters"/> to be used.</param>
    /// <param name="p">The P field of the <see cref="ScryptParameters"/> to be used.</param>
    /// <returns>The decoded private key.</returns>
    public static byte[] GetPrivateKeyFromNEP2(string nep2, byte[] passphrase, byte version, int N = 16384, int r = 8, int p = 8)
    {
        if (nep2 == null) throw new ArgumentNullException(nameof(nep2));
        if (passphrase == null) throw new ArgumentNullException(nameof(passphrase));
        byte[] data = nep2.Base58CheckDecode();
        if (data.Length != 39 || data[0] != 0x01 || data[1] != 0x42 || data[2] != 0xe0)
            throw new FormatException();
        byte[] addresshash = new byte[4];
        Buffer.BlockCopy(data, 3, addresshash, 0, 4);
        byte[] derivedkey = SCrypt.Generate(passphrase, addresshash, N, r, p, 64);
        byte[] derivedhalf1 = derivedkey[..32];
        byte[] derivedhalf2 = derivedkey[32..];
        Array.Clear(derivedkey, 0, derivedkey.Length);
        byte[] encryptedkey = new byte[32];
        Buffer.BlockCopy(data, 7, encryptedkey, 0, 32);
        Array.Clear(data, 0, data.Length);
        byte[] prikey = XOR(Decrypt(encryptedkey, derivedhalf2), derivedhalf1);
        Array.Clear(derivedhalf1, 0, derivedhalf1.Length);
        Array.Clear(derivedhalf2, 0, derivedhalf2.Length);
        ECPoint pubkey = Cryptography.ECC.ECCurve.Secp256r1.G * prikey;
        UInt160 script_hash = Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash();
        string address = script_hash.ToAddress(version);
        if (!Encoding.ASCII.GetBytes(address).Sha256().Sha256().AsSpan(0, 4).SequenceEqual(addresshash))
            throw new FormatException();
        return prikey;
    }

    /// <summary>
    /// Decodes a private key from the specified WIF string.
    /// </summary>
    /// <param name="wif">The WIF string to be decoded.</param>
    /// <returns>The decoded private key.</returns>
    public static byte[] GetPrivateKeyFromWIF(string wif)
    {
        if (wif is null) throw new ArgumentNullException(nameof(wif));
        byte[] data = wif.Base58CheckDecode();
        if (data.Length != 34 || data[0] != 0x80 || data[33] != 0x01)
            throw new FormatException();
        byte[] privateKey = new byte[32];
        Buffer.BlockCopy(data, 1, privateKey, 0, privateKey.Length);
        Array.Clear(data, 0, data.Length);
        return privateKey;
    }

    /// <summary>
    /// Calculates the network fee for the specified transaction.
    /// </summary>
    /// <param name="snapshot">The snapshot used to read data.</param>
    /// <param name="tx">The transaction to calculate.</param>
    /// <returns>The network fee of the transaction.</returns>
    public long CalculateNetworkFee(DataCache snapshot, Transaction tx)
    {
        UInt160[] hashes = tx.GetScriptHashesForVerifying(snapshot);

        // base size for transaction: includes const_header + signers + attributes + script + hashes
        int size = Transaction.HeaderSize + tx.Signers.GetVarSize() + tx.Attributes.GetVarSize() + tx.Script.GetVarSize() + IO.Helper.GetVarSize(hashes.Length);
        uint exec_fee_factor = NativeContract.Policy.GetExecFeeFactor(snapshot);
        long networkFee = 0;
        int index = -1;
        foreach (UInt160 hash in hashes)
        {
            index++;
            byte[] witness_script = GetAccount(hash)?.Contract?.Script;
            byte[] invocationScript = null;

            if (tx.Witnesses != null)
            {
                if (witness_script is null)
                {
                    // Try to find the script in the witnesses
                    Witness witness = tx.Witnesses[index];
                    witness_script = witness?.VerificationScript.ToArray();

                    if (witness_script is null || witness_script.Length == 0)
                    {
                        // Then it's a contract-based witness, so try to get the corresponding invocation script for it
                        invocationScript = witness?.InvocationScript.ToArray();
                    }
                }
            }

            if (witness_script is null || witness_script.Length == 0)
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
                using ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Verification, tx, snapshot.CreateSnapshot(), settings: ProtocolSettings);
                engine.LoadContract(contract, md, CallFlags.ReadOnly);
                if (invocationScript != null) engine.LoadScript(invocationScript, configureState: p => p.CallFlags = CallFlags.None);
                if (engine.Execute() == VMState.FAULT) throw new ArgumentException($"Smart contract {contract.Hash} verification fault.");
                if (!engine.ResultStack.Pop().GetBoolean()) throw new ArgumentException($"Smart contract {contract.Hash} returns false.");

                networkFee += engine.GasConsumed;
            }
            else if (IsSignatureContract(witness_script))
            {
                size += 67 + witness_script.GetVarSize();
                networkFee += exec_fee_factor * SignatureContractCost();
            }
            else if (IsMultiSigContract(witness_script, out int m, out int n))
            {
                int size_inv = 66 * m;
                size += IO.Helper.GetVarSize(size_inv) + size_inv + witness_script.GetVarSize();
                networkFee += exec_fee_factor * MultiSignatureContractCost(m, n);
            }
            else
            {
                //We can support more contract types in the future.
            }
        }
        networkFee += size * NativeContract.Policy.GetFeePerByte(snapshot);
        return networkFee;
    }

}
