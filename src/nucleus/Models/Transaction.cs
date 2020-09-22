using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Neo.IO;

namespace Neo.Models
{
    public class Transaction : ISerializable
    {
        public const int MaxTransactionSize = 102400;
        public const uint MaxValidUntilBlockIncrement = 2102400;
        public const int MaxTransactionAttributes = 16;

        public byte Version;
        public uint Nonce;
        public long SystemFee;
        public long NetworkFee;
        public uint ValidUntilBlock;
        public Signer[] Signers;
        public TransactionAttribute[] Attributes;
        public byte[] Script;
        public Witness[] Witnesses;

        const int HeaderSize =
            sizeof(byte) +  //Version
            sizeof(uint) +  //Nonce
            sizeof(long) +  //SystemFee
            sizeof(long) +  //NetworkFee
            sizeof(uint);   //ValidUntilBlock

        public int Size => HeaderSize +
            Signers.GetVarSize() +
            IO.Extensions.GetVarSize(Attributes.Length) +
            Attributes.Sum(a => a.Size) +
            Attributes.GetVarSize() +
            Script.GetVarSize() +
            Witnesses.GetVarSize();

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Version = reader.ReadByte();
            if (Version > 0) throw new FormatException();
            Nonce = reader.ReadUInt32();
            SystemFee = reader.ReadInt64();
            if (SystemFee < 0) throw new FormatException();
            NetworkFee = reader.ReadInt64();
            if (NetworkFee < 0) throw new FormatException();
            if (SystemFee + NetworkFee < SystemFee) throw new FormatException();
            ValidUntilBlock = reader.ReadUInt32();
            Signers = DeserializeSigners(reader, MaxTransactionAttributes);
            Attributes = DeserializeAttributes(reader, MaxTransactionAttributes - Signers.Length);
            Script = reader.ReadVarBytes(ushort.MaxValue);
            if (Script.Length == 0) throw new FormatException();
            Witnesses = reader.ReadSerializableArray<Witness>();
        }

        private static Signer[] DeserializeSigners(BinaryReader reader, int maxCount)
        {
            int count = (int)reader.ReadVarInt((ulong)maxCount);
            if (count == 0) throw new FormatException();
            HashSet<UInt160> hashset = new HashSet<UInt160>();

            var signers = new Signer[count];
            for (int i = 0; i < count; i++)
            {
                signers[i] = reader.ReadSerializable<Signer>();
                if (!hashset.Add(signers[i].Account)) throw new FormatException();
            }
            return signers;
        }

        private static TransactionAttribute[] DeserializeAttributes(BinaryReader reader, int maxCount)
        {
            int count = (int)reader.ReadVarInt((ulong)maxCount);
            HashSet<Type> hashset = new HashSet<Type>();

            TransactionAttribute[] attributes = new TransactionAttribute[count];
            for (int i = 0; i < count; i++)
            {
                attributes[i] = TransactionAttribute.DeserializeFrom(reader);
                if (!attributes[i].AllowMultiple && !hashset.Add(attributes[i].GetType()))
                    throw new FormatException();
            }
            return attributes;
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(Nonce);
            writer.Write(SystemFee);
            writer.Write(NetworkFee);
            writer.Write(ValidUntilBlock);
            writer.Write(Signers);
            writer.WriteVarInt(Attributes.Length);
            for (int i = 0; i < Attributes.Length; i++)
            {
                Attributes[i].Serialize(writer);
            }
            writer.WriteVarBytes(Script);
            writer.Write(Witnesses);
        }
    }
}
