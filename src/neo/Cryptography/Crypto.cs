using System;
using System.Numerics;
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

        public static bool VerifySignature(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature, ReadOnlySpan<byte> pubkey, ECC.ECCurve.Curve curve = ECC.ECCurve.Curve.Secp256r1)
        {
            if (pubkey.Length == 33 && (pubkey[0] == 0x02 || pubkey[0] == 0x03))
            {
                try
                {
                    switch (curve)
                    {
                        case ECC.ECCurve.Curve.Secp256r1:
                            pubkey = ECC.ECPoint.DecodePoint(pubkey, ECC.ECCurve.Secp256r1).EncodePoint(false).AsSpan(1);
                            break;
                        case ECC.ECCurve.Curve.Secp256k1:
                            pubkey = ECC.ECPoint.DecodePoint(pubkey, ECC.ECCurve.Secp256k1).EncodePoint(false).AsSpan(1);
                            break;
                        default: return false;
                    }
                }
                catch
                {
                    return false;
                }
            }
            else if (pubkey.Length == 65 && pubkey[0] == 0x04)
            {
                pubkey = pubkey[1..];
            }
            else if (pubkey.Length != 64)
            {
                throw new ArgumentException();
            }

            switch (curve)
            {
                case ECC.ECCurve.Curve.Secp256r1:
                    {
                        var ncurve = ECCurve.NamedCurves.nistP256;

                        using (var ecdsa = ECDsa.Create(new ECParameters
                        {
                            Curve = ncurve,
                            Q = new ECPoint
                            {
                                X = pubkey[..32].ToArray(),
                                Y = pubkey[32..].ToArray()
                            }
                        }))
                        {
                            return ecdsa.VerifyData(message, signature, HashAlgorithmName.SHA256);
                        }
                    }
                case ECC.ECCurve.Curve.Secp256k1:
                    {
                        var publicKey = ECC.ECPoint.FromBytes(pubkey.ToArray(), ECC.ECCurve.Secp256k1);
                        var r = new BigInteger(signature[..32].ToArray(), true, true);
                        var s = new BigInteger(signature[32..].ToArray(), true, true);

                        return ECC.ECCurve.Secp256k1.VerifySignature(message.Sha256(), publicKey, r, s);
                    }
                default: return false;
            }
        }
    }
}
