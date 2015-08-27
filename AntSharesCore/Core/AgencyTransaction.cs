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
    /// 9. 交易数量精确到10^-4，交易价格精确到10^-4；
    /// </summary>
    public class AgencyTransaction : Transaction
    {
        public UInt256 AssetId;
        public UInt256 ValueAssetId;
        public UInt160 Agent;
        public Order[] Orders;
        public SplitOrder SplitOrder;

        public AgencyTransaction()
            : base(TransactionType.AgencyTransaction)
        {
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            this.AssetId = reader.ReadSerializable<UInt256>();
            this.ValueAssetId = reader.ReadSerializable<UInt256>();
            if (AssetId == ValueAssetId) throw new FormatException();
            this.Agent = reader.ReadSerializable<UInt160>();
            this.Orders = new Order[reader.ReadVarInt()];
            for (int i = 0; i < Orders.Length; i++)
            {
                Orders[i] = new Order();
                Orders[i].DeserializeInTransaction(reader, this);
            }
            ulong count = reader.ReadVarInt();
            if (count > 1) throw new FormatException();
            if (count == 0)
            {
                this.SplitOrder = null;
            }
            else
            {
                this.SplitOrder = reader.ReadSerializable<SplitOrder>();
            }
        }

        public override IEnumerable<TransactionInput> GetAllInputs()
        {
            return Orders.SelectMany(p => p.Inputs).Concat(base.GetAllInputs());
        }

        public override UInt160[] GetScriptHashesForVerifying()
        {
            HashSet<UInt160> hashes = new HashSet<UInt160>();
            foreach (var group in Inputs.GroupBy(p => p.PrevTxId))
            {
                Transaction tx = Blockchain.Default.GetTransaction(group.Key);
                if (tx == null) throw new InvalidOperationException();
                AgencyTransaction tx_agency = tx as AgencyTransaction;
                if (tx_agency?.SplitOrder == null || tx_agency.AssetId != AssetId || tx_agency.ValueAssetId != ValueAssetId || tx_agency.Agent != Agent)
                {
                    hashes.UnionWith(group.Select(p => tx.Outputs[p.PrevIndex].ScriptHash));
                }
                else
                {
                    hashes.UnionWith(group.Select(p => tx.Outputs[p.PrevIndex].ScriptHash).Where(p => p != tx_agency.SplitOrder.Client));
                }
            }
            hashes.Add(Agent);
            return hashes.OrderBy(p => p).ToArray();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(AssetId);
            writer.Write(ValueAssetId);
            writer.Write(Agent);
            writer.WriteVarInt(Orders.Length);
            for (int i = 0; i < Orders.Length; i++)
            {
                Orders[i].SerializeInTransaction(writer);
            }
            if (SplitOrder == null)
            {
                writer.WriteVarInt(0);
            }
            else
            {
                writer.WriteVarInt(1);
                writer.Write(SplitOrder);
            }
        }

        //TODO: 此处需要较多的测试来证明它的正确性
        //因为委托交易的验证算法有点太复杂了，
        //考虑未来是否可以优化这个算法
        public override VerificationResult Verify()
        {
            VerificationResult result = base.Verify();
            foreach (Order order in Orders)
            {
                result |= order.Verify();
                if (result.HasFlag(VerificationResult.InvalidSignature))
                    break;
            }
            RegisterTransaction asset_value = Blockchain.Default.GetTransaction(ValueAssetId) as RegisterTransaction;
            if (asset_value?.AssetType != AssetType.Currency)
            {
                result |= VerificationResult.IncorrectFormat;
                return result;
            }
            List<Order> orders = new List<Order>(Orders);
            foreach (var group in Inputs.GroupBy(p => p.PrevTxId))
            {
                Transaction tx = Blockchain.Default.GetTransaction(group.Key);
                if (tx == null)
                {
                    result |= VerificationResult.LackOfInformation;
                    return result;
                }
                AgencyTransaction tx_agency = tx as AgencyTransaction;
                if (tx_agency?.SplitOrder == null || tx_agency.AssetId != AssetId || tx_agency.ValueAssetId != ValueAssetId || tx_agency.Agent != Agent)
                    continue;
                var outputs = group.Select(p => new
                {
                    Input = p,
                    Output = tx_agency.Outputs[p.PrevIndex]
                }).Where(p => p.Output.ScriptHash == tx_agency.SplitOrder.Client).ToDictionary(p => p.Input, p => p.Output);
                if (outputs.Count == 0) continue;
                if (outputs.Count != tx_agency.Outputs.Count(p => p.ScriptHash == tx_agency.SplitOrder.Client))
                {
                    result |= VerificationResult.IncorrectFormat;
                    return result;
                }
                orders.Add(new Order
                {
                    AssetId = this.AssetId,
                    ValueAssetId = this.ValueAssetId,
                    Agent = this.Agent,
                    Amount = tx_agency.SplitOrder.Amount,
                    Price = tx_agency.SplitOrder.Price,
                    Client = tx_agency.SplitOrder.Client,
                    Inputs = outputs.Keys.ToArray()
                });
            }
            if (orders.Count < 2)
            {
                result |= VerificationResult.IncorrectFormat;
                return result;
            }
            if (orders.Count(p => p.Amount > Fixed8.Zero) == 0 || orders.Count(p => p.Amount < Fixed8.Zero) == 0)
            {
                result |= VerificationResult.IncorrectFormat;
                return result;
            }
            Fixed8 amount_unmatched = orders.Sum(p => p.Amount);
            if (amount_unmatched == Fixed8.Zero)
            {
                if (SplitOrder != null)
                {
                    result |= VerificationResult.IncorrectFormat;
                    return result;
                }
            }
            else
            {
                if (SplitOrder?.Amount != amount_unmatched)
                {
                    result |= VerificationResult.IncorrectFormat;
                    return result;
                }
            }
            foreach (Order order in orders)
            {
                TransactionOutput[] inputs = order.Inputs.Select(p => References[p]).ToArray();
                if (order.Amount > Fixed8.Zero)
                {
                    if (inputs.Any(p => p.AssetId != order.ValueAssetId))
                    {
                        result |= VerificationResult.IncorrectFormat;
                        return result;
                    }
                    if (inputs.Sum(p => p.Value) < order.Amount * order.Price)
                    {
                        result |= VerificationResult.Imbalanced;
                        return result;
                    }
                }
                else
                {
                    if (inputs.Any(p => p.AssetId != order.AssetId))
                    {
                        result |= VerificationResult.IncorrectFormat;
                        return result;
                    }
                    if (inputs.Sum(p => p.Value) < order.Amount)
                    {
                        result |= VerificationResult.Imbalanced;
                        return result;
                    }
                }
            }
            if (SplitOrder != null)
            {
                Fixed8 price_worst = amount_unmatched > Fixed8.Zero ? orders.Min(p => p.Price) : orders.Max(p => p.Price);
                if (SplitOrder.Price != price_worst)
                {
                    result |= VerificationResult.IncorrectFormat;
                    return result;
                }
                Order[] orders_worst = orders.Where(p => p.Price == price_worst && p.Client == SplitOrder.Client).ToArray();
                if (orders_worst.Length == 0)
                {
                    result |= VerificationResult.IncorrectFormat;
                    return result;
                }
                Fixed8 amount_worst = orders_worst.Sum(p => p.Amount);
                if (amount_worst.Abs() < amount_unmatched.Abs())
                {
                    result |= VerificationResult.IncorrectFormat;
                    return result;
                }
                Order order_combine = new Order
                {
                    AssetId = this.AssetId,
                    ValueAssetId = this.ValueAssetId,
                    Agent = this.Agent,
                    Amount = amount_worst - amount_unmatched,
                    Price = price_worst,
                    Client = SplitOrder.Client,
                    Inputs = orders_worst.SelectMany(p => p.Inputs).ToArray()
                };
                foreach (Order order_worst in orders_worst)
                {
                    orders.Remove(order_worst);
                }
                orders.Add(order_combine);
            }
            foreach (var group in orders.GroupBy(p => p.Client))
            {
                TransactionOutput[] inputs = group.SelectMany(p => p.Inputs).Select(p => References[p]).ToArray();
                TransactionOutput[] outputs = Outputs.Where(p => p.ScriptHash == group.Key).ToArray();
                Fixed8 money_spent = inputs.Where(p => p.AssetId == ValueAssetId).Sum(p => p.Value) - outputs.Where(p => p.AssetId == ValueAssetId).Sum(p => p.Value);
                Fixed8 amount_changed = outputs.Where(p => p.AssetId == AssetId).Sum(p => p.Value) - inputs.Where(p => p.AssetId == AssetId).Sum(p => p.Value);
                if (amount_changed != group.Sum(p => p.Amount))
                {
                    result |= VerificationResult.Imbalanced;
                    break;
                }
                if (money_spent > group.Sum(p => p.Amount * p.Price))
                {
                    result |= VerificationResult.Imbalanced;
                    break;
                }
            }
            return result;
        }
    }
}
