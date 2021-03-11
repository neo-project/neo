using System;
using System.Security.Cryptography;

namespace Neo.Cryptography
{
    /// <summary>
    /// A cryptographic helper class.
    /// </summary>
    public static class Crypto
    {
        /// <summary>
        /// Gets a 160-bit hash value of a specified message.
        /// </summary>
        /// <param name="message">The message to be hashed.</param>
        /// <returns>160-bit hash value.</returns>
        public static byte[] Hash160(ReadOnlySpan<byte> message)
        {
            return message.Sha256().RIPEMD160();
        }

        /// <summary>
        /// Gets a 256-bit hash value of a specified message.
        /// </summary>
        /// <param name="message">The message to be hashed.</param>
        /// <returns>256-bit hash value.</returns>
        public static byte[] Hash256(ReadOnlySpan<byte> message)
        {
            return message.Sha256().Sha256();
        }

        /// <summary>
        /// Signs the specified message using the ECDSA algorithm.
        /// </summary>
        /// <param name="message">The message to be signed.</param>
        /// <param name="prikey">The private key to be used.</param>
        /// <param name="pubkey">The public key to be used.</param>
        /// <returns>The ECDSA signature for the specified message.</returns>
        public static byte[] Sign(byte[] message, byte[] prikey, byte[] pubkey)
        {
            using var ecdsa = ECDsa.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                D = prikey,
                Q = new ECPoint
                {
                    X = pubkey[..32],
                    Y = pubkey[32..]
                }
            });
            return ecdsa.SignData(message, HashAlgorithmName.SHA256);
        }

        /// <summary>
        /// Verifies that a digital signature is appropriate for the provided key and message.
        /// </summary>
        /// <param name="message">The signed message.</param>
        /// <param name="signature">The signature to be verified.</param>
        /// <param name="pubkey">The public key to be used.</param>
        /// <returns><see langword="true"/> if the signature is valid; otherwise, <see langword="false"/>.</returns>
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

        /// <summary>
        /// Verifies that a digital signature is appropriate for the provided key and message.
        /// </summary>
        /// <param name="message">The signed message.</param>
        /// <param name="signature">The signature to be verified.</param>
        /// <param name="pubkey">The public key to be used.</param>
        /// <param name="curve">The curve to be used by the ECDSA algorithm.</param>
        /// <returns><see langword="true"/> if the signature is valid; otherwise, <see langword="false"/>.</returns>
        public static bool VerifySignature(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature, ReadOnlySpan<byte> pubkey, ECC.ECCurve curve)
        {
            return VerifySignature(message, signature, ECC.ECPoint.DecodePoint(pubkey, curve));
        }
    }
}
