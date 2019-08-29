using RocksDbSharp;
using System;
using System.Collections.Generic;

namespace Neo.IO.Data.RocksDB
{
    public class ColumnFamily
    {
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Column family handle
        /// </summary>
        public ColumnFamilyHandle Handle { get; internal set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="handle">Handle</param>
        public ColumnFamily(string name, ColumnFamilyHandle handle)
        {
            Name = name;
            Handle = handle;
        }

        /// <summary>
        /// Get or create the column family
        /// </summary>
        /// <param name="db">DB</param>
        /// <param name="name">Name</param>
        /// <returns>Return column family</returns>
        internal ColumnFamily(RocksDb db, string name)
        {
            Name = name;
            try
            {
                // Try open
                Handle = db.GetColumnFamily(name);
            }
            catch (Exception e)
            {
                if (e is RocksDbSharpException || e is KeyNotFoundException)
                {
                    Handle = db.CreateColumnFamily(new ColumnFamilyOptions(), name);
                    return;
                }

                throw e;
            }
        }

        /// <summary>
        /// String representation
        /// </summary>
        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
