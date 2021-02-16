using System;
using System.Security.Cryptography;

namespace Neo.Cryptography
{
    public static class Crypto
    {
        public static byte[] Hash160(ReadOnlySpan<byte> message)
        {
            return message.Sha256().RIPEMD160();
        }

        public static byte[] Hash256(ReadOnlySpan<byte> message)
        {
            return message.Sha256().Sha256();
        }

        public static byte[] Sign(byte[] message, byte[] prikey, byte[] pubkey)
        {
            using (var ecdsa = ECDsa.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                D = prikey,
                Q = new ECPoint
                {
                    X = pubkey[..32],
                    Y = pubkey[32..]
                }
            }))
            {
                return ecdsa.SignData(message, HashAlgorithmName.SHA256);
            }
        }

        public static bool VerifySignature(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature, ECC.ECPoint pubkey)
        {
            ECCurve curve =
                pubkey.Curve == ECC.ECCurve.Secp256r1 ? ECCurve.NamedCurves.nistP256 :
                pubkey.Curve == ECC.ECCurve.Secp256k1 ? ECCurve.CreateFromFriendlyName("secP256k1") :
                throw new NotSupportedException();
            byte[] buffer = pubkey.EncodePoint(false);
            using var ecdsa = ECDsa.Create(new ECParameters
            {
                Curve = curve,
                Q = new ECPoint
                {
                    X = buffer[1..33],
                    Y = buffer[33..]
                }
            });
            return ecdsa.VerifyData(message, signature, HashAlgorithmName.SHA256);
        }

        public static bool VerifySignature(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature, ReadOnlySpan<byte> pubkey, ECC.ECCurve curve)
        {
            return VerifySignature(message, signature, ECC.ECPoint.DecodePoint(pubkey, curve));
        }
    }
}
