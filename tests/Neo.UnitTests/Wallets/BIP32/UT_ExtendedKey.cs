// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ExtendedKey.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.Wallets.BIP32;
using System;

namespace Neo.UnitTests.Wallets.BIP32
{
    [TestClass]
    public class UT_ExtendedKey
    {
        [TestMethod]
        public void TestVectors()
        {
            // Test vectors from BIP-0032
            // https://github.com/bitcoin/bips/blob/master/bip-0032.mediawiki#test-vectors

            ECCurve curve = ECCurve.Secp256k1;

            byte[] seed = Convert.FromHexString("000102030405060708090a0b0c0d0e0f");

            string path = "m";
            string extprv = "xprv9s21ZrQH143K3QTDL4LXw2F7HEK3wJUD2nW2nRk4stbPy6cq3jPPqjiChkVvvNKmPGJxWUtg6LnF5kejMRNNU3TGtRBeJgk33yuGBxrMPHi";
            ReadOnlySpan<byte> expected = extprv.Base58CheckDecode();
            ExtendedKey extKey = ExtendedKey.Create(seed, path, curve);
            CollectionAssert.AreEqual(expected[13..45].ToArray(), extKey.ChainCode);
            CollectionAssert.AreEqual(expected[46..78].ToArray(), extKey.PrivateKey);

            path = "m/0'";
            extprv = "xprv9uHRZZhk6KAJC1avXpDAp4MDc3sQKNxDiPvvkX8Br5ngLNv1TxvUxt4cV1rGL5hj6KCesnDYUhd7oWgT11eZG7XnxHrnYeSvkzY7d2bhkJ7";
            expected = extprv.Base58CheckDecode();
            extKey = ExtendedKey.Create(seed, path, curve);
            CollectionAssert.AreEqual(expected[13..45].ToArray(), extKey.ChainCode);
            CollectionAssert.AreEqual(expected[46..78].ToArray(), extKey.PrivateKey);

            path = "m/0'/1";
            extprv = "xprv9wTYmMFdV23N2TdNG573QoEsfRrWKQgWeibmLntzniatZvR9BmLnvSxqu53Kw1UmYPxLgboyZQaXwTCg8MSY3H2EU4pWcQDnRnrVA1xe8fs";
            expected = extprv.Base58CheckDecode();
            extKey = ExtendedKey.Create(seed, path, curve);
            CollectionAssert.AreEqual(expected[13..45].ToArray(), extKey.ChainCode);
            CollectionAssert.AreEqual(expected[46..78].ToArray(), extKey.PrivateKey);

            path = "m/0'/1/2'";
            extprv = "xprv9z4pot5VBttmtdRTWfWQmoH1taj2axGVzFqSb8C9xaxKymcFzXBDptWmT7FwuEzG3ryjH4ktypQSAewRiNMjANTtpgP4mLTj34bhnZX7UiM";
            expected = extprv.Base58CheckDecode();
            extKey = ExtendedKey.Create(seed, path, curve);
            CollectionAssert.AreEqual(expected[13..45].ToArray(), extKey.ChainCode);
            CollectionAssert.AreEqual(expected[46..78].ToArray(), extKey.PrivateKey);

            path = "m/0'/1/2'/2";
            extprv = "xprvA2JDeKCSNNZky6uBCviVfJSKyQ1mDYahRjijr5idH2WwLsEd4Hsb2Tyh8RfQMuPh7f7RtyzTtdrbdqqsunu5Mm3wDvUAKRHSC34sJ7in334";
            expected = extprv.Base58CheckDecode();
            extKey = ExtendedKey.Create(seed, path, curve);
            CollectionAssert.AreEqual(expected[13..45].ToArray(), extKey.ChainCode);
            CollectionAssert.AreEqual(expected[46..78].ToArray(), extKey.PrivateKey);

            path = "m/0'/1/2'/2/1000000000";
            extprv = "xprvA41z7zogVVwxVSgdKUHDy1SKmdb533PjDz7J6N6mV6uS3ze1ai8FHa8kmHScGpWmj4WggLyQjgPie1rFSruoUihUZREPSL39UNdE3BBDu76";
            expected = extprv.Base58CheckDecode();
            extKey = ExtendedKey.Create(seed, path, curve);
            CollectionAssert.AreEqual(expected[13..45].ToArray(), extKey.ChainCode);
            CollectionAssert.AreEqual(expected[46..78].ToArray(), extKey.PrivateKey);

            seed = Convert.FromHexString("fffcf9f6f3f0edeae7e4e1dedbd8d5d2cfccc9c6c3c0bdbab7b4b1aeaba8a5a29f9c999693908d8a8784817e7b7875726f6c696663605d5a5754514e4b484542");

            path = "m";
            extprv = "xprv9s21ZrQH143K31xYSDQpPDxsXRTUcvj2iNHm5NUtrGiGG5e2DtALGdso3pGz6ssrdK4PFmM8NSpSBHNqPqm55Qn3LqFtT2emdEXVYsCzC2U";
            expected = extprv.Base58CheckDecode();
            extKey = ExtendedKey.Create(seed, path, curve);
            CollectionAssert.AreEqual(expected[13..45].ToArray(), extKey.ChainCode);
            CollectionAssert.AreEqual(expected[46..78].ToArray(), extKey.PrivateKey);

            path = "m/0";
            extprv = "xprv9vHkqa6EV4sPZHYqZznhT2NPtPCjKuDKGY38FBWLvgaDx45zo9WQRUT3dKYnjwih2yJD9mkrocEZXo1ex8G81dwSM1fwqWpWkeS3v86pgKt";
            expected = extprv.Base58CheckDecode();
            extKey = ExtendedKey.Create(seed, path, curve);
            CollectionAssert.AreEqual(expected[13..45].ToArray(), extKey.ChainCode);
            CollectionAssert.AreEqual(expected[46..78].ToArray(), extKey.PrivateKey);

            path = "m/0/2147483647'";
            extprv = "xprv9wSp6B7kry3Vj9m1zSnLvN3xH8RdsPP1Mh7fAaR7aRLcQMKTR2vidYEeEg2mUCTAwCd6vnxVrcjfy2kRgVsFawNzmjuHc2YmYRmagcEPdU9";
            expected = extprv.Base58CheckDecode();
            extKey = ExtendedKey.Create(seed, path, curve);
            CollectionAssert.AreEqual(expected[13..45].ToArray(), extKey.ChainCode);
            CollectionAssert.AreEqual(expected[46..78].ToArray(), extKey.PrivateKey);

            path = "m/0/2147483647'/1";
            extprv = "xprv9zFnWC6h2cLgpmSA46vutJzBcfJ8yaJGg8cX1e5StJh45BBciYTRXSd25UEPVuesF9yog62tGAQtHjXajPPdbRCHuWS6T8XA2ECKADdw4Ef";
            expected = extprv.Base58CheckDecode();
            extKey = ExtendedKey.Create(seed, path, curve);
            CollectionAssert.AreEqual(expected[13..45].ToArray(), extKey.ChainCode);
            CollectionAssert.AreEqual(expected[46..78].ToArray(), extKey.PrivateKey);

            path = "m/0/2147483647'/1/2147483646'";
            extprv = "xprvA1RpRA33e1JQ7ifknakTFpgNXPmW2YvmhqLQYMmrj4xJXXWYpDPS3xz7iAxn8L39njGVyuoseXzU6rcxFLJ8HFsTjSyQbLYnMpCqE2VbFWc";
            expected = extprv.Base58CheckDecode();
            extKey = ExtendedKey.Create(seed, path, curve);
            CollectionAssert.AreEqual(expected[13..45].ToArray(), extKey.ChainCode);
            CollectionAssert.AreEqual(expected[46..78].ToArray(), extKey.PrivateKey);

            path = "m/0/2147483647'/1/2147483646'/2";
            extprv = "xprvA2nrNbFZABcdryreWet9Ea4LvTJcGsqrMzxHx98MMrotbir7yrKCEXw7nadnHM8Dq38EGfSh6dqA9QWTyefMLEcBYJUuekgW4BYPJcr9E7j";
            expected = extprv.Base58CheckDecode();
            extKey = ExtendedKey.Create(seed, path, curve);
            CollectionAssert.AreEqual(expected[13..45].ToArray(), extKey.ChainCode);
            CollectionAssert.AreEqual(expected[46..78].ToArray(), extKey.PrivateKey);

            seed = Convert.FromHexString("4b381541583be4423346c643850da4b320e46a87ae3d2a4e6da11eba819cd4acba45d239319ac14f863b8d5ab5a0d0c64d2e8a1e7d1457df2e5a3c51c73235be");

            path = "m";
            extprv = "xprv9s21ZrQH143K25QhxbucbDDuQ4naNntJRi4KUfWT7xo4EKsHt2QJDu7KXp1A3u7Bi1j8ph3EGsZ9Xvz9dGuVrtHHs7pXeTzjuxBrCmmhgC6";
            expected = extprv.Base58CheckDecode();
            extKey = ExtendedKey.Create(seed, path, curve);
            CollectionAssert.AreEqual(expected[13..45].ToArray(), extKey.ChainCode);
            CollectionAssert.AreEqual(expected[46..78].ToArray(), extKey.PrivateKey);

            path = "m/0'";
            extprv = "xprv9uPDJpEQgRQfDcW7BkF7eTya6RPxXeJCqCJGHuCJ4GiRVLzkTXBAJMu2qaMWPrS7AANYqdq6vcBcBUdJCVVFceUvJFjaPdGZ2y9WACViL4L";
            expected = extprv.Base58CheckDecode();
            extKey = ExtendedKey.Create(seed, path, curve);
            CollectionAssert.AreEqual(expected[13..45].ToArray(), extKey.ChainCode);
            CollectionAssert.AreEqual(expected[46..78].ToArray(), extKey.PrivateKey);

            seed = Convert.FromHexString("3ddd5602285899a946114506157c7997e5444528f3003f6134712147db19b678");

            path = "m";
            extprv = "xprv9s21ZrQH143K48vGoLGRPxgo2JNkJ3J3fqkirQC2zVdk5Dgd5w14S7fRDyHH4dWNHUgkvsvNDCkvAwcSHNAQwhwgNMgZhLtQC63zxwhQmRv";
            expected = extprv.Base58CheckDecode();
            extKey = ExtendedKey.Create(seed, path, curve);
            CollectionAssert.AreEqual(expected[13..45].ToArray(), extKey.ChainCode);
            CollectionAssert.AreEqual(expected[46..78].ToArray(), extKey.PrivateKey);

            path = "m/0'";
            extprv = "xprv9vB7xEWwNp9kh1wQRfCCQMnZUEG21LpbR9NPCNN1dwhiZkjjeGRnaALmPXCX7SgjFTiCTT6bXes17boXtjq3xLpcDjzEuGLQBM5ohqkao9G";
            expected = extprv.Base58CheckDecode();
            extKey = ExtendedKey.Create(seed, path, curve);
            CollectionAssert.AreEqual(expected[13..45].ToArray(), extKey.ChainCode);
            CollectionAssert.AreEqual(expected[46..78].ToArray(), extKey.PrivateKey);

            path = "m/0'/1'";
            extprv = "xprv9xJocDuwtYCMNAo3Zw76WENQeAS6WGXQ55RCy7tDJ8oALr4FWkuVoHJeHVAcAqiZLE7Je3vZJHxspZdFHfnBEjHqU5hG1Jaj32dVoS6XLT1";
            expected = extprv.Base58CheckDecode();
            extKey = ExtendedKey.Create(seed, path, curve);
            CollectionAssert.AreEqual(expected[13..45].ToArray(), extKey.ChainCode);
            CollectionAssert.AreEqual(expected[46..78].ToArray(), extKey.PrivateKey);
        }
    }
}
