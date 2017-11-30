using Neo.Core;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Neo.Wallets
{
    internal static class WalletIndexer
    {
        private static readonly Dictionary<uint, HashSet<UInt160>> indexes = new Dictionary<uint, HashSet<UInt160>>();
        private static readonly Dictionary<UInt160, HashSet<CoinReference>> accounts_tracked = new Dictionary<UInt160, HashSet<CoinReference>>();
        private static readonly Dictionary<CoinReference, Coin> coins_tracked = new Dictionary<CoinReference, Coin>();

        private static readonly object SyncRoot = new object();

        static WalletIndexer()
        {
            Thread thread = new Thread(ProcessBlocks)
            {
                IsBackground = true,
                Name = "WalletIndexer.ProcessBlocks"
            };
            thread.Start();
        }

        private static void ProcessBlock(Block block, HashSet<UInt160> accounts)
        {
            foreach (Transaction tx in block.Transactions)
            {
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
                            coins_tracked.Add(reference, new Coin
                            {
                                Reference = reference,
                                Output = output,
                                State = CoinState.Confirmed
                            });
                        }
                    }
                }
            }
            foreach (Transaction tx in block.Transactions)
            {
                foreach (CoinReference input in tx.Inputs)
                {
                    if (coins_tracked.TryGetValue(input, out Coin coin))
                    {
                        if (coin.Output.AssetId.Equals(Blockchain.GoverningToken.Hash))
                        {
                            coin.State |= CoinState.Spent | CoinState.Confirmed;
                        }
                        else
                        {
                            accounts_tracked[coin.Output.ScriptHash].Remove(input);
                            coins_tracked.Remove(input);
                        }
                    }
                }
            }
            foreach (ClaimTransaction tx in block.Transactions.OfType<ClaimTransaction>())
            {
                foreach (CoinReference claim in tx.Claims)
                {
                    if (coins_tracked.TryGetValue(claim, out Coin coin))
                    {
                        accounts_tracked[coin.Output.ScriptHash].Remove(claim);
                        coins_tracked.Remove(claim);
                    }
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
                    HashSet<UInt160> accounts = indexes[height];
                    ProcessBlock(block, accounts);
                    indexes.Remove(height);
                    if (indexes.TryGetValue(++height, out HashSet<UInt160> accounts_next))
                        accounts_next.UnionWith(accounts);
                    else
                        indexes.Add(height, accounts);
                }
            }
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

        public static void RegisterAccounts(IEnumerable<UInt160> accounts, uint height = 0)
        {
            lock (SyncRoot)
            {
                bool index_exists = indexes.TryGetValue(height, out HashSet<UInt160> index);
                if (!index_exists) index = new HashSet<UInt160>();
                foreach (UInt160 account in accounts)
                    if (!accounts_tracked.ContainsKey(account))
                    {
                        index.Add(account);
                        accounts_tracked.Add(account, new HashSet<CoinReference>());
                    }
                if (!index_exists && index.Count > 0)
                    indexes.Add(height, index);
            }
        }

        public static void UnregisterAccounts(IEnumerable<UInt160> accounts)
        {
            lock (SyncRoot)
            {
                foreach (UInt160 account in accounts)
                {
                    if (accounts_tracked.TryGetValue(account, out HashSet<CoinReference> references))
                    {
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
                            coins_tracked.Remove(reference);
                    }
                }
            }
        }
    }
}
