using AntShares.Cryptography;
using AntShares.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AntShares.Core
{
    public abstract class Transaction : ISignable
    {
        public readonly TransactionType Type;
        public TransactionInput[] Inputs;
        public TransactionOutput[] Outputs;

        private UInt256 hash = null;

        public UInt256 Hash
        {
            get
            {
                if (hash == null)
                {
                    hash = new UInt256(this.ToArray().Sha256().Sha256());
                }
                return hash;
            }
        }

        public byte[][] Scripts { get; set; }

        protected Transaction(TransactionType type)
        {
            this.Type = type;
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            if ((TransactionType)reader.ReadByte() != Type)
                throw new FormatException();
            DeserializeWithoutType(reader);
        }

        protected abstract void DeserializeExclusiveData(BinaryReader reader);

        internal static Transaction DeserializeFrom(BinaryReader reader)
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

        void ISignable.FromUnsignedArray(byte[] value)
        {
            using (MemoryStream ms = new MemoryStream(value, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                if ((TransactionType)reader.ReadByte() != Type)
                    throw new FormatException();
                DeserializeExclusiveData(reader);
                this.Inputs = reader.ReadSerializableArray<TransactionInput>();
                this.Outputs = reader.ReadSerializableArray<TransactionOutput>();
            }
        }

        byte[] ISignable.GetHashForSigning()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write((byte)Type);
                SerializeExclusiveData(writer);
                writer.Write(Inputs);
                writer.Write(Outputs);
                writer.Flush();
                return ms.ToArray().Sha256();
            }
        }

        public virtual UInt160[] GetScriptHashesForVerifying()
        {
            HashSet<UInt160> hashes = new HashSet<UInt160>();
            for (int i = 0; i < Inputs.Length; i++)
            {
                //TODO: 获取 TransactionInput 所指向的 TransactionOutput 中的 ScriptHash，用以确定交易中需要签名的地址
                //需要本地区块链数据库，否则无法验证
                //无法验证的情况下，抛出异常：
                //throw new InvalidOperationException();
                throw new NotImplementedException();
            }
            return hashes.OrderBy(p => p).ToArray();
        }

        void ISerializable.Serialize(BinaryWriter writer)
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

        byte[] ISignable.ToUnsignedArray()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write((byte)Type);
                SerializeExclusiveData(writer);
                writer.Write(Inputs);
                writer.Write(Outputs);
                writer.Flush();
                return ms.ToArray();
            }
        }
    }
}
