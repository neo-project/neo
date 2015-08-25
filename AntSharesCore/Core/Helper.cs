using AntShares.Core.Scripts;
using AntShares.Cryptography;
using System;
using System.Linq;
using System.Security.Cryptography;

namespace AntShares.Core
{
    public static class Helper
    {
        private const byte CoinVersion = 0x17;

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

        public static string ToAddress(this UInt160 hash)
        {
            byte[] data = new byte[] { CoinVersion }.Concat(hash.ToArray()).ToArray();
            return Base58.Encode(data.Concat(data.Sha256().Sha256().Take(4)).ToArray());
        }

        public static UInt160 ToScriptHash(this string address)
        {
            byte[] data = Base58.Decode(address);
            if (data.Length != 25)
                throw new FormatException();
            if (data[0] != CoinVersion)
                throw new FormatException();
            if (!data.Take(21).Sha256().Sha256().Take(4).SequenceEqual(data.Skip(21)))
                throw new FormatException();
            return new UInt160(data.Skip(1).Take(20).ToArray());
        }

        public static UInt160 ToScriptHash(this byte[] redeemScript)
        {
            return new UInt160(redeemScript.Sha256().RIPEMD160());
        }

        internal static VerificationResult VerifySignature(this ISignable signable)
        {
            UInt160[] hashes;
            try
            {
                hashes = signable.GetScriptHashesForVerifying();
            }
            catch (InvalidOperationException)
            {
                return VerificationResult.LackOfInformation;
            }
            byte[][] scripts = signable.Scripts;
            if (hashes.Length != scripts.Length)
                return VerificationResult.InvalidSignature;
            for (int i = 0; i < hashes.Length; i++)
            {
                using (ScriptBuilder sb = new ScriptBuilder())
                {
                    byte[] script = sb.Add(scripts[i]).Add(ScriptOp.OP_DUP).Add(ScriptOp.OP_HASH160).Push(hashes[i]).Add(ScriptOp.OP_EQUALVERIFY).Add(ScriptOp.OP_EVAL).ToArray();
                    if (!ScriptEngine.Execute(script, signable.GetHashForSigning()))
                        return VerificationResult.InvalidSignature;
                }
            }
            return VerificationResult.OK;
        }
    }
}
