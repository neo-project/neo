using AntShares.Cryptography.ECC;
using AntShares.IO;
using AntShares.Wallets;
using System;
using System.IO;
using System.Linq;

namespace AntShares.Core
{
    public class EnrollmentTransaction : Transaction
    {
        public ECPoint PublicKey;

        [NonSerialized]
        private UInt160 _miner = null;
        public UInt160 Miner
        {
            get
            {
                if (_miner == null)
                {
                    _miner = SignatureContract.Create(PublicKey).ScriptHash;
                }
                return _miner;
            }
        }

        public override Fixed8 SystemFee => Fixed8.FromDecimal(1000);

        public EnrollmentTransaction()
            : base(TransactionType.EnrollmentTransaction)
        {
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            PublicKey = ECPoint.DeserializeFrom(reader, ECCurve.Secp256r1);
        }

        public override UInt160[] GetScriptHashesForVerifying()
        {
            return base.GetScriptHashesForVerifying().Union(new UInt160[] { Miner }).OrderBy(p => p).ToArray();
        }

        protected override void OnDeserialized()
        {
            base.OnDeserialized();
            if (Outputs.Length == 0 || Outputs[0].AssetId != Blockchain.AntCoin.Hash || Outputs[0].ScriptHash != Miner)
                throw new FormatException();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(PublicKey);
        }
    }
}
