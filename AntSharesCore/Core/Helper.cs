using AntShares.Core.Scripts;
using AntShares.Cryptography;
using AntShares.Cryptography.ECC;
using AntShares.Wallets;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
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
            {
                return ecdsa.SignHash(signable.GetHashForSigning());
            }
#else
            var ecdsa = new AntShares.Cryptography.ECC.ECDsa(prikey, ECCurve.Secp256r1);
            BigInteger[] bi = ecdsa.GenerateSignature(signable.GetHashForSigning());
            byte[] r = bi[0].ToByteArray().Take(32).Reverse().ToArray();
            byte[] s = bi[1].ToByteArray().Take(32).Reverse().ToArray();
            return Enumerable.Repeat((byte)0, 32 - r.Length).Concat(r).Concat(Enumerable.Repeat((byte)0, 32 - s.Length)).Concat(s).ToArray();
#endif
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
        public static bool VerifySignature(this ISignable signable, ECPoint pubkey, byte[] signature)
        {
#if NET461
            const int ECDSA_PUBLIC_P256_MAGIC = 0x31534345;
            byte[] bytes = BitConverter.GetBytes(ECDSA_PUBLIC_P256_MAGIC).Concat(BitConverter.GetBytes(32)).Concat(pubkey.EncodePoint(false).Skip(1)).ToArray();
            using (CngKey key = CngKey.Import(bytes, CngKeyBlobFormat.EccPublicBlob))
            using (ECDsaCng ecdsa = new ECDsaCng(key))
            {
                return ecdsa.VerifyHash(signable.GetHashForSigning(), signature);
            }
#else
            BigInteger r = new BigInteger(signature.Take(32).Reverse().Concat(new byte[1]).ToArray());
            BigInteger s = new BigInteger(signature.Skip(32).Reverse().Concat(new byte[1]).ToArray());
            var ecdsa = new AntShares.Cryptography.ECC.ECDsa(pubkey);
            return ecdsa.VerifySignature(signable.GetHashForSigning(), r, s);
#endif
        }
    }
}
