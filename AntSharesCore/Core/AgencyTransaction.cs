using AntShares.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AntShares.Core
{
    public class AgencyTransaction : Transaction
    {
        public Order[] Orders;

        public AgencyTransaction()
            : base(TransactionType.AgencyTransaction)
        {
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            this.Orders = reader.ReadSerializableArray<Order>();
            if (Orders.Length < 2) throw new FormatException();
            if (Orders.Select(p => p.Agent).Distinct().Count() != 1)
                throw new FormatException();
            if (Orders.Select(p => p.AssetId).Distinct().Count() != 1)
                throw new FormatException();
            if (Orders.Select(p => p.ValueAssetId).Distinct().Count() != 1)
                throw new FormatException();
            if (Orders.Count(p => p.Amount > Fixed8.Zero) == 0 || Orders.Count(p => p.Amount < Fixed8.Zero) == 0)
                throw new FormatException();
        }

        public override IEnumerable<TransactionInput> GetAllInputs()
        {
            return Orders.SelectMany(p => p.Inputs).Concat(base.GetAllInputs());
        }

        public override UInt160[] GetScriptHashesForVerifying()
        {
            //TODO: 未完全成交订单的输出作为本次输入的，只需代理人签名即可
            //对于某些交易输入，可能来自于上一次的撮合交易中未完全成交的订单，
            //这些输入应当由交易的代理人签名，而不是资产所有者签名
            return base.GetScriptHashesForVerifying().Union(new UInt160[] { Orders[0].Agent }).OrderBy(p => p).ToArray();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(Orders);
        }

        public override bool Verify()
        {
            if (!base.Verify()) return false;
            foreach (Order order in Orders)
                if (!order.VerifySignature())
                    return false;
            return true;
        }

        internal override bool VerifyBalance()
        {
            if (Outputs.Any(p => p.Value <= Fixed8.Zero))
                return false;
            IDictionary<TransactionInput, TransactionOutput> references = GetReferences();
            IDictionary<UInt256, TransactionResult> results = references.Values.Select(p => new
            {
                AssetId = p.AssetId,
                Value = p.Value
            }).Concat(Outputs.Select(p => new
            {
                AssetId = p.AssetId,
                Value = -p.Value
            })).GroupBy(p => p.AssetId, (k, g) => new TransactionResult
            {
                AssetId = k,
                Amount = g.Sum(p => p.Value)
            }).Where(p => p.Amount != Fixed8.Zero).ToDictionary(p => p.AssetId);
            if (results.Count > 1) return false;
            if (results.Count == 1 && !results.ContainsKey(Blockchain.AntCoin.Hash))
                return false;
            if (SystemFee > Fixed8.Zero && (results.Count == 0 || results[Blockchain.AntCoin.Hash].Amount < SystemFee))
                return false;
            foreach (Order order in Orders)
            {
                TransactionOutput[] order_references = order.Inputs.Select(p => references[p]).ToArray();
                if (order.Amount > Fixed8.Zero)
                {
                    if (order_references.Any(p => p.AssetId != order.ValueAssetId))
                        return false;
                    if (order_references.Sum(p => p.Value) < Fixed8.Multiply(order.Amount, order.Price))
                        return false;
                }
                else
                {
                    if (order_references.Any(p => p.AssetId != order.AssetId))
                        return false;
                    if (order_references.Sum(p => p.Value) < order.Amount)
                        return false;
                }
            }
            //TODO: 所有订单中，最多只能有一个订单未完全成交
            //TODO: 成交是否符合每一个订单的要求（价格、数量等）
        }
    }
}
