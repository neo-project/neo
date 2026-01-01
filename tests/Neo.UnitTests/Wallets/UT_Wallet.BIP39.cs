// Copyright (C) 2015-2026 The Neo Project.
//
// UT_Wallet.BIP39.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Wallets;
using System;
using System.Globalization;

namespace Neo.UnitTests.Wallets
{
    [TestClass]
    public class WalletBIP39Tests
    {
        [TestMethod]
        public void EntropyTooShort_ThrowsArgumentException()
        {
            byte[] entropy = new byte[15]; // 120 bits, less than 128
            Assert.ThrowsExactly<ArgumentException>(() => Wallet.GetMnemonicCode(entropy));
        }

        [TestMethod]
        public void EntropyNotMultipleOf4_ThrowsArgumentException()
        {
            byte[] entropy = new byte[17]; // 136 bits, not multiple of 32 bits (not multiple of 4 bytes)
            Assert.ThrowsExactly<ArgumentException>(() => Wallet.GetMnemonicCode(entropy));
        }

        [TestMethod]
        public void InvariantCulture_EqualsEnglish()
        {
            byte[] entropy = new byte[16];
            for (int i = 0; i < entropy.Length; i++) entropy[i] = (byte)(i * 3 + 7);
            string[] english = Wallet.GetMnemonicCode(entropy, new CultureInfo("en"));
            string[] invariant = Wallet.GetMnemonicCode(entropy, CultureInfo.InvariantCulture);
            CollectionAssert.AreEqual(english, invariant);
        }

        [TestMethod]
        public void SpecificCulture_FallsBackToParentOrEnglish()
        {
            byte[] entropy = new byte[16];
            for (int i = 0; i < entropy.Length; i++) entropy[i] = (byte)(i + 1);
            // en-US typically falls back to en resource if en-US specific resource not present
            var en = Wallet.GetMnemonicCode(entropy.AsSpan(), new CultureInfo("en"));
            var enUs = Wallet.GetMnemonicCode(entropy.AsSpan(), new CultureInfo("en-US"));
            CollectionAssert.AreEqual(en, enUs);
        }

        [TestMethod]
        public void TestVectors()
        {
            // Test vectors from BIP39 standard
            // https://github.com/bitcoin/bips/blob/master/bip-0039.mediawiki#user-content-Test_vectors

            CultureInfo culture = new("en");

            byte[] entropy = Convert.FromHexString("00000000000000000000000000000000");
            string expectedMnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
            string mnemonic = string.Join(' ', Wallet.GetMnemonicCode(entropy, culture));
            Assert.AreEqual(expectedMnemonic, mnemonic);
            CollectionAssert.AreEquivalent(entropy, Wallet.MnemonicToEntropy(mnemonic.Split(' ')));

            entropy = Convert.FromHexString("7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f");
            expectedMnemonic = "legal winner thank year wave sausage worth useful legal winner thank yellow";
            mnemonic = string.Join(' ', Wallet.GetMnemonicCode(entropy, culture));
            Assert.AreEqual(expectedMnemonic, mnemonic);
            CollectionAssert.AreEquivalent(entropy, Wallet.MnemonicToEntropy(mnemonic.Split(' ')));

            entropy = Convert.FromHexString("80808080808080808080808080808080");
            expectedMnemonic = "letter advice cage absurd amount doctor acoustic avoid letter advice cage above";
            mnemonic = string.Join(' ', Wallet.GetMnemonicCode(entropy, culture));
            Assert.AreEqual(expectedMnemonic, mnemonic);
            CollectionAssert.AreEquivalent(entropy, Wallet.MnemonicToEntropy(mnemonic.Split(' ')));

            entropy = Convert.FromHexString("ffffffffffffffffffffffffffffffff");
            expectedMnemonic = "zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo wrong";
            mnemonic = string.Join(' ', Wallet.GetMnemonicCode(entropy, culture));
            Assert.AreEqual(expectedMnemonic, mnemonic);
            CollectionAssert.AreEquivalent(entropy, Wallet.MnemonicToEntropy(mnemonic.Split(' ')));

            entropy = Convert.FromHexString("000000000000000000000000000000000000000000000000");
            expectedMnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon agent";
            mnemonic = string.Join(' ', Wallet.GetMnemonicCode(entropy, culture));
            Assert.AreEqual(expectedMnemonic, mnemonic);
            CollectionAssert.AreEquivalent(entropy, Wallet.MnemonicToEntropy(mnemonic.Split(' ')));

            entropy = Convert.FromHexString("7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f");
            expectedMnemonic = "legal winner thank year wave sausage worth useful legal winner thank year wave sausage worth useful legal will";
            mnemonic = string.Join(' ', Wallet.GetMnemonicCode(entropy, culture));
            Assert.AreEqual(expectedMnemonic, mnemonic);
            CollectionAssert.AreEquivalent(entropy, Wallet.MnemonicToEntropy(mnemonic.Split(' ')));

            entropy = Convert.FromHexString("808080808080808080808080808080808080808080808080");
            expectedMnemonic = "letter advice cage absurd amount doctor acoustic avoid letter advice cage absurd amount doctor acoustic avoid letter always";
            mnemonic = string.Join(' ', Wallet.GetMnemonicCode(entropy, culture));
            Assert.AreEqual(expectedMnemonic, mnemonic);
            CollectionAssert.AreEquivalent(entropy, Wallet.MnemonicToEntropy(mnemonic.Split(' ')));

            entropy = Convert.FromHexString("ffffffffffffffffffffffffffffffffffffffffffffffff");
            expectedMnemonic = "zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo when";
            mnemonic = string.Join(' ', Wallet.GetMnemonicCode(entropy, culture));
            Assert.AreEqual(expectedMnemonic, mnemonic);
            CollectionAssert.AreEquivalent(entropy, Wallet.MnemonicToEntropy(mnemonic.Split(' ')));

            entropy = Convert.FromHexString("0000000000000000000000000000000000000000000000000000000000000000");
            expectedMnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon art";
            mnemonic = string.Join(' ', Wallet.GetMnemonicCode(entropy, culture));
            Assert.AreEqual(expectedMnemonic, mnemonic);
            CollectionAssert.AreEquivalent(entropy, Wallet.MnemonicToEntropy(mnemonic.Split(' ')));

            entropy = Convert.FromHexString("7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f");
            expectedMnemonic = "legal winner thank year wave sausage worth useful legal winner thank year wave sausage worth useful legal winner thank year wave sausage worth title";
            mnemonic = string.Join(' ', Wallet.GetMnemonicCode(entropy, culture));
            Assert.AreEqual(expectedMnemonic, mnemonic);
            CollectionAssert.AreEquivalent(entropy, Wallet.MnemonicToEntropy(mnemonic.Split(' ')));

            entropy = Convert.FromHexString("8080808080808080808080808080808080808080808080808080808080808080");
            expectedMnemonic = "letter advice cage absurd amount doctor acoustic avoid letter advice cage absurd amount doctor acoustic avoid letter advice cage absurd amount doctor acoustic bless";
            mnemonic = string.Join(' ', Wallet.GetMnemonicCode(entropy, culture));
            Assert.AreEqual(expectedMnemonic, mnemonic);
            CollectionAssert.AreEquivalent(entropy, Wallet.MnemonicToEntropy(mnemonic.Split(' ')));

            entropy = Convert.FromHexString("ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff");
            expectedMnemonic = "zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo vote";
            mnemonic = string.Join(' ', Wallet.GetMnemonicCode(entropy, culture));
            Assert.AreEqual(expectedMnemonic, mnemonic);
            CollectionAssert.AreEquivalent(entropy, Wallet.MnemonicToEntropy(mnemonic.Split(' ')));

            entropy = Convert.FromHexString("9e885d952ad362caeb4efe34a8e91bd2");
            expectedMnemonic = "ozone drill grab fiber curtain grace pudding thank cruise elder eight picnic";
            mnemonic = string.Join(' ', Wallet.GetMnemonicCode(entropy, culture));
            Assert.AreEqual(expectedMnemonic, mnemonic);
            CollectionAssert.AreEquivalent(entropy, Wallet.MnemonicToEntropy(mnemonic.Split(' ')));

            entropy = Convert.FromHexString("6610b25967cdcca9d59875f5cb50b0ea75433311869e930b");
            expectedMnemonic = "gravity machine north sort system female filter attitude volume fold club stay feature office ecology stable narrow fog";
            mnemonic = string.Join(' ', Wallet.GetMnemonicCode(entropy, culture));
            Assert.AreEqual(expectedMnemonic, mnemonic);
            CollectionAssert.AreEquivalent(entropy, Wallet.MnemonicToEntropy(mnemonic.Split(' ')));

            entropy = Convert.FromHexString("68a79eaca2324873eacc50cb9c6eca8cc68ea5d936f98787c60c7ebc74e6ce7c");
            expectedMnemonic = "hamster diagram private dutch cause delay private meat slide toddler razor book happy fancy gospel tennis maple dilemma loan word shrug inflict delay length";
            mnemonic = string.Join(' ', Wallet.GetMnemonicCode(entropy, culture));
            Assert.AreEqual(expectedMnemonic, mnemonic);
            CollectionAssert.AreEquivalent(entropy, Wallet.MnemonicToEntropy(mnemonic.Split(' ')));

            entropy = Convert.FromHexString("c0ba5a8e914111210f2bd131f3d5e08d");
            expectedMnemonic = "scheme spot photo card baby mountain device kick cradle pact join borrow";
            mnemonic = string.Join(' ', Wallet.GetMnemonicCode(entropy, culture));
            Assert.AreEqual(expectedMnemonic, mnemonic);
            CollectionAssert.AreEquivalent(entropy, Wallet.MnemonicToEntropy(mnemonic.Split(' ')));

            entropy = Convert.FromHexString("6d9be1ee6ebd27a258115aad99b7317b9c8d28b6d76431c3");
            expectedMnemonic = "horn tenant knee talent sponsor spell gate clip pulse soap slush warm silver nephew swap uncle crack brave";
            mnemonic = string.Join(' ', Wallet.GetMnemonicCode(entropy, culture));
            Assert.AreEqual(expectedMnemonic, mnemonic);
            CollectionAssert.AreEquivalent(entropy, Wallet.MnemonicToEntropy(mnemonic.Split(' ')));

            entropy = Convert.FromHexString("9f6a2878b2520799a44ef18bc7df394e7061a224d2c33cd015b157d746869863");
            expectedMnemonic = "panda eyebrow bullet gorilla call smoke muffin taste mesh discover soft ostrich alcohol speed nation flash devote level hobby quick inner drive ghost inside";
            mnemonic = string.Join(' ', Wallet.GetMnemonicCode(entropy, culture));
            Assert.AreEqual(expectedMnemonic, mnemonic);
            CollectionAssert.AreEquivalent(entropy, Wallet.MnemonicToEntropy(mnemonic.Split(' ')));

            entropy = Convert.FromHexString("23db8160a31d3e0dca3688ed941adbf3");
            expectedMnemonic = "cat swing flag economy stadium alone churn speed unique patch report train";
            mnemonic = string.Join(' ', Wallet.GetMnemonicCode(entropy, culture));
            Assert.AreEqual(expectedMnemonic, mnemonic);
            CollectionAssert.AreEquivalent(entropy, Wallet.MnemonicToEntropy(mnemonic.Split(' ')));

            entropy = Convert.FromHexString("8197a4a47f0425faeaa69deebc05ca29c0a5b5cc76ceacc0");
            expectedMnemonic = "light rule cinnamon wrap drastic word pride squirrel upgrade then income fatal apart sustain crack supply proud access";
            mnemonic = string.Join(' ', Wallet.GetMnemonicCode(entropy, culture));
            Assert.AreEqual(expectedMnemonic, mnemonic);
            CollectionAssert.AreEquivalent(entropy, Wallet.MnemonicToEntropy(mnemonic.Split(' ')));

            entropy = Convert.FromHexString("066dca1a2bb7e8a1db2832148ce9933eea0f3ac9548d793112d9a95c9407efad");
            expectedMnemonic = "all hour make first leader extend hole alien behind guard gospel lava path output census museum junior mass reopen famous sing advance salt reform";
            mnemonic = string.Join(' ', Wallet.GetMnemonicCode(entropy, culture));
            Assert.AreEqual(expectedMnemonic, mnemonic);
            CollectionAssert.AreEquivalent(entropy, Wallet.MnemonicToEntropy(mnemonic.Split(' ')));

            entropy = Convert.FromHexString("f30f8c1da665478f49b001d94c5fc452");
            expectedMnemonic = "vessel ladder alter error federal sibling chat ability sun glass valve picture";
            mnemonic = string.Join(' ', Wallet.GetMnemonicCode(entropy, culture));
            Assert.AreEqual(expectedMnemonic, mnemonic);
            CollectionAssert.AreEquivalent(entropy, Wallet.MnemonicToEntropy(mnemonic.Split(' ')));

            entropy = Convert.FromHexString("c10ec20dc3cd9f652c7fac2f1230f7a3c828389a14392f05");
            expectedMnemonic = "scissors invite lock maple supreme raw rapid void congress muscle digital elegant little brisk hair mango congress clump";
            mnemonic = string.Join(' ', Wallet.GetMnemonicCode(entropy, culture));
            Assert.AreEqual(expectedMnemonic, mnemonic);
            CollectionAssert.AreEquivalent(entropy, Wallet.MnemonicToEntropy(mnemonic.Split(' ')));

            entropy = Convert.FromHexString("f585c11aec520db57dd353c69554b21a89b20fb0650966fa0a9d6f74fd989d8f");
            expectedMnemonic = "void come effort suffer camp survey warrior heavy shoot primary clutch crush open amazing screen patrol group space point ten exist slush involve unfold";
            mnemonic = string.Join(' ', Wallet.GetMnemonicCode(entropy, culture));
            Assert.AreEqual(expectedMnemonic, mnemonic);
            CollectionAssert.AreEquivalent(entropy, Wallet.MnemonicToEntropy(mnemonic.Split(' ')));
        }
    }
}
