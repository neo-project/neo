using AntShares.Core;
using AntShares.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using WalletAccount = AntShares.Wallets.Account;
using WalletContract = AntShares.Wallets.Contract;
using WalletUnspentCoin = AntShares.Wallets.UnspentCoin;

namespace AntShares.Implementations.Wallets.EntityFramework
{
    public class UserWallet : Wallet
    {
        private UserWallet(string path, string password, bool create)
            : base(path, password, create)
        {
        }

        public static UserWallet CreateDatabase(string path, string password)
        {
            using (WalletDataContext ctx = new WalletDataContext(path))
            {
                ctx.Database.EnsureDeleted();
                ctx.Database.EnsureCreated();
            }
            UserWallet wallet = new UserWallet(path, password, true);
            wallet.CreateAccount();
            return wallet;
        }

        protected override IEnumerable<WalletAccount> LoadAccounts()
        {
            using (WalletDataContext ctx = new WalletDataContext(DbPath))
            {
                foreach (byte[] encryptedPrivateKey in ctx.Accounts.Select(p => p.PrivateKeyEncrypted))
                {
                    byte[] decryptedPrivateKey = DecryptPrivateKey(encryptedPrivateKey);
                    WalletAccount account = new WalletAccount(decryptedPrivateKey);
                    Array.Clear(decryptedPrivateKey, 0, decryptedPrivateKey.Length);
                    yield return account;
                }
            }
        }

        protected override IEnumerable<WalletContract> LoadContracts()
        {
            using (WalletDataContext ctx = new WalletDataContext(DbPath))
            {
                foreach (Contract contract in ctx.Contracts)
                {
                    yield return new WalletContract(contract.RedeemScript, new UInt160(contract.PublicKeyHash));
                }
            }
        }

        protected override byte[] LoadStoredData(string name)
        {
            using (WalletDataContext ctx = new WalletDataContext(DbPath))
            {
                return ctx.Keys.FirstOrDefault(p => p.Name == name).Value;
            }
        }

        protected override IEnumerable<WalletUnspentCoin> LoadUnspentCoins(bool is_change)
        {
            using (WalletDataContext ctx = new WalletDataContext(DbPath))
            {
                foreach (UnspentCoin coin in ctx.UnspentCoins.Where(p => p.IsChange == is_change))
                {
                    yield return new WalletUnspentCoin
                    {
                        Input = new TransactionInput
                        {
                            PrevHash = new UInt256(coin.TxId),
                            PrevIndex = coin.Index
                        },
                        AssetId = new UInt256(coin.AssetId),
                        Value = new Fixed8(coin.Value),
                        ScriptHash = new UInt160(coin.ScriptHash)
                    };
                }
            }
        }

        protected override void OnCreateAccount(WalletAccount account)
        {
            byte[] decryptedPrivateKey = new byte[96];
            Buffer.BlockCopy(account.PublicKey.EncodePoint(false), 1, decryptedPrivateKey, 0, 64);
            using (account.Decrypt())
            {
                Buffer.BlockCopy(account.PrivateKey, 0, decryptedPrivateKey, 64, 32);
            }
            byte[] encryptedPrivateKey = EncryptPrivateKey(decryptedPrivateKey);
            Array.Clear(decryptedPrivateKey, 0, decryptedPrivateKey.Length);
            using (WalletDataContext ctx = new WalletDataContext(DbPath))
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

        protected override void OnDeleteAccount(UInt160 publicKeyHash)
        {
            using (WalletDataContext ctx = new WalletDataContext(DbPath))
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

        protected override void OnProcessNewBlock(IEnumerable<TransactionInput> spent, IEnumerable<WalletUnspentCoin> unspent)
        {
            using (WalletDataContext ctx = new WalletDataContext(DbPath))
            {
                foreach (TransactionInput input in spent)
                {
                    UnspentCoin unspent_coin = ctx.UnspentCoins.FirstOrDefault(p => p.TxId == input.PrevHash.ToArray() && p.Index == input.PrevIndex);
                    if (unspent_coin != null)
                        ctx.UnspentCoins.Remove(unspent_coin);
                }
                foreach (WalletUnspentCoin coin in unspent)
                {
                    UnspentCoin unspent_coin = ctx.UnspentCoins.FirstOrDefault(p => p.TxId == coin.Input.PrevHash.ToArray() && p.Index == coin.Input.PrevIndex);
                    if (unspent_coin == null)
                    {
                        unspent_coin = ctx.UnspentCoins.Add(new UnspentCoin
                        {
                            TxId = coin.Input.PrevHash.ToArray(),
                            Index = coin.Input.PrevIndex,
                            AssetId = coin.AssetId.ToArray(),
                            Value = coin.Value.GetData(),
                            ScriptHash = coin.ScriptHash.ToArray(),
                            IsChange = false
                        }).Entity;
                    }
                    else
                    {
                        unspent_coin.IsChange = false;
                    }
                }
                ctx.SaveChanges();
            }
        }

        public static UserWallet OpenDatabase(string path, string password)
        {
            return new UserWallet(path, password, false);
        }

        public void Rebuild()
        {
            //TODO: 重建钱包数据库中的交易数据
            //1. 清空所有交易数据；
            //2. 穷举所有的Unspent，找出钱包账户所持有的那部分；
            //3. 写入数据库；
            throw new NotImplementedException();
        }

        protected override void SaveStoredData(string name, byte[] value)
        {
            using (WalletDataContext ctx = new WalletDataContext(DbPath))
            {
                Key key = ctx.Keys.FirstOrDefault(p => p.Name == name);
                if (key == null)
                {
                    key = ctx.Keys.Add(new Key
                    {
                        Name = name,
                        Value = value
                    }).Entity;
                }
                else
                {
                    key.Value = value;
                }
                ctx.SaveChanges();
            }
        }
    }
}
