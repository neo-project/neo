using AntShares.Cryptography.ECC;
using AntShares.IO;
using AntShares.IO.Json;
using AntShares.Wallets;
using System;
using System.IO;
using System.Linq;

namespace AntShares.Core
{
    /// <summary>
    /// 用于报名成为记账候选人的特殊交易
    /// </summary>
    public class EnrollmentTransaction : Transaction
    {
        /// <summary>
        /// 记账人的公钥
        /// </summary>
        public ECPoint PublicKey;

        private UInt160 _miner = null;
        /// <summary>
        /// 记账人的抵押地址
        /// </summary>
        public UInt160 Miner
        {
            get
            {
                if (_miner == null)
                {
                    _miner = SignatureContract.Create(PublicKey).ScriptHash;
                }
                return _miner;
            }
        }

        /// <summary>
        /// 系统费用
        /// </summary>
        public override Fixed8 SystemFee => Fixed8.FromDecimal(1000);

        public EnrollmentTransaction()
            : base(TransactionType.EnrollmentTransaction)
        {
        }

        /// <summary>
        /// 序列化交易中的额外数据
        /// </summary>
        /// <param name="reader">数据来源</param>
        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            PublicKey = ECPoint.DeserializeFrom(reader, ECCurve.Secp256r1);
        }

        /// <summary>
        /// 获取需要校验的脚本Hash
        /// </summary>
        /// <returns>返回需要校验的脚本Hash</returns>
        public override UInt160[] GetScriptHashesForVerifying()
        {
            return base.GetScriptHashesForVerifying().Union(new UInt160[] { Miner }).OrderBy(p => p).ToArray();
        }

        /// <summary>
        /// 反序列化进行完毕时触发
        /// </summary>
        protected override void OnDeserialized()
        {
            base.OnDeserialized();
            if (Outputs.Length == 0 || Outputs[0].AssetId != Blockchain.AntCoin.Hash || Outputs[0].ScriptHash != Miner)
                throw new FormatException();
        }

        /// <summary>
        /// 序列化交易中的额外数据
        /// </summary>
        /// <param name="writer">存放序列化后的结果</param>
        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(PublicKey);
        }

        /// <summary>
        /// 变成json对象
        /// </summary>
        /// <returns>返回json对象</returns>
        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["pubkey"] = PublicKey.ToString();
            return json;
        }
    }
}
