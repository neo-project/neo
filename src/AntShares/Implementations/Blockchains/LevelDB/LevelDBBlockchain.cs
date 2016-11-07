using AntShares.Core;
using AntShares.IO;
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
        private List<UInt256> header_index = new List<UInt256>();
        private Dictionary<UInt256, Header> header_cache = new Dictionary<UInt256, Header>();
        private Dictionary<UInt256, Block> block_cache = new Dictionary<UInt256, Block>();
        private uint current_block_height = 0;
        private uint stored_header_count = 0;
        private AutoResetEvent new_block_event = new AutoResetEvent(false);
        private bool disposed = false;

        public override BlockchainAbility Ability => BlockchainAbility.All;
        public override UInt256 CurrentBlockHash => header_index[(int)current_block_height];
        public override UInt256 CurrentHeaderHash => header_index[header_index.Count - 1];
        public override uint HeaderHeight => (uint)header_index.Count - 1;
        public override uint Height => current_block_height;
        public override bool IsReadOnly => false;
        public bool VerifyBlocks { get; set; } = true;

        public LevelDBBlockchain(string path)
        {
            header_index.Add(GenesisBlock.Hash);
            Version version;
            Slice value;
            db = DB.Open(path, new Options { CreateIfMissing = true });
            if (db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.CFG_Version), out value) && Version.TryParse(value.ToString(), out version) && version >= Version.Parse("0.6.6043.32131"))
            {
                ReadOptions options = new ReadOptions { FillCache = false };
                value = db.Get(options, SliceBuilder.Begin(DataEntryPrefix.SYS_CurrentBlock));
                UInt256 current_header_hash = new UInt256(value.ToArray().Take(32).ToArray());
                this.current_block_height = BitConverter.ToUInt32(value.ToArray(), 32);
                uint current_header_height = current_block_height;
                if (db.TryGet(options, SliceBuilder.Begin(DataEntryPrefix.SYS_CurrentHeader), out value))
                {
                    current_header_hash = new UInt256(value.ToArray().Take(32).ToArray());
                    current_header_height = BitConverter.ToUInt32(value.ToArray(), 32);
                }
                foreach (UInt256 hash in db.Find(options, SliceBuilder.Begin(DataEntryPrefix.IX_HeaderHashList), (k, v) =>
                {
                    using (MemoryStream ms = new MemoryStream(v.ToArray(), false))
                    using (BinaryReader r = new BinaryReader(ms))
                    {
                        return new
                        {
                            Index = BitConverter.ToUInt32(k.ToArray(), 1),
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
                    Header[] headers = db.Find(options, SliceBuilder.Begin(DataEntryPrefix.DATA_Header), (k, v) => Header.FromTrimmedData(v.ToArray(), sizeof(long))).OrderBy(p => p.Height).ToArray();
                    for (int i = 1; i < headers.Length; i++)
                    {
                        header_index.Add(headers[i].Hash);
                    }
                }
                else if (current_header_height >= stored_header_count)
                {
                    for (UInt256 hash = current_header_hash; hash != header_index[(int)stored_header_count - 1];)
                    {
                        Header header = Header.FromTrimmedData(db.Get(options, SliceBuilder.Begin(DataEntryPrefix.DATA_Header).Add(hash)).ToArray(), sizeof(long));
                        header_index.Insert((int)stored_header_count, hash);
                        hash = header.PrevBlock;
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
                db.Put(WriteOptions.Default, SliceBuilder.Begin(DataEntryPrefix.CFG_Version), GetType().GetTypeInfo().Assembly.GetName().Version.ToString());
            }
            thread_persistence = new Thread(PersistBlocks);
            thread_persistence.Name = "LevelDBBlockchain.PersistBlocks";
            thread_persistence.Start();
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
            lock (header_index)
            {
                if (block.Height - 1 >= header_index.Count) return false;
                if (block.Height == header_index.Count)
                {
                    if (VerifyBlocks && !block.Verify()) return false;
                    WriteBatch batch = new WriteBatch();
                    OnAddHeader(block.Header, batch);
                    db.Write(WriteOptions.Default, batch);
                }
                if (block.Height < header_index.Count)
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
                        if (header.Height - 1 >= header_index.Count) break;
                        if (header.Height < header_index.Count) continue;
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
            if (base.ContainsBlock(hash)) return true;
            return GetHeader(hash)?.Height <= current_block_height;
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
            lock (header_index)
            {
                if (header_index.Count <= height) return null;
                return header_index[(int)height];
            }
        }

        private Block GetBlockInternal(ReadOptions options, UInt256 hash)
        {
            Slice value;
            if (!db.TryGet(options, SliceBuilder.Begin(DataEntryPrefix.DATA_Header).Add(hash), out value))
                return null;
            int height;
            Block block = Block.FromTrimmedData(value.ToArray(), sizeof(long), p => GetTransaction(options, p, out height));
            if (block.Transactions.Length == 0) return null;
            return block;
        }

        public override byte[] GetContract(UInt160 hash)
        {
            Slice value;
            if (!db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.DATA_Contract).Add(hash), out value))
                return null;
            return value.ToArray();
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

        public override Header GetHeader(uint height)
        {
            Header header = base.GetHeader(height);
            if (header != null) return header;
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
            Header header = base.GetHeader(hash);
            if (header != null) return header;
            lock (header_cache)
            {
                if (header_cache.ContainsKey(hash))
                    return header_cache[hash];
            }
            Slice value;
            if (!db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.DATA_Header).Add(hash), out value))
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
                if (header.Height + 1 >= header_index.Count)
                    return null;
                return header_index[(int)header.Height + 1];
            }
        }

        public override Fixed8 GetQuantityIssued(UInt256 asset_id)
        {
            Slice quantity;
            if (!db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.ST_QuantityIssued).Add(asset_id), out quantity))
                quantity = 0L;
            return new Fixed8(quantity.ToInt64());
        }

        public override long GetSysFeeAmount(UInt256 hash)
        {
            Slice value;
            if (!db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.DATA_Header).Add(hash), out value))
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
                    Transaction tx = GetTransaction(options, hash, out height);
                    yield return new Vote
                    {
                        Enrollments = tx.Attributes.Where(p => p.Usage == TransactionAttributeUsage.Vote).Select(p => new UInt256(p.Data)).ToArray(),
                        Count = indexes.Sum(p => tx.Outputs[p].Value)
                    };
                }
            }
            foreach (Transaction tx in others)
            {
                UInt256[] enrollments = tx.Attributes.Where(p => p.Usage == TransactionAttributeUsage.Vote).Select(p => new UInt256(p.Data)).ToArray();
                if (enrollments.Length == 0) continue;
                yield return new Vote
                {
                    Enrollments = enrollments,
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

        private void OnAddHeader(Header header, WriteBatch batch)
        {
            header_index.Add(header.Hash);
            while ((int)header.Height - 2000 >= stored_header_count)
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
            batch.Put(SliceBuilder.Begin(DataEntryPrefix.DATA_Header).Add(header.Hash), SliceBuilder.Begin().Add(0L).Add(header.ToArray()));
            batch.Put(SliceBuilder.Begin(DataEntryPrefix.SYS_CurrentHeader), SliceBuilder.Begin().Add(header.Hash).Add(header.Height));
        }

        private void Persist(Block block)
        {
            const int UnclaimedItemSize = sizeof(ushort) + sizeof(uint);
            MultiValueDictionary<UInt256, ushort> unspents = new MultiValueDictionary<UInt256, ushort>(p =>
            {
                Slice value;
                if (!db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.IX_Unspent).Add(p), out value))
                    value = new byte[0];
                return new HashSet<ushort>(value.ToArray().GetUInt16Array());
            });
            MultiValueDictionary<UInt256, ushort, uint> unclaimed = new MultiValueDictionary<UInt256, ushort, uint>(p =>
            {
                Slice value;
                if (!db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.IX_Unclaimed).Add(p), out value))
                    value = new byte[0];
                byte[] data = value.ToArray();
                return Enumerable.Range(0, data.Length / UnclaimedItemSize).ToDictionary(i => BitConverter.ToUInt16(data, i * UnclaimedItemSize), i => BitConverter.ToUInt32(data, i * UnclaimedItemSize + sizeof(ushort)));
            });
            MultiValueDictionary<UInt256, ushort> unspent_votes = new MultiValueDictionary<UInt256, ushort>(p =>
            {
                Slice value;
                if (!db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.IX_Vote).Add(p), out value))
                    value = new byte[0];
                return new HashSet<ushort>(value.ToArray().GetUInt16Array());
            });
            Dictionary<UInt256, Fixed8> quantities = new Dictionary<UInt256, Fixed8>();
            WriteBatch batch = new WriteBatch();
            long amount_sysfee = GetSysFeeAmount(block.PrevBlock) + (long)block.Transactions.Sum(p => p.SystemFee);
            batch.Put(SliceBuilder.Begin(DataEntryPrefix.DATA_Header).Add(block.Hash), SliceBuilder.Begin().Add(amount_sysfee).Add(block.Trim()));
            foreach (Transaction tx in block.Transactions)
            {
                batch.Put(SliceBuilder.Begin(DataEntryPrefix.DATA_Transaction).Add(tx.Hash), SliceBuilder.Begin().Add(block.Height).Add(tx.ToArray()));
                if (tx.Attributes.Any(p => p.Usage == TransactionAttributeUsage.Vote))
                {
                    unspent_votes.AddEmpty(tx.Hash);
                    for (ushort index = 0; index < tx.Outputs.Length; index++)
                    {
                        if (tx.Outputs[index].AssetId == AntShare.Hash)
                        {
                            unspent_votes.Add(tx.Hash, index);
                        }
                    }
                }
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
                    case TransactionType.PublishTransaction:
                        foreach (byte[] script in ((PublishTransaction)tx).Contracts)
                        {
                            batch.Put(SliceBuilder.Begin(DataEntryPrefix.DATA_Contract).Add(script.ToScriptHash()), script);
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
            batch.Put(SliceBuilder.Begin(DataEntryPrefix.SYS_CurrentBlock), SliceBuilder.Begin().Add(block.Hash).Add(block.Height));
            // There's a bug in .Net Core.
            // When calling DB.Write(), it will throw LevelDBException sometimes.
            // But when you try to catch the exception, the bug disappears.
            // We shall remove the "try...catch" clause when Microsoft fix the bug.
            byte retry = 0;
            while (true)
            {
                try
                {
                    db.Write(WriteOptions.Default, batch);
                    break;
                }
                catch (LevelDBException ex)
                {
                    if (++retry >= 4) throw;
                    File.AppendAllText("leveldb.log", ex.Message + "\r\n");
                }
            }
            current_block_height = block.Height;
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
