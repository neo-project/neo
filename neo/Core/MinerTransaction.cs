using Neo.IO.Json;
using System;
using System.IO;
using System.Linq;

namespace Neo.Core
{
    /// <summary>
    /// 用于分配字节费的特殊交易
    /// </summary>
    public class MinerTransaction : Transaction
    {
        /// <summary>
        /// 随机数
        /// </summary>
        public uint Nonce;

        public override Fixed8 NetworkFee => Fixed8.Zero;

        public override int Size => base.Size + sizeof(uint);

        public MinerTransaction()
            : base(TransactionType.MinerTransaction)
        {
        }

        /// <summary>
        /// 反序列化交易中的额外数据
        /// </summary>
        /// <param name="reader">数据来源</param>
        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            if (Version != 0) throw new FormatException();
            this.Nonce = reader.ReadUInt32();
        }

        /// <summary>
        /// 反序列化进行完毕时触发
        /// </summary>
        protected override void OnDeserialized()
        {
            base.OnDeserialized();
            if (Inputs.Length != 0)
                throw new FormatException();
            if (Outputs.Any(p => p.AssetId != Blockchain.UtilityToken.Hash))
                throw new FormatException();
        }

        /// <summary>
        /// 序列化交易中的额外数据
        /// </summary>
        /// <param name="writer">存放序列化后的结果 </param>
        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(Nonce);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["nonce"] = Nonce;
            return json;
        }
    }
}
