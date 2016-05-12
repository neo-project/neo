using AntShares.Core.Scripts;
using AntShares.Cryptography.ECC;
using AntShares.IO;
using System;
using System.IO;
using System.Linq;

namespace AntShares.Wallets
{
    /// <summary>
    /// 多方签名合约，该合约需要指定的N个账户中至少M个账户签名后才能生效
    /// </summary>
    public class MultiSigContract : Contract
    {
        private int m;
        private ECPoint[] publicKeys;

        /// <summary>
        /// 合约的形式参数列表
        /// </summary>
        public override ContractParameterType[] ParameterList => Enumerable.Repeat(ContractParameterType.Signature, m).ToArray();

        /// <summary>
        /// 用指定的N个公钥创建一个MultiSigContract实例，并指定至少需要M个账户的签名
        /// </summary>
        /// <param name="publicKeyHash">合约所属的账户</param>
        /// <param name="m">一个整数，该合约至少需要包含此数量的签名才能生效</param>
        /// <param name="publicKeys">公钥列表，该合约需要此列表中至少m个账户签名后才能生效</param>
        /// <returns>返回一个多方签名合约</returns>
        public static MultiSigContract Create(UInt160 publicKeyHash, int m, params ECPoint[] publicKeys)
        {
            return new MultiSigContract
            {
                RedeemScript = CreateMultiSigRedeemScript(m, publicKeys),
                PublicKeyHash = publicKeyHash,
                m = m,
                publicKeys = publicKeys
            };
        }

        /// <summary>
        /// 用指定的N个公钥创建一段MultiSigContract合约的脚本，并指定至少需要M个账户的签名
        /// </summary>
        /// <param name="m">一个整数，该合约至少需要包含此数量的签名才能生效</param>
        /// <param name="publicKeys">公钥列表，该合约需要此列表中至少m个账户签名后才能生效</param>
        /// <returns>返回一段多方签名合约的脚本代码</returns>
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

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="reader">反序列化的数据来源</param>
        public override void Deserialize(BinaryReader reader)
        {
            m = (int)reader.ReadVarInt(int.MaxValue);
            publicKeys = new ECPoint[reader.ReadVarInt(0x10000000)];
            for (int i = 0; i < publicKeys.Length; i++)
            {
                publicKeys[i] = ECPoint.DeserializeFrom(reader, ECCurve.Secp256r1);
            }
            RedeemScript = CreateMultiSigRedeemScript(m, publicKeys);
            PublicKeyHash = reader.ReadSerializable<UInt160>();
        }

        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="writer">存放序列化后的结果</param>
        public override void Serialize(BinaryWriter writer)
        {
            writer.WriteVarInt(m);
            writer.Write(publicKeys);
            writer.Write(PublicKeyHash);
        }
    }
}
