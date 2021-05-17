using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using System;
using System.Numerics;
using FluentAssertions;
using System.Linq;

namespace Neo.UnitTests.Cryptography
{
    [TestClass]
    public class UT_VRF
    {
        [TestMethod]
        public void TestDerivePublicKey_()
        {
            Action action = () => VRF.DerivePubkeyPoint(new byte[] { 0x01 });
            action.Should().Throw<ArgumentException>();
        }

        /// Test vector for `P256-SHA256-TAI` cipher suite
        /// ASCII: "sample"
        /// Source: [VRF-draft-08](https://tools.ietf.org/pdf/draft-irtf-cfrg-vrf-08) (section A.1)
        [TestMethod]
        public void TestProveP256Sha256Tai_1()
        {
            // Secret Key (labelled as x)
            var x = "c9afa9d845ba75166b5c215767b1d6934e50c3db36e89b127b8a622b120f6721".HexToBytes();
            // Data: ASCII "sample"
            var alpha = "73616d706c65".HexToBytes();

            var pi = VRF.Prove(x, alpha);
            var expected_pi = "029bdca4cc39e57d97e2f42f88bcf0ecb1120fb67eb408a856050dbfbcbf57c524347fc46ccd87843ec0a9fdc090a407c6fbae8ac1480e240c58854897eabbc3a7bb61b201059f89186e7175af796d65e7".HexToBytes();
            pi.SequenceEqual(expected_pi).Should().BeTrue();
        }

        /// Test vector for `P256-SHA256-TAI` cipher suite
        /// ASCII: "sample"
        /// Source: [VRF-draft-08](https://tools.ietf.org/pdf/draft-irtf-cfrg-vrf-08) (section A.1)
        [TestMethod]
        public void TestVerifyP256Sha256Tai_1()
        {
            var y = "0360fed4ba255a9d31c961eb74c6356d68c049b8923b61fa6ce669622e60f29fb6".HexToBytes();
            var pi = "029bdca4cc39e57d97e2f42f88bcf0ecb1120fb67eb408a856050dbfbcbf57c524347fc46ccd87843ec0a9fdc090a407c6fbae8ac1480e240c58854897eabbc3a7bb61b201059f89186e7175af796d65e7".HexToBytes();
            var alpha = "73616d706c65".HexToBytes();
            var beta = VRF.Verify(y, pi, alpha);
            var expected_beta =
                "59ca3801ad3e981a88e36880a3aee1df38a0472d5be52d6e39663ea0314e594c".HexToBytes();

            beta.SequenceEqual(expected_beta).Should().BeTrue();
        }

        /// Test vector for `P256-SHA256-TAI` cipher suite
        /// ASCII: "test"
        /// Source: [VRF-draft-08](https://tools.ietf.org/pdf/draft-irtf-cfrg-vrf-08) (section A.1)
        [TestMethod]
        public void TestProveP256Sha256Tai_2()
        {
            var x = "c9afa9d845ba75166b5c215767b1d6934e50c3db36e89b127b8a622b120f6721".HexToBytes();
            var alpha = "74657374".HexToBytes();
            var pi = VRF.Prove(x, alpha);
            var expected_pi = "03873a1cce2ca197e466cc116bca7b1156fff599be67ea40b17256c4f34ba2549c94ffd2b31588b5fe034fd92c87de5b520b12084da6c4ab63080a7c5467094a1ee84b80b59aca54bba2e2baa0d108191b".HexToBytes();
            pi.SequenceEqual(expected_pi).Should().BeTrue();
        }

        /// Test vector for `P256-SHA256-TAI` cipher suite
        /// ASCII: "test"
        /// Source: [VRF-draft-08](https://tools.ietf.org/pdf/draft-irtf-cfrg-vrf-08) (section A.1)
        [TestMethod]
        public void TestVerifyP256Sha256Tai_2()
        {
            var y = "0360fed4ba255a9d31c961eb74c6356d68c049b8923b61fa6ce669622e60f29fb6".HexToBytes();
            var pi = "03873a1cce2ca197e466cc116bca7b1156fff599be67ea40b17256c4f34ba2549c94ffd2b31588b5fe034fd92c87de5b520b12084da6c4ab63080a7c5467094a1ee84b80b59aca54bba2e2baa0d108191b".HexToBytes();
            var alpha = "74657374".HexToBytes();
            var beta = VRF.Verify(y, pi, alpha);
            var expected_beta =
                "dc85c20f95100626eddc90173ab58d5e4f837bb047fb2f72e9a408feae5bc6c1".HexToBytes();
            beta.SequenceEqual(expected_beta).Should().BeTrue();
        }

        /// Test vector for `P256-SHA256-TAI` cipher suite
        /// ASCII: "Example of ECDSA with ansip256r1 and SHA-256"
        /// Source: [VRF-draft-08](https://tools.ietf.org/pdf/draft-irtf-cfrg-vrf-08) (section A.1)
        [TestMethod]
        public void TestProveP256Sha256Tai_3()
        {
            var x = "2ca1411a41b17b24cc8c3b089cfd033f1920202a6c0de8abb97df1498d50d2c8".HexToBytes();
            var alpha = "4578616d706c65206f66204543445341207769746820616e736970323536723120616e64205348412d323536".HexToBytes();
            var expected_pi = "02abe3ce3b3aa2ab3c6855a7e729517ebfab6901c2fd228f6fa066f15ebc9b9d415a680736f7c33f6c796e367f7b2f467026495907affb124be9711cf0e2d05722d3a33e11d0c5bf932b8f0c5ed1981b64".HexToBytes();
            var pi = VRF.Prove(x, alpha);
            pi.SequenceEqual(expected_pi).Should().BeTrue();
        }

        /// Test vector for `P256-SHA256-TAI` cipher suite
        /// ASCII: "Example of ECDSA with ansip256r1 and SHA-256"
        /// Source: [VRF-draft-08](https://tools.ietf.org/pdf/draft-irtf-cfrg-vrf-08) (section A.1)
        [TestMethod]
        public void TestVerifyP256Sha256Tai_3()
        {
            var y = "03596375e6ce57e0f20294fc46bdfcfd19a39f8161b58695b3ec5b3d16427c274d".HexToBytes();
            var pi = "02abe3ce3b3aa2ab3c6855a7e729517ebfab6901c2fd228f6fa066f15ebc9b9d415a680736f7c33f6c796e367f7b2f467026495907affb124be9711cf0e2d05722d3a33e11d0c5bf932b8f0c5ed1981b64".HexToBytes();
            var alpha = "4578616d706c65206f66204543445341207769746820616e736970323536723120616e64205348412d323536".HexToBytes();
            var beta = VRF.Verify(y, pi, alpha);
            var expected_beta =
                "e880bde34ac5263b2ce5c04626870be2cbff1edcdadabd7d4cb7cbc696467168".HexToBytes();
            beta.SequenceEqual(expected_beta).Should().BeTrue();
        }

        /// Test vector for `P256-SHA256-TAI` cipher suite
        /// ASCII: "sample"
        /// Source: [VRF-draft-08](https://tools.ietf.org/pdf/draft-irtf-cfrg-vrf-08) (section A.1)
        [TestMethod]
        public void TestHashToTryAndIncrement_1()
        {
            var public_key_hex =
                "0360fed4ba255a9d31c961eb74c6356d68c049b8923b61fa6ce669622e60f29fb6".HexToBytes();
            var public_key = Neo.Cryptography.ECC.ECPoint.FromBytes(public_key_hex, Neo.Cryptography.ECC.ECCurve.Secp256r1);
            var data = "73616d706c65".HexToBytes();
            var hash = VRF.HashToTryAndIncrement(public_key, data);
            var hash_bytes = hash.EncodePoint(true);
            var expected_hash =
                "02e2e1ab1b9f5a8a68fa4aad597e7493095648d3473b213bba120fe42d1a595f3e".HexToBytes();
            hash_bytes.SequenceEqual(expected_hash).Should().BeTrue();
        }

        /// Test vector for `P256-SHA256-TAI` cipher suite
        /// ASCII: "test"
        /// Source: [VRF-draft-08](https://tools.ietf.org/pdf/draft-irtf-cfrg-vrf-08) (section A.1)
        [TestMethod]
        public void TestHashToTryAndIncrement_2()
        {
            var public_key_hex =
                "03596375e6ce57e0f20294fc46bdfcfd19a39f8161b58695b3ec5b3d16427c274d".HexToBytes();
            var public_key = Neo.Cryptography.ECC.ECPoint.FromBytes(public_key_hex, Neo.Cryptography.ECC.ECCurve.Secp256r1);
            var data = "4578616d706c65206f66204543445341207769746820616e736970323536723120616e64205348412d323536".HexToBytes();
            var hash = VRF.HashToTryAndIncrement(public_key, data);
            var hash_bytes = hash.EncodePoint(true);

            var expected_hash =
                "02141e41d4d55802b0e3adaba114c81137d95fd3869b6b385d4487b1130126648d".HexToBytes();
            hash_bytes.SequenceEqual(expected_hash).Should().BeTrue();
        }

        /// Test vector for `P-256` curve with `SHA-256`
        /// Message: sample
        /// Source: [RFC6979](https://tools.ietf.org/html/rfc6979) (section A.2.5)
        [TestMethod]
        public void TestGenerateNonceP256_1()
        {
            var sk = "c9afa9d845ba75166b5c215767b1d6934e50c3db36e89b127b8a622b120f6721".HexToBytes();
            var data = "73616d706c65".HexToBytes();
            var nonce = VRF.GenerateNonce(sk, data).ToByteArray(true, true);
            var expected_nonce =
                "A6E3C57DD01ABE90086538398355DD4C3B17AA873382B0F24D6129493D8AAD60".HexToBytes();
            nonce.SequenceEqual(expected_nonce).Should().BeTrue(); ;
        }

        /// Test vector for `P-256` curve with `SHA-256`
        /// Message: test
        /// Source: [RFC6979](https://tools.ietf.org/html/rfc6979) (section A.2.5)
        [TestMethod]
        public void TestGenerateNonceP256_2()
        {
            var sk = "c9afa9d845ba75166b5c215767b1d6934e50c3db36e89b127b8a622b120f6721".HexToBytes();
            var data = "74657374".HexToBytes();
            var nonce = VRF.GenerateNonce(sk, data).ToByteArray(true, true);
            var expected_nonce =
                "D16B6AE827F17175E040871A1C7EC3500192C4C92677336EC2537ACAEE0008E0".HexToBytes();
            nonce.SequenceEqual(expected_nonce).Should().BeTrue();
        }

        /// Test vector for `P256-SHA256-TAI` cipher suite
        /// ASCII: "sample"
        /// Source: [VRF-draft-08](https://tools.ietf.org/pdf/draft-irtf-cfrg-vrf-08) (section A.1)
        [TestMethod]
        public void TestGenerateNonceP256_3()
        {
            var sk = "c9afa9d845ba75166b5c215767b1d6934e50c3db36e89b127b8a622b120f6721".HexToBytes();
            var data =
                "02e2e1ab1b9f5a8a68fa4aad597e7493095648d3473b213bba120fe42d1a595f3e".HexToBytes();
            var nonce = VRF.GenerateNonce(sk, data).ToByteArray(true, true);
            var expected_nonce =
                "b7de5757b28c349da738409dfba70763ace31a6b15be8216991715fbc833e5fa".HexToBytes();
            nonce.SequenceEqual(expected_nonce).Should().BeTrue(); ;
        }

        /// Test vector for `P256-SHA256-TAI` cipher suite
        /// ASCII: "test"
        /// Source: [VRF-draft-08](https://tools.ietf.org/pdf/draft-irtf-cfrg-vrf-08) (section A.1)
        [TestMethod]
        public void TestGenerateNonceP256_4()
        {
            var sk = "c9afa9d845ba75166b5c215767b1d6934e50c3db36e89b127b8a622b120f6721".HexToBytes();
            var data =
                "02ca565721155f9fd596f1c529c7af15dad671ab30c76713889e3d45b767ff6433".HexToBytes();
            var nonce = VRF.GenerateNonce(sk, data).ToByteArray(true, true);
            var expected_nonce =
                "c3c4f385523b814e1794f22ad1679c952e83bff78583c85eb5c2f6ea6eee2e7d".HexToBytes();
            nonce.SequenceEqual(expected_nonce).Should().BeTrue();
        }

        /// Test vector for `P256-SHA256-TAI` cipher suite
        /// ASCII: "Example of ECDSA with ansip256r1 and SHA-256"
        /// Source: [VRF-draft-08](https://tools.ietf.org/pdf/draft-irtf-cfrg-vrf-08) (section A.1)
        [TestMethod]
        public void TestGenerateNonceP256_5()
        {
            var sk = "2ca1411a41b17b24cc8c3b089cfd033f1920202a6c0de8abb97df1498d50d2c8".HexToBytes();
            var data =
                "02141e41d4d55802b0e3adaba114c81137d95fd3869b6b385d4487b1130126648d".HexToBytes();
            var nonce = VRF.GenerateNonce(sk, data).ToByteArray(true, true);
            var expected_nonce =
                "6ac8f1efa102bdcdcc8db99b755d39bc995491e3f9dea076add1905a92779610".HexToBytes();
            nonce.SequenceEqual(expected_nonce).Should().BeTrue();
        }

        /// Test vector for `P256-SHA256-TAI` cipher suite
        /// ASCII: "sample"
        /// Source: [VRF-draft-08](https://tools.ietf.org/pdf/draft-irtf-cfrg-vrf-08) (section A.1)
        [TestMethod]
        public void TestHashPoints()
        {
            var hash_hex =
                "02e2e1ab1b9f5a8a68fa4aad597e7493095648d3473b213bba120fe42d1a595f3e".HexToBytes();
            var pi_hex = "029bdca4cc39e57d97e2f42f88bcf0ecb1120fb67eb408a856050dbfbcbf57c524347fc46ccd87843ec0a9fdc090a407c6fbae8ac1480e240c58854897eabbc3a7bb61b201059f89186e7175af796d65e7".HexToBytes();
            var hash_point = Neo.Cryptography.ECC.ECPoint.FromBytes(hash_hex, Neo.Cryptography.ECC.ECCurve.Secp256r1);
            var c_s_hex = pi_hex[33..];
            var gamma_point = Neo.Cryptography.ECC.ECPoint.FromBytes(pi_hex[..33], Neo.Cryptography.ECC.ECCurve.Secp256r1);
            var gamma_hex = gamma_point.EncodePoint(true);
            var u_hex =
                "030286d82c95d54feef4d39c000f8659a5ce00a5f71d3a888bd1b8e8bf07449a50".HexToBytes();
            var u_point = Neo.Cryptography.ECC.ECPoint.FromBytes(u_hex, Neo.Cryptography.ECC.ECCurve.Secp256r1);
            var v_hex =
                "03e4258b4a5f772ed29830050712fa09ea8840715493f78e5aaaf7b27248efc216".HexToBytes();
            var v_point = Neo.Cryptography.ECC.ECPoint.FromBytes(v_hex, Neo.Cryptography.ECC.ECCurve.Secp256r1);
            var computed_c = VRF.HashPoints(new Neo.Cryptography.ECC.ECPoint[] { hash_point, gamma_point, u_point, v_point }).ToByteArray(false, true);
            var expected_c = c_s_hex[..16];
            computed_c.SequenceEqual(expected_c).Should().BeTrue();
        }

        /// Test vector for `P256-SHA256-TAI` cipher suite
        /// ASCII: "sample"
        /// Source: [VRF-draft-08](https://tools.ietf.org/pdf/draft-irtf-cfrg-vrf-08) (section A.1)
        [TestMethod]
        public void TestDecodeProof()
        {
            var gamma_hex = "029bdca4cc39e57d97e2f42f88bcf0ecb1120fb67eb408a856050dbfbcbf57c524347fc46ccd87843ec0a9fdc090a407c6fbae8ac1480e240c58854897eabbc3a7bb61b201059f89186e7175af796d65e7".HexToBytes();
            var (derived_gamma, derived_c, _) = VRF.DecodeProof(gamma_hex);
            var c_s_hex = gamma_hex[33..];
            var c_hex = c_s_hex[..16];
            var expected_gamma = Neo.Cryptography.ECC.ECPoint.FromBytes(gamma_hex[..33], Neo.Cryptography.ECC.ECCurve.Secp256r1);
            var expected_c = new BigInteger(c_hex, true, true);
            derived_c.Equals(expected_c).Should().BeTrue();
            expected_gamma.Equals(derived_gamma).Should().BeTrue();
        }
    }
}
