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

        protected override void OnDeserialized()
        {
            base.OnDeserialized();
            if (Outputs.All(p => p.AssetId != Blockchain.AntShare.Hash))
                throw new FormatException();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(Enrollments);
        }

        public override VerificationResult Verify()
        {
            VerificationResult result = base.Verify();
            if (Blockchain.Default.Ability.HasFlag(BlockchainAbility.UnspentIndexes))
            {
                HashSet<Secp256r1Point> pubkeys = new HashSet<Secp256r1Point>();
                foreach (UInt256 vote in Enrollments)
                {
                    EnrollmentTransaction tx = Blockchain.Default.GetTransaction(vote) as EnrollmentTransaction;
                    if (tx == null)
                    {
                        result |= VerificationResult.LackOfInformation;
                        continue;
                    }
                    if (!Blockchain.Default.ContainsUnspent(vote, 0))
                    {
                        result |= VerificationResult.IncorrectFormat;
                        break;
                    }
                    if (!pubkeys.Add(tx.PublicKey))
                    {
                        result |= VerificationResult.IncorrectFormat;
                        break;
                    }
                }
            }
            else
            {
                result |= VerificationResult.Incapable;
            }
            return result;
        }
    }
}
