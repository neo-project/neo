using Neo.Implementations.Blockchains.Utilities;

namespace Neo.Implementations.Blockchains.LiteDB
{
    internal class ReadOptions : AbstractReadOptions
    {
        public bool VerifyChecksums
        {
            set
            {
            }
        }

        public override bool FillCache
        {
            set
            {
            }
        }

        public override AbstractSnapshot Snapshot
        {
            set
            {
            }
        }
    }
}
