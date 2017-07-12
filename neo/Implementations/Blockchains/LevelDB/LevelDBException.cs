using System.Data.Common;

namespace Neo.Implementations.Blockchains.LevelDB
{
    internal class LevelDBException : DbException
    {
        internal LevelDBException(string message)
            : base(message)
        {
        }
    }
}
