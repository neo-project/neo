using System.Data.Common;

namespace Neo.IO.Data.LevelDB
{
    public class LevelDBException : DbException
    {
        internal LevelDBException(string message)
            : base(message)
        {
        }
    }
}
