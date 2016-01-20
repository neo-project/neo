using AntShares.Core;
using AntShares.IO;
using AntShares.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using WalletAccount = AntShares.Wallets.Account;
using WalletContract = AntShares.Wallets.Contract;
using WalletUnspentCoin = AntShares.Wallets.UnspentCoin;

namespace AntShares.Implementations.Wallets.EntityFramework
{
    public class UserWallet : Wallet
    {
        protected UserWallet(string path, string password, bool create)
            : base(path, password, create)
        {
        }

        protected UserWallet(string path, SecureString password, bool create)
            : base(path, password, create)
        {
        }

        public override void AddContract(WalletContract contract)
        {
            base.AddContract(contract);
            using (WalletDataContext ctx = new WalletDataContext(DbPath))
            {
                Contract db_contract = ctx.Contracts.FirstOrDefault(p => p.ScriptHash.SequenceEqual(contract.ScriptHash.ToArray()));
                if (db_contract == null)
                {
                    db_contract = ctx.Contracts.Add(new Contract
                    {
                        Type = contract.GetType().ToString(),
                        RawData = contract.ToArray(),
                        ScriptHash = contract.ScriptHash.ToArray(),
                        PublicKeyHash = contract.PublicKeyHash.ToArray()
                    }).Entity;
                }
                else
                {
                    db_contract.PublicKeyHash = contract.PublicKeyHash.ToArray();
                }
                ctx.SaveChanges();
            }
        }

        protected override void BuildDatabase()
        {
            using (WalletDataContext ctx = new WalletDataContext(DbPath))
            {
                ctx.Database.EnsureDeleted();
                ctx.Database.EnsureCreated();
            }
        }

        public static UserWallet Create(string path, string password)
        {
            UserWallet wallet = new UserWallet(path, password, true);
            wallet.CreateAccount();
            return wallet;
        }

        public override WalletAccount CreateAccount(byte[] privateKey)
        {
            WalletAccount account = base.CreateAccount(privateKey);
            OnCreateAccount(account);
            AddContract(SignatureContract.Create(account.PublicKey));
            return account;
        }

        public override bool DeleteAccount(UInt160 publicKeyHash)
        {
            bool flag = base.DeleteAccount(publicKeyHash);
            if (flag)
            {
                using (WalletDataContext ctx = new WalletDataContext(DbPath))
                {
                    Account account = ctx.Accounts.FirstOrDefault(p => p.PublicKeyHash.SequenceEqual(publicKeyHash.ToArray()));
                    if (account != null)
                    {
                        ctx.Accounts.Remove(account);
                        ctx.SaveChanges();
                    }
                }
            }
            return flag;
        }

        public override bool DeleteContract(UInt160 scriptHash)
        {
            bool flag = base.DeleteContract(scriptHash);
            if (flag)
            {
                using (WalletDataContext ctx = new WalletDataContext(DbPath))
                {
                    Contract contract = ctx.Contracts.FirstOrDefault(p => p.ScriptHash.SequenceEqual(scriptHash.ToArray()));
                    if (contract != null)
                    {
                        ctx.Contracts.Remove(contract);
                        ctx.SaveChanges();
                    }
                }
            }
            return flag;
        }

        public override WalletUnspentCoin[] FindUnspentCoins(UInt256 asset_id, Fixed8 amount)
        {
            return FindUnspentCoins(FindUnspentCoins().Where(p => GetContract(p.ScriptHash) is SignatureContract), asset_id, amount) ?? base.FindUnspentCoins(asset_id, amount);
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
                    Type type = Type.GetType(contract.Type, false);
                    if (type == null || !typeof(WalletContract).IsAssignableFrom(type)) continue;
                    yield return (WalletContract)contract.RawData.AsSerializable(type);
                }
            }
        }

        protected override byte[] LoadStoredData(string name)
        {
            using (WalletDataContext ctx = new WalletDataContext(DbPath))
            {
                return ctx.Keys.FirstOrDefault(p => p.Name == name)?.Value;
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

        private void OnCreateAccount(WalletAccount account)
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
                Account db_account = ctx.Accounts.FirstOrDefault(p => p.PublicKeyHash.SequenceEqual(account.PublicKeyHash.ToArray()));
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

        protected override void OnProcessNewBlock(IEnumerable<TransactionInput> spent, IEnumerable<WalletUnspentCoin> unspent)
        {
            using (WalletDataContext ctx = new WalletDataContext(DbPath))
            {
                foreach (TransactionInput input in spent)
                {
                    UnspentCoin unspent_coin = ctx.UnspentCoins.FirstOrDefault(p => p.TxId.SequenceEqual(input.PrevHash.ToArray()) && p.Index == input.PrevIndex);
                    if (unspent_coin != null)
                        ctx.UnspentCoins.Remove(unspent_coin);
                }
                foreach (WalletUnspentCoin coin in unspent)
                {
					//这样速度更快，但不知道会不会出问题
                    //UnspentCoin unspent_coin = ctx.UnspentCoins.FirstOrDefault(p => p.TxId.SequenceEqual(coin.Input.PrevHash.ToArray()) && p.Index == coin.Input.PrevIndex);
                    UnspentCoin unspent_coin = null;
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
                ctx.Keys.First(p => p.Name == "Height").Value = BitConverter.GetBytes(WalletHeight);
                ctx.SaveChanges();
            }
        }

        public static UserWallet Open(string path, string password)
        {
            return new UserWallet(path, password, false);
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
