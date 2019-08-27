using Neo.Persistence;
using RocksDbSharp;
using System;
using System.Collections.Generic;

namespace Neo.IO.Data.RocksDB
{
    public class DB : IDisposable
    {
        public static readonly Options OptionsDefault = new Options();
        public static readonly ReadOptions ReadDefault = new ReadOptions();
        public static readonly WriteOptions WriteDefault = new WriteOptions();
        public static readonly WriteOptions WriteDefaultSync = new WriteOptions();

        #region Families

        public ColumnFamilyHandle DATA_Block;
        public ColumnFamilyHandle DATA_Transaction;

        public ColumnFamilyHandle ST_Contract;
        public ColumnFamilyHandle ST_Storage;

        public ColumnFamilyHandle IX_HeaderHashList;
        public ColumnFamilyHandle IX_CurrentBlock;
        public ColumnFamilyHandle IX_CurrentHeader;

        public ColumnFamilyHandle SYS_Version;

        public ColumnFamilyHandle DefaultFamily;

        #endregion

        static DB()
        {
            WriteDefaultSync.SetSync(true);
        }

        private readonly RocksDb _rocksDb;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="db">Database</param>
        private DB(RocksDb db)
        {
            _rocksDb = db;

            // Get column families

            DATA_Block = PrefixToFamily(Prefixes.DATA_Block);
            DATA_Transaction = PrefixToFamily(Prefixes.DATA_Transaction);

            IX_CurrentBlock = PrefixToFamily(Prefixes.IX_CurrentBlock);
            IX_CurrentHeader = PrefixToFamily(Prefixes.IX_CurrentHeader);
            IX_HeaderHashList = PrefixToFamily(Prefixes.IX_HeaderHashList);

            ST_Contract = PrefixToFamily(Prefixes.ST_Contract);
            ST_Storage = PrefixToFamily(Prefixes.ST_Storage);
            SYS_Version = PrefixToFamily(Prefixes.SYS_Version);

            DefaultFamily = _rocksDb.GetDefaultColumnFamily();
        }

        /// <summary>
        /// Create or get the family
        /// </summary>
        /// <param name="prefix">Prefix</param>
        /// <returns>Return column family</returns>
        internal ColumnFamilyHandle PrefixToFamily(byte prefix)
        {
            try
            {
                // Try open
                return _rocksDb.GetColumnFamily(prefix.ToString("x2"));
            }
            catch (Exception e)
            {
                if (e is RocksDbSharpException || e is KeyNotFoundException)
                {
                    return _rocksDb.CreateColumnFamily(new ColumnFamilyOptions(), prefix.ToString("x2"));
                }

                throw e;
            }
        }

        /// <summary>
        /// Open database
        /// </summary>
        /// <returns>DB</returns>
        public static DB Open()
        {
            return Open(OptionsDefault);
        }

        /// <summary>
        /// Open database
        /// </summary>
        /// <param name="config">Configuration</param>
        /// <returns>DB</returns>
        public static DB Open(Options config)
        {
            var families = new ColumnFamilies();

            families.Add(Prefixes.DATA_Block.ToString("x2"), new ColumnFamilyOptions());
            families.Add(Prefixes.DATA_Transaction.ToString("x2"), new ColumnFamilyOptions());

            families.Add(Prefixes.IX_CurrentBlock.ToString("x2"), new ColumnFamilyOptions());
            families.Add(Prefixes.IX_CurrentHeader.ToString("x2"), new ColumnFamilyOptions());
            families.Add(Prefixes.IX_HeaderHashList.ToString("x2"), new ColumnFamilyOptions());

            families.Add(Prefixes.ST_Contract.ToString("x2"), new ColumnFamilyOptions());
            families.Add(Prefixes.ST_Storage.ToString("x2"), new ColumnFamilyOptions());
            families.Add(Prefixes.SYS_Version.ToString("x2"), new ColumnFamilyOptions());

            return new DB(RocksDb.Open(config.Build(), config.FilePath, families));
        }

        /// <summary>
        /// Free resources
        /// </summary>
        public void Dispose()
        {
            _rocksDb?.Dispose();
        }

        public void Delete(ColumnFamilyHandle family, WriteOptions options, Slice key)
        {
            _rocksDb.Remove(key.ToArray(), family, options);
        }

        public byte[] Get(ColumnFamilyHandle family, ReadOptions options, Slice key)
        {
            var value = _rocksDb.Get(key.ToArray(), family, options);

            if (value == null)
                throw new RocksDbSharpException("not found");

            return value;
        }

        public bool TryGet(ColumnFamilyHandle family, ReadOptions options, Slice key, out byte[] value)
        {
            value = _rocksDb.Get(key.ToArray(), family, options);
            return value != null;
        }

        public void Put(ColumnFamilyHandle family, WriteOptions options, Slice key, byte[] value)
        {
            _rocksDb.Put(key.ToArray(), value, family, options);
        }

        public RocksDbSharp.Snapshot GetSnapshot()
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
