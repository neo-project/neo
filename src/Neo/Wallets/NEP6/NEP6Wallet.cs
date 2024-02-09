// Copyright (C) 2015-2024 The Neo Project.
//
// NEP6Wallet.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Neo.Wallets.NEP6
{
    /// <summary>
    /// An implementation of the NEP-6 wallet standard.
    /// </summary>
    /// <remarks>https://github.com/neo-project/proposals/blob/master/nep-6.mediawiki</remarks>
    public class NEP6Wallet : Wallet
    {
        private string _password;
        private string? name;
        private Version version = null!;
        private readonly Dictionary<UInt160, NEP6Account> _accounts;
        private readonly JToken? extra;

        /// <summary>
        /// The parameters of the SCrypt algorithm used for encrypting and decrypting the private keys in the wallet.
        /// </summary>
        public readonly ScryptParameters Scrypt;

        public override string? Name => name;

        /// <summary>
        /// The version of the wallet standard. It is currently fixed at 1.0 and will be used for functional upgrades in the future.
        /// </summary>
        public override Version Version => version;

        /// <summary>
        /// Loads or creates a wallet at the specified path.
        /// </summary>
        /// <param name="path">The path of the wallet file.</param>
        /// <param name="password">The password of the wallet.</param>
        /// <param name="settings">The <see cref="ProtocolSettings"/> to be used by the wallet.</param>
        /// <param name="name">The name of the wallet. If the wallet is loaded from an existing file, this parameter is ignored.</param>
        public NEP6Wallet(string path, string password, ProtocolSettings settings, string? name = null) : base(path, settings)
        {
            this._password = password;
            if (File.Exists(path))
            {
                JObject wallet = JToken.Parse(File.ReadAllBytes(path)).NullExceptionOr<JObject>();
                LoadFromJson(wallet, out Scrypt, out _accounts, out extra);
            }
            else
            {
                this.name = name;
                this.version = Version.Parse("1.0");
                this.Scrypt = ScryptParameters.Default;
                this._accounts = new Dictionary<UInt160, NEP6Account>();
                this.extra = JToken.Null;
            }
        }

        /// <summary>
        /// Loads the wallet with the specified JSON string.
        /// </summary>
        /// <param name="path">The path of the wallet.</param>
        /// <param name="password">The password of the wallet.</param>
        /// <param name="settings">The <see cref="ProtocolSettings"/> to be used by the wallet.</param>
        /// <param name="json">The JSON object representing the wallet.</param>
        public NEP6Wallet(string path, string password, ProtocolSettings settings, JObject json) : base(path, settings)
        {
            this._password = password;
            LoadFromJson(json, out Scrypt, out _accounts, out extra);
        }

        private void LoadFromJson(JObject wallet, out ScryptParameters scrypt, out Dictionary<UInt160, NEP6Account> accounts, out JToken? extra)
        {
            this.version = Version.Parse(wallet["version"]!.AsString());
            this.name = wallet["name"]?.AsString();
            scrypt = ScryptParameters.FromJson((JObject)wallet["scrypt"].NotNull());
            accounts = wallet["accounts"].NullExceptionOr<JArray>().Select(p => NEP6Account.FromJson(p.NullExceptionOr<JObject>(), this)).ToDictionary(p => p.ScriptHash);
            extra = wallet["extra"];
            if (!VerifyPasswordInternal(_password))
                throw new InvalidOperationException("Wrong password.");
        }

        private void AddAccount(NEP6Account account)
        {
            lock (_accounts)
            {
                if (_accounts.TryGetValue(account.ScriptHash, out NEP6Account? account_old))
                {
                    account.Label = account_old.Label;
                    account.IsDefault = account_old.IsDefault;
                    account.Lock = account_old.Lock;
                    if (account.Contract == null)
                    {
                        account.Contract = account_old.Contract;
                    }
                    else
                    {
                        NEP6Contract? contract_old = (NEP6Contract?)account_old.Contract;
                        if (contract_old != null)
                        {
                            NEP6Contract contract = (NEP6Contract)account.Contract;
                            contract.ParameterNames = contract_old.ParameterNames;
                            contract.Deployed = contract_old.Deployed;
                        }
                    }
                    account.Extra = account_old.Extra;
                }
                _accounts[account.ScriptHash] = account;
            }
        }

        public override bool Contains(UInt160 scriptHash)
        {
            lock (_accounts)
            {
                return _accounts.ContainsKey(scriptHash);
            }
        }

        public override WalletAccount CreateAccount(byte[] privateKey)
        {
            if (privateKey is null) throw new ArgumentNullException(nameof(privateKey));
            KeyPair key = new(privateKey);
            if (key.PublicKey.IsInfinity) throw new ArgumentException(null, nameof(privateKey));
            NEP6Contract contract = new()
            {
                Script = Contract.CreateSignatureRedeemScript(key.PublicKey),
                ParameterList = new[] { ContractParameterType.Signature },
                ParameterNames = new[] { "signature" },
                Deployed = false
            };
            NEP6Account account = new(this, contract.ScriptHash, key, _password)
            {
                Contract = contract
            };
            AddAccount(account);
            return account;
        }

        public override WalletAccount CreateAccount(Contract contract, KeyPair? key = null)
        {
            if (contract is not NEP6Contract nep6contract)
            {
                nep6contract = new NEP6Contract
                {
                    Script = contract.Script,
                    ParameterList = contract.ParameterList,
                    ParameterNames = contract.ParameterList.Select((p, i) => $"parameter{i}").ToArray(),
                    Deployed = false
                };
            }
            NEP6Account account;
            if (key == null)
                account = new NEP6Account(this, nep6contract.ScriptHash);
            else
                account = new NEP6Account(this, nep6contract.ScriptHash, key, _password);
            account.Contract = nep6contract;
            AddAccount(account);
            return account;
        }

        public override WalletAccount CreateAccount(UInt160 scriptHash)
        {
            NEP6Account account = new(this, scriptHash);
            AddAccount(account);
            return account;
        }

        /// <summary>
        /// Decrypts the specified NEP-2 string with the password of the wallet.
        /// </summary>
        /// <param name="nep2key">The NEP-2 string to decrypt.</param>
        /// <returns>The decrypted private key.</returns>
        internal KeyPair DecryptKey(string nep2key)
        {
            return new KeyPair(GetPrivateKeyFromNEP2(nep2key, _password, ProtocolSettings!.AddressVersion, Scrypt.N, Scrypt.R, Scrypt.P));
        }

        public override void Delete()
        {
            if (File.Exists(Path)) File.Delete(Path);
        }

        public override bool DeleteAccount(UInt160 scriptHash)
        {
            lock (_accounts)
            {
                return _accounts.Remove(scriptHash);
            }
        }

        public override WalletAccount? GetAccount(UInt160 scriptHash)
        {
            lock (_accounts)
            {
                _accounts.TryGetValue(scriptHash, out NEP6Account? account);
                return account;
            }
        }

        public override IEnumerable<WalletAccount> GetAccounts()
        {
            lock (_accounts)
            {
                foreach (NEP6Account account in _accounts.Values)
                    yield return account;
            }
        }

        public override WalletAccount Import(X509Certificate2 cert)
        {
            KeyPair key;
            using (ECDsa ecdsa = cert.GetECDsaPrivateKey()!)
            {
                key = new KeyPair(ecdsa.ExportParameters(true).D!);
            }
            NEP6Contract contract = new()
            {
                Script = Contract.CreateSignatureRedeemScript(key.PublicKey),
                ParameterList = new[] { ContractParameterType.Signature },
                ParameterNames = new[] { "signature" },
                Deployed = false
            };
            NEP6Account account = new(this, contract.ScriptHash, key, _password)
            {
                Contract = contract
            };
            AddAccount(account);
            return account;
        }

        public override WalletAccount Import(string wif)
        {
            KeyPair key = new(GetPrivateKeyFromWIF(wif));
            NEP6Contract contract = new()
            {
                Script = Contract.CreateSignatureRedeemScript(key.PublicKey),
                ParameterList = new[] { ContractParameterType.Signature },
                ParameterNames = new[] { "signature" },
                Deployed = false
            };
            NEP6Account account = new(this, contract.ScriptHash, key, _password)
            {
                Contract = contract
            };
            AddAccount(account);
            return account;
        }

        public override WalletAccount Import(string nep2, string passphrase, int N = 16384, int r = 8, int p = 8)
        {
            KeyPair key = new(GetPrivateKeyFromNEP2(nep2, passphrase, ProtocolSettings!.AddressVersion, N, r, p));
            NEP6Contract contract = new()
            {
                Script = Contract.CreateSignatureRedeemScript(key.PublicKey),
                ParameterList = new[] { ContractParameterType.Signature },
                ParameterNames = new[] { "signature" },
                Deployed = false
            };
            NEP6Account account;
            if (Scrypt.N == 16384 && Scrypt.R == 8 && Scrypt.P == 8)
                account = new NEP6Account(this, contract.ScriptHash, nep2);
            else
                account = new NEP6Account(this, contract.ScriptHash, key, passphrase);
            account.Contract = contract;
            AddAccount(account);
            return account;
        }

        /// <summary>
        /// Exports the wallet as JSON
        /// </summary>
        public JObject ToJson()
        {
            lock (_accounts)
            {
                return new()
                {
                    ["name"] = name,
                    ["version"] = version.ToString(),
                    ["scrypt"] = Scrypt.ToJson(),
                    ["accounts"] = _accounts.Values.Select(p => p.ToJson()).ToArray(),
                    ["extra"] = extra
                };
            }
        }

        public override void Save()
        {
            File.WriteAllText(Path, ToJson().ToString());
        }

        public override bool VerifyPassword(string password)
        {
            return this._password == password;
        }

        private bool VerifyPasswordInternal(string password)
        {
            lock (_accounts)
            {
                NEP6Account? account = _accounts.Values.FirstOrDefault(p => !p.Decrypted);
                if (account == null)
                {
                    account = _accounts.Values.FirstOrDefault(p => p.HasKey);
                }
                if (account == null) return true;
                if (account.Decrypted)
                {
                    return account.VerifyPassword(password);
                }
                else
                {
                    try
                    {
                        account.GetKey(password);
                        return true;
                    }
                    catch (FormatException)
                    {
                        return false;
                    }
                }
            }
        }

        public override bool ChangePassword(string oldPassword, string newPassword)
        {
            bool succeed = true;
            lock (_accounts)
            {
                Parallel.ForEach(_accounts.Values, (account, state) =>
                {
                    if (!account.ChangePasswordPrepare(oldPassword, newPassword))
                    {
                        state.Stop();
                        succeed = false;
                    }
                });
            }
            if (succeed)
            {
                lock (_accounts)
                {
                    foreach (NEP6Account account in _accounts.Values)
                        account.ChangePasswordCommit();
                }

                _password = newPassword;
            }
            else
            {
                lock (_accounts)
                {
                    foreach (NEP6Account account in _accounts.Values)
                        account.ChangePasswordRoolback();
                }
            }
            return succeed;
        }
    }
}
