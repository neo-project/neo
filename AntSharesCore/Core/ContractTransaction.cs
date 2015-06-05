using AntShares.IO;
using System;
using System.IO;

namespace AntShares.Core
{
    public class ContractTransaction : Transaction
    {
        public TransactionAttribute[] Attributes;

        public ContractTransaction()
            : base(TransactionType.ContractTransaction)
        {
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            this.Attributes = reader.ReadSerializableArray<TransactionAttribute>();
        }

        public override UInt160[] GetScriptHashesForVerifying()
        {
            //TODO: 判断交易输出是否包含股权
            //1. 取出所有包含股权的交易输出
            //2. 取出其中的输出地址，这些地址是需要签名交易的
            //需要本地区块链数据库，否则无法验证
            //3. 无法验证的情况下，抛出异常：
            //throw new InvalidOperationException();
            //4. 如果不是股权交易合同，则直接返回父类的实现：
            //return base.GetScriptHashesForVerifying();

            throw new NotImplementedException();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(Attributes);
        }
    }
}
