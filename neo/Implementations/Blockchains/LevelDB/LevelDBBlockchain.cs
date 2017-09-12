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

namespace Neo.Implementations.Blockchains.LevelDB
{
    public class LevelDBBlockchain : Blockchain
    {
        private DB db;
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
        public bool VerifyBlocks { get; set; } = true;

        public LevelDBBlockchain(string path)
        {
            header_index.Add(GenesisBlock.Hash);
            Version version;
            Slice value;
            db = DB.Open(path, new Options { CreateIfMissing = true });
            if (db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.SYS_Version), out value) && Version.TryParse(value.ToString(), out version) && version >= Version.Parse("1.5"))
            {
                ReadOptions options = new ReadOptions { FillCache = false };
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
                WriteBatch batch = new WriteBatch();
                ReadOptions options = new ReadOptions { FillCache = false };
                using (Iterator it = db.NewIterator(options))
                {
                    for (it.SeekToFirst(); it.Valid(); it.Next())
                    {
                        batch.Delete(it.Key());
                    }
                }
                db.Write(WriteOptions.Default, batch);
                Persist(GenesisBlock);
                db.Put(WriteOptions.Default, SliceBuilder.Begin(DataEntryPrefix.SYS_Version), GetType().GetTypeInfo().Assembly.GetName().Version.ToString());
            }
            thread_persistence = new Thread(PersistBlocks);
            thread_persistence.Name = "LevelDBBlockchain.PersistBlocks";
            thread_persistence.Start();
        }

        public override bool AddBlock(Block block)
        {
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
                    WriteBatch batch = new WriteBatch();
                    OnAddHeader(block.Header, batch);
                    db.Write(WriteOptions.Default, batch);
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
                    WriteBatch batch = new WriteBatch();
                    foreach (Header header in headers)
                    {
                        if (header.Index - 1 >= header_index.Count) break;
                        if (header.Index < header_index.Count) continue;
                        if (VerifyBlocks && !header.Verify()) break;
                        OnAddHeader(header, batch);
                        header_cache.Add(header.Hash, header);
                    }
                    db.Write(WriteOptions.Default, batch);
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
            return db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.DATA_Transaction).Add(hash), out value);
        }

        public override bool ContainsUnspent(UInt256 hash, ushort index)
        {
            UnspentCoinState state = db.TryGet<UnspentCoinState>(ReadOptions.Default, DataEntryPrefix.ST_Coin, hash);
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
            return db.TryGet<AccountState>(ReadOptions.Default, DataEntryPrefix.ST_Account, script_hash);
        }

        public override AssetState GetAssetState(UInt256 asset_id)
        {
            return db.TryGet<AssetState>(ReadOptions.Default, DataEntryPrefix.ST_Asset, asset_id);
        }

        public override Block GetBlock(UInt256 hash)
        {
            return GetBlockInternal(ReadOptions.Default, hash);
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

        private Block GetBlockInternal(ReadOptions options, UInt256 hash)
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
            return db.TryGet<ContractState>(ReadOptions.Default, DataEntryPrefix.ST_Contract, hash);
        }

        public override IEnumerable<ValidatorState> GetEnrollments()
        {
            return db.Find<ValidatorState>(ReadOptions.Default, DataEntryPrefix.ST_Validator).Union(StandbyValidators.Select(p => new ValidatorState
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
                if (header_cache.TryGetValue(hash, out Header header))
                    return header;
            }
            Slice value;
            if (!db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.DATA_Block).Add(hash), out value))
                return null;
            return Header.FromTrimmedData(value.ToArray(), sizeof(long));
        }

        public override Block GetNextBlock(UInt256 hash)
        {
            return GetBlockInternal(ReadOptions.Default, GetNextBlockHash(hash));
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
            return db.TryGet<StorageItem>(ReadOptions.Default, DataEntryPrefix.ST_Storage, key);
        }

        public override long GetSysFeeAmount(UInt256 hash)
        {
            Slice value;
            if (!db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.DATA_Block).Add(hash), out value))
                return 0;
            return value.ToArray().ToInt64(0);
        }

        public override DataCache<TKey, TValue> CreateCache<TKey, TValue>()
        {
            Type t = typeof(TValue);
            if (t == typeof(AccountState)) return new DbCache<TKey, TValue>(db, DataEntryPrefix.ST_Account);
            if (t == typeof(UnspentCoinState)) return new DbCache<TKey, TValue>(db, DataEntryPrefix.ST_Coin);
            if (t == typeof(SpentCoinState)) return new DbCache<TKey, TValue>(db, DataEntryPrefix.ST_SpentCoin);
            if (t == typeof(ValidatorState)) return new DbCache<TKey, TValue>(db, DataEntryPrefix.ST_Validator);
            if (t == typeof(AssetState)) return new DbCache<TKey, TValue>(db, DataEntryPrefix.ST_Asset);
            if (t == typeof(ContractState)) return new DbCache<TKey, TValue>(db, DataEntryPrefix.ST_Contract);
            if (t == typeof(StorageItem)) return new DbCache<TKey, TValue>(db, DataEntryPrefix.ST_Storage);
            throw new NotSupportedException();
        }

        public override Transaction GetTransaction(UInt256 hash, out int height)
        {
            return GetTransaction(ReadOptions.Default, hash, out height);
        }

        private Transaction GetTransaction(ReadOptions options, UInt256 hash, out int height)
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
            Transaction tx = GetTransaction(ReadOptions.Default, hash, out height);
            if (tx == null) return null;
            SpentCoinState state = db.TryGet<SpentCoinState>(ReadOptions.Default, DataEntryPrefix.ST_SpentCoin, hash);
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
            ReadOptions options = new ReadOptions();
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
            ReadOptions options = new ReadOptions();
            using (options.Snapshot = db.GetSnapshot())
            {
                IList<Transaction> transactions = others as IList<Transaction> ?? others.ToList();
                var inputs = transactions.SelectMany(p => p.Inputs).GroupBy(p => p.PrevHash, (k, g) =>
                {
                    int height;
                    Transaction tx = GetTransaction(options, k, out height);
                    return g.Select(p => tx.Outputs[p.PrevIndex]);
                }).SelectMany(p => p).Where(p => p.AssetId.Equals(GoverningToken.Hash)).Select(p => new
                {
                    p.ScriptHash,
                    Value = -p.Value
                });
                var outputs = transactions.SelectMany(p => p.Outputs).Where(p => p.AssetId.Equals(GoverningToken.Hash)).Select(p => new
                {
                    p.ScriptHash,
                    p.Value
                });
                var changes = inputs.Concat(outputs).GroupBy(p => p.ScriptHash).ToDictionary(p => p.Key, p => p.Sum(i => i.Value));
                var accounts = db.Find<AccountState>(options, DataEntryPrefix.ST_Account).Where(p => p.Votes.Length > 0).ToArray();
                if (accounts.Length > 0)
                    foreach (AccountState account in accounts)
                    {
                        Fixed8 balance = account.Balances.TryGetValue(GoverningToken.Hash, out Fixed8 value) ? value : Fixed8.Zero;
                        if (changes.TryGetValue(account.ScriptHash, out Fixed8 change))
                            balance += change;
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
                        Count = GoverningToken.Amount
                    };
            }
        }

        public override bool IsDoubleSpend(Transaction tx)
        {
            if (tx.Inputs.Length == 0) return false;
            ReadOptions options = new ReadOptions();
            using (options.Snapshot = db.GetSnapshot())
            {
                foreach (var group in tx.Inputs.GroupBy(p => p.PrevHash))
                {
                    UnspentCoinState state = db.TryGet<UnspentCoinState>(options, DataEntryPrefix.ST_Coin, group.Key);
                    if (state == null) return true;
                    if (group.Any(p => p.PrevIndex >= state.Items.Length || state.Items[p.PrevIndex].HasFlag(CoinState.Spent)))
                        return true;
                }
            }
            return false;
        }

        private void OnAddHeader(Header header, WriteBatch batch)
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
            WriteBatch batch = new WriteBatch();
            DbCache<UInt160, AccountState> accounts = new DbCache<UInt160, AccountState>(db, DataEntryPrefix.ST_Account);
            DbCache<UInt256, UnspentCoinState> unspentcoins = new DbCache<UInt256, UnspentCoinState>(db, DataEntryPrefix.ST_Coin);
            DbCache<UInt256, SpentCoinState> spentcoins = new DbCache<UInt256, SpentCoinState>(db, DataEntryPrefix.ST_SpentCoin);
            DbCache<ECPoint, ValidatorState> validators = new DbCache<ECPoint, ValidatorState>(db, DataEntryPrefix.ST_Validator);
            DbCache<UInt256, AssetState> assets = new DbCache<UInt256, AssetState>(db, DataEntryPrefix.ST_Asset);
            DbCache<UInt160, ContractState> contracts = new DbCache<UInt160, ContractState>(db, DataEntryPrefix.ST_Contract);
            DbCache<StorageKey, StorageItem> storages = new DbCache<StorageKey, StorageItem>(db, DataEntryPrefix.ST_Storage);
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
                    Transaction tx_prev = GetTransaction(ReadOptions.Default, group.Key, out height);
                    foreach (CoinReference input in group)
                    {
                        unspentcoins.GetAndChange(input.PrevHash).Items[input.PrevIndex] |= CoinState.Spent;
                        if (tx_prev.Outputs[input.PrevIndex].AssetId.Equals(GoverningToken.Hash))
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
                            contracts.GetOrAdd(publish_tx.ScriptHash, () => new ContractState
                            {
                                Script = publish_tx.Script,
                                ParameterList = publish_tx.ParameterList,
                                ReturnType = publish_tx.ReturnType,
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
                            ApplicationEngine engine = new ApplicationEngine(TriggerType.Application, itx, script_table, service, itx.Gas);
                            engine.LoadScript(itx.Script, false);
                            if (engine.Execute())
                            {
                                service.Commit();
                                if (service.Notifications.Count > 0)
                                    OnNotify(service.Notifications.ToArray());
                            }
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
            db.Write(WriteOptions.Default, batch);
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
                    Block block;
                    lock (block_cache)
                    {
                        if (!block_cache.ContainsKey(hash)) break;
                        block = block_cache[hash];
                    }
                    Persist(block);
                    OnPersistCompleted(block);
                    lock (block_cache)
                    {
                        block_cache.Remove(hash);
                    }
                }
            }
        }
    }
}
