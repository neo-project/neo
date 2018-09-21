using Neo.Cryptography.ECC;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Linq;

namespace Neo.SmartContract
{
    public class Contract
    {
        public byte[] Script;
        public ContractParameterType[] ParameterList;

        private string _address;
        public string Address
        {
            get
            {
                if (_address == null)
                {
                    _address = ScriptHash.ToAddress();
                }
                return _address;
            }
        }

        private UInt160 _scriptHash;
        public virtual UInt160 ScriptHash
        {
            get
            {
                if (_scriptHash == null)
                {
                    _scriptHash = Script.ToScriptHash();
                }
                return _scriptHash;
            }
        }

        public static Contract Create(ContractParameterType[] parameterList, byte[] redeemScript)
        {
            return new Contract
            {
                Script = redeemScript,
                ParameterList = parameterList
            };
        }

        public static Contract CreateMultiSigContract(int m, params ECPoint[] publicKeys)
        {
            return new Contract
            {
                Script = CreateMultiSigRedeemScript(m, publicKeys),
                ParameterList = Enumerable.Repeat(ContractParameterType.Signature, m).ToArray()
            };
        }

        public static byte[] CreateMultiSigRedeemScript(int m, params ECPoint[] publicKeys)
        {
            if (!(1 <= m && m <= publicKeys.Length && publicKeys.Length <= 1024))
                throw new ArgumentException();
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitPush(m);
                foreach (ECPoint publicKey in publicKeys.OrderBy(p => p))
                {
                    sb.EmitPush(publicKey.EncodePoint(true));
                }
                sb.EmitPush(publicKeys.Length);
                sb.Emit(OpCode.CHECKMULTISIG);
                return sb.ToArray();
            }
        }

        public static Contract CreateSignatureContract(ECPoint publicKey)
        {
            return new Contract
            {
                Script = CreateSignatureRedeemScript(publicKey),
                ParameterList = new[] { ContractParameterType.Signature }
            };
        }

        public static byte[] CreateSignatureRedeemScript(ECPoint publicKey)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitPush(publicKey.EncodePoint(true));
                sb.Emit(OpCode.CHECKSIG);
                return sb.ToArray();
            }
        }
    }
}
