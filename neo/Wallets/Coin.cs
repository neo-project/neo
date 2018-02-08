using Neo.Core;
using System;

namespace Neo.Wallets
{
    public class Coin : IEquatable<Coin>
    {
        public CoinReference Reference;
        public TransactionOutput Output;
        public CoinState State;

        private string _address = null;
        public string Address
        {
            get
            {
                if (_address == null)
                {
                    _address = Wallet.ToAddress(Output.ScriptHash);
                }
                return _address;
            }
        }

        public bool Equals(Coin other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (ReferenceEquals(null, other)) return false;
            return Reference.Equals(other.Reference);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Coin);
        }

        public override int GetHashCode()
        {
            return Reference.GetHashCode();
        }
    }
}
