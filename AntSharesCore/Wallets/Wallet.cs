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

        private byte[] masterKey;
        private byte[] iv;

        protected Wallet(byte[] masterKey, byte[] iv)
        {
            this.masterKey = masterKey;
            this.iv = iv;
            ProtectedMemory.Protect(masterKey, MemoryProtectionScope.SameProcess);
        }

        public Account CreateAccount()
        {
            using (CngKey key = CngKey.Create(CngAlgorithm.ECDsaP256, null, new CngKeyCreationParameters { ExportPolicy = CngExportPolicies.AllowPlaintextArchiving }))
            {
                byte[] privateKey = key.Export(CngKeyBlobFormat.EccPrivateBlob);
                Account account = new Account(privateKey);
                SaveAccount(account);
                Array.Clear(privateKey, 0, privateKey.Length);
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
                using (AesManaged aes = new AesManaged())
                {
                    aes.Padding = PaddingMode.None;
                    using (ICryptoTransform decryptor = aes.CreateDecryptor(masterKey, iv))
                    {
                        return decryptor.TransformFinalBlock(encryptedPrivateKey, 0, encryptedPrivateKey.Length);
                    }
                }
            }
            finally
            {
                ProtectedMemory.Protect(masterKey, MemoryProtectionScope.SameProcess);
            }
        }

        protected abstract void DeleteAccount(UInt160 publicKeyHash);

        protected byte[] EncryptPrivateKey(byte[] decryptedPrivateKey)
        {
            ProtectedMemory.Unprotect(masterKey, MemoryProtectionScope.SameProcess);
            try
            {
                using (AesManaged aes = new AesManaged())
                {
                    aes.Padding = PaddingMode.None;
                    using (ICryptoTransform encryptor = aes.CreateEncryptor(masterKey, iv))
                    {
                        return encryptor.TransformFinalBlock(decryptedPrivateKey, 0, decryptedPrivateKey.Length);
                    }
                }
            }
            finally
            {
                ProtectedMemory.Protect(masterKey, MemoryProtectionScope.SameProcess);
            }
        }

        public abstract Account GetAccount(UInt160 publicKeyHash);

        public abstract Account GetAccountByScriptHash(UInt160 scriptHash);

        public abstract IEnumerable<Account> GetAccounts();

        public abstract IEnumerable<UInt160> GetAddresses();

        public abstract Contract GetContract(UInt160 scriptHash);

        public abstract IEnumerable<Contract> GetContracts();

        public Account Import(string wif)
        {
            if (wif == null)
                throw new ArgumentNullException();
            byte[] data = Base58.Decode(wif);
            if (data.Length != 38 || data[0] != 0x80 || data[33] != 0x01)
                throw new FormatException();
            byte[] checksum = data.Sha256(0, data.Length - 4).Sha256();
            if (!data.Skip(data.Length - 4).SequenceEqual(checksum.Take(4)))
                throw new FormatException();
            byte[] privateKey = new byte[32];
            Buffer.BlockCopy(data, 1, privateKey, 0, privateKey.Length);
            Account account = new Account(privateKey);
            SaveAccount(account);
            Array.Clear(privateKey, 0, privateKey.Length);
            Array.Clear(data, 0, data.Length);
            return account;
        }

        protected abstract void SaveAccount(Account account);

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
