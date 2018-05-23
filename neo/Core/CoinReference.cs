using Neo.IO;
using Neo.IO.Json;
using Neo.VM;
using System;
using System.IO;

namespace Neo.Core
{
	/// <summary>
	/// 交易输入
	/// Transaction input
	/// </summary>
	public class CoinReference : IEquatable<CoinReference>, IInteropInterface, ISerializable
    {
		/// <summary>
		/// 引用交易的散列值
		/// Reference hash value of the transaction
		/// </summary>
		public UInt256 PrevHash;
        /// <summary>
        /// 引用交易输出的索引
		/// Reference transaction index
        /// </summary>
        public ushort PrevIndex;

        public int Size => PrevHash.Size + sizeof(ushort);

        void ISerializable.Deserialize(BinaryReader reader)
        {
            PrevHash = reader.ReadSerializable<UInt256>();
            PrevIndex = reader.ReadUInt16();
        }

		/// <summary>
		/// 比较当前对象与指定对象是否相等
		/// Compares the current object
		/// </summary>
		/// <param name="other">要比较的对象 the object to be compared</param>
		/// <returns>返回对象是否相等 Return whether the object is equal</returns>
		public bool Equals(CoinReference other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (ReferenceEquals(null, other)) return false;
            return PrevHash.Equals(other.PrevHash) && PrevIndex.Equals(other.PrevIndex);
        }

		/// <summary>
		/// 比较当前对象与指定对象是否相等
		/// Compares the current object
		/// </summary>
		/// <param name="obj">要比较的对象 the object to be compared</param>
		/// <returns>返回对象是否相等 Return whether the object is equal</returns>
		public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (ReferenceEquals(null, obj)) return false;
            if (!(obj is CoinReference)) return false;
            return Equals((CoinReference)obj);
        }

		/// <summary>
		/// 获得对象的HashCode
		/// Get the HashCode of the object
		/// </summary>
		/// <returns>返回对象的HashCode Return the object's HashCode</returns>
		public override int GetHashCode()
        {
            return PrevHash.GetHashCode() + PrevIndex.GetHashCode();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(PrevHash);
            writer.Write(PrevIndex);
        }

		/// <summary>
		/// 将交易输入转变为json对象
		/// Convert transaction input to json object
		/// </summary>
		/// <returns>返回json对象 json representation of the object</returns>
		public JObject ToJson()
        {
            JObject json = new JObject();
            json["txid"] = PrevHash.ToString();
            json["vout"] = PrevIndex;
            return json;
        }
    }
}
