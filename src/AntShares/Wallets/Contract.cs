using AntShares.Core;
using AntShares.Cryptography.ECC;
using AntShares.IO;
using AntShares.VM;
using System;
using System.IO;
using System.Linq;

namespace AntShares.Wallets
{
    public class Contract : VerificationCode, IEquatable<Contract>, ISerializable
    {
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
                if (Script.Length != 35) return false;
                if (Script[0] != 33 || Script[34] != (byte)OpCode.CHECKSIG)
                    return false;
                return true;
            }
        }

        public int Size => PublicKeyHash.Size + ParameterList.GetVarSize() + Script.GetVarSize();

        public ContractType Type
        {
            get
            {
                if (IsStandard) return ContractType.SignatureContract;
                if (IsMultiSigContract()) return ContractType.MultiSigContract;
                return ContractType.CustomContract;
            }
        }

        public static Contract Create(UInt160 publicKeyHash, ContractParameterType[] parameterList, byte[] redeemScript)
        {
            return new Contract
            {
                Script = redeemScript,
                ParameterList = parameterList,
                PublicKeyHash = publicKeyHash
            };
        }

        public static Contract CreateMultiSigContract(UInt160 publicKeyHash, int m, params ECPoint[] publicKeys)
        {
            return new Contract
            {
                Script = CreateMultiSigRedeemScript(m, publicKeys),
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
                sb.EmitPush(m);
                foreach (ECPoint publicKey in publicKeys.OrderBy(p => p))
                {
                    sb.EmitPush(publicKey.EncodePoint(true));
                }
                sb.EmitPush(publicKeys.Length);
                sb.Emit(OpCode.CHECKMULTISIG);
                return sb.ToArray();
            }
        }

        public static Contract CreateSignatureContract(ECPoint publicKey)
        {
            return new Contract
            {
                Script = CreateSignatureRedeemScript(publicKey),
                ParameterList = new[] { ContractParameterType.Signature },
                PublicKeyHash = publicKey.EncodePoint(true).ToScriptHash(),
            };
        }

        public static byte[] CreateSignatureRedeemScript(ECPoint publicKey)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitPush(publicKey.EncodePoint(true));
                sb.Emit(OpCode.CHECKSIG);
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
            Script = reader.ReadVarBytes();
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

        private bool IsMultiSigContract()
        {
            int m, n = 0;
            int i = 0;
            if (Script.Length < 37) return false;
            if (Script[i] > (byte)OpCode.PUSH16) return false;
            if (Script[i] < (byte)OpCode.PUSH1 && Script[i] != 1 && Script[i] != 2) return false;
            switch (Script[i])
            {
                case 1:
                    m = Script[++i];
                    ++i;
                    break;
                case 2:
                    m = Script.ToUInt16(++i);
                    i += 2;
                    break;
                default:
                    m = Script[i++] - 80;
                    break;
            }
            if (m < 1 || m > 1024) return false;
            while (Script[i] == 33)
            {
                i += 34;
                if (Script.Length <= i) return false;
                ++n;
            }
            if (n < m || n > 1024) return false;
            switch (Script[i])
            {
                case 1:
                    if (n != Script[++i]) return false;
                    ++i;
                    break;
                case 2:
                    if (n != Script.ToUInt16(++i)) return false;
                    i += 2;
                    break;
                default:
                    if (n != Script[i++] - 80) return false;
                    break;
            }
            if (Script[i++] != (byte)OpCode.CHECKMULTISIG) return false;
            if (Script.Length != i) return false;
            return true;
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
