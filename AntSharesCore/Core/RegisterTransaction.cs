using AntShares.Core.Scripts;
using AntShares.Cryptography.ECC;
using AntShares.IO;
using AntShares.IO.Json;
using AntShares.Wallets;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace AntShares.Core
{
    public class RegisterTransaction : Transaction
    {
        public AssetType AssetType;
        public string Name;
        /// <summary>
        /// 发行总量，共有2种模式：
        /// 1. 限量模式：当Amount为正数时，表示当前资产的最大总量为Amount，且不可修改（股权在未来可能会支持扩股或增发，会考虑需要公司签名或一定比例的股东签名认可）。
        /// 2. 不限量模式：当Amount等于-1时，表示当前资产可以由创建者无限量发行。这种模式的自由度最大，但是公信力最低，不建议使用。
        /// 在使用过程中，根据资产类型的不同，能够使用的总量模式也不同，具体规则如下：
        /// 1. 对于股权，只能使用限量模式；
        /// 2. 对于货币，只能使用不限量模式；
        /// 3. 对于点券，可以使用任意模式；
        /// </summary>
        public Fixed8 Amount;
        public ECPoint Issuer;
        public UInt160 Admin;

        private static readonly string ShareName = "[{'lang':'zh-CN','name':'股权'},{'lang':'en','name':'Share'}]";

        public override Fixed8 SystemFee => Fixed8.FromDecimal(10000);

        public RegisterTransaction()
            : base(TransactionType.RegisterTransaction)
        {
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            this.AssetType = (AssetType)reader.ReadByte();
            if (!Enum.IsDefined(typeof(AssetType), AssetType))
                throw new FormatException();
            this.Name = reader.ReadVarString();
            this.Amount = reader.ReadSerializable<Fixed8>();
            if (Amount == Fixed8.Zero || Amount < -Fixed8.Satoshi) throw new FormatException();
            if (AssetType == AssetType.Share && Amount <= Fixed8.Zero)
                throw new FormatException();
            if (AssetType == AssetType.Currency && Amount != -Fixed8.Satoshi)
                throw new FormatException();
            this.Issuer = ECPoint.DeserializeFrom(reader, ECCurve.Secp256r1);
            this.Admin = reader.ReadSerializable<UInt160>();
        }

        [NonSerialized]
        private Dictionary<CultureInfo, string> _names;
        public string GetName(CultureInfo culture = null)
        {
            string name_str = AssetType == AssetType.Share ? ShareName : Name;
            if (_names == null)
            {
                _names = ((JArray)JObject.Parse(name_str)).ToDictionary(p => CultureInfo.GetCultureInfo(p["lang"].AsString()), p => p["name"].AsString());
            }
            if (culture == null) culture = CultureInfo.CurrentCulture;
            if (_names.ContainsKey(culture))
            {
                return _names[culture];
            }
            else if (_names.ContainsKey(CultureInfo.GetCultureInfo("en")))
            {
                return _names[CultureInfo.GetCultureInfo("en")];
            }
            else
            {
                return _names.Values.First();
            }
        }

        public override UInt160[] GetScriptHashesForVerifying()
        {
            UInt160 issuer = SignatureContract.CreateSignatureRedeemScript(Issuer).ToScriptHash();
            return base.GetScriptHashesForVerifying().Union(new UInt160[] { issuer, Admin }).OrderBy(p => p).ToArray();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write((byte)AssetType);
            writer.WriteVarString(Name);
            writer.Write(Amount);
            writer.Write(Issuer);
            writer.Write(Admin);
        }

        public override string ToString()
        {
            return GetName();
        }
    }
}
