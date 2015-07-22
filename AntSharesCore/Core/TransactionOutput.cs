using AntShares.IO;
using System.IO;

namespace AntShares.Core
{
    public class TransactionOutput : ISerializable
    {
        public UInt256 AssetId;
        /// <summary>
        /// 输出的金额，其含义根据AssetId所指代的资产的类型不同而不同：
        /// 1. 对于货币，Value表示一个定点数，其值等于实际金额乘以10^8；
        /// 2. 对于其它资产类型，Value表示一个整数，其最小单位是1，不可分割；
        /// </summary>
        public long Value;
        public UInt160 ScriptHash;

        void ISerializable.Deserialize(BinaryReader reader)
        {
            this.AssetId = reader.ReadSerializable<UInt256>();
            this.Value = reader.ReadInt64();
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
