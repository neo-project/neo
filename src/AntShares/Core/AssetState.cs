using AntShares.Cryptography.ECC;
using AntShares.IO;
using AntShares.IO.Json;
using AntShares.VM;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace AntShares.Core
{
    public class AssetState : IInteropInterface, ISerializable
    {
        public const byte StateVersion = 0;
        public UInt256 AssetId;
        public AssetType AssetType;
        public string Name;
        public Fixed8 Amount;
        public Fixed8 Available;
        public byte Precision;
        public const byte FeeMode = 0;
        public Fixed8 Fee;
        public UInt160 FeeAddress;
        public ECPoint Owner;
        public UInt160 Admin;
        public UInt160 Issuer;
        public uint Expiration;
        public bool IsFrozen;

        int ISerializable.Size => sizeof(byte) + AssetId.Size + sizeof(AssetType) + Name.GetVarSize() + Amount.Size + Available.Size + sizeof(byte) + sizeof(byte) + Fee.Size + FeeAddress.Size + Owner.Size + Admin.Size + Issuer.Size + sizeof(uint) + sizeof(bool);

        void ISerializable.Deserialize(BinaryReader reader)
        {
            if (reader.ReadByte() != StateVersion) throw new FormatException();
            AssetId = reader.ReadSerializable<UInt256>();
            AssetType = (AssetType)reader.ReadByte();
            Name = reader.ReadVarString();
            Amount = reader.ReadSerializable<Fixed8>();
            Available = reader.ReadSerializable<Fixed8>();
            Precision = reader.ReadByte();
            reader.ReadByte(); //FeeMode
            Fee = reader.ReadSerializable<Fixed8>(); //Fee
            FeeAddress = reader.ReadSerializable<UInt160>();
            Owner = ECPoint.DeserializeFrom(reader, ECCurve.Secp256r1);
            Admin = reader.ReadSerializable<UInt160>();
            Issuer = reader.ReadSerializable<UInt160>();
            Expiration = reader.ReadUInt32();
            IsFrozen = reader.ReadBoolean();
        }

        private Dictionary<CultureInfo, string> _names;
        public string GetName(CultureInfo culture = null)
        {
            if (_names == null)
            {
                JObject name_obj;
                try
                {
                    name_obj = JObject.Parse(Name);
                }
                catch (FormatException)
                {
                    name_obj = Name;
                }
                if (name_obj is JString)
                    _names = new Dictionary<CultureInfo, string> { { new CultureInfo("en"), name_obj.AsString() } };
                else
                    _names = ((JArray)JObject.Parse(Name)).ToDictionary(p => new CultureInfo(p["lang"].AsString()), p => p["name"].AsString());
            }
            if (culture == null) culture = CultureInfo.CurrentCulture;
            if (_names.ContainsKey(culture))
            {
                return _names[culture];
            }
            else if (_names.ContainsKey(new CultureInfo("en")))
            {
                return _names[new CultureInfo("en")];
            }
            else
            {
                return _names.Values.First();
            }
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(StateVersion);
            writer.Write(AssetId);
            writer.Write((byte)AssetType);
            writer.WriteVarString(Name);
            writer.Write(Amount);
            writer.Write(Available);
            writer.Write(Precision);
            writer.Write(FeeMode);
            writer.Write(Fee);
            writer.Write(FeeAddress);
            writer.Write(Owner);
            writer.Write(Admin);
            writer.Write(Issuer);
            writer.Write(Expiration);
            writer.Write(IsFrozen);
        }

        byte[] IInteropInterface.ToArray()
        {
            return this.ToArray();
        }

        public override string ToString()
        {
            return GetName();
        }
    }
}
