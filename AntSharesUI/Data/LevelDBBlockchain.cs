using AntShares.Core;
using AntShares.IO;
using AntShares.Properties;
using LevelDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Transaction = AntShares.Core.Transaction;

namespace AntShares.Data
{
    internal class LevelDBBlockchain : Blockchain
    {
        private DB db;
        private Thread thread_persistence;
        private Dictionary<UInt256, Block> cache = new Dictionary<UInt256, Block>();
        private UInt256 current_block;
        private uint current_height;
        private object persistence_sync_obj = new object();
        private bool disposed = false;

        public override BlockchainAbility Ability => BlockchainAbility.All;
        public override UInt256 CurrentBlockHash => current_block;
        public override uint Height => current_height;
        public override bool IsReadOnly => false;

        public LevelDBBlockchain()
        {
            Slice value;
            db = DB.Open(Settings.Default.DataDirectoryPath);
            if (db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.CFG_Initialized), out value) && value.ToBoolean())
            {
                value = db.Get(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.SYS_CurrentBlock));
                this.current_block = new UInt256(value.ToArray().Take(32).ToArray());
                this.current_height = BitConverter.ToUInt32(value.ToArray(), 32);
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
                batch.Put(SliceBuilder.Begin(DataEntryPrefix.CFG_Version), 0);
                db.Write(WriteOptions.Default, batch);
                AddBlockToChain(GenesisBlock);
                db.Put(WriteOptions.Default, SliceBuilder.Begin(DataEntryPrefix.CFG_Initialized), true);
            }
            thread_persistence = new Thread(PersistBlocks);
            thread_persistence.Name = "LevelDBBlockchain.PersistBlocks";
            thread_persistence.Start();
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        }

        private void AddBlockToChain(Block block)
        {
            MultiValueDictionary<UInt256, ushort> unspents = new MultiValueDictionary<UInt256, ushort>(p =>
            {
                Slice value = new byte[0];
                db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.IX_Unspent).Add(p), out value);
                return new HashSet<ushort>(value.ToArray().GetUInt16Array());
            });
            MultiValueDictionary<UInt256, ushort> unspent_antshares = new MultiValueDictionary<UInt256, ushort>(p =>
            {
                Slice value = new byte[0];
                db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.IX_AntShare).Add(p), out value);
                return new HashSet<ushort>(value.ToArray().GetUInt16Array());
            });
            MultiValueDictionary<UInt256, ushort> unspent_votes = new MultiValueDictionary<UInt256, ushort>(p =>
            {
                Slice value = new byte[0];
                db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.IX_Vote).Add(p), out value);
                return new HashSet<ushort>(value.ToArray().GetUInt16Array());
            });
            Dictionary<UInt256, Fixed8> quantities = new Dictionary<UInt256, Fixed8>();
            uint height = block.Hash == GenesisBlock.Hash ? 0 : current_height + 1;
            WriteBatch batch = new WriteBatch();
            batch.Put(SliceBuilder.Begin(DataEntryPrefix.DATA_Block).Add(block.Hash), SliceBuilder.Begin().Add(height).Add(block.Trim()));
            batch.Put(SliceBuilder.Begin(DataEntryPrefix.IX_PrevBlock).Add(block.PrevBlock).Add(block.Hash), true);
            foreach (Transaction tx in block.Transactions)
            {
                batch.Put(SliceBuilder.Begin(DataEntryPrefix.DATA_Transaction).Add(tx.Hash), tx.ToArray());
                switch (tx.Type)
                {
                    case TransactionType.IssueTransaction:
                        foreach (TransactionResult result in tx.GetTransactionResults().Where(p => p.Amount < Fixed8.Zero))
                        {
                            if (quantities.ContainsKey(result.AssetId))
                            {
                                quantities[result.AssetId] -= result.Amount;
                            }
                            else
                            {
                                quantities.Add(result.AssetId, -result.Amount);
                            }
                        }
                        break;
                    case TransactionType.EnrollmentTransaction:
                        {
                            EnrollmentTransaction enroll_tx = (EnrollmentTransaction)tx;
                            batch.Put(SliceBuilder.Begin(DataEntryPrefix.IX_Enrollment).Add(tx.Hash), true);
                        }
                        break;
                    case TransactionType.VotingTransaction:
                        unspent_votes.AddEmpty(tx.Hash);
                        for (ushort index = 0; index < tx.Outputs.Length; index++)
                        {
                            if (tx.Outputs[index].AssetId == AntShare.Hash)
                            {
                                unspent_votes.Add(tx.Hash, index);
                            }
                        }
                        break;
                    case TransactionType.RegisterTransaction:
                        {
                            RegisterTransaction reg_tx = (RegisterTransaction)tx;
                            batch.Put(SliceBuilder.Begin(DataEntryPrefix.IX_Asset).Add(reg_tx.Hash), true);
                        }
                        break;
                }
                unspents.AddEmpty(tx.Hash);
                unspent_antshares.AddEmpty(tx.Hash);
                for (ushort index = 0; index < tx.Outputs.Length; index++)
                {
                    unspents.Add(tx.Hash, index);
                    if (tx.Outputs[index].AssetId == AntShare.Hash)
                    {
                        unspent_antshares.Add(tx.Hash, index);
                    }
                }
            }
            foreach (TransactionInput input in block.Transactions.SelectMany(p => p.GetAllInputs()))
            {
                if (input.PrevIndex == 0)
                {
                    batch.Delete(SliceBuilder.Begin(DataEntryPrefix.IX_Enrollment).Add(input.PrevTxId));
                }
                unspents.Remove(input.PrevTxId, input.PrevIndex);
                unspent_antshares.Remove(input.PrevTxId, input.PrevIndex);
                unspent_votes.Remove(input.PrevTxId, input.PrevIndex);
            }
            //统计AntCoin的发行量
            {
                Fixed8 amount_in = block.Transactions.SelectMany(p => p.References.Values.Where(o => o.AssetId == AntCoin.Hash)).Sum(p => p.Value);
                Fixed8 amount_out = block.Transactions.SelectMany(p => p.Outputs.Where(o => o.AssetId == AntCoin.Hash)).Sum(p => p.Value);
                if (amount_in != amount_out)
                {
                    quantities.Add(AntCoin.Hash, amount_out - amount_in);
                }
            }
            foreach (var unspent in unspents)
            {
                if (unspent.Value.Count == 0)
                {
                    batch.Delete(SliceBuilder.Begin(DataEntryPrefix.IX_Unspent).Add(unspent.Key));
                }
                else
                {
                    batch.Put(SliceBuilder.Begin(DataEntryPrefix.IX_Unspent).Add(unspent.Key), unspent.Value.ToByteArray());
                }
            }
            foreach (var unspent in unspent_antshares)
            {
                if (unspent.Value.Count == 0)
                {
                    batch.Delete(SliceBuilder.Begin(DataEntryPrefix.IX_AntShare).Add(unspent.Key));
                }
                else
                {
                    batch.Put(SliceBuilder.Begin(DataEntryPrefix.IX_AntShare).Add(unspent.Key), unspent.Value.ToByteArray());
                }
            }
            foreach (var unspent in unspent_votes)
            {
                if (unspent.Value.Count == 0)
                {
                    batch.Delete(SliceBuilder.Begin(DataEntryPrefix.IX_Vote).Add(unspent.Key));
                }
                else
                {
                    batch.Put(SliceBuilder.Begin(DataEntryPrefix.IX_Vote).Add(unspent.Key), unspent.Value.ToByteArray());
                }
            }
            foreach (var quantity in quantities)
            {
                batch.Put(SliceBuilder.Begin(DataEntryPrefix.ST_QuantityIssued).Add(quantity.Key), (GetQuantityIssued(quantity.Key) + quantity.Value).GetData());
            }
            batch.Put(SliceBuilder.Begin(DataEntryPrefix.SYS_CurrentBlock), SliceBuilder.Begin().Add(block.Hash).Add(height));
            db.Write(WriteOptions.Default, batch);
            current_block = block.Hash;
            current_height = height;
        }

        public override bool ContainsAsset(UInt256 hash)
        {
            if (base.ContainsAsset(hash)) return true;
            Slice value;
            return db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.IX_Asset).Add(hash), out value);
        }

        public override bool ContainsBlock(UInt256 hash)
        {
            if (base.ContainsBlock(hash)) return true;
            Slice value;
            //TODO: 尝试有没有更快的方法找出LevelDB中是否存在某一条记录
            //目前只能通过尝试读取某一条记录后判断是否成功，没有其它的好办法
            //对每一条记录添加一个只包含hash的索引记录，应该能提升判断的速度
            //但是这个方法会增加数据库的大小，需要测试一下优劣
            return db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.DATA_Block).Add(hash), out value);
        }

        public override bool ContainsTransaction(UInt256 hash)
        {
            if (base.ContainsTransaction(hash)) return true;
            Slice value;
            return db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.DATA_Transaction).Add(hash), out value);
        }

        public override bool ContainsUnspent(UInt256 hash, ushort index)
        {
            if (base.ContainsUnspent(hash, index)) return true;
            Slice value;
            if (!db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.IX_Unspent).Add(hash), out value))
                return false;
            return value.ToArray().GetUInt16Array().Contains(index);
        }

        private void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            Dispose();
        }

        public override void Dispose()
        {
            disposed = true;
            AppDomain.CurrentDomain.ProcessExit -= CurrentDomain_ProcessExit;
            base.Dispose();
            thread_persistence.Join();
            if (db != null)
            {
                db.Dispose();
                db = null;
            }
        }

        public override IEnumerable<RegisterTransaction> GetAssets()
        {
            yield return AntCoin;
            ReadOptions options = new ReadOptions();
            using (options.Snapshot = db.GetSnapshot())
            {
                foreach (Slice key in db.Find(options, SliceBuilder.Begin(DataEntryPrefix.IX_Asset), (k, v) => k))
                {
                    UInt256 hash = new UInt256(key.ToArray().Skip(1).ToArray());
                    yield return (RegisterTransaction)GetTransaction(hash, options);
                }
            }
        }

        public override Block GetBlock(UInt256 hash)
        {
            Block block = base.GetBlock(hash);
            if (block == null)
            {
                uint height;
                block = GetBlockAndHeight(hash, out height);
            }
            return block;
        }

        public override Block GetBlockAndHeight(UInt256 hash, out uint height)
        {
            Block block = base.GetBlockAndHeight(hash, out height);
            if (block == null)
            {
                block = GetBlockAndHeight(hash, ReadOptions.Default, out height);
            }
            return block;
        }

        private Block GetBlockAndHeight(UInt256 hash, ReadOptions options, out uint height)
        {
            Slice value;
            if (!db.TryGet(options, SliceBuilder.Begin(DataEntryPrefix.DATA_Block).Add(hash), out value))
            {
                height = 0;
                return null;
            }
            byte[] data = value.ToArray();
            height = BitConverter.ToUInt32(data, 0);
            return Block.FromTrimmedData(data, sizeof(uint), p => GetTransaction(p, options));
        }

        public override int GetBlockHeight(UInt256 hash)
        {
            Slice value;
            if (!db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.DATA_Block).Add(hash), out value))
                return -1;
            return BitConverter.ToInt32(value.ToArray(), 0);
        }

        public override IEnumerable<EnrollmentTransaction> GetEnrollments(IEnumerable<Transaction> others)
        {
            ReadOptions options = new ReadOptions();
            using (options.Snapshot = db.GetSnapshot())
            {
                foreach (Slice key in db.Find(options, SliceBuilder.Begin(DataEntryPrefix.IX_Enrollment), (k, v) => k))
                {
                    UInt256 hash = new UInt256(key.ToArray().Skip(1).Take(32).ToArray());
                    if (others.SelectMany(p => p.GetAllInputs()).Any(p => p.PrevTxId == hash && p.PrevIndex == 0))
                        continue;
                    yield return (EnrollmentTransaction)GetTransaction(hash, options);
                }
            }
            foreach (EnrollmentTransaction tx in others.OfType<EnrollmentTransaction>())
            {
                yield return tx;
            }
        }

        public override BlockHeader GetHeader(UInt256 hash)
        {
            BlockHeader header = base.GetBlock(hash)?.Header;
            if (header == null)
            {
                Slice value;
                if (db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.DATA_Block).Add(hash), out value))
                {
                    header = BlockHeader.FromTrimmedData(value.ToArray(), sizeof(uint));
                }
            }
            return header;
        }

        public override Block GetNextBlock(UInt256 hash)
        {
            ReadOptions options = new ReadOptions();
            using (options.Snapshot = db.GetSnapshot())
            {
                hash = GetNextBlockHash(hash, options);
                uint height;
                return GetBlockAndHeight(hash, options, out height);
            }
        }

        public override UInt256 GetNextBlockHash(UInt256 hash)
        {
            return GetNextBlockHash(hash, ReadOptions.Default);
        }

        private UInt256 GetNextBlockHash(UInt256 hash, ReadOptions options)
        {
            Slice value;
            if (!db.TryGet(options, SliceBuilder.Begin(DataEntryPrefix.IX_PrevBlock).Add(hash), out value))
                return null;
            return new UInt256(value.ToArray());
        }

        public override Fixed8 GetQuantityIssued(UInt256 asset_id)
        {
            Slice quantity = 0L;
            db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.ST_QuantityIssued).Add(asset_id), out quantity);
            return new Fixed8(quantity.ToInt64());
        }

        public override Transaction GetTransaction(UInt256 hash)
        {
            Transaction tx = base.GetTransaction(hash);
            if (tx == null)
            {
                tx = GetTransaction(hash, ReadOptions.Default);
            }
            return tx;
        }

        private Transaction GetTransaction(UInt256 hash, ReadOptions options)
        {
            Slice value;
            if (!db.TryGet(options, SliceBuilder.Begin(DataEntryPrefix.DATA_Transaction).Add(hash), out value))
                return null;
            return Transaction.DeserializeFrom(value.ToArray());
        }

        public override TransactionOutput GetUnspent(UInt256 hash, ushort index)
        {
            TransactionOutput unspent = base.GetUnspent(hash, index);
            if (unspent != null) return unspent;
            ReadOptions options = new ReadOptions();
            using (options.Snapshot = db.GetSnapshot())
            {
                Slice value;
                if (!db.TryGet(options, SliceBuilder.Begin(DataEntryPrefix.IX_Unspent).Add(hash), out value))
                    return null;
                if (!value.ToArray().GetUInt16Array().Contains(index))
                    return null;
                return GetTransaction(hash, options).Outputs[index];
            }
        }

        public override IEnumerable<TransactionOutput> GetUnspentAntShares()
        {
            ReadOptions options = new ReadOptions();
            using (options.Snapshot = db.GetSnapshot())
            {
                foreach (var kv in db.Find(options, SliceBuilder.Begin(DataEntryPrefix.IX_AntShare), (k, v) => new { Key = k, Value = v }))
                {
                    UInt256 hash = new UInt256(kv.Key.ToArray().Skip(1).ToArray());
                    ushort[] indexes = kv.Value.ToArray().GetUInt16Array();
                    Transaction tx = GetTransaction(hash, options);
                    foreach (ushort index in indexes)
                    {
                        yield return tx.Outputs[index];
                    }
                }
            }
        }

        public override IEnumerable<Vote> GetVotes(IEnumerable<Transaction> others)
        {
            ReadOptions options = new ReadOptions();
            using (options.Snapshot = db.GetSnapshot())
            {
                foreach (var kv in db.Find(options, SliceBuilder.Begin(DataEntryPrefix.IX_Vote), (k, v) => new { Key = k, Value = v }))
                {
                    UInt256 hash = new UInt256(kv.Key.ToArray().Skip(1).ToArray());
                    ushort[] indexes = kv.Value.ToArray().GetUInt16Array().Except(others.SelectMany(p => p.GetAllInputs()).Where(p => p.PrevTxId == hash).Select(p => p.PrevIndex)).ToArray();
                    if (indexes.Length == 0) continue;
                    VotingTransaction tx = (VotingTransaction)GetTransaction(hash, options);
                    yield return new Vote
                    {
                        Enrollments = tx.Enrollments,
                        Count = indexes.Sum(p => tx.Outputs[p].Value)
                    };
                }
            }
            foreach (VotingTransaction tx in others.OfType<VotingTransaction>())
            {
                yield return new Vote
                {
                    Enrollments = tx.Enrollments,
                    Count = tx.Outputs.Where(p => p.AssetId == AntShare.Hash).Sum(p => p.Value)
                };
            }
        }

        public override bool IsDoubleSpend(Transaction tx)
        {
            TransactionInput[] inputs = tx.GetAllInputs().ToArray();
            if (inputs.Length == 0) return false;
            if (MemoryPool.Values.SelectMany(p => p.GetAllInputs()).Intersect(inputs).Count() > 0)
                return true;
            ReadOptions options = new ReadOptions();
            using (options.Snapshot = db.GetSnapshot())
            {
                foreach (var group in inputs.GroupBy(p => p.PrevTxId))
                {
                    Slice value;
                    if (!db.TryGet(options, SliceBuilder.Begin(DataEntryPrefix.IX_Unspent).Add(group.Key), out value))
                        return true;
                    HashSet<ushort> unspents = new HashSet<ushort>(value.ToArray().GetUInt16Array());
                    if (group.Any(p => !unspents.Contains(p.PrevIndex)))
                        return true;
                }
            }
            return false;
        }

        protected override void OnBlock(Block block)
        {
            lock (cache)
            {
                if (!cache.ContainsKey(block.PrevBlock))
                    cache.Add(block.PrevBlock, block);
            }
        }

        private void PersistBlocks()
        {
            while (!disposed)
            {
                bool persisted = false;
                lock (persistence_sync_obj)
                {
                    while (true)
                    {
                        Block block = null;
                        lock (cache)
                        {
                            if (cache.ContainsKey(current_block))
                            {
                                block = cache[current_block];
                                cache.Remove(current_block);
                            }
                        }
                        if (block?.Verify() != VerificationResult.OK) break;
                        lock (SyncRoot)
                        {
                            AddBlockToChain(block);
                            RaisePersistCompleted(block);
                        }
                        persisted = true;
                    }
                    if (persisted)
                    {
                        lock (cache)
                        {
                            foreach (UInt256 hash in cache.Keys.Where(p => p != current_block && ContainsBlock(p)).ToArray())
                            {
                                cache.Remove(hash);
                            }
                        }
                    }
                }
                for (int i = 0; i < 50 && !disposed; i++)
                {
                    Thread.Sleep(100);
                }
            }
        }

        /// <summary>
        /// 将区块链的状态回滚到指定的位置
        /// </summary>
        /// <param name="hash">
        /// 要回滚到的区块的散列值
        /// </param>
        private void Rollback(UInt256 hash)
        {
            lock (persistence_sync_obj)
            {
                if (hash == current_block) return;
                List<Block> blocks = new List<Block>();
                UInt256 current = current_block;
                while (current != hash)
                {
                    if (current == GenesisBlock.Hash)
                        throw new InvalidOperationException();
                    uint height;
                    Block block = GetBlockAndHeight(current, ReadOptions.Default, out height);
                    blocks.Add(block);
                    current = block.PrevBlock;
                }
                WriteBatch batch = new WriteBatch();
                foreach (Block block in blocks)
                {
                    batch.Delete(SliceBuilder.Begin(DataEntryPrefix.DATA_Block).Add(block.Hash));
                    batch.Delete(SliceBuilder.Begin(DataEntryPrefix.IX_PrevBlock).Add(block.PrevBlock).Add(block.Hash));
                    foreach (Transaction tx in block.Transactions)
                    {
                        batch.Delete(SliceBuilder.Begin(DataEntryPrefix.DATA_Transaction).Add(tx.Hash));
                        batch.Delete(SliceBuilder.Begin(DataEntryPrefix.IX_Enrollment).Add(tx.Hash));
                        batch.Delete(SliceBuilder.Begin(DataEntryPrefix.IX_Unspent).Add(tx.Hash));
                        batch.Delete(SliceBuilder.Begin(DataEntryPrefix.IX_AntShare).Add(tx.Hash));
                        batch.Delete(SliceBuilder.Begin(DataEntryPrefix.IX_Vote).Add(tx.Hash));
                        if (tx.Type == TransactionType.RegisterTransaction)
                        {
                            RegisterTransaction reg_tx = (RegisterTransaction)tx;
                            batch.Delete(SliceBuilder.Begin(DataEntryPrefix.IX_Asset).Add(reg_tx.Hash));
                        }
                    }
                }
                HashSet<UInt256> tx_hashes = new HashSet<UInt256>(blocks.SelectMany(p => p.Transactions).Select(p => p.Hash));
                foreach (var group in blocks.SelectMany(p => p.Transactions).SelectMany(p => p.GetAllInputs()).GroupBy(p => p.PrevTxId).Where(g => !tx_hashes.Contains(g.Key)))
                {
                    Transaction tx = GetTransaction(group.Key, ReadOptions.Default);
                    Slice value = new byte[0];
                    db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.IX_Unspent).Add(tx.Hash), out value);
                    IEnumerable<ushort> indexes = value.ToArray().GetUInt16Array().Union(group.Select(p => p.PrevIndex));
                    batch.Put(SliceBuilder.Begin(DataEntryPrefix.IX_Unspent).Add(tx.Hash), indexes.ToByteArray());
                    TransactionInput[] antshares = group.Where(p => tx.Outputs[p.PrevIndex].AssetId == AntShare.Hash).ToArray();
                    if (antshares.Length > 0)
                    {
                        value = new byte[0];
                        db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.IX_AntShare).Add(tx.Hash), out value);
                        indexes = value.ToArray().GetUInt16Array().Union(antshares.Select(p => p.PrevIndex));
                        batch.Put(SliceBuilder.Begin(DataEntryPrefix.IX_AntShare).Add(tx.Hash), indexes.ToByteArray());
                    }
                    switch (tx.Type)
                    {
                        case TransactionType.EnrollmentTransaction:
                            if (group.Any(p => p.PrevIndex == 0))
                            {
                                batch.Put(SliceBuilder.Begin(DataEntryPrefix.IX_Enrollment).Add(tx.Hash), true);
                            }
                            break;
                        case TransactionType.VotingTransaction:
                            {
                                TransactionInput[] votes = group.Where(p => tx.Outputs[p.PrevIndex].AssetId == AntShare.Hash).ToArray();
                                if (votes.Length > 0)
                                {
                                    value = new byte[0];
                                    db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.IX_Vote).Add(tx.Hash), out value);
                                    indexes = value.ToArray().GetUInt16Array().Union(votes.Select(p => p.PrevIndex));
                                    batch.Put(SliceBuilder.Begin(DataEntryPrefix.IX_Vote).Add(tx.Hash), indexes.ToByteArray());
                                }
                            }
                            break;
                    }
                }
                //回滚AntCoin的发行量
                {
                    Fixed8 amount_in = blocks.SelectMany(p => p.Transactions).SelectMany(p => p.References.Values.Where(o => o.AssetId == Blockchain.AntCoin.Hash)).Sum(p => p.Value);
                    Fixed8 amount_out = blocks.SelectMany(p => p.Transactions).SelectMany(p => p.Outputs.Where(o => o.AssetId == Blockchain.AntCoin.Hash)).Sum(p => p.Value);
                    if (amount_in != amount_out)
                    {
                        batch.Put(SliceBuilder.Begin(DataEntryPrefix.ST_QuantityIssued).Add(AntCoin.Hash), (GetQuantityIssued(AntCoin.Hash) - (amount_out - amount_in)).GetData());
                    }
                }
                foreach (var result in blocks.SelectMany(p => p.Transactions).Where(p => p.Type == TransactionType.IssueTransaction).SelectMany(p => p.GetTransactionResults()).Where(p => p.Amount < Fixed8.Zero).GroupBy(p => p.AssetId, (k, g) => new
                {
                    AssetId = k,
                    Amount = -g.Sum(p => p.Amount)
                }))
                {
                    batch.Put(SliceBuilder.Begin(DataEntryPrefix.ST_QuantityIssued).Add(result.AssetId), (GetQuantityIssued(result.AssetId) - result.Amount).GetData());
                }
                current_block = current;
                current_height -= (uint)blocks.Count;
                batch.Put(SliceBuilder.Begin(DataEntryPrefix.SYS_CurrentBlock), SliceBuilder.Begin().Add(current_block).Add(current_height));
                db.Write(WriteOptions.Default, batch);
            }
        }
    }
}
