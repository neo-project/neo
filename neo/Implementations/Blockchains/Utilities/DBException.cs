using System.Data.Common;

namespace Neo.Implementations.Blockchains.Utilities
{
    internal class DBException : DbException
    {
        internal DBException(string message)
            : base(message)
        {
        }
    }
}
