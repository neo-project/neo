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
            if (Orders.Count(p => p.Amount > 0) == 0 || Orders.Count(p => p.Amount < 0) == 0)
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
            //TODO: 验证合法性
            //1. 输入输出
            //2. 成交是否符合每一个订单的要求（价格、数量等）
            //3. 所有订单中，最多只能有一个订单未完全成交
            //4. 每个订单的输入必须包含足额的交易物，且不能包含其它输入
        }
    }
}
