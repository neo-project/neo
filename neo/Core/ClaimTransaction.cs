using Neo.IO;
using Neo.IO.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Core
{
    public class ClaimTransaction : Transaction
    {
        public CoinReference[] Claims;

        public override Fixed8 NetworkFee => Fixed8.Zero;

        public override int Size => base.Size + Claims.GetVarSize();

        public ClaimTransaction()
            : base(TransactionType.ClaimTransaction)
        {
        }

        /// <summary>
        /// 反序列化交易中的额外数据
        /// </summary>
        /// <param name="reader">数据来源</param>
        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            if (Version != 0) throw new FormatException();
            Claims = reader.ReadSerializableArray<CoinReference>();
            if (Claims.Length == 0) throw new FormatException();
        }

        /// <summary>
        /// 获得需要校验的脚本Hash
        /// </summary>
        /// <returns>返回需要校验的脚本Hash</returns>
        public override UInt160[] GetScriptHashesForVerifying()
        {
            HashSet<UInt160> hashes = new HashSet<UInt160>(base.GetScriptHashesForVerifying());
            foreach (var group in Claims.GroupBy(p => p.PrevHash))
            {
                Transaction tx = Blockchain.Default.GetTransaction(group.Key);
                if (tx == null) throw new InvalidOperationException();
                foreach (CoinReference claim in group)
                {
                    if (tx.Outputs.Length <= claim.PrevIndex) throw new InvalidOperationException();
                    hashes.Add(tx.Outputs[claim.PrevIndex].ScriptHash);
                }
            }
            return hashes.OrderBy(p => p).ToArray();
        }

        /// <summary>
        /// 序列化交易中的额外数据
        /// </summary>
        /// <param name="writer">存放序列化后的结果</param>
        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(Claims);
        }

        /// <summary>
        /// 变成json对象
        /// </summary>
        /// <returns>返回json对象</returns>
        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["claims"] = new JArray(Claims.Select(p => p.ToJson()).ToArray());
            return json;
        }

        /// <summary>
        /// 验证交易
        /// </summary>
        /// <returns>返回验证结果</returns>
        public override bool Verify(IEnumerable<Transaction> mempool)
        {
            if (!base.Verify(mempool)) return false;
            if (Claims.Length != Claims.Distinct().Count())
                return false;
            if (mempool.OfType<ClaimTransaction>().Where(p => p != this).SelectMany(p => p.Claims).Intersect(Claims).Count() > 0)
                return false;
            TransactionResult result = GetTransactionResults().FirstOrDefault(p => p.AssetId == Blockchain.UtilityToken.Hash);
            if (result == null || result.Amount > Fixed8.Zero) return false;
            try
            {
                return Blockchain.CalculateBonus(Claims, false) == -result.Amount;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }
        }
    }
}
