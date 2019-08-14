using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract.Native;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_SingleModel
    {
        [TestMethod]
        public void TestCalculateVote()
        {
            List<SingleVoteUnit> votes = new List<SingleVoteUnit>();
            votes.Add(new SingleVoteUnit(new BigInteger(2),1));
            votes.Add(new SingleVoteUnit(new BigInteger(2), 2));
            votes.Add(new SingleVoteUnit(new BigInteger(1), 2));
            votes.Add(new SingleVoteUnit(new BigInteger(2), 1));
            votes.Add(new SingleVoteUnit(new BigInteger(5), 3));
            SingleModel singleModel = new SingleModel();
            int[] result=singleModel.CalculateVote(votes);
            Assert.AreEqual("3,1,2", String.Join(",",result));
        }
    }
}
