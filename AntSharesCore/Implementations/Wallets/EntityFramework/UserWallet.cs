using AntShares.Core;
using AntShares.IO;
using AntShares.Wallets;
using Microsoft.Data.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using CoreTransaction = AntShares.Core.Transaction;
using WalletAccount = AntShares.Wallets.Account;
using WalletCoin = AntShares.Wallets.Coin;
using WalletContract = AntShares.Wallets.Contract;

namespace AntShares.Implementations.Wallets.EntityFramework
{
    public class UserWallet : Wallet
    {
        public event EventHandler<IEnumerable<TransactionInfo>> TransactionsChanged;

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

        public static UserWallet Create(string path, SecureString password)
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

        public override WalletCoin[] FindUnspentCoins(UInt256 asset_id, Fixed8 amount)
        {
            return FindUnspentCoins(FindUnspentCoins().Where(p => GetContract(p.ScriptHash) is SignatureContract), asset_id, amount) ?? base.FindUnspentCoins(asset_id, amount);
        }

        private static IEnumerable<TransactionInfo> GetTransactionInfo(IEnumerable<Transaction> transactions)
        {
            return transactions.Select(p => new TransactionInfo
            {
                Transaction = CoreTransaction.DeserializeFrom(p.RawData),
                Height = p.Height,
                Time = p.Time
            });
        }

        public static Version GetVersion(string path)
        {
            byte[] buffer;
            using (WalletDataContext ctx = new WalletDataContext(path))
            {
                buffer = ctx.Keys.FirstOrDefault(p => p.Name == "Version")?.Value;
            }
            if (buffer == null) return new Version();
            int major = BitConverter.ToInt32(buffer, 0);
            int minor = BitConverter.ToInt32(buffer, 4);
            int build = BitConverter.ToInt32(buffer, 8);
            int revision = BitConverter.ToInt32(buffer, 12);
            return new Version(major, minor, build, revision);
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

        protected override IEnumerable<WalletCoin> LoadCoins()
        {
            using (WalletDataContext ctx = new WalletDataContext(DbPath))
            {
                foreach (Coin coin in ctx.Coins)
                {
                    yield return new WalletCoin
                    {
                        Input = new TransactionInput
                        {
                            PrevHash = new UInt256(coin.TxId),
                            PrevIndex = coin.Index
                        },
                        AssetId = new UInt256(coin.AssetId),
                        Value = new Fixed8(coin.Value),
                        ScriptHash = new UInt160(coin.ScriptHash),
                        State = coin.State
                    };
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

        public IEnumerable<TransactionInfo> LoadTransactions()
        {
            using (WalletDataContext ctx = new WalletDataContext(DbPath))
            {
                return GetTransactionInfo(ctx.Transactions.ToArray());
            }
        }

        public static void Migrate(string path_old, string path_new)
        {
            Version current_version = Assembly.GetExecutingAssembly().GetName().Version;
            using (WalletDataContext ctx_old = new WalletDataContext(path_old))
            using (WalletDataContext ctx_new = new WalletDataContext(path_new))
            {
                ctx_new.Database.EnsureCreated();
                ctx_new.Accounts.AddRange(ctx_old.Accounts);
                ctx_new.Contracts.AddRange(ctx_old.Contracts);
                ctx_new.Keys.AddRange(ctx_old.Keys.Where(p => p.Name != "Height" && p.Name != "Version"));
                SaveStoredData(ctx_new, "Height", BitConverter.GetBytes(0));
                SaveStoredData(ctx_new, "Version", new[] { current_version.Major, current_version.Minor, current_version.Build, current_version.Revision }.Select(p => BitConverter.GetBytes(p)).SelectMany(p => p).ToArray());
                ctx_new.SaveChanges();
            }
        }

        private void OnCoinsChanged(WalletDataContext ctx, IEnumerable<WalletCoin> added, IEnumerable<WalletCoin> changed, IEnumerable<WalletCoin> deleted)
        {
            foreach (WalletCoin coin in added)
            {
                ctx.Coins.Add(new Coin
                {
                    TxId = coin.Input.PrevHash.ToArray(),
                    Index = coin.Input.PrevIndex,
                    AssetId = coin.AssetId.ToArray(),
                    Value = coin.Value.GetData(),
                    ScriptHash = coin.ScriptHash.ToArray(),
                    State = CoinState.Unspent
                });
            }
            foreach (WalletCoin coin in changed)
            {
                ctx.Coins.First(p => p.TxId.SequenceEqual(coin.Input.PrevHash.ToArray()) && p.Index == coin.Input.PrevIndex).State = coin.State;
            }
            foreach (WalletCoin coin in deleted)
            {
                Coin unspent_coin = ctx.Coins.FirstOrDefault(p => p.TxId.SequenceEqual(coin.Input.PrevHash.ToArray()) && p.Index == coin.Input.PrevIndex);
                ctx.Coins.Remove(unspent_coin);
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

        protected override void OnProcessNewBlock(Block block, IEnumerable<CoreTransaction> transactions, IEnumerable<WalletCoin> added, IEnumerable<WalletCoin> changed, IEnumerable<WalletCoin> deleted)
        {
            Transaction[] tx_changed;
            using (WalletDataContext ctx = new WalletDataContext(DbPath))
            {
                foreach (Transaction db_tx in ctx.Transactions.Where(p => !p.Height.HasValue))
                    if (block.Transactions.Any(p => p.Hash == new UInt256(db_tx.Hash)))
                        db_tx.Height = block.Height;
                foreach (CoreTransaction tx in transactions)
                {
                    Transaction db_tx = ctx.Transactions.FirstOrDefault(p => p.Hash.SequenceEqual(tx.Hash.ToArray()));
                    if (db_tx == null)
                    {
                        ctx.Transactions.Add(new Transaction
                        {
                            Hash = tx.Hash.ToArray(),
                            Type = tx.Type,
                            RawData = tx.ToArray(),
                            Height = block.Height,
                            Time = block.Timestamp.ToDateTime()
                        });
                    }
                    else
                    {
                        db_tx.Height = block.Height;
                    }
                }
                tx_changed = ctx.ChangeTracker.Entries<Transaction>().Select(p => p.Entity).ToArray();
                OnCoinsChanged(ctx, added, changed, deleted);
                ctx.Keys.First(p => p.Name == "Height").Value = BitConverter.GetBytes(WalletHeight);
                ctx.SaveChanges();
            }
            if (tx_changed.Length > 0)
                TransactionsChanged?.Invoke(this, GetTransactionInfo(tx_changed));
        }

        protected override void OnSendTransaction(CoreTransaction tx, IEnumerable<WalletCoin> added, IEnumerable<WalletCoin> changed)
        {
            Transaction tx_changed;
            using (WalletDataContext ctx = new WalletDataContext(DbPath))
            {
                tx_changed = ctx.Transactions.Add(new Transaction
                {
                    Hash = tx.Hash.ToArray(),
                    Type = tx.Type,
                    RawData = tx.ToArray(),
                    Height = null,
                    Time = DateTime.Now
                }).Entity;
                OnCoinsChanged(ctx, added, changed, Enumerable.Empty<WalletCoin>());
                ctx.SaveChanges();
            }
            TransactionsChanged?.Invoke(this, GetTransactionInfo(new[] { tx_changed }));
        }

        public static UserWallet Open(string path, string password)
        {
            return new UserWallet(path, password, false);
        }

        public static UserWallet Open(string path, SecureString password)
        {
            return new UserWallet(path, password, false);
        }

        public override void Rebuild()
        {
            lock (SyncRoot)
            {
                base.Rebuild();
                using (WalletDataContext ctx = new WalletDataContext(DbPath))
                {
                    ctx.Keys.First(p => p.Name == "Height").Value = BitConverter.GetBytes(0);
                    ctx.Database.ExecuteSqlCommand($"DELETE FROM [{nameof(Transaction)}]");
                    ctx.Database.ExecuteSqlCommand($"DELETE FROM [{nameof(Coin)}]");
                    ctx.SaveChanges();
                }
            }
        }

        protected override void SaveStoredData(string name, byte[] value)
        {
            using (WalletDataContext ctx = new WalletDataContext(DbPath))
            {
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
    }
}
