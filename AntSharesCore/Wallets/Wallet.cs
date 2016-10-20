using AntShares.Core;
using AntShares.Core.Scripts;
using AntShares.Cryptography;
using AntShares.IO.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace AntShares.Wallets
{
    public abstract class Wallet : IDisposable
    {
        public event EventHandler BalanceChanged;

        public static readonly byte CoinVersion = Settings.Default.CoinVersion;

        private readonly string path;
        private readonly byte[] iv;
        private readonly byte[] masterKey;
        private readonly Dictionary<UInt160, Account> accounts;
        private readonly Dictionary<UInt160, Contract> contracts;
        private readonly TrackableCollection<TransactionInput, Coin> coins;
        private uint current_height;

        private readonly Thread thread;
        private bool isrunning = true;

        protected string DbPath => path;
        protected object SyncRoot { get; } = new object();
        public uint WalletHeight => current_height;
        protected abstract Version Version { get; }

        private Wallet(string path, byte[] passwordKey, bool create)
        {
            this.path = path;
            if (create)
            {
                this.iv = new byte[16];
                this.masterKey = new byte[32];
                this.accounts = new Dictionary<UInt160, Account>();
                this.contracts = new Dictionary<UInt160, Contract>();
                this.coins = new TrackableCollection<TransactionInput, Coin>();
                this.current_height = Blockchain.Default?.HeaderHeight + 1 ?? 0;
                using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(iv);
                    rng.GetBytes(masterKey);
                }
                BuildDatabase();
                SaveStoredData("PasswordHash", passwordKey.Sha256());
                SaveStoredData("IV", iv);
                SaveStoredData("MasterKey", masterKey.AesEncrypt(passwordKey, iv));
                SaveStoredData("Version", new[] { Version.Major, Version.Minor, Version.Build, Version.Revision }.Select(p => BitConverter.GetBytes(p)).SelectMany(p => p).ToArray());
                SaveStoredData("Height", BitConverter.GetBytes(current_height));
#if NET461
                ProtectedMemory.Protect(masterKey, MemoryProtectionScope.SameProcess);
#endif
            }
            else
            {
                byte[] passwordHash = LoadStoredData("PasswordHash");
                if (passwordHash != null && !passwordHash.SequenceEqual(passwordKey.Sha256()))
                    throw new CryptographicException();
                this.iv = LoadStoredData("IV");
                this.masterKey = LoadStoredData("MasterKey").AesDecrypt(passwordKey, iv);
#if NET461
                ProtectedMemory.Protect(masterKey, MemoryProtectionScope.SameProcess);
#endif
                this.accounts = LoadAccounts().ToDictionary(p => p.PublicKeyHash);
                this.contracts = LoadContracts().ToDictionary(p => p.ScriptHash);
                this.coins = new TrackableCollection<TransactionInput, Coin>(LoadCoins());
                this.current_height = BitConverter.ToUInt32(LoadStoredData("Height"), 0);
            }
            Array.Clear(passwordKey, 0, passwordKey.Length);
            this.thread = new Thread(ProcessBlocks);
            this.thread.IsBackground = true;
            this.thread.Name = "Wallet.ProcessBlocks";
            this.thread.Start();
        }

        protected Wallet(string path, string password, bool create)
            : this(path, password.ToAesKey(), create)
        {
        }

        protected Wallet(string path, SecureString password, bool create)
            : this(path, password.ToAesKey(), create)
        {
        }

        public virtual void AddContract(Contract contract)
        {
            lock (accounts)
            {
                if (!accounts.ContainsKey(contract.PublicKeyHash))
                    throw new InvalidOperationException();
                lock (contracts)
                {
                    contracts[contract.ScriptHash] = contract;
                }
            }
        }

        protected virtual void BuildDatabase()
        {
        }

        public static Fixed8 CalculateClaimAmount(IEnumerable<TransactionInput> inputs)
        {
            if (!Blockchain.Default.Ability.HasFlag(BlockchainAbility.UnspentIndexes))
                throw new NotSupportedException();
            List<Claimable> unclaimed = new List<Claimable>();
            foreach (var group in inputs.GroupBy(p => p.PrevHash))
            {
                Dictionary<ushort, Claimable> claimable = Blockchain.Default.GetUnclaimed(group.Key);
                if (claimable == null || claimable.Count == 0)
                    throw new ArgumentException();
                foreach (TransactionInput claim in group)
                {
                    if (!claimable.ContainsKey(claim.PrevIndex))
                        throw new ArgumentException();
                    unclaimed.Add(claimable[claim.PrevIndex]);
                }
            }
            return CalculateClaimAmountInternal(unclaimed);
        }

        public static Fixed8 CalculateClaimAmountUnavailable(IEnumerable<TransactionInput> inputs, uint height)
        {
            List<Claimable> unclaimed = new List<Claimable>();
            foreach (var group in inputs.GroupBy(p => p.PrevHash))
            {
                int height_start;
                Transaction tx = Blockchain.Default.GetTransaction(group.Key, out height_start);
                if (tx == null) throw new ArgumentException();
                foreach (TransactionInput claim in group)
                {
                    if (claim.PrevIndex >= tx.Outputs.Length || !tx.Outputs[claim.PrevIndex].AssetId.Equals(Blockchain.AntShare.Hash))
                        throw new ArgumentException();
                    unclaimed.Add(new Claimable
                    {
                        Output = tx.Outputs[claim.PrevIndex],
                        StartHeight = (uint)height_start,
                        EndHeight = height
                    });
                }
            }
            return CalculateClaimAmountInternal(unclaimed);
        }

        private static Fixed8 CalculateClaimAmountInternal(IEnumerable<Claimable> unclaimed)
        {
            Fixed8 amount_claimed = Fixed8.Zero;
            foreach (var group in unclaimed.GroupBy(p => new { p.StartHeight, p.EndHeight }))
            {
                uint amount = 0;
                uint ustart = group.Key.StartHeight / Blockchain.DecrementInterval;
                if (ustart < Blockchain.MintingAmount.Length)
                {
                    uint istart = group.Key.StartHeight % Blockchain.DecrementInterval;
                    uint uend = group.Key.EndHeight / Blockchain.DecrementInterval;
                    uint iend = group.Key.EndHeight % Blockchain.DecrementInterval;
                    if (uend >= Blockchain.MintingAmount.Length)
                    {
                        uend = (uint)Blockchain.MintingAmount.Length;
                        iend = 0;
                    }
                    if (iend == 0)
                    {
                        uend--;
                        iend = Blockchain.DecrementInterval;
                    }
                    while (ustart < uend)
                    {
                        amount += (Blockchain.DecrementInterval - istart) * Blockchain.MintingAmount[ustart];
                        ustart++;
                        istart = 0;
                    }
                    amount += (iend - istart) * Blockchain.MintingAmount[ustart];
                }
                amount += (uint)(Blockchain.Default.GetSysFeeAmount(group.Key.EndHeight - 1) - (group.Key.StartHeight == 0 ? 0 : Blockchain.Default.GetSysFeeAmount(group.Key.StartHeight - 1)));
                amount_claimed += group.Sum(p => p.Value) / 100000000 * amount;
            }
            return amount_claimed;
        }

        public bool ChangePassword(string password_old, string password_new)
        {
            byte[] passwordHash = LoadStoredData("PasswordHash");
            if (!passwordHash.SequenceEqual(password_old.ToAesKey().Sha256()))
                return false;
            byte[] passwordKey = password_new.ToAesKey();
#if NET461
            using (new ProtectedMemoryContext(masterKey, MemoryProtectionScope.SameProcess))
#endif
            {
                try
                {
                    SaveStoredData("MasterKey", masterKey.AesEncrypt(passwordKey, iv));
                    return true;
                }
                finally
                {
                    Array.Clear(passwordKey, 0, passwordKey.Length);
                }
            }
        }

        public bool ContainsAccount(Cryptography.ECC.ECPoint publicKey)
        {
            return ContainsAccount(publicKey.EncodePoint(true).ToScriptHash());
        }

        public bool ContainsAccount(UInt160 publicKeyHash)
        {
            lock (accounts)
            {
                return accounts.ContainsKey(publicKeyHash);
            }
        }

        public bool ContainsAddress(UInt160 scriptHash)
        {
            lock (contracts)
            {
                return contracts.ContainsKey(scriptHash);
            }
        }

        public Account CreateAccount()
        {
            byte[] privateKey = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }
            Account account = CreateAccount(privateKey);
            Array.Clear(privateKey, 0, privateKey.Length);
            return account;
        }

        public virtual Account CreateAccount(byte[] privateKey)
        {
            Account account = new Account(privateKey);
            lock (accounts)
            {
                accounts[account.PublicKeyHash] = account;
            }
            return account;
        }

        protected byte[] DecryptPrivateKey(byte[] encryptedPrivateKey)
        {
            if (encryptedPrivateKey == null) throw new ArgumentNullException(nameof(encryptedPrivateKey));
            if (encryptedPrivateKey.Length != 96) throw new ArgumentException();
#if NET461
            using (new ProtectedMemoryContext(masterKey, MemoryProtectionScope.SameProcess))
#endif
            {
                return encryptedPrivateKey.AesDecrypt(masterKey, iv);
            }
        }

        public virtual bool DeleteAccount(UInt160 publicKeyHash)
        {
            lock (accounts)
            {
                lock (contracts)
                {
                    foreach (Contract contract in contracts.Values.Where(p => p.PublicKeyHash == publicKeyHash).ToArray())
                    {
                        DeleteContract(contract.ScriptHash);
                    }
                }
                return accounts.Remove(publicKeyHash);
            }
        }

        public virtual bool DeleteContract(UInt160 scriptHash)
        {
            lock (contracts)
                lock (coins)
                {
                    foreach (TransactionInput key in coins.Where(p => p.ScriptHash == scriptHash).Select(p => p.Input).ToArray())
                    {
                        coins.Remove(key);
                    }
                    coins.Commit();
                    return contracts.Remove(scriptHash);
                }
        }

        public virtual void Dispose()
        {
            isrunning = false;
            if (!thread.ThreadState.HasFlag(ThreadState.Unstarted)) thread.Join();
        }

        protected byte[] EncryptPrivateKey(byte[] decryptedPrivateKey)
        {
#if NET461
            using (new ProtectedMemoryContext(masterKey, MemoryProtectionScope.SameProcess))
#endif
            {
                return decryptedPrivateKey.AesEncrypt(masterKey, iv);
            }
        }

        public IEnumerable<Coin> FindCoins()
        {
            lock (coins)
            {
                return coins.Where(p => p.State == CoinState.Unconfirmed || p.State == CoinState.Unspent).ToArray();
            }
        }

        public IEnumerable<Coin> FindUnspentCoins()
        {
            lock (coins)
            {
                return coins.Where(p => p.State == CoinState.Unspent).ToArray();
            }
        }

        public virtual Coin[] FindUnspentCoins(UInt256 asset_id, Fixed8 amount)
        {
            lock (coins)
            {
                return FindUnspentCoins(coins.Where(p => p.State == CoinState.Unspent), asset_id, amount);
            }
        }

        protected static Coin[] FindUnspentCoins(IEnumerable<Coin> unspents, UInt256 asset_id, Fixed8 amount)
        {
            Coin[] unspents_asset = unspents.Where(p => p.AssetId == asset_id).ToArray();
            Fixed8 sum = unspents_asset.Sum(p => p.Value);
            if (sum < amount) return null;
            if (sum == amount) return unspents_asset;
            Coin[] unspents_ordered = unspents_asset.OrderByDescending(p => p.Value).ToArray();
            int i = 0;
            while (unspents_ordered[i].Value <= amount)
                amount -= unspents_ordered[i++].Value;
            if (amount == Fixed8.Zero)
                return unspents_ordered.Take(i).ToArray();
            else
                return unspents_ordered.Take(i).Concat(new[] { unspents_ordered.Last(p => p.Value >= amount) }).ToArray();
        }

        public Account GetAccount(Cryptography.ECC.ECPoint publicKey)
        {
            return GetAccount(publicKey.EncodePoint(true).ToScriptHash());
        }

        public Account GetAccount(UInt160 publicKeyHash)
        {
            lock (accounts)
            {
                if (!accounts.ContainsKey(publicKeyHash)) return null;
                return accounts[publicKeyHash];
            }
        }

        public Account GetAccountByScriptHash(UInt160 scriptHash)
        {
            lock (accounts)
                lock (contracts)
                {
                    if (!contracts.ContainsKey(scriptHash)) return null;
                    return accounts[contracts[scriptHash].PublicKeyHash];
                }
        }

        public IEnumerable<Account> GetAccounts()
        {
            lock (accounts)
            {
                foreach (var pair in accounts)
                {
                    yield return pair.Value;
                }
            }
        }

        public IEnumerable<UInt160> GetAddresses()
        {
            lock (contracts)
            {
                foreach (var pair in contracts)
                {
                    yield return pair.Key;
                }
            }
        }

        public Fixed8 GetAvailable(UInt256 asset_id)
        {
            lock (coins)
            {
                return coins.Where(p => p.State == CoinState.Unspent && p.AssetId == asset_id).Sum(p => p.Value);
            }
        }

        public Fixed8 GetBalance(UInt256 asset_id)
        {
            lock (coins)
            {
                return coins.Where(p => (p.State == CoinState.Unconfirmed || p.State == CoinState.Unspent) && p.AssetId == asset_id).Sum(p => p.Value);
            }
        }

        public virtual UInt160 GetChangeAddress()
        {
            lock (contracts)
            {
                return contracts.Values.FirstOrDefault(p => p.IsStandard)?.ScriptHash ?? contracts.Keys.FirstOrDefault();
            }
        }

        public Contract GetContract(UInt160 scriptHash)
        {
            lock (contracts)
            {
                if (!contracts.ContainsKey(scriptHash)) return null;
                return contracts[scriptHash];
            }
        }

        public IEnumerable<Contract> GetContracts()
        {
            lock (contracts)
            {
                foreach (var pair in contracts)
                {
                    yield return pair.Value;
                }
            }
        }

        public IEnumerable<Contract> GetContracts(UInt160 publicKeyHash)
        {
            lock (contracts)
            {
                foreach (Contract contract in contracts.Values.Where(p => p.PublicKeyHash.Equals(publicKeyHash)))
                {
                    yield return contract;
                }
            }
        }

        public static byte[] GetPrivateKeyFromWIF(string wif)
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
            return privateKey;
        }

        public abstract IEnumerable<T> GetTransactions<T>() where T : Transaction;

        public IEnumerable<Coin> GetUnclaimedCoins()
        {
            lock (coins)
            {
                foreach (var coin in coins.Where(p => p.State == CoinState.Spent && p.AssetId == Blockchain.AntShare.Hash))
                {
                    yield return coin;
                }
            }
        }

        public Account Import(X509Certificate2 cert)
        {
            byte[] privateKey;
            using (ECDsa ecdsa = cert.GetECDsaPrivateKey())
            {
#if NET461
                privateKey = ((ECDsaCng)ecdsa).Key.Export(CngKeyBlobFormat.EccPrivateBlob);
#else
                privateKey = ecdsa.ExportParameters(true).D;
#endif
            }
            Account account = CreateAccount(privateKey);
            Array.Clear(privateKey, 0, privateKey.Length);
            return account;
        }

        public Account Import(string wif)
        {
            byte[] privateKey = GetPrivateKeyFromWIF(wif);
            Account account = CreateAccount(privateKey);
            Array.Clear(privateKey, 0, privateKey.Length);
            return account;
        }

        protected bool IsWalletTransaction(Transaction tx)
        {
            lock (contracts)
            {
                if (tx.Outputs.Any(p => contracts.ContainsKey(p.ScriptHash)))
                    return true;
                if (tx.Scripts.Any(p => contracts.ContainsKey(p.RedeemScript.ToScriptHash())))
                    return true;
            }
            return false;
        }

        protected abstract IEnumerable<Account> LoadAccounts();

        protected abstract IEnumerable<Coin> LoadCoins();

        protected abstract IEnumerable<Contract> LoadContracts();

        protected abstract byte[] LoadStoredData(string name);

        public T MakeTransaction<T>(T tx, Fixed8 fee) where T : Transaction
        {
            if (tx.Outputs == null) throw new ArgumentException();
            if (tx.Attributes == null) tx.Attributes = new TransactionAttribute[0];
            fee += tx.SystemFee;
            var pay_total = (typeof(T) == typeof(IssueTransaction) ? new TransactionOutput[0] : tx.Outputs).GroupBy(p => p.AssetId, (k, g) => new
            {
                AssetId = k,
                Value = g.Sum(p => p.Value)
            }).ToDictionary(p => p.AssetId);
            if (fee > Fixed8.Zero)
            {
                if (pay_total.ContainsKey(Blockchain.AntCoin.Hash))
                {
                    pay_total[Blockchain.AntCoin.Hash] = new
                    {
                        AssetId = Blockchain.AntCoin.Hash,
                        Value = pay_total[Blockchain.AntCoin.Hash].Value + fee
                    };
                }
                else
                {
                    pay_total.Add(Blockchain.AntCoin.Hash, new
                    {
                        AssetId = Blockchain.AntCoin.Hash,
                        Value = fee
                    });
                }
            }
            var pay_coins = pay_total.Select(p => new
            {
                AssetId = p.Key,
                Unspents = FindUnspentCoins(p.Key, p.Value.Value)
            }).ToDictionary(p => p.AssetId);
            if (pay_coins.Any(p => p.Value.Unspents == null)) return null;
            var input_sum = pay_coins.Values.ToDictionary(p => p.AssetId, p => new
            {
                AssetId = p.AssetId,
                Value = p.Unspents.Sum(q => q.Value)
            });
            UInt160 change_address = GetChangeAddress();
            List<TransactionOutput> outputs_new = new List<TransactionOutput>(tx.Outputs);
            foreach (UInt256 asset_id in input_sum.Keys)
            {
                if (input_sum[asset_id].Value > pay_total[asset_id].Value)
                {
                    outputs_new.Add(new TransactionOutput
                    {
                        AssetId = asset_id,
                        Value = input_sum[asset_id].Value - pay_total[asset_id].Value,
                        ScriptHash = change_address
                    });
                }
            }
            tx.Inputs = pay_coins.Values.SelectMany(p => p.Unspents).Select(p => p.Input).ToArray();
            tx.Outputs = outputs_new.ToArray();
            return tx;
        }

        protected abstract void OnProcessNewBlock(Block block, IEnumerable<Coin> added, IEnumerable<Coin> changed, IEnumerable<Coin> deleted);
        protected abstract void OnSaveTransaction(Transaction tx, IEnumerable<Coin> added, IEnumerable<Coin> changed);

        private void ProcessBlocks()
        {
            while (isrunning)
            {
                while (current_height <= Blockchain.Default?.Height && isrunning)
                {
                    lock (SyncRoot)
                    {
                        Block block = Blockchain.Default.GetBlock(current_height);
                        if (block != null) ProcessNewBlock(block);
                    }
                }
                for (int i = 0; i < 20 && isrunning; i++)
                {
                    Thread.Sleep(100);
                }
            }
        }

        private void ProcessNewBlock(Block block)
        {
            Coin[] changeset;
            lock (contracts)
                lock (coins)
                {
                    foreach (Transaction tx in block.Transactions)
                    {
                        for (ushort index = 0; index < tx.Outputs.Length; index++)
                        {
                            TransactionOutput output = tx.Outputs[index];
                            if (contracts.ContainsKey(output.ScriptHash))
                            {
                                TransactionInput key = new TransactionInput
                                {
                                    PrevHash = tx.Hash,
                                    PrevIndex = index
                                };
                                if (coins.Contains(key))
                                    coins[key].State = CoinState.Unspent;
                                else
                                    coins.Add(new Coin
                                    {
                                        Input = key,
                                        AssetId = output.AssetId,
                                        Value = output.Value,
                                        ScriptHash = output.ScriptHash,
                                        State = CoinState.Unspent
                                    });
                            }
                        }
                    }
                    foreach (Transaction tx in block.Transactions)
                    {
                        foreach (TransactionInput input in tx.GetAllInputs())
                        {
                            if (coins.Contains(input))
                            {
                                if (coins[input].AssetId == Blockchain.AntShare.Hash)
                                    coins[input].State = CoinState.Spent;
                                else
                                    coins.Remove(input);
                            }
                        }
                    }
                    foreach (ClaimTransaction tx in block.Transactions.OfType<ClaimTransaction>())
                    {
                        foreach (TransactionInput claim in tx.Claims)
                        {
                            if (coins.Contains(claim))
                            {
                                coins.Remove(claim);
                            }
                        }
                    }
                    current_height++;
                    changeset = coins.GetChangeSet();
                    OnProcessNewBlock(block, changeset.Where(p => ((ITrackable<TransactionInput>)p).TrackState == TrackState.Added), changeset.Where(p => ((ITrackable<TransactionInput>)p).TrackState == TrackState.Changed), changeset.Where(p => ((ITrackable<TransactionInput>)p).TrackState == TrackState.Deleted));
                    coins.Commit();
                }
            if (changeset.Length > 0)
                BalanceChanged?.Invoke(this, EventArgs.Empty);
        }

        public virtual void Rebuild()
        {
            lock (SyncRoot)
                lock (coins)
                {
                    coins.Clear();
                    coins.Commit();
                    current_height = 0;
                }
        }

        protected abstract void SaveStoredData(string name, byte[] value);

        public bool SaveTransaction(Transaction tx)
        {
            Coin[] changeset;
            lock (contracts)
                lock (coins)
                {
                    if (tx.GetAllInputs().Any(p => !coins.Contains(p) || coins[p].State != CoinState.Unspent))
                        return false;
                    foreach (TransactionInput input in tx.GetAllInputs())
                        coins[input].State = CoinState.Spending;
                    for (ushort i = 0; i < tx.Outputs.Length; i++)
                    {
                        if (contracts.ContainsKey(tx.Outputs[i].ScriptHash))
                            coins.Add(new Coin
                            {
                                Input = new TransactionInput
                                {
                                    PrevHash = tx.Hash,
                                    PrevIndex = i
                                },
                                AssetId = tx.Outputs[i].AssetId,
                                Value = tx.Outputs[i].Value,
                                ScriptHash = tx.Outputs[i].ScriptHash,
                                State = CoinState.Unconfirmed
                            });
                    }
                    changeset = coins.GetChangeSet();
                    OnSaveTransaction(tx, changeset.Where(p => ((ITrackable<TransactionInput>)p).TrackState == TrackState.Added), changeset.Where(p => ((ITrackable<TransactionInput>)p).TrackState == TrackState.Changed));
                    coins.Commit();
                }
            if (changeset.Length > 0)
                BalanceChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public bool Sign(SignatureContext context)
        {
            bool fSuccess = false;
            foreach (UInt160 scriptHash in context.ScriptHashes)
            {
                Contract contract = GetContract(scriptHash);
                if (contract == null) continue;
                Account account = GetAccountByScriptHash(scriptHash);
                if (account == null) continue;
                byte[] signature = context.Signable.Sign(account);
                fSuccess |= context.Add(contract, account.PublicKey, signature);
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
