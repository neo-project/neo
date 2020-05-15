
using Neo.VM;
using Neo.VM.Types;
using System.Numerics;

namespace Neo.SmartContract.Native.Tokens
{
    public class EconomicParameter : IInteroperable
    {
        public BigInteger GasPerBlock;
        public byte NeoHoldersRewardRatio;
        public byte CommitteesRewardRatio;
        public byte VotersRewardRatio;

        public uint TotalRewardRatio => (uint) (NeoHoldersRewardRatio + CommitteesRewardRatio + VotersRewardRatio);

        public virtual void FromStackItem(StackItem stackItem)
        {
            Struct @struct = (Struct)stackItem;
            GasPerBlock = @struct[0].GetBigInteger();
            NeoHoldersRewardRatio = @struct[1].GetSpan()[0];
            CommitteesRewardRatio = @struct[2].GetSpan()[0];
            VotersRewardRatio = @struct[3].GetSpan()[0];
        }

        public virtual StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Struct(referenceCounter) { GasPerBlock, new byte[] { NeoHoldersRewardRatio }, new byte[] { CommitteesRewardRatio }, new byte[] { VotersRewardRatio } };
        }
    }
}
