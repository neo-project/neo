using AntShares.IO;
using AntShares.IO.Json;
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
        /// 发行总量，共有3种模式：
        /// 1. 限量模式：当Amount为正数时，表示当前资产的最大总量为Amount，且不可修改（股权在未来可能会支持扩股或增发，会考虑需要公司签名或一定比例的股东签名认可）。
        /// 2. 信贷模式：当Amount等于0时，表示当前资产的发行方式为信贷方式。通常网关会采用这种模式，当用户充值时，产生正负两枚货币，负值表示负债，不可转让；当用户提现时，正负合并归零。无论何时，该资产在链上的总和为0。
        /// 3. 不限量模式：当Amount等于-1时，表示当前资产可以由创建者无限量发行。这种模式的自由度最大，但是公信力最低，不建议使用。
        /// 在使用过程中，根据资产类型的不同，能够使用的总量模式也不同，具体规则如下：
        /// 1. 对于股权，只能使用限量模式；
        /// 2. 对于货币，只能使用信贷模式；
        /// 3. 对于点券，可以使用任意模式；
        /// </summary>
        public Fixed8 Amount;
        public UInt160 Issuer;
        public UInt160 Admin;

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
            if (Amount < -Fixed8.Satoshi) throw new FormatException();
            if (AssetType == AssetType.Share && Amount <= Fixed8.Zero)
                throw new FormatException();
            if (AssetType == AssetType.Currency && Amount != Fixed8.Zero)
                throw new FormatException();
            this.Issuer = reader.ReadSerializable<UInt160>();
            this.Admin = reader.ReadSerializable<UInt160>();
        }

        [NonSerialized]
        private Dictionary<CultureInfo, string> _names;
        public string GetName(CultureInfo culture = null)
        {
            if (AssetType == AssetType.Share)
            {
                //TODO: 获取证书上的名称
                //股权的名称由证书上的公司名称决定，不能自定义
                //目前实名认证的相关设计还没有完全定型，所以暂时先不实现股权资产的名称查询
                throw new NotImplementedException();
            }
            if (_names == null)
            {
                _names = ((JArray)JObject.Parse(Name)).ToDictionary(p => CultureInfo.GetCultureInfo(p["lang"].AsString()), p => p["name"].AsString());
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
            return base.GetScriptHashesForVerifying().Union(new UInt160[] { Issuer, Admin }).OrderBy(p => p).ToArray();
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
            //TODO: 在资产名称的后面加上发行者的名称
            //如：CNY(由xxx公司发行)
            //用以区分不同主体发行的相同名称的资产
            return GetName();
        }
    }
}
