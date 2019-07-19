using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using ECCurve = Neo.Cryptography.ECC.ECCurve;
using ECPoint = Neo.Cryptography.ECC.ECPoint;

namespace Neo.UnitTests.Cryptography.ECC
{
    [TestClass]
    public class UT_ECPoint
    {
        public static byte[] generatePrivateKey(int privateKeyLength)
        {
            byte[] privateKey = new byte[privateKeyLength];
            for (int i = 0; i < privateKeyLength; i++)
            {
                privateKey[i] = (byte)((byte)i % byte.MaxValue);
            }
            return privateKey;
        }

        [TestMethod]
        public void TestCompareTo()
        {
            ECFieldElement X1 = new ECFieldElement(new BigInteger(100), ECCurve.Secp256k1);
            ECFieldElement Y1 = new ECFieldElement(new BigInteger(200), ECCurve.Secp256k1);
            ECFieldElement X2 = new ECFieldElement(new BigInteger(300), ECCurve.Secp256k1);
            ECFieldElement Y2 = new ECFieldElement(new BigInteger(400), ECCurve.Secp256k1);
            ECPoint point1 = new ECPoint(X1, Y1, ECCurve.Secp256k1);
            ECPoint point2 = new ECPoint(X2, Y1, ECCurve.Secp256k1);
            ECPoint point3 = new ECPoint(X1, Y2, ECCurve.Secp256k1);

            point1.CompareTo(point1).Should().Be(0);
            point1.CompareTo(point2).Should().Be(-1);
            point2.CompareTo(point1).Should().Be(1);
            point1.CompareTo(point3).Should().Be(-1);
            point3.CompareTo(point1).Should().Be(1);
        }

        [TestMethod]
        public void TestECPointConstructor()
        {
            ECPoint point = new ECPoint();
            point.X.Should().BeNull();
            point.Y.Should().BeNull();
            point.Curve.Should().Be(ECCurve.Secp256r1);

            ECFieldElement X = new ECFieldElement(new BigInteger(100), ECCurve.Secp256k1);
            ECFieldElement Y = new ECFieldElement(new BigInteger(200), ECCurve.Secp256k1);
            point = new ECPoint(X, Y, ECCurve.Secp256k1);
            point.X.Should().Be(X);
            point.Y.Should().Be(Y);
            point.Curve.Should().Be(ECCurve.Secp256k1);
        }

        [TestMethod]
        public void TestDecodePoint()
        {
            byte[] input1 = { 0 };
            Action action = () => ECPoint.DecodePoint(input1, ECCurve.Secp256k1);
            action.ShouldThrow<FormatException>();

            byte[] input2 = { 4, 121, 190, 102, 126, 249, 220, 187, 172, 85, 160, 98, 149, 206, 135, 11, 7, 2, 155, 252, 219, 45, 206, 40, 217, 89, 242, 129, 91, 22, 248, 23, 152, 72, 58, 218, 119, 38, 163, 196, 101, 93, 164, 251, 252, 14, 17, 8, 168, 253, 23, 180, 72, 166, 133, 84, 25, 156, 71, 208, 143, 251, 16, 212, 184 };
            ECPoint.DecodePoint(input2, ECCurve.Secp256k1).Should().Be(ECCurve.Secp256k1.G);
            action = () => ECPoint.DecodePoint(input2.Take(32).ToArray(), ECCurve.Secp256k1);
            action.ShouldThrow<FormatException>();

            byte[] input3 = { 2, 121, 190, 102, 126, 249, 220, 187, 172, 85, 160, 98, 149, 206, 135, 11, 7, 2, 155, 252, 219, 45, 206, 40, 217, 89, 242, 129, 91, 22, 248, 23, 152 };
            byte[] input4 = { 3, 107, 23, 209, 242, 225, 44, 66, 71, 248, 188, 230, 229, 99, 164, 64, 242, 119, 3, 125, 129, 45, 235, 51, 160, 244, 161, 57, 69, 216, 152, 194, 150 };
            ECPoint.DecodePoint(input3, ECCurve.Secp256k1).Should().Be(ECCurve.Secp256k1.G);
            ECPoint.DecodePoint(input4, ECCurve.Secp256r1).Should().Be(ECCurve.Secp256r1.G);

            action = () => ECPoint.DecodePoint(input3.Take(input3.Length - 1).ToArray(), ECCurve.Secp256k1);
            action.ShouldThrow<FormatException>();
        }

        [TestMethod]
        public void TestDecompressPoint()
        {
            ECPoint point = new ECPoint(null, null, ECCurve.Secp256k1);
            MethodInfo dynMethod = typeof(ECPoint).GetMethod("DecompressPoint", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            Action action = () => dynMethod.Invoke(point, new object[] { 10, new BigInteger(100), ECCurve.Secp256k1 });
            action.ShouldThrow<TargetInvocationException>().WithInnerException<ArithmeticException>();

            BigInteger X1 = new BigInteger(new byte[] { 150, 194, 152, 216, 69, 57, 161, 244, 160, 51, 235, 45, 129, 125, 3, 119, 242, 64, 164, 99, 229, 230, 188, 248, 71, 66, 44, 225, 242, 209, 23, 107, 0 });
            ((ECPoint)dynMethod.Invoke(point, new object[] { 1, X1, ECCurve.Secp256r1 })).Should().Be(ECCurve.Secp256r1.G);
            ECPoint result = (ECPoint)dynMethod.Invoke(point, new object[] { 0, X1, ECCurve.Secp256r1 });
            result.Should().Be(-ECCurve.Secp256r1.G);
        }

        [TestMethod]
        public void TestDeserializeFrom()
        {
            byte[] input1 = { 0 };
            Action action = () => ECPoint.DeserializeFrom(new BinaryReader(new MemoryStream(input1)), ECCurve.Secp256k1);
            action.ShouldThrow<FormatException>();

            byte[] input2 = { 4, 121, 190, 102, 126, 249, 220, 187, 172, 85, 160, 98, 149, 206, 135, 11, 7, 2, 155, 252, 219, 45, 206, 40, 217, 89, 242, 129, 91, 22, 248, 23, 152, 72, 58, 218, 119, 38, 163, 196, 101, 93, 164, 251, 252, 14, 17, 8, 168, 253, 23, 180, 72, 166, 133, 84, 25, 156, 71, 208, 143, 251, 16, 212, 184 };
            ECPoint.DeserializeFrom(new BinaryReader(new MemoryStream(input2)), ECCurve.Secp256k1).Should().Be(ECCurve.Secp256k1.G);
            action = () => ECPoint.DeserializeFrom(new BinaryReader(new MemoryStream(input2.Take(32).ToArray())), ECCurve.Secp256k1).Should().Be(ECCurve.Secp256k1.G);
            action.ShouldThrow<FormatException>();

            byte[] input3 = { 2, 121, 190, 102, 126, 249, 220, 187, 172, 85, 160, 98, 149, 206, 135, 11, 7, 2, 155, 252, 219, 45, 206, 40, 217, 89, 242, 129, 91, 22, 248, 23, 152 };
            ECPoint.DeserializeFrom(new BinaryReader(new MemoryStream(input3)), ECCurve.Secp256k1).Should().Be(ECCurve.Secp256k1.G);
            byte[] input4 = { 3, 107, 23, 209, 242, 225, 44, 66, 71, 248, 188, 230, 229, 99, 164, 64, 242, 119, 3, 125, 129, 45, 235, 51, 160, 244, 161, 57, 69, 216, 152, 194, 150 };
            ECPoint.DeserializeFrom(new BinaryReader(new MemoryStream(input4)), ECCurve.Secp256r1).Should().Be(ECCurve.Secp256r1.G);

            action = () => ECPoint.DeserializeFrom(new BinaryReader(new MemoryStream(input3.Take(input3.Length - 1).ToArray())), ECCurve.Secp256k1).Should().Be(ECCurve.Secp256k1.G);
            action.ShouldThrow<FormatException>();
        }

        [TestMethod]
        public void TestEncodePoint()
        {
            ECPoint point = new ECPoint(null, null, ECCurve.Secp256k1);
            byte[] result1 = { 0 };
            point.EncodePoint(true).Should().BeEquivalentTo(result1);

            point = ECCurve.Secp256k1.G;
            byte[] result2 = { 4, 121, 190, 102, 126, 249, 220, 187, 172, 85, 160, 98, 149, 206, 135, 11, 7, 2, 155, 252, 219, 45, 206, 40, 217, 89, 242, 129, 91, 22, 248, 23, 152, 72, 58, 218, 119, 38, 163, 196, 101, 93, 164, 251, 252, 14, 17, 8, 168, 253, 23, 180, 72, 166, 133, 84, 25, 156, 71, 208, 143, 251, 16, 212, 184 };
            point.EncodePoint(false).Should().BeEquivalentTo(result2);

            byte[] result3 = { 2, 121, 190, 102, 126, 249, 220, 187, 172, 85, 160, 98, 149, 206, 135, 11, 7, 2, 155, 252, 219, 45, 206, 40, 217, 89, 242, 129, 91, 22, 248, 23, 152 };
            point.EncodePoint(true).Should().BeEquivalentTo(result3);

            point = ECCurve.Secp256r1.G;
            byte[] result4 = { 3, 107, 23, 209, 242, 225, 44, 66, 71, 248, 188, 230, 229, 99, 164, 64, 242, 119, 3, 125, 129, 45, 235, 51, 160, 244, 161, 57, 69, 216, 152, 194, 150 };
            point.EncodePoint(true).Should().BeEquivalentTo(result4);
        }

        [TestMethod]
        public void TestEquals()
        {
            ECPoint point = ECCurve.Secp256k1.G;
            point.Equals(point).Should().BeTrue();
            point.Equals(null).Should().BeFalse();

            point = new ECPoint(null, null, ECCurve.Secp256k1);
            point.Equals(new ECPoint(null, null, ECCurve.Secp256r1)).Should().BeTrue();
            point.Equals(ECCurve.Secp256r1.G).Should().BeFalse();
            ECCurve.Secp256r1.G.Equals(point).Should().BeFalse();

            ECFieldElement X1 = new ECFieldElement(new BigInteger(100), ECCurve.Secp256k1);
            ECFieldElement Y1 = new ECFieldElement(new BigInteger(200), ECCurve.Secp256k1);
            ECFieldElement X2 = new ECFieldElement(new BigInteger(300), ECCurve.Secp256k1);
            ECFieldElement Y2 = new ECFieldElement(new BigInteger(400), ECCurve.Secp256k1);
            ECPoint point1 = new ECPoint(X1, Y1, ECCurve.Secp256k1);
            ECPoint point2 = new ECPoint(X2, Y1, ECCurve.Secp256k1);
            ECPoint point3 = new ECPoint(X1, Y2, ECCurve.Secp256k1);
            point1.Equals(point2).Should().BeFalse();
            point1.Equals(point3).Should().BeFalse();
        }

        [TestMethod]
        public void TestFromBytes()
        {
            byte[] input1 = { 0 };
            Action action = () => ECPoint.FromBytes(input1, ECCurve.Secp256k1);
            action.ShouldThrow<FormatException>();

            byte[] input2 = { 4, 121, 190, 102, 126, 249, 220, 187, 172, 85, 160, 98, 149, 206, 135, 11, 7, 2, 155, 252, 219, 45, 206, 40, 217, 89, 242, 129, 91, 22, 248, 23, 152, 72, 58, 218, 119, 38, 163, 196, 101, 93, 164, 251, 252, 14, 17, 8, 168, 253, 23, 180, 72, 166, 133, 84, 25, 156, 71, 208, 143, 251, 16, 212, 184 };
            ECPoint.FromBytes(input2, ECCurve.Secp256k1).Should().Be(ECCurve.Secp256k1.G);

            byte[] input3 = { 2, 121, 190, 102, 126, 249, 220, 187, 172, 85, 160, 98, 149, 206, 135, 11, 7, 2, 155, 252, 219, 45, 206, 40, 217, 89, 242, 129, 91, 22, 248, 23, 152 };
            ECPoint.FromBytes(input3, ECCurve.Secp256k1).Should().Be(ECCurve.Secp256k1.G);
            ECPoint.FromBytes(input2.Skip(1).ToArray(), ECCurve.Secp256k1).Should().Be(ECCurve.Secp256k1.G);

            byte[] input4 = generatePrivateKey(72);
            ECPoint.FromBytes(input4, ECCurve.Secp256k1).Should().Be(new ECPoint(new ECFieldElement(BigInteger.Parse("3634473727541135791764834762056624681715094789735830699031648273128038409767"), ECCurve.Secp256k1), new ECFieldElement(BigInteger.Parse("18165245710263168158644330920009617039772504630129940696140050972160274286151"), ECCurve.Secp256k1), ECCurve.Secp256k1));

            byte[] input5 = generatePrivateKey(96);
            ECPoint.FromBytes(input5, ECCurve.Secp256k1).Should().Be(new ECPoint(new ECFieldElement(BigInteger.Parse("1780731860627700044960722568376592200742329637303199754547598369979440671"), ECCurve.Secp256k1), new ECFieldElement(BigInteger.Parse("14532552714582660066924456880521368950258152170031413196862950297402215317055"), ECCurve.Secp256k1), ECCurve.Secp256k1));

            byte[] input6 = generatePrivateKey(104);
            ECPoint.FromBytes(input6, ECCurve.Secp256k1).Should().Be(new ECPoint(new ECFieldElement(BigInteger.Parse("3634473727541135791764834762056624681715094789735830699031648273128038409767"), ECCurve.Secp256k1), new ECFieldElement(BigInteger.Parse("18165245710263168158644330920009617039772504630129940696140050972160274286151"), ECCurve.Secp256k1), ECCurve.Secp256k1));
        }

        [TestMethod]
        public void TestMultiply()
        {
            ECPoint p = ECCurve.Secp256k1.G;
            BigInteger k = BigInteger.Parse("100");
            ECPoint.Multiply(p, k).Should().Be(new ECPoint(new ECFieldElement(BigInteger.Parse("107303582290733097924842193972465022053148211775194373671539518313500194639752"), ECCurve.Secp256k1), new ECFieldElement(BigInteger.Parse("103795966108782717446806684023742168462365449272639790795591544606836007446638"), ECCurve.Secp256k1), ECCurve.Secp256k1));

            k = BigInteger.Parse("10000");
            ECPoint.Multiply(p, k).Should().Be(new ECPoint(new ECFieldElement(BigInteger.Parse("55279067612272658004429375184716238028207484982037227804583126224321918234542"), ECCurve.Secp256k1), new ECFieldElement(BigInteger.Parse("93139664895507357192565643142424306097487832058389223752321585898830257071353"), ECCurve.Secp256k1), ECCurve.Secp256k1));

            k = BigInteger.Parse("10000000000000");
            ECPoint.Multiply(p, k).Should().Be(new ECPoint(new ECFieldElement(BigInteger.Parse("115045167963494515061513744671884131783397561769819471159495798754884242293003"), ECCurve.Secp256k1), new ECFieldElement(BigInteger.Parse("93759167105263077270762304290738437383691912799231615884447658154878797241853"), ECCurve.Secp256k1), ECCurve.Secp256k1));

            k = BigInteger.Parse("1000000000000000000000000000000000000000");
            ECPoint.Multiply(p, k).Should().Be(new ECPoint(new ECFieldElement(BigInteger.Parse("114831276968810911840931876895388845736099852671055832194631099067239418074350"), ECCurve.Secp256k1), new ECFieldElement(BigInteger.Parse("16721517996619732311261078486295444964227498319433363271180755596201863690708"), ECCurve.Secp256k1), ECCurve.Secp256k1));

            k = new BigInteger(generatePrivateKey(100));
            ECPoint.Multiply(p, k).Should().Be(new ECPoint(new ECFieldElement(BigInteger.Parse("19222995016448259376216431079553428738726180595337971417371897285865264889977"), ECCurve.Secp256k1), new ECFieldElement(BigInteger.Parse("6637081904924493791520919212064582313497884724460823966446023080706723904419"), ECCurve.Secp256k1), ECCurve.Secp256k1));

            k = new BigInteger(generatePrivateKey(120));
            ECPoint.Multiply(p, k).Should().Be(new ECPoint(new ECFieldElement(BigInteger.Parse("79652345192111851576650978679091010173409410384772942769927955775006682639778"), ECCurve.Secp256k1), new ECFieldElement(BigInteger.Parse("6460429961979335115790346961011058418773289452368186110818621539624566803831"), ECCurve.Secp256k1), ECCurve.Secp256k1));

            k = new BigInteger(generatePrivateKey(300));
            ECPoint.Multiply(p, k).Should().Be(new ECPoint(new ECFieldElement(BigInteger.Parse("105331914562708556186724786757483927866790351460145374033180496740107603569412"), ECCurve.Secp256k1), new ECFieldElement(BigInteger.Parse("60523670886755698512704385951571322569877668383890769288780681319304421873758"), ECCurve.Secp256k1), ECCurve.Secp256k1));
        }

        [TestMethod]
        public void TestDeserialize()
        {
            ECPoint point = new ECPoint(null, null, ECCurve.Secp256k1);
            byte[] input = { 4, 121, 190, 102, 126, 249, 220, 187, 172, 85, 160, 98, 149, 206, 135, 11, 7, 2, 155, 252, 219, 45, 206, 40, 217, 89, 242, 129, 91, 22, 248, 23, 152, 72, 58, 218, 119, 38, 163, 196, 101, 93, 164, 251, 252, 14, 17, 8, 168, 253, 23, 180, 72, 166, 133, 84, 25, 156, 71, 208, 143, 251, 16, 212, 184 };
            MethodInfo dynMethod = typeof(ECPoint).GetMethod("Neo.IO.ISerializable.Deserialize", BindingFlags.NonPublic | BindingFlags.Instance);
            dynMethod.Invoke(point, new object[] { new BinaryReader(new MemoryStream(input)) });
            point.X.Should().Be(ECCurve.Secp256k1.G.X);
            point.Y.Should().Be(ECCurve.Secp256k1.G.Y);
        }

        [TestMethod]
        public void TestSerialize()
        {
            MemoryStream stream = new MemoryStream();
            ECPoint point = new ECPoint(null, null, ECCurve.Secp256k1);
            MethodInfo dynMethod = typeof(ECPoint).GetMethod("Neo.IO.ISerializable.Serialize", BindingFlags.NonPublic | BindingFlags.Instance);
            dynMethod.Invoke(point, new object[] { new BinaryWriter(stream) });
            stream.ToArray().Should().BeEquivalentTo(new byte[] { 0 });
        }

        [TestMethod]
        public void TestOpAddition()
        {
            (ECCurve.Secp256k1.Infinity + ECCurve.Secp256k1.G).Should().Be(ECCurve.Secp256k1.G);
            (ECCurve.Secp256k1.G + ECCurve.Secp256k1.Infinity).Should().Be(ECCurve.Secp256k1.G);
            (ECCurve.Secp256k1.G + ECCurve.Secp256k1.G).Should().Be(new ECPoint(new ECFieldElement(BigInteger.Parse("89565891926547004231252920425935692360644145829622209833684329913297188986597"), ECCurve.Secp256k1), new ECFieldElement(BigInteger.Parse("12158399299693830322967808612713398636155367887041628176798871954788371653930"), ECCurve.Secp256k1), ECCurve.Secp256k1));
            (ECCurve.Secp256k1.G + new ECPoint(ECCurve.Secp256k1.G.X, new ECFieldElement(BigInteger.One, ECCurve.Secp256k1), ECCurve.Secp256k1)).Should().Be(ECCurve.Secp256k1.Infinity);
            (ECCurve.Secp256k1.G + ECCurve.Secp256k1.G + ECCurve.Secp256k1.G).Should().Be(new ECPoint(new ECFieldElement(BigInteger.Parse("112711660439710606056748659173929673102114977341539408544630613555209775888121"), ECCurve.Secp256k1), new ECFieldElement(BigInteger.Parse("25583027980570883691656905877401976406448868254816295069919888960541586679410"), ECCurve.Secp256k1), ECCurve.Secp256k1));
        }

        [TestMethod]
        public void TestOpMultiply()
        {
            ECPoint p = null;
            byte[] n = new byte[] { 1 };
            Action action = () => p = p * n;
            action.ShouldThrow<ArgumentNullException>();

            p = ECCurve.Secp256k1.G;
            n = null;
            action.ShouldThrow<ArgumentNullException>();

            n = new byte[] { 1 };
            action.ShouldThrow<ArgumentException>();

            p = ECCurve.Secp256k1.Infinity;
            n = new byte[32];
            (p * n).Should().Be(p);

            p = ECCurve.Secp256k1.G;
            (p * n).Should().Be(ECCurve.Secp256k1.Infinity);

            n[0] = 1;
            (p * n).Should().Be(new ECPoint(new ECFieldElement(BigInteger.Parse("63395642421589016740518975608504846303065672135176650115036476193363423546538"), ECCurve.Secp256k1), new ECFieldElement(BigInteger.Parse("29236048674093813394523910922582374630829081423043497254162533033164154049666"), ECCurve.Secp256k1), ECCurve.Secp256k1));
        }

        [TestMethod]
        public void TestOpUnaryNegation()
        {
            (-ECCurve.Secp256k1.G).Should().Be(new ECPoint(ECCurve.Secp256k1.G.X, -ECCurve.Secp256k1.G.Y, ECCurve.Secp256k1));
        }

        [TestMethod]
        public void TestTryParse()
        {
            ECPoint.TryParse("00", ECCurve.Secp256k1, out ECPoint result).Should().BeFalse();
            result.Should().BeNull();

            ECPoint.TryParse("0479be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798483ada7726a3c4655da4fbfc0e1108a8fd17b448a68554199c47d08ffb10d4b8", ECCurve.Secp256k1, out result).Should().BeTrue();
            result.Should().Be(ECCurve.Secp256k1.G);
        }

        [TestMethod]
        public void TestTwice()
        {
            ECCurve.Secp256k1.Infinity.Twice().Should().Be(ECCurve.Secp256k1.Infinity);
            new ECPoint(new ECFieldElement(BigInteger.Zero, ECCurve.Secp256k1), new ECFieldElement(BigInteger.Zero, ECCurve.Secp256k1), ECCurve.Secp256k1).Twice().Should().Be(ECCurve.Secp256k1.Infinity);
            ECCurve.Secp256k1.G.Twice().Should().Be(new ECPoint(new ECFieldElement(BigInteger.Parse("89565891926547004231252920425935692360644145829622209833684329913297188986597"), ECCurve.Secp256k1), new ECFieldElement(BigInteger.Parse("12158399299693830322967808612713398636155367887041628176798871954788371653930"), ECCurve.Secp256k1), ECCurve.Secp256k1));
        }
    }
}
