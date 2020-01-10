using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.SmartContract.Native.Votes.Interface
{
    interface ISingleVoteModel
    {
        int[] CalculateVote(List<CalculatedSingleVote> votes);
    }

    public class CalculatedSingleVote
    {
        public int Balance;
        public int Vote;

        public CalculatedSingleVote(int Balance, int Vote)
        {
            this.Balance = Balance;
            this.Vote = Vote;
        }
    }
}
