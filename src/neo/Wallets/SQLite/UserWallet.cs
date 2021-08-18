// Copyright (C) 2015-2021 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.EntityFrameworkCore;
using Neo.Cryptography;
using Neo.IO;
using Neo.SmartContract;
using Neo.Wallets.NEP6;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using static System.IO.Path;

namespace Neo.Wallets.SQLite
{
    /// <summary>
    /// A wallet implementation that uses SQLite as the underlying storage.
    /// </summary>
    public class UserWallet : Wallet
    {
        private readonly object db_lock = new();
        private readonly byte[] iv;
        private readonly byte[] salt;
        private readonly byte[] masterKey;
        private readonly ScryptParameters scrypt;
        private readonly Dictionary<UInt160, UserWalletAccount> accounts;

        public override string Name => GetFileNameWithoutExtension(Path);

        public override Version Version
        {
            get
            {
                byte[] buffer = LoadStoredData("Version");
                if (buffer == null || buffer.Length < 16) return new Version(0, 0);
                int major = BinaryPrimitives.ReadInt32LittleEndian(buffer);
                int minor = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(4));
                int build = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(8));
                int revision = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(12));
                return new Version(major, minor, build, revision);
            }
        }

        private UserWallet(string path, byte[] passwordKey, ProtocolSettings settings) : base(path, settings)
        {
            this.salt = LoadStoredData("Salt");
            byte[] passwordHash = LoadStoredData("PasswordHash");
            if (passwordHash != null && !passwordHash.SequenceEqual(passwordKey.Concat(salt).ToArray().Sha256()))
                throw new CryptographicException();
            this.iv = LoadStoredData("IV");
            this.masterKey = Decrypt(LoadStoredData("MasterKey"), passwordKey, iv);
            this.scrypt = new ScryptParameters
                (
                BinaryPrimitives.ReadInt32LittleEndian(LoadStoredData("ScryptN")),
                BinaryPrimitives.ReadInt32LittleEndian(LoadStoredData("ScryptR")),
                BinaryPrimitives.ReadInt32LittleEndian(LoadStoredData("ScryptP"))
                );
            this.accounts = LoadAccounts();
        }

        private UserWallet(string path, byte[] passwordKey, ProtocolSettings settings, ScryptParameters scrypt) : base(path, settings)
        {
            this.iv = new byte[16];
            this.salt = new byte[20];
            this.masterKey = new byte[32];
            this.scrypt = scrypt;
            this.accounts = new Dictionary<UInt160, UserWalletAccount>();
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(iv);
                rng.GetBytes(salt);
                rng.GetBytes(masterKey);
            }
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            byte[] versionBuffer = new byte[sizeof(int) * 4];
            BinaryPrimitives.WriteInt32LittleEndian(versionBuffer, version.Major);
            BinaryPrimitives.WriteInt32LittleEndian(versionBuffer.AsSpan(4), version.Minor);
            BinaryPrimitives.WriteInt32LittleEndian(versionBuffer.AsSpan(8), version.Build);
            BinaryPrimitives.WriteInt32LittleEndian(versionBuffer.AsSpan(12), version.Revision);
            BuildDatabase();
            SaveStoredData("IV", iv);
            SaveStoredData("Salt", salt);
            SaveStoredData("PasswordHash", passwordKey.Concat(salt).ToArray().Sha256());
            SaveStoredData("MasterKey", Encrypt(masterKey, passwordKey, iv));
            SaveStoredData("Version", versionBuffer);
            SaveStoredData("ScryptN", this.scrypt.N);
            SaveStoredData("ScryptR", this.scrypt.R);
            SaveStoredData("ScryptP", this.scrypt.P);
        }

        private void AddAccount(UserWalletAccount account)
        {
            lock (accounts)
            {
                if (accounts.TryGetValue(account.ScriptHash, out UserWalletAccount account_old))
                {
                    if (account.Contract == null)
                    {
                        account.Contract = account_old.Contract;
                    }
                }
                accounts[account.ScriptHash] = account;
            }
            lock (db_lock)
            {
                using WalletDataContext ctx = new(Path);
                if (account.HasKey)
                {
                    string passphrase = Encoding.UTF8.GetString(masterKey);
                    Account db_account = ctx.Accounts.FirstOrDefault(p => p.PublicKeyHash == account.Key.PublicKeyHash.ToArray());
                    if (db_account == null)
                    {
                        db_account = ctx.Accounts.Add(new Account
                        {
                            Nep2key = account.Key.Export(passphrase, ProtocolSettings.AddressVersion, scrypt.N, scrypt.R, scrypt.P),
                            PublicKeyHash = account.Key.PublicKeyHash.ToArray()
                        }).Entity;
                    }
                    else
                    {
                        db_account.Nep2key = account.Key.Export(passphrase, ProtocolSettings.AddressVersion, scrypt.N, scrypt.R, scrypt.P);
                    }
                }
                if (account.Contract != null)
                {
                    Contract db_contract = ctx.Contracts.FirstOrDefault(p => p.ScriptHash == account.Contract.ScriptHash.ToArray());
                    if (db_contract != null)
                    {
                        db_contract.PublicKeyHash = account.Key.PublicKeyHash.ToArray();
                    }
                    else
                    {
                        ctx.Contracts.Add(new Contract
                        {
                            RawData = ((VerificationContract)account.Contract).ToArray(),
                            ScriptHash = account.Contract.ScriptHash.ToArray(),
                            PublicKeyHash = account.Key.PublicKeyHash.ToArray()
                        });
                    }
                }
                //add address
                {
                    Address db_address = ctx.Addresses.FirstOrDefault(p => p.ScriptHash == account.ScriptHash.ToArray());
                    if (db_address == null)
                    {
                        ctx.Addresses.Add(new Address
                        {
                            ScriptHash = account.ScriptHash.ToArray()
                        });
                    }
                }
                ctx.SaveChanges();
            }
        }

        private void BuildDatabase()
        {
            using WalletDataContext ctx = new(Path);
            ctx.Database.EnsureDeleted();
            ctx.Database.EnsureCreated();
        }

        public override bool ChangePassword(string oldPassword, string newPassword)
        {
            if (!VerifyPassword(oldPassword)) return false;
            byte[] passwordKey = newPassword.ToAesKey();
            try
            {
                SaveStoredData("PasswordHash", passwordKey.Concat(salt).ToArray().Sha256());
                SaveStoredData("MasterKey", Encrypt(masterKey, passwordKey, iv));
                return true;
            }
            finally
            {
                Array.Clear(passwordKey, 0, passwordKey.Length);
            }
        }

        public override bool Contains(UInt160 scriptHash)
        {
            lock (accounts)
            {
                return accounts.ContainsKey(scriptHash);
            }
        }

        /// <summary>
        /// Creates a new wallet at the specified path.
        /// </summary>
        /// <param name="path">The path of the wallet.</param>
        /// <param name="password">The password of the wallet.</param>
        /// <param name="settings">The <see cref="ProtocolSettings"/> to be used by the wallet.</param>
        /// <param name="scrypt">The parameters of the SCrypt algorithm used for encrypting and decrypting the private keys in the wallet.</param>
        /// <returns>The created wallet.</returns>
        public static UserWallet Create(string path, string password, ProtocolSettings settings, ScryptParameters scrypt = null)
        {
            return new UserWallet(path, password.ToAesKey(), settings, scrypt ?? ScryptParameters.Default);
        }

        /// <summary>
        /// Creates a new wallet at the specified path.
        /// </summary>
        /// <param name="path">The path of the wallet.</param>
        /// <param name="password">The password of the wallet.</param>
        /// <param name="settings">The <see cref="ProtocolSettings"/> to be used by the wallet.</param>
        /// <param name="scrypt">The parameters of the SCrypt algorithm used for encrypting and decrypting the private keys in the wallet.</param>
        /// <returns>The created wallet.</returns>
        public static UserWallet Create(string path, SecureString password, ProtocolSettings settings, ScryptParameters scrypt = null)
        {
            return new UserWallet(path, password.ToAesKey(), settings, scrypt ?? ScryptParameters.Default);
        }

        public override WalletAccount CreateAccount(byte[] privateKey)
        {
            KeyPair key = new(privateKey);
            VerificationContract contract = new()
            {
                Script = SmartContract.Contract.CreateSignatureRedeemScript(key.PublicKey),
                ParameterList = new[] { ContractParameterType.Signature }
            };
            UserWalletAccount account = new(contract.ScriptHash, ProtocolSettings)
            {
                Key = key,
                Contract = contract
            };
            AddAccount(account);
            return account;
        }

        public override WalletAccount CreateAccount(SmartContract.Contract contract, KeyPair key = null)
        {
            if (contract is not VerificationContract verification_contract)
            {
                verification_contract = new VerificationContract
                {
                    Script = contract.Script,
                    ParameterList = contract.ParameterList
                };
            }
            UserWalletAccount account = new(verification_contract.ScriptHash, ProtocolSettings)
            {
                Key = key,
                Contract = verification_contract
            };
            AddAccount(account);
            return account;
        }

        public override WalletAccount CreateAccount(UInt160 scriptHash)
        {
            UserWalletAccount account = new(scriptHash, ProtocolSettings);
            AddAccount(account);
            return account;
        }

        public override bool DeleteAccount(UInt160 scriptHash)
        {
            UserWalletAccount account;
            lock (accounts)
            {
                if (accounts.TryGetValue(scriptHash, out account))
                    accounts.Remove(scriptHash);
            }
            if (account != null)
            {
                lock (db_lock)
                {
                    using WalletDataContext ctx = new(Path);
                    if (account.HasKey)
                    {
                        Account db_account = ctx.Accounts.First(p => p.PublicKeyHash == account.Key.PublicKeyHash.ToArray());
                        ctx.Accounts.Remove(db_account);
                    }
                    if (account.Contract != null)
                    {
                        Contract db_contract = ctx.Contracts.First(p => p.ScriptHash == scriptHash.ToArray());
                        ctx.Contracts.Remove(db_contract);
                    }
                    //delete address
                    {
                        Address db_address = ctx.Addresses.First(p => p.ScriptHash == scriptHash.ToArray());
                        ctx.Addresses.Remove(db_address);
                    }
                    ctx.SaveChanges();
                }
                return true;
            }
            return false;
        }

        public override WalletAccount GetAccount(UInt160 scriptHash)
        {
            lock (accounts)
            {
                accounts.TryGetValue(scriptHash, out UserWalletAccount account);
                return account;
            }
        }

        public override IEnumerable<WalletAccount> GetAccounts()
        {
            lock (accounts)
            {
                foreach (UserWalletAccount account in accounts.Values)
                    yield return account;
            }
        }

        private Dictionary<UInt160, UserWalletAccount> LoadAccounts()
        {
            using WalletDataContext ctx = new(Path);
            string passphrase = Encoding.UTF8.GetString(masterKey);
            Dictionary<UInt160, UserWalletAccount> accounts = ctx.Addresses.Select(p => p.ScriptHash).AsEnumerable().Select(p => new UserWalletAccount(new UInt160(p), ProtocolSettings)).ToDictionary(p => p.ScriptHash);
            foreach (Contract db_contract in ctx.Contracts.Include(p => p.Account))
            {
                VerificationContract contract = db_contract.RawData.AsSerializable<VerificationContract>();
                UserWalletAccount account = accounts[contract.ScriptHash];
                account.Contract = contract;
                account.Key = new KeyPair(GetPrivateKeyFromNEP2(db_contract.Account.Nep2key, passphrase, ProtocolSettings.AddressVersion, scrypt.N, scrypt.R, scrypt.P));
            }
            return accounts;
        }

        private byte[] LoadStoredData(string name)
        {
            using WalletDataContext ctx = new(Path);
            return ctx.Keys.FirstOrDefault(p => p.Name == name)?.Value;
        }

        /// <summary>
        /// Opens a wallet at the specified path.
        /// </summary>
        /// <param name="path">The path of the wallet.</param>
        /// <param name="password">The password of the wallet.</param>
        /// <param name="settings">The <see cref="ProtocolSettings"/> to be used by the wallet.</param>
        /// <returns>The opened wallet.</returns>
        public static UserWallet Open(string path, string password, ProtocolSettings settings)
        {
            return new UserWallet(path, password.ToAesKey(), settings);
        }

        /// <summary>
        /// Opens a wallet at the specified path.
        /// </summary>
        /// <param name="path">The path of the wallet.</param>
        /// <param name="password">The password of the wallet.</param>
        /// <param name="settings">The <see cref="ProtocolSettings"/> to be used by the wallet.</param>
        /// <returns>The opened wallet.</returns>
        public static UserWallet Open(string path, SecureString password, ProtocolSettings settings)
        {
            return new UserWallet(path, password.ToAesKey(), settings);
        }

        private void SaveStoredData(string name, int value)
        {
            byte[] data = new byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(data, value);
            SaveStoredData(name, data);
        }

        private void SaveStoredData(string name, byte[] value)
        {
            lock (db_lock)
            {
                using WalletDataContext ctx = new(Path);
                SaveStoredData(ctx, name, value);
                ctx.SaveChanges();
            }
        }

        private static void SaveStoredData(WalletDataContext ctx, string name, byte[] value)
        {
            Key key = ctx.Keys.FirstOrDefault(p => p.Name == name);
            if (key == null)
            {
                ctx.Keys.Add(new Key
                {
                    Name = name,
                    Value = value
                });
            }
            else
            {
                key.Value = value;
            }
        }

        public override bool VerifyPassword(string password)
        {
            return password.ToAesKey().Concat(salt).ToArray().Sha256().SequenceEqual(LoadStoredData("PasswordHash"));
        }

        private static byte[] Encrypt(byte[] data, byte[] key, byte[] iv)
        {
            if (data == null || key == null || iv == null) throw new ArgumentNullException();
            if (data.Length % 16 != 0 || key.Length != 32 || iv.Length != 16) throw new ArgumentException();
            using Aes aes = Aes.Create();
            aes.Padding = PaddingMode.None;
            using ICryptoTransform encryptor = aes.CreateEncryptor(key, iv);
            return encryptor.TransformFinalBlock(data, 0, data.Length);
        }

        private static byte[] Decrypt(byte[] data, byte[] key, byte[] iv)
        {
            if (data == null || key == null || iv == null) throw new ArgumentNullException();
            if (data.Length % 16 != 0 || key.Length != 32 || iv.Length != 16) throw new ArgumentException();
            using Aes aes = Aes.Create();
            aes.Padding = PaddingMode.None;
            using ICryptoTransform decryptor = aes.CreateDecryptor(key, iv);
            return decryptor.TransformFinalBlock(data, 0, data.Length);
        }
    }
}
