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
        private AutoResetEvent new_block_event = new AutoResetEvent(false);
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
            if (db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.CFG_Version), out value) && Version.TryParse(value.ToString(), out version) && version >= Version.Parse("0.5"))
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
                    Dictionary<UInt256, Block> table = db.Find(options, SliceBuilder.Begin(DataEntryPrefix.DATA_Block), (k, v) => Block.FromTrimmedData(v.ToArray(), sizeof(long))).ToDictionary(p => p.PrevBlock);
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
                        Block header = Block.FromTrimmedData(db.Get(options, SliceBuilder.Begin(DataEntryPrefix.DATA_Block).Add(hash)).ToArray(), sizeof(long));
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
                if (header_chain.Nodes.ContainsKey(block.Hash))
                    new_block_event.Set();
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
            new_block_event.Set();
            AppDomain.CurrentDomain.ProcessExit -= CurrentDomain_ProcessExit;
            if (!thread_persistence.ThreadState.HasFlag(ThreadState.Unstarted))
                thread_persistence.Join();
            new_block_event.Dispose();
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
                int height;
                foreach (Slice key in db.Find(options, SliceBuilder.Begin(DataEntryPrefix.IX_Asset), (k, v) => k))
                {
                    UInt256 hash = new UInt256(key.ToArray().Skip(1).ToArray());
                    yield return (RegisterTransaction)GetTransaction(options, hash, out height);
                }
            }
        }

        public override Block GetBlock(UInt256 hash)
        {
            Block block = base.GetBlock(hash);
            if (block == null)
            {
                block = GetBlockInternal(ReadOptions.Default, hash);
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

        private Block GetBlockInternal(ReadOptions options, UInt256 hash)
        {
            Slice value;
            if (!db.TryGet(options, SliceBuilder.Begin(DataEntryPrefix.DATA_Block).Add(hash), out value))
                return null;
            int height;
            return Block.FromTrimmedData(value.ToArray(), sizeof(long), p => GetTransaction(options, p, out height));
        }

        public override IEnumerable<EnrollmentTransaction> GetEnrollments(IEnumerable<Transaction> others)
        {
            ReadOptions options = new ReadOptions();
            using (options.Snapshot = db.GetSnapshot())
            {
                int height;
                foreach (Slice key in db.Find(options, SliceBuilder.Begin(DataEntryPrefix.IX_Enrollment), (k, v) => k))
                {
                    UInt256 hash = new UInt256(key.ToArray().Skip(1).Take(32).ToArray());
                    if (others.SelectMany(p => p.GetAllInputs()).Any(p => p.PrevHash == hash && p.PrevIndex == 0))
                        continue;
                    yield return (EnrollmentTransaction)GetTransaction(options, hash, out height);
                }
            }
            foreach (EnrollmentTransaction tx in others.OfType<EnrollmentTransaction>())
            {
                yield return tx;
            }
        }

        public override Block GetHeader(uint height)
        {
            lock (header_chain)
            {
                if (header_index.Count <= height) return null;
                return header_chain[header_index[(int)height]];
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
            return GetBlockInternal(ReadOptions.Default, GetNextBlockHash(hash));
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

        public override long GetSysFeeAmount(UInt256 hash)
        {
            Slice value;
            if (!db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.DATA_Block).Add(hash), out value))
                return -1;
            return BitConverter.ToInt64(value.ToArray(), 0);
        }

        public override Transaction GetTransaction(UInt256 hash, out int height)
        {
            Transaction tx = base.GetTransaction(hash, out height);
            if (tx == null)
            {
                tx = GetTransaction(ReadOptions.Default, hash, out height);
            }
            return tx;
        }

        private Transaction GetTransaction(ReadOptions options, UInt256 hash, out int height)
        {
            Slice value;
            if (db.TryGet(options, SliceBuilder.Begin(DataEntryPrefix.DATA_Transaction).Add(hash), out value))
            {
                byte[] data = value.ToArray();
                height = BitConverter.ToInt32(data, 0);
                return Transaction.DeserializeFrom(data, sizeof(uint));
            }
            else
            {
                height = -1;
                return null;
            }
        }

        public override Dictionary<ushort, Claimable> GetUnclaimed(UInt256 hash)
        {
            int height;
            Transaction tx = GetTransaction(ReadOptions.Default, hash, out height);
            if (tx == null) return null;
            Slice value;
            if (db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.IX_Unclaimed).Add(hash), out value))
            {
                const int UnclaimedItemSize = sizeof(ushort) + sizeof(uint);
                byte[] data = value.ToArray();
                return Enumerable.Range(0, data.Length / UnclaimedItemSize).ToDictionary(i => BitConverter.ToUInt16(data, i * UnclaimedItemSize), i => new Claimable
                {
                    Output = tx.Outputs[BitConverter.ToUInt16(data, i * UnclaimedItemSize)],
                    StartHeight = (uint)height,
                    EndHeight = BitConverter.ToUInt32(data, i * UnclaimedItemSize + sizeof(ushort))
                });
            }
            else
            {
                return new Dictionary<ushort, Claimable>();
            }
        }

        public override TransactionOutput GetUnspent(UInt256 hash, ushort index)
        {
            ReadOptions options = new ReadOptions();
            using (options.Snapshot = db.GetSnapshot())
            {
                Slice value;
                if (!db.TryGet(options, SliceBuilder.Begin(DataEntryPrefix.IX_Unspent).Add(hash), out value))
                    return null;
                if (!value.ToArray().GetUInt16Array().Contains(index))
                    return null;
                int height;
                return GetTransaction(options, hash, out height).Outputs[index];
            }
        }

        public override IEnumerable<Vote> GetVotes(IEnumerable<Transaction> others)
        {
            ReadOptions options = new ReadOptions();
            using (options.Snapshot = db.GetSnapshot())
            {
                int height;
                foreach (var kv in db.Find(options, SliceBuilder.Begin(DataEntryPrefix.IX_Vote), (k, v) => new { Key = k, Value = v }))
                {
                    UInt256 hash = new UInt256(kv.Key.ToArray().Skip(1).ToArray());
                    ushort[] indexes = kv.Value.ToArray().GetUInt16Array().Except(others.SelectMany(p => p.GetAllInputs()).Where(p => p.PrevHash == hash).Select(p => p.PrevIndex)).ToArray();
                    if (indexes.Length == 0) continue;
                    VotingTransaction tx = (VotingTransaction)GetTransaction(options, hash, out height);
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
                        //Rollback(common.Item.Hash);
                        throw new InvalidDataException("Unexpected Rollback");
                    }
                }
            }
        }

        private void Persist(Block block)
        {
            const int UnclaimedItemSize = sizeof(ushort) + sizeof(uint);
            MultiValueDictionary<UInt256, ushort> unspents = new MultiValueDictionary<UInt256, ushort>(p =>
            {
                Slice value = new byte[0];
                db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.IX_Unspent).Add(p), out value);
                return new HashSet<ushort>(value.ToArray().GetUInt16Array());
            });
            MultiValueDictionary<UInt256, ushort, uint> unclaimed = new MultiValueDictionary<UInt256, ushort, uint>(p =>
            {
                Slice value = new byte[0];
                db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.IX_Unclaimed).Add(p), out value);
                byte[] data = value.ToArray();
                return Enumerable.Range(0, data.Length / UnclaimedItemSize).ToDictionary(i => BitConverter.ToUInt16(data, i * UnclaimedItemSize), i => BitConverter.ToUInt32(data, i * UnclaimedItemSize + sizeof(ushort)));
            });
            MultiValueDictionary<UInt256, ushort> unspent_votes = new MultiValueDictionary<UInt256, ushort>(p =>
            {
                Slice value = new byte[0];
                db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.IX_Vote).Add(p), out value);
                return new HashSet<ushort>(value.ToArray().GetUInt16Array());
            });
            Dictionary<UInt256, Fixed8> quantities = new Dictionary<UInt256, Fixed8>();
            WriteBatch batch = new WriteBatch();
            long amount_sysfee = GetSysFeeAmount(block.PrevBlock) + (long)block.Transactions.Sum(p => p.SystemFee);
            batch.Put(SliceBuilder.Begin(DataEntryPrefix.DATA_Block).Add(block.Hash), SliceBuilder.Begin().Add(amount_sysfee).Add(block.Trim()));
            foreach (Transaction tx in block.Transactions)
            {
                batch.Put(SliceBuilder.Begin(DataEntryPrefix.DATA_Transaction).Add(tx.Hash), SliceBuilder.Begin().Add(block.Height).Add(tx.ToArray()));
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
                    case TransactionType.ClaimTransaction:
                        foreach (TransactionInput input in ((ClaimTransaction)tx).Claims)
                        {
                            unclaimed.Remove(input.PrevHash, input.PrevIndex);
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
                for (ushort index = 0; index < tx.Outputs.Length; index++)
                {
                    unspents.Add(tx.Hash, index);
                }
            }
            foreach (var group in block.Transactions.SelectMany(p => p.GetAllInputs()).GroupBy(p => p.PrevHash))
            {
                int height;
                Transaction tx = GetTransaction(ReadOptions.Default, group.Key, out height);
                foreach (TransactionInput input in group)
                {
                    if (input.PrevIndex == 0)
                    {
                        batch.Delete(SliceBuilder.Begin(DataEntryPrefix.IX_Enrollment).Add(input.PrevHash));
                    }
                    unspents.Remove(input.PrevHash, input.PrevIndex);
                    unspent_votes.Remove(input.PrevHash, input.PrevIndex);
                    if (tx?.Outputs[input.PrevIndex].AssetId == AntShare.Hash)
                    {
                        unclaimed.Add(input.PrevHash, input.PrevIndex, block.Height);
                    }
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
            foreach (var spent in unclaimed)
            {
                if (spent.Value.Count == 0)
                {
                    batch.Delete(SliceBuilder.Begin(DataEntryPrefix.IX_Unclaimed).Add(spent.Key));
                }
                else
                {
                    using (MemoryStream ms = new MemoryStream(spent.Value.Count * UnclaimedItemSize))
                    using (BinaryWriter w = new BinaryWriter(ms))
                    {
                        foreach (var pair in spent.Value)
                        {
                            w.Write(pair.Key);
                            w.Write(pair.Value);
                        }
                        w.Flush();
                        batch.Put(SliceBuilder.Begin(DataEntryPrefix.IX_Unclaimed).Add(spent.Key), ms.ToArray());
                    }
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
                new_block_event.WaitOne();
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
            }
        }

        /*由于unclaimed无法恢复，所以Rollback没有办法实现，除非对每个交易输出都建立索引。
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
                Block block = GetBlockInternal(ReadOptions.Default, current);
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
                int height;
                Transaction tx = GetTransaction(ReadOptions.Default, group.Key, out height);
                Slice value = new byte[0];
                db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.IX_Unspent).Add(tx.Hash), out value);
                IEnumerable<ushort> indexes = value.ToArray().GetUInt16Array().Union(group.Select(p => p.PrevIndex));
                batch.Put(SliceBuilder.Begin(DataEntryPrefix.IX_Unspent).Add(tx.Hash), indexes.ToByteArray());
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
        }*/
    }
}
