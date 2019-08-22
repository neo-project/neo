using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using Neo.Cryptography;
using Neo.IO;
using Neo.SmartContract.Native.Votes.Model;

namespace Neo.UnitTests.SmartContract.Native.Votes.Model
{
    [TestClass]
    public class UT_VoteCreateState
    {

        VoteCreateState createState;
        [TestInitialize]
        public void TestSetup()
        {
            createState = new VoteCreateState();
        }

        [TestMethod]
        public void Check_Constructor()
        {
            createState = new VoteCreateState
                (
                    UInt256.Zero,
                    UInt160.Zero,
                    UInt160.Zero,
                    "Title",
                    "Descritpion",
                    2,
                    true
                );

            Byte[] temp;

            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                createState.Serialize(binaryWriter);
                binaryWriter.Flush();
                temp = memoryStream.ToArray();
            }

            VoteCreateState createState1 = new VoteCreateState();

            using (MemoryStream memoryStream1 = new MemoryStream(temp, false))
            using (BinaryReader binaryReader = new BinaryReader(memoryStream1))
            {
                createState1.Deserialize(binaryReader);
                createState1.GetId().ShouldBeEquivalentTo(createState.GetId());
            }
        }


    }
}
