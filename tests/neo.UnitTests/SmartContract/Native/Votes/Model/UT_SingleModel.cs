using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract.Native;
using System;
using System.Collections.Generic;
using System.Numerics;
using Neo.SmartContract.Native.Votes.Model;
using Neo.SmartContract.Native.Votes.Interface;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_SingleModel
    {
        [TestMethod]
        public void TestCalculateVote()
        {
            List<CalculatedSingleVote> votes = new List<CalculatedSingleVote>();
            votes.Add(new CalculatedSingleVote(2, 1));
            votes.Add(new CalculatedSingleVote(2, 2));
            votes.Add(new CalculatedSingleVote(1, 2));
            votes.Add(new CalculatedSingleVote(2, 1));
            votes.Add(new CalculatedSingleVote(5, 3));
            SingleModel singleModel = new SingleModel();
            int[] result = singleModel.CalculateVote(votes);
            Assert.AreEqual("3,1,2", String.Join(",", result));
        }
    }
}
