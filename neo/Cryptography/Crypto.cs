using Neo.VM;
using System;
using System.Linq;
using System.Security.Cryptography;

namespace Neo.Cryptography
{
    public class Crypto : ICrypto
    {
        public static readonly Crypto Default = new Crypto();

        public byte[] Hash160(byte[] message)
        {
            return message.Sha256().RIPEMD160();
        }

        public byte[] Hash256(byte[] message)
        {
            return message.Sha256().Sha256();
        }

        public byte[] Sign(byte[] message, byte[] prikey, byte[] pubkey)
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
                return ecdsa.SignData(message, HashAlgorithmName.SHA256);
            }
        }

        public bool VerifySignature(byte[] message, byte[] signature, byte[] pubkey)
        {
            if (pubkey.Length == 33 && (pubkey[0] == 0x02 || pubkey[0] == 0x03))
            {
                try
                {
                    pubkey = Cryptography.ECC.ECPoint.DecodePoint(pubkey, Cryptography.ECC.ECCurve.Secp256r1).EncodePoint(false).Skip(1).ToArray();
                }
                catch
                {
                    return false;
                }
            }
            else if (pubkey.Length == 65 && pubkey[0] == 0x04)
            {
                pubkey = pubkey.Skip(1).ToArray();
            }
            else if (pubkey.Length != 64)
            {
                throw new ArgumentException();
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
