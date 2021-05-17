using System.Numerics;
using System.Security.Cryptography;
using System;
using System.Linq;
namespace Neo.Cryptography
{

    /// <summary>
    /// Verifiable Random Function
    /// Based on VRF draft-irtf-cfrg-vrf-08
    /// 
    /// VRF function consumes an octet string as input
    /// and generates a verifiable random number during the consensus.
    /// </summary>
    public static class VRF
    {
        // Length of the order of the curve in bits
        private static int qlen = 256;
        // 2n = length of a field element in bits rounded up to the nearest even integer
        private static int n = 128;

        /// <summary>
        /// Function for deriving a public key given a secret key point.
        /// </summary>
        /// <param name="prikey"> An ECVRF secret key</param>
        /// <returns> An `ECPoint` representing the public key.</returns>
        public static ECC.ECPoint DerivePubkeyPoint(byte[] prikey)
        {
            if (prikey.Length != 32 && prikey.Length != 96 && prikey.Length != 104)
                throw new ArgumentException(null, nameof(prikey));

            if (prikey.Length == 32)
            {
                return ECC.ECCurve.Secp256r1.G * prikey;
            }
            else
            {
                return ECC.ECPoint.FromBytes(prikey, ECC.ECCurve.Secp256r1);
            }
        }


        /// <summary>
        /// ECVRF Nonce Generation from [RFC6979](https://tools.ietf.org/html/rfc6979)
        /// </summary>
        /// 
        /// <param name="prikey">An ECVRF secret key</param>
        /// <param name="data">An octet string</param>
        /// <returns>An integer between 1 and q-1</returns>
        public static BigInteger GenerateNonce(byte[] prikey, byte[] data)
        {
            // Bits to octets from data - bits2octets(h1)
            // We follow the new VRF-draft-08 in which the input is hashed`
            byte[] data_hash = SHA256.Create().ComputeHash(data);

            var data_trunc = Bits2Octets(data_hash, qlen);
            var padded_data_trunc = AppendLeadingZeros(data_trunc, qlen);

            // Bytes to octets from secret key - int2octects(x)
            // Left padding is required for inserting leading zeros
            byte[] padded_prikey_bytes =
                    AppendLeadingZeros(prikey, qlen);

            // Init `V` & `K`
            // `K = HMAC_K(V || 0x00 || int2octects(prikey) || bits2octects(data))`
            var v = Enumerable.Repeat((byte)0x01, 32).ToArray();
            var k = Enumerable.Repeat((byte)0x00, 32).ToArray();

            // First 2 rounds defined by specification
            for (var i = 0; i < 2; i++)
            {
                k = new HMACSHA256(k).ComputeHash(v.Concat(new byte[] { (byte)i }).ToArray().Concat(padded_prikey_bytes).Concat(padded_data_trunc).ToArray());
                v = new HMACSHA256(k).ComputeHash(v);
            }
            // Loop until valid `BigInteger` extracted from `V` is found
            while (true)
            {
                v = new HMACSHA256(k).ComputeHash(v);
                var ret_bn = Bits2Int(v, qlen);
                if (ret_bn > BigInteger.Zero && ret_bn < ECC.ECCurve.Secp256r1.N)
                {
                    return ret_bn;
                }
                k = new HMACSHA256(k).ComputeHash(v.Concat(new byte[] { (byte)0x00 }).ToArray());
                v = new HMACSHA256(k).ComputeHash(v);
            }
        }


        /// <summary>
        /// Function to convert an arbitrary string to a point in the curve as specified in VRF-draft-08
        /// (section 5.5).
        /// 
        /// </summary>
        /// <param name="data"> A slice representing the data to be converted to a point</param>
        /// <returns> A finite EC point in G.</returns>
        public static ECC.ECPoint ArbitraryStringToPoint(byte[] data)
        {
            byte[] v = new byte[] { 0x02 };
            v = v.Concat(data).ToArray();
            return ECC.ECPoint.FromBytes(v, ECC.ECCurve.Secp256r1);
        }


        /// <summary>
        /// Function to convert a `Hash(PK|DATA)` to a point in the curve as stated in [VRF-draft-08](https://tools.ietf.org/pdf/draft-irtf-cfrg-vrf-08)
        /// (section 5.4.1.1).
        /// </summary>
        /// 
        /// <param name="pubkey"> An `ECPoint` referencing the public key.</param>
        /// <param name="alpha"> A slice containing the input data.</param>
        /// <returns> Hashed value, a finite EC point in G.</returns>
        public static ECC.ECPoint HashToTryAndIncrement(ECC.ECPoint pubkey, byte[] alpha)
        {
            var pk_bytes = pubkey.EncodePoint(true);

            // 3. one_string = 0x01 = int_to_string(1, 1), a single octet with value 1
            byte[] cipher = new byte[] { (byte)CipherSuite.P256_SHA256_TAI, 0x01 };

            // 6.B. hash_string = Hash(suite_string || one_string || PK_string || alpha_string || ctr_string)
            byte[] v = cipher.Concat(pk_bytes).ToArray().Concat(alpha).ToArray().Concat(new byte[] { 0x00 }).ToArray();
            var position = v.Length - 1;

            // `Hash(cipher||PK||data)`
            var hash = SHA256.Create();

            foreach (int ctr in Enumerable.Range(0, 255))
            {
                v[position] = (byte)ctr;
                var attempted_hash = hash.ComputeHash(v);
                try
                {
                    return ArbitraryStringToPoint(attempted_hash);
                }
                catch
                {
                    continue;
                }
            }
            // Can not find a valid value
            throw new Exception();
        }


        /// <summary>
        ///  Function to hash a certain set of points as specified in [VRF-draft-08](https://tools.ietf.org/pdf/draft-irtf-cfrg-vrf-08)
        ///  (section 5.4.3).
        /// </summary>
        /// 
        /// <param name="points">EC points in G</param>
        /// <returns>* If successful, a `BigInteger` representing the hash of the points, truncated to length `n`.</returns>
        public static BigInteger HashPoints(ECC.ECPoint[] points)
        {
            // point_bytes = [P1||P2||...||Pn]
            // H(point_bytes)
            var point_bytes = new byte[] { (byte)CipherSuite.P256_SHA256_TAI, 0x02 };

            foreach (var point in points)
            {
                point_bytes = point_bytes.Concat(point.EncodePoint(true)).ToArray();
            }
            var c_string = SHA256.Create().ComputeHash(point_bytes);
            var truncated_c_string = c_string[0..16];
            return new BigInteger(truncated_c_string, true, true);
        }


        /// <summary>
        /// Decodes a VRF proof
        /// </summary>
        /// <param name="proof"> VRF proof, octet string (ptLen+n+qLen octets)</param>
        /// 
        /// <returns>
        /// "INVALID",
        /// Gamma - EC point
        /// c     - integer between 0 and 2^(8n)-1
        /// s     - integer between 0 and 2^(8qLen)-1
        /// </returns>
        public static Tuple<ECC.ECPoint, BigInteger, BigInteger> DecodeProof(byte[] proof)
        {
            var Gamma = ECC.ECPoint.FromBytes(proof[..33], ECC.ECCurve.Secp256r1);
            var c = new BigInteger(proof[33..49], true, true);
            var s = new BigInteger(proof[49..], true, true);

            return Tuple.Create(Gamma, c, s);
        }


        /// <summary>
        /// Computes the VRF hash output as result of the digest of a ciphersuite-dependent prefix
        /// concatenated with the gamma point ([VRF-draft-08](https://tools.ietf.org/pdf/draft-irtf-cfrg-vrf-08), section 5.2).
        /// </summary>
        /// 
        /// <param name="gamma"> An `ECPoint` representing the VRF gamma.</param>
        /// <returns>A vector of octets with the VRF hash output.</returns>
        public static byte[] GammaToHash(ECC.ECPoint gamma)
        {
            // Multiply gamma with cofactor
            return SHA256.Create().ComputeHash(new byte[] { (byte)CipherSuite.P256_SHA256_TAI, 0x03 }.Concat(gamma.EncodePoint(true)).ToArray());
        }


        /// <summary>
        /// Computes the VRF hash output as result of the digest of a ciphersuite-dependent prefix
        /// concatenated with the gamma point ([VRF-draft-08](https://tools.ietf.org/pdf/draft-irtf-cfrg-vrf-08), section 5.2).
        /// 
        /// </summary>
        /// <param name="proof"> VRF proof, octet string of length ptLen+n+qLen</param>
        /// <returns>VRF hash output, octet string of length hLen</returns>
        public static byte[] ProofToHash(byte[] proof)
        {
            var (gamma_point, _, _) = DecodeProof(proof);
            return GammaToHash(gamma_point);
        }


        /// <summary>
        /// Generates proof from a secret key and message as specified in the
        /// [VRF-draft-08](https://tools.ietf.org/pdf/draft-irtf-cfrg-vrf-08) (section 5.1).
        /// </summary>
        /// 
        /// <param name="prikey"> VRF private key.</param>
        /// <param name="alpha"> A slice representing the message in octets.</param>
        /// <returns>- VRF proof, octet string of length ptLen+n+qLen</returns>
        public static byte[] Prove(byte[] prikey, byte[] alpha)
        {
            // Step 1: derive public key from secret key
            // `Y = x * B`
            if (prikey.Length != 32) throw new FormatException();
            var pubkey_point = DerivePubkeyPoint(prikey);

            // Step 2: Hash to curve
            var h_point = HashToTryAndIncrement(pubkey_point, alpha);

            // Step 3: point to string
            var h_string = h_point.EncodePoint(true);

            // Step 4: Gamma = x * H
            var gamma_point = h_point * prikey;

            // Step 5: k = ECVRF_nonce_generation(SK, h_string)
            var k = GenerateNonce(prikey, h_string);
            var kBytes = k.ToByteArray(true, true);

            // Step 6: c = ECVRF_hash_points(H, Gamma, k*B, k*H)
            var u_point = DerivePubkeyPoint(k.ToByteArray(true, true));
            var v_point = h_point * kBytes;
            var c = HashPoints(new ECC.ECPoint[] { h_point, gamma_point, u_point, v_point });

            // Step 7: s = (k + c*x) mod q
            var num_x = new BigInteger(prikey, true, true);
            var s = (k + (c * num_x)) % ECC.ECCurve.Secp256r1.N;
            var gamma_string = gamma_point.EncodePoint(true);

            // Fixed size; len(c) must be n and len(s)=2n
            var c_string = AppendLeadingZeros(c.ToByteArray(true, true), n);
            var s_string = AppendLeadingZeros(s.ToByteArray(true, true), qlen);

            // Step 8: proof =  [Gamma_string||c_string||s_string]
            var proof = gamma_string.Concat(c_string).ToArray().Concat(s_string).ToArray();
            return proof;
        }


        /// <summary>
        /// Verifies the provided VRF proof and computes the VRF hash output as specified in
        /// [VRF-draft-08](https://tools.ietf.org/pdf/draft-irtf-cfrg-vrf-08) (section 5.3).
        /// </summary>
        /// 
        /// <param name="pubkey"> Public key, an EC point. </param>
        /// <param name="proof"> VRF proof, octet string of length ptLen+n+qLen. </param>
        /// <param name="alpha"> VRF input, octet string. </param>
        /// <returns>("VALID", beta_string), where beta_string is the VRF hash output,
        /// octet string of length hLen; or
        /// "INVALID" </returns>
        public static byte[] Verify(byte[] pubkey, byte[] proof, byte[] alpha)
        {
            // Step 1 2 3: decode proof
            var (gamma_point, c, s) = DecodeProof(proof);

            // Fixed size; len(c) must be n and len(s)=2n
            var c_string = AppendLeadingZeros(c.ToByteArray(true, true), qlen);
            var s_string = AppendLeadingZeros(s.ToByteArray(true, true), qlen);

            // Step 4: hash to curve
            var pubkey_point = ECC.ECPoint.FromBytes(pubkey, ECC.ECCurve.Secp256r1);
            var h_point = HashToTryAndIncrement(pubkey_point, alpha);

            // Step 5: U = s*B - c*Y
            var s_b = DerivePubkeyPoint(s_string);
            var c_y = pubkey_point * c_string;
            var u_point = s_b - c_y;

            // Step 6: V = sH -cGamma
            var s_h = h_point * s_string;
            var c_gamma = gamma_point * c_string;
            var v_point = s_h - c_gamma;

            // Step 5: hash points(...)
            var derived_c = HashPoints(new ECC.ECPoint[] { h_point, gamma_point, u_point, v_point });

            // Step 6: Check validity
            if (!derived_c.Equals(c)) throw new FormatException();
            return GammaToHash(gamma_point);
        }



        /// <summary>
        /// Appends leading zeros if provided slice is smaller than given length.
        /// </summary>
        /// 
        /// <param name="data"> A slice of octets.</param>
        /// <param name="bits_length"> An integer to specify the total length (in bits) after appending zeros.</param>
        /// <returns>A vector of octets with leading zeros (if necessary)</returns>
        private static byte[] AppendLeadingZeros(byte[] data, int bits_length)
        {
            if (data.Length * 8 > bits_length)
            {
                return data;
            }
            var res = Enumerable.Repeat((byte)0x00, (bits_length + 7) / 8).ToArray();
            data.CopyTo(res, res.Length - data.Length);
            return res;
        }


        /// <summary>
        /// Converts a slice of octets into a `BigInteger` of length `qlen` as specified in [RFC6979](https://tools.ietf.org/html/rfc6979#section-2.3.2)
        /// The input bit sequence (of length blen) is transformed into an integer using the big-endian convention
        /// </summary>
        /// 
        /// <param name="data"> A slice representing the number to be converted.</param>
        /// <param name="qlen"> The desired length for the output `BigInteger`.</param>
        /// <returns> If successful, a `BigInteger` representing the conversion.</returns>
        private static BigInteger Bits2Int(byte[] data, int qlen)
        {
            var data_len_bits = data.Length * 8;
            // big-endian
            var res = new BigInteger(data, true, true);
            if (data_len_bits > qlen)
                res >>= (data_len_bits - qlen);
            return res;
        }

        /// <summary>
        /// The bits2octets transform takes as input a sequence of blen bits and
        /// outputs a sequence of rlen bits, as specified in [RFC6979](https://tools.ietf.org/html/rfc6979#section-2.3.4).
        /// </summary>
        /// 
        /// <param name="data"> A slice of octets.</param>
        /// <param name="qlen"> An integer to specify the total length (in bits) after appending zeros.</param>
        /// <param name="order"> If successful, a vector of octets.</param>
        /// <returns></returns>
        private static byte[] Bits2Octets(byte[] data, int qlen)
        {
            var z1 = Bits2Int(data, qlen);
            var z2 = z1 % ECC.ECCurve.Secp256r1.N;
            return z2.Sign < 0 ? Int2Octets(z1, qlen) : Int2Octets(z2, qlen);
        }


        /// <summary>
        /// Convert a value into a sequence of rlen bits, https://tools.ietf.org/html/rfc6979#section-2.3.3
        /// </summary>
        /// 
        /// <param name="v"></param>
        /// <param name="qlen"></param>
        /// <returns></returns>
        private static byte[] Int2Octets(BigInteger v, int qlen)
        {
            var rolen = (qlen + 7) >> 3;
            var v_b = v.ToByteArray(true, true);
            var v_len = v_b.Length;
            // left pad with zeros if it's too short
            if (rolen > v_len)
            {
                var res = Enumerable.Repeat((byte)0x00, rolen).ToArray();
                v_b.CopyTo(res, rolen - v_len);
                return res;
            }

            // drop most significant bytes if it's too long
            return v_b[(v_len - rolen)..];
        }
    }

    /// Different cipher suites for different curves/algorithms
    /// https://tools.ietf.org/pdf/draft-irtf-cfrg-vrf-08.pdf
    /// (section 5.5)
    public enum CipherSuite
    {
        /// `NIST P-256` with `SHA256` and `ECVRF_hash_to_curve_try_and_increment`
        P256_SHA256_TAI = 0x01,
        /// `Secp256k1` with `SHA256` and `ECVRF_hash_to_curve_try_and_increment`
        SECP256K1_SHA256_TAI = 0xFE,
        /// `NIST K-163` with `SHA256` and `ECVRF_hash_to_curve_try_and_increment`
        K163_SHA256_TAI = 0xFF,
    }
}
