using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Caching;
using Neo.IO.Json;
using System;
using System.Collections.Generic;
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
                    ECPoint[] pubkeys;
                    try
                    {
                        pubkeys = Value.AsSerializableArray<ECPoint>((int)Blockchain.MaxValidators);
                    }
                    catch (FormatException)
                    {
                        return false;
                    }
                    UInt160 hash = new UInt160(Key);
                    AccountState account = Blockchain.Default.GetAccountState(hash);
                    if (account?.IsFrozen != false) return false;
                    if (pubkeys.Length > 0)
                    {
                        if (account.GetBalance(Blockchain.GoverningToken.Hash).Equals(Fixed8.Zero)) return false;
                        HashSet<ECPoint> sv = new HashSet<ECPoint>(Blockchain.StandbyValidators);
                        DataCache<ECPoint, ValidatorState> validators = Blockchain.Default.GetStates<ECPoint, ValidatorState>();
                        foreach (ECPoint pubkey in pubkeys)
                            if (!sv.Contains(pubkey) && validators.TryGet(pubkey)?.Registered != true)
                                return false;
                    }
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
