using Neo.IO;
using Neo.VM;
using System.IO;

namespace Neo.Core
{
	/// <summary>
	/// 为需要签名的数据提供一个接口
	/// Provides an interface for data that can be signed
	/// </summary>
	public interface IVerifiable : ISerializable, IScriptContainer
    {
		/// <summary>
		/// 用于验证该对象的脚本列表
		/// List of scripts used to validate this object
		/// </summary>
		Witness[] Scripts { get; set; }

		/// <summary>
		/// 反序列化未签名的数据
		/// Deserialize unsigned data
		/// </summary>
		/// <param name="reader">数据来源 Data Sources</param>
		void DeserializeUnsigned(BinaryReader reader);

		/// <summary>
		/// 获得需要校验的脚本Hash值
		/// / Get the script hash value that needs to be verified
		/// </summary>
		/// <returns>返回需要校验的脚本Hash值 returns the hash value of the script to be validated</returns>
		UInt160[] GetScriptHashesForVerifying();

		/// <summary>
		/// 序列化未签名的数据
		/// Serialize unsigned data
		/// </summary>
		/// <param name="writer">存放序列化后的结果 Store serialized results</param>
		void SerializeUnsigned(BinaryWriter writer);
    }
}
