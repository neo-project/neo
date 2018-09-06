using Neo.Core;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Caching;
using Neo.IO.Data.LevelDB;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Iterator = Neo.IO.Data.LevelDB.Iterator;

namespace Neo.Implementations.Blockchains.LevelDB
{
    public class LevelDBBlockchain : Blockchain
    {
        public static event EventHandler<ApplicationExecutedEventArgs> ApplicationExecuted;

        private DB db;
        private Thread thread_persistence;
        private ReaderWriterLockSlim headerIndexRwLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private List<UInt256> header_index = new List<UInt256>();
        private ReaderWriterLockSlim headerCacheRwLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
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

        /// <summary>
        /// Return true if haven't got valid handle
        /// </summary>
        public override bool IsDisposed => disposed;

        public LevelDBBlockchain(string path)
        {
            header_index.Add(GenesisBlock.Hash);
            Version version;
            Slice value;
            db = DB.Open(path, new Options { CreateIfMissing = true });
            if (db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.SYS_Version), out value) && Version.TryParse(value.ToString(), out version) && version >= Version.Parse("2.7.4"))
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
            thread_persistence.Priority = ThreadPriority.AboveNormal;
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
            headerIndexRwLock.EnterWriteLock();
            try
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
            finally
            {
                headerIndexRwLock.ExitWriteLock();
            }
            return true;
        }

        public void AddBlockDirectly(Block block)
        {
            if (block.Index != Height + 1)
                throw new InvalidOperationException();
            if (block.Index == header_index.Count)
            {
                WriteBatch batch = new WriteBatch();
                OnAddHeader(block.Header, batch);
                db.Write(WriteOptions.Default, batch);
            }

            lock (PersistLock)
            {
                Persist(block);
                OnPersistCompleted(block);
            }
        }

        protected internal override void AddHeaders(IEnumerable<Header> headers)
        {
            headerIndexRwLock.EnterWriteLock();
            try
            {
                headerCacheRwLock.EnterWriteLock();
                try
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
                finally
                {
                    headerCacheRwLock.ExitWriteLock();                    
                }
                
            }
            finally
            {
                headerIndexRwLock.ExitWriteLock();
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
            headerCacheRwLock.Dispose();
            headerIndexRwLock.Dispose();
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
            headerIndexRwLock.EnterReadLock();
            try
            {
                if (header_index.Count <= height) return null;
                return header_index[(int) height];
            }
            finally
            {
                headerIndexRwLock.ExitReadLock();
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
            HashSet<ECPoint> sv = new HashSet<ECPoint>(StandbyValidators);
            return db.Find<ValidatorState>(ReadOptions.Default, DataEntryPrefix.ST_Validator).Where(p => p.Registered || sv.Contains(p.PublicKey));
        }

        public override Header GetHeader(uint height)
        {
            UInt256 hash;
            headerIndexRwLock.EnterReadLock();
            try
            {
                if (header_index.Count <= height) return null;
                hash = header_index[(int) height];
            }
            finally
            {
                headerIndexRwLock.ExitReadLock();
            }
            return GetHeader(hash);
        }

        public override Header GetHeader(UInt256 hash)
        {
            headerCacheRwLock.EnterReadLock();
            try
            {
                if (header_cache.TryGetValue(hash, out Header header))
                    return header;
            }
            finally
            {
                headerCacheRwLock.ExitReadLock();
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
            headerIndexRwLock.EnterReadLock();
            try
            {
                if (header.Index + 1 >= header_index.Count)
                    return null;
                return header_index[(int) header.Index + 1];
            }
            finally
            {
                headerIndexRwLock.ExitReadLock();
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

        public override MetaDataCache<T> GetMetaData<T>()
        {
            Type t = typeof(T);
            if (t == typeof(ValidatorsCountState)) return new DbMetaDataCache<T>(db, DataEntryPrefix.IX_ValidatorsCount);
            throw new NotSupportedException();
        }

        public override DataCache<TKey, TValue> GetStates<TKey, TValue>()
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

        public override IEnumerable<TransactionOutput> GetUnspent(UInt256 hash)
        {
            ReadOptions options = new ReadOptions();
            using (options.Snapshot = db.GetSnapshot())
            {
                List<TransactionOutput> outputs = new List<TransactionOutput>();
                UnspentCoinState state = db.TryGet<UnspentCoinState>(options, DataEntryPrefix.ST_Coin, hash);
                if (state != null)
                {
                    int height;
                    Transaction tx = GetTransaction(options, hash, out height);
                    for (int i = 0; i < state.Items.Length; i++)
                    {
                        if (!state.Items[i].HasFlag(CoinState.Spent))
                        {
                            outputs.Add(tx.Outputs[i]);
                        }

                    }
                }
                return outputs;
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
            DbCache<UInt160, AccountState> accounts = new DbCache<UInt160, AccountState>(db, DataEntryPrefix.ST_Account, batch);
            DbCache<UInt256, UnspentCoinState> unspentcoins = new DbCache<UInt256, UnspentCoinState>(db, DataEntryPrefix.ST_Coin, batch);
            DbCache<UInt256, SpentCoinState> spentcoins = new DbCache<UInt256, SpentCoinState>(db, DataEntryPrefix.ST_SpentCoin, batch);
            DbCache<ECPoint, ValidatorState> validators = new DbCache<ECPoint, ValidatorState>(db, DataEntryPrefix.ST_Validator, batch);
            DbCache<UInt256, AssetState> assets = new DbCache<UInt256, AssetState>(db, DataEntryPrefix.ST_Asset, batch);
            DbCache<UInt160, ContractState> contracts = new DbCache<UInt160, ContractState>(db, DataEntryPrefix.ST_Contract, batch);
            DbCache<StorageKey, StorageItem> storages = new DbCache<StorageKey, StorageItem>(db, DataEntryPrefix.ST_Storage, batch);
            DbMetaDataCache<ValidatorsCountState> validators_count = new DbMetaDataCache<ValidatorsCountState>(db, DataEntryPrefix.IX_ValidatorsCount);
            CachedScriptTable script_table = new CachedScriptTable(contracts);
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
                    if (output.AssetId.Equals(GoverningToken.Hash) && account.Votes.Length > 0)
                    {
                        foreach (ECPoint pubkey in account.Votes)
                            validators.GetAndChange(pubkey, () => new ValidatorState(pubkey)).Votes += output.Value;
                        validators_count.GetAndChange().Votes[account.Votes.Length - 1] += output.Value;
                    }
                }
                foreach (var group in tx.Inputs.GroupBy(p => p.PrevHash))
                {
                    Transaction tx_prev = GetTransaction(ReadOptions.Default, group.Key, out int height);
                    foreach (CoinReference input in group)
                    {
                        unspentcoins.GetAndChange(input.PrevHash).Items[input.PrevIndex] |= CoinState.Spent;
                        TransactionOutput out_prev = tx_prev.Outputs[input.PrevIndex];
                        AccountState account = accounts.GetAndChange(out_prev.ScriptHash);
                        if (out_prev.AssetId.Equals(GoverningToken.Hash))
                        {
                            spentcoins.GetAndChange(input.PrevHash, () => new SpentCoinState
                            {
                                TransactionHash = input.PrevHash,
                                TransactionHeight = (uint)height,
                                Items = new Dictionary<ushort, uint>()
                            }).Items.Add(input.PrevIndex, block.Index);
                            if (account.Votes.Length > 0)
                            {
                                foreach (ECPoint pubkey in account.Votes)
                                {
                                    ValidatorState validator = validators.GetAndChange(pubkey);
                                    validator.Votes -= out_prev.Value;
                                    if (!validator.Registered && validator.Votes.Equals(Fixed8.Zero))
                                        validators.Delete(pubkey);
                                }
                                validators_count.GetAndChange().Votes[account.Votes.Length - 1] -= out_prev.Value;
                            }
                        }
                        account.Balances[out_prev.AssetId] -= out_prev.Value;
                    }
                }
                List<ApplicationExecutionResult> execution_results = new List<ApplicationExecutionResult>();
                switch (tx)
                {
#pragma warning disable CS0612
                    case RegisterTransaction tx_register:
                        assets.Add(tx.Hash, new AssetState
                        {
                            AssetId = tx_register.Hash,
                            AssetType = tx_register.AssetType,
                            Name = tx_register.Name,
                            Amount = tx_register.Amount,
                            Available = Fixed8.Zero,
                            Precision = tx_register.Precision,
                            Fee = Fixed8.Zero,
                            FeeAddress = new UInt160(),
                            Owner = tx_register.Owner,
                            Admin = tx_register.Admin,
                            Issuer = tx_register.Admin,
                            Expiration = block.Index + 2 * 2000000,
                            IsFrozen = false
                        });
                        break;
#pragma warning restore CS0612
                    case IssueTransaction _:
                        foreach (TransactionResult result in tx.GetTransactionResults().Where(p => p.Amount < Fixed8.Zero))
                            assets.GetAndChange(result.AssetId).Available -= result.Amount;
                        break;
                    case ClaimTransaction _:
                        foreach (CoinReference input in ((ClaimTransaction)tx).Claims)
                        {
                            if (spentcoins.TryGet(input.PrevHash)?.Items.Remove(input.PrevIndex) == true)
                                spentcoins.GetAndChange(input.PrevHash);
                        }
                        break;
#pragma warning disable CS0612
                    case EnrollmentTransaction tx_enrollment:
                        validators.GetAndChange(tx_enrollment.PublicKey, () => new ValidatorState(tx_enrollment.PublicKey)).Registered = true;
                        break;
#pragma warning restore CS0612
                    case StateTransaction tx_state:
                        foreach (StateDescriptor descriptor in tx_state.Descriptors)
                            switch (descriptor.Type)
                            {
                                case StateType.Account:
                                    ProcessAccountStateDescriptor(descriptor, accounts, validators, validators_count);
                                    break;
                                case StateType.Validator:
                                    ProcessValidatorStateDescriptor(descriptor, validators);
                                    break;
                            }
                        break;
#pragma warning disable CS0612
                    case PublishTransaction tx_publish:
                        contracts.GetOrAdd(tx_publish.ScriptHash, () => new ContractState
                        {
                            Script = tx_publish.Script,
                            ParameterList = tx_publish.ParameterList,
                            ReturnType = tx_publish.ReturnType,
                            ContractProperties = (ContractPropertyState)Convert.ToByte(tx_publish.NeedStorage),
                            Name = tx_publish.Name,
                            CodeVersion = tx_publish.CodeVersion,
                            Author = tx_publish.Author,
                            Email = tx_publish.Email,
                            Description = tx_publish.Description
                        });
                        break;
#pragma warning restore CS0612
                    case InvocationTransaction tx_invocation:
                        using (StateMachine service = new StateMachine(block, accounts, assets, contracts, storages))
                        {
                            ApplicationEngine engine = new ApplicationEngine(TriggerType.Application, tx_invocation, script_table, service, tx_invocation.Gas);
                            engine.LoadScript(tx_invocation.Script);
                            if (engine.Execute())
                            {
                                service.Commit();
                            }
                            execution_results.Add(new ApplicationExecutionResult
                            {
                                Trigger = TriggerType.Application,
                                ScriptHash = tx_invocation.Script.ToScriptHash(),
                                VMState = engine.State,
                                GasConsumed = engine.GasConsumed,
                                Stack = engine.ResultStack.ToArray(),
                                Notifications = service.Notifications.ToArray()
                            });
                        }
                        break;
                }
                if (execution_results.Count > 0)
                    ApplicationExecuted?.Invoke(this, new ApplicationExecutedEventArgs
                    {
                        Transaction = tx,
                        ExecutionResults = execution_results.ToArray()
                    });
            }
            accounts.DeleteWhere((k, v) => !v.IsFrozen && v.Votes.Length == 0 && v.Balances.All(p => p.Value <= Fixed8.Zero));
            accounts.Commit();
            unspentcoins.DeleteWhere((k, v) => v.Items.All(p => p.HasFlag(CoinState.Spent)));
            unspentcoins.Commit();
            spentcoins.DeleteWhere((k, v) => v.Items.Count == 0);
            spentcoins.Commit();
            validators.Commit();
            assets.Commit();
            contracts.Commit();
            storages.Commit();
            validators_count.Commit(batch);
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
                    headerIndexRwLock.EnterReadLock();
                    try
                    {
                        if (header_index.Count <= current_block_height + 1) break;
                        hash = header_index[(int) current_block_height + 1];
                    }
                    finally
                    {
                        headerIndexRwLock.ExitReadLock();
                    }
                    Block block;
                    lock (block_cache)
                    {
                        if (!block_cache.TryGetValue(hash, out block))
                            break;
                    }

                    VerificationCancellationToken.Cancel();
                    lock (PersistLock)
                    {
                        Persist(block);
                        OnPersistCompleted(block);
                        // Reset cancellation token.
                        VerificationCancellationToken = new CancellationTokenSource();
                    }

                    lock (block_cache)
                    {
                        block_cache.Remove(hash);
                    }

                    OnPersistUnlocked(block);
                }
            }
        }
    }
}
