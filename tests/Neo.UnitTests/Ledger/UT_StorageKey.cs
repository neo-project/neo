// Copyright (C) 2015-2025 The Neo Project.
//
// UT_StorageKey.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.SmartContract;
using System;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_StorageKey
    {
        [TestMethod]
        public void SameTest()
        {
            // None
            var key = new KeyBuilder(1, 2);
            CollectionAssert.AreEqual(key.ToArray(), StorageKey.Create(1, 2).ToArray());

            // Byte[]
            key = new KeyBuilder(1, 2);
            key.Add([3, 4]);
            CollectionAssert.AreEqual(key.ToArray(), StorageKey.Create(1, 2, [3, 4]).ToArray());

            // Byte
            key = new KeyBuilder(1, 2);
            key.Add((byte)3);
            CollectionAssert.AreEqual(key.ToArray(), StorageKey.Create(1, 2, (byte)3).ToArray());

            // Int
            key = new KeyBuilder(1, 2);
            key.AddBigEndian((int)3);
            CollectionAssert.AreEqual(key.ToArray(), StorageKey.Create(1, 2, (int)3).ToArray());

            // UInt
            key = new KeyBuilder(1, 2);
            key.AddBigEndian((uint)3);
            CollectionAssert.AreEqual(key.ToArray(), StorageKey.Create(1, 2, (uint)3).ToArray());

            // Long
            key = new KeyBuilder(1, 2);
            key.AddBigEndian((long)3);
            CollectionAssert.AreEqual(key.ToArray(), StorageKey.Create(1, 2, (long)3).ToArray());

            // ULong
            key = new KeyBuilder(1, 2);
            key.AddBigEndian((ulong)3);
            CollectionAssert.AreEqual(key.ToArray(), StorageKey.Create(1, 2, (ulong)3).ToArray());

            // UInt160
            key = new KeyBuilder(1, 2);
            key.Add(UInt160.Parse("2d3b96ae1bcc5a585e075e3b81920210dec16302"));
            CollectionAssert.AreEqual(key.ToArray(), StorageKey.Create(1, 2, UInt160.Parse("2d3b96ae1bcc5a585e075e3b81920210dec16302")).ToArray());

            // UInt256
            key = new KeyBuilder(1, 2);
            key.Add(UInt256.Parse("0x761a9bb72ca2a63984db0cc43f943a2a25e464f62d1a91114c2b6fbbfd24b51d"));
            CollectionAssert.AreEqual(key.ToArray(), StorageKey.Create(1, 2,
                UInt256.Parse("0x761a9bb72ca2a63984db0cc43f943a2a25e464f62d1a91114c2b6fbbfd24b51d")).ToArray());

            // UInt256+UInt160
            key = new KeyBuilder(1, 2);
            key.Add(UInt256.Parse("0x761a9bb72ca2a63984db0cc43f943a2a25e464f62d1a91114c2b6fbbfd24b51d"));
            key.Add(UInt160.Parse("2d3b96ae1bcc5a585e075e3b81920210dec16302"));
            CollectionAssert.AreEqual(key.ToArray(), StorageKey.Create(1, 2,
                UInt256.Parse("0x761a9bb72ca2a63984db0cc43f943a2a25e464f62d1a91114c2b6fbbfd24b51d"),
                UInt160.Parse("2d3b96ae1bcc5a585e075e3b81920210dec16302")).ToArray());

            // ISerializable
            key = new KeyBuilder(1, 2);
            key.Add(ECCurve.Secp256r1.G);
            CollectionAssert.AreEqual(key.ToArray(), StorageKey.Create(1, 2, ECCurve.Secp256r1.G).ToArray());
        }

        [TestMethod]
        public void Id_Get()
        {
            var uut = new StorageKey { Id = 1, Key = new byte[] { 0x01 } };
            Assert.AreEqual(1, uut.Id);
        }

        [TestMethod]
        public void Id_Set()
        {
            int val = 1;
            StorageKey uut = new() { Id = val };
            Assert.AreEqual(val, uut.Id);
        }

        [TestMethod]
        public void Key_Set()
        {
            byte[] val = new byte[] { 0x42, 0x32 };
            StorageKey uut = new() { Key = val };
            Assert.AreEqual(2, uut.Key.Length);
            Assert.AreEqual(val[0], uut.Key.Span[0]);
            Assert.AreEqual(val[1], uut.Key.Span[1]);
        }

        [TestMethod]
        public void Equals_SameObj()
        {
            StorageKey uut = new();
            Assert.IsTrue(uut.Equals(uut));
        }

        [TestMethod]
        public void Equals_Null()
        {
            StorageKey uut = new();
            Assert.IsFalse(uut.Equals(null));
        }

        [TestMethod]
        public void Equals_SameHash_SameKey()
        {
            int val = 0x42000000;
            byte[] keyVal = TestUtils.GetByteArray(10, 0x42);
            StorageKey newSk = new StorageKey
            {
                Id = val,
                Key = keyVal
            };
            StorageKey uut = new() { Id = val, Key = keyVal };
            Assert.IsTrue(uut.Equals(newSk));
        }

        [TestMethod]
        public void Equals_DiffHash_SameKey()
        {
            int val = 0x42000000;
            byte[] keyVal = TestUtils.GetByteArray(10, 0x42);
            StorageKey newSk = new StorageKey
            {
                Id = val,
                Key = keyVal
            };
            StorageKey uut = new() { Id = 0x78000000, Key = keyVal };
            Assert.IsFalse(uut.Equals(newSk));
        }

        [TestMethod]
        public void Equals_SameHash_DiffKey()
        {
            int val = 0x42000000;
            byte[] keyVal = TestUtils.GetByteArray(10, 0x42);
            StorageKey newSk = new StorageKey
            {
                Id = val,
                Key = keyVal
            };
            StorageKey uut = new() { Id = val, Key = TestUtils.GetByteArray(10, 0x88) };
            Assert.IsFalse(uut.Equals(newSk));
        }

        [TestMethod]
        public void GetHashCode_Get()
        {
            var data = TestUtils.GetByteArray(10, 0x42);
            StorageKey uut = new() { Id = 0x42000000, Key = data };
            Assert.AreEqual(HashCode.Combine(0x42000000, data.XxHash3_32()), uut.GetHashCode());
        }

        [TestMethod]
        public void Equals_Obj()
        {
            StorageKey uut = new();
            Assert.IsFalse(uut.Equals(1u));
            Assert.IsTrue(uut.Equals((object)uut));
        }
    }
}
