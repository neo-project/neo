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
using Neo.VM;
using Neo.Wallets.NEP6;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;

namespace Neo.Wallets;

/// <summary>
/// The base class of wallets.
/// </summary>
public abstract partial class Wallet
{
    private static readonly List<IWalletFactory> factories = new() { NEP6WalletFactory.Instance };

    /// <summary>
    /// The <see cref="Neo.ProtocolSettings"/> to be used by the wallet.
    /// </summary>
    public ProtocolSettings ProtocolSettings { get; }

    /// <summary>
    /// The name of the wallet.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// The path of the wallet.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// The version of the wallet.
    /// </summary>
    public abstract Version Version { get; }


    /// <summary>
    /// Deletes the entire database of the wallet.
    /// </summary>
    public abstract void Delete();

    /// <summary>
    /// Initializes a new instance of the <see cref="Wallet"/> class.
    /// </summary>
    /// <param name="path">The path of the wallet file.</param>
    /// <param name="settings">The <see cref="Neo.ProtocolSettings"/> to be used by the wallet.</param>
    protected Wallet(string path, ProtocolSettings settings)
    {
        this.ProtocolSettings = settings;
        this.Path = path;
    }

    /// <summary>
    /// Gets the available balance for the specified asset in the wallet.
    /// </summary>
    /// <param name="snapshot">The snapshot used to read data.</param>
    /// <param name="asset_id">The id of the asset.</param>
    /// <returns>The available balance for the specified asset.</returns>
    public BigDecimal GetAvailable(DataCache snapshot, UInt160 asset_id)
    {
        UInt160[] accounts = GetAccounts().Where(p => !p.WatchOnly).Select(p => p.ScriptHash).ToArray();
        return GetBalance(snapshot, asset_id, accounts);
    }

    /// <summary>
    /// Gets the balance for the specified asset in the wallet.
    /// </summary>
    /// <param name="snapshot">The snapshot used to read data.</param>
    /// <param name="asset_id">The id of the asset.</param>
    /// <param name="accounts">The accounts to be counted.</param>
    /// <returns>The balance for the specified asset.</returns>
    public BigDecimal GetBalance(DataCache snapshot, UInt160 asset_id, params UInt160[] accounts)
    {
        byte[] script;
        using (ScriptBuilder sb = new())
        {
            sb.EmitPush(0);
            foreach (UInt160 account in accounts)
            {
                sb.EmitDynamicCall(asset_id, "balanceOf", CallFlags.ReadOnly, account);
                sb.Emit(OpCode.ADD);
            }
            sb.EmitDynamicCall(asset_id, "decimals", CallFlags.ReadOnly);
            script = sb.ToArray();
        }
        using ApplicationEngine engine = ApplicationEngine.Run(script, snapshot, settings: ProtocolSettings, gas: 0_60000000L * accounts.Length);
        if (engine.State == VMState.FAULT)
            return new BigDecimal(BigInteger.Zero, 0);
        byte decimals = (byte)engine.ResultStack.Pop().GetInteger();
        BigInteger amount = engine.ResultStack.Pop().GetInteger();
        return new BigDecimal(amount, decimals);
    }

    private static byte[] Decrypt(byte[] data, byte[] key)
    {
        using Aes aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        using ICryptoTransform decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(data, 0, data.Length);
    }

    private static Signer[] GetSigners(UInt160 sender, Signer[] cosigners)
    {
        for (int i = 0; i < cosigners.Length; i++)
        {
            if (cosigners[i].Account.Equals(sender))
            {
                if (i == 0) return cosigners;
                List<Signer> list = new(cosigners);
                list.RemoveAt(i);
                list.Insert(0, cosigners[i]);
                return list.ToArray();
            }
        }
        return cosigners.Prepend(new Signer
        {
            Account = sender,
            Scopes = WitnessScope.None
        }).ToArray();
    }

    public static Wallet Create(string name, string path, string password, ProtocolSettings settings)
    {
        return GetFactory(path)?.CreateWallet(name, path, password, settings);
    }

    private static IWalletFactory GetFactory(string path)
    {
        return factories.FirstOrDefault(p => p.Handle(path));
    }

    public static void RegisterFactory(IWalletFactory factory)
    {
        factories.Add(factory);
    }
}
