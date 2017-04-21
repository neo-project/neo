using AntShares.Cryptography.ECC;
using AntShares.IO;
using AntShares.IO.Json;
using AntShares.Wallets;
using System;
using System.Collections.Generic;
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
        /// </summary>
        public Fixed8 Amount;
        public byte Precision;
        /// <summary>
        /// 发行者的公钥
        /// </summary>
        public ECPoint Owner;
        /// <summary>
        /// 资产管理员的合约散列值
        /// </summary>
        public UInt160 Admin;

        public override int Size => base.Size + sizeof(AssetType) + Name.GetVarSize() + Amount.Size + sizeof(byte) + Owner.Size + Admin.Size;

        /// <summary>
        /// 系统费用
        /// </summary>
        public override Fixed8 SystemFee
        {
            get
            {
                if (AssetType == AssetType.SystemShare || AssetType == AssetType.SystemCoin)
                    return Fixed8.Zero;
                return base.SystemFee;
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
            AssetType = (AssetType)reader.ReadByte();
            Name = reader.ReadVarString(1024);
            Amount = reader.ReadSerializable<Fixed8>();
            Precision = reader.ReadByte();
            Owner = ECPoint.DeserializeFrom(reader, ECCurve.Secp256r1);
            if (Owner.IsInfinity && AssetType != AssetType.SystemShare && AssetType != AssetType.SystemCoin)
                throw new FormatException();
            Admin = reader.ReadSerializable<UInt160>();
        }

        /// <summary>
        /// 获取需要校验的脚本Hash值
        /// </summary>
        /// <returns>返回需要校验的脚本Hash值</returns>
        public override UInt160[] GetScriptHashesForVerifying()
        {
            UInt160 owner = Contract.CreateSignatureRedeemScript(Owner).ToScriptHash();
            return base.GetScriptHashesForVerifying().Union(new[] { owner }).OrderBy(p => p).ToArray();
        }

        protected override void OnDeserialized()
        {
            base.OnDeserialized();
            if (AssetType == AssetType.SystemShare && !Hash.Equals(Blockchain.SystemShare.Hash))
                throw new FormatException();
            if (AssetType == AssetType.SystemCoin && !Hash.Equals(Blockchain.SystemCoin.Hash))
                throw new FormatException();
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
            writer.Write(Precision);
            writer.Write(Owner);
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
            json["asset"]["precision"] = Precision;
            json["asset"]["owner"] = Owner.ToString();
            json["asset"]["admin"] = Wallet.ToAddress(Admin);
            return json;
        }

        public override bool Verify(IEnumerable<Transaction> mempool)
        {
            if (!base.Verify(mempool)) return false;
            if (!Enum.IsDefined(typeof(AssetType), AssetType) || AssetType == AssetType.CreditFlag || AssetType == AssetType.DutyFlag)
                return false;
            if (Amount == Fixed8.Zero || Amount < -Fixed8.Satoshi) return false;
            if (AssetType == AssetType.Invoice && Amount != -Fixed8.Satoshi)
                return false;
            if (Precision > 8) return false;
            if (AssetType == AssetType.Share && Precision != 0)
                return false;
            if (Amount != -Fixed8.Satoshi && Amount.GetData() % (long)Math.Pow(10, 8 - Precision) != 0)
                return false;
            return true;
        }
    }
}
