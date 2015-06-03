using System.Globalization;
using System.Numerics;

namespace AntShares.Cryptography
{
    public static class Secp256r1Curve
    {
        internal static readonly BigInteger Q = BigInteger.Parse("00FFFFFFFF00000001000000000000000000000000FFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.AllowHexSpecifier);
        internal static readonly Secp256r1Element A = new Secp256r1Element(BigInteger.Parse("00FFFFFFFF00000001000000000000000000000000FFFFFFFFFFFFFFFFFFFFFFFC", NumberStyles.AllowHexSpecifier));
        internal static readonly Secp256r1Element B = new Secp256r1Element(BigInteger.Parse("005AC635D8AA3A93E7B3EBBD55769886BC651D06B0CC53B0F63BCE3C3E27D2604B", NumberStyles.AllowHexSpecifier));
        internal static readonly Secp256r1Point Infinity = new Secp256r1Point(null, null);
        public static readonly Secp256r1Point G = Secp256r1Point.DecodePoint(("04" + "6B17D1F2E12C4247F8BCE6E563A440F277037D812DEB33A0F4A13945D898C296" + "4FE342E2FE1A7F9B8EE7EB4A7C0F9E162BCE33576B315ECECBB6406837BF51F5").HexToBytes());
    }
}
