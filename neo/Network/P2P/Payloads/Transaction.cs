using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Json;
using Neo.Ledger;
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
        public const uint MaxValidUntilBlockIncrement = 2102400;
        /// <summary>
        /// Maximum number of attributes that can be contained within a transaction
        /// </summary>
        private const int MaxTransactionAttributes = 16;
        private const long VerificationGasLimited = 0_10000000;

        public byte Version;
        public uint Nonce;
        public byte[] Script;
        public byte[] Sender;
        public long Gas;
        public long NetworkFee;
        public uint ValidUntilBlock;
        public TransactionAttribute[] Attributes;
        public byte[] Witness;

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

        private UInt160 _sender_hash = null;
        public UInt160 SenderHash
        {
            get
            {
                if (_sender_hash is null)
                {
                    ComputeSenderHash();
                }
                return _sender_hash;
            }
        }

        public int Size =>
            sizeof(byte) +              //Version
            sizeof(uint) +              //Nonce
            Script.GetVarSize() +       //Script
            Sender.GetVarSize() +       //Sender
            sizeof(long) +              //Gas
            sizeof(long) +              //NetworkFee
            sizeof(uint) +              //ValidUntilBlock
            Attributes.GetVarSize() +   //Attributes
            Witness.GetVarSize();       //Witnesses

        public void CalculateFees()
        {
            if (Sender is null) Sender = new byte[35];
            if (Attributes is null) Attributes = new TransactionAttribute[0];
            if (Witness is null) Witness = new byte[74];
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
            using (Snapshot snapshot = Blockchain.Singleton.GetSnapshot())
            {
                long feeperbyte = NativeContract.Policy.GetFeePerByte(snapshot);
                long fee = feeperbyte * Size;
                if (fee > NetworkFee)
                    NetworkFee = fee;
            }
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            DeserializeUnsigned(reader);
            Witness = reader.ReadVarBytes(ushort.MaxValue);
        }

        public void DeserializeUnsigned(BinaryReader reader)
        {
            Version = reader.ReadByte();
            if (Version > 0) throw new FormatException();
            Nonce = reader.ReadUInt32();
            Script = reader.ReadVarBytes(ushort.MaxValue);
            if (Script.Length == 0) throw new FormatException();
            Sender = reader.ReadVarBytes(35);
            try
            {
                ComputeSenderHash();
            }
            catch
            {
                throw new FormatException();
            }
            Gas = reader.ReadInt64();
            if (Gas < 0) throw new FormatException();
            if (Gas % NativeContract.GAS.Factor != 0) throw new FormatException();
            NetworkFee = reader.ReadInt64();
            if (NetworkFee < 0) throw new FormatException();
            if (Gas + NetworkFee < Gas) throw new FormatException();
            ValidUntilBlock = reader.ReadUInt32();
            Attributes = reader.ReadSerializableArray<TransactionAttribute>(MaxTransactionAttributes);
        }

        private void ComputeSenderHash()
        {
            switch (Sender.Length)
            {
                case 20: //contract hash
                    _sender_hash = new UInt160(Sender);
                    break;
                case 33: //pubkey
                    _sender_hash = Contract.CreateSignatureRedeemScript(Sender.AsSerializable<ECPoint>()).ToScriptHash();
                    break;
                case 35: //compatible old address script
                    if (Sender[0] != 33 || Sender[34] != 0xAC)
                        throw new InvalidOperationException();
                    _sender_hash = Sender.ToScriptHash();
                    break;
                default:
                    throw new InvalidOperationException();
            }
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

        void ISerializable.Serialize(BinaryWriter writer)
        {
            ((IVerifiable)this).SerializeUnsigned(writer);
            writer.WriteVarBytes(Witness);
        }

        void IVerifiable.SerializeUnsigned(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(Nonce);
            writer.WriteVarBytes(Script);
            writer.WriteVarBytes(Sender);
            writer.Write(Gas);
            writer.Write(NetworkFee);
            writer.Write(ValidUntilBlock);
            writer.Write(Attributes);
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["txid"] = Hash.ToString();
            json["size"] = Size;
            json["version"] = Version;
            json["nonce"] = Nonce;
            json["script"] = Script.ToHexString();
            json["sender"] = SenderHash.ToAddress();
            json["gas"] = new BigDecimal(Gas, NativeContract.GAS.Decimals).ToString();
            json["net_fee"] = new BigDecimal(NetworkFee, NativeContract.GAS.Decimals).ToString();
            json["valid_until_block"] = ValidUntilBlock;
            json["attributes"] = Attributes.Select(p => p.ToJson()).ToArray();
            json["witness"] = Witness.ToHexString();
            return json;
        }

        bool IInventory.Verify(Snapshot snapshot)
        {
            return Verify(snapshot, Enumerable.Empty<Transaction>());
        }

        public virtual bool Verify(Snapshot snapshot, IEnumerable<Transaction> mempool)
        {
            if (ValidUntilBlock <= snapshot.Height || ValidUntilBlock > snapshot.Height + MaxValidUntilBlockIncrement)
                return false;
            int size = Size;
            if (size > MaxTransactionSize) return false;
            if (size > NativeContract.Policy.GetMaxLowPriorityTransactionSize(snapshot) && NetworkFee / size < NativeContract.Policy.GetFeePerByte(snapshot))
                return false;
            if (NativeContract.Policy.GetBlockedAccounts(snapshot).Contains(SenderHash))
                return false;
            BigInteger balance = NativeContract.GAS.BalanceOf(snapshot, SenderHash);
            BigInteger fee = Gas + NetworkFee;
            if (balance < fee) return false;
            fee += mempool.Where(p => p != this && p.Sender.Equals(Sender)).Sum(p => p.Gas + p.NetworkFee);
            if (balance < fee) return false;
            byte[] script = null, pubkey = null;
            switch (Sender.Length)
            {
                case 20:
                    script = snapshot.Contracts.TryGet(SenderHash)?.Script;
                    if (script is null) return false;
                    break;
                case 33:
                    pubkey = Sender;
                    break;
                case 35:
                    pubkey = Sender.Skip(1).Take(33).ToArray();
                    break;
                default:
                    return false;
            }
            if (script is null)
            {
                return Crypto.Default.VerifySignature(this.GetHashData(), Witness.Skip(1).Take(64).ToArray(), pubkey);
            }
            else
            {
                using (ApplicationEngine engine = new ApplicationEngine(TriggerType.Verification, this, snapshot, VerificationGasLimited))
                {
                    engine.LoadScript(script);
                    engine.LoadScript(Witness);
                    if (engine.Execute().HasFlag(VMState.FAULT)) return false;
                    if (engine.ResultStack.Count != 1 || !engine.ResultStack.Pop().GetBoolean()) return false;
                }
                return true;
            }
        }
    }
}
