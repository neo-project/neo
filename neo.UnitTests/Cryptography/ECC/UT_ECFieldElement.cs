using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using System;
using System.Numerics;
using System.Reflection;

namespace Neo.UnitTests.Cryptography.ECC
{
    [TestClass]
    public class UT_ECFieldElement
    {
        [TestMethod]
        public void TestECFieldElementConstructor ()
        {
            BigInteger input = new BigInteger(100);
            Action action = () => new ECFieldElement(input, ECCurve.Secp256k1);
            action.ShouldNotThrow();

            input = ECCurve.Secp256k1.Q;
            action = () => new ECFieldElement(input, ECCurve.Secp256k1);
            action.ShouldThrow<ArgumentException>();
        }

        [TestMethod]
        public void TestEquals()
        {
            BigInteger input = new BigInteger(100);
            object element = new ECFieldElement(input, ECCurve.Secp256k1);
            element.Equals(element).Should().BeTrue();
            element.Equals(1).Should().BeFalse();

            input = new BigInteger(200);
            element.Equals(new ECFieldElement(input, ECCurve.Secp256k1)).Should().BeFalse();
        }

        [TestMethod]
        public void TestFastLucasSequence()
        {
            BigInteger input = new BigInteger(100);
            ECFieldElement element = new ECFieldElement(input, ECCurve.Secp256k1);
            BigInteger p = ECCurve.Secp256k1.Q, P = new BigInteger(100), Q = new BigInteger(100), k = new BigInteger(100);
            MethodInfo dynMethod = typeof(ECFieldElement).GetMethod("FastLucasSequence", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            BigInteger[] result = (BigInteger[])dynMethod.Invoke(element, new object[] { p, P, Q, k });
            result.Length.Should().Be(2);
            result[0].Should().Be(BigInteger.Parse("54291889122352983044815998754139869736752555616211571799996439440928031994584335748393871158396540907297549" +
                "0355008852387781904431955206172450241620273083920976877257520258622942315361481829146774445786497935462753780354074019371263126918226216372821" +
                "48377922151055949114088968720000000000000000000000000000000000000000000000000000"));
            result[1].Should().Be(BigInteger.Parse("3796195317861153360631143355305468876976676103901517708672250570346390638982"));
        }

        [TestMethod]
        public void TestToByteArray()
        {
            byte[] result = new byte[32];
            result[31] = 100;
            new ECFieldElement(new BigInteger(100), ECCurve.Secp256k1).ToByteArray().Should().BeEquivalentTo(result);

            byte[] result2 = { 2, 53, 250, 221, 129, 194, 130, 43, 179, 240, 120, 119, 151, 61, 80, 242, 139, 242, 42, 49, 190, 142, 232, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            new ECFieldElement(BigInteger.Pow(new BigInteger(10), 75), ECCurve.Secp256k1).ToByteArray().Should().BeEquivalentTo(result2);

            byte[] result3 = { 221, 21, 254, 134, 175, 250, 217, 18, 73, 239, 14, 183, 19, 243, 158, 190, 170, 152, 123, 110, 111, 210, 160, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            new ECFieldElement(BigInteger.Pow(new BigInteger(10), 77), ECCurve.Secp256k1).ToByteArray().Should().BeEquivalentTo(result3);
        }
    }
}
