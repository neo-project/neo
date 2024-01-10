namespace Neo.Cryptography.BLS12_381.Tests;

[TestClass]
public class UT_Pairings
{
    [TestMethod]
    public void TestGtGenerator()
    {
        Assert.AreEqual(
            Gt.Generator,
            Bls12.Pairing(in G1Affine.Generator, in G2Affine.Generator)
        );
    }

    [TestMethod]
    public void TestBilinearity()
    {
        var a = Scalar.FromRaw(new ulong[] { 1, 2, 3, 4 }).Invert().Square();
        var b = Scalar.FromRaw(new ulong[] { 5, 6, 7, 8 }).Invert().Square();
        var c = a * b;

        var g = new G1Affine(G1Affine.Generator * a);
        var h = new G2Affine(G2Affine.Generator * b);
        var p = Bls12.Pairing(in g, in h);

        Assert.AreNotEqual(Gt.Identity, p);

        var expected = new G1Affine(G1Affine.Generator * c);

        Assert.AreEqual(p, Bls12.Pairing(in expected, in G2Affine.Generator));
        Assert.AreEqual(
            p,
            Bls12.Pairing(in G1Affine.Generator, in G2Affine.Generator) * c
        );
    }

    [TestMethod]
    public void TestUnitary()
    {
        var g = G1Affine.Generator;
        var h = G2Affine.Generator;
        var p = -Bls12.Pairing(in g, in h);
        var q = Bls12.Pairing(in g, -h);
        var r = Bls12.Pairing(-g, in h);

        Assert.AreEqual(p, q);
        Assert.AreEqual(q, r);
    }

    [TestMethod]
    public void TestMillerLoopResultDefault()
    {
        Assert.AreEqual(
            Gt.Identity,
            new MillerLoopResult(Fp12.One).FinalExponentiation()
        );
    }
}
