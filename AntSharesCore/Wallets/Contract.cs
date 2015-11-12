using AntShares.Core.Scripts;
using AntShares.Cryptography.ECC;
using System;
using System.Linq;

namespace AntShares.Wallets
{
    public class Contract : IEquatable<Contract>
    {
        public readonly byte[] RedeemScript;
        public readonly UInt160 ScriptHash;
        public readonly UInt160 PublicKeyHash;

        private string _address;
        public string Address
        {
            get
            {
                if (_address == null)
                {
                    _address = Wallet.ToAddress(ScriptHash);
                }
                return _address;
            }
        }

        public Contract(byte[] redeemScript, UInt160 publicKeyHash)
        {
            this.RedeemScript = redeemScript;
            this.ScriptHash = redeemScript.ToScriptHash();
            this.PublicKeyHash = publicKeyHash;
        }

        public static Contract CreateMultiSigContract(UInt160 publicKeyHash, int m, params ECPoint[] publicKeys)
        {
            return new Contract(CreateMultiSigRedeemScript(m, publicKeys), publicKeyHash);
        }

        public static byte[] CreateMultiSigRedeemScript(int m, params ECPoint[] publicKeys)
        {
            if (!(1 <= m && m <= publicKeys.Length && publicKeys.Length <= 1024))
                throw new ArgumentException();
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.Push(m);
                foreach (ECPoint publicKey in publicKeys.OrderBy(p => p))
                {
                    sb.Push(publicKey.EncodePoint(true));
                }
                sb.Push(publicKeys.Length);
                sb.Add(ScriptOp.OP_CHECKMULTISIG);
                return sb.ToArray();
            }
        }

        public static Contract CreateSignatureContract(ECPoint publicKey)
        {
            byte[] pubKeyData = publicKey.EncodePoint(true);
            UInt160 publicKeyHash = pubKeyData.ToScriptHash();
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.Push(pubKeyData);
                sb.Add(ScriptOp.OP_CHECKSIG);
                return new Contract(sb.ToArray(), publicKeyHash);
            }
        }

        public bool Equals(Contract other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (ReferenceEquals(null, other)) return false;
            return ScriptHash.Equals(other.ScriptHash);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Contract);
        }

        public override int GetHashCode()
        {
            return ScriptHash.GetHashCode();
        }

        public override string ToString()
        {
            return Address;
        }
    }
}
