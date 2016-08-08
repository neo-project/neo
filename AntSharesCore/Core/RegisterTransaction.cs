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
    /// <summary>
    /// 用于资产登记的特殊交易
    /// </summary>
    public class RegisterTransaction : Transaction
    {
        /// <summary>
        /// 资产类别
        /// </summary>
        public AssetType AssetType;
        /// <summary>
        /// 资产名称
        /// </summary>
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
        /// <summary>
        /// 发行者的公钥
        /// </summary>
        public ECPoint Issuer;
        /// <summary>
        /// 资产管理员的合约散列值
        /// </summary>
        public UInt160 Admin;

        private static readonly string ShareName = "[{'lang':'zh-CN','name':'股权'},{'lang':'en','name':'Share'}]";

        /// <summary>
        /// 系统费用
        /// </summary>
        public override Fixed8 SystemFee
        {
            get
            {
                if (AssetType == AssetType.AntShare || AssetType == AssetType.AntCoin)
                    return Fixed8.Zero;
#if TESTNET
                return Fixed8.FromDecimal(100);
#else
                return Fixed8.FromDecimal(10000);
#endif
            }
        }

        public RegisterTransaction()
            : base(TransactionType.RegisterTransaction)
        {
        }

        /// <summary>
        /// 反序列化交易中额外的数据
        /// </summary>
        /// <param name="reader">数据来源</param>
        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            this.AssetType = (AssetType)reader.ReadByte();
            if (!Enum.IsDefined(typeof(AssetType), AssetType) || AssetType == AssetType.CreditFlag || AssetType == AssetType.DutyFlag)
                throw new FormatException();
            this.Name = reader.ReadVarString();
            this.Amount = reader.ReadSerializable<Fixed8>();
            if (Amount == Fixed8.Zero || Amount < -Fixed8.Satoshi) throw new FormatException();
            if (AssetType == AssetType.Share && Amount <= Fixed8.Zero)
                throw new FormatException();
            if (AssetType == AssetType.Invoice && Amount != -Fixed8.Satoshi)
                throw new FormatException();
            this.Issuer = ECPoint.DeserializeFrom(reader, ECCurve.Secp256r1);
            this.Admin = reader.ReadSerializable<UInt160>();
        }

        [NonSerialized]
        private Dictionary<CultureInfo, string> _names;
        /// <summary>
        /// 获取资产的本地化名称
        /// </summary>
        /// <param name="culture">区域性/语言名称</param>
        /// <returns>返回资产名称</returns>
        public string GetName(CultureInfo culture = null)
        {
            string name_str = AssetType == AssetType.Share ? ShareName : Name;
            if (_names == null)
            {
                JObject name_obj = JObject.Parse(name_str);
                if (name_obj is JString)
                    _names = new Dictionary<CultureInfo, string> { { CultureInfo.GetCultureInfo("en"), name_obj.AsString() } };
                else
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

        /// <summary>
        /// 获取需要校验的脚本Hash值
        /// </summary>
        /// <returns>返回需要校验的脚本Hash值</returns>
        public override UInt160[] GetScriptHashesForVerifying()
        {
            UInt160 issuer = SignatureContract.CreateSignatureRedeemScript(Issuer).ToScriptHash();
            return base.GetScriptHashesForVerifying().Union(new UInt160[] { issuer, Admin }).OrderBy(p => p).ToArray();
        }

        /// <summary>
        /// 序列化交易中额外的数据
        /// </summary>
        /// <param name="writer">存放序列化后的结果</param>
        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write((byte)AssetType);
            writer.WriteVarString(Name);
            writer.Write(Amount);
            writer.Write(Issuer);
            writer.Write(Admin);
        }

        /// <summary>
        /// 变成json对象
        /// </summary>
        /// <returns>返回json对象</returns>
        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["asset"] = new JObject();
            json["asset"]["type"] = AssetType;
            try
            {
                json["asset"]["name"] = Name == "" ? null : JObject.Parse(Name);
            }
            catch (FormatException)
            {
                json["asset"]["name"] = Name;
            }
            json["asset"]["amount"] = Amount.ToString();
            json["asset"]["high"] = Amount.GetData() >> 32;
            json["asset"]["low"] = Amount.GetData() & 0xffffffff;
            json["asset"]["issuer"] = Issuer.ToString();
            json["asset"]["admin"] = Wallet.ToAddress(Admin);
            return json;
        }

        /// <summary>
        /// 返回资产的本地化名称
        /// </summary>
        /// <returns>返回资产的本地化名称</returns>
        public override string ToString()
        {
            return GetName();
        }
    }
}
