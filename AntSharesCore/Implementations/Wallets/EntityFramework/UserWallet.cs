using AntShares.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using WAccount = AntShares.Wallets.Account;
using WContract = AntShares.Wallets.Contract;

namespace AntShares.Implementations.Wallets.EntityFramework
{
    public class UserWallet : Wallet
    {
        private string path;

        private UserWallet(string path, byte[] masterKey, byte[] iv)
            : base(masterKey, iv)
        {
            this.path = path;
        }

        public static UserWallet CreateDatabase(string path, string password)
        {
            using (WalletDataContext ctx = new WalletDataContext(path))
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                byte[] passwordKey = password.ToAesKey();
                byte[] masterKey = new byte[32];
                byte[] iv = new byte[16];
                rng.GetNonZeroBytes(masterKey);
                rng.GetNonZeroBytes(iv);
                masterKey.AesEncrypt(passwordKey, iv);
                Array.Clear(passwordKey, 0, passwordKey.Length);
                ctx.Database.EnsureDeleted();
                ctx.Database.EnsureCreated();
                ctx.Keys.Add(new Key
                {
                    Name = Key.MasterKey,
                    Value = masterKey
                });
                ctx.Keys.Add(new Key
                {
                    Name = Key.IV,
                    Value = iv
                });
                ctx.SaveChanges();
            }
            UserWallet wallet = OpenDatabase(path, password);
            wallet.CreateAccount();
            return wallet;
        }

        protected override void DeleteAccount(UInt160 publicKeyHash)
        {
            using (WalletDataContext ctx = new WalletDataContext(path))
            {
                Account account = ctx.Accounts.FirstOrDefault(p => p.PublicKeyHash == publicKeyHash.ToArray());
                if (account != null)
                {
                    ctx.Contracts.RemoveRange(ctx.Contracts.Where(p => p.PublicKeyHash == publicKeyHash.ToArray()));
                    ctx.Accounts.Remove(account);
                    ctx.SaveChanges();
                }
            }
        }

        public override WAccount GetAccount(UInt160 publicKeyHash)
        {
            using (WalletDataContext ctx = new WalletDataContext(path))
            {
                return GetAccountInternal(ctx.Accounts.FirstOrDefault(p => p.PublicKeyHash == publicKeyHash.ToArray())?.PrivateKeyEncrypted);
            }
        }

        public override WAccount GetAccountByScriptHash(UInt160 scriptHash)
        {
            using (WalletDataContext ctx = new WalletDataContext(path))
            {
                byte[] publicKeyHash = ctx.Contracts.FirstOrDefault(p => p.ScriptHash == scriptHash.ToArray())?.PublicKeyHash;
                if (publicKeyHash == null) return null;
                return GetAccountInternal(ctx.Accounts.FirstOrDefault(p => p.PublicKeyHash == publicKeyHash)?.PrivateKeyEncrypted);
            }
        }

        private WAccount GetAccountInternal(byte[] encryptedPrivateKey)
        {
            if (encryptedPrivateKey?.Length != 96) return null;
            byte[] decryptedPrivateKey = DecryptPrivateKey(encryptedPrivateKey);
            WAccount account = new WAccount(decryptedPrivateKey);
            Array.Clear(decryptedPrivateKey, 0, decryptedPrivateKey.Length);
            return account;
        }

        public override IEnumerable<WAccount> GetAccounts()
        {
            using (WalletDataContext ctx = new WalletDataContext(path))
            {
                foreach (byte[] encryptedPrivateKey in ctx.Accounts.Select(p => p.PrivateKeyEncrypted))
                {
                    yield return GetAccountInternal(encryptedPrivateKey);
                }
            }
        }

        public override IEnumerable<UInt160> GetAddresses()
        {
            using (WalletDataContext ctx = new WalletDataContext(path))
            {
                foreach (byte[] scriptHash in ctx.Contracts.Select(p => p.ScriptHash))
                {
                    yield return new UInt160(scriptHash);
                }
            }
        }

        public override WContract GetContract(UInt160 scriptHash)
        {
            using (WalletDataContext ctx = new WalletDataContext(path))
            {
                byte[] redeemScript = ctx.Contracts.FirstOrDefault(p => p.ScriptHash == scriptHash.ToArray())?.RedeemScript;
                if (redeemScript == null) return null;
                return new WContract(redeemScript);
            }
        }

        public override IEnumerable<WContract> GetContracts()
        {
            using (WalletDataContext ctx = new WalletDataContext(path))
            {
                foreach (byte[] redeemScript in ctx.Contracts.Select(p => p.RedeemScript))
                {
                    yield return new WContract(redeemScript);
                }
            }
        }

        public static UserWallet OpenDatabase(string path, string password)
        {
            using (WalletDataContext ctx = new WalletDataContext(path))
            {
                byte[] masterKey = ctx.Keys.First(p => p.Name == Key.MasterKey).Value;
                byte[] passwordKey = password.ToAesKey();
                byte[] iv = ctx.Keys.First(p => p.Name == Key.IV).Value;
                masterKey.AesDecrypt(passwordKey, iv);
                Array.Clear(passwordKey, 0, passwordKey.Length);
                return new UserWallet(path, masterKey, iv);
            }
        }

        public void Rebuild()
        {
            //TODO: 重建钱包数据库中的交易数据
            //1. 清空所有交易数据；
            //2. 穷举所有的Unspent，找出钱包账户所持有的那部分；
            //3. 写入数据库；
            throw new NotImplementedException();
        }

        protected override void SaveAccount(WAccount account)
        {
            byte[] decryptedPrivateKey = new byte[96];
            Buffer.BlockCopy(account.PublicKey.EncodePoint(false), 1, decryptedPrivateKey, 0, 64);
            using (account.Decrypt())
            {
                Buffer.BlockCopy(account.PrivateKey, 0, decryptedPrivateKey, 64, 32);
            }
            byte[] encryptedPrivateKey = EncryptPrivateKey(decryptedPrivateKey);
            Array.Clear(decryptedPrivateKey, 0, decryptedPrivateKey.Length);
            using (WalletDataContext ctx = new WalletDataContext(path))
            {
                Account db_account = ctx.Accounts.FirstOrDefault(p => p.PublicKeyHash == account.PublicKeyHash.ToArray());
                if (db_account == null)
                {
                    db_account = ctx.Accounts.Add(new Account
                    {
                        PrivateKeyEncrypted = encryptedPrivateKey,
                        PublicKeyHash = account.PublicKeyHash.ToArray()
                    }).Entity;
                }
                else
                {
                    db_account.PrivateKeyEncrypted = encryptedPrivateKey;
                }
                ctx.SaveChanges();
            }
        }
    }
}
