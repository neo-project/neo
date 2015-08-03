using System.IO;
using System.Linq;

namespace AntShares.Core
{
    public class GenerationTransaction : Transaction
    {
        public uint Nonce;

        public GenerationTransaction()
            : base(TransactionType.GenerationTransaction)
        {
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            this.Nonce = reader.ReadUInt32();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(Nonce);
        }

        public override VerificationResult Verify()
        {
            VerificationResult result = base.Verify();
            if (Inputs.Length != 0)
                result |= VerificationResult.IncorrectFormat;
            if (Outputs.Any(p => p.AssetId != Blockchain.AntCoin.Hash || p.Value <= Fixed8.Zero))
                result |= VerificationResult.IncorrectFormat;
            return result;
        }

        internal override VerificationResult VerifyBalance()
        {
            return VerificationResult.OK;
        }
    }
}
