using AntShares.Cryptography;
using System;
using System.Linq;
using System.Security.Cryptography;

namespace AntShares.VM
{
    internal class ECDsaCrypto : ICrypto
    {
        public static readonly ECDsaCrypto Default = new ECDsaCrypto();

        public byte[] Hash160(byte[] message)
        {
            return message.Sha256().RIPEMD160();
        }

        public byte[] Hash256(byte[] message)
        {
            return message.Sha256().Sha256();
        }

        public bool VerifySignature(byte[] message, byte[] signature, byte[] pubkey)
        {
            try
            {
                pubkey = Cryptography.ECC.ECPoint.DecodePoint(pubkey, Cryptography.ECC.ECCurve.Secp256r1).EncodePoint(false).Skip(1).ToArray();
            }
            catch
            {
                return false;
            }
#if NET461
            const int ECDSA_PUBLIC_P256_MAGIC = 0x31534345;
            pubkey = BitConverter.GetBytes(ECDSA_PUBLIC_P256_MAGIC).Concat(BitConverter.GetBytes(32)).Concat(pubkey).ToArray();
            using (CngKey key = CngKey.Import(pubkey, CngKeyBlobFormat.EccPublicBlob))
            using (ECDsaCng ecdsa = new ECDsaCng(key))
#else
            using (var ecdsa = ECDsa.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint
                {
                    X = pubkey.Take(32).ToArray(),
                    Y = pubkey.Skip(32).ToArray()
                }
            }))
#endif
            {
                return ecdsa.VerifyData(message, signature, HashAlgorithmName.SHA256);
            }
        }
    }
}
