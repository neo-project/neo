using System.IO;

namespace Neo.IO
{
	/// <summary>
	/// 为序列化提供一个接口
	/// Provides an interface for serialization
	/// </summary>
	public interface ISerializable
    {
        int Size { get; }

		/// <summary>
		/// 序列化
		/// Serialization
		/// </summary>
		/// <param name="writer">存放序列化后的结果 Store serialized results</param>
		void Serialize(BinaryWriter writer);

        /// <summary>
        /// 反序列化
		/// Deserialization
        /// </summary>
        /// <param name="reader">数据来源 Data Sources</param>
        void Deserialize(BinaryReader reader);
    }
}
