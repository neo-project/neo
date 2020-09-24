using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Neo.IO;
using Neo.IO.Json;

namespace Neo.Models
{
    public class Transaction : IWitnessed, IEquatable<Transaction>
    {
        public const int MaxTransactionSize = 102400;
        public const uint MaxValidUntilBlockIncrement = 2102400;
        public const int MaxTransactionAttributes = 16;

        private readonly uint magic;
        private byte version;
        private uint nonce;
        private long systemFee;
        private long networkFee;
        private uint validUntilBlock;
        private Signer[] signers;
        private TransactionAttribute[] attributes;
        private byte[] script;
        public Witness[] Witnesses;

        private Lazy<UInt256> hash;
        public UInt256 Hash
        {
            get
            {
                hash ??= new Lazy<UInt256>(() => this.CalculateHash(magic));
                return hash.Value;
            }
        }

        public byte Version { get => version; set { version = value; hash = null; } }
        public uint Nonce { get => nonce; set { nonce = value; hash = null; } }
        public long SystemFee { get => systemFee; set { systemFee = value; hash = null; } }
        public long NetworkFee { get => networkFee; set { networkFee = value; hash = null; } }
        public uint ValidUntilBlock { get => validUntilBlock; set { validUntilBlock = value; hash = null; } }
        public Signer[] Signers { get => signers; set { signers = value; hash = null; } }
        public TransactionAttribute[] Attributes { get => attributes; set { attributes = value; hash = null; attributesCache = null; } }
        public byte[] Script { get => script; set { script = value; hash = null; } }

        /// <summary>
        /// The <c>NetworkFee</c> for the transaction divided by its <c>Size</c>.
        /// <para>Note that this property must be used with care. Getting the value of this property multiple times will return the same result. The value of this property can only be obtained after the transaction has been completely built (no longer modified).</para>
        /// </summary>
        public long FeePerByte => NetworkFee / Size;
        
        Witness[] IWitnessed.Witnesses => Witnesses;

        /// <summary>
        /// The first signer is the sender of the transaction, regardless of its WitnessScope.
        /// The sender will pay the fees of the transaction.
        /// </summary>
        public UInt160 Sender => Signers[0].Account;

        public const int HeaderSize =
            sizeof(byte) +  //Version
            sizeof(uint) +  //Nonce
            sizeof(long) +  //SystemFee
            sizeof(long) +  //NetworkFee
            sizeof(uint);   //ValidUntilBlock

        public Transaction(uint magic)
        {
            this.magic = magic;
        }

        private Dictionary<Type, TransactionAttribute[]> attributesCache;

        public T[] GetAttributes<T>() where T : TransactionAttribute
        {
            attributesCache ??= attributes.GroupBy(p => p.GetType()).ToDictionary(p => p.Key, p => (TransactionAttribute[])p.OfType<T>().ToArray());
            attributesCache.TryGetValue(typeof(T), out var result);
            return (T[])result;
        }

        public T GetAttribute<T>() where T : TransactionAttribute
        {
            return GetAttributes<T>()?.First();
        }

        public bool Equals(Transaction other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Hash.Equals(other.Hash);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Transaction);
        }

        public override int GetHashCode()
        {
            return Hash.GetHashCode();
        }

        public int Size => HeaderSize +
            Signers.GetVarSize() +
            BinaryFormat.GetVarSize(Attributes.Length) +
            Attributes.Sum(a => a.Size) +
            Attributes.GetVarSize() +
            Script.GetVarSize() +
            Witnesses.GetVarSize();

        public void Deserialize(BinaryReader reader)
        {
            ((IWitnessed)this).DeserializeUnsigned(reader);
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

        void IWitnessed.DeserializeUnsigned(BinaryReader reader)
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
            Signers = DeserializeSigners(reader, MaxTransactionAttributes).ToArray();
            Attributes = DeserializeAttributes(reader, MaxTransactionAttributes - Signers.Length).ToArray();
            Script = reader.ReadVarBytes(ushort.MaxValue);
            if (Script.Length == 0) throw new FormatException();
        }

        public void Serialize(BinaryWriter writer)
        {
            this.SerializeUnsigned(writer);
            writer.Write(Witnesses);
        }

        public void SerializeUnsigned(BinaryWriter writer)
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
        }
    }
}
