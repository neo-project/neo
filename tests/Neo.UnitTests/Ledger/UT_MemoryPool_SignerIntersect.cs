// Copyright (C) 2015-2026 The Neo Project.
//
// UT_MemoryPool_SignerIntersect.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using System.Collections.Generic;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_MemoryPool_SignerIntersect
    {
        private static UInt160 Account(byte seed)
        {
            return new UInt160(Crypto.Hash160([seed]));
        }

        private static Signer[] Signers(params UInt160[] accounts)
        {
            var signers = new Signer[accounts.Length];
            for (int i = 0; i < accounts.Length; i++)
                signers[i] = new Signer { Account = accounts[i] };
            return signers;
        }

        private static Transaction TxWithConflicts(params UInt256[] hashes)
        {
            var attributes = new TransactionAttribute[hashes.Length];
            for (int i = 0; i < hashes.Length; i++)
                attributes[i] = new Conflicts { Hash = hashes[i] };
            return new Transaction
            {
                Signers = [new Signer { Account = UInt160.Zero }],
                Attributes = attributes,
                Script = new byte[] { 0x41 },
                Witnesses = [Witness.Empty]
            };
        }

        [TestMethod]
        public void ContainsAccount_Empty_IsFalse()
        {
            Assert.IsFalse(MemoryPool.ContainsAccount([], Account(1)));
        }

        [TestMethod]
        public void ContainsAccount_MatchesFirstAndLast()
        {
            var a = Account(1);
            var b = Account(2);
            var c = Account(3);
            var signers = Signers(a, b, c);

            Assert.IsTrue(MemoryPool.ContainsAccount(signers, a));
            Assert.IsTrue(MemoryPool.ContainsAccount(signers, b));
            Assert.IsTrue(MemoryPool.ContainsAccount(signers, c));
            Assert.IsFalse(MemoryPool.ContainsAccount(signers, Account(9)));
        }

        [TestMethod]
        public void HasCommonSigner_OverlapOnLastSigner()
        {
            var a = Account(1);
            var b = Account(2);
            var c = Account(3);
            var d = Account(4);

            Assert.IsFalse(MemoryPool.HasCommonSigner([], Signers(a)));
            Assert.IsFalse(MemoryPool.HasCommonSigner(Signers(a), []));
            Assert.IsFalse(MemoryPool.HasCommonSigner(Signers(a, b), Signers(c, d)));
            Assert.IsTrue(MemoryPool.HasCommonSigner(Signers(a, b), Signers(c, b)));
            Assert.IsTrue(MemoryPool.HasCommonSigner(Signers(b), Signers(a, b)));
        }

        [TestMethod]
        public void HasCommonAccount_OverlapOnSecondary()
        {
            var a = Account(1);
            var b = Account(2);
            var c = Account(3);

            Assert.IsFalse(MemoryPool.HasCommonAccount([], Signers(a)));
            Assert.IsFalse(MemoryPool.HasCommonAccount([a, c], Signers(b)));
            Assert.IsTrue(MemoryPool.HasCommonAccount([a, b], Signers(c, b)));
        }

        [TestMethod]
        public void ContainsPersistedConflict_HitsAndMisses()
        {
            var hit = UInt256.Zero;
            var miss = UInt256.Parse("0x0102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f20");
            var persisted = new HashSet<UInt256> { hit };

            Assert.IsFalse(MemoryPool.ContainsPersistedConflict(TxWithConflicts(), persisted));
            Assert.IsFalse(MemoryPool.ContainsPersistedConflict(TxWithConflicts(miss), persisted));
            Assert.IsTrue(MemoryPool.ContainsPersistedConflict(TxWithConflicts(miss, hit), persisted));
        }
    }
}
