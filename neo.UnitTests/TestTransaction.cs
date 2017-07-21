using Neo.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.UnitTests
{
    public class TestTransaction : Transaction
    {
        public TestTransaction(UInt256 assetId, TransactionType type) : base(type)
        {
            TransactionOutput transVal = new TransactionOutput();
            transVal.Value = Fixed8.FromDecimal(50);
            transVal.AssetId = assetId;           
            base.Outputs = new TransactionOutput[1] { transVal };
        }
    }
}
