using Neo.IO.Caching;
using Neo.IO.Data.LevelDB;
using Neo.IO.Wrappers;
using Neo.Ledger;
using LSnapshot = Neo.IO.Data.LevelDB.Snapshot;

namespace Neo.Persistence.LevelDB
{
    internal class DbSnapshot : Snapshot
    {
        private readonly LevelDBCore db;
        private readonly LSnapshot snapshot;
        private readonly WriteBatch batch;

        public override DataCache<UInt256, TrimmedBlock> Blocks { get; }
        public override DataCache<UInt256, TransactionState> Transactions { get; }
        public override DataCache<UInt160, ContractState> Contracts { get; }
        public override DataCache<StorageKey, StorageItem> Storages { get; }
        public override DataCache<UInt32Wrapper, HeaderHashList> HeaderHashList { get; }
        public override MetaDataCache<HashIndexState> BlockHashIndex { get; }
        public override MetaDataCache<HashIndexState> HeaderHashIndex { get; }

        public DbSnapshot(LevelDBCore db)
        {
            this.db = db;
            this.snapshot = db.GetSnapshot();
            this.batch = new WriteBatch();
            ReadOptions options = new ReadOptions { FillCache = false, Snapshot = snapshot };
            Blocks = new DbCache<UInt256, TrimmedBlock>(db, Prefixes.DATA_Block, options, batch);
            Transactions = new DbCache<UInt256, TransactionState>(db, Prefixes.DATA_Transaction, options, batch);
            Contracts = new DbCache<UInt160, ContractState>(db, Prefixes.ST_Contract, options, batch);
            Storages = new DbCache<StorageKey, StorageItem>(db, Prefixes.ST_Storage, options, batch);
            HeaderHashList = new DbCache<UInt32Wrapper, HeaderHashList>(db, Prefixes.IX_HeaderHashList, options, batch);
            BlockHashIndex = new DbMetaDataCache<HashIndexState>(db, Prefixes.IX_CurrentBlock, options, batch);
            HeaderHashIndex = new DbMetaDataCache<HashIndexState>(db, Prefixes.IX_CurrentHeader, options, batch);
        }

        public override void Commit()
        {
            base.Commit();
            db.Write(WriteOptions.Default, batch);
        }

        public override void Dispose()
        {
            snapshot.Dispose();
        }
    }
}
