using AntShares.Core;
using AntShares.IO;
using AntShares.IO.Caching;
using LevelDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace AntShares.Implementations.Blockchains.LevelDB
{
    public class LevelDBBlockchain : Blockchain
    {
        private DB db;
        private Thread thread_persistence;
        private Tree<UInt256, Block> header_chain = new Tree<UInt256, Block>(GenesisBlock.Hash, GenesisBlock);
        private List<UInt256> header_index = new List<UInt256>();
        private Dictionary<UInt256, Block> block_cache = new Dictionary<UInt256, Block>();
        private UInt256 current_block_hash = GenesisBlock.Hash;
        private UInt256 current_header_hash = GenesisBlock.Hash;
        private uint current_block_height = 0;
        private uint stored_header_count = 0;
        private bool disposed = false;

        public override BlockchainAbility Ability => BlockchainAbility.All;
        public override UInt256 CurrentBlockHash => current_block_hash;
        public override UInt256 CurrentHeaderHash => current_header_hash;
        public override uint HeaderHeight => header_chain.Nodes[current_header_hash].Height;
        public override uint Height => current_block_height;
        public override bool IsReadOnly => false;
        public bool VerifyBlocks { get; set; } = true;

        public LevelDBBlockchain(string path)
        {
            header_index.Add(GenesisBlock.Hash);
            Version version;
            Slice value;
            db = DB.Open(path, new Options { CreateIfMissing = true });
            if (db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.CFG_Version), out value) && Version.TryParse(value.ToString(), out version) && version >= Version.Parse("0.4"))
            {
                ReadOptions options = new ReadOptions { FillCache = false };
                value = db.Get(options, SliceBuilder.Begin(DataEntryPrefix.SYS_CurrentBlock));
                this.current_block_hash = new UInt256(value.ToArray().Take(32).ToArray());
                this.current_block_height = BitConverter.ToUInt32(value.ToArray(), 32);
                foreach (Block header in db.Find(options, SliceBuilder.Begin(DataEntryPrefix.DATA_HeaderList), (k, v) =>
                {
                    using (MemoryStream ms = new MemoryStream(v.ToArray(), false))
                    using (BinaryReader r = new BinaryReader(ms))
                    {
                        return new
                        {
                            Index = BitConverter.ToUInt32(k.ToArray(), 1),
                            Headers = r.ReadSerializableArray<Block>()
                        };
                    }
                }).OrderBy(p => p.Index).SelectMany(p => p.Headers).ToArray())
                {
                    if (header.Hash != GenesisBlock.Hash)
                    {
                        header_chain.Add(header.Hash, header, header.PrevBlock);
                        header_index.Add(header.Hash);
                    }
                    stored_header_count++;
                }
                if (stored_header_count == 0)
                {
                    Dictionary<UInt256, Block> table = db.Find(options, SliceBuilder.Begin(DataEntryPrefix.DATA_Block), (k, v) => Block.FromTrimmedData(v.ToArray(), 0)).ToDictionary(p => p.PrevBlock);
                    for (UInt256 hash = GenesisBlock.Hash; hash != current_block_hash;)
                    {
                        Block header = table[hash];
                        header_chain.Add(header.Hash, header, header.PrevBlock);
                        header_index.Add(header.Hash);
                        hash = header.Hash;
                    }
                }
                else if (current_block_height >= stored_header_count)
                {
                    List<Block> list = new List<Block>();
                    for (UInt256 hash = current_block_hash; hash != header_index[(int)stored_header_count - 1];)
                    {
                        Block header = Block.FromTrimmedData(db.Get(options, SliceBuilder.Begin(DataEntryPrefix.DATA_Block).Add(hash)).ToArray(), 0);
                        list.Add(header);
                        header_index.Insert((int)stored_header_count, hash);
                        hash = header.PrevBlock;
                    }
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        header_chain.Add(list[i].Hash, list[i], list[i].PrevBlock);
                    }
                }
                this.current_header_hash = header_index[header_index.Count - 1];
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
                db.Put(WriteOptions.Default, SliceBuilder.Begin(DataEntryPrefix.CFG_Version), Assembly.GetExecutingAssembly().GetName().Version.ToString());
            }
            thread_persistence = new Thread(PersistBlocks);
            thread_persistence.Name = "LevelDBBlockchain.PersistBlocks";
            thread_persistence.Start();
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        }

        protected internal override bool AddBlock(Block block)
        {
            lock (block_cache)
            {
                if (!block_cache.ContainsKey(block.Hash))
                {
                    block_cache.Add(block.Hash, block);
                }
            }
            lock (header_chain)
            {
                if (!header_chain.Nodes.ContainsKey(block.PrevBlock)) return false;
                if (!header_chain.Nodes.ContainsKey(block.Hash))
                {
                    if (VerifyBlocks && !block.Verify()) return false;
                    header_chain.Add(block.Hash, block.Header, block.PrevBlock);
                    OnAddHeader(block);
                }
            }
            return true;
        }

        protected internal override void AddHeaders(IEnumerable<Block> headers)
        {
            lock (header_chain)
            {
                foreach (Block header in headers)
                {
                    if (!header_chain.Nodes.ContainsKey(header.PrevBlock)) break;
                    if (header_chain.Nodes.ContainsKey(header.Hash)) continue;
                    if (VerifyBlocks && !header.Verify()) break;
                    header_chain.Add(header.Hash, header, header.PrevBlock);
                    OnAddHeader(header);
                }
            }
        }

        public override bool ContainsAsset(UInt256 hash)
        {
            if (base.ContainsAsset(hash)) return true;
            Slice value;
            return db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.IX_Asset).Add(hash), out value);
        }

        public override bool ContainsBlock(UInt256 hash)
        {
            if (!header_chain.Nodes.ContainsKey(hash)) return false;
            TreeNode<Block> node = header_chain.Nodes[hash];
            TreeNode<Block> i = header_chain.Nodes[current_block_hash];
            if (i.Height < node.Height) return false;
            while (i.Height > node.Height)
                i = i.Parent;
            return i == node;
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
            if (!thread_persistence.ThreadState.HasFlag(ThreadState.Unstarted))
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
                block = GetBlockInternal(hash, ReadOptions.Default);
            }
            return block;
        }

        public override UInt256 GetBlockHash(uint height)
        {
            UInt256 hash = base.GetBlockHash(height);
            if (hash != null) return hash;
            if (current_block_height < height) return null;
            lock (header_chain)
            {
                if (header_index.Count <= height) return null;
                return header_index[(int)height];
            }
        }

        private Block GetBlockInternal(UInt256 hash, ReadOptions options)
        {
            Slice value;
            if (!db.TryGet(options, SliceBuilder.Begin(DataEntryPrefix.DATA_Block).Add(hash), out value))
                return null;
            return Block.FromTrimmedData(value.ToArray(), 0, p => GetTransaction(p, options));
        }

        public override IEnumerable<EnrollmentTransaction> GetEnrollments(IEnumerable<Transaction> others)
        {
            ReadOptions options = new ReadOptions();
            using (options.Snapshot = db.GetSnapshot())
            {
                foreach (Slice key in db.Find(options, SliceBuilder.Begin(DataEntryPrefix.IX_Enrollment), (k, v) => k))
                {
                    UInt256 hash = new UInt256(key.ToArray().Skip(1).Take(32).ToArray());
                    if (others.SelectMany(p => p.GetAllInputs()).Any(p => p.PrevHash == hash && p.PrevIndex == 0))
                        continue;
                    yield return (EnrollmentTransaction)GetTransaction(hash, options);
                }
            }
            foreach (EnrollmentTransaction tx in others.OfType<EnrollmentTransaction>())
            {
                yield return tx;
            }
        }

        public override Block GetHeader(UInt256 hash)
        {
            if (!header_chain.Nodes.ContainsKey(hash)) return null;
            return header_chain[hash];
        }

        public override UInt256[] GetLeafHeaderHashes()
        {
            lock (header_chain)
            {
                return header_chain.Leaves.Select(p => p.Item.Hash).ToArray();
            }
        }

        public override Block GetNextBlock(UInt256 hash)
        {
            return GetBlockInternal(GetNextBlockHash(hash), ReadOptions.Default);
        }

        public override UInt256 GetNextBlockHash(UInt256 hash)
        {
            lock (header_chain)
            {
                if (!header_chain.Nodes.ContainsKey(hash)) return null;
                uint height = header_chain.Nodes[hash].Height;
                if (hash != header_index[(int)height]) return null;
                if (header_index.Count <= height + 1) return null;
                return header_chain[header_index[(int)height + 1]].Hash;
            }
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
                    ushort[] indexes = kv.Value.ToArray().GetUInt16Array().Except(others.SelectMany(p => p.GetAllInputs()).Where(p => p.PrevHash == hash).Select(p => p.PrevIndex)).ToArray();
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
            lock (MemoryPool)
            {
                if (MemoryPool.Values.SelectMany(p => p.GetAllInputs()).Intersect(inputs).Count() > 0)
                    return true;
            }
            ReadOptions options = new ReadOptions();
            using (options.Snapshot = db.GetSnapshot())
            {
                foreach (var group in inputs.GroupBy(p => p.PrevHash))
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

        private void OnAddHeader(Block header)
        {
            if (header.PrevBlock == current_header_hash)
            {
                current_header_hash = header.Hash;
                header_index.Add(header.Hash);
                uint height = header_chain.Nodes[current_header_hash].Height;
                if (height % 2000 == 0)
                {
                    WriteBatch batch = new WriteBatch();
                    while (height - 2000 > stored_header_count)
                    {
                        using (MemoryStream ms = new MemoryStream())
                        using (BinaryWriter w = new BinaryWriter(ms))
                        {
                            w.Write(header_index.Skip((int)stored_header_count).Take(2000).Select(p => header_chain[p]).ToArray());
                            w.Flush();
                            batch.Put(SliceBuilder.Begin(DataEntryPrefix.DATA_HeaderList).Add(stored_header_count), ms.ToArray());
                        }
                        stored_header_count += 2000;
                    }
                    db.Write(WriteOptions.Default, batch);
                }
            }
            else
            {
                TreeNode<Block> main = header_chain.Leaves.OrderByDescending(p => p.Height).First();
                if (main.Item.Hash != current_header_hash)
                {
                    TreeNode<Block> fork = header_chain.Nodes[current_header_hash];
                    current_header_hash = main.Item.Hash;
                    TreeNode<Block> common = header_chain.FindCommonNode(main, fork);
                    header_index.RemoveRange((int)common.Height + 1, header_index.Count - (int)common.Height - 1);
                    for (TreeNode<Block> i = main; i != common; i = i.Parent)
                    {
                        header_index.Insert((int)common.Height + 1, i.Item.Hash);
                    }
                    if (header_chain.Nodes[current_block_hash].Height > common.Height)
                    {
                        Rollback(common.Item.Hash);
                    }
                }
            }
        }

        private void Persist(Block block)
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
            WriteBatch batch = new WriteBatch();
            batch.Put(SliceBuilder.Begin(DataEntryPrefix.DATA_Block).Add(block.Hash), block.Trim());
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
                    batch.Delete(SliceBuilder.Begin(DataEntryPrefix.IX_Enrollment).Add(input.PrevHash));
                }
                unspents.Remove(input.PrevHash, input.PrevIndex);
                unspent_antshares.Remove(input.PrevHash, input.PrevIndex);
                unspent_votes.Remove(input.PrevHash, input.PrevIndex);
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
            current_block_hash = block.Hash;
            current_block_height = block.Hash == GenesisBlock.Hash ? 0 : current_block_height + 1;
            batch.Put(SliceBuilder.Begin(DataEntryPrefix.SYS_CurrentBlock), SliceBuilder.Begin().Add(block.Hash).Add(current_block_height));
            db.Write(WriteOptions.Default, batch);
        }

        private void PersistBlocks()
        {
            while (!disposed)
            {
                while (!disposed)
                {
                    UInt256 hash;
                    lock (header_chain)
                    {
                        TreeNode<Block> node = header_chain.Nodes[current_block_hash];
                        if (header_index.Count <= node.Height + 1) break;
                        hash = header_index[(int)node.Height + 1];
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
                for (int i = 0; i < 10 && !disposed; i++)
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
            if (hash == current_block_hash) return;
            List<Block> blocks = new List<Block>();
            UInt256 current = current_block_hash;
            while (current != hash)
            {
                if (current == GenesisBlock.Hash)
                    throw new InvalidOperationException();
                Block block = GetBlockInternal(current, ReadOptions.Default);
                blocks.Add(block);
                current = block.PrevBlock;
            }
            WriteBatch batch = new WriteBatch();
            foreach (Block block in blocks)
            {
                batch.Delete(SliceBuilder.Begin(DataEntryPrefix.DATA_Block).Add(block.Hash));
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
            foreach (var group in blocks.SelectMany(p => p.Transactions).SelectMany(p => p.GetAllInputs()).GroupBy(p => p.PrevHash).Where(g => !tx_hashes.Contains(g.Key)))
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
            current_block_hash = current;
            current_block_height -= (uint)blocks.Count;
            batch.Put(SliceBuilder.Begin(DataEntryPrefix.SYS_CurrentBlock), SliceBuilder.Begin().Add(current_block_hash).Add(current_block_height));
            db.Write(WriteOptions.Default, batch);
        }
    }
}
