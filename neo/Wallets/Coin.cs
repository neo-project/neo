using Neo.Core;
using Neo.IO.Caching;
using System;

namespace Neo.Wallets
{
    public class Coin : IEquatable<Coin>, ITrackable<CoinReference>
    {
        public CoinReference Reference;
        public TransactionOutput Output;

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

        CoinReference ITrackable<CoinReference>.Key => Reference;

        private CoinState state;
        public CoinState State
        {
            get
            {
                return state;
            }
            set
            {
                if (state != value)
                {
                    state = value;
                    ITrackable<CoinReference> _this = this;
                    if (_this.TrackState == TrackState.None)
                        _this.TrackState = TrackState.Changed;
                }
            }
        }

        TrackState ITrackable<CoinReference>.TrackState { get; set; }

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
