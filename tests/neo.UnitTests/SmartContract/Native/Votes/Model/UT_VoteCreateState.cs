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

        [TestMethod]
        public void Check_VoteCreateState()
        {
            var createState = new VoteCreateState()
            {
                TransactionHash = UInt256.Zero,
                CallingScriptHash = UInt160.Zero,
                Originator = UInt160.Zero,
                Title = "Title",
                Description = "Descritpion",
                CandidateNumber = 2,
                IsSequence = true
            };
            byte[] temp = createState.ToArray();
            var createState1 = temp.AsSerializable<VoteCreateState>();

            createState1.GetId().Should().BeEquivalentTo(createState.GetId());
            createState.Size.Should().Be(createState1.Size);
        }
    }
}
