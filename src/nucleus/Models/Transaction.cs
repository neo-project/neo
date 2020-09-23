using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Neo.IO;
using Neo.IO.Json;

namespace Neo.Models
{
    public class Transaction : ISignable
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

        /// <summary>
        /// The first signer is the sender of the transaction, regardless of its WitnessScope.
        /// The sender will pay the fees of the transaction.
        /// </summary>
        public UInt160 Sender => Signers[0].Account;

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
            ((ISignable)this).SerializeUnsigned(writer);
            writer.Write(Witnesses);
        }

        Witness[] ISignable.Witnesses => Witnesses;

        void ISignable.SerializeUnsigned(BinaryWriter writer)
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

        public JObject ToJson(uint magic, byte addressVersion)
        {
            JObject json = new JObject();
            json["hash"] = this.CalculateHash(magic).ToString();
            json["size"] = Size;
            json["version"] = Version;
            json["nonce"] = Nonce;
            json["sender"] = Sender.ToAddress(addressVersion);
            json["sysfee"] = SystemFee.ToString();
            json["netfee"] = NetworkFee.ToString();
            json["validuntilblock"] = ValidUntilBlock;
            json["signers"] = Signers.Select(p => p.ToJson()).ToArray();
            json["attributes"] = Attributes.Select(p => p.ToJson()).ToArray();
            json["script"] = Convert.ToBase64String(Script);
            json["witnesses"] = Witnesses.Select(p => p.ToJson()).ToArray();
            return json;
        }

        public static Transaction FromJson(JObject json, byte? addressVersion)
        {
            Transaction tx = new Transaction();
            tx.Version = byte.Parse(json["version"].AsString());
            tx.Nonce = uint.Parse(json["nonce"].AsString());
            tx.Signers = ((JArray)json["signers"]).Select(p => Signer.FromJson(p, addressVersion)).ToArray();
            tx.SystemFee = long.Parse(json["sysfee"].AsString());
            tx.NetworkFee = long.Parse(json["netfee"].AsString());
            tx.ValidUntilBlock = uint.Parse(json["validuntilblock"].AsString());
            tx.Attributes = ((JArray)json["attributes"]).Select(p => TransactionAttribute.FromJson(p)).ToArray();
            tx.Script = Convert.FromBase64String(json["script"].AsString());
            tx.Witnesses = ((JArray)json["witnesses"]).Select(p => Witness.FromJson(p)).ToArray();
            return tx;
        }
    }
}
