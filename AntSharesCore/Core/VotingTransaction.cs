using AntShares.Cryptography;
using AntShares.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AntShares.Core
{
    public class VotingTransaction : Transaction
    {
        public UInt256[] Enrollments;

        public override Fixed8 SystemFee => Fixed8.FromDecimal(10);

        public VotingTransaction()
            : base(TransactionType.VotingTransaction)
        {
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            this.Enrollments = reader.ReadSerializableArray<UInt256>();
            if (Enrollments.Length == 0 || Enrollments.Length > 1024)
                throw new FormatException();
            if (Enrollments.Length != Enrollments.Distinct().Count())
                throw new FormatException();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(Enrollments);
        }

        public override bool Verify()
        {
            if (!base.Verify()) return false;
            if (!Blockchain.Default.Ability.HasFlag(BlockchainAbility.UnspentIndexes))
                return false;
            HashSet<ECCPublicKey> pubkeys = new HashSet<ECCPublicKey>();
            foreach (UInt256 vote in Enrollments)
            {
                EnrollmentTransaction tx = Blockchain.Default.GetTransaction(vote) as EnrollmentTransaction;
                if (tx == null) return false;
                if (!Blockchain.Default.ContainsUnspent(vote, 0))
                    return false;
                if (!pubkeys.Add(tx.PublicKey)) return false;
            }
            return true;
        }

        internal override bool VerifyBalance()
        {
            if (!base.VerifyBalance()) return false;
            if (Outputs.All(p => p.AssetId != Blockchain.AntShare.Hash))
                return false;
            return true;
        }
    }
}
