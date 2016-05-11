using AntShares.Cryptography.ECC;
using AntShares.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AntShares.IO.Json;

namespace AntShares.Core
{
    /// <summary>
    /// 用于投票选出记账人的特殊交易
    /// </summary>
    public class VotingTransaction : Transaction
    {
        /// <summary>
        /// 报名表的散列值列表，本交易中的选票将投给这些报名表所指代的候选人
        /// </summary>
        public UInt256[] Enrollments;

        /// <summary>
        /// 系统费用
        /// </summary>
        public override Fixed8 SystemFee => Fixed8.FromDecimal(10);

        public VotingTransaction()
            : base(TransactionType.VotingTransaction)
        {
        }

        /// <summary>
        /// 反序列化交易中的额外数据
        /// </summary>
        /// <param name="reader">数据来源</param>
        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            this.Enrollments = reader.ReadSerializableArray<UInt256>();
            if (Enrollments.Length == 0 || Enrollments.Length > 1024)
                throw new FormatException();
            if (Enrollments.Length != Enrollments.Distinct().Count())
                throw new FormatException();
        }

        /// <summary>
        /// 反序列化进行完毕时触发
        /// </summary>
        protected override void OnDeserialized()
        {
            base.OnDeserialized();
            if (Outputs.All(p => p.AssetId != Blockchain.AntShare.Hash))
                throw new FormatException();
        }

        /// <summary>
        /// 序列化交易中的额外数据
        /// </summary>
        /// <param name="writer">存放序列化后的结果</param>
        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(Enrollments);
        }

        /// <summary>
        /// 将交易转变为json对象的形式
        /// </summary>
        /// <returns>返回Json对象</returns>
        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["enrollments"] = new JArray(Enrollments.Select(p => (JObject)p.ToString()).ToArray());
            return json;
        }

        /// <summary>
        /// 验证交易
        /// </summary>
        /// <returns>返回验证结果</returns>
        public override bool Verify()
        {
            if (!base.Verify()) return false;
            if (!Blockchain.Default.Ability.HasFlag(BlockchainAbility.UnspentIndexes))
                return false;
            HashSet<ECPoint> pubkeys = new HashSet<ECPoint>();
            foreach (UInt256 vote in Enrollments)
            {
                EnrollmentTransaction tx = Blockchain.Default.GetTransaction(vote) as EnrollmentTransaction;
                if (tx == null) return false;
                if (!Blockchain.Default.ContainsUnspent(vote, 0)) return false;
                if (!pubkeys.Add(tx.PublicKey)) return false;
            }
            return true;
        }
    }
}
