using Neo.Core;
using Neo.IO;
using Neo.IO.Data.LevelDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Neo.Wallets
{
    public static class WalletIndexer
    {
        public static event EventHandler<BalanceEventArgs> BalanceChanged;

        private static readonly Dictionary<uint, HashSet<UInt160>> indexes = new Dictionary<uint, HashSet<UInt160>>();
        private static readonly Dictionary<UInt160, HashSet<CoinReference>> accounts_tracked = new Dictionary<UInt160, HashSet<CoinReference>>();
        private static readonly Dictionary<CoinReference, Coin> coins_tracked = new Dictionary<CoinReference, Coin>();

        private static readonly DB db;
        private static readonly object SyncRoot = new object();

        public static uint IndexHeight
        {
            get
            {
                lock (SyncRoot)
                {
                    if (indexes.Count == 0) return 0;
                    return indexes.Keys.Min();
                }
            }
        }

        static WalletIndexer()
        {
            string path = $"Index_{Settings.Default.Magic:X8}";
            Directory.CreateDirectory(path);
            db = DB.Open(path, new Options { CreateIfMissing = true });
            if (db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.SYS_Version), out Slice value) && Version.TryParse(value.ToString(), out _))
            {
                ReadOptions options = new ReadOptions { FillCache = false };
                foreach (var group in db.Find(options, SliceBuilder.Begin(DataEntryPrefix.ST_Account), (k, v) => new
                {
                    Account = new UInt160(k.ToArray().Skip(1).ToArray()),
                    Height = v.ToUInt32()
                }).GroupBy(p => p.Height, p => p.Account))
                {
                    indexes.Add(group.Key, new HashSet<UInt160>(group));
                    foreach (UInt160 account in group)
                        accounts_tracked.Add(account, new HashSet<CoinReference>());
                }
                foreach (Coin coin in db.Find(options, SliceBuilder.Begin(DataEntryPrefix.ST_Coin), (k, v) => new Coin
                {
                    Reference = k.ToArray().Skip(1).ToArray().AsSerializable<CoinReference>(),
                    Output = v.ToArray().AsSerializable<TransactionOutput>(),
                    State = (CoinState)v.ToArray()[60]
                }))
                {
                    accounts_tracked[coin.Output.ScriptHash].Add(coin.Reference);
                    coins_tracked.Add(coin.Reference, coin);
                }
            }
            else
            {
                WriteBatch batch = new WriteBatch();
                ReadOptions options = new ReadOptions { FillCache = false };
                using (Iterator it = db.NewIterator(options))
                {
                    for (it.SeekToFirst(); it.Valid(); it.Next())
                    {
                        batch.Delete(it.Key());
                    }
                }
                batch.Put(SliceBuilder.Begin(DataEntryPrefix.SYS_Version), Assembly.GetExecutingAssembly().GetName().Version.ToString());
                db.Write(WriteOptions.Default, batch);
            }
            Thread thread = new Thread(ProcessBlocks)
            {
                IsBackground = true,
                Name = $"{nameof(WalletIndexer)}.{nameof(ProcessBlocks)}"
            };
            thread.Start();
        }

        public static IEnumerable<Coin> GetCoins(IEnumerable<UInt160> accounts)
        {
            lock (SyncRoot)
            {
                foreach (UInt160 account in accounts)
                    foreach (CoinReference reference in accounts_tracked[account])
                        yield return coins_tracked[reference];
            }
        }

        public static IEnumerable<UInt256> GetTransactions(IEnumerable<UInt160> accounts)
        {
            ReadOptions options = new ReadOptions { FillCache = false };
            using (options.Snapshot = db.GetSnapshot())
            {
                IEnumerable<UInt256> results = Enumerable.Empty<UInt256>();
                foreach (UInt160 account in accounts)
                    results = results.Union(db.Find(options, SliceBuilder.Begin(DataEntryPrefix.ST_Transaction).Add(account), (k, v) => new UInt256(k.ToArray().Skip(21).ToArray())));
                foreach (UInt256 hash in results)
                    yield return hash;
            }
        }

        private static void ProcessBlock(Block block, HashSet<UInt160> accounts, WriteBatch batch)
        {
            foreach (Transaction tx in block.Transactions)
            {
                HashSet<UInt160> accounts_changed = new HashSet<UInt160>();
                for (ushort index = 0; index < tx.Outputs.Length; index++)
                {
                    TransactionOutput output = tx.Outputs[index];
                    if (accounts_tracked.ContainsKey(output.ScriptHash))
                    {
                        CoinReference reference = new CoinReference
                        {
                            PrevHash = tx.Hash,
                            PrevIndex = index
                        };
                        if (coins_tracked.TryGetValue(reference, out Coin coin))
                        {
                            coin.State |= CoinState.Confirmed;
                        }
                        else
                        {
                            accounts_tracked[output.ScriptHash].Add(reference);
                            coins_tracked.Add(reference, coin = new Coin
                            {
                                Reference = reference,
                                Output = output,
                                State = CoinState.Confirmed
                            });
                        }
                        batch.Put(SliceBuilder.Begin(DataEntryPrefix.ST_Coin).Add(reference), SliceBuilder.Begin().Add(output).Add((byte)coin.State));
                        accounts_changed.Add(output.ScriptHash);
                    }
                }
                foreach (CoinReference input in tx.Inputs)
                {
                    if (coins_tracked.TryGetValue(input, out Coin coin))
                    {
                        if (coin.Output.AssetId.Equals(Blockchain.GoverningToken.Hash))
                        {
                            coin.State |= CoinState.Spent | CoinState.Confirmed;
                            batch.Put(SliceBuilder.Begin(DataEntryPrefix.ST_Coin).Add(input), SliceBuilder.Begin().Add(coin.Output).Add((byte)coin.State));
                        }
                        else
                        {
                            accounts_tracked[coin.Output.ScriptHash].Remove(input);
                            coins_tracked.Remove(input);
                            batch.Delete(DataEntryPrefix.ST_Coin, input);
                        }
                        accounts_changed.Add(coin.Output.ScriptHash);
                    }
                }
                if (tx is ClaimTransaction ctx)
                {
                    foreach (CoinReference claim in ctx.Claims)
                    {
                        if (coins_tracked.TryGetValue(claim, out Coin coin))
                        {
                            accounts_tracked[coin.Output.ScriptHash].Remove(claim);
                            coins_tracked.Remove(claim);
                            batch.Delete(DataEntryPrefix.ST_Coin, claim);
                            accounts_changed.Add(coin.Output.ScriptHash);
                        }
                    }
                }
                if (accounts_changed.Count > 0)
                {
                    foreach (UInt160 account in accounts_changed)
                        batch.Put(SliceBuilder.Begin(DataEntryPrefix.ST_Transaction).Add(account).Add(tx.Hash), false);
                    BalanceChanged?.Invoke(null, new BalanceEventArgs
                    {
                        Transaction = tx,
                        RelatedAccounts = accounts_changed.ToArray(),
                        Height = block.Index,
                        Time = block.Timestamp
                    });
                }
            }
        }

        private static void ProcessBlocks()
        {
            bool need_sleep = false;
            for (; ; )
            {
                if (need_sleep)
                {
                    Thread.Sleep(2000);
                    need_sleep = false;
                }
                lock (SyncRoot)
                {
                    if (indexes.Count == 0)
                    {
                        need_sleep = true;
                        continue;
                    }
                    uint height = indexes.Keys.Min();
                    Block block = Blockchain.Default?.GetBlock(height);
                    if (block == null)
                    {
                        need_sleep = true;
                        continue;
                    }
                    WriteBatch batch = new WriteBatch();
                    HashSet<UInt160> accounts = indexes[height];
                    ProcessBlock(block, accounts, batch);
                    indexes.Remove(height++);
                    foreach (UInt160 account in accounts)
                        batch.Put(SliceBuilder.Begin(DataEntryPrefix.ST_Account).Add(account), height);
                    db.Write(WriteOptions.Default, batch);
                    if (indexes.TryGetValue(height, out HashSet<UInt160> accounts_next))
                        accounts_next.UnionWith(accounts);
                    else
                        indexes.Add(height, accounts);
                }
            }
        }

        public static void RebuildIndex()
        {
            lock (SyncRoot)
            {
                WriteBatch batch = new WriteBatch();
                foreach (UInt160 account in accounts_tracked.Keys)
                    batch.Put(SliceBuilder.Begin(DataEntryPrefix.ST_Account).Add(account), 0u);
                foreach (CoinReference reference in coins_tracked.Keys)
                    batch.Delete(DataEntryPrefix.ST_Coin, reference);
                ReadOptions options = new ReadOptions { FillCache = false };
                foreach (Slice key in db.Find(options, SliceBuilder.Begin(DataEntryPrefix.ST_Transaction), (k, v) => k))
                    batch.Delete(key);
                db.Write(WriteOptions.Default, batch);
                indexes.Clear();
                if (accounts_tracked.Count > 0)
                    indexes[0] = new HashSet<UInt160>(accounts_tracked.Keys);
                foreach (HashSet<CoinReference> coins in accounts_tracked.Values)
                    coins.Clear();
                coins_tracked.Clear();
            }
        }

        public static void RegisterAccounts(IEnumerable<UInt160> accounts, uint height = 0)
        {
            lock (SyncRoot)
            {
                WriteBatch batch = new WriteBatch();
                bool index_exists = indexes.TryGetValue(height, out HashSet<UInt160> index);
                if (!index_exists) index = new HashSet<UInt160>();
                foreach (UInt160 account in accounts)
                    if (!accounts_tracked.ContainsKey(account))
                    {
                        batch.Put(SliceBuilder.Begin(DataEntryPrefix.ST_Account).Add(account), height);
                        index.Add(account);
                        accounts_tracked.Add(account, new HashSet<CoinReference>());
                    }
                if (!index_exists && index.Count > 0)
                    indexes.Add(height, index);
                db.Write(WriteOptions.Default, batch);
            }
        }

        public static void UnregisterAccounts(IEnumerable<UInt160> accounts)
        {
            lock (SyncRoot)
            {
                WriteBatch batch = new WriteBatch();
                ReadOptions options = new ReadOptions { FillCache = false };
                foreach (UInt160 account in accounts)
                {
                    if (accounts_tracked.TryGetValue(account, out HashSet<CoinReference> references))
                    {
                        batch.Delete(DataEntryPrefix.ST_Account, account);
                        foreach (uint height in indexes.Keys.ToArray())
                        {
                            HashSet<UInt160> index = indexes[height];
                            if (index.Remove(account))
                            {
                                if (index.Count == 0)
                                    indexes.Remove(height);
                                break;
                            }
                        }
                        accounts_tracked.Remove(account);
                        foreach (CoinReference reference in references)
                        {
                            batch.Delete(DataEntryPrefix.ST_Coin, reference);
                            coins_tracked.Remove(reference);
                        }
                        foreach (Slice key in db.Find(options, SliceBuilder.Begin(DataEntryPrefix.ST_Transaction).Add(account), (k, v) => k))
                            batch.Delete(key);
                    }
                }
                db.Write(WriteOptions.Default, batch);
            }
        }
    }
}
