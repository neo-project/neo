using AntShares.IO;
using System;
using System.IO;

namespace AntShares.Core
{
    public class SplitOrder : ISerializable
    {
        public Fixed8 Amount;
        public Fixed8 Price;
        public UInt160 Client;

        void ISerializable.Deserialize(BinaryReader reader)
        {
            this.Amount = reader.ReadSerializable<Fixed8>();
            if (Amount == Fixed8.Zero) throw new FormatException();
            if (Amount.GetData() % 10000 != 0) throw new FormatException();
            this.Price = reader.ReadSerializable<Fixed8>();
            if (Price <= Fixed8.Zero) throw new FormatException();
            if (Price.GetData() % 10000 != 0) throw new FormatException();
            this.Client = reader.ReadSerializable<UInt160>();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Amount);
            writer.Write(Price);
            writer.Write(Client);
        }
    }
}
