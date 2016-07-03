using AntShares.Core.Scripts;
using AntShares.Cryptography;
using AntShares.Wallets;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace AntShares.Core
{
    /// <summary>
    /// 包含一系列签名与验证的扩展方法
    /// </summary>
    public static class Helper
    {
        /// <summary>
        /// 获取需要签名的散列值
        /// </summary>
        /// <param name="signable">要签名的数据</param>
        /// <returns>返回需要签名的散列值</returns>
        public static byte[] GetHashForSigning(this ISignable signable)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                signable.SerializeUnsigned(writer);
                writer.Flush();
                return ms.ToArray().Sha256();
            }
        }

        /// <summary>
        /// 根据传入的公私钥，对可签名的对象进行签名
        /// </summary>
        /// <param name="signable">要签名的数据</param>
        /// <param name="prikey">私钥</param>
        /// <param name="pubkey">公钥</param>
        /// <returns>返回签名后的结果</returns>
        internal static byte[] Sign(this ISignable signable, byte[] prikey, byte[] pubkey)
        {
#if NET461
            const int ECDSA_PRIVATE_P256_MAGIC = 0x32534345;
            prikey = BitConverter.GetBytes(ECDSA_PRIVATE_P256_MAGIC).Concat(BitConverter.GetBytes(32)).Concat(pubkey).Concat(prikey).ToArray();
            using (CngKey key = CngKey.Import(prikey, CngKeyBlobFormat.EccPrivateBlob))
            using (ECDsaCng ecdsa = new ECDsaCng(key))
#else
            using (var ecdsa = ECDsa.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                D = prikey,
                Q = new ECPoint
                {
                    X = pubkey.Take(32).ToArray(),
                    Y = pubkey.Skip(32).ToArray()
                }
            }))
#endif
            {
                return ecdsa.SignHash(signable.GetHashForSigning());
            }
        }

        /// <summary>
        /// 根据传入的账户信息，对可签名的对象进行签名
        /// </summary>
        /// <param name="signable">要签名的数据</param>
        /// <param name="account">用于签名的账户</param>
        /// <returns>返回签名后的结果</returns>
        public static byte[] Sign(this ISignable signable, Account account)
        {
            using (account.Decrypt())
            {
                return signable.Sign(account.PrivateKey, account.PublicKey.EncodePoint(false).Skip(1).ToArray());
            }
        }

        internal static bool VerifySignature(this ISignable signable)
        {
            UInt160[] hashes;
            try
            {
                hashes = signable.GetScriptHashesForVerifying();
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            if (hashes.Length != signable.Scripts.Length) return false;
            for (int i = 0; i < hashes.Length; i++)
            {
                if (hashes[i] != signable.Scripts[i].RedeemScript.ToScriptHash()) return false;
                ScriptEngine engine = new ScriptEngine(signable.Scripts[i], signable);
                if (!engine.Execute()) return false;
            }
            return true;
        }

        /// <summary>
        /// 根据传入的公钥与签名，对可签名对象的签名进行验证
        /// </summary>
        /// <param name="signable">要验证的数据</param>
        /// <param name="pubkey">公钥</param>
        /// <param name="signature">签名</param>
        /// <returns>返回验证结果</returns>
        public static bool VerifySignature(this ISignable signable, Cryptography.ECC.ECPoint pubkey, byte[] signature)
        {
            byte[] pubk = pubkey.EncodePoint(false).Skip(1).ToArray();
#if NET461
            const int ECDSA_PUBLIC_P256_MAGIC = 0x31534345;
            pubk = BitConverter.GetBytes(ECDSA_PUBLIC_P256_MAGIC).Concat(BitConverter.GetBytes(32)).Concat(pubk).ToArray();
            using (CngKey key = CngKey.Import(pubk, CngKeyBlobFormat.EccPublicBlob))
            using (ECDsaCng ecdsa = new ECDsaCng(key))
#else
            using (var ecdsa = ECDsa.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint
                {
                    X = pubk.Take(32).ToArray(),
                    Y = pubk.Skip(32).ToArray()
                }
            }))
#endif
            {
                return ecdsa.VerifyHash(signable.GetHashForSigning(), signature);
            }
        }
    }
}
