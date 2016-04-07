using AntShares.Core;
using AntShares.IO.Caching;
using System;

namespace AntShares.Wallets
{
    public class Coin : IEquatable<Coin>, ITrackable<TransactionInput>
    {
        public TransactionInput Input;
        public UInt256 AssetId;
        public Fixed8 Value;
        public UInt160 ScriptHash;

        [NonSerialized]
        private string _address = null;
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

        TransactionInput ITrackable<TransactionInput>.Key => Input;

        [NonSerialized]
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
                    ITrackable<TransactionInput> _this = this;
                    if (_this.TrackState == TrackState.None)
                        _this.TrackState = TrackState.Changed;
                }
            }
        }

        TrackState ITrackable<TransactionInput>.TrackState { get; set; }

        public bool Equals(Coin other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (ReferenceEquals(null, other)) return false;
            return Input.Equals(other.Input);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Coin);
        }

        public override int GetHashCode()
        {
            return Input.GetHashCode();
        }
    }
}
