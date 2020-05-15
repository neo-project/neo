using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Linq;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        public const long ECDsaVerifyPrice = 0_01000000;

        [InteropService("Neo.Crypto.SHA256", 0_01000000, TriggerType.All, CallFlags.None)]
        private bool Crypto_SHA256()
        {
            StackItem item0 = Pop();
            ReadOnlySpan<byte> value = item0 switch
            {
                InteropInterface _interface => _interface.GetInterface<IVerifiable>().GetHashData(),
                Null _ => ScriptContainer.GetHashData(),
                _ => item0.GetSpan()
            };
            Push(value.Sha256());
            return true;
        }

        [InteropService("Neo.Crypto.VerifyWithECDsaSecp256r1", ECDsaVerifyPrice, TriggerType.All, CallFlags.None)]
        private bool Crypto_VerifyWithECDsaSecp256r1()
        {
            return Crypto_ECDsaVerify(ECCurve.Secp256r1);
        }

        [InteropService("Neo.Crypto.VerifyWithECDsaSecp256k1", ECDsaVerifyPrice, TriggerType.All, CallFlags.None)]
        private bool Crypto_VerifyWithECDsaSecp256k1()
        {
            return Crypto_ECDsaVerify(ECCurve.Secp256k1);
        }

        private bool Crypto_ECDsaVerify(ECCurve curve)
        {
            StackItem item0 = Pop();
            ReadOnlySpan<byte> message = item0 switch
            {
                InteropInterface _interface => _interface.GetInterface<IVerifiable>().GetHashData(),
                Null _ => ScriptContainer.GetHashData(),
                _ => item0.GetSpan()
            };
            if (!TryPop(out ReadOnlySpan<byte> pubkey)) return false;
            if (!TryPop(out ReadOnlySpan<byte> signature)) return false;
            try
            {
                Push(Crypto.VerifySignature(message, signature, pubkey, curve));
            }
            catch (ArgumentException)
            {
                Push(false);
            }
            return true;
        }

        [InteropService("Neo.Crypto.CheckMultisigWithECDsaSecp256r1", 0, TriggerType.All, CallFlags.None)]
        private bool Crypto_CheckMultisigWithECDsaSecp256r1()
        {
            return Crypto_ECDsaCheckMultiSig(ECCurve.Secp256r1);
        }

        [InteropService("Neo.Crypto.CheckMultisigWithECDsaSecp256k1", 0, TriggerType.All, CallFlags.None)]
        private bool Crypto_CheckMultisigWithECDsaSecp256k1()
        {
            return Crypto_ECDsaCheckMultiSig(ECCurve.Secp256k1);
        }

        private bool Crypto_ECDsaCheckMultiSig(ECCurve curve)
        {
            StackItem item0 = Pop();
            ReadOnlySpan<byte> message = item0 switch
            {
                InteropInterface _interface => _interface.GetInterface<IVerifiable>().GetHashData(),
                Null _ => ScriptContainer.GetHashData(),
                _ => item0.GetSpan()
            };
            int n;
            byte[][] pubkeys;
            StackItem item = Pop();
            if (item is Array array1)
            {
                pubkeys = array1.Select(p => p.GetSpan().ToArray()).ToArray();
                n = pubkeys.Length;
                if (n == 0) return false;
            }
            else
            {
                n = (int)item.GetBigInteger();
                if (n < 1 || n > CurrentContext.EvaluationStack.Count) return false;
                pubkeys = new byte[n][];
                for (int i = 0; i < n; i++)
                    pubkeys[i] = Pop().GetSpan().ToArray();
            }
            if (!AddGas(ECDsaVerifyPrice * n)) return false;
            int m;
            byte[][] signatures;
            item = Pop();
            if (item is Array array2)
            {
                signatures = array2.Select(p => p.GetSpan().ToArray()).ToArray();
                m = signatures.Length;
                if (m == 0 || m > n) return false;
            }
            else
            {
                m = (int)item.GetBigInteger();
                if (m < 1 || m > n || m > CurrentContext.EvaluationStack.Count) return false;
                signatures = new byte[m][];
                for (int i = 0; i < m; i++)
                    signatures[i] = Pop().GetSpan().ToArray();
            }
            bool fSuccess = true;
            try
            {
                for (int i = 0, j = 0; fSuccess && i < m && j < n;)
                {
                    if (Crypto.VerifySignature(message, signatures[i], pubkeys[j], curve))
                        i++;
                    j++;
                    if (m - i > n - j)
                        fSuccess = false;
                }
            }
            catch (ArgumentException)
            {
                fSuccess = false;
            }
            Push(fSuccess);
            return true;
        }
    }
}
