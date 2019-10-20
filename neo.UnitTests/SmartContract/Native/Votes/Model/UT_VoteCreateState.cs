using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        public void Check_VoteCreateState()
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

            byte[] temp = createState.ToArray();
            var createState1 = temp.AsSerializable<VoteCreateState>();

            createState1.GetId().ShouldBeEquivalentTo(createState.GetId());
            createState.Size.ShouldBeEquivalentTo(createState1.Size);
        }
    }
}
