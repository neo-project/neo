// Copyright (C) 2015-2023 The Neo Project.
//
// The neo is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static Neo.SmartContract.Helper;
using ECPoint = Neo.Cryptography.ECC.ECPoint;

namespace Neo.Wallets;

/// <summary>
/// The base class of wallets.
/// </summary>
public abstract partial class Wallet
{
    /// <summary>
    /// Changes the password of the wallet.
    /// </summary>
    /// <param name="oldPassword">The old password of the wallet.</param>
    /// <param name="newPassword">The new password to be used.</param>
    /// <returns><see langword="true"/> if the password is changed successfully; otherwise, <see langword="false"/>.</returns>
    public abstract bool ChangePassword(string oldPassword, string newPassword);

    /// <summary>
    /// Makes a transaction to transfer assets.
    /// </summary>
    /// <param name="snapshot">The snapshot used to read data.</param>
    /// <param name="outputs">The array of <see cref="TransferOutput"/> that contain the asset, amount, and targets of the transfer.</param>
    /// <param name="from">The account to transfer from.</param>
    /// <param name="cosigners">The cosigners to be added to the transaction.</param>
    /// <param name="persistingBlock">The block environment to execute the transaction. If null, <see cref="ApplicationEngine.CreateDummyBlock"></see> will be used.</param>
    /// <returns>The created transaction.</returns>
    public Transaction MakeTransaction(DataCache snapshot, TransferOutput[] outputs, UInt160 from = null, Signer[] cosigners = null, Block persistingBlock = null)
    {
        UInt160[] accounts;
        if (from is null)
        {
            accounts = GetAccounts().Where(p => !p.Lock && !p.WatchOnly).Select(p => p.ScriptHash).ToArray();
        }
        else
        {
            accounts = new[] { from };
        }
        Dictionary<UInt160, Signer> cosignerList = cosigners?.ToDictionary(p => p.Account) ?? new Dictionary<UInt160, Signer>();
        byte[] script;
        List<(UInt160 Account, BigInteger Value)> balances_gas = null;
        using (ScriptBuilder sb = new())
        {
            foreach (var (assetId, group, sum) in outputs.GroupBy(p => p.AssetId, (k, g) => (k, g, g.Select(p => p.Value.Value).Sum())))
            {
                var balances = new List<(UInt160 Account, BigInteger Value)>();
                foreach (UInt160 account in accounts)
                {
                    using ScriptBuilder sb2 = new();
                    sb2.EmitDynamicCall(assetId, "balanceOf", CallFlags.ReadOnly, account);
                    using ApplicationEngine engine = ApplicationEngine.Run(sb2.ToArray(), snapshot, settings: ProtocolSettings, persistingBlock: persistingBlock);
                    if (engine.State != VMState.HALT)
                        throw new InvalidOperationException($"Execution for {assetId}.balanceOf('{account}' fault");
                    BigInteger value = engine.ResultStack.Pop().GetInteger();
                    if (value.Sign > 0) balances.Add((account, value));
                }
                BigInteger sum_balance = balances.Select(p => p.Value).Sum();
                if (sum_balance < sum)
                    throw new InvalidOperationException($"It does not have enough balance, expected: {sum} found: {sum_balance}");
                foreach (TransferOutput output in group)
                {
                    balances = balances.OrderBy(p => p.Value).ToList();
                    var balances_used = FindPayingAccounts(balances, output.Value.Value);
                    foreach (var (account, value) in balances_used)
                    {
                        if (cosignerList.TryGetValue(account, out Signer signer))
                        {
                            if (signer.Scopes != WitnessScope.Global)
                                signer.Scopes |= WitnessScope.CalledByEntry;
                        }
                        else
                        {
                            cosignerList.Add(account, new Signer
                            {
                                Account = account,
                                Scopes = WitnessScope.CalledByEntry
                            });
                        }
                        sb.EmitDynamicCall(output.AssetId, "transfer", account, output.ScriptHash, value, output.Data);
                        sb.Emit(OpCode.ASSERT);
                    }
                }
                if (assetId.Equals(NativeContract.GAS.Hash))
                    balances_gas = balances;
            }
            script = sb.ToArray();
        }
        if (balances_gas is null)
            balances_gas = accounts.Select(p => (Account: p, Value: NativeContract.GAS.BalanceOf(snapshot, p))).Where(p => p.Value.Sign > 0).ToList();

        return MakeTransaction(snapshot, script, cosignerList.Values.ToArray(), Array.Empty<TransactionAttribute>(), balances_gas, persistingBlock: persistingBlock);
    }

    /// <summary>
    /// Makes a transaction to run a smart contract.
    /// </summary>
    /// <param name="snapshot">The snapshot used to read data.</param>
    /// <param name="script">The script to be loaded in the transaction.</param>
    /// <param name="sender">The sender of the transaction.</param>
    /// <param name="cosigners">The cosigners to be added to the transaction.</param>
    /// <param name="attributes">The attributes to be added to the transaction.</param>
    /// <param name="maxGas">The maximum gas that can be spent to execute the script.</param>
    /// <param name="persistingBlock">The block environment to execute the transaction. If null, <see cref="ApplicationEngine.CreateDummyBlock"></see> will be used.</param>
    /// <returns>The created transaction.</returns>
    public Transaction MakeTransaction(DataCache snapshot, ReadOnlyMemory<byte> script, UInt160 sender = null, Signer[] cosigners = null, TransactionAttribute[] attributes = null, long maxGas = ApplicationEngine.TestModeGas, Block persistingBlock = null)
    {
        UInt160[] accounts;
        if (sender is null)
        {
            accounts = GetAccounts().Where(p => !p.Lock && !p.WatchOnly).Select(p => p.ScriptHash).ToArray();
        }
        else
        {
            accounts = new[] { sender };
        }
        var balances_gas = accounts.Select(p => (Account: p, Value: NativeContract.GAS.BalanceOf(snapshot, p))).Where(p => p.Value.Sign > 0).ToList();
        return MakeTransaction(snapshot, script, cosigners ?? Array.Empty<Signer>(), attributes ?? Array.Empty<TransactionAttribute>(), balances_gas, maxGas, persistingBlock: persistingBlock);
    }

    private Transaction MakeTransaction(DataCache snapshot, ReadOnlyMemory<byte> script, Signer[] cosigners, TransactionAttribute[] attributes, List<(UInt160 Account, BigInteger Value)> balances_gas, long maxGas = ApplicationEngine.TestModeGas, Block persistingBlock = null)
    {
        Random rand = new();
        foreach (var (account, value) in balances_gas)
        {
            Transaction tx = new()
            {
                Version = 0,
                Nonce = (uint)rand.Next(),
                Script = script,
                ValidUntilBlock = NativeContract.Ledger.CurrentIndex(snapshot) + ProtocolSettings.MaxValidUntilBlockIncrement,
                Signers = GetSigners(account, cosigners),
                Attributes = attributes,
            };

            // will try to execute 'transfer' script to check if it works
            using (ApplicationEngine engine = ApplicationEngine.Run(script, snapshot.CreateSnapshot(), tx, settings: ProtocolSettings, gas: maxGas, persistingBlock: persistingBlock))
            {
                if (engine.State == VMState.FAULT)
                {
                    throw new InvalidOperationException($"Failed execution for '{Convert.ToBase64String(script.Span)}'", engine.FaultException);
                }
                tx.SystemFee = engine.GasConsumed;
            }

            tx.NetworkFee = CalculateNetworkFee(snapshot, tx);
            if (value >= tx.SystemFee + tx.NetworkFee) return tx;
        }
        throw new InvalidOperationException("Insufficient GAS");
    }

    /// <summary>
    /// Signs the <see cref="IVerifiable"/> in the specified <see cref="ContractParametersContext"/> with the wallet.
    /// </summary>
    /// <param name="context">The <see cref="ContractParametersContext"/> to be used.</param>
    /// <returns><see langword="true"/> if the signature is successfully added to the context; otherwise, <see langword="false"/>.</returns>
    public bool Sign(ContractParametersContext context)
    {
        if (context.Network != ProtocolSettings.Network) return false;
        bool fSuccess = false;
        foreach (UInt160 scriptHash in context.ScriptHashes)
        {
            WalletAccount account = GetAccount(scriptHash);

            if (account != null)
            {
                // Try to sign self-contained multiSig

                Contract multiSigContract = account.Contract;

                if (multiSigContract != null &&
                    IsMultiSigContract(multiSigContract.Script, out int m, out ECPoint[] points))
                {
                    foreach (var point in points)
                    {
                        account = GetAccount(point);
                        if (account?.HasKey != true) continue;
                        KeyPair key = account.GetKey();
                        byte[] signature = context.Verifiable.Sign(key, context.Network);
                        fSuccess |= context.AddSignature(multiSigContract, key.PublicKey, signature);
                        if (fSuccess) m--;
                        if (context.Completed || m <= 0) break;
                    }
                    continue;
                }
                else if (account.HasKey)
                {
                    // Try to sign with regular accounts
                    KeyPair key = account.GetKey();
                    byte[] signature = context.Verifiable.Sign(key, context.Network);
                    fSuccess |= context.AddSignature(account.Contract, key.PublicKey, signature);
                    continue;
                }
            }

            // Try Smart contract verification

            var contract = NativeContract.ContractManagement.GetContract(context.Snapshot, scriptHash);

            if (contract != null)
            {
                var deployed = new DeployedContract(contract);

                // Only works with verify without parameters

                if (deployed.ParameterList.Length == 0)
                {
                    fSuccess |= context.Add(deployed);
                }
            }
        }

        return fSuccess;
    }

    /// <summary>
    /// Checks that the specified password is correct for the wallet.
    /// </summary>
    /// <param name="password">The password to be checked.</param>
    /// <returns><see langword="true"/> if the password is correct; otherwise, <see langword="false"/>.</returns>
    public abstract bool VerifyPassword(string password);

}
