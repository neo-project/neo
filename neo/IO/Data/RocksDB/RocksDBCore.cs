using RocksDbSharp;
using System;
using System.Collections.Generic;

namespace Neo.IO.Data.RocksDB
{
    public class RocksDBCore : IDisposable
    {
        public static readonly Options OptionsDefault = new Options();
        public static readonly ReadOptions ReadDefault = new ReadOptions();
        public static readonly WriteOptions WriteDefault = new WriteOptions();
        public static readonly WriteOptions WriteDefaultSync = new WriteOptions();

        #region Families

        private const string DATA_Block_Name = "Block";
        private const string DATA_Transaction_Name = "Transaction";

        private const string ST_Contract_Name = "Contract";
        private const string ST_Storage_Name = "Storage";

        private const string IX_HeaderHashList_Name = "HeaderHashList";
        private const string IX_CurrentBlock_Name = "CurrentBlock";
        private const string IX_CurrentHeader_Name = "CurrentHeader";

        private const string SYS_Version_Name = "Version";

        internal readonly ColumnFamily DATA_Block;
        internal readonly ColumnFamily DATA_Transaction;

        internal readonly ColumnFamily ST_Contract;
        internal readonly ColumnFamily ST_Storage;

        internal readonly ColumnFamily IX_HeaderHashList;
        internal readonly ColumnFamily IX_CurrentBlock;
        internal readonly ColumnFamily IX_CurrentHeader;

        internal readonly ColumnFamily SYS_Version;

        internal readonly ColumnFamily DefaultFamily;

        #endregion

        static RocksDBCore()
        {
            WriteDefaultSync.SetSync(true);
        }

        private readonly RocksDb _rocksDb;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="db">Database</param>
        private RocksDBCore(RocksDb db)
        {
            _rocksDb = db ?? throw new NullReferenceException(nameof(db));

            // Get column families

            DATA_Block = new ColumnFamily(db, DATA_Block_Name);
            DATA_Transaction = new ColumnFamily(db, DATA_Transaction_Name);

            IX_CurrentBlock = new ColumnFamily(db, IX_CurrentBlock_Name);
            IX_CurrentHeader = new ColumnFamily(db, IX_CurrentHeader_Name);
            IX_HeaderHashList = new ColumnFamily(db, IX_HeaderHashList_Name);

            ST_Contract = new ColumnFamily(db, ST_Contract_Name);
            ST_Storage = new ColumnFamily(db, ST_Storage_Name);
            SYS_Version = new ColumnFamily(db, SYS_Version_Name);

            DefaultFamily = new ColumnFamily("", _rocksDb.GetDefaultColumnFamily());
        }

        /// <summary>
        /// Open database
        /// </summary>
        /// <returns>DB</returns>
        public static RocksDBCore Open()
        {
            return Open(OptionsDefault);
        }

        /// <summary>
        /// Open database
        /// </summary>
        /// <param name="config">Configuration</param>
        /// <returns>DB</returns>
        public static RocksDBCore Open(Options config)
        {
            var families = new ColumnFamilies
            {
                { DATA_Block_Name, new ColumnFamilyOptions() },
                { DATA_Transaction_Name, new ColumnFamilyOptions() },

                { IX_CurrentBlock_Name, new ColumnFamilyOptions() },
                { IX_CurrentHeader_Name, new ColumnFamilyOptions() },
                { IX_HeaderHashList_Name, new ColumnFamilyOptions() },

                { ST_Contract_Name, new ColumnFamilyOptions() },
                { ST_Storage_Name, new ColumnFamilyOptions() },

                { SYS_Version_Name, new ColumnFamilyOptions() }
            };

            return new RocksDBCore(RocksDb.Open(config.Build(), config.FilePath, families));
        }

        /// <summary>
        /// Free resources
        /// </summary>
        public void Dispose()
        {
            _rocksDb.Dispose();
        }

        public void Delete(ColumnFamily family, WriteOptions options, byte[] key)
        {
            _rocksDb.Remove(key, family.Handle, options);
        }

        public byte[] Get(ColumnFamily family, ReadOptions options, byte[] key)
        {
            var value = _rocksDb.Get(key, family.Handle, options);

            if (value == null)
                throw new RocksDbSharpException("not found");

            return value;
        }

        public bool TryGet(ColumnFamily family, ReadOptions options, byte[] key, out byte[] value)
        {
            value = _rocksDb.Get(key, family.Handle, options);
            return value != null;
        }

        public void Put(ColumnFamily family, WriteOptions options, byte[] key, byte[] value)
        {
            _rocksDb.Put(key, value, family.Handle, options);
        }

        public Snapshot GetSnapshot()
        {
            return _rocksDb.CreateSnapshot();
        }

        public Iterator NewIterator(ColumnFamily family, ReadOptions options)
        {
            return _rocksDb.NewIterator(family.Handle, options);
        }

        public void Write(WriteOptions options, WriteBatch batch)
        {
            _rocksDb.Write(batch, options);
        }

        public void Clear(ColumnFamily familiy)
        {
            // Drop the column family
            _rocksDb.DropColumnFamily(familiy.Name);
            // The handle is invalid now, require to obtains a new column family
            familiy.Handle = new ColumnFamily(_rocksDb, familiy.Name).Handle;
        }
    }
}
