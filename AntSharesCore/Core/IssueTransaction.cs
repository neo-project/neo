using System;
using System.IO;

namespace AntShares.Core
{
    public class IssueTransaction : Transaction
    {
        public IssueTransaction()
            : base(TransactionType.IssueTransaction)
        {
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
        }

        public override UInt160[] GetScriptHashesForVerifying()
        {
            //TODO: 资产分发交易的签名验证列表
            //1. 所有的交易输入地址；
            //2. 资产的管理员；
            //3. 如果是股权交易，则还要包含所有的接受者；
            throw new NotImplementedException();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
        }
    }
}
