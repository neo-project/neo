using Neo.IO;
using Neo.IO.Json;
using System;
using System.IO;
using System.Linq;

namespace Neo.Core
{
    public class StateDescriptor : ISerializable
    {
        public StateType Type;
        public byte[] Key;
        public string Field;
        public byte[] Value;

        public int Size => sizeof(StateType) + Key.GetVarSize() + Field.GetVarSize() + Value.GetVarSize();

        public Fixed8 SystemFee
        {
            get
            {
                switch (Type)
                {
                    case StateType.Validator:
                        return GetSystemFee_Validator();
                    default:
                        return Fixed8.Zero;
                }
            }
        }

        private void CheckAccountState()
        {
            if (Key.Length != 20) throw new FormatException();
            if (Field != "Votes") throw new FormatException();
        }

        private void CheckValidatorState()
        {
            if (Key.Length != 33) throw new FormatException();
            if (Field != "Registered") throw new FormatException();
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Type = (StateType)reader.ReadByte();
            if (!Enum.IsDefined(typeof(StateType), Type))
                throw new FormatException();
            Key = reader.ReadVarBytes(100);
            Field = reader.ReadVarString(32);
            Value = reader.ReadVarBytes(65535);
            switch (Type)
            {
                case StateType.Account:
                    CheckAccountState();
                    break;
                case StateType.Validator:
                    CheckValidatorState();
                    break;
            }
        }

        private Fixed8 GetSystemFee_Validator()
        {
            switch (Field)
            {
                case "Registered":
                    if (Value.Any(p => p != 0))
                        return Fixed8.FromDecimal(1000);
                    else
                        return Fixed8.Zero;
                default:
                    throw new InvalidOperationException();
            }
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Type);
            writer.WriteVarBytes(Key);
            writer.WriteVarString(Field);
            writer.WriteVarBytes(Value);
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["type"] = Type;
            json["key"] = Key.ToHexString();
            json["field"] = Field;
            json["value"] = Value.ToHexString();
            return json;
        }

        internal bool Verify()
        {
            switch (Type)
            {
                case StateType.Account:
                    return VerifyAccountState();
                case StateType.Validator:
                    return VerifyValidatorState();
                default:
                    return false;
            }
        }

        private bool VerifyAccountState()
        {
            switch (Field)
            {
                case "Votes":
                    if (Blockchain.Default == null) return false;
                    int votes_count;
                    using (MemoryStream ms = new MemoryStream(Value, false))
                    using (BinaryReader reader = new BinaryReader(ms))
                    {
                        votes_count = (int)reader.ReadVarInt();
                    }
                    if (votes_count > Blockchain.MaxValidators) return false;
                    UInt160 hash = new UInt160(Key);
                    AccountState account = Blockchain.Default.GetAccountState(hash);
                    if (account?.IsFrozen != false) return false;
                    Fixed8 balance = account.GetBalance(Blockchain.GoverningToken.Hash);
                    if (balance.Equals(Fixed8.Zero) && votes_count > 0) return false;
                    return true;
                default:
                    return false;
            }
        }

        private bool VerifyValidatorState()
        {
            switch (Field)
            {
                case "Registered":
                    return true;
                default:
                    return false;
            }
        }
    }
}
