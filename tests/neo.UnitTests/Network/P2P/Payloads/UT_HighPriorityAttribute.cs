using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Native;
using System;
using System.IO;

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

            using var msRead = new MemoryStream();
            using var msWrite = new MemoryStream();
            using (var stream = new BinaryWriter(msWrite))
            {
                var data = (test as TransactionAttribute).ToArray();
                msRead.Write(data);
                msRead.Seek(0, SeekOrigin.Begin);
            }

            using var reader = new BinaryReader(msRead);
            clone = TransactionAttribute.DeserializeFrom(reader) as HighPriorityAttribute;
            Assert.AreEqual(clone.Type, test.Type);

            // Wrong type

            msRead.Seek(0, SeekOrigin.Begin);
            msRead.WriteByte(0xfe);
            msRead.Seek(0, SeekOrigin.Begin);
            Assert.ThrowsException<FormatException>(() => TransactionAttribute.DeserializeFrom(reader));
            msRead.Seek(0, SeekOrigin.Begin);
            Assert.ThrowsException<FormatException>(() => new HighPriorityAttribute().Deserialize(reader));
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
