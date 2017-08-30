using Neo.Core;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.SmartContract;
using Neo.VM;
using System;
using System.IO;
using System.Linq;

namespace Neo.Wallets
{
    public class VerificationContract : Contract, IEquatable<VerificationContract>, ISerializable
    {
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

        public int Size => PublicKeyHash.Size + ParameterList.GetVarSize() + Script.GetVarSize();

        public static VerificationContract Create(UInt160 publicKeyHash, ContractParameterType[] parameterList, byte[] redeemScript)
        {
            return new VerificationContract
            {
                Script = redeemScript,
                ParameterList = parameterList,
                PublicKeyHash = publicKeyHash
            };
        }

        public static VerificationContract CreateMultiSigContract(UInt160 publicKeyHash, int m, params ECPoint[] publicKeys)
        {
            return new VerificationContract
            {
                Script = CreateMultiSigRedeemScript(m, publicKeys),
                ParameterList = Enumerable.Repeat(ContractParameterType.Signature, m).ToArray(),
                PublicKeyHash = publicKeyHash
            };
        }

        public static VerificationContract CreateSignatureContract(ECPoint publicKey)
        {
            return new VerificationContract
            {
                Script = CreateSignatureRedeemScript(publicKey),
                ParameterList = new[] { ContractParameterType.Signature },
                PublicKeyHash = publicKey.EncodePoint(true).ToScriptHash(),
            };
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="reader">数据来源</param>
        public void Deserialize(BinaryReader reader)
        {
            PublicKeyHash = reader.ReadSerializable<UInt160>();
            ParameterList = reader.ReadVarBytes().Select(p => (ContractParameterType)p).ToArray();
            Script = reader.ReadVarBytes();
        }

        /// <summary>
        /// 比较与另一个对象是否相等
        /// </summary>
        /// <param name="other">另一个对象</param>
        /// <returns>返回比较的结果</returns>
        public bool Equals(VerificationContract other)
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
            return Equals(obj as VerificationContract);
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
        /// 序列化
        /// </summary>
        /// <param name="writer">存放序列化后的结果</param>
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(PublicKeyHash);
            writer.WriteVarBytes(ParameterList.Cast<byte>().ToArray());
            writer.WriteVarBytes(Script);
        }
    }
}
