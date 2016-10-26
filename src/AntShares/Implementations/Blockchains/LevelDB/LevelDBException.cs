using System.Data.Common;

namespace AntShares.Implementations.Blockchains.LevelDB
{
    internal class LevelDBException : DbException
    {
        internal LevelDBException(string message)
            : base(message)
        {
        }
    }
}
