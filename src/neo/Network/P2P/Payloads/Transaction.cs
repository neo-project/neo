using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Array = Neo.VM.Types.Array;

namespace Neo.Network.P2P.Payloads
{
    public class Transaction : IEquatable<Transaction>, IInventory, IInteroperable
    {
        public const int MaxTransactionSize = 102400;
        public const uint MaxValidUntilBlockIncrement = 2102400;
        /// <summary>
        /// Maximum number of attributes that can be contained within a transaction
        /// </summary>
        private const int MaxTransactionAttributes = 16;
        /// <summary>
        /// Maximum number of cosigners that can be contained within a transaction
        /// </summary>
        private const int MaxCosigners = 16;

        private byte version;
        private uint nonce;
        private UInt160 sender;
        private long sysfee;
        private long netfee;
        private uint validUntilBlock;
        private TransactionAttribute[] attributes;
        private Cosigner[] cosigners;
        private byte[] script;
        private Witness[] witnesses;

        public const int HeaderSize =
            sizeof(byte) +  //Version
            sizeof(uint) +  //Nonce
            20 +            //Sender
            sizeof(long) +  //SystemFee
            sizeof(long) +  //NetworkFee
            sizeof(uint);   //ValidUntilBlock

        public TransactionAttribute[] Attributes
        {
            get => attributes;
            set { attributes = value; _hash = null; _size = 0; }
        }

        public Cosigner[] Cosigners
        {
            get => cosigners;
            set { cosigners = value; _hash = null; _size = 0; }
        }

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
                    _hash = new UInt256(Crypto.Hash256(this.GetHashData()));
                }
                return _hash;
            }
        }

        InventoryType IInventory.InventoryType => InventoryType.TX;

        /// <summary>
        /// Distributed to consensus nodes.
        /// </summary>
        public long NetworkFee
        {
            get => netfee;
            set { netfee = value; _hash = null; }
        }

        public uint Nonce
        {
            get => nonce;
            set { nonce = value; _hash = null; }
        }

        public byte[] Script
        {
            get => script;
            set { script = value; _hash = null; _size = 0; }
        }

        public UInt160 Sender
        {
            get => sender;
            set { sender = value; _hash = null; }
        }

        private int _size;
        public int Size
        {
            get
            {
                if (_size == 0)
                {
                    _size = HeaderSize +
                        Attributes.GetVarSize() +   //Attributes
                        Cosigners.GetVarSize() +    //Cosigners
                        Script.GetVarSize() +       //Script
                        Witnesses.GetVarSize();     //Witnesses
                }
                return _size;
            }
        }

        /// <summary>
        /// Fee to be burned.
        /// </summary>
        public long SystemFee
        {
            get => sysfee;
            set { sysfee = value; _hash = null; }
        }

        public uint ValidUntilBlock
        {
            get => validUntilBlock;
            set { validUntilBlock = value; _hash = null; }
        }

        public byte Version
        {
            get => version;
            set { version = value; _hash = null; }
        }

        public Witness[] Witnesses
        {
            get => witnesses;
            set { witnesses = value; _size = 0; }
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            int startPosition = -1;
            if (reader.BaseStream.CanSeek)
                startPosition = (int)reader.BaseStream.Position;
            DeserializeUnsigned(reader);
            Witnesses = reader.ReadSerializableArray<Witness>();
            if (startPosition >= 0)
                _size = (int)reader.BaseStream.Position - startPosition;
        }

        public void DeserializeUnsigned(BinaryReader reader)
        {
            Version = reader.ReadByte();
            if (Version > 0) throw new FormatException();
            Nonce = reader.ReadUInt32();
            Sender = reader.ReadSerializable<UInt160>();
            SystemFee = reader.ReadInt64();
            if (SystemFee < 0) throw new FormatException();
            NetworkFee = reader.ReadInt64();
            if (NetworkFee < 0) throw new FormatException();
            if (SystemFee + NetworkFee < SystemFee) throw new FormatException();
            ValidUntilBlock = reader.ReadUInt32();
            Attributes = reader.ReadSerializableArray<TransactionAttribute>(MaxTransactionAttributes);
            Cosigners = reader.ReadSerializableArray<Cosigner>(MaxCosigners);
            if (Cosigners.Select(u => u.Account).Distinct().Count() != Cosigners.Length) throw new FormatException();
            Script = reader.ReadVarBytes(ushort.MaxValue);
            if (Script.Length == 0) throw new FormatException();
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

        public UInt160[] GetScriptHashesForVerifying(StoreView snapshot)
        {
            var hashes = new HashSet<UInt160> { Sender };
            hashes.UnionWith(Cosigners.Select(p => p.Account));
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
            writer.Write(Nonce);
            writer.Write(Sender);
            writer.Write(SystemFee);
            writer.Write(NetworkFee);
            writer.Write(ValidUntilBlock);
            writer.Write(Attributes);
            writer.Write(Cosigners);
            writer.WriteVarBytes(Script);
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["hash"] = Hash.ToString();
            json["size"] = Size;
            json["version"] = Version;
            json["nonce"] = Nonce;
            json["sender"] = Sender.ToAddress();
            json["sys_fee"] = SystemFee.ToString();
            json["net_fee"] = NetworkFee.ToString();
            json["valid_until_block"] = ValidUntilBlock;
            json["attributes"] = Attributes.Select(p => p.ToJson()).ToArray();
            json["cosigners"] = Cosigners.Select(p => p.ToJson()).ToArray();
            json["script"] = Convert.ToBase64String(Script);
            json["witnesses"] = Witnesses.Select(p => p.ToJson()).ToArray();
            return json;
        }

        public static Transaction FromJson(JObject json)
        {
            Transaction tx = new Transaction();
            tx.Version = byte.Parse(json["version"].AsString());
            tx.Nonce = uint.Parse(json["nonce"].AsString());
            tx.Sender = json["sender"].AsString().ToScriptHash();
            tx.SystemFee = long.Parse(json["sys_fee"].AsString());
            tx.NetworkFee = long.Parse(json["net_fee"].AsString());
            tx.ValidUntilBlock = uint.Parse(json["valid_until_block"].AsString());
            tx.Attributes = ((JArray)json["attributes"]).Select(p => TransactionAttribute.FromJson(p)).ToArray();
            tx.Cosigners = ((JArray)json["cosigners"]).Select(p => Cosigner.FromJson(p)).ToArray();
            tx.Script = Convert.FromBase64String(json["script"].AsString());
            tx.Witnesses = ((JArray)json["witnesses"]).Select(p => Witness.FromJson(p)).ToArray();
            return tx;
        }

        bool IInventory.Verify(StoreView snapshot)
        {
            return Verify(snapshot, BigInteger.Zero) == VerifyResult.Succeed;
        }

        public virtual VerifyResult VerifyForEachBlock(StoreView snapshot, BigInteger totalSenderFeeFromPool)
        {
            if (ValidUntilBlock <= snapshot.Height || ValidUntilBlock > snapshot.Height + MaxValidUntilBlockIncrement)
                return VerifyResult.Expired;
            UInt160[] hashes = GetScriptHashesForVerifying(snapshot);
            if (NativeContract.Policy.GetBlockedAccounts(snapshot).Intersect(hashes).Any())
                return VerifyResult.PolicyFail;
            BigInteger balance = NativeContract.GAS.BalanceOf(snapshot, Sender);
            BigInteger fee = SystemFee + NetworkFee + totalSenderFeeFromPool;
            if (balance < fee) return VerifyResult.InsufficientFunds;
            if (hashes.Length != Witnesses.Length) return VerifyResult.Invalid;
            for (int i = 0; i < hashes.Length; i++)
            {
                if (Witnesses[i].VerificationScript.Length > 0) continue;
                if (snapshot.Contracts.TryGet(hashes[i]) is null) return VerifyResult.Invalid;
            }
            return VerifyResult.Succeed;
        }

        public virtual VerifyResult Verify(StoreView snapshot, BigInteger totalSenderFeeFromPool)
        {
            VerifyResult result = VerifyForEachBlock(snapshot, totalSenderFeeFromPool);
            if (result != VerifyResult.Succeed) return result;
            int size = Size;
            if (size > MaxTransactionSize) return VerifyResult.Invalid;
            long net_fee = NetworkFee - size * NativeContract.Policy.GetFeePerByte(snapshot);
            if (net_fee < 0) return VerifyResult.InsufficientFunds;
            if (!this.VerifyWitnesses(snapshot, net_fee)) return VerifyResult.Invalid;
            return VerifyResult.Succeed;
        }

        public StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Array(referenceCounter, new StackItem[]
            {
                // Computed properties
                Hash.ToArray(),

                // Transaction properties
                (int)Version,
                Nonce,
                Sender.ToArray(),
                SystemFee,
                NetworkFee,
                ValidUntilBlock,
                Script,
            });
        }
    }
}
