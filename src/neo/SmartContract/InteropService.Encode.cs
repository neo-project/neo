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

            private static bool Encode_PubKeyToAddress(ApplicationEngine engine)
            {
                ReadOnlySpan<byte> pubKey = engine.CurrentContext.EvaluationStack.Pop().GetSpan();
                string address = SmartContract.Contract.CreateSignatureRedeemScript(Cryptography.ECC.ECPoint.FromBytes(pubKey.ToArray(), Cryptography.ECC.ECCurve.Secp256r1)).ToScriptHash().ToAddress();
                engine.CurrentContext.EvaluationStack.Push(address);
                return true;
            }
        }
    }
}
