using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Native;
using Neo.VM.Types;
using System;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        public const long ECDsaVerifyPrice = 0_00032768;

        public static readonly InteropDescriptor Neo_Crypto_RIPEMD160 = Register("Neo.Crypto.RIPEMD160", nameof(RIPEMD160), 0_00032768, CallFlags.None, true);
        public static readonly InteropDescriptor Neo_Crypto_SHA256 = Register("Neo.Crypto.SHA256", nameof(Sha256), 0_00032768, CallFlags.None, true);
        public static readonly InteropDescriptor Neo_Crypto_VerifyWithECDsaSecp256r1 = Register("Neo.Crypto.VerifyWithECDsaSecp256r1", nameof(VerifyWithECDsaSecp256r1), ECDsaVerifyPrice, CallFlags.None, true);
        public static readonly InteropDescriptor Neo_Crypto_VerifyWithECDsaSecp256k1 = Register("Neo.Crypto.VerifyWithECDsaSecp256k1", nameof(VerifyWithECDsaSecp256k1), ECDsaVerifyPrice, CallFlags.None, true);
        public static readonly InteropDescriptor Neo_Crypto_CheckMultisigWithECDsaSecp256r1 = Register("Neo.Crypto.CheckMultisigWithECDsaSecp256r1", nameof(CheckMultisigWithECDsaSecp256r1), 0, CallFlags.None, true);
        public static readonly InteropDescriptor Neo_Crypto_CheckMultisigWithECDsaSecp256k1 = Register("Neo.Crypto.CheckMultisigWithECDsaSecp256k1", nameof(CheckMultisigWithECDsaSecp256k1), 0, CallFlags.None, true);

        protected internal byte[] RIPEMD160(StackItem item)
        {
            ReadOnlySpan<byte> value = item switch
            {
                InteropInterface _interface => _interface.GetInterface<IVerifiable>().GetHashData(),
                Null _ => ScriptContainer.GetHashData(),
                _ => item.GetSpan()
            };
            return value.RIPEMD160();
        }

        protected internal byte[] Sha256(StackItem item)
        {
            ReadOnlySpan<byte> value = item switch
            {
                InteropInterface _interface => _interface.GetInterface<IVerifiable>().GetHashData(),
                Null _ => ScriptContainer.GetHashData(),
                _ => item.GetSpan()
            };
            return value.Sha256();
        }

        protected internal bool VerifyWithECDsaSecp256r1(StackItem item, byte[] pubkey, byte[] signature)
        {
            return VerifyWithECDsa(item, pubkey, signature, ECCurve.Secp256r1);
        }

        protected internal bool VerifyWithECDsaSecp256k1(StackItem item, byte[] pubkey, byte[] signature)
        {
            return VerifyWithECDsa(item, pubkey, signature, ECCurve.Secp256k1);
        }

        private bool VerifyWithECDsa(StackItem item, byte[] pubkey, byte[] signature, ECCurve curve)
        {
            ReadOnlySpan<byte> message = item switch
            {
                InteropInterface _interface => _interface.GetInterface<IVerifiable>().GetHashData(),
                Null _ => ScriptContainer.GetHashData(),
                _ => item.GetSpan()
            };
            try
            {
                return Crypto.VerifySignature(message, signature, pubkey, curve);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        protected internal bool CheckMultisigWithECDsaSecp256r1(StackItem item, byte[][] pubkeys, byte[][] signatures)
        {
            return CheckMultiSigWithECDsa(item, pubkeys, signatures, ECCurve.Secp256r1);
        }

        protected internal bool CheckMultisigWithECDsaSecp256k1(StackItem item, byte[][] pubkeys, byte[][] signatures)
        {
            return CheckMultiSigWithECDsa(item, pubkeys, signatures, ECCurve.Secp256k1);
        }

        private bool CheckMultiSigWithECDsa(StackItem item0, byte[][] pubkeys, byte[][] signatures, ECCurve curve)
        {
            int m = signatures.Length, n = pubkeys.Length;
            ReadOnlySpan<byte> message = item0 switch
            {
                InteropInterface _interface => _interface.GetInterface<IVerifiable>().GetHashData(),
                Null _ => ScriptContainer.GetHashData(),
                _ => item0.GetSpan()
            };
            if (n == 0 || m == 0 || m > n) throw new ArgumentException();
            AddGas(ECDsaVerifyPrice * n * NativeContract.Policy.GetBaseExecFee(Snapshot));
            try
            {
                for (int i = 0, j = 0; i < m && j < n;)
                {
                    if (Crypto.VerifySignature(message, signatures[i], pubkeys[j], curve))
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
