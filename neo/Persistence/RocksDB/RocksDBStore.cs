using Neo.IO.Caching;
using Neo.IO.Data;
using Neo.IO.Data.RocksDB;
using Neo.IO.Wrappers;
using Neo.Ledger;
using RocksDbSharp;
using System;
using System.Reflection;
using System.Text;

namespace Neo.Persistence.RocksDB
{
    public class RocksDBStore : Store, IDisposable
    {
        private readonly DB db;

        public RocksDBStore(string path)
        {
            db = DB.Open(new Options { CreateIfMissing = true, FilePath = path });

            if (db.TryGet(db.SYS_Version, DB.ReadDefault, new byte[0], out var value) &&
                Version.TryParse(Encoding.UTF8.GetString(value), out var version) && version >= Version.Parse("2.9.1"))
                return;

            using (var batch = new WriteBatch())
            {
                var options = new ReadOptions();
                options.SetFillCache(false);

                // Clean entries

                using (var it = db.NewIterator(db.SYS_Version, options))
                {
                    for (it.SeekToFirst(); it.Valid(); it.Next())
                    {
                        batch.Delete(it.Key());
                    }
                }

                // Update version

                db.Put(db.SYS_Version, DB.WriteDefault, new byte[0], Encoding.UTF8.GetBytes(Assembly.GetExecutingAssembly().GetName().Version.ToString()));
                db.Write(DB.WriteDefault, batch);
            }
        }

        public void Dispose()
        {
            db.Dispose();
        }

        public override DataCache<UInt256, TrimmedBlock> GetBlocks()
        {
            return new DbCache<UInt256, TrimmedBlock>(db, null, null, db.DATA_Block);
        }

        public override DataCache<UInt160, ContractState> GetContracts()
        {
            return new DbCache<UInt160, ContractState>(db, null, null, db.ST_Contract);
        }

        public override Snapshot GetSnapshot()
        {
            return new DbSnapshot(db);
        }

        public override DataCache<StorageKey, StorageItem> GetStorages()
        {
            return new DbCache<StorageKey, StorageItem>(db, null, null, db.ST_Storage);
        }

        public override DataCache<UInt256, TransactionState> GetTransactions()
        {
            return new DbCache<UInt256, TransactionState>(db, null, null, db.DATA_Transaction);
        }

        public override DataCache<UInt32Wrapper, HeaderHashList> GetHeaderHashList()
        {
            return new DbCache<UInt32Wrapper, HeaderHashList>(db, null, null, db.IX_HeaderHashList);
        }

        public override MetaDataCache<HashIndexState> GetBlockHashIndex()
        {
            return new DbMetaDataCache<HashIndexState>(db, null, null, db.IX_CurrentBlock);
        }

        public override MetaDataCache<HashIndexState> GetHeaderHashIndex()
        {
            return new DbMetaDataCache<HashIndexState>(db, null, null, db.IX_CurrentHeader);
        }

        public override byte[] Get(byte prefix, byte[] key)
        {
            if (!db.TryGet(db.DefaultFamily, DB.ReadDefault, SliceBuilder.Begin(prefix).Add(key), out var value))
                return null;
            return value;
        }

        public override void Put(byte prefix, byte[] key, byte[] value)
        {
            db.Put(db.DefaultFamily, DB.WriteDefault, SliceBuilder.Begin(prefix).Add(key), value);
        }

        public override void PutSync(byte prefix, byte[] key, byte[] value)
        {
            db.Put(db.DefaultFamily, DB.WriteDefaultSync, SliceBuilder.Begin(prefix).Add(key), value);
        }
    }
}
