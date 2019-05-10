using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Json;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    public class Transaction : IEquatable<Transaction>, IInventory
    {
        public const int MaxTransactionSize = 102400;
        /// <summary>
        /// Maximum number of attributes that can be contained within a transaction
        /// </summary>
        private const int MaxCosigners = 16;

        public byte Version;
        public byte[] Script;
        public Fixed8 Gas;
        public UInt160[] Cosigners;
        public Witness[] Witnesses { get; set; }

        private Fixed8 _feePerByte = -Fixed8.Satoshi;
        /// <summary>
        /// The <c>NetworkFee</c> for the transaction divided by its <c>Size</c>.
        /// <para>Note that this property must be used with care. Getting the value of this property multiple times will return the same result. The value of this property can only be obtained after the transaction has been completely built (no longer modified).</para>
        /// </summary>
        public Fixed8 FeePerByte
        {
            get
            {
                if (_feePerByte == -Fixed8.Satoshi)
                    _feePerByte = NetworkFee / Size;
                return _feePerByte;
            }
        }

        private UInt256 _hash = null;
        public UInt256 Hash
        {
            get
            {
                if (_hash == null)
                {
                    _hash = new UInt256(Crypto.Default.Hash256(this.GetHashData()));
                }
                return _hash;
            }
        }

        InventoryType IInventory.InventoryType => InventoryType.TX;

        public bool IsLowPriority => NetworkFee < ProtocolSettings.Default.LowPriorityThreshold;

        public virtual Fixed8 NetworkFee => Fixed8.Zero;

        public virtual int Size =>
            sizeof(byte) +              //Version
            Script.GetVarSize() +       //Script
            Gas.Size +                  //Gas
            Cosigners.GetVarSize() +    //Cosigners
            Witnesses.GetVarSize();     //Witnesses

        void ISerializable.Deserialize(BinaryReader reader)
        {
            ((IVerifiable)this).DeserializeUnsigned(reader);
            Witnesses = reader.ReadSerializableArray<Witness>();
        }

        public void DeserializeUnsigned(BinaryReader reader)
        {
            Version = reader.ReadByte();
            if (Version > 0) throw new FormatException();
            Script = reader.ReadVarBytes(65536);
            if (Script.Length == 0) throw new FormatException();
            Gas = reader.ReadSerializable<Fixed8>();
            if (Gas < Fixed8.Zero) throw new FormatException();
            Cosigners = reader.ReadSerializableArray<UInt160>(MaxCosigners);
            if (Cosigners.Distinct().Count() != Cosigners.Length)
                throw new FormatException();
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

        public static Fixed8 GetGas(Fixed8 consumed)
        {
            Fixed8 gas = consumed - Fixed8.FromDecimal(10);
            if (gas <= Fixed8.Zero) return Fixed8.Zero;
            return gas.Ceiling();
        }

        public override int GetHashCode()
        {
            return Hash.GetHashCode();
        }

        byte[] IScriptContainer.GetMessage()
        {
            return this.GetHashData();
        }

        public virtual UInt160[] GetScriptHashesForVerifying(Snapshot snapshot)
        {
            return Cosigners.OrderBy(p => p).ToArray();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            ((IVerifiable)this).SerializeUnsigned(writer);
            writer.Write(Witnesses);
        }

        void IVerifiable.SerializeUnsigned(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.WriteVarBytes(Script);
            writer.Write(Gas);
            writer.Write(Cosigners);
        }

        public virtual JObject ToJson()
        {
            JObject json = new JObject();
            json["txid"] = Hash.ToString();
            json["size"] = Size;
            json["version"] = Version;
            json["script"] = Script.ToHexString();
            json["gas"] = Gas.ToString();
            json["cosigners"] = Cosigners.Select(p => (JObject)p.ToAddress()).ToArray();
            json["net_fee"] = NetworkFee.ToString();
            json["witnesses"] = Witnesses.Select(p => p.ToJson()).ToArray();
            return json;
        }

        bool IInventory.Verify(Snapshot snapshot)
        {
            return Verify(snapshot, Enumerable.Empty<Transaction>());
        }

        public virtual bool Verify(Snapshot snapshot, IEnumerable<Transaction> mempool)
        {
            if (Size > MaxTransactionSize) return false;
            if (Gas.GetData() % 100000000 != 0) return false;
            return this.VerifyWitnesses(snapshot);
        }
    }
}
