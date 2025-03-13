// Copyright (C) 2015-2025 The Neo Project.
//
// SQLiteWallet.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.EntityFrameworkCore;
using Neo.Cryptography;
using Neo.Extensions;
using Neo.SmartContract;
using Neo.Wallets.NEP6;
using System.Buffers.Binary;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using static System.IO.Path;

namespace Neo.Wallets.SQLite
{
    /// <summary>
    /// A wallet implementation that uses SQLite as the underlying storage.
    /// </summary>
    class SQLiteWallet : Wallet
    {
        private readonly object _db_lock = new();
        private readonly byte[] _iv;
        private readonly byte[] _salt;
        private readonly byte[] _masterKey;
        private readonly ScryptParameters _scrypt;
        private readonly Dictionary<UInt160, SQLiteWalletAccount> accounts;

        public override string Name => GetFileNameWithoutExtension(Path);

        public override Version Version
        {
            get
            {
                var buffer = LoadStoredData("Version");
                if (buffer == null || buffer.Length < 16) return new Version(0, 0);
                var major = BinaryPrimitives.ReadInt32LittleEndian(buffer);
                var minor = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(4));
                var build = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(8));
                var revision = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(12));
                return new Version(major, minor, build, revision);
            }
        }

        private SQLiteWallet(string path, byte[] passwordKey, ProtocolSettings settings) : base(path, settings)
        {
            _salt = LoadStoredData("Salt");
            var passwordHash = LoadStoredData("PasswordHash");
            if (passwordHash != null && !passwordHash.SequenceEqual(passwordKey.Concat(_salt).ToArray().Sha256()))
                throw new CryptographicException();
            _iv = LoadStoredData("IV");
            _masterKey = Decrypt(LoadStoredData("MasterKey"), passwordKey, _iv);
            _scrypt = new ScryptParameters
                (
                BinaryPrimitives.ReadInt32LittleEndian(LoadStoredData("ScryptN")),
                BinaryPrimitives.ReadInt32LittleEndian(LoadStoredData("ScryptR")),
                BinaryPrimitives.ReadInt32LittleEndian(LoadStoredData("ScryptP"))
                );
            accounts = LoadAccounts();
        }

        private SQLiteWallet(string path, byte[] passwordKey, ProtocolSettings settings, ScryptParameters scrypt) : base(path, settings)
        {
            _iv = new byte[16];
            _salt = new byte[20];
            _masterKey = new byte[32];
            _scrypt = scrypt;
            accounts = [];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(_iv);
                rng.GetBytes(_salt);
                rng.GetBytes(_masterKey);
            }
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var versionBuffer = new byte[sizeof(int) * 4];
            BinaryPrimitives.WriteInt32LittleEndian(versionBuffer, version.Major);
            BinaryPrimitives.WriteInt32LittleEndian(versionBuffer.AsSpan(4), version.Minor);
            BinaryPrimitives.WriteInt32LittleEndian(versionBuffer.AsSpan(8), version.Build);
            BinaryPrimitives.WriteInt32LittleEndian(versionBuffer.AsSpan(12), version.Revision);
            BuildDatabase();
            SaveStoredData("IV", _iv);
            SaveStoredData("Salt", _salt);
            SaveStoredData("PasswordHash", passwordKey.Concat(_salt).ToArray().Sha256());
            SaveStoredData("MasterKey", Encrypt(_masterKey, passwordKey, _iv));
            SaveStoredData("Version", versionBuffer);
            SaveStoredData("ScryptN", _scrypt.N);
            SaveStoredData("ScryptR", _scrypt.R);
            SaveStoredData("ScryptP", _scrypt.P);
        }

        private void AddAccount(SQLiteWalletAccount account)
        {
            lock (accounts)
            {
                if (accounts.TryGetValue(account.ScriptHash, out var account_old))
                {
                    account.Contract ??= account_old.Contract;
                }
                accounts[account.ScriptHash] = account;
            }
            lock (_db_lock)
            {
                using var ctx = new WalletDataContext(Path);
                if (account.HasKey)
                {
                    var dbAccount = ctx.Accounts.FirstOrDefault(p => p.PublicKeyHash == account.Key.PublicKeyHash.ToArray());
                    if (dbAccount == null)
                    {
                        dbAccount = ctx.Accounts.Add(new Account
                        {
                            Nep2key = account.Key.Export(_masterKey, ProtocolSettings.AddressVersion, _scrypt.N, _scrypt.R, _scrypt.P),
                            PublicKeyHash = account.Key.PublicKeyHash.ToArray()
                        }).Entity;
                    }
                    else
                    {
                        dbAccount.Nep2key = account.Key.Export(_masterKey, ProtocolSettings.AddressVersion, _scrypt.N, _scrypt.R, _scrypt.P);
                    }
                }
                if (account.Contract != null)
                {
                    var dbContract = ctx.Contracts.FirstOrDefault(p => p.ScriptHash == account.Contract.ScriptHash.ToArray());
                    if (dbContract != null)
                    {
                        dbContract.PublicKeyHash = account.Key.PublicKeyHash.ToArray();
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
                    var dbAddress = ctx.Addresses.FirstOrDefault(p => p.ScriptHash == account.ScriptHash.ToArray());
                    if (dbAddress == null)
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
            using var ctx = new WalletDataContext(Path);
            ctx.Database.EnsureDeleted();
            ctx.Database.EnsureCreated();
        }

        public override bool ChangePassword(string oldPassword, string newPassword)
        {
            if (!VerifyPassword(oldPassword)) return false;
            var passwordKey = ToAesKey(newPassword);
            try
            {
                SaveStoredData("PasswordHash", passwordKey.Concat(_salt).ToArray().Sha256());
                SaveStoredData("MasterKey", Encrypt(_masterKey, passwordKey, _iv));
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
        public static SQLiteWallet Create(string path, string password, ProtocolSettings settings, ScryptParameters scrypt = null)
        {
            return new SQLiteWallet(path, ToAesKey(password), settings, scrypt ?? ScryptParameters.Default);
        }

        public override WalletAccount CreateAccount(byte[] privateKey)
        {
            var key = new KeyPair(privateKey);
            var contract = new VerificationContract()
            {
                Script = SmartContract.Contract.CreateSignatureRedeemScript(key.PublicKey),
                ParameterList = [ContractParameterType.Signature]
            };
            var account = new SQLiteWalletAccount(contract.ScriptHash, ProtocolSettings)
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
            var account = new SQLiteWalletAccount(verification_contract.ScriptHash, ProtocolSettings)
            {
                Key = key,
                Contract = verification_contract
            };
            AddAccount(account);
            return account;
        }

        public override WalletAccount CreateAccount(UInt160 scriptHash)
        {
            var account = new SQLiteWalletAccount(scriptHash, ProtocolSettings);
            AddAccount(account);
            return account;
        }

        public override void Delete()
        {
            using var ctx = new WalletDataContext(Path);
            ctx.Database.EnsureDeleted();
        }

        public override bool DeleteAccount(UInt160 scriptHash)
        {
            SQLiteWalletAccount account;
            lock (accounts)
            {
                if (accounts.TryGetValue(scriptHash, out account))
                    accounts.Remove(scriptHash);
            }
            if (account != null)
            {
                lock (_db_lock)
                {
                    using var ctx = new WalletDataContext(Path);
                    if (account.HasKey)
                    {
                        var dbAccount = ctx.Accounts.First(p => p.PublicKeyHash == account.Key.PublicKeyHash.ToArray());
                        ctx.Accounts.Remove(dbAccount);
                    }
                    if (account.Contract != null)
                    {
                        var dbContract = ctx.Contracts.First(p => p.ScriptHash == scriptHash.ToArray());
                        ctx.Contracts.Remove(dbContract);
                    }
                    //delete address
                    {
                        var dbAddress = ctx.Addresses.First(p => p.ScriptHash == scriptHash.ToArray());
                        ctx.Addresses.Remove(dbAddress);
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
                accounts.TryGetValue(scriptHash, out var account);
                return account;
            }
        }

        public override IEnumerable<WalletAccount> GetAccounts()
        {
            lock (accounts)
            {
                foreach (var account in accounts.Values)
                    yield return account;
            }
        }

        private Dictionary<UInt160, SQLiteWalletAccount> LoadAccounts()
        {
            using var ctx = new WalletDataContext(Path);
            var accounts = ctx.Addresses.Select(p => new SQLiteWalletAccount(p.ScriptHash, ProtocolSettings))
                .ToDictionary(p => p.ScriptHash);
            foreach (var dbContract in ctx.Contracts.Include(p => p.Account))
            {
                var contract = dbContract.RawData.AsSerializable<VerificationContract>();
                var account = accounts[contract.ScriptHash];
                account.Contract = contract;
                account.Key = new KeyPair(GetPrivateKeyFromNEP2(dbContract.Account.Nep2key, _masterKey, ProtocolSettings.AddressVersion, _scrypt.N, _scrypt.R, _scrypt.P));
            }
            return accounts;
        }

        private byte[] LoadStoredData(string name)
        {
            using var ctx = new WalletDataContext(Path);
            return ctx.Keys.FirstOrDefault(p => p.Name == name)?.Value;
        }

        /// <summary>
        /// Opens a wallet at the specified path.
        /// </summary>
        /// <param name="path">The path of the wallet.</param>
        /// <param name="password">The password of the wallet.</param>
        /// <param name="settings">The <see cref="ProtocolSettings"/> to be used by the wallet.</param>
        /// <returns>The opened wallet.</returns>
        public static new SQLiteWallet Open(string path, string password, ProtocolSettings settings)
        {
            return new SQLiteWallet(path, ToAesKey(password), settings);
        }

        public override void Save()
        {
            // Do nothing
        }

        private void SaveStoredData(string name, int value)
        {
            var data = new byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(data, value);
            SaveStoredData(name, data);
        }

        private void SaveStoredData(string name, byte[] value)
        {
            lock (_db_lock)
            {
                using var ctx = new WalletDataContext(Path);
                SaveStoredData(ctx, name, value);
                ctx.SaveChanges();
            }
        }

        private static void SaveStoredData(WalletDataContext ctx, string name, byte[] value)
        {
            var key = ctx.Keys.FirstOrDefault(p => p.Name == name);
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
            return ToAesKey(password).Concat(_salt).ToArray().Sha256().SequenceEqual(LoadStoredData("PasswordHash"));
        }

        private static byte[] Encrypt(byte[] data, byte[] key, byte[] iv)
        {
            ArgumentNullException.ThrowIfNull(data);
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(iv);

            if (data.Length % 16 != 0 || key.Length != 32 || iv.Length != 16) throw new ArgumentException();
            using var aes = Aes.Create();
            aes.Padding = PaddingMode.None;
            using var encryptor = aes.CreateEncryptor(key, iv);
            return encryptor.TransformFinalBlock(data, 0, data.Length);
        }

        private static byte[] Decrypt(byte[] data, byte[] key, byte[] iv)
        {
            ArgumentNullException.ThrowIfNull(data);
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(iv);

            if (data.Length % 16 != 0 || key.Length != 32 || iv.Length != 16) throw new ArgumentException();
            using var aes = Aes.Create();
            aes.Padding = PaddingMode.None;
            using var decryptor = aes.CreateDecryptor(key, iv);
            return decryptor.TransformFinalBlock(data, 0, data.Length);
        }

        private static byte[] ToAesKey(string password)
        {
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var passwordHash = SHA256.HashData(passwordBytes);
            var passwordHash2 = SHA256.HashData(passwordHash);
            Array.Clear(passwordBytes, 0, passwordBytes.Length);
            Array.Clear(passwordHash, 0, passwordHash.Length);
            return passwordHash2;
        }
    }
}
