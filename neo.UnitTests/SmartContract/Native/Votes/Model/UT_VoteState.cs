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
    public class UT_VoteState
    {
        VoteState voteState;
        SingleCandidate candidate;
        MultiCandidate multiCandidate;
        [TestInitialize]
        public void TestSetup()
        {
            candidate = new SingleCandidate(1);
            List<int> lists = new List<int>();
            lists.Add(1);
            lists.Add(2);
            lists.Add(3);
            multiCandidate = new MultiCandidate(lists);
            voteState = new VoteState(UInt160.Zero, candidate);
        }

        [TestMethod]
        public void Check_VoteState()
        {
            byte[] temp;
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                voteState.Serialize(binaryWriter);
                temp = memoryStream.ToArray();
            }
            using (MemoryStream memoryStream = new MemoryStream(temp, false))
            using (BinaryReader binaryReader = new BinaryReader(memoryStream))
            {
                VoteState newState = new VoteState();
                newState.Deserialize(binaryReader);
                newState.GetCandidate().Should().BeOfType<SingleCandidate>();
            }
            voteState = new VoteState(UInt160.Zero, multiCandidate);
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                voteState.Serialize(binaryWriter);
                temp = memoryStream.ToArray();
            }

            using (MemoryStream memoryStream = new MemoryStream(temp, false))
            using (BinaryReader binaryReader = new BinaryReader(memoryStream))
            {
                VoteState newState = new VoteState();
                newState.Deserialize(binaryReader);
                newState.GetCandidate().Should().BeOfType<MultiCandidate>();
            }
        }
    }
}
