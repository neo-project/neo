using AntShares.IO;
using System;
using System.IO;
using System.Linq;

namespace AntShares.Core
{
    public class UnspentCoinState : ISerializable
    {
        public const byte StateVersion = 0;
        public CoinState[] Items;

        int ISerializable.Size => sizeof(byte) + Items.GetVarSize();

        void ISerializable.Deserialize(BinaryReader reader)
        {
            if (reader.ReadByte() != StateVersion) throw new FormatException();
            Items = reader.ReadVarBytes().Select(p => (CoinState)p).ToArray();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(StateVersion);
            writer.WriteVarBytes(Items.Cast<byte>().ToArray());
        }
    }
}
