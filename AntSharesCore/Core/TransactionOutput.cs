using AntShares.IO;
using System.IO;

namespace AntShares.Core
{
    public class TransactionOutput : ISerializable
    {
        public UInt256 AssetId;
        /// <summary>
        /// 交易输出可以为正数或负数，但不能为零。
        /// 如果为负数，那么它必须符合下列要求：
        /// 1. 只有信贷模式的资产可以有负输出；
        /// 2. 负输入需由资产的管理员签名使用，而负输出只能发送到资产的发行者地址；
        /// 3. 负输出只能由IssueTransaction来新增，如果是其它的交易类型，则必须包含绝对值大于或等于输出的负输入；
        /// </summary>
        public Fixed8 Value;
        public UInt160 ScriptHash;

        void ISerializable.Deserialize(BinaryReader reader)
        {
            this.AssetId = reader.ReadSerializable<UInt256>();
            this.Value = reader.ReadSerializable<Fixed8>();
            this.ScriptHash = reader.ReadSerializable<UInt160>();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(AssetId);
            writer.Write(Value);
            writer.Write(ScriptHash);
        }
    }
}
