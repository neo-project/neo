using AntShares.IO;
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
        }

        public override IEnumerable<TransactionInput> GetAllInputs()
        {
            return Orders.SelectMany(p => p.Inputs).Concat(base.GetAllInputs());
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(Orders);
        }
    }
}
