using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Json;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    public sealed class Transaction : IEquatable<Transaction>, IInventory
    {
        public const int MaxTransactionSize = 102400;
        /// <summary>
        /// Maximum number of cosigners that can be contained within a transaction
        /// </summary>
        private const int MaxCosigners = 16;

        public byte Version;
        public byte[] Script;
        public long Gas;
        public long NetworkFee;
        public UInt160[] Cosigners;
        public Witness[] Witnesses { get; set; }

        /// <summary>
        /// The <c>NetworkFee</c> for the transaction divided by its <c>Size</c>.
        /// <para>Note that this property must be used with care. Getting the value of this property multiple times will return the same result. The value of this property can only be obtained after the transaction has been completely built (no longer modified).</para>
        /// </summary>
        public long FeePerByte => NetworkFee / Size;

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

        public int Size =>
            sizeof(byte) +              //Version
            Script.GetVarSize() +       //Script
            sizeof(long) +              //Gas
            sizeof(long) +              //NetworkFee
            Cosigners.GetVarSize() +    //Cosigners
            Witnesses.GetVarSize();     //Witnesses

        void ISerializable.Deserialize(BinaryReader reader)
        {
            DeserializeUnsigned(reader);
            Witnesses = reader.ReadSerializableArray<Witness>();
        }

        public void DeserializeUnsigned(BinaryReader reader)
        {
            Version = reader.ReadByte();
            if (Version > 0) throw new FormatException();
            Script = reader.ReadVarBytes(ushort.MaxValue);
            if (Script.Length == 0) throw new FormatException();
            Gas = reader.ReadInt64();
            if (Gas < 0) throw new FormatException();
            NetworkFee = reader.ReadInt64();
            if (NetworkFee < 0) throw new FormatException();
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

        public static long GetGas(long consumed)
        {
            long d = (long)NativeContract.GAS.Factor;
            long gas = consumed - 10 * d;
            if (gas <= 0) return 0;
            long remainder = gas % d;
            if (remainder == 0) return gas;
            if (remainder > 0)
                return gas - remainder + d;
            else
                return gas - remainder;
        }

        public override int GetHashCode()
        {
            return Hash.GetHashCode();
        }

        byte[] IScriptContainer.GetMessage()
        {
            return this.GetHashData();
        }

        public UInt160[] GetScriptHashesForVerifying(Snapshot snapshot)
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
            writer.Write(NetworkFee);
            writer.Write(Cosigners);
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["txid"] = Hash.ToString();
            json["size"] = Size;
            json["version"] = Version;
            json["script"] = Script.ToHexString();
            json["gas"] = new BigDecimal(Gas, (byte)NativeContract.GAS.Decimals).ToString();
            json["net_fee"] = new BigDecimal(NetworkFee, (byte)NativeContract.GAS.Decimals).ToString();
            json["cosigners"] = Cosigners.Select(p => (JObject)p.ToAddress()).ToArray();
            json["witnesses"] = Witnesses.Select(p => p.ToJson()).ToArray();
            return json;
        }

        bool IInventory.Verify(Snapshot snapshot)
        {
            return Verify(snapshot, Enumerable.Empty<Transaction>());
        }

        public bool Verify(Snapshot snapshot, IEnumerable<Transaction> mempool)
        {
            if (Size > MaxTransactionSize) return false;
            if (Gas % NativeContract.GAS.Factor != 0) return false;
            return this.VerifyWitnesses(snapshot);
        }
    }
}
