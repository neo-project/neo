using AntShares.Core;
using LevelDB;
using System;
using System.Linq;

namespace AntShares.Data
{
    internal static class Helper
    {
        public static Slice IndexKey(this RegisterTransaction tx)
        {
            return new byte[] { (byte)DataEntryPrefix.IX_Register, (byte)tx.RegisterType }.Concat(tx.Hash.ToArray()).ToArray();
        }

        public static Slice Key(this Block block)
        {
            return new byte[] { (byte)DataEntryPrefix.Block }.Concat(block.Hash.ToArray()).ToArray();
        }

        public static Slice Key(this Transaction tx)
        {
            return new byte[] { (byte)DataEntryPrefix.Transaction }.Concat(tx.Hash.ToArray()).ToArray();
        }

        public static Slice UnspentKey(this Transaction tx, ushort index)
        {
            return new byte[] { (byte)DataEntryPrefix.Unspent }.Concat(tx.Hash.ToArray()).Concat(BitConverter.GetBytes(index)).ToArray();
        }

        public static Slice UnspentKey(this TransactionInput tx_in)
        {
            return new byte[] { (byte)DataEntryPrefix.Unspent }.Concat(tx_in.PrevTxId.ToArray()).Concat(BitConverter.GetBytes(tx_in.PrevIndex)).ToArray();
        }
    }
}
