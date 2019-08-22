using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using Neo.Cryptography;
using Neo.IO;
using Neo.SmartContract.Native.Votes.Model;
using Neo.SmartContract.Native.Votes.Interface;

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
            List<int> list = new List<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            multiCandidate = new MultiCandidate(list);

            singleCandidate = new SingleCandidate(1);
        }

        [TestMethod]
        public void Check_SingleCandidate()
        {
            byte[] temp;
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                singleCandidate.Serialize(binaryWriter);
                temp = memoryStream.ToArray();
            }

            using (MemoryStream memoryStream = new MemoryStream(temp, false))
            using (BinaryReader binaryReader = new BinaryReader(memoryStream))
            {
                SingleCandidate newCandidate = new SingleCandidate();
                newCandidate.Deserialize(binaryReader);
                Assert.AreEqual(newCandidate.GetCandidate(), singleCandidate.GetCandidate());
            }
        }

        [TestMethod]
        public void Check_MultiCandidate()
        {
            byte[] temp;
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                multiCandidate.Serialize(binaryWriter);
                temp = memoryStream.ToArray();
            }

            using (MemoryStream memoryStream = new MemoryStream(temp, false))
            using (BinaryReader binaryReader = new BinaryReader(memoryStream))
            {
                MultiCandidate newCandidate = new MultiCandidate();
                newCandidate.Deserialize(binaryReader);
                Assert.AreEqual(newCandidate.GetCandidate().Count, multiCandidate.GetCandidate().Count);
            }
        }
    }
}
