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
    public static class Helper
    {
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

        internal static byte[] Sign(this ISignable signable, byte[] prikey, byte[] pubkey)
        {
            const int ECDSA_PRIVATE_P256_MAGIC = 0x32534345;
            prikey = BitConverter.GetBytes(ECDSA_PRIVATE_P256_MAGIC).Concat(BitConverter.GetBytes(32)).Concat(pubkey).Concat(prikey).ToArray();
            using (CngKey key = CngKey.Import(prikey, CngKeyBlobFormat.EccPrivateBlob))
            using (ECDsaCng ecdsa = new ECDsaCng(key))
            {
                return ecdsa.SignHash(signable.GetHashForSigning());
            }
        }

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
                ScriptEngine engine = new ScriptEngine(signable.Scripts[i], signable.GetHashForSigning());
                if (!engine.Execute()) return false;
            }
            return true;
        }
    }
}
