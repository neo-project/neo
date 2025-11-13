// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ProofOfWork.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Extensions;
using System;
using System.Buffers.Binary;

namespace Neo.UnitTests.Cryptography
{
    [TestClass]
    public class UT_ProofOfWork
    {
        [TestMethod]
        public void VerifyDifficulty_ShouldReturnTrue_WhenProofIsBelowDifficulty()
        {
            var proofBytes = new byte[32];
            BinaryPrimitives.WriteInt64BigEndian(proofBytes, 10);

            var proof = new UInt256(proofBytes);
            var result = ProofOfWork.VerifyDifficulty(proof, 100);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void VerifyDifficulty_ShouldReturnFalse_WhenProofIsAboveDifficulty()
        {
            var proofBytes = new byte[32];
            BinaryPrimitives.WriteInt64BigEndian(proofBytes, 200);

            var proof = new UInt256(proofBytes);
            var result = ProofOfWork.VerifyDifficulty(proof, 100);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ComputeNonce_ShouldReturnNonceMeetingDifficulty()
        {
            var blockHash = new UInt256(new byte[32]);
            ulong difficulty = 0x00FFFFFFFFFFFFFF; // very low difficulty for quick test

            var nonce = ProofOfWork.ComputeNonce(blockHash, difficulty);
            var pow = ProofOfWork.Compute(blockHash, nonce);

            Assert.IsTrue(ProofOfWork.VerifyDifficulty(pow, difficulty));
            Assert.AreEqual(0, pow.ToArray()[0]);
        }
    }
}
