using Neo.Cryptography;
using Neo.Cryptography.ECC;
using System;
using System.IO;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        public const long CheckSigPrice = 1 << 15;

        public static readonly InteropDescriptor Neo_Crypto_CheckSig = Register("Neo.Crypto.CheckSig", nameof(CheckSig), CheckSigPrice, CallFlags.None);
        public static readonly InteropDescriptor Neo_Crypto_CheckMultisig = Register("Neo.Crypto.CheckMultisig", nameof(CheckMultisig), 0, CallFlags.None);

        protected internal bool CheckSig(byte[] pubkey, byte[] signature)
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(ms);
            writer.Write(ProtocolSettings.Magic);
            ScriptContainer.SerializeUnsigned(writer);
            writer.Flush();
            try
            {
                return Crypto.VerifySignature(ms.ToArray(), signature, pubkey, ECCurve.Secp256r1);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        protected internal bool CheckMultisig(byte[][] pubkeys, byte[][] signatures)
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(ms);
            writer.Write(ProtocolSettings.Magic);
            ScriptContainer.SerializeUnsigned(writer);
            writer.Flush();
            byte[] message = ms.ToArray();
            int m = signatures.Length, n = pubkeys.Length;
            if (n == 0 || m == 0 || m > n) throw new ArgumentException();
            AddGas(CheckSigPrice * n * exec_fee_factor);
            try
            {
                for (int i = 0, j = 0; i < m && j < n;)
                {
                    if (Crypto.VerifySignature(message, signatures[i], pubkeys[j], ECCurve.Secp256r1))
                        i++;
                    j++;
                    if (m - i > n - j)
                        return false;
                }
            }
            catch (ArgumentException)
            {
                return false;
            }
            return true;
        }
    }
}
