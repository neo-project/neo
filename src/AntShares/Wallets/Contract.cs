using AntShares.Core;
using AntShares.Cryptography.ECC;
using AntShares.IO;
using AntShares.VM;
using System;
using System.IO;
using System.Linq;

namespace AntShares.Wallets
{
    public class Contract : IEquatable<Contract>, ISerializable
    {
        /// <summary>
        /// 合约脚本代码
        /// </summary>
        public byte[] RedeemScript;

        /// <summary>
        /// 合约的形式参数列表
        /// </summary>
        public ContractParameterType[] ParameterList;

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

        public bool IsStandard
        {
            get
            {
                if (RedeemScript.Length != 35) return false;
                if (RedeemScript[0] != 33 || RedeemScript[34] != (byte)ScriptOp.OP_CHECKSIG)
                    return false;
                return true;
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

        public int Size => PublicKeyHash.Size + ParameterList.Length.GetVarSize() + ParameterList.Length + RedeemScript.Length.GetVarSize() + RedeemScript.Length;

        public static Contract Create(UInt160 publicKeyHash, ContractParameterType[] parameterList, byte[] redeemScript)
        {
            return new Contract
            {
                RedeemScript = redeemScript,
                ParameterList = parameterList,
                PublicKeyHash = publicKeyHash
            };
        }

        public static Contract CreateMultiSigContract(UInt160 publicKeyHash, int m, params ECPoint[] publicKeys)
        {
            return new Contract
            {
                RedeemScript = CreateMultiSigRedeemScript(m, publicKeys),
                ParameterList = Enumerable.Repeat(ContractParameterType.Signature, m).ToArray(),
                PublicKeyHash = publicKeyHash
            };
        }

        public static byte[] CreateMultiSigRedeemScript(int m, params ECPoint[] publicKeys)
        {
            if (!(1 <= m && m <= publicKeys.Length && publicKeys.Length <= 1024))
                throw new ArgumentException();
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.Push(m);
                foreach (ECPoint publicKey in publicKeys.OrderBy(p => p))
                {
                    sb.Push(publicKey.EncodePoint(true));
                }
                sb.Push(publicKeys.Length);
                sb.Add(ScriptOp.OP_CHECKMULTISIG);
                return sb.ToArray();
            }
        }

        public static Contract CreateSignatureContract(ECPoint publicKey)
        {
            return new Contract
            {
                RedeemScript = CreateSignatureRedeemScript(publicKey),
                ParameterList = new[] { ContractParameterType.Signature },
                PublicKeyHash = publicKey.EncodePoint(true).ToScriptHash(),
            };
        }

        public static byte[] CreateSignatureRedeemScript(ECPoint publicKey)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.Push(publicKey.EncodePoint(true));
                sb.Add(ScriptOp.OP_CHECKSIG);
                return sb.ToArray();
            }
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="reader">数据来源</param>
        public void Deserialize(BinaryReader reader)
        {
            PublicKeyHash = reader.ReadSerializable<UInt160>();
            ParameterList = reader.ReadVarBytes().Select(p => (ContractParameterType)p).ToArray();
            RedeemScript = reader.ReadVarBytes();
        }

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
        /// 序列化
        /// </summary>
        /// <param name="writer">存放序列化后的结果</param>
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(PublicKeyHash);
            writer.WriteVarBytes(ParameterList.Cast<byte>().ToArray());
            writer.WriteVarBytes(RedeemScript);
        }
    }
}
