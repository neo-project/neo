using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.SmartContract.Native.Votes.Interface;
using Neo.SmartContract.Native.Votes.Model;
using System.Collections.Generic;

namespace Neo.UnitTests.SmartContract.Native.Votes.Model
{
    [TestClass]
    public class UT_VoteState
    {
        VoteState voteState;
        SingleCandidate candidate;
        MultiCandidate multiCandidate;

        [TestInitialize]
        public void TestSetup()
        {
            candidate = new SingleCandidate(1);
            List<int> lists = new List<int> { 1, 2, 3 };
            multiCandidate = new MultiCandidate(lists);
            voteState = new VoteState(UInt160.Zero, candidate);
        }

        [TestMethod]
        public void Check_VoteState()
        {
            byte[] temp = voteState.ToArray();

            var newState = temp.AsSerializable<VoteState>();
            newState.GetCandidate().Should().BeOfType<SingleCandidate>();

            voteState = new VoteState(UInt160.Zero, multiCandidate);
            temp = voteState.ToArray();
            newState = new VoteState(UInt160.Zero, multiCandidate);

            newState.GetCandidate().Should().BeOfType<MultiCandidate>();
        }
    }
}
