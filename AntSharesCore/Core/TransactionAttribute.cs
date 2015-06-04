using AntShares.IO;
using System;
using System.IO;

namespace AntShares.Core
{
    public class TransactionAttribute : ISerializable
    {
        public TransactionAttributeUsage Usage;
        public byte[] Data;

        void ISerializable.Deserialize(BinaryReader reader)
        {
            this.Usage = (TransactionAttributeUsage)reader.ReadByte();
            if (!Enum.IsDefined(typeof(TransactionAttributeUsage), Usage))
                throw new FormatException();
            //Data的长度由Usage决定，目前只有32字节一种情况
            this.Data = reader.ReadBytes(32);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Usage);
            writer.Write(Data);
        }
    }
}
