// Copyright (C) 2015-2026 The Neo Project.
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
            Assert.AreSequenceEqual(key.ToArray(), StorageKey.Create(1, 2).ToArray());

            // Byte[]
            key = new KeyBuilder(1, 2);
            key.Add([3, 4]);
            Assert.AreSequenceEqual(key.ToArray(), StorageKey.Create(1, 2, [3, 4]).ToArray());

            // Byte
            key = new KeyBuilder(1, 2);
            key.Add((byte)3);
            Assert.AreSequenceEqual(key.ToArray(), StorageKey.Create(1, 2, (byte)3).ToArray());

            // Int
            key = new KeyBuilder(1, 2);
            key.AddBigEndian((int)3);
            Assert.AreSequenceEqual(key.ToArray(), StorageKey.Create(1, 2, (int)3).ToArray());

            // UInt
            key = new KeyBuilder(1, 2);
            key.AddBigEndian((uint)3);
            Assert.AreSequenceEqual(key.ToArray(), StorageKey.Create(1, 2, (uint)3).ToArray());

            // Long
            key = new KeyBuilder(1, 2);
            key.AddBigEndian((long)3);
            Assert.AreSequenceEqual(key.ToArray(), StorageKey.Create(1, 2, (long)3).ToArray());

            // ULong
            key = new KeyBuilder(1, 2);
            key.AddBigEndian((ulong)3);
            Assert.AreSequenceEqual(key.ToArray(), StorageKey.Create(1, 2, (ulong)3).ToArray());

            // UInt160
            key = new KeyBuilder(1, 2);
            key.Add(UInt160.Parse("2d3b96ae1bcc5a585e075e3b81920210dec16302"));
            Assert.AreSequenceEqual(key.ToArray(), StorageKey.Create(1, 2, UInt160.Parse("2d3b96ae1bcc5a585e075e3b81920210dec16302")).ToArray());

            // UInt256
            key = new KeyBuilder(1, 2);
            key.Add(UInt256.Parse("0x761a9bb72ca2a63984db0cc43f943a2a25e464f62d1a91114c2b6fbbfd24b51d"));
            Assert.AreSequenceEqual(key.ToArray(), StorageKey.Create(1, 2,
                UInt256.Parse("0x761a9bb72ca2a63984db0cc43f943a2a25e464f62d1a91114c2b6fbbfd24b51d")).ToArray());

            // UInt256+UInt160
            key = new KeyBuilder(1, 2);
            key.Add(UInt256.Parse("0x761a9bb72ca2a63984db0cc43f943a2a25e464f62d1a91114c2b6fbbfd24b51d"));
            key.Add(UInt160.Parse("2d3b96ae1bcc5a585e075e3b81920210dec16302"));
            Assert.AreSequenceEqual(key.ToArray(), StorageKey.Create(1, 2,
                UInt256.Parse("0x761a9bb72ca2a63984db0cc43f943a2a25e464f62d1a91114c2b6fbbfd24b51d"),
                UInt160.Parse("2d3b96ae1bcc5a585e075e3b81920210dec16302")).ToArray());

            // UInt160+Int
            key = new KeyBuilder(1, 2);
            key.Add(UInt160.Parse("2d3b96ae1bcc5a585e075e3b81920210dec16302"));
            key.AddBigEndian(123); // method Offset

            Assert.AreSequenceEqual(key.ToArray(), StorageKey.Create(1, 2,
                UInt160.Parse("2d3b96ae1bcc5a585e075e3b81920210dec16302"), 123).ToArray());

            // ISerializable
            key = new KeyBuilder(1, 2);
            key.Add(ECCurve.Secp256r1.G);
            Assert.AreSequenceEqual(key.ToArray(), StorageKey.Create(1, 2, ECCurve.Secp256r1.G).ToArray());
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
            var val = 1;
            StorageKey uut = new() { Id = val };
            Assert.AreEqual(val, uut.Id);
        }

        [TestMethod]
        public void Key_Set()
        {
            byte[] val = [0x42, 0x32];
            StorageKey uut = new() { Key = val };
            Assert.HasCount(2, uut.Key);
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
            var val = 0x42000000;
            var keyVal = TestUtils.GetByteArray(10, 0x42);
            var newSk = new StorageKey
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
            var val = 0x42000000;
            var keyVal = TestUtils.GetByteArray(10, 0x42);
            var newSk = new StorageKey
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
            var val = 0x42000000;
            var keyVal = TestUtils.GetByteArray(10, 0x42);
            var newSk = new StorageKey
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

        // Builds the serialized form (little-endian id + key), the same buffer ToArray() returns.
        private static byte[] Serialized(int id, byte[] key)
        {
            var buffer = new byte[sizeof(int) + key.Length];
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(buffer, id);
            key.CopyTo(buffer, sizeof(int));
            return buffer;
        }

        [TestMethod]
        public void StartsWith_CachedKey_MatchesSerializedForm()
        {
            // StorageKey.Create fills _cache eagerly.
            var key = StorageKey.Create(1, 2, [3, 4]);
            var serializedPrefix = Serialized(1, [2, 3]);
            Assert.IsTrue(key.StartsWith(serializedPrefix));

            // A prefix that matches only Key (without the 4-byte id) must not match.
            Assert.IsFalse(key.StartsWith([2, 3]));
        }

        [TestMethod]
        public void StartsWith_UncachedKey_MatchesSerializedForm()
        {
            // Object-initializer keys leave _cache empty until Build() runs.
            var key = new StorageKey { Id = 1, Key = new byte[] { 2, 3, 4 } };
            var serializedPrefix = Serialized(1, [2, 3]);
            Assert.IsTrue(key.StartsWith(serializedPrefix));

            // A prefix that matches only Key (without the 4-byte id) must not match.
            Assert.IsFalse(key.StartsWith([2, 3]));
        }

        [TestMethod]
        public void SequenceEqual_CachedKey_MatchesSerializedForm()
        {
            var key = StorageKey.Create(1, 2, [3, 4]);
            Assert.IsTrue(key.SequenceEqual(Serialized(1, [2, 3, 4])));

            // Key alone (without the 4-byte id) must not be considered equal.
            Assert.IsFalse(key.SequenceEqual([2, 3, 4]));
        }

        [TestMethod]
        public void SequenceEqual_UncachedKey_MatchesSerializedForm()
        {
            var key = new StorageKey { Id = 1, Key = new byte[] { 2, 3, 4 } };
            Assert.IsTrue(key.SequenceEqual(Serialized(1, [2, 3, 4])));

            // Key alone (without the 4-byte id) must not be considered equal.
            Assert.IsFalse(key.SequenceEqual([2, 3, 4]));
        }

        [TestMethod]
        public void Compare_CachedKey_MatchesSerializedForm()
        {
            var key = StorageKey.Create(1, 2, [3, 4]);
            var comparer = ByteArrayComparer.Default;

            Assert.AreEqual(0, key.Compare(comparer, Serialized(1, [2, 3, 4])));
            Assert.IsGreaterThan(0, key.Compare(comparer, Serialized(1, [2, 3])));
        }

        [TestMethod]
        public void Compare_UncachedKey_MatchesSerializedForm()
        {
            var key = new StorageKey { Id = 1, Key = new byte[] { 2, 3, 4 } };
            var comparer = ByteArrayComparer.Default;

            Assert.AreEqual(0, key.Compare(comparer, Serialized(1, [2, 3, 4])));
            Assert.IsGreaterThan(0, key.Compare(comparer, Serialized(1, [2, 3])));
        }
    }
}
