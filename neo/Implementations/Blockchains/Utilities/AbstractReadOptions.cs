using System;
using Neo.Implementations.Blockchains.Utilities;

namespace Neo.Implementations.Blockchains.Utilities
{
    public abstract class AbstractReadOptions
    {
		public abstract bool FillCache
		{
            set;
		}

		public abstract AbstractSnapshot Snapshot
        {
            set;
        }
    }
}
