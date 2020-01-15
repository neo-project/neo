using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.SmartContract.Native.Votes.Model;
using System.Collections.Generic;

namespace Neo.UnitTests.SmartContract.Native.Votes.Model
{
    [TestClass]
    public class UT_Candidate
    {
        MultiCandidate multiCandidate;
        SingleCandidate singleCandidate;

        [TestInitialize]
        public void TestSetUp()
        {
            List<int> list = new List<int> { 1, 2, 3 };

            multiCandidate = new MultiCandidate(list);
            singleCandidate = new SingleCandidate(1);
        }

        [TestMethod]
        public void Check_SingleCandidate()
        {
            byte[] temp = singleCandidate.ToArray();
            var newCandidate = temp.AsSerializable<SingleCandidate>();

            Assert.AreEqual(newCandidate.GetCandidate(), singleCandidate.GetCandidate());
        }

        [TestMethod]
        public void Check_MultiCandidate()
        {
            byte[] temp = multiCandidate.ToArray();
            var newCandidate = temp.AsSerializable<MultiCandidate>();

            CollectionAssert.AreEqual(newCandidate.GetCandidate(), multiCandidate.GetCandidate());
        }
    }
}
