using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Core;
using Neo.IO.Json;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_Witness
    {
        Witness uut;

        [TestInitialize]
        public void TestSetup()
        {
            uut = new Witness();
        }

        [TestMethod]
        public void InvocationScript_Get()
        {
            uut.InvocationScript.Should().BeNull();
        }

        [TestMethod]
        public void InvocationScript_Set()
        {
            byte[] dataArray = new byte[] { 0, 32, 32, 20, 32, 32 };
            uut.InvocationScript = dataArray;
            uut.InvocationScript.Length.Should().Be(6);
            Assert.AreEqual(uut.InvocationScript.ToHexString(), "002020142020");          
        }

        private void setupWitnessWithValues(Witness uut, int lenghtInvocation, int lengthVerification, out byte[] invocationScript, out byte[] verificationScript)
        {
            invocationScript = TestUtils.GetByteArray(lenghtInvocation, 0x20);
            verificationScript = TestUtils.GetByteArray(lengthVerification, 0x20);
            uut.InvocationScript = invocationScript;
            uut.VerificationScript = verificationScript;
        }

        [TestMethod]
        public void SizeWitness_Small_Arrary()
        {
            byte[] invocationScript;
            byte[] verificationScript;
            setupWitnessWithValues(uut, 252, 253, out invocationScript, out verificationScript);

            uut.Size.Should().Be(509); // (1 + 252*1) + (1 + 2 + 253*1)
        }

        [TestMethod]
        public void SizeWitness_Large_Arrary()
        {
            byte[] invocationScript;
            byte[] verificationScript;
            setupWitnessWithValues(uut, 65535, 65536, out invocationScript, out verificationScript);

            uut.Size.Should().Be(131079); // (1 + 2 + 65535*1) + (1 + 4 + 65536*1)
        }

        [TestMethod]
        public void ToJson()
        {
            byte[] invocationScript;
            byte[] verificationScript;
            setupWitnessWithValues(uut, 2, 3, out invocationScript, out verificationScript);

            JObject json = uut.ToJson();
            Assert.IsTrue(json.ContainsProperty("invocation"));
            Assert.IsTrue(json.ContainsProperty("verification"));            
            Assert.AreEqual(json["invocation"].AsString(), "2020");
            Assert.AreEqual(json["verification"].AsString(), "202020");

        }
    }
}