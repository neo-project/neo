using AntShares.IO;
using System;
using System.Collections.Generic;
using System.IO;

namespace AntShares.Core
{
    public abstract class Transaction : ISerializable
    {
        public readonly TransactionType Type;
        public TransactionInput[] Inputs;
        public TransactionOutput[] Outputs;
        public byte[][] Scripts;

        protected Transaction(TransactionType type)
        {
            this.Type = type;
        }

        public void Deserialize(BinaryReader reader)
        {
            if ((TransactionType)reader.ReadByte() != Type)
                throw new FormatException();
            DeserializeWithoutType(reader);
        }

        protected abstract void DeserializeExclusiveData(BinaryReader reader);

        public static Transaction DeserializeFrom(BinaryReader reader)
        {
            TransactionType type = (TransactionType)reader.ReadByte();
            string typeName = string.Format("{0}.{1}", typeof(Transaction).Namespace, type);
            Transaction transaction = typeof(Transaction).Assembly.CreateInstance(typeName) as Transaction;
            if (transaction == null)
                throw new FormatException();
            transaction.DeserializeWithoutType(reader);
            return transaction;
        }

        private void DeserializeWithoutType(BinaryReader reader)
        {
            DeserializeExclusiveData(reader);
            this.Inputs = reader.ReadSerializableArray<TransactionInput>();
            this.Outputs = reader.ReadSerializableArray<TransactionOutput>();
            this.Scripts = reader.ReadBytesArray();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Type);
            SerializeExclusiveData(writer);
            writer.Write(Inputs);
            writer.Write(Outputs);
            writer.Write(Scripts);
        }

        public virtual IEnumerable<TransactionInput> GetAllInputs()
        {
            return Inputs;
        }

        protected abstract void SerializeExclusiveData(BinaryWriter writer);
    }
}
