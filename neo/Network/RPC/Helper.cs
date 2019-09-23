using Neo.SmartContract;
using Neo.Wallets;
using System;
using System.Numerics;

namespace Neo.Network.RPC
{
    public static class Helper
    {
        public const int AddressLength = 34;
        public const int HashLength = 40;
        public const int WifLength = 52;
        public const int PrivateKeyLength = 64;

        /// <summary>
        /// Parse address or scripthash string to UInt160
        /// </summary>
        /// <param name="addressOrHash">account address or scripthash string</param>
        /// <returns></returns>
        public static UInt160 ToUInt160(this string addressOrHash)
        {
            if (string.IsNullOrEmpty(addressOrHash)) { throw new ArgumentNullException(); }
            if (addressOrHash.StartsWith("0x")) { addressOrHash = addressOrHash.Substring(2); }

            if (addressOrHash.Length == AddressLength)
            {
                return Wallets.Helper.ToScriptHash(addressOrHash);
            }
            else if (addressOrHash.Length == HashLength)
            {
                return UInt160.Parse(addressOrHash);
            }

            throw new FormatException();
        }

        /// <summary>
        /// Get the verification script hash of KeyPair
        /// </summary>
        /// <param name="key">account KeyPair</param>
        /// <returns></returns>
        public static UInt160 ToScriptHash(this KeyPair key)
        {
            return Contract.CreateSignatureRedeemScript(key.PublicKey).ToScriptHash();
        }

        /// <summary>
        /// Parse WIF or private key hex string to KeyPair
        /// </summary>
        /// <param name="key">WIF or private key hex string</param>
        /// <returns></returns>
        public static KeyPair ToKeyPair(this string key)
        {
            if (string.IsNullOrEmpty(key)) { throw new ArgumentNullException(); }
            if (key.StartsWith("0x")) { key = key.Substring(2); }

            if (key.Length == WifLength)
            {
                return new KeyPair(Wallet.GetPrivateKeyFromWIF(key));
            }
            else if (key.Length == PrivateKeyLength)
            {
                return new KeyPair(key.HexToBytes());
            }

            throw new FormatException();
        }

        /// <summary>
        /// Convert decimal amount to BigInteger: amount * 10 ^ decimals
        /// </summary>
        /// <param name="amount">float value</param>
        /// <param name="decimals">token decimals</param>
        /// <returns></returns>
        public static BigInteger ToBigInteger(this decimal amount, uint decimals)
        {
            BigInteger multiplier = BigInteger.Pow(10, (int)decimals);
            var (numerator, denominator) = Fraction(amount);
            var res = multiplier * numerator / denominator;
            return res;
            //var amountBuilder = new StringBuilder(amount.ToString());
            //for (int i = 0; i < decimals; i++)
            //{
            //    amountBuilder.Append("0");
            //}
            //string amountStr = amountBuilder.ToString();

            //int pointPos = amountStr.IndexOf('.');
            //if (pointPos > -1)
            //{
            //    amountStr = amountStr.Remove(pointPos, 1);
            //    amountStr = amountStr.Remove(pointPos + (int)decimals);
            //}

            //return BigInteger.Parse(amountStr);
        }

        static (BigInteger numerator, BigInteger denominator) Fraction(decimal d)
        {
            int[] bits = decimal.GetBits(d);
            BigInteger numerator = (1 - ((bits[3] >> 30) & 2)) *
                                   unchecked(((BigInteger)(uint)bits[2] << 64) |
                                             ((BigInteger)(uint)bits[1] << 32) |
                                              (BigInteger)(uint)bits[0]);
            BigInteger denominator = BigInteger.Pow(10, (bits[3] >> 16) & 0xff);
            return (numerator, denominator);
        }
    }
}
