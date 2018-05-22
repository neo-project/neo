using Neo.IO;
using Neo.SmartContract;
using Neo.VM;
using System;
using System.IO;
using System.Linq;

namespace Neo.Implementations.Wallets.EntityFramework
{
    public class VerificationContract : SmartContract.Contract, IEquatable<VerificationContract>, ISerializable
    {
        public int Size => 20 + ParameterList.GetVarSize() + Script.GetVarSize();

		/// <summary>
		/// 反序列化
		/// Deserialization
		/// </summary>
		/// <param name="reader">数据来源 Data Sources</param>
		public void Deserialize(BinaryReader reader)
        {
            reader.ReadSerializable<UInt160>();
            ParameterList = reader.ReadVarBytes().Select(p => (ContractParameterType)p).ToArray();
            Script = reader.ReadVarBytes();
        }

        /// <summary>
        /// 比较与另一个对象是否相等
		/// Compare if it's equal to another object
        /// </summary>
        /// <param name="other">另一个对象 other obejct</param>
        /// <returns>返回比较的结果 the result of the comparison</returns>
        public bool Equals(VerificationContract other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (ReferenceEquals(null, other)) return false;
            return ScriptHash.Equals(other.ScriptHash);
        }

		/// <summary>
		/// 比较与另一个对象是否相等
		/// Compare if it's equal to another object
		/// </summary>
		/// <param name="obj">另一个对象 other obejct</param>
		/// <returns>返回比较的结果 the result of the comparison</returns>
		public override bool Equals(object obj)
        {
            return Equals(obj as VerificationContract);
        }

        /// <summary>
        /// 获得HashCode
		/// HashCode
        /// </summary>
        /// <returns>返回HashCode returns the HashCode</returns>
        public override int GetHashCode()
        {
            return ScriptHash.GetHashCode();
        }

		/// <summary>
		/// 序列化
		/// Serialization
		/// </summary>
		/// <param name="writer">存放序列化后的结果 Store serialized results</param>
		public void Serialize(BinaryWriter writer)
        {
            writer.Write(new UInt160());
            writer.WriteVarBytes(ParameterList.Select(p => (byte)p).ToArray());
            writer.WriteVarBytes(Script);
        }
    }
}
