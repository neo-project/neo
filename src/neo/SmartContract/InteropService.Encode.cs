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
            public static readonly InteropDescriptor PubKey2Address = Register("Neo.Crypto.PubKey2Address", Crypto_PubKey2Address, 0_01000000, TriggerType.All, CallFlags.None);
            public static readonly InteropDescriptor PubKey2ScriptHash = Register("Neo.Crypto.PubKey2ScriptHash", Crypto_PubKey2ScriptHash, 0_01000000, TriggerType.All, CallFlags.None);
            public static readonly InteropDescriptor Address2ScriptHash = Register("Neo.Crypto.Address2ScriptHash", Crypto_Address2ScriptHash, 0_01000000, TriggerType.All, CallFlags.None);
            public static readonly InteropDescriptor ScriptHash2Address = Register("Neo.Crypto.ScriptHash2Address", Crypto_ScriptHash2Address, 0_01000000, TriggerType.All, CallFlags.None);

            private static bool Crypto_PubKey2Address(ApplicationEngine engine)
            {
                ReadOnlySpan<byte> pubKey = engine.CurrentContext.EvaluationStack.Pop().GetSpan();
                byte[] scriptHash = Cryptography.ECC.ECPoint.FromBytes(pubKey.ToArray(), Cryptography.ECC.ECCurve.Secp256r1).EncodePoint(true).ToScriptHash().ToAddress().HexToBytes(); ;
                engine.CurrentContext.EvaluationStack.Push(scriptHash);
                return true;
            }

            private static bool Crypto_PubKey2ScriptHash(ApplicationEngine engine)
            {
                ReadOnlySpan<byte> pubKey = engine.CurrentContext.EvaluationStack.Pop().GetSpan();
                byte[] scriptHash = Cryptography.ECC.ECPoint.FromBytes(pubKey.ToArray(), Cryptography.ECC.ECCurve.Secp256r1).EncodePoint(true).ToScriptHash().ToArray();
                engine.CurrentContext.EvaluationStack.Push(scriptHash);
                return true;
            }

            private static bool Crypto_Address2ScriptHash(ApplicationEngine engine)
            {
                ReadOnlySpan<byte> address = engine.CurrentContext.EvaluationStack.Pop().GetSpan();
                byte[] scriptHash = address.ToHexString().ToScriptHash().ToArray();
                engine.CurrentContext.EvaluationStack.Push(scriptHash);
                return true;
            }

            private static bool Crypto_ScriptHash2Address(ApplicationEngine engine)
            {
                ReadOnlySpan<byte> hash = engine.CurrentContext.EvaluationStack.Pop().GetSpan();
                byte[] address = new UInt160(hash).ToAddress().HexToBytes();
                engine.CurrentContext.EvaluationStack.Push(address);
                return true;
            }
        }
    }
}
