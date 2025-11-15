// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ECPoint.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using Neo.Cryptography.ECC;
using Neo.Extensions.IO;
using Neo.IO;
using System.Numerics;
using ECCurve = Neo.Cryptography.ECC.ECCurve;
using ECPoint = Neo.Cryptography.ECC.ECPoint;

namespace Neo.UnitTests.Cryptography.ECC;

[TestClass]
public class UT_ECPoint
{
    private static readonly string s_uncompressed = "04" +
        "79be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798" +
        "483ada7726a3c4655da4fbfc0e1108a8fd17b448a68554199c47d08ffb10d4b8";

    private static readonly string s_compressedX =
        "02" + "79be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798";

    private static readonly string s_compressedY =
        "03" + "6b17d1f2e12c4247f8bce6e563a440f277037d812deb33a0f4a13945d898c296";

    public static byte[] GeneratePrivateKey(int privateKeyLength)
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
        var X1 = new ECFieldElement(new BigInteger(100), ECCurve.Secp256k1);
        var X2 = new ECFieldElement(new BigInteger(200), ECCurve.Secp256k1);
        var X3 = new ECFieldElement(new BigInteger(100), ECCurve.Secp256r1);

        Assert.AreEqual(-1, X1.CompareTo(X2));
        Assert.ThrowsExactly<InvalidOperationException>(() => X1.CompareTo(X3));
    }

    [TestMethod]
    public void TestECPointConstructor()
    {
        ECPoint point = new();
        Assert.IsNull(point.X);
        Assert.IsNull(point.Y);
        Assert.AreEqual(ECCurve.Secp256r1, point.Curve);

        var X = new ECFieldElement(new BigInteger(100), ECCurve.Secp256k1);
        var Y = new ECFieldElement(new BigInteger(200), ECCurve.Secp256k1);
        point = new ECPoint(X, Y, ECCurve.Secp256k1);
        Assert.AreEqual(X, point.X);
        Assert.AreEqual(Y, point.Y);
        Assert.AreEqual(ECCurve.Secp256k1, point.Curve);
        Assert.ThrowsExactly<ArgumentException>(() => new ECPoint(X, null, ECCurve.Secp256k1));
        Assert.ThrowsExactly<ArgumentException>(() => new ECPoint(null, Y, ECCurve.Secp256k1));
    }

    [TestMethod]
    public void TestDecodePoint()
    {
        byte[] input1 = [0];
        Action action = () => ECPoint.DecodePoint(input1, ECCurve.Secp256k1);
        Assert.ThrowsExactly<FormatException>(action);

        var uncompressed = s_uncompressed.HexToBytes();
        Assert.AreEqual(ECCurve.Secp256k1.G, ECPoint.DecodePoint(uncompressed, ECCurve.Secp256k1));
        action = () => ECPoint.DecodePoint(uncompressed.Take(32).ToArray(), ECCurve.Secp256k1);
        Assert.ThrowsExactly<FormatException>(action);

        byte[] input3 = s_compressedX.HexToBytes();
        byte[] input4 = s_compressedY.HexToBytes();
        Assert.AreEqual(ECCurve.Secp256k1.G, ECPoint.DecodePoint(input3, ECCurve.Secp256k1));
        Assert.AreEqual(ECCurve.Secp256r1.G, ECPoint.DecodePoint(input4, ECCurve.Secp256r1));

        action = () => ECPoint.DecodePoint(input3.Take(input3.Length - 1).ToArray(), ECCurve.Secp256k1);
        Assert.ThrowsExactly<FormatException>(action);
    }

    [TestMethod]
    public void TestDeserializeFrom()
    {
        byte[] input1 = [0];
        var reader1 = new MemoryReader(input1);
        try
        {
            ECPoint.DeserializeFrom(ref reader1, ECCurve.Secp256k1);
            Assert.Fail("Expected FormatException was not thrown");
        }
        catch (FormatException) { }

        var input2 = s_uncompressed.HexToBytes();
        var reader2 = new MemoryReader(input2);
        Assert.AreEqual(ECPoint.DeserializeFrom(ref reader2, ECCurve.Secp256k1), ECCurve.Secp256k1.G);
        reader2 = new(input2.Take(32).ToArray());
        try
        {
            ECPoint.DeserializeFrom(ref reader2, ECCurve.Secp256k1);
            Assert.Fail();
        }
        catch (FormatException) { }

        var input3 = s_compressedX.HexToBytes();
        var reader3 = new MemoryReader(input3);
        Assert.AreEqual(ECCurve.Secp256k1.G, ECPoint.DeserializeFrom(ref reader3, ECCurve.Secp256k1));

        var input4 = s_compressedY.HexToBytes();
        var reader4 = new MemoryReader(input4);
        Assert.AreEqual(ECCurve.Secp256r1.G, ECPoint.DeserializeFrom(ref reader4, ECCurve.Secp256r1));

        reader3 = new(input3.Take(input3.Length - 1).ToArray());
        try
        {
            ECPoint.DeserializeFrom(ref reader3, ECCurve.Secp256k1);
            Assert.Fail("Expected FormatException was not thrown");
        }
        catch (FormatException) { }
    }

    [TestMethod]
    public void TestEncodePoint()
    {
        var point = new ECPoint(null, null, ECCurve.Secp256k1);
        byte[] result1 = [0];
        CollectionAssert.AreEqual(result1, point.EncodePoint(true));

        point = ECCurve.Secp256k1.G;
        var result2 = s_uncompressed.HexToBytes();
        CollectionAssert.AreEqual(result2, point.EncodePoint(false));
        CollectionAssert.AreEqual(result2, point.EncodePoint(false));

        var result3 = s_compressedX.HexToBytes();
        CollectionAssert.AreEqual(result3, point.EncodePoint(true));
        CollectionAssert.AreEqual(result3, point.EncodePoint(true));

        point = ECCurve.Secp256r1.G;
        var result4 = s_compressedY.HexToBytes();
        CollectionAssert.AreEqual(result4, point.EncodePoint(true));
        CollectionAssert.AreEqual(result4, point.EncodePoint(true));

        // Test cache
        point = ECPoint.DecodePoint(ECCurve.Secp256r1.G.EncodePoint(true), ECCurve.Secp256r1);
        CollectionAssert.AreEqual(result4, point.EncodePoint(true));
        CollectionAssert.AreEqual(result4, point.EncodePoint(true));

        var result5 = "04" + "6b17d1f2e12c4247f8bce6e563a440f277037d812deb33a0f4a13945d898c296" +
            "4fe342e2fe1a7f9b8ee7eb4a7c0f9e162bce33576b315ececbb6406837bf51f5";
        point = ECPoint.DecodePoint(ECCurve.Secp256r1.G.EncodePoint(false), ECCurve.Secp256r1);
        CollectionAssert.AreEqual(result4, point.EncodePoint(true));
        CollectionAssert.AreEqual(result4, point.EncodePoint(true));
        CollectionAssert.AreEqual(result5.HexToBytes(), point.EncodePoint(false));
        CollectionAssert.AreEqual(result5.HexToBytes(), point.EncodePoint(false));
    }

    [TestMethod]
    public void TestEquals()
    {
        var point = ECCurve.Secp256k1.G;
        Assert.IsTrue(point.Equals(point));
        Assert.IsFalse(point.Equals(null));

        point = new ECPoint(null, null, ECCurve.Secp256k1);
        Assert.IsFalse(point.Equals(new ECPoint(null, null, ECCurve.Secp256r1)));
        Assert.IsFalse(point.Equals(ECCurve.Secp256r1.G));
        Assert.IsFalse(ECCurve.Secp256r1.G.Equals(point));

        var X1 = new ECFieldElement(new BigInteger(100), ECCurve.Secp256k1);
        var Y1 = new ECFieldElement(new BigInteger(200), ECCurve.Secp256k1);
        var X2 = new ECFieldElement(new BigInteger(300), ECCurve.Secp256k1);
        var Y2 = new ECFieldElement(new BigInteger(400), ECCurve.Secp256k1);
        var point1 = new ECPoint(X1, Y1, ECCurve.Secp256k1);
        var point2 = new ECPoint(X2, Y1, ECCurve.Secp256k1);
        var point3 = new ECPoint(X1, Y2, ECCurve.Secp256k1);
        Assert.IsFalse(point1.Equals(point2));
        Assert.IsFalse(point1.Equals(point3));
    }

    [TestMethod]
    public void TestGetHashCode()
    {
        var pointA = new ECPoint(ECCurve.Secp256k1.G.X, ECCurve.Secp256k1.G.Y, ECCurve.Secp256k1);
        var pointB = new ECPoint(ECCurve.Secp256k1.G.Y, ECCurve.Secp256k1.G.X, ECCurve.Secp256k1);
        var pointC = new ECPoint(ECCurve.Secp256k1.G.Y, ECCurve.Secp256k1.G.X, ECCurve.Secp256r1); // different curve
        var pointD = new ECPoint(ECCurve.Secp256k1.G.Y, ECCurve.Secp256k1.G.X, ECCurve.Secp256k1);

        Assert.AreNotEqual(pointA.GetHashCode(), pointB.GetHashCode());
        Assert.AreNotEqual(pointA.GetHashCode(), pointC.GetHashCode());
        Assert.AreNotEqual(pointB.GetHashCode(), pointC.GetHashCode());
        Assert.AreEqual(pointB.GetHashCode(), pointD.GetHashCode());
    }

    [TestMethod]
    public void TestEqualsObject()
    {
        object point = ECCurve.Secp256k1.G;
        Assert.IsTrue(point.Equals(point));
        Assert.IsFalse(point.Equals(null));
        Assert.IsFalse(point.Equals(1u));

        point = new ECPoint(null, null, ECCurve.Secp256k1);
        Assert.IsFalse(point.Equals(new ECPoint(null, null, ECCurve.Secp256r1)));
        Assert.IsFalse(point.Equals(ECCurve.Secp256r1.G));
        Assert.IsFalse(ECCurve.Secp256r1.G.Equals(point));

        var X1 = new ECFieldElement(new BigInteger(100), ECCurve.Secp256k1);
        var Y1 = new ECFieldElement(new BigInteger(200), ECCurve.Secp256k1);
        var X2 = new ECFieldElement(new BigInteger(300), ECCurve.Secp256k1);
        var Y2 = new ECFieldElement(new BigInteger(400), ECCurve.Secp256k1);
        object point1 = new ECPoint(X1, Y1, ECCurve.Secp256k1);
        object point2 = new ECPoint(X2, Y1, ECCurve.Secp256k1);
        object point3 = new ECPoint(X1, Y2, ECCurve.Secp256k1);
        Assert.IsFalse(point1.Equals(point2));
        Assert.IsFalse(point1.Equals(point3));
    }

    [TestMethod]
    public void TestFromBytes()
    {
        byte[] input1 = [0];
        Assert.ThrowsExactly<FormatException>(() => ECPoint.FromBytes(input1, ECCurve.Secp256k1));

        var input2 = s_uncompressed.HexToBytes();
        Assert.AreEqual(ECCurve.Secp256k1.G, ECPoint.FromBytes(input2, ECCurve.Secp256k1));

        var input3 = s_compressedX.HexToBytes();
        Assert.AreEqual(ECCurve.Secp256k1.G, ECPoint.FromBytes(input3, ECCurve.Secp256k1));
        Assert.AreEqual(ECCurve.Secp256k1.G, ECPoint.FromBytes(input2.Skip(1).ToArray(), ECCurve.Secp256k1));

        var input4 = GeneratePrivateKey(72);
        Assert.AreEqual(
            new ECPoint(
                new(BigInteger.Parse("3634473727541135791764834762056624681715094789735830699031648273128038409767"), ECCurve.Secp256k1),
                new(BigInteger.Parse("18165245710263168158644330920009617039772504630129940696140050972160274286151"), ECCurve.Secp256k1),
                ECCurve.Secp256k1
            ),
            ECPoint.FromBytes(input4, ECCurve.Secp256k1));

        var input5 = GeneratePrivateKey(96);
        Assert.AreEqual(
            new ECPoint(
                new(BigInteger.Parse("1780731860627700044960722568376592200742329637303199754547598369979440671"), ECCurve.Secp256k1),
                new(BigInteger.Parse("14532552714582660066924456880521368950258152170031413196862950297402215317055"), ECCurve.Secp256k1),
                ECCurve.Secp256k1
            ),
            ECPoint.FromBytes(input5, ECCurve.Secp256k1));

        var input6 = GeneratePrivateKey(104);
        Assert.AreEqual(
            new ECPoint(
                new(BigInteger.Parse("3634473727541135791764834762056624681715094789735830699031648273128038409767"), ECCurve.Secp256k1),
                new(BigInteger.Parse("18165245710263168158644330920009617039772504630129940696140050972160274286151"), ECCurve.Secp256k1),
                ECCurve.Secp256k1
            ),
            ECPoint.FromBytes(input6, ECCurve.Secp256k1));
    }

    [TestMethod]
    public void TestGetSize()
    {
        Assert.AreEqual(33, ECCurve.Secp256k1.G.Size);
        Assert.AreEqual(1, ECCurve.Secp256k1.Infinity.Size);
    }

    [TestMethod]
    public void TestMultiply()
    {
        ECPoint p = ECCurve.Secp256k1.G;
        BigInteger k = BigInteger.Parse("100");
        Assert.AreEqual(
            new ECPoint(
                new(BigInteger.Parse("107303582290733097924842193972465022053148211775194373671539518313500194639752"), ECCurve.Secp256k1),
                new(BigInteger.Parse("103795966108782717446806684023742168462365449272639790795591544606836007446638"), ECCurve.Secp256k1),
                ECCurve.Secp256k1
            ),
            ECPoint.Multiply(p, k));

        k = BigInteger.Parse("10000");
        Assert.AreEqual(new ECPoint(
                new(BigInteger.Parse("55279067612272658004429375184716238028207484982037227804583126224321918234542"), ECCurve.Secp256k1),
                new(BigInteger.Parse("93139664895507357192565643142424306097487832058389223752321585898830257071353"), ECCurve.Secp256k1),
                ECCurve.Secp256k1
            ),
            ECPoint.Multiply(p, k));

        k = BigInteger.Parse("10000000000000");
        Assert.AreEqual(new ECPoint(
                new(BigInteger.Parse("115045167963494515061513744671884131783397561769819471159495798754884242293003"), ECCurve.Secp256k1),
                new(BigInteger.Parse("93759167105263077270762304290738437383691912799231615884447658154878797241853"), ECCurve.Secp256k1),
                ECCurve.Secp256k1
            ),
            ECPoint.Multiply(p, k));

        k = BigInteger.Parse("1000000000000000000000000000000000000000");
        Assert.AreEqual(new ECPoint(
                new(BigInteger.Parse("114831276968810911840931876895388845736099852671055832194631099067239418074350"), ECCurve.Secp256k1),
                new(BigInteger.Parse("16721517996619732311261078486295444964227498319433363271180755596201863690708"), ECCurve.Secp256k1),
                ECCurve.Secp256k1
            ),
            ECPoint.Multiply(p, k));

        k = new BigInteger(GeneratePrivateKey(100));
        Assert.AreEqual(new ECPoint(
                new(BigInteger.Parse("19222995016448259376216431079553428738726180595337971417371897285865264889977"), ECCurve.Secp256k1),
                new(BigInteger.Parse("6637081904924493791520919212064582313497884724460823966446023080706723904419"), ECCurve.Secp256k1),
                ECCurve.Secp256k1
            ),
            ECPoint.Multiply(p, k));

        k = new BigInteger(GeneratePrivateKey(120));
        Assert.AreEqual(new ECPoint(
                new(BigInteger.Parse("79652345192111851576650978679091010173409410384772942769927955775006682639778"), ECCurve.Secp256k1),
                new(BigInteger.Parse("6460429961979335115790346961011058418773289452368186110818621539624566803831"), ECCurve.Secp256k1),
                ECCurve.Secp256k1
            ),
            ECPoint.Multiply(p, k));

        k = new BigInteger(GeneratePrivateKey(300));
        Assert.AreEqual(new ECPoint(
                new(BigInteger.Parse("105331914562708556186724786757483927866790351460145374033180496740107603569412"), ECCurve.Secp256k1),
                new(BigInteger.Parse("60523670886755698512704385951571322569877668383890769288780681319304421873758"), ECCurve.Secp256k1),
                ECCurve.Secp256k1
            ),
            ECPoint.Multiply(p, k));
    }

    [TestMethod]
    public void TestDeserialize()
    {
        var point = new ECPoint(null, null, ECCurve.Secp256k1);
        ISerializable serializable = point;

        var input = s_uncompressed.HexToBytes();
        var reader = new MemoryReader(input);
        serializable.Deserialize(ref reader);
        Assert.AreEqual(ECCurve.Secp256k1.G.X, point.X);
        Assert.AreEqual(ECCurve.Secp256k1.G.Y, point.Y);
    }

    [TestMethod]
    public void TestSerialize()
    {
        var stream = new MemoryStream();
        var point = new ECPoint(null, null, ECCurve.Secp256k1);
        ISerializable serializable = point;
        serializable.Serialize(new BinaryWriter(stream));
        CollectionAssert.AreEqual(new byte[] { 0 }, stream.ToArray());

        CollectionAssert.AreEqual(point.GetSpan().ToArray(), stream.ToArray());
        point = ECCurve.Secp256r1.G;
        CollectionAssert.AreEqual(point.GetSpan().ToArray(), point.ToArray());
    }

    [TestMethod]
    public void TestOpAddition()
    {
        Assert.AreEqual(ECCurve.Secp256k1.Infinity + ECCurve.Secp256k1.G, ECCurve.Secp256k1.G);
        Assert.AreEqual(ECCurve.Secp256k1.G + ECCurve.Secp256k1.Infinity, ECCurve.Secp256k1.G);
        Assert.AreEqual(ECCurve.Secp256k1.G + ECCurve.Secp256k1.G, new ECPoint(
            new(BigInteger.Parse("89565891926547004231252920425935692360644145829622209833684329913297188986597"), ECCurve.Secp256k1),
            new(BigInteger.Parse("12158399299693830322967808612713398636155367887041628176798871954788371653930"), ECCurve.Secp256k1),
            ECCurve.Secp256k1
        ));

        Assert.AreEqual(ECCurve.Secp256k1.Infinity,
            ECCurve.Secp256k1.G + new ECPoint(ECCurve.Secp256k1.G.X, new(BigInteger.One, ECCurve.Secp256k1), ECCurve.Secp256k1));
        Assert.AreEqual(ECCurve.Secp256k1.G + ECCurve.Secp256k1.G + ECCurve.Secp256k1.G, new ECPoint(
            new(BigInteger.Parse("112711660439710606056748659173929673102114977341539408544630613555209775888121"), ECCurve.Secp256k1),
            new(BigInteger.Parse("25583027980570883691656905877401976406448868254816295069919888960541586679410"), ECCurve.Secp256k1),
            ECCurve.Secp256k1
        ));
    }

    [TestMethod]
    public void TestOpMultiply()
    {
        var p = ECCurve.Secp256k1.G;
        byte[] n = [1];
        Assert.ThrowsExactly<ArgumentException>(() => p *= n);

        p = ECCurve.Secp256k1.Infinity;
        n = new byte[32];
        Assert.AreEqual(p, p * n);

        p = ECCurve.Secp256k1.G;
        Assert.AreEqual(ECCurve.Secp256k1.Infinity, p * n);

        n[0] = 1;
        Assert.AreEqual(new ECPoint(
            new(BigInteger.Parse("63395642421589016740518975608504846303065672135176650115036476193363423546538"), ECCurve.Secp256k1),
            new(BigInteger.Parse("29236048674093813394523910922582374630829081423043497254162533033164154049666"), ECCurve.Secp256k1),
            ECCurve.Secp256k1
        ), p * n);
    }

    [TestMethod]
    public void TestOpSubtraction()
    {
        Assert.AreEqual(ECCurve.Secp256k1.G, ECCurve.Secp256k1.G - ECCurve.Secp256k1.Infinity);
        Assert.AreEqual(ECCurve.Secp256k1.Infinity, ECCurve.Secp256k1.G - ECCurve.Secp256k1.G);
    }

    [TestMethod]
    public void TestOpUnaryNegation()
    {
        Assert.AreEqual(new ECPoint(ECCurve.Secp256k1.G.X, -ECCurve.Secp256k1.G.Y!, ECCurve.Secp256k1), -ECCurve.Secp256k1.G);
    }

    [TestMethod]
    public void TestTryParse()
    {
        Assert.IsFalse(ECPoint.TryParse("00", ECCurve.Secp256k1, out var result));
        Assert.IsNull(result);

        Assert.IsTrue(ECPoint.TryParse(s_uncompressed, ECCurve.Secp256k1, out result));
        Assert.AreEqual(ECCurve.Secp256k1.G, result);
    }

    [TestMethod]
    public void TestTwice()
    {
        Assert.AreEqual(ECCurve.Secp256k1.Infinity, ECCurve.Secp256k1.Infinity.Twice());
        Assert.AreEqual(
            ECCurve.Secp256k1.Infinity,
            new ECPoint(
                new(BigInteger.Zero, ECCurve.Secp256k1),
                new(BigInteger.Zero, ECCurve.Secp256k1),
                ECCurve.Secp256k1
            ).Twice());
        Assert.AreEqual(new ECPoint(
            new(BigInteger.Parse("89565891926547004231252920425935692360644145829622209833684329913297188986597"), ECCurve.Secp256k1),
            new(BigInteger.Parse("12158399299693830322967808612713398636155367887041628176798871954788371653930"), ECCurve.Secp256k1),
            ECCurve.Secp256k1
        ), ECCurve.Secp256k1.G.Twice());
    }
}

#nullable disable
