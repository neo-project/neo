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

        internal readonly ColumnFamilyHandle DATA_Block;
        internal readonly ColumnFamilyHandle DATA_Transaction;

        internal readonly ColumnFamilyHandle ST_Contract;
        internal readonly ColumnFamilyHandle ST_Storage;

        internal readonly ColumnFamilyHandle IX_HeaderHashList;
        internal readonly ColumnFamilyHandle IX_CurrentBlock;
        internal readonly ColumnFamilyHandle IX_CurrentHeader;

        internal readonly ColumnFamilyHandle SYS_Version;

        internal readonly ColumnFamilyHandle DefaultFamily;

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

            DATA_Block = PrefixToFamily(DATA_Block_Name);
            DATA_Transaction = PrefixToFamily(DATA_Transaction_Name);

            IX_CurrentBlock = PrefixToFamily(IX_CurrentBlock_Name);
            IX_CurrentHeader = PrefixToFamily(IX_CurrentHeader_Name);
            IX_HeaderHashList = PrefixToFamily(IX_HeaderHashList_Name);

            ST_Contract = PrefixToFamily(ST_Contract_Name);
            ST_Storage = PrefixToFamily(ST_Storage_Name);
            SYS_Version = PrefixToFamily(SYS_Version_Name);

            DefaultFamily = _rocksDb.GetDefaultColumnFamily();
        }

        /// <summary>
        /// Create or get the family
        /// </summary>
        /// <param name="name">Name</param>
        /// <returns>Return column family</returns>
        internal ColumnFamilyHandle PrefixToFamily(string name)
        {
            try
            {
                // Try open
                return _rocksDb.GetColumnFamily(name);
            }
            catch (Exception e)
            {
                if (e is RocksDbSharpException || e is KeyNotFoundException)
                {
                    return _rocksDb.CreateColumnFamily(new ColumnFamilyOptions(), name);
                }

                throw e;
            }
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

        public void Delete(ColumnFamilyHandle family, WriteOptions options, byte[] key)
        {
            _rocksDb.Remove(key, family, options);
        }

        public byte[] Get(ColumnFamilyHandle family, ReadOptions options, byte[] key)
        {
            var value = _rocksDb.Get(key, family, options);

            if (value == null)
                throw new RocksDbSharpException("not found");

            return value;
        }

        public bool TryGet(ColumnFamilyHandle family, ReadOptions options, byte[] key, out byte[] value)
        {
            value = _rocksDb.Get(key, family, options);
            return value != null;
        }

        public void Put(ColumnFamilyHandle family, WriteOptions options, byte[] key, byte[] value)
        {
            _rocksDb.Put(key, value, family, options);
        }

        public Snapshot GetSnapshot()
        {
            return _rocksDb.CreateSnapshot();
        }

        public Iterator NewIterator(ColumnFamilyHandle family, ReadOptions options)
        {
            return _rocksDb.NewIterator(family, options);
        }

        public void Write(WriteOptions options, WriteBatch batch)
        {
            _rocksDb.Write(batch, options);
        }
    }
}
