using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Native;
using System;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_HighPriorityAttribute
    {
        [TestMethod]
        public void Size_Get()
        {
            var test = new HighPriorityAttribute();
            test.Size.Should().Be(1);
        }

        [TestMethod]
        public void ToJson()
        {
            var test = new HighPriorityAttribute();
            var json = test.ToJson().ToString();
            Assert.AreEqual(@"{""type"":""HighPriority""}", json);
        }

        [TestMethod]
        public void DeserializeAndSerialize()
        {
            var test = new HighPriorityAttribute();

            var clone = test.ToArray().AsSerializable<HighPriorityAttribute>();
            Assert.AreEqual(clone.Type, test.Type);

            // As transactionAttribute

            byte[] buffer = test.ToArray();
            var reader = new MemoryReader(buffer);
            clone = TransactionAttribute.DeserializeFrom(ref reader) as HighPriorityAttribute;
            Assert.AreEqual(clone.Type, test.Type);

            // Wrong type

            buffer[0] = 0xff;
            reader = new MemoryReader(buffer);
            try
            {
                TransactionAttribute.DeserializeFrom(ref reader);
                Assert.Fail();
            }
            catch (FormatException) { }
            reader = new MemoryReader(buffer);
            try
            {
                new HighPriorityAttribute().Deserialize(ref reader);
                Assert.Fail();
            }
            catch (FormatException) { }
        }

        [TestMethod]
        public void Verify()
        {
            var test = new HighPriorityAttribute();
            var snapshot = TestBlockchain.GetTestSnapshot();

            Assert.IsFalse(test.Verify(snapshot, new Transaction() { Signers = Array.Empty<Signer>() }));
            Assert.IsFalse(test.Verify(snapshot, new Transaction() { Signers = new Signer[] { new Signer() { Account = UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01") } } }));
            Assert.IsTrue(test.Verify(snapshot, new Transaction() { Signers = new Signer[] { new Signer() { Account = NativeContract.NEO.GetCommitteeAddress(snapshot) } } }));
        }
    }
}
