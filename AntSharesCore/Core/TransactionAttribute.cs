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
            int length;
            switch (Usage)
            {
                case TransactionAttributeUsage.ContractHash:
                case TransactionAttributeUsage.ECDH02:
                case TransactionAttributeUsage.ECDH03:
                    length = 32;
                    break;
                case TransactionAttributeUsage.LockAfter:
                case TransactionAttributeUsage.LockBefore:
                    length = 4;
                    break;
                default:
                    if (Usage >= TransactionAttributeUsage.Remark)
                        length = reader.ReadByte();
                    else
                        throw new FormatException();
                    break;
            }
            this.Data = reader.ReadBytes(length);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Usage);
            writer.Write(Data);
        }
    }
}
