// Copyright (C) 2015-2023 The Neo Project.
//
// The neo is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.SmartContract;
using Neo.Wallets.NEP6;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using ECPoint = Neo.Cryptography.ECC.ECPoint;

namespace Neo.Wallets;

/// <summary>
/// The base class of wallets.
/// </summary>
public abstract partial class Wallet
{
    /// <summary>
    /// Deletes an account from the wallet.
    /// </summary>
    /// <param name="scriptHash">The hash of the account.</param>
    /// <returns><see langword="true"/> if the account is removed; otherwise, <see langword="false"/>.</returns>
    public abstract bool DeleteAccount(UInt160 scriptHash);

    /// <summary>
    /// Gets the account with the specified hash.
    /// </summary>
    /// <param name="scriptHash">The hash of the account.</param>
    /// <returns>The account with the specified hash.</returns>
    public abstract WalletAccount GetAccount(UInt160 scriptHash);

    /// <summary>
    /// Gets all the accounts from the wallet.
    /// </summary>
    /// <returns>All accounts in the wallet.</returns>
    public abstract IEnumerable<WalletAccount> GetAccounts();

    /// <summary>
    /// Gets the account with the specified public key.
    /// </summary>
    /// <param name="pubkey">The public key of the account.</param>
    /// <returns>The account with the specified public key.</returns>
    public WalletAccount GetAccount(ECPoint pubkey)
    {
        return GetAccount(Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash());
    }

    /// <summary>
    /// Gets the default account of the wallet.
    /// </summary>
    /// <returns>The default account of the wallet.</returns>
    public virtual WalletAccount GetDefaultAccount()
    {
        WalletAccount first = null;
        foreach (WalletAccount account in GetAccounts())
        {
            if (account.IsDefault) return account;
            if (first == null) first = account;
        }
        return first;
    }

    /// <summary>
    /// Determines whether the specified account is included in the wallet.
    /// </summary>
    /// <param name="scriptHash">The hash of the account.</param>
    /// <returns><see langword="true"/> if the account is included in the wallet; otherwise, <see langword="false"/>.</returns>
    public abstract bool Contains(UInt160 scriptHash);

    /// <summary>
    /// Creates a standard account with the specified private key.
    /// </summary>
    /// <param name="privateKey">The private key of the account.</param>
    /// <returns>The created account.</returns>
    public abstract WalletAccount CreateAccount(byte[] privateKey);

    /// <summary>
    /// Creates a contract account for the wallet.
    /// </summary>
    /// <param name="contract">The contract of the account.</param>
    /// <param name="key">The private key of the account.</param>
    /// <returns>The created account.</returns>
    public abstract WalletAccount CreateAccount(Contract contract, KeyPair key = null);


    /// <summary>
    /// Creates a watch-only account for the wallet.
    /// </summary>
    /// <param name="scriptHash">The hash of the account.</param>
    /// <returns>The created account.</returns>
    public abstract WalletAccount CreateAccount(UInt160 scriptHash);


    /// <summary>
    /// Creates a standard account for the wallet.
    /// </summary>
    /// <returns>The created account.</returns>
    public WalletAccount CreateAccount()
    {
        byte[] privateKey = new byte[32];
    generate:
        try
        {
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }
            return CreateAccount(privateKey);
        }
        catch (ArgumentException)
        {
            goto generate;
        }
        finally
        {
            Array.Clear(privateKey, 0, privateKey.Length);
        }
    }

    /// <summary>
    /// Creates a contract account for the wallet.
    /// </summary>
    /// <param name="contract">The contract of the account.</param>
    /// <param name="privateKey">The private key of the account.</param>
    /// <returns>The created account.</returns>
    public WalletAccount CreateAccount(Contract contract, byte[] privateKey)
    {
        if (privateKey == null) return CreateAccount(contract);
        return CreateAccount(contract, new KeyPair(privateKey));
    }

    private static List<(UInt160 Account, BigInteger Value)> FindPayingAccounts(List<(UInt160 Account, BigInteger Value)> orderedAccounts, BigInteger amount)
    {
        var result = new List<(UInt160 Account, BigInteger Value)>();
        BigInteger sum_balance = orderedAccounts.Select(p => p.Value).Sum();
        if (sum_balance == amount)
        {
            result.AddRange(orderedAccounts);
            orderedAccounts.Clear();
        }
        else
        {
            for (int i = 0; i < orderedAccounts.Count; i++)
            {
                if (orderedAccounts[i].Value < amount)
                    continue;
                if (orderedAccounts[i].Value == amount)
                {
                    result.Add(orderedAccounts[i]);
                    orderedAccounts.RemoveAt(i);
                }
                else
                {
                    result.Add((orderedAccounts[i].Account, amount));
                    orderedAccounts[i] = (orderedAccounts[i].Account, orderedAccounts[i].Value - amount);
                }
                break;
            }
            if (result.Count == 0)
            {
                int i = orderedAccounts.Count - 1;
                while (orderedAccounts[i].Value <= amount)
                {
                    result.Add(orderedAccounts[i]);
                    amount -= orderedAccounts[i].Value;
                    orderedAccounts.RemoveAt(i);
                    i--;
                }
                if (amount > 0)
                {
                    for (i = 0; i < orderedAccounts.Count; i++)
                    {
                        if (orderedAccounts[i].Value < amount)
                            continue;
                        if (orderedAccounts[i].Value == amount)
                        {
                            result.Add(orderedAccounts[i]);
                            orderedAccounts.RemoveAt(i);
                        }
                        else
                        {
                            result.Add((orderedAccounts[i].Account, amount));
                            orderedAccounts[i] = (orderedAccounts[i].Account, orderedAccounts[i].Value - amount);
                        }
                        break;
                    }
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Imports an account from a <see cref="X509Certificate2"/>.
    /// </summary>
    /// <param name="cert">The <see cref="X509Certificate2"/> to import.</param>
    /// <returns>The imported account.</returns>
    public virtual WalletAccount Import(X509Certificate2 cert)
    {
        byte[] privateKey;
        using (ECDsa ecdsa = cert.GetECDsaPrivateKey())
        {
            privateKey = ecdsa.ExportParameters(true).D;
        }
        WalletAccount account = CreateAccount(privateKey);
        Array.Clear(privateKey, 0, privateKey.Length);
        return account;
    }

    /// <summary>
    /// Imports an account from the specified WIF string.
    /// </summary>
    /// <param name="wif">The WIF string to import.</param>
    /// <returns>The imported account.</returns>
    public virtual WalletAccount Import(string wif)
    {
        byte[] privateKey = GetPrivateKeyFromWIF(wif);
        WalletAccount account = CreateAccount(privateKey);
        Array.Clear(privateKey, 0, privateKey.Length);
        return account;
    }

    /// <summary>
    /// Imports an account from the specified NEP-2 string.
    /// </summary>
    /// <param name="nep2">The NEP-2 string to import.</param>
    /// <param name="passphrase">The passphrase of the private key.</param>
    /// <param name="N">The N field of the <see cref="ScryptParameters"/> to be used.</param>
    /// <param name="r">The R field of the <see cref="ScryptParameters"/> to be used.</param>
    /// <param name="p">The P field of the <see cref="ScryptParameters"/> to be used.</param>
    /// <returns>The imported account.</returns>
    public virtual WalletAccount Import(string nep2, string passphrase, int N = 16384, int r = 8, int p = 8)
    {
        byte[] privateKey = GetPrivateKeyFromNEP2(nep2, passphrase, ProtocolSettings.AddressVersion, N, r, p);
        WalletAccount account = CreateAccount(privateKey);
        Array.Clear(privateKey, 0, privateKey.Length);
        return account;
    }

}
