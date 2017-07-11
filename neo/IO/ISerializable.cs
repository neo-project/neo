using System.IO;

namespace Neo.IO
{
    /// <summary>
    /// 为序列化提供一个接口
    /// </summary>
    public interface ISerializable
    {
        int Size { get; }

        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="writer">存放序列化后的结果</param>
        void Serialize(BinaryWriter writer);

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="reader">数据来源</param>
        void Deserialize(BinaryReader reader);
    }
}
