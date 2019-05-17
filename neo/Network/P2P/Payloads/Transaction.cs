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
using System.Numerics;

namespace Neo.Network.P2P.Payloads
{
    public class Transaction : IEquatable<Transaction>, IInventory
    {
        public const int MaxTransactionSize = 102400;
        /// <summary>
        /// Maximum number of attributes that can be contained within a transaction
        /// </summary>
        private const int MaxTransactionAttributes = 16;

        public byte Version;
        public byte[] Script;
        public UInt160 Sender;
        public long Gas;
        public long NetworkFee;
        public TransactionAttribute[] Attributes;
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
            Sender.Size +               //Sender
            sizeof(long) +              //Gas
            sizeof(long) +              //NetworkFee
            Attributes.GetVarSize() +   //Attributes
            Witnesses.GetVarSize();     //Witnesses

        public void CalculateGas()
        {
            if (Sender is null) Sender = UInt160.Zero;
            if (Attributes is null) Attributes = new TransactionAttribute[0];
            if (Witnesses is null) Witnesses = new Witness[0];
            _hash = null;
            long consumed;
            using (ApplicationEngine engine = ApplicationEngine.Run(Script, this))
            {
                if (engine.State.HasFlag(VMState.FAULT))
                    throw new InvalidOperationException();
                consumed = engine.GasConsumed;
            }
            _hash = null;
            long d = (long)NativeContract.GAS.Factor;
            Gas = consumed - ApplicationEngine.GasFree;
            if (Gas <= 0)
            {
                Gas = 0;
            }
            else
            {
                long remainder = Gas % d;
                if (remainder == 0) return;
                if (remainder > 0)
                    Gas += d - remainder;
                else
                    Gas -= remainder;
            }
        }

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
            Sender = reader.ReadSerializable<UInt160>();
            Gas = reader.ReadInt64();
            if (Gas < 0) throw new FormatException();
            if (Gas % NativeContract.GAS.Factor != 0) throw new FormatException();
            NetworkFee = reader.ReadInt64();
            if (NetworkFee < 0) throw new FormatException();
            if (Gas + NetworkFee < Gas) throw new FormatException();
            Attributes = reader.ReadSerializableArray<TransactionAttribute>(MaxTransactionAttributes);
            var cosigners = GetScriptHashesForVerifying(null);
            if (cosigners.Distinct().Count() != cosigners.Length) throw new FormatException();
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

        public UInt160[] GetScriptHashesForVerifying(Snapshot snapshot)
        {
            HashSet<UInt160> hashes = new HashSet<UInt160> { Sender };
            hashes.UnionWith(Attributes.Where(p => p.Usage == TransactionAttributeUsage.Script).Select(p => new UInt160(p.Data)));
            return hashes.OrderBy(p => p).ToArray();
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
            writer.Write(Sender);
            writer.Write(Gas);
            writer.Write(NetworkFee);
            writer.Write(Attributes);
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["txid"] = Hash.ToString();
            json["size"] = Size;
            json["version"] = Version;
            json["script"] = Script.ToHexString();
            json["sender"] = Sender.ToAddress();
            json["gas"] = new BigDecimal(Gas, (byte)NativeContract.GAS.Decimals).ToString();
            json["net_fee"] = new BigDecimal(NetworkFee, (byte)NativeContract.GAS.Decimals).ToString();
            json["attributes"] = Attributes.Select(p => p.ToJson()).ToArray();
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
            BigInteger balance = NativeContract.GAS.BalanceOf(snapshot, Sender);
            BigInteger fee = Gas + NetworkFee;
            if (balance < fee) return false;
            fee += mempool.Where(p => p != this && p.Sender.Equals(Sender)).Sum(p => p.Gas + p.NetworkFee);
            if (balance < fee) return false;
            return this.VerifyWitnesses(snapshot);
        }
    }
}
