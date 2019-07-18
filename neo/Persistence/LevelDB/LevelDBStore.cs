using LevelDB;
using Neo.IO.Caching;
using Neo.IO.Data.LevelDB;
using Neo.IO.Wrappers;
using Neo.Ledger;
using System;
using System.Reflection;
using System.Text;

namespace Neo.Persistence.LevelDB
{
    public class LevelDBStore : Store, IDisposable
    {
        private readonly DB db;

        public LevelDBStore(string path)
        {
            this.db = new DB(new Options { CreateIfMissing = true }, path);
            byte[] value = db.Get(new byte[] { Prefixes.SYS_Version }, new ReadOptions());
            if (value == null) return;
            if (Version.TryParse(Encoding.UTF8.GetString(value), out Version version) && version >= Version.Parse("2.9.1")) return;
            WriteBatch batch = new WriteBatch();
            ReadOptions options = new ReadOptions { FillCache = false };
            using (Iterator it = db.CreateIterator(options))
            {
                for (it.SeekToFirst(); it.IsValid(); it.Next())
                {
                    batch.Delete(it.Key());
                }
            }
            db.Put(new byte[] { Prefixes.SYS_Version }, Encoding.UTF8.GetBytes(Assembly.GetExecutingAssembly().GetName().Version.ToString()), new WriteOptions());
            db.Write(batch, new WriteOptions());
        }

        public void Dispose()
        {
            db.Dispose();
        }

        public override byte[] Get(byte prefix, byte[] key)
        {
            return db.Get(((Slice)SliceBuilder.Begin(prefix).Add(key)).ToArray(), new ReadOptions());
        }

        public override DataCache<UInt256, TrimmedBlock> GetBlocks()
        {
            return new DbCache<UInt256, TrimmedBlock>(db, null, null, Prefixes.DATA_Block);
        }

        public override DataCache<UInt160, ContractState> GetContracts()
        {
            return new DbCache<UInt160, ContractState>(db, null, null, Prefixes.ST_Contract);
        }

        public override Snapshot GetSnapshot()
        {
            return new DbSnapshot(db);
        }

        public override DataCache<StorageKey, StorageItem> GetStorages()
        {
            return new DbCache<StorageKey, StorageItem>(db, null, null, Prefixes.ST_Storage);
        }

        public override DataCache<UInt256, TransactionState> GetTransactions()
        {
            return new DbCache<UInt256, TransactionState>(db, null, null, Prefixes.DATA_Transaction);
        }

        public override DataCache<UInt32Wrapper, HeaderHashList> GetHeaderHashList()
        {
            return new DbCache<UInt32Wrapper, HeaderHashList>(db, null, null, Prefixes.IX_HeaderHashList);
        }

        public override MetaDataCache<HashIndexState> GetBlockHashIndex()
        {
            return new DbMetaDataCache<HashIndexState>(db, null, null, Prefixes.IX_CurrentBlock);
        }

        public override MetaDataCache<HashIndexState> GetHeaderHashIndex()
        {
            return new DbMetaDataCache<HashIndexState>(db, null, null, Prefixes.IX_CurrentHeader);
        }

        public override void Put(byte prefix, byte[] key, byte[] value)
        {
            db.Put(((Slice)SliceBuilder.Begin(prefix).Add(key)).ToArray(), value, new WriteOptions());
        }

        public override void PutSync(byte prefix, byte[] key, byte[] value)
        {
            db.Put(((Slice)SliceBuilder.Begin(prefix).Add(key)).ToArray(), value, new WriteOptions { Sync = true });
        }
    }
}
