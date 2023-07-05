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
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using System;
using System.Linq;
using static Neo.SmartContract.Helper;

namespace Neo.Network.P2P.Payloads
{
    /// <summary>
    /// Represents a transaction.
    /// </summary>
    public partial class Transaction : IEquatable<Transaction>, IInventory, IInteroperable
    {
        public bool VerifyPartialStateDependent(ProtocolSettings settings, DataCache snapshot)
        {
            if (NativeContract.Ledger.ContainsTransaction(snapshot, Hash)) return false;
            uint height = NativeContract.Ledger.CurrentIndex(snapshot);
            if (ValidUntilBlock <= height || ValidUntilBlock > height + settings.MaxValidUntilBlockIncrement)
                return false;
            UInt160[] hashes = GetScriptHashesForVerifying(snapshot);
            if (hashes.Any(hash => NativeContract.Policy.IsBlocked(snapshot, hash)))
            {
                return false;
            }
            var nvb = GetAttribute<NotValidBefore>();
            if (nvb is not null)
            {
                var maxNVBDelta = NativeContract.Notary.GetMaxNotValidBeforeDelta(snapshot);
                if (height + maxNVBDelta < nvb.Height) return false;
                if (nvb.Height + maxNVBDelta < ValidUntilBlock) return false;
            }
            var notaryAssisted = GetAttribute<NotaryAssisted>();
            long notary_fee = (notaryAssisted.NKeys + 1) * NativeContract.Notary.GetNotaryServiceFeePerKey(snapshot);
            long net_fee = NetworkFee - Size * NativeContract.Policy.GetFeePerByte(snapshot) - notary_fee;
            if (net_fee < 0) return false;
            if (net_fee > MaxVerificationGas) net_fee = MaxVerificationGas;
            uint execFeeFactor = NativeContract.Policy.GetExecFeeFactor(snapshot);
            for (int i = 0; i < hashes.Length; i++)
            {
                if (IsSignatureContract(witnesses[i].VerificationScript.Span))
                    net_fee -= execFeeFactor * SignatureContractCost();
                else if (IsMultiSigContract(witnesses[i].VerificationScript.Span, out int m, out int n))
                {
                    net_fee -= execFeeFactor * MultiSignatureContractCost(m, n);
                }
                else
                {
                    if (!this.VerifyWitness(settings, snapshot, hashes[i], witnesses[i], net_fee, out long fee))
                        continue;
                    net_fee -= fee;
                }
                if (net_fee < 0) return false;
            }
            return true;
        }

        public bool VerifyPartialStateIndependent(ProtocolSettings settings, bool verifySig = true)
        {
            if (Size > MaxTransactionSize) return false;
            try
            {
                _ = new Script(Script, true);
            }
            catch (BadScriptException)
            {
                return false;
            }
            if (!verifySig) return true;
            UInt160[] hashes = GetScriptHashesForVerifying(null);
            for (int i = 0; i < hashes.Length; i++)
            {
                if (IsSignatureContract(witnesses[i].VerificationScript.Span))
                {
                    if (hashes[i] != witnesses[i].ScriptHash) return false;
                    var pubkey = witnesses[i].VerificationScript.Span[2..35];
                    try
                    {
                        if (!Crypto.VerifySignature(this.GetSignData(settings.Network), witnesses[i].InvocationScript.Span[2..], pubkey, ECCurve.Secp256r1))
                            return false;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
