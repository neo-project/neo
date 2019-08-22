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
        public int balance;
        public int vote;
        public CalculatedSingleVote(int Balance, int Vote)
        {
            balance = Balance;
            vote = Vote;
        }
    }
}
