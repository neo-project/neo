using Neo.IO;
using Neo.VM;
using System.IO;

namespace Neo.Core
{
    /// <summary>
    /// 为需要签名的数据提供一个接口
    /// </summary>
    public interface IVerifiable : ISerializable, IScriptContainer
    {
        /// <summary>
        /// 用于验证该对象的脚本列表
        /// </summary>
        Witness[] Scripts { get; set; }
        
        /// <summary>
        /// 反序列化未签名的数据
        /// </summary>
        /// <param name="reader">数据来源</param>
        void DeserializeUnsigned(BinaryReader reader);

        /// <summary>
        /// 获得需要校验的脚本Hash值
        /// </summary>
        /// <returns>返回需要校验的脚本Hash值</returns>
        UInt160[] GetScriptHashesForVerifying();
        
        /// <summary>
        /// 序列化未签名的数据
        /// </summary>
        /// <param name="writer">存放序列化后的结果</param>
        void SerializeUnsigned(BinaryWriter writer);
    }
}
