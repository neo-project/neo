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

        public Contract(byte[] redeemScript)
        {
            this.RedeemScript = redeemScript;
            this.ScriptHash = redeemScript.ToScriptHash();
        }

        public static Contract CreateMultiSigContract(int m, params ECPoint[] publicKeys)
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
                return new Contract(sb.ToArray());
            }
        }

        public static Contract CreateSignatureContract(ECPoint publicKey)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.Push(publicKey.EncodePoint(true));
                sb.Add(ScriptOp.OP_CHECKSIG);
                return new Contract(sb.ToArray());
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
