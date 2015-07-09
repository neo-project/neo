using AntShares.Cryptography;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace AntShares.Wallets
{
    internal class UserWallet : Wallet
    {
        private static readonly string TemplatePath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Data", "wt.mdf");
        private string connectionString;

        private UserWallet(string connectionString, byte[] masterKey)
            : base(masterKey)
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
                rng.GetNonZeroBytes(masterKey);
                masterKey.AesEncrypt(passwordKey);
                Array.Clear(passwordKey, 0, passwordKey.Length);
                ctx.Database.Delete();
                ctx.Database.Create();
                ctx.Keys.Add(new Key
                {
                    Name = Key.MasterKey,
                    Value = masterKey
                });
                ctx.SaveChanges();
            }
            UserWallet wallet = OpenDatabase(path, password);
            wallet.CreateEntry();
            return wallet;
        }

        protected override void DeleteEntry(UInt160 scriptHash)
        {
            using (WalletDataContext ctx = new WalletDataContext(connectionString))
            {
                Account account = ctx.Accounts.FirstOrDefault(p => p.ScriptHash == scriptHash.ToArray());
                if (account != null)
                {
                    ctx.Accounts.Remove(account);
                    ctx.SaveChanges();
                }
            }
        }

        public override IEnumerable<UInt160> GetAddresses()
        {
            using (WalletDataContext ctx = new WalletDataContext(connectionString))
            {
                return ctx.Accounts.Select(p => p.ScriptHash).ToArray().Select(p => new UInt160(p));
            }
        }

        protected override void GetEncryptedEntry(UInt160 scriptHash, out byte[] redeemScript, out byte[] encryptedPrivateKey)
        {
            using (WalletDataContext ctx = new WalletDataContext(connectionString))
            {
                //Account account = ctx.Accounts.FirstOrDefault(p => p.ScriptHash == scriptHash.ToArray());
                //It throws a NotSupportedException:
                //LINQ to Entities does not recognize the method 'Byte[] ToArray()' method, and this method cannot be translated into a store expression.
                //I don't know why.
                //So,
                byte[] temp = scriptHash.ToArray();
                Account account = ctx.Accounts.FirstOrDefault(p => p.ScriptHash == temp);
                //It works!

                if (account == null)
                {
                    redeemScript = null;
                    encryptedPrivateKey = null;
                    return;
                }
                redeemScript = account.RedeemScript;
                encryptedPrivateKey = account.PrivateKeyEncrypted;
            }
        }

        public static UserWallet OpenDatabase(string path, string password)
        {
            SqlConnectionStringBuilder sb = new SqlConnectionStringBuilder();
            sb.AttachDBFilename = path;
            sb.DataSource = @"(LocalDB)\v11.0";
            sb.IntegratedSecurity = true;
            string connectionString = sb.ToString();
            using (WalletDataContext ctx = new WalletDataContext(connectionString))
            {
                Key key = ctx.Keys.FirstOrDefault(p => p.Name == Key.MasterKey);
                byte[] masterKey = key.Value;
                byte[] passwordKey = password.ToAesKey();
                masterKey.AesDecrypt(passwordKey);
                Array.Clear(passwordKey, 0, passwordKey.Length);
                return new UserWallet(connectionString, masterKey);
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

        protected override void SaveEncryptedEntry(UInt160 scriptHash, byte[] redeemScript, byte[] encryptedPrivateKey)
        {
            using (WalletDataContext ctx = new WalletDataContext(connectionString))
            {
                byte[] scriptHashBytes = scriptHash.ToArray();
                Account account = ctx.Accounts.FirstOrDefault(p => p.ScriptHash == scriptHashBytes);
                if (account == null)
                {
                    account = ctx.Accounts.Add(new Account
                    {
                        ScriptHash = scriptHash.ToArray(),
                        RedeemScript = redeemScript,
                        PrivateKeyEncrypted = encryptedPrivateKey
                    });
                }
                else
                {
                    account.RedeemScript = redeemScript;
                    account.PrivateKeyEncrypted = encryptedPrivateKey;
                }
                ctx.SaveChanges();
            }
        }
    }
}
