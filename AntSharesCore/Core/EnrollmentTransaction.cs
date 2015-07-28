using System.IO;
using System.Linq;

namespace AntShares.Core
{
    public class EnrollmentTransaction : Transaction
    {
        public override Fixed8 SystemFee => Fixed8.FromDecimal(1000);

        public EnrollmentTransaction()
            : base(TransactionType.EnrollTransaction)
        {
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
        }

        public override UInt160[] GetScriptHashesForVerifying()
        {
            return base.GetScriptHashesForVerifying().Union(Outputs.Select(p => p.ScriptHash)).OrderBy(p => p).ToArray();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
        }

        internal override bool VerifyBalance()
        {
            if (!base.VerifyBalance()) return false;
            if (Outputs.Length != 1 || Outputs[0].AssetId != Blockchain.AntCoin.Hash)
                return false;
            return true;
        }
    }
}
