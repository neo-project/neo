using Neo.IO.Json;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using UserWallet = Neo.Wallets.SQLite.UserWallet;

namespace Neo.Wallets.NEP6
{
    /// <summary>
    /// An implementation of the NEP-6 wallet standard.
    /// </summary>
    /// <remarks>https://github.com/neo-project/proposals/blob/master/nep-6.mediawiki</remarks>
    public class NEP6Wallet : Wallet
    {
        private string password;
        private string name;
        private Version version;
        private readonly Dictionary<UInt160, NEP6Account> accounts;
        private readonly JObject extra;

        /// <summary>
        /// The parameters of the SCrypt algorithm used for encrypting and decrypting the private keys in the wallet.
        /// </summary>
        public readonly ScryptParameters Scrypt;

        public override string Name => name;

        /// <summary>
        /// The version of the wallet standard. It is currently fixed at 1.0 and will be used for functional upgrades in the future.
        /// </summary>
        public override Version Version => version;

        /// <summary>
        /// Loads or creates a wallet at the specified path.
        /// </summary>
        /// <param name="path">The path of the wallet file.</param>
        /// <param name="settings">The <see cref="ProtocolSettings"/> to be used by the wallet.</param>
        /// <param name="name">The name of the wallet. If the wallet is loaded from an existing file, this parameter is ignored.</param>
        public NEP6Wallet(string path, ProtocolSettings settings, string name = null) : base(path, settings)
        {
            if (File.Exists(path))
            {
                JObject wallet = JObject.Parse(File.ReadAllBytes(path));
                LoadFromJson(wallet, out Scrypt, out accounts, out extra);
            }
            else
            {
                this.name = name;
                this.version = Version.Parse("1.0");
                this.Scrypt = ScryptParameters.Default;
                this.accounts = new Dictionary<UInt160, NEP6Account>();
                this.extra = JObject.Null;
            }
        }

        /// <summary>
        /// Loads the wallet with the specified JSON string.
        /// </summary>
        /// <param name="path">The path of the wallet.</param>
        /// <param name="settings">The <see cref="ProtocolSettings"/> to be used by the wallet.</param>
        /// <param name="json">The JSON object representing the wallet.</param>
        public NEP6Wallet(string path, ProtocolSettings settings, JObject json) : base(path, settings)
        {
            LoadFromJson(json, out Scrypt, out accounts, out extra);
        }

        private void LoadFromJson(JObject wallet, out ScryptParameters scrypt, out Dictionary<UInt160, NEP6Account> accounts, out JObject extra)
        {
            this.version = Version.Parse(wallet["version"].AsString());
            this.name = wallet["name"]?.AsString();
            scrypt = ScryptParameters.FromJson(wallet["scrypt"]);
            accounts = ((JArray)wallet["accounts"]).Select(p => NEP6Account.FromJson(p, this)).ToDictionary(p => p.ScriptHash);
            extra = wallet["extra"];
        }

        private void AddAccount(NEP6Account account)
        {
            lock (accounts)
            {
                if (accounts.TryGetValue(account.ScriptHash, out NEP6Account account_old))
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
                        NEP6Contract contract_old = (NEP6Contract)account_old.Contract;
                        if (contract_old != null)
                        {
                            NEP6Contract contract = (NEP6Contract)account.Contract;
                            contract.ParameterNames = contract_old.ParameterNames;
                            contract.Deployed = contract_old.Deployed;
                        }
                    }
                    account.Extra = account_old.Extra;
                }
                accounts[account.ScriptHash] = account;
            }
        }

        public override bool Contains(UInt160 scriptHash)
        {
            lock (accounts)
            {
                return accounts.ContainsKey(scriptHash);
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
            NEP6Account account = new(this, contract.ScriptHash, key, password)
            {
                Contract = contract
            };
            AddAccount(account);
            return account;
        }

        public override WalletAccount CreateAccount(Contract contract, KeyPair key = null)
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
                account = new NEP6Account(this, nep6contract.ScriptHash, key, password);
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
        public KeyPair DecryptKey(string nep2key)
        {
            return new KeyPair(GetPrivateKeyFromNEP2(nep2key, password, ProtocolSettings.AddressVersion, Scrypt.N, Scrypt.R, Scrypt.P));
        }

        public override bool DeleteAccount(UInt160 scriptHash)
        {
            lock (accounts)
            {
                return accounts.Remove(scriptHash);
            }
        }

        public override WalletAccount GetAccount(UInt160 scriptHash)
        {
            lock (accounts)
            {
                accounts.TryGetValue(scriptHash, out NEP6Account account);
                return account;
            }
        }

        public override IEnumerable<WalletAccount> GetAccounts()
        {
            lock (accounts)
            {
                foreach (NEP6Account account in accounts.Values)
                    yield return account;
            }
        }

        public WalletAccount GetDefaultAccount()
        {
            NEP6Account first = null;
            lock (accounts)
            {
                foreach (NEP6Account account in accounts.Values)
                {
                    if (account.IsDefault) return account;
                    if (first == null) first = account;
                }
            }
            return first;
        }

        public override WalletAccount Import(X509Certificate2 cert)
        {
            KeyPair key;
            using (ECDsa ecdsa = cert.GetECDsaPrivateKey())
            {
                key = new KeyPair(ecdsa.ExportParameters(true).D);
            }
            NEP6Contract contract = new()
            {
                Script = Contract.CreateSignatureRedeemScript(key.PublicKey),
                ParameterList = new[] { ContractParameterType.Signature },
                ParameterNames = new[] { "signature" },
                Deployed = false
            };
            NEP6Account account = new(this, contract.ScriptHash, key, password)
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
            NEP6Account account = new(this, contract.ScriptHash, key, password)
            {
                Contract = contract
            };
            AddAccount(account);
            return account;
        }

        public override WalletAccount Import(string nep2, string passphrase, int N = 16384, int r = 8, int p = 8)
        {
            KeyPair key = new(GetPrivateKeyFromNEP2(nep2, passphrase, ProtocolSettings.AddressVersion, N, r, p));
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

        internal void Lock()
        {
            password = null;
        }

        /// <summary>
        /// Migrates the accounts from <see cref="UserWallet"/> to a new <see cref="NEP6Wallet"/>.
        /// </summary>
        /// <param name="path">The path of the new wallet file.</param>
        /// <param name="db3path">The path of the db3 wallet file.</param>
        /// <param name="password">The password of the wallets.</param>
        /// <param name="settings">The <see cref="ProtocolSettings"/> to be used by the wallet.</param>
        /// <returns>The created new wallet.</returns>
        public static NEP6Wallet Migrate(string path, string db3path, string password, ProtocolSettings settings)
        {
            UserWallet wallet_old = UserWallet.Open(db3path, password, settings);
            NEP6Wallet wallet_new = new(path, settings, wallet_old.Name);
            using (wallet_new.Unlock(password))
            {
                foreach (WalletAccount account in wallet_old.GetAccounts())
                {
                    wallet_new.CreateAccount(account.Contract, account.GetKey());
                }
            }
            return wallet_new;
        }

        /// <summary>
        /// Saves the wallet to the file.
        /// </summary>
        public void Save()
        {
            JObject wallet = new();
            wallet["name"] = name;
            wallet["version"] = version.ToString();
            wallet["scrypt"] = Scrypt.ToJson();
            wallet["accounts"] = new JArray(accounts.Values.Select(p => p.ToJson()));
            wallet["extra"] = extra;
            File.WriteAllText(Path, wallet.ToString());
        }

        /// <summary>
        /// Unlocks the wallet with the specified password.
        /// </summary>
        /// <param name="password">The password of the wallet.</param>
        /// <returns>The object that can be disposed to lock the wallet again.</returns>
        public IDisposable Unlock(string password)
        {
            if (!VerifyPassword(password))
                throw new CryptographicException();
            this.password = password;
            return new WalletLocker(this);
        }

        /// <summary>
        /// Unlock empty wallet with the specified password.
        /// </summary>
        /// <param name="password">The password of the wallet.</param>
        public void UnlockEmpty(string password)
        {
            this.password = password;
        }

        public override bool VerifyPassword(string password)
        {
            lock (accounts)
            {
                NEP6Account account = accounts.Values.FirstOrDefault(p => !p.Decrypted);
                if (account == null)
                {
                    account = accounts.Values.FirstOrDefault(p => p.HasKey);
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
            lock (accounts)
            {
                Parallel.ForEach(accounts.Values, (account, state) =>
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
                foreach (NEP6Account account in accounts.Values)
                    account.ChangePasswordCommit();
                if (password != null)
                    password = newPassword;
            }
            else
            {
                foreach (NEP6Account account in accounts.Values)
                    account.ChangePasswordRoolback();
            }
            return succeed;
        }
    }
}
