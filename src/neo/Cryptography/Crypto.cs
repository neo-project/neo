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
                return ecdsa.SignData(SafeMessage(message), HashAlgorithmName.SHA256);
            }
        }

        /// <summary>
        /// Append the network magic before sign it
        /// </summary>
        /// <param name="message">Message</param>
        /// <returns>Magic+Message</returns>
        private static byte[] SafeMessage(ReadOnlySpan<byte> message)
        {
            var magic = BitConverter.GetBytes(ProtocolSettings.Default.Magic);

            var networkMsg = new byte[message.Length + 4];
            magic.CopyTo(networkMsg, 0);

            message.ToArray().CopyTo(networkMsg, 4);
            return networkMsg;
        }

        public static bool VerifySignature(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature, ReadOnlySpan<byte> pubkey)
        {
            if (pubkey.Length == 33 && (pubkey[0] == 0x02 || pubkey[0] == 0x03))
            {
                try
                {
                    pubkey = ECC.ECPoint.DecodePoint(pubkey, ECC.ECCurve.Secp256r1).EncodePoint(false).AsSpan(1);
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
            using (var ecdsa = ECDsa.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint
                {
                    X = pubkey[..32].ToArray(),
                    Y = pubkey[32..].ToArray()
                }
            }))
            {
                return ecdsa.VerifyData(SafeMessage(message), signature, HashAlgorithmName.SHA256);
            }
        }
    }
}
