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
            sb.DataSource = @"(LocalDB)\v11.0";
            sb.IntegratedSecurity = true;
            using (WalletDataContext ctx = new WalletDataContext(sb.ToString()))
            {
                try
                {
                    ctx.ExecuteCommand(string.Format("DROP DATABASE \"{0}\"", path));
                }
                catch { }
            }
            sb.AttachDBFilename = path;
            using (WalletDataContext ctx = new WalletDataContext(sb.ToString()))
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                byte[] passwordKey = password.ToAesKey();
                byte[] masterKey = new byte[32];
                rng.GetNonZeroBytes(masterKey);
                masterKey.AesEncrypt(passwordKey);
                Array.Clear(passwordKey, 0, passwordKey.Length);
                ctx.Keys.InsertOnSubmit(new Key
                {
                    Name = KeyNames.MasterKey,
                    Value = masterKey
                });
                ctx.CreateDatabase();
                ctx.SubmitChanges();
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
                    ctx.Accounts.DeleteOnSubmit(account);
                }
            }
        }

        public override IEnumerable<UInt160> GetAddresses()
        {
            using (WalletDataContext ctx = new WalletDataContext(connectionString))
            {
                return ctx.Accounts.Select(p => p.ScriptHash.ToArray()).ToArray().Select(p => new UInt160(p));
            }
        }

        protected override void GetEncryptedEntry(UInt160 scriptHash, out byte[] redeemScript, out byte[] encryptedPrivateKey)
        {
            using (WalletDataContext ctx = new WalletDataContext(connectionString))
            {
                Account account = ctx.Accounts.FirstOrDefault(p => p.ScriptHash == scriptHash.ToArray());
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
                Key key = ctx.Keys.FirstOrDefault(p => p.Name == KeyNames.MasterKey);
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
                Account account = ctx.Accounts.FirstOrDefault(p => p.ScriptHash == scriptHash.ToArray());
                if (account == null)
                {
                    ctx.Accounts.InsertOnSubmit(account = new Account
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
                ctx.SubmitChanges();
            }
        }
    }
}
