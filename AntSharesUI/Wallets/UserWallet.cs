using AntShares.Cryptography;
using AntShares.Data;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using DbAccount = AntShares.Data.Account;

namespace AntShares.Wallets
{
    internal class UserWallet : Wallet
    {
        private string connectionString;

        private UserWallet(string connectionString, byte[] masterKey, byte[] iv)
            : base(masterKey, iv)
        {
            this.connectionString = connectionString;
        }

        public static UserWallet CreateDatabase(string path, string password)
        {
            SqlConnectionStringBuilder sb = new SqlConnectionStringBuilder();
            sb.AttachDBFilename = path;
            sb.DataSource = @"(LocalDB)\v11.0";
            sb.IntegratedSecurity = true;
            using (WalletDataContext ctx = new WalletDataContext(sb.ToString()))
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                byte[] passwordKey = password.ToAesKey();
                byte[] masterKey = new byte[32];
                byte[] iv = new byte[16];
                rng.GetNonZeroBytes(masterKey);
                rng.GetNonZeroBytes(iv);
                masterKey.AesEncrypt(passwordKey, iv);
                Array.Clear(passwordKey, 0, passwordKey.Length);
                ctx.Database.Delete();
                ctx.Database.Create();
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
            using (WalletDataContext ctx = new WalletDataContext(connectionString))
            {
                DbAccount account = ctx.Accounts.FirstOrDefault(p => p.PublicKeyHash == publicKeyHash.ToArray());
                if (account != null)
                {
                    ctx.Contracts.RemoveRange(ctx.Contracts.Where(p => p.PublicKeyHash == publicKeyHash.ToArray()));
                    ctx.Accounts.Remove(account);
                    ctx.SaveChanges();
                }
            }
        }

        public override Account GetAccount(UInt160 publicKeyHash)
        {
            using (WalletDataContext ctx = new WalletDataContext(connectionString))
            {
                return GetAccountInternal(ctx.Accounts.FirstOrDefault(p => p.PublicKeyHash == publicKeyHash.ToArray())?.PrivateKeyEncrypted);
            }
        }

        public override Account GetAccountByScriptHash(UInt160 scriptHash)
        {
            using (WalletDataContext ctx = new WalletDataContext(connectionString))
            {
                byte[] publicKeyHash = ctx.Contracts.FirstOrDefault(p => p.ScriptHash == scriptHash.ToArray())?.PublicKeyHash;
                if (publicKeyHash == null) return null;
                return GetAccountInternal(ctx.Accounts.FirstOrDefault(p => p.PublicKeyHash == publicKeyHash)?.PrivateKeyEncrypted);
            }
        }

        private Account GetAccountInternal(byte[] encryptedPrivateKey)
        {
            if (encryptedPrivateKey?.Length != 96) return null;
            byte[] decryptedPrivateKey = DecryptPrivateKey(encryptedPrivateKey);
            Account account = new Account(decryptedPrivateKey);
            Array.Clear(decryptedPrivateKey, 0, decryptedPrivateKey.Length);
            return account;
        }

        public override IEnumerable<Account> GetAccounts()
        {
            using (WalletDataContext ctx = new WalletDataContext(connectionString))
            {
                foreach (byte[] encryptedPrivateKey in ctx.Accounts.Select(p => p.PrivateKeyEncrypted))
                {
                    yield return GetAccountInternal(encryptedPrivateKey);
                }
            }
        }

        public override IEnumerable<UInt160> GetAddresses()
        {
            using (WalletDataContext ctx = new WalletDataContext(connectionString))
            {
                foreach (byte[] scriptHash in ctx.Contracts.Select(p => p.ScriptHash))
                {
                    yield return new UInt160(scriptHash);
                }
            }
        }

        public override Contract GetContract(UInt160 scriptHash)
        {
            using (WalletDataContext ctx = new WalletDataContext(connectionString))
            {
                byte[] redeemScript = ctx.Contracts.FirstOrDefault(p => p.ScriptHash == scriptHash.ToArray())?.RedeemScript;
                if (redeemScript == null) return null;
                return new Contract(redeemScript);
            }
        }

        public override IEnumerable<Contract> GetContracts()
        {
            using (WalletDataContext ctx = new WalletDataContext(connectionString))
            {
                foreach (byte[] redeemScript in ctx.Contracts.Select(p => p.RedeemScript))
                {
                    yield return new Contract(redeemScript);
                }
            }
        }

        public static UserWallet OpenDatabase(string path, string password)
        {
            SqlConnectionStringBuilder sb = new SqlConnectionStringBuilder();
            sb.AttachDBFilename = path;
            sb.DataSource = @"(LocalDB)\MSSQLLocalDB";
            sb.IntegratedSecurity = true;
            string connectionString = sb.ToString();
            using (WalletDataContext ctx = new WalletDataContext(connectionString))
            {
                byte[] masterKey = ctx.Keys.First(p => p.Name == Key.MasterKey).Value;
                byte[] passwordKey = password.ToAesKey();
                byte[] iv = ctx.Keys.First(p => p.Name == Key.IV).Value;
                masterKey.AesDecrypt(passwordKey, iv);
                Array.Clear(passwordKey, 0, passwordKey.Length);
                return new UserWallet(connectionString, masterKey, iv);
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

        protected override void SaveAccount(Account account)
        {
            byte[] decryptedPrivateKey = new byte[96];
            Buffer.BlockCopy(account.PublicKey, 0, decryptedPrivateKey, 0, 64);
            using (account.Decrypt())
            {
                Buffer.BlockCopy(account.PrivateKey, 0, decryptedPrivateKey, 64, 32);
            }
            byte[] encryptedPrivateKey = EncryptPrivateKey(decryptedPrivateKey);
            Array.Clear(decryptedPrivateKey, 0, decryptedPrivateKey.Length);
            using (WalletDataContext ctx = new WalletDataContext(connectionString))
            {
                DbAccount db_account = ctx.Accounts.FirstOrDefault(p => p.PublicKeyHash == account.PublicKeyHash.ToArray());
                if (db_account == null)
                {
                    db_account = ctx.Accounts.Add(new DbAccount
                    {
                        PrivateKeyEncrypted = encryptedPrivateKey,
                        PublicKeyHash = account.PublicKeyHash.ToArray()
                    });
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
