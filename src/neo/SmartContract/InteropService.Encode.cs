using Neo.Cryptography;
using Neo.IO;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Linq;

namespace Neo.SmartContract
{
    partial class InteropService
    {
        public static class Encode
        {
            /// <summary>
            /// Convert public key to corresponding wallet account address
            /// </summary>
            public static readonly InteropDescriptor PubKeyToAddress = Register("Neo.Encode.PubKeyToAddress", Encode_PubKeyToAddress, 0_00010000, TriggerType.All, CallFlags.None);

            /// <summary>
            /// Convert public key to corresponding wallet account scripthash
            /// </summary>
            public static readonly InteropDescriptor PubKeyToScriptHash = Register("Neo.Encode.PubKeyToScriptHash", Encode_PubKeyToScriptHash, 0_00010000, TriggerType.All, CallFlags.None);

            /// <summary>
            /// Convert wallet account address to corresponding scripthash
            /// </summary>
            public static readonly InteropDescriptor AddressToScriptHash = Register("Neo.Encode.AddressToScriptHash", Encode_AddressToScriptHash, 0_00010000, TriggerType.All, CallFlags.None);

            /// <summary>
            /// Convert wallet account scripthash to corresponding address
            /// </summary>
            public static readonly InteropDescriptor ScriptHashToAddress = Register("Neo.Encode.ScriptHashToAddress", Encode_ScriptHashToAddress, 0_00010000, TriggerType.All, CallFlags.None);

            private static bool Encode_PubKeyToAddress(ApplicationEngine engine)
            {
                ReadOnlySpan<byte> pubKey = engine.CurrentContext.EvaluationStack.Pop().GetSpan();
                string address = SmartContract.Contract.CreateSignatureRedeemScript(Cryptography.ECC.ECPoint.FromBytes(pubKey.ToArray(), Cryptography.ECC.ECCurve.Secp256r1)).ToScriptHash().ToAddress();
                engine.CurrentContext.EvaluationStack.Push(address);
                return true;
            }

            private static bool Encode_PubKeyToScriptHash(ApplicationEngine engine)
            {
                ReadOnlySpan<byte> pubKey = engine.CurrentContext.EvaluationStack.Pop().GetSpan();
                byte[] scriptHash = SmartContract.Contract.CreateSignatureRedeemScript(Cryptography.ECC.ECPoint.FromBytes(pubKey.ToArray(), Cryptography.ECC.ECCurve.Secp256r1)).ToScriptHash().ToArray();
                engine.CurrentContext.EvaluationStack.Push(scriptHash);
                return true;
            }

            private static bool Encode_AddressToScriptHash(ApplicationEngine engine)
            {
                string address = engine.CurrentContext.EvaluationStack.Pop().GetString();
                byte[] scriptHash = address.ToScriptHash().ToArray();
                engine.CurrentContext.EvaluationStack.Push(scriptHash);
                return true;
            }

            private static bool Encode_ScriptHashToAddress(ApplicationEngine engine)
            {
                ReadOnlySpan<byte> hash = engine.CurrentContext.EvaluationStack.Pop().GetSpan();
                string address = new UInt160(hash).ToAddress();
                engine.CurrentContext.EvaluationStack.Push(address);
                return true;
            }
        }
    }
}
