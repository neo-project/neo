using RocksDbSharp;

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
        /// String representation
        /// </summary>
        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
