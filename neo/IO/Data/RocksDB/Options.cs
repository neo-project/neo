using RocksDbSharp;

namespace Neo.IO.Data.RocksDB
{
    public class Options
    {
        /// <summary>
        /// File Path
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// SetCreateIfMissing causes Open to create a new database on disk if it does not already exist.
        /// </summary>
        public bool CreateIfMissing { get; set; } = true;

        /// <summary>
        /// SetMaxOpenFiles sets the number of files than can be used at once by the database.
        /// </summary>
        public int MaxOpenFiles { get; set; } = 1000;

        /// <summary>
        /// SetErrorIfExists, if passed true, will cause the opening of a database that already exists to throw an error.
        /// </summary>
        public bool ErrorIfExists { get; set; } = false;

        /// <summary>
        /// SetBlockSize sets the approximate size of user data packed per block.
        /// The default is roughly 4096 uncompressed bytes. A better setting depends on your use case.
        /// </summary>
        public ulong BlockSize { get; set; } = 4096;

        /// <summary>
        /// SetParanoidChecks, when called with true, will cause the database to do
        /// aggressive checking of the data it is processing and will stop early if it detects errors.
        /// </summary>
        public bool ParanoidChecks { get; set; } = false;

        /// <summary>
        /// SetWriteBufferSize sets the number of bytes the database will build up in
        /// memory (backed by an unsorted log on disk) before converting to a sorted on-disk file.
        /// </summary>
        public ulong WriteBufferSize { get; set; } = 4 << 20;

        /// <summary>
        /// Build the current options
        /// </summary>
        public DbOptions Build()
        {
            var blockOpts = new BlockBasedTableOptions();

            blockOpts.SetBlockSize(BlockSize);

            var options = new DbOptions();

            options.SetCreateMissingColumnFamilies(true);
            options.SetCreateIfMissing(CreateIfMissing);
            options.SetErrorIfExists(ErrorIfExists);
            options.SetMaxOpenFiles(MaxOpenFiles);
            options.SetParanoidChecks(ParanoidChecks);
            options.SetWriteBufferSize(WriteBufferSize);
            options.SetBlockBasedTableFactory(blockOpts);

            return options;
        }
    }
}
