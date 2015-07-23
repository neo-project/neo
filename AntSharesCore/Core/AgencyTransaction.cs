using AntShares.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AntShares.Core
{
    /// <summary>
    /// 交易规则：
    /// 1. 单个交易中，所有订单的代理人必须是同一人；
    /// 2. 单个交易中，所有订单的交易商品必须完全相同，交易货币也必须完全相同；
    /// 3. 交易商品不能和交易货币相同；
    /// 4. 买盘和卖盘两者都至少需要包含一笔订单；
    /// 5. 交易中不能包含完全未成交的订单，且至多只能包含一笔部分成交的订单；
    /// 6. 如果存在部分成交的订单，则该订单的价格必须是最差的，即：对于买单，它的价格是最低价格；对于卖单，它的价格是最高价格；
    /// 7. 对于买单，需以不高于委托方所指定的价格成交；
    /// 8. 对于卖单，需以不低于委托方所指定的价格成交；
    /// 9. 交易数量精确到10^-5，交易价格精确到10^-3；
    /// </summary>
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

        //TODO: 此处需要较多的测试来证明它的正确性
        //因为委托交易的验证算法有点太复杂了，
        //考虑未来是否可以优化这个算法
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
            Order[] orders = Orders; //TODO: 对于每一个非订单输入，要检查是否是上一轮撮合中的部分成交订单，然后加入到本轮的订单列表中
            foreach (Order order in orders)
            {
                TransactionOutput[] inputs = order.Inputs.Select(p => references[p]).ToArray();
                if (order.Amount > Fixed8.Zero)
                {
                    if (inputs.Any(p => p.AssetId != order.ValueAssetId))
                        return false;
                    if (inputs.Sum(p => p.Value) < Fixed8.Multiply(order.Amount, order.Price))
                        return false;
                }
                else
                {
                    if (inputs.Any(p => p.AssetId != order.AssetId))
                        return false;
                    if (inputs.Sum(p => p.Value) < order.Amount)
                        return false;
                }
            }
            int partially = 0;
            foreach (var group in orders.GroupBy(p => p.Client))
            {
                TransactionOutput[] inputs = group.SelectMany(p => p.Inputs).Select(p => references[p]).ToArray();
                TransactionOutput[] outputs = Outputs.Where(p => p.ScriptHash == group.Key).ToArray();
                Fixed8 money_spent = inputs.Where(p => p.AssetId == orders[0].ValueAssetId).Sum(p => p.Value) - outputs.Where(p => p.AssetId == orders[0].ValueAssetId).Sum(p => p.Value);
                Fixed8 amount_changed = outputs.Where(p => p.AssetId == orders[0].AssetId).Sum(p => p.Value) - inputs.Where(p => p.AssetId == orders[0].AssetId).Sum(p => p.Value);
                Fixed8 amount_group = group.Sum(p => p.Amount);
                if (amount_changed == amount_group)
                {
                    if (money_spent > group.Sum(p => Fixed8.Multiply(p.Amount, p.Price)))
                        return false;
                }
                else if (++partially > 1)
                {
                    return false;
                }
                else
                {
                    Fixed8 amount_diff = orders.Sum(p => p.Amount);
                    if (amount_changed != amount_group - amount_diff)
                        return false;
                    Fixed8 price_worst;
                    if (amount_diff > Fixed8.Zero)
                    {
                        price_worst = group.Min(p => p.Price);
                        if (price_worst != orders.Min(p => p.Price))
                            return false;
                    }
                    else
                    {
                        price_worst = group.Max(p => p.Price);
                        if (price_worst != orders.Max(p => p.Price))
                            return false;
                    }
                    Order[] orders_worst = group.Where(p => p.Price == price_worst).ToArray();
                    Fixed8 amount_worst = orders_worst.Sum(p => p.Amount);
                    if (amount_worst.Abs() < amount_diff.Abs())
                        return false;
                    Order order_combine = new Order
                    {
                        AssetId = orders[0].AssetId,
                        ValueAssetId = orders[0].ValueAssetId,
                        Amount = amount_worst - amount_diff,
                        Price = price_worst,
                        Client = orders[0].Client,
                        Agent = orders[0].Agent,
                        Inputs = orders_worst.SelectMany(p => p.Inputs).ToArray()
                    };
                    List<Order> orders_new = group.Where(p => p.Price != price_worst).ToList();
                    if (order_combine.Amount == Fixed8.Zero)
                        return false;
                    orders_new.Add(order_combine);
                    if (money_spent > orders_new.Sum(p => Fixed8.Multiply(p.Amount, p.Price)))
                        return false;
                }
            }
            return true;
        }
    }
}
