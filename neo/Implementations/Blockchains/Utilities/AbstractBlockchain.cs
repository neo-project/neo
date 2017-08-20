using Neo.Core;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Caching;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Neo.Implementations.Blockchains.Utilities
{
    public class AbstractBlockchain : Blockchain
    {
        private AbstractDB db;
        private Thread thread_persistence;
        private List<UInt256> header_index = new List<UInt256>();
        private Dictionary<UInt256, Header> header_cache = new Dictionary<UInt256, Header>();
        private Dictionary<UInt256, Block> block_cache = new Dictionary<UInt256, Block>();
        private uint current_block_height = 0;
        private uint stored_header_count = 0;
        private AutoResetEvent new_block_event = new AutoResetEvent(false);
        private bool disposed = false;

        public override UInt256 CurrentBlockHash => header_index[(int)current_block_height];
        public override UInt256 CurrentHeaderHash => header_index[header_index.Count - 1];
        public override uint HeaderHeight => (uint)header_index.Count - 1;
        public override uint Height => current_block_height;
        public override bool VerifyBlocks { get; set; } = true;

        private AbstractEntityFactory f;

        public AbstractBlockchain(string path, AbstractEntityFactory f)
        {
            this.f = f;

            header_index.Add(GenesisBlock.Hash);
            Version version;
            Slice value;
            AbstractOptions dbOptions = f.newOptions();
            dbOptions.CreateIfMissing = true;
            db = f.Open(path, dbOptions);

            if (db.TryGet(f.getDefaultReadOptions(), SliceBuilder.Begin(DataEntryPrefix.SYS_Version), out value) && Version.TryParse(value.ToString(), out version) && version >= Version.Parse("1.5"))
            {
                AbstractReadOptions options = f.newReadOptions();
                options.FillCache = false;
                value = db.Get(options, SliceBuilder.Begin(DataEntryPrefix.SYS_CurrentBlock));
                UInt256 current_header_hash = new UInt256(value.ToArray().Take(32).ToArray());
                this.current_block_height = value.ToArray().ToUInt32(32);
                uint current_header_height = current_block_height;
                if (db.TryGet(options, SliceBuilder.Begin(DataEntryPrefix.SYS_CurrentHeader), out value))
                {
                    current_header_hash = new UInt256(value.ToArray().Take(32).ToArray());
                    current_header_height = value.ToArray().ToUInt32(32);
                }
                foreach (UInt256 hash in db.Find(options, SliceBuilder.Begin(DataEntryPrefix.IX_HeaderHashList), (k, v) =>
                {
                    using (MemoryStream ms = new MemoryStream(v.ToArray(), false))
                    using (BinaryReader r = new BinaryReader(ms))
                    {
                        return new
                        {
                            Index = k.ToArray().ToUInt32(1),
                            Hashes = r.ReadSerializableArray<UInt256>()
                        };
                    }
                }).OrderBy(p => p.Index).SelectMany(p => p.Hashes).ToArray())
                {
                    if (!hash.Equals(GenesisBlock.Hash))
                    {
                        header_index.Add(hash);
                    }
                    stored_header_count++;
                }
                if (stored_header_count == 0)
                {
                    Header[] headers = db.Find(options, SliceBuilder.Begin(DataEntryPrefix.DATA_Block), (k, v) => Header.FromTrimmedData(v.ToArray(), sizeof(long))).OrderBy(p => p.Index).ToArray();
                    for (int i = 1; i < headers.Length; i++)
                    {
                        header_index.Add(headers[i].Hash);
                    }
                }
                else if (current_header_height >= stored_header_count)
                {
                    for (UInt256 hash = current_header_hash; hash != header_index[(int)stored_header_count - 1];)
                    {
                        Header header = Header.FromTrimmedData(db.Get(options, SliceBuilder.Begin(DataEntryPrefix.DATA_Block).Add(hash)).ToArray(), sizeof(long));
                        header_index.Insert((int)stored_header_count, hash);
                        hash = header.PrevHash;
                    }
                }
            }
            else
            {
                AbstractWriteBatch batch = f.newWriteBatch();
                AbstractReadOptions options = f.newReadOptions();
                options.FillCache = false;
                using (AbstractIterator it = db.NewIterator(options))
                {
                    for (it.SeekToFirst(); it.Valid(); it.Next())
                    {
                        batch.Delete(it.Key());
                    }
                }
                db.Write(f.getDefaultWriteOptions(), batch);
                Persist(GenesisBlock);
                db.Put(f.getDefaultWriteOptions(), SliceBuilder.Begin(DataEntryPrefix.SYS_Version), GetType().GetTypeInfo().Assembly.GetName().Version.ToString());
            }
            thread_persistence = new Thread(PersistBlocks);
            thread_persistence.Name = "LevelDBBlockchain.PersistBlocks";
            thread_persistence.Start();
        }

        public override bool AddBlock(Block block)
        {
            Console.WriteLine($"AddBlock[0] block.Hash{block.Hash}");
            lock (block_cache)
            {
                if (!block_cache.ContainsKey(block.Hash))
                {
                    block_cache.Add(block.Hash, block);
                }
            }
            lock (header_index)
            {
                if (block.Index - 1 >= header_index.Count) return false;
                if (block.Index == header_index.Count)
                {
                    if (VerifyBlocks && !block.Verify()) return false;
                    AbstractWriteBatch batch = f.newWriteBatch();
                    OnAddHeader(block.Header, batch);
                    db.Write(f.getDefaultWriteOptions(), batch);
                }
                if (block.Index < header_index.Count)
                    new_block_event.Set();
            }
            return true;
        }

        protected internal override void AddHeaders(IEnumerable<Header> headers)
        {
            lock (header_index)
            {
                lock (header_cache)
                {
                    AbstractWriteBatch batch = f.newWriteBatch();
                    foreach (Header header in headers)
                    {
                        if (header.Index - 1 >= header_index.Count) break;
                        if (header.Index < header_index.Count) continue;
                        if (VerifyBlocks && !header.Verify()) break;
                        OnAddHeader(header, batch);
                        header_cache.Add(header.Hash, header);
                    }
                    db.Write(f.getDefaultWriteOptions(), batch);
                    header_cache.Clear();
                }
            }
        }

        public override bool ContainsBlock(UInt256 hash)
        {
            return GetHeader(hash)?.Index <= current_block_height;
        }

        public override bool ContainsTransaction(UInt256 hash)
        {
            Slice value;
            return db.TryGet(f.getDefaultReadOptions(), SliceBuilder.Begin(DataEntryPrefix.DATA_Transaction).Add(hash), out value);
        }

        public override bool ContainsUnspent(UInt256 hash, ushort index)
        {
            UnspentCoinState state = db.TryGet<UnspentCoinState>(f.getDefaultReadOptions(), DataEntryPrefix.ST_Coin, hash);
            if (state == null) return false;
            if (index >= state.Items.Length) return false;
            return !state.Items[index].HasFlag(CoinState.Spent);
        }

        public override void Dispose()
        {
            disposed = true;
            new_block_event.Set();
            if (!thread_persistence.ThreadState.HasFlag(ThreadState.Unstarted))
                thread_persistence.Join();
            new_block_event.Dispose();
            if (db != null)
            {
                db.Dispose();
                db = null;
            }
        }

        public override AccountState GetAccountState(UInt160 script_hash)
        {
            return db.TryGet<AccountState>(f.getDefaultReadOptions(), DataEntryPrefix.ST_Account, script_hash);
        }

        public override AssetState GetAssetState(UInt256 asset_id)
        {
            return db.TryGet<AssetState>(f.getDefaultReadOptions(), DataEntryPrefix.ST_Asset, asset_id);
        }

        public override Block GetBlock(UInt256 hash)
        {
            return GetBlockInternal(f.getDefaultReadOptions(), hash);
        }

        public override UInt256 GetBlockHash(uint height)
        {
            if (current_block_height < height) return null;
            lock (header_index)
            {
                if (header_index.Count <= height) return null;
                return header_index[(int)height];
            }
        }

        private Block GetBlockInternal(AbstractReadOptions options, UInt256 hash)
        {
            Slice value;
            if (!db.TryGet(options, SliceBuilder.Begin(DataEntryPrefix.DATA_Block).Add(hash), out value))
                return null;
            int height;
            Block block = Block.FromTrimmedData(value.ToArray(), sizeof(long), p => GetTransaction(options, p, out height));
            if (block.Transactions.Length == 0) return null;
            return block;
        }

        public override ContractState GetContract(UInt160 hash)
        {
            return db.TryGet<ContractState>(f.getDefaultReadOptions(), DataEntryPrefix.ST_Contract, hash);
        }

        public override IEnumerable<ValidatorState> GetEnrollments()
        {
            return db.Find<ValidatorState>(f.getDefaultReadOptions(), DataEntryPrefix.ST_Validator).Union(StandbyValidators.Select(p => new ValidatorState
            {
                PublicKey = p
            }));
        }

        public override Header GetHeader(uint height)
        {
            UInt256 hash;
            lock (header_index)
            {
                if (header_index.Count <= height) return null;
                hash = header_index[(int)height];
            }
            return GetHeader(hash);
        }

        public override Header GetHeader(UInt256 hash)
        {
            lock (header_cache)
            {
                if (header_cache.ContainsKey(hash))
                    return header_cache[hash];
            }
            Slice value;
            if (!db.TryGet(f.getDefaultReadOptions(), SliceBuilder.Begin(DataEntryPrefix.DATA_Block).Add(hash), out value))
                return null;
            return Header.FromTrimmedData(value.ToArray(), sizeof(long));
        }

        public override Block GetNextBlock(UInt256 hash)
        {
            return GetBlockInternal(f.getDefaultReadOptions(), GetNextBlockHash(hash));
        }

        public override UInt256 GetNextBlockHash(UInt256 hash)
        {
            Header header = GetHeader(hash);
            if (header == null) return null;
            lock (header_index)
            {
                if (header.Index + 1 >= header_index.Count)
                    return null;
                return header_index[(int)header.Index + 1];
            }
        }

        public override StorageItem GetStorageItem(StorageKey key)
        {
            return db.TryGet<StorageItem>(f.getDefaultReadOptions(), DataEntryPrefix.ST_Storage, key);
        }

        public override long GetSysFeeAmount(UInt256 hash)
        {
            Slice value;
            if (!db.TryGet(f.getDefaultReadOptions(), SliceBuilder.Begin(DataEntryPrefix.DATA_Block).Add(hash), out value))
                return 0;
            return value.ToArray().ToInt64(0);
        }

        public override DataCache<TKey, TValue> GetTable<TKey, TValue>()
        {
            Type t = typeof(TValue);
            if (t == typeof(AccountState)) return new DbCache<TKey, TValue>(db, DataEntryPrefix.ST_Account, f);
            if (t == typeof(UnspentCoinState)) return new DbCache<TKey, TValue>(db, DataEntryPrefix.ST_Coin, f);
            if (t == typeof(SpentCoinState)) return new DbCache<TKey, TValue>(db, DataEntryPrefix.ST_SpentCoin, f);
            if (t == typeof(ValidatorState)) return new DbCache<TKey, TValue>(db, DataEntryPrefix.ST_Validator, f);
            if (t == typeof(AssetState)) return new DbCache<TKey, TValue>(db, DataEntryPrefix.ST_Asset, f);
            if (t == typeof(ContractState)) return new DbCache<TKey, TValue>(db, DataEntryPrefix.ST_Contract, f);
            if (t == typeof(StorageItem)) return new DbCache<TKey, TValue>(db, DataEntryPrefix.ST_Storage, f);
            throw new NotSupportedException();
        }

        public override Transaction GetTransaction(UInt256 hash, out int height)
        {
            return GetTransaction(f.getDefaultReadOptions(), hash, out height);
        }

        private Transaction GetTransaction(AbstractReadOptions options, UInt256 hash, out int height)
        {
            Slice value;
            if (db.TryGet(options, SliceBuilder.Begin(DataEntryPrefix.DATA_Transaction).Add(hash), out value))
            {
                byte[] data = value.ToArray();
                height = data.ToInt32(0);
                return Transaction.DeserializeFrom(data, sizeof(uint));
            }
            else
            {
                height = -1;
                return null;
            }
        }

        public override Dictionary<ushort, SpentCoin> GetUnclaimed(UInt256 hash)
        {
            int height;
            Transaction tx = GetTransaction(f.getDefaultReadOptions(), hash, out height);
            if (tx == null) return null;
            SpentCoinState state = db.TryGet<SpentCoinState>(f.getDefaultReadOptions(), DataEntryPrefix.ST_SpentCoin, hash);
            if (state != null)
            {
                return state.Items.ToDictionary(p => p.Key, p => new SpentCoin
                {
                    Output = tx.Outputs[p.Key],
                    StartHeight = (uint)height,
                    EndHeight = p.Value
                });
            }
            else
            {
                return new Dictionary<ushort, SpentCoin>();
            }
        }

        public override TransactionOutput GetUnspent(UInt256 hash, ushort index)
        {
            AbstractReadOptions options = f.newReadOptions();
            using (options.Snapshot = db.GetSnapshot())
            {
                UnspentCoinState state = db.TryGet<UnspentCoinState>(options, DataEntryPrefix.ST_Coin, hash);
                if (state == null) return null;
                if (index >= state.Items.Length) return null;
                if (state.Items[index].HasFlag(CoinState.Spent)) return null;
                int height;
                return GetTransaction(options, hash, out height).Outputs[index];
            }
        }

        public override IEnumerable<VoteState> GetVotes(IEnumerable<Transaction> others)
        {
            AbstractReadOptions options = f.newReadOptions();
            using (options.Snapshot = db.GetSnapshot())
            {
                var inputs = others.SelectMany(p => p.Inputs).GroupBy(p => p.PrevHash, (k, g) =>
                {
                    int height;
                    Transaction tx = GetTransaction(options, k, out height);
                    return g.Select(p => tx.Outputs[p.PrevIndex]);
                }).SelectMany(p => p).Where(p => p.AssetId.Equals(SystemShare.Hash)).Select(p => new
                {
                    p.ScriptHash,
                    Value = -p.Value
                });
                var outputs = others.SelectMany(p => p.Outputs).Where(p => p.AssetId.Equals(SystemShare.Hash)).Select(p => new
                {
                    p.ScriptHash,
                    p.Value
                });
                var changes = inputs.Concat(outputs).GroupBy(p => p.ScriptHash).ToDictionary(p => p.Key, p => p.Sum(i => i.Value));
                var accounts = db.Find<AccountState>(options, DataEntryPrefix.ST_Account).Where(p => p.Votes.Length > 0).ToArray();
                if (accounts.Length > 0)
                    foreach (AccountState account in accounts)
                    {
                        Fixed8 balance = account.Balances.ContainsKey(SystemShare.Hash) ? account.Balances[SystemShare.Hash] : Fixed8.Zero;
                        if (changes.ContainsKey(account.ScriptHash))
                            balance += changes[account.ScriptHash];
                        if (balance <= Fixed8.Zero) continue;
                        yield return new VoteState
                        {
                            PublicKeys = account.Votes,
                            Count = balance
                        };
                    }
                else
                    yield return new VoteState
                    {
                        PublicKeys = StandbyValidators,
                        Count = SystemShare.Amount
                    };
            }
        }

        public override bool IsDoubleSpend(Transaction tx)
        {
            if (tx.Print)
            {
                Console.WriteLine("IsDoubleSpend 0");
            }
            if (tx.Inputs.Length == 0) return false;
            if (tx.Print)
            {
                Console.WriteLine("IsDoubleSpend 1");
            }
            AbstractReadOptions options = f.newReadOptions();
            using (options.Snapshot = db.GetSnapshot())
            {
                foreach (var group in tx.Inputs.GroupBy(p => p.PrevHash))
                {
                    UnspentCoinState state = db.TryGet<UnspentCoinState>(options, DataEntryPrefix.ST_Coin, group.Key);
                    if (tx.Print)
                    {
                        Console.WriteLine($"IsDoubleSpend 2 {group.Key}");
                    }
                    if (state == null) return true;
                    if (tx.Print)
                    {
                        Console.WriteLine($"IsDoubleSpend 3 {group.Key}");
                    }
                    if (group.Any(p => p.PrevIndex >= state.Items.Length || state.Items[p.PrevIndex].HasFlag(CoinState.Spent)))
                        return true;
                    if (tx.Print)
                    {
                        Console.WriteLine($"IsDoubleSpend 4 {group.Key}");
                    }
                }
            }
            if (tx.Print)
            {
                Console.WriteLine($"IsDoubleSpend 5");
            }
            return false;
        }

        private void OnAddHeader(Header header, AbstractWriteBatch batch)
        {
            header_index.Add(header.Hash);
            while ((int)header.Index - 2000 >= stored_header_count)
            {
                using (MemoryStream ms = new MemoryStream())
                using (BinaryWriter w = new BinaryWriter(ms))
                {
                    w.Write(header_index.Skip((int)stored_header_count).Take(2000).ToArray());
                    w.Flush();
                    batch.Put(SliceBuilder.Begin(DataEntryPrefix.IX_HeaderHashList).Add(stored_header_count), ms.ToArray());
                }
                stored_header_count += 2000;
            }
            batch.Put(SliceBuilder.Begin(DataEntryPrefix.DATA_Block).Add(header.Hash), SliceBuilder.Begin().Add(0L).Add(header.ToArray()));
            batch.Put(SliceBuilder.Begin(DataEntryPrefix.SYS_CurrentHeader), SliceBuilder.Begin().Add(header.Hash).Add(header.Index));
        }

        private void Persist(Block block)
        {
            AbstractWriteBatch batch = f.newWriteBatch();
            DbCache<UInt160, AccountState> accounts = new DbCache<UInt160, AccountState>(db, DataEntryPrefix.ST_Account, f);
            DbCache<UInt256, UnspentCoinState> unspentcoins = new DbCache<UInt256, UnspentCoinState>(db, DataEntryPrefix.ST_Coin, f);
            DbCache<UInt256, SpentCoinState> spentcoins = new DbCache<UInt256, SpentCoinState>(db, DataEntryPrefix.ST_SpentCoin, f);
            DbCache<ECPoint, ValidatorState> validators = new DbCache<ECPoint, ValidatorState>(db, DataEntryPrefix.ST_Validator, f);
            DbCache<UInt256, AssetState> assets = new DbCache<UInt256, AssetState>(db, DataEntryPrefix.ST_Asset, f);
            DbCache<UInt160, ContractState> contracts = new DbCache<UInt160, ContractState>(db, DataEntryPrefix.ST_Contract, f);
            DbCache<StorageKey, StorageItem> storages = new DbCache<StorageKey, StorageItem>(db, DataEntryPrefix.ST_Storage, f);
            long amount_sysfee = GetSysFeeAmount(block.PrevHash) + (long)block.Transactions.Sum(p => p.SystemFee);
            batch.Put(SliceBuilder.Begin(DataEntryPrefix.DATA_Block).Add(block.Hash), SliceBuilder.Begin().Add(amount_sysfee).Add(block.Trim()));
            foreach (Transaction tx in block.Transactions)
            {
                batch.Put(SliceBuilder.Begin(DataEntryPrefix.DATA_Transaction).Add(tx.Hash), SliceBuilder.Begin().Add(block.Index).Add(tx.ToArray()));
                unspentcoins.Add(tx.Hash, new UnspentCoinState
                {
                    Items = Enumerable.Repeat(CoinState.Confirmed, tx.Outputs.Length).ToArray()
                });
                foreach (TransactionOutput output in tx.Outputs)
                {
                    AccountState account = accounts.GetAndChange(output.ScriptHash, () => new AccountState(output.ScriptHash));
                    if (account.Balances.ContainsKey(output.AssetId))
                        account.Balances[output.AssetId] += output.Value;
                    else
                        account.Balances[output.AssetId] = output.Value;
                }
                foreach (var group in tx.Inputs.GroupBy(p => p.PrevHash))
                {
                    int height;
                    Transaction tx_prev = GetTransaction(f.getDefaultReadOptions(), group.Key, out height);
                    foreach (CoinReference input in group)
                    {
                        unspentcoins.GetAndChange(input.PrevHash).Items[input.PrevIndex] |= CoinState.Spent;
                        if (tx_prev.Outputs[input.PrevIndex].AssetId.Equals(SystemShare.Hash))
                        {
                            spentcoins.GetAndChange(input.PrevHash, () => new SpentCoinState
                            {
                                TransactionHash = input.PrevHash,
                                TransactionHeight = (uint)height,
                                Items = new Dictionary<ushort, uint>()
                            }).Items.Add(input.PrevIndex, block.Index);
                        }
                        accounts.GetAndChange(tx_prev.Outputs[input.PrevIndex].ScriptHash).Balances[tx_prev.Outputs[input.PrevIndex].AssetId] -= tx_prev.Outputs[input.PrevIndex].Value;
                    }
                }
                switch (tx.Type)
                {
                    case TransactionType.RegisterTransaction:
                        {
#pragma warning disable CS0612
                            RegisterTransaction rtx = (RegisterTransaction)tx;
                            assets.Add(tx.Hash, new AssetState
                            {
                                AssetId = rtx.Hash,
                                AssetType = rtx.AssetType,
                                Name = rtx.Name,
                                Amount = rtx.Amount,
                                Available = Fixed8.Zero,
                                Precision = rtx.Precision,
                                Fee = Fixed8.Zero,
                                FeeAddress = new UInt160(),
                                Owner = rtx.Owner,
                                Admin = rtx.Admin,
                                Issuer = rtx.Admin,
                                Expiration = block.Index + 2 * 2000000,
                                IsFrozen = false
                            });
#pragma warning restore CS0612
                        }
                        break;
                    case TransactionType.IssueTransaction:
                        foreach (TransactionResult result in tx.GetTransactionResults().Where(p => p.Amount < Fixed8.Zero))
                            assets.GetAndChange(result.AssetId).Available -= result.Amount;
                        break;
                    case TransactionType.ClaimTransaction:
                        foreach (CoinReference input in ((ClaimTransaction)tx).Claims)
                        {
                            if (spentcoins.TryGet(input.PrevHash)?.Items.Remove(input.PrevIndex) == true)
                                spentcoins.GetAndChange(input.PrevHash);
                        }
                        break;
                    case TransactionType.EnrollmentTransaction:
                        {
#pragma warning disable CS0612
                            EnrollmentTransaction enroll_tx = (EnrollmentTransaction)tx;
                            validators.GetOrAdd(enroll_tx.PublicKey, () => new ValidatorState
                            {
                                PublicKey = enroll_tx.PublicKey
                            });
#pragma warning restore CS0612
                        }
                        break;
                    case TransactionType.PublishTransaction:
                        {
#pragma warning disable CS0612
                            PublishTransaction publish_tx = (PublishTransaction)tx;
                            contracts.GetOrAdd(publish_tx.Code.ScriptHash, () => new ContractState
                            {
                                Code = publish_tx.Code,
                                HasStorage = publish_tx.NeedStorage,
                                Name = publish_tx.Name,
                                CodeVersion = publish_tx.CodeVersion,
                                Author = publish_tx.Author,
                                Email = publish_tx.Email,
                                Description = publish_tx.Description
                            });
#pragma warning restore CS0612
                        }
                        break;
                    case TransactionType.InvocationTransaction:
                        {
                            InvocationTransaction itx = (InvocationTransaction)tx;
                            CachedScriptTable script_table = new CachedScriptTable(contracts);
                            StateMachine service = new StateMachine(accounts, validators, assets, contracts, storages);
                            ApplicationEngine engine = new ApplicationEngine(itx, script_table, service, itx.Gas);
                            engine.LoadScript(itx.Script, false);
                            if (engine.Execute()) service.Commit();
                        }
                        break;
                }
            }
            accounts.DeleteWhere((k, v) => !v.IsFrozen && v.Votes.Length == 0 && v.Balances.All(p => p.Value <= Fixed8.Zero));
            accounts.Commit(batch);
            unspentcoins.DeleteWhere((k, v) => v.Items.All(p => p.HasFlag(CoinState.Spent)));
            unspentcoins.Commit(batch);
            spentcoins.DeleteWhere((k, v) => v.Items.Count == 0);
            spentcoins.Commit(batch);
            validators.Commit(batch);
            assets.Commit(batch);
            contracts.Commit(batch);
            storages.Commit(batch);
            batch.Put(SliceBuilder.Begin(DataEntryPrefix.SYS_CurrentBlock), SliceBuilder.Begin().Add(block.Hash).Add(block.Index));


            Console.WriteLine($"Persist[0]  batch.Count{batch}");
            db.Write(f.getDefaultWriteOptions(), batch);
            Console.WriteLine($"Persist[1]  batch.Count{batch.Count()}");
            current_block_height = block.Index;
        }

        private void PersistBlocks()
        {
            while (!disposed)
            {
                new_block_event.WaitOne();
                while (!disposed)
                {
                    UInt256 hash;
                    lock (header_index)
                    {
                        if (header_index.Count <= current_block_height + 1) break;
                        hash = header_index[(int)current_block_height + 1];
                    }


                    Console.WriteLine($"PersistBlocks[0] hash{hash}");

                    Block block;
                    lock (block_cache)
                    {
                        if (!block_cache.ContainsKey(hash)) break;
                        block = block_cache[hash];
                    }
                    Console.WriteLine($"PersistBlocks[1] hash{hash}");

                    Persist(block);
                    OnPersistCompleted(block);

                    Console.WriteLine($"PersistBlocks[2] hash{hash}");

                    lock (block_cache)
                    {
                        block_cache.Remove(hash);
                    }

                    Console.WriteLine($"PersistBlocks[3] block_cache.Count{block_cache.Count}");

                }
            }
        }
    }
}
