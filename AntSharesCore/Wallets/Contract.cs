using AntShares.Core.Scripts;
using AntShares.Cryptography.ECC;
using AntShares.IO;
using System;
using System.IO;

namespace AntShares.Wallets
{
    /// <summary>
    /// 所有合约的基类
    /// </summary>
    public abstract class Contract : IEquatable<Contract>, ISerializable
    {
        /// <summary>
        /// 合约脚本代码
        /// </summary>
        public byte[] RedeemScript;
        /// <summary>
        /// 公钥散列值，用于标识该合约在钱包中隶属于哪一个账户
        /// </summary>
        public UInt160 PublicKeyHash;

        private string _address;
        /// <summary>
        /// 合约地址
        /// </summary>
        public string Address
        {
            get
            {
                if (_address == null)
                {
                    _address = Wallet.ToAddress(ScriptHash);
                }
                return _address;
            }
        }

        private UInt160 _scriptHash;
        /// <summary>
        /// 脚本散列值
        /// </summary>
        public UInt160 ScriptHash
        {
            get
            {
                if (_scriptHash == null)
                {
                    _scriptHash = RedeemScript.ToScriptHash();
                }
                return _scriptHash;
            }
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="reader">数据来源</param>
        public abstract void Deserialize(BinaryReader reader);

        /// <summary>
        /// 比较与另一个对象是否相等
        /// </summary>
        /// <param name="other">另一个对象</param>
        /// <returns>返回比较的结果</returns>
        public bool Equals(Contract other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (ReferenceEquals(null, other)) return false;
            return ScriptHash.Equals(other.ScriptHash);
        }

        /// <summary>
        /// 比较与另一个对象是否相等
        /// </summary>
        /// <param name="obj">另一个对象</param>
        /// <returns>返回比较的结果</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as Contract);
        }

        /// <summary>
        /// 获得HashCode
        /// </summary>
        /// <returns>返回HashCode</returns>
        public override int GetHashCode()
        {
            return ScriptHash.GetHashCode();
        }

        /// <summary>
        /// 判断由指定的账户签名后，合约是否可以达成
        /// </summary>
        /// <param name="publicKeys">公钥列表</param>
        /// <returns>返回判断的结果</returns>
        public abstract bool IsCompleted(ECPoint[] publicKeys);

        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="writer">存放序列化后的结果</param>
        public abstract void Serialize(BinaryWriter writer);
    }
}
