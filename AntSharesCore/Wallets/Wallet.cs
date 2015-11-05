using AntShares.Core;
using AntShares.Cryptography;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace AntShares.Wallets
{
    public abstract class Wallet
    {
        public const byte CoinVersion = 0x17;

        private readonly string path;
        private readonly byte[] iv;
        private readonly byte[] masterKey;
        private readonly Dictionary<UInt160, Account> accounts;
        private readonly Dictionary<UInt160, Contract> contracts;
        private readonly HashSet<UnspentCoin> unspent_coins;
        private readonly HashSet<UnspentCoin> change_coins;
        private uint current_height;

        protected string DbPath => path;
        protected uint WalletHeight => current_height;

        protected Wallet(string path, string password, bool create)
        {
            this.path = path;
            byte[] passwordKey = password.ToAesKey();
            if (create)
            {
                this.iv = new byte[16];
                this.masterKey = new byte[32];
                this.accounts = new Dictionary<UInt160, Account>();
                this.contracts = new Dictionary<UInt160, Contract>();
                this.unspent_coins = new HashSet<UnspentCoin>();
                this.change_coins = new HashSet<UnspentCoin>();
                this.current_height = Blockchain.Default?.HeaderHeight + 1 ?? 0;
                using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
                {
                    rng.GetNonZeroBytes(iv);
                    rng.GetNonZeroBytes(masterKey);
                }
                SaveStoredData("IV", iv);
                SaveStoredData("MasterKey", masterKey.AesEncrypt(passwordKey, iv));
                SaveStoredData("Height", BitConverter.GetBytes(current_height));
            }
            else
            {
                this.iv = LoadStoredData("IV");
                this.masterKey = LoadStoredData("MasterKey").AesDecrypt(passwordKey, iv);
                this.accounts = LoadAccounts().ToDictionary(p => p.PublicKeyHash);
                this.contracts = LoadContracts().ToDictionary(p => p.ScriptHash);
                this.unspent_coins = new HashSet<UnspentCoin>(LoadUnspentCoins(false));
                this.change_coins = new HashSet<UnspentCoin>(LoadUnspentCoins(true));
                this.current_height = BitConverter.ToUInt32(LoadStoredData("Height"), 0);
            }
            Array.Clear(passwordKey, 0, passwordKey.Length);
            ProtectedMemory.Protect(masterKey, MemoryProtectionScope.SameProcess);
        }

        public void ChangePassword(string password)
        {
            byte[] passwordKey = password.ToAesKey();
            ProtectedMemory.Unprotect(masterKey, MemoryProtectionScope.SameProcess);
            try
            {
                SaveStoredData("MasterKey", masterKey.AesEncrypt(passwordKey, iv));
            }
            finally
            {
                Array.Clear(passwordKey, 0, passwordKey.Length);
                ProtectedMemory.Protect(masterKey, MemoryProtectionScope.SameProcess);
            }
        }

        public Account CreateAccount()
        {
            using (CngKey key = CngKey.Create(CngAlgorithm.ECDsaP256, null, new CngKeyCreationParameters { ExportPolicy = CngExportPolicies.AllowPlaintextArchiving }))
            {
                byte[] privateKey = key.Export(CngKeyBlobFormat.EccPrivateBlob);
                Account account = new Account(privateKey);
                Array.Clear(privateKey, 0, privateKey.Length);
                accounts.Add(account.PublicKeyHash, account);
                OnCreateAccount(account);
                return account;
            }
        }

        protected byte[] DecryptPrivateKey(byte[] encryptedPrivateKey)
        {
            if (encryptedPrivateKey == null) throw new ArgumentNullException(nameof(encryptedPrivateKey));
            if (encryptedPrivateKey.Length != 96) throw new ArgumentException();
            ProtectedMemory.Unprotect(masterKey, MemoryProtectionScope.SameProcess);
            try
            {
                return encryptedPrivateKey.AesDecrypt(masterKey, iv);
            }
            finally
            {
                ProtectedMemory.Protect(masterKey, MemoryProtectionScope.SameProcess);
            }
        }

        protected byte[] EncryptPrivateKey(byte[] decryptedPrivateKey)
        {
            ProtectedMemory.Unprotect(masterKey, MemoryProtectionScope.SameProcess);
            try
            {
                return decryptedPrivateKey.AesEncrypt(masterKey, iv);
            }
            finally
            {
                ProtectedMemory.Protect(masterKey, MemoryProtectionScope.SameProcess);
            }
        }

        public Account GetAccount(UInt160 publicKeyHash)
        {
            return accounts[publicKeyHash];
        }

        public Account GetAccountByScriptHash(UInt160 scriptHash)
        {
            return accounts[contracts[scriptHash].PublicKeyHash];
        }

        public IEnumerable<Account> GetAccounts()
        {
            return accounts.Values;
        }

        public IEnumerable<UInt160> GetAddresses()
        {
            return contracts.Keys;
        }

        public Contract GetContract(UInt160 scriptHash)
        {
            return contracts[scriptHash];
        }

        public IEnumerable<Contract> GetContracts()
        {
            return contracts.Values;
        }

        public Account Import(string wif)
        {
            if (wif == null) throw new ArgumentNullException();
            byte[] data = Base58.Decode(wif);
            if (data.Length != 38 || data[0] != 0x80 || data[33] != 0x01)
                throw new FormatException();
            byte[] checksum = data.Sha256(0, data.Length - 4).Sha256();
            if (!data.Skip(data.Length - 4).SequenceEqual(checksum.Take(4)))
                throw new FormatException();
            byte[] privateKey = new byte[32];
            Buffer.BlockCopy(data, 1, privateKey, 0, privateKey.Length);
            Array.Clear(data, 0, data.Length);
            Account account = new Account(privateKey);
            Array.Clear(privateKey, 0, privateKey.Length);
            accounts.Add(account.PublicKeyHash, account);
            OnCreateAccount(account);
            return account;
        }

        protected abstract IEnumerable<Account> LoadAccounts();

        protected abstract IEnumerable<Contract> LoadContracts();

        protected abstract byte[] LoadStoredData(string name);

        protected abstract IEnumerable<UnspentCoin> LoadUnspentCoins(bool is_change);

        protected abstract void OnCreateAccount(Account account);

        protected abstract void OnDeleteAccount(UInt160 publicKeyHash);

        protected abstract void SaveStoredData(string name, byte[] value);

        public bool Sign(SignatureContext context)
        {
            bool fSuccess = false;
            foreach (UInt160 scriptHash in context.ScriptHashes)
            {
                Contract contract = GetContract(scriptHash);
                if (contract == null) continue;
                Account account = GetAccountByScriptHash(scriptHash);
                if (account == null) continue;
                byte[] signature;
                using (account.Decrypt())
                {
                    signature = context.Signable.Sign(account.PrivateKey, account.PublicKey.EncodePoint(false).Skip(1).ToArray());
                }
                fSuccess |= context.Add(contract.RedeemScript, account.PublicKey, signature);
            }
            return fSuccess;
        }

        public static string ToAddress(UInt160 scriptHash)
        {
            byte[] data = new byte[] { CoinVersion }.Concat(scriptHash.ToArray()).ToArray();
            return Base58.Encode(data.Concat(data.Sha256().Sha256().Take(4)).ToArray());
        }

        public static UInt160 ToScriptHash(string address)
        {
            byte[] data = Base58.Decode(address);
            if (data.Length != 25)
                throw new FormatException();
            if (data[0] != CoinVersion)
                throw new FormatException();
            if (!data.Take(21).Sha256().Sha256().Take(4).SequenceEqual(data.Skip(21)))
                throw new FormatException();
            return new UInt160(data.Skip(1).Take(20).ToArray());
        }
    }
}
