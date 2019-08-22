using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.SmartContract.Native.Votes.Interface
{
    interface IMultiVoteModel
    {
        //TODO: details for interface
        int[,] CalculateVote(List<CalculatedMultiVote> votes);
    }

    public class CalculatedMultiVote
    {
        public int balance;
        public List<int> vote;
        public CalculatedMultiVote(int Balance, List<int> Vote)
        {
            balance = Balance;
            vote = Vote;
        }
    }
}
