using AntShares.IO;
using System;
using System.IO;
using System.Linq;

namespace AntShares.Core
{
    public class VotingTransaction : Transaction
    {
        public UInt256[] Votes;

        public override Fixed8 SystemFee => Fixed8.FromDecimal(10);

        public VotingTransaction()
            : base(TransactionType.VotingTransaction)
        {
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            this.Votes = reader.ReadSerializableArray<UInt256>();
            if (Votes.Length == 0 || Votes.Length > 256)
                throw new FormatException();
            if (Votes.Length != Votes.Distinct().Count())
                throw new FormatException();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(Votes);
        }

        public override bool Verify()
        {
            if (!base.Verify()) return false;
            if (!Blockchain.Default.Ability.HasFlag(BlockchainAbility.UnspentIndexes))
                return false;
            foreach (UInt256 vote in Votes)
            {
                EnrollmentTransaction tx = Blockchain.Default.GetTransaction(vote) as EnrollmentTransaction;
                if (tx == null) return false;
                if (Blockchain.Default.GetUnspent(vote, 0) == null)
                    return false;
            }
            return true;
        }
    }
}
