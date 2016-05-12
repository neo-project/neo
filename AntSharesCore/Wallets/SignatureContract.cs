using AntShares.Core.Scripts;
using AntShares.Cryptography.ECC;
using AntShares.IO;
using System.IO;

namespace AntShares.Wallets
{
    /// <summary>
    /// 简单签名合约，该合约只需要一个指定账户的签名即可生效
    /// </summary>
    public class SignatureContract : Contract
    {
        private ECPoint publicKey;

        /// <summary>
        /// 合约的形式参数列表
        /// </summary>
        public override ContractParameterType[] ParameterList => new[] { ContractParameterType.Signature };

        /// <summary>
        /// 用指定的公钥创建一个SignatureContract实例
        /// </summary>
        /// <param name="publicKey">用于创建SignatureContract实例的公钥</param>
        /// <returns>返回一个简单签名合约</returns>
        public static SignatureContract Create(ECPoint publicKey)
        {
            return new SignatureContract
            {
                RedeemScript = CreateSignatureRedeemScript(publicKey),
                PublicKeyHash = publicKey.EncodePoint(true).ToScriptHash(),
                publicKey = publicKey
            };
        }

        /// <summary>
        /// 用指定的公钥创建一段SignatureContract合约的脚本
        /// </summary>
        /// <param name="publicKey">用于创建SignatureContract合约脚本的公钥</param>
        /// <returns>返回一段简单签名合约的脚本代码</returns>
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
        /// <param name="reader">反序列化的数据来源</param>
        public override void Deserialize(BinaryReader reader)
        {
            publicKey = ECPoint.DeserializeFrom(reader, ECCurve.Secp256r1);
            RedeemScript = CreateSignatureRedeemScript(publicKey);
            PublicKeyHash = publicKey.EncodePoint(true).ToScriptHash();
        }

        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="writer">存放序列化后的结果</param>
        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(publicKey);
        }
    }
}
