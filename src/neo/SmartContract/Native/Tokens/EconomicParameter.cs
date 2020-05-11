
using Neo.VM;
using Neo.VM.Types;
using System.Numerics;

namespace Neo.SmartContract.Native.Tokens
{
    public class EconomicParameter : IInteroperable
    {
        public BigInteger GasPerBlock;
        public uint NeoHoldersRewardRatio;
        public uint CommitteesRewardRatio;
        public uint VotersRewardRatio;

        public uint TotalRewardRatio => NeoHoldersRewardRatio + CommitteesRewardRatio + VotersRewardRatio;

        public virtual void FromStackItem(StackItem stackItem)
        {
            Struct @struct = (Struct)stackItem;
            GasPerBlock = @struct[0].GetBigInteger();
            NeoHoldersRewardRatio = (uint)@struct[1].GetBigInteger();
            CommitteesRewardRatio = (uint)@struct[2].GetBigInteger();
            VotersRewardRatio = (uint)@struct[3].GetBigInteger();
        }

        public virtual StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Struct(referenceCounter) { GasPerBlock, NeoHoldersRewardRatio, CommitteesRewardRatio, VotersRewardRatio };
        }
    }
}
