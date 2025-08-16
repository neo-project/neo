// Copyright (C) 2015-2025 The Neo Project.
//
// DevWallet.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Exceptions;
using Neo.Build.Core.Exceptions.Wallet;
using Neo.Build.Core.Interfaces;
using Neo.Build.Core.Models;
using Neo.Build.Core.Models.Wallets;
using Neo.Cryptography.ECC;
using Neo.SmartContract;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Build.Core.Wallets
{
    using Helper = Neo.SmartContract.Helper;

    /// <summary>
    /// Developer wallet.
    /// </summary>
    public class DevWallet : Wallet, IConvertToObject<WalletModel>
    {
        /// <summary>
        /// Creates a new developer wallet.
        /// </summary>
        /// <param name="walletModel">Wallet <see cref="JsonModel"/>.</param>
        /// <param name="protocolSettings"><see cref="ProtocolSettings"/> to be used with this wallet.</param>
        /// <exception cref="NeoBuildInvalidVersionFormatException"></exception>
        public DevWallet(
            WalletModel walletModel,
            ProtocolSettings protocolSettings)
            : base(string.Empty, protocolSettings)
        {
            if (walletModel.Version is null)
                throw new NeoBuildWalletInvalidVersionException(string.Empty);

            if (walletModel.Version != Version)
                throw new NeoBuildWalletInvalidVersionException(walletModel.Version);

            _walletName = walletModel.Name;
            _sCryptParameters = walletModel.SCrypt ?? SCryptModel.Default;

            if (walletModel.Accounts != null)
            {
                foreach (var account in walletModel.Accounts)
                {
                    if (account is null) continue;
                    if (account.Address is null) continue;
                    _walletAccounts[account.Address] = new(account, ProtocolSettings);
                }
            }
        }

        public DevWallet(
            string filename,
            ProtocolSettings protocolSettings)
            : base(filename, protocolSettings)
        {
            _fileInfo = new FileInfo(filename);

            if (_fileInfo.Exists == false) return;

            var walletModel = JsonModel.FromJson<WalletModel>(_fileInfo);

            if (walletModel is null) return;

            if (walletModel.Version is null)
                throw new NeoBuildWalletInvalidVersionException(string.Empty);

            if (walletModel.Version != Version)
                throw new NeoBuildWalletInvalidVersionException(walletModel.Version);

            _walletName = walletModel.Name;
            _sCryptParameters = walletModel.SCrypt ?? SCryptModel.Default;

            if (walletModel.Accounts != null)
            {
                foreach (var account in walletModel.Accounts)
                {
                    if (account is null) continue;
                    if (account.Address is null) continue;
                    _walletAccounts[account.Address] = new(account, ProtocolSettings);
                }
            }
        }

        public DevWallet(WalletModel walletModel) : this(walletModel, walletModel.Extra?.ProtocolConfiguration?.ToObject() ?? ProtocolSettings.Default) { }
        public DevWallet() : base(string.Empty, ProtocolSettings.Default) { }
        public DevWallet(ProtocolSettings protocolSettings) : base(string.Empty, protocolSettings) { }

        private readonly ConcurrentDictionary<UInt160, DevWalletAccount> _walletAccounts = new();
        private readonly string? _walletName;
        private readonly SCryptModel _sCryptParameters = SCryptModel.Default;

        private FileInfo? _fileInfo;

        public ScryptParameters SCryptParameters => _sCryptParameters.ToObject();

        public override string? Name => _walletName;

        public override Version Version => new(1, 0);

        public override bool Contains(UInt160 scriptHash) =>
            _walletAccounts.ContainsKey(scriptHash);

        public WalletAccount CreateMultiSigAccount(ECPoint[] publicKeys, string? name = null, bool isDefaultAccount = false)
        {
            var contract = Contract.CreateMultiSigContract(publicKeys.Length, publicKeys);
            var account = _walletAccounts.Values.FirstOrDefault(
                f =>
                    f.HasKey &&
                    f.Lock == false &&
                    publicKeys.Contains(f.GetKey().PublicKey));

            var newAccount = CreateAccount(contract, account?.GetKey(), name);
            newAccount.IsDefault = isDefaultAccount;

            return newAccount;
        }

        public override WalletAccount CreateAccount(byte[] privateKey) =>
            CreateAccount(privateKey, null);

        public WalletAccount CreateAccount(byte[] privateKey, string? name = null)
        {
            var kp = new KeyPair(privateKey);
            var c = Contract.CreateSignatureContract(kp.PublicKey);
            var wa = new DevWalletAccount(c, ProtocolSettings, kp, name);

            _ = _walletAccounts.TryAdd(wa.ScriptHash, wa);

            return wa;
        }

        public override WalletAccount CreateAccount(Contract contract, KeyPair? key = null) =>
            CreateAccount(contract, key, null);

        public WalletAccount CreateAccount(Contract contract, KeyPair? key = null, string? name = null)
        {
            var wa = new DevWalletAccount(contract, ProtocolSettings, key, name);

            _ = _walletAccounts.TryAdd(wa.ScriptHash, wa);

            return wa;
        }

        public override WalletAccount CreateAccount(UInt160 scriptHash) =>
            CreateAccount(scriptHash, null);

        public WalletAccount CreateAccount(UInt160 scriptHash, string? name = null)
        {
            var wa = new DevWalletAccount(scriptHash, ProtocolSettings, name);

            _ = _walletAccounts.TryAdd(wa.ScriptHash, wa);

            return wa;
        }

        public override bool DeleteAccount(UInt160 scriptHash)
        {
            var account = GetAccount(scriptHash);
            if (account is null)
                return false;
            return _walletAccounts.TryRemove(scriptHash, out _);
        }

        public bool DeleteAccount(string name)
        {
            var account = GetAccount(name);
            if (account is null)
                return false;
            return DeleteAccount(account.ScriptHash);
        }

        public override WalletAccount? GetAccount(UInt160 scriptHash)
        {
            if (_walletAccounts.TryGetValue(scriptHash, out var wa))
            {
                if (wa is not null and { Lock: false })
                    return wa;
                throw new NeoBuildWalletAccountLockedException($"{scriptHash}");
            }

            return null;
        }

        public IEnumerable<DevWalletAccount> GetMultiSigAddressAccounts() =>
            _walletAccounts
                .Where(static w =>
                    w.Value.Lock == false &&
                    Helper.IsMultiSigContract(w.Value.Contract.Script))
                .Select(static s => s.Value);

        public IEnumerable<DevWalletAccount> GetConsensusAccounts() =>
            _walletAccounts
                .Where(static w =>
                    w.Value.HasKey &&
                    w.Value.IsDefault &&
                    w.Value.Lock == false &&
                    Helper.IsMultiSigContract(w.Value.Contract.Script))
                .Select(static s => s.Value);

        public WalletAccount? GetAccount(string name) =>
            _walletAccounts.Values.Where(w =>
                    w.Lock == false &&
                    w.Label.Equals(name))
                .FirstOrDefault();

        public override IEnumerable<WalletAccount> GetAccounts() =>
            _walletAccounts.Values
                .Where(static w => w.Lock == false);

        public override void Delete()
        {
            if (_fileInfo is null)
                // TODO: Add its own exception class
                throw new NeoBuildException("Memory only wallet!", NeoBuildErrorCodes.General.PathNotFound);

            _fileInfo.Delete();
        }

        public override void Save()
        {
            if (_fileInfo is null)
                // TODO: Add its own exception class
                throw new NeoBuildException("Memory only wallet!", NeoBuildErrorCodes.General.PathNotFound);

            File.WriteAllText(_fileInfo.FullName, ToString());
        }

        public void Save(string filename)
        {
            _fileInfo = new(filename);
            File.WriteAllText(_fileInfo.FullName, ToString());
        }

        public override bool ChangePassword(string oldPassword, string newPassword) =>
            throw new NotImplementedException();

        public override bool VerifyPassword(string password) =>
            throw new NotImplementedException();

        /// <summary>
        /// Converts to a <see cref="JsonModel"/>.
        /// </summary>
        /// <returns>A <see cref="JsonModel"/> that can be serialized to a JSON string.</returns>
        public WalletModel ToObject() =>
            new()
            {
                Name = Name,
                Version = Version,
                SCrypt = _sCryptParameters,
                Accounts = [.. _walletAccounts.Values.Select(static s => s.ToObject())],
                Extra = new()
                {
                    ProtocolConfiguration = new()
                    {
                        Network = ProtocolSettings.Network,
                        AddressVersion = ProtocolSettings.AddressVersion,
                        MillisecondsPerBlock = ProtocolSettings.MillisecondsPerBlock,
                        MaxTransactionsPerBlock = ProtocolSettings.MaxTransactionsPerBlock,
                        MemoryPoolMaxTransactions = ProtocolSettings.MemoryPoolMaxTransactions,
                        MaxTraceableBlocks = ProtocolSettings.MaxTraceableBlocks,
                        InitialGasDistribution = ProtocolSettings.InitialGasDistribution,
                        ValidatorsCount = ProtocolSettings.ValidatorsCount,
                        SeedList = ProtocolSettings.SeedList,
                        Hardforks = ProtocolSettings.Hardforks,
                        StandbyCommittee = [.. ProtocolSettings.StandbyCommittee],
                    },
                },
            };

        /// <summary>
        /// <see cref="DevWallet"/> as JSON string.
        /// </summary>
        /// <returns>JSON</returns>
        public override string ToString() =>
            ToObject().ToString();
    }
}
