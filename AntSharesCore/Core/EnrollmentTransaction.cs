using AntShares.Core.Scripts;
using AntShares.Cryptography;
using AntShares.IO;
using System;
using System.IO;
using System.Linq;

namespace AntShares.Core
{
    public class EnrollmentTransaction : Transaction
    {
        public Secp256r1Point PublicKey;

        [NonSerialized]
        private UInt160 _miner = null;
        public UInt160 Miner
        {
            get
            {
                if (_miner == null)
                {
                    _miner = ScriptBuilder.CreateRedeemScript(1, PublicKey).ToScriptHash();
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
            this.PublicKey = Secp256r1Point.DeserializeFrom(reader);
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
