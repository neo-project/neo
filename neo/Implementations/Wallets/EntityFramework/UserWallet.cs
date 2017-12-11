using Microsoft.EntityFrameworkCore;
using Neo.Core;
using Neo.Cryptography;
using Neo.IO;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;

namespace Neo.Implementations.Wallets.EntityFramework
{
    public class UserWallet : Wallet, IDisposable
    {
        public override event EventHandler<BalanceEventArgs> BalanceChanged;

        private readonly string path;
        private readonly byte[] iv;
        private readonly byte[] masterKey;
        private readonly Dictionary<UInt160, WalletAccount> accounts;
        private readonly Dictionary<UInt256, Transaction> unconfirmed = new Dictionary<UInt256, Transaction>();

        public override string Name => Path.GetFileNameWithoutExtension(path);
        public override uint WalletHeight => WalletIndexer.IndexHeight;

        public override Version Version
        {
            get
            {
                byte[] buffer = LoadStoredData("Version");
                if (buffer == null) return new Version(0, 0);
                int major = buffer.ToInt32(0);
                int minor = buffer.ToInt32(4);
                int build = buffer.ToInt32(8);
                int revision = buffer.ToInt32(12);
                return new Version(major, minor, build, revision);
            }
        }

        private UserWallet(string path, byte[] passwordKey)
        {
            this.path = path;
            byte[] passwordHash = LoadStoredData("PasswordHash");
            if (passwordHash != null && !passwordHash.SequenceEqual(passwordKey.Sha256()))
                throw new CryptographicException();
            this.iv = LoadStoredData("IV");
            this.masterKey = LoadStoredData("MasterKey").AesDecrypt(passwordKey, iv);
#if NET47
            ProtectedMemory.Protect(masterKey, MemoryProtectionScope.SameProcess);
#endif
            this.accounts = LoadAccounts();
            WalletIndexer.RegisterAccounts(accounts.Keys);
            WalletIndexer.BalanceChanged += WalletIndexer_BalanceChanged;
        }

        public override void ApplyTransaction(Transaction tx)
        {
            lock (unconfirmed)
            {
                unconfirmed[tx.Hash] = tx;
            }
            BalanceChanged?.Invoke(this, new BalanceEventArgs
            {
                Transaction = tx,
                RelatedAccounts = tx.Scripts.Select(p => p.ScriptHash).Union(tx.Outputs.Select(p => p.ScriptHash)).Where(p => Contains(p)).ToArray(),
                Height = null,
                Time = DateTime.UtcNow.ToTimestamp()
            });
        }

        public bool ChangePassword(string password_old, string password_new)
        {
            if (!VerifyPassword(password_old)) return false;
            byte[] passwordKey = password_new.ToAesKey();
#if NET47
            using (new ProtectedMemoryContext(masterKey, MemoryProtectionScope.SameProcess))
#endif
            {
                try
                {
                    SaveStoredData("PasswordHash", passwordKey.Sha256());
                    SaveStoredData("MasterKey", masterKey.AesEncrypt(passwordKey, iv));
                    return true;
                }
                finally
                {
                    Array.Clear(passwordKey, 0, passwordKey.Length);
                }
            }
        }

        public override bool Contains(UInt160 scriptHash)
        {
            return accounts.ContainsKey(scriptHash);
        }

        public override WalletAccount CreateAccount(byte[] privateKey)
        {
            throw new NotSupportedException();
        }

        public override WalletAccount CreateAccount(SmartContract.Contract contract, KeyPair key = null)
        {
            throw new NotSupportedException();
        }

        public override WalletAccount CreateAccount(UInt160 scriptHash)
        {
            throw new NotSupportedException();
        }

        private byte[] DecryptPrivateKey(byte[] encryptedPrivateKey)
        {
            if (encryptedPrivateKey == null) throw new ArgumentNullException(nameof(encryptedPrivateKey));
            if (encryptedPrivateKey.Length != 96) throw new ArgumentException();
#if NET47
            using (new ProtectedMemoryContext(masterKey, MemoryProtectionScope.SameProcess))
#endif
            {
                return encryptedPrivateKey.AesDecrypt(masterKey, iv);
            }
        }

        public override bool DeleteAccount(UInt160 scriptHash)
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
            WalletIndexer.BalanceChanged -= WalletIndexer_BalanceChanged;
        }

        public override Coin[] FindUnspentCoins(UInt256 asset_id, Fixed8 amount)
        {
            return FindUnspentCoins(FindUnspentCoins().ToArray().Where(p => GetAccount(p.Output.ScriptHash).Contract.IsStandard), asset_id, amount) ?? base.FindUnspentCoins(asset_id, amount);
        }

        public override WalletAccount GetAccount(UInt160 scriptHash)
        {
            accounts.TryGetValue(scriptHash, out WalletAccount account);
            return account;
        }

        public override IEnumerable<WalletAccount> GetAccounts()
        {
            return accounts.Values;
        }

        public override IEnumerable<Coin> GetCoins(IEnumerable<UInt160> accounts)
        {
            if (unconfirmed.Count == 0)
                return WalletIndexer.GetCoins(accounts);
            else
                return GetCoinsInternal();
            IEnumerable<Coin> GetCoinsInternal()
            {
                HashSet<CoinReference> inputs, claims;
                Coin[] coins_unconfirmed;
                lock (unconfirmed)
                {
                    inputs = new HashSet<CoinReference>(unconfirmed.Values.SelectMany(p => p.Inputs));
                    claims = new HashSet<CoinReference>(unconfirmed.Values.OfType<ClaimTransaction>().SelectMany(p => p.Claims));
                    coins_unconfirmed = unconfirmed.Values.Select(tx => tx.Outputs.Select((o, i) => new Coin
                    {
                        Reference = new CoinReference
                        {
                            PrevHash = tx.Hash,
                            PrevIndex = (ushort)i
                        },
                        Output = o,
                        State = CoinState.Unconfirmed
                    })).SelectMany(p => p).ToArray();
                }
                foreach (Coin coin in WalletIndexer.GetCoins(accounts))
                {
                    if (inputs.Contains(coin.Reference))
                    {
                        if (coin.Output.AssetId.Equals(Blockchain.GoverningToken.Hash))
                            yield return new Coin
                            {
                                Reference = coin.Reference,
                                Output = coin.Output,
                                State = coin.State | CoinState.Spent
                            };
                        continue;
                    }
                    else if (claims.Contains(coin.Reference))
                    {
                        continue;
                    }
                    yield return coin;
                }
                HashSet<UInt160> accounts_set = new HashSet<UInt160>(accounts);
                foreach (Coin coin in coins_unconfirmed)
                {
                    if (accounts_set.Contains(coin.Output.ScriptHash))
                        yield return coin;
                }
            }
        }

        public override IEnumerable<UInt256> GetTransactions()
        {
            foreach (UInt256 hash in WalletIndexer.GetTransactions(accounts.Keys))
                yield return hash;
            lock (unconfirmed)
            {
                foreach (UInt256 hash in unconfirmed.Keys)
                    yield return hash;
            }
        }

        private Dictionary<UInt160, WalletAccount> LoadAccounts()
        {
            using (WalletDataContext ctx = new WalletDataContext(path))
            {
                Dictionary<UInt160, WalletAccount> accounts = ctx.Addresses.Select(p => p.ScriptHash).AsEnumerable().Select(p => new UserWalletAccount(new UInt160(p))).ToDictionary(p => p.ScriptHash, p => (WalletAccount)p);
                foreach (Contract db_contract in ctx.Contracts.Include(p => p.Account))
                {
                    VerificationContract contract = db_contract.RawData.AsSerializable<VerificationContract>();
                    UserWalletAccount account = (UserWalletAccount)accounts[contract.ScriptHash];
                    account.Contract = contract;
                    account.Key = new KeyPair(DecryptPrivateKey(db_contract.Account.PrivateKeyEncrypted));
                }
                return accounts;
            }
        }

        private byte[] LoadStoredData(string name)
        {
            using (WalletDataContext ctx = new WalletDataContext(path))
            {
                return ctx.Keys.FirstOrDefault(p => p.Name == name)?.Value;
            }
        }

        public static UserWallet Open(string path, string password)
        {
            return new UserWallet(path, password.ToAesKey());
        }

        public static UserWallet Open(string path, SecureString password)
        {
            return new UserWallet(path, password.ToAesKey());
        }

        private void SaveStoredData(string name, byte[] value)
        {
            using (WalletDataContext ctx = new WalletDataContext(path))
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

        public override bool VerifyPassword(string password)
        {
            return password.ToAesKey().Sha256().SequenceEqual(LoadStoredData("PasswordHash"));
        }

        private void WalletIndexer_BalanceChanged(object sender, BalanceEventArgs e)
        {
            lock (unconfirmed)
            {
                unconfirmed.Remove(e.Transaction.Hash);
            }
            UInt160[] relatedAccounts = e.RelatedAccounts.Where(p => Contains(p)).ToArray();
            if (relatedAccounts.Length > 0)
            {
                BalanceChanged?.Invoke(this, new BalanceEventArgs
                {
                    Transaction = e.Transaction,
                    RelatedAccounts = relatedAccounts,
                    Height = e.Height,
                    Time = e.Time
                });
            }
        }
    }
}
