using Neo.IO.Caching;
using Neo.IO.Data;
using Neo.IO.Data.LevelDB;
using Neo.IO.Wrappers;
using Neo.Ledger;
using System;
using System.Reflection;

namespace Neo.Persistence.LevelDB
{
    public class LevelDBStore : Store
    {
        private readonly LevelDBCore db;

        public LevelDBStore(string path)
        {
            this.db = LevelDBCore.Open(path, new Options { CreateIfMissing = true });
            if (db.TryGet(ReadOptions.Default, SliceBuilder.Begin(Prefixes.SYS_Version), out Slice value) && Version.TryParse(value.ToString(), out Version version) && version >= Version.Parse("2.9.1"))
                return;
            WriteBatch batch = new WriteBatch();
            ReadOptions options = new ReadOptions { FillCache = false };
            using (Iterator it = db.NewIterator(options))
            {
                for (it.SeekToFirst(); it.Valid(); it.Next())
                {
                    batch.Delete(it.Key());
                }
            }
            db.Put(WriteOptions.Default, SliceBuilder.Begin(Prefixes.SYS_Version), Assembly.GetExecutingAssembly().GetName().Version.ToString());
            db.Write(WriteOptions.Default, batch);
        }

        public override void Dispose()
        {
            db.Dispose();
        }

        public override DataCache<UInt256, TrimmedBlock> GetBlocks()
        {
            return new DbCache<UInt256, TrimmedBlock>(db, Prefixes.DATA_Block);
        }

        public override DataCache<UInt160, ContractState> GetContracts()
        {
            return new DbCache<UInt160, ContractState>(db, Prefixes.ST_Contract);
        }

        public override Snapshot GetSnapshot()
        {
            return new DbSnapshot(db);
        }

        public override DataCache<StorageKey, StorageItem> GetStorages()
        {
            return new DbCache<StorageKey, StorageItem>(db, Prefixes.ST_Storage);
        }

        public override DataCache<UInt256, TransactionState> GetTransactions()
        {
            return new DbCache<UInt256, TransactionState>(db, Prefixes.DATA_Transaction);
        }

        public override DataCache<UInt32Wrapper, HeaderHashList> GetHeaderHashList()
        {
            return new DbCache<UInt32Wrapper, HeaderHashList>(db, Prefixes.IX_HeaderHashList);
        }

        public override MetaDataCache<HashIndexState> GetBlockHashIndex()
        {
            return new DbMetaDataCache<HashIndexState>(db, Prefixes.IX_CurrentBlock);
        }

        public override MetaDataCache<HashIndexState> GetHeaderHashIndex()
        {
            return new DbMetaDataCache<HashIndexState>(db, Prefixes.IX_CurrentHeader);
        }

        public override byte[] Get(byte[] key)
        {
            if (!db.TryGet(ReadOptions.Default, key, out Slice slice))
                return null;
            return slice.ToArray();
        }

        public override void Put(byte[] key, byte[] value)
        {
            db.Put(WriteOptions.Default, key, value);
        }

        public override void PutSync(byte[] key, byte[] value)
        {
            db.Put(WriteOptions.DefaultSync, key, value);
        }
    }
}
