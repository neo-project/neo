using Neo.Cryptography;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Linq;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract
{
    partial class InteropService
    {
        public static class Crypto
        {
            public static readonly InteropDescriptor SHA256 = Register("Neo.Crypto.SHA256", Crypto_SHA256, 0_01000000, TriggerType.All, CallFlags.None);

            public static readonly InteropDescriptor VerifyWithECDsaSecp256r1 = Register("Neo.Crypto.ECDsa.Secp256r1.Verify", Crypto_ECDsaSecp256r1Verify, 0_01000000, TriggerType.All, CallFlags.None);
            public static readonly InteropDescriptor VerifyWithECDsaSecp256k1 = Register("Neo.Crypto.ECDsa.Secp256k1.Verify", Crypto_ECDsaSecp256k1Verify, 0_01000000, TriggerType.All, CallFlags.None);
            public static readonly InteropDescriptor CheckMultisigWithECDsaSecp256r1 = Register("Neo.Crypto.ECDsa.Secp256r1.CheckMultiSig", Crypto_ECDsaSecp256r1CheckMultiSig, 0, TriggerType.All, CallFlags.None);
            public static readonly InteropDescriptor CheckMultisigWithECDsaSecp256k1 = Register("Neo.Crypto.ECDsa.Secp256k1.CheckMultiSig", Crypto_ECDsaSecp256k1CheckMultiSig, 0, TriggerType.All, CallFlags.None);

            private static bool Crypto_SHA256(ApplicationEngine engine)
            {
                StackItem item0 = engine.CurrentContext.EvaluationStack.Pop();
                ReadOnlySpan<byte> value = item0 switch
                {
                    InteropInterface _interface => _interface.GetInterface<IVerifiable>().GetHashData(),
                    Null _ => engine.ScriptContainer.GetHashData(),
                    _ => item0.GetSpan()
                };

                engine.CurrentContext.EvaluationStack.Push(value.ToArray().Sha256());
                return true;
            }

            private static bool Crypto_ECDsaSecp256r1Verify(ApplicationEngine engine)
            {
                return Crypto_ECDsaVerify(engine, Cryptography.ECC.ECCurve.Secp256r1);
            }

            private static bool Crypto_ECDsaSecp256k1Verify(ApplicationEngine engine)
            {
                return Crypto_ECDsaVerify(engine, Cryptography.ECC.ECCurve.Secp256k1);
            }

            private static bool Crypto_ECDsaVerify(ApplicationEngine engine, Cryptography.ECC.ECCurve curve)
            {
                StackItem item0 = engine.CurrentContext.EvaluationStack.Pop();
                ReadOnlySpan<byte> message = item0 switch
                {
                    InteropInterface _interface => _interface.GetInterface<IVerifiable>().GetHashData(),
                    Null _ => engine.ScriptContainer.GetHashData(),
                    _ => item0.GetSpan()
                };
                ReadOnlySpan<byte> pubkey = engine.CurrentContext.EvaluationStack.Pop().GetSpan();
                ReadOnlySpan<byte> signature = engine.CurrentContext.EvaluationStack.Pop().GetSpan();
                try
                {
                    engine.CurrentContext.EvaluationStack.Push(Cryptography.Crypto.VerifySignature(message, signature, pubkey, curve));
                }
                catch (ArgumentException)
                {
                    engine.CurrentContext.EvaluationStack.Push(false);
                }
                return true;
            }

            private static bool Crypto_ECDsaSecp256r1CheckMultiSig(ApplicationEngine engine)
            {
                return Crypto_ECDsaCheckMultiSig(engine, Cryptography.ECC.ECCurve.Secp256r1);
            }

            private static bool Crypto_ECDsaSecp256k1CheckMultiSig(ApplicationEngine engine)
            {
                return Crypto_ECDsaCheckMultiSig(engine, Cryptography.ECC.ECCurve.Secp256k1);
            }

            private static bool Crypto_ECDsaCheckMultiSig(ApplicationEngine engine, Cryptography.ECC.ECCurve curve)
            {
                StackItem item0 = engine.CurrentContext.EvaluationStack.Pop();
                ReadOnlySpan<byte> message = item0 switch
                {
                    InteropInterface _interface => _interface.GetInterface<IVerifiable>().GetHashData(),
                    Null _ => engine.ScriptContainer.GetHashData(),
                    _ => item0.GetSpan()
                };
                int n;
                byte[][] pubkeys;
                StackItem item = engine.CurrentContext.EvaluationStack.Pop();
                if (item is Array array1)
                {
                    pubkeys = array1.Select(p => p.GetSpan().ToArray()).ToArray();
                    n = pubkeys.Length;
                    if (n == 0) return false;
                }
                else
                {
                    n = (int)item.GetBigInteger();
                    if (n < 1 || n > engine.CurrentContext.EvaluationStack.Count) return false;
                    pubkeys = new byte[n][];
                    for (int i = 0; i < n; i++)
                        pubkeys[i] = engine.CurrentContext.EvaluationStack.Pop().GetSpan().ToArray();
                }
                if (!engine.AddGas(VerifyWithECDsaSecp256r1.FixedPrice * n)) return false;
                int m;
                byte[][] signatures;
                item = engine.CurrentContext.EvaluationStack.Pop();
                if (item is Array array2)
                {
                    signatures = array2.Select(p => p.GetSpan().ToArray()).ToArray();
                    m = signatures.Length;
                    if (m == 0 || m > n) return false;
                }
                else
                {
                    m = (int)item.GetBigInteger();
                    if (m < 1 || m > n || m > engine.CurrentContext.EvaluationStack.Count) return false;
                    signatures = new byte[m][];
                    for (int i = 0; i < m; i++)
                        signatures[i] = engine.CurrentContext.EvaluationStack.Pop().GetSpan().ToArray();
                }
                bool fSuccess = true;
                try
                {
                    for (int i = 0, j = 0; fSuccess && i < m && j < n;)
                    {
                        if (Cryptography.Crypto.VerifySignature(message, signatures[i], pubkeys[j], curve))
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
                engine.CurrentContext.EvaluationStack.Push(fSuccess);
                return true;
            }
        }
    }
}
