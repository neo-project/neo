
using Neo.VM;
using Neo.VM.Types;

namespace Neo.SmartContract.Native.Tokens
{
    public class EconomicParameter : IInteroperable
    {
        public uint GasPerBlock;
        public uint NeoHoldersRewardRatio;
        public uint CommitteesRewardRatio;
        public uint VotersRewardRatio;
        public uint TotalRewardRatio => NeoHoldersRewardRatio + CommitteesRewardRatio + VotersRewardRatio;

        public virtual void FromStackItem(StackItem stackItem)
        {
            GasPerBlock = (uint) ((Struct)stackItem)[0].GetBigInteger();
            NeoHoldersRewardRatio = (uint) ((Struct)stackItem)[1].GetBigInteger();
            CommitteesRewardRatio = (uint) ((Struct)stackItem)[2].GetBigInteger();
            VotersRewardRatio = (uint) ((Struct)stackItem)[3].GetBigInteger();
        }

        public virtual StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Struct(referenceCounter) { GasPerBlock, NeoHoldersRewardRatio, CommitteesRewardRatio, VotersRewardRatio };
        }
    }
}
